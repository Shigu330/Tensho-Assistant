using System.Text.RegularExpressions;

namespace TenshoAssistant.Reminders;

public enum GardenChatKind { PlantedBed, PlantedPot, Tended, Uprooted, Harvested }

/// <summary>A planting the game announced in the chat: patch and bed for a garden patch, nothing for a flowerpot.</summary>
public sealed record GardenChatEvent(GardenChatKind Kind,int? Patch,int? Bed);

/// <summary>
/// 0.460 (user request, in-game campaign): the garden journal fills itself from the game's own chat lines. Each line is the LogMessage row in the client's
/// language: its text parts stay literal and its numbers are read where the row puts them (docs/evidence/0.459-garden-detection.md). English and French name
/// the bed first, German and Japanese the patch. Only lines without a sender count: a player typing the same words is ignored.
/// </summary>
public sealed class GardenChat
{
    private readonly Regex? plantedBed,plantedPot;
    private readonly bool bedFirst;
    // 0.616: lines without a number, compared without spaces or invisible hyphens: the game writes « vigueur! » in the chat and « vigueur ! » in its sheet.
    private readonly HashSet<string> tended,uprooted;

    /// <param name="plantedBed">LogMessage 4015, in parts: text, or null for a number.</param>
    /// <param name="plantedPot">LogMessage 3448, in parts.</param>
    /// <param name="tended">LogMessage 4017 and 3450: bed and flowerpot.</param>
    /// <param name="uprooted">LogMessage 4025 and 3452: bed and flowerpot.</param>
    public GardenChat(IReadOnlyList<string?>? plantedBed,IReadOnlyList<string?>? plantedPot,bool bedFirst,IEnumerable<string?>? tended=null,IEnumerable<string?>? uprooted=null)
    {
        this.plantedBed=Pattern(plantedBed);this.plantedPot=Pattern(plantedPot);this.bedFirst=bedFirst;
        this.tended=Lines(tended);this.uprooted=Lines(uprooted);
    }
    private static HashSet<string> Lines(IEnumerable<string?>? lines)=>(lines ?? []).Select(Bare).Where(line=>line.Length>0).ToHashSet();
    internal static string Bare(string? text)=>string.Concat((text ?? "").Where(character=>!char.IsWhiteSpace(character) && character!='\u00AD'));

    public static bool BedComesFirst(string language)=>language is "English" or "French";

    /// <summary>X-581 (0.1572): the bed's state lines (null: not read; the journal's times alone).</summary>
    public GardenTalk? Talk { get; init; }

    /// <summary>#36 (0.978, docs/evidence/0.978-garden-harvest.md): EObjName 2003757, « emplacement », a garden bed: the target while harvesting.</summary>
    public const uint BedObject=2003757;
    /// <summary>#36 (0.978): a harvest is the ordinary item gain line (chat type 62, no sender) while the target is a garden bed (the caller checks housing).
    /// X-382 (0.1360, dalamud.log of 14/11 23:48: « Garden menu read … bed 6 », then « Vous obtenez 9 carottes de Krakka » 1.5 s later, the harvest
    /// not seen): or a bed's menu read here just before (<paramref name="bedMenuJustSeen"/>, within <see cref="GardenMenuMemory.HarvestWindow"/>),
    /// the target being no longer the bed when the gain line comes.
    /// X-410 M1 (0.1378): with only the menu to go on, the gain line must name a garden item (<paramref name="gardenItem"/> true); a line naming
    /// no item (null) keeps the menu rule; one naming only other items (false: a purchase, an item received after a menu closed) is no harvest.</summary>
    public static bool IsHarvest(int chatType,string sender,uint target,bool bedMenuJustSeen=false,bool? gardenItem=null)=>
        chatType==62 && sender.Length==0 && (target==BedObject || bedMenuJustSeen && gardenItem!=false);

    /// <summary>X-410 M1 (0.1378): whether a gain line names a garden item: null when it names no item, else whether one of them is.</summary>
    public static bool? GardenItem(IReadOnlyCollection<uint> items,Func<uint,bool> isGardenItem)=>items.Count==0 ? null:items.Any(isGardenItem);

