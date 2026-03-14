// <copyright file="CreateIngredientDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.DTOs;

public record CreateIngredientDto(
    string name,
    decimal amount,
    Guid unitId,
    int order);
