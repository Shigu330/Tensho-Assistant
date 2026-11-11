// 0.1076 (audit A057, rest): REFERENCES names the eight community switches of Settings › Advanced and the « game data only » switch,
// in both halves, and its submarine duration row cites DEC-043.
internal static class ReferencesSwitches1076Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("REFERENCES: eight community switches and DEC-043, in both halves",() =>
        {
            var refs = File.ReadAllText(Path.GetFullPath("docs/project/REFERENCES.md")).Replace("\r\n","\n");
            Check(refs.Contains("(LuminaSupplemental, Garland, wiki, GatherBuddy, Teamcraft, Fashion Report, PaissaDB, Universalis)"),"English list of the eight switches");
            Check(refs.Contains("(LuminaSupplemental, Garland, wiki, GatherBuddy, Teamcraft, Revue de mode, PaissaDB, Universalis)"),"French list of the eight switches");
            var advanced = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Advanced.cs"));
            foreach (var key in new[]{"Advanced.Lumina\"","Advanced.Garland\"","Advanced.Wiki\"","Advanced.GatherBuddy\"","Advanced.Teamcraft\"","Advanced.Fashion\"","Advanced.Paissa\"","Advanced.Universalis\""})
                Check(advanced.Contains(key),$"the switch {key} still exists, as the list says");
            Check(refs.Contains("(DEC-043, reading pending)") && refs.Contains("(DEC-043, relevé en attente)"),"the submarine duration row cites DEC-043 in both halves");
        }),
    ];
}
