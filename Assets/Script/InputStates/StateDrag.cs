// 完全重写 StateDrag.cs

using System.Collections.Generic;
using UnityEngine;
using Assets.Script.My.Extention;

public class StateDrag : IInputState
{
    private GameObject primaryTarget;
    private Vector3 startMouseScreenPos;
    private Vector3 startMouseWorldPos;
    private bool isDragging = false;
    private const float DRAG_THRESHOLD = 5f;

    // 节点起始信息
    private Dictionary<GameObject, NodeMoveInfo> nodeInfos = new Dictionary<GameObject, NodeMoveInfo>();

    // 锚点起始信息
    private Dictionary<string, AnchorMoveInfo> anchorInfos = new Dictionary<string, AnchorMoveInfo>();

    // 未选中但受影响的锚点（属于选中节点，但锚点本身未被选中）
    private Dictionary<string, UnselectedAnchorInfo> unselectedAnchorInfos = new Dictionary<string, UnselectedAnchorInfo>();

    // 后继节点的射线信息（需要更新起点）
    private Dictionary<int, List<SuccessorLineInfo>> successorLineInfos = new Dictionary<int, List<SuccessorLineInfo>>();

    private struct NodeMoveInfo
    {
        public Node node;
        public Vector3Int startGridPos;
        public Vector3 startWorldPos;
        public List<LineRendererInfo> lineRenderers;
    }

    private struct LineRendererInfo
    {
        public LineRenderer lr;
        public string preNodeId;
        public Vector3 startEndPoint;
    }

    private struct AnchorMoveInfo
    {
        public int targetNodeId;
        public string preNodeId;
        public int anchorIndex;
        public Vector3Int startGridPos;
        public Vector3 startWorldPos;
        public GameObject anchorObj;
        public LineRenderer lr;
    }

    private struct UnselectedAnchorInfo
    {
        public int targetNodeId;
        public string preNodeId;
        public int anchorIndex;
        public Vector3 startWorldPos;
        public GameObject anchorObj;
        public LineRenderer lr;
    }

    private struct SuccessorLineInfo
    {
        public int successorNodeId;
        public LineRenderer lr;
        public int preNodeId; // 被移动的前置节点ID
    }

    private bool isPrimaryTargetNode;
    private bool isPrimaryTargetAnchor;
    private bool isEditMode;

    public StateDrag(GameObject target, Vector3 mousePos)
    {
        this.primaryTarget = target;
        this.startMouseScreenPos = mousePos;
        this.isPrimaryTargetNode = target.tag == Constants.Tags.Node;
        this.isPrimaryTargetAnchor = target.tag == Constants.Tags.Anchor;
    }

    public void OnEnter(InputManager context)
    {
        startMouseWorldPos = context.GetMouseWorldPos();
        isEditMode = context.CurrentEditPanel != null;

        HandleSelectionOnEnter(context);
        RecordStartPositions(context);
    }

    private void HandleSelectionOnEnter(InputManager context)
    {
        bool isShift = Input.GetKey(KeyCode.LeftShift);

        if (isPrimaryTargetNode)
        {
            if (!isShift)
            {
                if (!SelectionManager.Instance.ContainsNode(primaryTarget))
                {
                    SelectionManager.Instance.ClearNodes();
                    SelectionManager.Instance.ClearAnchors();
                    SelectionManager.Instance.AddNode(primaryTarget);
                }
            }
        }
        else if (isPrimaryTargetAnchor)
        {
            if (!isShift)
            {
                if (!SelectionManager.Instance.ContainsAnchor(primaryTarget))
                {
                    SelectionManager.Instance.ClearAnchors();
                    SelectionManager.Instance.AddAnchor(primaryTarget);
                }
            }
            SelectionManager.Instance.SelectAnchor(primaryTarget);
        }
    }

