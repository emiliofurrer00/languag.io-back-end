using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Languag.io.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeckVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudySessionResponses_Cards_CardId",
                table: "StudySessionResponses");

            migrationBuilder.AddColumn<Guid>(
                name: "DeckVersionId",
                table: "StudySessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CardId",
                table: "StudySessionResponses",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "DeckVersionCardId",
                table: "StudySessionResponses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentVersionNumber",
                table: "Decks",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "DeckVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeckId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeckVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeckVersions_Decks_DeckId",
                        column: x => x.DeckId,
                        principalTable: "Decks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeckVersions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DeckVersionCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeckVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalCardId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FrontText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    BackText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ExampleSentence = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    FrontAudioAssetId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeckVersionCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeckVersionCards_AudioAssets_FrontAudioAssetId",
                        column: x => x.FrontAudioAssetId,
                        principalTable: "AudioAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DeckVersionCards_DeckVersions_DeckVersionId",
                        column: x => x.DeckVersionId,
                        principalTable: "DeckVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeckVersionCardChoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeckVersionCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalChoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeckVersionCardChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeckVersionCardChoices_DeckVersionCards_DeckVersionCardId",
                        column: x => x.DeckVersionCardId,
                        principalTable: "DeckVersionCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_DeckVersionId",
                table: "StudySessions",
                column: "DeckVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessionResponses_DeckVersionCardId",
                table: "StudySessionResponses",
                column: "DeckVersionCardId");

            migrationBuilder.CreateIndex(
                name: "IX_DeckVersionCardChoices_DeckVersionCardId_Order",
                table: "DeckVersionCardChoices",
                columns: new[] { "DeckVersionCardId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_DeckVersionCardChoices_OriginalChoiceId",
                table: "DeckVersionCardChoices",
                column: "OriginalChoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeckVersionCards_DeckVersionId_Order",
                table: "DeckVersionCards",
                columns: new[] { "DeckVersionId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_DeckVersionCards_FrontAudioAssetId",
                table: "DeckVersionCards",
                column: "FrontAudioAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_DeckVersionCards_OriginalCardId",
                table: "DeckVersionCards",
                column: "OriginalCardId");

            migrationBuilder.CreateIndex(
                name: "IX_DeckVersions_CreatedByUserId",
                table: "DeckVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeckVersions_DeckId_VersionNumber",
                table: "DeckVersions",
                columns: new[] { "DeckId", "VersionNumber" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "DeckVersions" (
                    "Id",
                    "DeckId",
                    "VersionNumber",
                    "Title",
                    "Description",
                    "Category",
                    "Color",
                    "Visibility",
                    "CreatedByUserId",
                    "CreatedAtUtc")
                SELECT
                    md5(d."Id"::text || ':deck-version:1')::uuid,
                    d."Id",
                    1,
                    d."Title",
                    d."Description",
                    d."Category",
                    d."Color",
                    d."Visibility",
                    d."OwnerId",
                    d."UpdatedAtUtc"
                FROM "Decks" d
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "DeckVersions" existing
                    WHERE existing."DeckId" = d."Id"
                      AND existing."VersionNumber" = 1
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO "DeckVersionCards" (
                    "Id",
                    "DeckVersionId",
                    "OriginalCardId",
                    "Type",
                    "FrontText",
                    "BackText",
                    "ExampleSentence",
                    "Order",
                    "FrontAudioAssetId")
                SELECT
                    md5(c."Id"::text || ':deck-version-card:1')::uuid,
                    md5(c."DeckId"::text || ':deck-version:1')::uuid,
                    c."Id",
                    c."Type",
                    c."FrontText",
                    c."BackText",
                    c."ExampleSentence",
                    c."Order",
                    c."FrontAudioAssetId"
                FROM "Cards" c
                WHERE EXISTS (
                    SELECT 1
                    FROM "DeckVersions" version
                    WHERE version."Id" = md5(c."DeckId"::text || ':deck-version:1')::uuid
                )
                AND NOT EXISTS (
                    SELECT 1
                    FROM "DeckVersionCards" existing
                    WHERE existing."Id" = md5(c."Id"::text || ':deck-version-card:1')::uuid
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO "DeckVersionCardChoices" (
                    "Id",
                    "DeckVersionCardId",
                    "OriginalChoiceId",
                    "Text",
                    "IsCorrect",
                    "Order")
                SELECT
                    md5(choice."Id"::text || ':deck-version-choice:1')::uuid,
                    md5(choice."CardId"::text || ':deck-version-card:1')::uuid,
                    choice."Id",
                    choice."Text",
                    choice."IsCorrect",
                    choice."Order"
                FROM "CardChoices" choice
                WHERE EXISTS (
                    SELECT 1
                    FROM "DeckVersionCards" version_card
                    WHERE version_card."Id" = md5(choice."CardId"::text || ':deck-version-card:1')::uuid
                )
                AND NOT EXISTS (
                    SELECT 1
                    FROM "DeckVersionCardChoices" existing
                    WHERE existing."Id" = md5(choice."Id"::text || ':deck-version-choice:1')::uuid
                );
                """);

            migrationBuilder.Sql("""
                UPDATE "StudySessions" session
                SET "DeckVersionId" = md5(session."DeckId"::text || ':deck-version:1')::uuid
                WHERE session."DeckVersionId" IS NULL
                  AND EXISTS (
                      SELECT 1
                      FROM "DeckVersions" version
                      WHERE version."Id" = md5(session."DeckId"::text || ':deck-version:1')::uuid
                  );
                """);

            migrationBuilder.Sql("""
                UPDATE "StudySessionResponses" response
                SET "DeckVersionCardId" = md5(response."CardId"::text || ':deck-version-card:1')::uuid
                WHERE response."CardId" IS NOT NULL
                  AND response."DeckVersionCardId" IS NULL
                  AND EXISTS (
                      SELECT 1
                      FROM "DeckVersionCards" version_card
                      WHERE version_card."Id" = md5(response."CardId"::text || ':deck-version-card:1')::uuid
                  );
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_StudySessionResponses_Cards_CardId",
                table: "StudySessionResponses",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StudySessionResponses_DeckVersionCards_DeckVersionCardId",
                table: "StudySessionResponses",
                column: "DeckVersionCardId",
                principalTable: "DeckVersionCards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StudySessions_DeckVersions_DeckVersionId",
                table: "StudySessions",
                column: "DeckVersionId",
                principalTable: "DeckVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudySessionResponses_Cards_CardId",
                table: "StudySessionResponses");

            migrationBuilder.DropForeignKey(
                name: "FK_StudySessionResponses_DeckVersionCards_DeckVersionCardId",
                table: "StudySessionResponses");

            migrationBuilder.DropForeignKey(
                name: "FK_StudySessions_DeckVersions_DeckVersionId",
                table: "StudySessions");

            migrationBuilder.DropTable(
                name: "DeckVersionCardChoices");

            migrationBuilder.DropTable(
                name: "DeckVersionCards");

            migrationBuilder.DropTable(
                name: "DeckVersions");

            migrationBuilder.DropIndex(
                name: "IX_StudySessions_DeckVersionId",
                table: "StudySessions");

            migrationBuilder.DropIndex(
                name: "IX_StudySessionResponses_DeckVersionCardId",
                table: "StudySessionResponses");

            migrationBuilder.DropColumn(
                name: "DeckVersionId",
                table: "StudySessions");

            migrationBuilder.DropColumn(
                name: "DeckVersionCardId",
                table: "StudySessionResponses");

            migrationBuilder.DropColumn(
                name: "CurrentVersionNumber",
                table: "Decks");

            migrationBuilder.Sql("""
                DELETE FROM "StudySessionResponses"
                WHERE "CardId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CardId",
                table: "StudySessionResponses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StudySessionResponses_Cards_CardId",
                table: "StudySessionResponses",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
