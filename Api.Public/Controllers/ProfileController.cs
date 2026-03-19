// <copyright file="ProfileController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Security.Claims;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Public.Controllers;

[ApiController]
[Route("profile")]
[Authorize]
public class ProfileController : ControllerBase
{
  private readonly IUserService userService;

  public ProfileController(IUserService userService)
  {
    this.userService = userService;
  }

  [HttpGet]
  public async Task<IActionResult> GetProfile()
  {
    var userId = this.GetUserId();
    if (userId == null)
    {
      return this.Unauthorized();
    }

    var user = await this.userService.GetByIdAsync(userId.Value);
    if (user == null)
    {
      return this.NotFound(new { message = "User not found" });
    }

    return this.Ok(new { user.Id, user.Username, user.Email, user.CreatedAt });
  }

  [HttpPut("password")]
  public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
  {
    var userId = this.GetUserId();
    if (userId == null)
    {
      return this.Unauthorized();
    }

    var result = await this.userService.ChangePasswordAsync(userId.Value, dto.currentPassword, dto.newPassword);
    if (!result)
    {
      return this.BadRequest(new { message = "Current password is incorrect" });
    }

    return this.Ok(new { message = "Password changed successfully" });
  }

  [HttpPut("email")]
  public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailDto dto)
  {
    var userId = this.GetUserId();
    if (userId == null)
    {
      return this.Unauthorized();
    }

    try
    {
      var result = await this.userService.ChangeEmailAsync(userId.Value, dto.newEmail);
      if (!result)
      {
        return this.NotFound(new { message = "User not found" });
      }

      return this.Ok(new { message = "Email changed successfully" });
    }
    catch (InvalidOperationException ex)
    {
      return this.Conflict(new { message = ex.Message });
    }
  }

  [HttpDelete]
  public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountDto dto)
  {
    var userId = this.GetUserId();
    if (userId == null)
    {
      return this.Unauthorized();
    }

    var result = await this.userService.DeleteAccountAsync(userId.Value, dto.password);
    if (!result)
    {
      return this.BadRequest(new { message = "Password is incorrect" });
    }

    return this.NoContent();
  }

  private Guid? GetUserId()
  {
    var claim = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return Guid.TryParse(claim, out var id) ? id : null;
  }
}
