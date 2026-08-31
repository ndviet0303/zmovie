using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZMovie.Infrastructure.Engagement.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EngagementInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS public.saved_titles (
                    user_id uuid NOT NULL,
                    title_id uuid NOT NULL,
                    saved_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_saved_titles PRIMARY KEY (user_id, title_id)
                );

                CREATE TABLE IF NOT EXISTS public.title_reviews (
                    id uuid NOT NULL,
                    title_id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    author_name character varying(300) NOT NULL,
                    rating integer NOT NULL,
                    comment character varying(2000) NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_title_reviews PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS public.watch_history (
                    user_id uuid NOT NULL,
                    playable_id uuid NOT NULL,
                    title_id uuid NOT NULL,
                    episode_number integer NULL,
                    progress_seconds double precision NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_watch_history PRIMARY KEY (user_id, playable_id)
                );

                CREATE INDEX IF NOT EXISTS ix_saved_titles_title_id ON public.saved_titles (title_id);
                CREATE INDEX IF NOT EXISTS ix_saved_titles_user_id_saved_at ON public.saved_titles (user_id, saved_at);
                CREATE INDEX IF NOT EXISTS ix_title_reviews_title_id_updated_at ON public.title_reviews (title_id, updated_at);
                CREATE UNIQUE INDEX IF NOT EXISTS ix_title_reviews_title_id_user_id ON public.title_reviews (title_id, user_id);
                CREATE INDEX IF NOT EXISTS ix_watch_history_title_id ON public.watch_history (title_id);
                CREATE INDEX IF NOT EXISTS ix_watch_history_user_id_title_id_updated_at ON public.watch_history (user_id, title_id, updated_at);
                CREATE INDEX IF NOT EXISTS ix_watch_history_user_id_updated_at ON public.watch_history (user_id, updated_at);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saved_titles",
                schema: "public");

            migrationBuilder.DropTable(
                name: "title_reviews",
                schema: "public");

            migrationBuilder.DropTable(
                name: "watch_history",
                schema: "public");
        }
    }
}
