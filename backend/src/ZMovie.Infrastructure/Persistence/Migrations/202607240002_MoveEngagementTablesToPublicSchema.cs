using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ZMovie.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CatalogDbContext))]
[Migration("202607240002_MoveEngagementTablesToPublicSchema")]
public partial class MoveEngagementTablesToPublicSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE IF EXISTS engagement.saved_titles SET SCHEMA public;");
        migrationBuilder.Sql("ALTER TABLE IF EXISTS engagement.watch_history SET SCHEMA public;");
        migrationBuilder.Sql("ALTER TABLE IF EXISTS engagement.title_view_events SET SCHEMA public;");
        migrationBuilder.Sql("ALTER TABLE IF EXISTS engagement.title_reviews SET SCHEMA public;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE IF EXISTS public.saved_titles SET SCHEMA engagement;");
        migrationBuilder.Sql("ALTER TABLE IF EXISTS public.watch_history SET SCHEMA engagement;");
        migrationBuilder.Sql("ALTER TABLE IF EXISTS public.title_view_events SET SCHEMA engagement;");
        migrationBuilder.Sql("ALTER TABLE IF EXISTS public.title_reviews SET SCHEMA engagement;");
    }
}
