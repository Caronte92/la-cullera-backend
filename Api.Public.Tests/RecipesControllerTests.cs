// <copyright file="RecipesControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Security.Claims;
using Api.Public.Controllers;
using Application.Common.Models;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Api.Public.Tests;

public class RecipesControllerTests
{
  private readonly Mock<IRecipeRepository> recipeRepoMock;
  private readonly Mock<ITagRepository> tagRepoMock;
  private readonly RecipesController sut;

  public RecipesControllerTests()
  {
    this.recipeRepoMock = new Mock<IRecipeRepository>();
    this.tagRepoMock = new Mock<ITagRepository>();
    this.sut = new RecipesController(this.recipeRepoMock.Object, this.tagRepoMock.Object);
    this.sut.ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext(),
    };
  }

  private void SetUser(Guid userId)
  {
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
    this.sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
  }

  // --- GetAll ---

  [Fact]
  public async Task GetAll_ShouldReturnOkWithPagedResult()
  {
    var recipes = new List<Recipe> { new Recipe { Name = "Paella", Slug = "paella", Difficulty = "medium" } };
    var paged = new PagedResult<Recipe>(recipes, 1, 1, 10);
    this.recipeRepoMock
        .Setup(r => r.GetPagedAsync(1, 10, null, null, default))
        .ReturnsAsync(paged);

    var result = await this.sut.GetAll();

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    ok.Value.Should().Be(paged);
  }

  [Fact]
  public async Task GetAll_ShouldClampInvalidPageSize()
  {
    var paged = new PagedResult<Recipe>([], 0, 1, 10);
    this.recipeRepoMock
        .Setup(r => r.GetPagedAsync(1, 10, null, null, default))
        .ReturnsAsync(paged);

    var result = await this.sut.GetAll(page: -1, pageSize: 999);

    result.Should().BeOfType<OkObjectResult>();
    this.recipeRepoMock.Verify(r => r.GetPagedAsync(1, 10, null, null, default), Times.Once);
  }

  // --- GetBySlug ---

  [Fact]
  public async Task GetBySlug_ShouldReturnOk_WhenFound()
  {
    var recipe = new Recipe { Name = "Paella", Slug = "paella", Difficulty = "medium" };
    this.recipeRepoMock
        .Setup(r => r.GetBySlugAsync("paella", default))
        .ReturnsAsync(recipe);

    var result = await this.sut.GetBySlug("paella");

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    ok.Value.Should().Be(recipe);
  }

  [Fact]
  public async Task GetBySlug_ShouldReturnNotFound_WhenNotFound()
  {
    this.recipeRepoMock
        .Setup(r => r.GetBySlugAsync("nonexistent", default))
        .ReturnsAsync((Recipe?)null);

    var result = await this.sut.GetBySlug("nonexistent");

    result.Should().BeOfType<NotFoundObjectResult>();
  }

  // --- GetByTag ---

  [Fact]
  public async Task GetByTag_ShouldReturnOk()
  {
    var tagId = Guid.NewGuid();
    var recipes = new List<Recipe>
    {
      new Recipe { Name = "Pasta", Slug = "pasta", Difficulty = "easy" },
    };
    this.recipeRepoMock
        .Setup(r => r.GetByTagAsync(tagId, default))
        .ReturnsAsync(recipes);

    var result = await this.sut.GetByTag(tagId);

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    ok.Value.Should().Be(recipes);
  }

  // --- GetMyRecipes ---

  [Fact]
  public async Task GetMyRecipes_ShouldReturnUnauthorized_WhenNoUser()
  {
    var result = await this.sut.GetMyRecipes();

    result.Should().BeOfType<UnauthorizedResult>();
  }

  [Fact]
  public async Task GetMyRecipes_ShouldReturnOk_WhenAuthenticated()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    var recipes = new List<Recipe>
    {
      new Recipe { Name = "Mine", Slug = "mine", Difficulty = "easy", UserId = userId },
    };
    this.recipeRepoMock
        .Setup(r => r.GetByUserIdAsync(userId, default))
        .ReturnsAsync(recipes);

    var result = await this.sut.GetMyRecipes();

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    ok.Value.Should().Be(recipes);
  }

  // --- Create ---

  [Fact]
  public async Task Create_ShouldReturnUnauthorized_WhenNoUser()
  {
    var dto = new CreateRecipeDto("Test", null, null, 4, 30, "easy", [], [], []);

    var result = await this.sut.Create(dto);

    result.Should().BeOfType<UnauthorizedResult>();
  }

  [Fact]
  public async Task Create_ShouldReturnConflict_WhenSlugExists()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    var dto = new CreateRecipeDto("Paella", null, null, 4, 30, "easy", [], [], []);
    this.recipeRepoMock
        .Setup(r => r.ExistsBySlugAsync("paella", default))
        .ReturnsAsync(true);

    var result = await this.sut.Create(dto);

    result.Should().BeOfType<ConflictObjectResult>();
  }

  [Fact]
  public async Task Create_ShouldReturnCreated_WhenValid()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    var ingredients = new[] { new CreateIngredientDto("Arroz", 500, Guid.NewGuid(), 1) };
    var steps = new[] { new CreateStepDto(1, "Sofreír") };
    var tagId = Guid.NewGuid();
    var dto = new CreateRecipeDto("Paella", null, null, 4, 30, "medium", ingredients, steps, new[] { tagId });

    this.recipeRepoMock
        .Setup(r => r.ExistsBySlugAsync("paella", default))
        .ReturnsAsync(false);

    var result = await this.sut.Create(dto);

    var created = result.Should().BeOfType<CreatedResult>().Subject;
    created.Location.Should().Contain("paella");
    this.recipeRepoMock.Verify(r => r.AddAsync(It.Is<Recipe>(rec =>
        rec.Name == "Paella" &&
        rec.Slug == "paella" &&
        rec.UserId == userId &&
        rec.Ingredients.Count == 1 &&
        rec.Steps.Count == 1 &&
        rec.RecipeTags.Count == 1), default), Times.Once);
    this.recipeRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }

  // --- Update ---

  [Fact]
  public async Task Update_ShouldReturnUnauthorized_WhenNoUser()
  {
    var dto = new UpdateRecipeDto("Test", null, null, 4, 30, "easy", [], [], []);

    var result = await this.sut.Update(Guid.NewGuid(), dto);

    result.Should().BeOfType<UnauthorizedResult>();
  }

  [Fact]
  public async Task Update_ShouldReturnNotFound_WhenRecipeNotFound()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    this.recipeRepoMock
        .Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>(), default))
        .ReturnsAsync((Recipe?)null);

    var dto = new UpdateRecipeDto("Test", null, null, 4, 30, "easy", [], [], []);
    var result = await this.sut.Update(Guid.NewGuid(), dto);

    result.Should().BeOfType<NotFoundObjectResult>();
  }

  [Fact]
  public async Task Update_ShouldReturnForbid_WhenNotOwner()
  {
    var userId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();
    this.SetUser(userId);
    var recipe = new Recipe { Name = "Other", Slug = "other", Difficulty = "easy", UserId = otherUserId };
    this.recipeRepoMock
        .Setup(r => r.GetWithDetailsAsync(It.IsAny<Guid>(), default))
        .ReturnsAsync(recipe);

    var dto = new UpdateRecipeDto("Updated", null, null, 4, 30, "easy", [], [], []);
    var result = await this.sut.Update(recipe.Id, dto);

    result.Should().BeOfType<ForbidResult>();
  }

  [Fact]
  public async Task Update_ShouldReturnOk_WhenValid()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    var recipe = new Recipe { Name = "Old", Slug = "old", Difficulty = "easy", UserId = userId };
    this.recipeRepoMock
        .Setup(r => r.GetWithDetailsAsync(recipe.Id, default))
        .ReturnsAsync(recipe);
    this.recipeRepoMock
        .Setup(r => r.ExistsBySlugAsync("updated", default))
        .ReturnsAsync(false);

    var dto = new UpdateRecipeDto("Updated", null, null, 6, 45, "hard", [], [], []);
    var result = await this.sut.Update(recipe.Id, dto);

    result.Should().BeOfType<OkObjectResult>();
    this.recipeRepoMock.Verify(r => r.UpdateAsync(It.Is<Recipe>(rec =>
        rec.Name == "Updated" && rec.Slug == "updated"), default), Times.Once);
    this.recipeRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }

  // --- Delete ---

  [Fact]
  public async Task Delete_ShouldReturnUnauthorized_WhenNoUser()
  {
    var result = await this.sut.Delete(Guid.NewGuid());

    result.Should().BeOfType<UnauthorizedResult>();
  }

  [Fact]
  public async Task Delete_ShouldReturnNotFound_WhenRecipeNotFound()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    this.recipeRepoMock
        .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
        .ReturnsAsync((Recipe?)null);

    var result = await this.sut.Delete(Guid.NewGuid());

    result.Should().BeOfType<NotFoundObjectResult>();
  }

  [Fact]
  public async Task Delete_ShouldReturnForbid_WhenNotOwner()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    var recipe = new Recipe { Name = "Other", Slug = "other", Difficulty = "easy", UserId = Guid.NewGuid() };
    this.recipeRepoMock
        .Setup(r => r.GetByIdAsync(recipe.Id, default))
        .ReturnsAsync(recipe);

    var result = await this.sut.Delete(recipe.Id);

    result.Should().BeOfType<ForbidResult>();
  }

  [Fact]
  public async Task Delete_ShouldReturnNoContent_WhenValid()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    var recipe = new Recipe { Name = "Mine", Slug = "mine", Difficulty = "easy", UserId = userId };
    this.recipeRepoMock
        .Setup(r => r.GetByIdAsync(recipe.Id, default))
        .ReturnsAsync(recipe);

    var result = await this.sut.Delete(recipe.Id);

    result.Should().BeOfType<NoContentResult>();
    this.recipeRepoMock.Verify(r => r.DeleteAsync(recipe, userId.ToString(), default), Times.Once);
    this.recipeRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }
}
