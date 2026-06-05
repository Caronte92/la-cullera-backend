// <copyright file="AuthIntegrationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Api.Public.Tests.Integration;

public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly CustomWebApplicationFactory<Program> factory;

  public AuthIntegrationTests(CustomWebApplicationFactory<Program> factory)
  {
    this.factory = factory;
  }

  [Fact]
  public async Task Register_ShouldReturnCreated_WhenDataIsValid()
  {
    var client = this.CreateClientWithIsolatedDb();
    var dto = new CreateUserDto("anyone", "anyone@test.com", "Password123!");

    var response = await client.PostAsJsonAsync("/auth/register", dto);

    response.StatusCode.Should().Be(HttpStatusCode.Created);
  }

  [Fact]
  public async Task Login_ShouldReturnTokens_WhenCredentialsAreValid()
  {
    // Arrange — seed user directly via service (registration endpoint is closed)
    var appFactory = this.factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureAppConfiguration((_, config) =>
      {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
          { "JWT_SECRET", "this-is-a-very-secure-secret-key-for-testing-purposes-only-32chars" },
        });
      });
    });

    using (var scope = appFactory.Services.CreateScope())
    {
      var userService = scope.ServiceProvider.GetRequiredService<Application.Interfaces.IUserService>();
      await userService.RegisterAsync(new CreateUserDto("loginuser_it", "login_it@test.com", "Password123!"));
    }

    var client = appFactory.CreateClient();
    var loginRequest = new AuthenticationRequest("loginuser_it", "Password123!");

    // Act
    var response = await client.PostAsJsonAsync("/auth/login", loginRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var authResponse = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
    authResponse.Should().NotBeNull();
    authResponse!.accessToken.Should().NotBeNullOrEmpty();
    authResponse.refreshToken.Should().NotBeNullOrEmpty();
  }

  [Fact]
  public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
  {
    // Arrange
    var client = this.CreateClientWithIsolatedDb();
    var loginRequest = new AuthenticationRequest("nonexistent", "wrongpass");

    // Act
    var response = await client.PostAsJsonAsync("/auth/login", loginRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  private HttpClient CreateClientWithIsolatedDb()
  {
    return this.factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureAppConfiguration((context, config) =>
        {
          config.AddInMemoryCollection(new Dictionary<string, string?>
          {
              { "JWT_SECRET", "this-is-a-very-secure-secret-key-for-testing-purposes-only-32chars" },
          });
        });
    }).CreateClient();
  }
}
