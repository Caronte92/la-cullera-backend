// <copyright file="UpdateRecipeDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.DTOs;

public record UpdateRecipeDto(
    string name,
    string? imageUrl,
    string? videoUrl,
    int servingBase,
    int timeCook,
    string difficulty,
    IEnumerable<CreateIngredientDto> ingredients,
    IEnumerable<CreateStepDto> steps,
    IEnumerable<Guid> tagIds);
