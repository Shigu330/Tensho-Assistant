using TenshoAssistant.Leves;

// X-404 (0.1372): the LeveClient sheet writes the « Client: » label before each name (probed on 2026-11-15 with tools/XbmProbe: English
// « Client: » on 282 of 282 names, French « Client : » with a no-break space on 282, German « Auftraggeber: » on 242 and « Auftraggeberin: » on 40,
// Japanese none, its colons parting a group from a person: « ブルゲール商会：仕入担当ウィンスティン・ワルダー »). The page showed it twice.
internal static class LeveClient1372Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string[] Named(params string[] names) { var labels = LeveCatalog.ClientLabels(names); return names.Select(name => LeveCatalog.WithoutClientLabel(name, labels)).ToArray(); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Leve client 1372: the sheet's label is taken off once, in English, French (no-break space) and German (two forms)", () =>
        {
            var english = Named("Client: 2nd Levy Infantry, Yellowjacket Sergeant Sylskaetsyn", "Client: Stillglade Fane ", "Client: Moogle's Gift Mounts", "");
            Check(english.SequenceEqual(["2nd Levy Infantry, Yellowjacket Sergeant Sylskaetsyn", "Stillglade Fane", "Moogle's Gift Mounts", ""]), "en: " + string.Join(" | ", english));
            var french = Named("Client\u00a0: Sergent-major Sylskaetsyn", "Client\u00a0: Wynstin Walder, du Consortium de Brugaire", "Client\u00a0: Maisenta, maison de commerce “le Lièvre noir”");
            Check(french.SequenceEqual(["Sergent-major Sylskaetsyn", "Wynstin Walder, du Consortium de Brugaire", "Maisenta, maison de commerce “le Lièvre noir”"]), "fr: " + string.Join(" | ", french));
            var german = Named(Enumerable.Repeat("Auftraggeber: Gelbjackengefreiter Sylskaetsyn", 6).Concat(Enumerable.Repeat("Auftraggeberin: Audrie", 2)).ToArray());
            Check(german.Distinct().SequenceEqual(["Gelbjackengefreiter Sylskaetsyn", "Audrie"]), "de: " + string.Join(" | ", german.Distinct()));
        }),
        ("Leve client 1372: no label is taken off where the sheet has none (Japanese), nor from a name whose colon is its own", () =>
        {
            var japanese = new[] { "第一艦隊第二戦隊黄套：シルスケートシン陸曹", "ブルゲール商会：仕入担当ウィンスティン・ワルダー", "アマジナ鉱山社：技師", "隊商互助会：商人", "モグモグ" };
            Check(LeveCatalog.ClientLabels(japanese).Count == 0 && Named(japanese).SequenceEqual(japanese), "ja kept as it is");
            var mixed = Named("Client: Ser Aymeric", "Client: Ul'dah: Ironworks", "Client: Ul'dah: Ironworks", "Client: Hildibrand");
            Check(mixed.SequenceEqual(["Ser Aymeric", "Ul'dah: Ironworks", "Ul'dah: Ironworks", "Hildibrand"]), "only the label, once: " + string.Join(" | ", mixed));
            Check(LeveCatalog.WithoutClientLabel("Client:", ["Client:"]) == "Client:", "a name that is only the label stays as it is");
            Check(LeveCatalog.ClientLabels([]).Count == 0, "no names, no label");
        }),
        ("Leve client 1372: wiring: the plugin takes the label off the names it lists; the aetheryte entry names the leve's NPC in both languages", () =>
        {
            var plugin = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("leveList??=LeveClientsNamed(") && plugin.Contains("Leves.LeveCatalog.WithoutClientLabel(leve.Client,labels)"), "names without the label");
            var en = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            var fr = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            Check(en.Contains("\"AetheryteClient\": \"Nearest aetheryte to the leve's NPC\"") && fr.Contains("\"AetheryteClient\": \"Éthérite la plus proche du PNJ du mandat\""), "the NPC of LevelLevemete, not « the client »");
        }),
    ];
}
