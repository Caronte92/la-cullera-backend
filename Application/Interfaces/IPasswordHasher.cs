// <copyright file="IPasswordHasher.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Interfaces;

/// <summary>
/// Service for securely hashing and verifying passwords.
/// </summary>
public interface IPasswordHasher
{
  /// <summary>
  /// Hashes a plain text password.
  /// </summary>
  /// <param name="password">The plain text password to hash.</param>
  /// <returns>The hashed password.</returns>
  string HashPassword(string password);

  /// <summary>
  /// Verifies a password against a hash.
  /// </summary>
  /// <param name="hash">The password hash from the database.</param>
  /// <param name="password">The plain text password to verify.</param>
  /// <returns>True if the password matches the hash; otherwise false.</returns>
  bool VerifyPassword(string hash, string password);
}
