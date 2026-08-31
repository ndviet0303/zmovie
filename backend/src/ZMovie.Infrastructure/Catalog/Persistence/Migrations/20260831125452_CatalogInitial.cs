using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZMovie.Infrastructure.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS public.episodes (
                    id uuid NOT NULL,
                    title_id uuid NOT NULL,
                    number integer NOT NULL,
                    name character varying(200) NOT NULL,
                    hls_url character varying(2000) NOT NULL,
                    CONSTRAINT pk_episodes PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS public.genres (
                    id uuid NOT NULL,
                    slug character varying(160) NOT NULL,
                    name character varying(100) NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_genres PRIMARY KEY (id)
                );

                CREATE TABLE IF NOT EXISTS public.title_genres (
                    title_id uuid NOT NULL,
                    genre_id uuid NOT NULL,
                    assigned_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_title_genres PRIMARY KEY (title_id, genre_id)
                );

                CREATE TABLE IF NOT EXISTS public.titles (
                    id uuid NOT NULL,
                    slug character varying(160) NOT NULL,
                    vietnamese_title character varying(300) NOT NULL,
                    english_title character varying(300) NOT NULL,
                    vietnamese_synopsis text NOT NULL,
                    english_synopsis text NOT NULL,
                    genre character varying(100) NOT NULL,
                    year integer NOT NULL,
                    type character varying(32) NOT NULL,
                    poster_url character varying(2000) NOT NULL,
                    runtime_minutes integer NOT NULL,
                    featured boolean NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_titles PRIMARY KEY (id)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_episodes_title_id_number ON public.episodes (title_id, number);
                CREATE UNIQUE INDEX IF NOT EXISTS ix_genres_slug ON public.genres (slug);
                CREATE INDEX IF NOT EXISTS ix_title_genres_genre_id ON public.title_genres (genre_id);
                CREATE INDEX IF NOT EXISTS ix_titles_featured_created_at ON public.titles (featured, created_at);
                CREATE INDEX IF NOT EXISTS ix_titles_genre_created_at ON public.titles (genre, created_at);
                CREATE UNIQUE INDEX IF NOT EXISTS ix_titles_slug ON public.titles (slug);
                CREATE INDEX IF NOT EXISTS ix_titles_type_created_at ON public.titles (type, created_at);
                CREATE INDEX IF NOT EXISTS ix_titles_updated_at ON public.titles (updated_at);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "episodes",
                schema: "public");

            migrationBuilder.DropTable(
                name: "genres",
                schema: "public");

            migrationBuilder.DropTable(
                name: "title_genres",
                schema: "public");

            migrationBuilder.DropTable(
                name: "titles",
                schema: "public");
        }
    }
}
