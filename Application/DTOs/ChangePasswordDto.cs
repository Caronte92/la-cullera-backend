// <copyright file="ChangePasswordDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.DTOs;

public record ChangePasswordDto(
    string currentPassword,
    string newPassword);
