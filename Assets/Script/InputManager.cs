// 修改 InputManager.cs - 在 UpdateKeyboardEvent 方法中添加空格键处理

using Assets.Script.My.Extention;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    #region 属性
    public UIReferences UI { get; private set; }
    public GameObject CurrentEditPanel { get; private set; }

    // 新增：跟踪非编辑模式下锚点显示状态
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

    #region 工具方法 (供状态类调用)

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

    // 供 StateDragNode 调用
    public void MoveNodeVisual(GameObject obj, Vector3Int gridPosI)
    {
        Node node = obj.GetComponent<Node>();
        node.UpdateGridPos(gridPosI);

        Vector3 newPos = UI.grid.CellToWorld(gridPosI);
        newPos.z = 0;
        obj.transform.position = newPos;

        // 更新子节点的连线
        foreach (var afterId in node.sc.After_technology)
        {
            // 假设你的 Tilemap 是直接挂着节点，保留原有逻辑
            GameObject child = UI.tilemap.transform.Find(afterId.ToString())?.gameObject;
            child?.GetComponent<Node>().UpdateNodeAppearance();
        }

        // 如果是当前编辑的节点，同步更新面板数值
        if (CurrentEditPanel && CurrentEditPanel.GetComponent<PanelScienceEdit>().sc.Id == node.sc.Id)
        {
            CurrentEditPanel.GetComponent<PanelScienceEdit>()
                .UpdatePositionByDrag(new Vector2Int(gridPosI.y, gridPosI.x)); // 注意 xy 对应

            node.ClearAnchor();
            node.UpdateLineAnchor();
        }
    }

    // 供 StateDragAnchor 调用
    public void MoveAnchorVisual(GameObject anchorObj, Vector3 worldPos, Vector3Int gridPosI)
    {
        int lrIndex = int.Parse(anchorObj.name);
        LineRenderer lr = anchorObj.GetComponentInParent<LineRenderer>();

        Vector2 anchorMoveTo = new Vector2(worldPos.x, worldPos.y);
        Vector3 lineIndexAt = new Vector3(anchorMoveTo.x, anchorMoveTo.y, 1); // Z=1 确保显示
        anchorObj.transform.position = anchorMoveTo;
        lr.SetPosition(lrIndex, lineIndexAt);

        // 更新数据
        string nodeFrom = lr.gameObject.name.Split("->")[0];
        string nodeTo = lr.gameObject.name.Split("->")[1];

        if (DataManager.Instance.ScienceDict.TryGetValue(int.Parse(nodeTo), out var sc))
        {
            int positionCount = lr.positionCount;
            string newpos = null;

            for (int i = 1; i < positionCount - 1; i++)
            {
                if (newpos is not null) newpos += "_";
                // 注意：这里原来是用 lr.GetPosition(i) 转 Cell，可能存在精度问题，
                // 虽然你现在已经传了 gridPosI，但实际上直接用，因为为了保持对多个点的一致性，
                // 还是保留 LineRenderer 的点比较安全
                var cellPos = UI.grid.WorldToCell(lr.GetPosition(i));
                newpos += $"{cellPos.y}_{cellPos.x}";
            }

            sc.PathNode = sc.PathNode.UpdatePathNodeById(nodeFrom, newpos);

            if (CurrentEditPanel)
            {
                CurrentEditPanel.GetComponent<PanelScienceEdit>().UpdatePrePath(sc.PathNode);
            }
        }
    }

    // 供 StateIdle 调用
    public (LineRenderer, int, Vector3) DetectLineNearMouse()
    {
        Vector3 mousePos = GetMouseWorldPos();
        List<GameObject> linestoCheck = new List<GameObject>();

        if (CurrentEditPanel != null)
        {
            int currentId = CurrentEditPanel.GetComponent<PanelScienceEdit>().sc.Id;
            Node currentNode = NodeManager.Instance.GetNode(currentId); // 假设你有 NodeManager

            if (currentNode != null)
            {
                foreach (Transform child in currentNode.transform)
                {
                    if (child.tag == Constants.Tags.NodeLine)
                        linestoCheck.Add(child.gameObject);
                }
            }
        }
        else
        {
            return (null, -1, Vector3.zero);
        }

        float minDst = 5f;
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

                // 简单的点到线段距离计算
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

        // 当进入编辑模式时，重置非编辑模式的锚点显示状态
        if (panel != null)
        {
            // 清除非编辑模式下显示的锚点（如果有的话）
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

        // 如果之前在非编辑模式下显示了锚点，先清除
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
        // 如果多选了，且当前有编辑面板，则关闭面板
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

    #region 键盘事件 (通用，不依赖状态)
    private void UpdateKeyboardEvent()
    {
        // 撤销/重做
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Z)) { CommandManager.Instance.Undo(); return; }
            if (Input.GetKeyDown(KeyCode.Y)) { CommandManager.Instance.Redo(); return; }
        }
        if (!EventSystem.current.currentSelectedGameObject)
        {
            // 删除
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                // 优先检查：是否有选中的锚点（多选或单选）
                if (SelectionManager.Instance.AnchorCount > 0)
                {
                    // 删除所有选中的锚点
                    DeleteSelectedAnchors();
                    return;
                }

                // 其次检查：编辑模式下的单选锚点（兼容旧逻辑）
                if (SelectionManager.Instance.SelectedAnchor != null)
                {
                    var cmd = new DeleteAnchorCommand(SelectionManager.Instance.SelectedAnchor);
                    CommandManager.Instance.ExecuteCommand(cmd);
                    SelectionManager.Instance.ClearAnchor();
                    return;
                }
                // 最后：删除节点 (仅在编辑模式下)
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
            // 空格键：非编辑模式下切换选中节点的锚点显示
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleAnchorsForSelectedNodes();
            }
            // 放大/缩小 小键盘加减：调整选中节点尺寸
            if (CurrentEditPanel == null)
            {
                if (Input.GetKeyDown(KeyCode.KeypadPlus))
                {
                    ChangeSelectedNodesScale(true); // 变大
                }
                else if (Input.GetKeyDown(KeyCode.KeypadMinus))
                {
                    ChangeSelectedNodesScale(false); // 变小
                }
            }
            // 截图
            if (Input.GetKeyDown(KeyCode.F2))
            {
                // 确保 ScreenshotManager 存在
                if (ScreenshotManager.Instance == null)
                {
                    gameObject.AddComponent<ScreenshotManager>();
                }
                ScreenshotManager.Instance.CaptureSceneToClipboard();
            }
        }
    }

    /// <summary>
    /// 批量修改选中节点的尺寸
    /// </summary>
    /// <param name="increase">true为变大，false为变小</param>
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
            // 计算新的尺寸参数
            var (newIconScale, newLineScale) = CalculateNewScale(node.sc.IconScale, increase);
            // 只有尺寸确实发生变化才添加命令
            if (Mathf.Abs(newIconScale - node.sc.IconScale) > 0.001f)
            {
                batchCmd.Add(new ChangeNodeScaleCommand(node, newIconScale, newLineScale));
            }
        }
        if (batchCmd.HasCommands)
        {
            CommandManager.Instance.ExecuteCommand(batchCmd);
            EventCenter.Instance.TriggerLogMessage(increase ? "增大节点尺寸" : "减小节点尺寸");
        }
    }
    /// <summary>
    /// 根据当前尺寸和方向计算新尺寸
    /// </summary>
    private (float icon, float line) CalculateNewScale(float currentIconScale, bool increase)
    {
        // 定义三个档位
        // Small:  Icon=0.9, Line=0.12 (Thin)
        // Middle: Icon=1.9, Line=0.24 (Medium)
        // Large:  Icon=3.9, Line=0.49 (Thick)
        // 简单的模糊匹配逻辑
        bool isSmall = currentIconScale <= Constants.NodeScale.Small + 0.1f;
        bool isLarge = currentIconScale >= Constants.NodeScale.Large - 0.1f;
        bool isMiddle = !isSmall && !isLarge;
        if (increase)
        {
            // 变大逻辑
            if (isSmall)
                return (Constants.NodeScale.Middle, Constants.LineWidth.Medium);
            else
                return (Constants.NodeScale.Large, Constants.LineWidth.Thick);
        }
        else
        {
            // 变小逻辑
            if (isLarge)
                return (Constants.NodeScale.Middle, Constants.LineWidth.Medium);
            else
                return (Constants.NodeScale.Small, Constants.LineWidth.Thin);
        }
    }


    /// <summary>
    /// 删除所有选中的锚点
    /// </summary>
    private void DeleteSelectedAnchors()
    {
        var anchorPositions = SelectionManager.Instance.GetSelectedAnchorPositions();
        if (anchorPositions.Count == 0) return;

        // 创建批量删除命令
        var batchCmd = new BatchDeleteAnchorCommand();

        foreach (var kvp in anchorPositions)
        {
            // 解析 key: "targetNodeId->preNodeId->anchorIndex"
            var parts = kvp.Key.Split(new string[] { "->" }, System.StringSplitOptions.None);
            int targetNodeId = int.Parse(parts[0]);
            string preNodeId = parts[1];
            int anchorIndex = int.Parse(parts[2]);

            // 查找对应的锚点GameObject
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
    /// <summary>
    /// 查找锚点GameObject
    /// </summary>
    private GameObject FindAnchorGameObject(int targetNodeId, string preNodeId, int anchorIndex)
    {
        var nodeTransform = UI.tilemap.transform.Find(targetNodeId.ToString());
        if (nodeTransform == null) return null;

        var lineTransform = nodeTransform.Find($"{preNodeId}->{targetNodeId}");
        if (lineTransform == null) return null;

        var anchorTransform = lineTransform.Find(anchorIndex.ToString());
        return anchorTransform?.gameObject;
    }

    /// <summary>
    /// 切换选中节点的锚点显示状态（仅在非编辑模式下生效）
    /// </summary>
    private void ToggleAnchorsForSelectedNodes()
    {
        // 如果在编辑模式下，不处理（编辑模式有自己的锚点显示逻辑）
        if (CurrentEditPanel != null)
        {
            EventCenter.Instance.TriggerLogWarning("编辑模式下请使用编辑面板管理锚点");
            return;
        }

        // 检查是否有选中的节点
        var selectedNodes = SelectionManager.Instance.GetSelectedNodes();
        if (selectedNodes.Count == 0)
        {
            EventCenter.Instance.TriggerLogWarning("请先选中节点");
            return;
        }

        // 切换状态
        _anchorsVisibleInNormalMode = !_anchorsVisibleInNormalMode;

        if (_anchorsVisibleInNormalMode)
        {
            ShowAnchorsForSelectedNodes();
            EventCenter.Instance.TriggerLogMessage($"显示 {selectedNodes.Count} 个节点的锚点");
        }
        else
        {
            HideAnchorsForSelectedNodes();
            EventCenter.Instance.TriggerLogMessage("隐藏锚点");
        }
    }

    /// <summary>
    /// 显示选中节点的锚点
    /// </summary>
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

        // 锚点创建后，刷新选中锚点的高亮状态
        SelectionManager.Instance.RefreshAnchorHighlights();
    }

    /// <summary>
    /// 隐藏选中节点的锚点
    /// </summary>
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

        SelectionManager.Instance.ClearNodes(); // 清空选择防止引用空对象
        CommandManager.Instance.ExecuteCommand(deleteCmd);

        panelScript.DestoryPanel();
        CurrentEditPanel = null;
    }

    private void UpdateDebugInput()
    {
        // 临时调试代码
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

    #region 公共属性和方法
    /// <summary>
    /// 非编辑模式下锚点是否正在显示
    /// </summary>
    public bool AnchorsVisibleInNormalMode => _anchorsVisibleInNormalMode;
    /// <summary>
    /// 重置锚点显示状态（供 SelectionManager 调用）
    /// </summary>
    public void ResetAnchorVisibilityState()
    {
        _anchorsVisibleInNormalMode = false;
    }
    /// <summary>
    /// 设置非编辑模式下锚点可见状态（供外部调用）
    /// </summary>
    public void SetAnchorsVisibleInNormalMode(bool visible)
    {
        _anchorsVisibleInNormalMode = visible;
    }
    #endregion
}
