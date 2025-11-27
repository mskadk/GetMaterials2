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
        set
        {
            _parent = value;
        }
    }
    private Grid grid
    {
        get
        {
            if (_grid == null)
                _grid = UIReferences.Instance?.grid;
            return _grid;
        }
        set
        {
            _grid = value;
        }
    }
    private Color getColor(int i)
    {
        return config.GetColor(i);
    }
    /// <returns>List射线子物体</returns>
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
        // 新增：如果当前是选中状态（即处于编辑模式），必须强制刷新锚点
        // 判断依据可以是是否有 border 子物体，或者外部传入状态
        // 更简单的做法：先清除所有锚点，再重新生成（如果有需要）

        // 检查是否处于编辑状态（有 border 子物体且激活）
        var border = transform.Find("border");
        if (border && border.gameObject.activeSelf)
        {
            ClearAnchor();      // 先清除旧的（解决残留问题）
            UpdateLineAnchor(); // 再生成新的（解决次序和新锚点显示问题）
        }
    }

    private void UpdateNodeStyle()
    {
        //节点颜色
        sr.color = getColor(sc.IconColor);
        //节点尺寸

        transform.localScale = sc.IconScale switch
        {
            Constants.NodeScale.Large => Vector3.one * Constants.NodeScale.Large,
            Constants.NodeScale.Middle => Vector3.one * Constants.NodeScale.Middle,
            Constants.NodeScale.Small => Vector3.one * Constants.NodeScale.Small,
            _ => Vector3.one * Constants.NodeScale.Middle,
        };
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

    /// <summary>
    /// 只更新连线位置（不重建锚点，不改变线的数量）
    /// </summary>
    public void UpdateLineOnly()
    {
        if (grid == null)
            grid = UIReferences.Instance?.grid;
        if (parent == null)
            parent = transform.parent?.gameObject;
        if (grid == null || parent == null) return;
        List<Vector3Int> PrePathsList = sc.PathNode.ParesV3IList();
        List<int> PreNodesList = sc.Pre_technology.ToList();
        if (PreNodesList == null || PreNodesList.Contains(-2))
        {
            foreach (var item in getAllLineGOs())
            {
                Destroy(item);
            }
            return;
        }
        List<string> LineNameList = new();
        foreach (var preNodeId in PreNodesList)
        {
            GameObject preNodeGameObject = parent.transform.Find(preNodeId.ToString())?.gameObject;
            if (preNodeGameObject == null) continue;
            Science parentSc = preNodeGameObject.GetComponent<Node>()?.sc;
            if (parentSc == null) continue;
            string lineName = $"{parentSc.Id}->{this.sc.Id}";
            LineNameList.Add(lineName);
            Transform lineTransform = transform.Find(lineName);
            if (lineTransform == null) continue; // 如果线不存在，跳过（不创建新的）
            var line = lineTransform.GetComponent<LineRenderer>();
            if (line == null) continue;
            // 计算路径点
            List<Vector3> positions = new() { preNodeGameObject.transform.position + Vector3.forward };
            foreach (var path in PrePathsList)
            {
                if (path.x == parentSc.Id)
                {
                    var pos = grid.CellToWorld(new(path.z, path.y, 1));
                    positions.Add(pos);
                }
            }
            positions.Add(transform.position + Vector3.forward);
            // 只有当点数量匹配时才更新（避免锚点数量变化导致问题）
            if (line.positionCount == positions.Count)
            {
                line.SetPositions(positions.ToArray());
            }
            // 更新线的颜色样式
            line.startColor = getColor(parentSc.IconColor);
            line.endColor = getColor(this.sc.IconColor);
            line.startWidth = line.endWidth = sc.LineScale;
        }
        // 清除多余的线
        foreach (var item in getAllLineGOs())
        {
            if (!LineNameList.Contains(item.name))
            {
                Destroy(item);
            }
        }
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

        // 锚点创建完成后，刷新选中状态的高亮显示
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
    #endregion

    void UpdateLine()
    {
        List<Vector3Int> PrePathsList = sc.PathNode.ParesV3IList();
        List<int> PreNodesList = sc.Pre_technology.ToList();
        //没有前置，删除所有线
        //2025-8-22 排除-2，表示排除pda解锁的科技，这些科技没有前置所以不需要绘制前置线路
        if (PreNodesList is null || PreNodesList.Contains(-2))
        {
            // 清空
            foreach (var item in getAllLineGOs())
            {
                Destroy(item);
            }
            return;
        }
        else
        {
            List<string> LineNameList = new();
            //用前置字段生成线
            foreach (var preNodeId in PreNodesList)
            {
                GameObject preNodeGameObject = parent.transform.Find(preNodeId.ToString()).gameObject;
                Science parentSc = preNodeGameObject.GetComponent<Node>().sc;
                string lineName = $"{parentSc.Id}->{this.sc.Id}";
                LineNameList.Add(lineName);
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
                line.positionCount = PrePathsList.Count + 2;
                if (PrePathsList.Count == 0)
                {
                    line.SetPositions(new[] {
                    preNodeGameObject.transform.position + Vector3.forward,
                    transform.position + Vector3.forward
                    });
                }
                else
                {
                    List<Vector3> positions = new() {
                        preNodeGameObject.transform.position + Vector3.forward
                    };
                    foreach (var 路径 in PrePathsList)
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
                //if (sc.LineScale == 8)
                //{
                //    line.startWidth = line.endWidth = .15f;
                //}
                //else if (sc.LineScale == 4)
                //{
                //    line.startWidth = line.endWidth = .05f;
                //}
                line.startWidth = line.endWidth = sc.LineScale;
            }
            //清除多余线
            foreach (var item in getAllLineGOs())
            {
                if (!LineNameList.Contains(item.name))
                {
                    Destroy(item);
                }
            }
        }
    }

    /// <summary>
    /// 检查当前节点是否已经生成了锚点
    /// </summary>
    public bool HasAnchors()
    {
        List<GameObject> listLine = getAllLineGOs();
        foreach (var line in listLine)
        {
            // 锚点是作为 LineRenderer 物体的子物体生成的
            // 如果 Line 有子物体，说明锚点已存在
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