    private void RecordStartPositions(InputManager context)
    {
        var grid = context.UI.grid;
        var selectedAnchorPositions = SelectionManager.Instance.GetSelectedAnchorPositions();

        // 在编辑模式下，如果主要目标是锚点，则不记录节点（不移动节点）
        bool shouldRecordNodes = !(isEditMode && isPrimaryTargetAnchor);

        if (shouldRecordNodes)
        {
            // 记录选中的节点
            foreach (var nodeObj in SelectionManager.Instance.GetSelectedNodes())
            {
                if (nodeObj == null) continue;
                var node = nodeObj.GetComponent<Node>();
                if (node == null) continue;

                RecordNodeInfo(context, nodeObj, node, grid, selectedAnchorPositions);
            }
        }

        // 记录选中的锚点
        foreach (var kvp in selectedAnchorPositions)
        {
            var parts = kvp.Key.Split(new string[] { "->" }, System.StringSplitOptions.None);
            int targetNodeId = int.Parse(parts[0]);
            string preNodeId = parts[1];
            int anchorIndex = int.Parse(parts[2]);
            var anchorObj = FindAnchorGameObject(context, targetNodeId, preNodeId, anchorIndex);
            if (anchorObj == null) continue;
            var lr = anchorObj.GetComponentInParent<LineRenderer>();
            // 关键修改：从GameObject的实际位置获取，而不是用存储的位置
            Vector3 actualWorldPos = anchorObj.transform.position;
            Vector3Int actualGridPos = grid.WorldToCell(actualWorldPos);
            anchorInfos[kvp.Key] = new AnchorMoveInfo
            {
                targetNodeId = targetNodeId,
                preNodeId = preNodeId,
                anchorIndex = anchorIndex,
                startGridPos = actualGridPos,
                startWorldPos = actualWorldPos,
                anchorObj = anchorObj,
                lr = lr
            };
        }
    }

    private void RecordNodeInfo(InputManager context, GameObject nodeObj, Node node, Grid grid,
        Dictionary<string, Vector3Int> selectedAnchorPositions)
    {
        var info = new NodeMoveInfo
        {
            node = node,
            startGridPos = new Vector3Int(node.sc.HexGridY, node.sc.HexGridX, 0),
            startWorldPos = nodeObj.transform.position,
            lineRenderers = new List<LineRendererInfo>()
        };

        // 收集该节点下的所有LineRenderer（该节点作为目标节点的射线）
        foreach (Transform child in nodeObj.transform)
        {
            if (child.tag == Constants.Tags.NodeLine)
            {
                var lr = child.GetComponent<LineRenderer>();
                if (lr != null && lr.positionCount > 0)
                {
                    var lineName = child.name;
                    var parts = lineName.Split(new string[] { "->" }, System.StringSplitOptions.None);
                    string preNodeId = parts[0];

                    info.lineRenderers.Add(new LineRendererInfo
                    {
                        lr = lr,
                        preNodeId = preNodeId,
                        startEndPoint = lr.GetPosition(lr.positionCount - 1)
                    });

                    // 收集该LineRenderer下未被选中的锚点
                    for (int i = 1; i < lr.positionCount - 1; i++)
                    {
                        string anchorKey = $"{node.sc.Id}->{preNodeId}->{i}";

                        if (!selectedAnchorPositions.ContainsKey(anchorKey))
                        {
                            var anchorTransform = child.Find(i.ToString());
                            if (anchorTransform != null)
                            {
                                unselectedAnchorInfos[anchorKey] = new UnselectedAnchorInfo
                                {
                                    targetNodeId = node.sc.Id,
                                    preNodeId = preNodeId,
                                    anchorIndex = i,
                                    startWorldPos = anchorTransform.position,
                                    anchorObj = anchorTransform.gameObject,
                                    lr = lr
                                };
                            }
                        }
                    }
                }
            }
        }

        nodeInfos[nodeObj] = info;

        // 收集后继节点的射线信息（这些射线的起点需要更新）
        CollectSuccessorLineInfos(context, node);
    }

    /// <summary>
    /// 收集后继节点的射线信息
    /// </summary>
    private void CollectSuccessorLineInfos(InputManager context, Node node)
    {
        int nodeId = node.sc.Id;

        if (!successorLineInfos.ContainsKey(nodeId))
        {
            successorLineInfos[nodeId] = new List<SuccessorLineInfo>();
        }

        foreach (var successorId in node.sc.After_technology)
        {
            // 查找后继节点
            var successorTransform = context.UI.tilemap.transform.Find(successorId.ToString());
            if (successorTransform == null) continue;

            // 查找后继节点中，以当前节点为起点的射线
            string lineName = $"{nodeId}->{successorId}";
            var lineTransform = successorTransform.Find(lineName);
            if (lineTransform == null) continue;

            var lr = lineTransform.GetComponent<LineRenderer>();
            if (lr == null || lr.positionCount == 0) continue;

            successorLineInfos[nodeId].Add(new SuccessorLineInfo
            {
                successorNodeId = successorId,
                lr = lr,
                preNodeId = nodeId
            });
        }
    }

    private GameObject FindAnchorGameObject(InputManager context, int targetNodeId, string preNodeId, int anchorIndex)
    {
        var nodeTransform = context.UI.tilemap.transform.Find(targetNodeId.ToString());
        if (nodeTransform == null) return null;

        var lineTransform = nodeTransform.Find($"{preNodeId}->{targetNodeId}");
        if (lineTransform == null) return null;

        var anchorTransform = lineTransform.Find(anchorIndex.ToString());
        return anchorTransform?.gameObject;
    }

