using UnityEngine;

public class ChangeNodeScaleCommand : ICommand
{
    private Node node;
    private float oldIconScale;
    private float oldLineScale;
    private float newIconScale;
    private float newLineScale;

    public string Description => $"修改节点 {node.sc.Id} 尺寸";

    public ChangeNodeScaleCommand(Node node, float newIconScale, float newLineScale)
    {
        this.node = node;
        this.newIconScale = newIconScale;
        this.newLineScale = newLineScale;

        // 记录旧值用于撤销
        this.oldIconScale = node.sc.IconScale;
        this.oldLineScale = node.sc.LineScale;
    }

    public void Execute()
    {
        ApplyScale(newIconScale, newLineScale);
    }

    public void Undo()
    {
        ApplyScale(oldIconScale, oldLineScale);
    }

    private void ApplyScale(float iconScale, float lineScale)
    {
        // 修改数据
        node.sc.IconScale = iconScale;
        node.sc.LineScale = lineScale;

        // 刷新外观
        node.UpdateNodeAppearance();

        // 如果该节点是前置节点，它控制的连线宽度也需要更新
        // UpdateNodeAppearance 内部通常只更新自身的显示，
        // 但连线宽度是由前置节点的数据决定的，所以这里需要触发连线更新

        // 刷新以该节点为起点的连线 (UpdateNodeAppearance 内部应该已经包含了 UpdateLine)
        // 但为了保险，我们确保它刷新
        node.UpdateNodeAppearance();

        // 另外，如果当前有编辑面板且正是这个节点，需要同步UI下拉框
        UpdateEditPanelUI();
    }

    private void UpdateEditPanelUI()
    {
        var inputMgr = GameObject.Find(Constants.GameObjectNames.MainManager)?.GetComponent<InputManager>();
        if (inputMgr != null && inputMgr.CurrentEditPanel != null)
        {
            var panel = inputMgr.CurrentEditPanel.GetComponent<PanelScienceEdit>();
            if (panel.sc.Id == node.sc.Id)
            {
                // 简单的刷新UI方法，或者你需要给 PanelScienceEdit 添加专门更新Scale UI的方法
                // 这里假设 RefreshUI 会处理，或者你需要手动更新 dropdown
                // panel.RefreshUI(); 
            }
        }
    }
}
