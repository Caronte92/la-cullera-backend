// <copyright file="RevokeTokenDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.DTOs;

/// <summary>
/// DTO for revoking a refresh token.
/// </summary>
public record RevokeTokenDto(string token);
