namespace TenshoAssistant.Reminders;

/// <summary>One seed of the embedded garden data (Lotlab, GPL-3.0): growing hours and hours before it wilts without tending (0 = none).</summary>
public sealed record GardenSeed(uint SeedId,uint HarvestId,int GrowHours,int WiltHours);

/// <summary>
/// 0.449 (DEC-027, point 3): the garden journal's durations filled in from the seed, read from Reminders/Data/garden-seeds.csv
/// (tools/garden-data.py, Lotlab/FFXIV-Gardening-Tracker). The player can still change them; nothing is read from the game.
/// </summary>
public static class GardenSeeds
{
    public static IReadOnlyList<GardenSeed> Read(TextReader reader)
    {
        var seeds=new List<GardenSeed>();
        var seen=new HashSet<uint>();
        while(reader.ReadLine() is { } line)
        {
            if(line.StartsWith('#') || line.StartsWith("seed,",StringComparison.Ordinal))continue;
            var cells=line.Split(',');
            if(cells.Length!=4 || !uint.TryParse(cells[0],out var seed) || !uint.TryParse(cells[1],out var harvest) || !int.TryParse(cells[2],out var grow)
                || !int.TryParse(cells[3],out var wilt) || seed==0 || grow<=0 || wilt<0 || !seen.Add(seed))continue;
            seeds.Add(new(seed,harvest,grow,wilt));
        }
        return seeds;
    }

    /// <summary>The journal's growing hours for this seed (bounded like a typed value).</summary>
    public static int GrowHours(GardenSeed seed)=>GardenJournal.ClampGrow(seed.GrowHours);

    /// <summary>The tending reminder: when the plant starts to wilt without care (0 = no tending reminder).</summary>
    public static int TendHours(GardenSeed seed)=>GardenJournal.ClampTend(seed.WiltHours);

    /// <summary>The seed's name as the journal keeps it (1 to 40 characters), or null.</summary>
    public static string? JournalName(string? name)
    {
        var clean=(name ?? "").Trim();
        if(clean.Length>GardenJournal.MaximumText)clean=clean[..GardenJournal.MaximumText].TrimEnd();
        return GardenJournal.CleanText(clean);
    }
}
