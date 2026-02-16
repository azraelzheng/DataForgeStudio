using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Shared.DTO;

namespace DataForgeStudio.Core.Interfaces;

/// <summary>
/// 数据库服务接口
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// 执行 SQL 查询并返回字典列表
    /// </summary>
    Task<ApiResponse<List<Dictionary<string, object>>>> ExecuteQueryAsync(DataSource dataSource, string sql, Dictionary<string, object>? parameters);

    /// <summary>
    /// 测试数据库连接
    /// </summary>
    Task<ApiResponse<bool>> TestConnectionAsync(DataSource dataSource);

    /// <summary>
    /// 测试数据库连接（使用明文密码，用于创建前测试）
    /// </summary>
    Task<ApiResponse<bool>> TestConnectionWithCredentialsAsync(DataSource dataSource, string plainPassword);

    /// <summary>
    /// 执行 SQL 查询并返回 DataTable
    /// </summary>
    Task<ApiResponse<System.Data.DataTable>> ExecuteQueryDataTableAsync(DataSource dataSource, string sql, Dictionary<string, object>? parameters);

    /// <summary>
    /// 获取数据库列表
    /// </summary>
    Task<ApiResponse<List<string>>> GetDatabasesAsync(string dbType, string connectionString);

    /// <summary>
    /// 获取表结构信息
    /// </summary>
    Task<ApiResponse<List<TableColumnDto>>> GetTableStructureAsync(DataSource dataSource, string tableName);

    /// <summary>
    /// 获取所有表及其列信息（用于SQL编辑器自动补全）
    /// </summary>
    Task<ApiResponse<List<TableInfoDto>>> GetAllTablesAsync(DataSource dataSource);

    /// <summary>
    /// 获取查询的字段结构信息（不返回数据行）
    /// </summary>
    /// <param name="dataSource">数据源</param>
    /// <param name="sql">SQL 查询语句</param>
    /// <param name="parameters">查询参数</param>
    /// <returns>字段结构列表</returns>
    Task<ApiResponse<List<FieldSchemaDto>>> GetQuerySchemaAsync(
        DataSource dataSource,
        string sql,
        Dictionary<string, object>? parameters);
}
