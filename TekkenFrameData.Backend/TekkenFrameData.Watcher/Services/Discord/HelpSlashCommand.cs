using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Linq;
using System.Collections.Generic;

namespace TekkenFrameData.Watcher.Services.Discord;

public class HelpSlashCommand : ApplicationCommandModule
{
    [SlashCommand("help", "Показать список доступных команд")]
    public async Task Help(InteractionContext ctx)
    {
        var embed = new DiscordEmbedBuilder()
            .WithTitle("Доступные команды Discord-бота")
            .WithColor(DiscordColor.Azure)
            .WithDescription(DiscordHelpFormatter.HelpText);

        await ctx.CreateResponseAsync(
            InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral()
        );
    }
}