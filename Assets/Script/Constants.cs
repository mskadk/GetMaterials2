/// <summary>
/// 全局常量定义
/// </summary>
public static class Constants
{
    /// <summary>
    /// Tag 常量
    /// </summary>
    public static class Tags
    {
        public const string Node = "Node";
        public const string NodeLine = "NodeLine";
        public const string Anchor = "Anchor";
        public const string Untagged = "Untagged";
        public const string MainCamera = "MainCamera";
    }

    /// <summary>
    /// GameObject 名称
    /// </summary>
    public static class GameObjectNames
    {
        public const string MainManager = "MainManager";
        public const string InputManager = "InputManager";
        public const string CameraSence = "CameraSence";
        public const string Grid = "Grid";
        public const string Tilemap = "Tilemap";
        public const string Canvas = "Canvas";

        public const string ToggleMoveCam = "ToggleMoveCam";
        public const string ToggleMoveNode = "ToggleMoveNode";
        public const string ToggleEditNode = "ToggleEditNode";
        public const string ToggleTTI = "ToggleTTI";
        public const string ToggleTTIFilter = "ToggleTTIFilter";
        public const string ScrollViewTechTreeItem = "ScrollViewTechTreeItem";
        public const string TipText = "tiptext";
        public const string IfFilterFrom = "IfFilterFrom";
        public const string IfFilterTo = "IfFilterTo";
        public const string BtnFilterClear = "BtnFilterClear";
        public const string ButtonClipBoard = "ButtonClipBoard";
        public const string ButtonExport = "ButtonExport";
        public const string NewNodeColorSlider = "NewNodeColorSlider";
    }

    /// <summary>
    /// UI 层级路径
    /// </summary>
    public static class UIPath
    {
        public const string ScrollViewContent = "Viewport/Content";
        public const string PanelRightContent = "Panel/Panel_RightContent";
    }

    /// <summary>
    /// 文件名
    /// </summary>
    public static class FileNames
    {
        public const string Science = "Science";
        public const string ScienceExcel = "Science.xlsx";
        public const string TechTreeItem = "G_TechTreeItem.xlsx";
    }

    /// <summary>
    /// 节点颜色索引
    /// </summary>
    public static class NodeColorIndex
    {
        public const int White = 0;
        public const int Red = 1;
        public const int Orange = 2;
        public const int Yellow = 3;
        public const int Green = 4;
        public const int Blue = 5;
    }

    /// <summary>
    /// 节点缩放
    /// </summary>
    public static class NodeScale
    {
        public const float Large = 4f;
        public const float Middle = 2f;
        public const float Small = 1f;
    }

    /// <summary>
    /// 线条粗细
    /// </summary>
    public static class LineWidth
    {
        public const float Thick = 12f;
        public const float Medium = 8f;
        public const float Thin = 4f;
    }

    /// <summary>
    /// 特殊 ID
    /// </summary>
    public static class SpecialIds
    {
        public const string InvalidId = "-1";
        public const string PdaTech = "-2";
        public const string NewNodePrefix = "tmp_";
    }

    /// <summary>
    /// 正则表达式模式
    /// </summary>
    public static class RegexPatterns
    {
        // 前置科技格式: -1 或 stringId|stringId|...，ID 不允许包含列表/路径结构分隔符。
        public const string PreTechnology = @"^-1$|^(?!-1$)(?!-2$)[^|:,\s]+(?:\|(?!-1$)(?!-2$)[^|:,\s]+)*$";

        // 路径节点格式：放宽正则，详细验证交给解析器
        // 匹配 -1 或新格式路径字符串。
        public const string PathNode = @"^-1$|^[^|:,\s]+,[tblrc]:(?:-?\d+(?:\.\d+)?,-?\d+(?:\.\d+)?(?:_-?\d+(?:\.\d+)?,-?\d+(?:\.\d+)?)*)?:[tblrc](?:\|[^|:,\s]+,[tblrc]:(?:-?\d+(?:\.\d+)?,-?\d+(?:\.\d+)?(?:_-?\d+(?:\.\d+)?,-?\d+(?:\.\d+)?)*)?:[tblrc])*$";
    }


    /// <summary>
    /// 锚点方向标记常量
    /// </summary>
    public static class AnchorMarks
    {
        public const string Top = "t";
        public const string Bottom = "b";
        public const string Left = "l";
        public const string Right = "r";
        public const string Center = "c";
    }

    /// <summary>
    /// 颜色值
    /// </summary>
    public static class Colors
    {
        public static readonly UnityEngine.Color GreenUsed = new UnityEngine.Color(0.2f, 0.6f, 0.2f);
        public static readonly UnityEngine.Color RedDuplicate = UnityEngine.Color.red;
        public static readonly UnityEngine.Color BlackNormal = UnityEngine.Color.black;
        public static readonly UnityEngine.Color AnchorNormal = new UnityEngine.Color(0.94f, 0.99f, 0.53f);
        public static readonly UnityEngine.Color AnchorSelected = new UnityEngine.Color(0.92f, 0.45f, 0.25f);
        public static readonly UnityEngine.Color AnchorHover = new UnityEngine.Color(0.6f, 0.8f, 1f, 1f);
        public static readonly UnityEngine.Color ActiveLineHighlight = new UnityEngine.Color(0f, 1f, 1f, 1f);
    }
}
