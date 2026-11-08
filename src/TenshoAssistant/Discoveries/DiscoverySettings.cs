namespace TenshoAssistant.Discoveries;

/// <summary>X-43 (0.807): what is noted. On by default: everything stays on the player's disk; the debug log only in developer mode.</summary>
public sealed class DiscoverySettings
{
    public bool Enabled { get; set; }=true;
    public bool MonsterPlaces { get; set; }=true;
    /// <summary>X-72 C (0.856): the hunt marks' spawn places, and the FATEs seen.</summary>
    public bool HuntMarks { get; set; }=true;
    public bool Fates { get; set; }=true;
    /// <summary>X-74 (0.860): where the items got come from (desynthesis, ventures, duty chests, FATEs, monsters, opened items), and the fish caught.</summary>
    public bool ItemSources { get; set; }=true;
    public bool Fishing { get; set; }=true;
    /// <summary>X-73.4 (0.861): the Fashion Report's judgement of the outfit shown.</summary>
    public bool FashionReport { get; set; }=true;
    /// <summary>X-52 (0.817): the prices seen at the market board, in place of Universalis' for the player's world when newer.</summary>
    public bool MarketPrices { get; set; }=true;
    public bool DebugLog { get; set; }
    /// <summary>X-77.1 (0.883): once a day, take the common file and (when GitHub is connected) send your own sightings. Off by default: the player turns it on.</summary>
    public bool AutoExchange { get; set; }
}
