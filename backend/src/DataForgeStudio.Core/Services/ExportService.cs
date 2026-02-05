using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using DataForgeStudio.Core.Interfaces;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// Excel 导出选项
/// </summary>
public class ExcelExportOptions
{
    public string SheetName { get; set; } = "Sheet1";
    public string? Title { get; set; }
    public bool AutoFilter { get; set; } = true;
    public bool FreezeHeaderRow { get; set; } = true;
}

/// <summary>
/// 导出服务实现 - 使用 ClosedXML 优化 Excel 导出
/// </summary>
public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
    }

    public Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, ExcelExportOptions options, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(options.SheetName);

            var properties = typeof(T).GetProperties();
            var rowCount = data.Count();

            _logger.LogInformation("开始导出 Excel: {Count} 行, {ColumnCount} 列", rowCount, properties.Length);

            // 写入表头
            var headerRow = 1;
            for (int i = 0; i < properties.Length; i++)
            {
                var cell = worksheet.Cell(headerRow, i + 1);
                cell.Value = properties[i].Name;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // 添加标题行（如果有）
            if (!string.IsNullOrEmpty(options.Title))
            {
                worksheet.Row(1).InsertRowsAbove(1);
                var titleCell = worksheet.Cell(1, 1);
                titleCell.Value = options.Title;
                titleCell.Style.Font.Bold = true;
                titleCell.Style.Font.FontSize = 14;
                worksheet.Range(1, 1, 1, properties.Length).Merge();

                headerRow = 2;
            }

            // 写入数据
            int dataRow = headerRow + 1;
            foreach (var item in data)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Excel 导出已取消");
                    break;
                }

                for (int col = 0; col < properties.Length; col++)
                {
                    var property = properties[col];
                    var value = property.GetValue(item);
                    var cell = worksheet.Cell(dataRow, col + 1);

                    if (value != null)
                    {
                        cell.Value = XLCellValue.FromObject(value);
                    }

                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                dataRow++;
            }

            // 自动调整列宽
            if (rowCount > 0)
            {
                worksheet.Columns().AdjustToContents();
                // 限制最大列宽（ClosedXL 中 Width 是设置而非获取）
                foreach (var column in worksheet.ColumnsUsed())
                {
                    if (column.Width > 50)
                    {
                        column.Width = 50;
                    }
                }
            }

            // 启用自动筛选
            if (options.AutoFilter)
            {
                worksheet.Range(headerRow, 1, headerRow, properties.Length).SetAutoFilter();
            }

            // 冻结标题行
            if (options.FreezeHeaderRow)
            {
                worksheet.SheetView.FreezeRows(headerRow);
            }

            _logger.LogInformation("Excel 导出完成: {Rows} 行", dataRow - headerRow);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }, cancellationToken);
    }
}
