using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Infrastructure;

public class UnitRepositoryTests
{
  private DbContextOptions<AppDbContext> CreateOptions()
  {
    return new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
  }

  private static Unit CreateUnit(string name = "gramo", string abbreviation = "g", string type = "weight", decimal factor = 1m)
  {
    return new Unit
    {
      Name = name,
      Abbreviation = abbreviation,
      Type = type,
      ToBaseFactor = factor,
    };
  }

  [Fact]
  public async Task AddAsync_ShouldAddUnitToDatabase()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var repo = new UnitRepository(context);
    var unit = CreateUnit();

    await repo.AddAsync(unit);
    await repo.SaveChangesAsync();

    var saved = await context.Units.FirstOrDefaultAsync();
    saved.Should().NotBeNull();
    saved!.Name.Should().Be("gramo");
    saved.Abbreviation.Should().Be("g");
  }

  [Fact]
  public async Task GetByIdAsync_ShouldReturnUnit()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var unit = CreateUnit();
    context.Units.Add(unit);
    await context.SaveChangesAsync();

    var repo = new UnitRepository(context);
    var result = await repo.GetByIdAsync(unit.Id);

    result.Should().NotBeNull();
    result!.Name.Should().Be("gramo");
  }

  [Fact]
  public async Task GetByIdAsync_ShouldNotReturnDeletedUnit()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var unit = CreateUnit();
    unit.IsDeleted = true;
    context.Units.Add(unit);
    await context.SaveChangesAsync();

    var repo = new UnitRepository(context);
    var result = await repo.GetByIdAsync(unit.Id);

    result.Should().BeNull();
  }

  [Fact]
  public async Task GetAllAsync_ShouldReturnOrderedByTypeThenFactor()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    context.Units.Add(CreateUnit("litro", "L", "volume", 1000m));
    context.Units.Add(CreateUnit("mililitro", "ml", "volume", 1m));
    context.Units.Add(CreateUnit("kilogramo", "kg", "weight", 1000m));
    context.Units.Add(CreateUnit("gramo", "g", "weight", 1m));
    await context.SaveChangesAsync();

    var repo = new UnitRepository(context);
    var result = (await repo.GetAllAsync()).ToList();

    result.Should().HaveCount(4);
    result[0].Type.Should().Be("volume");
    result[0].Name.Should().Be("mililitro");
    result[1].Name.Should().Be("litro");
    result[2].Type.Should().Be("weight");
    result[2].Name.Should().Be("gramo");
    result[3].Name.Should().Be("kilogramo");
  }

  [Fact]
  public async Task GetByTypeAsync_ShouldFilterByType()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    context.Units.Add(CreateUnit("litro", "L", "volume", 1000m));
    context.Units.Add(CreateUnit("mililitro", "ml", "volume", 1m));
    context.Units.Add(CreateUnit("gramo", "g", "weight", 1m));
    await context.SaveChangesAsync();

    var repo = new UnitRepository(context);
    var result = (await repo.GetByTypeAsync("volume")).ToList();

    result.Should().HaveCount(2);
    result.Should().AllSatisfy(u => u.Type.Should().Be("volume"));
    result[0].ToBaseFactor.Should().BeLessThan(result[1].ToBaseFactor);
  }

  [Fact]
  public async Task ExistsByNameAsync_ShouldReturnTrueWhenExists()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    context.Units.Add(CreateUnit());
    await context.SaveChangesAsync();

    var repo = new UnitRepository(context);
    var result = await repo.ExistsByNameAsync("gramo");

    result.Should().BeTrue();
  }

  [Fact]
  public async Task ExistsByNameAsync_ShouldReturnFalseWhenNotExists()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);

    var repo = new UnitRepository(context);
    var result = await repo.ExistsByNameAsync("onza");

    result.Should().BeFalse();
  }

  [Fact]
  public async Task GetConversionFactorAsync_ShouldReturnCorrectFactor()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var gram = CreateUnit("gramo", "g", "weight", 1m);
    var kg = CreateUnit("kilogramo", "kg", "weight", 1000m);
    context.Units.AddRange(gram, kg);
    await context.SaveChangesAsync();

    var repo = new UnitRepository(context);

    // 1 kg = 1000 g → factor from kg to g = 1000/1 = 1000
    var factor = await repo.GetConversionFactorAsync(kg.Id, gram.Id);
    factor.Should().Be(1000m);

    // 1 g = 0.001 kg → factor from g to kg = 1/1000 = 0.001
    var reverseFactor = await repo.GetConversionFactorAsync(gram.Id, kg.Id);
    reverseFactor.Should().Be(0.001m);
  }

  [Fact]
  public async Task GetConversionFactorAsync_ShouldReturnNullForDifferentTypes()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var gram = CreateUnit("gramo", "g", "weight", 1m);
    var liter = CreateUnit("litro", "L", "volume", 1000m);
    context.Units.AddRange(gram, liter);
    await context.SaveChangesAsync();

    var repo = new UnitRepository(context);
    var factor = await repo.GetConversionFactorAsync(gram.Id, liter.Id);

    factor.Should().BeNull();
  }

  [Fact]
  public async Task GetConversionFactorAsync_ShouldReturnNullWhenUnitNotFound()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var gram = CreateUnit();
    context.Units.Add(gram);
    await context.SaveChangesAsync();

    var repo = new UnitRepository(context);
    var factor = await repo.GetConversionFactorAsync(gram.Id, Guid.NewGuid());

    factor.Should().BeNull();
  }

  [Fact]
  public async Task UpdateAsync_ShouldSetUpdatedAt()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var unit = CreateUnit();
    context.Units.Add(unit);
    await context.SaveChangesAsync();

    var repo = new UnitRepository(context);
    unit.Name = "gramo actualizado";
    await repo.UpdateAsync(unit);
    await repo.SaveChangesAsync();

    var updated = await context.Units.FirstAsync(u => u.Id == unit.Id);
    updated.Name.Should().Be("gramo actualizado");
    updated.UpdatedAt.Should().NotBeNull();
  }

  [Fact]
  public async Task DeleteAsync_ShouldSoftDeleteUnit()
  {
    var options = this.CreateOptions();
    using var context = new AppDbContext(options);
    var unit = CreateUnit();
    context.Units.Add(unit);
    await context.SaveChangesAsync();

    var repo = new UnitRepository(context);
    await repo.DeleteAsync(unit, "admin");
    await repo.SaveChangesAsync();

    var deleted = await context.Units.IgnoreQueryFilters().FirstAsync(u => u.Id == unit.Id);
    deleted.IsDeleted.Should().BeTrue();
    deleted.DeletedAt.Should().NotBeNull();
    deleted.DeletedBy.Should().Be("admin");
  }
}
