using DataForgeStudio.Core.Services;

namespace DataForgeStudio.Core.Interfaces;

/// <summary>
/// 导出服务接口
/// </summary>
public interface IExportService
{
    /// <summary>
    /// 导出数据到 Excel
    /// </summary>
    Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, ExcelExportOptions options, System.Threading.CancellationToken cancellationToken = default);
}
