using Application.Common.Models;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RecipeRepository : IRecipeRepository
{
  private readonly AppDbContext context;

  public RecipeRepository(AppDbContext context)
  {
    this.context = context;
  }

  public async Task<Recipe?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    return await this.context.Recipes
        .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
  }

  public async Task<Recipe?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
  {
    return await this.context.Recipes
        .AsNoTracking()
        .Include(r => r.Ingredients.OrderBy(i => i.Order))
            .ThenInclude(i => i.Unit)
        .Include(r => r.Steps.OrderBy(s => s.Order))
        .Include(r => r.RecipeTags)
            .ThenInclude(rt => rt.Tag)
        .Include(r => r.User)
        .FirstOrDefaultAsync(r => r.Slug == slug && !r.IsDeleted, cancellationToken);
  }

  public async Task<Recipe?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
  {
    return await this.context.Recipes
        .Include(r => r.Ingredients.OrderBy(i => i.Order))
            .ThenInclude(i => i.Unit)
        .Include(r => r.Steps.OrderBy(s => s.Order))
        .Include(r => r.RecipeTags)
            .ThenInclude(rt => rt.Tag)
        .Include(r => r.User)
        .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
  }

  public async Task<PagedResult<Recipe>> GetPagedAsync(
      int page,
      int pageSize,
      Guid? userId = null,
      string? search = null,
      IEnumerable<Guid>? tagIds = null,
      string? difficulty = null,
      string? timeRange = null,
      CancellationToken cancellationToken = default)
  {
    var query = this.context.Recipes
        .AsNoTracking()
        .Where(r => !r.IsDeleted);

    if (userId.HasValue)
    {
      query = query.Where(r => r.UserId == userId.Value);
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
      query = query.Where(r => EF.Functions.ILike(r.Name, $"%{search}%"));
    }

    var tagList = tagIds?.ToList();
    if (tagList is { Count: > 0 })
    {
      query = query.Where(r => r.RecipeTags.Any(rt => tagList.Contains(rt.TagId)));
    }

    if (!string.IsNullOrWhiteSpace(difficulty))
    {
      query = query.Where(r => r.Difficulty == difficulty);
    }

    query = timeRange switch
    {
      "under30" => query.Where(r => r.TimeCook < 30),
      "30to60" => query.Where(r => r.TimeCook >= 30 && r.TimeCook <= 60),
      "over60" => query.Where(r => r.TimeCook > 60),
      _ => query,
    };

    var totalCount = await query.CountAsync(cancellationToken);

    var items = await query
        .OrderByDescending(r => r.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Include(r => r.RecipeTags)
            .ThenInclude(rt => rt.Tag)
        .Include(r => r.User)
        .ToListAsync(cancellationToken);

    return new PagedResult<Recipe>(items, totalCount, page, pageSize);
  }

  public async Task<IEnumerable<Recipe>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
  {
    return await this.context.Recipes
        .AsNoTracking()
        .Where(r => r.UserId == userId && !r.IsDeleted)
        .OrderByDescending(r => r.CreatedAt)
        .Include(r => r.RecipeTags)
            .ThenInclude(rt => rt.Tag)
        .ToListAsync(cancellationToken);
  }

  public async Task<IEnumerable<Recipe>> GetByTagAsync(Guid tagId, CancellationToken cancellationToken = default)
  {
    return await this.context.Recipes
        .AsNoTracking()
        .Where(r => !r.IsDeleted && r.RecipeTags.Any(rt => rt.TagId == tagId))
        .OrderByDescending(r => r.CreatedAt)
        .Include(r => r.RecipeTags)
            .ThenInclude(rt => rt.Tag)
        .ToListAsync(cancellationToken);
  }

  public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
  {
    return await this.context.Recipes
        .AnyAsync(r => r.Slug == slug && !r.IsDeleted, cancellationToken);
  }

  public async Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default)
  {
    await this.context.Recipes.AddAsync(recipe, cancellationToken);
  }

  public async Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default)
  {
    recipe.UpdatedAt = DateTime.UtcNow;
    await Task.CompletedTask;
  }

  public async Task DeleteChildrenAsync(Guid recipeId, CancellationToken cancellationToken = default)
  {
    await this.context.RecipeTags
        .Where(rt => rt.RecipeId == recipeId)
        .ExecuteDeleteAsync(cancellationToken);
    await this.context.Steps
        .Where(s => s.RecipeId == recipeId)
        .ExecuteDeleteAsync(cancellationToken);
    await this.context.Ingredients
        .Where(i => i.RecipeId == recipeId)
        .ExecuteDeleteAsync(cancellationToken);
  }

  public async Task ReplaceChildrenAsync(
    Guid recipeId,
    IEnumerable<Ingredient> ingredients,
    IEnumerable<Step> steps,
    IEnumerable<RecipeTag> recipeTags,
    CancellationToken cancellationToken = default)
  {
    await this.DeleteChildrenAsync(recipeId, cancellationToken);

    await this.context.Ingredients.AddRangeAsync(ingredients, cancellationToken);
    await this.context.Steps.AddRangeAsync(steps, cancellationToken);
    await this.context.RecipeTags.AddRangeAsync(recipeTags, cancellationToken);
  }

  public async Task DeleteAsync(Recipe recipe, string? deletedBy = null, CancellationToken cancellationToken = default)
  {
    recipe.IsDeleted = true;
    recipe.DeletedAt = DateTime.UtcNow;
    recipe.DeletedBy = deletedBy;
    await Task.CompletedTask;
  }

  public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    await this.context.SaveChangesAsync(cancellationToken);
  }
}
