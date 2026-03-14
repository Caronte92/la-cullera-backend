// <copyright file="LoginResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.DTOs;

public record LoginResponse(string accessToken, string tokenType = "Bearer", int expiresInSeconds = 3600);
