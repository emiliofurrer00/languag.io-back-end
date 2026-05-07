using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Languag.io.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiSagaGenerationJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiSagaGenerationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TargetLanguage = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    NativeLanguage = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Difficulty = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RequestedDeckCount = table.Column<int>(type: "integer", nullable: false),
                    RequestedCardsPerDeck = table.Column<int>(type: "integer", nullable: false),
                    RequestedMultiChoiceCountPerDeck = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IncludeAudio = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AudioStatus = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedSagaId = table.Column<Guid>(type: "uuid", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UsageWeekStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiSagaGenerationJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiSagaGenerationJobs_Sagas_CreatedSagaId",
                        column: x => x.CreatedSagaId,
                        principalTable: "Sagas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AiSagaGenerationJobs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiSagaGenerationJobs_CreatedSagaId",
                table: "AiSagaGenerationJobs",
                column: "CreatedSagaId");

            migrationBuilder.CreateIndex(
                name: "IX_AiSagaGenerationJobs_Status_CreatedAtUtc",
                table: "AiSagaGenerationJobs",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AiSagaGenerationJobs_UserId_CreatedAtUtc",
                table: "AiSagaGenerationJobs",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AiSagaGenerationJobs_UserId_UsageWeekStartUtc",
                table: "AiSagaGenerationJobs",
                columns: new[] { "UserId", "UsageWeekStartUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiSagaGenerationJobs");
        }
    }
}
