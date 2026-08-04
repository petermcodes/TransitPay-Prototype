using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TransitPay.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTripManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "trip_id",
                table: "transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "trips",
                columns: table => new
                {
                    trip_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    driver_id = table.Column<int>(type: "integer", nullable: false),
                    bus_id = table.Column<int>(type: "integer", nullable: true),
                    origin_station_id = table.Column<int>(type: "integer", nullable: false),
                    final_destination_station_id = table.Column<int>(type: "integer", nullable: false),
                    route_name = table.Column<string>(type: "text", nullable: false),
                    trip_status = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    passenger_count = table.Column<int>(type: "integer", nullable: false),
                    total_revenue = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trips", x => x.trip_id);
                    table.ForeignKey(
                        name: "FK_trips_stations_final_destination_station_id",
                        column: x => x.final_destination_station_id,
                        principalTable: "stations",
                        principalColumn: "station_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trips_stations_origin_station_id",
                        column: x => x.origin_station_id,
                        principalTable: "stations",
                        principalColumn: "station_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trips_users_driver_id",
                        column: x => x.driver_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_trip_id",
                table: "transactions",
                column: "trip_id");

            migrationBuilder.CreateIndex(
                name: "IX_trips_driver_id",
                table: "trips",
                column: "driver_id",
                unique: true,
                filter: "trip_status = 1");

            migrationBuilder.CreateIndex(
                name: "IX_trips_final_destination_station_id",
                table: "trips",
                column: "final_destination_station_id");

            migrationBuilder.CreateIndex(
                name: "IX_trips_origin_station_id",
                table: "trips",
                column: "origin_station_id");

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_trips_trip_id",
                table: "transactions",
                column: "trip_id",
                principalTable: "trips",
                principalColumn: "trip_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transactions_trips_trip_id",
                table: "transactions");

            migrationBuilder.DropTable(
                name: "trips");

            migrationBuilder.DropIndex(
                name: "IX_transactions_trip_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "trip_id",
                table: "transactions");
        }
    }
}
