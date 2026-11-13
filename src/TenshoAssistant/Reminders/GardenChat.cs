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
    private static string Bare(string? text)=>string.Concat((text ?? "").Where(character=>!char.IsWhiteSpace(character) && character!='\u00AD'));

    public static bool BedComesFirst(string language)=>language is "English" or "French";

    /// <summary>#36 (0.978, docs/evidence/0.978-garden-harvest.md): EObjName 2003757, « emplacement », a garden bed: the target while harvesting.</summary>
    public const uint BedObject=2003757;
    /// <summary>#36 (0.978): a harvest is the ordinary item gain line (chat type 62, no sender) while the target is a garden bed (the caller checks housing).</summary>
    public static bool IsHarvest(int chatType,string sender,uint target)=>chatType==62 && sender.Length==0 && target==BedObject;

    private static Regex? Pattern(IReadOnlyList<string?>? parts)
    {
        if(parts is null || !parts.Any(part=>part is { Length:>0 }))return null;
        return new("^"+string.Concat(parts.Select(part=>part is null ? @"\D*(\d+)\D*?":Regex.Escape(part)))+"$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(50));
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
        var tokens=Regex.Matches(macro,@"<[^>]*>",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(50));
        var built=new System.Text.StringBuilder("^");
        var at=0;int patch=0,bed=0,group=0;
        foreach(Match token in tokens)
        {
            built.Append(Regex.Escape(macro[at..token.Index]));
            at=token.Index+token.Length;
            var number=Regex.Match(token.Value,@"^<(num|ordinal)\(lnum([12])\)>$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(50));
            if(!number.Success)return null;
            group++;
            if(number.Groups[2].Value=="1"){if(patch!=0)return null;patch=group;}else{if(bed!=0)return null;bed=group;}
            built.Append(number.Groups[1].Value=="ordinal" ? @"(\d+)(?:st|nd|rd|th)?":@"(\d+)");
        }
        built.Append(Regex.Escape(macro[at..])).Append('$');
        if(patch==0 || bed==0)return null;
        return new(new Regex(built.ToString(),RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(50)),patch,bed);
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
    private readonly object gate=new();
    private (int Patch,int Bed,string? Place,DateTimeOffset At)? last;
    public void Seen(int patch,int bed,string? place,DateTimeOffset at){lock(gate)last=(patch,bed,place,at);}
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
