using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TransitPay.API.Migrations
{
    /// <inheritdoc />
    public partial class Phase6BusinessLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_fare_rules_stations_destination_station_id",
                table: "fare_rules");

            migrationBuilder.DropForeignKey(
                name: "FK_fare_rules_stations_origin_station_id",
                table: "fare_rules");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_stations_station_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_fare_rules_origin_station_id",
                table: "fare_rules");

            migrationBuilder.AddColumn<int>(
                name: "fare_id",
                table: "transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "origin_station_id",
                table: "transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_number",
                table: "transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "passenger_type",
                table: "cards",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "qr_codes",
                columns: table => new
                {
                    qr_code_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    card_id = table.Column<int>(type: "integer", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qr_codes", x => x.qr_code_id);
                    table.ForeignKey(
                        name: "FK_qr_codes_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "cards",
                        principalColumn: "card_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_mobile_number",
                table: "users",
                column: "mobile_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_fare_id",
                table: "transactions",
                column: "fare_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_origin_station_id",
                table: "transactions",
                column: "origin_station_id");

            migrationBuilder.CreateIndex(
                name: "IX_fare_rules_origin_station_id_destination_station_id_vehicle~",
                table: "fare_rules",
                columns: new[] { "origin_station_id", "destination_station_id", "vehicle_type", "passenger_type" },
                unique: true,
                filter: "is_active = true AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_cards_card_number",
                table: "cards",
                column: "card_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qr_codes_card_id",
                table: "qr_codes",
                column: "card_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qr_codes_card_id_is_active",
                table: "qr_codes",
                columns: new[] { "card_id", "is_active" },
                unique: true,
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "IX_qr_codes_token",
                table: "qr_codes",
                column: "token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_fare_rules_stations_destination_station_id",
                table: "fare_rules",
                column: "destination_station_id",
                principalTable: "stations",
                principalColumn: "station_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_fare_rules_stations_origin_station_id",
                table: "fare_rules",
                column: "origin_station_id",
                principalTable: "stations",
                principalColumn: "station_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_fare_rules_fare_id",
                table: "transactions",
                column: "fare_id",
                principalTable: "fare_rules",
                principalColumn: "fare_id");

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_stations_origin_station_id",
                table: "transactions",
                column: "origin_station_id",
                principalTable: "stations",
                principalColumn: "station_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_stations_station_id",
                table: "transactions",
                column: "station_id",
                principalTable: "stations",
                principalColumn: "station_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_fare_rules_stations_destination_station_id",
                table: "fare_rules");

            migrationBuilder.DropForeignKey(
                name: "FK_fare_rules_stations_origin_station_id",
                table: "fare_rules");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_fare_rules_fare_id",
                table: "transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_stations_origin_station_id",
                table: "transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_stations_station_id",
                table: "transactions");

            migrationBuilder.DropTable(
                name: "qr_codes");

            migrationBuilder.DropIndex(
                name: "IX_users_mobile_number",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_transactions_fare_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_origin_station_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_fare_rules_origin_station_id_destination_station_id_vehicle~",
                table: "fare_rules");

            migrationBuilder.DropIndex(
                name: "IX_cards_card_number",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "fare_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "origin_station_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "reference_number",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "passenger_type",
                table: "cards");

            migrationBuilder.CreateIndex(
                name: "IX_fare_rules_origin_station_id",
                table: "fare_rules",
                column: "origin_station_id");

            migrationBuilder.AddForeignKey(
                name: "FK_fare_rules_stations_destination_station_id",
                table: "fare_rules",
                column: "destination_station_id",
                principalTable: "stations",
                principalColumn: "station_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_fare_rules_stations_origin_station_id",
                table: "fare_rules",
                column: "origin_station_id",
                principalTable: "stations",
                principalColumn: "station_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_stations_station_id",
                table: "transactions",
                column: "station_id",
                principalTable: "stations",
                principalColumn: "station_id");
        }
    }
}
