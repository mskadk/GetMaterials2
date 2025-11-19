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
        Vector3Int cellPos = grid.WorldToCell(worldPos);
        var px = grid.CellToWorld(cellPos).x;
        var py = grid.CellToWorld(cellPos).y;
        transform.position = new Vector3(px, py, 0);

        // 左键点击 - 创建节点
        if (!EventSystem.current.currentSelectedGameObject && Input.GetMouseButtonDown(0))
        {
            CreateNode(cellPos);
        }

        // 右键或ESC - 取消
        if (!EventSystem.current.currentSelectedGameObject && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
        {
            Destroy(gameObject);
        }
    }

    private void CreateNode(Vector3Int pos)
    {
        // 获取当前颜色索引
        int colorInt = UIManager.Instance.CurrentNodeColorIndex; // 确保 UIManager 有这个属性

        // 使用命令创建节点 (包含数据初始化和物体生成)
        var createCmd = new CreateNodeCommand(pos, colorInt, ui);
        CommandManager.Instance.ExecuteCommand(createCmd);

        // 提示
        EventCenter.Instance.TriggerLogMessage($"在 ({pos.x},{pos.y}) 创建了新节点");
    }
}
