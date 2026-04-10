// <copyright file="ProfileControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Security.Claims;
using Api.Public.Controllers;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Api.Public.Tests;

public class ProfileControllerTests
{
  private readonly Mock<IUserService> userServiceMock;
  private readonly ProfileController sut;

  public ProfileControllerTests()
  {
    this.userServiceMock = new Mock<IUserService>();
    this.sut = new ProfileController(this.userServiceMock.Object);
    this.sut.ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext(),
    };
  }

  // --- GetProfile ---
  [Fact]
  public async Task GetProfile_ShouldReturnUnauthorized_WhenNoUser()
  {
    var result = await this.sut.GetProfile();

    result.Should().BeOfType<UnauthorizedResult>();
  }

  [Fact]
  public async Task GetProfile_ShouldReturnNotFound_WhenUserNotFound()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    this.userServiceMock.Setup(s => s.GetByIdAsync(userId, default)).ReturnsAsync((User?)null);

    var result = await this.sut.GetProfile();

    result.Should().BeOfType<NotFoundObjectResult>();
  }

  [Fact]
  public async Task GetProfile_ShouldReturnOk_WhenAuthenticated()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    var user = new User { Username = "test", Email = "test@example.com", PasswordHash = "hash", RoleId = Guid.NewGuid() };
    this.userServiceMock.Setup(s => s.GetByIdAsync(userId, default)).ReturnsAsync(user);

    var result = await this.sut.GetProfile();

    result.Should().BeOfType<OkObjectResult>();
  }

  // --- ChangePassword ---
  [Fact]
  public async Task ChangePassword_ShouldReturnUnauthorized_WhenNoUser()
  {
    var dto = new ChangePasswordDto("old", "new");

    var result = await this.sut.ChangePassword(dto);

    result.Should().BeOfType<UnauthorizedResult>();
  }

  [Fact]
  public async Task ChangePassword_ShouldReturnBadRequest_WhenWrongPassword()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    this.userServiceMock.Setup(s => s.ChangePasswordAsync(userId, "wrong", "new", default)).ReturnsAsync(false);

    var result = await this.sut.ChangePassword(new ChangePasswordDto("wrong", "new"));

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task ChangePassword_ShouldReturnOk_WhenValid()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    this.userServiceMock.Setup(s => s.ChangePasswordAsync(userId, "old", "new", default)).ReturnsAsync(true);

    var result = await this.sut.ChangePassword(new ChangePasswordDto("old", "new"));

    result.Should().BeOfType<OkObjectResult>();
  }

  // --- ChangeEmail ---
  [Fact]
  public async Task ChangeEmail_ShouldReturnUnauthorized_WhenNoUser()
  {
    var result = await this.sut.ChangeEmail(new ChangeEmailDto("new@example.com"));

    result.Should().BeOfType<UnauthorizedResult>();
  }

  [Fact]
  public async Task ChangeEmail_ShouldReturnNotFound_WhenUserNotFound()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    this.userServiceMock.Setup(s => s.ChangeEmailAsync(userId, "new@example.com", default)).ReturnsAsync(false);

    var result = await this.sut.ChangeEmail(new ChangeEmailDto("new@example.com"));

    result.Should().BeOfType<NotFoundObjectResult>();
  }

  [Fact]
  public async Task ChangeEmail_ShouldReturnConflict_WhenEmailTaken()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    this.userServiceMock
        .Setup(s => s.ChangeEmailAsync(userId, "taken@example.com", default))
        .ThrowsAsync(new InvalidOperationException("Email already in use"));

    var result = await this.sut.ChangeEmail(new ChangeEmailDto("taken@example.com"));

    result.Should().BeOfType<ConflictObjectResult>();
  }

  [Fact]
  public async Task ChangeEmail_ShouldReturnOk_WhenValid()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    this.userServiceMock.Setup(s => s.ChangeEmailAsync(userId, "new@example.com", default)).ReturnsAsync(true);

    var result = await this.sut.ChangeEmail(new ChangeEmailDto("new@example.com"));

    result.Should().BeOfType<OkObjectResult>();
  }

  // --- DeleteAccount ---
  [Fact]
  public async Task DeleteAccount_ShouldReturnUnauthorized_WhenNoUser()
  {
    var result = await this.sut.DeleteAccount(new DeleteAccountDto("pass"));

    result.Should().BeOfType<UnauthorizedResult>();
  }

  [Fact]
  public async Task DeleteAccount_ShouldReturnBadRequest_WhenWrongPassword()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    this.userServiceMock.Setup(s => s.DeleteAccountAsync(userId, "wrong", default)).ReturnsAsync(false);

    var result = await this.sut.DeleteAccount(new DeleteAccountDto("wrong"));

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task DeleteAccount_ShouldReturnNoContent_WhenValid()
  {
    var userId = Guid.NewGuid();
    this.SetUser(userId);
    this.userServiceMock.Setup(s => s.DeleteAccountAsync(userId, "pass", default)).ReturnsAsync(true);

    var result = await this.sut.DeleteAccount(new DeleteAccountDto("pass"));

    result.Should().BeOfType<NoContentResult>();
  }

  private void SetUser(Guid userId)
  {
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
    this.sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
  }
}
