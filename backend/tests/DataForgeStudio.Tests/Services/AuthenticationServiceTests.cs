using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using DataForgeStudio.Core.Services;
using DataForgeStudio.Domain.Entities;
using DataForgeStudio.Domain.Interfaces;
using DataForgeStudio.Shared.DTO;
using DataForgeStudio.Shared.Exceptions;

namespace DataForgeStudio.Tests.Services;

public class AuthenticationServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AuthenticationService _authService;

    public AuthenticationServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Setup default configuration
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns("DataForgeStudioV4JWTSecretKey256BitsLongSecure2025ChangeThisInProduction");

        _mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockSection.Object);
        _mockConfiguration.Setup(c => c["Security:Jwt:Secret"]).Returns("DataForgeStudioV4JWTSecretKey256BitsLongSecure2025ChangeThisInProduction");
        _mockConfiguration.Setup(c => c["Security:Jwt:Issuer"]).Returns("DataForgeStudio");
        _mockConfiguration.Setup(c => c["Security:Jwt:Audience"]).Returns("DataForgeStudio.Client");
        _mockConfiguration.Setup(c => c["Security:Jwt:ExpirationMinutes"]).Returns("60");

        _authService = new AuthenticationService(
            _mockUserRepo.Object,
            _mockConfiguration.Object
        );
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var validPassword = "Admin@123";
        var request = new LoginRequest { Username = "admin", Password = validPassword };
        var user = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(validPassword),
            IsActive = true,
            IsLocked = false,
            MustChangePassword = false,
            RealName = "系统管理员",
            Email = "admin@example.com",
            PasswordFailCount = 0
        };

        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(user);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mockUserRepo.Setup(r => r.GetUserWithPermissionsAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request, "127.0.0.1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data?.Token);
        Assert.Equal("登录成功", result.Message);
        Assert.True(result.Data.ExpiresIn > 0);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest { Username = "nonexistent", Password = "password" };
        _mockUserRepo.Setup(r => r.GetByUsernameAsync("nonexistent")).ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(request, "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("用户名或密码错误", result.Message);
    }

    [Fact]
    public async Task LoginAsync_UserLocked_ReturnsFailure()
    {
        // Arrange
        var validPassword = "Admin@123";
        var request = new LoginRequest { Username = "admin", Password = validPassword };
        var user = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(validPassword),
            IsActive = true,
            IsLocked = true
        };

        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request, "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("账户已被锁定，请联系管理员", result.Message);
    }

    [Fact]
    public async Task LoginAsync_UserInactive_ReturnsFailure()
    {
        // Arrange
        var validPassword = "Admin@123";
        var request = new LoginRequest { Username = "admin", Password = validPassword };
        var user = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(validPassword),
            IsActive = false,
            IsLocked = false
        };

        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request, "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("用户已被禁用", result.Message);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_IncreasesFailureCount()
    {
        // Arrange
        var validPassword = "Admin@123";
        var request = new LoginRequest { Username = "admin", Password = "wrongpassword" };
        var user = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(validPassword),
            IsActive = true,
            IsLocked = false,
            PasswordFailCount = 2
        };

        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(user);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        var result = await _authService.LoginAsync(request, "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("用户名或密码错误", result.Message);
        _mockUserRepo.Verify(r => r.UpdateAsync(It.Is<User>(u => u.PasswordFailCount == 3)), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_MaxFailureCount_LocksAccount()
    {
        // Arrange
        var validPassword = "Admin@123";
        var request = new LoginRequest { Username = "admin", Password = "wrongpassword" };
        var user = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(validPassword),
            IsActive = true,
            IsLocked = false,
            PasswordFailCount = 4
        };

        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(user);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        var result = await _authService.LoginAsync(request, "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        _mockUserRepo.Verify(r => r.UpdateAsync(It.Is<User>(u => u.IsLocked == true)), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_MustChangePassword_ReturnsSpecialResponse()
    {
        // Arrange
        var validPassword = "Admin@123";
        var request = new LoginRequest { Username = "admin", Password = validPassword };
        var user = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(validPassword),
            IsActive = true,
            IsLocked = false,
            MustChangePassword = true,
            PasswordFailCount = 0
        };

        _mockUserRepo.Setup(r => r.GetByUsernameAsync("admin")).ReturnsAsync(user);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        var result = await _authService.LoginAsync(request, "127.0.0.1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("首次登录必须修改密码", result.Message);
        Assert.True(result.Data.RequiresPasswordChange);
        Assert.Empty(result.Data.Token);
    }

    [Fact]
    public void GenerateJwtToken_ValidInput_ReturnsValidToken()
    {
        // Act
        var token = _authService.GenerateJwtToken(1, "admin");

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        // JWT tokens have 3 parts separated by dots
        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsTrue()
    {
        // Arrange
        var token = _authService.GenerateJwtToken(1, "admin");

        // Act
        var isValid = _authService.ValidateToken(token);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsFalse()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var isValid = _authService.ValidateToken(invalidToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ChangePassword_ValidPassword_ReturnsSuccess()
    {
        // Arrange
        var oldPassword = "Admin@123";
        var newPassword = "NewPass@456";
        var request = new ChangePasswordRequest
        {
            OldPassword = oldPassword,
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };
        var user = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword),
            MustChangePassword = true
        };

        _mockUserRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _mockUserRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        // Act
        var result = await _authService.ChangePasswordAsync(1, request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("密码修改成功", result.Message);
    }

    [Fact]
    public async Task ChangePassword_PasswordMismatch_ReturnsFailure()
    {
        // Arrange
        var request = new ChangePasswordRequest
        {
            OldPassword = "Admin@123",
            NewPassword = "NewPass@456",
            ConfirmPassword = "Different@789"
        };

        // Act
        var result = await _authService.ChangePasswordAsync(1, request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("两次输入的密码不一致", result.Message);
    }

    [Fact]
    public async Task ChangePassword_WrongOldPassword_ReturnsFailure()
    {
        // Arrange
        var validPassword = "Admin@123";
        var newPassword = "NewPass@456";
        var request = new ChangePasswordRequest
        {
            OldPassword = "wrongpassword",
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };
        var user = new User
        {
            UserId = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(validPassword)
        };

        _mockUserRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _authService.ChangePasswordAsync(1, request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("旧密码错误", result.Message);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
