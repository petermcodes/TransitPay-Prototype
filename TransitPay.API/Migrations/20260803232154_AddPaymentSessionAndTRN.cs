using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitPay.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentSessionAndTRN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "driver_id",
                table: "transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "payment_session_id",
                table: "transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transaction_reference_number",
                table: "transactions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "payment_sessions",
                columns: table => new
                {
                    payment_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    origin_station_id = table.Column<int>(type: "integer", nullable: false),
                    destination_station_id = table.Column<int>(type: "integer", nullable: false),
                    fare = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_sessions", x => x.payment_session_id);
                    table.ForeignKey(
                        name: "FK_payment_sessions_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "cards",
                        principalColumn: "card_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_sessions_stations_destination_station_id",
                        column: x => x.destination_station_id,
                        principalTable: "stations",
                        principalColumn: "station_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_sessions_stations_origin_station_id",
                        column: x => x.origin_station_id,
                        principalTable: "stations",
                        principalColumn: "station_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_transactions_driver_id",
                table: "transactions",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_payment_session_id",
                table: "transactions",
                column: "payment_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_transaction_reference_number",
                table: "transactions",
                column: "transaction_reference_number",
                unique: true,
                filter: "transaction_reference_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payment_sessions_card_id",
                table: "payment_sessions",
                column: "card_id",
                unique: true,
                filter: "status IN (0, 1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_payment_sessions_destination_station_id",
                table: "payment_sessions",
                column: "destination_station_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_sessions_origin_station_id",
                table: "payment_sessions",
                column: "origin_station_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_sessions_user_id",
                table: "payment_sessions",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_payment_sessions_payment_session_id",
                table: "transactions",
                column: "payment_session_id",
                principalTable: "payment_sessions",
                principalColumn: "payment_session_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_users_driver_id",
                table: "transactions",
                column: "driver_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transactions_payment_sessions_payment_session_id",
                table: "transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_transactions_users_driver_id",
                table: "transactions");

            migrationBuilder.DropTable(
                name: "payment_sessions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_driver_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_payment_session_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_transaction_reference_number",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "driver_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "payment_session_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "transaction_reference_number",
                table: "transactions");
        }
    }
}
