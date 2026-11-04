namespace TenshoAssistant.MyTools;

/// <summary>A text in several languages; a tool shows the player's language, then English, then the first one written.</summary>
public sealed record TemplateText(IReadOnlyDictionary<string,string> Values)
{
    public static readonly IReadOnlyList<string> Languages=["en","fr","de","ja"];
    public string For(string language)
        =>Values.TryGetValue(language,out var text) ? text:Values.TryGetValue("en",out var english) ? english:Languages.Select(code=>Values.GetValueOrDefault(code)).FirstOrDefault(value=>value is not null) ?? "";
}

public enum TemplateReset { None,Daily,Weekly }
public enum TemplateTimer { Daily,Weekly,Supply,Hour }

public abstract record TemplateBlock;
public sealed record HeadingBlock(TemplateText Text):TemplateBlock;
public sealed record NoteBlock(TemplateText Text):TemplateBlock;
public sealed record ChecklistBlock(TemplateReset Reset,IReadOnlyList<TemplateText> Entries):TemplateBlock;
/// <param name="Target">0: no target.</param>
public sealed record CounterBlock(TemplateText Label,int Target):TemplateBlock;
/// <param name="HourUtc">Only for <see cref="TemplateTimer.Hour"/>.</param>
public sealed record TimerBlock(TemplateText Label,TemplateTimer Timer,int HourUtc):TemplateBlock;
public sealed record TemplateItem(uint Id,int Wanted);
public sealed record ItemListBlock(IReadOnlyList<TemplateItem> Items):TemplateBlock;
public sealed record LinkBlock(TemplateText Label,string Url):TemplateBlock;

/// <summary>
/// TEMPLATES-001 (DEC-029): a tool made of data. It describes a page built from Tensho's blocks and holds no code, command or macro: nothing in it is ever run or sent to the game. The names of
/// items always come from the game's data.
/// </summary>
/// <param name="Theme">T8: the one setting a tool may propose, by name (<see cref="ToolTemplateFormat.Themes"/>). Applied only on the player's click.</param>
public sealed record ToolTemplate(string Id,TemplateText Name,string? Author,string? License,IReadOnlyList<TemplateBlock> Blocks,string? Theme=null);
