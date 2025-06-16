using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
    public string? Description { get; set; }
    public string[]? Strengths { get; set; }
    public string[]? Weaknesess { get; set; }
}
