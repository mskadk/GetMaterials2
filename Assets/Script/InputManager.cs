using Assets.Script.My.Extention;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    #region 属性
    public UIReferences UI { get; private set; }
    public GameObject CurrentEditPanel { get; private set; }

    private bool _anchorsVisibleInNormalMode = false;
    #endregion

    #region 状态机
    private IInputState currentState;
    #endregion

    public void Initialize()
    {
        UI = UIReferences.Instance;
        ChangeState(new StateIdle());
    }

    private void Update()
    {
        if (currentState != null)
            currentState.OnUpdate(this);

        UpdateKeyboardEvent();
        UpdateDebugInput();
    }

    public void ChangeState(IInputState newState)
    {
        currentState?.OnExit(this);
        currentState = newState;
        currentState?.OnEnter(this);
    }

    #region 工具方法

    public Vector3 GetMouseWorldPos()
    {
        Vector3 p = UI.camSence.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0;
        return p;
    }

    public GameObject RayDetect(string tagFilter = null)
    {
        GameObject ob = null;
        Vector3 worldPos = GetMouseWorldPos();
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector3.forward, Mathf.Infinity);
        if (hit)
        {
            ob = hit.transform.gameObject;
        }
        if (hit && tagFilter != null && hit.transform.gameObject.tag != tagFilter)
        {
            return null;
        }
        return ob;
    }

    /// <summary>
    /// 移动节点视觉（使用世界坐标）
    /// </summary>
    public void MoveNodeVisual(GameObject obj, Vector2 worldPos)
    {
        Node node = obj.GetComponent<Node>();
        node.UpdateWorldPos(worldPos);

        Vector3 newPos = new Vector3(worldPos.x, worldPos.y, 0);
        obj.transform.position = newPos;

        foreach (var afterId in node.sc.After_technology)
        {
            GameObject child = UI.tilemap.transform.Find(afterId.ToString())?.gameObject;
            child?.GetComponent<Node>().UpdateNodeAppearance();
        }

        if (CurrentEditPanel && CurrentEditPanel.GetComponent<PanelScienceEdit>().sc.Id == node.sc.Id)
        {
            CurrentEditPanel.GetComponent<PanelScienceEdit>()
                .UpdatePositionByDrag(worldPos);

            node.ClearAnchor();
            node.UpdateLineAnchor();
        }
    }

    /// <summary>
    /// 移动锚点视觉（使用世界坐标）
    /// </summary>
    public void MoveAnchorVisual(GameObject anchorObj, Vector3 worldPos)
    {
        int lrIndex = int.Parse(anchorObj.name);
        LineRenderer lr = anchorObj.GetComponentInParent<LineRenderer>();

        Vector2 anchorMoveTo = new Vector2(worldPos.x, worldPos.y);
        Vector3 lineIndexAt = new Vector3(anchorMoveTo.x, anchorMoveTo.y, 1);
        anchorObj.transform.position = anchorMoveTo;
        lr.SetPosition(lrIndex, lineIndexAt);

        string nodeFrom = lr.gameObject.name.Split("->")[0];
        string nodeTo = lr.gameObject.name.Split("->")[1];

        if (DataManager.Instance.ScienceDict.TryGetValue(int.Parse(nodeTo), out var sc))
        {
            // 解析当前路径，保留方向信息
            var connections = sc.PathNode.ParsePathConnections();
            int preId = int.Parse(nodeFrom);

            for (int c = 0; c < connections.Count; c++)
            {
                if (connections[c].PreId == preId)
                {
                    var conn = connections[c];
                    // 从 LineRenderer 重建中间点（跳过首尾）
                    conn.Waypoints.Clear();
                    int positionCount = lr.positionCount;
                    for (int i = 1; i < positionCount - 1; i++)
                    {
                        Vector3 pos = lr.GetPosition(i);
                        conn.Waypoints.Add(new Vector2(
                            (float)System.Math.Round(pos.x, 1),
                            (float)System.Math.Round(pos.y, 1)));
                    }
                    connections[c] = conn;
                    break;
                }
            }

            sc.PathNode = MyExtensions.SerializePathConnections(connections);

            if (CurrentEditPanel)
            {
                CurrentEditPanel.GetComponent<PanelScienceEdit>().UpdatePrePath(sc.PathNode);
            }
        }
    }

    public (LineRenderer, int, Vector3) DetectLineNearMouse()
    {
        Vector3 mousePos = GetMouseWorldPos();
        List<GameObject> linestoCheck = new List<GameObject>();

        if (CurrentEditPanel != null)
        {
            int currentId = CurrentEditPanel.GetComponent<PanelScienceEdit>().sc.Id;
            Node currentNode = NodeManager.Instance.GetNode(currentId);

            if (currentNode != null)
            {
                foreach (Transform child in currentNode.transform)
                {
                    if (child.tag == Constants.Tags.NodeLine) linestoCheck.Add(child.gameObject);
                }
            }
        }
        else
        {
            return (null, -1, Vector3.zero);
        }

        float minDst = 500f;
        LineRenderer bestLr = null;
        int bestIndex = -1;
        Vector3 bestPoint = Vector3.zero;

        foreach (var lineObj in linestoCheck)
        {
            LineRenderer lr = lineObj.GetComponent<LineRenderer>();
            if (!lr) continue;

            for (int i = 0; i < lr.positionCount - 1; i++)
            {
                Vector3 p1 = lr.GetPosition(i);
                Vector3 p2 = lr.GetPosition(i + 1);
                p1.z = 0; p2.z = 0;

                float dst = HandleUtility_DistancePointLine(mousePos, p1, p2);
                if (dst < minDst)
                {
                    minDst = dst;
                    bestLr = lr;
                    bestIndex = i + 1;
                    bestPoint = GetMouseWorldPos();
                }
            }
        }
        return (bestLr, bestIndex, bestPoint);
    }

    private float HandleUtility_DistancePointLine(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 proj = ProjectPointOnLineSegment(a, b, p);
        return Vector3.Distance(p, proj);
    }

    private Vector3 ProjectPointOnLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point)
    {
        Vector3 vector = linePoint2 - linePoint1;
        Vector3 vector2 = point - linePoint1;
        float d = vector.sqrMagnitude;
        if (d == 0f) return linePoint1;
        float t = Vector3.Dot(vector2, vector) / d;
        if (t < 0f) return linePoint1;
        if (t > 1f) return linePoint2;
        return linePoint1 + vector * t;
    }

    #endregion

    #region 外部调用接口
    public void SetCurrentEditPanel(GameObject panel)
    {
        CurrentEditPanel = panel;

        if (panel != null)
        {
            if (_anchorsVisibleInNormalMode)
            {
                HideAnchorsForSelectedNodes();
                _anchorsVisibleInNormalMode = false;
            }
        }
    }

    public void OpenEditPanel(GameObject nodeObj)
    {
        string nodeId = nodeObj.name;
        Node node = nodeObj.GetComponent<Node>();

        if (_anchorsVisibleInNormalMode)
        {
            HideAnchorsForSelectedNodes();
            _anchorsVisibleInNormalMode = false;
        }

        CurrentEditPanel = Instantiate(UI.panelNodeEditPrefab);
        CurrentEditPanel.transform.SetParent(
            UI.canvas.transform.Find(Constants.UIPath.PanelRightContent),
            false
        );
        CurrentEditPanel.name = $"{nodeId}(Edit)";

        var panelScript = CurrentEditPanel.GetComponent<PanelScienceEdit>();
        panelScript.node = node;
        panelScript.sc = node.sc;

        node.SetSelectStyle(true);
        EventCenter.Instance.TriggerNodeSelected(node);
        EventCenter.Instance.TriggerLogMessage($"编辑节点：{nodeId}:{node.sc.Name}");
    }

    public void CheckEditPanelState()
    {
        if (SelectionManager.Instance.Count > 1)
        {
            if (CurrentEditPanel)
            {
                CurrentEditPanel.GetComponent<PanelScienceEdit>().DestoryPanel();
                CurrentEditPanel = null;
            }
        }
    }
    #endregion

    #region 键盘事件
    private void UpdateKeyboardEvent()
    {
        // 撤销/重做
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Z)) { CommandManager.Instance.Undo(); return; }
            if (Input.GetKeyDown(KeyCode.Y)) { CommandManager.Instance.Redo(); return; }
            if (Input.GetKeyDown(KeyCode.C))
            {
                CopySelectedNodesToClipboard();
                return;
            }
        }

        if (!EventSystem.current.currentSelectedGameObject)
        {
            // === 方向键修改锚点方向 ===
            if (HandleDirectionKeyInput()) return;

            // === 数字键切换活动连接线 ===
            if (HandleNumberKeyInput()) return;

            // 删除
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                if (SelectionManager.Instance.AnchorCount > 0)
                {
                    DeleteSelectedAnchors();
                    return;
                }

                if (SelectionManager.Instance.SelectedAnchor != null)
                {
                    var cmd = new DeleteAnchorCommand(SelectionManager.Instance.SelectedAnchor);
                    CommandManager.Instance.ExecuteCommand(cmd);
                    SelectionManager.Instance.ClearAnchor();
                    return;
                }

                if (CurrentEditPanel)
                {
                    DeleteCurrentNode();
                }
            }

            // ESC
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (CurrentEditPanel)
                {
                    CurrentEditPanel.GetComponent<PanelScienceEdit>().DestoryPanel();
                    CurrentEditPanel = null;
                }

                if (_anchorsVisibleInNormalMode)
                {
                    HideAnchorsForSelectedNodes();
                    _anchorsVisibleInNormalMode = false;
                }
            }

            // 空格键切换锚点显示
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleAnchorsForSelectedNodes();
            }

            // 放大/缩小节点
            if (CurrentEditPanel == null)
            {
                if (Input.GetKeyDown(KeyCode.KeypadPlus))
                {
                    ChangeSelectedNodesScale(true);
                }
                else if (Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    ChangeSelectedNodesScale(false);
                }
            }

            // 截图
            if (Input.GetKeyDown(KeyCode.F2))
            {
                if (ScreenshotManager.Instance == null)
                {
                    gameObject.AddComponent<ScreenshotManager>();
                }
                ScreenshotManager.Instance.CaptureSceneToClipboard();
            }
        }
    }

    /// <summary>
    /// 处理方向键输入，修改活动连接线的锚点方向
    /// </summary>
    private bool HandleDirectionKeyInput()
    {
        // 只在有选中节点时生效
        if (SelectionManager.Instance.NodeCount == 0) return false;

        // 检查是否有活动连接线
        var activeLine = SelectionManager.Instance.GetActiveLine();
        if (activeLine == null) return false;

        bool isRightShift = Input.GetKey(KeyCode.RightShift);
        AnchorDirection? newDir = null;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            newDir = AnchorDirection.Top;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            newDir = AnchorDirection.Bottom;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            newDir = AnchorDirection.Left;
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            newDir = AnchorDirection.Right;

        if (newDir.HasValue)
        {
            // RightShift + 方向键 = 修改终止方向，否则修改起始方向
            bool isStart = !isRightShift;
            SelectionManager.Instance.UpdateActiveLineDirection(isStart, newDir.Value);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 处理数字键输入，切换活动连接线
    /// </summary>
    private bool HandleNumberKeyInput()
    {
        // 只在有选中节点时生效
        if (SelectionManager.Instance.NodeCount == 0) return false;

        // 检查主键盘数字键 1-9
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + (i - 1)))
            {
                // 用户按 1 对应索引 0，按 2 对应索引 1，以此类推
                SelectionManager.Instance.SetActiveLineIndex(i - 1);

                var lines = SelectionManager.Instance.GetPrimaryNodeLines();
                if (i <= lines.Count)
                {
                    EventCenter.Instance.TriggerLogMessage($"切换到连接线 {i}/{lines.Count}");
                }
                return true;
            }
        }

        return false;
    }

    private void CopySelectedNodesToClipboard()
    {
        // 1. 获取 SelectionManager 中选中的节点列表
        var selectedNodes = SelectionManager.Instance.GetSelectedNodes();
        List<Science> list = new List<Science>();

        // 2. 如果有选中节点，收集它们的数据
        if (selectedNodes.Count > 0)
        {
            foreach (var obj in selectedNodes)
            {
                if (obj != null)
                {
                    var node = obj.GetComponent<Node>();
                    if (node != null && node.sc != null)
                    {
                        list.Add(node.sc);
                    }
                }
            }
        }
        // 3. 如果没有选中节点，但当前正在编辑某个节点（编辑面板），复制该节点
        else if (CurrentEditPanel != null)
        {
            var panel = CurrentEditPanel.GetComponent<PanelScienceEdit>();
            if (panel != null && panel.sc != null)
            {
                list.Add(panel.sc);
            }
        }

        // 4. 如果收集到了数据，调用 DataManager 复制
        if (list.Count > 0)
        {
            DataManager.Instance.ScienceToClipBoard(list);
        }
    }

    private void ChangeSelectedNodesScale(bool increase)
    {
        var selectedNodes = SelectionManager.Instance.GetSelectedNodes();
        if (selectedNodes.Count == 0) return;

        var batchCmd = new BatchChangeNodeScaleCommand();
        foreach (var nodeObj in selectedNodes)
        {
            if (nodeObj == null) continue;
            var node = nodeObj.GetComponent<Node>();
            if (node == null) continue;

            var (newIconScale, newLineScale) = CalculateNewScale(node.sc.IconScale, increase);

            if (Mathf.Abs(newIconScale - node.sc.IconScale) > 0.001f)
            {
                batchCmd.Add(new ChangeNodeScaleCommand(node, newIconScale, newLineScale));
            }
        }

        if (batchCmd.HasCommands)
        {
            CommandManager.Instance.ExecuteCommand(batchCmd);
            EventCenter.Instance.TriggerLogMessage(increase ? "放大节点尺寸" : "缩小节点尺寸");
        }
    }

    private (float icon, float line) CalculateNewScale(float currentIconScale, bool increase)
    {
        bool isSmall = currentIconScale <= Constants.NodeScale.Small + 0.1f;
        bool isLarge = currentIconScale >= Constants.NodeScale.Large - 0.1f;

        if (increase)
        {
            if (isSmall)
                return (Constants.NodeScale.Middle, Constants.LineWidth.Medium);
            else
                return (Constants.NodeScale.Large, Constants.LineWidth.Thick);
        }
        else
        {
            if (isLarge)
                return (Constants.NodeScale.Middle, Constants.LineWidth.Medium);
            else
                return (Constants.NodeScale.Small, Constants.LineWidth.Thin);
        }
    }

    private void DeleteSelectedAnchors()
    {
        var anchorPositions = SelectionManager.Instance.GetSelectedAnchorPositions();
        if (anchorPositions.Count == 0) return;

        var batchCmd = new BatchDeleteAnchorCommand();

        foreach (var kvp in anchorPositions)
        {
            var parts = kvp.Key.Split(new string[] { "->" }, System.StringSplitOptions.None);
            int targetNodeId = int.Parse(parts[0]);
            string preNodeId = parts[1];
            int anchorIndex = int.Parse(parts[2]);

            var anchorObj = FindAnchorGameObject(targetNodeId, preNodeId, anchorIndex);
            if (anchorObj != null)
            {
                batchCmd.Add(new DeleteAnchorCommand(anchorObj));
            }
        }

        if (batchCmd.Count > 0)
        {
            CommandManager.Instance.ExecuteCommand(batchCmd);
        }

        SelectionManager.Instance.ClearAnchors();
    }

    private GameObject FindAnchorGameObject(int targetNodeId, string preNodeId, int anchorIndex)
    {
        var nodeTransform = UI.tilemap.transform.Find(targetNodeId.ToString());
        if (nodeTransform == null) return null;

        var lineTransform = nodeTransform.Find($"{preNodeId}->{targetNodeId}");
        if (lineTransform == null) return null;

        var anchorTransform = lineTransform.Find(anchorIndex.ToString());
        return anchorTransform?.gameObject;
    }

    private void ToggleAnchorsForSelectedNodes()
    {
        if (CurrentEditPanel != null)
        {
            EventCenter.Instance.TriggerLogWarning("编辑模式下请使用编辑面板管理锚点");
            return;
        }

        var selectedNodes = SelectionManager.Instance.GetSelectedNodes();
        if (selectedNodes.Count == 0)
        {
            EventCenter.Instance.TriggerLogWarning("请先选中节点");
            return;
        }

        _anchorsVisibleInNormalMode = !_anchorsVisibleInNormalMode;

        if (_anchorsVisibleInNormalMode)
        {
            ShowAnchorsForSelectedNodes(); EventCenter.Instance.TriggerLogMessage($"显示 {selectedNodes.Count} 个节点的锚点");
        }
        else
        {
            HideAnchorsForSelectedNodes();
            EventCenter.Instance.TriggerLogMessage("隐藏锚点");
        }
    }

    private void ShowAnchorsForSelectedNodes()
    {
        var selectedNodes = SelectionManager.Instance.GetSelectedNodes();
        foreach (var nodeObj in selectedNodes)
        {
            if (nodeObj != null && nodeObj.tag == Constants.Tags.Node)
            {
                Node node = nodeObj.GetComponent<Node>();
                if (node != null)
                {
                    node.UpdateLineAnchor();
                }
            }
        }

        SelectionManager.Instance.RefreshAnchorHighlights();
    }

    private void HideAnchorsForSelectedNodes()
    {
        var selectedNodes = SelectionManager.Instance.GetSelectedNodes();
        foreach (var nodeObj in selectedNodes)
        {
            if (nodeObj.tag == Constants.Tags.Node)
            {
                Node node = nodeObj.GetComponent<Node>();
                if (node != null)
                {
                    node.ClearAnchor();
                }
            }
        }
    }

    private void DeleteCurrentNode()
    {
        var panelScript = CurrentEditPanel.GetComponent<PanelScienceEdit>();
        var sc = panelScript.sc;
        var deleteCmd = new DeleteNodeCommand(sc, UI);

        SelectionManager.Instance.ClearNodes();
        CommandManager.Instance.ExecuteCommand(deleteCmd);

        panelScript.DestoryPanel();
        CurrentEditPanel = null;
    }

    private void UpdateDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.Slash))
        {
            foreach (var sc in DataManager.Instance.ScienceDict)
                Debug.Log($"KEY:{sc.Key}, VALUE:{sc.Value}");
        }
        if (Input.GetKeyDown(KeyCode.Period))
        {
            var hitObj = RayDetect();
            if (hitObj != null && hitObj.tag == Constants.Tags.Node)
            {
                var node = hitObj.GetComponent<Node>();
                if (DataManager.Instance.ScienceDict.TryGetValue(node.sc.Id, out Science sc))
                    Debug.Log($"数据：{sc}");
                Debug.Log($"After:{string.Join("|", sc.After_technology)}");
            }
        }
    }
    #endregion

    #region 属性访问和方法
    public bool AnchorsVisibleInNormalMode => _anchorsVisibleInNormalMode;

    public void ResetAnchorVisibilityState()
    {
        _anchorsVisibleInNormalMode = false;
    }

    public void SetAnchorsVisibleInNormalMode(bool visible)
    {
        _anchorsVisibleInNormalMode = visible;
    }
    #endregion
}
