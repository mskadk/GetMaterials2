using UnityEngine;
using UnityEngine.EventSystems;

public class GhostNode : MonoBehaviour
{
    private UIReferences ui;
    private Grid grid;

    void Start()
    {
        ui = UIReferences.Instance;
        grid = ui.grid;
    }

    void Update()
    {
        // 跟随鼠标移动
        Vector3 worldPos = ui.camSence.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;

        // 根据网格类型决定是否对齐
        Vector3 displayPos = GridManager.Instance.SnapToGrid(worldPos);
        transform.position = displayPos;

        // 左键 - 创建节点
        if (!EventSystem.current.currentSelectedGameObject && Input.GetMouseButtonDown(0))
        {
            CreateNode(displayPos);
        }

        // 右键或ESC - 取消
        if (!EventSystem.current.currentSelectedGameObject &&
            (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
        {
            Destroy(gameObject);
        }
    }

    private void CreateNode(Vector3 worldPos)
    {
        // 获取当前颜色索引
        int colorInt = UIManager.Instance.CurrentNodeColorIndex;

        // 使用世界坐标创建节点
        Vector2 pos = new Vector2(
            (float)System.Math.Round(worldPos.x, 3),
            (float)System.Math.Round(worldPos.y, 3)
        );

        var createCmd = new CreateNodeCommand(pos, colorInt, ui);
        CommandManager.Instance.ExecuteCommand(createCmd);

        EventCenter.Instance.TriggerLogMessage($"在 ({pos.x:F3},{pos.y:F3}) 创建了新节点");
    }
}
