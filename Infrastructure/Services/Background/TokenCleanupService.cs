// <copyright file="TokenCleanupService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Background;

/// <summary>
/// Background service to clean up expired and revoked tokens.
/// </summary>
public class TokenCleanupService : BackgroundService
{
  private readonly IServiceProvider serviceProvider;
  private readonly ILogger<TokenCleanupService> logger;
  private readonly TimeSpan cleanupInterval = TimeSpan.FromHours(24);

  public TokenCleanupService(IServiceProvider serviceProvider, ILogger<TokenCleanupService> logger)
  {
    this.serviceProvider = serviceProvider;
    this.logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    this.logger.LogInformation("Token Cleanup Service starting.");

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        await this.CleanupTokensAsync(stoppingToken);
      }
      catch (Exception ex)
      {
        this.logger.LogError(ex, "Error occurred executing Token Cleanup.");
      }

      await Task.Delay(this.cleanupInterval, stoppingToken);
    }
  }

  private async Task CleanupTokensAsync(CancellationToken cancellationToken)
  {
    using var scope = this.serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var cutoffDate = DateTime.UtcNow.AddDays(-30); // Keep expired tokens for 30 days for audit

    var deletedCount = await context.RefreshTokens
        .Where(t => t.ExpiresAt < cutoffDate || (t.RevokedAt != null && t.RevokedAt < cutoffDate))
        .ExecuteDeleteAsync(cancellationToken);

    if (deletedCount > 0)
    {
      this.logger.LogInformation("Cleaned up {Count} expired refresh tokens.", deletedCount);
    }
  }
}
