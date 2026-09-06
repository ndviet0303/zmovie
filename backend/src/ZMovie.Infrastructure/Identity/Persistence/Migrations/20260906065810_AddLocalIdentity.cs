using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZMovie.Infrastructure.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email",
                schema: "public",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                schema: "public",
                table: "users",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "password_reset_expires_at",
                schema: "public",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_reset_token_hash",
                schema: "public",
                table: "users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "username",
                schema: "public",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE public.users
                SET username = LEFT(
                    regexp_replace(lower(split_part(email, '@', 1)), '[^a-z0-9_.-]+', '', 'g'),
                    48
                ) || '-' || LEFT(id::text, 8);
                """);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                schema: "public",
                table: "users",
                column: "email",
                unique: true,
                filter: "password_hash IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                schema: "public",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email",
                schema: "public",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_username",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_hash",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_reset_expires_at",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_reset_token_hash",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "username",
                schema: "public",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                schema: "public",
                table: "users",
                column: "email");
        }
    }
}
