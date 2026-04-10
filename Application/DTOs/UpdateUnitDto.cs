// <copyright file="UpdateUnitDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Application.DTOs;

public record UpdateUnitDto(
    string name,
    string abbreviation,
    string type,
    decimal toBaseFactor);
