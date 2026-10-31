using Lumina.Excel.Sheets;

namespace TenshoAssistant.Gathering;

internal static class FishingNotebookCopy
{
    public static FishingNotebookEntry? FromRow(FishParameter row,FishingNoteInfo? note)
    {
        if(row.RowId==0 || row.Item.RowId==0 || row.Item.RowType!=typeof(Item))return null;
        var linked=note is { } value && value.RowId==row.RowId && value.Item.RowId==row.Item.RowId && value.Item.RowType==typeof(Item);
        var folklore=row.GatheringSubCategory.RowId==0 ? string.Empty:row.GatheringSubCategory.Value.FolkloreBook.ExtractText();
        return new(row.RowId,row.Item.RowId,row.FishingSpot.RowId,row.Text.ExtractText(),folklore,row.IsInLog,row.IsHidden,
            linked ? FishingNotebookIndex.Flag(note!.Value.TimeRestriction):null,
            linked ? FishingNotebookIndex.Flag(note!.Value.WeatherRestriction):null,linked);
    }
}
