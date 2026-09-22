using ClosedXML.Excel;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;

namespace My.XXX.Infrastructure
{
    public class ExcelHelper
    {
        public static MemoryStream DataTableToExcel(DataTable dt, string SheetName)
        {
            MemoryStream stream = new();
            try
            {
                if (dt != null && dt.Rows.Count > 0)
                {
                    using var workbook = new XLWorkbook();
                    var sheet = workbook.Worksheets.Add(SheetName);
                    for (var column = 0; column < dt.Columns.Count; column++)
                    {
                        sheet.Cell(1, column + 1).Value = dt.Columns[column].ColumnName;
                    }
                    for (var row = 0; row < dt.Rows.Count; row++)
                    {
                        for (var column = 0; column < dt.Columns.Count; column++)
                        {
                            // 保留文本值，包括前导零以及类似公式的字符串。
                            sheet.Cell(row + 2, column + 1).Value = dt.Rows[row][column].ToString();
                        }
                    }
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                }
                return stream;
            }
            catch
            {
                stream.Dispose();
                return null;
            }
        }

        public static MemoryStream ListToExport<T>(string SheetName, List<T> list, Dictionary<string, string> Columnname) where T : class
        {
            if (list == null || list.Count == 0)
            {
                return null;
            }
            MemoryStream stream = new();
            try
            {
                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add(string.IsNullOrEmpty(SheetName) ? "Sheet1" : SheetName);
                var properties = list[0].GetType().GetProperties();
                var columns = new List<(PropertyInfo Property, int Column)>();
                var column = 1;
                foreach (var mapping in Columnname)
                {
                    sheet.Cell(1, column).Value = mapping.Value;
                    foreach (var property in properties)
                    {
                        if (property.Name == mapping.Key)
                        {
                            columns.Add((property, column));
                            break;
                        }
                    }
                    column++;
                }
                for (var row = 0; row < list.Count; row++)
                {
                    foreach (var mapping in columns)
                    {
                        var value = mapping.Property.GetValue(list[row]);
                        if (value != null)
                        {
                            sheet.Cell(row + 2, mapping.Column).Value = value.ToString();
                        }
                    }
                }
                workbook.SaveAs(stream);
                stream.Position = 0;
                return stream;
            }
            catch
            {
                stream.Dispose();
                return null;
            }
        }
    }
}
