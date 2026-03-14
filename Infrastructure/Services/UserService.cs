// <copyright file="UserService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Infrastructure.Services;

/// <summary>
/// Service for user authentication and management.
/// </summary>
public class UserService : IUserService
{
  private readonly IUserRepository userRepository;
  private readonly IPasswordHasher passwordHasher;
  private readonly ITokenService tokenService;
  private readonly int maxLoginAttempts;
  private readonly int lockoutDurationMinutes;
  private readonly int refreshTokenExpirationDays;

  public UserService(
      IUserRepository userRepository,
      IPasswordHasher passwordHasher,
      ITokenService tokenService,
      IConfiguration configuration)
  {
    this.userRepository = userRepository;
    this.passwordHasher = passwordHasher;
    this.tokenService = tokenService;

    // Load configuration with defaults
    this.maxLoginAttempts = int.TryParse(configuration["Security:MaxLoginAttempts"], out var maxAttempts) ? maxAttempts : 5;
    this.lockoutDurationMinutes = int.TryParse(configuration["Security:LockoutDurationMinutes"], out var lockoutMin) ? lockoutMin : 15;
    this.refreshTokenExpirationDays = int.TryParse(configuration["Security:RefreshTokenExpirationDays"], out var refreshDays) ? refreshDays : 7;
  }

  /// <inheritdoc/>
  public async Task<AuthenticationResponse?> AuthenticateAsync(
      AuthenticationRequest request,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var user = await this.userRepository.GetWithRefreshTokensAsync(request.username, cancellationToken);

    if (user == null)
    {
      Log.Warning("Login attempt with non-existent username: {Username}", request.username);
      return null;
    }

    if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
    {
      Log.Warning("Login attempt for locked account: {UserId}", user.Id);
      return null;
    }

    if (!this.passwordHasher.VerifyPassword(user.PasswordHash, request.password))
    {
      await this.HandleFailedLoginAttemptAsync(user, cancellationToken);
      Log.Warning("Failed login attempt for user: {UserId}", user.Id);
      return null;
    }

    if (user.AccessFailedCount > 0)
    {
      user.AccessFailedCount = 0;
      user.LockoutEnd = null;
      await this.userRepository.SaveChangesAsync(cancellationToken);
    }

    var accessToken = this.tokenService.CreateToken(
        user.Id.ToString(),
        user.Username,
        new[] { user.IsActive ? "User" : "InactiveUser" });

    var refreshToken = this.GenerateRefreshToken(request.ipAddress);
    refreshToken.UserId = user.Id;
    await this.userRepository.AddRefreshTokenAsync(refreshToken, cancellationToken);

    this.RemoveOldRefreshTokens(user);

    await this.userRepository.SaveChangesAsync(cancellationToken);

    Log.Information("User {UserId} authenticated successfully", user.Id);

    return new AuthenticationResponse(
        accessToken,
        refreshToken.Token,
        "Bearer",
        3600);
  }

  /// <inheritdoc/>
  public async Task<AuthenticationResponse?> RefreshTokenAsync(
      RefreshTokenRequest request,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    var user = await this.userRepository.GetByRefreshTokenAsync(request.refreshToken, cancellationToken);

    if (user == null)
    {
      Log.Warning("Refresh token not found: {Token}", request.refreshToken);
      return null;
    }

    var refreshToken = user.RefreshTokens.Single(rt => rt.Token == request.refreshToken);

    if (!refreshToken.IsActive)
    {
      Log.Warning("Inactive refresh token used: {Token}", request.refreshToken);
      return null;
    }

    var newAccessToken = this.tokenService.CreateToken(
        user.Id.ToString(),
        user.Username,
        new[] { user.IsActive ? "User" : "InactiveUser" });

    var newRefreshToken = this.GenerateRefreshToken(request.ipAddress);

    refreshToken.RevokedAt = DateTime.UtcNow;
    refreshToken.RevokedByIp = request.ipAddress;
    refreshToken.ReplacedByToken = newRefreshToken.Token;

    newRefreshToken.UserId = user.Id;
    await this.userRepository.AddRefreshTokenAsync(newRefreshToken, cancellationToken);
    this.RemoveOldRefreshTokens(user);

    await this.userRepository.SaveChangesAsync(cancellationToken);

    Log.Information("Refresh token rotated for user {UserId}", user.Id);

    return new AuthenticationResponse(
        newAccessToken,
        newRefreshToken.Token,
        "Bearer",
        3600);
  }

