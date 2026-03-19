// <copyright file="UserRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User-specific data access operations.
/// </summary>
public class UserRepository : IUserRepository
{
  private readonly AppDbContext context;

  public UserRepository(AppDbContext context)
  {
    this.context = context;
  }

  public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
  {
    return await this.context.Users
        .FirstOrDefaultAsync(
            u => (u.Username == usernameOrEmail || u.Email == usernameOrEmail) && !u.IsDeleted,
            cancellationToken);
  }

  public async Task<User?> GetWithRefreshTokensAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
  {
    return await this.context.Users
        .Include(u => u.RefreshTokens)
        .FirstOrDefaultAsync(
            u => (u.Username == usernameOrEmail || u.Email == usernameOrEmail) && !u.IsDeleted,
            cancellationToken);
  }

  public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
  {
    return await this.context.Users
        .Include(u => u.RefreshTokens)
        .FirstOrDefaultAsync(
            u => u.RefreshTokens.Any(rt => rt.Token == refreshToken),
            cancellationToken);
  }

  public async Task<bool> ExistsByUsernameOrEmailAsync(string username, string email, CancellationToken cancellationToken = default)
  {
    return await this.context.Users
        .AnyAsync(u => (u.Username == username || u.Email == email) && !u.IsDeleted, cancellationToken);
  }

  public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
  {
    return await this.context.Users
        .AnyAsync(u => u.Username == username && !u.IsDeleted, cancellationToken);
  }

  public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
  {
    return await this.context.Users
        .AnyAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);
  }

  public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    return await this.context.Users
        .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
  }

  public async Task AddAsync(User user, CancellationToken cancellationToken = default)
  {
    await this.context.Users.AddAsync(user, cancellationToken);
  }

  public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
  {
    await this.context.Set<RefreshToken>().AddAsync(refreshToken, cancellationToken);
  }

  public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
  {
    user.UpdatedAt = DateTime.UtcNow;
    this.context.Users.Update(user);
    return Task.CompletedTask;
  }

  public Task DeleteAsync(User user, CancellationToken cancellationToken = default)
  {
    user.IsDeleted = true;
    user.DeletedAt = DateTime.UtcNow;
    this.context.Users.Update(user);
    return Task.CompletedTask;
  }

  public async Task<bool> ExistsByEmailAsync(string email, Guid excludeUserId, CancellationToken cancellationToken = default)
  {
    return await this.context.Users
        .AnyAsync(u => u.Email == email && u.Id != excludeUserId && !u.IsDeleted, cancellationToken);
  }

  public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    await this.context.SaveChangesAsync(cancellationToken);
  }
}
