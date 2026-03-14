// <copyright file="UserServiceTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Application.Tests.Infrastructure;

public class UserServiceTests
{
  private readonly Mock<IUserRepository> userRepositoryMock;
  private readonly Mock<IPasswordHasher> passwordHasherMock;
  private readonly Mock<ITokenService> tokenServiceMock;
  private readonly IConfiguration configuration;
  private readonly UserService sut;

  public UserServiceTests()
  {
    this.userRepositoryMock = new Mock<IUserRepository>();
    this.passwordHasherMock = new Mock<IPasswordHasher>();
    this.tokenServiceMock = new Mock<ITokenService>();

    this.configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          { "Security:MaxLoginAttempts", "5" },
          { "Security:LockoutDurationMinutes", "15" },
          { "Security:RefreshTokenExpirationDays", "7" },
        })
        .Build();

    this.sut = new UserService(
        this.userRepositoryMock.Object,
        this.passwordHasherMock.Object,
        this.tokenServiceMock.Object,
        this.configuration);
  }

  [Fact]
  public async Task AuthenticateAsync_ShouldReturnNull_WhenUserNotFound()
  {
    var request = new AuthenticationRequest("unknown", "password");
    this.userRepositoryMock
        .Setup(r => r.GetWithRefreshTokensAsync("unknown", default))
        .ReturnsAsync((User?)null);

    var result = await this.sut.AuthenticateAsync(request);

    result.Should().BeNull();
  }

  [Fact]
  public async Task AuthenticateAsync_ShouldReturnNull_WhenAccountIsLocked()
  {
    var user = CreateUser();
    user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
    this.userRepositoryMock
        .Setup(r => r.GetWithRefreshTokensAsync("testuser", default))
        .ReturnsAsync(user);

    var request = new AuthenticationRequest("testuser", "password");
    var result = await this.sut.AuthenticateAsync(request);

    result.Should().BeNull();
  }

  [Fact]
  public async Task AuthenticateAsync_ShouldReturnNull_WhenPasswordIsInvalid()
  {
    var user = CreateUser();
    this.userRepositoryMock
        .Setup(r => r.GetWithRefreshTokensAsync("testuser", default))
        .ReturnsAsync(user);
    this.passwordHasherMock
        .Setup(p => p.VerifyPassword(user.PasswordHash, "wrongpass"))
        .Returns(false);

    var request = new AuthenticationRequest("testuser", "wrongpass");
    var result = await this.sut.AuthenticateAsync(request);

    result.Should().BeNull();
    this.userRepositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }

  [Fact]
  public async Task AuthenticateAsync_ShouldLockAccount_AfterMaxFailedAttempts()
  {
    var user = CreateUser();
    user.AccessFailedCount = 4;
    this.userRepositoryMock
        .Setup(r => r.GetWithRefreshTokensAsync("testuser", default))
        .ReturnsAsync(user);
    this.passwordHasherMock
        .Setup(p => p.VerifyPassword(user.PasswordHash, "wrongpass"))
        .Returns(false);

    var request = new AuthenticationRequest("testuser", "wrongpass");
    await this.sut.AuthenticateAsync(request);

    user.AccessFailedCount.Should().Be(5);
    user.LockoutEnd.Should().NotBeNull();
    user.LockoutEnd!.Value.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
  }

  [Fact]
  public async Task AuthenticateAsync_ShouldReturnTokens_WhenCredentialsAreValid()
  {
    var user = CreateUser();
    this.userRepositoryMock
        .Setup(r => r.GetWithRefreshTokensAsync("testuser", default))
        .ReturnsAsync(user);
    this.passwordHasherMock
        .Setup(p => p.VerifyPassword(user.PasswordHash, "Password123!"))
        .Returns(true);
    this.tokenServiceMock
        .Setup(t => t.CreateToken(user.Id.ToString(), user.Username, It.IsAny<IEnumerable<string>>()))
        .Returns("access-token");

    var request = new AuthenticationRequest("testuser", "Password123!");
    var result = await this.sut.AuthenticateAsync(request);

    result.Should().NotBeNull();
    result!.accessToken.Should().Be("access-token");
    result.refreshToken.Should().NotBeNullOrEmpty();
    result.tokenType.Should().Be("Bearer");
    result.expiresIn.Should().Be(3600);
  }

  [Fact]
  public async Task AuthenticateAsync_ShouldResetFailedAttempts_OnSuccessfulLogin()
  {
    var user = CreateUser();
    user.AccessFailedCount = 3;
    this.userRepositoryMock
        .Setup(r => r.GetWithRefreshTokensAsync("testuser", default))
        .ReturnsAsync(user);
    this.passwordHasherMock
        .Setup(p => p.VerifyPassword(user.PasswordHash, "Password123!"))
        .Returns(true);
    this.tokenServiceMock
        .Setup(t => t.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
        .Returns("access-token");

    var request = new AuthenticationRequest("testuser", "Password123!");
    await this.sut.AuthenticateAsync(request);

    user.AccessFailedCount.Should().Be(0);
    user.LockoutEnd.Should().BeNull();
  }

  [Fact]
  public async Task RegisterAsync_ShouldCreateUser_WhenDataIsValid()
  {
    var dto = new CreateUserDto("newuser", "new@example.com", "Password123!", "New", "User");
    this.userRepositoryMock
        .Setup(r => r.ExistsByUsernameOrEmailAsync("newuser", "new@example.com", default))
        .ReturnsAsync(false);
    this.passwordHasherMock
        .Setup(p => p.HashPassword("Password123!"))
        .Returns("hashed-password");

    var result = await this.sut.RegisterAsync(dto);

    result.Should().NotBeNull();
    result.Username.Should().Be("newuser");
    result.Email.Should().Be("new@example.com");
    result.PasswordHash.Should().Be("hashed-password");
    this.userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Once);
    this.userRepositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }

  [Fact]
  public async Task RegisterAsync_ShouldThrow_WhenUserAlreadyExists()
  {
    var dto = new CreateUserDto("existing", "existing@example.com", "Password123!", "Existing", "User");
    this.userRepositoryMock
        .Setup(r => r.ExistsByUsernameOrEmailAsync("existing", "existing@example.com", default))
        .ReturnsAsync(true);

    var act = async () => await this.sut.RegisterAsync(dto);

    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("Username or email already exists");
  }

  [Fact]
  public async Task RefreshTokenAsync_ShouldReturnNull_WhenTokenNotFound()
  {
    this.userRepositoryMock
        .Setup(r => r.GetByRefreshTokenAsync("bad-token", default))
        .ReturnsAsync((User?)null);

    var request = new RefreshTokenRequest("bad-token");
    var result = await this.sut.RefreshTokenAsync(request);

    result.Should().BeNull();
  }

  [Fact]
  public async Task RefreshTokenAsync_ShouldReturnNull_WhenTokenIsInactive()
  {
    var user = CreateUser();
    var refreshToken = new RefreshToken
    {
      Token = "expired-token",
      ExpiresAt = DateTime.UtcNow.AddDays(-1),
      UserId = user.Id,
    };
    user.RefreshTokens.Add(refreshToken);
    this.userRepositoryMock
        .Setup(r => r.GetByRefreshTokenAsync("expired-token", default))
        .ReturnsAsync(user);

    var request = new RefreshTokenRequest("expired-token");
    var result = await this.sut.RefreshTokenAsync(request);

    result.Should().BeNull();
  }

  [Fact]
  public async Task RefreshTokenAsync_ShouldRotateTokens_WhenValid()
  {
    var user = CreateUser();
    var refreshToken = new RefreshToken
    {
      Token = "valid-token",
      ExpiresAt = DateTime.UtcNow.AddDays(7),
      UserId = user.Id,
    };
    user.RefreshTokens.Add(refreshToken);
    this.userRepositoryMock
        .Setup(r => r.GetByRefreshTokenAsync("valid-token", default))
        .ReturnsAsync(user);
    this.tokenServiceMock
        .Setup(t => t.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
        .Returns("new-access-token");

    var request = new RefreshTokenRequest("valid-token");
    var result = await this.sut.RefreshTokenAsync(request);

    result.Should().NotBeNull();
    result!.accessToken.Should().Be("new-access-token");
    refreshToken.RevokedAt.Should().NotBeNull();
    refreshToken.ReplacedByToken.Should().NotBeNullOrEmpty();
    this.userRepositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }

  [Fact]
  public async Task RevokeTokenAsync_ShouldReturnFalse_WhenTokenNotFound()
  {
    this.userRepositoryMock
        .Setup(r => r.GetByRefreshTokenAsync("nonexistent", default))
        .ReturnsAsync((User?)null);

    var result = await this.sut.RevokeTokenAsync("nonexistent");

    result.Should().BeFalse();
  }

  [Fact]
  public async Task RevokeTokenAsync_ShouldRevokeActiveToken()
  {
    var user = CreateUser();
    var refreshToken = new RefreshToken
    {
      Token = "active-token",
      ExpiresAt = DateTime.UtcNow.AddDays(7),
      UserId = user.Id,
    };
    user.RefreshTokens.Add(refreshToken);
    this.userRepositoryMock
        .Setup(r => r.GetByRefreshTokenAsync("active-token", default))
        .ReturnsAsync(user);

    var result = await this.sut.RevokeTokenAsync("active-token", "127.0.0.1");

    result.Should().BeTrue();
    refreshToken.RevokedAt.Should().NotBeNull();
    refreshToken.RevokedByIp.Should().Be("127.0.0.1");
    this.userRepositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }

  [Fact]
  public async Task UnlockAccountAsync_ShouldReturnFalse_WhenUserNotFound()
  {
    var userId = Guid.NewGuid();
    this.userRepositoryMock
        .Setup(r => r.GetByIdAsync(userId, default))
        .ReturnsAsync((User?)null);

    var result = await this.sut.UnlockAccountAsync(userId);

    result.Should().BeFalse();
  }

  [Fact]
  public async Task UnlockAccountAsync_ShouldResetLockout()
  {
    var user = CreateUser();
    user.AccessFailedCount = 5;
    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
    this.userRepositoryMock
        .Setup(r => r.GetByIdAsync(user.Id, default))
        .ReturnsAsync(user);

    var result = await this.sut.UnlockAccountAsync(user.Id);

    result.Should().BeTrue();
    user.AccessFailedCount.Should().Be(0);
    user.LockoutEnd.Should().BeNull();
    this.userRepositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }

  private static User CreateUser()
  {
    return new User
    {
      Id = Guid.NewGuid(),
      Username = "testuser",
      Email = "test@example.com",
      PasswordHash = "hashed-password",
      FirstName = "Test",
      LastName = "User",
      IsActive = true,
    };
  }
}
