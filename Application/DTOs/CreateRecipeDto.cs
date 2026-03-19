// <copyright file="CreateRecipeDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.DTOs;

public record CreateRecipeDto(
    string name,
    string? imageUrl,
    string? videoUrl,
    int servingBase,
    int timeCook,
    string difficulty,
    IEnumerable<CreateIngredientDto> ingredients,
    IEnumerable<CreateStepDto> steps,
    IEnumerable<string> tags);
