using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TenshoAssistant.MyTools;

/// <summary>Why a tool was refused: <see cref="Code"/> names a text of the page, <see cref="Where"/> the place in the file.</summary>
public sealed record TemplateProblem(string Code,string Where="");
public sealed record TemplateParse(ToolTemplate? Template,TemplateProblem? Problem);

/// <summary>
/// TEMPLATES-001 (DEC-029): a tool's file is untrusted. The format is strict: an unknown or repeated field, an unknown block or a value out of bounds refuses the whole file, with the reason. Texts are
/// plain: control characters, the game's glyphs and text direction marks are removed. A link is https only. No command, macro, file path or game payload has a place in it.
/// </summary>
public static partial class ToolTemplateFormat
{
    public const int Schema=1,MaxBytes=65536,MaxBlocks=100,MaxEntries=100,MaxItems=200,MaxLabel=200,MaxNote=2000,MaxLink=300,MaxAuthor=40,MaxWanted=999999,MaxTarget=999999;
    public const string SharePrefix="TENSHO1:";
    /// <summary>T8: the settings a tool may propose are this list and nothing else. Same order as PluginTheme; a theme's ThemeMode is its position plus one (0 follows the game).</summary>
    public static readonly IReadOnlyList<string> Themes=["dark","light","classic-ff","clear-blue","clear-white","clear-green"];
    public static int ThemeMode(string theme)=>Math.Max(0,Themes.ToList().IndexOf(theme)+1);
    private const int MaxShareChars=120000;

