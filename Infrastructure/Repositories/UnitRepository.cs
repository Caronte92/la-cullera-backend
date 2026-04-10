using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UnitRepository : IUnitRepository
{
  private readonly AppDbContext context;

  public UnitRepository(AppDbContext context)
  {
    this.context = context;
  }

  public async Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    return await this.context.Units
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
  }

  public async Task<IEnumerable<Unit>> GetAllAsync(CancellationToken cancellationToken = default)
  {
    return await this.context.Units
        .AsNoTracking()
        .Where(u => !u.IsDeleted)
        .OrderBy(u => u.Type)
        .ThenBy(u => u.ToBaseFactor)
        .ToListAsync(cancellationToken);
  }

  public async Task<IEnumerable<Unit>> GetByTypeAsync(string type, CancellationToken cancellationToken = default)
  {
    return await this.context.Units
        .AsNoTracking()
        .Where(u => u.Type == type && !u.IsDeleted)
        .OrderBy(u => u.ToBaseFactor)
        .ToListAsync(cancellationToken);
  }

  public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
  {
    return await this.context.Units
        .AnyAsync(u => u.Name == name && !u.IsDeleted, cancellationToken);
  }

  public async Task<decimal?> GetConversionFactorAsync(Guid fromUnitId, Guid toUnitId, CancellationToken cancellationToken = default)
  {
    var units = await this.context.Units
        .AsNoTracking()
        .Where(u => (u.Id == fromUnitId || u.Id == toUnitId) && !u.IsDeleted)
        .ToListAsync(cancellationToken);

    if (units.Count != 2)
    {
      return null;
    }

    var from = units.First(u => u.Id == fromUnitId);
    var to = units.First(u => u.Id == toUnitId);

    if (from.Type != to.Type)
    {
      return null;
    }

    return from.ToBaseFactor / to.ToBaseFactor;
  }

  public async Task AddAsync(Unit unit, CancellationToken cancellationToken = default)
  {
    await this.context.Units.AddAsync(unit, cancellationToken);
  }

  public async Task UpdateAsync(Unit unit, CancellationToken cancellationToken = default)
  {
    unit.UpdatedAt = DateTime.UtcNow;
    this.context.Units.Update(unit);
    await Task.CompletedTask;
  }

  public async Task DeleteAsync(Unit unit, string? deletedBy = null, CancellationToken cancellationToken = default)
  {
    unit.IsDeleted = true;
    unit.DeletedAt = DateTime.UtcNow;
    unit.DeletedBy = deletedBy;
    this.context.Units.Update(unit);
    await Task.CompletedTask;
  }

  public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    await this.context.SaveChangesAsync(cancellationToken);
  }
}
