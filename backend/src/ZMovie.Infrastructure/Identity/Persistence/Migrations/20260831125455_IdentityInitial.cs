using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZMovie.Infrastructure.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IdentityInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS public.users (
                    id uuid NOT NULL,
                    google_subject character varying(128) NOT NULL,
                    email character varying(320) NOT NULL,
                    display_name character varying(300) NOT NULL,
                    avatar_url character varying(2000) NULL,
                    role character varying(32) NOT NULL DEFAULT 'member',
                    created_at timestamp with time zone NOT NULL,
                    last_signed_in_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_users PRIMARY KEY (id)
                );

                CREATE INDEX IF NOT EXISTS ix_users_email ON public.users (email);
                CREATE UNIQUE INDEX IF NOT EXISTS ix_users_google_subject ON public.users (google_subject);
                CREATE INDEX IF NOT EXISTS ix_users_role_created_at ON public.users (role, created_at);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "users",
                schema: "public");
        }
    }
}
