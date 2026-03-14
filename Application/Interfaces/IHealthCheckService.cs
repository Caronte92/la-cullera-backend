// <copyright file="IHealthCheckService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Application.Common;

namespace Application.Interfaces;

public interface IHealthCheckService
{
  Task<HealthStatus> CheckAsync();
}
