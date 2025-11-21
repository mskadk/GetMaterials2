using UnityEngine;

public class StateDragAnchor : IInputState
{
    private GameObject targetAnchor;
    private Vector3 startMousePos;
    private bool isDragging = false;
    private const float DRAG_THRESHOLD = 5f;

    public StateDragAnchor(GameObject anchor, Vector3 mousePos)
    {
        this.targetAnchor = anchor;
        this.startMousePos = mousePos;
    }

    public void OnEnter(InputManager context)
    {
        // 选中 Anchor
        SelectionManager.Instance.SelectAnchor(targetAnchor);
    }

    public void OnUpdate(InputManager context)
    {
        if (!isDragging && Vector3.Distance(Input.mousePosition, startMousePos) > DRAG_THRESHOLD)
        {
            isDragging = true;
        }

        if (isDragging)
        {
            Vector3 worldPos = context.GetMouseWorldPos();
            Vector3Int currentGridPos = context.UI.grid.WorldToCell(worldPos);
            Vector3 gridCenterPos = context.UI.grid.CellToWorld(currentGridPos);

            // 调用 Context 的方法处理具体的连线数据更新
            context.MoveAnchorVisual(targetAnchor, gridCenterPos, currentGridPos);
        }

        if (Input.GetMouseButtonUp(0))
        {
            context.ChangeState(new StateIdle());
        }
    }

    public void OnExit(InputManager context)
    {
        if (!isDragging)
        {
            // 只是点击，已经在OnEnter里选中了，这里不需要做额外的事
            // 除非你想实现点击空白取消选中Anchor，那是在Idle状态处理的
        }
    }
}
