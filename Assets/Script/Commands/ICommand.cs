/// <summary>
/// 命令接口 - 支持执行和撤销
/// </summary>
public interface ICommand
{
    /// <summary>
    /// 执行命令
    /// </summary>
    void Execute();

    /// <summary>
    /// 撤销命令
    /// </summary>
    void Undo();

    /// <summary>
    /// 命令描述（用于调试和日志）
    /// </summary>
    string Description { get; }
}
