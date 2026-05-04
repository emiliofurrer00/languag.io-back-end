using System;
using Languag.io.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Languag.io.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260504213000_AddAudioAssetsSchema")]
    public partial class AddAudioAssetsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AudioStatus",
                table: "AiDeckGenerationJobs",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeAudio",
                table: "AiDeckGenerationJobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "FrontAudioAssetId",
                table: "Cards",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AudioAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TextHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NormalizedText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Model = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Voice = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Speed = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    InstructionsHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PublicUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioAssets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_FrontAudioAssetId",
                table: "Cards",
                column: "FrontAudioAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AudioAssets_TextHash",
                table: "AudioAssets",
                column: "TextHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_AudioAssets_FrontAudioAssetId",
                table: "Cards",
                column: "FrontAudioAssetId",
                principalTable: "AudioAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_AudioAssets_FrontAudioAssetId",
                table: "Cards");

            migrationBuilder.DropTable(
                name: "AudioAssets");

            migrationBuilder.DropIndex(
                name: "IX_Cards_FrontAudioAssetId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "FrontAudioAssetId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "AudioStatus",
                table: "AiDeckGenerationJobs");

            migrationBuilder.DropColumn(
                name: "IncludeAudio",
                table: "AiDeckGenerationJobs");
        }
    }
}
