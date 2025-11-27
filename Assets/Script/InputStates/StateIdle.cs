// 修改 StateIdle.cs

using UnityEngine;
using UnityEngine.EventSystems;

public class StateIdle : IInputState
{
    private float lastClickTime;
    private const float DOUBLE_CLICK_TIME = 0.3f;

    public void OnEnter(InputManager context) { }
    public void OnExit(InputManager context) { }

    public void OnUpdate(InputManager context)
    {
        // 1. 右键处理
        if (Input.GetMouseButtonDown(1))
        {
            HandleRightClick(context);
            return;
        }

        // 2. 左键处理
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            GameObject hit = context.RayDetect();

            // 编辑模式
            if (context.CurrentEditPanel != null)
            {
                // 双击处理添加锚点
                if (Time.time - lastClickTime < DOUBLE_CLICK_TIME)
                {
                    if (HandleLineDoubleClick(context)) return;
                }
                lastClickTime = Time.time;

                int editId = context.CurrentEditPanel.GetComponent<PanelScienceEdit>().sc.Id;

                // 情况 A: 点击了当前正在编辑的节点 -> 进入统一拖拽状态
                if (hit != null && hit.tag == Constants.Tags.Node && hit.GetComponent<Node>().sc.Id == editId)
                {
                    context.ChangeState(new StateDrag(hit, Input.mousePosition));
                    return;
                }

                // 情况 B: 点击了锚点 -> 进入统一拖拽状态
                if (hit != null && hit.tag == Constants.Tags.Anchor)
                {
                    context.ChangeState(new StateDrag(hit, Input.mousePosition));
                    return;
                }

                // 情况 C: 点击了空地或其他节点 -> 忽略
                return;
            }

            // 普通模式

            // 双击处理
            if (Time.time - lastClickTime < DOUBLE_CLICK_TIME)
            {
                // 普通模式下的双击逻辑（如果需要）
            }
            lastClickTime = Time.time;

            if (hit == null)
            {
                // 点击空地 -> 框选
                context.ChangeState(new StateBoxSelect(Input.mousePosition));
            }
            else if (hit.tag == Constants.Tags.Node)
            {
                // 点击节点 -> 统一拖拽状态
                context.ChangeState(new StateDrag(hit, Input.mousePosition));
            }
            else if (hit.tag == Constants.Tags.Anchor)
            {
                // 点击锚点 -> 统一拖拽状态
                var renderer = hit.GetComponent<Renderer>();
                if (renderer != null && renderer.enabled)
                {
                    context.ChangeState(new StateDrag(hit, Input.mousePosition));
                }
            }
        }
    }

    private void HandleRightClick(InputManager context)
    {
        GameObject hit = context.RayDetect();

        if (hit && hit.tag == Constants.Tags.Node)
        {
            if (context.CurrentEditPanel != null)
            {
                var currentId = context.CurrentEditPanel.GetComponent<PanelScienceEdit>().sc.Id;
                var clickId = hit.GetComponent<Node>().sc.Id;

                if (currentId != clickId)
                {
                    context.CurrentEditPanel.GetComponent<PanelScienceEdit>().DestoryPanel();
                    SelectionManager.Instance.ClearNodes();
                    SelectionManager.Instance.ClearAnchors();
                    SelectionManager.Instance.AddNode(hit);
                    context.OpenEditPanel(hit);
                }
            }
            else
            {
                SelectionManager.Instance.ClearNodes();
                SelectionManager.Instance.ClearAnchors();
                SelectionManager.Instance.AddNode(hit);
                context.OpenEditPanel(hit);
            }

            hit.GetComponent<Node>().UpdateLineAnchor();
        }
        else
        {
            SelectionManager.Instance.ClearNodes();
            SelectionManager.Instance.ClearAnchors();
            if (context.CurrentEditPanel)
            {
                context.CurrentEditPanel.GetComponent<PanelScienceEdit>().DestoryPanel();
                context.SetCurrentEditPanel(null);
            }
        }
    }

    private bool HandleLineDoubleClick(InputManager context)
    {
        if (context.CurrentEditPanel == null) return false;
        var (lr, index, point) = context.DetectLineNearMouse();
        if (lr != null)
        {
            var cmd = new AddAnchorCommand(lr, index, point);
            CommandManager.Instance.ExecuteCommand(cmd);
            return true;
        }
        return false;
    }
}
