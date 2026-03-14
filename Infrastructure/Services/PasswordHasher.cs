// <copyright file="PasswordHasher.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Service for hashing and verifying passwords using BCrypt.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
  private const int WorkFactor = 12;

  /// <inheritdoc/>
  public string HashPassword(string password)
  {
    ArgumentNullException.ThrowIfNull(password);

    return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
  }

  /// <inheritdoc/>
  public bool VerifyPassword(string hash, string password)
  {
    ArgumentNullException.ThrowIfNull(hash);
    ArgumentNullException.ThrowIfNull(password);

    try
    {
      return BCrypt.Net.BCrypt.Verify(password, hash);
    }
    catch (BCrypt.Net.SaltParseException)
    {
      // Invalid hash format
      return false;
    }
  }
}
