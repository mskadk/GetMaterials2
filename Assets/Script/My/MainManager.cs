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
    
    enum Mode
    {
        none,
        science,
        productionList,
    }

    [Header("=== 配置文件 ===")]
    public EditorConfig config;
    // UI 引用（自动获取，无需手动赋值）
    private UIReferences ui;

    // 新增：输入管理器
    private InputManager inputManager;

    // 保留这些，因为需要在运行时创建
    private GameObject content;

    // Excel
    ExcelManager em;
    
    // 其他运行时变量
    Color NewNodeColor;
    int NewNodeColorInt;

    #region 初始化
    private void Init()
    {
        // 首先获取 UI 引用
        ui = UIReferences.Instance;

        // 验证引用
        if (!ui.ValidateReferences())
        {
            Debug.LogError("UI 引用验证失败！请在 Inspector 中检查 UIReferences 组件");
            return;
        }

        // 订阅事件
        SubscribeEvents();

        // 初始化数据管理器
        DataManager.Instance.Initialize(config);

        // 初始化输入管理器
        inputManager = gameObject.AddComponent<InputManager>();
        inputManager.Initialize();

        // initExcel() 删除的方法，迁移到DataManager中
        initUI();
        //initTilemap(); // 删除的功能
        //initTTI(); // 迁移到了UIManager
        // 初始化 UI 管理器
        UIManager.Instance.Initialize();
        initNode();

        // 数据加载完成
        EventCenter.Instance.TriggerDataLoaded();
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    private void SubscribeEvents()
    {
        EventCenter.Instance.OnTechTreeItemUpdate += UIManager.Instance.UpdateTTIShow;
    }
    private void OnDestroy()
    {
        // 取消订阅
        if (EventCenter.Instance != null)
        {
            EventCenter.Instance.OnTechTreeItemUpdate -= UIManager.Instance.UpdateTTIShow;
        }
    }

    private void initUI()
    {
        // 不再使用 GameObject.Find，直接从 UIReferences 获取，其他的初始化工作也由UIR代管了，棒！
        content = ui.scrollViewTechTreeItem.transform.Find(Constants.UIPath.ScrollViewContent).gameObject;
        //初始化时调用一次，调整滑条颜色
        UIManager.Instance.NewNodeColorControll();
    }

    private void initNode()
    {
        foreach (var sc in DataManager.Instance.ScienceDict.Values)
        {

            GameObject o = Instantiate(ui.nodePrefab,
                ui.grid.CellToWorld(new(sc.HexGridY, sc.HexGridX, 0)),
                new Quaternion(),
                ui.tilemap.transform);
            o.name = sc.Id.ToString();
            o.GetComponent<Node>().sc = sc;

            // 使用事件更新科技树项
            EventCenter.Instance.TriggerTechTreeItemUpdate("", sc.Building_unlock);
            EventCenter.Instance.TriggerTechTreeItemUpdate("", sc.NonBuilding_unlock);

        }
    }



    #endregion

    private void Start()
    {
        Init();
    }

    private void Update()
    {
        // 输入处理已移到 InputManager，这里不需要任何代码
    }

    #region 主界面 & 模式选择
    public void SwitchPage()
    {
        switch (ui.dpMainPage.value)
        {
            case (int)Mode.science: OnDrawScience(); break;
            case (int)Mode.productionList: OffDrawScience(); break;
            default:
                break;
        }
    }

    public void ToggleMoveCam()
    {
        ui.camSence.GetComponent<CameraEventControll>().相机控制 = ui.toggleMoveCam.isOn;
    }

    private void OnDrawScience()
    {
        ui.camSence.GetComponent<GridDrawer>().渲染至游戏 = true;
        ui.camSence.GetComponent<CameraEventControll>().相机控制 = true;
        ui.tilemap.SetActive(true);
    }

    private void OffDrawScience()
    {
        ui.camSence.GetComponent<GridDrawer>().渲染至游戏 = false;
        ui.camSence.GetComponent<CameraEventControll>().相机控制 = false;
        ui.tilemap.SetActive(false);
    }

    public void BtnNewNode()
    {
        GameObject newNode = Instantiate(ui.ghostNodePrefab);
        EventCenter.Instance.TriggerLogMessage("添加节点中……");
    }

    #region 科技解锁项筛选

    #endregion

    #endregion

    #region 保存与导出
    public void ReloadSheets()
    {
        #if UNITY_EDITOR
            EditorUtility.DisplayDialog("重载", "进行了重载(但是还没有实现所以现在没有变化（)", "好");
        #endif
    }
    public async void SaveScience()
    {
        //按钮UI暂时禁用
        //GameObject btn = GameObject.Find(Constants.GameObjectNames.ButtonExport);
        // 改为：
        GameObject btn = ui.btnExport.gameObject;
        Button btncomp = btn.GetComponent<Button>();
        Text tx = btn.GetComponentInChildren<Text>();
        tx.text = "导出中……";
        btncomp.interactable = false;

        // 触发保存开始事件
        EventCenter.Instance.TriggerDataSaveStarted();

        //使用Task和计时进行保存
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        string fullname = null;
        await Task.Run(() =>
        {
            // 使用 DataManager 保存
            fullname = DataManager.Instance.SaveData();
        });
        sw.Stop();
        EventCenter.Instance.TriggerLogMessage($"成功导出：{fullname}，用时：{sw.ElapsedMilliseconds}毫秒");
        EventCenter.Instance.TriggerDataSaveCompleted(fullname);
        //解禁UI
        tx.text = "导出到桌面";
        btncomp.interactable = true;
    }

    public void ScienceToClipBoard()
    {
        string outs = "";
        foreach (var sc in DataManager.Instance.ScienceDict.Values)
        {
            outs += sc.ParseString();
            outs += "\n";
        }
        GUIUtility.systemCopyBuffer = outs;
    }
    #endregion

    #region 外部调用,利用幽灵节点的左键点击创建新的节点
    // 修改 NewNode() 方法使用命令
    public void NewNode(Vector3Int pos)
    {
        int colorInt = UIManager.Instance.CurrentNodeColorIndex;
        var createCmd = new CreateNodeCommand(pos, colorInt, ui);
        CommandManager.Instance.ExecuteCommand(createCmd);
    }

    #endregion

}
