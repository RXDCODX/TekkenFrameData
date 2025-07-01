using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TekkenFrameData.Cli.Migrations;

/// <inheritdoc />
public partial class SteamConfig : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SteamClientLogin",
            table: "Configuration",
            type: "text",
            nullable: false,
            defaultValue: ""
        );

        migrationBuilder.AddColumn<string>(
            name: "SteamClientPassword",
            table: "Configuration",
            type: "text",
            nullable: false,
            defaultValue: ""
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SteamClientLogin", table: "Configuration");

        migrationBuilder.DropColumn(name: "SteamClientPassword", table: "Configuration");
    }
}
