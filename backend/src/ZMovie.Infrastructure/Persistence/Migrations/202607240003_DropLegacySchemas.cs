using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ZMovie.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CatalogDbContext))]
[Migration("202607240003_DropLegacySchemas")]
public partial class DropLegacySchemas : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // These schemas are intentionally dropped without CASCADE: if a table was
        // missed by the move migrations, the migration must fail instead of deleting it.
        migrationBuilder.Sql("DROP SCHEMA IF EXISTS catalog;");
        migrationBuilder.Sql("DROP SCHEMA IF EXISTS engagement;");
        migrationBuilder.Sql("DROP SCHEMA IF EXISTS identity;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema("catalog");
        migrationBuilder.EnsureSchema("engagement");
        migrationBuilder.EnsureSchema("identity");
    }
}
