using System.ComponentModel.DataAnnotations;

namespace TekkenFrameData.Watcher.Domains.FrameData;

public class TekkenCharacter
{
    [Key]
    [Required]
    public string Name { get; set; }
    public string LinkToImage { get; set; }
    public IEnumerable<TekkenMove> Movelist { get; set; }
    public DateTime LastUpdateTime { get; set; } = DateTime.Now.ToLocalTime();
}