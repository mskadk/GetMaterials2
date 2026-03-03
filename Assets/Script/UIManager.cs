using Assets.Script.My.Extention;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    #region 单例
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<UIManager>();
            }
            return _instance;
        }
    }
    #endregion
    // UIManager.cs
    public int CurrentNodeColorIndex => (int)ui.newNodeColorSlider.value;
    [Header("=== 框选 UI ===")]
    public RectTransform selectionBox; // 拖拽一个半透明的 Image (UI Panel)

    private UIReferences ui;
    private GameObject content;
    private int filterMin = 0;
    private int filterMax = 100000;

    private void Awake()
    {
        if (_instance == null) _instance = this;
        ui = GetComponent<UIReferences>() ?? UIReferences.Instance;
    }

    public void Initialize()
    {
        content = ui.scrollViewTechTreeItem.transform.Find(Constants.UIPath.ScrollViewContent).gameObject;

        SubscribeEvents();
        InitTTI();
        NewNodeColorControll(); // 初始化颜色滑条

        if (selectionBox) selectionBox.gameObject.SetActive(false);
        ToggleTTI();

    }

    private void SubscribeEvents()
    {
        EventCenter.Instance.OnTechTreeItemUpdate += UpdateTTIShow;
        EventCenter.Instance.OnCommandHistoryChanged += UpdateUndoRedoButtons;
        if (ui.dpGridType != null)
        {
            ui.dpGridType.onValueChanged.AddListener(OnGridTypeChanged);
        }
        if (ui.btnUndo != null)
        {
            ui.btnUndo.onClick.AddListener(() => CommandManager.Instance.Undo());
        }
        if (ui.btnRedo != null)
        {
            ui.btnRedo.onClick.AddListener(() => CommandManager.Instance.Redo());
        }
        // 初始化按钮状态
        UpdateUndoRedoButtons(CommandManager.Instance.CanUndo, CommandManager.Instance.CanRedo);
    }

    private void OnDestroy()
    {
        if (EventCenter.Instance != null)
        {
            EventCenter.Instance.OnTechTreeItemUpdate -= UpdateTTIShow;
            EventCenter.Instance.OnCommandHistoryChanged -= UpdateUndoRedoButtons;
        }
        if (ui.dpGridType != null)
        {
            ui.dpGridType.onValueChanged.RemoveListener(OnGridTypeChanged);
        }
    }

    private void UpdateUndoRedoButtons(bool canUndo, bool canRedo)
    {
        if (ui.btnUndo != null) ui.btnUndo.interactable = canUndo;
        if (ui.btnRedo != null) ui.btnRedo.interactable = canRedo;
    }

    #region TTI 逻辑 (从 MainManager 搬运)
    private void InitTTI()
    {
        foreach (var tti in DataManager.Instance.TechTreeItemDict)
        {
            GameObject o = Instantiate(ui.techTreeItemTextPrefab, content.transform);
            o.name = tti.Key.ToString();
            TechTreeItemText ttit = o.GetComponent<TechTreeItemText>();
            tti.Value.GO = o;
            ttit.t_id.text = tti.Key.ToString();
            ttit.t_name.text = tti.Value.Name;
            ttit.t_desc.text = tti.Value.Desc;
            ttit.t_times.text = "0";
        }


    }

    public void UpdateTTIShow(string oldStr, string newStr)
    {
        // ... 原封不动搬运 MainManager 中的 UpdateTTIShow 代码 ...
        // 注意：把 debug.Log 改为 EventCenter.Instance.TriggerLog...
        var oldList = MyExtensions.ToList(oldStr);
        var newList = MyExtensions.ToList(newStr);
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

    #region 过滤器逻辑 (从 MainManager 搬运)
    public void ToggleTTI()
    {
        bool isOn = ui.toggleTTI.isOn;
        ui.scrollViewTechTreeItem.gameObject.SetActive(isOn);
        ui.toggleTTIFilter.gameObject.SetActive(isOn);
        ui.ifFilterFrom.gameObject.SetActive(isOn);
        ui.textDao.gameObject.SetActive(isOn);
        ui.ifFilterTo.gameObject.SetActive(isOn);
        ui.btnFilterClear.gameObject.SetActive(isOn);
    }

    public void UpdateTTIFilterMin()
    {
        if (int.TryParse(ui.ifFilterFrom.text, out int result))
        {
            filterMin = result;
            UpdateFilter();
        }
    }

    public void UpdateTTIFilterMax()
    {
        if (int.TryParse(ui.ifFilterTo.text, out int result))
        {
            filterMax = result;
            UpdateFilter();
        }
    }

    private void UpdateFilter()
    {
        foreach (var ttit in DataManager.Instance.TechTreeItemDict)
        {
            var g = ttit.Value.GO;
            var gc = g.GetComponent<TechTreeItemText>();
            int id = int.Parse(gc.t_id.text);
            g.SetActive(id >= filterMin && id <= filterMax);
        }
    }

    public void ClearFilter()
    {
        ui.ifFilterFrom.text = "0";
        filterMin = 0;
        ui.ifFilterTo.text = "100000";
        filterMax = 100000;
        UpdateFilter();
    }

    public void ToggleTTIFilter()
    {
        foreach (var ttit in DataManager.Instance.TechTreeItemDict)
        {
            if (ttit.Value.GO.GetComponent<TechTreeItemText>().t_times.text == "1")
            {
                ttit.Value.GO.SetActive(ui.toggleTTIFilter.isOn);
            }
        }
    }
    #endregion

    #region 颜色控制 (从 MainManager 搬运)
    public void NewNodeColorControll()
    {
        int colorInt = (int)ui.newNodeColorSlider.value;
        // 这里只处理 UI 显示
        Color color = DataManager.Instance.GetColor(colorInt);

        ColorBlock v = ui.newNodeColorSlider.colors;
        v.normalColor = color;
        v.pressedColor = color;
        v.selectedColor = color;
        v.highlightedColor = color;
        ui.newNodeColorSlider.colors = v;
    }
    #endregion

    #region 主界面 : 通过按钮添加节点
    public void BtnNewNode()
    {
        GameObject newNode = Instantiate(ui.ghostNodePrefab);
        EventCenter.Instance.TriggerLogMessage("添加节点中……");
    }

    #endregion

    #region 框选相关的内容
    private void OnGridTypeChanged(int index)
    {
        GridType type = (GridType)index;
        GridManager.Instance.SetGridType(type);
    }
    // 更新选框视觉
    public void UpdateSelectionBox(Vector2 startScreenPos, Vector2 currentScreenPos)
    {
        if (!selectionBox) return;
        if (!selectionBox.gameObject.activeSelf) selectionBox.gameObject.SetActive(true);
        RectTransform parentRect = selectionBox.parent as RectTransform;
        Vector2 localStart, localCurrent;
        Camera uiCamera = GameObject.Find("CameraUI").GetComponent<Camera>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, startScreenPos, uiCamera, out localStart);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, currentScreenPos, uiCamera, out localCurrent);
        Vector2 center = (localStart + localCurrent) * 0.5f;
        Vector2 size = new Vector2(Mathf.Abs(localCurrent.x - localStart.x), Mathf.Abs(localCurrent.y - localStart.y));

        selectionBox.anchoredPosition = center;
        selectionBox.sizeDelta = size;
    }
    public void HideSelectionBox()
    {
        if (selectionBox) selectionBox.gameObject.SetActive(false);
    }
    #endregion

    public async void SaveScience()
    {
        GameObject btn = ui.btnExport.gameObject;
        Button btncomp = ui.btnExport;
        Text tx = btn.GetComponentInChildren<Text>();

        string originalText = tx.text;
        tx.text = "导出中...";
        btncomp.interactable = false;

        EventCenter.Instance.TriggerDataSaveStarted();

        Stopwatch sw = Stopwatch.StartNew();
        string fullname = null;

        await System.Threading.Tasks.Task.Run(() =>
        {
            fullname = DataManager.Instance.SaveData();
        });

        sw.Stop();

        EventCenter.Instance.TriggerLogMessage($"成功导出到{fullname}，耗时：{sw.ElapsedMilliseconds}毫秒");
        EventCenter.Instance.TriggerDataSaveCompleted(fullname);

        tx.text = originalText;
        btncomp.interactable = true;
    }

}