    private static Regex? Pattern(IReadOnlyList<string?>? parts)
    {
        if(parts is null || !parts.Any(part=>part is { Length:>0 }))return null;
        return new("^"+string.Concat(parts.Select(part=>part is null ? @"\D*(\d+)\D*?":Regex.Escape(part)))+"$",GardenJournal.Linear,GardenJournal.MatchTimeout);
    }

    /// <summary>The planting a line announces, or null. A line with a sender (a player) never counts.</summary>
    public GardenChatEvent? Read(string sender,string text)
    {
        if(sender.Length>0 || text.Length is 0 or >200)return null;
        var bare=Bare(text);
        if(tended.Contains(bare))return new(GardenChatKind.Tended,null,null);
        if(uprooted.Contains(bare))return new(GardenChatKind.Uprooted,null,null);
        if(plantedPot?.IsMatch(text)==true)return new(GardenChatKind.PlantedPot,null,null);
        if(plantedBed?.Match(text) is not { Success:true, Groups.Count:3 } match)return null;
        if(!int.TryParse(match.Groups[1].Value,out var first) || !int.TryParse(match.Groups[2].Value,out var second))return null;
        var (bed,patch)=bedFirst ? (first,second):(second,first);
        return bed is >= 1 and <= 8 && patch is >= 1 and <= 3 ? new(GardenChatKind.PlantedBed,patch,bed):null;
    }
}

/// <summary>
/// X-179 (0.1165, user's finding of 2026-11-12): the title of the game's menu on a garden bed (« Potager 1, emplacement 6 »), Addon row 6420 in the
/// client's language, read with its own numbers: lnum1 is the patch, lnum2 the bed, in every language (« <ordinal(lnum2)> Bed, <ordinal(lnum1)> Patch »
/// in English, « Beet <num(lnum1)>, Furche <num(lnum2)> » in German; docs/evidence/0.1165-garden-menu.md). A row with any other macro is not used: nothing is guessed.
/// </summary>
public sealed class GardenMenuTitle
{
    public const uint AddonRow=6420;
    private readonly Regex pattern;
    private readonly int patchGroup,bedGroup;
    private GardenMenuTitle(Regex pattern,int patchGroup,int bedGroup){this.pattern=pattern;this.patchGroup=patchGroup;this.bedGroup=bedGroup;}

    /// <summary>From the row's macro text (Lumina's ToMacroString); null when it is not exactly text, the patch number and the bed number.</summary>
    public static GardenMenuTitle? From(string? macro)
    {
        if(string.IsNullOrWhiteSpace(macro))return null;
        var tokens=Regex.Matches(macro,@"<[^>]*>",GardenJournal.Linear,GardenJournal.MatchTimeout);
        var built=new System.Text.StringBuilder("^");
        var at=0;int patch=0,bed=0,group=0;
        foreach(Match token in tokens)
        {
            built.Append(Regex.Escape(macro[at..token.Index]));
            at=token.Index+token.Length;
            var number=Regex.Match(token.Value,@"^<(num|ordinal)\(lnum([12])\)>$",GardenJournal.Linear,GardenJournal.MatchTimeout);
            if(!number.Success)return null;
            group++;
            if(number.Groups[2].Value=="1"){if(patch!=0)return null;patch=group;}else{if(bed!=0)return null;bed=group;}
            built.Append(number.Groups[1].Value=="ordinal" ? @"(\d+)(?:st|nd|rd|th)?":@"(\d+)");
        }
        built.Append(Regex.Escape(macro[at..])).Append('$');
        if(patch==0 || bed==0)return null;
        return new(new Regex(built.ToString(),GardenJournal.Linear,GardenJournal.MatchTimeout),patch,bed);
    }

