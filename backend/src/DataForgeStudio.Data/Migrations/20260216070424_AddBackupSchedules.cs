using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataForgeStudio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBackupSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "BackupRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BackupSchedules",
                columns: table => new
                {
                    ScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ScheduleType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecurringDays = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ScheduledTime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    OnceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetentionCount = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastRunTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRunTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupSchedules", x => x.ScheduleId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackupSchedules_IsEnabled",
                table: "BackupSchedules",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_BackupSchedules_NextRunTime",
                table: "BackupSchedules",
                column: "NextRunTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackupSchedules");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "BackupRecords");
        }
    }
}
