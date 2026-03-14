// <copyright file="TagsController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Globalization;
using System.Text.RegularExpressions;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Public.Controllers;

[ApiController]
[Route("tags")]
public partial class TagsController : ControllerBase
{
  private readonly ITagRepository tagRepository;

  public TagsController(ITagRepository tagRepository)
  {
    this.tagRepository = tagRepository;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll()
  {
    var tags = await this.tagRepository.GetAllAsync();
    return this.Ok(tags);
  }

  [HttpGet("{slug}")]
  public async Task<IActionResult> GetBySlug(string slug)
  {
    var tag = await this.tagRepository.GetBySlugAsync(slug);

    if (tag == null)
    {
      return this.NotFound(new { message = "Tag not found" });
    }

    return this.Ok(tag);
  }

  [HttpGet("search")]
  public async Task<IActionResult> Search([FromQuery] string q)
  {
    if (string.IsNullOrWhiteSpace(q))
    {
      return this.BadRequest(new { message = "Search query is required" });
    }

    var tags = await this.tagRepository.SearchByNameAsync(q);
    return this.Ok(tags);
  }

  [Authorize]
  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateTagDto dto)
  {
    if (await this.tagRepository.ExistsByNameAsync(dto.name))
    {
      return this.Conflict(new { message = "A tag with this name already exists" });
    }

    var tag = new Tag
    {
      Name = dto.name,
      Slug = GenerateSlug(dto.name),
    };

    await this.tagRepository.AddAsync(tag);
    await this.tagRepository.SaveChangesAsync();

    return this.Created($"/tags/{tag.Slug}", new { tag.Id, tag.Name, tag.Slug });
  }

  [Authorize]
  [HttpPut("{id:guid}")]
  public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTagDto dto)
  {
    var tag = await this.tagRepository.GetByIdAsync(id);

    if (tag == null)
    {
      return this.NotFound(new { message = "Tag not found" });
    }

    if (tag.Name != dto.name && await this.tagRepository.ExistsByNameAsync(dto.name))
    {
      return this.Conflict(new { message = "A tag with this name already exists" });
    }

    tag.Name = dto.name;
    tag.Slug = GenerateSlug(dto.name);

    await this.tagRepository.UpdateAsync(tag);
    await this.tagRepository.SaveChangesAsync();

    return this.Ok(new { tag.Id, tag.Name, tag.Slug });
  }

  [Authorize]
  [HttpDelete("{id:guid}")]
  public async Task<IActionResult> Delete(Guid id)
  {
    var tag = await this.tagRepository.GetByIdAsync(id);

    if (tag == null)
    {
      return this.NotFound(new { message = "Tag not found" });
    }

    await this.tagRepository.DeleteAsync(tag);
    await this.tagRepository.SaveChangesAsync();

    return this.NoContent();
  }

  private static string GenerateSlug(string name)
  {
    var slug = name.ToLower(CultureInfo.InvariantCulture).Trim();
    slug = SlugInvalidCharsRegex().Replace(slug, string.Empty);
    slug = SlugWhitespaceRegex().Replace(slug, "-");
    slug = SlugMultipleDashRegex().Replace(slug, "-");
    return slug.Trim('-');
  }

  [GeneratedRegex(@"[^a-z0-9\s-]")]
  private static partial Regex SlugInvalidCharsRegex();

  [GeneratedRegex(@"\s+")]
  private static partial Regex SlugWhitespaceRegex();

  [GeneratedRegex(@"-{2,}")]
  private static partial Regex SlugMultipleDashRegex();
}
