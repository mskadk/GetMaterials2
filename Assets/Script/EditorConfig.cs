using UnityEngine;

[CreateAssetMenu(fileName = "EditorConfig", menuName = "TechTree/EditorConfig")]
public class EditorConfig : ScriptableObject
{
    [Header("=== 工作路径配置 ===")]
    [Tooltip("精灵资源路径")]
    public string spritePath = "D:\\work\\manager\\Assets WorkSpace\\FreeWorld\\sprite\\";

    [Tooltip("Excel表格路径")]
    public string excelPath = "D:\\work\\manager\\策划\\项目企划\\数据表\\";

    [Tooltip("导出保存路径")]
    public string savePath = "C:\\Users\\Administrator\\Desktop\\";

    [Header("=== 节点颜色配置 ===")]
    public Color colorRed = Color.red;
    public Color colorOrange = new Color(1, 0.5f, 0);
    public Color colorYellow = new Color(1, 1, 0);
    public Color colorGreen = Color.green;
    public Color colorBlue = new Color(0.2f, 0.5f, 1);
    public Color colorWhite = Color.white;

    [Header("=== 预制体引用 ===")]
    [Tooltip("可选：后续可以把预制体引用也放这里")]
    public GameObject nodePrefab;
    public GameObject ghostNodePrefab;
    public GameObject editPanelPrefab;

    /// <summary>
    /// 根据颜色索引获取颜色
    /// </summary>
    public Color GetColor(int colorIndex)
    {
        return colorIndex switch
        {
            1 => colorRed,
            2 => colorOrange,
            3 => colorYellow,
            4 => colorGreen,
            5 => colorBlue,
            _ => colorWhite,
        };
    }
}
