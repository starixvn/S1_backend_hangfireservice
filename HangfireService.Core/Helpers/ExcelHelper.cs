using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace HangfireService.Core.Helpers
{

    public class ExcelHelper
    {
        
        public class ExportDataToExcel
        {
            public string SheetName { get; set; }
            public IEnumerable<Dictionary<string, dynamic>> Content { get; set; }
            public List<string> Titles { get; set; }

        }
        
        public byte[] Export<T>(IEnumerable<T> t, string password) where T : notnull, new()
        {
            ExcelPackage.LicenseContext = LicenseContext.Commercial;
            using var ms = new MemoryStream();
            using var p = new ExcelPackage(ms);
            p.Workbook.Protection.SetPassword(password);
            var ws = p.Workbook.Worksheets.Add(nameof(t));

            if (t.Any())
            {
                var propertyInfo = t.FirstOrDefault().GetType().GetProperties();
                ExportBody(ws, t, propertyInfo, password);
                ExportHeader(ws, propertyInfo);
            }

            p.Encryption.Version = EncryptionVersion.Standard;
            p.Save(password);
            return ms.ToArray();
        }

        private void ExportBody<T>(
            ExcelWorksheet ws,
            IEnumerable<T> data,
            IEnumerable<PropertyInfo> propertyInfo, string password)
        {
            var currentRow = 2;
            for (var i = 0; i < data.Count(); i++)
            {
                var bt = data.ElementAt(i);

                for (var j = 0; j < propertyInfo.Count(); j++)
                {
                    var prop = propertyInfo.ElementAt(j);
                    var propValue = prop.GetValue(bt, null);

                    if (propValue != null && propValue.GetType().Name == "DateTime" && ((DateTime)propValue).Year > 1)
                    {
                        var time = new DateTimeOffset((DateTime)propValue);
                        ws.Cells[currentRow, j + 1].Value = time.ToOffset(TimeSpan.FromHours(8)).DateTime;
                        ws.Cells[currentRow, j + 1].Style.Numberformat.Format = "mm/dd/yyyy h:mm:ss\\ AM/PM";
                    }
                    else if (propValue != null && propValue.GetType().Name == "DateTimeOffset" && ((DateTimeOffset)propValue).Year > 1)
                    {
                        var time = (DateTimeOffset)propValue;
                        ws.Cells[currentRow, j + 1].Value = time.DateTime;
                        ws.Cells[currentRow, j + 1].Style.Numberformat.Format = "mm/dd/yyyy h:mm:ss\\ AM/PM";
                    }
                    else
                    {
                        ws.Cells[currentRow, j + 1].Value = propValue;
                    }
                }

                currentRow++;
            }
            ws.Protection.SetPassword(password);
        }
        
        private void ExportHeader(ExcelWorksheet ws, IEnumerable<PropertyInfo> propertyInfo)
        {
            SetHeader(ws, propertyInfo.Select(p => p.Name), 1);
        }

        public void SetHeader(ExcelWorksheet ws, IEnumerable<string> titles, int currentRow)
        {
            for (var i = 0; i < titles.Count(); i++)
            {
                var cell = ws.Cells[currentRow, i + 1];
                cell.Value = titles.ElementAt(i);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#B7DEE8"));
                ws.Column(i + 1).AutoFit();
            }
        }

        public FileContentResult ExcelFile (byte[] data, string fileName)
        {
            return new FileContentResult(data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = $"{fileName}_{DateTime.Now.ToString("yyyyMMdd")}.xlsx"
            };
        }
    }
}