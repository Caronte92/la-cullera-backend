using Domain.Entities;

namespace Application.Interfaces;

public interface ISharedRecipeRepository
{
  Task<SharedRecipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

  Task<SharedRecipe?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

  Task<IEnumerable<SharedRecipe>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

  Task<IEnumerable<SharedRecipe>> GetBySharedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

  Task<SharedRecipe?> GetByRecipeAndUserAsync(Guid recipeId, Guid userId, CancellationToken cancellationToken = default);

  Task AddAsync(SharedRecipe sharedRecipe, CancellationToken cancellationToken = default);

  Task AcceptAsync(Guid sharedRecipeId, Guid userId, CancellationToken cancellationToken = default);

  Task DeleteAsync(SharedRecipe sharedRecipe, string? deletedBy = null, CancellationToken cancellationToken = default);

  Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
