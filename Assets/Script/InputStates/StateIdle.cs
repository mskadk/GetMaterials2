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
        // 1. 右键处理 (保持不变，这是退出的唯一途径)
        if (Input.GetMouseButtonDown(1))
        {
            HandleRightClick(context);
            return;
        }

        // 2. 左键处理
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            // 射线检测
            GameObject hit = context.RayDetect();

            // ============ 【修复重点：编辑模式下的拦截逻辑】 ============
            if (context.CurrentEditPanel != null)
            {
                // 双击连线添加锚点 (保持原功能)
                if (Time.time - lastClickTime < DOUBLE_CLICK_TIME)
                {
                    if (HandleLineDoubleClick(context)) return;
                }
                lastClickTime = Time.time;

                int editId = context.CurrentEditPanel.GetComponent<PanelScienceEdit>().sc.Id;

                // 情况 A: 点击了当前正在编辑的节点 -> 允许进入拖拽状态
                if (hit != null && hit.tag == Constants.Tags.Node && hit.GetComponent<Node>().sc.Id == editId)
                {
                    context.ChangeState(new StateDragNode(hit, Input.mousePosition));
                    return;
                }

                // 情况 B: 点击了锚点 -> 允许进入锚点拖拽状态
                if (hit != null && hit.tag == Constants.Tags.Anchor)
                {
                    context.ChangeState(new StateDragAnchor(hit, Input.mousePosition));
                    return;
                }

                // 情况 C: 点击了空地 或 其他节点 -> 【直接忽略】
                // 既不触发框选，也不触发取消选择，完美符合你的要求
                return;
            }
            // =========================================================

            // --- 以下是 普通模式 (非编辑模式) 的逻辑 ---

            // 双击检测
            if (Time.time - lastClickTime < DOUBLE_CLICK_TIME)
            {
                // 普通模式下如果需要双击逻辑写在这里，目前看来主要是编辑模式用
            }
            lastClickTime = Time.time;

            if (hit == null)
            {
                // 点击空地 -> 框选
                context.ChangeState(new StateBoxSelect(Input.mousePosition));
            }
            else if (hit.tag == Constants.Tags.Node)
            {
                // 点击节点 -> 拖拽/选中
                context.ChangeState(new StateDragNode(hit, Input.mousePosition));
            }
            else if (hit.tag == Constants.Tags.Anchor)
            {
                // 普通模式下是否允许拖拽Anchor? 通常允许
                context.ChangeState(new StateDragAnchor(hit, Input.mousePosition));
            }
        }
    }

    private void HandleRightClick(InputManager context)
    {
        GameObject hit = context.RayDetect();

        // 如果点击了节点
        if (hit && hit.tag == Constants.Tags.Node)
        {
            // 编辑模式下，如果右键点了别的节点，切换编辑对象
            if (context.CurrentEditPanel != null)
            {
                var currentId = context.CurrentEditPanel.GetComponent<PanelScienceEdit>().sc.Id;
                var clickId = hit.GetComponent<Node>().sc.Id;

                // 只有点的不是自己时才切换，避免闪烁
                if (currentId != clickId)
                {
                    context.CurrentEditPanel.GetComponent<PanelScienceEdit>().DestoryPanel();
                    SelectionManager.Instance.ClearNodes(); // 清理旧的
                    SelectionManager.Instance.AddNode(hit); // 选中新的
                    context.OpenEditPanel(hit);
                }
                // 如果右键点的是自己，不做任何事（或者你可以选择刷新面板）
            }
            else
            {
                // 普通模式 -> 打开面板
                SelectionManager.Instance.ClearNodes();
                SelectionManager.Instance.AddNode(hit);
                context.OpenEditPanel(hit);
            }

            hit.GetComponent<Node>().UpdateLineAnchor();
        }
        else
        {
            // 右键点击空地 -> 退出编辑模式
            SelectionManager.Instance.ClearNodes();
            if (context.CurrentEditPanel)
            {
                context.CurrentEditPanel.GetComponent<PanelScienceEdit>().DestoryPanel();
                context.SetCurrentEditPanel(null); // 确保 InputManager 知道面板没了
            }
        }
    }

    private bool HandleLineDoubleClick(InputManager context)
    {
        // 逻辑保持不变
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
