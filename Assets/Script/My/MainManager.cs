using Assets.Script.My.Excel;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
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
    public const string WorkSpace_Sprite = "D:\\work\\manager\\Assets WorkSpace\\FreeWorld\\sprite\\";
    public const string WorkSpace_Excel = "D:\\work\\manager\\策划\\项目企划\\数据表\\";

    public GameObject Node;
    public Camera camSence;
    public GridDrawer gridDrawer;
    #region Excel
    ExcelManager em;
    #endregion

    #region Dictionary
    Dictionary<int, Science> ScienceDict;
    #endregion

    #region Hexagon Tilemap
    GameObject tilemap;
    Grid grid;
    #endregion

    #region UI
    public Dropdown dpMainPage;
    public GameObject panelNodeEditPrefab;
    public GameObject canvas;
    #endregion

    #region Command
    Toggle useMoveCam;
    Toggle useMoveNode;
    Toggle useEditNode;
    #endregion


    private void Start()
    {
        Init();
    }

    private void Update()
    {
        updateMouseEvent();
    }

    #region UI，界面选择
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

    //public void btnCommandClicked()
    //{
    //    Debug.Log($"{ifx.text},{ify.text}clicked");
    //    testGenerateHeaxgon();
    //}

    #endregion


    #region INIT
    private void Init()
    {
        initReadExcel();
        initUI();
        initTechMap();
        initScienceNode();
    }


    private void initReadExcel()
    {
        em = new();
        ScienceDict = em.Load(WorkSpace_Excel + "Science.xlsx");
    }

    private void initUI()
    {
        useMoveCam = GameObject.Find("ToggleMoveCam").GetComponent<Toggle>();
        useMoveNode = GameObject.Find("ToggleMoveNode").GetComponent<Toggle>();
        useEditNode = GameObject.Find("ToggleEditNode").GetComponent<Toggle>();
    }

    private void initTechMap()
    {
        tilemap = GameObject.Find("Tilemap");
        grid = GameObject.Find("Grid").GetComponent<Grid>();
    }

    private void initScienceNode()
    {
        foreach (var sc in ScienceDict.Values)
        {

            GameObject o = Instantiate(Node, grid.CellToWorld(new(sc.HexGridY, sc.HexGridX, 0)), new Quaternion(), tilemap.transform);
            o.name = sc.Id.ToString();
            o.GetComponent<Node>().sc = sc;
        }
    }

    #endregion

    #region 鼠标事件
    bool edit = false;
    public void setEditFalse() => edit = false;
    private GameObject nodeMove;
    private void updateMouseEvent()
    {

        //鼠标选择节点
        if (useEditNode.isOn && !edit && Input.GetMouseButtonDown(1))
        {

            GameObject target = rayDetect();
            string name;
            if (target is not null)
            {
                name = target.name;
                target.TryGetComponent(out Node n);
                if (n)
                {
                    GameObject p = Instantiate(panelNodeEditPrefab);
                    p.transform.SetParent(canvas.transform.Find("Panel/Panel_RightContent"), false);
                    p.name = $"{name}(Edit)";
                    ScienceDict.TryGetValue(int.Parse(name), out var sc);
                    p.GetComponent<PanelScienceEdit>().sc = sc;
                    edit = true;
                    n.ShowLineAnchor();

                    name += $"\n{n.sc.Name}";
                    Debug.Log(name);
                }
            }
        }

        //鼠标拖动节点
        if (useMoveNode.isOn && Input.GetMouseButtonDown(0))
        {
            nodeMove = rayDetect();
        }
        if (nodeMove && Input.GetMouseButton(0))
        {
            Vector3 worldPos = camSence.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPosI = grid.WorldToCell(new Vector3(worldPos.x, worldPos.y, 0));
            Vector3 gridPos = grid.CellToWorld(gridPosI);
            nodeMove.transform.position = gridPos;
            Node n = nodeMove.GetComponent<Node>();
            n.UpdateGridPos(gridPosI);
            foreach (var item in n.sc.After_technology)
            {
                GameObject child = tilemap.transform.Find(item.ToString()).gameObject;
                if (child)
                {
                    child.GetComponent<Node>().UpdateLineStart(n.transform.position);
                }
            }
            GameObject edit = GameObject.Find($"{nodeMove.name}(Edit)");
            if (edit)
            {
                edit.GetComponent<PanelScienceEdit>().UpdatePositionTextImmediate(new(gridPosI.x, gridPosI.y));

            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            nodeMove = null;
        }
    }
    #endregion

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
