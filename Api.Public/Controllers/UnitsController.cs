// <copyright file="UnitsController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Public.Controllers;

[ApiController]
[Route("units")]
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

  [Authorize]
  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateUnitDto dto)
  {
    if (await this.unitRepository.ExistsByNameAsync(dto.name))
    {
      return this.Conflict(new { message = "A unit with this name already exists" });
    }

    var unit = new Unit
    {
      Name = dto.name,
      Abbreviation = dto.abbreviation,
      Type = dto.type,
      ToBaseFactor = dto.toBaseFactor,
    };

    await this.unitRepository.AddAsync(unit);
    await this.unitRepository.SaveChangesAsync();

    return this.Created($"/units/{unit.Id}", new { unit.Id, unit.Name, unit.Abbreviation });
  }

  [Authorize]
  [HttpPut("{id:guid}")]
  public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnitDto dto)
  {
    var unit = await this.unitRepository.GetByIdAsync(id);

    if (unit == null)
    {
      return this.NotFound(new { message = "Unit not found" });
    }

    if (unit.Name != dto.name && await this.unitRepository.ExistsByNameAsync(dto.name))
    {
      return this.Conflict(new { message = "A unit with this name already exists" });
    }

    unit.Name = dto.name;
    unit.Abbreviation = dto.abbreviation;
    unit.Type = dto.type;
    unit.ToBaseFactor = dto.toBaseFactor;

    await this.unitRepository.UpdateAsync(unit);
    await this.unitRepository.SaveChangesAsync();

    return this.Ok(new { unit.Id, unit.Name, unit.Abbreviation });
  }

  [Authorize]
  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(Guid id)
  {
    var unit = await this.unitRepository.GetByIdAsync(id);

    if (unit == null)
    {
      return this.NotFound(new { message = "Unit not found" });
    }

    await this.unitRepository.DeleteAsync(unit);
    await this.unitRepository.SaveChangesAsync();

    return this.NoContent();
  }
}
