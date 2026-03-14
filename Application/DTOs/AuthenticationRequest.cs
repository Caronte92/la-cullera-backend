// <copyright file="AuthenticationRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.DTOs;

/// <summary>
/// Request DTO for user authentication.
/// </summary>
/// <param name="username">The username or email.</param>
/// <param name="password">The user's password.</param>
/// <param name="ipAddress">The IP address of the client.</param>
public record AuthenticationRequest(string username, string password, string? ipAddress = null);
