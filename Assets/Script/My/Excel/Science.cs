using Assets.Script.My.Extention;
using System;
using System.Collections.Generic;
using System.Linq;

public class Science
{
    string id;
    int subType;
    int moduleId;
    float iconScale;
    float lineScale;
    string name;
    string detail;
    string detail_2;
    string building_unlock;
    string nonBuilding_unlock;
    float worldPosX;  // 原 hexGridX，现在是世界坐标X
    float worldPosY;  // 原 hexGridY，现在是世界坐标Y
    string pre_technology;
    string pathNode;
    string s_Materials;
    float time;
    int iconColor;
    string trigger_technology;
    bool apply;

    HashSet<string> after_technology;

    #region 封装字段
    public string Id { get => id; set => id = value; }
    public int SubType { get => subType; set => subType = value; }
    public int ModuleId { get => moduleId; set => moduleId = value; }
    public float IconScale { get => iconScale; set => iconScale = value; }
    public float LineScale { get => lineScale; set => lineScale = value; }
    public string Name { get => name; set => name = value; }
    public string Detail { get => detail; set => detail = value; }
    public string Detail_2 { get => detail_2; set => detail_2 = value; }
    public string Building_unlock { get => building_unlock; set => building_unlock = value; }
    public string NonBuilding_unlock { get => nonBuilding_unlock; set => nonBuilding_unlock = value; }

    // 保持属性名不变，但类型改为float（世界坐标）
    public float HexGridX { get => worldPosX; set => worldPosX = value; }
    public float HexGridY { get => worldPosY; set => worldPosY = value; }

    // 新增便捷属性
    public UnityEngine.Vector2 WorldPosition
    {
        get => new UnityEngine.Vector2(worldPosX, worldPosY);
        set { worldPosX = value.x; worldPosY = value.y; }
    }

    public string Pre_technology { get => pre_technology; set => pre_technology = value; }
    public string PathNode { get => pathNode; set => pathNode = value; }
    public string S_Materials { get => s_Materials; set => s_Materials = value; }
    public float Time { get => time; set => time = value; }
    public int IconColor { get => iconColor; set => iconColor = value; }
    public string Trigger_technology { get => trigger_technology; set => trigger_technology = value; }
    public bool Apply { get => apply; set => apply = value; }
    public HashSet<string> After_technology { get => after_technology; set => after_technology = value; }
    #endregion

    #region 构造函数
    public Science()
    {
        after_technology = new HashSet<string>();
    }

    public Science(string id, int subType, int moduleId, float iconScale, float lineScale,
        string name, string detail, string detail_2, string building_unlock, string nonBuilding_unlock,
        float worldPosX, float worldPosY, string pre_technology, string pathNode,
        string s_materials, float time, int iconColor, string trigger_technology,
        bool apply = true)
    {
        this.id = id;
        this.subType = subType;
        this.moduleId = moduleId;
        this.iconScale = iconScale;
        this.lineScale = lineScale;
        this.name = name;
        this.detail = detail;
        this.detail_2 = detail_2;
        this.building_unlock = building_unlock;
        this.nonBuilding_unlock = nonBuilding_unlock;
        this.worldPosX = worldPosX;
        this.worldPosY = worldPosY;
        this.pre_technology = pre_technology;
        this.pathNode = pathNode;
        this.s_Materials = s_materials;
        this.time = time;
        this.iconColor = iconColor;
        this.trigger_technology = trigger_technology;
        this.apply = apply;
        this.after_technology = new HashSet<string>();
    }
    #endregion

    #region Getter Setter
    public List<string> GetPredecessorList(Science sc)
    {
        List<string> list = new();
        if (!sc.pre_technology.Equals("-1"))
        {
            foreach (var pre in sc.pre_technology.Split("|"))
            {
                list.Add(pre.Trim());
            }
        }
        return list;
    }

    public void AddSuccessor(Science sc, string id)
    {
        sc.after_technology.Add(id);
    }

    public List<string> GetSucccessorList(Science sc)
    {
        return sc.after_technology.ToList();
    }

    public override string ToString()
    {
        return
         $" {string.Format("{0,-5}", id)}\t" +
         $" {subType}\t" +
         $" {moduleId}\t" +
         $" {iconScale}\t" +
         $" {string.Format("{0,-5}", lineScale)}\t" +
         $" {string.Format("{0,-20}", name)}\t" +
         $" {string.Format("{0,-5}", detail)}\t" +
         $" {string.Format("{0,-5}", detail_2)}\t" +
         $" {string.Format("{0,-35}", building_unlock)}\t" +
         $" {string.Format("{0,-60}", nonBuilding_unlock)}\t" +
         $" {$"({worldPosX:F3},{worldPosY:F3})"}\t" +
         $" {pre_technology}\t" +
         $" {pathNode}\t" +
         $" {s_Materials}\t" +
         $" {time}\t" +
         $" {iconColor}\t" +
         $" {trigger_technology}\t" +
         $" {apply}";
    }

    internal string ParseString()
    {
        return $"{id}\t{subType}\t{moduleId}\t{iconScale}\t{lineScale}\t{name}\t{detail}\t{detail_2}\t{building_unlock}\t{nonBuilding_unlock}\t{worldPosX:F3}\t{worldPosY:F3}\t{pre_technology}\t{pathNode}\t{s_Materials}\t{time}\t{IconColor}\t{trigger_technology}\t{apply}";
    }
    #endregion
}
