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

    // 节点初始信息（使用世界坐标）
    private Dictionary<GameObject, NodeMoveInfo> nodeInfos = new Dictionary<GameObject, NodeMoveInfo>();

    // 锚点初始信息（使用世界坐标）
    private Dictionary<string, AnchorMoveInfo> anchorInfos = new Dictionary<string, AnchorMoveInfo>();

    // 未选中的受影响锚点
    private Dictionary<string, UnselectedAnchorInfo> unselectedAnchorInfos = new Dictionary<string, UnselectedAnchorInfo>();

    // 后继节点的连接线信息
    private Dictionary<string, List<SuccessorLineInfo>> successorLineInfos = new Dictionary<string, List<SuccessorLineInfo>>();

    private struct NodeMoveInfo
    {
        public Node node;
        public Vector2 startWorldPos;  // 世界坐标
        public List<LineRendererInfo> lineRenderers;
    }

    private struct LineRendererInfo
    {
        public LineRenderer lr;
        public string preNodeId;
        public Vector3 startEndPoint;
        public AnchorDirection startDirection;  // 新增：起始方向
        public AnchorDirection endDirection;    // 新增：终止方向
    }

    private struct AnchorMoveInfo
    {
        public string targetNodeId;
        public string preNodeId;
        public int anchorIndex;
        public Vector2 startWorldPos;  // 世界坐标
        public GameObject anchorObj;
        public LineRenderer lr;
    }

    private struct UnselectedAnchorInfo
    {
        public string targetNodeId;
        public string preNodeId;
        public int anchorIndex;
        public Vector3 startWorldPos;
        public GameObject anchorObj;
        public LineRenderer lr;
    }

    private struct SuccessorLineInfo
    {
        public string successorNodeId;
        public LineRenderer lr;
        public string preNodeId;
        public AnchorDirection startDirection;  // 新增：起始方向
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

        bool shouldRecordNodes = !(isEditMode && isPrimaryTargetAnchor);

        if (shouldRecordNodes)
        {
            foreach (var nodeObj in SelectionManager.Instance.GetSelectedNodes())
            {
                if (nodeObj == null) continue;
                var node = nodeObj.GetComponent<Node>();
                if (node == null) continue;

                RecordNodeInfo(context, nodeObj, node, grid, selectedAnchorPositions);
            }
        }

        // 记录选中的锚点（使用世界坐标）
        foreach (var kvp in selectedAnchorPositions)
        {
            var parts = kvp.Key.Split(new string[] { "->" }, System.StringSplitOptions.None);
            string targetNodeId = parts[0];
            string preNodeId = parts[1];
            int anchorIndex = int.Parse(parts[2]);
            var anchorObj = FindAnchorGameObject(context, targetNodeId, preNodeId, anchorIndex);
            if (anchorObj == null) continue;
            var lr = anchorObj.GetComponentInParent<LineRenderer>();

            // 使用实际的世界坐标
            Vector2 actualWorldPos = anchorObj.transform.position;

            anchorInfos[kvp.Key] = new AnchorMoveInfo
            {
                targetNodeId = targetNodeId,
                preNodeId = preNodeId,
                anchorIndex = anchorIndex,
                startWorldPos = actualWorldPos,
                anchorObj = anchorObj,
                lr = lr
            };
        }
    }

    private void RecordNodeInfo(InputManager context, GameObject nodeObj, Node node, Grid grid,
    Dictionary<string, Vector2> selectedAnchorPositions)
    {
        // 解析当前节点的路径连接信息
        var connections = node.sc.PathNode.ParsePathConnections();

        var info = new NodeMoveInfo
        {
            node = node,
            startWorldPos = new Vector2(node.sc.HexGridX, node.sc.HexGridY),
            lineRenderers = new List<LineRendererInfo>()
        };

        // 收集该节点下的所有LineRenderer
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

                    // 查找对应的连接信息，获取方向
                    AnchorDirection startDir = AnchorDirection.Center;
                    AnchorDirection endDir = AnchorDirection.Center;

                    foreach (var conn in connections)
                    {
                        if (conn.PreId == preNodeId)
                        {
                            startDir = conn.StartDirection;
                            endDir = conn.EndDirection;
                            break;
                        }
                    }

                    info.lineRenderers.Add(new LineRendererInfo
                    {
                        lr = lr,
                        preNodeId = preNodeId,
                        startEndPoint = lr.GetPosition(lr.positionCount - 1),
                        startDirection = startDir,
                        endDirection = endDir
                    });

                    // 收集该LineRenderer下未被选中的锚点（从LineRenderer获取位置，而非GameObject）
                    for (int i = 1; i < lr.positionCount - 1; i++)
                    {
                        string anchorKey = $"{node.sc.Id}->{preNodeId}->{i}";

                        if (!selectedAnchorPositions.ContainsKey(anchorKey))
                        {
                            // 从 LineRenderer 获取精确位置，而不是从锚点GameObject
                            Vector3 posFromLR = lr.GetPosition(i);

                            var anchorTransform = child.Find(i.ToString());

                            unselectedAnchorInfos[anchorKey] = new UnselectedAnchorInfo
                            {
                                targetNodeId = node.sc.Id,
                                preNodeId = preNodeId,
                                anchorIndex = i,
                                startWorldPos = new Vector3(posFromLR.x, posFromLR.y, 0),  // 从LR获取，去掉Z
                                anchorObj = anchorTransform?.gameObject,  // 可能为null
                                lr = lr
                            };
                        }
                    }
                }
            }
        }

        nodeInfos[nodeObj] = info;

        // 收集后继节点的连接线信息
        CollectSuccessorLineInfos(context, node);
    }


    private void CollectSuccessorLineInfos(InputManager context, Node node)
    {
        string nodeId = node.sc.Id;

        if (!successorLineInfos.ContainsKey(nodeId))
        {
            successorLineInfos[nodeId] = new List<SuccessorLineInfo>();
        }

        foreach (var successorId in node.sc.After_technology)
        {
            var successorTransform = context.UI.tilemap.transform.Find(successorId.ToString());
            if (successorTransform == null) continue;

            // 获取后继节点的路径连接信息
            var successorNode = successorTransform.GetComponent<Node>();
            if (successorNode == null) continue;

            var successorConnections = successorNode.sc.PathNode.ParsePathConnections();

            // 查找当前节点作为前置的连接信息
            AnchorDirection startDir = AnchorDirection.Center;
            foreach (var conn in successorConnections)
            {
                if (conn.PreId == nodeId)
                {
                    startDir = conn.StartDirection;
                    break;
                }
            }

            string lineName = $"{nodeId}->{successorId}";
            var lineTransform = successorTransform.Find(lineName);
            if (lineTransform == null) continue;

            var lr = lineTransform.GetComponent<LineRenderer>();
            if (lr == null || lr.positionCount == 0) continue;

            successorLineInfos[nodeId].Add(new SuccessorLineInfo
            {
                successorNodeId = successorId,
                lr = lr,
                preNodeId = nodeId,
                startDirection = startDir
            });
        }
    }

    private GameObject FindAnchorGameObject(InputManager context, string targetNodeId, string preNodeId, int anchorIndex)
    {
        var nodeTransform = context.UI.tilemap.transform.Find(targetNodeId);
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

        // 记录移动后的节点位置和Transform，用于计算锚点位置
        Dictionary<string, Vector3> movedNodeNewPositions = new Dictionary<string, Vector3>();
        Dictionary<string, Transform> movedNodeTransforms = new Dictionary<string, Transform>();

        // ========== 第一轮：更新所有节点位置 ==========
        foreach (var kvp in nodeInfos)
        {
            GameObject nodeObj = kvp.Key;
            if (nodeObj == null) continue;

            NodeMoveInfo info = kvp.Value;

            // 计算目标世界坐标
            Vector2 targetWorldPos = info.startWorldPos + new Vector2(worldDelta.x, worldDelta.y);

            // 根据网格设置决定是否吸附
            Vector3 snappedWorldPos = GridManager.Instance.SnapToGrid(new Vector3(targetWorldPos.x, targetWorldPos.y, 0));
            snappedWorldPos.z = 0;

            // 更新节点位置
            nodeObj.transform.position = snappedWorldPos;

            // 更新节点数据（保留小数）
            info.node.sc.HexGridX = (float)System.Math.Round(snappedWorldPos.x, 3);
            info.node.sc.HexGridY = (float)System.Math.Round(snappedWorldPos.y, 3);

            movedNodeNewPositions[info.node.sc.Id] = snappedWorldPos;
            movedNodeTransforms[info.node.sc.Id] = nodeObj.transform;
        }

        // ========== 第二轮：更新所有连接线 ==========
        foreach (var kvp in nodeInfos)
        {
            GameObject nodeObj = kvp.Key;
            if (nodeObj == null) continue;

            NodeMoveInfo info = kvp.Value;

            // 更新该节点下的所有LineRenderer
            foreach (var lrInfo in info.lineRenderers)
            {
                // 计算带方向偏移的终点位置
                Vector3 newEndPoint = MyExtensions.GetAnchorWorldPosition(nodeObj.transform, lrInfo.endDirection);
                newEndPoint.z = 1;
                lrInfo.lr.SetPosition(lrInfo.lr.positionCount - 1, newEndPoint);

                // 更新起点：优先使用已移动的前置节点位置
                string preNodeId = lrInfo.preNodeId;
                if (!string.IsNullOrEmpty(preNodeId))
                {
                    if (movedNodeTransforms.TryGetValue(preNodeId, out Transform preNodeTransform))
                    {
                        // 前置节点也在移动
                        Vector3 newStartPoint = MyExtensions.GetAnchorWorldPosition(preNodeTransform, lrInfo.startDirection);
                        newStartPoint.z = 1;
                        lrInfo.lr.SetPosition(0, newStartPoint);
                    }
                    else
                    {
                        // 前置节点没有移动，从场景中查找
                        var preNodeObj = context.UI.tilemap.transform.Find(preNodeId);
                        if (preNodeObj != null)
                        {
                            Vector3 newStartPoint = MyExtensions.GetAnchorWorldPosition(preNodeObj, lrInfo.startDirection);
                            newStartPoint.z = 1;
                            lrInfo.lr.SetPosition(0, newStartPoint);
                        }
                    }
                }
            }
        }

        // 3. 更新后继节点的连接线起点
        foreach (var kvp in successorLineInfos)
        {
            string movedNodeId = kvp.Key;
            if (!movedNodeTransforms.TryGetValue(movedNodeId, out Transform movedNodeTransform)) continue;

            foreach (var lineInfo in kvp.Value)
            {
                // 检查后继节点是否也在移动（如果是，已在上面处理过）
                bool successorAlsoMoved = movedNodeTransforms.ContainsKey(lineInfo.successorNodeId);
                if (successorAlsoMoved) continue;

                // 计算带方向偏移的起点位置
                Vector3 newStartPoint = MyExtensions.GetAnchorWorldPosition(movedNodeTransform, lineInfo.startDirection);
                newStartPoint.z = 1;
                lineInfo.lr.SetPosition(0, newStartPoint);
            }
        }

        // 4. 更新未选中锚点（保持世界坐标不变）
        foreach (var kvp in unselectedAnchorInfos)
        {
            var info = kvp.Value;
            if (info.lr == null) continue;

            // 保持 LineRenderer 中的位置不变
            Vector3 linePos = new Vector3(info.startWorldPos.x, info.startWorldPos.y, 1);
            info.lr.SetPosition(info.anchorIndex, linePos);

            // 如果锚点 GameObject 存在，也更新它的位置
            if (info.anchorObj != null)
            {
                info.anchorObj.transform.position = new Vector2(info.startWorldPos.x, info.startWorldPos.y);
            }
        }


        // 5. 移动选中的锚点
        foreach (var kvp in anchorInfos)
        {
            var info = kvp.Value;
            if (info.anchorObj == null) continue;

            Vector2 targetWorldPos = info.startWorldPos + new Vector2(worldDelta.x, worldDelta.y);

            // 根据网格设置决定是否吸附
            Vector3 snappedWorldPos = GridManager.Instance.SnapToGrid(new Vector3(targetWorldPos.x, targetWorldPos.y, 0));

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
        var batchCmd = new BatchMoveCommand();
        bool hasNodeMoved = false;

        // 处理节点移动命令
        foreach (var kvp in nodeInfos)
        {
            GameObject nodeObj = kvp.Key;
            if (nodeObj == null) continue;
            NodeMoveInfo info = kvp.Value;

            Vector2 currentPos = new Vector2(info.node.sc.HexGridX, info.node.sc.HexGridY);

            // 比较世界坐标是否变化
            if (Vector2.Distance(info.startWorldPos, currentPos) > 0.001f)
            {
                hasNodeMoved = true;
                batchCmd.Add(new MoveNodeCommand(info.node, info.startWorldPos, currentPos));
            }
        }

        // 处理锚点命令
        foreach (var kvp in anchorInfos)
        {
            var info = kvp.Value;
            if (info.anchorObj == null) continue;

            Vector2 currentPos = new Vector2(info.anchorObj.transform.position.x, info.anchorObj.transform.position.y);

            if (Vector2.Distance(info.startWorldPos, currentPos) > 0.001f)
            {
                UpdateAnchorInPathNode(context, info);

                Vector2 worldPos = info.anchorObj.transform.position;
                SelectionManager.Instance.UpdateAnchorPosition(
                    info.targetNodeId,
                    info.preNodeId,
                    info.anchorIndex,
                    worldPos
                );
            }
        }

        if (hasNodeMoved && batchCmd.HasCommands)
        {
            CommandManager.Instance.ExecuteCommand(batchCmd);
        }
    }

    private void UpdateAnchorInPathNode(InputManager context, AnchorMoveInfo info)
    {
        if (!DataManager.Instance.TryGetScience(info.targetNodeId, out var sc)) return;
        if (info.lr == null) return;

        // 解析当前路径，保留方向信息
        var connections = sc.PathNode.ParsePathConnections();
        string preId = info.preNodeId;

        bool found = false;
        for (int c = 0; c < connections.Count; c++)
        {
            if (connections[c].PreId == preId)
            {
                var conn = connections[c];
                // 从 LineRenderer 重建中间点（跳过首尾）
                conn.Waypoints.Clear();
                for (int i = 1; i < info.lr.positionCount - 1; i++)
                {
                    Vector3 pos = info.lr.GetPosition(i);
                    conn.Waypoints.Add(new Vector2(
                        (float)System.Math.Round(pos.x, 1),
                        (float)System.Math.Round(pos.y, 1)));
                }
                connections[c] = conn;
                found = true;
                break;
            }
        }

        // 如果没找到对应连接，不应该发生，但做个保护
        if (!found && info.lr.positionCount > 2)
        {
            var waypoints = new List<Vector2>();
            for (int i = 1; i < info.lr.positionCount - 1; i++)
            {
                Vector3 pos = info.lr.GetPosition(i);
                waypoints.Add(new Vector2(
                    (float)System.Math.Round(pos.x, 1),
                    (float)System.Math.Round(pos.y, 1)));
            }
            connections.Add(new PathConnection(preId, AnchorDirection.Center, AnchorDirection.Center, waypoints));
        }

        sc.PathNode = MyExtensions.SerializePathConnections(connections);

        if (context.CurrentEditPanel != null)
        {
            var panelScript = context.CurrentEditPanel.GetComponent<PanelScienceEdit>();
            if (panelScript.sc.Id == info.targetNodeId)
            {
                panelScript.RefreshUI();  // 只刷新UI显示，不触发验证回调
            }
        }
    }

    private void RefreshAffectedNodes(InputManager context)
    {
        HashSet<string> affectedNodeIds = new HashSet<string>();

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

        foreach (var kvp in anchorInfos)
        {
            affectedNodeIds.Add(kvp.Value.targetNodeId);
        }

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
