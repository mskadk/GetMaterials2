using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class GridDrawer : MonoBehaviour
{
    private Camera cam;
    private Grid grid;
    private float camLocalLeft;
    private float camLocalTop;
    private float camLocalRight;
    private float camLocalBottom;

    public Material lineMaterial;
    [Range(0.2f, 5)]
    public float scale = 1f;
    public Color lineColor = Color.gray;
    private float hex_H;
    private float hex_L;
    [Tooltip("是否开启调试信息")]
    public bool 开启调试信息 = false;
    public bool 渲染游戏视图 = true;
    public bool 渲染场景视图 = true;

    [Header("=== 辅助对齐线 ===")]
    public bool 启用对齐线 = true;
    public Color 对齐线颜色 = new Color(0f, 1f, 1f, 0.5f); // 青色，半透明
    public float 对齐线长度 = 100f; // 射线长度，足够长以贯穿屏幕
    [Range(0, 90)]
    public float 对齐线角度偏移 = 30f; // 平顶六边形通常是30度，尖顶是0度

    void Start()
    {
        cam = GetComponent<Camera>();
        grid = GameObject.Find("Grid").GetComponent<Grid>();
        if (开启调试信息)
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
        // 根据你的DrawHexagon逻辑：
        // vertices[0] = new(center.x - hex_L / 2, center.y + hex_H);
        // vertices[1] = new(center.x + hex_L / 2, center.y + hex_H);
        // 这表明是一个 平顶(Flat-Top) 六边形布局，其邻居方向通常在 30°, 90°, 150°...

        hex_L = grid.cellSize.y / 2 * scale;
        hex_H = grid.cellSize.x / 2 * scale;

        camLocalTop = cam.orthographicSize;
        camLocalBottom = -cam.orthographicSize;
        camLocalLeft = -cam.orthographicSize * cam.aspect;
        camLocalRight = cam.orthographicSize * cam.aspect;
    }


    #region 渲染开关
    // 场景视图渲染
    private void OnDrawGizmos()
    {
        if (cam == null || !渲染场景视图) return;
        DrawAll();
    }
    // 游戏视图渲染
    private void OnRenderObject()
    {
        if (cam == null || !渲染游戏视图) return;
        // 确保只在当前相机的渲染过程中绘制
        if (Camera.current != cam) return;

        DrawAll();
    }

    private void DrawAll()
    {
        GL.PushMatrix();
        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        }
        lineMaterial.SetPass(0);

        // 1. 绘制网格
        DrawHexagonGrid();

        // 2. 绘制鼠标对齐辅助线
        if (启用对齐线 && Input.GetMouseButton(0))
        {
            DrawAlignmentLines();
        }

        GL.PopMatrix();
    }
    #endregion


    private void DrawHexagonGrid()
    {
        GL.Begin(GL.LINES);
        GL.Color(lineColor);

        bool drawZero = false;
        Vector3Int lb = grid.LocalToCell(new(cam.transform.position.x + camLocalLeft - 1, cam.transform.position.y + camLocalBottom - 1, 0));
        Vector3Int rt = grid.LocalToCell(new(cam.transform.position.x + camLocalRight + 1, cam.transform.position.y + camLocalTop + 1, 0));

        for (int y = lb.y; y < rt.y; y++)
        {
            for (int x = lb.x; x < rt.x; x++)
            {
                // 排除0点
                if (x == 0 && y == 0)
                {
                    drawZero = true;
                    continue;
                }
                Vector3 center = grid.CellToLocal(new Vector3Int(x, y, 0));
                DrawHexagon(center);
            }
        }

        // 单独绘制0点
        if (drawZero)
        {
            GL.Color(Color.red);
            DrawHexagon(grid.CellToLocal(new(0, 0, 0)));
        }

        GL.End();
    }

    /// <summary>
    /// 绘制鼠标所在的六向射线
    /// </summary>
    private void DrawAlignmentLines()
    {
        // 获取鼠标在世界空间的位置
        Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        // 获取鼠标所在的网格中心
        Vector3Int cellPos = grid.WorldToCell(mousePos);
        Vector3 centerPos = grid.CellToWorld(cellPos);

        GL.Begin(GL.LINES);
        GL.Color(对齐线颜色);

        // 绘制3条穿过中心的线（相当于6条射线）
        // i=0: 30度 (和 210度)
        // i=1: 90度 (和 270度)
        // i=2: 150度 (和 330度)
        for (int i = 0; i < 3; i++)
        {
            float angle = (对齐线角度偏移 + i * 60) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);

            // 从中心向两边延伸
            GL.Vertex(centerPos - dir * 对齐线长度);
            GL.Vertex(centerPos + dir * 对齐线长度);
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

        // 0-1, 1-2... ...6-0
        for (int i = 0; i < 6; i++)
        {
            GL.Vertex(vertices[i]);
            GL.Vertex(vertices[(i + 1) % 6]);
        }
    }

    #region Debug
    string oldlog = null;
    string newlog = null;
    IEnumerator debugSize()
    {
        while (true)
        {
            newlog = $"cam.orthographicSize:{cam.orthographicSize}\n" +
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
    #endregion
}
