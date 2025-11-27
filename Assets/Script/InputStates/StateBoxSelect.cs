// 修改 StateBoxSelect.cs

using System.Collections.Generic;
using UnityEngine;
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

        // 1. 选中框选区域内的节点和已存在的锚点
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

        // 2. 计算式选中锚点（即使锚点GameObject不存在）
        var newlySelectedAnchors = SelectAnchorsByCalculation(context, worldMin, worldMax);

        // 3. 如果有新选中的锚点，确保它们的节点显示锚点
        if (newlySelectedAnchors.Count > 0)
        {
            EnsureAnchorsVisible(context, newlySelectedAnchors);
        }

        context.CheckEditPanelState();
    }

    /// <summary>
    /// 通过计算选中锚点，返回新选中的锚点信息
    /// </summary>
    private List<(int targetNodeId, string preNodeId, int anchorIndex, Vector3Int gridPos)> SelectAnchorsByCalculation(
        InputManager context, Vector3 worldMin, Vector3 worldMax)
    {
        var grid = context.UI.grid;
        var newlySelected = new List<(int, string, int, Vector3Int)>();

        Vector3Int gridMin = grid.WorldToCell(worldMin);
        Vector3Int gridMax = grid.WorldToCell(worldMax);

        int minX = Mathf.Min(gridMin.x, gridMax.x);
        int maxX = Mathf.Max(gridMin.x, gridMax.x);
        int minY = Mathf.Min(gridMin.y, gridMax.y);
        int maxY = Mathf.Max(gridMin.y, gridMax.y);

        var selectedNodes = SelectionManager.Instance.GetSelectedNodes();
        foreach (var nodeObj in selectedNodes)
        {
            if (nodeObj == null) continue;
            var node = nodeObj.GetComponent<Node>();
            if (node == null || node.sc == null) continue;

            var anchorPositions = ParseAnchorPositions(node.sc);

            foreach (var anchorInfo in anchorPositions)
            {
                int gridX = anchorInfo.gridPos.x;
                int gridY = anchorInfo.gridPos.y;

                if (gridX >= minX && gridX <= maxX && gridY >= minY && gridY <= maxY)
                {
                    // 检查是否已经选中
                    if (!SelectionManager.Instance.ContainsAnchorPosition(
                        node.sc.Id, anchorInfo.preNodeId, anchorInfo.anchorIndex))
                    {
                        Vector3Int gridPos = new Vector3Int(gridX, gridY, 0);
                        SelectionManager.Instance.AddAnchorByPosition(
                            node.sc.Id,
                            anchorInfo.preNodeId,
                            anchorInfo.anchorIndex,
                            gridPos
                        );
                        newlySelected.Add((node.sc.Id, anchorInfo.preNodeId, anchorInfo.anchorIndex, gridPos));
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
        List<(int targetNodeId, string preNodeId, int anchorIndex, Vector3Int gridPos)> anchors)
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

        // 更新 InputManager 的锚点可见状态
        context.SetAnchorsVisibleInNormalMode(true);
    }

    private List<(string preNodeId, int anchorIndex, Vector2Int gridPos)> ParseAnchorPositions(Science sc)
    {
        var result = new List<(string preNodeId, int anchorIndex, Vector2Int gridPos)>();

        if (string.IsNullOrEmpty(sc.PathNode) || sc.PathNode == "-1")
            return result;

        var paths = sc.PathNode.Split('|');
        foreach (var path in paths)
        {
            var segments = path.Split('_');
            if (segments.Length < 3) continue;

            string preNodeId = segments[0];

            int anchorIndex = 1;
            for (int i = 1; i + 1 < segments.Length; i += 2)
            {
                if (int.TryParse(segments[i], out int y) && int.TryParse(segments[i + 1], out int x))
                {
                    result.Add((preNodeId, anchorIndex, new Vector2Int(x, y)));
                    anchorIndex++;
                }
            }
        }

        return result;
    }
}
