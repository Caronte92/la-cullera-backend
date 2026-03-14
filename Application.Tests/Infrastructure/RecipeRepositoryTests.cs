using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Infrastructure;

public class RecipeRepositoryTests
{
  private DbContextOptions<AppDbContext> CreateOptions()
  {
    return new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
  }

  private static Recipe CreateRecipe(string name = "Test Recipe", string slug = "test-recipe", Guid? userId = null)
  {
    return new Recipe
    {
      Name = name,
      Slug = slug,
      Difficulty = "easy",
      UserId = userId ?? Guid.NewGuid(),
      ServingBase = 4,
      TimeCook = 30,
    };
  }

  private static Unit CreateUnit(string name = "gramo", string abbreviation = "g", string type = "weight", decimal factor = 1m)
  {
    return new Unit
    {
      Name = name,
      Abbreviation = abbreviation,
      Type = type,
      ToBaseFactor = factor,
    };
  }

  [Fact]
  public async Task AddAsync_ShouldAddRecipeToDatabase()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var repo = new RecipeRepository(context);
    var recipe = CreateRecipe();

    await repo.AddAsync(recipe);
    await repo.SaveChangesAsync();

    var saved = await context.Recipes.FirstOrDefaultAsync();
    saved.Should().NotBeNull();
    saved!.Name.Should().Be("Test Recipe");
    saved.Slug.Should().Be("test-recipe");
  }

  [Fact]
  public async Task GetByIdAsync_ShouldReturnRecipe()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var recipe = CreateRecipe();
    context.Recipes.Add(recipe);
    await context.SaveChangesAsync();

    var repo = new RecipeRepository(context);
    var result = await repo.GetByIdAsync(recipe.Id);

    result.Should().NotBeNull();
    result!.Id.Should().Be(recipe.Id);
  }

  [Fact]
  public async Task GetByIdAsync_ShouldNotReturnDeletedRecipe()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var recipe = CreateRecipe();
    recipe.IsDeleted = true;
    context.Recipes.Add(recipe);
    await context.SaveChangesAsync();

    var repo = new RecipeRepository(context);
    var result = await repo.GetByIdAsync(recipe.Id);

    result.Should().BeNull();
  }

  [Fact]
  public async Task GetBySlugAsync_ShouldReturnRecipeWithDetails()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var unit = CreateUnit();
    context.Units.Add(unit);

    var user = new User
    {
      Username = "chef",
      Email = "chef@test.com",
      PasswordHash = "hash",
    };
    context.Users.Add(user);

    var tag = new Tag { Name = "vegano", Slug = "vegano" };
    context.Tags.Add(tag);

    var recipe = CreateRecipe(userId: user.Id);
    recipe.Ingredients.Add(new Ingredient { Name = "Tomate", Amount = 2, UnitId = unit.Id, Order = 1 });
    recipe.Steps.Add(new Step { Order = 1, Description = "Cortar tomate" });
    recipe.RecipeTags.Add(new RecipeTag { TagId = tag.Id });
    context.Recipes.Add(recipe);
    await context.SaveChangesAsync();

    var repo = new RecipeRepository(context);
    var result = await repo.GetBySlugAsync("test-recipe");

    result.Should().NotBeNull();
    result!.Ingredients.Should().HaveCount(1);
    result.Steps.Should().HaveCount(1);
    result.RecipeTags.Should().HaveCount(1);
    result.User.Should().NotBeNull();
  }

  [Fact]
  public async Task GetWithDetailsAsync_ShouldIncludeAllRelations()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var unit = CreateUnit();
    context.Units.Add(unit);

    var user = new User
    {
      Username = "chef2",
      Email = "chef2@test.com",
      PasswordHash = "hash",
    };
    context.Users.Add(user);

    var recipe = CreateRecipe(name: "Paella", slug: "paella", userId: user.Id);
    recipe.Ingredients.Add(new Ingredient { Name = "Arroz", Amount = 500, UnitId = unit.Id, Order = 1 });
    recipe.Ingredients.Add(new Ingredient { Name = "Azafrán", Amount = 1, UnitId = unit.Id, Order = 2 });
    recipe.Steps.Add(new Step { Order = 1, Description = "Sofreír" });
    recipe.Steps.Add(new Step { Order = 2, Description = "Añadir arroz" });
    context.Recipes.Add(recipe);
    await context.SaveChangesAsync();

    var repo = new RecipeRepository(context);
    var result = await repo.GetWithDetailsAsync(recipe.Id);

    result.Should().NotBeNull();
    result!.Ingredients.Should().HaveCount(2);
    result.Ingredients.First().Order.Should().Be(1);
    result.Steps.Should().HaveCount(2);
    result.Steps.First().Order.Should().Be(1);
    result.User.Username.Should().Be("chef2");
  }

  [Fact]
  public async Task GetPagedAsync_ShouldReturnPagedResults()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var user = new User { Username = "pager", Email = "pager@test.com", PasswordHash = "hash" };
    context.Users.Add(user);
    for (int i = 0; i < 15; i++)
    {
      context.Recipes.Add(CreateRecipe($"Recipe {i}", $"recipe-{i}", user.Id));
    }

    await context.SaveChangesAsync();

    var repo = new RecipeRepository(context);
    var result = await repo.GetPagedAsync(2, 10);

    result.TotalCount.Should().Be(15);
    result.TotalPages.Should().Be(2);
    result.Items.Should().HaveCount(5);
  }

  [Fact]
  public async Task GetPagedAsync_ShouldFilterByUserId()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var user1 = new User { Username = "u1", Email = "u1@test.com", PasswordHash = "hash" };
    var user2 = new User { Username = "u2", Email = "u2@test.com", PasswordHash = "hash" };
    context.Users.AddRange(user1, user2);

    context.Recipes.Add(CreateRecipe("Recipe A", "recipe-a", user1.Id));
    context.Recipes.Add(CreateRecipe("Recipe B", "recipe-b", user1.Id));
    context.Recipes.Add(CreateRecipe("Recipe C", "recipe-c", user2.Id));
    await context.SaveChangesAsync();

    var repo = new RecipeRepository(context);
    var result = await repo.GetPagedAsync(1, 10, userId: user1.Id);

    result.TotalCount.Should().Be(2);
    result.Items.Should().AllSatisfy(r => r.UserId.Should().Be(user1.Id));
  }

  [Fact]
  public async Task GetByUserIdAsync_ShouldReturnUserRecipes()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var userId = Guid.NewGuid();

    context.Recipes.Add(CreateRecipe("Mine 1", "mine-1", userId));
    context.Recipes.Add(CreateRecipe("Mine 2", "mine-2", userId));
    context.Recipes.Add(CreateRecipe("Other", "other", Guid.NewGuid()));
    await context.SaveChangesAsync();

    var repo = new RecipeRepository(context);
    var result = await repo.GetByUserIdAsync(userId);

    result.Should().HaveCount(2);
  }

  [Fact]
  public async Task GetByTagAsync_ShouldReturnRecipesWithTag()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var tag = new Tag { Name = "Italiana", Slug = "italiana" };
    context.Tags.Add(tag);

    var recipe1 = CreateRecipe("Pasta", "pasta");
    recipe1.RecipeTags.Add(new RecipeTag { TagId = tag.Id });
    context.Recipes.Add(recipe1);

    var recipe2 = CreateRecipe("Pizza", "pizza");
    recipe2.RecipeTags.Add(new RecipeTag { TagId = tag.Id });
    context.Recipes.Add(recipe2);

    context.Recipes.Add(CreateRecipe("Sushi", "sushi"));
    await context.SaveChangesAsync();

    var repo = new RecipeRepository(context);
    var result = await repo.GetByTagAsync(tag.Id);

    result.Should().HaveCount(2);
  }

  [Fact]
  public async Task ExistsBySlugAsync_ShouldReturnTrueWhenExists()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    context.Recipes.Add(CreateRecipe());
    await context.SaveChangesAsync();

    var repo = new RecipeRepository(context);
    var result = await repo.ExistsBySlugAsync("test-recipe");

    result.Should().BeTrue();
  }

  [Fact]
  public async Task ExistsBySlugAsync_ShouldReturnFalseForDeleted()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var recipe = CreateRecipe();
    recipe.IsDeleted = true;
    context.Recipes.Add(recipe);
    await context.SaveChangesAsync();

    var repo = new RecipeRepository(context);
    var result = await repo.ExistsBySlugAsync("test-recipe");

    result.Should().BeFalse();
  }

  [Fact]
  public async Task UpdateAsync_ShouldSetUpdatedAt()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var recipe = CreateRecipe();
    context.Recipes.Add(recipe);
    await context.SaveChangesAsync();

    var repo = new RecipeRepository(context);
    recipe.Name = "Updated Name";
    await repo.UpdateAsync(recipe);
    await repo.SaveChangesAsync();

    var updated = await context.Recipes.FirstAsync(r => r.Id == recipe.Id);
    updated.Name.Should().Be("Updated Name");
    updated.UpdatedAt.Should().NotBeNull();
  }

  [Fact]
  public async Task DeleteAsync_ShouldSoftDeleteRecipe()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var recipe = CreateRecipe();
    context.Recipes.Add(recipe);
    await context.SaveChangesAsync();

    var repo = new RecipeRepository(context);
    await repo.DeleteAsync(recipe, "admin");
    await repo.SaveChangesAsync();

    var deleted = await context.Recipes.IgnoreQueryFilters().FirstAsync(r => r.Id == recipe.Id);
    deleted.IsDeleted.Should().BeTrue();
    deleted.DeletedAt.Should().NotBeNull();
    deleted.DeletedBy.Should().Be("admin");
  }
}
