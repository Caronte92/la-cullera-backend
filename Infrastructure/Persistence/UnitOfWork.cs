// <copyright file="UnitOfWork.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence;

/// <summary>
/// Implementation of Unit of Work pattern using AppDbContext.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
  private readonly AppDbContext context;
  private IDbContextTransaction? currentTransaction;

  public UnitOfWork(AppDbContext context)
  {
    this.context = context;
  }

  public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    return await this.context.SaveChangesAsync(cancellationToken);
  }

  public async Task BeginTransactionAsync()
  {
    if (this.currentTransaction != null)
    {
      return;
    }

    this.currentTransaction = await this.context.Database.BeginTransactionAsync();
  }

  public async Task CommitAsync()
  {
    try
    {
      await this.context.SaveChangesAsync();
      if (this.currentTransaction != null)
      {
        await this.currentTransaction.CommitAsync();
      }
    }
    catch
    {
      await this.RollbackAsync();
      throw;
    }
    finally
    {
      if (this.currentTransaction != null)
      {
        this.currentTransaction.Dispose();
        this.currentTransaction = null;
      }
    }
  }

  public async Task RollbackAsync()
  {
    try
    {
      if (this.currentTransaction != null)
      {
        await this.currentTransaction.RollbackAsync();
      }
    }
    finally
    {
      if (this.currentTransaction != null)
      {
        this.currentTransaction.Dispose();
        this.currentTransaction = null;
      }
    }
  }

  public void Dispose()
  {
    this.context.Dispose();
    GC.SuppressFinalize(this);
  }
}
