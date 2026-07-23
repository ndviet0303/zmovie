using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ZMovie.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EngagementDbContext))]
[Migration("202607230004_AddPlayableProgress")]
public partial class AddPlayableProgress : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(name: "pk_watch_history", schema: "engagement", table: "watch_history");
        migrationBuilder.AddColumn<Guid>(name: "playable_id", schema: "engagement", table: "watch_history", type: "uuid", nullable: true);
        migrationBuilder.Sql("UPDATE engagement.watch_history SET playable_id = title_id WHERE playable_id IS NULL;");
        migrationBuilder.AlterColumn<Guid>(name: "playable_id", schema: "engagement", table: "watch_history", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        migrationBuilder.AddPrimaryKey(name: "pk_watch_history", schema: "engagement", table: "watch_history", columns: new[] { "user_id", "playable_id" });
        migrationBuilder.CreateIndex(name: "ix_watch_history_user_id_title_id_updated_at", schema: "engagement", table: "watch_history", columns: new[] { "user_id", "title_id", "updated_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "ix_watch_history_user_id_title_id_updated_at", schema: "engagement", table: "watch_history");
        migrationBuilder.DropPrimaryKey(name: "pk_watch_history", schema: "engagement", table: "watch_history");
        migrationBuilder.DropColumn(name: "playable_id", schema: "engagement", table: "watch_history");
        migrationBuilder.AddPrimaryKey(name: "pk_watch_history", schema: "engagement", table: "watch_history", columns: new[] { "user_id", "title_id" });
    }
}
