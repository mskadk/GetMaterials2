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

    [Header("=== 辅助线设置 ===")]
    public bool 启用辅助线 = true;
    public Color 辅助线颜色 = new Color(0f, 1f, 1f, 0.5f); // 增加透明度可见性
    public float 辅助线长度 = 1000f; // 增加长度以覆盖屏幕
    [Range(0, 90)]
    public float 辅助线角度偏移 = 0f; // 默认为0，具体值在切换时设置

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
        // 初始化角度偏移
        OnGridTypeChanged(currentGridType);
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

        // 根据网格类型设置辅助线角度
        // Hexagon: 30度偏移 (垂直线 + 60度交叉) 或 0度偏移 (水平线 + 60度交叉)
        // Square: 0度 (水平垂直) + 45度 (对角)
        // Free: 0度 (水平垂直) + 45度 (对角)
        辅助线角度偏移 = newType switch
        {
            GridType.Hexagon => 30f,  // 六边形通常是竖直方向，偏移30度
            GridType.Square => 0f,    // 正方形从0度开始
            GridType.Free => 0f,      // 自由模式从0度开始
            _ => 0f
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

        // 2. 绘制移动辅助线（鼠标按下时）
        if (启用辅助线 && Input.GetMouseButton(0))
        {
            DrawAlignmentLines();
        }

        GL.PopMatrix();
    }
    #endregion

    #region 绘制六边形网格
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

    #region 绘制正方形网格
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
                // 使用 GetCellCenterLocal 获取中心
                Vector3 center = grid.GetCellCenterLocal(new Vector3Int(x, y, 0));
                DrawSquare(center, halfSize);
            }
        }

        // 原点用红色绘制
        if (drawZero)
        {
            GL.Color(Color.red);
            Vector3 zeroCenter = grid.GetCellCenterLocal(new Vector3Int(0, 0, 0));
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

    #region 自由模式绘制（坐标轴 + 稀疏参考点）
    private void DrawFreeGrid()
    {
        if (grid == null) return;

        GL.Begin(GL.LINES);

        // 自由模式只绘制坐标轴作为参考
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

        // 可选：绘制稀疏的点阵作为参考
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

    #region 辅助线绘制
    private void DrawAlignmentLines()
    {
        Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Vector3 centerPos;
        if (currentGridType == GridType.Free)
        {
            // Free模式：辅助线跟随鼠标位置
            centerPos = mousePos;
        }
        else if (currentGridType == GridType.Square)
        {
            // Square模式：吸附到 0.5 格子
            float step = grid.cellSize.x * 0.5f;
            float x = Mathf.Round(mousePos.x / step) * step;
            float y = Mathf.Round(mousePos.y / step) * step;
            centerPos = new Vector3(x, y, 0);
        }
        else // Hexagon
        {
            // Hexagon模式：辅助线跟随最近的网格中心
            Vector3Int cellPos = grid.WorldToCell(mousePos);
            Vector3 center = grid.GetCellCenterWorld(cellPos);
            centerPos = new Vector3(center.x, center.y, 0);
        }
        GL.Begin(GL.LINES);
        GL.Color(辅助线颜色);
        // 设置线条数量和角度间隔
        int lineCount;
        float angleStep;
        float startAngle = 辅助线角度偏移;
        switch (currentGridType)
        {
            case GridType.Hexagon:
                lineCount = 3; // 3条线，6个方向
                angleStep = 60f;
                break;
            case GridType.Square:
                lineCount = 4; // 4条线 (水平垂直 + 对角)
                angleStep = 45f;
                break;
            case GridType.Free:
                lineCount = 4; // 4条线 (米字型)
                angleStep = 45f;
                startAngle = 0f;
                break;
            default:
                lineCount = 3;
                angleStep = 60f;
                break;
        }
        for (int i = 0; i < lineCount; i++)
        {
            float angle = (startAngle + i * angleStep) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            GL.Vertex(centerPos - dir * 辅助线长度);
            GL.Vertex(centerPos + dir * 辅助线长度);
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
