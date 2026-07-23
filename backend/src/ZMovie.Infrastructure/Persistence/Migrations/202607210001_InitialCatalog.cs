using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZMovie.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CatalogDbContext))]
[Migration("202607210001_InitialCatalog")]
public partial class InitialCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema("catalog");
        migrationBuilder.CreateTable(
            name: "titles", schema: "catalog",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false), slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false), english_title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false), vietnamese_title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false), english_synopsis = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false), vietnamese_synopsis = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false), genre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false), year = table.Column<int>(type: "integer", nullable: false), type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false), poster_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false), runtime_minutes = table.Column<int>(type: "integer", nullable: false), featured = table.Column<bool>(type: "boolean", nullable: false), created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_titles", x => x.id));
        migrationBuilder.CreateIndex(name: "ix_titles_slug", schema: "catalog", table: "titles", column: "slug", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "titles", schema: "catalog");
}
