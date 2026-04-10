namespace Application.DTOs;

public record SharedRecipeDto(
    Guid id,
    Guid recipeId,
    string recipeName,
    string recipeSlug,
    string sharedByUsername,
    DateTime createdAt);
