// <copyright file="Repository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Linq.Expressions;
using Application.Interfaces;
using Domain.Common;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Implementación genérica del patrón Repository.
/// </summary>
/// <typeparam name="TEntity">Tipo de entidad.</typeparam>
public class Repository<TEntity> : IRepository<TEntity>
    where TEntity : BaseEntity
{
  private readonly AppDbContext context;
  private readonly DbSet<TEntity> dbSet;

  public Repository(AppDbContext context)
  {
    this.context = context ?? throw new ArgumentNullException(nameof(context));
    this.dbSet = context.Set<TEntity>();
  }

  public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    return await this.dbSet
        .AsNoTracking()
        .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
  }

  public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
  {
    return await this.dbSet
        .AsNoTracking()
        .Where(e => !e.IsDeleted)
        .ToListAsync(cancellationToken);
  }

  public async Task<Application.Common.Models.PagedResult<TEntity>> GetPagedAsync(
      int page,
      int pageSize,
      Expression<Func<TEntity, bool>>? predicate = null,
      Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
      CancellationToken cancellationToken = default)
  {
    var query = this.dbSet.AsNoTracking().Where(e => !e.IsDeleted);

    if (predicate != null)
    {
      query = query.Where(predicate);
    }

    var totalCount = await query.CountAsync(cancellationToken);

    if (orderBy != null)
    {
      query = orderBy(query);
    }
    else
    {
      query = query.OrderByDescending(e => e.CreatedAt);
    }

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    return new Application.Common.Models.PagedResult<TEntity>(items, totalCount, page, pageSize);
  }

  public async Task<IEnumerable<TEntity>> FindAsync(
      Expression<Func<TEntity, bool>> predicate,
      CancellationToken cancellationToken = default)
  {
    return await this.dbSet
        .AsNoTracking()
        .Where(e => !e.IsDeleted)
        .Where(predicate)
        .ToListAsync(cancellationToken);
  }

  public async Task<TEntity?> FirstOrDefaultAsync(
      Expression<Func<TEntity, bool>> predicate,
      CancellationToken cancellationToken = default)
  {
    return await this.dbSet
        .AsNoTracking()
        .Where(e => !e.IsDeleted)
        .FirstOrDefaultAsync(predicate, cancellationToken);
  }

  public async Task<bool> AnyAsync(
      Expression<Func<TEntity, bool>> predicate,
      CancellationToken cancellationToken = default)
  {
    return await this.dbSet
        .Where(e => !e.IsDeleted)
        .AnyAsync(predicate, cancellationToken);
  }

  public async Task<int> CountAsync(
      Expression<Func<TEntity, bool>>? predicate = null,
      CancellationToken cancellationToken = default)
  {
    var query = this.dbSet.Where(e => !e.IsDeleted);

    if (predicate != null)
    {
      query = query.Where(predicate);
    }

    return await query.CountAsync(cancellationToken);
  }

  public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(entity);

    entity.CreatedAt = DateTime.UtcNow;
    await this.dbSet.AddAsync(entity, cancellationToken);
  }

  public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(entities);

    var entityList = entities.ToList();
    foreach (var entity in entityList)
    {
      entity.CreatedAt = DateTime.UtcNow;
    }

    await this.dbSet.AddRangeAsync(entityList, cancellationToken);
  }

  public Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(entity);

    entity.UpdatedAt = DateTime.UtcNow;
    this.dbSet.Update(entity);
    return Task.CompletedTask;
  }

  public async Task DeleteAsync(Guid id, string? deletedBy = null, CancellationToken cancellationToken = default)
  {
    var entity = await this.dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    if (entity != null)
    {
      await this.DeleteAsync(entity, deletedBy, cancellationToken);
    }
  }

  public Task DeleteAsync(TEntity entity, string? deletedBy = null, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(entity);

    entity.IsDeleted = true;
    entity.DeletedAt = DateTime.UtcNow;
    entity.DeletedBy = deletedBy;

    this.dbSet.Update(entity);
    return Task.CompletedTask;
  }

  public async Task DeleteRangeAsync(IEnumerable<Guid> ids, string? deletedBy = null,
      CancellationToken cancellationToken = default)
  {
    var entities = await this.dbSet
        .Where(e => ids.Contains(e.Id) && !e.IsDeleted)
        .ToListAsync(cancellationToken);

    foreach (var entity in entities)
    {
      entity.IsDeleted = true;
      entity.DeletedAt = DateTime.UtcNow;
      entity.DeletedBy = deletedBy;
    }

    this.dbSet.UpdateRange(entities);
  }

  public async Task HardDeleteAsync(Guid id, CancellationToken cancellationToken = default)
  {
    var entity = await this.dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    if (entity != null)
    {
      this.dbSet.Remove(entity);
    }
  }

  public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    await this.context.SaveChangesAsync(cancellationToken);
  }
}
