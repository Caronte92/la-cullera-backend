// <copyright file="AppDbContext.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Linq;
using Domain.Common;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext
{
  public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
  {
  }

  public DbSet<User> Users { get; set; } = null!;

  public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Configure BaseEntity for all entities
    ConfigureBaseEntity(modelBuilder);

    // Configure specific entities
    ConfigureUserEntity(modelBuilder);
    ConfigureRefreshTokenEntity(modelBuilder);
  }

  /// <summary>
  /// Configures soft delete and audit trail for all entities inheriting from BaseEntity.
  /// </summary>
  private static void ConfigureBaseEntity(ModelBuilder modelBuilder)
  {
    // Get all entity types that inherit from BaseEntity
    var baseEntityTypes = modelBuilder.Model.GetEntityTypes()
        .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType));

    foreach (var entityType in baseEntityTypes)
    {
      // Configure soft delete query filter
      var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
      var filterExpression = System.Linq.Expressions.Expression.Lambda(
          System.Linq.Expressions.Expression.Equal(
              System.Linq.Expressions.Expression.Property(parameter, "IsDeleted"),
              System.Linq.Expressions.Expression.Constant(false)),
          parameter);

      entityType.SetQueryFilter(filterExpression);

      // Configure CreatedAt default value
      entityType
          .FindProperty(nameof(BaseEntity.CreatedAt))?
          .SetDefaultValueSql("CURRENT_TIMESTAMP");

      // Create index on IsDeleted for query performance
      var tableName = entityType.GetTableName() ?? entityType.DisplayName();
      modelBuilder
          .Entity(entityType.ClrType)
          .HasIndex(nameof(BaseEntity.IsDeleted))
          .HasDatabaseName($"IX_{tableName}_IsDeleted");

      // Create index on CreatedAt for sorting
      modelBuilder
          .Entity(entityType.ClrType)
          .HasIndex(nameof(BaseEntity.CreatedAt))
          .HasDatabaseName($"IX_{tableName}_CreatedAt");
    }
  }

  private static void ConfigureUserEntity(ModelBuilder modelBuilder)
  {
    var userBuilder = modelBuilder.Entity<User>();

    userBuilder.HasIndex(u => u.Username)
        .IsUnique()
        .HasDatabaseName("IX_User_Username");

    userBuilder.HasIndex(u => u.Email)
        .IsUnique()
        .HasDatabaseName("IX_User_Email");

    userBuilder.Property(u => u.Username)
        .HasMaxLength(50)
        .IsRequired();

    userBuilder.Property(u => u.Email)
        .HasMaxLength(255)
        .IsRequired();

    userBuilder.Property(u => u.PasswordHash)
        .HasMaxLength(255)
        .IsRequired();

    userBuilder.Property(u => u.FirstName)
        .HasMaxLength(100)
        .IsRequired();

    userBuilder.Property(u => u.LastName)
        .HasMaxLength(100)
        .IsRequired();

    userBuilder.Property(u => u.CreatedBy)
        .HasMaxLength(255);

    userBuilder.Property(u => u.UpdatedBy)
        .HasMaxLength(255);

    userBuilder.Property(u => u.DeletedBy)
        .HasMaxLength(255);

    userBuilder.HasMany(u => u.RefreshTokens)
        .WithOne(rt => rt.User)
        .HasForeignKey(rt => rt.UserId)
        .OnDelete(DeleteBehavior.Cascade);
  }

  private static void ConfigureRefreshTokenEntity(ModelBuilder modelBuilder)
  {
    var refreshTokenBuilder = modelBuilder.Entity<RefreshToken>();

    refreshTokenBuilder.Property(rt => rt.Token)
        .HasMaxLength(500)
        .IsRequired();

    refreshTokenBuilder.HasIndex(rt => rt.Token)
        .IsUnique()
        .HasDatabaseName("IX_RefreshToken_Token");

    refreshTokenBuilder.HasIndex(rt => rt.ExpiresAt)
        .HasDatabaseName("IX_RefreshToken_ExpiresAt");

    refreshTokenBuilder.Property(rt => rt.CreatedByIp)
        .HasMaxLength(50);

    refreshTokenBuilder.Property(rt => rt.RevokedByIp)
        .HasMaxLength(50);

    refreshTokenBuilder.Property(rt => rt.ReplacedByToken)
        .HasMaxLength(500);
  }
}
