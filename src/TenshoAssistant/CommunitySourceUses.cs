namespace TenshoAssistant;

/// <summary>X-122, D-101 (user's decision of 2026-11-10): one use of a community source, with its own switch under the source's switch.</summary>
public enum SourceUse
{
    GarlandDutyLoot, GarlandMonsterLoot, GarlandItemSources,
    WikiMonsters, WikiDutyGuides, WikiBlueMagic, WikiBeastmaster, WikiItemAcquisition, WikiVistas, WikiIslandAnimals,
    UniversalisRetainerCompare, UniversalisToolPrices,
}

/// <summary>
/// X-122, D-101: the switch of each use of Garland Tools, the wiki and Universalis (Settings › Data and sources), on by default. A use asks
/// its source only while both the source's switch and its own are on (CommunitySources.Allowed), so « game and observed data only », which
/// turns the sources off, still cuts every use.
/// </summary>
public sealed class SourceUseSettings
{
    public bool GarlandDutyLoot { get; set; } = true;
    public bool GarlandMonsterLoot { get; set; } = true;
    public bool GarlandItemSources { get; set; } = true;
    public bool WikiMonsters { get; set; } = true;
    public bool WikiDutyGuides { get; set; } = true;
    public bool WikiBlueMagic { get; set; } = true;
    public bool WikiBeastmaster { get; set; } = true;
    public bool WikiItemAcquisition { get; set; } = true;
    public bool WikiVistas { get; set; } = true;
    public bool WikiIslandAnimals { get; set; } = true;
    public bool UniversalisRetainerCompare { get; set; } = true;
    public bool UniversalisToolPrices { get; set; } = true;

    public bool Get(SourceUse use)=>use switch
    {
        SourceUse.GarlandDutyLoot=>GarlandDutyLoot,
        SourceUse.GarlandMonsterLoot=>GarlandMonsterLoot,
        SourceUse.GarlandItemSources=>GarlandItemSources,
        SourceUse.WikiMonsters=>WikiMonsters,
        SourceUse.WikiDutyGuides=>WikiDutyGuides,
        SourceUse.WikiBlueMagic=>WikiBlueMagic,
        SourceUse.WikiBeastmaster=>WikiBeastmaster,
        SourceUse.WikiItemAcquisition=>WikiItemAcquisition,
        SourceUse.WikiVistas=>WikiVistas,
        SourceUse.WikiIslandAnimals=>WikiIslandAnimals,
        SourceUse.UniversalisRetainerCompare=>UniversalisRetainerCompare,
        SourceUse.UniversalisToolPrices=>UniversalisToolPrices,
        _=>false,
    };
}
