using UnityEngine;

/// <summary>
/// 创建节点命令
/// </summary>
public class CreateNodeCommand : ICommand
{
    private UIReferences ui;
    private Science newScience;
    private GameObject createdNode;
    private Vector2 worldPosition;
    private int nodeColorInt;

    public string Description => $"创建节点 {newScience.Id}:{newScience.Name} 在 ({worldPosition.x:F3},{worldPosition.y:F3})";

    /// <summary>
    /// 使用世界坐标创建节点
    /// </summary>
    public CreateNodeCommand(Vector2 worldPos, int colorInt, UIReferences uiRefs)
    {
        this.ui = uiRefs;
        this.worldPosition = worldPos;
        this.nodeColorInt = colorInt;

        // 生成新ID
        string id;
        int index = 1;
        do
        {
            id = $"{Constants.SpecialIds.NewNodePrefix}{index:000}";
            index++;
        } while (DataManager.Instance.ScienceDict.ContainsKey(id));

        // 创建Science对象（使用世界坐标）
        newScience = new Science(
            id, 1, 0, 1, 4,
            "新科技", "描述", "备注",
            "-1", "-1",
            (float)System.Math.Round(worldPos.x, 3),  // 世界坐标X
            (float)System.Math.Round(worldPos.y, 3),  // 世界坐标Y
            "-1", "-1", "-1",
            0.01f, colorInt, "-1"
        );
    }

    /// <summary>
    /// 兼容旧的网格坐标调用（会转换为世界坐标）
    /// </summary>
    public CreateNodeCommand(Vector3Int gridPos, int colorInt, UIReferences uiRefs)
        : this(GridToWorld(gridPos, uiRefs), colorInt, uiRefs)
    {
    }

    private static Vector2 GridToWorld(Vector3Int gridPos, UIReferences ui)
    {
        if (ui?.grid != null)
        {
            Vector3 worldPos = ui.grid.CellToWorld(gridPos);
            return new Vector2(worldPos.x, worldPos.y);
        }
        // 如果没有grid，直接使用网格坐标作为世界坐标
        return new Vector2(gridPos.x, gridPos.y);
    }

    public void Execute()
    {
        DataManager.Instance.ScienceDict.Add(newScience.Id, newScience);

        if (createdNode == null)
        {
            createdNode = NodeManager.Instance.CreateNodeObject(newScience);
        }
        else
        {
            createdNode.SetActive(true);
        }

        EventCenter.Instance.TriggerNodeCreated(createdNode.GetComponent<Node>());
    }

    public void Undo()
    {
        DataManager.Instance.ScienceDict.Remove(newScience.Id);

        if (createdNode != null)
        {
            createdNode.SetActive(false);
        }
    }
}
