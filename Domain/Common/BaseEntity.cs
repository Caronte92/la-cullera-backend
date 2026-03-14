// <copyright file="BaseEntity.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Domain.Common;

/// <summary>
/// Clase base para todas las entidades del dominio.
/// Proporciona propiedades comunes como Id, timestamps, y soft delete.
/// </summary>
public abstract class BaseEntity
{
  /// <summary>
  /// Gets or sets identificador único de la entidad.
  /// </summary>
  public Guid Id { get; set; } = Guid.NewGuid();

  /// <summary>
  /// Gets or sets fecha de creación de la entidad.
  /// </summary>
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// Gets or sets usuario que creó la entidad.
  /// </summary>
  public string? CreatedBy { get; set; }

  /// <summary>
  /// Gets or sets fecha de última actualización.
  /// </summary>
  public DateTime? UpdatedAt { get; set; }

  /// <summary>
  /// Gets or sets usuario que actualizó la entidad.
  /// </summary>
  public string? UpdatedBy { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether indica si la entidad ha sido eliminada (soft delete).
  /// </summary>
  public bool IsDeleted { get; set; }

  /// <summary>
  /// Gets or sets fecha de eliminación lógica.
  /// </summary>
  public DateTime? DeletedAt { get; set; }

  /// <summary>
  /// Gets or sets usuario que eliminó la entidad.
  /// </summary>
  public string? DeletedBy { get; set; }
}
