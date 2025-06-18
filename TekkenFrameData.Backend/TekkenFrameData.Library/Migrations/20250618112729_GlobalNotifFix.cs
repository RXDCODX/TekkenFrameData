using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TekkenFrameData.Cli.Migrations
{
    /// <inheritdoc />
    public partial class GlobalNotifFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TwitchId",
                table: "GlobalNotificatoinChannelsState");

            migrationBuilder.AddColumn<Guid>(
                name: "ChannelId",
                table: "GlobalNotificatoinChannelsState",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_GlobalNotificatoinChannelsState_ChannelId",
                table: "GlobalNotificatoinChannelsState",
                column: "ChannelId");

            migrationBuilder.AddForeignKey(
                name: "FK_GlobalNotificatoinChannelsState_TekkenChannels_ChannelId",
                table: "GlobalNotificatoinChannelsState",
                column: "ChannelId",
                principalTable: "TekkenChannels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GlobalNotificatoinChannelsState_TekkenChannels_ChannelId",
                table: "GlobalNotificatoinChannelsState");

            migrationBuilder.DropIndex(
                name: "IX_GlobalNotificatoinChannelsState_ChannelId",
                table: "GlobalNotificatoinChannelsState");

            migrationBuilder.DropColumn(
                name: "ChannelId",
                table: "GlobalNotificatoinChannelsState");

            migrationBuilder.AddColumn<string>(
                name: "TwitchId",
                table: "GlobalNotificatoinChannelsState",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
