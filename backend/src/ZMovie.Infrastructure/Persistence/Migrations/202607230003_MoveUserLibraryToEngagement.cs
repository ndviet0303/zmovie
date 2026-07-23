using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ZMovie.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EngagementDbContext))]
[Migration("202607230003_MoveUserLibraryToEngagement")]
public partial class MoveUserLibraryToEngagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("fk_saved_titles_users_user_id", "saved_titles", "identity");
        migrationBuilder.DropForeignKey("fk_saved_titles_titles_title_id", "saved_titles", "identity");
        migrationBuilder.DropForeignKey("fk_watch_history_users_user_id", "watch_history", "identity");
        migrationBuilder.DropForeignKey("fk_watch_history_titles_title_id", "watch_history", "identity");
        migrationBuilder.EnsureSchema("engagement");
        migrationBuilder.RenameTable("saved_titles", "identity", newName: "saved_titles", newSchema: "engagement");
        migrationBuilder.RenameTable("watch_history", "identity", newName: "watch_history", newSchema: "engagement");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable("saved_titles", "engagement", newName: "saved_titles", newSchema: "identity");
        migrationBuilder.RenameTable("watch_history", "engagement", newName: "watch_history", newSchema: "identity");
    }
}
