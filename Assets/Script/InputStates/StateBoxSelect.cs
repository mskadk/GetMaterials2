using System.Collections.Generic;
using UnityEngine;
using System.Globalization;
using Assets.Script.My.Extention;

public class StateBoxSelect : IInputState
{
    private Vector2 startScreenPos;
    private bool isDragging = false;
    private const float DRAG_THRESHOLD = 5f;
    private bool isShiftHeld = false;

    public StateBoxSelect(Vector3 mousePos)
    {
        this.startScreenPos = mousePos;
        this.isShiftHeld = Input.GetKey(KeyCode.LeftShift);
    }

    public void OnEnter(InputManager context)
    {
        if (!isShiftHeld)
        {
            SelectionManager.Instance.ClearNodes();
            SelectionManager.Instance.ClearAnchors();
        }
    }

    public void OnUpdate(InputManager context)
    {
        if (!isDragging && Vector2.Distance(Input.mousePosition, startScreenPos) > DRAG_THRESHOLD)
        {
            isDragging = true;
        }

        if (isDragging)
        {
            UIManager.Instance.UpdateSelectionBox(startScreenPos, Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            context.ChangeState(new StateIdle());
        }
    }

    public void OnExit(InputManager context)
    {
        if (isDragging)
        {
            UIManager.Instance.HideSelectionBox();
            PerformBoxSelection(context);
        }
        else
        {
            if (context.CurrentEditPanel == null && !isShiftHeld)
            {
                SelectionManager.Instance.ClearNodes();
                SelectionManager.Instance.ClearAnchors();
            }
        }
    }

    private void PerformBoxSelection(InputManager context)
    {
        Vector2 endPos = Input.mousePosition;
        Vector2 min = Vector2.Min(startScreenPos, endPos);
        Vector2 max = Vector2.Max(startScreenPos, endPos);
        Vector3 worldMin = context.UI.camSence.ScreenToWorldPoint(min);
        Vector3 worldMax = context.UI.camSence.ScreenToWorldPoint(max);

        // 创建世界坐标选框
        Rect selectionRect = Rect.MinMaxRect(worldMin.x, worldMin.y, worldMax.x, worldMax.y);

        // 1. 选中框选范围内的节点和已实例化的锚点
        // (如果有锚点GameObject在场景中，OverlapAreaAll 会检测到)
        Collider2D[] hits = Physics2D.OverlapAreaAll(worldMin, worldMax);
        foreach (var hit in hits)
        {
            GameObject obj = hit.gameObject;
            if (obj.tag == Constants.Tags.Node)
            {
                SelectionManager.Instance.AddNode(obj);
            }
            else if (obj.tag == Constants.Tags.Anchor)
            {
                var renderer = obj.GetComponent<Renderer>();
                if (renderer != null && renderer.enabled)
                {
                    SelectionManager.Instance.AddAnchor(obj);
                }
            }
        }

        // 2. 纯数学方式选中锚点 (针对锚点尚未实例化的情况)
        // 解析所有数据的路径点，判断是否在框内
        var newlySelectedAnchors = SelectAnchorsByCalculation(context, selectionRect);

        // 3. 如果有新选中的锚点，确保它们所在的节点显示锚点
        if (newlySelectedAnchors.Count > 0)
        {
            EnsureAnchorsVisible(context, newlySelectedAnchors);
        }

        context.CheckEditPanelState();
    }

    /// <summary>
    /// 通过计算选中锚点，返回新选中的锚点信息
    /// </summary>
    private List<(int targetNodeId, string preNodeId, int anchorIndex, Vector2 worldPos)> SelectAnchorsByCalculation(
        InputManager context, Rect selectionRect)
    {
        var newlySelected = new List<(int, string, int, Vector2)>();

        var selectedNodes = SelectionManager.Instance.GetSelectedNodes();

        // 遍历所有选中的节点（通常框选也包含节点，如果只框选锚点，可能需要遍历所有节点）
        // 为了支持框选空节点间的连线锚点，我们遍历所有可见节点更稳妥
        // 这里优化为遍历 DataManager 中的所有数据
        foreach (var sc in DataManager.Instance.ScienceDict.Values)
        {
            var anchorPositions = ParseAnchorPositions(sc);

            foreach (var anchorInfo in anchorPositions)
            {
                Vector2 pos = anchorInfo.worldPos;

                if (selectionRect.Contains(pos))
                {
                    // 检查是否已经选中
                    if (!SelectionManager.Instance.ContainsAnchorPosition(
                        sc.Id, anchorInfo.preNodeId, anchorInfo.anchorIndex))
                    {
                        SelectionManager.Instance.AddAnchorByPosition(
                            sc.Id,
                            anchorInfo.preNodeId,
                            anchorInfo.anchorIndex,
                            pos
                        );
                        newlySelected.Add((sc.Id, anchorInfo.preNodeId, anchorInfo.anchorIndex, pos));
                    }
                }
            }
        }

        return newlySelected;
    }

    /// <summary>
    /// 确保选中的锚点可见（生成锚点GameObject）
    /// </summary>
    private void EnsureAnchorsVisible(InputManager context,
        List<(int targetNodeId, string preNodeId, int anchorIndex, Vector2 worldPos)> anchors)
    {
        // 收集需要显示锚点的节点ID
        HashSet<int> nodeIdsNeedingAnchors = new HashSet<int>();
        foreach (var anchor in anchors)
        {
            nodeIdsNeedingAnchors.Add(anchor.targetNodeId);
        }

        // 为这些节点生成锚点
        foreach (var nodeId in nodeIdsNeedingAnchors)
        {
            var node = NodeManager.Instance.GetNode(nodeId);
            if (node != null)
            {
                node.UpdateLineAnchor();
            }
        }

        // 刷新高亮状态
        SelectionManager.Instance.RefreshAnchorHighlights();

        // 开启 InputManager 的锚点可见状态
        context.SetAnchorsVisibleInNormalMode(true);
    }

    private List<(string preNodeId, int anchorIndex, Vector2 worldPos)> ParseAnchorPositions(Science sc)
    {
        var result = new List<(string preNodeId, int anchorIndex, Vector2 worldPos)>();

        if (string.IsNullOrEmpty(sc.PathNode) || sc.PathNode == "-1")
            return result;

        // 使用新格式解析
        var connections = sc.PathNode.ParsePathConnections();

        foreach (var conn in connections)
        {
            string preNodeId = conn.PreId.ToString();
            int anchorIndex = 1;

            foreach (var wp in conn.Waypoints)
            {
                result.Add((preNodeId, anchorIndex, wp));
                anchorIndex++;
            }
        }

        return result;
    }

}
