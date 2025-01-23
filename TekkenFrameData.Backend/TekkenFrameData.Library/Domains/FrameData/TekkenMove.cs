using System.ComponentModel.DataAnnotations;

namespace TekkenFrameData.Library.Domains.FrameData;

public class TekkenMove
{
    [MaxLength(150)]
    public string CharacterName => Character.Name;
    public required TekkenCharacter Character { get; set; }
    public bool IsFromStance => !string.IsNullOrWhiteSpace(StanceCode);
    [MaxLength(10)]
    public string? StanceCode { get; set; } = string.Empty;
    [MaxLength(100)]
    public string? StanceName { get; set; } = string.Empty;
    public bool HeatEngage { get; set; }
    public bool HeatSmash { get; set; }
    public bool PowerCrush { get; set; }
    public bool Throw { get; set; }
    public bool Homing { get; set; }
    public bool Tornado { get; set; }
    public bool HeatBurst { get; set; }
    public bool RequiresHeat { get; set; }
    [MaxLength(30)]
    [Required]
    public required string Command { get; set; }
    [MaxLength(20)]
    public string? HitLevel { get; set; }
    [MaxLength(20)]
    public string? Damage { get; set; }
    [MaxLength(30)]
    public string? StartUpFrame { get; set; }
    [MaxLength(20)]
    public string? BlockFrame { get; set; }
    [MaxLength(20)]
    public string? HitFrame { get; set; }
    [MaxLength(100)]
    public string? CounterHitFrame { get; set; }
    public string? Notes { get; set; }
    public bool IsUserChanged { get; set; } = false;
}