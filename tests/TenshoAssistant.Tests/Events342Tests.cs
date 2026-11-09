using TenshoAssistant.Events;

// 0.342 (R-09, DEC-023 point 7; user requests: event shops, the right names, check what other plugins do): running festivals from the
// game, their quests, names from the guide NPC (French client) or LuminaSupplemental, game flags hidden, event shops from the directory.
internal static class Events342Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Festivals: names file, running slots, French name from the guide NPC (earliest separator), game flags hidden",() =>
        {
            using var reader = new StringReader("# notice\nFestivalId,Name\n0,None\n120,Special Event Flag\n176,All Saint's Wake (2026)\n257,Unknown\n");
            var names = Festivals.ReadNames(reader);
            Check(names.Count == 2 && names[176] == "All Saint's Wake (2026)" && !names.ContainsKey(257) && !names.ContainsKey(0),"None and Unknown are no name");
            Check(Festivals.Running([(0,0),(176,1),(176,1),(120,0)]).Select(festival => festival.Id).SequenceEqual([176u,120u]),"empty slots and duplicates dropped");
            Check(Festivals.FrenchNameFromGuide("démarcheur de la Veillée des saints") == "Veillée des saints","de la");
            Check(Festivals.FrenchNameFromGuide("démarcheuse des Feux de la Mort") == "Feux de la Mort","des, before a later « de la »");
            Check(Festivals.FrenchNameFromGuide("organisatrice de la Commémoration") == "Commémoration","organiser");
            Check(Festivals.FrenchNameFromGuide("démarcheur de la fête des étoiles") == "Fête des étoiles","capitalised");
            Check(Festivals.FrenchNameFromGuide("Cait Sith") is null,"no separator: no name");
            var quests = new Dictionary<uint,IReadOnlyList<uint>> { [176] = [71003u] };
            var layouts = new FestivalLayouts(new Dictionary<uint,IReadOnlyList<(uint,uint)>> { [256] = [(1058700u,133u)] },new Dictionary<uint,string>());
            Check(Festivals.Technical(120,names,quests,layouts) && Festivals.Technical(137,names,quests,layouts),"flag by name, and no quest nor NPC");
            Check(!Festivals.Technical(176,names,quests,layouts) && !Festivals.Technical(256,names,quests,layouts),"quests or NPCs: an event");
            Check(!Festivals.Technical(137,names,null,null),"not hidden before the data is read");
        }),
        ("Festivals: read on the framework thread while shown, layouts checked before parsing, shops cached",() =>
        {
            var reader = Read("Events/FestivalReader.Dalamud.cs");
            Check(reader.Contains("TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);",StringComparison.Ordinal) && reader.Contains("TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);",StringComparison.Ordinal),"framework thread, removed on dispose");
            Check(reader.Contains("client.IsLoggedIn",StringComparison.Ordinal) && reader.Contains("catch(Exception error)",StringComparison.Ordinal),"logged in, guarded");
            var check = reader.IndexOf("NpcShopLinks.ValidLayout(bytes)",StringComparison.Ordinal);
            Check(check > 0 && reader.IndexOf("data.GetFile<Lumina.Data.Files.LgbFile>(path)",StringComparison.Ordinal) > check,"layout checked before Lumina parses it");
            Check(reader.Contains("Interlocked.Exchange(ref layoutsStarted,1)",StringComparison.Ordinal),"layouts read once, after the page asked");
            var page = Read("MainWindow.Events.cs");
            Check(page.Contains("Events.Festivals.Technical(",StringComparison.Ordinal) && page.Contains("Events.NameFromGuide",StringComparison.Ordinal),"flags hidden, name source said");
            Check(page.Contains("eventShops.TryGetValue(festival,out var vendors)",StringComparison.Ordinal),"shops computed once per festival");
        }),
    ];
}
