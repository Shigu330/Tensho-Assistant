// 0.1830 (X-828, review of 0.1826, findings from before the block). M1: a new gist was made before github.json was known to be writable; at
// the first exchange with that file unwritable, the gist was made, then the exchange stopped before the issue; if the rotation's state could not
// be written either (the same folder), that gist was left behind and a second one made after a reload. Now github.json is written right before
// a new gist is made: nothing is made while it cannot be (behaviour: MetaUnsaved1825Tests, first case, against a fake GitHub). M2 (a damaged row
// met by a save, settled at once, so the copy on the gist is never read back): set aside with its reason (a read-back adds the copy, counting this
// client's sightings twice, and comes only at the next start), said in the code. Here, the wiring.
internal static class ReviewOld1830Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Review old 1830: M1: github.json written before a new gist is made; M2 set aside with its reason in the code",() =>
        {
            var code=Read("src/TenshoAssistant/Discoveries/GitHubExchange.cs");
            var made=code.IndexOf("        if(gist.Length==0)\n        {\n",StringComparison.Ordinal);
            var post=code.IndexOf("written=await Api(HttpMethod.Post,\"https://api.github.com/gists\",",StringComparison.Ordinal);
            Check(made>0 && post>made && code[made..post].Contains("KeepMeta();",StringComparison.Ordinal),"KeepMeta before the POST of a new gist");
            Check(Read("tests/TenshoAssistant.Tests/MetaUnsaved1825Tests.cs").Contains("Check(server.Count(\"POST\",Gists)==0 && server.Count(\"POST\",Issues)==0,\"no gist made, no issue opened\");",StringComparison.Ordinal),
                "the behaviour case: no gist made while github.json cannot be written");
            Check(Read("src/TenshoAssistant/Discoveries/DiscoveryDatabase.cs").Contains("X-828 M2 (review of 0.1826): with one client, the row written holds everything.",StringComparison.Ordinal),
                "M2's limit and reason said where the row is set aside");
        }),
    ];
}
