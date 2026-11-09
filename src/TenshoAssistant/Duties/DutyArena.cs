using System.Text;
using System.Text.Json;

namespace TenshoAssistant.Duties;

public enum ArenaShape { Round, Square }
/// <summary>A mark on the arena drawing: a short label (a waymark, a role, a number) at a place from -1 to 1 on each axis (north up).</summary>
public sealed class ArenaToken { public string Label { get; set; } = ""; public float X { get; set; } public float Y { get; set; } }
/// <summary>X-90 (0.995, DEC-045): the player's own note and arena drawing for one duty.</summary>
public sealed class DutyNote
{
    public string Text { get; set; } = "";
    public ArenaShape Shape { get; set; }
    public List<ArenaToken> Tokens { get; set; } = [];
}

/// <summary>
/// X-90 (0.995, DEC-045 « notes and a drawing of the arena »; DEC-041: preparation only): the limits of a note, and its share code « TENSHO-ARENA1: » +
/// Base64 of a small JSON (the duty, the text, the shape, the marks). Nothing is drawn over the game, nothing is placed in it: the marks are the player's own.
/// </summary>
public static class DutyArena
{
    public const int MaxTokens=24,MaxLabel=4,MaxText=2000,MaxNotes=500;
    public const string Prefix="TENSHO-ARENA1:";
    /// <summary>The labels offered in one click: the game's waymark names and the usual roles; any other short label can be typed.</summary>
    public static readonly string[] QuickLabels=["A","B","C","D","1","2","3","4","T1","T2","H1","H2","D1","D2","D3","D4"];
    public static float Clamp(float value)=>float.IsFinite(value) ? Math.Clamp(value,-1,1):0;
    public static string CleanLabel(string label)=>new string((label ?? "").Trim().Where(character=>!char.IsControl(character)).Take(MaxLabel).ToArray());

    private sealed record Shared(uint Duty,string Text,ArenaShape Shape,List<(string Label,float X,float Y)> Tokens);
    private sealed record SharedDto(uint Duty,string Text,int Shape,string[] Labels,float[] X,float[] Y);
    public static string Share(uint duty,DutyNote note)
    {
        var tokens=note.Tokens.Take(MaxTokens).ToArray();
        var dto=new SharedDto(duty,note.Text.Length<=MaxText ? note.Text:note.Text[..MaxText],(int)note.Shape,tokens.Select(token=>CleanLabel(token.Label)).ToArray(),
            tokens.Select(token=>Clamp(token.X)).ToArray(),tokens.Select(token=>Clamp(token.Y)).ToArray());
        return Prefix+Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dto)));
    }
    /// <summary>The duty and the note of a share code, or null when it is not one (wrong prefix, too long, broken, out of bounds).</summary>
    public static (uint Duty,DutyNote Note)? Parse(string? code)
    {
        var text=(code ?? "").Trim();
        if(!text.StartsWith(Prefix,StringComparison.Ordinal) || text.Length>20_000)return null;
        try
        {
            var dto=JsonSerializer.Deserialize<SharedDto>(Encoding.UTF8.GetString(Convert.FromBase64String(text[Prefix.Length..])));
            if(dto is null || dto.Duty==0 || dto.Labels is null || dto.X is null || dto.Y is null || dto.Labels.Length>MaxTokens
               || dto.X.Length!=dto.Labels.Length || dto.Y.Length!=dto.Labels.Length || (dto.Text ?? "").Length>MaxText || dto.Shape is <0 or >1)return null;
            var note=new DutyNote{Text=dto.Text ?? "",Shape=(ArenaShape)dto.Shape,
                Tokens=dto.Labels.Select((label,index)=>new ArenaToken{Label=CleanLabel(label),X=Clamp(dto.X[index]),Y=Clamp(dto.Y[index])}).Where(token=>token.Label.Length>0).ToList()};
            return (dto.Duty,note);
        }
        catch(Exception error) when (error is FormatException or JsonException or ArgumentException or DecoderFallbackException){return null;}
    }
}