    public void OnUpdate(InputManager context)
    {
        if (!isDragging && Vector3.Distance(Input.mousePosition, startMouseScreenPos) > DRAG_THRESHOLD)
        {
            isDragging = true;
            if (isPrimaryTargetNode)
            {
                primaryTarget.GetComponent<Node>()?.SetHoverStyle(true);
            }
        }

        if (isDragging)
        {
            PerformDrag(context);
        }

        if (Input.GetMouseButtonUp(0))
        {
            context.ChangeState(new StateIdle());
        }
    }

    private void PerformDrag(InputManager context)
    {
        Vector3 currentMouseWorldPos = context.GetMouseWorldPos();
        Vector3 worldDelta = currentMouseWorldPos - startMouseWorldPos;
        var grid = context.UI.grid;

        // 收集被移动的节点ID及其新位置（用于更新后继节点的射线起点）
        Dictionary<int, Vector3> movedNodeNewPositions = new Dictionary<int, Vector3>();

        // 1. 移动选中的节点
        foreach (var kvp in nodeInfos)
        {
            GameObject nodeObj = kvp.Key;
            if (nodeObj == null) continue;

            NodeMoveInfo info = kvp.Value;

            Vector3 targetWorldPos = info.startWorldPos + worldDelta;
            Vector3Int targetGridPos = grid.WorldToCell(targetWorldPos);
            Vector3 snappedWorldPos = grid.CellToWorld(targetGridPos);
            snappedWorldPos.z = 0;

            // 更新节点位置
            nodeObj.transform.position = snappedWorldPos;

            // 更新节点数据
            info.node.sc.HexGridX = targetGridPos.y;
            info.node.sc.HexGridY = targetGridPos.x;

            // 记录新位置
            movedNodeNewPositions[info.node.sc.Id] = snappedWorldPos;

            // 更新该节点下所有LineRenderer的末端点（该节点作为目标节点）
            foreach (var lrInfo in info.lineRenderers)
            {
                Vector3 newEndPoint = snappedWorldPos + Vector3.forward;
                lrInfo.lr.SetPosition(lrInfo.lr.positionCount - 1, newEndPoint);

                // 如果前置节点也被移动了，更新射线起点
                if (int.TryParse(lrInfo.preNodeId, out int preNodeId))
                {
                    if (movedNodeNewPositions.TryGetValue(preNodeId, out Vector3 preNodeNewPos))
                    {
                        Vector3 newStartPoint = preNodeNewPos + Vector3.forward;
                        lrInfo.lr.SetPosition(0, newStartPoint);
                    }
                }
            }
        }

        // 2. 更新后继节点的射线起点
        foreach (var kvp in successorLineInfos)
        {
            int movedNodeId = kvp.Key;
            if (!movedNodeNewPositions.TryGetValue(movedNodeId, out Vector3 newPos)) continue;

            foreach (var lineInfo in kvp.Value)
            {
                // 检查后继节点是否也被移动了
                bool successorAlsoMoved = false;
                foreach (var nodeKvp in nodeInfos)
                {
                    if (nodeKvp.Value.node.sc.Id == lineInfo.successorNodeId)
                    {
                        successorAlsoMoved = true;
                        break;
                    }
                }

                // 更新射线起点
                Vector3 newStartPoint = newPos + Vector3.forward;
                lineInfo.lr.SetPosition(0, newStartPoint);

                // 如果后继节点也被移动了，射线终点已经在上面的循环中更新了
                // 如果后继节点没有被移动，射线终点保持不变
            }
        }

        // 3. 保持未选中锚点的世界坐标不变
        foreach (var kvp in unselectedAnchorInfos)
        {
            var info = kvp.Value;
            if (info.anchorObj == null || info.lr == null) continue;

            info.anchorObj.transform.position = info.startWorldPos;

            Vector3 linePos = new Vector3(info.startWorldPos.x, info.startWorldPos.y, 1);
            info.lr.SetPosition(info.anchorIndex, linePos);
        }

        // 4. 移动选中的锚点
        foreach (var kvp in anchorInfos)
        {
            var info = kvp.Value;
            if (info.anchorObj == null) continue;

            Vector3 targetWorldPos = info.startWorldPos + worldDelta;
            Vector3Int targetGridPos = grid.WorldToCell(targetWorldPos);
            Vector3 snappedWorldPos = grid.CellToWorld(targetGridPos);

            info.anchorObj.transform.position = new Vector2(snappedWorldPos.x, snappedWorldPos.y);

            if (info.lr != null)
            {
                Vector3 linePos = new Vector3(snappedWorldPos.x, snappedWorldPos.y, 1);
                info.lr.SetPosition(info.anchorIndex, linePos);
            }
        }
    }

