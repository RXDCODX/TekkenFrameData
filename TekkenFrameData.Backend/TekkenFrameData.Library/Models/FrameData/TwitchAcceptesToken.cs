using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TekkenFrameData.Library.Models.FrameData;

public class TwitchAcceptesToken(string twitchId)
{
    [Key]
    [Required]
    public string TwitchId { get; init; } = twitchId;
    public string Token { get; set; } = Guid.NewGuid().ToString();
    public DateTime WhenCreated { get; set; } = DateTime.Now;

    [NotMapped]
    public TimeSpan TimePassedSinceCreated => DateTime.Now - WhenCreated;
}
