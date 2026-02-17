using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataForgeStudio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSummaryTypeToReportField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SummaryType",
                table: "ReportFields",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SummaryType",
                table: "ReportFields");
        }
    }
}
