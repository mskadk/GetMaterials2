using UnityEngine;
using UnityEngine.Tilemaps;

public enum GridType
{
    Hexagon,
    Square,
    Free
}

public class GridManager : MonoBehaviour
{
    public GridType CurrentGridType { get; private set; } = GridType.Square;

    // 网格尺寸配置
    public static readonly Vector3 HexCellSize = new Vector3(86.59766f, 100f, 100f);
    public static readonly Vector3 SquareCellSize = new Vector3(100f, 100f, 100f);
    public static readonly Vector3 FreeCellSize = new Vector3(100f, 100f, 100f);

    private static GridManager _instance;
    public static GridManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GridManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GridManager");
                    _instance = go.AddComponent<GridManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) Destroy(gameObject);
    }

    public void SetGridType(GridType type)
    {
        if (CurrentGridType == type) return;

        CurrentGridType = type;
        var grid = UIReferences.Instance.grid;
        if (grid == null) return;

        switch (type)
        {
            case GridType.Hexagon:
                grid.cellLayout = GridLayout.CellLayout.Hexagon;
                grid.cellSwizzle = GridLayout.CellSwizzle.YXZ;
                grid.cellSize = HexCellSize;
                break;
            case GridType.Square:
                grid.cellLayout = GridLayout.CellLayout.Rectangle;
                grid.cellSwizzle = GridLayout.CellSwizzle.YXZ;
                grid.cellSize = SquareCellSize;
                break;
            case GridType.Free:
                // Free模式使用矩形布局，但不强制对齐
                grid.cellLayout = GridLayout.CellLayout.Rectangle;
                grid.cellSwizzle = GridLayout.CellSwizzle.YXZ;
                grid.cellSize = FreeCellSize;
                break;
        }

        // 通知GridDrawer更新绘制模式
        var gridDrawer = UIReferences.Instance.camSence?.GetComponent<GridDrawer>();
        if (gridDrawer != null)
        {
            gridDrawer.OnGridTypeChanged(type);
        }

        // 刷新节点位置以匹配新的网格布局
        NodeManager.Instance.RefreshNodePositions();

        EventCenter.Instance.TriggerLogMessage($"已切换到 {GetGridTypeName(type)} 网格");
    }

    /// <summary>
    /// 根据当前网格类型，将世界坐标对齐到网格
    /// </summary>
    public Vector3 SnapToGrid(Vector3 worldPos)
    {
        var grid = UIReferences.Instance.grid;
        if (grid == null) return worldPos;
        // 1. 自由模式：不吸附
        if (CurrentGridType == GridType.Free)
        {
            return worldPos;
        }
        // 2. 正方形模式：吸附到成倍率的网格
        if (CurrentGridType == GridType.Square)
        {
            // 获取当前格子大小（假设XY一致）
            float cellSize = grid.cellSize.x;
            float step = cellSize * 0.25f; // 吸附步长
            // 数学四舍五入计算
            float x = Mathf.Round(worldPos.x / step) * step;
            float y = Mathf.Round(worldPos.y / step) * step;
            return new Vector3(x, y, 0);
        }
        // 3. 六边形模式：保持原有的中心吸附
        // 使用 GetCellCenterWorld 确保获取的是六边形准确中心
        Vector3Int cellPos = grid.WorldToCell(worldPos);
        Vector3 center = grid.GetCellCenterWorld(cellPos);
        return new Vector3(center.x, center.y, 0);
    }

    /// <summary>
    /// 获取鼠标位置对应的网格坐标
    /// </summary>
    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        var grid = UIReferences.Instance.grid;
        if (grid == null) return Vector3Int.zero;
        return grid.WorldToCell(worldPos);
    }

    /// <summary>
    /// 获取网格坐标对应的世界坐标
    /// </summary>
    public Vector3 CellToWorld(Vector3Int cellPos)
    {
        var grid = UIReferences.Instance.grid;
        if (grid == null) return Vector3.zero;
        return grid.CellToWorld(cellPos);
    }

    /// <summary>
    /// 检查当前是否启用网格对齐
    /// </summary>
    public bool IsSnapEnabled => CurrentGridType != GridType.Free;

    private string GetGridTypeName(GridType type)
    {
        return type switch
        {
            GridType.Hexagon => "六边形",
            GridType.Square => "正方形",
            GridType.Free => "自由",
            _ => type.ToString()
        };
    }
}
