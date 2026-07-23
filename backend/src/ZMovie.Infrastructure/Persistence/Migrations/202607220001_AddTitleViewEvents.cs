using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ZMovie.Infrastructure.Persistence.Migrations;

[Migration("202607220001_AddTitleViewEvents")]
[DbContext(typeof(CatalogDbContext))]
public partial class AddTitleViewEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "title_view_events",
            schema: "catalog",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                title_id = table.Column<Guid>(type: "uuid", nullable: false),
                episode_number = table.Column<int>(type: "integer", nullable: true),
                viewer_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                viewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_title_view_events", x => x.id);
                table.ForeignKey("fk_title_view_events_titles_title_id", x => x.title_id, principalSchema: "catalog", principalTable: "titles", principalColumn: "id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(name: "ix_title_view_events_viewed_at_title_id", schema: "catalog", table: "title_view_events", columns: new[] { "viewed_at", "title_id" });
        migrationBuilder.CreateIndex(name: "ix_title_view_events_title_id_viewer_id_episode_number_viewed_at", schema: "catalog", table: "title_view_events", columns: new[] { "title_id", "viewer_id", "episode_number", "viewed_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "title_view_events", schema: "catalog");
}
