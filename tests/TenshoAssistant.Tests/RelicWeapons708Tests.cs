using TenshoAssistant.Relics;

// W-03 (0.708): the Zodiac weapons and the book in progress.
internal static class RelicWeapons708Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static readonly RelicBook Book=new(1,"b",[new("m1","z",3),new("m2","z",3)],[new("d","z")],[new("f","z")],[new("l","z")]);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Relics: stages become one line per class, and an empty class is left out",() =>
        {
            var lines=RelicBoard.Lines([[1u,2u,0u],[3u,4u,0u]]);
            Check(lines.Count==2 && lines[0].SequenceEqual([1u,3u]) && lines[1].SequenceEqual([2u,4u]),"lines");
        }),
        ("Relics: the furthest stage held, none is not unknown",() =>
        {
            uint[] line=[1,2,3];
            Check(RelicBoard.Reached(line,item=>item==2 ? 1:0)==1,"second stage");
            Check(RelicBoard.Reached(line,_=>0)==-1,"none");
            Check(RelicBoard.Reached(line,_=>null) is null,"not read");
            Check(RelicBoard.Reached(line,item=>item==1 ? 1:null)==0,"what is read counts");
        }),
        ("Relics: a book counts each target once, a short list as not done",() =>
        {
            var (done,total)=RelicBoard.Done(Book,new(RelicReadStatus.Loaded,1,[3,2],[true],[false],[true]));
            Check(total==5 && done==3,"m1, d, l");
            Check(RelicBoard.Done(Book,new(RelicReadStatus.Loaded,1,[],[],[],[])).Done==0,"nothing read");
        }),
        ("Relics: the page only reads, on the framework thread, once the character is loaded",() =>
        {
            var reader=Read("Relics/RelicWeapons.Dalamud.cs");
            Check(Read("Plugin.cs").Contains("mainWindow.ReadRelics=()=>framework.RunOnFrameworkThread(()=>"),"framework thread");
            Check(reader.Contains("player->IsLoaded"),"loaded first");
        }),
    ];
}
