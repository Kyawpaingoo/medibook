using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddXminConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No schema change: "xmin" is Postgres' built-in per-row system column, already
            // present on every table. It cannot be added as a real column — attempting to do
            // so fails with "column name "xmin" conflicts with a system column name". This
            // migration exists only to record the EF model-snapshot change (tbSlots/tbAppointments
            // now use xmin as an optimistic-concurrency token).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
