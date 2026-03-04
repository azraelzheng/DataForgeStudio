using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataForgeStudio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dashboards",
                columns: table => new
                {
                    DashboardId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Theme = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "dark"),
                    RefreshInterval = table.Column<int>(type: "int", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    LayoutConfig = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThemeConfig = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    PublicUrl = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AuthorizedUserIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    BackgroundColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "#0a1628"),
                    BackgroundImage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dashboards", x => x.DashboardId);
                    table.ForeignKey(
                        name: "FK_Dashboards_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DashboardWidgets",
                columns: table => new
                {
                    WidgetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DashboardId = table.Column<int>(type: "int", nullable: false),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    WidgetType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PositionX = table.Column<int>(type: "int", nullable: false),
                    PositionY = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    DataConfig = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StyleConfig = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardWidgets", x => x.WidgetId);
                    table.ForeignKey(
                        name: "FK_DashboardWidgets_Dashboards_DashboardId",
                        column: x => x.DashboardId,
                        principalTable: "Dashboards",
                        principalColumn: "DashboardId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DashboardWidgets_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "ReportId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WidgetRules",
                columns: table => new
                {
                    RuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WidgetId = table.Column<int>(type: "int", nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Field = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Operator = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActionValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WidgetRules", x => x.RuleId);
                    table.ForeignKey(
                        name: "FK_WidgetRules_DashboardWidgets_WidgetId",
                        column: x => x.WidgetId,
                        principalTable: "DashboardWidgets",
                        principalColumn: "WidgetId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_DataSources_DefaultCount",
                table: "DataSources",
                sql: "[IsDefault] = 0 OR NOT EXISTS (SELECT 1 FROM DataSources d2 WHERE d2.IsDefault = 1 AND d2.DataSourceId <> [DataSourceId])");

            migrationBuilder.CreateIndex(
                name: "IX_Dashboards_CreatedBy",
                table: "Dashboards",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Dashboards_CreatedTime",
                table: "Dashboards",
                column: "CreatedTime");

            migrationBuilder.CreateIndex(
                name: "IX_Dashboards_IsPublic",
                table: "Dashboards",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_Dashboards_PublicUrl",
                table: "Dashboards",
                column: "PublicUrl",
                filter: "[PublicUrl] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Dashboards_Status",
                table: "Dashboards",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardWidgets_DashboardId_ReportId",
                table: "DashboardWidgets",
                columns: new[] { "DashboardId", "ReportId" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardWidgets_ReportId",
                table: "DashboardWidgets",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_WidgetRules_WidgetId",
                table: "WidgetRules",
                column: "WidgetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WidgetRules");

            migrationBuilder.DropTable(
                name: "DashboardWidgets");

            migrationBuilder.DropTable(
                name: "Dashboards");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DataSources_DefaultCount",
                table: "DataSources");
        }
    }
}
