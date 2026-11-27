// X-747 (0.1772, the user's idea I-019): a developer-mode probe of the furniture placed in a house (FFXIVClientStructs: IndoorTerritory →
// FurnitureManager.FurnitureVector) and of its event objects, before showing summoning bells on Tensho's map (R06: nothing shown until a
// reading in the user's house says which Id or object is the bell). Read only, developer mode only, no player's name, its own cleanup.
internal static class HousingProbe1772Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Housing probe 1772: wiring: developer mode only, inside a house, read only, said when it changes, stopped on a failure, removed on dispose", () =>
        {
            var probe = Read("src/TenshoAssistant/Maps/HousingFurnitureProbe.Dalamud.cs");
            Check(probe.Contains("if (failed || !developer() || !client.IsLoggedIn || now - next < 5000) return;", StringComparison.Ordinal), "developer mode, every five seconds");
            Check(probe.Contains("if (manager == null || manager->IndoorTerritory == null || !manager->IsInside()) { said = null; return; }", StringComparison.Ordinal), "inside a house only");
            Check(probe.Contains("if (count is < 0 or > 5000) { said = null; return; }", StringComparison.Ordinal) && probe.Contains("if (text == said) return;", StringComparison.Ordinal), "a bounded count; said only when it changes");
            Check(probe.Contains("thing.ObjectKind == ObjectKind.EventObj", StringComparison.Ordinal) && !probe.Contains("ObjectKind.Player", StringComparison.Ordinal), "event objects only, never players");
            Check(probe.Contains("failed = true;", StringComparison.Ordinal) && probe.Contains("CompetitiveGate.Remove(framework, OnUpdate)", StringComparison.Ordinal), "stopped on a failure, removed on dispose");
            // X-153 (0.1900): registered by the map family.
            Check(Read("src/TenshoAssistant/Plugin.Maps.cs").Contains("housingProbe=new Maps.HousingFurnitureProbe(framework,client,objects,log,()=>configuration.DeveloperMode),()=>housingProbe?.Dispose());", StringComparison.Ordinal), "registered with its cleanup, developer mode");
            var evidence = Read("docs/evidence/0.1772-housing-furniture.md");
            Check(evidence.Contains("FurnitureVector", StringComparison.Ordinal) && evidence.Contains("\n---\n", StringComparison.Ordinal), "the evidence, both halves");
        }),
    ];
}
