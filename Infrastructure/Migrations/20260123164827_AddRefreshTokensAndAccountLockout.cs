using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
  /// <inheritdoc />
  public partial class AddRefreshTokensAndAccountLockout : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "Users",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            AccessFailedCount = table.Column<int>(type: "integer", nullable: false),
            LockoutEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            UpdatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
            IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_Users", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "RefreshTokens",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
            ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            CreatedByIp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
            RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            RevokedByIp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
            ReplacedByToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            CreatedBy = table.Column<string>(type: "text", nullable: true),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            UpdatedBy = table.Column<string>(type: "text", nullable: true),
            IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            DeletedBy = table.Column<string>(type: "text", nullable: true),
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_RefreshTokens", x => x.Id);
            table.ForeignKey(
                      name: "FK_RefreshTokens_Users_UserId",
                      column: x => x.UserId,
                      principalTable: "Users",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "IX_RefreshToken_ExpiresAt",
          table: "RefreshTokens",
          column: "ExpiresAt");

      migrationBuilder.CreateIndex(
          name: "IX_RefreshToken_Token",
          table: "RefreshTokens",
          column: "Token",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_RefreshTokens_CreatedAt",
          table: "RefreshTokens",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_RefreshTokens_IsDeleted",
          table: "RefreshTokens",
          column: "IsDeleted");

      migrationBuilder.CreateIndex(
          name: "IX_RefreshTokens_UserId",
          table: "RefreshTokens",
          column: "UserId");

      migrationBuilder.CreateIndex(
          name: "IX_User_Email",
          table: "Users",
          column: "Email",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_User_Username",
          table: "Users",
          column: "Username",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_Users_CreatedAt",
          table: "Users",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_Users_IsDeleted",
          table: "Users",
          column: "IsDeleted");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "RefreshTokens");

      migrationBuilder.DropTable(
          name: "Users");
    }
  }
}
