using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TekkenFrameData.Cli.Migrations;

/// <inheritdoc />
public partial class ChangeMaxLenghtString : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "StartUpFrame",
            table: "tekken_moves",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(30)",
            oldMaxLength: 30,
            oldNullable: true
        );

        migrationBuilder.AlterColumn<string>(
            name: "HitLevel",
            table: "tekken_moves",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20,
            oldNullable: true
        );

        migrationBuilder.AlterColumn<string>(
            name: "HitFrame",
            table: "tekken_moves",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20,
            oldNullable: true
        );

        migrationBuilder.AlterColumn<string>(
            name: "Damage",
            table: "tekken_moves",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20,
            oldNullable: true
        );

        migrationBuilder.AlterColumn<string>(
            name: "BlockFrame",
            table: "tekken_moves",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(20)",
            oldMaxLength: 20,
            oldNullable: true
        );

        migrationBuilder.AlterColumn<string>(
            name: "Command",
            table: "tekken_moves",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(30)",
            oldMaxLength: 30
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "StartUpFrame",
            table: "tekken_moves",
            type: "character varying(30)",
            maxLength: 30,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldNullable: true
        );

        migrationBuilder.AlterColumn<string>(
            name: "HitLevel",
            table: "tekken_moves",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldNullable: true
        );

        migrationBuilder.AlterColumn<string>(
            name: "HitFrame",
            table: "tekken_moves",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldNullable: true
        );

        migrationBuilder.AlterColumn<string>(
            name: "Damage",
            table: "tekken_moves",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldNullable: true
        );

        migrationBuilder.AlterColumn<string>(
            name: "BlockFrame",
            table: "tekken_moves",
            type: "character varying(20)",
            maxLength: 20,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldNullable: true
        );

        migrationBuilder.AlterColumn<string>(
            name: "Command",
            table: "tekken_moves",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100
        );
    }
}
