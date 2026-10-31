using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;
using TenshoAssistant.Collections;
using TenshoAssistant.Items;

if(args.Length!=1)throw new ArgumentException("Supply installed sqpack directory; no network or native client calls.");
var entries=new List<CollectionEntry>();var copiedItems=new List<ItemDefinition>();var linked=0;var missingUi=0;var distinctIds=0;
using(var data=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=Language.French}))
{
    var sheet=data.Excel.GetSheet<Orchestrion>();var uiSheet=data.Excel.GetSheet<OrchestrionUiparam>();var categories=data.Excel.GetSheet<OrchestrionCategory>();
    var titleSheet=data.Excel.GetSheet<Title>();var achievementSheet=data.Excel.GetSheet<Achievement>();
    var achievementCategories=data.Excel.GetSheet<AchievementCategory>();var achievementKinds=data.Excel.GetSheet<AchievementKind>();
    var titles=titleSheet.Select(CompletionCopy.Title).OfType<CollectionEntry>().ToArray();
    var achievements=new List<CollectionEntry>();
    foreach(var row in achievementSheet)
    {
        AchievementCategory? category=achievementCategories.TryGetRow(row.AchievementCategory.RowId,out var group) ? group:null;
        AchievementKind? kind=category is { } nativeGroup && achievementKinds.TryGetRow(nativeGroup.AchievementKind.RowId,out var nativeKind) ? nativeKind:null;
        if(CompletionCopy.Achievement(row,category,kind) is not { } copied)continue;
        if(copied.Achievement is not { } metadata)throw new Exception("Missing achievement metadata.");
        if(metadata.RewardItemId!=0 && !data.Excel.GetSheet<Item>().HasRow(metadata.RewardItemId))throw new Exception("Reward Item unresolved.");
        if(metadata.RewardTitleId!=0 && !titleSheet.HasRow(metadata.RewardTitleId))throw new Exception("Reward Title unresolved.");
        if(copied.IconId!=0 && !data.FileExists($"ui/icon/{copied.IconId/1000*1000:D6}/{copied.IconId:D6}.tex"))throw new Exception("Missing achievement icon.");
        var foreign=achievementCategories.First(other=>other.RowId!=row.AchievementCategory.RowId);
        var rejected=CompletionCopy.Achievement(row,foreign,kind)!;
        if(rejected.Achievement!.CategoryName.Length!=0 || rejected.Achievement.KindName.Length!=0)throw new Exception("Foreign achievement category supplied labels.");
        if(category is { } matchedCategory)
        {
            var foreignKind=achievementKinds.First(other=>other.RowId!=matchedCategory.AchievementKind.RowId);
            if(CompletionCopy.Achievement(row,category,foreignKind)!.Achievement!.KindName.Length!=0)throw new Exception("Foreign achievement kind supplied a label.");
        }
        achievements.Add(copied);
    }
    if(titles.Any(entry=>entry.Id>ushort.MaxValue) || achievements.Any(entry=>entry.Id>int.MaxValue))throw new Exception("Native unlock ID cast is not lossless.");
    Console.WriteLine($"PASS completion catalogue: {titles.Length} named titles, {titles.Count(entry=>entry.Title!.Masculine!=entry.Title.Feminine)} gender variants; {achievements.Count} named achievements, {achievements.Select(entry=>entry.Achievement!.CategoryId).Distinct().Count()} categories; {achievements.Count(entry=>entry.Achievement!.RewardItemId!=0)} Item rewards, {achievements.Count(entry=>entry.Achievement!.RewardTitleId!=0)} Title rewards; all identities/icons resolved, foreign categories refused");
    entries.AddRange(titles);entries.AddRange(achievements);
    foreach(var row in sheet)
    {
        OrchestrionUiparam? ui=uiSheet.TryGetRow(row.RowId,out var detail) ? detail:null;
        OrchestrionCategory? category=ui is { } value && categories.TryGetRow(value.OrchestrionCategory.RowId,out var group) ? group:null;
        if(OrchestrionCopy.FromRows(row,ui,category,false) is not { } entry)continue;
        entries.Add(entry);if(entry.Music is null)missingUi++;
        if(entry.IconId!=0 && !data.FileExists($"ui/icon/{entry.IconId/1000*1000:D6}/{entry.IconId:D6}.tex"))throw new Exception("Missing native category icon.");
        if(OrchestrionCopy.FromRows(row,null,null,false)!.Music is not null)throw new Exception("Missing UI metadata fabricated.");
        var foreign=uiSheet.First(other=>other.RowId!=row.RowId);
        if(OrchestrionCopy.FromRows(row,foreign,category,false)!.Music is not null)throw new Exception("Foreign UI metadata accepted.");
        if(ui is { } nativeUi)
        {
            var foreignCategory=categories.First(other=>other.RowId!=nativeUi.OrchestrionCategory.RowId);
            var unlinked=OrchestrionCopy.FromRows(row,ui,foreignCategory,false)!;
            if(unlinked.Music?.CategoryId!=nativeUi.OrchestrionCategory.RowId || unlinked.Music.CategoryName.Length!=0 || unlinked.IconId!=0)
                throw new Exception("Foreign category supplied a label or icon.");
        }
    }
    foreach(var item in data.Excel.GetSheet<Item>())
    {
        var id=ItemOrchestrionCopy.Identity(item);
        if(id!=0)
        {
            if(!sheet.HasRow(id))throw new Exception("Orchestrion Item additional data unresolved.");
            linked++;if(item.RowId!=id)distinctIds++;
        }
        copiedItems.Add(new(item.RowId,item.Name.ExtractText(),item.CanBeHq,item.Icon,OrchestrionId:id));
    }
}
var catalog=ItemCatalog.Build(copiedItems,new("native Item/AdditionalData","French","installed-game",1));
var musicEntries=entries.Where(entry=>entry.Kind==CollectionKind.Orchestrion).ToArray();
var withItems=musicEntries.Count(entry=>catalog.ForOrchestrion(entry.Id).Count>0);
var multiple=musicEntries.Count(entry=>catalog.ForOrchestrion(entry.Id).Count>1);
Console.WriteLine($"PASS native orchestrion: {musicEntries.Length} named rows, {musicEntries.Select(entry=>entry.Music?.CategoryId).Distinct().Count()} category identities, {missingUi} missing UI rows, {linked} item links, {distinctIds} different Item/music IDs, {withItems} entries with items, {multiple} multiple-item associations");
foreach(var entry in musicEntries.Take(4))Console.WriteLine($"MUSIC {entry.Id} {entry.Name}, category={entry.Music?.CategoryId} {entry.Music?.CategoryName}, order={entry.Music?.Order}, icon={entry.IconId}; Item={string.Join(',',catalog.ForOrchestrion(entry.Id).Select(item=>item.Id))}");
if(musicEntries.Length<100 || withItems<100)throw new Exception("Unexpected empty/small native domain.");
var service=new CollectionService();service.Publish(0,entries.Select(entry=>entry with {ConfirmedUnlocked=true}));
if(service.State.Entries.Any(entry=>entry.ConfirmedUnlocked) || service.State.ObservedAt is not null)throw new Exception("Offline native fixture fabricated ownership.");
Console.WriteLine("PASS static projection remains usable after GameData disposal; missing/foreign UI metadata rejected; acquisition not observed offline");
