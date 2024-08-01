using Assets.Script.My.Extention;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Node : MonoBehaviour
{
    public Science sc;
    public GameObject linesPrefab;
    SpriteRenderer sr;
    TextMesh tm;
    GameObject parent;
    bool updateLine;
    Grid grid;
    // Start 
    void Start()
    {
        parent = transform.parent.gameObject;
        grid = GameObject.Find("Grid").GetComponent<Grid>();
        if (sc != null)
        {
            sr = GetComponent<SpriteRenderer>();
            tm = transform.Find("text").GetComponent<TextMesh>();
        }
        updateLine = true;
    }

    // Update 
    void Update()
    {
        if (sc != null)
        {
            UpdateNode();
            if (updateLine)
            {
                UpdateLine();

            }
        }
    }

    #region MouseEvent
    private void OnMouseUpAsButton()
    {
        Debug.Log($"{sc.Id}:{sc.Name}");
    }

    #endregion

    #region LineManager
    void UpdateLine()
    {
        List<Vector3Int> 前置路径列表 = sc.PathNode.ParesV3IList();
        List<int> 前置节点列表 = sc.Pre_technology.ToList();
        if (前置节点列表 is null)
        {
            return;
        }
        else
        {
            foreach (var pre in 前置节点列表)
            {
                GameObject p = parent.transform.Find(pre.ToString()).gameObject;
                Science psc = p.GetComponent<Node>().sc;
                string lineName = $"{psc.Id}->{sc.Id}";
                GameObject lineGO = Instantiate(linesPrefab, transform.position, new(), transform);
                lineGO.name = lineName;
                var line = lineGO.GetComponent<LineRenderer>();
                line.startColor = getColor(psc.IconColor);
                line.endColor = getColor(sc.IconColor);
                if (前置路径列表.Count == 0)
                {
                    line.SetPositions(new[] {
                    p.transform.position + Vector3.forward,
                    transform.position + Vector3.forward
                    });
                }
                else
                {
                    List<Vector3> positions = new() { p.transform.position + Vector3.forward};
                    foreach (var item in 前置路径列表)
                    {
                        if (item.x == psc.Id)
                        {
                            var pos = grid.CellToWorld(new(item.z, item.y, 1));
                            positions.Add(pos);
                        }
                    }
                    positions.Add(transform.position + Vector3.forward);
                    line.positionCount = positions.Count;
                    line.SetPositions(positions.ToArray());
                }
            }
        }
        updateLine = false;
    }
    #endregion

    private void UpdateNode()
    {
        sr.color = getColor(sc.IconColor);
        tm.text = $"{sc.Id}\n{sc.Name}";
    }


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
}
