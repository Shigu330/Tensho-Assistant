using System.Text.Json;

namespace TenshoAssistant.Inventory;

/// <summary>Whose gil changed: the character, their retainers together, or the company chest.</summary>
public enum GilSource { Character, Retainers, Company }

/// <summary>What the change most likely was, from the game window open when it happened (Other when none matched).</summary>
// 0.903 (X-84): Mail: gil taken from a letter or attached to one (LetterViewer, LetterEditor). Added last: the saved numbers stay the same.
public enum GilCategory { Other, NpcShop, Market, Repair, Teleport, Retainer, Company, Mail }

/// <summary>One change of a gil balance: when, by how much, the balance after it, whose, and its category.</summary>
public sealed record GilEntry(DateTimeOffset At,long Delta,long Balance,GilSource Source,GilCategory Category);

/// <summary>
/// g, I-04 (0.351): the gil journal (user's choice: kept 90 days), original code (ideas from GilDelta and Tracky, MIT, read only).
/// A change is classified by the game window open within the last 15 seconds (the gil moves when the window's action ends). Kept per
/// character in one file, 90 days and 20 000 entries at most.
/// </summary>
public static class GilJournal
{
    public const int KeepDays=90,MaximumEntries=20000;
    public static readonly TimeSpan ContextWindow=TimeSpan.FromSeconds(15);

    /// <summary>The game windows that tell a category (addon names), most specific first.</summary>
    public static readonly IReadOnlyList<(string Addon,GilCategory Category)> Windows=
    [
        ("Repair",GilCategory.Repair),("Teleport",GilCategory.Teleport),
        ("ItemSearchResult",GilCategory.Market),("ItemSearch",GilCategory.Market),("RetainerSell",GilCategory.Market),("RetainerSellList",GilCategory.Market),
        ("Shop",GilCategory.NpcShop),("InclusionShop",GilCategory.NpcShop),("ShopExchangeItem",GilCategory.NpcShop),
        ("Bank",GilCategory.Retainer),("RetainerList",GilCategory.Retainer),("SelectString",GilCategory.Other),
        ("FreeCompanyChest",GilCategory.Company),
        // 0.903 (X-84, user's test, docs/evidence/0.903-letters.md): the log showed « Gil other: +1, windows seen in the last 15 s: LetterList, LetterViewer … ».
        ("LetterViewer",GilCategory.Mail),("LetterEditor",GilCategory.Mail),
        // 0.775 (X-18): the game takes the teleport's price on arrival, after its window closed: the log showed « Gil other: -1,240, windows seen in the last
        // 15 s: NowLoading » with the chat line « Vous avez dépensé 1240 gils pour vous téléporter ». Only a spending counts (see GilWatcher).
        ("NowLoading",GilCategory.Teleport),
    ];

    /// <summary>0.775: a gain is never a teleport, even right after a loading screen.</summary>
    public static GilCategory ForDelta(GilCategory category,long delta)=>category==GilCategory.Teleport && delta>0 ? GilCategory.Other:category;

    /// <summary>0.769 (X-19, user report): gil moved between the character and their retainers or the company chest is neither earned nor spent: shown apart, out of the net.</summary>
    public static bool IsTransfer(GilCategory category)=>category is GilCategory.Retainer or GilCategory.Company;

    /// <summary>
    /// 0.769 (X-19): a rise of the retainers' gil is a deposit as far as the character's deposits since then cover it, the rest their market sales; a fall is a withdrawal.
    /// Returns the parts, and what is left of the deposits.
    /// </summary>
    public static (long Transfer,long Sales,long DepositsLeft) SplitRetainerRise(long rise,long deposits)
    {
        if(rise<=0)return (rise,0,deposits);
        var transfer=Math.Min(rise,Math.Max(0,deposits));
        return (transfer,rise-transfer,Math.Max(0,deposits)-transfer);
    }

    /// <summary>The category of a change at <paramref name="now"/>, from when each window was last seen open.</summary>
    public static GilCategory Classify(IReadOnlyDictionary<string,DateTimeOffset> lastSeen,DateTimeOffset now)
    {
        GilCategory? best=null;DateTimeOffset bestAt=default;
        foreach(var (addon,category) in Windows)
            if(category!=GilCategory.Other && lastSeen.TryGetValue(addon,out var seen) && now-seen<=ContextWindow && (best is null || seen>bestAt))
            {best=category;bestAt=seen;}
        return best ?? GilCategory.Other;
    }

    /// <summary>Entries kept: the last 90 days, the newest 20 000 at most, oldest first.</summary>
    public static List<GilEntry> Prune(IEnumerable<GilEntry> entries,DateTimeOffset now)
    {
        var kept=entries.Where(entry=>now-entry.At<=TimeSpan.FromDays(KeepDays) && entry.At<=now.AddMinutes(5)).OrderBy(entry=>entry.At).ToList();
        if(kept.Count>MaximumEntries)kept.RemoveRange(0,kept.Count-MaximumEntries);
        return kept;
    }

    /// <summary>Gains and spendings per category since a time, for one source.</summary>
    public static IReadOnlyList<(GilCategory Category,long Gained,long Spent)> Totals(IEnumerable<GilEntry> entries,GilSource source,DateTimeOffset since)
        =>entries.Where(entry=>entry.Source==source && entry.At>=since).GroupBy(entry=>entry.Category)
            .Select(group=>(group.Key,group.Where(entry=>entry.Delta>0).Sum(entry=>entry.Delta),group.Where(entry=>entry.Delta<0).Sum(entry=>-entry.Delta)))
            .OrderByDescending(total=>total.Item2+total.Item3).ToArray();

    private static readonly JsonSerializerOptions Json=new(){MaxDepth=8};
    private sealed record FileDto(int Schema,GilEntry[] Entries);

    /// <summary>Reads a journal file (a missing file is an empty journal; a bad one throws, so it is never overwritten by mistake).</summary>
    public static List<GilEntry> Read(string path)
    {
        if(!File.Exists(path))return [];
        var info=new FileInfo(path);
        if(info.Length>8*1024*1024)throw new InvalidDataException("Gil journal too large.");
        var dto=JsonSerializer.Deserialize<FileDto>(File.ReadAllBytes(path),Json) ?? throw new InvalidDataException("Empty gil journal.");
        if(dto.Schema!=1 || dto.Entries is null)throw new InvalidDataException("Unknown gil journal schema.");
        return dto.Entries.ToList();
    }

    /// <summary>Writes a journal file through a temporary file (a crash never leaves half a file).</summary>
    public static void Write(string path,IReadOnlyList<GilEntry> entries)
    {
        AtomicFile.Write(path,JsonSerializer.SerializeToUtf8Bytes(new FileDto(1,entries.ToArray()),Json));
    }
}
