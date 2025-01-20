using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TekkenFrameData.Watcher.Services.TelegramBotService.Commands;

public partial class Commands
{
    public async Task<Message> OnFramedataCommandReceived(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var msg = message.Text;
        var text = string.Empty;
        InlineKeyboardMarkup? markup = null;
        var split = msg.Split(" ");

        if (split.Length >= 3)
        {
            var bb = split.Skip(1).ToArray();

            var move = frameData.GetMove(bb);

            if (move != null)
            {
                text = $"""
                        🎭 <b>Character</b> 🎭
                                <i>{move.Character.Name}</i>

                        ///////////////////
                        🔡 <b>Input</b> 🔡
                                <i>{move.Command}</i>

                        🚀 <b>Startup</b> 🚀
                                <i>{move.StartUpFrame}</i>

                        🏁 <b>Block frame</b> 🏁
                                <i>{move.BlockFrame}</i>

                        🎯 <b>Hit frame</b> 🎯
                                <i>{move.HitFrame}</i>

                        🤝 <b>Counter hit frame</b> 🤝
                                <i>{move.CounterHitFrame}</i>

                        ///////////////////
                        📊 <b>Hit Level</b> 📊
                                <i>{move.HitLevel}</i>

                        💥 <b>Damage</b> 💥
                                <i>{move.Damage}</i>

                        📝 <b>Notes</b> 📝

                        <i>{move.Notes}</i>
                      
                        """;

                var buttons = new List<InlineKeyboardButton>();

                if (move.HeatEngage)
                {
                    var button = new InlineKeyboardButton("Heat Engager");
                    button.CallbackData = $"framedata:{move.Character.Name}:heatengage";
                    buttons.Add(button);
                }

                if (move.Tornado)
                {
                    var button = new InlineKeyboardButton("Tornado");
                    button.CallbackData = $"framedata:{move.Character.Name}:tornado";
                    buttons.Add(button);
                }

                if (move.HeatSmash)
                {
                    var button = new InlineKeyboardButton("Heat Smash");
                    button.CallbackData = $"framedata:{move.Character.Name}:heatsmash";
                    buttons.Add(button);
                }

                if (move.PowerCrush)
                {
                    var button = new InlineKeyboardButton("Power Crush");
                    button.CallbackData = $"framedata:{move.Character.Name}:powercrush";
                    buttons.Add(button);
                }

                if (move.HeatBurst)
                {
                    var button = new InlineKeyboardButton("Heat Burst");
                    button.CallbackData = $"framedata:{move.Character.Name}:heatburst";
                    buttons.Add(button);
                }

                if (move.Homing)
                {
                    var button = new InlineKeyboardButton("Homing");
                    button.CallbackData = $"framedata:{move.Character.Name}:homing";
                    buttons.Add(button);
                }

                if (!string.IsNullOrWhiteSpace(move.StanceCode))
                {
                    var button = new InlineKeyboardButton(move.StanceName);
                    button.CallbackData = $"framedata:{move.Character.Name}:stance:{move.StanceCode}";
                    buttons.Add(button);
                }

                if (move.Throw)
                {
                    var button = new InlineKeyboardButton("Throw");
                    button.CallbackData = $"framedata:{move.Character.Name}:throw";
                    buttons.Add(button);
                }

                markup = new InlineKeyboardMarkup(buttons);
            }
            else
                text = "Плохие параметры запроса фреймдаты.";
        }

        return await botClient.SendTextMessageAsync(
            message.Chat.Id,
            text,
            replyMarkup: markup,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }
}