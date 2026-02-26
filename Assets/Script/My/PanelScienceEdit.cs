using Assets.Script.My.Extention;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

/*
 * 从MainManager生成的节点，附带的Science数据是来自科技字典的数据，因此拿到的Science是来自字典的浅拷贝。
 * 对于鼠标指针移动产生的位置改变，修改节点位置时，会直接修改字典中对应的位置信息。
 * 此时会进行判断，如果也打开了节点编辑的窗口，那么窗口中存储的备份信息，也会同步进行修改，但由于这个备份信息是从面板中读取数据生成的，因此相当于是深拷贝。
 * 所以这里应该实现的逻辑为：
 * 1、生成节点时传递字典中对应Science的引用
 * 2、鼠标拖动时，会直接更新Science字典中的位置信息（同时为后继节点更新画线的起始位置，这里不需要更新路径、因为没有涉及到）
 * 3、点击进入节点编辑模式时，将节点信息数据传入到编辑界面中，同时再从编辑界面生成一个备份Science数据scBackup
 * 4、当打开了编辑界面的同时，拖动节点，会同时更新字典中的Science数据、scBackup中的数据，然后刷新界面显示的位置数据
 * 5、当在编辑界面点击了退出而不是提交的时候，使用scBackup的数据对字典中的Science数据进行覆盖（相当于撤销所有改动）
 * 6、当点击了提交按钮的时候，不做任何改动并关闭编辑界面，因为对界面的编辑相当于是对浅拷贝的编辑，也就是对生成的Science字典中的数据进行直接修改
 * 7、正因为对字典数据直接进行修改，在输入的时候要进行严格的格式判断，如果输入的内容被判断为不合法，则不应读取非法的输入值
 * 8、但是正因为直接对字典数据进行修改，所以说，提交按钮是没有实际作用的
 * 9、而且需要做一个编辑撤销的功能，也就是在界面生成的时候创建一个栈，将修改过的数据都压到栈中，撤销行为发生的时候再把栈推出
 */
public class PanelScienceEdit : MonoBehaviour
{
    public Science sc { get; set; }
    public Node node { get; set; }
    public GameObject content;
    public GameObject TTITPrefab;
    TipText debug;
    Button submit;

    InputField i_id;
    InputField i_subtype;
    InputField i_name;

    InputField i_x;
    InputField i_y;
    Dropdown d_color;
    Dropdown d_scale;
    InputField i_icon;
    InputField i_pre;
    InputField i_prePath;
    InputField i_trigger;

    InputField i_material;
    InputField i_time;

    InputField i_build;
    InputField i_unbuild;

    InputField i_desc;
    InputField i_descAdd;


    private void initComponentAndScience()
    {
        //绑定组件
        i_id = transform.Find("id").GetComponent<InputField>();
        i_subtype = transform.Find("subtype").GetComponent<InputField>();
        i_name = transform.Find("name").GetComponent<InputField>();
        i_x = transform.Find("x").GetComponent<InputField>();
        i_y = transform.Find("y").GetComponent<InputField>();
        d_color = transform.Find("dp_icon_color").GetComponent<Dropdown>();
        d_scale = transform.Find("dp_icon&line").GetComponent<Dropdown>();
        i_icon = transform.Find("moudleId").GetComponent<InputField>();
        i_pre = transform.Find("pre").GetComponent<InputField>();
        i_prePath = transform.Find("pre_path").GetComponent<InputField>();
        i_trigger = transform.Find("trigger").GetComponent<InputField>();
        i_material = transform.Find("s_material").GetComponent<InputField>();
        i_time = transform.Find("time").GetComponent<InputField>();
        i_build = transform.Find("build_unlock").GetComponent<InputField>();
        i_unbuild = transform.Find("unbuild_unlock").GetComponent<InputField>();
        i_desc = transform.Find("detail").GetComponent<InputField>();
        i_descAdd = transform.Find("detail2").GetComponent<InputField>();
        //获取携带的Science，并更新值
        i_id.text = sc.Id.ToString();
        i_subtype.text = sc.SubType.ToString();
        i_name.text = sc.Name;
        i_x.text = sc.HexGridX.ToString("F3");
        i_y.text = sc.HexGridY.ToString("F3");
        d_color.value = sc.IconColor - 1;
        d_scale.value = sc.IconScale switch
        {
            Constants.NodeScale.Large => 0,
            Constants.NodeScale.Middle => 1,
            Constants.NodeScale.Small => 2,
            _ => 1,
        };
        i_icon.text = sc.ModuleId.ToString();
        i_pre.text = sc.Pre_technology.ToString();
        i_prePath.text = sc.PathNode.ToString();
        i_trigger.text = sc.Trigger_technology.ToString();
        i_material.text = sc.S_Materials.ToString();
        i_time.text = sc.Time.ToString();
        i_build.text = sc.Building_unlock.ToString();
        i_unbuild.text = sc.NonBuilding_unlock.ToString();
        i_desc.text = sc.Detail.ToString();
        i_descAdd.text = sc.Detail_2.ToString();

        debug = GameObject.Find("tiptext").GetComponent<TipText>();

        submit = transform.Find("btn_submit").GetComponent<Button>();

        updateTTIContent();
    }
    void Start()
    {
        initComponentAndScience();
        LoadScience();
    }

