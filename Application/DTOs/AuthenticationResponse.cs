// <copyright file="AuthenticationResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.DTOs;

/// <summary>
/// Response DTO for successful authentication.
/// </summary>
/// <param name="accessToken">The JWT access token.</param>
/// <param name="refreshToken">The refresh token.</param>
/// <param name="tokenType">The type of token (typically "Bearer").</param>
/// <param name="expiresIn">The number of seconds until the access token expires.</param>
public record AuthenticationResponse(
    string accessToken,
    string refreshToken,
    string tokenType,
    int expiresIn);
