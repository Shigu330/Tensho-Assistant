using System.Globalization;
using System.Text;
using System.Text.Json;
using Lumina;
using Lumina.Data;
using Lumina.Data.Structs.Excel;
using Lumina.Excel;
using Lumina.Text.ReadOnly;

// X-487 (11/16, the user's request « the game's help covers the whole game », rule R06): read-only index of the game's own help texts
// (window help, guides, tutorials, tips, descriptions of contents and systems), to search before any assumption or in-game reading.
// Nothing is run and nothing is written in the game; the texts are Square Enix data: the index stays outside the repository and is
// never copied into the plugin (cited by sheet, row and game version only).
//   sheets <sqpack>                     every sheet with text columns: rows, text columns, characters (French), for choosing the list
//   find <sqpack> <language> <text>     every sheet, row and column whose text holds <text> (any case, accents ignored)
//   index <sqpack> <folder> [sheets…]   writes <folder>/help-fr.jsonl, help-en.jsonl and meta.json (game version, sheets and rows);
//                                       default sheets: Sheets below (chosen from « sheets » and « find », docs/evidence/0.1474-help-texts.md)
//   search <folder> <words…>            the texts holding every word (any case, accents ignored), French and English side by side
Console.OutputEncoding=new UTF8Encoding(false);
if(args.Length<2)throw new ArgumentException("Usage: sheets <sqpack> | find <sqpack> <French|English> <text> | index <sqpack> <folder> [sheets…] | search <folder> <words…>");

