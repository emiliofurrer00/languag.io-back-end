using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Languag.io.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSagas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sagas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sagas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sagas_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SagaChapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SagaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SagaChapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SagaChapters_Sagas_SagaId",
                        column: x => x.SagaId,
                        principalTable: "Sagas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SagaLessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SagaChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeckId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SagaLessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SagaLessons_Decks_DeckId",
                        column: x => x.DeckId,
                        principalTable: "Decks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SagaLessons_SagaChapters_SagaChapterId",
                        column: x => x.SagaChapterId,
                        principalTable: "SagaChapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SagaProgresses",
                columns: table => new
                {
                    SagaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastStudiedLessonId = table.Column<Guid>(type: "uuid", nullable: true),
                    HighestCompletedLessonId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastStudiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SagaProgresses", x => new { x.SagaId, x.UserId });
                    table.ForeignKey(
                        name: "FK_SagaProgresses_SagaLessons_HighestCompletedLessonId",
                        column: x => x.HighestCompletedLessonId,
                        principalTable: "SagaLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SagaProgresses_SagaLessons_LastStudiedLessonId",
                        column: x => x.LastStudiedLessonId,
                        principalTable: "SagaLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SagaProgresses_Sagas_SagaId",
                        column: x => x.SagaId,
                        principalTable: "Sagas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SagaProgresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SagaChapters_SagaId_Order",
                table: "SagaChapters",
                columns: new[] { "SagaId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_SagaLessons_DeckId",
                table: "SagaLessons",
                column: "DeckId");

            migrationBuilder.CreateIndex(
                name: "IX_SagaLessons_SagaChapterId_Order",
                table: "SagaLessons",
                columns: new[] { "SagaChapterId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_SagaProgresses_HighestCompletedLessonId",
                table: "SagaProgresses",
                column: "HighestCompletedLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_SagaProgresses_LastStudiedLessonId",
                table: "SagaProgresses",
                column: "LastStudiedLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_SagaProgresses_UserId_LastStudiedAtUtc",
                table: "SagaProgresses",
                columns: new[] { "UserId", "LastStudiedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Sagas_OwnerId_UpdatedAtUtc",
                table: "Sagas",
                columns: new[] { "OwnerId", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Sagas_Visibility",
                table: "Sagas",
                column: "Visibility");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SagaProgresses");

            migrationBuilder.DropTable(
                name: "SagaLessons");

            migrationBuilder.DropTable(
                name: "SagaChapters");

            migrationBuilder.DropTable(
                name: "Sagas");
        }
    }
}