    /// <summary>The patch (1 to 3) and bed (1 to 8) a menu title names, or null.</summary>
    public (int Patch,int Bed)? Read(string? text)
    {
        if(text is null || text.Length is 0 or >80)return null;
        var match=pattern.Match(text.Trim());
        if(!match.Success || !int.TryParse(match.Groups[patchGroup].Value,out var patch) || !int.TryParse(match.Groups[bedGroup].Value,out var bed))return null;
        return patch is >= 1 and <= 3 && bed is >= 1 and <= 8 ? (patch,bed):null;
    }
}

/// <summary>
/// X-185 (0.1211, N23): whether a harvest line is a new harvest. A bed just read in its menu, other than the last one harvested, always is (two beds
/// harvested within seconds); without it, only 5 s after the last harvest (0.997: one harvest prints several gain lines, the crop then its seeds).
/// </summary>
public static class GardenHarvests
{
    public static readonly TimeSpan SameHarvest=TimeSpan.FromSeconds(5);
    public static bool IsNew(DateTimeOffset at,DateTimeOffset lastAt,(int Patch,int Bed)? slot,(int Patch,int Bed)? lastSlot)
        =>slot is not null && slot!=lastSlot || at-lastAt>=SameHarvest;
}

/// <summary>
/// X-179 (0.1165): the last garden bed whose menu was seen, for the tending or uprooting line that follows (the line itself does not say which bed).
/// Used once, at the same place only, within 30 seconds; otherwise forgotten.
/// </summary>
public sealed class GardenMenuMemory
{
    public static readonly TimeSpan Window=TimeSpan.FromSeconds(30);
    /// <summary>X-382 (0.1360): a gain line within this time of a bed's menu, at the same place, is that bed's harvest (1.3 to 1.6 s in the log of 14/11).
    /// X-395 M3 (0.1366): 4 s instead of 10 s, so that a menu closed without anything done is soon forgotten.</summary>
    public static readonly TimeSpan HarvestWindow=TimeSpan.FromSeconds(4);
    private readonly object gate=new();
    private (int Patch,int Bed,string? Place,DateTimeOffset At)? last;
    public void Seen(int patch,int bed,string? place,DateTimeOffset at){lock(gate)last=(patch,bed,place,at);}
    /// <summary>X-395 M3 (0.1366): a planting closes the menu's story: its bed is not the next gain's harvest.</summary>
    public void Forget(){lock(gate)last=null;}
    /// <summary>X-382 (0.1360): the bed of the menu seen at this place within <paramref name="window"/>, without forgetting it.</summary>
    public (int Patch,int Bed)? Peek(string? place,DateTimeOffset now,TimeSpan window)
    {
        lock(gate)
        {
            if(last is not { } seen || now<seen.At || now-seen.At>window || string.IsNullOrEmpty(place) || seen.Place!=place)return null;
            return (seen.Patch,seen.Bed);
        }
    }

    public (int Patch,int Bed)? Take(string? place,DateTimeOffset now)
    {
        lock(gate)
        {
            if(last is not { } seen)return null;
            last=null;
            if(now<seen.At || now-seen.At>Window || string.IsNullOrEmpty(place) || seen.Place!=place)return null;
            return (seen.Patch,seen.Bed);
        }
    }
}

/// <summary>X-581 (0.1572): what the game says of a bed when it is opened (docs/evidence/0.1572-garden-ripe.md): None (not said), growing well,
/// growing but in need of care, ripe, or withered.</summary>
public enum GardenGameState { None, Growing, Wilting, Ripe, Withered }

/// <summary>
/// X-581 (0.1572, user report of 18/11: four beds said « Ripe » that were not): the line the game writes in the chat when a bed is opened, from
/// its own event sheet <see cref="Sheet"/> (rows TALK_VIGOROUS, TALK_DEPRESSED, TALK_RIPE, TALK_DEAD, in the client's language): « Le semis de
/// carotte de Krakka se porte bien. », « … est arrivée à maturité ! ». Each row in parts (text, or null for a macro: the crop's name, a
/// non-breaking space), compared without spaces like the other garden lines. The only sure word on a bed's ripeness. Pure (tests).
/// </summary>
public sealed class GardenTalk
{
    public const string Sheet="custom/001/CmnDefHousingGardeningPlant_00151";
    private readonly (Regex Pattern,GardenGameState State)[] lines;
    private GardenTalk((Regex Pattern,GardenGameState State)[] lines){this.lines=lines;}

