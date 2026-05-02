using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Languag.io.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiDeckGenerationJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiDeckGenerationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TargetLanguage = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    NativeLanguage = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Difficulty = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RequestedCardCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedDeckId = table.Column<Guid>(type: "uuid", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiDeckGenerationJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiDeckGenerationJobs_Decks_CreatedDeckId",
                        column: x => x.CreatedDeckId,
                        principalTable: "Decks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AiDeckGenerationJobs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiDeckGenerationJobs_CreatedDeckId",
                table: "AiDeckGenerationJobs",
                column: "CreatedDeckId");

            migrationBuilder.CreateIndex(
                name: "IX_AiDeckGenerationJobs_Status_CreatedAtUtc",
                table: "AiDeckGenerationJobs",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AiDeckGenerationJobs_UserId_CreatedAtUtc",
                table: "AiDeckGenerationJobs",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiDeckGenerationJobs");
        }
    }
}
