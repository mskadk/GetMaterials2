using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 组件引用集合 - 避免使用 GameObject.Find()
/// 所有引用通过 Inspector 拖拽赋值
/// </summary>
public class UIReferences : MonoBehaviour
{
    [Header("=== Toggle 组件 ===")]
    //public Toggle toggleMoveCam;
    //public Toggle toggleMoveNode;
    //public Toggle toggleEditNode;
    public Toggle toggleTTI;
    public Toggle toggleTTIFilter;

    [Header("=== InputField 组件 ===")]
    public InputField ifFilterFrom;
    public InputField ifFilterTo;

    [Header("=== Button 组件 ===")]
    public Button btnFilterClear;
    public Button btnClipBoard;
    public Button btnExport;

    [Header("=== 其他 UI 组件 ===")]
    public ScrollRect scrollViewTechTreeItem;
    public Slider newNodeColorSlider;
    public TipText tipText;
    public Dropdown dpMainPage;

    [Header("=== 场景对象 ===")]
    public GameObject canvas;
    public GameObject tilemap;
    public Grid grid;
    public Camera camSence;

    [Header("=== 预制体 ===")]
    public GameObject nodePrefab;
    public GameObject ghostNodePrefab;
    public GameObject panelNodeEditPrefab;
    public GameObject techTreeItemTextPrefab;

    #region 单例模式
    private static UIReferences _instance;
    public static UIReferences Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<UIReferences>();
                if (_instance == null)
                {
                    Debug.LogError("场景中没有 UIReferences 组件！");
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Debug.LogWarning("场景中有多个 UIReferences，已销毁重复的实例");
            Destroy(gameObject);
        }
    }
    #endregion

    /// <summary>
    /// 验证所有引用是否已赋值
    /// </summary>
    public bool ValidateReferences()
    {
        bool isValid = true;

        if (scrollViewTechTreeItem == null) { Debug.LogError("scrollViewTechTreeItem 未赋值"); isValid = false; }
        if (tipText == null) { Debug.LogError("tipText 未赋值"); isValid = false; }
        if (grid == null) { Debug.LogError("grid 未赋值"); isValid = false; }
        if (camSence == null) { Debug.LogError("camSence 未赋值"); isValid = false; }

        return isValid;
    }
}
