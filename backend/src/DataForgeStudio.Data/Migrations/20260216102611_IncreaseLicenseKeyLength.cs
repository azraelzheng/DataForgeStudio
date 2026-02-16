using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataForgeStudio.Data.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseLicenseKeyLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LicenseKey",
                table: "Licenses",
                type: "nvarchar(max)",
                maxLength: 4096,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "LicenseKey",
                table: "Licenses",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 4096);
        }
    }
}
