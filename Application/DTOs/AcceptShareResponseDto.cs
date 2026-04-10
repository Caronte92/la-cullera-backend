namespace Application.DTOs;

public record AcceptShareResponseDto(
    Guid recipeId,
    string recipeSlug);
