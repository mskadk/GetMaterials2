using Assets.Script.My.Extention;
using UnityEngine;

public class AddAnchorCommand : ICommand
{
    private string targetPreId;
    private int targetNodeId;
    private int insertIndex;
    private Vector2 newWorldPos; // 改为世界坐标
    private string oldPath;
    private string newPath;

    public string Description => "添加锚点";

    public AddAnchorCommand(LineRenderer lr, int index, Vector3 worldPos)
    {
        if (!lr.name.Contains("->"))
        {
            Debug.LogError($"LineRenderer 命名格式错误: {lr.name}");
            return;
        }

        var parts = lr.name.Split(new string[] { "->" }, System.StringSplitOptions.None);
        targetPreId = parts[0];
        targetNodeId = int.Parse(parts[1]);
        insertIndex = index;

        // 获取吸附后的世界坐标
        // 如果是在 Free 模式下，SnapToGrid 会直接返回 worldPos
        // 如果是在 Hex/Square 模式下，会返回网格中心
        Vector3 snappedPos = GridManager.Instance.SnapToGrid(worldPos);
        newWorldPos = new Vector2(snappedPos.x, snappedPos.y);

        if (DataManager.Instance.TryGetScience(targetNodeId, out var sc))
        {
            oldPath = sc.PathNode;
            // 使用新的 InsertPathNode 方法（传入 Vector2）
            newPath = oldPath.InsertPathNode(targetPreId, insertIndex, newWorldPos);
        }
    }

    public void Execute()
    {
        if (DataManager.Instance.TryGetScience(targetNodeId, out var sc))
        {
            sc.PathNode = newPath;
            NodeManager.Instance.GetNode(targetNodeId)?.UpdateNodeAppearance();
            RefreshEditPanelUI(targetNodeId);
        }
    }

    public void Undo()
    {
        if (DataManager.Instance.TryGetScience(targetNodeId, out var sc))
        {
            sc.PathNode = oldPath;
            NodeManager.Instance.GetNode(targetNodeId)?.UpdateNodeAppearance();
            RefreshEditPanelUI(targetNodeId);
        }
    }

    private void RefreshEditPanelUI(int nodeId)
    {
        var inputMgr = GameObject.Find(Constants.GameObjectNames.MainManager)?.GetComponent<InputManager>();
        var currentPanel = inputMgr?.CurrentEditPanel;

        if (currentPanel != null)
        {
            var panelScript = currentPanel.GetComponent<PanelScienceEdit>();
            if (panelScript.sc.Id == nodeId)
            {
                panelScript.RefreshUI();
            }
        }
    }
}
