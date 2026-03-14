using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TagRepository : ITagRepository
{
  private readonly AppDbContext context;

  public TagRepository(AppDbContext context)
  {
    this.context = context;
  }

  public async Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
  {
    return await this.context.Tags
        .AsNoTracking()
        .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
  }

  public async Task<Tag?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
  {
    return await this.context.Tags
        .AsNoTracking()
        .FirstOrDefaultAsync(t => t.Slug == slug && !t.IsDeleted, cancellationToken);
  }

  public async Task<IEnumerable<Tag>> GetAllAsync(CancellationToken cancellationToken = default)
  {
    return await this.context.Tags
        .AsNoTracking()
        .Where(t => !t.IsDeleted)
        .OrderBy(t => t.Name)
        .ToListAsync(cancellationToken);
  }

  public async Task<IEnumerable<Tag>> SearchByNameAsync(string name, CancellationToken cancellationToken = default)
  {
    return await this.context.Tags
        .AsNoTracking()
        .Where(t => !t.IsDeleted && EF.Functions.ILike(t.Name, $"%{name}%"))
        .OrderBy(t => t.Name)
        .ToListAsync(cancellationToken);
  }

  public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
  {
    return await this.context.Tags
        .AnyAsync(t => t.Name == name && !t.IsDeleted, cancellationToken);
  }

  public async Task AddAsync(Tag tag, CancellationToken cancellationToken = default)
  {
    await this.context.Tags.AddAsync(tag, cancellationToken);
  }

  public async Task UpdateAsync(Tag tag, CancellationToken cancellationToken = default)
  {
    tag.UpdatedAt = DateTime.UtcNow;
    this.context.Tags.Update(tag);
    await Task.CompletedTask;
  }

  public async Task DeleteAsync(Tag tag, string? deletedBy = null, CancellationToken cancellationToken = default)
  {
    tag.IsDeleted = true;
    tag.DeletedAt = DateTime.UtcNow;
    tag.DeletedBy = deletedBy;
    this.context.Tags.Update(tag);
    await Task.CompletedTask;
  }

  public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    await this.context.SaveChangesAsync(cancellationToken);
  }
}
