// <copyright file="RefreshToken.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Represents a refresh token for JWT authentication.
/// </summary>
public class RefreshToken : BaseEntity
{
  /// <summary>
  /// Gets or sets the token value (should be cryptographically random).
  /// </summary>
  public string Token { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the expiration date/time of this token.
  /// </summary>
  public DateTime ExpiresAt { get; set; }

  /// <summary>
  /// Gets or sets the IP address from which this token was created.
  /// </summary>
  public string? CreatedByIp { get; set; }

  /// <summary>
  /// Gets or sets the date/time when this token was revoked (if applicable).
  /// </summary>
  public DateTime? RevokedAt { get; set; }

  /// <summary>
  /// Gets or sets the IP address from which this token was revoked.
  /// </summary>
  public string? RevokedByIp { get; set; }

  /// <summary>
  /// Gets or sets the token that replaced this one (when refreshed).
  /// </summary>
  public string? ReplacedByToken { get; set; }

  /// <summary>
  /// Gets or sets the user ID that owns this refresh token.
  /// </summary>
  public Guid UserId { get; set; }

  /// <summary>
  /// Gets or sets the user that owns this refresh token.
  /// </summary>
  public User User { get; set; } = null!;

  /// <summary>
  /// Gets a value indicating whether this token is expired.
  /// </summary>
  public bool IsExpired => DateTime.UtcNow >= this.ExpiresAt;

  /// <summary>
  /// Gets a value indicating whether this token has been revoked.
  /// </summary>
  public bool IsRevoked => this.RevokedAt != null;

  /// <summary>
  /// Gets a value indicating whether this token is active (not expired and not revoked).
  /// </summary>
  public bool IsActive => !this.IsRevoked && !this.IsExpired;
}
