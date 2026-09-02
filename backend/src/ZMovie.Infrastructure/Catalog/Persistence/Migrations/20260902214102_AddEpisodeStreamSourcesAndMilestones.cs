using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZMovie.Infrastructure.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEpisodeStreamSourcesAndMilestones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "intro_end",
                schema: "public",
                table: "episodes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "intro_start",
                schema: "public",
                table: "episodes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "outro_end",
                schema: "public",
                table: "episodes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "outro_start",
                schema: "public",
                table: "episodes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "episode_stream_sources",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    episode_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "hls"),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    subtitle_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    audio_track = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_episode_stream_sources", x => x.id);
                    table.ForeignKey(
                        name: "fk_episode_stream_sources_episodes_episode_id",
                        column: x => x.episode_id,
                        principalSchema: "public",
                        principalTable: "episodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_episode_stream_sources_episode_priority",
                schema: "public",
                table: "episode_stream_sources",
                columns: new[] { "episode_id", "priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "episode_stream_sources",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "intro_end",
                schema: "public",
                table: "episodes");

            migrationBuilder.DropColumn(
                name: "intro_start",
                schema: "public",
                table: "episodes");

            migrationBuilder.DropColumn(
                name: "outro_end",
                schema: "public",
                table: "episodes");

            migrationBuilder.DropColumn(
                name: "outro_start",
                schema: "public",
                table: "episodes");
        }
    }
}
