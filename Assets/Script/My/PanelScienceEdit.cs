using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

public class PanelScienceEdit : MonoBehaviour
{
    public Science sc { get; set; }
    public MainManager mm;

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
    // Start is called before the first frame update
    void Start()
    {
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

        mm = GameObject.Find("MainManager").GetComponent<MainManager>();
        initEditPanel();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdatePositionTextImmediate(Vector2Int pos)
    {
        sc.HexGridX = pos.y;
        sc.HexGridY = pos.x;
        i_x.text = sc.HexGridX.ToString();
        i_y.text = sc.HexGridY.ToString();

    }

    void initEditPanel()
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

    }

    public void DestoryPanel()
    {
        mm.setEditFalse();
        Destroy(transform.gameObject);
    }

    public void SubmitPanel()
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
            d_color.value +1,
            i_trigger.text
            );
    }
}
