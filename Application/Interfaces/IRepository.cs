// <copyright file="IRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Linq.Expressions;
using Domain.Common;

namespace Application.Interfaces;

/// <summary>
/// Interfaz genérica para operaciones de repositorio.
/// </summary>
/// <typeparam name="TEntity">Tipo de entidad.</typeparam>
public interface IRepository<TEntity>
    where TEntity : BaseEntity
{
  /// <summary>
  /// Obtiene una entidad por su ID.
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

  /// <summary>
  /// Obtiene entidades paginadas.
  /// </summary>
  /// <param name="page">Número de página (1-based).</param>
  /// <param name="pageSize">Tamaño de página.</param>
  /// <param name="predicate">Filtro opcional.</param>
  /// <param name="orderBy">Ordenamiento opcional.</param>
  /// <returns>Resultado paginado.</returns>
  Task<Application.Common.Models.PagedResult<TEntity>> GetPagedAsync(
      int page,
      int pageSize,
      Expression<Func<TEntity, bool>>? predicate = null,
      Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Obtiene todas las entidades (excluyendo eliminadas).
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Obtiene entidades que cumplen con una condición.
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task<IEnumerable<TEntity>> FindAsync(
      Expression<Func<TEntity, bool>> predicate,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Busca la primera entidad que cumpla una condición.
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task<TEntity?> FirstOrDefaultAsync(
      Expression<Func<TEntity, bool>> predicate,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Verifica si existe una entidad que cumpla una condición.
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task<bool> AnyAsync(
      Expression<Func<TEntity, bool>> predicate,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Cuenta las entidades que cumplen una condición.
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task<int> CountAsync(
      Expression<Func<TEntity, bool>>? predicate = null,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Añade una nueva entidad.
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

  /// <summary>
  /// Añade múltiples entidades.
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

  /// <summary>
  /// Actualiza una entidad existente.
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

  /// <summary>
  /// Elimina de forma lógica una entidad (soft delete).
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task DeleteAsync(Guid id, string? deletedBy = null, CancellationToken cancellationToken = default);

  /// <summary>
  /// Elimina una entidad de forma lógica.
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task DeleteAsync(TEntity entity, string? deletedBy = null, CancellationToken cancellationToken = default);

  /// <summary>
  /// Elimina múltiples entidades de forma lógica.
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task DeleteRangeAsync(IEnumerable<Guid> ids, string? deletedBy = null,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Elimina de forma física una entidad de la base de datos.
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task HardDeleteAsync(Guid id, CancellationToken cancellationToken = default);

  /// <summary>
  /// Guarda los cambios en la base de datos.
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
