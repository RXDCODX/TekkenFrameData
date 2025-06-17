﻿using System.Collections.Generic;
using TekkenFrameData.Library.Models.FrameData;
using TekkenFrameData.Library.Models.FrameData.Entitys.Enums;

namespace TekkenFrameData.Watcher.Services.Framedata;

public partial class Tekken8FrameData
{
    private readonly CancellationToken _cancellationToken = lifetime.ApplicationStopping;
    private static readonly KeyValuePair<string, string> DefaultValuePair = new(
        string.Empty,
        string.Empty
    );

    internal static readonly Dictionary<TekkenMoveTag, string[]> MoveTags = new()
    {
        {
            TekkenMoveTag.HeatEngage,
            [
                "engage",
                "enga",
                "enggg",
                "engg",
                "heatengage",
                "heatengagage",
                "he",
                "eng",
                "hes",
                "heatengagers",
                "engs",
            ]
        },
        {
            TekkenMoveTag.HeatSmash,
            ["smash", "heatsmash", "smsh", "heatsmsh", "hs", "hses", "hss"]
        },
        {
            TekkenMoveTag.PowerCrush,
            [
                "crush",
                "powercrush",
                "pc",
                "power",
                "armor",
                "armori",
                "power_crush",
                "power crush",
                "pcs",
            ]
        },
        { TekkenMoveTag.Throw, ["throw", "throws", "throwbrow", "grab", "grabs"] },
        { TekkenMoveTag.Homing, ["homing", "homari", "homings", "hmngs", "hmng"] },
        {
            TekkenMoveTag.Tornado,
            [
                "tornado",
                "trnd",
                "wind",
                "taifun",
                "ts",
                "tail_spin",
                "tailspin",
                "screw",
                "s!",
                "s",
                "screws",
            ]
        },
        {
            TekkenMoveTag.HeatBurst,
            ["hb", "heatburst", "heat burst", "hear_burst", "burst", "hbs", "bursts"]
        },
    };

    public readonly Uri BasePath = new("https://tekkendocs.com");
    public List<TekkenMove> VictorinaMoves = [];
    public bool ParsingActive { get; set; }
}
