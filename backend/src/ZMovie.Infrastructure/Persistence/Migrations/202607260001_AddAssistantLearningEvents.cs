using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ZMovie.Infrastructure.Persistence;

#nullable disable

namespace ZMovie.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CatalogDbContext))]
[Migration("202607260001_AddAssistantLearningEvents")]
public partial class AddAssistantLearningEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "assistant_learning_events",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                recommendation_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                title_id = table.Column<Guid>(type: "uuid", nullable: false),
                features = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                rank = table.Column<int>(type: "integer", nullable: false),
                event_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                reward = table.Column<double>(type: "double precision", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table => table.PrimaryKey("pk_assistant_learning_events", x => x.id));

        migrationBuilder.CreateIndex(name: "ix_assistant_learning_events_user_id_recommendation_id_title_id", schema: "public", table: "assistant_learning_events", columns: new[] { "user_id", "recommendation_id", "title_id" });
        migrationBuilder.CreateIndex(name: "ix_assistant_learning_events_user_id_created_at", schema: "public", table: "assistant_learning_events", columns: new[] { "user_id", "created_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "assistant_learning_events", schema: "public");
}
