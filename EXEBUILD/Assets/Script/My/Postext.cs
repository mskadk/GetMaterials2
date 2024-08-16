using UnityEngine;
using UnityEngine.UI;

namespace Assets.Script.My
{
    /// <summary>
    /// 屏幕左下角显示鼠标在那个六边形位置的坐标
    /// </summary>
    public class Postext : MonoBehaviour
    {
        Camera cam;
        Grid grid;
        // Use this for initialization
        void Start()
        {
            cam = GameObject.Find("CameraSence").GetComponent<Camera>();
            grid = GameObject.Find("Grid").GetComponent<Grid>();
        }

        // Update is called once per frame
        void Update()
        {
            var pos = grid.WorldToCell(cam.ScreenToWorldPoint(Input.mousePosition));
            GetComponent<Text>().text = $"({pos.y},{pos.x})";
        }
    }
}