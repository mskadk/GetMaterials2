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
    SpriteRenderer sr;
    TextMesh tm;
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
    /// <returns>List(GameObject)</returns>
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
        if (sc != null)
        {
            sr = GetComponent<SpriteRenderer>();
            tm = transform.Find("text").GetComponent<TextMesh>();
        }
        UpdateNodeStyle();
        UpdateLine();
    }
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
        sr.color = getColor(sc.IconColor);
        if (sc.IconScale == 0.75)
        {
            transform.localScale = .75f * Vector3.one;
        }
        else if (sc.IconScale == 1.5)
        {
            transform.localScale = 1.75f * Vector3.one;
        }
        tm.text = $"{sc.Id}\n{sc.Name}";
        transform.localScale = sc.IconScale * Vector3.one;
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

                    }
                }
            }
        }
    }

    public void ClearAnchor()
    {
        List<GameObject> listLine = getAllLineGOs();
        foreach (var line in listLine)
        {
            while (line.transform.childCount > 0)
            {
                DestroyImmediate(line.transform.GetChild(0).gameObject);
            }
        }
    }
    #endregion

    void UpdateLine()
    {
        List<Vector3Int> List前置路径 = sc.PathNode.ParesV3IList();
        List<int> List前置节点 = sc.Pre_technology.ToList();
        //没有前置，删除所有线
        if (List前置节点 is null)
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
