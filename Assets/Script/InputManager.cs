using Assets.Script.My.Extention;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 输入管理器 - 处理所有鼠标和键盘输入
/// </summary>
public class InputManager : MonoBehaviour
{
    #region 引用
    private UIReferences ui;
    #endregion

    #region 输入状态
    private Vector3 鼠标按下位置_屏幕;
    private GameObject rayHitMove;
    private GameObject rayHitNode;
    private GameObject currentEditPanel;
    #endregion

    // 当前选中的锚点对象
    private GameObject selectedAnchor;
    private float lastClickTime;
    private const float DOUBLE_CLICK_TIME = 0.3f;

    // 记录节点移动前的基准位置
    private Vector3Int lastNodePosition;

    // 当前选中的对象集合
    private HashSet<GameObject> selectedObjects = new HashSet<GameObject>();

    // 框选相关
    private bool isBoxSelecting = false;
    private Vector2 boxStartPos; // 屏幕坐标

    // 批量移动相关：记录每个选中物体的初始位置
    private Dictionary<GameObject, Vector3Int> batchStartPositions = new Dictionary<GameObject, Vector3Int>();

    // 拖拽相关
    private Vector3 dragStartMousePos;
    private bool isDragging = false;
    private const float DRAG_THRESHOLD = 5f; // 拖拽阈值

    #region 初始化
    public void Initialize()
    {
        ui = UIReferences.Instance;
    }
    #endregion

    #region Update 主循环
    private void Update()
    {
        UpdateMouseEvent();
        UpdateKeyboardEvent();
        UpdateDebugInput();
    }
    #endregion

    #region 射线检测
    /// <summary>
    /// 射线检测 - 获取鼠标点击的对象
    /// </summary>
    private GameObject RayDetect(string tagFilter = null)
    {
        GameObject ob = null;
        Vector3 worldPos = ui.camSence.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0; // 强制 Z=0
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
    #endregion

    #region 鼠标事件处理
    private void UpdateMouseEvent()
    {
        // 1. 优先处理右键（编辑/取消）
        if (Input.GetMouseButtonDown(1))
        {
            HandleRightClick();
            return;
        }

        // 2. 鼠标左键按下
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            dragStartMousePos = Input.mousePosition;
            isDragging = false;

            rayHitMove = RayDetect();

            // 如果点击了节点 -> 准备拖拽
            if (rayHitMove != null && rayHitMove.tag == Constants.Tags.Node)
            {
                // 记录基准点
                Node n = rayHitMove.GetComponent<Node>();
                lastNodePosition = new Vector3Int(n.sc.HexGridY, n.sc.HexGridX, 0);
            }
            // 锚点也准备拖拽
            else if (rayHitMove != null && rayHitMove.tag == Constants.Tags.Anchor)
            {
                // 锚点不需要记录 lastNodePosition，它直接跟随鼠标
            }
        }

        // 3. 鼠标拖动中
        if (Input.GetMouseButton(0))
        {
            // 检测是否达到拖拽阈值
            if (!isDragging && Vector3.Distance(Input.mousePosition, dragStartMousePos) > DRAG_THRESHOLD)
            {
                isDragging = true; // 开始拖拽

                // 如果点击的是空地 -> 开始框选
                if (rayHitMove == null)
                {
                    // 【规则】只有在非编辑模式下才允许框选
                    if (currentEditPanel == null)
                    {
                        isBoxSelecting = true;
                        boxStartPos = dragStartMousePos;
                        // 开始框选时，如果没有按 Shift，清除已有选择
                        if (!Input.GetKey(KeyCode.LeftShift)) ClearSelection();
                    }
                }
                // 如果点击的是节点 -> 开始移动
                else if (rayHitMove.tag == Constants.Tags.Node)
                {
                    // 如果拖拽的是未选中的节点 -> 单选它
                    if (!selectedObjects.Contains(rayHitMove))
                    {
                        if (!Input.GetKey(KeyCode.LeftShift)) ClearSelection();
                        AddToSelection(rayHitMove);
                    }

                    // 初始化批量移动数据
                    batchStartPositions.Clear();
                    foreach (var obj in selectedObjects)
                    {
                        if (obj.tag == Constants.Tags.Node)
                        {
                            Node n = obj.GetComponent<Node>();
                            batchStartPositions[obj] = new Vector3Int(n.sc.HexGridY, n.sc.HexGridX, 0);
                        }
                    }
                }
            }

            // 执行拖拽逻辑
            if (isDragging)
            {
                if (isBoxSelecting)
                {
                    UIManager.Instance.UpdateSelectionBox(boxStartPos, Input.mousePosition);
                }
                else if (rayHitMove != null)
                {
                    if (rayHitMove.tag == Constants.Tags.Node)
                    {
                        HandleBatchMove(); // 批量移动节点
                    }
                    else if (rayHitMove.tag == Constants.Tags.Anchor)
                    {
                        // 锚点移动（保持单体）
                        Vector3 worldPos = ui.camSence.ScreenToWorldPoint(Input.mousePosition);
                        worldPos.z = 0;
                        Vector3Int currentGridPos = ui.grid.WorldToCell(worldPos);
                        Vector3 gridCenterPos = ui.grid.CellToWorld(currentGridPos);
                        HandleAnchorMove(gridCenterPos, currentGridPos);
                    }
                }
            }
        }

        // 4. 鼠标左键松开
        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                // 结束拖拽/框选
                if (isBoxSelecting)
                {
                    isBoxSelecting = false;
                    UIManager.Instance.HideSelectionBox();
                    PerformBoxSelection();
                }
                else if (rayHitMove != null)
                {
                    // 结算移动命令
                    FinishBatchMove();
                }
            }
            else
            {
                // 这是一个点击事件（没有发生拖拽）
                HandleLeftClick();
            }

