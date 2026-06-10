using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JAA.Migrations
{
    /// <inheritdoc />
    public partial class AddDeclineReasonToQuotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeclineReason",
                table: "RepairQuotations",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeclineReason",
                table: "RepairQuotations");
        }
    }
}
