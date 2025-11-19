using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 命令管理器 - 管理撤销/重做栈
/// </summary>
public class CommandManager : MonoBehaviour
{
	#region 单例
	private static CommandManager _instance;
	public static CommandManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = FindFirstObjectByType<CommandManager>();
				if (_instance == null)
				{
					GameObject go = new GameObject("CommandManager");
					_instance = go.AddComponent<CommandManager>();
					DontDestroyOnLoad(go);
				}
			}
			return _instance;
		}
	}

	private void Awake()
	{
		if (_instance == null)
		{
			_instance = this;
			DontDestroyOnLoad(gameObject);
		}
		else if (_instance != this)
		{
			Destroy(gameObject);
		}
	}
	#endregion

	#region 命令栈
	private Stack<ICommand> undoStack = new Stack<ICommand>();
	private Stack<ICommand> redoStack = new Stack<ICommand>();

	[Header("调试信息")]
	[SerializeField] private int undoCount = 0;
	[SerializeField] private int redoCount = 0;
	[SerializeField] private int maxStackSize = 50;
	#endregion

	#region 执行命令
	/// <summary>
	/// 执行命令并加入撤销栈
	/// </summary>
	public void ExecuteCommand(ICommand command)
	{
		if (command == null)
		{
			Debug.LogWarning("尝试执行空命令");
			return;
		}

		// 执行命令
		command.Execute();

		// 压入撤销栈
		undoStack.Push(command);

		// 清空重做栈（执行新命令后，之前的重做历史失效）
		redoStack.Clear();

		// 限制栈大小
		if (undoStack.Count > maxStackSize)
		{
			// 移除最早的命令
			var tempStack = new Stack<ICommand>();
			for (int i = 0; i < maxStackSize; i++)
			{
				tempStack.Push(undoStack.Pop());
			}
			undoStack.Clear();
			while (tempStack.Count > 0)
			{
				undoStack.Push(tempStack.Pop());
			}
		}

		UpdateDebugInfo();

		EventCenter.Instance.TriggerLogMessage($"执行: {command.Description}");
	}
	#endregion

	#region 撤销/重做
	/// <summary>
	/// 撤销上一个命令
	/// </summary>
	public void Undo()
	{
		if (undoStack.Count > 0)
		{
			ICommand command = undoStack.Pop();
			command.Undo();
			redoStack.Push(command);

			UpdateDebugInfo();
			EventCenter.Instance.TriggerLogMessage($"撤销: {command.Description}");
		}
		else
		{
			EventCenter.Instance.TriggerLogWarning("没有可撤销的操作");
		}
	}

	/// <summary>
	/// 重做上一个撤销的命令
	/// </summary>
	public void Redo()
	{
		if (redoStack.Count > 0)
		{
			ICommand command = redoStack.Pop();
			command.Execute();
			undoStack.Push(command);

			UpdateDebugInfo();
			EventCenter.Instance.TriggerLogMessage($"重做: {command.Description}");
		}
		else
		{
			EventCenter.Instance.TriggerLogWarning("没有可重做的操作");
		}
	}
	#endregion

	#region 查询状态
	/// <summary>
	/// 是否可以撤销
	/// </summary>
	public bool CanUndo => undoStack.Count > 0;

	/// <summary>
	/// 是否可以重做
	/// </summary>
	public bool CanRedo => redoStack.Count > 0;

	/// <summary>
	/// 获取撤销栈数量
	/// </summary>
	public int UndoCount => undoStack.Count;

	/// <summary>
	/// 获取重做栈数量
	/// </summary>
	public int RedoCount => redoStack.Count;
	#endregion

	#region 清空
	/// <summary>
	/// 清空所有命令历史
	/// </summary>
	public void Clear()
	{
		undoStack.Clear();
		redoStack.Clear();
		UpdateDebugInfo();
		EventCenter.Instance.TriggerLogMessage("已清空命令历史");
	}
	#endregion

	#region 调试
	private void UpdateDebugInfo()
	{
		undoCount = undoStack.Count;
		redoCount = redoStack.Count;
	}

	/// <summary>
	/// 打印命令历史
	/// </summary>
	public void PrintHistory()
	{
		Debug.Log("=== 撤销栈 ===");
		foreach (var cmd in undoStack)
		{
			Debug.Log($"  - {cmd.Description}");
		}

		Debug.Log("=== 重做栈 ===");
		foreach (var cmd in redoStack)
		{
			Debug.Log($"  - {cmd.Description}");
		}
	}
	#endregion
}
