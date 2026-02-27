using DataForgeStudio.Shared.Utils;
using Xunit;

namespace DataForgeStudio.Tests.Services;

/// <summary>
/// SQL Server 连接字符串构建测试
/// 验证命名实例（如 ALLWIN104\TPLUS）和默认实例的连接字符串格式
/// </summary>
public class ConnectionStringBuilderTests
{
    [Theory]
    [InlineData("localhost", 1433, "Server=localhost,1433;Database=TestDB;User Id=sa;Password=123;Connection Timeout=30;TrustServerCertificate=True;")]
    [InlineData("192.168.1.100", 1433, "Server=192.168.1.100,1433;Database=TestDB;User Id=sa;Password=123;Connection Timeout=30;TrustServerCertificate=True;")]
    [InlineData("ALLWIN104", 1433, "Server=ALLWIN104,1433;Database=TestDB;User Id=sa;Password=123;Connection Timeout=30;TrustServerCertificate=True;")]
    [InlineData("ALLWIN104\\TPLUS", 1433, "Server=ALLWIN104\\TPLUS;Database=TestDB;User Id=sa;Password=123;Connection Timeout=30;TrustServerCertificate=True;")]
    [InlineData("SERVER\\INSTANCENAME", 1433, "Server=SERVER\\INSTANCENAME;Database=TestDB;User Id=sa;Password=123;Connection Timeout=30;TrustServerCertificate=True;")]
    [InlineData("192.168.1.100\\SQLEXPRESS", 1433, "Server=192.168.1.100\\SQLEXPRESS;Database=TestDB;User Id=sa;Password=123;Connection Timeout=30;TrustServerCertificate=True;")]
    public void BuildSqlServerConnectionString_ShouldHandleNamedInstances(
        string server, int port, string expected)
    {
        // Act
        var result = ConnectionStringBuilder.BuildSqlServerConnectionString(
            server, port, "TestDB", "sa", "123", 30);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("localhost", false)]
    [InlineData("192.168.1.100", false)]
    [InlineData("ALLWIN104", false)]
    [InlineData("ALLWIN104\\TPLUS", true)]
    [InlineData("SERVER\\INSTANCENAME", true)]
    [InlineData("192.168.1.100\\SQLEXPRESS", true)]
    public void IsNamedInstance_ShouldDetectNamedInstances(string server, bool expected)
    {
        // Act
        var result = ConnectionStringBuilder.IsNamedInstance(server);

        // Assert
        Assert.Equal(expected, result);
    }
}
