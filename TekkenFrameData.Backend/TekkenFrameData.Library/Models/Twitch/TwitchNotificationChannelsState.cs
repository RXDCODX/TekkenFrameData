using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TekkenFrameData.Library.Models.FrameData;

namespace TekkenFrameData.Library.Models.Twitch;

public class TwitchNotificationChannelsState
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid ChannelId { get; set; }

    [ForeignKey(nameof(ChannelId))]
    public required TwitchTekkenChannel Channel { get; set; }
    public required Guid MessageId { get; set; }

    [ForeignKey(nameof(MessageId))]
    public required GlobalNotificationMessage Message { get; set; }
    public bool IsFinished { get; set; }
}
