using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TekkenFrameData.Library.Models.Discord;

public class DiscordFramedataChannel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public ulong GuildId { get; set; }
    public string? GuildName { get; set; }
    public ulong ChannelId { get; set; }
    public string? ChannelName { get; set; }
    public string? OwnerName { get; set; }
    public ulong? OwnerId { get; set; }
}
