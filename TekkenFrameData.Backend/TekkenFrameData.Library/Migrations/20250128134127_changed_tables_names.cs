using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TekkenFrameData.Cli.Migrations;

/// <inheritdoc />
public partial class changed_tables_names : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_TekkenMoves_TekkenCharacters_CharacterName",
            table: "TekkenMoves"
        );

        migrationBuilder.DropPrimaryKey(name: "PK_TekkenMoves", table: "TekkenMoves");

        migrationBuilder.DropPrimaryKey(name: "PK_TekkenCharacters", table: "TekkenCharacters");

        migrationBuilder.RenameTable(name: "TekkenMoves", newName: "tekken_moves");

        migrationBuilder.RenameTable(name: "TekkenCharacters", newName: "tekken_characters");

        migrationBuilder.AddPrimaryKey(
            name: "PK_tekken_moves",
            table: "tekken_moves",
            columns: new[] { "CharacterName", "Command" }
        );

        migrationBuilder.AddPrimaryKey(
            name: "PK_tekken_characters",
            table: "tekken_characters",
            column: "Name"
        );

        migrationBuilder.AddForeignKey(
            name: "FK_tekken_moves_tekken_characters_CharacterName",
            table: "tekken_moves",
            column: "CharacterName",
            principalTable: "tekken_characters",
            principalColumn: "Name"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_tekken_moves_tekken_characters_CharacterName",
            table: "tekken_moves"
        );

        migrationBuilder.DropPrimaryKey(name: "PK_tekken_moves", table: "tekken_moves");

        migrationBuilder.DropPrimaryKey(name: "PK_tekken_characters", table: "tekken_characters");

        migrationBuilder.RenameTable(name: "tekken_moves", newName: "TekkenMoves");

        migrationBuilder.RenameTable(name: "tekken_characters", newName: "TekkenCharacters");

        migrationBuilder.AddPrimaryKey(
            name: "PK_TekkenMoves",
            table: "TekkenMoves",
            columns: new[] { "CharacterName", "Command" }
        );

        migrationBuilder.AddPrimaryKey(
            name: "PK_TekkenCharacters",
            table: "TekkenCharacters",
            column: "Name"
        );

        migrationBuilder.AddForeignKey(
            name: "FK_TekkenMoves_TekkenCharacters_CharacterName",
            table: "TekkenMoves",
            column: "CharacterName",
            principalTable: "TekkenCharacters",
            principalColumn: "Name"
        );
    }
}
