using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TekkenFrameData.Library.Models.FrameData;

[Table("tekken_characters")]
public class Character
{
    [Key]
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }
    public string? LinkToImage { get; set; }
    public ICollection<Move> Movelist { get; set; } = [];
    public DateTime LastUpdateTime { get; set; } = DateTime.Now.ToLocalTime();
    public string? Description { get; set; }
    public string[]? Strengths { get; set; }
    public string[]? Weaknesess { get; set; }
    public byte[]? Image { get; set; }
    public string? ImageExtension { get; set; }
    public string? PageUrl { get; set; }
}
