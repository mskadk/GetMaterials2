using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace Assets.Script.My.Extention
{
    public static class MyExtensions
    {
        /// <summary>
        /// 将输入的字符串以“|”分隔，组成整形数组
        /// <para>空字符串、-1字符串返回空整形列表</para>
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns></returns>
        public static List<int> ToList(this string input)
        {
            if (string.IsNullOrEmpty(input) || input.Equals("-1"))
            {
                return new();
            }
            return input.Split("|")
                         .Select(item =>
                         {
                             return int.Parse(item);
                         })
                         .ToList();
        }

        /// <summary>
        /// 将输入的字符串以“|”做大分隔，“_"做小分隔，组成三维整形数组。例如：
        /// <para>1_0_0_1_1|2_0_0_1_1 =></para>
        /// <para>[1,0,0],[1,1,1], </para>
        /// <para>[2,0,0],[2,1,1]</para>
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns></returns>
        public static List<Vector3Int> ParesV3IList(this string input)
        {
            List<Vector3Int> SplitedString = new();
            string[] MainParts = input.Split("|");

            foreach (string item in MainParts)
            {
                if (item == "-1")
                {
                    return new();
                }
                string[] SubPatrs = item.Split("_");
                int PartLength = (SubPatrs.Length - 1) / 2;
                for (int i = 1, n = PartLength; n > 0; i += 2, n--)
                {
                    SplitedString.Add(new Vector3Int(
                        int.Parse(SubPatrs[0]), int.Parse(SubPatrs[i]), int.Parse(SubPatrs[i + 1])
                        ));

                }
            }
            return SplitedString;
        }
        /// <summary>
        /// 替换 前置科技 字符串中，某个科技的id
        /// </summary>
        /// <param name="input">Pre_Techonology 字段</param>
        /// <param name="oldId">旧id</param>
        /// <param name="newId">新id</param>
        /// <returns></returns>
        public static string ReplacePreTech(this string input, string oldId, string newId, string args = "|")
        {
            string result = null;
            List<string> list = input.Split(args).ToList();
            var i = list.IndexOf(oldId);
            list[i] = newId;
            for (int j = 0; j < list.Count; j++)
            {
                result += list[j];
                if (j != list.Count - 1)
                {
                    result += args;
                }
            }
            return result;
        }

        /// <summary>
        /// 替换 路径 字符串中，某个科技的id
        /// </summary>
        /// <param name="input">PathNode 字段</param>
        /// <param name="oldId">旧id</param>
        /// <param name="newId">新id</param>
        /// <returns></returns>
        public static string PeplacePathNode(this string input, string oldId, string newId)
        {
            string result = null;
            if (input == "-1")
            {
                return input;
            }
            else
            {
                //10_1_2_3_4|11_1_2_3_4 => 10_1_2_3_4
                var s = input.Split("|").ToList();
                string res = null;
                for (int i = 0; i < s.Count; i++)
                {
                    var ss = s[i].Split("_");
                    if (ss[0] == oldId)
                    {
                        ss[0] = newId;
                    }
                    for (int j = 0; j < ss.Length; j++)
                    {
                        res += ss[j];
                        if (j != ss.Length - 1)
                        {
                            res += "_";
                        }
                    }
                    result += res;
                    res = null;
                    if (i != s.Count - 1)
                    {
                        result += "|";
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 从竖线、横线分隔的路径列表中剔除目标id的整条数据
        /// </summary>
        /// <param name="input"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static string RemoveIdPrePath(this string input, string target)
        {
            if (input == "-1") { return input; }
            List<string> result = null;
            List<string> paths = input.Split("|").ToList();
            foreach (var path in paths)
            {
                if (path.Split("_")[0] != target)
                {
                    result.Add(path);
                }
            }
            return ToString(result);
        }

        /// <summary>
        /// 从竖线分隔的id列表中剔除目标id
        /// </summary>
        /// <param name="input"></param>
        /// <param name="removeID"></param>
        /// <returns></returns>
        public static string RemoveIdPreNode(this string input, string target)
        {
            var l = input.Split("|").ToList();
            l.Remove(target);
            return ToString(l);
        }

        /// <summary>
        /// 将List<string>以符号组成字符串
        /// </summary>
        /// <param name="input">输入内容</param>
        /// <param name="args">分隔符号</param>
        /// <returns></returns>
        public static string ToString(this List<string> input, string args = "|")
        {
            if (input is null)
            {
                return "-1";
            }
            string result = null;
            for (int i = 0; i < input.Count; i++)
            {
                result += input[i].ToString();
                if (i != input.Count - 1)
                {
                    result += args;
                }
            }
            return result is null ? "-1" : result;
        }

        /// <summary>
        /// 更新前置路径中，某个id的路径字段
        /// </summary>
        /// <param name="input"></param>
        /// <param name="id"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string UpdatePathNodeById(this string input, string id, string newPath)
        {
            string result = null;
            var paths = input.Split("|").ToList();
            foreach (var path in paths)
            {
                if (result is not null)
                {
                    result += "|";
                }
                if (path.Split("_")[0] == id)
                {
                    result += $"{id}_{newPath}";
                }
                else
                {
                    result += path;
                }
            }
            return result;
        }


        /// <summary>
        /// 在指定的前置节点路径中插入一个新的坐标点
        /// </summary>
        public static string InsertPathNode(this string pathNodeStr, string targetPreId, int insertIndex, Vector2Int newPoint)
        {
            if (string.IsNullOrEmpty(pathNodeStr) || pathNodeStr == "-1")
                return $"{targetPreId}_{newPoint.y}_{newPoint.x}";

            var paths = pathNodeStr.Split('|').ToList();
            bool found = false;

            for (int i = 0; i < paths.Count; i++)
            {
                var segments = paths[i].Split('_').ToList();
                string preId = segments[0];

                if (preId == targetPreId)
                {
                    // 格式: ID_Y1_X1_Y2_X2...
                    // 索引: 0  1  2  3  4
                    // 坐标组索引 k 对应 segments 中的 1+2*k 和 2+2*k
                    // insertIndex 是 LineRenderer 的索引，从 1 开始（0是起点）
                    // 我们要插入在第 insertIndex 个锚点之前
                    // 对应的 segments 插入位置是 1 + (insertIndex - 1) * 2 = 2 * insertIndex - 1

                    int listIndex = 2 * insertIndex - 1;
                    // 保护一下，防止越界追加
                    if (listIndex > segments.Count) listIndex = segments.Count;
                    if (listIndex < 1) listIndex = 1;

                    segments.Insert(listIndex, newPoint.x.ToString()); // X (注意顺序，你的格式可能是 Y_X)
                    segments.Insert(listIndex, newPoint.y.ToString()); // Y

                    paths[i] = string.Join("_", segments);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                paths.Add($"{targetPreId}_{newPoint.y}_{newPoint.x}");
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
                    // anchorIndex 是第几个锚点（从1开始）
                    // 对应的 segments 索引是 1 + (anchorIndex-1)*2
                    int removeStart = 1 + (anchorIndex - 1) * 2;

                    if (removeStart + 1 < segments.Count)
                    {
                        segments.RemoveRange(removeStart, 2); // 删除 Y 和 X
                    }

                    // 如果只剩 ID 了，这就变成空路径了，是否要保留取决于你的逻辑
                    // 这里假设保留 ID 代表直连
                    if (segments.Count > 1)
                    {
                        newPaths.Add(string.Join("_", segments));
                    }
                    // 如果删光了锚点，但还要保留连接关系，可能需要处理。
                    // 你的逻辑似乎是：如果 PathNode 里没有这个 ID，就是直连。
                    // 所以如果 segments.Count == 1，就不加到 newPaths 里
                }
                else
                {
                    newPaths.Add(path);
                }
            }

            if (newPaths.Count == 0) return "-1";
            return string.Join("|", newPaths);
        }

    }



}