  /// <inheritdoc/>
  public async Task<bool> RevokeTokenAsync(
      string token,
      string? ipAddress = null,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(token);

    var user = await this.userRepository.GetByRefreshTokenAsync(token, cancellationToken);

    if (user == null)
    {
      return false;
    }

    var refreshToken = user.RefreshTokens.Single(rt => rt.Token == token);

    if (!refreshToken.IsActive)
    {
      return false;
    }

    refreshToken.RevokedAt = DateTime.UtcNow;
    refreshToken.RevokedByIp = ipAddress;

    await this.userRepository.SaveChangesAsync(cancellationToken);

    Log.Information("Refresh token revoked for user {UserId}", user.Id);

    return true;
  }

  /// <inheritdoc/>
  public async Task<User> RegisterAsync(
      CreateUserDto dto,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(dto);

    var exists = await this.userRepository.ExistsByUsernameOrEmailAsync(dto.username, dto.email, cancellationToken);

    if (exists)
    {
      throw new InvalidOperationException("Username or email already exists");
    }

    var user = new User
    {
      Username = dto.username,
      Email = dto.email,
      PasswordHash = this.passwordHasher.HashPassword(dto.password),
      FirstName = dto.firstName,
      LastName = dto.lastName,
      IsActive = true,
    };

    await this.userRepository.AddAsync(user, cancellationToken);
    await this.userRepository.SaveChangesAsync(cancellationToken);

    Log.Information("New user registered: {UserId}", user.Id);

    return user;
  }

  /// <inheritdoc/>
  public async Task<bool> UnlockAccountAsync(
      Guid userId,
      CancellationToken cancellationToken = default)
  {
    var user = await this.userRepository.GetByIdAsync(userId, cancellationToken);

    if (user == null)
    {
      return false;
    }

    user.AccessFailedCount = 0;
    user.LockoutEnd = null;

    await this.userRepository.SaveChangesAsync(cancellationToken);

    Log.Information("Account unlocked: {UserId}", user.Id);

    return true;
  }

  private async Task HandleFailedLoginAttemptAsync(User user, CancellationToken cancellationToken)
  {
    user.AccessFailedCount++;

    if (user.AccessFailedCount >= this.maxLoginAttempts)
    {
      user.LockoutEnd = DateTime.UtcNow.AddMinutes(this.lockoutDurationMinutes);
      Log.Warning("Account locked due to too many failed attempts: {UserId}", user.Id);
    }

    await this.userRepository.SaveChangesAsync(cancellationToken);
  }

  private RefreshToken GenerateRefreshToken(string? ipAddress)
  {
    var randomBytes = new byte[64];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(randomBytes);
    var token = Convert.ToBase64String(randomBytes);

    return new RefreshToken
    {
      Token = token,
      ExpiresAt = DateTime.UtcNow.AddDays(this.refreshTokenExpirationDays),
      CreatedByIp = ipAddress,
    };
  }

  private void RemoveOldRefreshTokens(User user)
  {
    var oldTokens = user.RefreshTokens
        .Where(rt => !rt.IsActive && rt.CreatedAt.AddDays(this.refreshTokenExpirationDays) < DateTime.UtcNow)
        .ToList();

    foreach (var token in oldTokens)
    {
      user.RefreshTokens.Remove(token);
    }
  }
}
