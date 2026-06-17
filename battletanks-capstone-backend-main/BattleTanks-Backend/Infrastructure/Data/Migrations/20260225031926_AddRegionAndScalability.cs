using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BattleTanks_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRegionAndScalability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "GameSessions",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "LATAM");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_Region",
                table: "GameSessions",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_Region_CreatedAt",
                table: "GameSessions",
                columns: new[] { "Region", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameSessions_Region_CreatedAt",
                table: "GameSessions");

            migrationBuilder.DropIndex(
                name: "IX_GameSessions_Region",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "GameSessions");
        }
    }
}
