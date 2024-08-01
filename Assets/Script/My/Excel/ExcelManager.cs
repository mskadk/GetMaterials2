using Assets.Script.My.Extention;
using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using static Assets.Script.My.Extention.MyExtensions;

namespace Assets.Script.My.Excel
{
    public class ExcelManager
    {
        const int SKIP_ROW = 2;
        FileInfo fileInfo;
        ExcelPackage excelPackage;
        ExcelWorksheet worksheet;

        Dictionary<int, Science> ScienceDict;
        private enum Scc : int//Sciencs Col
        {
            ID = 1,
            SubType = 2,
            ModuleId = 3,
            IconScale = 4,
            LineScale = 5,
            Name = 6,
            Detail = 7,
            Detail_2 = 8,
            Building_unlock = 9,
            NonBuilding_unlock = 10,
            HexGridX = 11,
            HexGridY = 12,
            Pre_technology = 13,
            PathNode = 14,
            S_Materials = 15,
            Time = 16,
            IconColor = 17,
            Trigger_technology = 18,

        }; //以名称方式确定表字段的位置
        public ExcelManager()
        {

        }
        /// <summary>
        /// 读取科技表，组成科技字典
        /// <para>自动为节点创建后继列表</para>
        /// </summary>
        /// <param name="FilePath">表名字</param>
        public Dictionary<int, Science> Load(string FilePath)
        {
            fileInfo = new FileInfo(FilePath);
            excelPackage = new ExcelPackage(fileInfo);
            worksheet = excelPackage.Workbook.Worksheets[1];
            int startRow = worksheet.Dimension.Start.Row;
            int endRow = worksheet.Dimension.Rows;
            //为科技表建立字典，索引为科技的ID
            ScienceDict = new Dictionary<int, Science>();
            Dictionary<int, List<int>> AfterDict = new();
            for (int i = startRow + SKIP_ROW; i <= endRow; i++)
            {
                Science science = new(
                            worksheet.Cells[i, (int)Scc.ID].GetValue<int>(),
                            worksheet.Cells[i, (int)Scc.SubType].GetValue<int>(),
                            worksheet.Cells[i, (int)Scc.ModuleId].GetValue<int>(),
                            worksheet.Cells[i, (int)Scc.IconScale].GetValue<float>(),
                            worksheet.Cells[i, (int)Scc.LineScale].GetValue<float>(),
                            worksheet.Cells[i, (int)Scc.Name].GetValue<string>(),
                            worksheet.Cells[i, (int)Scc.Detail].GetValue<string>(),
                            worksheet.Cells[i, (int)Scc.Detail_2].GetValue<string>(),
                            worksheet.Cells[i, (int)Scc.Building_unlock].GetValue<string>(),
                            worksheet.Cells[i, (int)Scc.NonBuilding_unlock].GetValue<string>(),
                            worksheet.Cells[i, (int)Scc.HexGridX].GetValue<int>(),
                            worksheet.Cells[i, (int)Scc.HexGridY].GetValue<int>(),
                            worksheet.Cells[i, (int)Scc.Pre_technology].GetValue<string>(),
                            worksheet.Cells[i, (int)Scc.PathNode].GetValue<string>(),
                            worksheet.Cells[i, (int)Scc.S_Materials].GetValue<string>(),
                            worksheet.Cells[i, (int)Scc.Time].GetValue<float>(),
                            worksheet.Cells[i, (int)Scc.IconColor].GetValue<int>(),
                            worksheet.Cells[i, (int)Scc.Trigger_technology].GetValue<string>()
                    );
                if (science.SubType == 1)
                {
                    ScienceDict.Add(science.Id, science);
                    if (!science.Pre_technology.Equals("-1"))
                    {
                        AfterDict.Add(science.Id, science.Pre_technology.ToList());

                    }
                }
            }

            foreach (var kvp in AfterDict)
            {
                foreach (var value in kvp.Value)
                {
                    ScienceDict.TryGetValue(value, out var sc);
                    sc?.After_technology.Add(kvp.Key);
                }
            }
            return ScienceDict;
        }
    }
}
