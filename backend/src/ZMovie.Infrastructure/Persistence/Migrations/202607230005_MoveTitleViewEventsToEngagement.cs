using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ZMovie.Infrastructure.Persistence;

#nullable disable

namespace ZMovie.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EngagementDbContext))]
[Migration("202607230005_MoveTitleViewEventsToEngagement")]
public partial class MoveTitleViewEventsToEngagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE IF EXISTS catalog.title_view_events SET SCHEMA engagement;");
        migrationBuilder.Sql("ALTER TABLE IF EXISTS engagement.title_view_events RENAME COLUMN viewer_id TO session_id;");
        migrationBuilder.Sql("ALTER TABLE IF EXISTS engagement.title_view_events ADD COLUMN IF NOT EXISTS user_id uuid NULL;");
        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_title_view_events_title_id_user_id_session_id_episode_number_viewed_at ON engagement.title_view_events (title_id, user_id, session_id, episode_number, viewed_at);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE IF EXISTS engagement.title_view_events DROP COLUMN IF EXISTS user_id;");
        migrationBuilder.Sql("ALTER TABLE IF EXISTS engagement.title_view_events RENAME COLUMN session_id TO viewer_id;");
        migrationBuilder.Sql("ALTER TABLE IF EXISTS engagement.title_view_events SET SCHEMA catalog;");
    }
}
