using System.Security.Claims;
using System.Security.Cryptography;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Public.Controllers;

[ApiController]
[Authorize]
public class SharedRecipesController : ControllerBase
{
  private readonly ISharedRecipeRepository sharedRecipeRepository;
  private readonly IRecipeRepository recipeRepository;

  public SharedRecipesController(
      ISharedRecipeRepository sharedRecipeRepository,
      IRecipeRepository recipeRepository)
  {
    this.sharedRecipeRepository = sharedRecipeRepository;
    this.recipeRepository = recipeRepository;
  }

  [HttpPost("recipes/{id:guid}/share")]
  public async Task<IActionResult> Share(Guid id)
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

    var token = GenerateToken();

    var sharedRecipe = new SharedRecipe
    {
      RecipeId = id,
      SharedByUserId = userId.Value,
      Token = token,
      CreatedBy = userId.Value.ToString(),
    };

    await this.sharedRecipeRepository.AddAsync(sharedRecipe);
    await this.sharedRecipeRepository.SaveChangesAsync();

    return this.Created(
        $"/recipes/shared/{token}",
        new ShareRecipeResponseDto(token, id, userId.Value, sharedRecipe.CreatedAt));
  }

  [HttpPost("recipes/shared/accept/{token}")]
  public async Task<IActionResult> Accept(string token)
  {
    var userId = this.GetUserId();
    if (userId == null)
    {
      return this.Unauthorized();
    }

    var shared = await this.sharedRecipeRepository.GetByTokenAsync(token);
    if (shared == null)
    {
      return this.NotFound(new { message = "Share token not found" });
    }

    if (shared.SharedByUserId == userId.Value)
    {
      return this.BadRequest(new { message = "Cannot accept your own shared recipe" });
    }

    if (shared.UserId != null && shared.UserId != userId.Value)
    {
      return this.Conflict(new { message = "This share token has already been claimed" });
    }

    if (shared.UserId == userId.Value)
    {
      return this.Ok(new AcceptShareResponseDto(shared.RecipeId, shared.Recipe.Slug));
    }

    var existing = await this.sharedRecipeRepository.GetByRecipeAndUserAsync(shared.RecipeId, userId.Value);
    if (existing != null)
    {
      return this.Conflict(new { message = "This recipe has already been shared with you" });
    }

    await this.sharedRecipeRepository.AcceptAsync(shared.Id, userId.Value);
    await this.sharedRecipeRepository.SaveChangesAsync();

    return this.Ok(new AcceptShareResponseDto(shared.RecipeId, shared.Recipe.Slug));
  }

  [HttpGet("recipes/shared")]
  public async Task<IActionResult> GetMySharedRecipes()
  {
    var userId = this.GetUserId();
    if (userId == null)
    {
      return this.Unauthorized();
    }

    var shares = await this.sharedRecipeRepository.GetByUserIdAsync(userId.Value);

    var result = shares.Select(sr => new SharedRecipeDto(
        sr.Id,
        sr.RecipeId,
        sr.Recipe.Name,
        sr.Recipe.Slug,
        sr.SharedByUser.Username,
        sr.CreatedAt));

    return this.Ok(result);
  }

  [HttpGet("recipes/shared/{token}")]
  public async Task<IActionResult> GetByToken(string token)
  {
    var userId = this.GetUserId();
    if (userId == null)
    {
      return this.Unauthorized();
    }

    var shared = await this.sharedRecipeRepository.GetByTokenAsync(token);
    if (shared == null)
    {
      return this.NotFound(new { message = "Share token not found" });
    }

    if (shared.UserId != null && shared.UserId != userId.Value && shared.SharedByUserId != userId.Value)
    {
      return this.Forbid();
    }

    return this.Ok(shared.Recipe);
  }

  [HttpDelete("recipes/shared/{id:guid}")]
  public async Task<IActionResult> Delete(Guid id)
  {
    var userId = this.GetUserId();
    if (userId == null)
    {
      return this.Unauthorized();
    }

    var shared = await this.sharedRecipeRepository.GetByIdAsync(id);
    if (shared == null)
    {
      return this.NotFound(new { message = "Shared recipe not found" });
    }

    if (shared.SharedByUserId != userId.Value && shared.UserId != userId.Value)
    {
      return this.Forbid();
    }

    await this.sharedRecipeRepository.DeleteAsync(shared, userId.Value.ToString());
    await this.sharedRecipeRepository.SaveChangesAsync();

    return this.NoContent();
  }

  private Guid? GetUserId()
  {
    var claim = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return Guid.TryParse(claim, out var id) ? id : null;
  }

  private static string GenerateToken()
  {
    var bytes = RandomNumberGenerator.GetBytes(64);
    return Convert.ToBase64String(bytes)
        .Replace("+", "-")
        .Replace("/", "_")
        .TrimEnd('=');
  }
}
