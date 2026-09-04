using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitPay.API.Migrations
{
    /// <inheritdoc />
    public partial class AddGcashTopUpSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gcash_topup_sessions",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<int>(type: "integer", nullable: false),
                    transaction_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    mobile_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    otp_attempts = table.Column<int>(type: "integer", nullable: false),
                    gcash_reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gcash_topup_sessions", x => x.session_id);
                    table.ForeignKey(
                        name: "FK_gcash_topup_sessions_cards_card_id",
                        column: x => x.card_id,
                        principalTable: "cards",
                        principalColumn: "card_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gcash_topup_sessions_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "transaction_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gcash_topup_sessions_card_id",
                table: "gcash_topup_sessions",
                column: "card_id");

            migrationBuilder.CreateIndex(
                name: "IX_gcash_topup_sessions_status",
                table: "gcash_topup_sessions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_gcash_topup_sessions_transaction_id",
                table: "gcash_topup_sessions",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_gcash_topup_sessions_user_id",
                table: "gcash_topup_sessions",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gcash_topup_sessions");
        }
    }
}
