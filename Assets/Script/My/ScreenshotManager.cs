using System.Collections;
using UnityEngine;

public class ScreenshotManager : MonoBehaviour
{
    public static ScreenshotManager Instance;

    [Header("设置")]
    [Tooltip("截图边缘留白的大小（单位：世界坐标）")]
    public float padding = 2f;
    [Tooltip("截图的分辨率倍数，1为屏幕当前分辨率，2为2倍清晰度")]
    public int resolutionMultiplier = 2;

    private Camera cam;
    private Grid grid;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        cam = UIReferences.Instance.camSence;
        grid = UIReferences.Instance.grid;
    }

    /// <summary>
    /// 执行全场景截图并复制到剪贴板
    /// </summary>
    public void CaptureSceneToClipboard()
    {
        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        // 1. 计算包含所有节点的包围盒
        Bounds? sceneBounds = CalculateSceneBounds();

        if (sceneBounds == null)
        {
            EventCenter.Instance.TriggerLogWarning("场景中没有节点，无法截图");
            yield break;
        }

        Bounds bounds = sceneBounds.Value;
        // 加上留白
        bounds.Expand(padding);

        // 2. 记录相机原始状态
        Vector3 originalPos = cam.transform.position;
        float originalSize = cam.orthographicSize;
        RenderTexture originalTarget = cam.targetTexture;

        // 3. 计算适配包围盒所需的相机参数
        float targetHeight = bounds.size.y / 2f;
        float targetWidth = bounds.size.x / 2f;
        float screenAspect = (float)Screen.width / Screen.height;

        // 根据长宽比决定是以高度适配还是以宽度适配
        float targetSize = targetHeight;
        if (targetWidth / screenAspect > targetHeight)
        {
            targetSize = targetWidth / screenAspect;
        }

        // 4. 设置截图用的 RenderTexture
        int width = Screen.width * resolutionMultiplier;
        int height = Screen.height * resolutionMultiplier;
        RenderTexture rt = new RenderTexture(width, height, 24);

        // 5. 临时移动相机并渲染
        cam.targetTexture = rt;
        cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, originalPos.z);
        cam.orthographicSize = targetSize;

        // 等待一帧结束，确保所有渲染指令（包括GridDrawer的GL绘制）都准备好
        // 注意：对于GL.LINES，通常在OnPostRender或OnRenderObject中绘制
        // 手动调用 cam.Render() 会触发这些回调
        cam.Render();

        // 6. 读取像素到 Texture2D
        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGBA32, false);
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        // 7. 恢复相机状态
        cam.targetTexture = originalTarget;
        cam.transform.position = originalPos;
        cam.orthographicSize = originalSize;
        RenderTexture.active = null;
        Destroy(rt);

        // 8. 复制到剪贴板
        ClipboardHelper.CopyTextureToClipboard(screenShot);

        Destroy(screenShot);

        EventCenter.Instance.TriggerLogMessage("场景截图已复制到剪贴板！");
    }

    private Bounds? CalculateSceneBounds()
    {
        var nodes = FindObjectsOfType<Node>();
        if (nodes.Length == 0) return null;

        Bounds bounds = new Bounds(nodes[0].transform.position, Vector3.zero);

        foreach (var node in nodes)
        {
            bounds.Encapsulate(node.transform.position);

            // 也可以考虑把锚点包含进去，防止边缘的线被切断
            // 这里简单处理，通常节点就是边缘
        }

        return bounds;
    }
}
