using Microsoft.AspNetCore.Builder;
using NetEscapades.AspNetCore.SecurityHeaders.Infrastructure;

namespace DataForgeStudio.Api;

/// <summary>
/// 安全响应头配置
/// </summary>
public static class SecurityHeadersConfig
{
    /// <summary>
    /// 获取安全响应头策略集合
    /// </summary>
    /// <param name="isDevelopment">是否为开发环境</param>
    /// <returns>安全响应头策略集合</returns>
    public static HeaderPolicyCollection GetHeaderPolicyCollection(bool isDevelopment)
    {
        var policy = new HeaderPolicyCollection()
            // 防止点击劫持攻击
            .AddFrameOptionsDeny()
            // 防止 MIME 类型嗅探
            .AddContentTypeOptionsNoSniff()
            // HTTP 严格传输安全（HSTS）- 1 年有效期，包含子域名
            .AddStrictTransportSecurityMaxAgeIncludeSubDomains(maxAgeInSeconds: 60 * 60 * 24 * 365) // 1 year
            // 引用策略 - 仅在跨域时发送来源
            .AddReferrerPolicyStrictOriginWhenCrossOrigin()
            // 内容安全策略（CSP）
            .AddContentSecurityPolicy(builder =>
            {
                if (isDevelopment)
                {
                    // 开发环境：允许内联脚本和样式，方便开发调试
                    builder.AddDefaultSrc().Self().UnsafeInline().UnsafeEval();
                    builder.AddScriptSrc().Self().UnsafeInline().UnsafeEval();
                    builder.AddStyleSrc().Self().UnsafeInline();
                }
                else
                {
                    // 生产环境：严格的安全策略
                    builder.AddDefaultSrc().Self();
                    builder.AddScriptSrc().Self();
                    builder.AddStyleSrc().Self().UnsafeInline(); // 允许内联样式（某些 UI 组件需要）
                    builder.AddImgSrc().Self().Data().Blob();
                    builder.AddConnectSrc().Self();
                }
            })
            // 移除服务器头信息，防止泄露服务器版本
            .RemoveServerHeader()
            // XSS 保护（虽然现代浏览器已不再需要，但保留以兼容旧浏览器）
            .AddXssProtectionBlock()
            // 自定义响应头
            .AddCustomHeader("X-Powered-By", "DataForgeStudio V4");

        return policy;
    }
}
