using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System;
using System.Collections.Generic;
using System.Linq;
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


    }

}
