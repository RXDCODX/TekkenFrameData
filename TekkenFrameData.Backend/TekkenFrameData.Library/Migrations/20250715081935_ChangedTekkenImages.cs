using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TekkenFrameData.Cli.Migrations;

/// <inheritdoc />
public partial class ChangedTekkenImages : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "Image",
            table: "tekken_characters",
            type: "bytea",
            nullable: true
        );

        migrationBuilder.AddColumn<string>(
            name: "ImageExtension",
            table: "tekken_characters",
            type: "text",
            nullable: true
        );

        migrationBuilder.AddColumn<string>(
            name: "PageUrl",
            table: "tekken_characters",
            type: "text",
            nullable: true
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Image", table: "tekken_characters");

        migrationBuilder.DropColumn(name: "ImageExtension", table: "tekken_characters");

        migrationBuilder.DropColumn(name: "PageUrl", table: "tekken_characters");
    }
}
