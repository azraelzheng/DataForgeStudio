namespace DataForgeStudio.Shared.DTO;

/// <summary>
/// 字段结构信息 DTO
/// </summary>
public class FieldSchemaDto
{
    /// <summary>
    /// 字段名
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// SQL 数据类型（如 datetime, nvarchar, int）
    /// </summary>
    public string SqlDataType { get; set; } = string.Empty;

    /// <summary>
    /// 系统数据类型（String, Number, DateTime, Boolean）
    /// </summary>
    public string SystemDataType { get; set; } = string.Empty;
}
