// <copyright file="SharedRecipesControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Security.Claims;
using Api.Public.Controllers;
using Application.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Api.Public.Tests;

public class SharedRecipesControllerTests
{
  private readonly Mock<ISharedRecipeRepository> sharedRepoMock;
  private readonly Mock<IRecipeRepository> recipeRepoMock;
  private readonly SharedRecipesController sut;

  public SharedRecipesControllerTests()
  {
    this.sharedRepoMock = new Mock<ISharedRecipeRepository>();
    this.recipeRepoMock = new Mock<IRecipeRepository>();
    this.sut = new SharedRecipesController(this.sharedRepoMock.Object, this.recipeRepoMock.Object);
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

  // --- Share ---

  [Fact]
  public async Task Share_ShouldReturnUnauthorized_WhenNoUser()
  {
    var result = await this.sut.Share(Guid.NewGuid());

    result.Should().BeOfType<UnauthorizedResult>();
  }

  [Fact]
  public async Task Share_ShouldReturnNotFound_WhenRecipeNotFound()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    this.recipeRepoMock
        .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
        .ReturnsAsync((Recipe?)null);

    var result = await this.sut.Share(Guid.NewGuid());

    result.Should().BeOfType<NotFoundObjectResult>();
  }

  [Fact]
  public async Task Share_ShouldReturnForbid_WhenNotOwner()
  {
    var userId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();
    this.SetUser(userId);
    var recipe = new Recipe { Name = "Test", Slug = "test", Difficulty = "easy", UserId = otherUserId };
    this.recipeRepoMock
        .Setup(r => r.GetByIdAsync(recipe.Id, default))
        .ReturnsAsync(recipe);

    var result = await this.sut.Share(recipe.Id);

    result.Should().BeOfType<ForbidResult>();
  }

  [Fact]
  public async Task Share_ShouldReturnCreated_WhenValid()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    var recipe = new Recipe { Name = "Paella", Slug = "paella", Difficulty = "medium", UserId = userId };
    this.recipeRepoMock
        .Setup(r => r.GetByIdAsync(recipe.Id, default))
        .ReturnsAsync(recipe);

    var result = await this.sut.Share(recipe.Id);

