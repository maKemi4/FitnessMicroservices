using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StatisticsService.Migrations
{
    /// <inheritdoc />
    public partial class InitialStatisticsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "statistics");

            migrationBuilder.CreateTable(
                name: "completed_trainings",
                schema: "statistics",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_completed_trainings", x => x.EventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_completed_trainings_UserId",
                schema: "statistics",
                table: "completed_trainings",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "completed_trainings",
                schema: "statistics");
        }
    }
}
