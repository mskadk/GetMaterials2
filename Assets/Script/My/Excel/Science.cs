using Assets.Script.My.Extention;
using System.Collections.Generic;
using System.Linq;

public class Science
{

    int id;
    int subType;
    int moduleId;
    float iconScale;
    float lineScale;
    string name;
    string detail;
    string detail_2;
    string building_unlock;
    string nonBuilding_unlock;
    int hexGridX;
    int hexGridY;
    string pre_technology;
    string pathNode;
    string s_Materials;
    float time;
    int iconColor;
    string trigger_technology;

    HashSet<int> after_technology;

    #region 封装字段
    public int Id { get => id; set => id = value; }
    public int SubType { get => subType; set => subType = value; }
    public int ModuleId { get => moduleId; set => moduleId = value; }
    public float IconScale { get => iconScale; set => iconScale = value; }
    public float LineScale { get => lineScale; set => lineScale = value; }
    public string Name { get => name; set => name = value; }
    public string Detail { get => detail; set => detail = value; }
    public string Detail_2 { get => detail_2; set => detail_2 = value; }
    public string Building_unlock { get => building_unlock; set => building_unlock = value; }
    public string NonBuilding_unlock { get => nonBuilding_unlock; set => nonBuilding_unlock = value; }
    public int HexGridX { get => hexGridX; set => hexGridX = value; }
    public int HexGridY { get => hexGridY; set => hexGridY = value; }
    public string Pre_technology { get => pre_technology; set => pre_technology = value; }
    public string PathNode { get => pathNode; set => pathNode = value; }
    public string S_Materials { get => s_Materials; set => s_Materials = value; }
    public float Time { get => time; set => time = value; }
    public int IconColor { get => iconColor; set => iconColor = value; }
    public string Trigger_technology { get => trigger_technology; set => trigger_technology = value; }
    public HashSet<int> After_technology { get => after_technology; set => after_technology = value; }
    #endregion

    #region 构造函数
    public Science()
    {
    }

    public Science(int id, int subType, int moduleId, float iconScale, float lineScale, string name, string detail, string detail_2, string building_unlock, string nonBuilding_unlock, int hexGridX, int hexGridY, string pre_technology, string pathNode, string s_materials, float time, int iconColor, string trigger_technology)
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
        this.hexGridX = hexGridX;
        this.hexGridY = hexGridY;
        this.pre_technology = pre_technology;
        this.pathNode = pathNode;
        this.s_Materials = s_materials;
        this.time = time;
        this.iconColor = iconColor;
        this.trigger_technology = trigger_technology;
        this.after_technology = new();
    }

    #endregion

    #region Getter Setter
    public List<int> GetPredecessorList(Science sc)
    {
        List<int> list = new();
        //判断Path_Node是否为空，不为空则进行拆分，
        if (!sc.pre_technology.Equals("-1"))
        {
            foreach (var pre in sc.pre_technology.Split("|"))
            {
                list.Add(int.Parse(pre.Split("_")[0]));
            }
        }
        return list;
    }

    public void AddSuccessor(Science sc, int id)
    {
        sc.after_technology.Add(id);
    }

    public List<int> GetSucccessorList(Science sc)
    {
        return sc.after_technology.ToList();
    }

    public override string ToString()
    {
        return
         $" {string.Format("{0,-5}", id)                                                 }\t"+
         $" {subType                                                                     }\t"+
         $" {moduleId                                                                    }\t"+
         $" {iconScale                                                                   }\t"+
         $" {string.Format("{0,-5}", lineScale)                                          }\t"+
         $" {string.Format("{0,-20}", name)                                              }\t"+
         $" {string.Format("{0,-5}", detail)}\t"+
         $" {string.Format("{0,-5}", detail_2)}\t"+
         $" {string.Format("{0,-35}", building_unlock)                                   }\t"+
         $" {string.Format("{0,-60}", nonBuilding_unlock)                                }\t"+
         $" {$"({hexGridX},{hexGridY})"                                                  }\t"+
         $" {pre_technology                                                              }\t"+
         $" {pathNode                                                                    }\t"+
         $" {s_Materials                                                                 }\t"+
         $" {time                                                                        }\t"+
         $" {iconColor                                                                   }\t"+
         $" {trigger_technology}" +
         $"";                                                       

    }



    #endregion

}
