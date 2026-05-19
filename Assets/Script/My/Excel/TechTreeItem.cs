using UnityEngine;

namespace Assets.Script.My.Excel
{
    public class TechTreeItem
    {

        string id;
        string name;
        string desc;
        string aimItem;
        GameObject go;

        public TechTreeItem(string id, string name, string desc, string aimItem)
        {
            this.id = id;
            this.name = name;
            this.desc = desc;
            this.aimItem = aimItem;
        }

        public string Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public string Desc { get => desc; set => desc = value; }
        public string AimItem { get => aimItem; set => aimItem = value; }
        public GameObject GO { get => go; set => go = value; }
    }
}
