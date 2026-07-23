using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ZMovie.Infrastructure.Persistence;

#nullable disable

namespace ZMovie.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CatalogDbContext))]
[Migration("202607230006_AddTitleReviews")]
public partial class AddTitleReviews : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "title_reviews",
            schema: "engagement",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                title_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                author_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                rating = table.Column<int>(type: "integer", nullable: false),
                comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table => table.PrimaryKey("pk_title_reviews", x => x.id));
        migrationBuilder.CreateIndex(name: "ix_title_reviews_title_id_user_id", schema: "engagement", table: "title_reviews", columns: new[] { "title_id", "user_id" }, unique: true);
        migrationBuilder.CreateIndex(name: "ix_title_reviews_title_id_updated_at", schema: "engagement", table: "title_reviews", columns: new[] { "title_id", "updated_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "title_reviews", schema: "engagement");
}
