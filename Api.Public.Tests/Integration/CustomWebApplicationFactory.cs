// <copyright file="CustomWebApplicationFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Data.Common;
using System.Linq;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Api.Public.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that replaces Npgsql with SQLite in-memory for testing.
/// </summary>
/// <typeparam name="TProgram">The program entry point type.</typeparam>
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
  private static readonly string DbName = $"file:testdb_{Guid.NewGuid():N}?mode=memory&cache=shared";
  private readonly DbConnection keepAliveConnection;

  public CustomWebApplicationFactory()
  {
    // Keep a connection open so the shared-cache in-memory database survives
    this.keepAliveConnection = new SqliteConnection($"DataSource={DbName}");
    this.keepAliveConnection.Open();
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.ConfigureTestServices(services =>
    {
      // Remove ALL EF Core, DbContext, and Npgsql-related services
      var efDescriptors = services.Where(d =>
      {
        var st = d.ServiceType.FullName ?? string.Empty;
        var it = d.ImplementationType?.FullName ?? string.Empty;
        return st.Contains("DbContext") ||
               st.Contains("EntityFramework") ||
               st.Contains("Npgsql") ||
               st.Contains("Migration") ||
               it.Contains("DbContext") ||
               it.Contains("EntityFramework") ||
               it.Contains("Npgsql");
      }).ToList();

      foreach (var d in efDescriptors)
      {
        services.Remove(d);
      }

      // Replace Redis cache with in-memory
      var cacheDescriptors = services.Where(d =>
          d.ServiceType == typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache)).ToList();
      foreach (var d in cacheDescriptors)
      {
        services.Remove(d);
      }

      services.AddDistributedMemoryCache();

      // Remove health checks that need real DB/Redis
      var healthCheckDescriptors = services.Where(d =>
          d.ServiceType.FullName?.Contains("HealthCheck") == true).ToList();
      foreach (var d in healthCheckDescriptors)
      {
        services.Remove(d);
      }

      services.AddHealthChecks();

      // Remove background services that depend on real DB
      var hostedServiceDescriptors = services.Where(d =>
          d.ServiceType == typeof(IHostedService) &&
          d.ImplementationType?.Name == "TokenCleanupService").ToList();
      foreach (var d in hostedServiceDescriptors)
      {
        services.Remove(d);
      }

      // Add SQLite in-memory DbContext using shared-cache database
      services.AddDbContext<AppDbContext>(options =>
      {
        options.UseSqlite($"DataSource={DbName}");
      });
    });
  }

  protected override IHost CreateHost(IHostBuilder builder)
  {
    var host = base.CreateHost(builder);

    // Ensure the SQLite database schema is created
    using var scope = host.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    return host;
  }

  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);
    if (disposing)
    {
      this.keepAliveConnection.Dispose();
    }
  }
}
