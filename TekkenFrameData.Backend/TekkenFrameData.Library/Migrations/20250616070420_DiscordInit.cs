using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TekkenFrameData.Cli.Migrations;

/// <inheritdoc />
public partial class DiscordInit : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "tekken_characters",
            type: "text",
            nullable: true
        );

        migrationBuilder.AddColumn<string[]>(
            name: "Strengths",
            table: "tekken_characters",
            type: "text[]",
            nullable: true
        );

        migrationBuilder.AddColumn<string[]>(
            name: "Weaknesess",
            table: "tekken_characters",
            type: "text[]",
            nullable: true
        );

        migrationBuilder.AddColumn<string>(
            name: "DiscordToken",
            table: "Configuration",
            type: "text",
            nullable: false,
            defaultValue: ""
        );

        migrationBuilder.CreateTable(
            name: "DiscordFramedataChannels",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                GuildName = table.Column<string>(type: "text", nullable: true),
                ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                ChannelName = table.Column<string>(type: "text", nullable: true),
                OwnerName = table.Column<string>(type: "text", nullable: true),
                OwnerId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DiscordFramedataChannels", x => x.Id);
            }
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DiscordFramedataChannels");

        migrationBuilder.DropColumn(name: "Description", table: "tekken_characters");

        migrationBuilder.DropColumn(name: "Strengths", table: "tekken_characters");

        migrationBuilder.DropColumn(name: "Weaknesess", table: "tekken_characters");

        migrationBuilder.DropColumn(name: "DiscordToken", table: "Configuration");
    }
}
