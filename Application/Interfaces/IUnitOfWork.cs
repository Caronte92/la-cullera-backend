// <copyright file="IUnitOfWork.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Interfaces;

/// <summary>
/// Interface for Unit of Work pattern to manage transactions.
/// </summary>
public interface IUnitOfWork : IDisposable
{
  /// <summary>
  /// Saves all changes made in this context to the database.
  /// </summary>
  /// <returns>The number of state entries written to the database.</returns>
  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Begins a new database transaction.
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task BeginTransactionAsync();

  /// <summary>
  /// Commits the current database transaction.
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task CommitAsync();

  /// <summary>
  /// Rolls back the current database transaction.
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task RollbackAsync();
}
