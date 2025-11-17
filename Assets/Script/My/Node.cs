using Assets.Script.My;
using Assets.Script.My.Extention;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Node : MonoBehaviour
{
    public Science sc;
    public GameObject linesPrefab;
    public GameObject anchorPrefab;
    public Sprite BorderSprite;
    public Sprite SelectingSprite;
    public Sprite OriginalSprite;
    SpriteRenderer sr;
    TextMesh tmUp;
    TextMesh tmDown;
    GameObject parent;
    Grid grid;
    private Color getColor(int i)
    {
        var color = i switch
        {
            1 => Color.red,
            2 => new(1, .5f, 0),
            3 => new(1, 1, 0),
            4 => Color.green,
            5 => new(.2f, .5f, 1),
            _ => Color.white,
        };
        return color;
    }
    /// <returns>List射线子物体</returns>
    private List<GameObject> getAllLineGOs()
    {
        var list = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).tag is "NodeLine")
            {
                list.Add(transform.GetChild(i).gameObject);
            }
        }
        return list;
    }
    void Start()
    {
        parent = transform.parent.gameObject;
        grid = GameObject.Find("Grid").GetComponent<Grid>();
        sr = GetComponent<SpriteRenderer>();
        tmUp = transform.Find("text_up").GetComponent<TextMesh>();
        tmDown = transform.Find("text_down").GetComponent<TextMesh>();

        UpdateNodeStyle();
        UpdateLine();
    }
    #region Style = 左键选中绘制边，右键选中绘制框

    public void SetSelectStyle(bool select)
    {
        var b = transform.Find("border");
        if (select)
        {
            if (!b)
            {
                GameObject border = new();
                border.transform.position = transform.position;
                border.transform.localScale = transform.localScale;
                border.transform.SetParent(transform);
                border.name = "border";
                var sp = border.AddComponent<SpriteRenderer>();
                sp.sprite = SelectingSprite;
            }
            else
            {
                b.gameObject.SetActive(true);
            }
        }
        else
        {
            if (b)
            {
                b.gameObject.SetActive(false);
            }
        }
    }

    public void SetHoverStyle(bool hover)
    {
        if (hover)
        {
            sr.sprite = BorderSprite;
        }
        else
        {
            sr.sprite = OriginalSprite;
        }
    }
    #endregion

    public void UpdateGridPos(Vector3Int pos)
    {
        sc.HexGridX = pos.y;
        sc.HexGridY = pos.x;
        UpdateLine();
    }

    /// <summary>
    /// 根据绑定的Science值更新外观，节点样式和线样式
    /// </summary>
    public void UpdateNodeAppearance()
    {
        UpdateNodeStyle();
        UpdateLine();
    }

    private void UpdateNodeStyle()
    {
        //节点颜色
        sr.color = getColor(sc.IconColor);
        //节点尺寸
        if (sc.IconScale == 0.75)
        {
            transform.localScale = .75f * Vector3.one;
        }
        else if (sc.IconScale == 1.5)
        {
            transform.localScale = 1.75f * Vector3.one;
        }
        //显示的id和名字
        tmUp.text = $"{sc.Id}";
        tmDown.text = $"{sc.Name}";
        //尺寸缩放
        transform.localScale = sc.IconScale * Vector3.one;
        //创建图标
        Transform del = transform.Find("fw_icon");
        if (del)
        {
            Destroy(del.gameObject);
        }
        if (false || Environment.MachineName == "DESKTOP-0418DES")
        {
            Debug.LogWarning("跳过图表绘制");
            return;
        }
        GameObject g = SpriteManager.Paint(gameObject, "Icon_Technology", 0, sc.ModuleId);
        g.transform.localScale = Vector3.one * .01f;
        g.GetComponent<MeshRenderer>().material.shader = Shader.Find("Custom/ScienceIcon_Shader");
        g.GetComponent<MeshRenderer>().material.SetColor("_TintColor", getColor(sc.IconColor));

    }

    #region 连接线相关

    #region 连接线锚点相关
    public void UpdateLineAnchor()
    {
        List<GameObject> listLine = getAllLineGOs();
        foreach (var line in listLine)
        {
            var lr = line.GetComponent<LineRenderer>();
            if (lr.positionCount > 2)
            {
                for (int i = 1; i < lr.positionCount - 1; i++)
                {
                    string name = $"{i}";
                    if (!line.transform.Find(name))
                    {
                        GameObject anchor = Instantiate(anchorPrefab, line.transform);
                        anchor.transform.position = new(lr.GetPosition(i).x, lr.GetPosition(i).y);
                        anchor.name = name;
                        anchor.transform.Find("text").GetComponent<TextMesh>().text = name;

                    }
                }
            }
        }
    }

    public void ClearAnchor()
    {
        List<GameObject> listLine = getAllLineGOs();
        List<GameObject> del = new();
        foreach (var line in listLine)
        {
            for (int i = 0; i < line.transform.childCount; i++)
            {
                del.Add(line.transform.GetChild(i).gameObject);
            }
            foreach (var item in del)
            {
                DestroyImmediate(item);
            }
        }
    }
    #endregion

    void UpdateLine()
    {
        List<Vector3Int> List前置路径 = sc.PathNode.ParesV3IList();
        List<int> List前置节点 = sc.Pre_technology.ToList();
        //没有前置，删除所有线
        //2025-8-22 排除-2，表示排除pda解锁的科技，这些科技没有前置所以不需要绘制前置线路
        if (List前置节点 is null || List前置节点.Contains(-2))
        {

            foreach (var item in getAllLineGOs())
            {
                Destroy(item);
            }
            return;
        }
        else
        {
            List<string> listLineName = new();
            //用前置字段生成线
            foreach (var 节点id in List前置节点)
            {
                GameObject parentNode = parent.transform.Find(节点id.ToString()).gameObject;
                Science parentSc = parentNode.GetComponent<Node>().sc;
                string lineName = $"{parentSc.Id}->{this.sc.Id}";
                listLineName.Add(lineName);
                //找到线或创建线
                GameObject lineGO;
                if (transform.Find(lineName))
                {
                    lineGO = transform.Find(lineName).gameObject;
                }
                else
                {
                    lineGO = Instantiate(linesPrefab, transform.position, new(), transform);
                    lineGO.name = lineName;
                }
                var line = lineGO.GetComponent<LineRenderer>();
                line.positionCount = List前置路径.Count + 2;
                if (List前置路径.Count == 0)
                {
                    line.SetPositions(new[] {
                    parentNode.transform.position + Vector3.forward,
                    transform.position + Vector3.forward
                    });
                }
                else
                {
                    List<Vector3> positions = new() {
                        parentNode.transform.position + Vector3.forward
                    };
                    foreach (var 路径 in List前置路径)
                    {
                        if (路径.x == parentSc.Id)
                        {
                            var pos = grid.CellToWorld(new(路径.z, 路径.y, 1));
                            positions.Add(pos);
                        }
                    }
                    positions.Add(transform.position + Vector3.forward);
                    line.positionCount = positions.Count;
                    line.SetPositions(positions.ToArray());
                }
                //更新线的外观样式
                line.startColor = getColor(parentSc.IconColor);
                line.endColor = getColor(this.sc.IconColor);
                if (sc.LineScale == 8)
                {
                    line.startWidth = line.endWidth = .15f;
                }
                else if (sc.LineScale == 4)
                {
                    line.startWidth = line.endWidth = .05f;

                }
            }
            //清除多余线
            foreach (var item in getAllLineGOs())
            {
                if (!listLineName.Contains(item.name))
                {
                    Destroy(item);
                }
            }
        }
    }
    public void UpdateLineStart(Vector3 position)
    {
        foreach (var item in getAllLineGOs())
        {
            item.GetComponent<LineRenderer>().SetPosition(0, position);
        }
    }


    #endregion




}
