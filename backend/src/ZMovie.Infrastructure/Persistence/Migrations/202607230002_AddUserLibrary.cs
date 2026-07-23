using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ZMovie.Infrastructure.Persistence.Migrations;

[Migration("202607230002_AddUserLibrary")]
[DbContext(typeof(CatalogDbContext))]
public partial class AddUserLibrary : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "saved_titles", schema: "identity", columns: table => new { user_id = table.Column<Guid>(type: "uuid", nullable: false), title_id = table.Column<Guid>(type: "uuid", nullable: false), saved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false) }, constraints: table => { table.PrimaryKey("pk_saved_titles", x => new { x.user_id, x.title_id }); table.ForeignKey("fk_saved_titles_users_user_id", x => x.user_id, principalSchema: "identity", principalTable: "users", principalColumn: "id", onDelete: ReferentialAction.Cascade); table.ForeignKey("fk_saved_titles_titles_title_id", x => x.title_id, principalSchema: "catalog", principalTable: "titles", principalColumn: "id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateTable(name: "watch_history", schema: "identity", columns: table => new { user_id = table.Column<Guid>(type: "uuid", nullable: false), title_id = table.Column<Guid>(type: "uuid", nullable: false), episode_number = table.Column<int>(type: "integer", nullable: true), progress_seconds = table.Column<double>(type: "double precision", nullable: false), updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false) }, constraints: table => { table.PrimaryKey("pk_watch_history", x => new { x.user_id, x.title_id }); table.ForeignKey("fk_watch_history_users_user_id", x => x.user_id, principalSchema: "identity", principalTable: "users", principalColumn: "id", onDelete: ReferentialAction.Cascade); table.ForeignKey("fk_watch_history_titles_title_id", x => x.title_id, principalSchema: "catalog", principalTable: "titles", principalColumn: "id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateIndex(name: "ix_saved_titles_user_id_saved_at", schema: "identity", table: "saved_titles", columns: new[] { "user_id", "saved_at" });
        migrationBuilder.CreateIndex(name: "ix_watch_history_user_id_updated_at", schema: "identity", table: "watch_history", columns: new[] { "user_id", "updated_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) { migrationBuilder.DropTable(name: "saved_titles", schema: "identity"); migrationBuilder.DropTable(name: "watch_history", schema: "identity"); }
}
