using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Script.My.Extention;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance;

    private HashSet<GameObject> selectedNodes = new HashSet<GameObject>();

    // 选中的锚点：存储锚点的关键信息
    // Key: "目标节点ID->前置节点ID->锚点索引" (例如 "-3->0->1")
    // Value: 锚点的世界坐标 (Vector2)
    private Dictionary<string, Vector2> selectedAnchorPositions = new Dictionary<string, Vector2>();

    // 当前高亮的锚点GameObject（用于显示效果）
    private HashSet<GameObject> highlightedAnchorObjects = new HashSet<GameObject>();

    // 单选锚点（编辑模式专用）
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

        // 移除该节点相关的所有锚点选择
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

        // 同时清空锚点选择
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
    /// 生成锚点唯一Key
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
    public void AddAnchorByPosition(int targetNodeId, string preNodeId, int anchorIndex, Vector2 worldPos)
    {
        string key = GetAnchorKey(targetNodeId, preNodeId, anchorIndex);
        if (!selectedAnchorPositions.ContainsKey(key))
        {
            selectedAnchorPositions[key] = worldPos;
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

        // 从锚点GameObject获取信息
        var anchorInfo = GetAnchorInfoFromGameObject(anchorObj);
        if (anchorInfo.HasValue)
        {
            var (targetNodeId, preNodeId, anchorIndex, worldPos) = anchorInfo.Value;
            AddAnchorByPosition(targetNodeId, preNodeId, anchorIndex, worldPos);
        }
    }

    /// <summary>
    /// 从锚点GameObject获取信息
    /// </summary>
    private (int targetNodeId, string preNodeId, int anchorIndex, Vector2 worldPos)? GetAnchorInfoFromGameObject(GameObject anchorObj)
    {
        if (anchorObj == null) return null;

        // 锚点的父级是LineRenderer，命名格式为 "PreID->TargetID"
        var lineObj = anchorObj.transform.parent;
        if (lineObj == null) return null;

        var lineName = lineObj.name;
        if (!lineName.Contains("->")) return null;

        var parts = lineName.Split(new string[] { "->" }, System.StringSplitOptions.None);
        string preNodeId = parts[0];
        int targetNodeId = int.Parse(parts[1]);

        // 锚点名字就是索引
        int anchorIndex = int.Parse(anchorObj.name);

        // 获取世界坐标
        Vector2 worldPos = anchorObj.transform.position;

        return (targetNodeId, preNodeId, anchorIndex, worldPos);
    }

    /// <summary>
    /// 更新已选中锚点的位置信息
    /// </summary>
    public void UpdateAnchorPosition(int targetNodeId, string preNodeId, int anchorIndex, Vector2 newWorldPos)
    {
        string key = GetAnchorKey(targetNodeId, preNodeId, anchorIndex);
        if (selectedAnchorPositions.ContainsKey(key))
        {
            selectedAnchorPositions[key] = newWorldPos;
        }
    }

    /// <summary>
    /// 兼容旧代码：如果仍有代码传入 Vector3Int，将其转换为 Vector2
    /// </summary>
    public void UpdateAnchorPosition(int targetNodeId, string preNodeId, int anchorIndex, Vector3Int gridPos)
    {
        // 这里假设 gridPos 是某种网格坐标，但在新系统中我们应尽量避免使用它
        // 为了兼容，我们可能需要 Grid 引用来转换，或者直接报错提醒
        // 暂时留空或简单的转换（假设调用者知道自己在做什么，或者调用者已经修正）
        // 建议：直接修改调用处，不要使用这个重载
        Debug.LogWarning("UpdateAnchorPosition with Vector3Int is deprecated. Use Vector2 worldPos instead.");
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
    /// 清空所有锚点选择
    /// </summary>
    public void ClearAnchors()
    {
        // 恢复颜色效果
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

        // 清空单选锚点
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
    /// 刷新锚点高亮状态（当锚点GameObject被重建后调用）
    /// </summary>
    public void RefreshAnchorHighlights()
    {
        // 清空旧的引用记录（因为物体可能已被销毁重建）
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

    // 修改返回类型为 Vector2
    public Dictionary<string, Vector2> GetSelectedAnchorPositions() => new Dictionary<string, Vector2>(selectedAnchorPositions);

    public void ToggleAnchorSelection(GameObject anchor)
    {
        if (ContainsAnchor(anchor))
            RemoveAnchor(anchor);
        else
            AddAnchor(anchor);
    }
    #endregion

    #region 单选锚点（编辑模式专用）
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

    #region 框选逻辑支持
    /// <summary>
    /// 根据世界坐标矩形选中物体（节点和锚点）
    /// </summary>
    /// <param name="worldRect">世界坐标矩形</param>
    /// <param name="clearPrevious">是否清空之前的选择</param>
    public void SelectInWorldRect(Rect worldRect, bool clearPrevious = true)
    {
        if (clearPrevious)
        {
            ClearNodes();
            ClearAnchors();
        }

        var tilemap = UIReferences.Instance.tilemap;
        if (tilemap == null) return;

        // 1. 遍历所有节点
        foreach (Transform nodeTrans in tilemap.transform)
        {
            if (nodeTrans.tag == Constants.Tags.Node)
            {
                // 检查节点是否在框内
                if (worldRect.Contains(nodeTrans.position))
                {
                    AddNode(nodeTrans.gameObject);
                }

                // 2. 遍历节点下的所有连线，查找锚点
                // 锚点是 LineRenderer 的子物体
                foreach (Transform lineTrans in nodeTrans)
                {
                    if (lineTrans.tag == Constants.Tags.NodeLine)
                    {
                        foreach (Transform anchorTrans in lineTrans)
                        {
                            // 假设锚点没有特殊的Tag，或者Tag是Anchor
                            // 我们可以通过名字（数字）或者组件（SpriteRenderer）来判断
                            // 这里假设所有子物体都是锚点
                            if (anchorTrans.GetComponent<SpriteRenderer>() != null)
                            {
                                if (worldRect.Contains(anchorTrans.position))
                                {
                                    AddAnchor(anchorTrans.gameObject);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion

    #region 统一查询
    public List<GameObject> GetAllSelectedObjects()
    {
        var result = new List<GameObject>();
        result.AddRange(selectedNodes);
        // 注意：锚点选择是基于位置的，不一定总有对应的GameObject
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
