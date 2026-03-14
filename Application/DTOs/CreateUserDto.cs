// <copyright file="CreateUserDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.DTOs;

public record CreateUserDto(
    string username,
    string email,
    string password,
    string firstName,
    string lastName);
