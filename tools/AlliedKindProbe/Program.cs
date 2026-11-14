using Lumina;
using Lumina.Data;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using TenshoAssistant.Societies;

// X-265 (0.1306): read-only evidence for the kind of each allied society: the ClassJobCategory of its quests (Quest.BeastTribe,
// ClassJobCategory0), and the kind of each class that category allows (the class's own ClassJobCategory). Columns of ClassJobCategory
// read raw: column i + 1 is ClassJob row i (checked below on the one-class categories).
if(args.Length<1)throw new ArgumentException("Supply the installed sqpack directory; optionally a language (French, English).");
var language=args.Length>1 ? Enum.Parse<Language>(args[1]):Language.French;
using var data=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=language,CacheFileResources=false});
var classes=data.Excel.GetSheet<ClassJob>(language);
var classCategories=Enumerable.Range(0,(int)classes.Max(row=>row.RowId)+1).Select(id=>classes.GetRowOrDefault((uint)id)?.ClassJobCategory.RowId ?? 0).ToArray();
var raw=data.Excel.GetSheet<RawRow>(language,"ClassJobCategory");
bool[] Allowed(uint category)=>raw.GetRowOrDefault(category) is { } row ? classCategories.Select((_,index)=>index+1<raw.Columns.Count && row.ReadColumn(index+1) is true).ToArray():[];
// The one-class categories name their class: « BOT » allows the ClassJob whose abbreviation is « BOT » and only it.
foreach(var category in raw.Where(row=>row.RowId>0))
{
    var allowed=Allowed(category.RowId);var name=category.ReadColumn(0)?.ToString() ?? "";
    if(classes.FirstOrDefault(row=>row.Abbreviation.ExtractText()==name) is { RowId:var single } && single>0)
        Console.WriteLine($"CHECK category {category.RowId} {name}: allows {string.Join(",",allowed.Select((on,index)=>on ? index:-1).Where(index=>index>=0))} (ClassJob {single}) {(allowed.Count(on=>on)==1 && allowed[single] ? "OK":"MISMATCH")}");
}
foreach(var kind in new[]{AlliedKinds.CombatNameCategory,AlliedKinds.CraftingCategory,AlliedKinds.GatheringCategory})
    Console.WriteLine($"NAME category {kind}: {raw.GetRowOrDefault(kind)?.ReadColumn(0)}");
var quests=data.Excel.GetSheet<Quest>(language);
foreach(var tribe in data.Excel.GetSheet<BeastTribe>(language))
{
    if(tribe.RowId==0)continue;
    var categories=quests.Where(quest=>quest.BeastTribe.RowId==tribe.RowId).Select(quest=>quest.ClassJobCategory0.RowId).Where(id=>id!=0).Distinct().ToArray();
    var kind=categories.Aggregate(AlliedKind.None,(sum,category)=>sum|AlliedKinds.OfCategory(Allowed(category),classCategories));
    Console.WriteLine($"SOCIETY {tribe.RowId} {tribe.Name.ExtractText()}: categories {string.Join(",",categories)} → {kind}");
}
