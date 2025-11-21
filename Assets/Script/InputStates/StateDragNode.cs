using System.Collections.Generic;
using UnityEngine;

public class StateDragNode : IInputState
{
    private GameObject targetNode;
    private Vector3 startMousePos; // 屏幕坐标
    private Vector3 lastMouseWorldPos;
    private bool isDragging = false;
    private const float DRAG_THRESHOLD = 5f;

    // 记录批量移动的起始位置
    private Dictionary<GameObject, Vector3Int> batchStartPositions = new Dictionary<GameObject, Vector3Int>();

    public StateDragNode(GameObject target, Vector3 mousePos)
    {
        this.targetNode = target;
        this.startMousePos = mousePos;
    }

    public void OnEnter(InputManager context)
    {
        lastMouseWorldPos = context.GetMouseWorldPos();

        // 1. 处理选中逻辑 (按下时如果没按Shift且没点在已选物体上，不仅是选中，还要考虑是否清除其他)
        if (!Input.GetKey(KeyCode.LeftShift))
        {
            if (!SelectionManager.Instance.Contains(targetNode))
            {
                SelectionManager.Instance.ClearNodes();
                SelectionManager.Instance.AddNode(targetNode);
            }
        }
        else
        {
            // Shift按下时，暂时不处理，等MouseUp决定是加选还是减选
        }

        // 2. 记录所有选中物体的起始位置
        foreach (var obj in SelectionManager.Instance.GetSelectedNodes())
        {
            if (obj.tag == Constants.Tags.Node)
            {
                Node n = obj.GetComponent<Node>();
                batchStartPositions[obj] = new Vector3Int(n.sc.HexGridY, n.sc.HexGridX, 0);
            }
        }
    }

    public void OnUpdate(InputManager context)
    {
        // 检测是否开始拖拽
        if (!isDragging && Vector3.Distance(Input.mousePosition, startMousePos) > DRAG_THRESHOLD)
        {
            isDragging = true;
            // 设置 Hover 样式
            targetNode.GetComponent<Node>().SetHoverStyle(true);
        }

        // 拖拽中
        if (isDragging)
        {
            Vector3 currentMouseWorldPos = context.GetMouseWorldPos();
            Vector3 worldDelta = currentMouseWorldPos - lastMouseWorldPos;

            foreach (var kvp in batchStartPositions)
            {
                GameObject obj = kvp.Key;
                Vector3Int startGridPos = kvp.Value;

                Vector3 startNodeWorldPos = context.UI.grid.CellToWorld(startGridPos);
                startNodeWorldPos.z = 0;
                Vector3 targetWorldPos = startNodeWorldPos + worldDelta;
                Vector3Int targetGridPos = context.UI.grid.WorldToCell(targetWorldPos);

                // 调用 InputManager 的公共方法移动节点
                context.MoveNodeVisual(obj, targetGridPos);
            }
        }

        // 鼠标松开
        if (Input.GetMouseButtonUp(0))
        {
            context.ChangeState(new StateIdle());
        }
    }

    public void OnExit(InputManager context)
    {
        targetNode.GetComponent<Node>().SetHoverStyle(false);

        if (isDragging)
        {
            // 提交移动命令
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
                    batchCmd.Add(new MoveNodeCommand(n, startPos, currentPos, context.UI.grid));
                }
            }

            if (hasMoved)
            {
                CommandManager.Instance.ExecuteCommand(batchCmd);
            }
        }
        else
        {
            // 未发生拖拽，视为点击
            HandleClick(context);
        }
    }

    private void HandleClick(InputManager context)
    {
        // 编辑模式下，如果点的是当前编辑节点，不做额外处理
        if (context.CurrentEditPanel != null)
        {
            return;
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            SelectionManager.Instance.ToggleSelection(targetNode);
        }
        else
        {
            // 普通点击，如果之前为了批量移动多选了，现在只保留点击的这个
            SelectionManager.Instance.ClearNodes();
            SelectionManager.Instance.AddNode(targetNode);
        }

        // 选中逻辑发生变化，可能需要更新面板状态
        context.CheckEditPanelState();
    }
}