    #region 初始化
    void LoadScience()
    {
    }
    #endregion

    void Update()
    {

    }

    #region 内容与UI应用
    // 添加一个公共方法，用于外部刷新 UI
    public void RefreshUI()
    {
        if (sc != null)
        {
            if (i_prePath != null)
            {
                i_prePath.text = sc.PathNode;
            }
            // 使用三位小数显示
            i_x.text = sc.HexGridX.ToString("F1");
            i_y.text = sc.HexGridY.ToString("F1");
        }
    }

    int oldId;
    public void UpdateId()
    {
        oldId = sc.Id;
        int id = int.Parse(i_id.text);
        if (id == oldId) return;
        if (DataManager.Instance.ScienceDict.ContainsKey(id))
        {
            debug.LogError($"{id}已经存在");
            i_id.transform.Find("Text (Legacy)").GetComponent<Text>().color = Color.red;
        }
        else if (i_id.text == "-1")
        {
            debug.LogError($"{id}不能是id");
            i_id.transform.Find("Text (Legacy)").GetComponent<Text>().color = Color.red;
        }
        else
        {
            //更新字典内容
            sc.Id = id;
            DataManager.Instance.UpdateScienceId(oldId, id);
            //更新panel字体颜色
            i_id.transform.Find("Text (Legacy)").GetComponent<Text>().color = Color.black;
            //更新nodeid的文本
            node.transform.Find("text_up").GetComponent<TextMesh>().text = sc.Id.ToString();
            //更新node名字
            node.name = id.ToString();
            //更新自身射线的名字
            for (int i = 0; i < node.transform.childCount; i++)
            {
                GameObject child = node.transform.GetChild(i).gameObject;
                if (child.tag is "NodeLine")
                {
                    child.name = $"{child.name.Split("->")[0]}->{id}";
                }
            }

            //更新后继节点中，前驱字段的id
            //更新后继节点中，前驱路径字段的id
            foreach (var idAfter in sc.After_technology)
            {
                DataManager.Instance.ScienceDict.TryGetValue(idAfter, out var scAfter);
                if (scAfter != null)
                {
                    scAfter.Pre_technology = scAfter.Pre_technology.ReplacePreTech(oldId.ToString(), id.ToString());
                    scAfter.PathNode = scAfter.PathNode.ReplacePathNode(oldId.ToString(), id.ToString());
                }
            }

            //更新前置sc的Afternode中，自己的id
            sc.Pre_technology.Split("|").ToList().ForEach(idpre =>
            {
                DataManager.Instance.ScienceDict.TryGetValue(int.Parse(idpre), out var scpre);
                if (scpre != null)
                {
                    scpre.After_technology.Remove(oldId);
                    scpre.After_technology.Add(id);
                }
            });
            //更新panel名字
            transform.name = $"{id}(Edit)";
        }

    }

