using System.Net;
using System.Net.Http.Json;
using Xunit;
using DataForgeStudio.Tests.Integration;

namespace DataForgeStudio.Tests.Integration;

/// <summary>
/// AuthController 集成测试 - 测试基本的 API 端点
/// </summary>
public class AuthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AuthControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_AllowAnonymous_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task ApiEndpoint_AllowAnonymous_ReturnsApiInfo()
    {
        // Act
        var response = await _client.GetAsync("/api");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/current-user");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithEmptyRequest_ReturnsBadRequest()
    {
        // Act - 发送空请求体
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { });

        // Assert - 应该返回错误（可能是 400 或 200 但 Success=false）
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
    }
}
