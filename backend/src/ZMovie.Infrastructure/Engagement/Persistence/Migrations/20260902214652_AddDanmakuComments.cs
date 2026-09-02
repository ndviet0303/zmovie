using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZMovie.Infrastructure.Engagement.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDanmakuComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS public.danmaku_comments (
                    id uuid NOT NULL,
                    title_slug character varying(160) NOT NULL,
                    episode_number integer NOT NULL,
                    time_seconds integer NOT NULL,
                    content character varying(500) NOT NULL,
                    color character varying(20) NOT NULL DEFAULT '#ffffff',
                    user_id uuid NULL,
                    author_name character varying(100) NOT NULL DEFAULT 'Anonymous',
                    created_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_danmaku_comments PRIMARY KEY (id)
                );

                CREATE INDEX IF NOT EXISTS ix_danmaku_slug_episode_time
                    ON public.danmaku_comments (title_slug, episode_number, time_seconds);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "danmaku_comments",
                schema: "public");
        }
    }
}
