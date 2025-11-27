using System.Collections.Generic;

public class BatchChangeNodeScaleCommand : ICommand
{
    private List<ChangeNodeScaleCommand> commands = new List<ChangeNodeScaleCommand>();

    public string Description => $"批量修改 {commands.Count} 个节点尺寸";

    public void Add(ChangeNodeScaleCommand cmd)
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
        foreach (var cmd in commands) cmd.Undo();
    }
}
