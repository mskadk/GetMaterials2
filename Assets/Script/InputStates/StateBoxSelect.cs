using UnityEngine;

public class StateBoxSelect : IInputState
{
    private Vector2 startScreenPos;
    private bool isDragging = false;
    private const float DRAG_THRESHOLD = 5f;

    public StateBoxSelect(Vector3 mousePos)
    {
        this.startScreenPos = mousePos;
    }

    public void OnEnter(InputManager context)
    {
        if (!Input.GetKey(KeyCode.LeftShift))
        {
            SelectionManager.Instance.ClearNodes();
        }
    }

    public void OnUpdate(InputManager context)
    {
        if (!isDragging && Vector2.Distance(Input.mousePosition, startScreenPos) > DRAG_THRESHOLD)
        {
            isDragging = true;
        }

        if (isDragging)
        {
            UIManager.Instance.UpdateSelectionBox(startScreenPos, Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            context.ChangeState(new StateIdle());
        }
    }

    public void OnExit(InputManager context)
    {
        if (isDragging)
        {
            UIManager.Instance.HideSelectionBox();
            PerformBoxSelection(context);
        }
        else
        {
            // 只是点击了空地 -> 清除所有选择
            if (context.CurrentEditPanel == null && !Input.GetKey(KeyCode.LeftShift))
            {
                SelectionManager.Instance.ClearNodes();
                SelectionManager.Instance.ClearAnchor();
            }

            // 如果处于编辑模式且点击空地 -> 关闭面板
            if (context.CurrentEditPanel != null)
            {
                // 原逻辑：右键空地才关闭，左键空地只取消选择。
                // 这里保持原逻辑，不做面板销毁
            }
        }
    }

    private void PerformBoxSelection(InputManager context)
    {
        Vector2 endPos = Input.mousePosition;
        Vector2 min = Vector2.Min(startScreenPos, endPos);
        Vector2 max = Vector2.Max(startScreenPos, endPos);
        Vector3 worldMin = context.UI.camSence.ScreenToWorldPoint(min);
        Vector3 worldMax = context.UI.camSence.ScreenToWorldPoint(max);

        Collider2D[] hits = Physics2D.OverlapAreaAll(worldMin, worldMax);

        foreach (var hit in hits)
        {
            GameObject obj = hit.gameObject;
            if (obj.tag == Constants.Tags.Node)
            {
                SelectionManager.Instance.AddNode(obj);
            }
            else if (obj.tag == Constants.Tags.Anchor && obj.GetComponent<Renderer>().enabled)
            {
                SelectionManager.Instance.SelectAnchor(obj);
            }
        }

        context.CheckEditPanelState();
    }
}
