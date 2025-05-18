using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TekkenFrameData.Cli.Migrations;

/// <inheritdoc />
public partial class TelegramAndTwitchConfigurationToDatabase : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long[]>(
            name: "AdminIdsArray",
            table: "Configuration",
            type: "bigint[]",
            nullable: false,
            defaultValue: Array.Empty<long>()
        );

        migrationBuilder.AddColumn<string>(
            name: "BotToken",
            table: "Configuration",
            type: "text",
            nullable: false,
            defaultValue: ""
        );

        migrationBuilder.AddColumn<Guid>(
            name: "TokenId",
            table: "Configuration",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
        );

        migrationBuilder.CreateTable(
            name: "TwitchToken",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AccessToken = table.Column<string>(type: "text", nullable: false),
                RefreshToken = table.Column<string>(type: "text", nullable: false),
                ExpiresIn = table.Column<TimeSpan>(type: "interval", nullable: false),
                WhenCreated = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false
                ),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TwitchToken", x => x.Id);
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_Configuration_TokenId",
            table: "Configuration",
            column: "TokenId"
        );

        migrationBuilder.AddForeignKey(
            name: "FK_Configuration_TwitchToken_TokenId",
            table: "Configuration",
            column: "TokenId",
            principalTable: "TwitchToken",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Configuration_TwitchToken_TokenId",
            table: "Configuration"
        );

        migrationBuilder.DropTable(name: "TwitchToken");

        migrationBuilder.DropIndex(name: "IX_Configuration_TokenId", table: "Configuration");

        migrationBuilder.DropColumn(name: "AdminIdsArray", table: "Configuration");

        migrationBuilder.DropColumn(name: "BotToken", table: "Configuration");

        migrationBuilder.DropColumn(name: "TokenId", table: "Configuration");
    }
}
