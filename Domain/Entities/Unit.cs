using Domain.Common;

namespace Domain.Entities;

public class Unit : BaseEntity
{
  public required string Name { get; set; }

  public required string Abbreviation { get; set; }

  public required string Type { get; set; }

  public decimal ToBaseFactor { get; set; }

  public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
}
