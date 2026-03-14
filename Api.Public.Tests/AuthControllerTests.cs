// <copyright file="AuthControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

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

public class AuthControllerTests
{
  private readonly Mock<IUserService> userServiceMock;
  private readonly AuthController sut;

  public AuthControllerTests()
  {
    this.userServiceMock = new Mock<IUserService>();
    this.sut = new AuthController(this.userServiceMock.Object);
    this.sut.ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext(),
    };
  }

  [Fact]
  public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
  {
    var request = new AuthenticationRequest("testuser", "Password123!");
    var response = new AuthenticationResponse("access-token", "refresh-token", "Bearer", 3600);
    this.userServiceMock
        .Setup(s => s.AuthenticateAsync(It.IsAny<AuthenticationRequest>(), default))
        .ReturnsAsync(response);

    var result = await this.sut.Login(request);

    var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
    okResult.Value.Should().Be(response);
  }

  [Fact]
  public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
  {
    var request = new AuthenticationRequest("testuser", "wrongpass");
    this.userServiceMock
        .Setup(s => s.AuthenticateAsync(It.IsAny<AuthenticationRequest>(), default))
        .ReturnsAsync((AuthenticationResponse?)null);

    var result = await this.sut.Login(request);

    result.Should().BeOfType<UnauthorizedResult>();
  }

  [Fact]
  public async Task Register_ShouldReturnCreated_WhenDataIsValid()
  {
    var dto = new CreateUserDto("newuser", "new@example.com", "Password123!");
    var user = new User
    {
      Id = Guid.NewGuid(),
      Username = "newuser",
      Email = "new@example.com",
      PasswordHash = "hash",
    };
    this.userServiceMock
        .Setup(s => s.RegisterAsync(dto, default))
        .ReturnsAsync(user);

    var result = await this.sut.Register(dto);

    var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
    createdResult.Location.Should().Contain(user.Id.ToString());
  }

  [Fact]
  public async Task Register_ShouldReturnConflict_WhenUserAlreadyExists()
  {
    var dto = new CreateUserDto("existing", "existing@example.com", "Password123!");
    this.userServiceMock
        .Setup(s => s.RegisterAsync(dto, default))
        .ThrowsAsync(new InvalidOperationException("Username or email already exists"));

    var result = await this.sut.Register(dto);

    result.Should().BeOfType<ConflictObjectResult>();
  }

  [Fact]
  public async Task Refresh_ShouldReturnOk_WhenTokenIsValid()
  {
    var request = new RefreshTokenRequest("valid-refresh-token");
    var response = new AuthenticationResponse("new-access", "new-refresh", "Bearer", 3600);
    this.userServiceMock
        .Setup(s => s.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>(), default))
        .ReturnsAsync(response);

    var result = await this.sut.Refresh(request);

    var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
    okResult.Value.Should().Be(response);
  }

  [Fact]
  public async Task Refresh_ShouldReturnUnauthorized_WhenTokenIsInvalid()
  {
    var request = new RefreshTokenRequest("bad-token");
    this.userServiceMock
        .Setup(s => s.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>(), default))
        .ReturnsAsync((AuthenticationResponse?)null);

    var result = await this.sut.Refresh(request);

    result.Should().BeOfType<UnauthorizedResult>();
  }

  [Fact]
  public async Task Revoke_ShouldReturnOk_WhenTokenRevoked()
  {
    var revokeRequest = new RevokeTokenDto("active-token");
    this.userServiceMock
        .Setup(s => s.RevokeTokenAsync("active-token", It.IsAny<string?>(), default))
        .ReturnsAsync(true);

    var result = await this.sut.Revoke(revokeRequest);

    result.Should().BeOfType<OkObjectResult>();
  }

  [Fact]
  public async Task Revoke_ShouldReturnNotFound_WhenTokenNotFound()
  {
    var revokeRequest = new RevokeTokenDto("nonexistent-token");
    this.userServiceMock
        .Setup(s => s.RevokeTokenAsync("nonexistent-token", It.IsAny<string?>(), default))
        .ReturnsAsync(false);

    var result = await this.sut.Revoke(revokeRequest);

    result.Should().BeOfType<NotFoundObjectResult>();
  }
}
