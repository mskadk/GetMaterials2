using Assets.Script.My.Extention;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Assets.Script.My.Excel
{
    public class ExcelManager
    {
        const int SKIP_ROW = 2;
        FileInfo fileInfo;
        ExcelPackage excelPackage;
        ExcelWorksheet worksheet;
        List<string> TypeRow = new();
        List<string> NameRow = new();

        Dictionary<int, Science> ScienceDict;
        Dictionary<int, TechTreeItem> TechTreeItemDict;

        private enum Scc : int
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
            HexGridX = 11,  // 字段名保持不变，但存储的是世界坐标
            HexGridY = 12,
            Pre_technology = 13,
            PathNode = 14,
            S_Materials = 15,
            Time = 16,
            IconColor = 17,
            Trigger_technology = 18,
        };

        public ExcelManager()
        {
        }

        public string SaveScience(string path, string source, Dictionary<int, Science> dict)
        {
            ScienceDict = dict;
            string saveName = "Science";
            string suffix = ".xlsx";
            FileInfo saveFI = new FileInfo(path + saveName + $"({DateTime.Now:yyyy-MM-dd_HH.mm.ss})" + suffix);
            ExcelPackage saveEP = new(saveFI);
            if (saveEP.Workbook.Worksheets.Count > 0)
            {
                saveEP.Workbook.Worksheets.Delete(1);
            }
            saveEP.Workbook.Worksheets.Add("Sheet1");
            ExcelWorksheet saveEW = saveEP.Workbook.Worksheets[1];

            // 保存表头
            for (int j = 1; j <= 18; j++)
            {
                saveEW.Cells[1, j].Value = TypeRow[j - 1];
                saveEW.Cells[2, j].Value = NameRow[j - 1];
            }

            // 保存字典内容
            int row = 3;
            foreach (var sc in ScienceDict.Values)
            {
                saveEW.Cells[row, (int)Scc.ID].Value = sc.Id;
                saveEW.Cells[row, (int)Scc.SubType].Value = sc.SubType;
                saveEW.Cells[row, (int)Scc.ModuleId].Value = sc.ModuleId;
                saveEW.Cells[row, (int)Scc.IconScale].Value = sc.IconScale;
                saveEW.Cells[row, (int)Scc.LineScale].Value = sc.LineScale;
                saveEW.Cells[row, (int)Scc.Name].Value = sc.Name;
                saveEW.Cells[row, (int)Scc.Detail].Value = sc.Detail;
                saveEW.Cells[row, (int)Scc.Detail_2].Value = sc.Detail_2;
                saveEW.Cells[row, (int)Scc.Building_unlock].Value = sc.Building_unlock;
                saveEW.Cells[row, (int)Scc.NonBuilding_unlock].Value = sc.NonBuilding_unlock;

                // 保存为三位小数的float
                saveEW.Cells[row, (int)Scc.HexGridX].Value = Math.Round(sc.HexGridX, 3);
                saveEW.Cells[row, (int)Scc.HexGridY].Value = Math.Round(sc.HexGridY, 3);

                saveEW.Cells[row, (int)Scc.Pre_technology].Value = sc.Pre_technology;
                saveEW.Cells[row, (int)Scc.PathNode].Value = sc.PathNode;
                saveEW.Cells[row, (int)Scc.S_Materials].Value = sc.S_Materials;
                saveEW.Cells[row, (int)Scc.Time].Value = sc.Time;
                saveEW.Cells[row, (int)Scc.IconColor].Value = sc.IconColor;
                saveEW.Cells[row, (int)Scc.Trigger_technology].Value = sc.Trigger_technology;
                row++;
            }

            // 设置保存样式
            saveEW.Cells.AutoFitColumns();
            saveEW.Cells.Style.Font.Name = "等线";
            saveEW.Cells["A2:R2"].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thick;
            saveEP.Save();

            return saveFI.FullName;
        }

        public Dictionary<int, Science> LoadScience(string FilePath)
        {
            fileInfo = new FileInfo(FilePath);
            excelPackage = new ExcelPackage(fileInfo);
            worksheet = excelPackage.Workbook.Worksheets[1];
            int startRow = worksheet.Dimension.Start.Row;
            int endRow = worksheet.Dimension.Rows;

            // 建立科技表的表头字段
            TypeRow.Clear();
            NameRow.Clear();
            for (int i = 1; i <= 18; i++)
            {
                TypeRow.Add(worksheet.Cells[1, i].GetValue<string>());
                NameRow.Add(worksheet.Cells[2, i].GetValue<string>());
            }

            // 为科技表建立字典
            ScienceDict = new Dictionary<int, Science>();
            Dictionary<int, List<int>> AfterDict = new();

            for (int i = startRow + SKIP_ROW; i <= endRow; i++)
            {
                // 读取坐标为float（兼容旧的int格式和新的float格式）
                float posX = worksheet.Cells[i, (int)Scc.HexGridX].GetValue<float>();
                float posY = worksheet.Cells[i, (int)Scc.HexGridY].GetValue<float>();

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
                    posX,  // 世界坐标X
                    posY,  // 世界坐标Y
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

        public Dictionary<int, TechTreeItem> LoadTechTreeitem(string FilePath)
        {
            fileInfo = new FileInfo(FilePath);
            excelPackage = new ExcelPackage(fileInfo);
            worksheet = excelPackage.Workbook.Worksheets[1];
            int startRow = worksheet.Dimension.Start.Row;
            int endRow = worksheet.Dimension.Rows;
            TechTreeItemDict = new Dictionary<int, TechTreeItem>();
            for (int i = startRow + SKIP_ROW; i <= endRow; i++)
            {
                TechTreeItem item = new(
                    worksheet.Cells[i, 1].GetValue<int>(),
                    worksheet.Cells[i, 3].GetValue<string>(),
                    worksheet.Cells[i, 4].GetValue<string>()
                );
                TechTreeItemDict.Add(item.Id, item);
            }
            return TechTreeItemDict;
        }
    }
}
