using Domain.Common;

namespace Domain.Entities;

public class Role : BaseEntity
{
  public bool Admin { get; set; }

  public bool Write { get; set; }

  public bool Read { get; set; }

  public ICollection<User> Users { get; set; } = new List<User>();
}
