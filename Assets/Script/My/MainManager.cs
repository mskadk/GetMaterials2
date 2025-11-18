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
    // 新增：UI 引用（自动获取，无需手动赋值）
    private UIReferences ui;

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
    // 用于方法：updateMouseEvent()的
    private GameObject panel;
    List<GameObject> listSelect = new();
    private Vector3 鼠标按下位置_屏幕;
    private GameObject rayHitMove;
    private GameObject rayHitNode;

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
        initExcel();
        initUI();
        initTTI();
        initNode();
    }

    private void initExcel()
    {
        em = new();
        ScienceDict = em.LoadScience(config.excelPath + Constants.FileNames.ScienceExcel);
        TechTreeItemDict = em.LoadTechTreeitem(config.excelPath + Constants.FileNames.TechTreeItem);


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
            UpdateTTIShow("", sc.Building_unlock);
            UpdateTTIShow("", sc.NonBuilding_unlock);

        }
    }



    #endregion

    private void Start()
    {
        Init();
    }

    #region 射线检测
    GameObject rayDetect()
    {
        GameObject ob = null;
        Vector3 vvv = ui.camSence.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(vvv, Vector3.forward, Mathf.Infinity);
        if (hit)
        {
            ob = hit.transform.gameObject;
        }
        return ob;

    }
    #endregion

    private void Update()
    {
        updateMouseEvent();
        updateKeyboardEvent();
        debugPrintDict();
    }

    #region Debug
    private void debugPrintDict()
    {
        // / 打印整个科技表字典
        if (Input.GetKeyDown(KeyCode.Slash))
        {
            foreach (var sc in ScienceDict)
            {
                Debug.Log($"KEY:{sc.Key},VALUE:{sc.Value}");
            }
        }
        // . 从科技字典找到鼠标指向的ui.nodePrefab的Science数据
        if (Input.GetKeyDown(KeyCode.Period))
        {
            var v = rayDetect();
            if (v != null && v.tag is Constants.Tags.Node)
            {
                if (ScienceDict.TryGetValue(v.GetComponent<Node>().sc.Id, out Science sc))
                {
                    Debug.Log($"来自字典：{sc}");
                }
                Debug.Log($"来自节点：{v.GetComponent<Node>().sc}");
                string at = null;
                sc.After_technology.ToList().ForEach(x => at += x.ToString() + "|");
                Debug.Log($"After:{at}");
            }
            else
            {
                Debug.Log("未选中/非节点");
            }
        }
    }

    #endregion

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
        ui.tipText.Log("添加节点中……");
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
                    ttit.t_times.color = Color.red;
                    ttit.t_id.color = Color.red;
                    ttit.t_name.color = Color.red;
                    ui.tipText.LogError($"{t.name}解锁项——重复引用！");
                }
                else if (times_i == 1)
                {
                    ttit.t_times.color = new Color(.2f, .6f, .2f);
                    ttit.t_id.color = new Color(.2f, .6f, .2f);
                    ttit.t_name.color = new Color(.2f, .6f, .2f);
                }
                else if (times_i < 1)
                {
                    ttit.t_times.color = Color.black;
                    ttit.t_id.color = Color.black;
                    ttit.t_name.color = Color.black;
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
                    ttit.t_times.color = Color.red;
                    ttit.t_id.color = Color.red;
                    ttit.t_name.color = Color.red;
                    ui.tipText.LogError($"{t.name}解锁项被重复解锁");
                }
                else if (times_i == 1)
                {
                    ttit.t_times.color = new Color(.2f, .6f, .2f);
                    ttit.t_id.color = new Color(.2f, .6f, .2f);
                    ttit.t_name.color = new Color(.2f, .6f, .2f);
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
            ui.tipText.LogError($"{newNotFound} 是不存在的科技解锁项。");
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
        ui.tipText.Log($"成功导出：{fullname}，用时：{sw.ElapsedMilliseconds}毫秒");
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

    #region 鼠标事件 && 键盘事件
    private void updateKeyboardEvent()
    {
        if (!EventSystem.current.currentSelectedGameObject && panel)
        {
            //Esc 关闭编辑界面，功能与鼠标右键点空一样
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                panel.GetComponent<PanelScienceEdit>().DestoryPanel();
            }
            //Delete 删除选中的节点，需要右键选中先
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                var sc = panel.GetComponent<PanelScienceEdit>().sc;
                var id = sc.Id;
                var node = ui.tilemap.transform.Find(id.ToString());
                //清除科技解锁项的标记
                UpdateTTIShow(sc.Building_unlock, "");
                UpdateTTIShow(sc.NonBuilding_unlock, "");
                //删除前节点的后继
                foreach (var pre in sc.Pre_technology.ToList())
                {
                    ScienceDict.TryGetValue(pre, out var science);
                    science.After_technology.Remove(id);
                }
                //删除后节点的前置字段
                foreach (var aft in sc.After_technology)
                {
                    ScienceDict.TryGetValue(aft, out var science);
                    science.Pre_technology = science.Pre_technology.RemoveIdPreNode(id.ToString());
                    science.PathNode = science.PathNode.RemoveIdPrePath(id.ToString());
                    ui.tilemap.transform.Find(aft.ToString()).GetComponent<Node>().UpdateNodeAppearance();
                }
                //删除我
                ScienceDict.Remove(sc.Id);
                Destroy(node.gameObject);
                panel.GetComponent<PanelScienceEdit>().DestoryPanel();
            }

        }

    }

    private void updateMouseEvent()
    {
        #region 节点编辑功能
        if (ui.toggleEditNode && ui.toggleEditNode.isOn)
        {
            if (Input.GetMouseButtonDown(1))
            {
                //点到UI，不进行处理
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
                //右键点空，关闭面板
                if (panel && !EventSystem.current.currentSelectedGameObject)
                {
                    panel.GetComponent<PanelScienceEdit>().DestoryPanel();
                }

                //鼠标选择节点，实例化编辑窗口
                if ((rayHitNode = rayDetect()) && rayHitNode.tag is Constants.Tags.Node)
                {
                    string nodeId = rayHitNode.name;
                    Node n = rayHitNode.GetComponent<Node>();
                    //实例化Panel
                    panel = Instantiate(ui.panelNodeEditPrefab);
                    panel.transform.SetParent(ui.canvas.transform.Find(Constants.UIPath.PanelRightContent), false);
                    panel.name = $"{nodeId}(Edit)";
                    //从节点获取科技信息
                    panel.GetComponent<PanelScienceEdit>().node = n;
                    panel.GetComponent<PanelScienceEdit>().sc = n.sc;
                    //node
                    n.SetSelectStyle(true);
                    n.UpdateLineAnchor();
                    ui.tipText.Log($"编辑节点：{nodeId}:{n.sc.Name}");
                }
            }
        }

        #endregion

        #region 节点移动
        if (ui.toggleEditNode && ui.toggleMoveNode.isOn)
        {
            if (Input.GetMouseButtonDown(0))
            {
                鼠标按下位置_屏幕 = Input.mousePosition;
                rayHitMove = rayDetect();
                if (rayHitMove && rayHitMove.tag is Constants.Tags.Node)
                {
                    //左键点击时切换sprite
                    rayHitMove.GetComponent<Node>().SetHoverStyle(true);
                }
            }
            if (rayHitMove && Input.GetMouseButton(0))
            {
                var 点击时 = ui.grid.WorldToCell(ui.camSence.ScreenToWorldPoint(鼠标按下位置_屏幕));
                var 按住时 = ui.grid.WorldToCell(ui.camSence.ScreenToWorldPoint(Input.mousePosition));
                //未移动出一个网格，不算移动节点
                if (点击时 == 按住时)
                {
                    return;
                }
                鼠标按下位置_屏幕 = Input.mousePosition;
                Vector3 鼠标_世界位置 = ui.camSence.ScreenToWorldPoint(Input.mousePosition);
                Vector3Int gridPosI = ui.grid.WorldToCell(new Vector3(鼠标_世界位置.x, 鼠标_世界位置.y, 0));
                Vector3 gridPos = ui.grid.CellToWorld(gridPosI);
                rayHitMove.transform.position = gridPos;
                //节点的处理逻辑
                if (rayHitMove.tag is Constants.Tags.Node)
                {
                    Node n = rayHitMove.GetComponent<Node>();
                    n.UpdateGridPos(gridPosI);
                    //更新 后继点 的连线起始位置
                    foreach (var item in n.sc.After_technology)
                    {
                        GameObject child = ui.tilemap.transform.Find(item.ToString()).gameObject;
                        if (child)
                        {
                            child.GetComponent<Node>().UpdateNodeAppearance();
                        }
                    }
                    //如果打开了节点编辑窗口，则更新窗口中的位置信息
                    GameObject editPanel = GameObject.Find($"{rayHitMove.name}(Edit)");
                    if (editPanel)
                    {
                        editPanel.GetComponent<PanelScienceEdit>().UpdatePositionByDrag(new(gridPosI.x, gridPosI.y));
                        n.ClearAnchor();
                        n.UpdateLineAnchor();
                    }
                }
                //线的处理逻辑 未处理
                else if (rayHitMove.tag is Constants.Tags.NodeLine)
                {

                }
                //锚点的处理逻辑
                else if (rayHitMove.tag is Constants.Tags.Anchor)
                {
                    int lrIndex = int.Parse(rayHitMove.name);
                    LineRenderer lr = rayHitMove.GetComponentInParent<LineRenderer>();
                    //更新锚点位置
                    Vector2 anchorMoveTo = new(gridPos.x, gridPos.y);
                    Vector3 lineIndexAt = new(anchorMoveTo.x, anchorMoveTo.y, 1);
                    rayHitMove.transform.position = anchorMoveTo;
                    lr.SetPosition(lrIndex, lineIndexAt);
                    //更新字典中的路径位置
                    string nodeFrom = lr.gameObject.name.Split("->")[0];
                    string nodeTo = lr.gameObject.name.Split("->")[1];
                    ScienceDict.TryGetValue(int.Parse(nodeTo), out var sc);
                    int c = lr.positionCount;
                    string newpos = null;
                    for (int i = 1; i < c - 1; i++)
                    {
                        if (newpos is not null)
                        {
                            newpos += "_";
                        }
                        newpos += $"{ui.grid.WorldToCell(lr.GetPosition(i)).y}_{ui.grid.WorldToCell(lr.GetPosition(i)).x}";
                    }
                    sc.PathNode = sc.PathNode.UpdatePathNodeById(nodeFrom, newpos);
                    //更新编辑界面中，路径字段显示的文字
                    panel.GetComponent<PanelScienceEdit>().UpdatePrePath(sc.PathNode);
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (rayHitMove && rayHitMove.tag is Constants.Tags.Node)
                {
                    rayHitMove.GetComponent<Node>().SetHoverStyle(false);
                }
                rayHitMove = null;
            }
        }
        #endregion
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

    }

    #endregion

}
