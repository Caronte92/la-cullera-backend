// <copyright file="UnitsControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Api.Public.Controllers;
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
}
