using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;

namespace TekkenFrameData.Library.Models.FrameData;

public class TwitchTekkenChannel
{
    [Key]
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
    public required string TwitchId { get; set; }
    public string? Name { get; set; }
    public TekkenFramedataStatus FramedataStatus { get; set; }
}
