using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ZMovie.Infrastructure.Persistence;

#nullable disable

namespace ZMovie.Infrastructure.Persistence.Migrations;

/// <summary>
/// The engagement tables are keyed user-first, so deleting a title (admin catalog delete)
/// had to sequential-scan them. These indexes make the per-title cleanup an index scan.
/// </summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("202607260003_AddTitleIdIndexes")]
public partial class AddTitleIdIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(name: "ix_saved_titles_title_id", schema: "public", table: "saved_titles", column: "title_id");
        migrationBuilder.CreateIndex(name: "ix_watch_history_title_id", schema: "public", table: "watch_history", column: "title_id");
        migrationBuilder.CreateIndex(name: "ix_assistant_learning_events_title_id", schema: "public", table: "assistant_learning_events", column: "title_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "ix_assistant_learning_events_title_id", schema: "public", table: "assistant_learning_events");
        migrationBuilder.DropIndex(name: "ix_watch_history_title_id", schema: "public", table: "watch_history");
        migrationBuilder.DropIndex(name: "ix_saved_titles_title_id", schema: "public", table: "saved_titles");
    }
}
