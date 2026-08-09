using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransitPay.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTrnCounterTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dedicated counter table for atomic TRN generation.
            // Each payment/top-up performs a single INSERT ... ON CONFLICT ... RETURNING
            // against this table, guaranteeing unique sequence numbers under concurrency.
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS trn_counters (
                    counter_date date NOT NULL PRIMARY KEY,
                    last_sequence integer NOT NULL
                );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS trn_counters;");
        }
    }
}
