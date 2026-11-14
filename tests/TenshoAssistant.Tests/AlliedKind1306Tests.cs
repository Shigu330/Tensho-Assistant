using TenshoAssistant;
using TenshoAssistant.Societies;

// 0.1306 (X-265, user request of 13/11 around 15:05, « sort allied societies by kind: combat, crafting, gathering »): the Allied societies
// page lists one kind, kept in the configuration; the kind comes from the ClassJobCategory of each society's quests (probed on 14/11 by
// tools/AlliedKindProbe: 14 combat, 5 crafting, 3 gathering, the Namazu crafting and gathering), never from a hand-written list.
internal static class AlliedKind1306Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    // ClassJob rows 0 to 5 with their own categories: adventurer, gladiator (DoW 30), conjurer (DoM 31), carpenter (33), miner (32), botanist (32).
    private static readonly uint[] Classes=[0,30,31,33,32,32];
    private static AlliedSociety Society(uint row,AlliedKind kind)=>new(row,$"Society {row}","","",8,0,0,Kind:kind);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Allied kind 1306: a class's kind from its own category; a quest category's kinds are those of the classes it allows",() =>
        {
            Check(AlliedKinds.OfClass(0)==AlliedKind.None && AlliedKinds.OfClass(30)==AlliedKind.Combat && AlliedKinds.OfClass(31)==AlliedKind.Combat
                && AlliedKinds.OfClass(33)==AlliedKind.Crafting && AlliedKinds.OfClass(32)==AlliedKind.Gathering,"classes");
            Check(AlliedKinds.OfCategory([false,true,true,false,false,false],Classes)==AlliedKind.Combat,"combat (142)");
            Check(AlliedKinds.OfCategory([false,false,false,true,false,false],Classes)==AlliedKind.Crafting,"crafting (33)");
            Check(AlliedKinds.OfCategory([false,false,false,false,true,true],Classes)==AlliedKind.Gathering,"gathering (32)");
            Check(AlliedKinds.OfCategory([false,false,false,true,true,true],Classes)==(AlliedKind.Crafting|AlliedKind.Gathering),"Namazu (35): both");
            Check(AlliedKinds.OfCategory([true],Classes)==AlliedKind.None && AlliedKinds.OfCategory([],Classes)==AlliedKind.None
                && AlliedKinds.OfCategory([false,true,true,true],[0])==AlliedKind.None,"adventurer only, nothing, a short class list");
        }),
        ("Allied kind 1306: the filter lists one kind, a society of two kinds under each, an unknown kind only under « all »; names from the game",() =>
        {
            var combat=Society(1,AlliedKind.Combat);var namazu=Society(11,AlliedKind.Crafting|AlliedKind.Gathering);var unknown=Society(30,AlliedKind.None);
            Check(AlliedKinds.Listed(combat,AlliedKind.None) && AlliedKinds.Listed(namazu,AlliedKind.None) && AlliedKinds.Listed(unknown,AlliedKind.None),"all");
            Check(AlliedKinds.Listed(combat,AlliedKind.Combat) && !AlliedKinds.Listed(namazu,AlliedKind.Combat) && !AlliedKinds.Listed(unknown,AlliedKind.Combat),"combat");
            Check(AlliedKinds.Listed(namazu,AlliedKind.Crafting) && AlliedKinds.Listed(namazu,AlliedKind.Gathering) && !AlliedKinds.Listed(combat,AlliedKind.Gathering),"the Namazu under both");
            var names=new Dictionary<AlliedKind,string>{[AlliedKind.Combat]="combattants et mages",[AlliedKind.Crafting]="artisans",[AlliedKind.Gathering]="récolteurs"};
            Check(AlliedKinds.Names(namazu.Kind,names)=="artisans, récolteurs" && AlliedKinds.Names(combat.Kind,names)=="combattants et mages"
                && AlliedKinds.Names(AlliedKind.None,names)=="" && AlliedKinds.Names(AlliedKind.Crafting,new Dictionary<AlliedKind,string>())=="","names");
            Check(AlliedKinds.Shown is [AlliedKind.Combat,AlliedKind.Crafting,AlliedKind.Gathering] && (int)AlliedKind.Combat==1 && (int)AlliedKind.Crafting==2 && (int)AlliedKind.Gathering==4,
                "the saved values stay 1, 2, 4");
            Check(new AlliedCatalog([],[]).KindNames.Count==0 && new Configuration().AlliedKindFilter==0 && ConfigurationReset.Reset.Contains("AlliedKindFilter"),"defaults: all, reset with the settings");
        }),
        ("Allied kind 1306: the loader reads the kinds from the quests' categories, the page offers the kept choice and a kind column",() =>
        {
            var loader=Read("src/TenshoAssistant/Societies/AlliedSocieties.Dalamud.cs");
            Check(loader.Contains("quest.BeastTribe.RowId is var tribe and not 0 && quest.ClassJobCategory0.RowId is var category and not 0",StringComparison.Ordinal)
                && loader.Contains("AlliedKinds.OfCategory(allowed,classCategories)",StringComparison.Ordinal)
                && loader.Contains("catch(Exception){kinds.Clear();names.Clear();}",StringComparison.Ordinal),"loader");
            var page=Read("src/TenshoAssistant/MainWindow.AlliedSocieties.cs");
            Check(page.Contains("ComboSetting(\"Allied.Kind\",\"Allied.KindTip\",()=>configuration.AlliedKindFilter",StringComparison.Ordinal)
                && page.Contains("if(!AlliedKinds.Listed(society,chosen))continue;",StringComparison.Ordinal)
                && page.Contains("\"ColumnSociety\",\"ColumnKind\",",StringComparison.Ordinal),"page");
            foreach(var language in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                foreach(var key in new[]{"\"ColumnKind\":","\"Kind\":","\"KindTip\":","\"KindAll\":"})Check(text.Contains(key,StringComparison.Ordinal),$"{language}: {key}");
            }
        }),
    ];
}
