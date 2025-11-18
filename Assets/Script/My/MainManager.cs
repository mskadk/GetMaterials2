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
    // Dictionary
    public Dictionary<int, Science> ScienceDict;
    public Dictionary<int, TechTreeItem> TechTreeItemDict;
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

        // 初始化输入管理器
        inputManager = gameObject.AddComponent<InputManager>();
        inputManager.Initialize(this);

        initExcel();
        initUI();
        //initTilemap(); // 删除的功能
        initTTI();
        initNode();

        // 数据加载完成
        EventCenter.Instance.TriggerDataLoaded();
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    private void SubscribeEvents()
    {
        EventCenter.Instance.OnTechTreeItemUpdate += UpdateTTIShow;
    }
    private void OnDestroy()
    {
        // 取消订阅
        if (EventCenter.Instance != null)
        {
            EventCenter.Instance.OnTechTreeItemUpdate -= UpdateTTIShow;
        }
    }

    private void initExcel()
    {
        em = new();
        ScienceDict = em.LoadScience(config.excelPath + Constants.FileNames.ScienceExcel);
        TechTreeItemDict = em.LoadTechTreeitem(config.excelPath + Constants.FileNames.TechTreeItem);

        // 保持不变，但字典已改为属性，带有private set
    }

    private void initUI()
    {
        // 不再使用 GameObject.Find，直接从 UIReferences 获取，其他的初始化工作也由UIR代管了，棒！
        content = ui.scrollViewTechTreeItem.transform.Find(Constants.UIPath.ScrollViewContent).gameObject;
        //初始化时调用一次，调整滑条颜色
        NewNodeColorControll();
    }

    private void initTTI()
    {
        foreach (var tti in TechTreeItemDict)
        {
            GameObject o = Instantiate(ui.techTreeItemTextPrefab, content.transform);
            o.name = tti.Key.ToString();
            TechTreeItemText ttit = o.GetComponent<TechTreeItemText>();
            tti.Value.GO = o;
            ttit.t_id.text = tti.Key.ToString();
            ttit.t_name.text = tti.Value.Name;
            ttit.t_desc.text = tti.Value.Desc;
            ttit.t_times.text = 0.ToString();
        }
    }

    private void initNode()
    {
        foreach (var sc in ScienceDict.Values)
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

    // 滑条修改新节点的颜色
    public void NewNodeColorControll()
    {
        NewNodeColorInt = (int)ui.newNodeColorSlider.value;

        // 改用配置文件的颜色
        NewNodeColor = config.GetColor(NewNodeColorInt);

        ColorBlock v = new()
        {
            colorMultiplier = 1,
            normalColor = NewNodeColor,
            pressedColor = NewNodeColor,
            selectedColor = NewNodeColor,
            highlightedColor = NewNodeColor,
        };

        ui.newNodeColorSlider.colors = v;

    }

    #region 科技解锁项筛选
    public void ToggleTTI()
    {
        ui.scrollViewTechTreeItem.gameObject.SetActive(ui.toggleTTI.isOn);
        ui.toggleTTIFilter.gameObject.SetActive(ui.toggleTTI.isOn);
        ui.ifFilterFrom.gameObject.SetActive(ui.toggleTTI.isOn);
        ui.ifFilterTo.gameObject.SetActive(ui.toggleTTI.isOn);
        ui.btnFilterClear.gameObject.SetActive(ui.toggleTTI.isOn);
    }

    int min = 0;
    int max = 100000;
    public void UpdateTTIFilterMin()
    {
        min = int.Parse(ui.ifFilterFrom.text);
        updateFilter();
    }
    public void UpdateTTIFilterMax()
    {
        max = int.Parse(ui.ifFilterTo.text);
        updateFilter();
    }
    private void updateFilter()
    {
        foreach (var ttit in TechTreeItemDict)
        {
            var g = ttit.Value.GO;
            var gc = g.GetComponent<TechTreeItemText>();
            if (int.Parse(gc.t_id.text) >= min && int.Parse(gc.t_id.text) <= max)
            {
                g.SetActive(true);
            }
            else
            {
                g.SetActive(false);
            }
        }
    }
    public void ClearFilter()
    {
        ui.ifFilterFrom.text = "0";
        min = 0;
        ui.ifFilterTo.text = "100000";
        max = 100000;
        updateFilter();
    }



    public void ToggleTTIFilter()
    {
        foreach (var ttit in TechTreeItemDict)
        {
            if (ttit.Value.GO.GetComponent<TechTreeItemText>().t_times.text == "1")
            {
                ttit.Value.GO.SetActive(ui.toggleTTIFilter.isOn);
            }
        }
    }

    public void UpdateTTIShow(string oldStr = "", string newStr = "")
    {
        var oldList = oldStr.ToList();
        var newList = newStr.ToList();
        string newNotFound = null;

        foreach (var item in oldList)
        {
            Transform t = content.transform.Find(item.ToString());
            if (t)
            {
                var ttit = t.GetComponent<TechTreeItemText>();
                int times_i = int.Parse(ttit.t_times.text);
                times_i--;
                if (times_i > 1)
                {
                    ttit.t_times.color = Constants.Colors.RedDuplicate;
                    ttit.t_id.color = Constants.Colors.RedDuplicate;
                    ttit.t_name.color = Constants.Colors.RedDuplicate;
                    EventCenter.Instance.TriggerLogError($"{t.name}解锁项——重复引用！");
                }
                else if (times_i == 1)
                {
                    ttit.t_times.color = Constants.Colors.GreenUsed;
                    ttit.t_id.color = Constants.Colors.GreenUsed;
                    ttit.t_name.color = Constants.Colors.GreenUsed;
                }
                else if (times_i < 1)
                {
                    ttit.t_times.color = Constants.Colors.BlackNormal;
                    ttit.t_id.color = Constants.Colors.BlackNormal;
                    ttit.t_name.color = Constants.Colors.BlackNormal;
                }
                ttit.t_times.text = times_i.ToString();
            }
        }

        foreach (var item in newList)
        {
            Transform t = content.transform.Find(item.ToString());
            if (t)
            {
                var ttit = t.GetComponent<TechTreeItemText>();
                int times_i = int.Parse(ttit.t_times.text);
                times_i++;

                if (times_i > 1)
                {
                    ttit.t_times.color = Constants.Colors.RedDuplicate;
                    ttit.t_id.color = Constants.Colors.RedDuplicate;
                    ttit.t_name.color = Constants.Colors.RedDuplicate;
                    EventCenter.Instance.TriggerLogError($"{t.name}解锁项被重复解锁（TechTreeItem：id 重复引用）");
                }
                else if (times_i == 1)
                {
                    ttit.t_times.color = Constants.Colors.GreenUsed;
                    ttit.t_id.color = Constants.Colors.GreenUsed;
                    ttit.t_name.color = Constants.Colors.GreenUsed;
                }
                ttit.t_times.text = times_i.ToString();
            }
            else
            {
                newNotFound += newNotFound + " " + item.ToString();
            }
        }
        if (newNotFound is not null)
        {
            EventCenter.Instance.TriggerLogError($"{newNotFound} 是不存在的科技解锁项。");
        }


    }

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
            // 把 WorkSpace_Saveto 改成 config.savePath
            fullname = em.SaveScience(config.savePath,
                                       config.excelPath + Constants.FileNames.ScienceExcel,
                                       ScienceDict);
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
        foreach (var sc in ScienceDict.Values)
        {
            outs += sc.ParseString();
            outs += "\n";
        }
        GUIUtility.systemCopyBuffer = outs;
    }
    #endregion

    #region 外部调用,利用幽灵节点的左键点击创建新的节点
    public void NewNode(Vector3Int pos)
    {
        int id = Constants.SpecialIds.NewNodeStartId;
        while (ScienceDict.ContainsKey(id)) { id--; }

        GameObject o = Instantiate(ui.nodePrefab,
            ui.grid.CellToWorld(new(pos.x, pos.y, 0)),
            new Quaternion(), ui.tilemap.transform);
        o.name = id.ToString();
        Science sc = new(id, 1, 0, .75f, 4, "新科技", "描述", "备注", "-1", "-1",
            pos.y, pos.x, "-1", "-1", "-1", .01f, NewNodeColorInt, "-1");
        o.GetComponent<Node>().sc = sc;
        ScienceDict.Add(id, sc);
        // 触发节点创建事件
        EventCenter.Instance.TriggerNodeCreated(o.GetComponent<Node>());
    }

    #endregion

}
