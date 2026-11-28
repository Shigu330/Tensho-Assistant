using TenshoAssistant.Maps;

// 0.1673 (X-684, fourth part: monsters): one monster tooltip (MainWindow.MonsterTooltip.cs) for the Tensho map's monsters, hunt marks,
// leve targets and beasts to tame, the overlay above the monsters and the Monsters page: name, level or rank, zone, every reason.
internal static class MonsterTips1673Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Monster tips 1673: name, level, zone and every reason once, blank ones left out",()=>
        {
            var facts=MonsterTips.Build(" Coeurl ","Level 12","Thanalan",["Blue magic: Bad Breath","",null,"Hunting log","Blue magic: Bad Breath"," "]);
            Check(facts is { Name:"Coeurl",Level:"Level 12",Zone:"Thanalan" } && facts.Notes.SequenceEqual(["Blue magic: Bad Breath","Hunting log"]),"the facts");
            Check(MonsterTips.Build("Mark","",null,[]) is { Level:null,Zone:null,Notes.Count:0 },"nothing known: nothing said");
        }),
        ("Monster tips 1673: the map, the overlay and the Monsters page use the one monster tooltip; a later layer's tooltip still wins",()=>
        {
            var map=Read("src/TenshoAssistant/MainWindow.MapView.cs");
            Check(map.Split("MapMonsterTip(MonsterTips.Build(").Length-1==4,"monsters, hunt marks, leve targets, beasts to tame");
            Check(map.Contains("if(mapMonsterTip is { } monsterTip && ReferenceEquals(monsterTip.Text,tooltip))DrawMonsterTooltip(monsterTip.Facts,monsterTip.After);\n                else WrappedTooltip(tooltip);"),"the text set last decides");
            Check(!map.Contains("localization.Format(\"MapView.MonsterLabel\",monster.Name,monster.Level)+") && !map.Contains("tooltip=localization.Format(\"MapView.LeveTarget\""),"no hand-written monster tooltip left on the map");
            Check(Read("src/TenshoAssistant/MainWindow.MonsterIcons.cs").Contains("if(tip is not null)DrawMonsterTooltip(tip);"),"the overlay");
            Check(Read("src/TenshoAssistant/MainWindow.Monsters.cs").Contains("if(RichTipHovered())DrawMonsterTooltip(MonsterTips.Build("),"the Monsters page, for every player now");
        }),
    ];
}
