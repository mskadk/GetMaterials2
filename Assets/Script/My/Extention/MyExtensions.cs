using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Assets.Script.My.Extention
{
    /// <summary>
    /// 锚点方向枚举
    /// </summary>
    public enum AnchorDirection
    {
        Center, // c - 缺省值
        Top,    // t
        Bottom, // b
        Left,   // l
        Right   // r
    }

    /// <summary>
    /// 解析后的路径连接信息
    /// </summary>
    public struct PathConnection
    {
        public string PreId;
        public AnchorDirection StartDirection;  // 起始节点的锚点方向
        public AnchorDirection EndDirection;    // 终止节点的锚点方向
        public List<Vector2> Waypoints;        // 中间点列表

        public PathConnection(string preId, AnchorDirection startDir, AnchorDirection endDir, List<Vector2> waypoints)
        {
            PreId = preId;
            StartDirection = startDir;
            EndDirection = endDir;
            Waypoints = waypoints ?? new List<Vector2>();
        }
    }

    public static class MyExtensions
    {
        // 坐标精度：三位小数
        private const string FLOAT_FORMAT = "F1";

        #region 方向标记转换

        public static AnchorDirection ParseDirection(string mark)
        {
            if (string.IsNullOrEmpty(mark)) return AnchorDirection.Center;
            return mark.Trim().ToLower() switch
            {
                "t" => AnchorDirection.Top,
                "b" => AnchorDirection.Bottom,
                "l" => AnchorDirection.Left,
                "r" => AnchorDirection.Right,
                "c" => AnchorDirection.Center,
                _ => AnchorDirection.Center
            };
        }

        public static string DirectionToMark(AnchorDirection dir)
        {
            return dir switch
            {
                AnchorDirection.Top => "t",
                AnchorDirection.Bottom => "b",
                AnchorDirection.Left => "l",
                AnchorDirection.Right => "r",
                AnchorDirection.Center => "c",
                _ => "c"
            };
        }

        /// <summary>
        /// 获取节点上指定方向锚点的世界坐标
        /// </summary>
        public static Vector3 GetAnchorWorldPosition(Transform nodeTransform, AnchorDirection dir)
        {
            string anchorName = dir switch
            {
                AnchorDirection.Top => "anc_top",
                AnchorDirection.Bottom => "anc_bottom",
                AnchorDirection.Left => "anc_left",
                AnchorDirection.Right => "anc_right",
                _ => null
            };

            if (anchorName != null)
            {
                Transform anchorTrans = nodeTransform.Find(anchorName);
                if (anchorTrans != null)
                {
                    return anchorTrans.position;
                }
            }

            // Center 或找不到锚点时返回节点中心
            return nodeTransform.position;
        }

        #endregion

        /// <summary>
        /// 将输入的字符串以"|"分隔，组成 ID 字符串数组
        /// </summary>
        public static List<string> ToList(this string input)
        {
            if (string.IsNullOrEmpty(input) || input.Equals("-1"))
            {
                return new();
            }
            return input.Split("|")
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrEmpty(item))
                .ToList();
        }

        #region 新格式解析 (v2)

        /// <summary>
        /// 解析新格式路径字符串为 PathConnection 列表
        /// 新格式: "preId,startDir:x,y_x,y:endDir" 多组用 "|" 分隔
        /// 兼容旧格式: "preId_x_y_x_y"
        /// </summary>
        public static List<PathConnection> ParsePathConnections(this string input)
        {
            List<PathConnection> result = new();

            if (string.IsNullOrEmpty(input) || input == "-1")
                return result;

            string[] groups = input.Split('|');

            foreach (string group in groups)
            {
                if (string.IsNullOrWhiteSpace(group)) continue;

                // 判断是新格式还是旧格式：新格式包含 ":"
                if (group.Contains(':'))
                {
                    var conn = ParseNewFormatGroup(group);
                    if (conn.HasValue) result.Add(conn.Value);
                }
                else
                {
                    var conn = ParseOldFormatGroup(group);
                    if (conn.HasValue) result.Add(conn.Value);
                }
            }

            return result;
        }

        /// <summary>
        /// 解析新格式单组: "preId,startDir:x,y_x,y:endDir"
        /// </summary>
        private static PathConnection? ParseNewFormatGroup(string group)
        {
            // 用 ':' 分隔为最多3段
            string[] sections = group.Split(':');
            if (sections.Length < 1) return null;

            // === 第1段：起始信息 "preId,startDir" 或 "preId" ===
            string startSection = sections[0];
            string[] startParts = startSection.Split(',');
            string preId = startParts[0].Trim();
            if (string.IsNullOrEmpty(preId)) return null;

            AnchorDirection startDir = AnchorDirection.Center;
            if (startParts.Length > 1)
                startDir = ParseDirection(startParts[1]);

            // === 第2段：中间点 "x,y_x,y" （可能为空或不存在）===
            List<Vector2> waypoints = new();
            AnchorDirection endDir = AnchorDirection.Center;

            if (sections.Length >= 2 && !string.IsNullOrWhiteSpace(sections[1]))
            {
                string waypointSection = sections[1];
                string[] pointStrs = waypointSection.Split('_');
                foreach (string pointStr in pointStrs)
                {
                    if (string.IsNullOrWhiteSpace(pointStr)) continue;
                    string[] coords = pointStr.Split(',');
                    if (coords.Length >= 2 &&
                        float.TryParse(coords[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                        float.TryParse(coords[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                    {
                        waypoints.Add(new Vector2(x, y));
                    }
                }
            }

            // === 第3段：终点方向 "endDir" （可能不存在）===
            if (sections.Length >= 3 && !string.IsNullOrWhiteSpace(sections[2]))
            {
                endDir = ParseDirection(sections[2]);
            }

            return new PathConnection(preId, startDir, endDir, waypoints);
        }

        /// <summary>
        /// 解析旧格式单组: "preId_x_y_x_y" → 兼容转换为 PathConnection（方向均为 Center）
        /// </summary>
        private static PathConnection? ParseOldFormatGroup(string group)
        {
            string[] parts = group.Split('_');
            if (parts.Length < 1) return null;
            string preId = parts[0].Trim();
            if (string.IsNullOrEmpty(preId)) return null;

            List<Vector2> waypoints = new();
            for (int i = 1; i < parts.Length - 1; i += 2)
            {
                if (float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(parts[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                {
                    waypoints.Add(new Vector2(x, y));
                }
            }

            return new PathConnection(preId, AnchorDirection.Center, AnchorDirection.Center, waypoints);
        }

        /// <summary>
        /// 将 PathConnection 列表序列化为新格式字符串
        /// </summary>
        public static string SerializePathConnections(List<PathConnection> connections)
        {
            if (connections == null || connections.Count == 0) return "-1";

            List<string> groups = new();
            foreach (var conn in connections)
            {
                groups.Add(SerializeOneConnection(conn));
            }
            return string.Join("|", groups);
        }

        /// <summary>
        /// 序列化单个 PathConnection 为新格式字符串
        /// </summary>
        private static string SerializeOneConnection(PathConnection conn)
        {
            // 起始段
            string startMark = DirectionToMark(conn.StartDirection);
            string startSection = $"{conn.PreId},{startMark}";

            // 中间点段
            string waypointSection = "";
            if (conn.Waypoints != null && conn.Waypoints.Count > 0)
            {
                waypointSection = string.Join("_", conn.Waypoints.Select(wp =>
                    $"{wp.x.ToString(FLOAT_FORMAT, CultureInfo.InvariantCulture)},{wp.y.ToString(FLOAT_FORMAT, CultureInfo.InvariantCulture)}"));
            }

            // 终点段
            string endMark = DirectionToMark(conn.EndDirection);

            return $"{startSection}:{waypointSection}:{endMark}";
        }

        #endregion

        #region 兼容旧接口

        /// <summary>
        /// 【兼容旧代码】解析路径节点字符串为世界坐标列表
        /// 返回 Vector3: x=preId, y=worldX, z=worldY
        /// 同时支持新旧格式
        /// </summary>
        public static List<Vector3> ParsePathNodeList(this string input)
        {
            List<Vector3> result = new();

            if (string.IsNullOrEmpty(input) || input == "-1")
                return result;

            var connections = ParsePathConnections(input);
            foreach (var conn in connections)
            {
                foreach (var wp in conn.Waypoints)
                {
                    if (float.TryParse(conn.PreId, NumberStyles.Float, CultureInfo.InvariantCulture, out float numericPreId))
                    {
                        result.Add(new Vector3(numericPreId, wp.x, wp.y));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 旧方法保持兼容
        /// </summary>
        [Obsolete("请使用 ParsePathConnections 获取完整的连接信息")]
        public static List<Vector3Int> ParesV3IList(this string input)
        {
            var floatList = ParsePathNodeList(input);
            return floatList.Select(v => new Vector3Int(
                (int)v.x,Mathf.RoundToInt(v.y),
                Mathf.RoundToInt(v.z)
            )).ToList();
        }

        #endregion

        #region 路径编辑操作（适配新格式）

        /// <summary>
        /// 替换前置科技字符串中某个科技的id
        /// </summary>
        public static string ReplacePreTech(this string input, string oldId, string newId, string args = "|")
        {
            if (string.IsNullOrEmpty(input) || input == "-1") return input;

            List<string> list = input.Split(args).ToList();
            var i = list.IndexOf(oldId);
            if (i >= 0) list[i] = newId;

            return string.Join(args, list);
        }

        /// <summary>
        /// 替换路径字符串中某个科技的id（支持新旧格式）
        /// </summary>
        public static string ReplacePathNode(this string input, string oldId, string newId)
        {
            if (string.IsNullOrEmpty(input) || input == "-1") return input;

            var connections = ParsePathConnections(input);
            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i].PreId == oldId)
                {
                    var conn = connections[i];
                    conn.PreId = newId;
                    connections[i] = conn;
                }
            }
            return SerializePathConnections(connections);
        }

        /// <summary>
        /// 从路径列表中剔除目标id的整条数据
        /// </summary>
        public static string RemoveIdPrePath(this string input, string target)
        {
            if (string.IsNullOrEmpty(input) || input == "-1") return input;

            var connections = ParsePathConnections(input);
            connections.RemoveAll(c => c.PreId == target);

            return connections.Count == 0 ? "-1" : SerializePathConnections(connections);
        }

        /// <summary>
        /// 从竖线分隔的id列表中剔除目标id
        /// </summary>
        public static string RemoveIdPreNode(this string input, string target)
        {
            if (string.IsNullOrEmpty(input) || input == "-1") return input;

            var list = input.Split('|').ToList();
            list.Remove(target);

            return list.Count == 0 ? "-1" : string.Join("|", list);
        }

        /// <summary>
        /// 将List<string>以符号组成字符串
        /// </summary>
        public static string ToString(this List<string> input, string args = "|")
        {
            if (input == null || input.Count == 0) return "-1";
            return string.Join(args, input);
        }

        /// <summary>
        /// 更新前置路径中某个id的路径字段（新格式）
        /// </summary>
        public static string UpdatePathNodeById(this string input, string id, string newPath)
        {
            if (string.IsNullOrEmpty(input) || input == "-1")
            {
                return string.IsNullOrEmpty(newPath) ? "-1" : newPath;
            }

            var connections = ParsePathConnections(input);
            string targetId = id;

            if (!string.IsNullOrEmpty(newPath))
            {
                var newConns = ParsePathConnections(newPath);

                // 用循环查找，避免 struct FirstOrDefault 问题
                PathConnection? newConn = null;
                foreach (var nc in newConns)
                {
                    if (nc.PreId == targetId) { newConn = nc; break; }
                }

                if (!newConn.HasValue)
                {
                    // newPath 中没有找到对应 preId 的连接，直接返回
                    return input;
                }

                bool found = false;
                for (int i = 0; i < connections.Count; i++)
                {
                    if (connections[i].PreId == targetId)
                    {
                        connections[i] = newConn.Value;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    connections.Add(newConn.Value);
                }
            }
            else
            {
                connections.RemoveAll(c => c.PreId == targetId);
            }

            return connections.Count == 0 ? "-1" : SerializePathConnections(connections);
        }


        /// <summary>
        /// 在指定的前置节点路径中插入一个新的坐标点
        /// </summary>
        public static string InsertPathNode(this string pathNodeStr, string targetPreId, int insertIndex, Vector2 newPoint)
        {
            string preId = targetPreId;

            var connections = ParsePathConnections(pathNodeStr);
            bool found = false;

            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i].PreId == preId)
                {
                    var conn = connections[i];
                    int idx = Mathf.Clamp(insertIndex - 1, 0, conn.Waypoints.Count);
                    conn.Waypoints.Insert(idx, newPoint);
                    connections[i] = conn;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                connections.Add(new PathConnection(preId, AnchorDirection.Center, AnchorDirection.Center,
                    new List<Vector2> { newPoint }));
            }

            return SerializePathConnections(connections);
        }

        /// <summary>
        /// 删除指定前置节点路径中的某个坐标点
        /// </summary>
        public static string RemovePathNode(this string pathNodeStr, string targetPreId, int anchorIndex)
        {
            if (string.IsNullOrEmpty(pathNodeStr) || pathNodeStr == "-1") return pathNodeStr;

            string preId = targetPreId;
            var connections = ParsePathConnections(pathNodeStr);

            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i].PreId == preId)
                {
                    var conn = connections[i];
                    int idx = anchorIndex - 1;
                    if (idx >= 0 && idx < conn.Waypoints.Count)
                    {
                        conn.Waypoints.RemoveAt(idx);
                    }

                    if (conn.Waypoints.Count == 0)
                    {
                        // 没有中间点了，但保留连接信息（方向信息仍有意义）
                        connections[i] = conn;
                    }
                    else
                    {
                        connections[i] = conn;
                    }
                    break;
                }
            }

            return connections.Count == 0 ? "-1" : SerializePathConnections(connections);
        }

        /// <summary>
        /// 格式化世界坐标为字符串（三位小数）
        /// </summary>
        public static string FormatWorldPos(float x, float y)
        {
            return $"{x.ToString(FLOAT_FORMAT)}_{y.ToString(FLOAT_FORMAT)}";
        }

        /// <summary>
        /// 格式化世界坐标为字符串（三位小数）
        /// </summary>
        public static string FormatWorldPos(Vector2 pos)
        {
            return FormatWorldPos(pos.x, pos.y);
        }

        #endregion
    }
}
