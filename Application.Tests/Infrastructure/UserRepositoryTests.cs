// <copyright file="UserRepositoryTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Infrastructure;

public class UserRepositoryTests
{
  private readonly DbContextOptions<AppDbContext> dbContextOptions;

  public UserRepositoryTests()
  {
    this.dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
  }

  [Fact]
  public async Task AddAsync_ShouldAddUserToDatabase()
  {
    using var context = new AppDbContext(this.dbContextOptions);
    var repository = new UserRepository(context);
    var user = new User
    {
      Username = "testuser",
      Email = "test@example.com",
      PasswordHash = "hash",
    };

    await repository.AddAsync(user);
    await repository.SaveChangesAsync();

    var saved = await context.Users.FirstOrDefaultAsync();
    saved.Should().NotBeNull();
    saved!.Username.Should().Be("testuser");
  }

  [Fact]
  public async Task GetByUsernameOrEmailAsync_ShouldFindByUsername()
  {
    using var context = new AppDbContext(this.dbContextOptions);
    var repository = new UserRepository(context);
    var user = new User
    {
      Username = "findme",
      Email = "find@example.com",
      PasswordHash = "hash",
    };
    context.Users.Add(user);
    await context.SaveChangesAsync();

    var result = await repository.GetByUsernameOrEmailAsync("findme");

    result.Should().NotBeNull();
    result!.Username.Should().Be("findme");
  }

  [Fact]
  public async Task GetByUsernameOrEmailAsync_ShouldFindByEmail()
  {
    using var context = new AppDbContext(this.dbContextOptions);
    var repository = new UserRepository(context);
    var user = new User
    {
      Username = "emailuser",
      Email = "email@example.com",
      PasswordHash = "hash",
    };
    context.Users.Add(user);
    await context.SaveChangesAsync();

    var result = await repository.GetByUsernameOrEmailAsync("email@example.com");

    result.Should().NotBeNull();
    result!.Email.Should().Be("email@example.com");
  }

  [Fact]
  public async Task GetByUsernameOrEmailAsync_ShouldNotFindDeletedUser()
  {
    using var context = new AppDbContext(this.dbContextOptions);
    var repository = new UserRepository(context);
    var user = new User
    {
      Username = "deleted",
      Email = "deleted@example.com",
      PasswordHash = "hash",
      IsDeleted = true,
    };
    context.Users.Add(user);
    await context.SaveChangesAsync();

    var result = await repository.GetByUsernameOrEmailAsync("deleted");

    result.Should().BeNull();
  }

  [Fact]
  public async Task GetWithRefreshTokensAsync_ShouldIncludeTokens()
  {
    using var context = new AppDbContext(this.dbContextOptions);
    var repository = new UserRepository(context);
    var user = new User
    {
      Username = "tokenuser",
      Email = "token@example.com",
      PasswordHash = "hash",
    };
    context.Users.Add(user);
    await context.SaveChangesAsync();

    var refreshToken = new RefreshToken
    {
      Token = "test-refresh-token",
      ExpiresAt = DateTime.UtcNow.AddDays(7),
      UserId = user.Id,
    };
    context.Set<RefreshToken>().Add(refreshToken);
    await context.SaveChangesAsync();

    var result = await repository.GetWithRefreshTokensAsync("tokenuser");

    result.Should().NotBeNull();
    result!.RefreshTokens.Should().HaveCount(1);
    result.RefreshTokens.First().Token.Should().Be("test-refresh-token");
  }

  [Fact]
  public async Task GetByRefreshTokenAsync_ShouldFindUserByToken()
  {
    using var context = new AppDbContext(this.dbContextOptions);
    var repository = new UserRepository(context);
    var user = new User
    {
      Username = "refreshuser",
      Email = "refresh@example.com",
      PasswordHash = "hash",
    };
    context.Users.Add(user);
    await context.SaveChangesAsync();

    var refreshToken = new RefreshToken
    {
      Token = "unique-refresh-token",
      ExpiresAt = DateTime.UtcNow.AddDays(7),
      UserId = user.Id,
    };
    context.Set<RefreshToken>().Add(refreshToken);
    await context.SaveChangesAsync();

    var result = await repository.GetByRefreshTokenAsync("unique-refresh-token");

    result.Should().NotBeNull();
    result!.Username.Should().Be("refreshuser");
  }

  [Fact]
  public async Task GetByRefreshTokenAsync_ShouldReturnNull_WhenTokenNotFound()
  {
    using var context = new AppDbContext(this.dbContextOptions);
    var repository = new UserRepository(context);

    var result = await repository.GetByRefreshTokenAsync("nonexistent-token");

    result.Should().BeNull();
  }

  [Fact]
  public async Task ExistsByUsernameOrEmailAsync_ShouldReturnTrue_WhenUsernameExists()
  {
    using var context = new AppDbContext(this.dbContextOptions);
    var repository = new UserRepository(context);
    var user = new User
    {
      Username = "existing",
      Email = "existing@example.com",
      PasswordHash = "hash",
    };
    context.Users.Add(user);
    await context.SaveChangesAsync();

    var result = await repository.ExistsByUsernameOrEmailAsync("existing", "other@example.com");

    result.Should().BeTrue();
  }

  [Fact]
  public async Task ExistsByUsernameOrEmailAsync_ShouldReturnFalse_WhenNeitherExists()
  {
    using var context = new AppDbContext(this.dbContextOptions);
    var repository = new UserRepository(context);

    var result = await repository.ExistsByUsernameOrEmailAsync("nonexistent", "nope@example.com");

    result.Should().BeFalse();
  }

  [Fact]
  public async Task GetByIdAsync_ShouldReturnUser()
  {
    using var context = new AppDbContext(this.dbContextOptions);
    var repository = new UserRepository(context);
    var user = new User
    {
      Username = "byid",
      Email = "byid@example.com",
      PasswordHash = "hash",
    };
    context.Users.Add(user);
    await context.SaveChangesAsync();

    var result = await repository.GetByIdAsync(user.Id);

    result.Should().NotBeNull();
    result!.Username.Should().Be("byid");
  }

  [Fact]
  public async Task GetByIdAsync_ShouldReturnNull_WhenDeleted()
  {
    using var context = new AppDbContext(this.dbContextOptions);
    var repository = new UserRepository(context);
    var user = new User
    {
      Username = "softdeleted",
      Email = "softdeleted@example.com",
      PasswordHash = "hash",
      IsDeleted = true,
    };
    context.Users.Add(user);
    await context.SaveChangesAsync();

    var result = await repository.GetByIdAsync(user.Id);

    result.Should().BeNull();
  }
}
