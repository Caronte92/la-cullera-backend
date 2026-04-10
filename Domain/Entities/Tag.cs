using Domain.Common;

namespace Domain.Entities;

public class Tag : BaseEntity
{
  public required string Name { get; set; }

  public required string Slug { get; set; }

  public ICollection<RecipeTag> RecipeTags { get; set; } = new List<RecipeTag>();
}