    public void OnExit(InputManager context)
    {
        if (isPrimaryTargetNode && primaryTarget != null)
        {
            primaryTarget.GetComponent<Node>()?.SetHoverStyle(false);
        }

        if (isDragging)
        {
            CommitMoveCommands(context);
            RefreshAffectedNodes(context);
        }
        else
        {
            HandleClickSelection();
        }
    }

    private void CommitMoveCommands(InputManager context)
    {
        var grid = context.UI.grid;
        var batchCmd = new BatchMoveCommand();
        bool hasNodeMoved = false;
        // 处理节点移动命令
        foreach (var kvp in nodeInfos)
        {
            GameObject nodeObj = kvp.Key;
            if (nodeObj == null) continue;
            NodeMoveInfo info = kvp.Value;
            Vector3Int currentPos = new Vector3Int(info.node.sc.HexGridY, info.node.sc.HexGridX, 0);
            if (info.startGridPos != currentPos)
            {
                hasNodeMoved = true;
                batchCmd.Add(new MoveNodeCommand(info.node, info.startGridPos, currentPos, grid));
            }
        }
        // 处理锚点数据更新
        foreach (var kvp in anchorInfos)
        {
            var info = kvp.Value;
            if (info.anchorObj == null) continue;
            Vector3Int currentPos = grid.WorldToCell(info.anchorObj.transform.position);
            if (info.startGridPos != currentPos)
            {
                // 更新 PathNode 数据
                UpdateAnchorInPathNode(context, info);

                // 同步更新 SelectionManager 中存储的锚点位置
                SelectionManager.Instance.UpdateAnchorPosition(
                    info.targetNodeId,
                    info.preNodeId,
                    info.anchorIndex,
                    currentPos
                );
            }
        }
        if (hasNodeMoved && batchCmd.HasCommands())
        {
            CommandManager.Instance.ExecuteCommand(batchCmd);
        }
    }

    private void UpdateAnchorInPathNode(InputManager context, AnchorMoveInfo info)
    {
        if (!DataManager.Instance.TryGetScience(info.targetNodeId, out var sc)) return;
        if (info.lr == null) return;

        string newPath = null;
        var grid = context.UI.grid;

        for (int i = 1; i < info.lr.positionCount - 1; i++)
        {
            if (newPath != null) newPath += "_";
            var cellPos = grid.WorldToCell(info.lr.GetPosition(i));
            newPath += $"{cellPos.y}_{cellPos.x}";
        }

        if (string.IsNullOrEmpty(newPath))
        {
            sc.PathNode = sc.PathNode.RemoveIdPrePath(info.preNodeId);
        }
        else
        {
            sc.PathNode = sc.PathNode.UpdatePathNodeById(info.preNodeId, newPath);
        }

        if (context.CurrentEditPanel != null)
        {
            var panelScript = context.CurrentEditPanel.GetComponent<PanelScienceEdit>();
            if (panelScript.sc.Id == info.targetNodeId)
            {
                panelScript.UpdatePrePath(sc.PathNode);
            }
        }
    }

    private void RefreshAffectedNodes(InputManager context)
    {
        HashSet<int> affectedNodeIds = new HashSet<int>();

        // 收集被移动的节点及其后继节点
        foreach (var kvp in nodeInfos)
        {
            var node = kvp.Value.node;
            if (node == null) continue;

            affectedNodeIds.Add(node.sc.Id);
            foreach (var afterId in node.sc.After_technology)
            {
                affectedNodeIds.Add(afterId);
            }
        }

        // 收集锚点所属的节点
        foreach (var kvp in anchorInfos)
        {
            affectedNodeIds.Add(kvp.Value.targetNodeId);
        }

        // 刷新节点外观
        foreach (var nodeId in affectedNodeIds)
        {
            var node = NodeManager.Instance.GetNode(nodeId);
            if (node != null)
            {
                node.UpdateLineOnly();
            }
        }
    }

    private void HandleClickSelection()
    {
        bool isShift = Input.GetKey(KeyCode.LeftShift);

        if (isPrimaryTargetNode)
        {
            if (isShift)
            {
                SelectionManager.Instance.ToggleSelection(primaryTarget);
            }
            else
            {
                SelectionManager.Instance.ClearNodes();
                SelectionManager.Instance.ClearAnchors();
                SelectionManager.Instance.AddNode(primaryTarget);
            }
        }
        else if (isPrimaryTargetAnchor)
        {
            if (isShift)
            {
                SelectionManager.Instance.ToggleAnchorSelection(primaryTarget);
            }
            else
            {
                SelectionManager.Instance.ClearAnchors();
                SelectionManager.Instance.AddAnchor(primaryTarget);
            }
        }
    }
}
