using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;
using TenshoAssistant.Gathering;

if(args.Length!=1)throw new ArgumentException("Supply installed sqpack directory.");
var rows=new List<FishingNotebookEntry>();var noteMismatch=0;var missing=0;
var timeCodes=new HashSet<byte>();var weatherCodes=new HashSet<byte>();
using(var data=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=Language.French}))
{
    var fish=data.Excel.GetSheet<FishParameter>();var notes=data.Excel.GetSheet<FishingNoteInfo>();var items=data.Excel.GetSheet<Item>();var spots=data.Excel.GetSheet<FishingSpot>();
    foreach(var row in fish)
    {
        FishingNoteInfo? note=notes.TryGetRow(row.RowId,out var found) ? found:null;
        if(FishingNotebookCopy.FromRow(row,note) is not { } copied)continue;
        if(!items.HasRow(copied.ItemId) || copied.NotebookSpotId!=0 && !spots.HasRow(copied.NotebookSpotId))throw new InvalidOperationException("Unresolved standard fish item or notebook spot.");
        if(note is null)missing++;else if(!copied.NoteLinked)noteMismatch++;
        if(copied.NoteLinked)
        {
            timeCodes.Add(note!.Value.TimeRestriction);weatherCodes.Add(note.Value.WeatherRestriction);
            if(copied.TimeRestricted!=FishingNotebookIndex.Flag(note.Value.TimeRestriction) || copied.WeatherRestricted!=FishingNotebookIndex.Flag(note.Value.WeatherRestriction))throw new InvalidOperationException("Flag projection differs.");
        }
        var unknown=FishingNotebookCopy.FromRow(row,null)!;
        if(unknown.NoteLinked || unknown.TimeRestricted is not null || unknown.WeatherRestricted is not null)throw new InvalidOperationException("Missing notebook fabricated negative restrictions.");
        if(notes.TryGetRow(row.RowId+1,out var wrongNote))
        {
            var wrong=FishingNotebookCopy.FromRow(row,wrongNote)!;
            if(wrong.NoteLinked || wrong.ItemId!=copied.ItemId || wrong.TimeRestricted is not null || wrong.WeatherRestricted is not null)
                throw new InvalidOperationException("Foreign note accepted or changed fish item identity.");
        }
        rows.Add(copied);
    }
    Console.WriteLine($"Rows: FishParameter={fish.Count}, FishingNoteInfo={notes.Count}, copied={rows.Count}, linked={rows.Count(row=>row.NoteLinked)}, missing={missing}, mismatched={noteMismatch}");
}
// Native sheets and game data are disposed: only copied managed references remain usable.
var index=new FishingNotebookIndex(rows);
if(index.Count<500 || rows.Count(row=>row.NoteLinked)<100)throw new InvalidOperationException("Fish notebook projection unexpectedly empty or incompatible.");
foreach(var group in rows.GroupBy(row=>row.ItemId))
    if(!index.ForItem(group.Key).Select(row=>row.FishId).ToHashSet().SetEquals(group.Select(row=>row.FishId)))throw new InvalidOperationException("Fish variants lost in shared index.");
Console.WriteLine($"PASS notebook index: {index.Count} fish, {rows.Select(row=>row.ItemId).Distinct().Count()} item IDs, {rows.Count(row=>!string.IsNullOrWhiteSpace(row.Folklore))} folklore, time=[{string.Join(',',timeCodes.Order())}], weather=[{string.Join(',',weatherCodes.Order())}]");
Console.WriteLine($"Unknown restrictions retained: time={rows.Count(row=>row.TimeRestricted is null)}, weather={rows.Count(row=>row.WeatherRestricted is null)}");
foreach(var entry in rows.Where(row=>row.TimeRestricted==true || row.WeatherRestricted==true).Take(3))Console.WriteLine($"Example FishParameter={entry.FishId}, Item={entry.ItemId}, spot={entry.NotebookSpotId}, time={entry.TimeRestricted}, weather={entry.WeatherRestricted}");
