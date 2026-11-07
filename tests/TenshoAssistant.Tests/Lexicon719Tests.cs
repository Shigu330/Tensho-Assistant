using TenshoAssistant.Lexicon;

// W-16 (0.719): the lexicon.
internal static class Lexicon719Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static LexiconEntry Word(string french,string english,LexiconKind kind=LexiconKind.Item)=>LexiconBoard.Entry(kind,french,english)!;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Lexicon: soft hyphens and no-break spaces go, as the game's French has them; both languages are needed",() =>
        {
            // Addon row 13987, French, as read on 2026-11-07.
            Check(LexiconBoard.Clean("af\u00ADfron\u00ADte\u00ADment crucial\u00A0!")=="affrontement crucial !","clean");
            Check(LexiconBoard.Fold("Eurêka Orthos")=="eureka orthos","fold");
            Check(LexiconBoard.Entry(LexiconKind.Item,"a","")is null && LexiconBoard.Entry(LexiconKind.Item,"","a") is null,"a side missing");
        }),
        ("Lexicon: found in either language, best first, one kind on request, and the total stays true",() =>
        {
            LexiconEntry[] words=[Word("Grande eureka","Long",LexiconKind.Place),Word("Eurêka","Eureka",LexiconKind.Place),Word("Eurêka Anemos","Anemos",LexiconKind.Place),Word("Pomme","Apple")];
            Check(LexiconBoard.Search(words,"eureka",null).Rows.Select(row=>row.French).SequenceEqual(["Eurêka","Eurêka Anemos","Grande eureka"]),"whole, start, anywhere");
            Check(LexiconBoard.Search(words,"APPLE",null).Rows.Single().French=="Pomme","the other language");
            Check(LexiconBoard.Search(words,"eureka",LexiconKind.Item).Total==0,"one kind");
            Check(LexiconBoard.Search(words,"e",null).Total==0,"too short");
            var many=Enumerable.Range(0,LexiconBoard.Most+5).Select(index=>Word("mot"+index,"word")).ToArray();
            var (rows,total)=LexiconBoard.Search(many,"mot",null);
            Check(rows.Count==LexiconBoard.Most && total==LexiconBoard.Most+5,"bounded, the count stays true");
        }),
        ("Lexicon: both the load and the search run off the frame, and both languages are asked by name",() =>
        {
            var page=Read("MainWindow.Lexicon.cs");
            Check(page.Contains("lexiconLoad=Task.Run(load)") && page.Contains("lexiconSearch=Task.Run(()=>LexiconBoard.Search("),"off the frame");
            var loader=Read("Lexicon/LexiconBoard.Dalamud.cs");
            Check(loader.Contains("ClientLanguage.English") && loader.Contains("ClientLanguage.French"),"languages");
        }),
    ];
}
