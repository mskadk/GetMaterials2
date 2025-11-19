using UnityEngine;

/// <summary>
/// 移动节点命令
/// </summary>
public class MoveNodeCommand : ICommand
{
    private Node node;
    private Vector3Int oldPosition;
    private Vector3Int newPosition;
    private Grid grid;
    private MainManager mainManager;

    public string Description => $"移动节点 {node.sc.Id}:{node.sc.Name} 从 ({oldPosition.y},{oldPosition.x}) 到 ({newPosition.y},{newPosition.x})";

    public MoveNodeCommand(Node node, Vector3Int oldPos, Vector3Int newPos, Grid grid, MainManager manager)
    {
        this.node = node;
        this.oldPosition = oldPos;
        this.newPosition = newPos;
        this.grid = grid;
        this.mainManager = manager;
    }

    public void Execute()
    {
        MoveNodeTo(newPosition);
    }

    public void Undo()
    {
        MoveNodeTo(oldPosition);
    }

    private void MoveNodeTo(Vector3Int targetPos)
    {
        // 更新节点位置
        Vector3 worldPos = grid.CellToWorld(new Vector3Int(targetPos.x, targetPos.y, 0));
        node.transform.position = worldPos;
        node.UpdateGridPos(targetPos);

        // 更新所有子节点的线条
        foreach (var afterId in node.sc.After_technology)
        {
            GameObject childNode = GameObject.Find(Constants.GameObjectNames.Tilemap)
                ?.transform.Find(afterId.ToString())?.gameObject;
            childNode?.GetComponent<Node>().UpdateNodeAppearance();
        }

        // 如果有编辑面板打开，更新面板中的位置信息
        GameObject editPanel = GameObject.Find($"{node.sc.Id}(Edit)");
        if (editPanel)
        {
            editPanel.GetComponent<PanelScienceEdit>()
                .UpdatePositionByDrag(new(targetPos.y, targetPos.x));
        }
    }
}
