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

    private GameObject _parent;
    private Grid _grid;

    private EditorConfig config;
    private GameObject parent
    {
        get
        {
            if (_parent == null)
                _parent = transform.parent?.gameObject;
            return _parent;
        }
        set => _parent = value;
    }
    private Grid grid
    {
        get
        {
            if (_grid == null)
                _grid = UIReferences.Instance?.grid;
            return _grid;
        }
        set => _grid = value;
    }

    private Color getColor(int i)
    {
        return config.GetColor(i);
    }

    private List<GameObject> getAllLineGOs()
    {
        var list = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).tag is Constants.Tags.NodeLine)
            {
                list.Add(transform.GetChild(i).gameObject);
            }
        }
        return list;
    }

    void Start()
    {
        var ui = UIReferences.Instance;
        config = GameObject.Find(Constants.GameObjectNames.MainManager).GetComponent<MainManager>().config;
        _parent = transform.parent.gameObject;
        _grid = ui.grid;
        sr = GetComponent<SpriteRenderer>();
        tmUp = transform.Find("text_up").GetComponent<TextMesh>();
        tmDown = transform.Find("text_down").GetComponent<TextMesh>();
        UpdateNodeStyle();
        UpdateLine();
    }

    #region Style
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

    /// <summary>
    /// 更新节点的世界坐标位置
    /// </summary>
    public void UpdateWorldPos(Vector2 worldPos)
    {
        sc.HexGridX = (float)Math.Round(worldPos.x, 3);
        sc.HexGridY = (float)Math.Round(worldPos.y, 3);
        UpdateLine();
    }

    /// <summary>
    /// 根据绑定的Science值更新外观
    /// </summary>
    public void UpdateNodeAppearance()
    {
        UpdateNodeStyle();
        UpdateLine();

        var border = transform.Find("border");
        if (border && border.gameObject.activeSelf)
        {
            ClearAnchor();
            UpdateLineAnchor();
        }
    }

    private void UpdateNodeStyle()
    {
        sr.color = getColor(sc.IconColor);

        transform.localScale = sc.IconScale switch
        {
            Constants.NodeScale.Large => Vector3.one * Constants.NodeScale.Large,
            Constants.NodeScale.Middle => Vector3.one * Constants.NodeScale.Middle,
            Constants.NodeScale.Small => Vector3.one * Constants.NodeScale.Small,
            _ => Vector3.one * Constants.NodeScale.Middle,
        };

        tmUp.text = $"{sc.Id}";
        tmDown.text = $"{sc.Name}";
        transform.localScale = sc.IconScale * Vector3.one;

        Transform del = transform.Find("fw_icon");
        if (del)
        {
            Destroy(del.gameObject);
        }
        if (false || Environment.MachineName == "DESKTOP-0418DES")
        {
            Debug.LogWarning("跳过图标绘制");
            return;
        }
        GameObject g = SpriteManager.Paint(gameObject, "Icon_Technology", 0, sc.ModuleId);
        //g.transform.localScale = Vector3.one * .01f;
        g.GetComponent<MeshRenderer>().material.shader = Shader.Find("Custom/ScienceIcon_Shader");
        g.GetComponent<MeshRenderer>().material.SetColor("_TintColor", getColor(sc.IconColor));
    }

    /// <summary>
    /// 只更新线的位置，不重建锚点
    /// </summary>
    public void UpdateLineOnly()
    {
        if (grid == null)
            grid = UIReferences.Instance?.grid;
        if (parent == null)
            parent = transform.parent?.gameObject;
        if (grid == null || parent == null) return;

        // 使用新的解析方法获取世界坐标
        List<Vector3> prePathsList = sc.PathNode.ParsePathNodeList();
        List<int> preNodesList = sc.Pre_technology.ToList();

        if (preNodesList == null || preNodesList.Contains(-2))
        {
            foreach (var item in getAllLineGOs())
            {
                Destroy(item);
            }
            return;
        }

        List<string> lineNameList = new();
        foreach (var preNodeId in preNodesList)
        {
            GameObject preNodeGameObject = parent.transform.Find(preNodeId.ToString())?.gameObject;
            if (preNodeGameObject == null) continue;
            Science parentSc = preNodeGameObject.GetComponent<Node>()?.sc;
            if (parentSc == null) continue;

            string lineName = $"{parentSc.Id}->{this.sc.Id}";
            lineNameList.Add(lineName);

            Transform lineTransform = transform.Find(lineName);
            if (lineTransform == null) continue;

            var line = lineTransform.GetComponent<LineRenderer>();
            if (line == null) continue;

            // 构建路径点（使用世界坐标）
            List<Vector3> positions = new() { preNodeGameObject.transform.position + Vector3.forward };
            foreach (var path in prePathsList)
            {
                if ((int)path.x == parentSc.Id)
                {
                    // path.y = worldX, path.z = worldY
                    positions.Add(new Vector3(path.y, path.z, 1));
                }
            }
            positions.Add(transform.position + Vector3.forward);

            if (line.positionCount == positions.Count)
            {
                line.SetPositions(positions.ToArray());
            }

            line.startColor = getColor(parentSc.IconColor);
            line.endColor = getColor(this.sc.IconColor);
            line.startWidth = line.endWidth = sc.LineScale;
        }

        foreach (var item in getAllLineGOs())
        {
            if (!lineNameList.Contains(item.name))
            {
                Destroy(item);
            }
        }
    }

    #region 线和锚点

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
                        anchor.transform.position = new Vector2(lr.GetPosition(i).x, lr.GetPosition(i).y);
                        anchor.name = name;
                        anchor.transform.Find("text").GetComponent<TextMesh>().text = name;
                    }
                }
            }
        }

        SelectionManager.Instance.RefreshAnchorHighlights();
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

    void UpdateLine()
    {
        List<Vector3> prePathsList = sc.PathNode.ParsePathNodeList();
        List<int> preNodesList = sc.Pre_technology.ToList();

        if (preNodesList is null || preNodesList.Contains(-2))
        {
            foreach (var item in getAllLineGOs())
            {
                Destroy(item);
            }
            return;
        }
        else
        {
            List<string> lineNameList = new();
            foreach (var preNodeId in preNodesList)
            {
                GameObject preNodeGameObject = parent.transform.Find(preNodeId.ToString())?.gameObject;
                if (preNodeGameObject == null) continue;

                Science parentSc = preNodeGameObject.GetComponent<Node>()?.sc;
                if (parentSc == null) continue;

                string lineName = $"{parentSc.Id}->{this.sc.Id}";
                lineNameList.Add(lineName);

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

                // 构建路径点
                List<Vector3> positions = new() { preNodeGameObject.transform.position + Vector3.forward };
                foreach (var path in prePathsList)
                {
                    if ((int)path.x == parentSc.Id)
                    {
                        // path.y = worldX, path.z = worldY
                        positions.Add(new Vector3(path.y, path.z, 1));
                    }
                }
                positions.Add(transform.position + Vector3.forward);

                line.positionCount = positions.Count;
                line.SetPositions(positions.ToArray());

                line.startColor = getColor(parentSc.IconColor);
                line.endColor = getColor(this.sc.IconColor);
                line.startWidth = line.endWidth = sc.LineScale;
            }

            foreach (var item in getAllLineGOs())
            {
                if (!lineNameList.Contains(item.name))
                {
                    Destroy(item);
                }
            }
        }
    }

    public bool HasAnchors()
    {
        List<GameObject> listLine = getAllLineGOs();
        foreach (var line in listLine)
        {
            if (line.transform.childCount > 0)
            {
                return true;
            }
        }
        return false;
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
