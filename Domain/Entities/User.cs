// <copyright file="User.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Represents a user entity.
/// </summary>
public class User : BaseEntity
{
  /// <summary>
  /// Gets or sets the username.
  /// </summary>
  public required string Username { get; set; }

  /// <summary>
  /// Gets or sets the email address.
  /// </summary>
  public required string Email { get; set; }

  /// <summary>
  /// Gets or sets the hashed password.
  /// </summary>
  public required string PasswordHash { get; set; }

  public Guid RoleId { get; set; }

  /// <summary>
  /// Gets or sets the refresh tokens for this user.
  /// </summary>
  public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

  public Role Role { get; set; } = null!;

  public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();

  public ICollection<SharedRecipe> SharedRecipes { get; set; } = new List<SharedRecipe>();

  public ICollection<SharedRecipe> SharedByMeRecipes { get; set; } = new List<SharedRecipe>();
}
