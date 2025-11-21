using System;
using UnityEngine;

/// <summary>
/// 事件中心 - 用于模块间解耦通信
/// </summary>
public class EventCenter : MonoBehaviour
{
    #region 单例模式
    private static EventCenter _instance;

    //private static bool _applicationIsQuitting = false;
    public static EventCenter Instance
    {
        get
        {
            //if (_applicationIsQuitting)
            //{
            //    return null;
            //}
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<EventCenter>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("EventCenter");
                    _instance = go.AddComponent<EventCenter>();
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

    #region 节点相关事件

    /// <summary>
    /// 节点被选中事件
    /// </summary>
    public event Action<Node> OnNodeSelected;

    /// <summary>
    /// 节点取消选中事件
    /// </summary>
    public event Action<Node> OnNodeDeselected;

    /// <summary>
    /// 节点位置改变事件
    /// </summary>
    public event Action<Node, Vector3Int> OnNodeMoved;

    /// <summary>
    /// 节点被删除事件
    /// </summary>
    public event Action<int> OnNodeDeleted;

    /// <summary>
    /// 新节点创建事件
    /// </summary>
    public event Action<Node> OnNodeCreated;

    /// <summary>
    /// 节点数据修改事件
    /// </summary>
    public event Action<Science> OnNodeDataChanged;

    #endregion

    #region 科技树项事件

    /// <summary>
    /// 科技树项显示状态更新
    /// </summary>
    public event Action<string, string> OnTechTreeItemUpdate;

    #endregion

    #region UI事件

    /// <summary>
    /// 日志消息
    /// </summary>
    public event Action<string> OnLogMessage;

    /// <summary>
    /// 警告消息
    /// </summary>
    public event Action<string> OnLogWarning;

    /// <summary>
    /// 错误消息
    /// </summary>
    public event Action<string> OnLogError;

    #endregion

    #region 数据事件

    /// <summary>
    /// 数据加载完成
    /// </summary>
    public event Action OnDataLoaded;

    /// <summary>
    /// 数据保存开始
    /// </summary>
    public event Action OnDataSaveStarted;

    /// <summary>
    /// 数据保存完成
    /// </summary>
    public event Action<string> OnDataSaveCompleted;

    #endregion

    #region 触发事件的方法

    public void TriggerNodeSelected(Node node)
    {
        OnNodeSelected?.Invoke(node);
    }

    public void TriggerNodeDeselected(Node node)
    {
        OnNodeDeselected?.Invoke(node);
    }

    public void TriggerNodeMoved(Node node, Vector3Int newPos)
    {
        OnNodeMoved?.Invoke(node, newPos);
    }

    public void TriggerNodeDeleted(int nodeId)
    {
        OnNodeDeleted?.Invoke(nodeId);
    }

    public void TriggerNodeCreated(Node node)
    {
        OnNodeCreated?.Invoke(node);
    }

    public void TriggerNodeDataChanged(Science science)
    {
        OnNodeDataChanged?.Invoke(science);
    }

    public void TriggerTechTreeItemUpdate(string oldStr, string newStr)
    {
        OnTechTreeItemUpdate?.Invoke(oldStr, newStr);
    }

    public void TriggerLogMessage(string message)
    {
        OnLogMessage?.Invoke(message);
    }

    public void TriggerLogWarning(string message)
    {
        OnLogWarning?.Invoke(message);
    }

    public void TriggerLogError(string message)
    {
        OnLogError?.Invoke(message);
    }

    public void TriggerDataLoaded()
    {
        OnDataLoaded?.Invoke();
    }

    public void TriggerDataSaveStarted()
    {
        OnDataSaveStarted?.Invoke();
    }

    public void TriggerDataSaveCompleted(string filepath)
    {
        OnDataSaveCompleted?.Invoke(filepath);
    }

    #endregion

    #region 清理

    private void OnDestroy()
    {
        // 清空所有事件订阅
        OnNodeSelected = null;
        OnNodeDeselected = null;
        OnNodeMoved = null;
        OnNodeDeleted = null;
        OnNodeCreated = null;
        OnNodeDataChanged = null;
        OnTechTreeItemUpdate = null;
        OnLogMessage = null;
        OnLogWarning = null;
        OnLogError = null;
        OnDataLoaded = null;
        OnDataSaveStarted = null;
        OnDataSaveCompleted = null;
        //_applicationIsQuitting = true;
    }

    #endregion
}
