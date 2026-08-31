using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZMovie.Infrastructure.Analytics.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AnalyticsInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS public.title_view_events (
                    id uuid NOT NULL,
                    title_id uuid NOT NULL,
                    episode_number integer NULL,
                    user_id uuid NULL,
                    session_id character varying(128) NOT NULL,
                    viewed_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_title_view_events PRIMARY KEY (id)
                );

                CREATE INDEX IF NOT EXISTS ix_title_view_events_session_id ON public.title_view_events (session_id);
                CREATE INDEX IF NOT EXISTS ix_title_view_events_title_id ON public.title_view_events (title_id);
                CREATE INDEX IF NOT EXISTS ix_title_view_events_user_id ON public.title_view_events (user_id);
                CREATE INDEX IF NOT EXISTS ix_title_view_events_viewed_at ON public.title_view_events (viewed_at);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "title_view_events",
                schema: "public");
        }
    }
}
