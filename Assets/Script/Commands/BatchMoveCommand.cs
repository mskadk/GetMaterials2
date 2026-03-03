using System.Collections.Generic;

public class BatchMoveCommand : ICommand
{
    private List<MoveNodeCommand> commands = new List<MoveNodeCommand>();

    public string Description => $"批量移动 {commands.Count} 个节点";

    public void Add(MoveNodeCommand cmd)
    {
        commands.Add(cmd);
    }

    public bool HasCommands => commands.Count > 0;

    public void Execute()
    {
        foreach (var cmd in commands) cmd.Execute();
    }

    public void Undo()
    {
        for (int i = commands.Count - 1; i >= 0; i--)
        {
            commands[i].Undo();
        }
    }
}
