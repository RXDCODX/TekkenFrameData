using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TekkenFrameData.Watcher.Domains.FrameData;

public class TekkenCharacter
{
    [Key]
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }
    public string? LinkToImage { get; set; }
    public ICollection<TekkenMove> Movelist { get; set; } = [];
    public System.DateTime LastUpdateTime { get; set; } = DateTime.Now.ToLocalTime();
}