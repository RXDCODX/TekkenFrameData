using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TekkenFrameData.Cli.Migrations;

/// <inheritdoc />
public partial class TokenPlusVictorina : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AcceptesTokens",
            columns: table => new
            {
                TwitchId = table.Column<string>(type: "text", nullable: false),
                Token = table.Column<string>(type: "text", nullable: false),
                WhenCreated = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false
                ),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AcceptesTokens", x => x.TwitchId);
            }
        );

        migrationBuilder.CreateTable(
            name: "TekkenChannels",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TwitchId = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: true),
                FramedataStatus = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TekkenChannels", x => x.Id);
            }
        );

        migrationBuilder.CreateTable(
            name: "TwitchLeaderboardUsers",
            columns: table => new
            {
                TwitchId = table.Column<string>(type: "text", nullable: false),
                ChannelId = table.Column<string>(type: "text", nullable: false),
                DisplayName = table.Column<string>(type: "text", nullable: false),
                TekkenVictorinaWins = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TwitchLeaderboardUsers", x => new { x.TwitchId, x.ChannelId });
            }
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AcceptesTokens");

        migrationBuilder.DropTable(name: "TekkenChannels");

        migrationBuilder.DropTable(name: "TwitchLeaderboardUsers");
    }
}
