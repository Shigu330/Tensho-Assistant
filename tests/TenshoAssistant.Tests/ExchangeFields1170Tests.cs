using System.IO.Compression;
using System.Text;
using TenshoAssistant.Discoveries;

// 0.1170 (X-186, user's choice of 12/11 « Oui, ignore-les », night review N31): the plugin reads the common file (v3) ignoring a field it does not
// know (a field added later needs no version 4), but a line that holds Own or Key (a player's own part, the fields tied to privacy) is refused whole;
// the merge tool stays strict on the players' gists. Behaviour tests on real gzip kind files read by ExchangeV3.ReadKind.
internal static class ExchangeFields1170Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (byte[] Bytes, ExchangeV3Entry Entry) KindFile(DiscoveryKind kind, params string[] facts)
    {
        var header = $"{{\"Format\":\"tensho-discoveries\",\"Version\":3,\"Kind\":{(int)kind},\"Count\":{facts.Length},\"Producer\":\"t\",\"Written\":\"2026-11-10T00:00:00+00:00\"}}";
        using var memory = new MemoryStream();
        using (var gzip = new GZipStream(memory, CompressionLevel.Optimal, true)) gzip.Write(Encoding.UTF8.GetBytes(string.Concat(new[] { header }.Concat(facts).Select(line => line + "\n"))));
        var bytes = memory.ToArray();
        return (bytes, new() { Name = ExchangeV3.FileName(kind), Kind = kind, Count = facts.Length, Bytes = bytes.Length, Sha256 = ExchangeV3.Sha256(bytes) });
    }
    private static string Fact(uint subject, string extra = "")
        => $"{{\"Kind\":5,\"Subject\":{subject},\"Detail\":0,\"Territory\":0,\"Map\":0,\"CellX\":0,\"CellZ\":0,\"X\":0,\"Z\":0,\"LevelMin\":1,\"LevelMax\":1,\"Seen\":3,\"Imported\":0,"
           + $"\"First\":\"2026-11-01T00:00:00+00:00\",\"Last\":\"2026-11-02T00:00:00+00:00\"{extra}}}";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Exchange fields 1170: a field the plugin does not know is ignored when it reads the common file; the fact is kept whole", () =>
        {
            var lines = Enumerable.Range(1, 40).Select(subject => Fact((uint)subject)).ToList();
            lines[0] = Fact(1, ",\"Rarity\":2,\"Notes\":{\"a\":[1,2]}");
            var (bytes, entry) = KindFile(DiscoveryKind.Venture, [.. lines]);
            var read = ExchangeV3.ReadKind(new MemoryStream(bytes), entry);
            Check(read.Refused == 0 && read.Facts.Count == 40, $"nothing refused ({read.Refused})");
            Check(read.Facts.Single(fact => fact.Subject == 1) is { Seen: 3, LevelMin: 1, Own: null }, "the known fields read, the unknown ones dropped");
        }),
        ("Exchange fields 1170: a line holding Own or Key (any case) is refused whole; the same names deeper in a value are not a refusal", () =>
        {
            var lines = Enumerable.Range(1, 100).Select(subject => Fact((uint)subject)).ToList();
            lines[0] = Fact(1, ",\"Own\":{\"LevelMin\":1}"); lines[1] = Fact(2, ",\"Key\":\"5:2\""); lines[2] = Fact(3, ",\"own\":null"); lines[3] = Fact(4, ",\"KEY\":1");
            lines[4] = Fact(5, ",\"Extra\":{\"Own\":1,\"Key\":2}");
            var (bytes, entry) = KindFile(DiscoveryKind.Venture, [.. lines]);
            var read = ExchangeV3.ReadKind(new MemoryStream(bytes), entry);
            Check(read.Refused == 4 && read.Facts.Count == 96, $"four lines refused ({read.Refused}, {read.Facts.Count})");
            Check(read.Facts.All(fact => fact.Subject > 4 && fact.Own is null), "none of them read in part");
            Check(ExchangeV3.NamesPrivate(Encoding.UTF8.GetBytes(Fact(9, ",\"Own\":{}"))) && !ExchangeV3.NamesPrivate(Encoding.UTF8.GetBytes(Fact(9, ",\"Extra\":{\"Own\":1}"))), "top level only");
        }),
        ("Exchange fields 1170: the merge tool stays strict on the players' gists; the README says both", () =>
        {
            var tool = File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py"));
            Check(tool.Contains("if set(f)-FIELDS or not typed(f) or f[\"Kind\"] not in KINDS"), "any name outside its list refuses a gist's line");
            var readme = File.ReadAllText(Path.GetFullPath("community-discoveries/README.md"));
            Check(readme.Contains("Since Tensho 0.1170 the plugin ignores a name it does not know") && readme.Contains("Depuis Tensho 0.1170, le plugin ignore un nom qu'il ne connaît pas"), "README in both languages");
        }),
    ];
}
