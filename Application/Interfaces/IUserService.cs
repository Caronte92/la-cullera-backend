// <copyright file="IUserService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Service for user authentication and management.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Authenticates a user with username/email and password.
    /// </summary>
    /// <param name="request">The authentication request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication response with tokens if successful; null otherwise.</returns>
    Task<AuthenticationResponse?> AuthenticateAsync(
        AuthenticationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    /// <param name="request">The refresh token request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New authentication response with tokens if successful; null otherwise.</returns>
    Task<AuthenticationResponse?> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    /// <param name="token">The token to revoke.</param>
    /// <param name="ipAddress">The IP address revoking the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if revoked successfully; false otherwise.</returns>
    Task<bool> RevokeTokenAsync(
        string token,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="dto">The user creation DTO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user.</returns>
    Task<User> RegisterAsync(
        CreateUserDto dto,
        CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    Task<bool> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken = default);

    Task<bool> DeleteAccountAsync(Guid userId, string password, CancellationToken cancellationToken = default);
}
