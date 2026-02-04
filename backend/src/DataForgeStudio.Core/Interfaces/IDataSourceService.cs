using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Interfaces;

/// <summary>
/// 数据源服务接口
/// </summary>
public interface IDataSourceService
{
    /// <summary>
    /// 获取数据源分页列表
    /// </summary>
    Task<ApiResponse<PagedResponse<DataSourceDto>>> GetDataSourcesAsync(PagedRequest request, string? dataSourceName = null, string? dbType = null, bool includeInactive = true);

    /// <summary>
    /// 获取数据源详情
    /// </summary>
    Task<ApiResponse<DataSourceDto>> GetDataSourceByIdAsync(int dataSourceId);

    /// <summary>
    /// 创建数据源
    /// </summary>
    Task<ApiResponse<DataSourceDto>> CreateDataSourceAsync(CreateDataSourceRequest request, int createdBy);

    /// <summary>
    /// 更新数据源
    /// </summary>
    Task<ApiResponse> UpdateDataSourceAsync(int dataSourceId, CreateDataSourceRequest request);

    /// <summary>
    /// 删除数据源
    /// </summary>
    Task<ApiResponse> DeleteDataSourceAsync(int dataSourceId);

    /// <summary>
    /// 测试连接
    /// </summary>
    Task<ApiResponse> TestConnectionAsync(int dataSourceId);

    /// <summary>
    /// 测试连接（创建前）
    /// </summary>
    Task<ApiResponse> TestConnectionAsync(CreateDataSourceRequest request);

    /// <summary>
    /// 获取数据库列表
    /// </summary>
    Task<ApiResponse<List<string>>> GetDatabasesAsync(CreateDataSourceRequest request);

    /// <summary>
    /// 停用/启用数据源
    /// </summary>
    Task<ApiResponse> ToggleActiveAsync(int dataSourceId);
}
