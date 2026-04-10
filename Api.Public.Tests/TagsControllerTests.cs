// <copyright file="TagsControllerTests.cs" company="PlaceholderCompany">
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

public class TagsControllerTests
{
  private readonly Mock<ITagRepository> tagRepoMock;
  private readonly TagsController sut;

  public TagsControllerTests()
  {
    this.tagRepoMock = new Mock<ITagRepository>();
    this.sut = new TagsController(this.tagRepoMock.Object);
    this.sut.ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext(),
    };
  }

  // --- GetAll ---
  [Fact]
  public async Task GetAll_ShouldReturnOkWithTags()
  {
    var tags = new List<Tag>
    {
      new Tag { Name = "Vegetarian", Slug = "vegetarian" },
      new Tag { Name = "Dessert", Slug = "dessert" },
    };
    this.tagRepoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(tags);

    var result = await this.sut.GetAll();

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    ok.Value.Should().Be(tags);
  }

  // --- GetBySlug ---
  [Fact]
  public async Task GetBySlug_ShouldReturnOk_WhenFound()
  {
    var tag = new Tag { Name = "Vegetarian", Slug = "vegetarian" };
    this.tagRepoMock.Setup(r => r.GetBySlugAsync("vegetarian", default)).ReturnsAsync(tag);

    var result = await this.sut.GetBySlug("vegetarian");

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    ok.Value.Should().Be(tag);
  }

  [Fact]
  public async Task GetBySlug_ShouldReturnNotFound_WhenNotFound()
  {
    this.tagRepoMock.Setup(r => r.GetBySlugAsync("nonexistent", default)).ReturnsAsync((Tag?)null);

    var result = await this.sut.GetBySlug("nonexistent");

    result.Should().BeOfType<NotFoundObjectResult>();
  }

  // --- Search ---
  [Fact]
  public async Task Search_ShouldReturnBadRequest_WhenQueryEmpty()
  {
    var result = await this.sut.Search(string.Empty);

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task Search_ShouldReturnOk_WhenQueryProvided()
  {
    var tags = new List<Tag> { new Tag { Name = "Vegan", Slug = "vegan" } };
    this.tagRepoMock.Setup(r => r.SearchByNameAsync("veg", default)).ReturnsAsync(tags);

    var result = await this.sut.Search("veg");

    var ok = result.Should().BeOfType<OkObjectResult>().Subject;
    ok.Value.Should().Be(tags);
  }

  // --- Create ---
  [Fact]
  public async Task Create_ShouldReturnConflict_WhenNameExists()
  {
    this.tagRepoMock.Setup(r => r.ExistsByNameAsync("Vegetarian", default)).ReturnsAsync(true);

    var result = await this.sut.Create(new CreateTagDto("Vegetarian"));

    result.Should().BeOfType<ConflictObjectResult>();
  }

  [Fact]
  public async Task Create_ShouldReturnCreated_WhenValid()
  {
    this.tagRepoMock.Setup(r => r.ExistsByNameAsync("Vegetarian", default)).ReturnsAsync(false);

    var result = await this.sut.Create(new CreateTagDto("Vegetarian"));

    var created = result.Should().BeOfType<CreatedResult>().Subject;
    created.Location.Should().Contain("vegetarian");
    this.tagRepoMock.Verify(
        r => r.AddAsync(
            It.Is<Tag>(t => t.Name == "Vegetarian" && t.Slug == "vegetarian"),
            default),
        Times.Once);
    this.tagRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }

  // --- Update ---
  [Fact]
  public async Task Update_ShouldReturnNotFound_WhenTagNotFound()
  {
    this.tagRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Tag?)null);

    var result = await this.sut.Update(Guid.NewGuid(), new UpdateTagDto("Updated"));

    result.Should().BeOfType<NotFoundObjectResult>();
  }

  [Fact]
  public async Task Update_ShouldReturnConflict_WhenNameExists()
  {
    var tag = new Tag { Name = "Old", Slug = "old" };
    this.tagRepoMock.Setup(r => r.GetByIdAsync(tag.Id, default)).ReturnsAsync(tag);
    this.tagRepoMock.Setup(r => r.ExistsByNameAsync("Taken", default)).ReturnsAsync(true);

    var result = await this.sut.Update(tag.Id, new UpdateTagDto("Taken"));

    result.Should().BeOfType<ConflictObjectResult>();
  }

  [Fact]
  public async Task Update_ShouldReturnOk_WhenValid()
  {
    var tag = new Tag { Name = "Old", Slug = "old" };
    this.tagRepoMock.Setup(r => r.GetByIdAsync(tag.Id, default)).ReturnsAsync(tag);
    this.tagRepoMock.Setup(r => r.ExistsByNameAsync("Updated", default)).ReturnsAsync(false);

    var result = await this.sut.Update(tag.Id, new UpdateTagDto("Updated"));

    result.Should().BeOfType<OkObjectResult>();
    this.tagRepoMock.Verify(
        r => r.UpdateAsync(
            It.Is<Tag>(t => t.Name == "Updated" && t.Slug == "updated"),
            default),
        Times.Once);
    this.tagRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }

  [Fact]
  public async Task Update_ShouldAllowSameName()
  {
    var tag = new Tag { Name = "Same", Slug = "same" };
    this.tagRepoMock.Setup(r => r.GetByIdAsync(tag.Id, default)).ReturnsAsync(tag);

    var result = await this.sut.Update(tag.Id, new UpdateTagDto("Same"));

    result.Should().BeOfType<OkObjectResult>();
    this.tagRepoMock.Verify(r => r.ExistsByNameAsync(It.IsAny<string>(), default), Times.Never);
  }

  // --- Delete ---
  [Fact]
  public async Task Delete_ShouldReturnNotFound_WhenTagNotFound()
  {
    this.tagRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Tag?)null);

    var result = await this.sut.Delete(Guid.NewGuid());

    result.Should().BeOfType<NotFoundObjectResult>();
  }

  [Fact]
  public async Task Delete_ShouldReturnNoContent_WhenValid()
  {
    var tag = new Tag { Name = "ToDelete", Slug = "todelete" };
    this.tagRepoMock.Setup(r => r.GetByIdAsync(tag.Id, default)).ReturnsAsync(tag);

    var result = await this.sut.Delete(tag.Id);

    result.Should().BeOfType<NoContentResult>();
    this.tagRepoMock.Verify(r => r.DeleteAsync(tag, null, default), Times.Once);
    this.tagRepoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
  }
}
