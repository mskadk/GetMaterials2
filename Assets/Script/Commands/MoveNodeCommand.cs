using UnityEngine;

/// <summary>
/// 移动节点命令（使用世界坐标）
/// </summary>
public class MoveNodeCommand : ICommand
{
    private Node node;
    private Vector2 oldWorldPosition;
    private Vector2 newWorldPosition;

    public string Description => $"移动节点 {node.sc.Id}:{node.sc.Name} 从 ({oldWorldPosition.x:F3},{oldWorldPosition.y:F3}) 到 ({newWorldPosition.x:F3},{newWorldPosition.y:F3})";

    /// <summary>
    /// 使用世界坐标创建移动命令
    /// </summary>
    public MoveNodeCommand(Node node, Vector2 oldWorldPos, Vector2 newWorldPos)
    {
        this.node = node;
        this.oldWorldPosition = oldWorldPos;
        this.newWorldPosition = newWorldPos;
    }

    /// <summary>
    /// 兼容旧的网格坐标调用
    /// </summary>
    public MoveNodeCommand(Node node, Vector3Int oldGridPos, Vector3Int newGridPos, Grid grid)
    {
        this.node = node;

        if (grid != null)
        {
            Vector3 oldWorld = grid.CellToWorld(new Vector3Int(oldGridPos.x, oldGridPos.y, 0));
            Vector3 newWorld = grid.CellToWorld(new Vector3Int(newGridPos.x, newGridPos.y, 0));
            this.oldWorldPosition = new Vector2(oldWorld.x, oldWorld.y);
            this.newWorldPosition = new Vector2(newWorld.x, newWorld.y);
        }
        else
        {
            this.oldWorldPosition = new Vector2(oldGridPos.x, oldGridPos.y);
            this.newWorldPosition = new Vector2(newGridPos.x, newGridPos.y);
        }
    }

    public void Execute()
    {
        MoveNodeTo(newWorldPosition);
    }

    public void Undo()
    {
        MoveNodeTo(oldWorldPosition);
    }

    private void MoveNodeTo(Vector2 targetWorldPos)
    {
        // 更新节点位置
        Vector3 worldPos = new Vector3(targetWorldPos.x, targetWorldPos.y, 0);
        node.transform.position = worldPos;
        node.UpdateWorldPos(targetWorldPos);

        // 更新所有子节点的连线
        foreach (var afterId in node.sc.After_technology)
        {
            GameObject childNode = GameObject.Find(Constants.GameObjectNames.Tilemap)
                ?.transform.Find(afterId.ToString())?.gameObject;
            childNode?.GetComponent<Node>().UpdateNodeAppearance();
        }

        // 如果有编辑面板打开，更新其中的位置信息
        GameObject editPanel = GameObject.Find($"{node.sc.Id}(Edit)");
        if (editPanel)
        {
            editPanel.GetComponent<PanelScienceEdit>()
                .UpdatePositionByDrag(targetWorldPos);
        }
    }
}
