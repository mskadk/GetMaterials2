using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance;

    private HashSet<GameObject> selectedObjects = new HashSet<GameObject>();
    public GameObject SelectedAnchor { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public bool Contains(GameObject obj) => selectedObjects.Contains(obj);
    public int Count => selectedObjects.Count;
    public List<GameObject> GetSelectedNodes() => selectedObjects.ToList();

    public void AddNode(GameObject nodeObj)
    {
        if (selectedObjects.Contains(nodeObj)) return;
        selectedObjects.Add(nodeObj);
        nodeObj.GetComponent<Node>().SetSelectStyle(true);
    }

    public void RemoveNode(GameObject nodeObj)
    {
        if (!selectedObjects.Contains(nodeObj)) return;
        selectedObjects.Remove(nodeObj);
        var node = nodeObj.GetComponent<Node>();
        node.SetSelectStyle(false);
        node.ClearAnchor(); // 只有选中时才显示Anchor，取消选中清除
    }

    public void ClearNodes()
    {
        foreach (var obj in selectedObjects.ToList())
        {
            var node = obj.GetComponent<Node>();
            if (node)
            {
                node.SetSelectStyle(false);
                node.ClearAnchor();
            }
        }
        selectedObjects.Clear();
    }

    public void SelectAnchor(GameObject anchor)
    {
        // 之前的Anchor变白
        if (SelectedAnchor != null)
            SelectedAnchor.GetComponent<SpriteRenderer>().color = Constants.Colors.AnchorNormal;

        SelectedAnchor = anchor;

        // 现在的Anchor变黄
        if (SelectedAnchor != null)
            SelectedAnchor.GetComponent<SpriteRenderer>().color = Constants.Colors.AnchorSelected;
    }

    public void ClearAnchor()
    {
        SelectAnchor(null);
    }

    public void ToggleSelection(GameObject obj)
    {
        if (selectedObjects.Contains(obj))
            RemoveNode(obj);
        else
            AddNode(obj);
    }
}
