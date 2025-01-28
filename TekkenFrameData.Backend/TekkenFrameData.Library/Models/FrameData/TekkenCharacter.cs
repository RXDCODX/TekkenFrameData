using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TekkenFrameData.Library.Models.FrameData;

[Table("tekken_characters")]
public class TekkenCharacter
{
    [Key]
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }
    public string? LinkToImage { get; set; }
    public ICollection<TekkenMove> Movelist { get; set; } = [];
    public DateTime LastUpdateTime { get; set; } = DateTime.Now.ToLocalTime();
}