// <copyright file="HealthStatus.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.Common;

public record HealthStatus(
    bool apiUp,
    bool databaseUp);
