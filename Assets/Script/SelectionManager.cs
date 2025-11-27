// 完全重写 SelectionManager.cs

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Script.My.Extention;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance;

    private HashSet<GameObject> selectedNodes = new HashSet<GameObject>();

    // 选中的锚点：存储锚点的关键信息，而不仅仅是GameObject
    // Key: "目标节点ID->前置节点ID->锚点索引"，例如 "-3->0->1"
    // Value: 锚点的网格坐标
    private Dictionary<string, Vector3Int> selectedAnchorPositions = new Dictionary<string, Vector3Int>();

    // 当前高亮的锚点GameObject（用于显示效果）
    private HashSet<GameObject> highlightedAnchorObjects = new HashSet<GameObject>();

    // 保留单选锚点的兼容性（用于编辑模式）
    public GameObject SelectedAnchor { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    #region 节点选择
    public bool ContainsNode(GameObject obj) => selectedNodes.Contains(obj);
    public bool Contains(GameObject obj) => selectedNodes.Contains(obj);
    public int Count => selectedNodes.Count;
    public int NodeCount => selectedNodes.Count;
    public List<GameObject> GetSelectedNodes() => selectedNodes.ToList();

    public void AddNode(GameObject nodeObj)
    {
        if (selectedNodes.Contains(nodeObj)) return;
        selectedNodes.Add(nodeObj);
        nodeObj.GetComponent<Node>().SetSelectStyle(true);
    }

    public void RemoveNode(GameObject nodeObj)
    {
        if (!selectedNodes.Contains(nodeObj)) return;
        selectedNodes.Remove(nodeObj);
        var node = nodeObj.GetComponent<Node>();
        if (node != null)
        {
            node.SetSelectStyle(false);
            node.ClearAnchor();
        }

        // 移除该节点相关的锚点选择
        RemoveAnchorsForNode(nodeObj);
    }

    public void ClearNodes()
    {
        foreach (var obj in selectedNodes.ToList())
        {
            if (obj != null)
            {
                var node = obj.GetComponent<Node>();
                if (node != null)
                {
                    node.SetSelectStyle(false);
                    node.ClearAnchor();
                }
            }
        }
        selectedNodes.Clear();

        // 同时清除锚点选择
        ClearAnchors();

        // 通知 InputManager 重置锚点显示状态
        var inputManager = GameObject.Find(Constants.GameObjectNames.MainManager)?.GetComponent<InputManager>();
        if (inputManager != null)
        {
            inputManager.ResetAnchorVisibilityState();
        }
    }

    public void ToggleSelection(GameObject obj)
    {
        if (selectedNodes.Contains(obj))
            RemoveNode(obj);
        else
            AddNode(obj);
    }
    #endregion

    #region 锚点选择（基于位置）

    /// <summary>
    /// 生成锚点的唯一Key
    /// </summary>
    private string GetAnchorKey(int targetNodeId, string preNodeId, int anchorIndex)
    {
        return $"{targetNodeId}->{preNodeId}->{anchorIndex}";
    }

    /// <summary>
    /// 解析锚点Key
    /// </summary>
    private (int targetNodeId, string preNodeId, int anchorIndex) ParseAnchorKey(string key)
    {
        var parts = key.Split(new string[] { "->" }, System.StringSplitOptions.None);
        return (int.Parse(parts[0]), parts[1], int.Parse(parts[2]));
    }

    /// <summary>
    /// 添加锚点选择（通过位置信息）
    /// </summary>
    public void AddAnchorByPosition(int targetNodeId, string preNodeId, int anchorIndex, Vector3Int gridPos)
    {
        string key = GetAnchorKey(targetNodeId, preNodeId, anchorIndex);
        if (!selectedAnchorPositions.ContainsKey(key))
        {
            selectedAnchorPositions[key] = gridPos;
        }

        // 尝试高亮对应的GameObject（如果存在）
        TryHighlightAnchorObject(targetNodeId, preNodeId, anchorIndex, true);
    }

    /// <summary>
    /// 添加锚点选择（通过GameObject）
    /// </summary>
    public void AddAnchor(GameObject anchorObj)
    {
        if (anchorObj == null) return;

        // 从锚点GameObject解析信息
        var anchorInfo = GetAnchorInfoFromGameObject(anchorObj);
        if (anchorInfo.HasValue)
        {
            var (targetNodeId, preNodeId, anchorIndex, gridPos) = anchorInfo.Value;
            AddAnchorByPosition(targetNodeId, preNodeId, anchorIndex, gridPos);
        }
    }

    /// <summary>
    /// 从锚点GameObject获取信息
    /// </summary>
    private (int targetNodeId, string preNodeId, int anchorIndex, Vector3Int gridPos)? GetAnchorInfoFromGameObject(GameObject anchorObj)
    {
        if (anchorObj == null) return null;

        // 锚点的父物体是LineRenderer，名称格式为 "PreID->TargetID"
        var lineObj = anchorObj.transform.parent;
        if (lineObj == null) return null;

        var lineName = lineObj.name;
        if (!lineName.Contains("->")) return null;

        var parts = lineName.Split(new string[] { "->" }, System.StringSplitOptions.None);
        string preNodeId = parts[0];
        int targetNodeId = int.Parse(parts[1]);

        // 锚点名称就是索引
        int anchorIndex = int.Parse(anchorObj.name);

        // 获取网格坐标
        var grid = UIReferences.Instance.grid;
        Vector3Int gridPos = grid.WorldToCell(anchorObj.transform.position);

        return (targetNodeId, preNodeId, anchorIndex, gridPos);
    }

    /// <summary>
    /// 更新已选中锚点的位置信息
    /// </summary>
    public void UpdateAnchorPosition(int targetNodeId, string preNodeId, int anchorIndex, Vector3Int newGridPos)
    {
        string key = GetAnchorKey(targetNodeId, preNodeId, anchorIndex);
        if (selectedAnchorPositions.ContainsKey(key))
        {
            selectedAnchorPositions[key] = newGridPos;
        }
    }


    /// <summary>
    /// 移除锚点选择
    /// </summary>
    public void RemoveAnchor(GameObject anchorObj)
    {
        if (anchorObj == null) return;

        var anchorInfo = GetAnchorInfoFromGameObject(anchorObj);
        if (anchorInfo.HasValue)
        {
            var (targetNodeId, preNodeId, anchorIndex, _) = anchorInfo.Value;
            string key = GetAnchorKey(targetNodeId, preNodeId, anchorIndex);
            selectedAnchorPositions.Remove(key);
            TryHighlightAnchorObject(targetNodeId, preNodeId, anchorIndex, false);
        }
    }

    /// <summary>
    /// 移除锚点选择（通过Key）
    /// </summary>
    public void RemoveAnchorByKey(string key)
    {
        if (selectedAnchorPositions.ContainsKey(key))
        {
            var (targetNodeId, preNodeId, anchorIndex) = ParseAnchorKey(key);
            selectedAnchorPositions.Remove(key);
            TryHighlightAnchorObject(targetNodeId, preNodeId, anchorIndex, false);
        }
    }

    /// <summary>
    /// 移除某个节点相关的所有锚点选择
    /// </summary>
    private void RemoveAnchorsForNode(GameObject nodeObj)
    {
        if (nodeObj == null) return;
        var node = nodeObj.GetComponent<Node>();
        if (node == null) return;

        int nodeId = node.sc.Id;
        var keysToRemove = selectedAnchorPositions.Keys
            .Where(k => k.StartsWith($"{nodeId}->"))
            .ToList();

        foreach (var key in keysToRemove)
        {
            selectedAnchorPositions.Remove(key);
        }
    }

    /// <summary>
    /// 清除所有锚点选择
    /// </summary>
    public void ClearAnchors()
    {
        // 清除高亮效果
        foreach (var anchorObj in highlightedAnchorObjects.ToList())
        {
            if (anchorObj != null)
            {
                var sr = anchorObj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = Constants.Colors.AnchorNormal;
                }
            }
        }
        highlightedAnchorObjects.Clear();
        selectedAnchorPositions.Clear();

        // 清除单选锚点
        SelectedAnchor = null;
    }

    /// <summary>
    /// 尝试高亮/取消高亮锚点GameObject
    /// </summary>
    private void TryHighlightAnchorObject(int targetNodeId, string preNodeId, int anchorIndex, bool highlight)
    {
        // 查找对应的锚点GameObject
        var tilemap = UIReferences.Instance.tilemap;
        var nodeTransform = tilemap.transform.Find(targetNodeId.ToString());
        if (nodeTransform == null) return;

        var lineTransform = nodeTransform.Find($"{preNodeId}->{targetNodeId}");
        if (lineTransform == null) return;

        var anchorTransform = lineTransform.Find(anchorIndex.ToString());
        if (anchorTransform == null) return;

        var anchorObj = anchorTransform.gameObject;
        var sr = anchorObj.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        if (highlight)
        {
            sr.color = Constants.Colors.AnchorSelected;
            highlightedAnchorObjects.Add(anchorObj);
        }
        else
        {
            sr.color = Constants.Colors.AnchorNormal;
            highlightedAnchorObjects.Remove(anchorObj);
        }
    }

    /// <summary>
    /// 刷新锚点高亮状态（当锚点GameObject被创建后调用）
    /// </summary>
    public void RefreshAnchorHighlights()
    {
        // 先清除旧的高亮记录（但不改变颜色，因为对象可能已被销毁重建）
        highlightedAnchorObjects.Clear();

        var tilemap = UIReferences.Instance.tilemap;

        foreach (var kvp in selectedAnchorPositions)
        {
            var parts = kvp.Key.Split(new string[] { "->" }, System.StringSplitOptions.None);
            if (parts.Length < 3) continue;

            int targetNodeId = int.Parse(parts[0]);
            string preNodeId = parts[1];
            int anchorIndex = int.Parse(parts[2]);

            // 查找锚点GameObject
            var nodeTransform = tilemap.transform.Find(targetNodeId.ToString());
            if (nodeTransform == null) continue;

            var lineTransform = nodeTransform.Find($"{preNodeId}->{targetNodeId}");
            if (lineTransform == null) continue;

            var anchorTransform = lineTransform.Find(anchorIndex.ToString());
            if (anchorTransform == null) continue;

            var anchorObj = anchorTransform.gameObject;
            var sr = anchorObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = Constants.Colors.AnchorSelected;
                highlightedAnchorObjects.Add(anchorObj);
            }
        }
    }

    public bool ContainsAnchor(GameObject anchor)
    {
        if (anchor == null) return false;
        var info = GetAnchorInfoFromGameObject(anchor);
        if (!info.HasValue) return false;

        var (targetNodeId, preNodeId, anchorIndex, _) = info.Value;
        string key = GetAnchorKey(targetNodeId, preNodeId, anchorIndex);
        return selectedAnchorPositions.ContainsKey(key);
    }

    public bool ContainsAnchorPosition(int targetNodeId, string preNodeId, int anchorIndex)
    {
        string key = GetAnchorKey(targetNodeId, preNodeId, anchorIndex);
        return selectedAnchorPositions.ContainsKey(key);
    }

    public int AnchorCount => selectedAnchorPositions.Count;

    public Dictionary<string, Vector3Int> GetSelectedAnchorPositions() => new Dictionary<string, Vector3Int>(selectedAnchorPositions);

    public void ToggleAnchorSelection(GameObject anchor)
    {
        if (ContainsAnchor(anchor))
            RemoveAnchor(anchor);
        else
            AddAnchor(anchor);
    }
    #endregion

    #region 单选锚点（编辑模式兼容）
    public void SelectAnchor(GameObject anchor)
    {
        if (SelectedAnchor != null && SelectedAnchor != anchor)
        {
            if (!ContainsAnchor(SelectedAnchor))
            {
                var sr = SelectedAnchor.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.color = Constants.Colors.AnchorNormal;
            }
        }

        SelectedAnchor = anchor;

        if (SelectedAnchor != null)
        {
            var sr = SelectedAnchor.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = Constants.Colors.AnchorSelected;
        }
    }

    public void ClearAnchor()
    {
        SelectAnchor(null);
    }
    #endregion

    #region 统一查询
    public List<GameObject> GetAllSelectedObjects()
    {
        var result = new List<GameObject>();
        result.AddRange(selectedNodes);
        // 注意：锚点现在是基于位置的，不一定有对应的GameObject
        return result;
    }

    public bool HasSelection => selectedNodes.Count > 0 || selectedAnchorPositions.Count > 0;

    public void ClearAll()
    {
        ClearNodes();
        ClearAnchors();
    }
    #endregion
}
