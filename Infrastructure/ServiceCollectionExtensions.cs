// <copyright file="ServiceCollectionExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Infrastructure;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddInfrastructureServices(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    var dbHost = configuration["Database:Host"] ?? "localhost";
    var dbPort = configuration["Database:Port"] ?? "5432";
    var dbName = configuration["Database:Name"] ?? throw new InvalidOperationException("Database:Name not configured.");
    var dbUser = configuration["Database:Username"] ?? throw new InvalidOperationException("Database:Username not configured.");
    var dbPassword = configuration["Database:Password"] ?? throw new InvalidOperationException("Database:Password not configured.");
    var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

    services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));

    // Register generic repository
    services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

    // User repository
    services.AddScoped<Application.Interfaces.IUserRepository, Infrastructure.Repositories.UserRepository>();

    // Recipe repository
    services.AddScoped<Application.Interfaces.IRecipeRepository, Infrastructure.Repositories.RecipeRepository>();

    // Tag repository
    services.AddScoped<Application.Interfaces.ITagRepository, Infrastructure.Repositories.TagRepository>();

    // Unit repository
    services.AddScoped<Application.Interfaces.IUnitRepository, Infrastructure.Repositories.UnitRepository>();

    // Shared recipe repository
    services.AddScoped<Application.Interfaces.ISharedRecipeRepository, Infrastructure.Repositories.SharedRecipeRepository>();

    // Token service for JWT creation
    services.AddScoped<Application.Interfaces.ITokenService, Infrastructure.Services.TokenService>();

    // Password hashing service (Singleton for performance)
    services.AddSingleton<Application.Interfaces.IPasswordHasher, Infrastructure.Services.PasswordHasher>();

    // User authentication service
    services.AddScoped<Application.Interfaces.IUserService, Infrastructure.Services.UserService>();

    // Unit of Work
    services.AddScoped<Application.Interfaces.IUnitOfWork, Infrastructure.Persistence.UnitOfWork>();

    // Caching (Redis)
    services.AddStackExchangeRedisCache(options =>
    {
      options.Configuration = configuration.GetConnectionString("Redis");
      options.InstanceName = "Backend:";
    });
    services.AddSingleton<Application.Interfaces.ICacheService, Infrastructure.Services.CacheService>();

    // Background Services
    services.AddHostedService<Infrastructure.Services.Background.TokenCleanupService>();

    // Health Checks
    var redisConnection = configuration.GetConnectionString("Redis") ?? string.Empty;
    services.AddHealthChecks()
        .AddNpgSql(connectionString, name: "database", tags: new[] { "ready" })
        .AddRedis(redisConnection, name: "redis", tags: new[] { "ready" });

    // OpenTelemetry
    services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
          metrics.AddAspNetCoreInstrumentation()
                 .AddPrometheusExporter();
        })
        .WithTracing(tracing =>
        {
          tracing.AddAspNetCoreInstrumentation()
                 .AddEntityFrameworkCoreInstrumentation();
        });

    return services;
  }
}
