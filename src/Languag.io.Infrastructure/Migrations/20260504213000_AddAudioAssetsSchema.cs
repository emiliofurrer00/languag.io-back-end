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
            migrationBuilder.Sql("""
                ALTER TABLE "AiDeckGenerationJobs"
                ADD COLUMN IF NOT EXISTS "AudioStatus" integer NOT NULL DEFAULT 1;

                ALTER TABLE "AiDeckGenerationJobs"
                ADD COLUMN IF NOT EXISTS "IncludeAudio" boolean NOT NULL DEFAULT false;

                ALTER TABLE "Cards"
                ADD COLUMN IF NOT EXISTS "FrontAudioAssetId" uuid;

                CREATE TABLE IF NOT EXISTS "AudioAssets" (
                    "Id" uuid NOT NULL,
                    "TextHash" character varying(64) NOT NULL,
                    "NormalizedText" character varying(1000) NOT NULL,
                    "LanguageCode" character varying(20) NOT NULL,
                    "Provider" character varying(40) NOT NULL,
                    "Model" character varying(80) NOT NULL,
                    "Voice" character varying(40) NOT NULL,
                    "Speed" numeric(4,2) NOT NULL,
                    "InstructionsHash" character varying(64) NOT NULL,
                    "StorageKey" character varying(512) NOT NULL,
                    "PublicUrl" character varying(1000) NOT NULL,
                    "Status" integer NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_AudioAssets" PRIMARY KEY ("Id")
                );

                CREATE INDEX IF NOT EXISTS "IX_Cards_FrontAudioAssetId"
                ON "Cards" ("FrontAudioAssetId");

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_AudioAssets_TextHash"
                ON "AudioAssets" ("TextHash");

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_Cards_AudioAssets_FrontAudioAssetId'
                    ) THEN
                        ALTER TABLE "Cards"
                        ADD CONSTRAINT "FK_Cards_AudioAssets_FrontAudioAssetId"
                        FOREIGN KEY ("FrontAudioAssetId")
                        REFERENCES "AudioAssets" ("Id")
                        ON DELETE SET NULL;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Cards"
                DROP CONSTRAINT IF EXISTS "FK_Cards_AudioAssets_FrontAudioAssetId";

                DROP TABLE IF EXISTS "AudioAssets";

                DROP INDEX IF EXISTS "IX_Cards_FrontAudioAssetId";

                ALTER TABLE "Cards"
                DROP COLUMN IF EXISTS "FrontAudioAssetId";

                ALTER TABLE "AiDeckGenerationJobs"
                DROP COLUMN IF EXISTS "AudioStatus";

                ALTER TABLE "AiDeckGenerationJobs"
                DROP COLUMN IF EXISTS "IncludeAudio";
                """);
        }
    }
}
