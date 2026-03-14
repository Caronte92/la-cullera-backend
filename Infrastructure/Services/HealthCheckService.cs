// <copyright file="HealthCheckService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Common;
using Application.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class HealthCheckService : IHealthCheckService
{
  private readonly AppDbContext context;

  public HealthCheckService(AppDbContext context)
  {
    this.context = context;
  }

  public async Task<HealthStatus> CheckAsync()
  {
    var databaseUp = await this.context.Database.CanConnectAsync();
    return new HealthStatus(
        apiUp: true,
        databaseUp: databaseUp);
  }
}