    private sealed class Refused(string code,string where=""):Exception(code)
    {
        public string Code{get;}=code;
        public string Where{get;}=where;
    }
    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,46}[a-z0-9]$")] private static partial Regex IdRule();
    [GeneratedRegex("^[A-Za-z0-9.+-]{1,40}$")] private static partial Regex LicenseRule();
    public static bool ValidId(string? id)=>id is not null && IdRule().IsMatch(id);

    public static string Clean(string? text)
    {
        if(string.IsNullOrEmpty(text))return "";
        var clean=new StringBuilder(text.Length);
        foreach(var letter in text)
            if(letter=='\n' || !(char.IsControl(letter) || letter is >='' and <='' or >='‪' and <='‮' or >='⁦' and <='⁩' or '‎' or '‏'))clean.Append(letter);
        return clean.ToString().Trim();
    }
    /// <summary>The address, if it can be opened: https, a real host, no account in the address.</summary>
    public static Uri? SafeLink(string? url)
        =>url is {Length:>0 and <=MaxLink} && Uri.TryCreate(url,UriKind.Absolute,out var uri) && uri.Scheme==Uri.UriSchemeHttps && uri.UserInfo.Length==0 && uri.Host.Contains('.') && !url.Any(char.IsControl) ? uri:null;

    public static TemplateParse Parse(string? json)
    {
        try
        {
            if(string.IsNullOrWhiteSpace(json))throw new Refused("Empty");
            if(Encoding.UTF8.GetByteCount(json)>MaxBytes)throw new Refused("TooLarge");
            using var document=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=8});
            return new(Read(document.RootElement),null);
        }
        catch(Refused refused){return new(null,new(refused.Code,refused.Where));}
        catch(JsonException){return new(null,new("NotJson"));}
    }

    private static Dictionary<string,JsonElement> Fields(JsonElement element,string where,params string[] allowed)
    {
        if(element.ValueKind!=JsonValueKind.Object)throw new Refused("BadValue",where);
        var fields=new Dictionary<string,JsonElement>(StringComparer.Ordinal);
        foreach(var field in element.EnumerateObject())
        {
            if(!allowed.Contains(field.Name,StringComparer.Ordinal))throw new Refused("UnknownField",$"{where}.{field.Name}");
            if(!fields.TryAdd(field.Name,field.Value))throw new Refused("RepeatedField",$"{where}.{field.Name}");
        }
        return fields;
    }
    private static JsonElement Need(Dictionary<string,JsonElement> fields,string name,string where)=>fields.TryGetValue(name,out var value) ? value:throw new Refused("Missing",$"{where}.{name}");
    private static string? OptionalText(Dictionary<string,JsonElement> fields,string name,string where)
    {
        if(!fields.TryGetValue(name,out var value))return null;
        if(value.ValueKind!=JsonValueKind.String)throw new Refused("BadValue",$"{where}.{name}");
        return value.GetString();
    }
    private static int Number(JsonElement value,int minimum,int maximum,string where)
        =>value.ValueKind==JsonValueKind.Number && value.TryGetInt32(out var number) && number>=minimum && number<=maximum ? number:throw new Refused("BadValue",where);
    private static TemplateText Text(JsonElement element,int maximum,string where)
    {
        var fields=Fields(element,where,[..TemplateText.Languages]);
        var values=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var (language,value) in fields)
        {
            if(value.ValueKind!=JsonValueKind.String)throw new Refused("BadValue",$"{where}.{language}");
            var text=Clean(value.GetString());
            if(text.Length>maximum)throw new Refused("TooLong",$"{where}.{language}");
            if(text.Length>0)values[language]=text;
        }
        return values.Count>0 ? new(values):throw new Refused("Missing",where);
    }
    private static JsonElement[] List(JsonElement element,int maximum,string where)
    {
        if(element.ValueKind!=JsonValueKind.Array)throw new Refused("BadValue",where);
        var rows=element.EnumerateArray().ToArray();
        return rows.Length<=maximum ? rows:throw new Refused("TooMany",where);
    }

    private static ToolTemplate Read(JsonElement root)
    {
        var fields=Fields(root,"tool","schema","id","name","author","license","settings","blocks");
        if(Need(fields,"schema","tool").ValueKind!=JsonValueKind.Number || !fields["schema"].TryGetInt32(out var schema) || schema!=Schema)throw new Refused("Schema");
        var id=Need(fields,"id","tool").ValueKind==JsonValueKind.String ? fields["id"].GetString():null;
        if(!ValidId(id))throw new Refused("BadId","tool.id");
        var author=Clean(OptionalText(fields,"author","tool"));
        if(author.Length>MaxAuthor)throw new Refused("TooLong","tool.author");
        var license=OptionalText(fields,"license","tool");
        if(license is not null && !LicenseRule().IsMatch(license))throw new Refused("BadValue","tool.license");
        string? theme=null;
        if(fields.TryGetValue("settings",out var settings))
        {
            var proposed=Fields(settings,"tool.settings","theme");
            theme=OptionalText(proposed,"theme","tool.settings");
            if(theme is not null && !Themes.Contains(theme,StringComparer.Ordinal))throw new Refused("BadValue","tool.settings.theme");
        }
        var rows=List(Need(fields,"blocks","tool"),MaxBlocks,"tool.blocks");
        return new(id!,Text(Need(fields,"name","tool"),MaxLabel,"tool.name"),author.Length>0 ? author:null,license,rows.Select((row,index)=>Block(row,$"blocks[{index}]")).ToArray(),theme);
    }

    private static TemplateBlock Block(JsonElement element,string where)
    {
        if(element.ValueKind!=JsonValueKind.Object || !element.TryGetProperty("type",out var kind) || kind.ValueKind!=JsonValueKind.String)throw new Refused("Missing",$"{where}.type");
        switch(kind.GetString())
        {
            case "heading":
            {
                var fields=Fields(element,where,"type","text");
                return new HeadingBlock(Text(Need(fields,"text",where),MaxLabel,$"{where}.text"));
            }
            case "note":
            {
                var fields=Fields(element,where,"type","text");
                return new NoteBlock(Text(Need(fields,"text",where),MaxNote,$"{where}.text"));
            }
            case "checklist":
            {
                var fields=Fields(element,where,"type","reset","entries");
                var reset=(OptionalText(fields,"reset",where) ?? "none") switch{"none"=>TemplateReset.None,"daily"=>TemplateReset.Daily,"weekly"=>TemplateReset.Weekly,_=>throw new Refused("BadValue",$"{where}.reset")};
                var entries=List(Need(fields,"entries",where),MaxEntries,$"{where}.entries");
                return entries.Length>0 ? new ChecklistBlock(reset,entries.Select((entry,index)=>Text(entry,MaxLabel,$"{where}.entries[{index}]")).ToArray()):throw new Refused("Missing",$"{where}.entries");
            }
            case "counter":
            {
                var fields=Fields(element,where,"type","label","target");
                return new CounterBlock(Text(Need(fields,"label",where),MaxLabel,$"{where}.label"),fields.TryGetValue("target",out var target) ? Number(target,0,MaxTarget,$"{where}.target"):0);
            }
            case "timer":
            {
                var fields=Fields(element,where,"type","label","at","hourUtc");
                var timer=OptionalText(fields,"at",where) switch{"daily"=>TemplateTimer.Daily,"weekly"=>TemplateTimer.Weekly,"supply"=>TemplateTimer.Supply,"hour"=>TemplateTimer.Hour,null=>throw new Refused("Missing",$"{where}.at"),_=>throw new Refused("BadValue",$"{where}.at")};
                if(timer!=TemplateTimer.Hour && fields.ContainsKey("hourUtc"))throw new Refused("UnknownField",$"{where}.hourUtc");
                var hour=timer==TemplateTimer.Hour ? Number(Need(fields,"hourUtc",where),0,23,$"{where}.hourUtc"):0;
                return new TimerBlock(Text(Need(fields,"label",where),MaxLabel,$"{where}.label"),timer,hour);
            }
            case "items":
            {
                var fields=Fields(element,where,"type","items");
                var rows=List(Need(fields,"items",where),MaxItems,$"{where}.items");
                if(rows.Length==0)throw new Refused("Missing",$"{where}.items");
                var items=rows.Select((row,index)=>
                {
                    var path=$"{where}.items[{index}]";
                    var item=Fields(row,path,"id","wanted");
                    if(Need(item,"id",path).ValueKind!=JsonValueKind.Number || !item["id"].TryGetUInt32(out var id) || id==0)throw new Refused("BadValue",$"{path}.id");
                    return new TemplateItem(id,item.TryGetValue("wanted",out var wanted) ? Number(wanted,1,MaxWanted,$"{path}.wanted"):1);
                }).ToArray();
                return items.DistinctBy(item=>item.Id).Count()==items.Length ? new ItemListBlock(items):throw new Refused("RepeatedItem",$"{where}.items");
            }
            case "link":
            {
                var fields=Fields(element,where,"type","label","url");
                var url=Need(fields,"url",where).ValueKind==JsonValueKind.String ? fields["url"].GetString():null;
                if(SafeLink(url) is null)throw new Refused("BadLink",$"{where}.url");
                return new LinkBlock(Text(Need(fields,"label",where),MaxLabel,$"{where}.label"),url!);
            }
            default:throw new Refused("UnknownBlock",where);
        }
    }

    public static string Serialize(ToolTemplate template)
    {
        using var buffer=new MemoryStream();
        using(var json=new Utf8JsonWriter(buffer,new JsonWriterOptions{Encoder=JavaScriptEncoder.UnsafeRelaxedJsonEscaping,Indented=true}))
        {
            json.WriteStartObject();
            json.WriteNumber("schema",Schema);json.WriteString("id",template.Id);
            WriteText(json,"name",template.Name);
            if(template.Author is not null)json.WriteString("author",template.Author);
            if(template.License is not null)json.WriteString("license",template.License);
            if(template.Theme is not null)
            {
                json.WriteStartObject("settings");
                json.WriteString("theme",template.Theme);
                json.WriteEndObject();
            }
            json.WriteStartArray("blocks");
            foreach(var block in template.Blocks)WriteBlock(json,block);
            json.WriteEndArray();
            json.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }
    private static void WriteText(Utf8JsonWriter json,string? name,TemplateText text)
    {
        if(name is null)json.WriteStartObject();else json.WriteStartObject(name);
        foreach(var language in TemplateText.Languages)if(text.Values.TryGetValue(language,out var value))json.WriteString(language,value);
        json.WriteEndObject();
    }
    private static void WriteBlock(Utf8JsonWriter json,TemplateBlock block)
    {
        json.WriteStartObject();
        switch(block)
        {
            case HeadingBlock heading:json.WriteString("type","heading");WriteText(json,"text",heading.Text);break;
            case NoteBlock note:json.WriteString("type","note");WriteText(json,"text",note.Text);break;
            case ChecklistBlock list:
                json.WriteString("type","checklist");
                json.WriteString("reset",list.Reset switch{TemplateReset.Daily=>"daily",TemplateReset.Weekly=>"weekly",_=>"none"});
                json.WriteStartArray("entries");
                foreach(var entry in list.Entries)WriteText(json,null,entry);
                json.WriteEndArray();
                break;
            case CounterBlock counter:
                json.WriteString("type","counter");WriteText(json,"label",counter.Label);
                if(counter.Target>0)json.WriteNumber("target",counter.Target);
                break;
            case TimerBlock timer:
                json.WriteString("type","timer");WriteText(json,"label",timer.Label);
                json.WriteString("at",timer.Timer switch{TemplateTimer.Weekly=>"weekly",TemplateTimer.Supply=>"supply",TemplateTimer.Hour=>"hour",_=>"daily"});
                if(timer.Timer==TemplateTimer.Hour)json.WriteNumber("hourUtc",timer.HourUtc);
                break;
            case ItemListBlock items:
                json.WriteString("type","items");
                json.WriteStartArray("items");
                foreach(var item in items.Items)
                {
                    json.WriteStartObject();
                    json.WriteNumber("id",item.Id);json.WriteNumber("wanted",item.Wanted);
                    json.WriteEndObject();
                }
                json.WriteEndArray();
                break;
            case LinkBlock link:json.WriteString("type","link");WriteText(json,"label",link.Label);json.WriteString("url",link.Url);break;
            default:throw new InvalidOperationException("Unknown block.");
        }
        json.WriteEndObject();
    }

    /// <summary>A tool as one line of text to paste anywhere.</summary>
    public static string Encode(string json)
    {
        using var buffer=new MemoryStream();
        using(var deflate=new DeflateStream(buffer,CompressionLevel.SmallestSize,true))deflate.Write(Encoding.UTF8.GetBytes(json));
        return SharePrefix+Convert.ToBase64String(buffer.ToArray()).TrimEnd('=').Replace('+','-').Replace('/','_');
    }
    /// <summary>What was pasted, as a tool: the shared line or the JSON itself. The decompressed size is bounded while reading.</summary>
    public static TemplateParse Import(string? pasted)
    {
        var text=pasted?.Trim() ?? "";
        if(!text.StartsWith(SharePrefix,StringComparison.Ordinal))return Parse(text);
        if(text.Length>MaxShareChars)return new(null,new("TooLarge"));
        try
        {
            var body=text[SharePrefix.Length..].Replace('-','+').Replace('_','/');
            using var source=new MemoryStream(Convert.FromBase64String(body.PadRight(body.Length+(4-body.Length%4)%4,'=')));
            using var deflate=new DeflateStream(source,CompressionMode.Decompress);
            using var target=new MemoryStream();
            var block=new byte[8192];int read;
            while((read=deflate.Read(block))>0)
            {
                if(target.Length+read>MaxBytes)return new(null,new("TooLarge"));
                target.Write(block,0,read);
            }
            return Parse(new UTF8Encoding(false,true).GetString(target.ToArray()));
        }
        catch(Exception error) when(error is FormatException or InvalidDataException or DecoderFallbackException){return new(null,new("BadShare"));}
    }
}
