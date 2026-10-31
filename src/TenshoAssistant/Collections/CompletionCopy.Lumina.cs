using Lumina.Excel.Sheets;

namespace TenshoAssistant.Collections;

public static class CompletionCopy
{
    public static CollectionEntry? Title(Title row)
    {
        var masculine=row.Masculine.ExtractText();var feminine=row.Feminine.ExtractText();
        if(row.RowId==0 || string.IsNullOrWhiteSpace(masculine) && string.IsNullOrWhiteSpace(feminine))return null;
        var name=string.IsNullOrWhiteSpace(masculine) ? feminine:masculine;
        if(!string.IsNullOrWhiteSpace(feminine) && feminine!=name)name+=" / "+feminine;
        return new(CollectionKind.Title,row.RowId,name,0,false,Title:new(masculine,feminine,row.IsPrefix,row.Order));
    }

    public static CollectionEntry? Achievement(Achievement row,AchievementCategory? category,AchievementKind? kind,string englishName="")
    {
        var name=row.Name.ExtractText();if(row.RowId==0 || string.IsNullOrWhiteSpace(name))return null;
        var matched=category is { } group && group.RowId==row.AchievementCategory.RowId;
        var kindId=matched ? category!.Value.AchievementKind.RowId:0;
        var kindMatched=matched && kind is { } nativeKind && nativeKind.RowId==kindId;
        var metadata=new AchievementMetadata(row.AchievementCategory.RowId,matched ? category!.Value.Name.ExtractText():"",matched ? category!.Value.Order:null,
            kindId,kindMatched ? kind!.Value.Name.ExtractText():"",kindMatched ? kind!.Value.Order:null,
            row.Order,row.Points,row.Item.RowId,row.Title.RowId,row.AchievementHideCondition.RowId,englishName);
        return new(CollectionKind.Achievement,row.RowId,name,row.Icon,false,Description:row.Description.ExtractText(),Achievement:metadata);
    }
}
