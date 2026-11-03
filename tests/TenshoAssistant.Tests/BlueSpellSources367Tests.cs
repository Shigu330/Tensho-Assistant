using TenshoAssistant.Monsters;

// 0.367 (g, C-13): blue magic sources from the wiki spell page — same table as item drops, place and coordinates parsed.
internal static class BlueSpellSources367Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Blue spell sources: wiki locations give a place and map coordinates; spell pages kept apart from item pages",() =>
        {
            Check(BlueSpellSources.Parse("East Shroud (23.1,20.4)") is { Place:"East Shroud",X:23.1f,Y:20.4f },"place and coordinates");
            Check(BlueSpellSources.Parse("Urqopacha (8.5,23.4,2.7)") is { Place:"Urqopacha",X:8.5f,Y:23.4f },"height ignored");
            Check(BlueSpellSources.Parse(" The Void Ark") is { Place:"The Void Ark",X:null,Y:null },"duty without coordinates");
            const string json = "{\"parse\":{\"title\":\"Bad Breath\",\"revid\":7,\"text\":{\"*\":\"<table class=\\\"item sortable\\\"><tr><th>Enemy</th><th>Level</th><th>Location</th></tr><tr><td>Lush Morbol</td><td>30-34</td><td>South Shroud</td></tr></table>\"}}}";
            Check(WikiMonsterParser.Parse(json,11,"Bad Breath",DateTimeOffset.UnixEpoch).Monsters is [{ Name:"Lush Morbol",Location:"South Shroud" }],"spell table parsed like item drops");
            Check(Read("Plugin.cs").Contains("family:\"wiki-blue-spells-2\"",StringComparison.Ordinal) && Read("Plugin.cs").Contains("Enabled=()=>configuration.CommunityWiki",StringComparison.Ordinal),
                "own cache family, wiki switch honoured");
        }),
    ];
}
