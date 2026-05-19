using Assets.Script.My.Extention;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 删除节点命令
/// </summary>
public class DeleteNodeCommand : ICommand
{
    private UIReferences ui;

    // 保存节点数据用于撤销（重建节点所需的数据）
    private Science deletedScience;

    // 记录受影响的其他节点数据，用于恢复连线关系
    private Dictionary<string, string> affectedPreTechs;
    private Dictionary<string, string> affectedPathNodes;

    public string Description => $"删除节点 {deletedScience.Id}:{deletedScience.Name}";

    public DeleteNodeCommand(Science science, UIReferences uiRefs)
    {
        this.ui = uiRefs;
        // 此时 science 对象还在内存中，我们持有它的引用即可
        this.deletedScience = science;

        // --- 记录受影响的后续节点数据（用于撤销时恢复连线）---
        affectedPreTechs = new Dictionary<string, string>();
        affectedPathNodes = new Dictionary<string, string>();

        foreach (var aftId in science.After_technology)
        {
            if (DataManager.Instance.TryGetScience(aftId, out var aftScience))
            {
                affectedPreTechs[aftId] = aftScience.Pre_technology;
                affectedPathNodes[aftId] = aftScience.PathNode;
            }
        }
    }

    public void Execute()
    {
        string id = deletedScience.Id;

        // 1. 更新 UI 显示（减少计数）
        EventCenter.Instance.TriggerTechTreeItemUpdate(deletedScience.Building_unlock, "");
        EventCenter.Instance.TriggerTechTreeItemUpdate(deletedScience.NonBuilding_unlock, "");

        // 2. 处理数据关联：从前置节点的 After 列表中移除自己
        foreach (var preId in deletedScience.Pre_technology.ToList())
        {
            if (DataManager.Instance.TryGetScience(preId, out var preScience))
            {
                preScience.After_technology.Remove(id);
            }
        }

        // 3. 处理数据关联：从后续节点的 Pre 和 Path 中移除自己
        foreach (var aftId in deletedScience.After_technology)
        {
            if (DataManager.Instance.TryGetScience(aftId, out var aftScience))
            {
                aftScience.Pre_technology = aftScience.Pre_technology.RemoveIdPreNode(id.ToString());
                aftScience.PathNode = aftScience.PathNode.RemoveIdPrePath(id.ToString());

                // 刷新后续节点的外观（线条会消失）
                NodeManager.Instance.GetNode(aftId)?.UpdateNodeAppearance();
            }
        }

        // 4. 从数据层彻底移除
        DataManager.Instance.RemoveScience(id);

        // 5. 【关键修改】从场景中彻底销毁物体，而不是隐藏
        NodeManager.Instance.DestroyNodeObject(id);
    }

    public void Undo()
    {
        string id = deletedScience.Id;

        // 1. 恢复数据到字典
        DataManager.Instance.AddScience(deletedScience);

        // 2. 【关键修改】使用 NodeManager 重新创建物体
        // 不需要 SetActive(true)，因为旧物体已经被销毁了，必须重生
        NodeManager.Instance.CreateNodeObject(deletedScience);

        // 3. 恢复前置节点的关联
        foreach (var pre in deletedScience.Pre_technology.Split('|'))
        {
            if (!string.IsNullOrWhiteSpace(pre))
            {
                string preId = pre.Trim();
                if (DataManager.Instance.TryGetScience(preId, out var preScience))
                {
                    if (!preScience.After_technology.Contains(id))
                    {
                        preScience.After_technology.Add(id);
                    }
                }
            }
        }

        // 4. 恢复后续节点的关联（前置字段和路径字段）
        foreach (var kvp in affectedPreTechs)
        {
            if (DataManager.Instance.TryGetScience(kvp.Key, out var aftScience))
            {
                aftScience.Pre_technology = kvp.Value;
                aftScience.PathNode = affectedPathNodes[kvp.Key];

                // 刷新后续节点外观（线条会重新出现）
                NodeManager.Instance.GetNode(kvp.Key)?.UpdateNodeAppearance();
            }
        }

        // 5. 恢复 UI 显示
        EventCenter.Instance.TriggerTechTreeItemUpdate("", deletedScience.Building_unlock);
        EventCenter.Instance.TriggerTechTreeItemUpdate("", deletedScience.NonBuilding_unlock);
    }
}
