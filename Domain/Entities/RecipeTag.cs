using Domain.Common;

namespace Domain.Entities;

public class RecipeTag : BaseEntity
{
  public Guid RecipeId { get; set; }

  public Guid TagId { get; set; }

  public Recipe Recipe { get; set; } = null!;

  public Tag Tag { get; set; } = null!;
}
