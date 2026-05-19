using System;
using UnityEngine;

/// <summary>
/// 事件中心 - 用于模块间解耦通信
/// </summary>
public class EventCenter : MonoBehaviour
{
    #region 单例模式
    private static EventCenter _instance;
    private static bool _applicationIsQuitting = false;

    public static EventCenter Instance
    {
        get
        {
            // 防止在应用退出时创建新实例
            if (_applicationIsQuitting)
            {
                Debug.LogWarning("[EventCenter] Instance requested after application quit. Returning null.");
                return null;
            }

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

    private void OnEnable()
    {
        _applicationIsQuitting = false;
    }
    #endregion

    #region 节点相关事件

    public event Action<Node> OnNodeSelected;
    public event Action<Node> OnNodeDeselected;
    public event Action<Node, Vector3Int> OnNodeMoved;
    public event Action<string> OnNodeDeleted;
    public event Action<Node> OnNodeCreated;
    public event Action<Science> OnNodeDataChanged;

    #endregion

    #region 科技树项事件

    public event Action<string, string> OnTechTreeItemUpdate;

    #endregion

    #region UI事件

    public event Action<string> OnLogMessage;
    public event Action<string> OnLogWarning;
    public event Action<string> OnLogError;

    /// <summary>
    /// 命令历史发生变化（执行/撤销/重做/清空）时触发，参数为 (canUndo, canRedo)
    /// </summary>
    public event Action<bool, bool> OnCommandHistoryChanged;

    #endregion

    #region 数据事件

    public event Action OnDataLoaded;
    public event Action OnDataSaveStarted;
    public event Action<string> OnDataSaveCompleted;

    #endregion

    #region 触发事件的方法

    public void TriggerNodeSelected(Node node) => OnNodeSelected?.Invoke(node);
    public void TriggerNodeDeselected(Node node) => OnNodeDeselected?.Invoke(node);
    public void TriggerNodeMoved(Node node, Vector3Int newPos) => OnNodeMoved?.Invoke(node, newPos);
    public void TriggerNodeDeleted(string nodeId) => OnNodeDeleted?.Invoke(nodeId);
    public void TriggerNodeCreated(Node node) => OnNodeCreated?.Invoke(node);
    public void TriggerNodeDataChanged(Science science) => OnNodeDataChanged?.Invoke(science);
    public void TriggerTechTreeItemUpdate(string oldStr, string newStr) => OnTechTreeItemUpdate?.Invoke(oldStr, newStr);
    public void TriggerLogMessage(string message) => OnLogMessage?.Invoke(message);
    public void TriggerLogWarning(string message) => OnLogWarning?.Invoke(message);
    public void TriggerLogError(string message) => OnLogError?.Invoke(message);
    public void TriggerCommandHistoryChanged(bool canUndo, bool canRedo) => OnCommandHistoryChanged?.Invoke(canUndo, canRedo);
    public void TriggerDataLoaded() => OnDataLoaded?.Invoke();
    public void TriggerDataSaveStarted() => OnDataSaveStarted?.Invoke();
    public void TriggerDataSaveCompleted(string filepath) => OnDataSaveCompleted?.Invoke(filepath);

    #endregion

    #region 清理

    private void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    private void OnDestroy()
    {
        _applicationIsQuitting = true;

        //// 清空所有事件订阅
        //OnNodeSelected = null;
        //OnNodeDeselected = null;
        //OnNodeMoved = null;
        //OnNodeDeleted = null;
        //OnNodeCreated = null;
        //OnNodeDataChanged = null;
        //OnTechTreeItemUpdate = null;
        //OnLogMessage = null;
        //OnLogWarning = null;
        //OnLogError = null;
        //OnDataLoaded = null;
        //OnDataSaveStarted = null;
        //OnDataSaveCompleted = null;

        if (_instance == this)
        {
            _instance = null;
        }
    }

    #endregion
}
