using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Languag.io.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardReviewStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardReviewStates",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeckId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IntervalDays = table.Column<int>(type: "integer", nullable: false),
                    EaseFactor = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    RepetitionCount = table.Column<int>(type: "integer", nullable: false),
                    LapseCount = table.Column<int>(type: "integer", nullable: false),
                    TotalReviews = table.Column<int>(type: "integer", nullable: false),
                    CorrectReviews = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardReviewStates", x => new { x.UserId, x.CardId });
                    table.ForeignKey(
                        name: "FK_CardReviewStates_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardReviewStates_Decks_DeckId",
                        column: x => x.DeckId,
                        principalTable: "Decks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardReviewStates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardReviewStates_CardId",
                table: "CardReviewStates",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_CardReviewStates_DeckId",
                table: "CardReviewStates",
                column: "DeckId");

            migrationBuilder.CreateIndex(
                name: "IX_CardReviewStates_UserId_DeckId_DueAtUtc",
                table: "CardReviewStates",
                columns: new[] { "UserId", "DeckId", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CardReviewStates_UserId_DueAtUtc",
                table: "CardReviewStates",
                columns: new[] { "UserId", "DueAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardReviewStates");
        }
    }
}