    /// <summary>The state a row of the sheet tells, by its key (column 0), or None for the other rows (menu choices, questions).</summary>
    public static GardenGameState StateOf(string? key)=>(key ?? "") switch
    {
        var text when text.EndsWith("_TALK_VIGOROUS",StringComparison.Ordinal)=>GardenGameState.Growing,
        var text when text.EndsWith("_TALK_DEPRESSED",StringComparison.Ordinal)=>GardenGameState.Wilting,
        var text when text.EndsWith("_TALK_RIPE",StringComparison.Ordinal)=>GardenGameState.Ripe,
        var text when text.EndsWith("_TALK_DEAD",StringComparison.Ordinal)=>GardenGameState.Withered,
        _=>GardenGameState.None,
    };

    /// <summary>The rules from the sheet's rows (key, text in parts); null unless the four states are all there (perhaps after a game update).</summary>
    public static GardenTalk? From(IEnumerable<(string Key,IReadOnlyList<string?>? Parts)> rows)
    {
        var lines=new List<(Regex,GardenGameState)>();
        foreach(var (key,parts) in rows)
        {
            var state=StateOf(key);
            if(state==GardenGameState.None || parts is null || !parts.Any(part=>!string.IsNullOrEmpty(GardenChat.Bare(part))))continue;
            Regex pattern=new("^"+string.Concat(parts.Select(part=>part is null ? ".*?":Regex.Escape(GardenChat.Bare(part))))+"$",GardenJournal.Linear,GardenJournal.MatchTimeout);
            lines.Add((pattern,state));
        }
        return lines.Select(line=>line.Item2).Distinct().Count()==4 ? new(lines.ToArray()):null;
    }

    /// <summary>The state a chat line tells, or None. A line with a sender (a player) never counts.</summary>
    public GardenGameState Read(string sender,string text)
    {
        if(sender.Length>0 || text.Length is 0 or >200)return GardenGameState.None;
        var bare=GardenChat.Bare(text);
        foreach(var (pattern,state) in lines)if(pattern.IsMatch(bare))return state;
        return GardenGameState.None;
    }
}

/// <summary>
/// X-581 (0.1572): which bed a state line is about. The game writes it just before the bed's menu opens (dalamud.log and chat history of
/// 19/11 23:08 to 23:09: eight beds, the line 65 to 85 ms before « Garden menu read »): the menu that opens at the same place within
/// <see cref="Window"/> after the line is that bed's. A line is used once; a menu opening without a line just before tells nothing.
/// </summary>
public sealed class GardenTalkMemory
{
    public static readonly TimeSpan Window=TimeSpan.FromSeconds(1);
    private readonly object gate=new();
    private (GardenGameState State,string Place,DateTimeOffset At,string? Text)? said;

    /// <param name="text">X-628 M2 (0.1576): the game's line itself, kept with its state.</param>
    public void Said(GardenGameState state,string? place,DateTimeOffset at,string? text=null)
    {
        lock(gate)said=state==GardenGameState.None || string.IsNullOrEmpty(place) ? null:(state,place,at,text);
    }

    /// <summary>A bed's menu opened at this place: the state said just before, or None.</summary>
    public GardenGameState Opened(string? place,DateTimeOffset at)=>Opened(place,at,out _);

    /// <summary>The same, with the game's line (null with None).</summary>
    public GardenGameState Opened(string? place,DateTimeOffset at,out string? text)
    {
        lock(gate)
        {
            text=null;
            if(said is not { } line)return GardenGameState.None;
            said=null;
            if(place!=line.Place || at<line.At || at-line.At>Window)return GardenGameState.None;
            text=line.Text;
            return line.State;
        }
    }
}
