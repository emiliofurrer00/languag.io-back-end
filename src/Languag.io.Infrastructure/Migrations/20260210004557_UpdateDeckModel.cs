using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Languag.io.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeckModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LanguageCode",
                table: "Decks",
                newName: "Color");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Decks",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Decks");

            migrationBuilder.RenameColumn(
                name: "Color",
                table: "Decks",
                newName: "LanguageCode");
        }
    }
}