    public void UpdatePrePath()
    {
        i_prePath.text = i_prePath.text.Replace("，", ",");

        // 用解析器验证格式是否合法
        string input = i_prePath.text.Trim();
        bool isValid = false;

        if (input == "-1")
        {
            isValid = true;
        }
        else
        {
            try
            {
                var connections = input.ParsePathConnections();
                // 只要能解析出至少一个连接，或者输入确实是合法的空中间点格式
                isValid = connections.Count > 0;

                // 额外检查：每个连接的 PreId 必须是有效数字
                foreach (var conn in connections)
                {
                    if (conn.PreId == 0 && !input.Contains("0,") && !input.StartsWith("0:"))
                    {
                        // PreId 为 0 但输入中没有明确的 0，说明解析出了默认值
                        isValid = false;
                        break;
                    }
                }
            }
            catch
            {
                isValid = false;
            }
        }

        if (isValid)
        {
            i_prePath.transform.Find("Text (Legacy)").GetComponent<Text>().color = Color.black;
            sc.PathNode = input;
            if (sc.PathNode != "-1")
            {
                // 前置校验
                var connections = sc.PathNode.ParsePathConnections();
                List<string> prePath = sc.Pre_technology.Split("|").ToList();

                bool have = false;
                foreach (var conn in connections)
                {
                    if (prePath.Contains(conn.PreId.ToString()))
                    {
                        have = true;
                        break;
                    }
                }
                if (!have)
                {
                    debug.LogWarning($"节点{sc.Id}引用了不存在的前置路径");
                }
            }

            node.UpdateNodeAppearance();
            node.ClearAnchor();
            node.UpdateLineAnchor();
        }
        else
        {
            i_prePath.transform.Find("Text (Legacy)").GetComponent<Text>().color = Color.red;
            debug.LogError($"路径格式不正确");
        }
    }



    public void UpdatePrePath(string path)
    {
        i_prePath.text = path;
    }

    public void UpdatePre()
    {
        if (Regex.IsMatch(i_pre.text, Constants.RegexPatterns.PreTechnology))
        {
            i_pre.transform.Find("Text (Legacy)").GetComponent<Text>().color = Color.black;
            //更新旧的后继节点
            string oldpre = sc.Pre_technology.ToString();
            string newpre = i_pre.text;
            var o = oldpre.Split("|").ToList();
            var n = newpre.Split("|").ToList();
            //输入内容校验
            foreach (var item in n)
            {
                if (item != "-1" && !DataManager.Instance.ScienceDict.ContainsKey(int.Parse(item)))
                {
                    debug.LogError($"当前所有节点中不存在一个id为{item}的节点。");
                    return;
                }
            }

            foreach (var oldsc in o)
            {
                DataManager.Instance.ScienceDict.TryGetValue(int.Parse(oldsc), out Science outSc);
                if (outSc is not null && outSc.After_technology.Contains(sc.Id))
                {
                    outSc.After_technology.Remove(sc.Id);
                }
            }
            foreach (var newSc in n)
            {
                DataManager.Instance.ScienceDict.TryGetValue(int.Parse(newSc), out Science outSc);
                if (outSc is not null)
                {
                    outSc.After_technology.Add(sc.Id);

                }
            }
            sc.Pre_technology = newpre;
            node.UpdateNodeAppearance();
        }
        else
        {
            i_pre.transform.Find("Text (Legacy)").GetComponent<Text>().color = Color.red;
            debug.LogError($"前置输入格式有误");
        }
    }

    public void UpdateNodeColor()
    {
        sc.IconColor = d_color.value + 1;
        node.UpdateNodeAppearance();
        var t = GameObject.Find("Tilemap");
        foreach (var item in sc.After_technology)
        {
            t.transform.Find(item.ToString()).GetComponent<Node>().UpdateNodeAppearance();
        }
    }

    public void UpdateNodeScale()
    {
        switch (d_scale.value)
        {
            //大
            case 0: sc.LineScale = Constants.LineWidth.Thick; sc.IconScale = Constants.NodeScale.Large; break;
            //中
            case 1: sc.LineScale = Constants.LineWidth.Medium; sc.IconScale = Constants.NodeScale.Middle; break;
            //小
            case 2: sc.LineScale = Constants.LineWidth.Thin; sc.IconScale = Constants.NodeScale.Small; ; break;
            default:
                break;
        }
        node.UpdateNodeAppearance();
    }

    public void UpdateIcon()
    {
        sc.ModuleId = int.Parse(i_icon.text);
        node.UpdateNodeAppearance();
    }

    public void UpdateTrigger()
    {
        sc.Trigger_technology = i_trigger.text;
    }

    public void UpdateMaterials()
    {
        //TODO 需求材料
        sc.S_Materials = i_material.text;
    }

    public void UpdateTime()
    {
        //TODO  研究时间
        sc.Time = float.Parse(i_time.text);
    }

    public void UpdateUnlockBuilding()
    {
        EventCenter.Instance.TriggerTechTreeItemUpdate(sc.Building_unlock, i_build.text);
        sc.Building_unlock = i_build.text;
        updateTTIContent();
    }

    public void UpdateUnlockNoBuilding()
    {
        EventCenter.Instance.TriggerTechTreeItemUpdate(sc.NonBuilding_unlock, i_unbuild.text);
        sc.NonBuilding_unlock = i_unbuild.text;
        updateTTIContent();
    }

