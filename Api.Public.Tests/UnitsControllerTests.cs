// <copyright file="UnitsControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Api.Public.Controllers;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Api.Public.Tests;

public class UnitsControllerTests
{
  private readonly Mock<IUnitRepository> unitRepoMock;
  private readonly UnitsController sut;

  public UnitsControllerTests()
  {
    this.unitRepoMock = new Mock<IUnitRepository>();
    this.sut = new UnitsController(this.unitRepoMock.Object);
    this.sut.ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext(),
    };
  }

  // --- GetAll ---

  [Fact]
  public async Task GetAll_ShouldReturnOkWithUnits()
  {
    var units = new List<Unit>
    {
      new Unit { Name = "Gram", Abbreviation = "g", Type = "weight", ToBaseFactor = 1m },
      new Unit { Name = "Kilogram", Abbreviation = "kg", Type = "weight", ToBaseFactor = 1000m },
    };
    this.unitRepoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(units);

    var result = await this.sut.GetAll();

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    ok.Value.Should().Be(units);
  }

  // --- GetById ---

  [Fact]
  public async Task GetById_ShouldReturnOk_WhenFound()
  {
    var unit = new Unit { Name = "Gram", Abbreviation = "g", Type = "weight", ToBaseFactor = 1m };
    this.unitRepoMock.Setup(r => r.GetByIdAsync(unit.Id, default)).ReturnsAsync(unit);

    var result = await this.sut.GetById(unit.Id);

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    ok.Value.Should().Be(unit);
  }

  [Fact]
  public async Task GetById_ShouldReturnNotFound_WhenNotFound()
  {
    this.unitRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Unit?)null);

    var result = await this.sut.GetById(Guid.NewGuid());

    result.Should().BeOfType<NotFoundObjectResult>();
  }

  // --- GetByType ---

  [Fact]
  public async Task GetByType_ShouldReturnOk()
  {
    var units = new List<Unit>
    {
      new Unit { Name = "Gram", Abbreviation = "g", Type = "weight", ToBaseFactor = 1m },
    };
    this.unitRepoMock.Setup(r => r.GetByTypeAsync("weight", default)).ReturnsAsync(units);

    var result = await this.sut.GetByType("weight");

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    ok.Value.Should().Be(units);
  }

  // --- GetConversion ---

  [Fact]
  public async Task GetConversion_ShouldReturnOk_WhenConversionPossible()
  {
    var fromId = Guid.NewGuid();
    var toId = Guid.NewGuid();
    this.unitRepoMock
        .Setup(r => r.GetConversionFactorAsync(fromId, toId, default))
        .ReturnsAsync(1000m);

    var result = await this.sut.GetConversion(fromId, toId);

    result.Should().BeOfType<OkObjectResult>();
  }

  [Fact]
  public async Task GetConversion_ShouldReturnBadRequest_WhenNotPossible()
  {
    var fromId = Guid.NewGuid();
    var toId = Guid.NewGuid();
    this.unitRepoMock
        .Setup(r => r.GetConversionFactorAsync(fromId, toId, default))
        .ReturnsAsync((decimal?)null);

    var result = await this.sut.GetConversion(fromId, toId);

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  // --- Create ---

  [Fact]
  public async Task Create_ShouldReturnConflict_WhenNameExists()
  {
    this.unitRepoMock.Setup(r => r.ExistsByNameAsync("Gram", default)).ReturnsAsync(true);

    var result = await this.sut.Create(new CreateUnitDto("Gram", "g", "weight", 1m));

    result.Should().BeOfType<ConflictObjectResult>();
  }

  [Fact]
  public async Task Create_ShouldReturnCreated_WhenValid()
  {
    this.unitRepoMock.Setup(r => r.ExistsByNameAsync("Gram", default)).ReturnsAsync(false);

    var result = await this.sut.Create(new CreateUnitDto("Gram", "g", "weight", 1m));

    result.Should().BeOfType<CreatedResult>();
    this.unitRepoMock.Verify(r => r.AddAsync(It.Is<Unit>(u =>
        u.Name == "Gram" &&
        u.Abbreviation == "g" &&
        u.Type == "weight" &&
        u.ToBaseFactor == 1m), default), Times.Once);
    this.unitRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }

  // --- Update ---

  [Fact]
  public async Task Update_ShouldReturnNotFound_WhenUnitNotFound()
  {
    this.unitRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Unit?)null);

    var result = await this.sut.Update(Guid.NewGuid(), new UpdateUnitDto("Updated", "u", "weight", 1m));

    result.Should().BeOfType<NotFoundObjectResult>();
  }

  [Fact]
  public async Task Update_ShouldReturnConflict_WhenNameExists()
  {
    var unit = new Unit { Name = "Old", Abbreviation = "o", Type = "weight", ToBaseFactor = 1m };
    this.unitRepoMock.Setup(r => r.GetByIdAsync(unit.Id, default)).ReturnsAsync(unit);
    this.unitRepoMock.Setup(r => r.ExistsByNameAsync("Taken", default)).ReturnsAsync(true);

    var result = await this.sut.Update(unit.Id, new UpdateUnitDto("Taken", "t", "weight", 1m));

    result.Should().BeOfType<ConflictObjectResult>();
  }

  [Fact]
  public async Task Update_ShouldReturnOk_WhenValid()
  {
    var unit = new Unit { Name = "Old", Abbreviation = "o", Type = "weight", ToBaseFactor = 1m };
    this.unitRepoMock.Setup(r => r.GetByIdAsync(unit.Id, default)).ReturnsAsync(unit);
    this.unitRepoMock.Setup(r => r.ExistsByNameAsync("Updated", default)).ReturnsAsync(false);

    var result = await this.sut.Update(unit.Id, new UpdateUnitDto("Updated", "u", "volume", 500m));

    result.Should().BeOfType<OkObjectResult>();
    this.unitRepoMock.Verify(r => r.UpdateAsync(It.Is<Unit>(u =>
        u.Name == "Updated" &&
        u.Abbreviation == "u" &&
        u.Type == "volume" &&
        u.ToBaseFactor == 500m), default), Times.Once);
    this.unitRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }

  [Fact]
  public async Task Update_ShouldAllowSameName()
  {
    var unit = new Unit { Name = "Gram", Abbreviation = "g", Type = "weight", ToBaseFactor = 1m };
    this.unitRepoMock.Setup(r => r.GetByIdAsync(unit.Id, default)).ReturnsAsync(unit);

    var result = await this.sut.Update(unit.Id, new UpdateUnitDto("Gram", "g", "weight", 1m));

    result.Should().BeOfType<OkObjectResult>();
    this.unitRepoMock.Verify(r => r.ExistsByNameAsync(It.IsAny<string>(), default), Times.Never);
  }

  // --- Delete ---

  [Fact]
  public async Task Delete_ShouldReturnNotFound_WhenUnitNotFound()
  {
    this.unitRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Unit?)null);

    var result = await this.sut.Delete(Guid.NewGuid());

    result.Should().BeOfType<NotFoundObjectResult>();
  }

  [Fact]
  public async Task Delete_ShouldReturnNoContent_WhenValid()
  {
    var unit = new Unit { Name = "ToDelete", Abbreviation = "td", Type = "weight", ToBaseFactor = 1m };
    this.unitRepoMock.Setup(r => r.GetByIdAsync(unit.Id, default)).ReturnsAsync(unit);

    var result = await this.sut.Delete(unit.Id);

    result.Should().BeOfType<NoContentResult>();
    this.unitRepoMock.Verify(r => r.DeleteAsync(unit, null, default), Times.Once);
    this.unitRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }
}
