using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TekkenFrameData.Cli.Migrations;

/// <inheritdoc />
public partial class AlisaBLockedUsers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AlisaIgnoreTwitchUsers",
            columns: table => new
            {
                TwitchId = table.Column<string>(type: "text", nullable: false),
                TwitchName = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AlisaIgnoreTwitchUsers", x => x.TwitchId);
            }
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AlisaIgnoreTwitchUsers");
    }
}
