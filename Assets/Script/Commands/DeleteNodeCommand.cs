using Assets.Script.My.Extention;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 删除节点命令
/// 当前存在问题，要么就是创建节点的时候，可用id没有更新，要么就是删除节点的时候，可用id没有还原，体现在删除节点之后，再创建新节点就没法删除了，哎！！！
/// </summary>
public class DeleteNodeCommand : ICommand
{
    private UIReferences ui;

    // 保存节点数据用于撤销
    private Science deletedScience;
    private Vector3 nodePosition;
    private List<int> preNodeIds;
    private Dictionary<int, string> affectedPreTechs;  // 受影响的后续节点的前置字段
    private Dictionary<int, string> affectedPathNodes; // 受影响的后续节点的路径字段

    public string Description => $"删除节点 {deletedScience.Id}:{deletedScience.Name}";

    public DeleteNodeCommand(Science science, UIReferences uiRefs)
    {
        this.ui = uiRefs;
        this.deletedScience = science;

        // 保存必要信息用于撤销
        var node = ui.tilemap.transform.Find(science.Id.ToString());
        if (node)
        {
            nodePosition = node.position;
        }

        // 保存前置节点ID列表
        preNodeIds = science.Pre_technology.ToList();

        // 保存受影响的后续节点的原始数据
        affectedPreTechs = new Dictionary<int, string>();
        affectedPathNodes = new Dictionary<int, string>();
        foreach (var aftId in science.After_technology)
        {
            if (DataManager.Instance.ScienceDict.TryGetValue(aftId, out var aftScience))
            {
                affectedPreTechs[aftId] = aftScience.Pre_technology;
                affectedPathNodes[aftId] = aftScience.PathNode;
            }
        }
    }

    public void Execute()
    {
        int id = deletedScience.Id;

        // 更新科技树项
        EventCenter.Instance.TriggerTechTreeItemUpdate(deletedScience.Building_unlock, "");
        EventCenter.Instance.TriggerTechTreeItemUpdate(deletedScience.NonBuilding_unlock, "");

        // 删除前置节点的后继
        foreach (var preId in deletedScience.Pre_technology.ToList())
        {
            if (DataManager.Instance.ScienceDict.TryGetValue(preId, out var preScience))
            {
                preScience.After_technology.Remove(id);
            }
        }

        // 删除后续节点的前置字段
        foreach (var aftId in deletedScience.After_technology)
        {
            if (DataManager.Instance.ScienceDict.TryGetValue(aftId, out var aftScience))
            {
                aftScience.Pre_technology = aftScience.Pre_technology.RemoveIdPreNode(id.ToString());
                aftScience.PathNode = aftScience.PathNode.RemoveIdPrePath(id.ToString());
                ui.tilemap.transform.Find(aftId.ToString())?.GetComponent<Node>().UpdateNodeAppearance();
            }
        }

        // 从字典删除
        DataManager.Instance.ScienceDict.Remove(id);

        // 销毁GameObject
        var nodeObj = ui.tilemap.transform.Find(id.ToString());
        if (nodeObj != null)
        {
            nodeObj.gameObject.SetActive(false); // 先隐藏，不直接销毁（方便撤销）
        }
    }

    public void Undo()
    {
        int id = deletedScience.Id;

        // 恢复到字典
        DataManager.Instance.ScienceDict.Add(id, deletedScience);

        // 恢复GameObject
        var nodeObj = ui.tilemap.transform.Find(id.ToString());
        if (nodeObj != null)
        {
            nodeObj.gameObject.SetActive(true);
        }
        else
        {
            // 如果已被销毁，需要重新创建
            GameObject newNode = Object.Instantiate(ui.nodePrefab, nodePosition, Quaternion.identity, ui.tilemap.transform);
            newNode.name = id.ToString();
            newNode.GetComponent<Node>().sc = deletedScience;
        }

        // 恢复前置节点的后继
        foreach (var preId in preNodeIds)
        {
            if (DataManager.Instance.ScienceDict.TryGetValue(preId, out var preScience))
            {
                if (!preScience.After_technology.Contains(id))
                {
                    preScience.After_technology.Add(id);
                }
            }
        }

        // 恢复后续节点的前置和路径字段
        foreach (var kvp in affectedPreTechs)
        {
            if (DataManager.Instance.ScienceDict.TryGetValue(kvp.Key, out var aftScience))
            {
                aftScience.Pre_technology = kvp.Value;
                aftScience.PathNode = affectedPathNodes[kvp.Key];
                ui.tilemap.transform.Find(kvp.Key.ToString())?.GetComponent<Node>().UpdateNodeAppearance();
            }
        }

        // 恢复科技树项
        EventCenter.Instance.TriggerTechTreeItemUpdate("", deletedScience.Building_unlock);
        EventCenter.Instance.TriggerTechTreeItemUpdate("", deletedScience.NonBuilding_unlock);
    }
}
