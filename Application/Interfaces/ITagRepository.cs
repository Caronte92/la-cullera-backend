using Domain.Entities;

namespace Application.Interfaces;

public interface ITagRepository
{
  Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

  Task<Tag?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

  Task<Tag?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

  Task<IEnumerable<Tag>> GetAllAsync(CancellationToken cancellationToken = default);

  Task<IEnumerable<Tag>> SearchByNameAsync(string name, CancellationToken cancellationToken = default);

  Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

  Task AddAsync(Tag tag, CancellationToken cancellationToken = default);

  Task UpdateAsync(Tag tag, CancellationToken cancellationToken = default);

  Task DeleteAsync(Tag tag, string? deletedBy = null, CancellationToken cancellationToken = default);

  Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
