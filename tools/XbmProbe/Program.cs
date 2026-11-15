using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Text.ReadOnly;

// Read-only evidence for the Beastmaster (XBM) sheets: every column, read raw (the typed rows of this Lumina.Excel do not all match the game's column hash).
if(args.Length<1)throw new ArgumentException("Supply the installed sqpack directory; then a language (French, English) and optionally sheet names.");
var language=args.Length>1 ? Enum.Parse<Language>(args[1]):Language.French;
using var data=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=language,CacheFileResources=false});
// X-481 (0.1470): « uld:NAME » lists the text nodes of the game window ui/uld/NAME.uld with a fixed text (Addon sheet row), in node order, with
// their parent, place and size: the labels the window shows next to the numbers it is given (XBMResult: the parts of the score).
if(args.Length>2 && args[2].StartsWith("uld:",StringComparison.Ordinal))
{
    var path=$"ui/uld/{args[2][4..]}.uld";
    var uld=data.GetFile<Lumina.Data.Files.UldFile>(path) ?? throw new FileNotFoundException(path);
    var addon=data.Excel.GetSheet<Lumina.Excel.Sheets.Addon>(language);
    void Nodes(string where,IEnumerable<Lumina.Data.Parsing.Uld.UldRoot.NodeData> nodes)
    {
        foreach(var node in nodes)
            if(node.Node is Lumina.Data.Parsing.Uld.NodeData.TextNode text)
                Console.WriteLine($"TEXT {where} node {node.NodeId} parent {node.ParentId} at {node.X},{node.Y} size {node.W}x{node.H} visible {node.Visible} "
                    +$"sheet {text.SheetType} text {text.TextId}"+(text.TextId!=0 && text.SheetType==Lumina.Data.Parsing.Uld.NodeData.SheetType.Addon && addon.GetRowOrDefault(text.TextId) is { } row
                        ? $" '{row.Text.ExtractText().Replace("\n"," ")}'":""));
    }
    Nodes("window",uld.WidgetData.Nodes ?? []);
    foreach(var component in uld.Components)Nodes($"component {component.Id} ({component.Type})",component.Nodes);
    return;
}
string[] names=args.Length>2 ? args[2..]:["XBMPet","XBMPetParamGrow","XBMBattleDetail","XBMBattleDetailAction","XBMActionEffectType","XBMActionTarget","XBMContent","XBMContentBattle","XBMElement","XBMScoreBonus","XBMScoreRank","XBMItem","XBMItemType","XBMEntrance","XBMStageEventType"];
string Cell(object? value)=>value switch
{
    null=>"",
    ReadOnlySeString text=>(Environment.GetEnvironmentVariable("PROBE_MACRO")=="1" ? text.ToMacroString():text.ExtractText()).Replace("\n"," ").Replace("|","/"),
    _=>value.ToString() ?? "",
};
foreach(var name in names)
{
    try
    {
        var raw=data.Excel.GetRawSheet(name,language);
        var columns=raw.Columns.Count;
        Console.WriteLine($"SHEET {name} {raw.GetType().Name} rows {raw.Count} columns {columns}: "+string.Join(",",raw.Columns.Select(column=>column.Type.ToString())));
        if(raw is RawSubrowExcelSheet)
        {
            foreach(var rows in data.Excel.GetSubrowSheet<RawSubrow>(language,name))
                foreach(var row in rows)Console.WriteLine($"{row.RowId}.{row.SubrowId}|"+string.Join("|",Enumerable.Range(0,columns).Select(index=>Cell(row.ReadColumn(index)))));
        }
        else
        {
            foreach(var row in data.Excel.GetSheet<RawRow>(language,name))Console.WriteLine($"{row.RowId}|"+string.Join("|",Enumerable.Range(0,columns).Select(index=>Cell(row.ReadColumn(index)))));
        }
    }
    catch(Exception error){Console.WriteLine($"SHEET {name} FAILED {error.GetType().Name}: {error.Message}");}
}
