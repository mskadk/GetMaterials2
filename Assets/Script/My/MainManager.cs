using Assets.Script.My.Excel;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MainManager : MonoBehaviour
{
    public const string WorkSpace_Sprite = "D:\\work\\manager\\Assets WorkSpace\\FreeWorld\\sprite\\";
    public const string WorkSpace_Excel = "D:\\work\\manager\\策划\\项目企划\\数据表\\";

    public GameObject Node;

    Camera camera;
    #region Excel
    ExcelManager em;
    #endregion

    #region Dictionary
    Dictionary<int, Science> ScienceDict;
    #endregion

    #region CommandDebug
    Button btn;
    InputField ifx;
    InputField ify;
    #endregion

    #region Hexagon Tilemap
    GameObject tilemap;
    Grid grid;
    #endregion

    private void Start()
    {
        camera = GameObject.Find("CameraSence").GetComponent<Camera>();
        initReadExcel();
        initUI();
        initTechMap();
        initScienceNode();

    }

    private void Update()
    {
        updateMouseEvent();
    }



    private void initScienceNode()
    {
        foreach (var sc in ScienceDict.Values)
        {

            GameObject o = Instantiate(Node, tilemap.transform);
            o.transform.position = grid.CellToWorld(new(sc.HexGridY, sc.HexGridX, 0));
            o.name = sc.Id.ToString();
            o.GetComponent<Node>().sc = sc;
        }
    }

    private void initReadExcel()
    {
        em = new();
        ScienceDict = em.Load(WorkSpace_Excel + "Science.xlsx");
    }

    private void initTechMap()
    {
        tilemap = GameObject.Find("Tilemap");
        grid = GameObject.Find("Grid").GetComponent<Grid>();
    }

    private void initUI()
    {
        btn = GameObject.Find("Button_Command").GetComponent<Button>();
        ifx = GameObject.Find("InputField_Command_x").GetComponent<InputField>();
        ify = GameObject.Find("InputField_Command_y").GetComponent<InputField>();
    }

    public void btnCommandClicked()
    {
        Debug.Log($"{ifx.text},{ify.text}clicked");
        testGenerateHeaxgon();
    }

    #region 鼠标事件
    private void updateMouseEvent()
    {
        if (Input.GetMouseButtonDown(1))
        {
            ///name是科技节点
            string name = rayDetect();
            if (name is not null)
            {
                GameObject o = tilemap.transform.Find(name).gameObject;
                if (o)
                {
                    Debug.Log(o.GetComponent<Node>().sc.Name);
                }
            }

        }
    }
    #endregion

    #region 射线检测
    string rayDetect()
    {
        string target = null;
        Vector3 vvv = camera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(vvv, Vector3.forward, Mathf.Infinity);
        if (hit)
        {
            target = hit.transform.name;
        }
        return target;

    }
    #endregion

    #region 测试_绘制一个六边形在六边形网格地图中
    private void testGenerateHeaxgon()
    {
        GameObject o = new($"Hex({ifx.text},{ify.text})");
        o.transform.SetParent(tilemap.transform);
        o.transform.position = grid.CellToWorld(new(int.Parse(ify.text), int.Parse(ifx.text), 0));

        GameObject h = new("hex");
        h.transform.SetParent(o.transform);
        h.transform.position = o.transform.position;
        var h_sr = h.AddComponent<SpriteRenderer>();
        h_sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/HexagonFlatTop.png");

        #region 显示文本生成
        GameObject text = new("text");
        text.transform.SetParent(o.transform);
        text.transform.position = o.transform.position;
        text.AddComponent<MeshRenderer>();
        var tm = text.AddComponent<TextMesh>();
        tm.fontSize = 200;
        tm.characterSize = 0.015f;
        tm.text = $"{ifx.text},{ify.text}";
        tm.color = Color.black;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        #endregion
    }
    #endregion
}