    result.Should().BeOfType<CreatedResult>();
    this.sharedRepoMock.Verify(r => r.AddAsync(It.Is<SharedRecipe>(sr =>
        sr.RecipeId == recipe.Id &&
        sr.SharedByUserId == userId &&
        !string.IsNullOrEmpty(sr.Token)), default), Times.Once);
    this.sharedRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }

  // --- Accept ---

  [Fact]
  public async Task Accept_ShouldReturnUnauthorized_WhenNoUser()
  {
    var result = await this.sut.Accept("some-token");

    result.Should().BeOfType<UnauthorizedResult>();
  }

  [Fact]
  public async Task Accept_ShouldReturnNotFound_WhenTokenNotFound()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    this.sharedRepoMock
        .Setup(r => r.GetByTokenAsync("invalid", default))
        .ReturnsAsync((SharedRecipe?)null);

    var result = await this.sut.Accept("invalid");

    result.Should().BeOfType<NotFoundObjectResult>();
  }

  [Fact]
  public async Task Accept_ShouldReturnBadRequest_WhenAcceptingOwnShare()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    var shared = new SharedRecipe
    {
      Token = "token",
      RecipeId = Guid.NewGuid(),
      SharedByUserId = userId,
      Recipe = new Recipe { Name = "Test", Slug = "test", Difficulty = "easy", UserId = userId },
    };
    this.sharedRepoMock
        .Setup(r => r.GetByTokenAsync("token", default))
        .ReturnsAsync(shared);

    var result = await this.sut.Accept("token");

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task Accept_ShouldReturnConflict_WhenAlreadyClaimedByOther()
  {
    var userId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();
    var ownerId = Guid.NewGuid();
    this.SetUser(userId);
    var shared = new SharedRecipe
    {
      Token = "token",
      RecipeId = Guid.NewGuid(),
      SharedByUserId = ownerId,
      UserId = otherUserId,
      Recipe = new Recipe { Name = "Test", Slug = "test", Difficulty = "easy", UserId = ownerId },
    };
    this.sharedRepoMock
        .Setup(r => r.GetByTokenAsync("token", default))
        .ReturnsAsync(shared);

    var result = await this.sut.Accept("token");

    result.Should().BeOfType<ConflictObjectResult>();
  }

  [Fact]
  public async Task Accept_ShouldReturnOk_WhenAlreadyAcceptedBySameUser()
  {
    var userId = Guid.NewGuid();
    var ownerId = Guid.NewGuid();
    this.SetUser(userId);
    var shared = new SharedRecipe
    {
      Token = "token",
      RecipeId = Guid.NewGuid(),
      SharedByUserId = ownerId,
      UserId = userId,
      Recipe = new Recipe { Name = "Test", Slug = "test", Difficulty = "easy", UserId = ownerId },
    };
    this.sharedRepoMock
        .Setup(r => r.GetByTokenAsync("token", default))
        .ReturnsAsync(shared);

    var result = await this.sut.Accept("token");

    result.Should().BeOfType<OkObjectResult>();
  }

  [Fact]
  public async Task Accept_ShouldReturnConflict_WhenRecipeAlreadySharedWithUser()
  {
    var userId = Guid.NewGuid();
    var ownerId = Guid.NewGuid();
    this.SetUser(userId);
    var shared = new SharedRecipe
    {
      Token = "token",
      RecipeId = Guid.NewGuid(),
      SharedByUserId = ownerId,
      Recipe = new Recipe { Name = "Test", Slug = "test", Difficulty = "easy", UserId = ownerId },
    };
    this.sharedRepoMock
        .Setup(r => r.GetByTokenAsync("token", default))
        .ReturnsAsync(shared);
    this.sharedRepoMock
        .Setup(r => r.GetByRecipeAndUserAsync(shared.RecipeId, userId, default))
        .ReturnsAsync(new SharedRecipe { Token = "other", RecipeId = shared.RecipeId, SharedByUserId = ownerId, UserId = userId });

    var result = await this.sut.Accept("token");

    result.Should().BeOfType<ConflictObjectResult>();
  }

  [Fact]
  public async Task Accept_ShouldReturnOk_WhenValid()
  {
    var userId = Guid.NewGuid();
    var ownerId = Guid.NewGuid();
    this.SetUser(userId);
    var recipeId = Guid.NewGuid();
    var shared = new SharedRecipe
    {
      Token = "token",
      RecipeId = recipeId,
      SharedByUserId = ownerId,
      Recipe = new Recipe { Name = "Test", Slug = "test", Difficulty = "easy", UserId = ownerId },
    };
    this.sharedRepoMock
        .Setup(r => r.GetByTokenAsync("token", default))
        .ReturnsAsync(shared);
    this.sharedRepoMock
        .Setup(r => r.GetByRecipeAndUserAsync(recipeId, userId, default))
        .ReturnsAsync((SharedRecipe?)null);

    var result = await this.sut.Accept("token");

    result.Should().BeOfType<OkObjectResult>();
    this.sharedRepoMock.Verify(r => r.AcceptAsync(shared.Id, userId, default), Times.Once);
    this.sharedRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }

  // --- GetMySharedRecipes ---

  [Fact]
  public async Task GetMySharedRecipes_ShouldReturnUnauthorized_WhenNoUser()
  {
    var result = await this.sut.GetMySharedRecipes();

    result.Should().BeOfType<UnauthorizedResult>();
  }

  [Fact]
  public async Task GetMySharedRecipes_ShouldReturnOk_WhenAuthenticated()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    var shares = new List<SharedRecipe>
    {
      new SharedRecipe
      {
        Token = "t1",
        RecipeId = Guid.NewGuid(),
        SharedByUserId = Guid.NewGuid(),
        UserId = userId,
        Recipe = new Recipe { Name = "Paella", Slug = "paella", Difficulty = "medium", UserId = Guid.NewGuid() },
        SharedByUser = new User { Username = "chef", Email = "chef@test.com", PasswordHash = "hash" },
      },
    };
    this.sharedRepoMock
        .Setup(r => r.GetByUserIdAsync(userId, default))
        .ReturnsAsync(shares);

    var result = await this.sut.GetMySharedRecipes();

    result.Should().BeOfType<OkObjectResult>();
  }

  // --- GetByToken ---

  [Fact]
  public async Task GetByToken_ShouldReturnUnauthorized_WhenNoUser()
  {
    var result = await this.sut.GetByToken("token");

    result.Should().BeOfType<UnauthorizedResult>();
  }

  [Fact]
  public async Task GetByToken_ShouldReturnNotFound_WhenTokenNotFound()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    this.sharedRepoMock
        .Setup(r => r.GetByTokenAsync("invalid", default))
        .ReturnsAsync((SharedRecipe?)null);

    var result = await this.sut.GetByToken("invalid");

    result.Should().BeOfType<NotFoundObjectResult>();
  }

  [Fact]
  public async Task GetByToken_ShouldReturnForbid_WhenClaimedByOtherUser()
  {
    var userId = Guid.NewGuid();
    var otherUserId = Guid.NewGuid();
    var ownerId = Guid.NewGuid();
    this.SetUser(userId);
    var shared = new SharedRecipe
    {
      Token = "token",
      RecipeId = Guid.NewGuid(),
      SharedByUserId = ownerId,
      UserId = otherUserId,
      Recipe = new Recipe { Name = "Test", Slug = "test", Difficulty = "easy", UserId = ownerId },
    };
    this.sharedRepoMock
        .Setup(r => r.GetByTokenAsync("token", default))
        .ReturnsAsync(shared);

    var result = await this.sut.GetByToken("token");

    result.Should().BeOfType<ForbidResult>();
  }

  [Fact]
  public async Task GetByToken_ShouldReturnOk_WhenUserIsRecipient()
  {
    var userId = Guid.NewGuid();
    var ownerId = Guid.NewGuid();
    this.SetUser(userId);
    var recipe = new Recipe { Name = "Test", Slug = "test", Difficulty = "easy", UserId = ownerId };
    var shared = new SharedRecipe
    {
      Token = "token",
      RecipeId = recipe.Id,
      SharedByUserId = ownerId,
      UserId = userId,
      Recipe = recipe,
    };
    this.sharedRepoMock
        .Setup(r => r.GetByTokenAsync("token", default))
        .ReturnsAsync(shared);

    var result = await this.sut.GetByToken("token");

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    ok.Value.Should().Be(recipe);
  }

  [Fact]
  public async Task GetByToken_ShouldReturnOk_WhenUserIsOwner()
  {
    var ownerId = Guid.NewGuid();
    this.SetUser(ownerId);
    var recipe = new Recipe { Name = "Test", Slug = "test", Difficulty = "easy", UserId = ownerId };
    var shared = new SharedRecipe
    {
      Token = "token",
      RecipeId = recipe.Id,
      SharedByUserId = ownerId,
      UserId = Guid.NewGuid(),
      Recipe = recipe,
    };
    this.sharedRepoMock
        .Setup(r => r.GetByTokenAsync("token", default))
        .ReturnsAsync(shared);

    var result = await this.sut.GetByToken("token");

    result.Should().BeOfType<OkObjectResult>();
  }

  // --- Delete ---

  [Fact]
  public async Task Delete_ShouldReturnUnauthorized_WhenNoUser()
  {
    var result = await this.sut.Delete(Guid.NewGuid());

    result.Should().BeOfType<UnauthorizedResult>();
  }

  [Fact]
  public async Task Delete_ShouldReturnNotFound_WhenNotFound()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    this.sharedRepoMock
        .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
        .ReturnsAsync((SharedRecipe?)null);

    var result = await this.sut.Delete(Guid.NewGuid());

    result.Should().BeOfType<NotFoundObjectResult>();
  }

  [Fact]
  public async Task Delete_ShouldReturnForbid_WhenNotOwnerOrRecipient()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    var shared = new SharedRecipe
    {
      Token = "token",
      RecipeId = Guid.NewGuid(),
      SharedByUserId = Guid.NewGuid(),
      UserId = Guid.NewGuid(),
    };
    this.sharedRepoMock
        .Setup(r => r.GetByIdAsync(shared.Id, default))
        .ReturnsAsync(shared);

    var result = await this.sut.Delete(shared.Id);

    result.Should().BeOfType<ForbidResult>();
  }

  [Fact]
  public async Task Delete_ShouldReturnNoContent_WhenOwnerDeletes()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    var shared = new SharedRecipe
    {
      Token = "token",
      RecipeId = Guid.NewGuid(),
      SharedByUserId = userId,
      UserId = Guid.NewGuid(),
    };
    this.sharedRepoMock
        .Setup(r => r.GetByIdAsync(shared.Id, default))
        .ReturnsAsync(shared);

    var result = await this.sut.Delete(shared.Id);

    result.Should().BeOfType<NoContentResult>();
    this.sharedRepoMock.Verify(r => r.DeleteAsync(shared, userId.ToString(), default), Times.Once);
    this.sharedRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }

  [Fact]
  public async Task Delete_ShouldReturnNoContent_WhenRecipientDeletes()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    var shared = new SharedRecipe
    {
      Token = "token",
      RecipeId = Guid.NewGuid(),
      SharedByUserId = Guid.NewGuid(),
      UserId = userId,
    };
    this.sharedRepoMock
        .Setup(r => r.GetByIdAsync(shared.Id, default))
        .ReturnsAsync(shared);

    var result = await this.sut.Delete(shared.Id);

    result.Should().BeOfType<NoContentResult>();
    this.sharedRepoMock.Verify(r => r.DeleteAsync(shared, userId.ToString(), default), Times.Once);
  }
}
