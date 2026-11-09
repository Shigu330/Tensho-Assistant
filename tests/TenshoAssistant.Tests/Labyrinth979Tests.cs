using TenshoAssistant.Competitive;

// X-109 (0.979, DEC-047): a developer reading inside the Beastmaster labyrinth only, read only, written on change, to find when a fight starts.
internal static class Labyrinth979Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Labyrinth 979: its territories stay cut, the reading is developer-only, labyrinth-only, on change",() =>
        {
            Check(CompetitiveZones.Labyrinth.All(CompetitiveZones.Listed.Contains),"still cut");
            var gate=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs"));
            Check(gate.Contains("if(CompetitiveGate.Developer?.Invoke()==true && CompetitiveZones.IsLabyrinth((uint)client.TerritoryType))"),"guard");
            Check(gate.Contains("if(seen==labyrinthSeen)return;"),"on change");
        }),
    ];
}
