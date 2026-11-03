using TenshoAssistant.Gear;

// 0.340 (R-11 step 4, DEC-023 point 5): chocobo barding — filter, progress, and the framework-thread reader's guards.
internal static class Barding340Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Barding: filter by name or teaching item, missing only, progress over the bardings an item teaches",() =>
        {
            var sets = new[]
            {
                new BardingSet(0,"Harnachement en cuir",0,0,0,0,0,0),
                new BardingSet(3,"Barde du Maelstrom",1,2,3,1,3,6021),
                new BardingSet(7,"Barde des Deux Vipères",1,2,3,2,7,6024),
            };
            string? Item(uint id) => id == 6024 ? "Barde des Deux Vipères (objet)" : null;
            Check(ChocoboBarding.Filter(sets,"maelstrom",false,null,Item).Single().Id == 3,"by name");
            Check(ChocoboBarding.Filter(sets,"(objet)",false,null,Item).Single().Id == 7,"by item name");
            var unlocked = new HashSet<uint> { 3 };
            Check(ChocoboBarding.Filter(sets,"",true,unlocked,Item).Select(set => set.Id).SequenceEqual([0u,7u]),"missing only");
            Check(ChocoboBarding.Filter(sets,"",true,null,Item).Count == 0,"missing only with unlocks not read: nothing claimed missing");
            Check(ChocoboBarding.Progress(sets,unlocked) == (1,2) && ChocoboBarding.Progress(sets,null) is null,"progress: taught bardings only, unknown when not read");
        }),
        ("Barding: read on the framework thread only while shown, guarded; teaching action and evidence pinned",() =>
        {
            var reader = Read("Gear/ChocoboReader.Dalamud.cs");
            Check(reader.Contains("framework.Update+=OnUpdate;",StringComparison.Ordinal) && reader.Contains("framework.Update-=OnUpdate;",StringComparison.Ordinal),"framework thread, removed on dispose");
            Check(reader.Contains("now>wantedUntil",StringComparison.Ordinal) && reader.Contains("client.IsLoggedIn",StringComparison.Ordinal) && reader.Contains("catch(Exception error)",StringComparison.Ordinal),"only while wanted and logged in, guarded");
            Check(Read("Gear/GearLoader.Dalamud.cs").Contains("ChocoboBarding.TeachAction",StringComparison.Ordinal) && ChocoboBarding.TeachAction == 1013,"ItemAction 1013 teaches a barding");
            Check(File.ReadAllText(Path.GetFullPath("docs/evidence/0.340-chocobo-barding.md")).Contains("MATCHED 101 data0==row 101 actions 1013x101",StringComparison.Ordinal),"evidence of the probe");
            Check(Read("Plugin.cs").Contains("new Gear.ChocoboReader(framework,client,unlocks,data,log),()=>chocobo?.Dispose());",StringComparison.Ordinal),"owned and disposed");
        }),
    ];
}
