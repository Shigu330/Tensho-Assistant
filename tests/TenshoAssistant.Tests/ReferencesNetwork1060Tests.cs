// 0.1060 (audit A057): REFERENCES sends the reader to the network list produced from the code (SECURITY.md, tools/docgen.py) in
// both halves, and the sources it says its old table misses are indeed in that produced list.
internal static class ReferencesNetwork1060Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("References: network table points to the list produced in SECURITY.md",() =>
        {
            var references = File.ReadAllText(Path.GetFullPath("docs/project/REFERENCES.md")).Replace("\r\n","\n");
            Check(references.Contains("### Read at run time (network)\n\nThe complete list, produced from the code at every version, is in [SECURITY.md](../../SECURITY.md)"),"English half points to SECURITY.md");
            Check(references.Contains("### Lues à l'exécution (réseau)\n\nLa liste complète, produite depuis le code à chaque version, est dans [SECURITY.md](../../SECURITY.md)"),"French half points to SECURITY.md");
            var security = File.ReadAllText(Path.GetFullPath("SECURITY.md"));
            foreach(var source in new[]{"PaissaDB","Shared discoveries exchange","Discord webhook","Desktop app update"})
                Check(security.Contains(source),"the produced network list has " + source);
        }),
    ];
}
