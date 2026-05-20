using Application.Common.Models;
using Domain.Entities;

namespace Application.Interfaces;

public interface IRecipeRepository
{
  Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

  Task<Recipe?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

  Task<Recipe?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

  Task<PagedResult<Recipe>> GetPagedAsync(int page, int pageSize, Guid? userId = null, string? search = null, CancellationToken cancellationToken = default);

  Task<IEnumerable<Recipe>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

  Task<IEnumerable<Recipe>> GetByTagAsync(Guid tagId, CancellationToken cancellationToken = default);

  Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default);

  Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default);

  Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default);

  Task DeleteAsync(Recipe recipe, string? deletedBy = null, CancellationToken cancellationToken = default);

  Task DeleteChildrenAsync(Guid recipeId, CancellationToken cancellationToken = default);

  Task ReplaceChildrenAsync(
    Guid recipeId,
    IEnumerable<Ingredient> ingredients,
    IEnumerable<Step> steps,
    IEnumerable<RecipeTag> recipeTags,
    CancellationToken cancellationToken = default);

  Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
