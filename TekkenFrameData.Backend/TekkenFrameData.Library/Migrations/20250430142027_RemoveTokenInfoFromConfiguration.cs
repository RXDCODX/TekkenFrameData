using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TekkenFrameData.Cli.Migrations;

/// <inheritdoc />
public partial class RemoveTokenInfoFromConfiguration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Configuration_TwitchToken_TokenId",
            table: "Configuration"
        );

        migrationBuilder.DropIndex(name: "IX_Configuration_TokenId", table: "Configuration");

        migrationBuilder.DropColumn(name: "TokenId", table: "Configuration");

        migrationBuilder.AddColumn<string>(
            name: "ApiClientId",
            table: "Configuration",
            type: "text",
            nullable: false,
            defaultValue: ""
        );

        migrationBuilder.AddColumn<string>(
            name: "ApiClientSecret",
            table: "Configuration",
            type: "text",
            nullable: false,
            defaultValue: ""
        );

        migrationBuilder.AddColumn<string>(
            name: "ClientOAuthToken",
            table: "Configuration",
            type: "text",
            nullable: false,
            defaultValue: ""
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ApiClientId", table: "Configuration");

        migrationBuilder.DropColumn(name: "ApiClientSecret", table: "Configuration");

        migrationBuilder.DropColumn(name: "ClientOAuthToken", table: "Configuration");

        migrationBuilder.AddColumn<Guid>(
            name: "TokenId",
            table: "Configuration",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
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
}
