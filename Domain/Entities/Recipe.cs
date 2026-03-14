using Domain.Common;

namespace Domain.Entities;

public class Recipe : BaseEntity
{
  public Guid UserId { get; set; }

  public required string Name { get; set; }

  public required string Slug { get; set; }

  public string? ImageUrl { get; set; }

  public string? VideoUrl { get; set; }

  public int ServingBase { get; set; }

  public int TimeCook { get; set; }

  public required string Difficulty { get; set; }

  public User User { get; set; } = null!;

  public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

  public ICollection<Step> Steps { get; set; } = new List<Step>();

  public ICollection<RecipeTag> RecipeTags { get; set; } = new List<RecipeTag>();
}
