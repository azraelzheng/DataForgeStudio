namespace DataForgeStudio.Core.Validators;

/// <summary>
/// 密码验证器
/// </summary>
public static class PasswordValidator
{
    /// <summary>
    /// 密码验证结果
    /// </summary>
    public class PasswordValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误消息列表
        /// </summary>
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// 验证密码强度
    /// </summary>
    /// <param name="password">要验证的密码</param>
    /// <returns>验证结果</returns>
    public static PasswordValidationResult ValidatePassword(string? password)
    {
        var result = new PasswordValidationResult();

        if (string.IsNullOrWhiteSpace(password))
        {
            result.Errors.Add("密码不能为空");
            return result;
        }

        if (password.Length < 8)
        {
            result.Errors.Add("密码长度至少为 8 个字符");
        }

        if (password.Length > 128)
        {
            result.Errors.Add("密码长度不能超过 128 个字符");
        }

        if (!password.Any(char.IsLower))
        {
            result.Errors.Add("密码必须包含至少一个小写字母");
        }

        if (!password.Any(char.IsUpper))
        {
            result.Errors.Add("密码必须包含至少一个大写字母");
        }

        if (!password.Any(char.IsDigit))
        {
            result.Errors.Add("密码必须包含至少一个数字");
        }

        // 检查弱密码
        var weakPasswords = new[] { "password", "12345678", "abcdefgh", "qwerty123", "password123", "admin123" };
        if (weakPasswords.Any(weak => password.ToLowerInvariant().Contains(weak)))
        {
            result.Errors.Add("密码过于简单，请使用更复杂的密码");
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    /// <summary>
    /// 验证密码并在无效时抛出异常
    /// </summary>
    /// <param name="password">要验证的密码</param>
    /// <exception cref="ArgumentException">密码无效时抛出</exception>
    public static void ValidateAndThrow(string? password)
    {
        var result = ValidatePassword(password);
        if (!result.IsValid)
        {
            throw new ArgumentException(string.Join("; ", result.Errors));
        }
    }
}
