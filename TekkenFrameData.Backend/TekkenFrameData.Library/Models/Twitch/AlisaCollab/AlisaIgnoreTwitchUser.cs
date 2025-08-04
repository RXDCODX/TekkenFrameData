using System.ComponentModel.DataAnnotations;

namespace TekkenFrameData.Library.Models.Twitch.AlisaCollab;

public class AlisaIgnoreTwitchUser
{
    [Key]
    public required string TwitchId { get; set; }
    public required string TwitchName { get; set; }
}
