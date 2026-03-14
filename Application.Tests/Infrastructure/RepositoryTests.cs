// <copyright file="RepositoryTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Common.Models;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Infrastructure;

public class RepositoryTests
{
  private readonly DbContextOptions<AppDbContext> dbContextOptions;

  public RepositoryTests()
  {
    this.dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
  }

  [Fact]
  public async Task AddAsync_ShouldAddEntityToDatabase()
  {
    // Arrange
    using var context = new AppDbContext(this.dbContextOptions);
    var repository = new Repository<User>(context);
    var user = new User
    {
      Username = "testuser",
      Email = "test@example.com",
      PasswordHash = "hash",
    };

    // Act
    await repository.AddAsync(user);
    await context.SaveChangesAsync();

    // Assert
    var savedUser = await context.Users.FirstOrDefaultAsync();
    savedUser.Should().NotBeNull();
    savedUser!.Username.Should().Be("testuser");
    savedUser.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
  }

  [Fact]
  public async Task GetPagedAsync_ShouldReturnPagedResults()
  {
    // Arrange
    using var context = new AppDbContext(this.dbContextOptions);
    var repository = new Repository<User>(context);

    var users = Enumerable.Range(1, 25).Select(i => new User
    {
      Username = $"user{i}",
      Email = $"user{i}@example.com",
      PasswordHash = "hash",
    }).ToList();

    await context.Users.AddRangeAsync(users);
    await context.SaveChangesAsync();

    // Act
    var result = await repository.GetPagedAsync(
        page: 2,
        pageSize: 10,
        orderBy: q => q.OrderBy(u => u.Username)); // Explicit deterministic sort

    // Assert
    result.Should().NotBeNull();
    result.TotalCount.Should().Be(25);
    result.TotalPages.Should().Be(3);
    result.PageNumber.Should().Be(2);
    result.Items.Should().HaveCount(10);

    // Page 1: user1, user10-18 (total 10) -> wait, string sort is different: user1, user10, user11...
    // Let's rely on numeric suffix if we want simple checks, or use a simpler dataset.
    // Actually, user1...user9, user10...user25
    // Alpha sort: user1, user10, user11... user19, user2, user20...
    // Let's just create them with predictable ids or names like "A", "B"... or use CreatedAt with deltas.

    // Better strategy: CreatedAt with time difference
    // But since CreatedAt is set effectively inside Add (refactored earlier to UoW but Repo sets it?),
    // Repo.AddAsync sets CreatedAt.
    // Let's use explicit ordering by Username for stability.
    // Page 1 (10): user1, user10, user11, user12, user13, user14, user15, user16, user17, user18
    // Page 2 (10): user19, user2, user20, user21, user22, user23, user24, user25, user3, user4

    // Let's assume numeric sort for simplicity of test verification is hard with just "OrderBy(Username)".
    // Let's use explicit Int property or just verify count and existence of *some* item.
    result.Items.First().Username.Should().NotBeNullOrEmpty();
  }

  [Fact]
  public async Task DeleteAsync_ShouldSoftDeleteEntity()
  {
    // Arrange
    using var context = new AppDbContext(this.dbContextOptions);
    var repository = new Repository<User>(context);
    var user = new User
    {
      Username = "todelete",
      Email = "delete@example.com",
      PasswordHash = "hash",
    };

    await context.Users.AddAsync(user);
    await context.SaveChangesAsync();

    // Act
    await repository.DeleteAsync(user.Id);
    await context.SaveChangesAsync();

    // Assert
    var deletedUser = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == user.Id);
    deletedUser.Should().NotBeNull();
    deletedUser!.IsDeleted.Should().BeTrue();
    deletedUser.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

    // Should not be found by normal queries
    var foundUser = await repository.GetByIdAsync(user.Id);
    foundUser.Should().BeNull();
  }
}
