using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BattleTanks_Backend.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FinalVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Agregar columna FirebaseUid
            migrationBuilder.AddColumn<string>(
                name: "FirebaseUid",
                table: "Players",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            // Agregar columna FirstName
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Players",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Agregar columna LastName
            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Players",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Eliminar columna PasswordHash que ya no se usa con Firebase Auth
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Players");

            // Índice único para FirebaseUid
            migrationBuilder.CreateIndex(
                name: "IX_Players_FirebaseUid",
                table: "Players",
                column: "FirebaseUid",
                unique: true);

            // Índice para TotalScore (leaderboard)
            migrationBuilder.CreateIndex(
                name: "IX_Players_TotalScore",
                table: "Players",
                column: "TotalScore");

            // Crear tabla ChatMessages
            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_GameSessions_RoomId",
                        column: x => x.RoomId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Crear tabla RoomPlayers
            migrationBuilder.CreateTable(
                name: "RoomPlayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomPlayers_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoomPlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Índices para ChatMessages
            migrationBuilder.CreateIndex(name: "IX_ChatMessages_PlayerId", table: "ChatMessages", column: "PlayerId");
            migrationBuilder.CreateIndex(name: "IX_ChatMessages_RoomId", table: "ChatMessages", column: "RoomId");
            migrationBuilder.CreateIndex(name: "IX_ChatMessages_Timestamp", table: "ChatMessages", column: "Timestamp");
            migrationBuilder.CreateIndex(name: "IX_ChatMessages_RoomId_Timestamp", table: "ChatMessages", columns: new[] { "RoomId", "Timestamp" });

            // Índices para RoomPlayers
            migrationBuilder.CreateIndex(name: "IX_RoomPlayers_PlayerId", table: "RoomPlayers", column: "PlayerId");
            migrationBuilder.CreateIndex(name: "IX_RoomPlayers_SessionId", table: "RoomPlayers", column: "SessionId");
            migrationBuilder.CreateIndex(name: "IX_RoomPlayers_SessionId_IsActive", table: "RoomPlayers", columns: new[] { "SessionId", "IsActive" });
            migrationBuilder.CreateIndex(name: "IX_RoomPlayers_SessionId_PlayerId", table: "RoomPlayers", columns: new[] { "SessionId", "PlayerId" }, unique: true);

            // Índices adicionales para Scores
            migrationBuilder.CreateIndex(name: "IX_Scores_PlayerId_AchievedAt", table: "Scores", columns: new[] { "PlayerId", "AchievedAt" });
            migrationBuilder.CreateIndex(name: "IX_Scores_SessionId_Points", table: "Scores", columns: new[] { "SessionId", "Points" });

            // Índice adicional para GameSessions
            migrationBuilder.CreateIndex(name: "IX_GameSessions_CreatedAt", table: "GameSessions", column: "CreatedAt");
            migrationBuilder.CreateIndex(name: "IX_GameSessions_Status", table: "GameSessions", column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ChatMessages");
            migrationBuilder.DropTable(name: "RoomPlayers");

            migrationBuilder.DropIndex(name: "IX_Players_FirebaseUid", table: "Players");
            migrationBuilder.DropIndex(name: "IX_Players_TotalScore", table: "Players");
            migrationBuilder.DropIndex(name: "IX_Scores_PlayerId_AchievedAt", table: "Scores");
            migrationBuilder.DropIndex(name: "IX_Scores_SessionId_Points", table: "Scores");
            migrationBuilder.DropIndex(name: "IX_GameSessions_CreatedAt", table: "GameSessions");
            migrationBuilder.DropIndex(name: "IX_GameSessions_Status", table: "GameSessions");

            migrationBuilder.DropColumn(name: "FirebaseUid", table: "Players");
            migrationBuilder.DropColumn(name: "FirstName", table: "Players");
            migrationBuilder.DropColumn(name: "LastName", table: "Players");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Players",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }
    }
}
