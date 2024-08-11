using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TekkenFrameData.Cli.Migrations;

/// <inheritdoc />
public partial class Init : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Configuration",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Configuration", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "TekkenCharacters",
            columns: table => new
            {
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                LinkToImage = table.Column<string>(type: "text", nullable: true),
                LastUpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TekkenCharacters", x => x.Name);
            });

        migrationBuilder.CreateTable(
            name: "TekkenMoves",
            columns: table => new
            {
                CharacterName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Command = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                StanceCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                StanceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                HeatEngage = table.Column<bool>(type: "boolean", nullable: false),
                HeatSmash = table.Column<bool>(type: "boolean", nullable: false),
                PowerCrush = table.Column<bool>(type: "boolean", nullable: false),
                Throw = table.Column<bool>(type: "boolean", nullable: false),
                Homing = table.Column<bool>(type: "boolean", nullable: false),
                Tornado = table.Column<bool>(type: "boolean", nullable: false),
                HeatBurst = table.Column<bool>(type: "boolean", nullable: false),
                RequiresHeat = table.Column<bool>(type: "boolean", nullable: false),
                HitLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                Damage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                StartUpFrame = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                BlockFrame = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                HitFrame = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                CounterHitFrame = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                Notes = table.Column<string>(type: "text", nullable: true),
                IsUserChanged = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TekkenMoves", x => new { x.CharacterName, x.Command });
                table.ForeignKey(
                    name: "FK_TekkenMoves_TekkenCharacters_CharacterName",
                    column: x => x.CharacterName,
                    principalTable: "TekkenCharacters",
                    principalColumn: "Name");
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Configuration");

        migrationBuilder.DropTable(
            name: "TekkenMoves");

        migrationBuilder.DropTable(
            name: "TekkenCharacters");
    }
}