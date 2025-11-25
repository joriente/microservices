using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductOrderingSystem.InventoryService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity_on_hand = table.Column<int>(type: "integer", nullable: false),
                    quantity_reserved = table.Column<int>(type: "integer", nullable: false),
                    reorder_level = table.Column<int>(type: "integer", nullable: false),
                    reorder_quantity = table.Column<int>(type: "integer", nullable: false),
                    last_restocked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fulfilled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    canceled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_reservations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_product_id",
                table: "inventory_items",
                column: "product_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_order_id",
                table: "inventory_reservations",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_reservations_product_id",
                table: "inventory_reservations",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_items");

            migrationBuilder.DropTable(
                name: "inventory_reservations");
        }
    }
}
