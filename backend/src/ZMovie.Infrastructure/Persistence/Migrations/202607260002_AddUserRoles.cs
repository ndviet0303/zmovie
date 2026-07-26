using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ZMovie.Infrastructure.Persistence;

#nullable disable

namespace ZMovie.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CatalogDbContext))]
[Migration("202607260002_AddUserRoles")]
public partial class AddUserRoles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "role",
            schema: "public",
            table: "users",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "member");

        migrationBuilder.CreateIndex(name: "ix_users_email", schema: "public", table: "users", column: "email");
        migrationBuilder.CreateIndex(name: "ix_users_role_created_at", schema: "public", table: "users", columns: new[] { "role", "created_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "ix_users_role_created_at", schema: "public", table: "users");
        migrationBuilder.DropIndex(name: "ix_users_email", schema: "public", table: "users");
        migrationBuilder.DropColumn(name: "role", schema: "public", table: "users");
    }
}
