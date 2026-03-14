// <copyright file="RefreshTokenRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.DTOs;

/// <summary>
/// Request DTO for refreshing an access token.
/// </summary>
/// <param name="refreshToken">The refresh token.</param>
/// <param name="ipAddress">The IP address of the client.</param>
public record RefreshTokenRequest(string refreshToken, string? ipAddress = null);
