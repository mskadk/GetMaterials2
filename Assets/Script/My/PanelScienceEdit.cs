using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

public class PanelScienceEdit : MonoBehaviour
{
    public Science sc { get; set; }
    Science scOld;
    public MainManager mm;
    TipText debug;

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

    Button submit;
    void Start()
    {
        debug = GameObject.Find("tiptext").GetComponent<TipText>();

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

        submit = transform.Find("btn_submit").GetComponent<Button>();

        mm = GameObject.Find("MainManager").GetComponent<MainManager>();
        LoadScience();
    }

    #region 初始化
    void LoadScience()
    {
        i_id.text = sc.Id.ToString();
        i_subtype.text = sc.SubType.ToString();
        i_name.text = sc.Name;
        i_x.text = sc.HexGridX.ToString();
        i_y.text = sc.HexGridY.ToString();
        d_color.value = sc.IconColor - 1;
        d_scale.value = sc.IconScale switch
        {
            1.5f => 0,
            .75f => 1,
            _ => 0,
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

        scOld = sc;
    }
    #endregion

    void Update()
    {

    }

    #region 内容校验
    public void CheckId()
    {
        int id = int.Parse(i_id.text);
        if (mm.ScienceDict.ContainsKey(id) || mm.tilemap.transform.Find(id.ToString()))
        {
            if (id == scOld.Id)
            {
                i_id.transform.Find("Text (Legacy)").GetComponent<Text>().color = Color.black;
                i_id.text = id.ToString();
            }
            else
            {
                debug.LogWarning($"{id}已经存在");
                i_id.transform.Find("Text (Legacy)").GetComponent<Text>().color = Color.red;

            }
        }
        else
        {
            i_id.transform.Find("Text (Legacy)").GetComponent<Text>().color = Color.black;
            i_id.text = id.ToString();
        }
    }
    #endregion

    #region 内容修改
    /// <summary>
    /// 给鼠标拖动用
    /// </summary>
    public void UpdatePositionTextImmediate(Vector2Int pos)
    {
        sc.HexGridX = pos.y;
        sc.HexGridY = pos.x;
        i_x.text = sc.HexGridX.ToString();
        i_y.text = sc.HexGridY.ToString();

    }

    /// <summary>
    /// 保存编辑内容到Science字典
    /// </summary>
    public void SavePanel()
    {
        Science newScience = ToScience();
        sc = newScience;
        LoadScience();
        debug.Log("修改成功喽~");
    }

    #endregion

    /// <summary>
    /// 关闭
    /// </summary>
    public void DestoryPanel()
    {
        sc = scOld;
        mm.setEditFalse();
        Destroy(transform.gameObject);
    }

    /// <summary>
    /// 将界面内容转为Science
    /// </summary>
    /// <returns>Science</returns>
    private Science ToScience()
    {
        Science save = new(
            int.Parse(i_id.text),
            int.Parse(i_subtype.text),
            int.Parse(i_icon.text),
            d_scale.value switch
            {
                0 => 1.5f,
                1 => .75f,
                _ => 1.5f,
            },
            d_scale.value switch
            {
                0 => 1.5f,
                1 => .75f,
                _ => 1.5f,
            },
            i_name.text,
            i_desc.text,
            i_descAdd.text,
            i_build.text,
            i_unbuild.text,
            int.Parse(i_x.text),
            int.Parse(i_y.text),
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
