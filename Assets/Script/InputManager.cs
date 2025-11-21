using Assets.Script.My.Extention;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
    #region 引用
    public UIReferences UI { get; private set; }
    public GameObject CurrentEditPanel { get; private set; }
    #endregion

    #region 状态机
    private IInputState currentState;
    #endregion

    public void Initialize()
    {
        UI = UIReferences.Instance;
        ChangeState(new StateIdle());
    }

    private void Update()
    {
        if (currentState != null)
            currentState.OnUpdate(this);

        UpdateKeyboardEvent();
        UpdateDebugInput();
    }

    public void ChangeState(IInputState newState)
    {
        currentState?.OnExit(this);

        currentState = newState;

        currentState?.OnEnter(this);
    }

    #region 工具方法 (供状态类调用)

    public Vector3 GetMouseWorldPos()
    {
        Vector3 p = UI.camSence.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0;
        return p;
    }

    public GameObject RayDetect(string tagFilter = null)
    {
        GameObject ob = null;
        Vector3 worldPos = GetMouseWorldPos();
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector3.forward, Mathf.Infinity);
        if (hit)
        {
            ob = hit.transform.gameObject;
        }
        if (hit && tagFilter != null && hit.transform.gameObject.tag != tagFilter)
        {
            return null;
        }
        return ob;
    }

    // 供 StateDragNode 调用
    public void MoveNodeVisual(GameObject obj, Vector3Int gridPosI)
    {
        Node node = obj.GetComponent<Node>();
        node.UpdateGridPos(gridPosI);

        Vector3 newPos = UI.grid.CellToWorld(gridPosI);
        newPos.z = 0;
        obj.transform.position = newPos;

        // 更新子节点连线
        foreach (var afterId in node.sc.After_technology)
        {
            // 这里假设 Tilemap 下直接挂着节点，根据原代码逻辑
            GameObject child = UI.tilemap.transform.Find(afterId.ToString())?.gameObject;
            child?.GetComponent<Node>().UpdateNodeAppearance();
        }

        // 如果是当前编辑的节点，同步更新面板数值
        if (CurrentEditPanel && CurrentEditPanel.GetComponent<PanelScienceEdit>().sc.Id == node.sc.Id)
        {
            CurrentEditPanel.GetComponent<PanelScienceEdit>()
                .UpdatePositionByDrag(new Vector2Int(gridPosI.y, gridPosI.x)); // 注意 xy 对应

            node.ClearAnchor();
            node.UpdateLineAnchor();
        }
    }

    // 供 StateDragAnchor 调用
    public void MoveAnchorVisual(GameObject anchorObj, Vector3 worldPos, Vector3Int gridPosI)
    {
        int lrIndex = int.Parse(anchorObj.name);
        LineRenderer lr = anchorObj.GetComponentInParent<LineRenderer>();

        Vector2 anchorMoveTo = new Vector2(worldPos.x, worldPos.y);
        Vector3 lineIndexAt = new Vector3(anchorMoveTo.x, anchorMoveTo.y, 1); // Z=1 确保显示
        anchorObj.transform.position = anchorMoveTo;
        lr.SetPosition(lrIndex, lineIndexAt);

        // 更新数据
        string nodeFrom = lr.gameObject.name.Split("->")[0];
        string nodeTo = lr.gameObject.name.Split("->")[1];

        if (DataManager.Instance.ScienceDict.TryGetValue(int.Parse(nodeTo), out var sc))
        {
            int positionCount = lr.positionCount;
            string newpos = null;

            for (int i = 1; i < positionCount - 1; i++)
            {
                if (newpos is not null) newpos += "_";
                // 注意这里原代码是用 lr.GetPosition(i) 转 Cell，可能存在精度问题，
                // 既然我们已经有了 gridPosI，其实可以直接用，但为了保持对多点连线的一致性，
                // 还是遍历 LineRenderer 的点比较安全
                var cellPos = UI.grid.WorldToCell(lr.GetPosition(i));
                newpos += $"{cellPos.y}_{cellPos.x}";
            }

            sc.PathNode = sc.PathNode.UpdatePathNodeById(nodeFrom, newpos);

            if (CurrentEditPanel)
            {
                CurrentEditPanel.GetComponent<PanelScienceEdit>().UpdatePrePath(sc.PathNode);
            }
        }
    }

    // 供 StateIdle 调用
    public (LineRenderer, int, Vector3) DetectLineNearMouse()
    {
        Vector3 mousePos = GetMouseWorldPos();
        List<GameObject> linestoCheck = new List<GameObject>();

        if (CurrentEditPanel != null)
        {
            int currentId = CurrentEditPanel.GetComponent<PanelScienceEdit>().sc.Id;
            Node currentNode = NodeManager.Instance.GetNode(currentId); // 假设你有 NodeManager

            if (currentNode != null)
            {
                foreach (Transform child in currentNode.transform)
                {
                    if (child.tag == Constants.Tags.NodeLine)
                        linestoCheck.Add(child.gameObject);
                }
            }
        }
        else
        {
            return (null, -1, Vector3.zero);
        }

        float minDst = 5f;
        LineRenderer bestLr = null;
        int bestIndex = -1;
        Vector3 bestPoint = Vector3.zero;

        foreach (var lineObj in linestoCheck)
        {
            LineRenderer lr = lineObj.GetComponent<LineRenderer>();
            if (!lr) continue;

            for (int i = 0; i < lr.positionCount - 1; i++)
            {
                Vector3 p1 = lr.GetPosition(i);
                Vector3 p2 = lr.GetPosition(i + 1);
                p1.z = 0; p2.z = 0;

                // 简易的点到线段距离计算
                float dst = HandleUtility_DistancePointLine(mousePos, p1, p2);
                if (dst < minDst)
                {
                    minDst = dst;
                    bestLr = lr;
                    bestIndex = i + 1;
                    bestPoint = GetMouseWorldPos();
                }
            }
        }
        return (bestLr, bestIndex, bestPoint);
    }

    private float HandleUtility_DistancePointLine(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 proj = ProjectPointOnLineSegment(a, b, p);
        return Vector3.Distance(p, proj);
    }

    private Vector3 ProjectPointOnLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point)
    {
        Vector3 vector = linePoint2 - linePoint1;
        Vector3 vector2 = point - linePoint1;
        float d = vector.sqrMagnitude;
        if (d == 0f) return linePoint1;
        float t = Vector3.Dot(vector2, vector) / d;
        if (t < 0f) return linePoint1;
        if (t > 1f) return linePoint2;
        return linePoint1 + vector * t;
    }

    #endregion

    #region 外部调用接口
    public void SetCurrentEditPanel(GameObject panel)
    {
        CurrentEditPanel = panel;
    }

    public void OpenEditPanel(GameObject nodeObj)
    {
        string nodeId = nodeObj.name;
        Node node = nodeObj.GetComponent<Node>();

        CurrentEditPanel = Instantiate(UI.panelNodeEditPrefab);
        CurrentEditPanel.transform.SetParent(
            UI.canvas.transform.Find(Constants.UIPath.PanelRightContent),
            false
        );
        CurrentEditPanel.name = $"{nodeId}(Edit)";

        var panelScript = CurrentEditPanel.GetComponent<PanelScienceEdit>();
        panelScript.node = node;
        panelScript.sc = node.sc;

        node.SetSelectStyle(true);
        EventCenter.Instance.TriggerNodeSelected(node);
        EventCenter.Instance.TriggerLogMessage($"编辑节点：{nodeId}:{node.sc.Name}");
    }

    public void CheckEditPanelState()
    {
        // 如果多选了，且当前有编辑面板，则关闭面板
        if (SelectionManager.Instance.Count > 1)
        {
            if (CurrentEditPanel)
            {
                CurrentEditPanel.GetComponent<PanelScienceEdit>().DestoryPanel();
                CurrentEditPanel = null;
            }
        }
    }
    #endregion

    #region 键盘事件 (通常独立于鼠标状态)
    private void UpdateKeyboardEvent()
    {
        // 撤销/重做
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Z)) { CommandManager.Instance.Undo(); return; }
            if (Input.GetKeyDown(KeyCode.Y)) { CommandManager.Instance.Redo(); return; }
        }

        if (!EventSystem.current.currentSelectedGameObject)
        {
            // 删除
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                // 优先删除 Anchor
                if (SelectionManager.Instance.SelectedAnchor != null)
                {
                    var cmd = new DeleteAnchorCommand(SelectionManager.Instance.SelectedAnchor);
                    CommandManager.Instance.ExecuteCommand(cmd);
                    SelectionManager.Instance.ClearAnchor();
                    return;
                }

                // 删除节点 (仅在编辑模式下)
                if (CurrentEditPanel)
                {
                    DeleteCurrentNode();
                }
            }

            // ESC
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (CurrentEditPanel)
                {
                    CurrentEditPanel.GetComponent<PanelScienceEdit>().DestoryPanel();
                    CurrentEditPanel = null;
                }
            }
        }
    }

    private void DeleteCurrentNode()
    {
        var panelScript = CurrentEditPanel.GetComponent<PanelScienceEdit>();
        var sc = panelScript.sc;
        var deleteCmd = new DeleteNodeCommand(sc, UI);

        SelectionManager.Instance.ClearNodes(); // 清除选择防止访问空引用
        CommandManager.Instance.ExecuteCommand(deleteCmd);

        panelScript.DestoryPanel();
        CurrentEditPanel = null;
    }

    private void UpdateDebugInput()
    {
        // 你的调试代码
        if (Input.GetKeyDown(KeyCode.Slash)) {
            foreach (var sc in DataManager.Instance.ScienceDict)
                Debug.Log($"KEY:{sc.Key}, VALUE:{sc.Value}");
        }
        if (Input.GetKeyDown(KeyCode.Period)) {
            var hitObj = RayDetect();
            if (hitObj != null && hitObj.tag == Constants.Tags.Node)
            {
                var node = hitObj.GetComponent<Node>();
                if (DataManager.Instance.ScienceDict.TryGetValue(node.sc.Id, out Science sc))
                    Debug.Log($"数据：{sc}");
                Debug.Log($"After:{string.Join("|", sc.After_technology)}");
            }
        }
    }
    #endregion
}
