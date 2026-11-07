using TenshoAssistant.Beastmaster;

// 0.500 (user request): the Beastmaster sheets are read raw, by column: a moved layout must fail, not give wrong values. Values from the game's
// sheets and the user's captures (docs/evidence/0.500-xbm-columns.md): Cu sith at rank 6 = 161 / 100 / 90 / 86 / 131, grow row 42, subrow 5.
internal static class BeastCatalogTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static XbmRow Row(uint row,ushort sub,params object?[] cells)=>new(row,sub,cells);
    private static object?[] Blank(int count,params (int Column,object Value)[] set)
    {
        var cells=new object?[count];
        foreach(var (column,value) in set)cells[column]=value;
        return cells;
    }
    private static Dictionary<string,IReadOnlyList<XbmRow>> Sheets()
    {
        var content=Blank(37,(0,1088u),(1,(byte)10),(2,(byte)3),(3,(byte)5),(4,250),(5,500),(6,0));
        return new()
        {
            ["XBMPet"]=[Row(0,0,Blank(27)),
                Row(1,0,Blank(27,(0,47),(1,(byte)1),(2,(byte)42),(4,242001u),(5,(ushort)45187),(9,"Slash"),(10,"Six hits"))),
                Row(2,0,Blank(27,(0,48),(1,(byte)2),(2,(byte)7),(4,242002u),(5,(ushort)48643),(9,"Fire"),(10,"Haste")))],
            ["XBMPetParamGrow"]=[Row(42,4,(ushort)154,(ushort)95,(ushort)88,(ushort)83,(ushort)125),Row(42,5,(ushort)161,(ushort)100,(ushort)90,(ushort)86,(ushort)131),
                Row(42,0,(ushort)10,(ushort)11,(ushort)12,(ushort)13,(ushort)14),Row(7,0,(ushort)300,(ushort)1,(ushort)2,(ushort)3,(ushort)4)],
            ["XBMBattleDetail"]=[Row(6,0,Blank(11,(0,14541u),(6,(ushort)1),(7,(ushort)2),(8,(byte)7),(10,242509u))),Row(6,1,Blank(11,(0,14542u),(6,(ushort)2),(10,242510u))),Row(1,0,Blank(11,(0,14531u),(8,(byte)9),(10,242501u)))],
            ["XBMBattleDetailAction"]=[Row(1,0,46866u,(byte)1,(byte)6,0u),Row(2,0,46870u,(byte)0,(byte)2,4880u)],
            ["XBMContent"]=[Row(0,0,Blank(37)),Row(1,0,content)],
            ["XBMContentBattle"]=[Row(1,1,(ushort)1),Row(1,0,(ushort)6)],
            ["XBMElement"]=[Row(0,0,""),Row(7,0,"Blunt"),Row(8,0,"Piercing"),Row(9,0,"Slashing")],
            ["XBMScoreBonus"]=[Row(0,0,false,"Noble I","Three down"),Row(1,0,false,"Noble II","Two down"),Row(2,0,false,"Noble III","One down")],
            ["XBMActionEffectType"]=[Row(0,0,"???"),Row(2,0,"Front"),Row(6,0,"Circle")],
            ["XBMActionTarget"]=[Row(0,0,""),Row(1,0,"Self")],
        };
    }
    private static BeastCatalog Build(Dictionary<string,IReadOnlyList<XbmRow>> sheets)=>BeastCatalog.Build(sheets,
        id=>id==47 ? "cu sith":"squirrel",family=>family==1 ? "Therians":"Insects",action=>action==45187 ? 2:5,
        id=>"enemy "+id,id=>"action "+id,id=>("Board one",30,25));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Beasts: columns of the game's sheets, attributes by rank, physical and magical attacks",() =>
        {
            var catalog=Build(Sheets());
            Check(catalog.Beasts.Count==2,"row 0 left out");
            var cu=catalog.Beasts[0];
            Check(cu.Name=="Cu sith" && cu.Family=="Therians" && cu.Icon==242001 && cu.Assault=="Slash" && cu.Volley=="Six hits","the sheet's columns");
            Check(cu.AutoAttack=="Piercing","piercing, as on the game's sheet");
            Check(catalog.Beasts[1].AutoAttack=="","a magical attack has no physical name");
            Check(cu.At(1)!.Strength==10,"rank 1 is the first subrow");
            Check(cu.At(2)! is {Strength:154,Intelligence:95,PhysicalResistance:88,MagicalResistance:83,Vitality:125},"column order, on the capture of rank 5");
            Check(cu.At(99)!.Strength==161,"above the table: the highest known");
        }),
        ("Boards: guardian, enemies with weakness and actions, bonuses with their points",() =>
        {
            var board=Build(Sheets()).Boards.Single();
            Check(board is {Name:"Board one",Level:30,ItemLevel:25,TeamSize:10,RankLimit:5},"the board's values");
            var guardian=board.Battles.Single(battle=>battle.Guardian);
            Check(guardian.Enemies.Count==2 && guardian.Enemies[0].Name=="Enemy 14541" && guardian.Enemies[0].Weakness=="Blunt","guardian of battle 6, subrow 0");
            Check(guardian.Enemies[0].Actions.Count==2 && guardian.Enemies[0].Actions[0]==new BoardAction("action 46866","Circle","Self") && guardian.Enemies[0].Actions[1].Shape=="Front","actions");
            Check(guardian.Enemies[1].Weakness=="","none stays empty");
            Check(board.Bonuses.Count==2 && board.Bonuses[0]==new BoardBonus("Noble I","Three down",250) && board.Bonuses[1].Points==500,"bonus row 0 is the first points column; 0 points left out");
        }),
        ("Beasts: another layout of the game's sheets fails, it is never read",() =>
        {
            var moved=Sheets();moved["XBMPet"]=[Row(1,0,Blank(28))];
            var failed=false;
            try{Build(moved);}catch(InvalidDataException){failed=true;}
            Check(failed,"a column more");
            var missing=Sheets();missing.Remove("XBMContent");
            failed=false;
            try{Build(missing);}catch(InvalidDataException){failed=true;}
            Check(failed,"a sheet less");
        }),
        ("Beast list: search, class, obtained, rank and sort",() =>
        {
            var catalog=Build(Sheets());
            Check(catalog.View("cu","",false,null,1,BeastSort.Name,false).Count==0,"class filter applies");
            Check(catalog.View("SIX",null,false,null,1,BeastSort.Name,false).Single().Id==1,"search in the attacks");
            Check(catalog.View("","Insects",false,null,1,BeastSort.Name,false).Single().Id==2,"class");
            HashSet<uint> mine=[2];
            Check(catalog.View("",null,true,mine,1,BeastSort.Name,false).Single().Id==2,"obtained only");
            Check(catalog.View("",null,true,null,1,BeastSort.Name,false).Count==2,"list not read: nothing hidden");
            Check(catalog.View("",null,false,null,1,BeastSort.Strength,true)[0].Id==2,"by strength at rank 1");
            Check(catalog.View("",null,false,null,3,BeastSort.Strength,true)[0].Id==2,"rank above the second beast's table keeps its value");
        }),
        ("Beast window probe: developer mode only, bounded, never the ranking or result windows",() =>
        {
            var probe=File.ReadAllText(Path.Combine(Path.GetFullPath("src/TenshoAssistant"),"Beastmaster/BeastWindowProbe.Dalamud.cs"));
            var windows=probe[probe.IndexOf("Windows =",StringComparison.Ordinal)..probe.IndexOf("private const int",StringComparison.Ordinal)];
            Check(windows.Contains("XBMMonsterBookDetail") && windows.Contains("XBMPetParty") && windows.Contains("XBMBattleMonsterDetail"),"the windows wanted");
            Check(!windows.Contains("XBMRanking") && !windows.Contains("XBMResult"),"other players and the character stay out");
            Check(probe.Contains("!developerMode()") && probe.Contains("depth > MaximumDepth") && probe.Contains("found.Count < MaximumTexts") && probe.Contains("written >= MaximumLinesPerWindow"),"off by default, recursion and output bounded");
            Check(probe.Contains("failed = true;") && probe.Contains("TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate)"),"stops on failure, cleaned up");
        }),
        ("Beastmaster page: raw reads guarded, read-only, in the self-check",() =>
        {
            string Code(string file)=>File.ReadAllText(Path.Combine(Path.GetFullPath("src/TenshoAssistant"),file));
            var loader=Code("Beastmaster/BeastCatalogLoader.Dalamud.cs");
            Check(loader.Contains("raw.Columns.Count != columns") && loader.Contains("service.Fail(error.Message)") && loader.Contains("TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate)"),"guards and cleanup");
            Check(loader.Contains("XBMManager.DataState.Received") && !loader.Contains("SetPetUnlocked"),"read-only, once the game has the list");
            Check(Code("MainWindow.SelfCheck.cs").Contains("(\"beastmaster\", SelfCheckBeastmaster)"),"in the self-check");
        }),
    ];
}
