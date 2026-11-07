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
        ("Relic families: a stage is its level and its word, a higher level's word is not taken, and a base line is found by its next stage",() =>
        {
            RelicItemRow[] rows=[new(1,"Phantom Sword Penumbrae",745,4,10),new(2,"Phantom Sword Umbrae",760,4,10),new(3,"Phantom Sword Penumbrae",1,4,10),new(4,"Phantom Axe Penumbrae",745,4,11),
                new(5,"Honorbound",485,4,10),new(6,"Honorbound Recollection",500,4,10),new(7,"Other Sword",485,4,10)];
            var lines=RelicBoard.FamilyLines(rows,[new(745,"Penumbrae"),new(760,"Umbrae")]);
            Check(lines.Count==2 && lines[0].SequenceEqual([1u,2u]) && lines[1].SequenceEqual([4u]),"by jobs, in stage order, not the replica at level 1");
            var bases=RelicBoard.Stage(rows,new(485,null,4,null," Recollection")).ToArray();
            Check(bases.Length==1 && bases[0].Id==5,"only the weapon with a next stage");
        }),
        ("Relic families: the rules cover the five lines, and rarity keeps out what a level alone would take",() =>
        {
            Check(RelicBoard.Rules.Select(family=>family.Key).SequenceEqual(["Anima","Eureka","Resistance","Manderville","Phantom"]),"lines");
            Check(RelicBoard.Levels().Contains(795) && RelicBoard.Levels().Contains(170),"levels");
            RelicItemRow[] rows=[new(1,"Almace",210,4,1),new(2,"Crafted Blade",210,2,1)];
            Check(RelicBoard.Stage(rows,new(210,null,4)).Single().Id==1,"rarity");
            // 0.717: 29 weapons say « Augmented » at 500, among them the crafted Crystarium ones.
            RelicItemRow[] augmented=[new(1,"Augmented Honorbound",500,4,1),new(2,"Augmented Crystarium Rod",500,3,2),new(3,"Honorbound Recollection",500,4,1)];
            var rule=RelicBoard.Rules.Single(family=>family.Key=="Resistance").Stages[1];
            Check(RelicBoard.Stage(augmented,rule).Single().Id==1,"the relic only");
        }),
        ("Relics: the page only reads, on the framework thread, once the character is loaded",() =>
        {
            var reader=Read("Relics/RelicWeapons.Dalamud.cs");
            Check(Read("Plugin.cs").Contains("mainWindow.ReadRelics=()=>framework.RunOnFrameworkThread(()=>"),"framework thread");
            Check(reader.Contains("player->IsLoaded"),"loaded first");
        }),
    ];
}
