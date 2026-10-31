using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Collections;

internal sealed partial class CollectionReader
{
    private ExcelSheet<Title>? titles;
    private ExcelSheet<Achievement>? achievements;
    private ExcelSheet<AchievementCategory>? achievementCategories;
    private ExcelSheet<AchievementKind>? achievementKinds;
    private bool titlesUnavailable,achievementsUnavailable,titleUnlockFailed,achievementUnlockFailed,titleRowFailed,achievementRowFailed;
    private bool titlesStable=true,achievementsStable=true;
    private bool completeTitles,completeAchievements;
    // Remembered title/achievement states of the current character (CompletionMemory) and when each list was last read completely.
    private readonly Dictionary<(CollectionKind,uint),(bool Positive,bool Negative)> completionMemory=[];
    private ulong completionOwner;
    private DateTimeOffset? titlesCheckedAt,achievementsCheckedAt;
    private void RememberCompletion(ulong owner,IEnumerable<CollectionEntry> source,DateTimeOffset? titles,DateTimeOffset? achievements)
    {
        if(completionOwner!=owner){completionMemory.Clear();completionOwner=owner;titlesCheckedAt=achievementsCheckedAt=null;}
        if(titles is { } t && (titlesCheckedAt is null || t>titlesCheckedAt))titlesCheckedAt=t;
        if(achievements is { } a && (achievementsCheckedAt is null || a>achievementsCheckedAt))achievementsCheckedAt=a;
        foreach(var entry in source)
            if(entry.Kind is CollectionKind.Title or CollectionKind.Achievement && (entry.ConfirmedUnlocked || entry.NegativeConfirmed))
                completionMemory[(entry.Kind,entry.Id)]=(entry.ConfirmedUnlocked,entry.NegativeConfirmed);
    }
    private void ApplyCompletionMemory(ulong owner,List<CollectionEntry> source,TimeSpan keep)
    {
        var now=DateTimeOffset.UtcNow;
        if(completionOwner!=owner){completionMemory.Clear();completionOwner=owner;titlesCheckedAt=achievementsCheckedAt=null;}
        var keepTitles=titlesCheckedAt is { } t && now-t<=keep;var keepAchievements=achievementsCheckedAt is { } a && now-a<=keep;
        for(var index=0;index<source.Count;index++)
        {
            var entry=source[index];
            if(entry.Kind==CollectionKind.Title && !completeTitles)source[index]=CompletionMemory.Merge(entry,completionMemory.TryGetValue((entry.Kind,entry.Id),out var title) ? title:null,keepTitles);
            if(entry.Kind==CollectionKind.Achievement && !completeAchievements)source[index]=CompletionMemory.Merge(entry,completionMemory.TryGetValue((entry.Kind,entry.Id),out var achievement) ? achievement:null,keepAchievements);
        }
        RememberCompletion(owner,source,completeTitles ? now:null,completeAchievements ? now:null);
    }
    private bool? lastTitlesLoaded,lastAchievementsLoaded;
    private string? titlesIssue,achievementsIssue;
    private int TitleCount=>titlesUnavailable ? 0:titles!.Count;
    private int AchievementCount=>achievementsUnavailable ? 0:achievements!.Count;

    private void ResetCompletion()
    {
        titlesUnavailable=false;achievementsUnavailable=false;titleUnlockFailed=false;achievementUnlockFailed=false;titleRowFailed=false;achievementRowFailed=false;
        titlesStable=true;achievementsStable=true;lastTitlesLoaded=null;lastAchievementsLoaded=null;titlesIssue=null;achievementsIssue=null;
    }

    private void PrepareCompletion()
    {
        if(!titlesUnavailable)
        {
            try{titles ??= data.GetExcelSheet<Title>();}
            catch(Exception error){titlesUnavailable=true;titlesIssue="Catalogue des titres indisponible ; les autres collections restent consultables.";log.Warning(error,"Title catalogue unavailable.");}
        }
        if(!achievementsUnavailable)
        {
            try
            {
                achievements ??= data.GetExcelSheet<Achievement>();
                // English names (0.180): the wiki names achievements in English (Aeturna: « Vintage Vogue » = no. 3185 « Vêtements vertueux »,
                // whose reward item is 0 in the game data: docs/evidence/0.180.0-achievement-probe.log).
                try{achievementsEnglish ??= data.GetExcelSheet<Achievement>(Dalamud.Game.ClientLanguage.English);}catch{achievementsEnglish=null;}
                achievementCategories ??= data.GetExcelSheet<AchievementCategory>();
                achievementKinds ??= data.GetExcelSheet<AchievementKind>();
            }
            catch(Exception error){achievementsUnavailable=true;achievementsIssue="Catalogue des succès indisponible ; les autres collections restent consultables.";log.Warning(error,"Achievement catalogue unavailable.");}
        }
    }

    private bool CompletionLoadingChanged()
    {
        try
        {
            return lastTitlesLoaded is { } title && title!=unlocks.IsTitleListLoaded
                || lastAchievementsLoaded is { } achievement && achievement!=unlocks.IsAchievementListLoaded;
        }
        catch{return true;} // Invalidate once; a failed final check records null and waits for the normal retry.
    }

