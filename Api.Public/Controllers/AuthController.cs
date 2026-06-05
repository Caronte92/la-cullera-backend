// <copyright file="AuthController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Public.Controllers;

/// <summary>
/// Controller for authentication endpoints.
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
  private readonly IUserService userService;

  public AuthController(IUserService userService)
  {
    this.userService = userService;
  }

  [HttpPost("login")]
  [EnableRateLimiting("login")]
  public async Task<IActionResult> Login([FromBody] AuthenticationRequest request)
  {
#pragma warning disable SA1101
    var requestWithIp = request with { ipAddress = this.HttpContext.Connection.RemoteIpAddress?.ToString() };
#pragma warning restore SA1101
    var response = await this.userService.AuthenticateAsync(requestWithIp);

    if (response == null)
    {
      return this.Unauthorized();
    }

    return this.Ok(response);
  }

  [HttpPost("refresh")]
  public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
  {
#pragma warning disable SA1101
    var requestWithIp = request with { ipAddress = this.HttpContext.Connection.RemoteIpAddress?.ToString() };
#pragma warning restore SA1101
    var response = await this.userService.RefreshTokenAsync(requestWithIp);

    if (response == null)
    {
      return this.Unauthorized();
    }

    return this.Ok(response);
  }

  [HttpPost("revoke")]
  [Authorize]
  public async Task<IActionResult> Revoke([FromBody] RevokeTokenDto request)
  {
    var ipAddress = this.HttpContext.Connection.RemoteIpAddress?.ToString();
    var result = await this.userService.RevokeTokenAsync(request.token, ipAddress);

    if (!result)
    {
      return this.NotFound(new { message = "Token not found" });
    }

    return this.Ok(new { message = "Token revoked" });
  }

  [HttpPost("register")]
  public IActionResult Register()
  {
    // Registration is currently closed.
    // Uncomment the block below to re-enable it.
    //
    // try
    // {
    //   var user = await this.userService.RegisterAsync(dto);
    //   return this.Created($"/users/{user.Id}", new { user.Id, user.Username, user.Email });
    // }
    // catch (InvalidOperationException ex)
    // {
    //   return this.Conflict(new { message = ex.Message });
    // }
    return this.NotFound();
  }
}
