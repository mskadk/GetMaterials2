using Assets.Script.My.Extention;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 输入管理器 - 处理所有鼠标和键盘输入
/// </summary>
public class InputManager : MonoBehaviour
{
    #region 引用
    private UIReferences ui;
    private MainManager mainManager;
    #endregion

    #region 输入状态
    private Vector3 鼠标按下位置_屏幕;
    private GameObject rayHitMove;
    private GameObject rayHitNode;
    private GameObject currentEditPanel;
    #endregion

    private Vector3Int lastNodePosition;  // 记录节点移动前的位置

    #region 初始化
    public void Initialize(MainManager manager)
    {
        mainManager = manager;
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
    private GameObject RayDetect()
    {
        GameObject ob = null;
        Vector3 worldPos = ui.camSence.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector3.forward, Mathf.Infinity);
        if (hit)
        {
            ob = hit.transform.gameObject;
        }
        return ob;
    }
    #endregion

    #region 鼠标事件处理
    private void UpdateMouseEvent()
    {
        // 节点编辑模式
        if (ui.toggleEditNode && ui.toggleEditNode.isOn)
        {
            HandleNodeEdit();
        }

        // 节点移动模式
        if (ui.toggleEditNode && ui.toggleMoveNode.isOn)
        {
            HandleNodeDrag();
        }
    }

