namespace DataForgeStudio.Shared.Exceptions;

/// <summary>
/// 应用程序异常基类
/// </summary>
public class AppException : Exception
{
    /// <summary>
    /// 错误代码
    /// </summary>
    public string ErrorCode { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public new string Message { get; set; }

    public AppException()
    {
    }

    public AppException(string message) : base(message)
    {
        Message = message;
    }

    public AppException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
        Message = message;
    }

    public AppException(string message, Exception innerException) : base(message, innerException)
    {
        Message = message;
    }

    public AppException(string errorCode, string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
        Message = message;
    }
}

/// <summary>
/// 验证异常
/// </summary>
public class ValidationException : AppException
{
    public ValidationException(string message) : base("VALIDATION_ERROR", message)
    {
    }

    public ValidationException(string errorCode, string message) : base(errorCode, message)
    {
    }
}

/// <summary>
/// 未授权异常
/// </summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "未授权访问") : base("UNAUTHORIZED", message)
    {
    }
}

/// <summary>
/// 未找到异常
/// </summary>
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base("NOT_FOUND", message)
    {
    }

    public NotFoundException(string resource, object id) : base("NOT_FOUND", $"{resource} (ID: {id}) 不存在")
    {
    }
}

/// <summary>
/// 业务异常
/// </summary>
public class BusinessException : AppException
{
    public BusinessException(string message) : base("BUSINESS_ERROR", message)
    {
    }

    public BusinessException(string errorCode, string message) : base(errorCode, message)
    {
    }
}

/// <summary>
/// 登录失败异常
/// </summary>
public class LoginFailedException : AppException
{
    public LoginFailedException(string message) : base("LOGIN_FAILED", message)
    {
    }
}
