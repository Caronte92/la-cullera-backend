// <copyright file="CacheService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text.Json;
using Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Services;

public class CacheService : ICacheService
{
  private readonly IDistributedCache cache;

  public CacheService(IDistributedCache cache)
  {
    this.cache = cache;
  }

  public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
  {
    var value = await this.cache.GetStringAsync(key, cancellationToken);
    if (value == null)
    {
      return default;
    }

    return JsonSerializer.Deserialize<T>(value);
  }

  public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
  {
    var options = new DistributedCacheEntryOptions
    {
      AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(60),
    };

    var json = JsonSerializer.Serialize(value);
    await this.cache.SetStringAsync(key, json, options, cancellationToken);
  }

  public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
  {
    await this.cache.RemoveAsync(key, cancellationToken);
  }
}