    private void updateTTIContent()
    {
        //清空content原有的内容
        if (content.transform.childCount != 0)
        {
            List<GameObject> list = new();
            for (int i = 0; i < content.transform.childCount; i++)
            {
                list.Add(content.transform.GetChild(i).gameObject);
            }
            foreach (var item in list)
            {
                DestroyImmediate(item.gameObject);
            }
        }
        //为sc中有的解锁项进行显示
        foreach (var id in sc.Building_unlock.ToList())
        {
            DataManager.Instance.TechTreeItemDict.TryGetValue(id, out var tti);
            if (tti != null)
            {
                if (!content.transform.Find(tti.Id.ToString()))
                {
                    GameObject o = Instantiate(tti.GO, content.transform);
                }
            }
        }
        foreach (var id in sc.NonBuilding_unlock.ToList())
        {
            DataManager.Instance.TechTreeItemDict.TryGetValue(id, out var tti);
            if (tti != null)
            {
                if (!content.transform.Find(tti.Id.ToString()))
                {
                    GameObject o = Instantiate(tti.GO, content.transform);
                }
            }
        }
        //如果有解锁项，则显示
        if (content.transform.childCount != 0)
        {
            transform.Find("sv_TTI").gameObject.SetActive(true);
        }
        else
        {
            transform.Find("sv_TTI").gameObject.SetActive(false);
        }
    }

    public void UpdateDescribe()
    {
        sc.Detail = i_desc.text;
    }

    public void UpdateAdditionNote()
    {
        sc.Detail_2 = i_descAdd.text;
    }

    public void UpdateName()
    {
        sc.Name = i_name.text;
        node.UpdateNodeAppearance();
    }

    #endregion


    #region 鼠标拖动更新位置
    /// <summary>
    /// 给鼠标拖动用
    /// </summary>
    public void UpdatePositionByDrag(Vector2 worldPos)
    {
        sc.HexGridX = (float)System.Math.Round(worldPos.x, 3);
        sc.HexGridY = (float)System.Math.Round(worldPos.y, 3);
        i_x.text = sc.HexGridX.ToString("F3");
        i_y.text = sc.HexGridY.ToString("F3");
    }
    /// <summary>
    /// 兼容旧的 Vector2Int 调用（已废弃）
    /// </summary>
    [System.Obsolete("请使用 UpdatePositionByDrag(Vector2) 版本")]
    public void UpdatePositionByDrag(Vector2Int pos)
    {
        UpdatePositionByDrag(new Vector2(pos.x, pos.y));
    }
    #endregion

    #region 面板按钮功能
    /// <summary>
    /// 保存编辑内容到Science字典
    /// </summary>
    public void SavePanel()
    {


        Destroy(transform.gameObject);
    }

    /// <summary>
    /// 关闭
    /// </summary>
    public void DestoryPanel()
    {
        node.ClearAnchor();
        node.SetSelectStyle(false);

        // 通知InputManager清除面板引用
        var inputManager = GameObject.Find(Constants.GameObjectNames.MainManager)
            ?.GetComponent<InputManager>();
        if (inputManager != null)
        {
            inputManager.SetCurrentEditPanel(null);
        }

        Destroy(transform.gameObject);
    }
    #endregion

    /// <returns>当前界面的Science,相当于深拷贝</returns>
    private Science GetPanelScience()
    {
        Science save = new(
            int.Parse(i_id.text),
            int.Parse(i_subtype.text),
            int.Parse(i_icon.text),
            d_scale.value switch
            {
                0 => Constants.NodeScale.Large,
                1 => Constants.NodeScale.Middle,
                2 => Constants.NodeScale.Small,
                _ => Constants.NodeScale.Middle,
            },
            d_scale.value switch
            {
                0 => Constants.LineWidth.Thick,
                1 => Constants.LineWidth.Medium,
                2 => Constants.LineWidth.Thin,
                _ => Constants.LineWidth.Medium,
            },
            i_name.text,
            i_desc.text,
            i_descAdd.text,
            i_build.text,
            i_unbuild.text,
            float.Parse(i_x.text),   // 改为 float
            float.Parse(i_y.text),   // 改为 float
            i_pre.text,
            i_prePath.text,
            i_material.text,
            float.Parse(i_time.text),
            d_color.value + 1,
            i_trigger.text
        );
        return save;
    }

}
