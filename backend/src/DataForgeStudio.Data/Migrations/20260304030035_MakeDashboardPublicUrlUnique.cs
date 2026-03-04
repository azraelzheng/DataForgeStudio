using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataForgeStudio.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeDashboardPublicUrlUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dashboards_PublicUrl",
                table: "Dashboards");

            migrationBuilder.CreateIndex(
                name: "IX_Dashboards_PublicUrl",
                table: "Dashboards",
                column: "PublicUrl",
                unique: true,
                filter: "[PublicUrl] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Dashboards_PublicUrl",
                table: "Dashboards");

            migrationBuilder.CreateIndex(
                name: "IX_Dashboards_PublicUrl",
                table: "Dashboards",
                column: "PublicUrl",
                filter: "[PublicUrl] IS NOT NULL");
        }
    }
}
