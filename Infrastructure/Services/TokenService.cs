// <copyright file="TokenService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

public class TokenService : ITokenService
{
  private readonly IConfiguration configuration;

  public TokenService(IConfiguration configuration)
  {
    this.configuration = configuration;
  }

  public string CreateToken(string userId, string username, IEnumerable<string>? roles = null)
  {
    var secret = this.configuration["JWT_SECRET"] ?? Environment.GetEnvironmentVariable("JWT_SECRET")
        ?? throw new InvalidOperationException("JWT_SECRET is not configured.");
    var expiryMinutesString = this.configuration["JWT_EXPIRY_MINUTES"] ?? Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "1440";
    var expiryMinutes = int.TryParse(expiryMinutesString, out var m) ? m : 1440;

    var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim("username", username),
        };

    if (roles != null)
    {
      foreach (var role in roles)
      {
        claims.Add(new Claim(ClaimTypes.Role, role));
      }
    }

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: "la-cullera-api",
        audience: "la-cullera-client",
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
        signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}
