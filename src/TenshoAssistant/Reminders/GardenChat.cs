using System.Text.RegularExpressions;

namespace TenshoAssistant.Reminders;

public enum GardenChatKind { PlantedBed, PlantedPot, Tended, Uprooted }

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
