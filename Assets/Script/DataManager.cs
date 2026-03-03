using System.Collections.Generic;
using Assets.Script.My.Excel;
using UnityEngine;

/// <summary>
/// 数据管理器 - 负责所有数据的加载、保存和访问
/// </summary>
public class DataManager : MonoBehaviour
{
    #region 单例
    private static DataManager _instance;
    public static DataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<DataManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DataManager");
                    _instance = go.AddComponent<DataManager>();
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

    #region 数据存储
    // Excel管理器
    private ExcelManager excelManager;

    // 数据字典
    public Dictionary<int, Science> ScienceDict { get; private set; }
    public Dictionary<int, TechTreeItem> TechTreeItemDict { get; private set; }

    // 配置文件引用
    private EditorConfig config;
    #endregion

    #region 初始化
    public void Initialize(EditorConfig editorConfig)
    {
        config = editorConfig;
        excelManager = new ExcelManager();

        LoadAllData();
    }

    private void LoadAllData()
    {
        // 加载科技数据
        string sciencePath = config.excelPath + Constants.FileNames.ScienceExcel;
        ScienceDict = excelManager.LoadScience(sciencePath);

        // 加载科技树项数据
        string itemPath = config.excelPath + Constants.FileNames.TechTreeItem;
        TechTreeItemDict = excelManager.LoadTechTreeitem(itemPath);

        EventCenter.Instance.TriggerLogMessage("数据加载完成");
        EventCenter.Instance.TriggerDataLoaded();
    }
    #endregion

    #region 数据操作
    public void AddScience(Science science)
    {
        if (!ScienceDict.ContainsKey(science.Id))
        {
            ScienceDict.Add(science.Id, science);
        }
    }

    public void RemoveScience(int id)
    {
        if (ScienceDict.ContainsKey(id))
        {
            ScienceDict.Remove(id);
        }
    }

    public bool TryGetScience(int id, out Science science)
    {
        return ScienceDict.TryGetValue(id, out science);
    }

    public bool TryGetTechTreeItem(int id, out TechTreeItem item)
    {
        return TechTreeItemDict.TryGetValue(id, out item);
    }

    public Color GetColor(int i)
    {
        return config.GetColor(i);
    }

    /// <summary>
    /// 更新科技ID（同时更新字典键值）
    /// </summary>
    public void UpdateScienceId(int oldId, int newId)
    {
        if (!ScienceDict.ContainsKey(oldId))
        {
            EventCenter.Instance.TriggerLogError($"更新ID失败：找不到旧ID {oldId}");
            return;
        }
        if (ScienceDict.ContainsKey(newId))
        {
            EventCenter.Instance.TriggerLogError($"更新ID失败：新ID {newId} 已存在");
            return;
        }
        // 获取对象
        Science science = ScienceDict[oldId];

        // 从旧键移除
        ScienceDict.Remove(oldId);

        // 更新对象内部ID
        science.Id = newId;

        // 添加到新键
        ScienceDict.Add(newId, science);

        EventCenter.Instance.TriggerLogMessage($"ID已更新：{oldId} -> {newId}");
    }
    #endregion

    #region 保存
    public string SaveData()
    {
        string savePath = config.savePath;
        string sourcePath = config.excelPath + Constants.FileNames.ScienceExcel;

        return excelManager.SaveScience(savePath, sourcePath, ScienceDict);
    }
    #endregion

    /// <summary>
    /// 将所有科技数据复制到剪贴板
    /// </summary>
    public void ScienceToClipBoard()
    {
        ScienceToClipBoard(ScienceDict.Values);
    }
    /// <summary>
    /// 将指定的科技数据列表复制到剪贴板
    /// </summary>
    public void ScienceToClipBoard(IEnumerable<Science> sciences)
    {
        string outs = "";
        foreach (var sc in sciences)
        {
            outs += sc.ParseString();
            outs += "\n";
        }

        if (!string.IsNullOrEmpty(outs))
        {
            GUIUtility.systemCopyBuffer = outs;
            EventCenter.Instance.TriggerLogMessage($"已复制 {System.Linq.Enumerable.Count(sciences)} 条科技数据到剪贴板");
        }
        else
        {
            EventCenter.Instance.TriggerLogWarning("没有可复制的数据");
        }
    }

}
