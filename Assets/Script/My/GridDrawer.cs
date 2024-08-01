using System.Collections;
using UnityEngine;

public class GridDrawer : MonoBehaviour
{
    private Camera cam;
    private Grid grid;
    private float camLocalLeft;
    private float camLocalTop;
    private float camLocalRight;
    private float camLocalBottom;

    private Material lineMaterial;

    public float scale = 1f;
    public Color lineColor = Color.gray;
    private float hex_H;
    private float hex_L;
    [Tooltip("如需调试在运行前打开！")]
    public bool 调试信息 = false;
    public bool 渲染至游戏 = true;
    public bool 渲染至场景 = true;

    void Start()
    {
        cam = GetComponent<Camera>();
        grid = GameObject.Find("Grid").GetComponent<Grid>();
        if (调试信息)
        {
            StartCoroutine(debugSize());

        }
    }

    void Update()
    {
        updateGridSize();
    }

    private void updateGridSize()
    {
        hex_L = grid.cellSize.y / 2 * scale;
        hex_H = grid.cellSize.x / 2 * scale;

        camLocalTop = cam.orthographicSize;
        camLocalBottom = -cam.orthographicSize;
        camLocalLeft = -cam.orthographicSize * cam.aspect;
        camLocalRight = cam.orthographicSize * cam.aspect;
    }

    string oldlog = null;
    string newlog = null;
    IEnumerator debugSize()
    {
        while (true)
        {
            newlog = $"cam.orthographicSize:{cam.orthographicSize}\n" +
               //$"cam.pixel:{cam.pixelWidth},{cam.pixelHeight}\n" +
               //$"cam.scaledPixel:{cam.scaledPixelWidth},{cam.scaledPixelHeight}\n" +
               //$"cam.pixelRect.center:{cam.pixelRect.center}\n" +
               //$"cam.pixelRect:{cam.pixelRect}\n" +
               $"cam.aspect:{cam.aspect} \n+" +
               $"cam.position:{cam.transform.position}\n" +
               $"cam.sizeConner:{camLocalTop},{camLocalRight},{camLocalBottom},{camLocalLeft}";
            if (oldlog is not null && !newlog.Equals(oldlog))
            {
                Debug.Log(newlog);
            }
            oldlog = string.Copy(newlog);
            yield return new WaitForSeconds(1);
        }

    }

    #region 渲染网格开关
    //绘制在场景
    private void OnDrawGizmos()
    {
        if (cam == null || !渲染至场景) return;
        GL.PushMatrix();
        DrawHexagonGrid();
        GL.PopMatrix();
    }
    //绘制在游戏，和场景先后顺序不能反
    private void OnPostRender()
    {
        if (cam == null || !渲染至游戏) return;
        GL.PushMatrix();
        DrawHexagonGrid();
        GL.PopMatrix();
    }
    #endregion


    private void DrawHexagonGrid()
    {
        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        }
        lineMaterial.SetPass(0);

        GL.Begin(GL.LINES);
        GL.Color(lineColor);

        //// Use camera orthographic size to determine grid range if not set
        bool drawZero = false;
        Vector3Int lb = grid.LocalToCell(new(cam.transform.position.x + camLocalLeft - 1, cam.transform.position.y + camLocalBottom - 1, 0));
        Vector3Int rt = grid.LocalToCell(new(cam.transform.position.x + camLocalRight + 1, cam.transform.position.y + camLocalTop + 1, 0));
        for (int y = lb.y; y < rt.y; y++)
        {
            for (int x = lb.x; x < rt.x; x++)
            {
                //中心0点标红
                if (0 == (x | y | 0))
                {
                    drawZero = true;
                    continue;
                }
                Vector3 center = grid.CellToLocal(new Vector3Int(x, y, 0));
                DrawHexagon(center);
            }
        }

        //中心0点标红
        if (drawZero)
        {
            GL.Color(Color.red);
            DrawHexagon(grid.CellToLocal(new(0, 0, 0)));
        }


        GL.End();
    }

    private void DrawHexagon(Vector2 center)
    {
        Vector2[] vertices = new Vector2[6];
        vertices[0] = new(center.x - hex_L / 2, center.y + hex_H);
        vertices[1] = new(center.x + hex_L / 2, center.y + hex_H);
        vertices[2] = new(center.x + hex_L, center.y);
        vertices[3] = new(center.x + hex_L / 2, center.y - hex_H);
        vertices[4] = new(center.x - hex_L / 2, center.y - hex_H);
        vertices[5] = new(center.x - hex_L, center.y);

        //0-1,1-2... ...6-0
        for (int i = 0; i < 6; i++)
        {
            GL.Vertex(vertices[i]);
            GL.Vertex(vertices[(i + 1) % 6]);
        }
    }

    private void showPositionHex(GameObject o, float posx, float posy)
    {
        GameObject text = new($"({posx},{posy})");
        text.transform.SetParent(o.transform);
        text.transform.position = o.transform.position;
        text.AddComponent<MeshRenderer>();
        var tm = text.AddComponent<TextMesh>();
        tm.fontSize = 200;
        tm.characterSize = 0.015f;
        tm.text = $"{posx},{posy}";
        tm.color = Color.black;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
    }

}
