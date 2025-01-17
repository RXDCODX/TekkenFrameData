namespace tekkenfd.Domains.FrameData
{
    public class TekkenMove
    {
        public string CharacterName { get; set; }
        public TekkenCharacter? Character { get; set; }
        public bool IsFromStance => !string.IsNullOrWhiteSpace(StanceCode);
        public string? StanceCode { get; set; } = string.Empty;
        public string? StanceName { get; set; } = string.Empty;
        public bool HeatEngage { get; set; }
        public bool HeatSmash { get; set; }
        public bool PowerCrush { get; set; }
        public bool Throw { get; set; }
        public bool Homing { get; set; }
        public bool Tornado { get; set; }
        public bool HeatBurst { get; set; }
        public bool RequiresHeat { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string Command { get; set; }
        public string? HitLevel { get; set; }
        public string? Damage { get; set; }
        public string? StartUpFrame { get; set; }
        public string? BlockFrame { get; set; }
        public string? HitFrame { get; set; }
        public string? CounterHitFrame { get; set; }
        public string? Notes { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public bool IsUserChanged { get; set; } = false;
    }
}