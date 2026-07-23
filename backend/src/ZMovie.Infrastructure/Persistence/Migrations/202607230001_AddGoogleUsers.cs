using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ZMovie.Infrastructure.Persistence.Migrations;

[Migration("202607230001_AddGoogleUsers")]
[DbContext(typeof(CatalogDbContext))]
public partial class AddGoogleUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema("identity");
        migrationBuilder.CreateTable(
            name: "users", schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                google_subject = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                avatar_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                last_signed_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_users", x => x.id));
        migrationBuilder.CreateIndex(name: "ix_users_google_subject", schema: "identity", table: "users", column: "google_subject", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "users", schema: "identity");
}