static string Fold(string text)=>new string(text.Normalize(NormalizationForm.FormD).Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant();

if(args[0]=="search")
{
    var folder=args[1];
    var words=args[2..].Select(Fold).ToArray();
    var meta=File.Exists(Path.Combine(folder,"meta.json")) ? JsonDocument.Parse(File.ReadAllText(Path.Combine(folder,"meta.json"))).RootElement.GetProperty("game").GetString():"?";
    var english=File.ReadLines(Path.Combine(folder,"help-en.jsonl")).Select(line=>JsonDocument.Parse(line).RootElement)
        .ToDictionary(row=>$"{row.GetProperty("s").GetString()}#{row.GetProperty("r").GetString()}#{row.GetProperty("c").GetInt32()}",row=>row.GetProperty("t").GetString() ?? "");
    var shown=0;
    foreach(var line in File.ReadLines(Path.Combine(folder,"help-fr.jsonl")))
    {
        var row=JsonDocument.Parse(line).RootElement;
        var text=row.GetProperty("t").GetString() ?? "";
        var key=$"{row.GetProperty("s").GetString()}#{row.GetProperty("r").GetString()}#{row.GetProperty("c").GetInt32()}";
        english.TryGetValue(key,out var twin);
        var folded=Fold(text+" "+twin);
        if(!words.All(folded.Contains))continue;
        Console.WriteLine($"{row.GetProperty("s").GetString()} row {row.GetProperty("r").GetString()} column {row.GetProperty("c").GetInt32()} (game {meta})");
        Console.WriteLine("  fr: "+text);
        if(!string.IsNullOrEmpty(twin))Console.WriteLine("  en: "+twin);
        if(++shown>=40){Console.WriteLine("… (40 shown)");break;}
    }
    Console.WriteLine($"{shown} text(s)");
    return;
}

var sqpack=args[1];
var version=File.Exists(Path.Combine(sqpack,"..","ffxivgame.ver")) ? File.ReadAllText(Path.Combine(sqpack,"..","ffxivgame.ver")).Trim():"unknown";
using var data=new GameData(sqpack,new LuminaOptions{DefaultExcelLanguage=Language.French,CacheFileResources=false});

string Cell(object? value)=>value is ReadOnlySeString text ? text.ExtractText().Replace("\r"," ").Replace("\n"," ").Trim():"";

IEnumerable<(string Row,int Column,string Text)> Texts(string name,Language language)
{
    // An unreadable sheet throws here or while its rows are read: the callers (sheets, find, index) catch it and name the sheet (find at the end).
    var raw=data.Excel.GetRawSheet(name,language);
    var columns=raw.Columns.Select((column,index)=>(column,index)).Where(c=>c.column.Type==ExcelColumnDataType.String).Select(c=>c.index).ToArray();
    if(columns.Length==0)yield break;
    if(raw is RawSubrowExcelSheet)
    {
        foreach(var rows in data.Excel.GetSubrowSheet<RawSubrow>(language,name))
            foreach(var row in rows)
                foreach(var column in columns)
                {
                    var text=Cell(row.ReadColumn(column));
                    if(text.Length>0)yield return ($"{row.RowId}.{row.SubrowId}",column,text);
                }
    }
    else
    {
        foreach(var row in data.Excel.GetSheet<RawRow>(language,name))
            foreach(var column in columns)
            {
                var text=Cell(row.ReadColumn(column));
                if(text.Length>0)yield return (row.RowId.ToString(CultureInfo.InvariantCulture),column,text);
            }
    }
}

if(args[0]=="sheets")
{
    Console.WriteLine($"GAME {version}");
    foreach(var name in data.Excel.SheetNames.OrderBy(n=>n,StringComparer.Ordinal))
    {
        try
        {
            var rows=0;var chars=0L;var cells=0;
            foreach(var (_, _, text) in Texts(name,Language.French)){cells++;chars+=text.Length;}
            if(cells==0)continue;
            rows=data.Excel.GetRawSheet(name,Language.French).Count;
            Console.WriteLine($"SHEET {name} rows {rows} texts {cells} characters {chars}");
        }
        catch(Exception error){Console.WriteLine($"SHEET {name} FAILED {error.GetType().Name}");}
    }
    return;
}

if(args[0]=="find")
{
    var language=Enum.Parse<Language>(args[2]);
    var wanted=Fold(string.Join(" ",args[3..]));
    Console.WriteLine($"GAME {version} FIND « {string.Join(" ",args[3..])} » ({language})");
    // X-496 M4 (0.1478): a sheet that could not be read is named at the end: « nothing found » never stands for « not searched » (R06).
    var unread=new List<string>();
    foreach(var name in data.Excel.SheetNames.OrderBy(n=>n,StringComparer.Ordinal))
    {
        var found=0;
        try
        {
            foreach(var (row,column,text) in Texts(name,language))
                if(Fold(text).Contains(wanted) && found++<2)
                    Console.WriteLine($"{name} row {row} column {column}: {(text.Length>160 ? text[..160]+"…":text)}");
        }
        catch(Exception error){unread.Add($"{name} ({error.GetType().Name})");}
        if(found>2)Console.WriteLine($"{name}: {found} texts in all");
    }
    if(unread.Count>0)Console.WriteLine($"NOT SEARCHED {unread.Count} sheet(s) unreadable in {language}: {string.Join(", ",unread)}");
    return;
}

if(args[0]=="index")
{
    var folder=args[2];
    Directory.CreateDirectory(folder);
    var sheets=args.Length>3 ? args[3..]:Sheets.Default;
    var counts=new SortedDictionary<string,int>(StringComparer.Ordinal);
    foreach(var (language,suffix) in new[]{(Language.French,"fr"),(Language.English,"en")})
    {
        using var writer=new StreamWriter(Path.Combine(folder,$"help-{suffix}.jsonl"),false,new UTF8Encoding(false));
        foreach(var name in sheets)
        {
            var n=0;
            try
            {
                foreach(var (row,column,text) in Texts(name,language))
                {
                    writer.WriteLine(JsonSerializer.Serialize(new{s=name,r=row,c=column,t=text}));
                    n++;
                }
            }
            catch(Exception error){Console.WriteLine($"SHEET {name} FAILED {error.GetType().Name}: {error.Message}");}
            if(suffix=="fr")counts[name]=n;
        }
    }
    File.WriteAllText(Path.Combine(folder,"meta.json"),JsonSerializer.Serialize(new{game=version,made=DateTime.UtcNow.ToString("o"),sheets=counts},
        new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine($"INDEX game {version}: {counts.Values.Sum()} French texts in {counts.Count(c=>c.Value>0)} sheets → {folder}");
    foreach(var (name,n) in counts)Console.WriteLine($"  {name} {n}");
    return;
}
throw new ArgumentException("Unknown mode: "+args[0]);

static class Sheets
{
    /// <summary>The sheets of the index (X-487, chosen on 11/16 from « sheets » (460 sheets with text columns) and « find » (Dédale bestial,
    /// aetherial wheels, Grâce): docs/evidence/0.1474-help-texts.md). Help of the game's systems first, then the descriptions the game gives
    /// of its contents, items, actions and statuses, then the texts shown inside contents and the system messages.</summary>
    public static readonly string[] Default =
    [
        // The game's own help: « learn about… » pages, guides, tutorials, window help, commands and settings.
        "Description","DescriptionString","DescriptionStandAloneTransient","HowTo","HowToCategory","HowToPage","GuideTitle","GuidePageString",
        "MultipleHelp","MultipleHelpString","EventTutorial","EventTutorialPage","ContentsTutorial","ContentsTutorialPage","MainCommand",
        "TextCommand","ConfigKey","Addon","AddonTransient",
        // Descriptions of contents and systems.
        "ContentFinderConditionTransient","ContentsNote","Fate","TripleTriadRule","MinionRules","GoldSaucerTextData","MJIText",
        "HWDInfoBoardArticleTransient","HWDDevLevelWebText","WKSText","WKSMissionText","WKSEmergencyInfoText","WKSPioneeringTrailString",
        "SkyIsland2Mission","SkyIsland2MissionDetail","PerformTransient","FGSAddon","XBMItem",
        // Items, key items, actions, traits, statuses, achievements, fish: what the game says each one does or how to get it.
        "Item","EventItemHelp","ActionTransient","CraftAction","TraitTransient","Status","Achievement","FishParameter","SpearfishingItem",
        // Texts shown inside contents (deep dungeons, field operations, duties) and the system messages.
        "PublicContentTextData","MassivePcContentTextData","InstanceContentTextData","LogMessage",
    ];
}
