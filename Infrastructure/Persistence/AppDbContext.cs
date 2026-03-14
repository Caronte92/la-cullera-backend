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
    public static readonly Guid AdminRoleId = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    public static readonly Guid UserRoleId = new("b2c3d4e5-f6a7-8901-bcde-f12345678901");

    public AppDbContext(DbContextOptions<AppDbContext> options)
          : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;

    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    public DbSet<Role> Roles { get; set; } = null!;

    public DbSet<Recipe> Recipes { get; set; } = null!;

    public DbSet<Ingredient> Ingredients { get; set; } = null!;

    public DbSet<Step> Steps { get; set; } = null!;

    public DbSet<Tag> Tags { get; set; } = null!;

    public DbSet<RecipeTag> RecipeTags { get; set; } = null!;

    public DbSet<Unit> Units { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure BaseEntity for all entities
        ConfigureBaseEntity(modelBuilder);

        // Configure specific entities
        ConfigureUserEntity(modelBuilder);
        ConfigureRefreshTokenEntity(modelBuilder);
        ConfigureRoleEntity(modelBuilder);
        ConfigureRecipeEntity(modelBuilder);
        ConfigureIngredientEntity(modelBuilder);
        ConfigureStepEntity(modelBuilder);
        ConfigureTagEntity(modelBuilder);
        ConfigureRecipeTagEntity(modelBuilder);
        ConfigureUnitEntity(modelBuilder);
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

        userBuilder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        userBuilder.HasMany(u => u.Recipes)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
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

    private static void ConfigureRoleEntity(ModelBuilder modelBuilder)
    {
        var roleBuilder = modelBuilder.Entity<Role>();

        roleBuilder.Property(r => r.Admin).IsRequired();
        roleBuilder.Property(r => r.Write).IsRequired();
        roleBuilder.Property(r => r.Read).IsRequired();

        roleBuilder.HasData(
            new Role { Id = AdminRoleId, Admin = true, Write = true, Read = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Role { Id = UserRoleId, Admin = false, Write = false, Read = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
    }

    private static void ConfigureRecipeEntity(ModelBuilder modelBuilder)
    {
        var recipeBuilder = modelBuilder.Entity<Recipe>();

        recipeBuilder.Property(r => r.Name)
            .HasMaxLength(200)
            .IsRequired();

        recipeBuilder.Property(r => r.Slug)
            .HasMaxLength(200)
            .IsRequired();

        recipeBuilder.HasIndex(r => r.Slug)
            .IsUnique()
            .HasDatabaseName("IX_Recipe_Slug");

        recipeBuilder.Property(r => r.ImageUrl).HasMaxLength(500);
        recipeBuilder.Property(r => r.VideoUrl).HasMaxLength(500);

        recipeBuilder.Property(r => r.Difficulty)
            .HasMaxLength(20)
            .IsRequired();
    }

    private static void ConfigureIngredientEntity(ModelBuilder modelBuilder)
    {
        var ingredientBuilder = modelBuilder.Entity<Ingredient>();

        ingredientBuilder.Property(i => i.Name)
            .HasMaxLength(200)
            .IsRequired();

        ingredientBuilder.Property(i => i.Amount)
            .HasPrecision(10, 2);

        ingredientBuilder.HasOne(i => i.Recipe)
            .WithMany(r => r.Ingredients)
            .HasForeignKey(i => i.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        ingredientBuilder.HasOne(i => i.Unit)
            .WithMany(u => u.Ingredients)
            .HasForeignKey(i => i.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureStepEntity(ModelBuilder modelBuilder)
    {
        var stepBuilder = modelBuilder.Entity<Step>();

        stepBuilder.Property(s => s.Description)
            .IsRequired();

        stepBuilder.HasOne(s => s.Recipe)
            .WithMany(r => r.Steps)
            .HasForeignKey(s => s.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureTagEntity(ModelBuilder modelBuilder)
    {
        var tagBuilder = modelBuilder.Entity<Tag>();

        tagBuilder.Property(t => t.Name)
            .HasMaxLength(100)
            .IsRequired();

        tagBuilder.Property(t => t.Slug)
            .HasMaxLength(100)
            .IsRequired();

        tagBuilder.HasIndex(t => t.Slug)
            .IsUnique()
            .HasDatabaseName("IX_Tag_Slug");

        tagBuilder.HasIndex(t => t.Name)
            .IsUnique()
            .HasDatabaseName("IX_Tag_Name");
    }

    private static void ConfigureRecipeTagEntity(ModelBuilder modelBuilder)
    {
        var recipeTagBuilder = modelBuilder.Entity<RecipeTag>();

        recipeTagBuilder.HasIndex(rt => new { rt.RecipeId, rt.TagId })
            .IsUnique()
            .HasDatabaseName("IX_RecipeTag_RecipeId_TagId");

        recipeTagBuilder.HasOne(rt => rt.Recipe)
            .WithMany(r => r.RecipeTags)
            .HasForeignKey(rt => rt.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        recipeTagBuilder.HasOne(rt => rt.Tag)
            .WithMany(t => t.RecipeTags)
            .HasForeignKey(rt => rt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureUnitEntity(ModelBuilder modelBuilder)
    {
        var unitBuilder = modelBuilder.Entity<Unit>();

        unitBuilder.Property(u => u.Name)
            .HasMaxLength(50)
            .IsRequired();

        unitBuilder.Property(u => u.Abbreviation)
            .HasMaxLength(10)
            .IsRequired();

        unitBuilder.Property(u => u.Type)
            .HasMaxLength(20)
            .IsRequired();

        unitBuilder.Property(u => u.ToBaseFactor)
            .HasPrecision(18, 6);

        unitBuilder.HasIndex(u => u.Name)
            .IsUnique()
            .HasDatabaseName("IX_Unit_Name");

        unitBuilder.HasIndex(u => u.Abbreviation)
            .IsUnique()
            .HasDatabaseName("IX_Unit_Abbreviation");
    }
}