    /// <summary>
    /// 处理节点编辑（右键点击打开编辑面板）
    /// </summary>
    private void HandleNodeEdit()
    {
        if (Input.GetMouseButtonDown(1))
        {
            // 点到UI上不处理
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // 右键空白处，关闭面板
            if (currentEditPanel && !EventSystem.current.currentSelectedGameObject)
            {
                currentEditPanel.GetComponent<PanelScienceEdit>().DestoryPanel();
                currentEditPanel = null;
            }

            // 选中节点，打开编辑面板
            rayHitNode = RayDetect();
            if (rayHitNode && rayHitNode.tag == Constants.Tags.Node)
            {
                OpenEditPanel(rayHitNode);
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
        node.UpdateLineAnchor();

        // 触发事件
        EventCenter.Instance.TriggerNodeSelected(node);
        EventCenter.Instance.TriggerLogMessage($"编辑节点：{nodeId}:{node.sc.Name}");
    }

    /// <summary>
    /// 处理节点拖拽（左键拖动）
    /// </summary>
    private void HandleNodeDrag()
    {
        // 鼠标按下
        if (Input.GetMouseButtonDown(0))
        {
            鼠标按下位置_屏幕 = Input.mousePosition;
            rayHitMove = RayDetect();

            if (rayHitMove && rayHitMove.tag == Constants.Tags.Node)
            {
                // 记录初始位置
                Node node = rayHitMove.GetComponent<Node>();
                lastNodePosition = new Vector3Int(node.sc.HexGridY, node.sc.HexGridX, 0);

                // 按下时切换样式
                node.SetHoverStyle(true);
            }
        }

        // 拖动中
        if (rayHitMove && Input.GetMouseButton(0))
        {
            HandleDragMove();
        }

        // 松开鼠标 - 生成命令
        if (Input.GetMouseButtonUp(0))
        {
            if (rayHitMove && rayHitMove.tag == Constants.Tags.Node)
            {

                Node node = rayHitMove.GetComponent<Node>();
                Vector3Int currentPos = new Vector3Int(node.sc.HexGridY, node.sc.HexGridX, 0);

                // 如果位置改变了，创建移动命令
                if (lastNodePosition != currentPos)
                {
                    var moveCmd = new MoveNodeCommand(
                        node,
                        lastNodePosition,
                        currentPos,
                        ui.grid,
                        mainManager
                    );

                    // 注意：这里是 Execute 后的状态，所以先 Undo 回到起始位置，再通过命令管理器执行
                    node.UpdateGridPos(lastNodePosition);  // 先回到原位置
                    CommandManager.Instance.ExecuteCommand(moveCmd);  // 再通过命令执行
                }

                node.SetHoverStyle(false);
            }
            rayHitMove = null;
        }
    }

    /// <summary>
    /// 执行拖动移动逻辑
    /// </summary>
    private void HandleDragMove()
    {
        // 检查是否移动到了新的格子
        var 按下时格子 = ui.grid.WorldToCell(ui.camSence.ScreenToWorldPoint(鼠标按下位置_屏幕));
        var 当前格子 = ui.grid.WorldToCell(ui.camSence.ScreenToWorldPoint(Input.mousePosition));

        // 未移动到新格子，不处理
        if (按下时格子 == 当前格子)
        {
            return;
        }

        // 更新鼠标位置记录
        鼠标按下位置_屏幕 = Input.mousePosition;

        // 计算新位置
        Vector3 鼠标_世界位置 = ui.camSence.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int gridPosI = ui.grid.WorldToCell(new Vector3(鼠标_世界位置.x, 鼠标_世界位置.y, 0));
        Vector3 gridPos = ui.grid.CellToWorld(gridPosI);

        // 移动对象
        rayHitMove.transform.position = gridPos;

        // 根据对象类型执行不同逻辑
        if (rayHitMove.tag == Constants.Tags.Node)
        {
            HandleNodeMove(gridPosI);
        }
        else if (rayHitMove.tag == Constants.Tags.Anchor)
        {
            HandleAnchorMove(gridPos, gridPosI);
        }
    }

    /// <summary>
    /// 处理节点移动
    /// </summary>
    private void HandleNodeMove(Vector3Int gridPosI)
    {
        Node node = rayHitMove.GetComponent<Node>();
        node.UpdateGridPos(gridPosI);

        // 更新子节点的线条起始位置
        foreach (var afterId in node.sc.After_technology)
        {
            GameObject childNode = ui.tilemap.transform.Find(afterId.ToString())?.gameObject;
            childNode?.GetComponent<Node>().UpdateNodeAppearance();
        }

        // 如果有打开编辑面板，更新位置信息
        GameObject editPanel = GameObject.Find($"{rayHitMove.name}(Edit)");
        if (editPanel)
        {
            editPanel.GetComponent<PanelScienceEdit>()
                .UpdatePositionByDrag(new(gridPosI.y, gridPosI.x));
            node.ClearAnchor();
            node.UpdateLineAnchor();
        }

        // 触发移动事件
        EventCenter.Instance.TriggerNodeMoved(node, gridPosI);
    }

    /// <summary>
    /// 处理锚点移动
    /// </summary>
    private void HandleAnchorMove(Vector3 gridPos, Vector3Int gridPosI)
    {
        int lrIndex = int.Parse(rayHitMove.name);
        LineRenderer lr = rayHitMove.GetComponentInParent<LineRenderer>();

        // 更新锚点位置
        Vector2 anchorMoveTo = new(gridPos.x, gridPos.y);
        Vector3 lineIndexAt = new(anchorMoveTo.x, anchorMoveTo.y, 1);
        rayHitMove.transform.position = anchorMoveTo;
        lr.SetPosition(lrIndex, lineIndexAt);

        // 更新字典中的路径位置
        string nodeFrom = lr.gameObject.name.Split("->")[0];
        string nodeTo = lr.gameObject.name.Split("->")[1];

        if (mainManager.ScienceDict.TryGetValue(int.Parse(nodeTo), out var sc))
        {
            int positionCount = lr.positionCount;
            string newpos = null;

            for (int i = 1; i < positionCount - 1; i++)
            {
                if (newpos is not null)
                {
                    newpos += "_";
                }
                var cellPos = ui.grid.WorldToCell(lr.GetPosition(i));
                newpos += $"{cellPos.y}_{cellPos.x}";
            }

            sc.PathNode = sc.PathNode.UpdatePathNodeById(nodeFrom, newpos);

            // 更新编辑面板中的路径字段显示
            if (currentEditPanel)
            {
                currentEditPanel.GetComponent<PanelScienceEdit>().UpdatePrePath(sc.PathNode);
            }
        }
    }
    #endregion

    #region 键盘事件处理
    private void UpdateKeyboardEvent()
    {
        // 撤销：Ctrl+Z
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
        {
            CommandManager.Instance.Undo();
            return;  // 避免继续处理其他按键
        }
        // 重做：Ctrl+Y 或 Ctrl+Shift+Z
        if (Input.GetKey(KeyCode.LeftControl) &&
            (Input.GetKeyDown(KeyCode.Y) ||
             (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Z))))
        {
            CommandManager.Instance.Redo();
            return;
        }

        // 只在有编辑面板且没有输入框获焦时处理
        if (!EventSystem.current.currentSelectedGameObject && currentEditPanel)
        {
            HandleEditPanelShortcuts();
        }
    }

    /// <summary>
    /// 处理编辑面板快捷键
    /// </summary>
    private void HandleEditPanelShortcuts()
    {
        // ESC - 关闭编辑面板
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            currentEditPanel.GetComponent<PanelScienceEdit>().DestoryPanel();
            currentEditPanel = null;
        }

        // Delete - 删除节点
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            DeleteCurrentNode();
        }
    }

    /// <summary>
    /// 删除当前编辑的节点
    /// </summary>
    private void DeleteCurrentNode()
    {
        var panelScript = currentEditPanel.GetComponent<PanelScienceEdit>();
        var sc = panelScript.sc;

        // 创建删除命令
        var deleteCmd = new DeleteNodeCommand(sc, mainManager, ui);
        CommandManager.Instance.ExecuteCommand(deleteCmd);

        // 关闭编辑面板
        panelScript.DestoryPanel();
        currentEditPanel = null;
    }
    #endregion

    #region 调试输入
    private void UpdateDebugInput()
    {
        // / 键 - 打印科技字典
        if (Input.GetKeyDown(KeyCode.Slash))
        {
            DebugPrintScienceDict();
        }

        // . 键 - 打印选中节点信息
        if (Input.GetKeyDown(KeyCode.Period))
        {
            DebugPrintSelectedNode();
        }
    }

    /// <summary>
    /// 调试：打印科技字典
    /// </summary>
    private void DebugPrintScienceDict()
    {
        foreach (var sc in mainManager.ScienceDict)
        {
            Debug.Log($"KEY:{sc.Key}, VALUE:{sc.Value}");
        }
    }

    /// <summary>
    /// 调试：打印选中节点信息
    /// </summary>
    private void DebugPrintSelectedNode()
    {
        var hitObj = RayDetect();
        if (hitObj != null && hitObj.tag == Constants.Tags.Node)
        {
            var node = hitObj.GetComponent<Node>();
            if (mainManager.ScienceDict.TryGetValue(node.sc.Id, out Science sc))
            {
                Debug.Log($"字典数据：{sc}");
            }
            Debug.Log($"测试节点：{node.sc}");

            string afterTech = string.Join("|", sc.After_technology);
            Debug.Log($"After:{afterTech}");
        }
        else
        {
            Debug.Log("未选中/非节点");
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 设置当前编辑面板（供外部调用）
    /// </summary>
    public void SetCurrentEditPanel(GameObject panel)
    {
        currentEditPanel = panel;
    }

    /// <summary>
    /// 获取当前编辑面板
    /// </summary>
    public GameObject GetCurrentEditPanel()
    {
        return currentEditPanel;
    }
    #endregion
}
