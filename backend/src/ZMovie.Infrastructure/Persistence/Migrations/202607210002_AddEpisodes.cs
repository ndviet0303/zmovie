using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ZMovie.Infrastructure.Persistence.Migrations;

[Migration("202607210002_AddEpisodes")]
[DbContext(typeof(CatalogDbContext))]
public partial class AddEpisodes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "episodes", schema: "catalog", columns: table => new { id = table.Column<Guid>(type: "uuid", nullable: false), title_id = table.Column<Guid>(type: "uuid", nullable: false), number = table.Column<int>(type: "integer", nullable: false), name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false), hls_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false) }, constraints: table => table.PrimaryKey("pk_episodes", x => x.id));
        migrationBuilder.CreateIndex(name: "ix_episodes_title_id_number", schema: "catalog", table: "episodes", columns: new[] { "title_id", "number" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("episodes", "catalog");
}
