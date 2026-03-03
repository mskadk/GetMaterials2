using Assets.Script.My.Extention;
using UnityEngine;

public class AddAnchorCommand : ICommand
{
    private string targetPreId;
    private int targetNodeId;
    private int insertIndex;
    private Vector2 newWorldPos;
    private string oldPath;
    private string newPath;

    public string Description => "添加锚点";

    public AddAnchorCommand(LineRenderer lr, int index, Vector3 worldPos)
    {
        if (!lr.name.Contains("->"))
        {
            Debug.LogError($"LineRenderer 名称格式错误: {lr.name}");
            return;
        }

        var parts = lr.name.Split(new string[] { "->" }, System.StringSplitOptions.None);
        targetPreId = parts[0];
        targetNodeId = int.Parse(parts[1]);
        insertIndex = index;

        Vector3 snappedPos = GridManager.Instance.SnapToGrid(worldPos);
        newWorldPos = new Vector2(
            (float)System.Math.Round(snappedPos.x, 1),
            (float)System.Math.Round(snappedPos.y, 1));

        if (DataManager.Instance.TryGetScience(targetNodeId, out var sc))
        {
            oldPath = sc.PathNode;

            // 如果当前路径是 -1 或空，需要先为该 preId 创建一个带方向的连接
            if (string.IsNullOrEmpty(oldPath) || oldPath == "-1")
            {
                var newConn = new PathConnection(
                    int.Parse(targetPreId),
                    AnchorDirection.Center,
                    AnchorDirection.Center,
                    new System.Collections.Generic.List<Vector2> { newWorldPos });
                newPath = MyExtensions.SerializePathConnections(
                    new System.Collections.Generic.List<PathConnection> { newConn });
            }
            else
            {
                newPath = oldPath.InsertPathNode(targetPreId, insertIndex, newWorldPos);
            }
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
