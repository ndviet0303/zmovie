using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ZMovie.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CatalogDbContext))]
[Migration("202607240001_MoveCatalogTablesToPublicSchema")]
public partial class MoveCatalogTablesToPublicSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE IF EXISTS catalog.titles SET SCHEMA public;");
        migrationBuilder.Sql("ALTER TABLE IF EXISTS catalog.episodes SET SCHEMA public;");
        migrationBuilder.Sql("ALTER TABLE IF EXISTS catalog.genres SET SCHEMA public;");
        migrationBuilder.Sql("ALTER TABLE IF EXISTS identity.users SET SCHEMA public;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE IF EXISTS public.titles SET SCHEMA catalog;");
        migrationBuilder.Sql("ALTER TABLE IF EXISTS public.episodes SET SCHEMA catalog;");
        migrationBuilder.Sql("ALTER TABLE IF EXISTS public.genres SET SCHEMA catalog;");
        migrationBuilder.Sql("ALTER TABLE IF EXISTS public.users SET SCHEMA identity;");
    }
}
