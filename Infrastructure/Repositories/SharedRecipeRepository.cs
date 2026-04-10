using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class SharedRecipeRepository : ISharedRecipeRepository
{
  private readonly AppDbContext context;

  public SharedRecipeRepository(AppDbContext context)
  {
    this.context = context;
  }

  public async Task<SharedRecipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    return await this.context.SharedRecipes
        .AsNoTracking()
        .Include(sr => sr.Recipe)
        .Include(sr => sr.SharedByUser)
        .Include(sr => sr.User)
        .FirstOrDefaultAsync(sr => sr.Id == id && !sr.IsDeleted, cancellationToken);
  }

  public async Task<SharedRecipe?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
  {
    return await this.context.SharedRecipes
        .AsNoTracking()
        .Include(sr => sr.Recipe)
            .ThenInclude(r => r.Ingredients.OrderBy(i => i.Order))
                .ThenInclude(i => i.Unit)
        .Include(sr => sr.Recipe)
            .ThenInclude(r => r.Steps.OrderBy(s => s.Order))
        .Include(sr => sr.Recipe)
            .ThenInclude(r => r.RecipeTags)
                .ThenInclude(rt => rt.Tag)
        .Include(sr => sr.Recipe)
            .ThenInclude(r => r.User)
        .Include(sr => sr.SharedByUser)
        .Include(sr => sr.User)
        .FirstOrDefaultAsync(sr => sr.Token == token && !sr.IsDeleted, cancellationToken);
  }

  public async Task<IEnumerable<SharedRecipe>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
  {
    return await this.context.SharedRecipes
        .AsNoTracking()
        .Where(sr => sr.UserId == userId && !sr.IsDeleted)
        .OrderByDescending(sr => sr.CreatedAt)
        .Include(sr => sr.Recipe)
        .Include(sr => sr.SharedByUser)
        .ToListAsync(cancellationToken);
  }

  public async Task<IEnumerable<SharedRecipe>> GetBySharedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
  {
    return await this.context.SharedRecipes
        .AsNoTracking()
        .Where(sr => sr.SharedByUserId == userId && !sr.IsDeleted)
        .OrderByDescending(sr => sr.CreatedAt)
        .Include(sr => sr.Recipe)
        .Include(sr => sr.User)
        .ToListAsync(cancellationToken);
  }

  public async Task<SharedRecipe?> GetByRecipeAndUserAsync(Guid recipeId, Guid userId, CancellationToken cancellationToken = default)
  {
    return await this.context.SharedRecipes
        .AsNoTracking()
        .FirstOrDefaultAsync(sr => sr.RecipeId == recipeId && sr.UserId == userId && !sr.IsDeleted, cancellationToken);
  }

  public async Task AddAsync(SharedRecipe sharedRecipe, CancellationToken cancellationToken = default)
  {
    await this.context.SharedRecipes.AddAsync(sharedRecipe, cancellationToken);
  }

  public async Task AcceptAsync(Guid sharedRecipeId, Guid userId, CancellationToken cancellationToken = default)
  {
    var entity = await this.context.SharedRecipes.FindAsync(new object[] { sharedRecipeId }, cancellationToken);
    if (entity != null)
    {
      entity.UserId = userId;
      entity.UpdatedAt = DateTime.UtcNow;
      entity.UpdatedBy = userId.ToString();
    }
  }

  public async Task DeleteAsync(SharedRecipe sharedRecipe, string? deletedBy = null, CancellationToken cancellationToken = default)
  {
    var entity = await this.context.SharedRecipes.FindAsync(new object[] { sharedRecipe.Id }, cancellationToken);
    if (entity != null)
    {
      entity.IsDeleted = true;
      entity.DeletedAt = DateTime.UtcNow;
      entity.DeletedBy = deletedBy;
    }
  }

  public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    await this.context.SaveChangesAsync(cancellationToken);
  }
}
