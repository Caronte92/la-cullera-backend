// <copyright file="PagedResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Common.Models;

/// <summary>
/// Represents a paginated result.
/// </summary>
/// <typeparam name="T">The type of items in the page.</typeparam>
public class PagedResult<T>
{
  public PagedResult(IEnumerable<T> items, int count, int pageNumber, int pageSize)
  {
    this.TotalCount = count;
    this.PageNumber = pageNumber;
    this.TotalPages = (int)Math.Ceiling(count / (double)pageSize);
    this.Items = items;
  }

  public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();

  public int PageNumber { get; set; }

  public int TotalPages { get; set; }

  public int TotalCount { get; set; }

  public bool HasPrevious => this.PageNumber > 1;

  public bool HasNext => this.PageNumber < this.TotalPages;
}
