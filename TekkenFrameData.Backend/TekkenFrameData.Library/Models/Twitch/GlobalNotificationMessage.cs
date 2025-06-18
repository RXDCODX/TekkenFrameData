using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TekkenFrameData.Library.Models.Twitch;

public class GlobalNotificationMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Message { get; set; }
    public GlobalNotificationsPlatforms Services { get; set; }
}
