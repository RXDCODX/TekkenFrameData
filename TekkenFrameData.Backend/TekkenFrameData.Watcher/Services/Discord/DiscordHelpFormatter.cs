using System.Collections.Generic;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;

namespace TekkenFrameData.Watcher.Services.Discord;

public class DiscordHelpFormatter(CommandContext ctx) : BaseHelpFormatter(ctx)
{
    public const string HelpText =
        "/framedata <персонаж> [удар] — Получить информацию о персонаже или конкретном муве.\n"
        + "/setframedatachannel — Указать канал для работы фреймдаты.\n"
        + "/help — Показать это сообщение.\n";

    private readonly DiscordEmbedBuilder _embed = new DiscordEmbedBuilder()
        .WithTitle("Доступные команды Discord-бота")
        .WithColor(DiscordColor.Azure)
        .WithDescription("Список всех доступных команд:");

    public override BaseHelpFormatter WithCommand(Command command)
    {
        var desc = string.IsNullOrWhiteSpace(command.Description)
            ? "Без описания"
            : command.Description;
        _embed.AddField($"/{command.Name}", desc, false);
        return this;
    }

    public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
    {
        foreach (var cmd in subcommands)
        {
            var desc = string.IsNullOrWhiteSpace(cmd.Description)
                ? "Без описания"
                : cmd.Description;
            _embed.AddField($"/{cmd.Name}", desc, false);
        }
        return this;
    }

    public override CommandHelpMessage Build()
    {
        return new CommandHelpMessage(embed: _embed.Build());
    }
}
