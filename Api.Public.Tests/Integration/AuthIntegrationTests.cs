// <copyright file="AuthIntegrationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
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
    // Arrange
    var client = this.CreateClientWithIsolatedDb();
    var uniqueId = Guid.NewGuid().ToString("N")[..8];

    var registerDto = new CreateUserDto(
        $"integration_reg_{uniqueId}",
        $"integration_reg_{uniqueId}@test.com",
        "Password123!",
        "Integration",
        "Test");

    // Act
    var response = await client.PostAsJsonAsync("/auth/register", registerDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var content = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
    content.GetProperty("username").GetString().Should().Be($"integration_reg_{uniqueId}");
  }

  [Fact]
  public async Task Login_ShouldReturnTokens_WhenCredentialsAreValid()
  {
    // Arrange
    var client = this.CreateClientWithIsolatedDb();
    var uniqueId = Guid.NewGuid().ToString("N")[..8];

    // Create user first
    var registerDto = new CreateUserDto(
        $"loginuser_{uniqueId}",
        $"login_{uniqueId}@test.com",
        "Password123!",
        "Login",
        "User");
    var registerResponse = await client.PostAsJsonAsync("/auth/register", registerDto);
    registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

    var loginRequest = new AuthenticationRequest($"loginuser_{uniqueId}", "Password123!");

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
