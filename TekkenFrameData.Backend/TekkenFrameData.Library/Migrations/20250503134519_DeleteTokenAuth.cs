using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TekkenFrameData.Cli.Migrations
{
    /// <inheritdoc />
    public partial class DeleteTokenAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientOAuthToken",
                table: "Configuration");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientOAuthToken",
                table: "Configuration",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
