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

        // 刷新外观（含连线宽度更新）
        node.UpdateNodeAppearance();

        // 如果当前有编辑面板且正是这个节点，需要同步UI
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
                panel.RefreshUI();
            }
        }
    }
}
