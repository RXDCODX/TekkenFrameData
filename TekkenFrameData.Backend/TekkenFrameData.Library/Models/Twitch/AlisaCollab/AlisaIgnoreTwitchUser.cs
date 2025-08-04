using System.ComponentModel.DataAnnotations;

namespace TekkenFrameData.Watcher.Services.AlisaService.Entitys;

public class AlisaIgnoreTwitchUser
{
    [Key]
    public required string TwitchId { get; set; }
    public required string TwitchName { get; set; }
}
