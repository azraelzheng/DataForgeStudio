namespace DataForgeStudio.Shared.Constants;

/// <summary>
/// 系统常量
/// </summary>
public static class SystemConstants
{
    /// <summary>
    /// Root 用户名
    /// </summary>
    public const string ROOT_USERNAME = "root";

    /// <summary>
    /// Root 用户默认密码
    /// </summary>
    public const string ROOT_DEFAULT_PASSWORD = "admin123";

    /// <summary>
    /// JWT Bearer 标识
    /// </summary>
    public const string JWT_BEARER_PREFIX = "Bearer ";

    /// <summary>
    /// 登录失败最大次数
    /// </summary>
    public const int MAX_LOGIN_FAILURE_COUNT = 5;

    /// <summary>
    /// 默认分页大小
    /// </summary>
    public const int DEFAULT_PAGE_SIZE = 20;

    /// <summary>
    /// 最大分页大小
    /// </summary>
    public const int MAX_PAGE_SIZE = 100;

    /// <summary>
    /// 密码最小长度
    /// </summary>
    public const int MIN_PASSWORD_LENGTH = 6;

    /// <summary>
    /// 会话超时时间（分钟）
    /// </summary>
    public const int SESSION_TIMEOUT_MINUTES = 30;

    /// <summary>
    /// 上传文件最大大小（MB）
    /// </summary>
    public const int MAX_UPLOAD_SIZE_MB = 10;

    /// <summary>
    /// 日期时间格式
    /// </summary>
    public const string DATETIME_FORMAT = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// 日期格式
    /// </summary>
    public const string DATE_FORMAT = "yyyy-MM-dd";

    /// <summary>
    /// 授权头名称
    /// </summary>
    public const string AUTHORIZATION_HEADER = "Authorization";

    /// <summary>
    /// 用户ID声明
    /// </summary>
    public const string USER_ID_CLAIM = "UserId";

    /// <summary>
    /// 用户名声明
    /// </summary>
    public const string USERNAME_CLAIM = "Username";

    /// <summary>
    /// AES 加密密钥长度
    /// </summary>
    public const int AES_KEY_SIZE = 256;

    /// <summary>
    /// AES 加密 IV 长度
    /// </summary>
    public const int AES_IV_SIZE = 128;

    /// <summary>
    /// RSA 密钥大小
    /// </summary>
    public const int RSA_KEY_SIZE = 2048;
}