    private bool ReadTitle(int index)
    {
        try
        {
            var row=titles!.GetRowAt(index);if(CompletionCopy.Title(row) is not { } copied)return true;
            if(character!=0 && !titleUnlockFailed)
            {
                try
                {
                    if(row.RowId>ushort.MaxValue)throw new InvalidDataException("Title ID exceeds native unlock argument.");
                    var before=unlocks.IsTitleListLoaded;var positive=unlocks.IsTitleUnlocked(row);var after=unlocks.IsTitleListLoaded;
                    titlesStable&=before && after;copied=CollectionAcquisitionEvidence.Observe(copied,positive,before,after);
                }
                catch(Exception error){titleUnlockFailed=true;titlesStable=false;titlesIssue="Acquisitions des titres partiellement lues ; les résultats restants sont inconnus.";log.Warning(error,"Title acquisition read unavailable.");}
            }
            else titlesStable=false;
            entries.Add(copied);return true;
        }
        catch(Exception error){titleRowFailed=true;titlesStable=false;titlesIssue="Catalogue des titres partiellement lu ; autres collections conservées.";log.Warning(error,"Title row unavailable.");return false;}
    }

    private bool ReadAchievement(int index)
    {
        try
        {
            var row=achievements!.GetRowAt(index);
            AchievementCategory? category=achievementCategories!.TryGetRow(row.AchievementCategory.RowId,out var group) ? group:null;
            AchievementKind? kind=category is { } nativeCategory && achievementKinds!.TryGetRow(nativeCategory.AchievementKind.RowId,out var nativeKind) ? nativeKind:null;
            var english=achievementsEnglish is { } englishSheet && englishSheet.TryGetRow(row.RowId,out var englishRow) ? englishRow.Name.ExtractText():"";
            if(CompletionCopy.Achievement(row,category,kind,english) is not { } copied)return true;
            if(character!=0 && !achievementUnlockFailed)
            {
                try
                {
                    if(row.RowId>int.MaxValue)throw new InvalidDataException("Achievement ID exceeds native unlock argument.");
                    var before=unlocks.IsAchievementListLoaded;var positive=unlocks.IsAchievementComplete(row);var after=unlocks.IsAchievementListLoaded;
                    achievementsStable&=before && after;copied=CollectionAcquisitionEvidence.Observe(copied,positive,before,after);
                }
                catch(Exception error){achievementUnlockFailed=true;achievementsStable=false;achievementsIssue="États des succès partiellement lus ; les résultats restants sont inconnus.";log.Warning(error,"Achievement acquisition read unavailable.");}
            }
            else achievementsStable=false;
            entries.Add(copied);return true;
        }
        catch(Exception error){achievementRowFailed=true;achievementsStable=false;achievementsIssue="Catalogue des succès partiellement lu ; autres collections conservées.";log.Warning(error,"Achievement row unavailable.");return false;}
    }

    private bool FinalizeCompletion()
    {
        bool? titleLoaded=null,achievementLoaded=null;
        if(character!=0 && !titlesUnavailable)
        {
            try{titleLoaded=unlocks.IsTitleListLoaded;}
            catch(Exception error){titlesIssue="Chargement des titres non vérifiable ; états négatifs inconnus.";titleUnlockFailed=true;log.Warning(error,"Title list coverage unavailable.");}
        }
        if(character!=0 && !achievementsUnavailable)
        {
            try{achievementLoaded=unlocks.IsAchievementListLoaded;}
            catch(Exception error){achievementsIssue="Chargement des succès non vérifiable ; états négatifs inconnus.";achievementUnlockFailed=true;log.Warning(error,"Achievement list coverage unavailable.");}
        }
        var titleComplete=titlesStable && titleLoaded==true && !titleUnlockFailed && !titleRowFailed;
        completeTitles=titleComplete;
        var achievementComplete=achievementsStable && achievementLoaded==true && !achievementUnlockFailed && !achievementRowFailed;
        completeAchievements=achievementComplete;
        for(var index=0;index<entries.Count;index++)
        {
            var entry=entries[index];
            if(entry.Kind==CollectionKind.Title)entries[index]=CollectionAcquisitionEvidence.RequireStableCoverage(entry,titleComplete);
            if(entry.Kind==CollectionKind.Achievement)entries[index]=CollectionAcquisitionEvidence.RequireStableCoverage(entry,achievementComplete);
        }
        if(character!=0 && !titleComplete)titlesIssue ??="Liste des titres non entièrement reçue pendant ce relevé : seules les acquisitions positives sont confirmées. Ouvrez les titres dans le jeu puis actualisez.";
        if(character!=0 && !achievementComplete)achievementsIssue ??="Liste des succès non entièrement reçue pendant ce relevé : seuls les succès positifs sont confirmés. Ouvrez les succès dans le jeu puis actualisez.";
        lastTitlesLoaded=titleLoaded;lastAchievementsLoaded=achievementLoaded;
        return titleLoaded==true && !titlesStable && !titleUnlockFailed && !titleRowFailed
            || achievementLoaded==true && !achievementsStable && !achievementUnlockFailed && !achievementRowFailed;
    }

    private IEnumerable<CollectionCategoryIssue> CompletionIssues()
    {
        if(titlesIssue is not null)yield return new(CollectionKind.Title,titlesIssue);
        if(achievementsIssue is not null)yield return new(CollectionKind.Achievement,achievementsIssue);
    }

    private Lumina.Excel.ExcelSheet<Achievement>? achievementsEnglish;
    private void ReleaseCompletion(){titles=null;achievements=null;achievementsEnglish=null;achievementCategories=null;achievementKinds=null;ResetCompletion();}
}
