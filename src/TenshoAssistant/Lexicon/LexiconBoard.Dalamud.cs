using Dalamud.Game;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Lexicon;

/// <summary>W-16 (0.719): the game's sheets, read in both languages once, off the frame, when the page is first opened.</summary>
internal static class LexiconLoader
{
    private const int LongestInterfaceText=60;

    public static IReadOnlyList<LexiconEntry> Read(IDataManager data)
    {
        var words=new List<LexiconEntry>(150000);
        Add<Item>(data,words,LexiconKind.Item,row=>row.Name.ExtractText());
        Add<Lumina.Excel.Sheets.Action>(data,words,LexiconKind.Action,row=>row.Name.ExtractText());
        Add<Trait>(data,words,LexiconKind.Trait,row=>row.Name.ExtractText());
        Add<Status>(data,words,LexiconKind.Status,row=>row.Name.ExtractText());
        Add<PlaceName>(data,words,LexiconKind.Place,row=>row.Name.ExtractText());
        Add<BNpcName>(data,words,LexiconKind.Monster,row=>row.Singular.ExtractText());
        Add<ENpcResident>(data,words,LexiconKind.Npc,row=>row.Singular.ExtractText());
        Add<Quest>(data,words,LexiconKind.Quest,row=>row.Name.ExtractText());
        Add<ContentFinderCondition>(data,words,LexiconKind.Duty,row=>row.Name.ExtractText());
        Add<Mount>(data,words,LexiconKind.Mount,row=>row.Singular.ExtractText());
        Add<Companion>(data,words,LexiconKind.Minion,row=>row.Singular.ExtractText());
        Add<Fate>(data,words,LexiconKind.Fate,row=>row.Name.ExtractText());
        Add<ClassJob>(data,words,LexiconKind.Job,row=>row.Name.ExtractText());
        Add<Achievement>(data,words,LexiconKind.Achievement,row=>row.Name.ExtractText());
        Add<Emote>(data,words,LexiconKind.Emote,row=>row.Name.ExtractText());
        // A text with a macro depends on the moment: it would show with a hole. Long ones are sentences, not words.
        Add<Addon>(data,words,LexiconKind.Interface,row=>row.Text.ToMacroString().Contains('<') || row.Text.ExtractText().Length>LongestInterfaceText ? "":row.Text.ExtractText());
        return words;
    }

    private static void Add<T>(IDataManager data,List<LexiconEntry> words,LexiconKind kind,Func<T,string> text) where T:struct,IExcelRow<T>
    {
        var english=data.GetExcelSheet<T>(ClientLanguage.English);
        var seen=new HashSet<(string,string)>();
        foreach(var row in data.GetExcelSheet<T>(ClientLanguage.French))
            if(english.GetRowOrDefault(row.RowId) is { } other && LexiconBoard.Entry(kind,text(row),text(other)) is { } entry && seen.Add((entry.French,entry.English)))words.Add(entry);
    }
}
