using System.Collections.Generic;
using TekkenFrameData.Library.Models.Discord;

namespace TekkenFrameData.Watcher.Services.Discord;

public class DiscordFramedataChannels(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ILogger<DiscordFramedataChannels> logger,
    IHostApplicationLifetime lifetime
)
{
    public List<ulong> Channels { get; private set; } = InitChannels(dbContextFactory);

    private readonly ILogger<DiscordFramedataChannels> _logger = logger;
    private readonly CancellationToken _cancellationToken = lifetime.ApplicationStopping;

    private static List<ulong> InitChannels(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var channels = dbContext.DiscordFramedataChannels.Select(e => e.ChannelId);
        return channels.ToList();
    }

    public async Task<bool> AddAsync(DiscordFramedataChannel channel)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(_cancellationToken);
        var isExists = await dbContext.DiscordFramedataChannels.AnyAsync(
            e => e.ChannelId == channel.ChannelId,
            cancellationToken: _cancellationToken
        );
        if (isExists)
        {
            return false;
        }

        dbContext.DiscordFramedataChannels.Add(channel);
        var rowNumbers = await dbContext.SaveChangesAsync(_cancellationToken);
        await UpdateAllowedChannels();
        return rowNumbers != 0;
    }

    public async Task<bool> RemoveAsync(ulong guildId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(_cancellationToken);
        var isExists = await dbContext.DiscordFramedataChannels.AnyAsync(
            e => e.GuildId == guildId,
            cancellationToken: _cancellationToken
        );
        if (!isExists)
        {
            return false;
        }

        await dbContext
            .DiscordFramedataChannels.Where(e => e.GuildId == guildId)
            .ExecuteDeleteAsync(cancellationToken: _cancellationToken);

        var rowNumbers = await dbContext.SaveChangesAsync(_cancellationToken);
        await UpdateAllowedChannels();
        return rowNumbers != 0;
    }

    private async Task UpdateAllowedChannels()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(_cancellationToken);
        Channels = dbContext.DiscordFramedataChannels.Select(e => e.ChannelId).ToList();
    }
}
