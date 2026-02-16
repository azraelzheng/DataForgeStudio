using System.Net;
using System.Net.Http.Json;
using Xunit;
using DataForgeStudio.Tests.Integration;

namespace DataForgeStudio.Tests.Integration;

/// <summary>
/// 集成测试 - 测试基本的 API 端点
/// 注意: 这些测试使用简化的 TestServer，不包含完整的业务控制器
/// AuthController 的测试由 AuthenticationServiceTests 单元测试覆盖
/// </summary>
public class AuthControllerTests : IDisposable
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public AuthControllerTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _factory?.Dispose();
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
        Assert.Contains("DataForgeStudio", content);
    }

    [Fact]
    public async Task HealthCheck_ReturnsCorrectJsonStructure()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\"", content.ToLower());
        Assert.Contains("\"timestamp\"", content.ToLower());
    }

    [Fact]
    public async Task ApiEndpoint_ReturnsCorrectJsonStructure()
    {
        // Act
        var response = await _client.GetAsync("/api");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"name\"", content.ToLower());
        Assert.Contains("\"version\"", content.ToLower());
    }
}
