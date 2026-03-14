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

  /// <summary>
  /// Gets or sets the first name.
  /// </summary>
  public required string FirstName { get; set; }

  /// <summary>
  /// Gets or sets the last name.
  /// </summary>
  public required string LastName { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether the user is active.
  /// </summary>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Gets or sets the number of failed login attempts.
  /// </summary>
  public int AccessFailedCount { get; set; }

  /// <summary>
  /// Gets or sets the date/time when the account lockout ends.
  /// </summary>
  public DateTime? LockoutEnd { get; set; }

  /// <summary>
  /// Gets or sets the refresh tokens for this user.
  /// </summary>
  public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

  /// <summary>
  /// Gets the full name of the user.
  /// </summary>
  public string FullName => $"{this.FirstName} {this.LastName}";
}