            isDragging = false;
            rayHitMove = null;
            batchStartPositions.Clear();
        }

        // 5. 处理双击连线（添加锚点）
        // 只有在编辑模式下才允许
        if (currentEditPanel != null)
        {
            HandleLineDoubleClick();
            HandleAnchorSelection(); // 锚点点击选中
        }
    }

    // 处理左键点击（非拖拽）
    private void HandleLeftClick()
    {
        GameObject hit = RayDetect();

        // 点击空地 -> 取消所有选中
        if (hit == null)
        {
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                ClearSelection();
                // 注意：这里不关闭面板，保留右键关闭的习惯
            }
        }
        // 点击节点 -> 选中/反选
        else if (hit.tag == Constants.Tags.Node)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                // 反选逻辑
                if (selectedObjects.Contains(hit))
                    RemoveFromSelection(hit);
                else
                    AddToSelection(hit);
            }
            else
            {
                // 单选（如果没有按Shift，且点击了未选中的节点，则清除其他只选这个）
                // 如果点击了已选中的节点，保持选中状态不变
                if (!selectedObjects.Contains(hit))
                {
                    ClearSelection();
                    AddToSelection(hit);
                }
            }

            // 更新面板状态（多选时关闭面板）
            UpdateEditPanelState();
        }
    }

    // 处理右键点击
    private void HandleRightClick()
    {
        GameObject hit = RayDetect();
        if (hit && hit.tag == Constants.Tags.Node)
        {
            // 1. 强制单选
            ClearSelection();
            AddToSelection(hit);

            // 2. 显示锚点（这是右键特权）
            hit.GetComponent<Node>().UpdateLineAnchor();

            // 3. 打开面板（防重复）
            if (currentEditPanel != null)
            {
                var currentId = currentEditPanel.GetComponent<PanelScienceEdit>().sc.Id;
                var clickId = hit.GetComponent<Node>().sc.Id;
                if (currentId == clickId) return;

                currentEditPanel.GetComponent<PanelScienceEdit>().DestoryPanel();
            }
            OpenEditPanel(hit);
        }
        else
        {
            // 右键空地 -> 取消选中，关闭面板
            ClearSelection();
            if (currentEditPanel)
            {
                currentEditPanel.GetComponent<PanelScienceEdit>().DestoryPanel();
                currentEditPanel = null;
            }
        }
    }

    // 批量移动逻辑（拖拽中）
    private void HandleBatchMove()
    {
        Vector3 worldPos = ui.camSence.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;
        Vector3Int currentGridPos = ui.grid.WorldToCell(worldPos);

        // 计算增量 (当前鼠标格子 - 起始基准格子)
        Vector3Int delta = currentGridPos - lastNodePosition;

        // 移动所有选中节点
        foreach (var kvp in batchStartPositions)
        {
            GameObject obj = kvp.Key;
            Vector3Int startPos = kvp.Value;
            Vector3Int targetPos = startPos + delta;

            HandleNodeMove(obj, targetPos);
        }
    }

    // 批量移动结算（松开鼠标）
    private void FinishBatchMove()
    {
        if (rayHitMove.tag == Constants.Tags.Node)
        {
            var batchCmd = new BatchMoveCommand();
            bool hasMoved = false;

            foreach (var kvp in batchStartPositions)
            {
                GameObject obj = kvp.Key;
                Vector3Int startPos = kvp.Value;
                Node n = obj.GetComponent<Node>();
                Vector3Int currentPos = new Vector3Int(n.sc.HexGridY, n.sc.HexGridX, 0);

                if (startPos != currentPos)
                {
                    hasMoved = true;
                    // 创建命令
                    var moveCmd = new MoveNodeCommand(n, startPos, currentPos, ui.grid); // 如果需要 Manager
                    batchCmd.Add(moveCmd);
                }
            }

            if (hasMoved)
            {
                CommandManager.Instance.ExecuteCommand(batchCmd);
            }

            // 恢复样式
            rayHitMove.GetComponent<Node>().SetHoverStyle(false);
        }
    }

    private void UpdateEditPanelState()
    {
        // 多选时关闭面板
        if (selectedObjects.Count > 1)
        {
            if (currentEditPanel)
            {
                currentEditPanel.GetComponent<PanelScienceEdit>().DestoryPanel();
                currentEditPanel = null;
            }
        }
    }

    /// <summary>
    /// 打开节点编辑面板
    /// </summary>
    private void OpenEditPanel(GameObject nodeObj)
    {
        string nodeId = nodeObj.name;
        Node node = nodeObj.GetComponent<Node>();

        // 实例化面板
        currentEditPanel = Instantiate(ui.panelNodeEditPrefab);
        currentEditPanel.transform.SetParent(
            ui.canvas.transform.Find(Constants.UIPath.PanelRightContent),
            false
        );
        currentEditPanel.name = $"{nodeId}(Edit)";

        // 设置面板数据
        var panelScript = currentEditPanel.GetComponent<PanelScienceEdit>();
        panelScript.node = node;
        panelScript.sc = node.sc;

        // 更新节点显示
        node.SetSelectStyle(true);

        // 触发事件
        EventCenter.Instance.TriggerNodeSelected(node);
        EventCenter.Instance.TriggerLogMessage($"编辑节点：{nodeId}:{node.sc.Name}");
    }

    /// <summary>
    /// 处理节点移动（视觉和数据更新）
    /// </summary>
    private void HandleNodeMove(GameObject obj, Vector3Int gridPosI)
    {
        Node node = obj.GetComponent<Node>();
        node.UpdateGridPos(gridPosI);

        Vector3 newPos = ui.grid.CellToWorld(gridPosI);
        newPos.z = 0;
        obj.transform.position = newPos;

        // 更新子节点连线
        foreach (var afterId in node.sc.After_technology)
        {
            GameObject child = ui.tilemap.transform.Find(afterId.ToString())?.gameObject;
            child?.GetComponent<Node>().UpdateNodeAppearance();
        }

        // 如果是当前编辑的节点，更新面板和锚点
        if (currentEditPanel && currentEditPanel.GetComponent<PanelScienceEdit>().sc.Id == node.sc.Id)
        {
            currentEditPanel.GetComponent<PanelScienceEdit>()
                .UpdatePositionByDrag(new(gridPosI.y, gridPosI.x)); // 注意顺序

            node.ClearAnchor();
            node.UpdateLineAnchor();
        }
    }

    /// <summary>
    /// 处理锚点移动
    /// </summary>
    private void HandleAnchorMove(Vector3 gridPos, Vector3Int gridPosI)
    {
        int lrIndex = int.Parse(rayHitMove.name);
        LineRenderer lr = rayHitMove.GetComponentInParent<LineRenderer>();

        Vector2 anchorMoveTo = new(gridPos.x, gridPos.y);
        Vector3 lineIndexAt = new(anchorMoveTo.x, anchorMoveTo.y, 1); // Z=1 确保显示
        rayHitMove.transform.position = anchorMoveTo;
        lr.SetPosition(lrIndex, lineIndexAt);

        string nodeFrom = lr.gameObject.name.Split("->")[0];
        string nodeTo = lr.gameObject.name.Split("->")[1];

        if (DataManager.Instance.ScienceDict.TryGetValue(int.Parse(nodeTo), out var sc))
        {
            int positionCount = lr.positionCount;
            string newpos = null;

            for (int i = 1; i < positionCount - 1; i++)
            {
                if (newpos is not null) newpos += "_";
                var cellPos = ui.grid.WorldToCell(lr.GetPosition(i));
                newpos += $"{cellPos.y}_{cellPos.x}";
            }

            sc.PathNode = sc.PathNode.UpdatePathNodeById(nodeFrom, newpos);

            if (currentEditPanel)
            {
                currentEditPanel.GetComponent<PanelScienceEdit>().UpdatePrePath(sc.PathNode);
            }
        }
    }
    #endregion

    #region 锚点操作逻辑
    private void HandleAnchorSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameObject hit = RayDetect();
            if (hit != null && hit.tag == Constants.Tags.Anchor)
            {
                if (selectedAnchor != null)
                    selectedAnchor.GetComponent<SpriteRenderer>().color = Color.white;
                selectedAnchor = hit;
                selectedAnchor.GetComponent<SpriteRenderer>().color = Color.yellow;
            }
            else if (hit == null)
            {
                if (selectedAnchor != null)
                    selectedAnchor.GetComponent<SpriteRenderer>().color = Color.white;
                selectedAnchor = null;
            }
        }
    }

    private void HandleLineDoubleClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time - lastClickTime < DOUBLE_CLICK_TIME)
            {
                var (lr, index, point) = DetectLineNearMouse();
                if (lr != null)
                {
                    var cmd = new AddAnchorCommand(lr, index, point);
                    CommandManager.Instance.ExecuteCommand(cmd);

                    string nodeToId = lr.name.Split(new string[] { "->" }, System.StringSplitOptions.None)[1];
                    int id = int.Parse(nodeToId);

                    if (currentEditPanel == null || currentEditPanel.GetComponent<PanelScienceEdit>().sc.Id != id)
                    {
                        if (currentEditPanel) currentEditPanel.GetComponent<PanelScienceEdit>().DestoryPanel();
                        var nodeObj = NodeManager.Instance.GetNode(id).gameObject;
                        OpenEditPanel(nodeObj);
                    }
                }
            }
            lastClickTime = Time.time;
        }
    }

    private (LineRenderer, int, Vector3) DetectLineNearMouse()
    {
        Vector3 mousePos = ui.camSence.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        var lines = GameObject.FindGameObjectsWithTag(Constants.Tags.NodeLine);

        float minDst = 5f; // 阈值
        LineRenderer bestLr = null;
        int bestIndex = -1;
        Vector3 bestPoint = Vector3.zero;

        foreach (var lineObj in lines)
        {
            LineRenderer lr = lineObj.GetComponent<LineRenderer>();
            if (!lr) continue;
            for (int i = 0; i < lr.positionCount - 1; i++)
            {
                Vector3 p1 = lr.GetPosition(i);
                Vector3 p2 = lr.GetPosition(i + 1);

                // 忽略 Z 轴差异
                p1.z = 0; p2.z = 0;

                float dst = HandleUtility_DistancePointLine(mousePos, p1, p2);
                if (dst < minDst)
                {
                    minDst = dst;
                    bestLr = lr;
                    bestIndex = i + 1;
                    bestPoint = ProjectPointOnLineSegment(p1, p2, mousePos);
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

    #region 键盘事件处理
    private void UpdateKeyboardEvent()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
        {
            CommandManager.Instance.Undo();
            return;
        }
        if (Input.GetKey(KeyCode.LeftControl) &&
            (Input.GetKeyDown(KeyCode.Y) ||
             (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Z))))
        {
            CommandManager.Instance.Redo();
            return;
        }

        if (!EventSystem.current.currentSelectedGameObject)
        {
            // 删除锚点
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                if (selectedAnchor != null)
                {
                    var cmd = new DeleteAnchorCommand(selectedAnchor);
                    CommandManager.Instance.ExecuteCommand(cmd);
                    selectedAnchor = null;
                    return;
                }

                // 删除节点
                if (currentEditPanel) DeleteCurrentNode();
            }

            // ESC 关闭面板
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentEditPanel)
                {
                    currentEditPanel.GetComponent<PanelScienceEdit>().DestoryPanel();
                    currentEditPanel = null;
                }
            }
        }
    }

    private void DeleteCurrentNode()
    {
        var panelScript = currentEditPanel.GetComponent<PanelScienceEdit>();
        var sc = panelScript.sc;
        var deleteCmd = new DeleteNodeCommand(sc, ui);
        CommandManager.Instance.ExecuteCommand(deleteCmd);
        panelScript.DestoryPanel();
        currentEditPanel = null;
    }
    #endregion

    #region 选中管理
    private void AddToSelection(GameObject obj)
    {
        if (selectedObjects.Contains(obj)) return;
        selectedObjects.Add(obj);

        if (obj.tag == Constants.Tags.Node)
        {
            var node = obj.GetComponent<Node>();
            node.SetSelectStyle(true);
            // 左键选中不显示锚点
        }
        else if (obj.tag == Constants.Tags.Anchor)
        {
            obj.GetComponent<SpriteRenderer>().color = Color.yellow;
        }
    }

    private void RemoveFromSelection(GameObject obj)
    {
        if (!selectedObjects.Contains(obj)) return;
        selectedObjects.Remove(obj);

        if (obj.tag == Constants.Tags.Node)
        {
            var node = obj.GetComponent<Node>();
            node.SetSelectStyle(false);
            node.ClearAnchor();
        }
        else if (obj.tag == Constants.Tags.Anchor)
        {
            obj.GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    private void ClearSelection()
    {
        foreach (var obj in selectedObjects.ToList())
        {
            RemoveFromSelection(obj);
        }
        selectedObjects.Clear();
    }
    #endregion

    #region 框选逻辑
    private void HandleBoxSelection()
    {
        // 1. 开始框选
        if (Input.GetMouseButtonDown(0))
        {
            // 这里的逻辑已经移到了 UpdateMouseEvent 的拖动部分
            // 如果没有点中物体，就会触发 isBoxSelecting = true
        }

        // 这里只保留辅助逻辑，实际框选在 UpdateMouseEvent 中处理
    }

    private void PerformBoxSelection()
    {
        Vector2 boxEndPos = Input.mousePosition;
        Vector2 min = Vector2.Min(boxStartPos, boxEndPos);
        Vector2 max = Vector2.Max(boxStartPos, boxEndPos);
        Vector3 worldMin = ui.camSence.ScreenToWorldPoint(min);
        Vector3 worldMax = ui.camSence.ScreenToWorldPoint(max);

        Collider2D[] hits = Physics2D.OverlapAreaAll(worldMin, worldMax);

        foreach (var hit in hits)
        {
            GameObject obj = hit.gameObject;
            if (obj.tag == Constants.Tags.Node)
            {
                AddToSelection(obj);
            }
        }
        UpdateEditPanelState();
    }
    #endregion

    #region 调试
    private void UpdateDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.Slash)) DebugPrintScienceDict();
        if (Input.GetKeyDown(KeyCode.Period)) DebugPrintSelectedNode();
    }
    private void DebugPrintScienceDict()
    {
        foreach (var sc in DataManager.Instance.ScienceDict)
            Debug.Log($"KEY:{sc.Key}, VALUE:{sc.Value}");
    }
    private void DebugPrintSelectedNode()
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
    #endregion

    #region 公共接口
    public void SetCurrentEditPanel(GameObject panel)
    {
        currentEditPanel = panel;
    }
    public GameObject GetCurrentEditPanel()
    {
        return currentEditPanel;
    }
    #endregion
}
