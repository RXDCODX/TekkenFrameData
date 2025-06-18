using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TekkenFrameData.Library.Models.Twitch;

public class TwitchNotificationChannelsState
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string TwitchId { get; set; }
    public required Guid MessageId { get; set; }

    [ForeignKey(nameof(MessageId))]
    public required GlobalNotificationMessage Message { get; set; }
    public bool IsFinished { get; set; }
}
