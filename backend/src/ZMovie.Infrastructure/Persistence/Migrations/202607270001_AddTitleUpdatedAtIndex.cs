using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ZMovie.Infrastructure.Persistence;

#nullable disable

namespace ZMovie.Infrastructure.Persistence.Migrations;

/// <summary>
/// The admin title list orders every page by updated_at. Measured against the real
/// 36k-row catalog that was a full sequential scan plus a top-N sort (~56 ms) just to
/// return 20 rows, on the most frequently hit query in the admin area.
/// </summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("202607270001_AddTitleUpdatedAtIndex")]
public partial class AddTitleUpdatedAtIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateIndex(name: "ix_titles_updated_at", schema: "public", table: "titles", column: "updated_at");

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropIndex(name: "ix_titles_updated_at", schema: "public", table: "titles");
}
