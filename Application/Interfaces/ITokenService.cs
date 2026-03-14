// <copyright file="ITokenService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Interfaces;

public interface ITokenService
{
  string CreateToken(string userId, string username, IEnumerable<string>? roles = null);
}
