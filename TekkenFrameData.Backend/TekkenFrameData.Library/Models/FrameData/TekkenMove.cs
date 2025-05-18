using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TekkenFrameData.Library.Models.FrameData;

[Table("tekken_moves")]
public class TekkenMove
{
    [MaxLength(150)]
    public required string CharacterName { get; init; }
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

    [MaxLength(100)]
    [Required]
    public required string Command { get; init; }

    [MaxLength(100)]
    public string? HitLevel { get; set; }

    [MaxLength(100)]
    public string? Damage { get; set; }

    [MaxLength(100)]
    public string? StartUpFrame { get; set; }

    [MaxLength(100)]
    public string? BlockFrame { get; set; }

    [MaxLength(100)]
    public string? HitFrame { get; set; }

    [MaxLength(100)]
    public string? CounterHitFrame { get; set; }
    public string? Notes { get; set; }
    public bool IsUserChanged { get; set; } = false;

    public override bool Equals(object? obj)
    {
        return obj is TekkenMove move
            && Command.Equals(move.Command, StringComparison.OrdinalIgnoreCase)
            && CharacterName.Equals(move.CharacterName, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(CharacterName, Command);
    }
}
