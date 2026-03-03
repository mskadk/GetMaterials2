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
    // Value: 锚点世界坐标 (Vector2)
    private Dictionary<string, Vector2> selectedAnchorPositions = new Dictionary<string, Vector2>();

    // 当前高亮的锚点GameObject（用于显示效果）
    private HashSet<GameObject> highlightedAnchorObjects = new HashSet<GameObject>();

    // 单选锚点（编辑模式专用）
    public GameObject SelectedAnchor { get; private set; }

    // === 新增：活动连接线管理 ===
    private int activeLineIndex = 0;  // 当前活动的连接线索引（0-based）
    private GameObject lastSelectedNode = null;  // 最后选中的节点（主节点）
    private LineRenderer highlightedLine = null;  // 当前高亮的连接线
    private Color originalLineStartColor;  // 原始起始颜色
    private Color originalLineEndColor;    // 原始结束颜色
    private float originalLineWidth;       // 原始线宽

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

    /// <summary>
    /// 获取主节点（最后选中的节点）
    /// </summary>
    public GameObject GetPrimaryNode() => lastSelectedNode;

    public void AddNode(GameObject nodeObj)
    {
        if (selectedNodes.Contains(nodeObj)) return;
        selectedNodes.Add(nodeObj);
        nodeObj.GetComponent<Node>().SetSelectStyle(true);

        // 更新主节点
        lastSelectedNode = nodeObj;

        // 重置活动连接线索引并更新高亮
        activeLineIndex = 0;
        UpdateActiveLineHighlight();
    }

    public void RemoveNode(GameObject nodeObj)
    {
        if (!selectedNodes.Contains(nodeObj)) return;
        selectedNodes.Remove(nodeObj);
        var node = nodeObj.GetComponent<Node>();
        if (node != null)
        {
            node.SetSelectStyle(false); node.ClearAnchor();
        }

        // 移除该节点相关的所有锚点选择
        RemoveAnchorsForNode(nodeObj);

        // 如果移除的是主节点，更新主节点
        if (lastSelectedNode == nodeObj)
        {
            lastSelectedNode = selectedNodes.Count > 0 ? selectedNodes.Last() : null;
            activeLineIndex = 0;
            UpdateActiveLineHighlight();
        }
    }

    public void ClearNodes()
    {
        // 先清除连接线高亮
        ClearActiveLineHighlight();

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
        lastSelectedNode = null;
        activeLineIndex = 0;

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
        if (selectedNodes.Contains(obj)) RemoveNode(obj);
        else
            AddNode(obj);
    }
    #endregion

    #region 活动连接线管理

    /// <summary>
    /// 获取主节点的所有入边连接线
    /// </summary>
    public List<LineRenderer> GetPrimaryNodeLines()
    {
        if (lastSelectedNode == null) return new List<LineRenderer>();

        var node = lastSelectedNode.GetComponent<Node>();
        if (node == null) return new List<LineRenderer>();

        var lines = new List<LineRenderer>();
        foreach (Transform child in lastSelectedNode.transform)
        {
            if (child.tag == Constants.Tags.NodeLine)
            {
                var lr = child.GetComponent<LineRenderer>();
                if (lr != null) lines.Add(lr);
            }
        }
        return lines;
    }

    /// <summary>
    /// 获取当前活动连接线的索引
    /// </summary>
    public int GetActiveLineIndex() => activeLineIndex;

    /// <summary>
    /// 获取当前活动连接线
    /// </summary>
    public LineRenderer GetActiveLine()
    {
        var lines = GetPrimaryNodeLines();
        if (lines.Count == 0) return null;
        if (activeLineIndex >= lines.Count) activeLineIndex = 0;
        return lines[activeLineIndex];
    }

    /// <summary>
    /// 切换活动连接线（通过数字键）
    /// </summary>
    public void SetActiveLineIndex(int index)
    {
        var lines = GetPrimaryNodeLines();
        if (lines.Count == 0) return;

        // 索引从0开始，但用户按的是1-9
        int newIndex = Mathf.Clamp(index, 0, lines.Count - 1);
        if (newIndex != activeLineIndex)
        {
            activeLineIndex = newIndex;
            UpdateActiveLineHighlight();
        }
    }

    /// <summary>
    /// 更新活动连接线的高亮显示
    /// </summary>
    public void UpdateActiveLineHighlight()
    {
        // 先清除旧的高亮
        ClearActiveLineHighlight();

        var lines = GetPrimaryNodeLines();
        if (lines.Count == 0) return;

        if (activeLineIndex >= lines.Count) activeLineIndex = 0;

        highlightedLine = lines[activeLineIndex];
        if (highlightedLine != null)
        {
            // 保存原始样式
            originalLineStartColor = highlightedLine.startColor;
            originalLineEndColor = highlightedLine.endColor;
            originalLineWidth = highlightedLine.startWidth;

            // 应用高亮样式
            highlightedLine.startColor = Constants.Colors.ActiveLineHighlight;
            highlightedLine.endColor = Constants.Colors.ActiveLineHighlight;
            highlightedLine.startWidth = originalLineWidth * 1.5f;
            highlightedLine.endWidth = originalLineWidth * 1.5f;
        }
    }

    /// <summary>
    /// 清除活动连接线的高亮
    /// </summary>
    private void ClearActiveLineHighlight()
    {
        if (highlightedLine != null)
        {
            // 恢复原始样式
            highlightedLine.startColor = originalLineStartColor;
            highlightedLine.endColor = originalLineEndColor;
            highlightedLine.startWidth = originalLineWidth;
            highlightedLine.endWidth = originalLineWidth;
            highlightedLine = null;
        }
    }

    /// <summary>
    /// 获取当前活动连接线的路径信息
    /// </summary>
    public PathConnection? GetActiveLineConnection()
    {
        if (lastSelectedNode == null) return null;

        var node = lastSelectedNode.GetComponent<Node>();
        if (node == null) return null;

        var activeLine = GetActiveLine();
        if (activeLine == null) return null;

        // 从连接线名称解析 preId
        var lineName = activeLine.gameObject.name;
        if (!lineName.Contains("->")) return null;

        var parts = lineName.Split(new string[] { "->" }, System.StringSplitOptions.None);
        int preId = int.Parse(parts[0]);

        // 从 PathNode 中查找对应的连接
        var connections = node.sc.PathNode.ParsePathConnections();
        foreach (var conn in connections)
        {
            if (conn.PreId == preId) return conn;
        }

        // 如果没有找到，返回一个默认的连接（方向都是 Center）
        return new PathConnection(preId, AnchorDirection.Center, AnchorDirection.Center, new List<Vector2>());
    }

    /// <summary>
    /// 更新当前活动连接线的锚点方向
    /// </summary>
    public void UpdateActiveLineDirection(bool isStart, AnchorDirection newDir)
    {
        if (lastSelectedNode == null) return;

        var node = lastSelectedNode.GetComponent<Node>();
        if (node == null) return;

        var activeLine = GetActiveLine();
        if (activeLine == null) return;

        // 从连接线名称解析 preId
        var lineName = activeLine.gameObject.name;
        if (!lineName.Contains("->")) return;

        var parts = lineName.Split(new string[] { "->" }, System.StringSplitOptions.None);
        int preId = int.Parse(parts[0]);

        // 解析当前路径
        var connections = node.sc.PathNode.ParsePathConnections();
        bool found = false;

        for (int i = 0; i < connections.Count; i++)
        {
            if (connections[i].PreId == preId)
            {
                var conn = connections[i];
                if (isStart)
                    conn.StartDirection = newDir;
                else
                    conn.EndDirection = newDir;
                connections[i] = conn;
                found = true;
                break;
            }
        }

        // 如果没找到对应连接，创建一个新的
        if (!found)
        {
            var newConn = new PathConnection(
                preId,
                isStart ? newDir : AnchorDirection.Center,
                isStart ? AnchorDirection.Center : newDir,
                new List<Vector2>()
            );
            connections.Add(newConn);
        }

        // 保存并刷新
        node.sc.PathNode = MyExtensions.SerializePathConnections(connections);
        node.UpdateNodeAppearance();

        // 重新高亮（因为 UpdateNodeAppearance 会重建线条）
        UpdateActiveLineHighlight();

        // 如果编辑面板打开，刷新显示
        var inputManager = GameObject.Find(Constants.GameObjectNames.MainManager)?.GetComponent<InputManager>();
        if (inputManager?.CurrentEditPanel != null)
        {
            var panelScript = inputManager.CurrentEditPanel.GetComponent<PanelScienceEdit>();
            if (panelScript.sc.Id == node.sc.Id)
            {
                panelScript.RefreshUI();
            }
        }

        // 日志提示
        string dirName = isStart ? "起始" : "终止";
        string dirMark = MyExtensions.DirectionToMark(newDir).ToUpper();
        EventCenter.Instance.TriggerLogMessage($"设置连接线 {preId}->{node.sc.Id} 的{dirName}方向为 {dirMark}");
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

        // 锚点的父对象是LineRenderer，名称格式为 "PreID->TargetID"
        var lineObj = anchorObj.transform.parent;
        if (lineObj == null) return null;

        var lineName = lineObj.name;
        if (!lineName.Contains("->")) return null;

        var parts = lineName.Split(new string[] { "->" }, System.StringSplitOptions.None);
        string preNodeId = parts[0];
        int targetNodeId = int.Parse(parts[1]);

        // 锚点名称就是索引
        int anchorIndex = int.Parse(anchorObj.name);

        // 获取世界坐标
        Vector2 worldPos = anchorObj.transform.position;

        return (targetNodeId, preNodeId, anchorIndex, worldPos);
    }

    /// <summary>
    /// 更新已选锚点的位置信息
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
    /// 兼容旧代码：如果其他代码传入 Vector3Int，给出警告
    /// </summary>
    public void UpdateAnchorPosition(int targetNodeId, string preNodeId, int anchorIndex, Vector3Int gridPos)
    {
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
    /// 刷新锚点高亮状态（锚点GameObject被重建后调用）
    /// </summary>
    public void RefreshAnchorHighlights()
    {
        // 清空旧的高亮记录（因为对象可能已被重建）
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

    public Dictionary<string, Vector2> GetSelectedAnchorPositions() => new Dictionary<string, Vector2>(selectedAnchorPositions);

    public void ToggleAnchorSelection(GameObject anchor)
    {
        if (ContainsAnchor(anchor)) RemoveAnchor(anchor);
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
                if (sr != null) sr.color = Constants.Colors.AnchorNormal;
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
    /// 根据世界坐标矩形选择实体（节点和锚点）
    /// </summary>
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

                // 2. 遍历节点下的所有连接线，检查锚点
                foreach (Transform lineTrans in nodeTrans)
                {
                    if (lineTrans.tag == Constants.Tags.NodeLine)
                    {
                        foreach (Transform anchorTrans in lineTrans)
                        {
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
