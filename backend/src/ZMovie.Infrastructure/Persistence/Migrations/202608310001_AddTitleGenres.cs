using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ZMovie.Infrastructure.Persistence;

#nullable disable

namespace ZMovie.Infrastructure.Persistence.Migrations;

[DbContext(typeof(LegacyCatalogDbContext))]
[Migration("202608310001_AddTitleGenres")]
public partial class AddTitleGenres : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "title_genres",
            schema: "public",
            columns: table => new
            {
                title_id = table.Column<Guid>(type: "uuid", nullable: false),
                genre_id = table.Column<Guid>(type: "uuid", nullable: false),
                assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_title_genres", x => new { x.title_id, x.genre_id });
            });

        migrationBuilder.CreateIndex(
            name: "ix_title_genres_genre_id",
            schema: "public",
            table: "title_genres",
            column: "genre_id");

        // Deterministic backfill matching titles.genre comma-separated tokens against genres.name
        migrationBuilder.Sql("""
            INSERT INTO public.title_genres (title_id, genre_id, assigned_at)
            SELECT DISTINCT t.id AS title_id, g.id AS genre_id, now() AS assigned_at
            FROM public.titles t
            CROSS JOIN LATERAL regexp_split_to_table(t.genre, '\s*,\s*') AS raw_genre(name)
            JOIN public.genres g ON lower(trim(raw_genre.name)) = lower(trim(g.name))
            WHERE length(trim(raw_genre.name)) > 0
            ON CONFLICT (title_id, genre_id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "title_genres", schema: "public");
    }
}
