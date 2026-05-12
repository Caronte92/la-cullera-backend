// <copyright file="CreateStepDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.DTOs;

public record CreateStepDto(
    int order,
    string description,
    int? timerSeconds = null);
