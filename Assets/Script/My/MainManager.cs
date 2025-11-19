using Assets.Script.My.Excel;
using Assets.Script.My.Extention;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainManager : MonoBehaviour
{
    [Header("=== 配置文件 ===")]
    public EditorConfig config;
    
    private void Start()
    {
        // 数据层初始化
        DataManager.Instance.Initialize(config);

        // 逻辑层初始化
        var inputManager = gameObject.AddComponent<InputManager>();
        inputManager.Initialize();

        // 视图层初始化
        UIManager.Instance.Initialize();

        // 场景对象初始化
        NodeManager.Instance.InitializeNodes();

        // 数据加载完成
        EventCenter.Instance.TriggerDataLoaded();
    }

    #region 保存与导出 先保留，咱不开工实装哈
    public void ReloadSheets()
    {
        #if UNITY_EDITOR
            EditorUtility.DisplayDialog("重载", "进行了重载(但是还没有实现所以现在没有变化（)", "好");
        #endif
    }
    #endregion


}
