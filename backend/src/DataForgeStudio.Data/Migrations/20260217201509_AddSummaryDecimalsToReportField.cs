using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataForgeStudio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSummaryDecimalsToReportField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SummaryDecimals",
                table: "ReportFields",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SummaryDecimals",
                table: "ReportFields");
        }
    }
}
