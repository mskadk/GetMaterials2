using Assets.Script.My.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public string WorkSpace_Sprite = "D:\\work\\manager\\Assets WorkSpace\\FreeWorld\\sprite\\";
    public string WorkSpace_Excel = "D:\\work\\manager\\策划\\项目企划\\数据表\\";

    public GameObject Node;
    public GameObject ghostNodePrefab;
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
        updateKeyboardEvent();
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

    #region 主界面 & 模式选择
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

    public void BtnNewNode()
    {
        GameObject newNode = Instantiate(ghostNodePrefab);
    }

    #endregion

    #region 鼠标事件 && 键盘事件
    private void updateKeyboardEvent()
    {
        if (!EventSystem.current.currentSelectedGameObject && panel)
        {
            //Esc 关闭编辑界面
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                panel.GetComponent<PanelScienceEdit>().DestoryPanel();
            }

        }

    }

    private Vector3 鼠标按下位置;
    bool editing = false;
    public void SetEditFalse() => editing = false;

    private GameObject rayHitMove;
    private GameObject rayHitEdit;
    private GameObject panel;
    private void updateMouseEvent()
    {
        //节点编辑功能
        if (useEditNode.isOn)
        {
            if (!editing && Input.GetMouseButtonDown(1))
            {
                //鼠标选择节点，实例化编辑窗口
                if ((rayHitEdit = rayDetect()) && rayHitEdit.tag is "Node")
                {
                    string nodeId = rayHitEdit.name;
                    Node n = rayHitEdit.GetComponent<Node>();
                    //实例化Panel
                    panel = Instantiate(panelNodeEditPrefab);
                    panel.transform.SetParent(canvas.transform.Find("Panel/Panel_RightContent"), false);
                    panel.name = $"{nodeId}(Edit)";
                    //从节点获取科技信息
                    panel.GetComponent<PanelScienceEdit>().node = n;
                    panel.GetComponent<PanelScienceEdit>().sc = n.sc;
                    editing = true;
                    //node
                    n.SetSelectStyle(true);
                    //n.UpdateLineAnchor();
                    debug.Log($"编辑节点：{nodeId}:{n.sc.Name}");
                }
            }
        }
        //节点移动功能
        if (useMoveNode.isOn)
        {
            //鼠标拖动节点
            if (Input.GetMouseButtonDown(0))
            {
                鼠标按下位置 = Input.mousePosition;
                rayHitMove = rayDetect();
                if (rayHitMove && rayHitMove.tag is "Node")
                {
                    rayHitMove.GetComponent<Node>().SetHoverStyle(true);
                }
            }
            if (rayHitMove && Input.GetMouseButton(0))
            {
                //鼠标还未移动，return
                if (鼠标按下位置 == Input.mousePosition)
                {
                    return;
                }
                Vector3 worldPos = camSence.ScreenToWorldPoint(Input.mousePosition);
                Vector3Int gridPosI = grid.WorldToCell(new Vector3(worldPos.x, worldPos.y, 0));
                Vector3 gridPos = grid.CellToWorld(gridPosI);
                rayHitMove.transform.position = gridPos;
                //节点的处理逻辑
                if (rayHitMove.tag is "Node")
                {
                    Node n = rayHitMove.GetComponent<Node>();
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
                    GameObject editPanel = GameObject.Find($"{rayHitMove.name}(Edit)");
                    if (editPanel)
                    {
                        editPanel.GetComponent<PanelScienceEdit>().UpdatePositionByDrag(new(gridPosI.x, gridPosI.y));

                    }
                }
                //线的处理逻辑
                else if (rayHitMove.tag is "NodeLine")
                {

                }
                //锚点的处理逻辑
                else if (rayHitMove.tag is "Anchor")
                {

                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (rayHitMove && rayHitMove.tag is "Node")
                {
                    rayHitMove.GetComponent<Node>().SetHoverStyle(false);
                }
                rayHitMove = null;
            }
        }



    }
    #endregion

    #region 外部调用
    public void NewNode(Vector3Int pos)
    {
        int id = -3;
        while (ScienceDict.ContainsKey(id)) { id--; }
        GameObject o = Instantiate(Node, grid.CellToWorld(new(pos.x, pos.y, 0)), new Quaternion(), tilemap.transform);
        o.name = id.ToString();
        Science sc = new(id,1,0,.75f,4,"新科技","描述","备注","-1","-1",pos.y,pos.x,"-1","-1","-1",.01f,1,"-1");
        o.GetComponent<Node>().sc = sc;
        ScienceDict.Add(id, sc);

    }
    #endregion

}
