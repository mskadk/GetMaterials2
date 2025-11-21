using Assets.Script.My.Extention;
using UnityEngine;

public class AddAnchorCommand : ICommand
{
    private string targetPreId;
    private int targetNodeId;
    private int insertIndex;
    private Vector2Int newGridPos;
    private string oldPath;
    private string newPath;

    public string Description => "添加锚点";

    public AddAnchorCommand(LineRenderer lr, int index, Vector3 worldPos)
    {
        // 增加容错：确保名字格式正确
        if (!lr.name.Contains("->"))
        {
            Debug.LogError($"LineRenderer 名字格式错误: {lr.name}");
            return;
        }
        // 解析名字 "PreID->TargetID"
        var parts = lr.name.Split(new string[] { "->" }, System.StringSplitOptions.None);
        targetPreId = parts[0];
        targetNodeId = int.Parse(parts[1]);
        insertIndex = index;

        // 计算格子坐标
        var grid = UIReferences.Instance.grid;
        Vector3Int cell = grid.WorldToCell(worldPos);
        newGridPos = new Vector2Int(cell.x, cell.y); // 注意 Y_X 格式

        // 获取旧数据
        if (DataManager.Instance.TryGetScience(targetNodeId, out var sc))
        {
            oldPath = sc.PathNode;
            // 计算新数据
            newPath = oldPath.InsertPathNode(targetPreId, insertIndex, newGridPos);
        }
    }

    public void Execute()
    {
        if (DataManager.Instance.TryGetScience(targetNodeId, out var sc))
        {
            sc.PathNode = newPath;

            // 1. 刷新场景物体（Node.cs 修改后，这里会自动刷新锚点）
            NodeManager.Instance.GetNode(targetNodeId)?.UpdateNodeAppearance();

            // 2. 刷新编辑面板 UI
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

    // 辅助方法：查找并刷新面板
    private void RefreshEditPanelUI(int nodeId)
    {
        // 通过 InputManager 获取当前面板
        var inputMgr = GameObject.Find(Constants.GameObjectNames.MainManager)?.GetComponent<InputManager>();
        var currentPanel = inputMgr?.CurrentEditPanel;

        if (currentPanel != null)
        {
            var panelScript = currentPanel.GetComponent<PanelScienceEdit>();
            // 只有当面板正在编辑当前节点时才刷新
            if (panelScript.sc.Id == nodeId)
            {
                panelScript.RefreshUI();
            }
        }
    }
}
