using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataForgeStudio.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDapingProjectsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DapingProjects",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublicUrl = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DapingProjects", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_DapingProjects_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DapingProjects_CreatedBy",
                table: "DapingProjects",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DapingProjects_CreatedTime",
                table: "DapingProjects",
                column: "CreatedTime");

            migrationBuilder.CreateIndex(
                name: "IX_DapingProjects_PublicUrl",
                table: "DapingProjects",
                column: "PublicUrl",
                unique: true,
                filter: "[PublicUrl] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DapingProjects_State",
                table: "DapingProjects",
                column: "State");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DapingProjects");
        }
    }
}
