namespace TenshoAssistant.Items;

/// <summary>X-197 (0.1761): an item that can be desynthesized: the crafter whose desynthesis it is (Item.ClassJobRepair) and its item level
/// (Item.LevelItem).</summary>
public sealed record DesynthInfo(uint Job,uint ItemLevel);

/// <summary>Where an item's level stands against your desynthesis level of its crafter.</summary>
public enum DesynthStanding { Unknown, Above, Same, Below }

/// <summary>What a coffer, a card pack or a container gives that a collection tracks: how many of them, how many not obtained yet, and
/// whether the community's list was cut (only its first items were looked at).</summary>
public sealed record CofferStanding(int Collectibles,int Missing,int Listed,bool Partial);

/// <summary>
/// X-197 (0.1761, the user's request of 12/11/2026, K-06; « on a déjà désynthèse et les paquets de cartes »: the panel under the game's item
/// tooltip already said what an item is desynthesized from and what a pack is the contents of). Two lines more: the item's desynthesis (its
/// crafter, its item level, your desynthesis level of that crafter read in the game, above or below), and for a coffer, a card pack or a
/// container (LuminaSupplemental's ItemSupplement, kinds 3, 6 and 7) what it holds that a collection tracks: all obtained, or how many are
/// missing (docs/evidence/0.1761-desynthesis-coffer.md). Pure (tests).
/// </summary>
public static class ItemDesynth
{
    /// <summary>The item's desynthesis: Item.Desynth not 0, its crafter one of the eight (ClassJob 8 to 15), an item level; else none.</summary>
    public static DesynthInfo? Of(ushort desynth,uint repairJob,uint itemLevel)=>desynth!=0 && repairJob is >= 8 and <= 15 && itemLevel>0 ? new(repairJob,itemLevel):null;

    /// <summary>Your desynthesis level as the game gives it (PlayerState.GetDesynthesisLevel: hundredths kept); outside 0 to 1000, or 0
    /// (not learned, or not read): unknown, never a level made up (R08).</summary>
    public static float? Level(float read)=>float.IsFinite(read) && read>0 && read<=1000 ? read:null;

    /// <summary>The item's level against your level (its whole part, as the game shows it in the desynthesis window).</summary>
    public static DesynthStanding Standing(uint itemLevel,float? yours)=>yours is not { } level ? DesynthStanding.Unknown
        :itemLevel>(uint)Math.Floor(level) ? DesynthStanding.Above:itemLevel==(uint)Math.Floor(level) ? DesynthStanding.Same:DesynthStanding.Below;

    /// <summary>The kinds of ItemSupplement read as « what it holds »: 3 loot (« Contains »), 6 card pack, 7 coffer.</summary>
    public static bool Holds(int kind)=>kind is 3 or 6 or 7;

    /// <summary>
    /// What a container holds that a collection tracks: each item listed once, <paramref name="acquired"/> null (no collection tracks it, or
    /// not known) leaves it out. Null when none is tracked. <see cref="CofferStanding.Partial"/>: the community's list of a kind holds more than
    /// it gave (it stops at 24), so the count is of the first ones only.
    /// </summary>
    public static CofferStanding? Coffer(IEnumerable<(int Kind,int Count,IReadOnlyList<uint> Items)> gives,Func<uint,bool?> acquired)
    {
        var listed=new HashSet<uint>();
        var partial=false;
        foreach(var (kind,count,items) in gives)
        {
            if(!Holds(kind))continue;
            if(count>items.Count)partial=true;
            listed.UnionWith(items);
        }
        int collectibles=0,missing=0;
        foreach(var item in listed)
        {
            if(acquired(item) is not { } owned)continue;
            collectibles++;
            if(!owned)missing++;
        }
        return collectibles==0 ? null:new(collectibles,missing,listed.Count,partial);
    }
}
