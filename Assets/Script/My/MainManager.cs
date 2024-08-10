using Assets.Script.My.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MainManager : MonoBehaviour
{
    enum Mode
    {
        none,
        science,
        productionList,
    }
    public string WorkSpace_Sprite = "D:\\work\\manager\\Assets WorkSpace\\FreeWorld\\sprite\\";
    public string WorkSpace_Excel = "D:\\work\\manager\\策划\\项目企划\\数据表\\";

    public GameObject Node;
    public Camera camSence;
    public GridDrawer gridDrawer;

    // Excel
    ExcelManager em;

    // Dictionary
    public Dictionary<int, Science> ScienceDict;

    // Hexagon Tilemap
    public GameObject tilemap;
    Grid grid;

    // UI
    public Dropdown dpMainPage;
    public GameObject panelNodeEditPrefab;
    public GameObject canvas;

    // Command
    Toggle useMoveCam;
    Toggle useMoveNode;
    Toggle useEditNode;
    TipText debug;

    #region 初始化
    private void Init()
    {
        initExcel();
        initUI();
        initTilemap();
        initNode();
    }

    private void initExcel()
    {
        if (Environment.MachineName == "AOYE")
        {
            WorkSpace_Sprite = "F:\\UnityWorkSpace\\GetMaterials2 Assets Space\\sprites\\";
            WorkSpace_Excel = "F:\\UnityWorkSpace\\GetMaterials2 Assets Space\\sheets\\";
        }

        em = new();
        ScienceDict = em.Load(WorkSpace_Excel + "Science.xlsx");
    }

    private void initUI()
    {
        useMoveCam = GameObject.Find("ToggleMoveCam").GetComponent<Toggle>();
        useMoveNode = GameObject.Find("ToggleMoveNode").GetComponent<Toggle>();
        useEditNode = GameObject.Find("ToggleEditNode").GetComponent<Toggle>();
        debug = GameObject.Find("tiptext").GetComponent<TipText>();
    }

    private void initTilemap()
    {
        tilemap = GameObject.Find("Tilemap");
        grid = GameObject.Find("Grid").GetComponent<Grid>();
    }

    private void initNode()
    {
        foreach (var sc in ScienceDict.Values)
        {

            GameObject o = Instantiate(Node, grid.CellToWorld(new(sc.HexGridY, sc.HexGridX, 0)), new Quaternion(), tilemap.transform);
            o.name = sc.Id.ToString();
            o.GetComponent<Node>().sc = sc;
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
        Vector3 vvv = camSence.ScreenToWorldPoint(Input.mousePosition);
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
        printDict();
    }
    /// <summary>
    /// 打印
    /// <para>/ 打印整个科技表字典</para>
    /// <para>. 鼠标位置Node的Science数据</para>
    /// </summary>
    private void printDict()
    {
        // / 打印整个科技表字典
        if (Input.GetKeyDown(KeyCode.Slash))
        {
            foreach (var sc in ScienceDict)
            {
                Debug.Log($"KEY:{sc.Key},VALUE:{sc.Value}");
            }
        }
        // . 从科技字典找到鼠标指向的Node的Science数据
        if (Input.GetKeyDown(KeyCode.Period))
        {
            var v = rayDetect();
            if (v != null && v.tag is "Node")
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

    #region 主界面模式选择
    public void SwitchPage()
    {
        switch (dpMainPage.value)
        {
            case (int)Mode.science: OnDrawScience(); break;
            case (int)Mode.productionList: OffDrawScience(); break;
            default:
                break;
        }
    }

    public void ToggleMoveCam()
    {
        camSence.GetComponent<CameraEventControll>().相机控制 = useMoveCam.isOn;
    }

    private void OnDrawScience()
    {
        camSence.GetComponent<GridDrawer>().渲染至游戏 = true;
        camSence.GetComponent<CameraEventControll>().相机控制 = true;
        tilemap.SetActive(true);
    }

    private void OffDrawScience()
    {
        camSence.GetComponent<GridDrawer>().渲染至游戏 = false;
        camSence.GetComponent<CameraEventControll>().相机控制 = false;
        tilemap.SetActive(false);
    }

    #endregion

    #region 鼠标事件
    private Vector3 mPosPri;
    bool selecting = false;
    public void SetEditFalse() => selecting = false;
    private GameObject nodeMove;
    private void updateMouseEvent()
    {
        //鼠标选择节点，实例化编辑窗口
        if (useEditNode.isOn && !selecting && Input.GetMouseButtonDown(1))
        {


            GameObject target = rayDetect();
            if (target)
            {
                string name = target.name;
                target.TryGetComponent(out Node n);
                if (n)
                {
                    GameObject p = Instantiate(panelNodeEditPrefab);
                    p.transform.SetParent(canvas.transform.Find("Panel/Panel_RightContent"), false);
                    p.name = $"{name}(Edit)";
                    ScienceDict.TryGetValue(int.Parse(name), out var sc);
                    p.GetComponent<PanelScienceEdit>().sc = sc;
                    p.GetComponent<PanelScienceEdit>().node = n;
                    selecting = true;
                    //n.UpdateLineAnchor();
                    name += $"\n{n.sc.Name}";
                    debug.Log(name);
                }
            }
        }

        //鼠标拖动节点
        if (useMoveNode.isOn && Input.GetMouseButtonDown(0))
        {
            mPosPri = Input.mousePosition;
            nodeMove = rayDetect();
        }
        if (nodeMove && Input.GetMouseButton(0))
        {
            //鼠标还未移动，return
            if (mPosPri == Input.mousePosition)
            {
                return;
            }
            Vector3 worldPos = camSence.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPosI = grid.WorldToCell(new Vector3(worldPos.x, worldPos.y, 0));
            Vector3 gridPos = grid.CellToWorld(gridPosI);
            nodeMove.transform.position = gridPos;
            //节点的处理逻辑
            if (nodeMove.tag is "Node")
            {
                Node n = nodeMove.GetComponent<Node>();
                n.UpdateGridPos(gridPosI);
                //更新 后继点 的连线起始位置
                foreach (var item in n.sc.After_technology)
                {
                    GameObject child = tilemap.transform.Find(item.ToString()).gameObject;
                    if (child)
                    {
                        child.GetComponent<Node>().UpdateLineStart(n.transform.position);
                    }
                }
                //如果打开了节点编辑窗口，则更新窗口中的位置信息
                GameObject edit = GameObject.Find($"{nodeMove.name}(Edit)");
                if (edit)
                {
                    edit.GetComponent<PanelScienceEdit>().UpdatePositionByDrag(new(gridPosI.x, gridPosI.y));

                }
            }
            //锚点的处理逻辑
            else if (nodeMove.tag is "Anchor")
            {

            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            nodeMove = null;
        }
    }
    #endregion






    #region 测试_绘制一个六边形在六边形网格地图中
    //private void testGenerateHeaxgon()
    //{
    //    GameObject o = new($"Hex({ifx.text},{ify.text})");
    //    o.transform.SetParent(tilemap.transform);
    //    o.transform.position = grid.CellToWorld(new(int.Parse(ify.text), int.Parse(ifx.text), 0));

    //    GameObject h = new("hex");
    //    h.transform.SetParent(o.transform);
    //    h.transform.position = o.transform.position;
    //    var h_sr = h.AddComponent<SpriteRenderer>();
    //    h_sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/HexagonFlatTop.png");

    //    #region 显示文本生成
    //    GameObject text = new("text");
    //    text.transform.SetParent(o.transform);
    //    text.transform.position = o.transform.position;
    //    text.AddComponent<MeshRenderer>();
    //    var tm = text.AddComponent<TextMesh>();
    //    tm.fontSize = 200;
    //    tm.characterSize = 0.015f;
    //    tm.text = $"{ifx.text},{ify.text}";
    //    tm.color = Color.black;
    //    tm.anchor = TextAnchor.MiddleCenter;
    //    tm.alignment = TextAlignment.Center;
    //    #endregion
    //}
    #endregion
}
