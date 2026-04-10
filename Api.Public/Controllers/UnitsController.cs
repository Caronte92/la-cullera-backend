// <copyright file="UnitsController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Public.Controllers;

[ApiController]
[Route("units")]
[Authorize]
public class UnitsController : ControllerBase
{
  private readonly IUnitRepository unitRepository;

  public UnitsController(IUnitRepository unitRepository)
  {
    this.unitRepository = unitRepository;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll()
  {
    var units = await this.unitRepository.GetAllAsync();
    return this.Ok(units);
  }

  [HttpGet("{id:guid}")]
  public async Task<IActionResult> GetById(Guid id)
  {
    var unit = await this.unitRepository.GetByIdAsync(id);

    if (unit == null)
    {
      return this.NotFound(new { message = "Unit not found" });
    }

    return this.Ok(unit);
  }

  [HttpGet("type/{type}")]
  public async Task<IActionResult> GetByType(string type)
  {
    var units = await this.unitRepository.GetByTypeAsync(type);
    return this.Ok(units);
  }

  [HttpGet("convert")]
  public async Task<IActionResult> GetConversion(
      [FromQuery] Guid fromUnitId,
      [FromQuery] Guid toUnitId)
  {
    var factor = await this.unitRepository.GetConversionFactorAsync(fromUnitId, toUnitId);

    if (factor == null)
    {
      return this.BadRequest(new { message = "Conversion not possible between these units" });
    }

    return this.Ok(new { fromUnitId, toUnitId, factor });
  }
}
