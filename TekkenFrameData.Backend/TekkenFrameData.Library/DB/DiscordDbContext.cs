using Microsoft.EntityFrameworkCore;
using TekkenFrameData.Library.Models.Discord;

namespace TekkenFrameData.Library.DB;

public partial class AppDbContext
{
    public DbSet<DiscordFramedataChannel> DiscordFramedataChannels { get; set; } = null!;
}
