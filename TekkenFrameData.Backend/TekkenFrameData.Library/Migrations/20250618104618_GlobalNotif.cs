using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TekkenFrameData.Cli.Migrations;

/// <inheritdoc />
public partial class GlobalNotif : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "GlobalNotificationMessage",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Message = table.Column<string>(type: "text", nullable: false),
                Services = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GlobalNotificationMessage", x => x.Id);
            }
        );

        migrationBuilder.CreateTable(
            name: "GlobalNotificatoinChannelsState",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TwitchId = table.Column<string>(type: "text", nullable: false),
                MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                IsFinished = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GlobalNotificatoinChannelsState", x => x.Id);
                table.ForeignKey(
                    name: "FK_GlobalNotificatoinChannelsState_GlobalNotificationMessage_M~",
                    column: x => x.MessageId,
                    principalTable: "GlobalNotificationMessage",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_GlobalNotificatoinChannelsState_MessageId",
            table: "GlobalNotificatoinChannelsState",
            column: "MessageId"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "GlobalNotificatoinChannelsState");

        migrationBuilder.DropTable(name: "GlobalNotificationMessage");
    }
}
