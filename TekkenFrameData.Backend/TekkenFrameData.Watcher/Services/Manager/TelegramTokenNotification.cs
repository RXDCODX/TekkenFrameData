﻿using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TekkenFrameData.Library.DB;
using TekkenFrameData.Library.Exstensions;
using Telegram.Bot;
using TwitchLib.Api.Interfaces;

namespace TekkenFrameData.Watcher.Services.Manager;

public class TelegramTokenNotification(
    ITelegramBotClient client,
    IServer server,
    IDbContextFactory<AppDbContext> factory,
    ILogger<TelegramTokenNotification> logger
)
{
    public async Task NotifyStreamerAboutAuthAsync(ITwitchAPI api)
    {
        try
        {
            await using var dbContext = await factory.CreateDbContextAsync();
            var configuration = dbContext.Configuration.Single();

            var addressesFeature = server.Features.Get<IServerAddressesFeature>();

            while (addressesFeature!.Addresses.Count == 0)
            {
                await Task.Delay(1000);
            }

            var address = addressesFeature.Addresses.FirstOrDefault();

            foreach (var t in configuration.AdminIdsArray)
            {
                await client.SendMessage(
                    t,
                    "Нужно пройти переаунтефикацию для твича!"
                        + Environment.NewLine
                        + Environment.NewLine
                        + $"""https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={api.Settings.ClientId}&redirect_uri={address}/twitchuserauth&scope=analytics:read:extensions+user:edit+user:read:email+clips:edit+bits:read+analytics:read:games+user:edit:broadcast+user:read:broadcast+chat:read+chat:edit+channel:moderate+channel:read:subscriptions+whispers:read+whispers:edit+moderation:read+channel:read:redemptions+channel:edit:commercial+channel:read:hype_train+channel:read:stream_key+channel:manage:extensions+channel:manage:broadcast+user:edit:follows+channel:manage:redemptions+channel:read:editors+channel:manage:videos+user:read:blocked_users+user:manage:blocked_users+user:read:subscriptions+user:read:follows+channel:manage:polls+channel:manage:predictions+channel:read:polls+channel:read:predictions+moderator:manage:automod+channel:manage:schedule+channel:read:goals+moderator:read:automod_settings+moderator:manage:automod_settings+moderator:manage:banned_users+moderator:read:blocked_terms+moderator:manage:blocked_terms+moderator:read:chat_settings+moderator:manage:chat_settings+channel:manage:raids+moderator:manage:announcements+moderator:manage:chat_messages+user:manage:chat_color+channel:manage:moderators+channel:read:vips+channel:manage:vips+user:manage:whispers+channel:read:charity+moderator:read:chatters+moderator:read:shield_mode+moderator:manage:shield_mode+moderator:read:shoutouts+moderator:manage:shoutouts+moderator:read:followers+channel:read:guest_star+channel:manage:guest_star+moderator:read:guest_star+moderator:manage:guest_star+channel:bot+user:bot+user:read:chat+channel:manage:ads+channel:read:ads+user:read:moderated_channels+user:write:chat+user:read:emotes+moderator:read:unban_requests+moderator:manage:unban_requests+moderator:read:suspicious_users"""
                );
            }
        }
        catch (Exception e)
        {
            logger.LogException(e);
        }
    }
}
