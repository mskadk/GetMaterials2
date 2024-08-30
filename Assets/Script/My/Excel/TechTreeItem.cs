using UnityEngine;

namespace Assets.Script.My.Excel
{
    public class TechTreeItem
    {

        int id;
        string name;
        string desc;
        GameObject go;

        public TechTreeItem(int id, string name, string desc)
        {
            this.id = id;
            this.name = name;
            this.desc = desc;
        }

        public int Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public string Desc { get => desc; set => desc = value; }
        public GameObject GO { get => go; set => go = value; }
    }
}