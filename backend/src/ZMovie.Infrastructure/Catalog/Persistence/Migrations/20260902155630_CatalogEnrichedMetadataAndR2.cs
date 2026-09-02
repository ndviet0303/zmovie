using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZMovie.Infrastructure.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogEnrichedMetadataAndR2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.Sql(
                """
                ALTER TABLE public.titles ADD COLUMN IF NOT EXISTS actors character varying(2000) NOT NULL DEFAULT '';
                ALTER TABLE public.titles ADD COLUMN IF NOT EXISTS directors character varying(1000) NOT NULL DEFAULT '';
                ALTER TABLE public.titles ADD COLUMN IF NOT EXISTS country character varying(100) NOT NULL DEFAULT '';
                ALTER TABLE public.titles ADD COLUMN IF NOT EXISTS trailer_url character varying(2000) NOT NULL DEFAULT '';
                ALTER TABLE public.titles ADD COLUMN IF NOT EXISTS is_r2_hosted boolean NOT NULL DEFAULT FALSE;
                ALTER TABLE public.episodes ADD COLUMN IF NOT EXISTS subtitle_url character varying(2000) NOT NULL DEFAULT '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.titles DROP COLUMN IF EXISTS actors;
                ALTER TABLE public.titles DROP COLUMN IF EXISTS directors;
                ALTER TABLE public.titles DROP COLUMN IF EXISTS country;
                ALTER TABLE public.titles DROP COLUMN IF EXISTS trailer_url;
                ALTER TABLE public.titles DROP COLUMN IF EXISTS is_r2_hosted;
                ALTER TABLE public.episodes DROP COLUMN IF EXISTS subtitle_url;
                """);
        }
    }
}
