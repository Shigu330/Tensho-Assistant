using TenshoAssistant.Monsters;

// 0.367 (g, C-13): blue magic sources from the wiki spell page — same table as item drops, place and coordinates parsed.
internal static class BlueSpellSources367Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Blue spell sources: wiki locations give a place and map coordinates; the wiki switch is honoured",() =>
        {
            Check(BlueSpellSources.Parse("East Shroud (23.1,20.4)") is { Place:"East Shroud",X:23.1f,Y:20.4f },"place and coordinates");
            Check(BlueSpellSources.Parse("Urqopacha (8.5,23.4,2.7)") is { Place:"Urqopacha",X:8.5f,Y:23.4f },"height ignored");
            Check(BlueSpellSources.Parse(" The Void Ark") is { Place:"The Void Ark",X:null,Y:null },"duty without coordinates");
            // 0.1019 (test audit #9): the cache family and the spell table are checked in BlueSpellName387Tests.
            Check(Read("Plugin.cs").Contains("Enabled=()=>configuration.CommunityWiki",StringComparison.Ordinal),"wiki switch honoured");
        }),
    ];
}
