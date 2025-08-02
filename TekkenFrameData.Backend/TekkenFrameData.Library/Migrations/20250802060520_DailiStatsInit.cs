using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TekkenFrameData.Cli.Migrations;

/// <inheritdoc />
public partial class DailiStatsInit : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "WankWavuPlayers",
            columns: table => new
            {
                TwitchId = table.Column<string>(type: "text", nullable: false),
                TekkenId = table.Column<string>(type: "text", nullable: false),
                SteamLink = table.Column<string>(type: "text", nullable: true),
                Nicknames = table.Column<string[]>(type: "text[]", nullable: true),
                PSNLink = table.Column<string>(type: "text", nullable: true),
                CurrentNickname = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WankWavuPlayers", x => x.TwitchId);
            }
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "WankWavuPlayers");
    }
}
