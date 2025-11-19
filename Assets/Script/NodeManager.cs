using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 节点管理器 - 负责场景中节点对象的生命周期管理
/// </summary>
public class NodeManager : MonoBehaviour
{
    #region 单例
    private static NodeManager _instance;
    public static NodeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<NodeManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("NodeManager");
                    _instance = go.AddComponent<NodeManager>();
                }
            }
            return _instance;
        }
    }
    private void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) Destroy(gameObject);

        ui = GetComponent<UIReferences>() ?? UIReferences.Instance;
    }
    #endregion

    private UIReferences ui;

    /// <summary>
    /// 初始化所有节点
    /// </summary>
    public void InitializeNodes()
    {
        // 清理现有节点（如果是重新加载）
        // ClearExistingNodes();

        foreach (var sc in DataManager.Instance.ScienceDict.Values)
        {
            CreateNodeObject(sc);

            // 更新科技树项显示状态
            EventCenter.Instance.TriggerTechTreeItemUpdate("", sc.Building_unlock);
            EventCenter.Instance.TriggerTechTreeItemUpdate("", sc.NonBuilding_unlock);
        }

        EventCenter.Instance.TriggerLogMessage("节点初始化完成");
    }

    /// <summary>
    /// 创建单个节点对象
    /// </summary>
    public GameObject CreateNodeObject(Science sc)
    {
        Vector3 worldPos = ui.grid.CellToWorld(new Vector3Int(sc.HexGridY, sc.HexGridX, 0));

        GameObject nodeObj = Instantiate(ui.nodePrefab, worldPos, Quaternion.identity, ui.tilemap.transform);
        nodeObj.name = sc.Id.ToString();

        Node nodeScript = nodeObj.GetComponent<Node>();
        nodeScript.sc = sc;
        // nodeScript.UpdateNodeAppearance(); // Start() 中会自动调用

        return nodeObj;
    }

    /// <summary>
    /// 销毁节点对象
    /// </summary>
    public void DestroyNodeObject(int id)
    {
        Transform nodeTrans = ui.tilemap.transform.Find(id.ToString());
        if (nodeTrans != null)
        {
            Destroy(nodeTrans.gameObject);
        }
    }

    /// <summary>
    /// 获取节点对象
    /// </summary>
    public Node GetNode(int id)
    {
        Transform nodeTrans = ui.tilemap.transform.Find(id.ToString());
        return nodeTrans != null ? nodeTrans.GetComponent<Node>() : null;
    }
}
