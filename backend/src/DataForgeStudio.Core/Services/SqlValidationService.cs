using Microsoft.Extensions.Logging;

namespace DataForgeStudio.Core.Services;

/// <summary>
/// SQL 验证服务接口
/// </summary>
public interface ISqlValidationService
{
    /// <summary>
    /// 验证 SQL 查询是否安全
    /// </summary>
    ValidationResult ValidateQuery(string sql);
}

/// <summary>
/// SQL 验证结果
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 是否通过验证
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// SQL 验证服务实现 - 防止 SQL 注入
/// </summary>
public class SqlValidationService : ISqlValidationService
{
    private readonly ILogger<SqlValidationService> _logger;

    /// <summary>
    /// 危险关键字列表（不区分大小写）
    /// </summary>
    private static readonly HashSet<string> DangerousKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "DROP", "DELETE", "INSERT", "UPDATE", "ALTER", "CREATE", "TRUNCATE",
        "EXEC", "EXECUTE", "EXECutesql", "sp_executesql",
        "xp_cmdshell", "sp_oacreate", "sp_configure",
        "GRANT", "REVOKE", "DENY",
        "BULK", "OPENROWSET", "OPENDATASOURCE",
        "UNION", "SELECT INTO", "INTO OUTFILE",
        "sp_", "xp_", "DECLARE", "CURSOR",
        "SHUTDOWN", "KILL"
    };

    /// <summary>
    /// 仅允许出现在 SELECT 语句中的关键字
    /// </summary>
    private static readonly HashSet<string> AllowedKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "FROM", "WHERE", "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER",
        "ON", "AND", "OR", "NOT", "IN", "LIKE", "BETWEEN", "IS", "NULL",
        "ORDER", "BY", "GROUP", "HAVING", "DISTINCT", "TOP", "LIMIT", "OFFSET",
        "AS", "ASC", "DESC", "WITH", "NOLOCK",
        "CASE", "WHEN", "THEN", "ELSE", "END",
        "SUM", "COUNT", "AVG", "MIN", "MAX", "CAST", "CONVERT", "COALESCE",
        "DATEDIFF", "DATEADD", "GETDATE", "NOW", "CURRENT_TIMESTAMP"
    };

    public SqlValidationService(ILogger<SqlValidationService> logger)
    {
        _logger = logger;
    }

    public ValidationResult ValidateQuery(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "SQL 语句不能为空" };
        }

        var trimmedSql = sql.Trim();

        // 必须以 SELECT 开头
        if (!trimmedSql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("SQL 查询被阻止: 不是以 SELECT 开头, SQL: {Sql}", sql);
            return new ValidationResult { IsValid = false, ErrorMessage = "只允许 SELECT 查询" };
        }

        // 转换为大写进行检查
        var upperSql = trimmedSql.ToUpper();

        // 检查注释注入（-- 和 /* */）
        if (upperSql.Contains("--") || upperSql.Contains("/*"))
        {
            _logger.LogWarning("SQL 查询被阻止: 包含注释字符, SQL: {Sql}", sql);
            return new ValidationResult { IsValid = false, ErrorMessage = "SQL 包含注释字符" };
        }

        // 检查分号（防止多语句注入）
        if (upperSql.Contains(";"))
        {
            _logger.LogWarning("SQL 查询被阻止: 包含分号, SQL: {Sql}", sql);
            return new ValidationResult { IsValid = false, ErrorMessage = "SQL 包含分号，只允许单语句" };
        }

        // 检查危险关键字
        foreach (var keyword in DangerousKeywords)
        {
            // 使用单词边界检查，避免误判（例如 "ORDER" 不会被误判为包含 "DROP"）
            if (ContainsWord(upperSql, keyword))
            {
                _logger.LogWarning("SQL 查询被阻止: 包含危险关键字 {Keyword}, SQL: {Sql}", keyword, sql);
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"SQL 包含危险关键字: {keyword}"
                };
            }
        }

        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// 检查是否包含完整的单词（使用空格、逗号、括号等作为分隔符）
    /// </summary>
    private static bool ContainsWord(string text, string word)
    {
        var index = 0;
        while ((index = text.IndexOf(word, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            // 检查单词边界
            bool isValidStart = index == 0 || !char.IsLetterOrDigit(text[index - 1]) && text[index - 1] != '_';
            bool isValidEnd = index + word.Length >= text.Length ||
                              !char.IsLetterOrDigit(text[index + word.Length]) && text[index + word.Length] != '_';

            if (isValidStart && isValidEnd)
            {
                return true;
            }

            index += word.Length;
        }
        return false;
    }
}
