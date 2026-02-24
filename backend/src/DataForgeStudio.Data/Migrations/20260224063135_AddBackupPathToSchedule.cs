using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataForgeStudio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBackupPathToSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DataSources_DefaultCount",
                table: "DataSources");

            migrationBuilder.AddColumn<string>(
                name: "BackupPath",
                table: "BackupSchedules",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackupPath",
                table: "BackupSchedules");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DataSources_DefaultCount",
                table: "DataSources",
                sql: "[IsDefault] = 0 OR NOT EXISTS (SELECT 1 FROM DataSources d2 WHERE d2.IsDefault = 1 AND d2.DataSourceId <> [DataSourceId])");
        }
    }
}
