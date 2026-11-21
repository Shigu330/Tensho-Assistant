using TenshoAssistant.Npcs;

// 0.1689 (X-684, eighth part, the user's request of 20/11: « every tooltip gives everything Tensho knows »): the one NPC tooltip (DrawNpcTooltip,
// facts in Npcs/NpcTips): icon, name, places, shops, what else Tensho knows (a Triple Triad match), then the click; the map's labels take the
// same facts as text; every NPC named in the map's lists, the Vendors page, an item's sources and the Triple Triad page goes through it.
internal static class NpcTips1689Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");
    private static string More(int count) => $"and {count} more";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("NPC tips 1689: the lists: blanks out, each line once, at most so many, then « and n more »", () =>
        {
            Check(NpcTips.Lines(["a", " ", null, "b", "a ", "c"], 6, More).SequenceEqual(["a", "b", "c"]), "blanks out, each once");
            Check(NpcTips.Lines(["p1", "p2", "p3", "p4", "p5"], NpcTips.MaxPlaces, More).SequenceEqual(["p1", "p2", "p3", "and 2 more"]), "three places, then how many more");
            Check(NpcTips.Lines([], 3, More).Count == 0, "nothing: no line");
        }),
        ("NPC tips 1689: a vendor's shops: those read (six at most), those with no sale read counted apart, « none » when neither; a service NPC has none", () =>
        {
            string Unknown(int count) => $"{count} unknown";
            var read = Enumerable.Range(1, 8).Select(index => $"shop {index}").ToList();
            var shops = NpcTips.Shops(9, read, 1, More, Unknown, "none");
            Check(shops.Count == 8 && shops[5] == "shop 6" && shops[6] == "and 2 more" && shops[7] == "1 unknown", string.Join(" | ", shops));
            Check(NpcTips.Shops(2, [], 2, More, Unknown, "none").SequenceEqual(["2 unknown"]), "only unknown shops: said");
            Check(NpcTips.Shops(1, [" "], 0, More, Unknown, "none").SequenceEqual(["none"]), "none read: « none »");
            Check(NpcTips.Shops(0, ["shop"], 0, More, Unknown, "none").Count == 0, "a service NPC with no shop id: no shop line (its name says what it does)");
        }),
        ("NPC tips 1689: the facts and their text for the map's labels: name, shops and notes; the places left to the icon", () =>
        {
            var facts = NpcTips.Build("  Gerolt ", 60412, ["Zone · X 1.0 Y 2.0"], ["· Shop — 3 item(s)"], ["Fee: 10 MGP", null, "", "Fee: 10 MGP", "Beaten: Yes"]);
            Check(facts.Name == "Gerolt" && facts.Icon == 60412 && facts.Places.Count == 1 && facts.Notes.SequenceEqual(["Fee: 10 MGP", "Beaten: Yes"]), "trimmed, notes cleaned");
            Check(NpcTips.Text(facts) == "Gerolt\n· Shop — 3 item(s)\nFee: 10 MGP\nBeaten: Yes", NpcTips.Text(facts));
            Check(NpcTips.Text(NpcTips.Build("Vendor", 0, [], [], [])) == "Vendor", "a service NPC: its name only, as the map's label since 0.679");
        }),
        ("NPC tips 1689: wiring: one body, used by the map's lists, the Vendors page, an item's sources and the Triple Triad page; nothing recopied", () =>
        {
            var body = Read("src/TenshoAssistant/MainWindow.NpcTooltip.cs");
            Check(body.Contains("private void DrawNpcTooltip(NpcTipFacts facts,string? after=null)", StringComparison.Ordinal) && body.Contains("using(TooltipScope.Begin())", StringComparison.Ordinal), "the body");
            Check(body.Contains("cached.Key==key && cached.Revision==localization.Revision", StringComparison.Ordinal), "built once while hovered, not every frame");
            Check(body.Contains("localization.Format(\"TriadNpcs.BeatenTip\"", StringComparison.Ordinal) && body.Contains("localization.Format(\"MapLocal.TriadMissing\",missing)", StringComparison.Ordinal), "a Triple Triad match: beaten and missing cards too");
            var lists = Read("src/TenshoAssistant/MainWindow.MapLocalLists.cs");
            Check(lists.Contains("if(ImGui.IsItemHovered())DrawNpcTooltip(VendorTipFacts(vendor),", StringComparison.Ordinal) && lists.Contains("if(ImGui.IsItemHovered())DrawNpcTooltip(TriadTipFacts(npc,beaten,missing.Count,questsDone,", StringComparison.Ordinal), "the map's lists");
            Check(Read("src/TenshoAssistant/MainWindow.TriadNpcs.cs").Contains("if(ImGui.IsItemHovered())DrawNpcTooltip(TriadTipFacts(npc,beaten,missing.Count,questsDone,Card),", StringComparison.Ordinal), "the Triple Triad page");
            Check(Read("src/TenshoAssistant/MainWindow.NpcVendors.cs").Contains("if(ImGui.IsItemHovered())DrawNpcTooltip(VendorTipFacts(offer.Vendor));", StringComparison.Ordinal), "the Vendors page");
            Check(Read("src/TenshoAssistant/MainWindow.SourceLinks.cs").Contains("if(ImGui.IsItemHovered())DrawNpcTooltip(VendorTipFacts(vendor),localization.Get(\"NpcUi.ClickVendor\"));", StringComparison.Ordinal), "an item's sources");
            var map = Read("src/TenshoAssistant/MainWindow.MapView.cs");
            Check(map.Contains("private string VendorLabel(Npcs.NpcVendor vendor)=>Npcs.NpcTips.Text(Npcs.NpcTips.Build(NpcName(vendor),0,[],VendorShopLines(vendor,", StringComparison.Ordinal), "the map's labels: the same shop lines as text (X-707)");
            var everywhere = string.Join("\n", Directory.EnumerateFiles(Path.GetFullPath("src/TenshoAssistant"), "MainWindow*.cs").Select(File.ReadAllText));
            Check(!everywhere.Contains("WrappedTooltip(localization.Format(\"TriadNpcs.Tooltip\"", StringComparison.Ordinal) && !everywhere.Contains("WrappedTooltip(VendorLabel(", StringComparison.Ordinal), "no NPC tooltip written by hand any more");
            Check(System.Text.RegularExpressions.Regex.Matches(everywhere, "\"TriadNpcs\\.Tooltip\"").Count == 1, "the Triple Triad facts said in one place");
            foreach (var code in new[] { "en", "fr" })
            {
                var language = Read($"src/TenshoAssistant/Localization/Languages/{code}.json");
                Check(language.Contains("\"BeatenTip\": \"", StringComparison.Ordinal) && language.Contains("\"ClickVendor\": \"", StringComparison.Ordinal), code + ": the texts");
            }
        }),
    ];
}
