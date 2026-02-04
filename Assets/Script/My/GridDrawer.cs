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

    public Material lineMaterial;
    [Range(0.2f, 5)]
    public float scale = 1f;
    public Color lineColor = Color.gray;

    // 六边形参数
    private float hex_H;
    private float hex_L;

    // 正方形参数
    private float squareSize;

    [Tooltip("是否输出调试信息")]
    public bool 开启调试信息 = false;
    public bool 渲染游戏视图 = true;
    public bool 渲染场景视图 = true;

    [Header("=== 对齐线设置 ===")]
    public bool 启用对齐线 = true;
    public Color 对齐线颜色 = new Color(0f, 1f, 1f, 0.1f);
    public float 对齐线长度 = 200f;
    [Range(0, 90)]
    public float 对齐线角度偏移 = 60f;

    // 当前网格类型
    private GridType currentGridType = GridType.Hexagon;

    void Start()
    {
        cam = GetComponent<Camera>();
        grid = GameObject.Find("Grid")?.GetComponent<Grid>();
        if (开启调试信息)
        {
            StartCoroutine(debugSize());
        }
    }

    void Update()
    {
        UpdateGridSize();
    }

    /// <summary>
    /// 当网格类型改变时调用
    /// </summary>
    public void OnGridTypeChanged(GridType newType)
    {
        currentGridType = newType;

        // 根据网格类型调整对齐线角度
        对齐线角度偏移 = newType switch
        {
            GridType.Hexagon => 60f,  // 六边形：60度间隔
            GridType.Square => 45f,   // 正方形：45度间隔（对角线+正交）
            GridType.Free => 45f,     // 自由模式：默认45度
            _ => 60f
        };
    }

    private void UpdateGridSize()
    {
        if (grid == null) return;

        // 六边形参数
        hex_L = grid.cellSize.y / 2 * scale;
        hex_H = grid.cellSize.x / 2 * scale;

        // 正方形参数
        squareSize = grid.cellSize.x * scale;

        camLocalTop = cam.orthographicSize;
        camLocalBottom = -cam.orthographicSize;
        camLocalLeft = -cam.orthographicSize * cam.aspect;
        camLocalRight = cam.orthographicSize * cam.aspect;
    }

    #region 渲染入口
    private void OnDrawGizmos()
    {
        if (cam == null || !渲染场景视图) return;
        DrawAll();
    }

    private void OnRenderObject()
    {
        if (cam == null || !渲染游戏视图) return;
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
        switch (currentGridType)
        {
            case GridType.Hexagon:
                DrawHexagonGrid();
                break;
            case GridType.Square:
                DrawSquareGrid();
                break;
            case GridType.Free:
                DrawFreeGrid();
                break;
        }

        // 2. 绘制对齐线（鼠标按下时）
        if (启用对齐线 && Input.GetMouseButton(0))
        {
            DrawAlignmentLines();
        }

        GL.PopMatrix();
    }
    #endregion

    #region 六边形网格绘制
    private void DrawHexagonGrid()
    {
        if (grid == null) return;

        GL.Begin(GL.LINES);
        GL.Color(lineColor);

        bool drawZero = false;
        Vector3Int lb = grid.LocalToCell(new Vector3(cam.transform.position.x + camLocalLeft - 1, cam.transform.position.y + camLocalBottom - 1, 0));
        Vector3Int rt = grid.LocalToCell(new Vector3(cam.transform.position.x + camLocalRight + 1, cam.transform.position.y + camLocalTop + 1, 0));

        for (int y = lb.y; y < rt.y; y++)
        {
            for (int x = lb.x; x < rt.x; x++)
            {
                if (x == 0 && y == 0)
                {
                    drawZero = true;
                    continue;
                }
                Vector3 center = grid.CellToLocal(new Vector3Int(x, y, 0));
                DrawHexagon(center);
            }
        }

        if (drawZero)
        {
            GL.Color(Color.red);
            DrawHexagon(grid.CellToLocal(new Vector3Int(0, 0, 0)));
        }

        GL.End();
    }

    private void DrawHexagon(Vector2 center)
    {
        Vector2[] vertices = new Vector2[6];
        vertices[0] = new Vector2(center.x - hex_L / 2, center.y + hex_H);
        vertices[1] = new Vector2(center.x + hex_L / 2, center.y + hex_H);
        vertices[2] = new Vector2(center.x + hex_L, center.y);
        vertices[3] = new Vector2(center.x + hex_L / 2, center.y - hex_H);
        vertices[4] = new Vector2(center.x - hex_L / 2, center.y - hex_H);
        vertices[5] = new Vector2(center.x - hex_L, center.y);

        for (int i = 0; i < 6; i++)
        {
            GL.Vertex(vertices[i]);
            GL.Vertex(vertices[(i + 1) % 6]);
        }
    }
    #endregion

    #region 正方形网格绘制
    private void DrawSquareGrid()
    {
        if (grid == null) return;

        GL.Begin(GL.LINES);
        GL.Color(lineColor);

        float halfSize = squareSize / 2;

        Vector3Int lb = grid.LocalToCell(new Vector3(cam.transform.position.x + camLocalLeft - 1, cam.transform.position.y + camLocalBottom - 1, 0));
        Vector3Int rt = grid.LocalToCell(new Vector3(cam.transform.position.x + camLocalRight + 1, cam.transform.position.y + camLocalTop + 1, 0));

        bool drawZero = false;

        for (int y = lb.y; y <= rt.y; y++)
        {
            for (int x = lb.x; x <= rt.x; x++)
            {
                if (x == 0 && y == 0)
                {
                    drawZero = true;
                    continue;
                }
                Vector3 center = grid.CellToLocal(new Vector3Int(x, y, 0));
                DrawSquare(center, halfSize);
            }
        }

        // 原点用红色绘制
        if (drawZero)
        {
            GL.Color(Color.red);
            Vector3 zeroCenter = grid.CellToLocal(new Vector3Int(0, 0, 0));
            DrawSquare(zeroCenter, halfSize);
        }

        GL.End();
    }

    private void DrawSquare(Vector3 center, float halfSize)
    {
        Vector2[] vertices = new Vector2[4];
        vertices[0] = new Vector2(center.x - halfSize, center.y + halfSize); // 左上
        vertices[1] = new Vector2(center.x + halfSize, center.y + halfSize); // 右上
        vertices[2] = new Vector2(center.x + halfSize, center.y - halfSize); // 右下
        vertices[3] = new Vector2(center.x - halfSize, center.y - halfSize); // 左下

        for (int i = 0; i < 4; i++)
        {
            GL.Vertex(vertices[i]);
            GL.Vertex(vertices[(i + 1) % 4]);
        }
    }
    #endregion

    #region 自由模式网格绘制（仅绘制参考线）
    private void DrawFreeGrid()
    {
        if (grid == null) return;

        GL.Begin(GL.LINES);

        // 自由模式只绘制坐标轴参考线
        Color axisColor = new Color(lineColor.r, lineColor.g, lineColor.b, lineColor.a * 0.5f);
        GL.Color(axisColor);

        // X轴（水平线）
        GL.Vertex(new Vector3(cam.transform.position.x + camLocalLeft, 0, 0));
        GL.Vertex(new Vector3(cam.transform.position.x + camLocalRight, 0, 0));

        // Y轴（垂直线）
        GL.Vertex(new Vector3(0, cam.transform.position.y + camLocalBottom, 0));
        GL.Vertex(new Vector3(0, cam.transform.position.y + camLocalTop, 0));

        // 原点标记
        GL.Color(Color.red);
        float markerSize = 0.2f * scale;
        // 小十字
        GL.Vertex(new Vector3(-markerSize, 0, 0));
        GL.Vertex(new Vector3(markerSize, 0, 0));
        GL.Vertex(new Vector3(0, -markerSize, 0));
        GL.Vertex(new Vector3(0, markerSize, 0));

        GL.End();

        // 可选：绘制淡化的网格点
        DrawFreeGridDots();
    }

    private void DrawFreeGridDots()
    {
        // 绘制稀疏的参考点
        GL.Begin(GL.LINES);
        Color dotColor = new Color(lineColor.r, lineColor.g, lineColor.b, lineColor.a * 0.3f);
        GL.Color(dotColor);

        float spacing = grid.cellSize.x * scale;
        float dotSize = 0.05f * scale;

        int startX = Mathf.FloorToInt((cam.transform.position.x + camLocalLeft) / spacing);
        int endX = Mathf.CeilToInt((cam.transform.position.x + camLocalRight) / spacing);
        int startY = Mathf.FloorToInt((cam.transform.position.y + camLocalBottom) / spacing);
        int endY = Mathf.CeilToInt((cam.transform.position.y + camLocalTop) / spacing);

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                Vector3 pos = new Vector3(x * spacing, y * spacing, 0);
                // 绘制小十字作为参考点
                GL.Vertex(new Vector3(pos.x - dotSize, pos.y, 0));
                GL.Vertex(new Vector3(pos.x + dotSize, pos.y, 0));
                GL.Vertex(new Vector3(pos.x, pos.y - dotSize, 0));
                GL.Vertex(new Vector3(pos.x, pos.y + dotSize, 0));
            }
        }

        GL.End();
    }
    #endregion

    #region 对齐线绘制
    private void DrawAlignmentLines()
    {
        Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Vector3 centerPos;

        if (currentGridType == GridType.Free)
        {
            // Free模式：对齐线跟随鼠标位置
            centerPos = mousePos;
        }
        else
        {
            // 其他模式：对齐线跟随网格中心
            Vector3Int cellPos = grid.WorldToCell(mousePos);
            // 这里也改为 GetCellCenterWorld
            Vector3 center = grid.GetCellCenterWorld(cellPos);
            centerPos = new Vector3(center.x, center.y, 0);
        }

        GL.Begin(GL.LINES);
        GL.Color(对齐线颜色);

        int lineCount = currentGridType switch
        {
            GridType.Hexagon => 3,  // 六边形：3条线（6个方向）
            GridType.Square => 4,   // 正方形：4条线（8个方向，含对角线）
            GridType.Free => 2,     // 自由模式：2条线（水平+垂直）
            _ => 3
        };

        float angleStep = currentGridType switch
        {
            GridType.Hexagon => 60f,
            GridType.Square => 45f,
            GridType.Free => 90f,
            _ => 60f
        };

        for (int i = 0; i < lineCount; i++)
        {
            float angle = (对齐线角度偏移 + i * angleStep) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);

            GL.Vertex(centerPos - dir * 对齐线长度);
            GL.Vertex(centerPos + dir * 对齐线长度);
        }

        GL.End();
    }
    #endregion

    #region Debug
    string oldlog = null;
    string newlog = null;
    IEnumerator debugSize()
    {
        while (true)
        {
            newlog = $"cam.orthographicSize:{cam.orthographicSize}\n" +
               $"cam.aspect:{cam.aspect}\n" +
               $"cam.position:{cam.transform.position}\n" +
               $"cam.sizeConner:{camLocalTop},{camLocalRight},{camLocalBottom},{camLocalLeft}\n" +
               $"gridType:{currentGridType}";
            if (oldlog != null && !newlog.Equals(oldlog))
            {
                Debug.Log(newlog);
            }
            oldlog = string.Copy(newlog);
            yield return new WaitForSeconds(1);
        }
    }
    #endregion
}
