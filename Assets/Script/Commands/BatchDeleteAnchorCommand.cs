using System.Collections.Generic;

public class BatchDeleteAnchorCommand : ICommand
{
    private List<DeleteAnchorCommand> commands = new List<DeleteAnchorCommand>();

    public string Description => $"批量删除 {commands.Count} 个锚点";

    public int Count => commands.Count;

    public void Add(DeleteAnchorCommand cmd)
    {
        commands.Add(cmd);
    }

    public void Execute()
    {
        // 倒序执行，因为删除锚点会影响后续锚点的索引
        for (int i = commands.Count - 1; i >= 0; i--)
        {
            commands[i].Execute();
        }
    }

    public void Undo()
    {
        // 正序恢复
        foreach (var cmd in commands)
        {
            cmd.Undo();
        }
    }
}
