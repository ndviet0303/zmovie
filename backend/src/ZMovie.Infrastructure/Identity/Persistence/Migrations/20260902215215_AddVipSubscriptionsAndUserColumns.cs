using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZMovie.Infrastructure.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVipSubscriptionsAndUserColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "subscription_tier",
                schema: "public",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Free");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "vip_expires_at",
                schema: "public",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "vip_subscriptions",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount_vnd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_gateway = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    transaction_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_fulfilled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vip_subscriptions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vip_subscriptions_gateway_reference",
                schema: "public",
                table: "vip_subscriptions",
                columns: new[] { "payment_gateway", "transaction_reference" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vip_subscriptions",
                schema: "public");

            migrationBuilder.DropColumn(
                name: "subscription_tier",
                schema: "public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "vip_expires_at",
                schema: "public",
                table: "users");
        }
    }
}
