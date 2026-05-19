using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 节点管理器 - 负责场景中节点的创建、销毁和管理
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
        foreach (var sc in DataManager.Instance.ScienceDict.Values)
        {
            CreateNodeObject(sc);

            EventCenter.Instance.TriggerTechTreeItemUpdate("", sc.Building_unlock);
            EventCenter.Instance.TriggerTechTreeItemUpdate("", sc.NonBuilding_unlock);
        }

        EventCenter.Instance.TriggerLogMessage("节点初始化完成");
    }

    /// <summary>
    /// 创建单个节点对象（使用世界坐标）
    /// </summary>
    public GameObject CreateNodeObject(Science sc)
    {
        // 直接使用存储的世界坐标
        Vector3 worldPos = new Vector3(sc.HexGridX, sc.HexGridY, 0);

        GameObject nodeObj = Instantiate(ui.nodePrefab, worldPos, Quaternion.identity, ui.tilemap.transform);
        nodeObj.name = sc.Id;

        Node nodeScript = nodeObj.GetComponent<Node>();
        nodeScript.sc = sc;

        return nodeObj;
    }

    /// <summary>
    /// 销毁节点对象
    /// </summary>
    public void DestroyNodeObject(string id)
    {
        Transform nodeTrans = ui.tilemap.transform.Find(id);
        if (nodeTrans != null)
        {
            Destroy(nodeTrans.gameObject);
        }
    }

    /// <summary>
    /// 获取节点对象
    /// </summary>
    public Node GetNode(string id)
    {
        Transform nodeTrans = ui.tilemap.transform.Find(id);
        return nodeTrans != null ? nodeTrans.GetComponent<Node>() : null;
    }

    /// <summary>
    /// 刷新所有节点位置（切换网格类型时调用）
    /// 现在使用世界坐标，节点位置不会因网格类型改变而变化
    /// </summary>
    public void RefreshNodePositions()
    {
        foreach (var sc in DataManager.Instance.ScienceDict.Values)
        {
            Node node = GetNode(sc.Id);
            if (node != null)
            {
                // 直接使用存储的世界坐标
                Vector3 worldPos = new Vector3(sc.HexGridX, sc.HexGridY, 0);
                node.transform.position = worldPos;
                node.UpdateLineOnly();
            }
        }
    }
}
