using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// SQL 表名验证器 - 防止通过表名进行 SQL 注入攻击
/// </summary>
public static class SqlTableNameValidator
{
    /// <summary>
    /// 表名格式正则表达式：允许字母、数字、下划线，支持 schema.table 格式
    /// </summary>
    private static readonly Regex TableNameRegex = new(
        @"^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)?$",
        RegexOptions.Compiled | RegexOptions.Singleline
    );

    /// <summary>
    /// 允许访问的系统表（用于元数据查询）
    /// </summary>
    private static readonly HashSet<string> AllowedSystemTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "information_schema.tables",
        "information_schema.columns",
        "information_schema.schemata",
        "sys.tables",
        "sys.columns",
        "sys.views",
        "sys.databases",
        "pg_database",
        "pg_attribute",
        "pg_constraint",
        "all_tab_columns",
        "all_cons_columns",
        "all_constraints",
        "all_users",
        "pragma_table_info"
    };

    /// <summary>
    /// 危险 SQL 关键词列表
    /// </summary>
    private static readonly string[] DangerousKeywords = new[]
    {
        "drop", "delete", "truncate", "insert", "update", "alter",
        "create", "exec", "execute", "--", ";", "/*", "*/", "xp_",
        "sp_", "declare", "cursor", "union", "script", "javascript"
    };

    /// <summary>
    /// 验证表名是否安全
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <returns>(是否有效, 错误信息)</returns>
    public static (bool IsValid, string? Error) ValidateTableName(string tableName)
    {
        // 检查是否为空
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return (false, "表名不能为空");
        }

        // 去除前后空格
        tableName = tableName.Trim();

        // 检查长度限制（SQL Server 标识符最大长度为 128）
        if (tableName.Length > 128)
        {
            return (false, "表名过长（最大 128 字符）");
        }

        // 检查格式
        if (!TableNameRegex.IsMatch(tableName))
        {
            return (false, $"表名格式无效: {tableName}。只允许字母、数字、下划线，且必须以字母或下划线开头");
        }

        // 检查危险关键词
        var lowerName = tableName.ToLowerInvariant();
        foreach (var keyword in DangerousKeywords)
        {
            if (lowerName.Contains(keyword))
            {
                return (false, $"表名包含危险关键词: {keyword}");
            }
        }

        // 检查是否包含特殊字符组合
        if (tableName.Contains("'") || tableName.Contains("\"") || tableName.Contains("[") || tableName.Contains("]"))
        {
            return (false, "表名不能包含引号或方括号");
        }

        // 检查是否包含空格
        if (tableName.Contains(" "))
        {
            return (false, "表名不能包含空格");
        }

        return (true, null);
    }

    /// <summary>
    /// 验证表名并在无效时抛出异常
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="logger">日志记录器</param>
    /// <exception cref="ArgumentException">表名无效时抛出</exception>
    public static void ValidateAndThrow(string tableName, ILogger? logger = null)
    {
        var (isValid, error) = ValidateTableName(tableName);
        if (!isValid)
        {
            logger?.LogWarning("检测到无效表名访问尝试: TableName={TableName}, Error={Error}", tableName, error);
            throw new ArgumentException($"无效的表名: {error}", nameof(tableName));
        }
    }

    /// <summary>
    /// 检查表名是否为允许的系统表
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <returns>是否为允许的系统表</returns>
    public static bool IsAllowedSystemTable(string tableName)
    {
        return !string.IsNullOrWhiteSpace(tableName) &&
               AllowedSystemTables.Contains(tableName.ToLowerInvariant());
    }

    /// <summary>
    /// 转义表名（用于动态 SQL 构建，但仍需配合参数化查询）
    /// </summary>
    /// <param name="tableName">原始表名</param>
    /// <returns>转义后的表名</returns>
    /// <exception cref="ArgumentException">表名无效时抛出</exception>
    public static string EscapeTableName(string tableName)
    {
        var (isValid, error) = ValidateTableName(tableName);
        if (!isValid)
        {
            throw new ArgumentException($"无法转义无效的表名: {error}", nameof(tableName));
        }

        // 对于包含 schema 的表名，分别转义
        if (tableName.Contains('.'))
        {
            var parts = tableName.Split('.');
            return $"[{parts[0]}].[{parts[1]}]";
        }

        return $"[{tableName}]";
    }
}
