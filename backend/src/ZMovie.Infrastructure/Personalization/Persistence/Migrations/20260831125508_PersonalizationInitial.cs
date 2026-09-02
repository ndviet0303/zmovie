using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZMovie.Infrastructure.Personalization.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersonalizationInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS public.assistant_learning_events (
                    id uuid NOT NULL,
                    recommendation_id uuid NOT NULL,
                    user_id uuid NOT NULL,
                    title_id uuid NOT NULL,
                    features character varying(2000) NOT NULL,
                    rank integer NOT NULL,
                    event_type character varying(32) NOT NULL,
                    reward double precision NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_assistant_learning_events PRIMARY KEY (id)
                );

                CREATE INDEX IF NOT EXISTS ix_assistant_learning_events_user_id_created_at ON public.assistant_learning_events (user_id, created_at);
                CREATE INDEX IF NOT EXISTS ix_assistant_learning_events_user_id_recommendation_id_title_id ON public.assistant_learning_events (user_id, recommendation_id, title_id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assistant_learning_events",
                schema: "public");
        }
    }
}
