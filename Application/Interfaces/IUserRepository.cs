// <copyright file="IUserRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Entities;

namespace Application.Interfaces;

/// <summary>
/// Repository interface for User-specific data access operations.
/// </summary>
public interface IUserRepository
{
  Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default);

  Task<User?> GetWithRefreshTokensAsync(string usernameOrEmail, CancellationToken cancellationToken = default);

  Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

  Task<bool> ExistsByUsernameOrEmailAsync(string username, string email, CancellationToken cancellationToken = default);

  Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

  Task AddAsync(User user, CancellationToken cancellationToken = default);

  Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

  Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
