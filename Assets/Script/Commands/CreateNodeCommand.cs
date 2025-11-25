using UnityEngine;

/// <summary>
/// 创建节点命令
/// </summary>
public class CreateNodeCommand : ICommand
{
    private UIReferences ui;
    private Science newScience;
    private GameObject createdNode;
    private Vector3Int gridPosition;
    private int nodeColorInt;

    public string Description => $"创建节点 {newScience.Id}:{newScience.Name} 在 ({gridPosition.y},{gridPosition.x})";

    public CreateNodeCommand(Vector3Int pos, int colorInt, UIReferences uiRefs)
    {
        this.ui = uiRefs;
        this.gridPosition = pos;
        this.nodeColorInt = colorInt;

        // 生成新ID
        int id = Constants.SpecialIds.NewNodeStartId;
        while (DataManager.Instance.ScienceDict.ContainsKey(id)) { id--; }

        // 创建Science数据
        newScience = new Science(
            id, 1, 0, 0.9f, 0.12f,
            "新科技", "介绍", "备注",
            "-1", "-1",
            pos.y, pos.x,
            "-1", "-1", "-1",
            0.01f, colorInt, "-1"
        );
    }

    public void Execute()
    {
        // 添加到字典
        DataManager.Instance.ScienceDict.Add(newScience.Id, newScience);

        // 创建GameObject
        if (createdNode == null)
        {
            createdNode = NodeManager.Instance.CreateNodeObject(newScience);
        }
        else
        {
            createdNode.SetActive(true);
        }

        // 触发创建事件
        EventCenter.Instance.TriggerNodeCreated(createdNode.GetComponent<Node>());
    }

    public void Undo()
    {
        // 从字典移除
        DataManager.Instance.ScienceDict.Remove(newScience.Id);

        // 隐藏GameObject（不销毁，方便重做）
        if (createdNode != null)
        {
            createdNode.SetActive(false);
        }
    }
}
