using Domain.Common;

namespace Domain.Entities;

public class Step : BaseEntity
{
  public Guid RecipeId { get; set; }

  public int Order { get; set; }

  public required string Description { get; set; }

  public Recipe Recipe { get; set; } = null!;
}
