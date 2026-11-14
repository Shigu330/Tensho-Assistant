using System.Text.Json;

namespace TenshoAssistant.Inventory;

/// <summary>Whose gil changed: the character, their retainers together, or the company chest.</summary>
public enum GilSource { Character, Retainers, Company }

/// <summary>What the change most likely was, from the game window open when it happened (Other when none matched).</summary>
// 0.903 (X-84): Mail: gil taken from a letter or attached to one (LetterViewer, LetterEditor). Added last: the saved numbers stay the same.
// 0.1102: OceanFishing: the gil of an ocean fishing voyage's missions (IKDMission, IKDFishingLog), added last too.
// 0.1319 (X-320): Fate (the FATE's reward window), Defeat (the gil a defeated monster gives), ChallengeLog (an entry of the challenge log
// done), added last too.
public enum GilCategory { Other, NpcShop, Market, Repair, Teleport, Retainer, Company, Mail, OceanFishing, Fate, Defeat, ChallengeLog }

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
        // 0.1102 (relais 20, dalamud.log of 11/11 15:12:48 and 15:21:02): « Gil other: +2,000 / +1,000, windows seen in the last 15 s: Bait,
        // IKDFishingLog, IKDMission, Catch… »: the reward of a voyage's mission, while its windows are open.
        ("IKDMission",GilCategory.OceanFishing),("IKDFishingLog",GilCategory.OceanFishing),
        // 0.775 (X-18): the game takes the teleport's price on arrival, after its window closed: the log showed « Gil other: -1,240, windows seen in the last
        // 15 s: NowLoading » with the chat line « Vous avez dépensé 1240 gils pour vous téléporter ». Only a spending counts (see GilWatcher).
        ("NowLoading",GilCategory.Teleport),
        // 0.1319 (X-320, dalamud.log of 11/12 and 11/13): « Gil other: +84 … +90, windows seen in the last 15 s: FateReward », with the chat lines
        // « Vous obtenez 242 sceaux du Maelstrom. » and « Vous obtenez 84 gils. » at the same second: a FATE's reward.
        ("FateReward",GilCategory.Fate),
    ];

    /// <summary>
    /// X-320 (0.1319): the game's log messages that tell a gain without a window, by LogMessage row (language-independent; French texts of the
    /// sheet, XbmProbe): 3780 « La rubrique … de votre carnet d'objectifs est achevée ! » (dalamud.log of 11/12 15:31:42 and 11/13 14:17:28:
    /// +1,000, +2,000 and +10,000 at the same second); 557 « … a vaincu … » (ItemGains.DefeatMessage; 11/11 22:39: « Vous avez vaincu le
    /// chasse-nuages. » then « Vous obtenez 63 gils. »). docs/evidence/0.1319-gil-other.md.
    /// </summary>
    public static readonly IReadOnlyList<(uint LogMessage,GilCategory Category)> Messages=[(3780,GilCategory.ChallengeLog),(557,GilCategory.Defeat)];

    /// <summary>
    /// X-350 (0.1334, review of the block 0.1318 to 0.1329, R03.2 and DEC-038): a log message of <see cref="Messages"/> noted at
    /// <paramref name="now"/>, never while Tensho is cut (<paramref name="cut"/>: PvP, ranked content, Triple Triad, Mahjong). Whether it was noted.
    /// </summary>
    public static bool NoteMessage(IDictionary<uint,DateTimeOffset> seen,uint messageId,DateTimeOffset now,bool cut)
    {
        if(cut)return false;
        foreach(var (id,_) in Messages)
            if(messageId==id){seen[id]=now;return true;}
        return false;
    }

    /// <summary>X-320: the gil follows its message within the second (read once a second here): a message counts 5 seconds, not 15.</summary>
    public static readonly TimeSpan MessageWindow=TimeSpan.FromSeconds(5);

    /// <summary>
    /// 0.775: a gain is never a teleport, even right after a loading screen. X-320 (0.1319): a FATE, a defeat or the challenge log only give
    /// gil: a spending next to them stays « Other ».
    /// </summary>
    public static GilCategory ForDelta(GilCategory category,long delta)=>category switch
    {
        GilCategory.Teleport when delta>0=>GilCategory.Other,
        GilCategory.Fate or GilCategory.Defeat or GilCategory.ChallengeLog when delta<0=>GilCategory.Other,
        _=>category,
    };

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
    /// <remarks>
    /// X-320 (0.1319): with the log messages seen lately (<paramref name="messagesSeen"/>, by LogMessage row): the newest window or message
    /// wins, except a defeat, which only counts when nothing else does (a FATE's reward or a challenge log entry comes among defeats).
    /// </remarks>
    public static GilCategory Classify(IReadOnlyDictionary<string,DateTimeOffset> lastSeen,DateTimeOffset now,IReadOnlyDictionary<uint,DateTimeOffset>? messagesSeen=null)
    {
        GilCategory? best=null;DateTimeOffset bestAt=default;
        foreach(var (addon,category) in Windows)
            if(category!=GilCategory.Other && lastSeen.TryGetValue(addon,out var seen) && now-seen<=ContextWindow && (best is null || seen>bestAt))
            {best=category;bestAt=seen;}
        var defeat=false;
        foreach(var (message,category) in Messages)
            if(messagesSeen is not null && messagesSeen.TryGetValue(message,out var seen) && now-seen<=MessageWindow)
            {
                if(category==GilCategory.Defeat){defeat=true;continue;}
                if(best is null || seen>bestAt){best=category;bestAt=seen;}
            }
        return best ?? (defeat ? GilCategory.Defeat:GilCategory.Other);
    }

    /// <summary>Entries kept: the last 90 days, the newest 20 000 at most, oldest first.</summary>
    public static List<GilEntry> Prune(IEnumerable<GilEntry> entries,DateTimeOffset now)
    {
        var kept=entries.Where(entry=>now-entry.At<=TimeSpan.FromDays(KeepDays) && entry.At<=now.AddMinutes(5)).OrderBy(entry=>entry.At).ToList();
        if(kept.Count>MaximumEntries)kept.RemoveRange(0,kept.Count-MaximumEntries);
        return kept;
    }

    /// <summary>
    /// X-119 (0.1147): the balances last recorded in a journal, per source (character, retainers, company chest), for the desktop application
    /// which reads no balance from the game: the newest entry of each source, null when the journal holds none (never a guessed zero, R06).
    /// </summary>
    public static (long? Gil,long? Retainers,long? Company) LastBalances(IEnumerable<GilEntry> entries)
    {
        long? Last(GilSource source)=>entries.Where(entry=>entry.Source==source).OrderBy(entry=>entry.At).Select(entry=>(long?)entry.Balance).LastOrDefault();
        return (Last(GilSource.Character),Last(GilSource.Retainers),Last(GilSource.Company));
    }

    /// <summary>Gains and spendings per category since a time, for one source.</summary>
    /// <summary>X-107 (0.986): the character's gil gained and spent since local midnight, transfers with retainers and the company chest left out.</summary>
    public static (long Gained,long Spent) Today(IEnumerable<GilEntry> entries,DateTimeOffset now)
    {
        var since=new DateTimeOffset(now.ToLocalTime().Date,now.ToLocalTime().Offset);
        var today=entries.Where(entry=>entry.Source==GilSource.Character && entry.At>=since && !IsTransfer(entry.Category)).ToArray();
        return (today.Where(entry=>entry.Delta>0).Sum(entry=>entry.Delta),today.Where(entry=>entry.Delta<0).Sum(entry=>-entry.Delta));
    }
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
