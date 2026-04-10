using Domain.Common;

namespace Domain.Entities;

public class SharedRecipe : BaseEntity
{
  public Guid? UserId { get; set; }

  public Guid RecipeId { get; set; }

  public Guid SharedByUserId { get; set; }

  public required string Token { get; set; }

  public User? User { get; set; }

  public Recipe Recipe { get; set; } = null!;

  public User SharedByUser { get; set; } = null!;
}
