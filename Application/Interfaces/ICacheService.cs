// <copyright file="ICacheService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Interfaces;

/// <summary>
/// Interface for caching services.
/// </summary>
public interface ICacheService
{
  Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

  Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

  Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
