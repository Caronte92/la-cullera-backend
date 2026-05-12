// <copyright file="RecipesController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Public.Controllers;

/// <summary>
/// Controller for managing recipes.
/// </summary>
[ApiController]
[Route("recipes")]
[Authorize]
public partial class RecipesController : ControllerBase
{
  private readonly IRecipeRepository recipeRepository;
  private readonly ITagRepository tagRepository;

  public RecipesController(IRecipeRepository recipeRepository, ITagRepository tagRepository)
  {
    this.recipeRepository = recipeRepository;
    this.tagRepository = tagRepository;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll(
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 10,
      [FromQuery] string? search = null,
      [FromQuery] Guid? userId = null)
  {
    if (page < 1)
    {
      page = 1;
    }

    if (pageSize < 1 || pageSize > 50)
    {
      pageSize = 10;
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
      try
      {
        _ = new Regex(search);
      }
      catch (ArgumentException)
      {
        return this.BadRequest(new { message = "Invalid regex pattern" });
      }
    }

    var result = await this.recipeRepository.GetPagedAsync(page, pageSize, userId, search);

    return this.Ok(result);
  }

  [HttpGet("{slug}")]
  public async Task<IActionResult> GetBySlug(string slug)
  {
    var recipe = await this.recipeRepository.GetBySlugAsync(slug);

    if (recipe == null)
    {
      return this.NotFound(new { message = "Recipe not found" });
    }

    return this.Ok(recipe);
  }

  [HttpGet("tag/{tagId:guid}")]
  public async Task<IActionResult> GetByTag(Guid tagId)
  {
    var recipes = await this.recipeRepository.GetByTagAsync(tagId);
    return this.Ok(recipes);
  }

  [HttpGet("mine")]
  public async Task<IActionResult> GetMyRecipes()
  {
    var userId = this.GetUserId();
    if (userId == null)
    {
      return this.Unauthorized();
    }

    var recipes = await this.recipeRepository.GetByUserIdAsync(userId.Value);
    return this.Ok(recipes);
  }

  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateRecipeDto dto)
  {
    var userId = this.GetUserId();
    if (userId == null)
    {
      return this.Unauthorized();
    }

    var slug = GenerateSlug(dto.name);

    if (await this.recipeRepository.ExistsBySlugAsync(slug))
    {
      return this.Conflict(new { message = "A recipe with this name already exists" });
    }

    var recipe = new Recipe
    {
      UserId = userId.Value,
      Name = dto.name,
      Slug = slug,
      ImageUrl = dto.imageUrl,
      VideoUrl = dto.videoUrl,
      ServingBase = dto.servingBase,
      TimeCook = dto.timeCook,
      Difficulty = dto.difficulty,
      CreatedBy = userId.Value.ToString(),
    };

    foreach (var ingredientDto in dto.ingredients)
    {
      recipe.Ingredients.Add(new Ingredient
      {
        Name = ingredientDto.name,
        Amount = ingredientDto.amount,
        UnitId = ingredientDto.unitId,
        Order = ingredientDto.order,
      });
    }

    foreach (var stepDto in dto.steps)
    {
      recipe.Steps.Add(new Step
      {
        Order = stepDto.order,
        Description = stepDto.description,
        TimerSeconds = stepDto.timerSeconds,
      });
    }

    foreach (var tagName in dto.tags)
    {
      var tag = await this.ResolveTagAsync(tagName);
      recipe.RecipeTags.Add(new RecipeTag { TagId = tag.Id });
    }

    await this.recipeRepository.AddAsync(recipe);
    await this.recipeRepository.SaveChangesAsync();

    return this.Created($"/recipes/{slug}", new { recipe.Id, recipe.Slug });
  }

  [HttpPut("{id:guid}")]
  public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRecipeDto dto)
  {
    var userId = this.GetUserId();
    if (userId == null)
    {
      return this.Unauthorized();
    }

    var recipe = await this.recipeRepository.GetWithDetailsAsync(id);

    if (recipe == null)
    {
      return this.NotFound(new { message = "Recipe not found" });
    }

    if (recipe.UserId != userId.Value)
    {
      return this.Forbid();
    }

    var newSlug = GenerateSlug(dto.name);
    if (newSlug != recipe.Slug && await this.recipeRepository.ExistsBySlugAsync(newSlug))
    {
      return this.Conflict(new { message = "A recipe with this name already exists" });
    }

    recipe.Name = dto.name;
    recipe.Slug = newSlug;
    recipe.ImageUrl = dto.imageUrl;
    recipe.VideoUrl = dto.videoUrl;
    recipe.ServingBase = dto.servingBase;
    recipe.TimeCook = dto.timeCook;
    recipe.Difficulty = dto.difficulty;
    recipe.UpdatedBy = userId.Value.ToString();

    recipe.Ingredients.Clear();
    foreach (var ingredientDto in dto.ingredients)
    {
      recipe.Ingredients.Add(new Ingredient
      {
        Name = ingredientDto.name,
        Amount = ingredientDto.amount,
        UnitId = ingredientDto.unitId,
        Order = ingredientDto.order,
      });
    }

    recipe.Steps.Clear();
    foreach (var stepDto in dto.steps)
    {
      recipe.Steps.Add(new Step
      {
        Order = stepDto.order,
        Description = stepDto.description,
        TimerSeconds = stepDto.timerSeconds,
      });
    }

    recipe.RecipeTags.Clear();
    foreach (var tagName in dto.tags)
    {
      var tag = await this.ResolveTagAsync(tagName);
      recipe.RecipeTags.Add(new RecipeTag { TagId = tag.Id });
    }

    await this.recipeRepository.UpdateAsync(recipe);
    await this.recipeRepository.SaveChangesAsync();

    return this.Ok(new { recipe.Id, recipe.Slug });
  }

  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(Guid id)
  {
    var userId = this.GetUserId();
    if (userId == null)
    {
      return this.Unauthorized();
    }

    var recipe = await this.recipeRepository.GetByIdAsync(id);

    if (recipe == null)
    {
      return this.NotFound(new { message = "Recipe not found" });
    }

    if (recipe.UserId != userId.Value)
    {
      return this.Forbid();
    }

    await this.recipeRepository.DeleteAsync(recipe, userId.Value.ToString());
    await this.recipeRepository.SaveChangesAsync();

    return this.NoContent();
  }

  private static string GenerateSlug(string name)
  {
    var slug = name.ToLower(CultureInfo.InvariantCulture).Trim();
    slug = SlugInvalidCharsRegex().Replace(slug, string.Empty);
    slug = SlugWhitespaceRegex().Replace(slug, "-");
    slug = SlugMultipleDashRegex().Replace(slug, "-");
    return slug.Trim('-');
  }

  [GeneratedRegex(@"[^a-z0-9\s-]")]
  private static partial Regex SlugInvalidCharsRegex();

  [GeneratedRegex(@"\s+")]
  private static partial Regex SlugWhitespaceRegex();

  [GeneratedRegex(@"-{2,}")]
  private static partial Regex SlugMultipleDashRegex();

  private async Task<Tag> ResolveTagAsync(string tagName)
  {
    var existing = await this.tagRepository.GetByNameAsync(tagName);
    if (existing != null)
    {
      return existing;
    }

    var tag = new Tag
    {
      Name = tagName,
      Slug = GenerateSlug(tagName),
    };

    await this.tagRepository.AddAsync(tag);
    await this.tagRepository.SaveChangesAsync();
    return tag;
  }

  private Guid? GetUserId()
  {
    var claim = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return Guid.TryParse(claim, out var id) ? id : null;
  }
}
