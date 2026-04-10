namespace Application.DTOs;

public record ShareRecipeResponseDto(
    string token,
    Guid recipeId,
    Guid sharedByUserId,
    DateTime createdAt);
