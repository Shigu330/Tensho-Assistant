namespace TenshoAssistant.Discoveries;

/// <summary>X-43 (0.807): what is noted. On by default: everything stays on the player's disk; the debug log only in developer mode.</summary>
public sealed class DiscoverySettings
{
    public bool Enabled { get; set; }=true;
    public bool MonsterPlaces { get; set; }=true;
    public bool DebugLog { get; set; }
}
