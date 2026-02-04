using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Assets.Script.My.Extention
{
    public static class MyExtensions
    {
        // 坐标精度：三位小数
        private const string FLOAT_FORMAT = "F3";

        /// <summary>
        /// 将输入的字符串以"|"分隔，组成整形数组
        /// <para>空字符串、-1字符串返回空整形列表</para>
        /// </summary>
        public static List<int> ToList(this string input)
        {
            if (string.IsNullOrEmpty(input) || input.Equals("-1"))
            {
                return new();
            }
            return input.Split("|")
                .Select(item => int.Parse(item))
                .ToList();
        }

        /// <summary>
        /// 解析路径节点字符串为世界坐标列表
        /// 格式: "preId_x_y_x_y|preId_x_y" => List of (preId, x, y)
        /// 返回 Vector3: x=preId, y=worldX, z=worldY
        /// </summary>
        public static List<Vector3> ParsePathNodeList(this string input)
        {
            List<Vector3> result = new();

            if (string.IsNullOrEmpty(input) || input == "-1")
            {
                return result;
            }

            string[] mainParts = input.Split('|');

            foreach (string item in mainParts)
            {
                string[] subParts = item.Split('_');
                if (subParts.Length < 3) continue;

                int preId = int.Parse(subParts[0]);

                // 每两个值是一组坐标 (x, y)
                for (int i = 1; i < subParts.Length - 1; i += 2)
                {
                    float x = float.Parse(subParts[i], CultureInfo.InvariantCulture);
                    float y = float.Parse(subParts[i + 1], CultureInfo.InvariantCulture);
                    result.Add(new Vector3(preId, x, y));
                }
            }

            return result;
        }

        /// <summary>
        /// 旧方法保持兼容（但内部转换为float处理）
        /// 返回 Vector3Int: x=preId, y=worldX取整, z=worldY取整
        /// </summary>
        [Obsolete("请使用 ParsePathNodeList 获取精确的世界坐标")]
        public static List<Vector3Int> ParesV3IList(this string input)
        {
            var floatList = ParsePathNodeList(input);
            return floatList.Select(v => new Vector3Int(
                (int)v.x,
                Mathf.RoundToInt(v.y),
                Mathf.RoundToInt(v.z)
            )).ToList();
        }

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
        /// 替换路径字符串中某个科技的id
        /// </summary>
        public static string ReplacePathNode(this string input, string oldId, string newId)
        {
            if (string.IsNullOrEmpty(input) || input == "-1") return input;

            var paths = input.Split('|').ToList();
            for (int i = 0; i < paths.Count; i++)
            {
                var segments = paths[i].Split('_').ToList();
                if (segments[0] == oldId)
                {
                    segments[0] = newId;
                    paths[i] = string.Join("_", segments);
                }
            }
            return string.Join("|", paths);
        }

        /// <summary>
        /// 从路径列表中剔除目标id的整条数据
        /// </summary>
        public static string RemoveIdPrePath(this string input, string target)
        {
            if (string.IsNullOrEmpty(input) || input == "-1") return input;

            var paths = input.Split('|')
                .Where(path => path.Split('_')[0] != target)
                .ToList();

            return paths.Count == 0 ? "-1" : string.Join("|", paths);
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
        /// 更新前置路径中某个id的路径字段（使用世界坐标）
        /// </summary>
        public static string UpdatePathNodeById(this string input, string id, string newPath)
        {
            if (string.IsNullOrEmpty(input) || input == "-1")
            {
                return string.IsNullOrEmpty(newPath) ? "-1" : $"{id}_{newPath}";
            }

            var paths = input.Split('|').ToList();
            bool found = false;

            for (int i = 0; i < paths.Count; i++)
            {
                if (paths[i].Split('_')[0] == id)
                {
                    if (string.IsNullOrEmpty(newPath))
                    {
                        paths.RemoveAt(i);
                    }
                    else
                    {
                        paths[i] = $"{id}_{newPath}";
                    }
                    found = true;
                    break;
                }
            }

            if (!found && !string.IsNullOrEmpty(newPath))
            {
                paths.Add($"{id}_{newPath}");
            }

            return paths.Count == 0 ? "-1" : string.Join("|", paths);
        }

        /// <summary>
        /// 在指定的前置节点路径中插入一个新的坐标点（使用世界坐标）
        /// </summary>
        public static string InsertPathNode(this string pathNodeStr, string targetPreId, int insertIndex, Vector2 newPoint)
        {
            string pointStr = $"{newPoint.x.ToString(FLOAT_FORMAT)}_{newPoint.y.ToString(FLOAT_FORMAT)}";

            if (string.IsNullOrEmpty(pathNodeStr) || pathNodeStr == "-1")
            {
                return $"{targetPreId}_{pointStr}";
            }

            var paths = pathNodeStr.Split('|').ToList();
            bool found = false;

            for (int i = 0; i < paths.Count; i++)
            {
                var segments = paths[i].Split('_').ToList();
                string preId = segments[0];

                if (preId == targetPreId)
                {
                    // 格式: ID_X1_Y1_X2_Y2...
                    // insertIndex 是 LineRenderer 的索引，从 1 开始
                    int listIndex = 1 + (insertIndex - 1) * 2;
                    if (listIndex > segments.Count) listIndex = segments.Count;
                    if (listIndex < 1) listIndex = 1;

                    segments.Insert(listIndex, newPoint.y.ToString(FLOAT_FORMAT));
                    segments.Insert(listIndex, newPoint.x.ToString(FLOAT_FORMAT));

                    paths[i] = string.Join("_", segments);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                paths.Add($"{targetPreId}_{pointStr}");
            }

            return string.Join("|", paths);
        }

        /// <summary>
        /// 删除指定前置节点路径中的某个坐标点
        /// </summary>
        public static string RemovePathNode(this string pathNodeStr, string targetPreId, int anchorIndex)
        {
            if (string.IsNullOrEmpty(pathNodeStr) || pathNodeStr == "-1") return pathNodeStr;

            var paths = pathNodeStr.Split('|').ToList();
            List<string> newPaths = new List<string>();

            foreach (var path in paths)
            {
                var segments = path.Split('_').ToList();
                string preId = segments[0];

                if (preId == targetPreId)
                {
                    int removeStart = 1 + (anchorIndex - 1) * 2;

                    if (removeStart + 1 < segments.Count)
                    {
                        segments.RemoveRange(removeStart, 2);
                    }

                    if (segments.Count > 1)
                    {
                        newPaths.Add(string.Join("_", segments));
                    }
                }
                else
                {
                    newPaths.Add(path);
                }
            }

            return newPaths.Count == 0 ? "-1" : string.Join("|", newPaths);
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
    }
}
