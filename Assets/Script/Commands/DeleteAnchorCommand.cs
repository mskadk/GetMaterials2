using Assets.Script.My.Extention;
using UnityEngine;

public class DeleteAnchorCommand : ICommand
{
    private string targetPreId;
    private int targetNodeId;
    private int anchorIndex; // 这个锚点是第几个（名字就是索引）
    private string oldPath;
    private string newPath;

    public string Description => "删除锚点";

    public DeleteAnchorCommand(GameObject anchorObj)
    {
        // 锚点的名字就是索引 "1", "2" 等
        anchorIndex = int.Parse(anchorObj.name);

        // 锚点的父物体是 LineRenderer
        var lr = anchorObj.transform.parent.gameObject;
        var parts = lr.name.Split(new string[] { "->" }, System.StringSplitOptions.None);
        targetPreId = parts[0];
        targetNodeId = int.Parse(parts[1]);

        if (DataManager.Instance.TryGetScience(targetNodeId, out var sc))
        {
            oldPath = sc.PathNode;
            newPath = oldPath.RemovePathNode(targetPreId, anchorIndex);
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
