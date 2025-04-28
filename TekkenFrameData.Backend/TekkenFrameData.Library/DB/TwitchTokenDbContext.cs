using Microsoft.EntityFrameworkCore;
using TekkenFrameData.Library.Models.ExternalServices.Twitch;

namespace TekkenFrameData.Library.DB;

public partial class AppDbContext
{
    public DbSet<TwitchTokenInfo> TwitchToken { get; set; } = null!;
}
