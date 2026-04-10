using Domain.Entities;

namespace Application.Interfaces;

public interface IUnitRepository
{
  Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

  Task<IEnumerable<Unit>> GetAllAsync(CancellationToken cancellationToken = default);

  Task<IEnumerable<Unit>> GetByTypeAsync(string type, CancellationToken cancellationToken = default);

  Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

  Task<decimal?> GetConversionFactorAsync(Guid fromUnitId, Guid toUnitId, CancellationToken cancellationToken = default);

  Task AddAsync(Unit unit, CancellationToken cancellationToken = default);

  Task UpdateAsync(Unit unit, CancellationToken cancellationToken = default);

  Task DeleteAsync(Unit unit, string? deletedBy = null, CancellationToken cancellationToken = default);

  Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
