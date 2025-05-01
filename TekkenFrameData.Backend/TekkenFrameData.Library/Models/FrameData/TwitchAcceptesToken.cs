using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;

namespace TekkenFrameData.Library.Models.FrameData;

public class TwitchAcceptesToken(string twitchId)
{
    [Key]
    [Required]
    public string TwitchId = twitchId;
    public string Token { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset WhenCreated { get; set; } = DateTimeOffset.Now;

    [NotMapped]
    public TimeSpan TimePassedSinceCreated => DateTimeOffset.Now - WhenCreated;
}
