using Domain.Common;

namespace Domain.Entities;

public class Ingredient : BaseEntity
{
  public Guid RecipeId { get; set; }

  public required string Name { get; set; }

  public decimal Amount { get; set; }

  public Guid UnitId { get; set; }

  public int Order { get; set; }

  public Recipe Recipe { get; set; } = null!;

  public Unit Unit { get; set; } = null!;
}
