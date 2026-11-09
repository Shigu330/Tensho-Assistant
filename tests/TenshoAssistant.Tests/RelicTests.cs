// Test audit of 2026-11-09, batch 2 (0.1021): RelicWeapons708Tests, RelicStages974Tests merged into RelicTests; every case is kept.
using TenshoAssistant.Relics;
internal static class RelicTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static readonly RelicBook Book=new(1,"b",[new("m1","z",3),new("m2","z",3)],[new("d","z")],[new("f","z")],[new("l","z")]);
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From RelicWeapons708Tests: W-03 (0.708): the Zodiac weapons and the book in progress.
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
        ("Relic armour: stages by name, level and « +n », as the sheet writes them, and a line per job",() =>
        {
            RelicItemRow[] rows=
            [
                new(1,"Antiquated Chivalrous Circlet",290,3,20),new(2,"Chivalrous Circlet",335,3,20),new(3,"Chivalrous Circlet +1",340,3,20),new(4,"Chivalrous Circlet +2",345,3,20),new(5,"Anemos Chivalrous Circlet",350,3,20),
                new(6,"Pacifist's Circlet",335,3,21),new(7,"Antiquated Pacifist's Circlet",290,3,21),
                new(8,"Carborundum Circlet of Fending",350,2,59),new(9,"Crafted Circlet",335,2,20),
                new(10,"Elemental Helm of Fending",380,3,59),new(11,"Elemental Helm of Fending +1",390,3,59),new(12,"Elemental Helm of Fending +2",390,3,59),
            ];
            var sets=RelicBoard.ArmourSets(rows,RelicBoard.ArmourRules.Single(family=>family.Key=="Eureka").Stages,category=>"c"+category);
            var paladin=sets.Single(set=>set.Category==20);
            Check(paladin.Name=="c20" && paladin.Stages.Take(5).Select(stage=>stage.Single()).SequenceEqual([1u,2u,3u,4u,5u]),"the five stages of a job");
            Check(paladin.Stages[5].Count==0,"not its Elemental armour");
            Check(sets.Single(set=>set.Category==21).Stages[1].Single()==6,"another job");
            var role=sets.Single(set=>set.Category==59);
            Check(role.Stages[4].Count==0 && role.Stages[5].Single()==10 && role.Stages[6].Single()==11 && role.Stages[7].Single()==12,"a crafted set stays out, the role's own set is there");
        }),
        ("Relic armour: pieces held, unknown stays unknown",() =>
        {
            Check(RelicBoard.PiecesHeld([1,2,3],id=>id==1 ? 2:id==2 ? 0:null)==(1,2),"mixed");
            Check(RelicBoard.PiecesHeld([1],_=>null)==(0,0),"unknown");
        }),
        ("Relics: the page only reads, on the framework thread, once the character is loaded",() =>
        {
            var reader=Read("Relics/RelicWeapons.Dalamud.cs");
            Check(Read("Plugin.cs").Contains("mainWindow.ReadRelics=()=>framework.RunOnFrameworkThread(()=>"),"framework thread");
            Check(reader.Contains("player->IsLoaded"),"loaded first");
        }),
        // From RelicStages974Tests: X-90 (0.974): relic weapons and armour by version (one stage, every job), beside the per-job view.
        ("Relic stages 974: grouped by item level, lowest first, unknown level left out",() =>
        {
            var levels=new Dictionary<uint,int>{[1]=135,[2]=150,[3]=135,[4]=150,[5]=0};
            var stages=RelicBoard.ByLevel([[1,2],[3,4,5]],id=>levels.GetValueOrDefault(id));
            Check(stages.Count==2 && stages[0].Level==135 && stages[0].Items.SequenceEqual([1u,3u]) && stages[1].Items.SequenceEqual([2u,4u]),"stages");
        }),
        ("Relic stages 974: the page offers the stage view for weapons and armour",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.RelicWeapons.cs"));
            Check(page.Contains("if(relicByStage){DrawRelicStageWeapons(family,lines);return;}") && page.Contains("if(relicByStage){DrawRelicStageArmour(family,sets);return;}"),"views");
        }),
    ];
}
