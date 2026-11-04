using System.Security.Cryptography;

namespace TenshoAssistant.MyTools;

public enum ToolBlockKind { Heading,Note,Checklist,Counter,Timer,Items,Link }

/// <summary>
/// TEMPLATES-001: a tool being edited in the game. It may be incomplete while it is written; saving goes through the same strict format as a shared file, so the editor cannot make what an import refuses.
/// Texts are written in the player's language; the other languages of a tool are kept.
/// </summary>
public sealed class ToolDraft
{
    public const string FreeLicense="CC0-1.0";
    public string Id{get;}
    public TemplateText Name{get;set;}
    public string Author{get;set;}
    public bool FreeToShare{get;set;}
    public List<TemplateBlock> Blocks{get;}

    private ToolDraft(string id,TemplateText name,string author,bool free,List<TemplateBlock> blocks){Id=id;Name=name;Author=author;FreeToShare=free;Blocks=blocks;}
    public static ToolDraft New()=>new("my-"+Convert.ToHexString(RandomNumberGenerator.GetBytes(5)).ToLowerInvariant(),Empty,"",false,[]);
    public static ToolDraft From(ToolTemplate template)=>new(template.Id,template.Name,template.Author ?? "",template.License==FreeLicense,template.Blocks.ToList());

    public static readonly TemplateText Empty=new(new Dictionary<string,string>());
    public static TemplateText With(TemplateText text,string language,string value)
    {
        var values=new Dictionary<string,string>(text.Values);
        if(string.IsNullOrWhiteSpace(value))values.Remove(language);else values[language]=value;
        return new(values);
    }
    public static TemplateBlock NewBlock(ToolBlockKind kind)=>kind switch
    {
        ToolBlockKind.Heading=>new HeadingBlock(Empty),
        ToolBlockKind.Note=>new NoteBlock(Empty),
        ToolBlockKind.Checklist=>new ChecklistBlock(TemplateReset.None,[Empty]),
        ToolBlockKind.Counter=>new CounterBlock(Empty,0),
        ToolBlockKind.Timer=>new TimerBlock(Empty,TemplateTimer.Daily,0),
        ToolBlockKind.Items=>new ItemListBlock([]),
        ToolBlockKind.Link=>new LinkBlock(Empty,"https://"),
        _=>throw new ArgumentOutOfRangeException(nameof(kind)),
    };
    public bool Move(int index,int offset)
    {
        var target=index+offset;
        if(index<0 || index>=Blocks.Count || target<0 || target>=Blocks.Count)return false;
        (Blocks[index],Blocks[target])=(Blocks[target],Blocks[index]);
        return true;
    }
    public ToolTemplate ToTemplate()=>new(Id,Name,Author.Trim().Length>0 ? Author.Trim():null,FreeToShare ? FreeLicense:null,Blocks.ToArray());
    /// <summary>The tool as it will be saved, or what stops it.</summary>
    public TemplateParse Check()=>ToolTemplateFormat.Parse(ToolTemplateFormat.Serialize(ToTemplate()));
}
