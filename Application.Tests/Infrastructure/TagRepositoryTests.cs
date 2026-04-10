using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Infrastructure;

public class TagRepositoryTests
{
  [Fact]
  public async Task AddAsync_ShouldAddTagToDatabase()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var repo = new TagRepository(context);
    var tag = CreateTag();

    await repo.AddAsync(tag);
    await repo.SaveChangesAsync();

    var saved = await context.Tags.FirstOrDefaultAsync();
    saved.Should().NotBeNull();
    saved!.Name.Should().Be("Vegano");
  }

  [Fact]
  public async Task GetByIdAsync_ShouldReturnTag()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var tag = CreateTag();
    context.Tags.Add(tag);
    await context.SaveChangesAsync();

    var repo = new TagRepository(context);
    var result = await repo.GetByIdAsync(tag.Id);

    result.Should().NotBeNull();
    result!.Name.Should().Be("Vegano");
  }

  [Fact]
  public async Task GetByIdAsync_ShouldNotReturnDeletedTag()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var tag = CreateTag();
    tag.IsDeleted = true;
    context.Tags.Add(tag);
    await context.SaveChangesAsync();

    var repo = new TagRepository(context);
    var result = await repo.GetByIdAsync(tag.Id);

    result.Should().BeNull();
  }

  [Fact]
  public async Task GetBySlugAsync_ShouldReturnTag()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var tag = CreateTag();
    context.Tags.Add(tag);
    await context.SaveChangesAsync();

    var repo = new TagRepository(context);
    var result = await repo.GetBySlugAsync("vegano");

    result.Should().NotBeNull();
    result!.Slug.Should().Be("vegano");
  }

  [Fact]
  public async Task GetAllAsync_ShouldReturnAllActiveTagsOrdered()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    context.Tags.Add(CreateTag("Postre", "postre"));
    context.Tags.Add(CreateTag("Aperitivo", "aperitivo"));
    var deleted = CreateTag("Borrado", "borrado");
    deleted.IsDeleted = true;
    context.Tags.Add(deleted);
    await context.SaveChangesAsync();

    var repo = new TagRepository(context);
    var result = (await repo.GetAllAsync()).ToList();

    result.Should().HaveCount(2);
    result[0].Name.Should().Be("Aperitivo");
    result[1].Name.Should().Be("Postre");
  }

  [Fact]
  public async Task ExistsByNameAsync_ShouldReturnTrueWhenExists()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    context.Tags.Add(CreateTag());
    await context.SaveChangesAsync();

    var repo = new TagRepository(context);
    var result = await repo.ExistsByNameAsync("Vegano");

    result.Should().BeTrue();
  }

  [Fact]
  public async Task ExistsByNameAsync_ShouldReturnFalseForDeleted()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var tag = CreateTag();
    tag.IsDeleted = true;
    context.Tags.Add(tag);
    await context.SaveChangesAsync();

    var repo = new TagRepository(context);
    var result = await repo.ExistsByNameAsync("Vegano");

    result.Should().BeFalse();
  }

  [Fact]
  public async Task ExistsByNameAsync_ShouldReturnFalseWhenNotExists()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);

    var repo = new TagRepository(context);
    var result = await repo.ExistsByNameAsync("NoExiste");

    result.Should().BeFalse();
  }

  [Fact]
  public async Task UpdateAsync_ShouldSetUpdatedAt()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var tag = CreateTag();
    context.Tags.Add(tag);
    await context.SaveChangesAsync();

    var repo = new TagRepository(context);
    tag.Name = "Vegetariano";
    await repo.UpdateAsync(tag);
    await repo.SaveChangesAsync();

    var updated = await context.Tags.FirstAsync(t => t.Id == tag.Id);
    updated.Name.Should().Be("Vegetariano");
    updated.UpdatedAt.Should().NotBeNull();
  }

  [Fact]
  public async Task DeleteAsync_ShouldSoftDeleteTag()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var tag = CreateTag();
    context.Tags.Add(tag);
    await context.SaveChangesAsync();

    var repo = new TagRepository(context);
    await repo.DeleteAsync(tag, "admin");
    await repo.SaveChangesAsync();

    var deleted = await context.Tags.IgnoreQueryFilters().FirstAsync(t => t.Id == tag.Id);
    deleted.IsDeleted.Should().BeTrue();
    deleted.DeletedAt.Should().NotBeNull();
    deleted.DeletedBy.Should().Be("admin");
  }

  private static Tag CreateTag(string name = "Vegano", string slug = "vegano")
  {
    return new Tag { Name = name, Slug = slug };
  }

  private DbContextOptions<AppDbContext> CreateOptions()
  {
    return new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
  }
}
