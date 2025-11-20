using System.Collections.Generic;

public class BatchMoveCommand : ICommand
{
    private List<MoveNodeCommand> commands = new List<MoveNodeCommand>();

    public string Description => $"批量移动 {commands.Count} 个节点";

    public void Add(MoveNodeCommand cmd)
    {
        commands.Add(cmd);
    }

    public void Execute()
    {
        foreach (var cmd in commands) cmd.Execute();
    }

    public void Undo()
    {
        // 撤销时建议倒序执行，虽然移动操作顺序可能不敏感，但这是好习惯
        for (int i = commands.Count - 1; i >= 0; i--)
        {
            commands[i].Undo();
        }
    }
}
