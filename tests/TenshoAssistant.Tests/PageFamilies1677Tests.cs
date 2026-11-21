using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant.UI;

// X-601 (0.1677 and after, user request of 18/11, D-199): the side bar shrinks to pages that group (one tab template, DrawPageHeader of
// X-600). No page is lost: every page is a side bar entry or a tab of one; a click on an entry, the quick search, a link and the last
// page kept open the right tab; the names are the game's own (docs/evidence/0.1677-game-names.md).
internal static class PageFamilies1677Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    private enum P { Home, Character, Blue, Gear, Settings, Debug }

    private static readonly PageFamilies.Member<P>[] Made =
    [
        new(P.Home, null, true, false),
        new(P.Character, null, true, true), new(P.Blue, P.Character, false, true), new(P.Gear, P.Character, false, true),
        new(P.Settings, null, true, true), new(P.Debug, P.Settings, false, true),
    ];

    /// <summary>The real page list, as the window builds it: page, parent, side bar entry, tab.</summary>
    internal static List<PageFamilies.Member<int>> Real(out List<string> names)
    {
        names = [];
        var rows = new List<(string Page, string? Parent, bool Label, bool Tab)>();
        foreach (Match row in Regex.Matches(Read("src/TenshoAssistant/MainWindow.Pages.cs"), @"^\s*new\(Page\.(\w+),(?:""\w+""|null),(null|PageLabel\(|\(\)=>).*$", RegexOptions.Multiline))
        {
            var parent = Regex.Match(row.Value, @""",Page\.(\w+)[,)]");
            rows.Add((row.Groups[1].Value, parent.Success ? parent.Groups[1].Value : null, row.Groups[2].Value != "null", row.Value.Contains(",Tab:")));
        }
        foreach (var row in rows) names.Add(row.Page);
        var index = names.Select((name, at) => (name, at)).ToDictionary(pair => pair.name, pair => pair.at);
        return rows.Select(row => new PageFamilies.Member<int>(index[row.Page], row.Parent is null ? null : index[row.Parent], row.Label, row.Tab)).ToList();
    }

    private static Dictionary<string, string> Texts(string code)
    {
        var values = new Dictionary<string, string>();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{code}.json")));
        foreach (var section in document.RootElement.EnumerateObject())
            if (section.Value.ValueKind == JsonValueKind.Object)
                foreach (var key in section.Value.EnumerateObject())
                    if (key.Value.ValueKind == JsonValueKind.String) values[section.Name + "." + key.Name] = key.Value.GetString()!;
        return values;
    }

    /// <summary>The names taken from the game: language key, French, English, and the sheet row the evidence cites.</summary>
    internal static readonly (string Key, string French, string English, string Source)[] GameNames =
    [
        ("Navigation.Character", "Personnage", "Character", "MainCommand 2"),
        ("Navigation.BlueMagic", "Grimoire de magie bleue", "Blue Magic Spellbook", "MainCommand 81"),
        ("Navigation.MaskedCarnivale", "La Grande Mascarade", "The Masked Carnivale", "ContentType 27"),
        ("Navigation.Beastmaster", "Dresseur", "Beastmaster", "CharaCardPlayStyle 35"),
        ("Navigation.Chocobo", "Chocobo", "Chocobo", "Addon 9081"),
        ("Navigation.HuntingLog", "Bestiaire", "Hunting Log", "MainCommand 8"),
        ("Navigation.Gear", "Équipement", "Gear", "HowToCategory 7"),
        ("Navigation.RelicWeapons", "Armes antiques", "Relic Weapons", "AchievementCategory 62"),
        ("Navigation.Items", "Objets", "Items", "HowToCategory 6"),
        ("Navigation.ItemSearch", "Recherche d'objets", "Item Search", "Addon 1940"),
        ("Navigation.Sort", "Trier", "Sort", "Addon 1389"),
        ("Navigation.Market", "Marchés", "Markets", "Addon 548"),
        ("Navigation.Crafting", "Artisanat", "Crafting", "HowToCategory 12"),
        ("Navigation.Macros", "Macros", "User Macros", "MainCommand 21"),
        ("Navigation.Gathering", "Récolte", "Gathering", "HowToCategory 13"),
        ("Navigation.OceanFishing", "Pêche en mer", "Ocean Fishing", "ContentType 31"),
        ("Navigation.Map", "Carte", "Map", "MainCommand 16"),
        ("Navigation.Fates", "ALÉA", "FATEs", "ContentType 8"),
        ("Navigation.Shops", "Échoppes", "Shops", "HowTo 20"),
        ("Navigation.Quests", "Quêtes", "Quests", "HowToCategory 5"),
        ("Navigation.Journal", "Journal", "Journal", "MainCommand 4"),
        ("Navigation.Leves", "Mandats", "Levequests", "ContentType 10"),
        ("Navigation.Events", "Événements saisonniers", "Seasonal Events", "AchievementCategory 38"),
        ("Navigation.AlliedSocieties", "Peuples alliés", "Society Quests", "NotebookDivisionCategory 5"),
        ("Navigation.GrandCompanies", "Grandes compagnies", "Grand Companies", "HowToCategory 10"),
        ("Navigation.Deliveries", "Livraisons", "Deliveries", "DescriptionString 1631"),
        ("Navigation.CustomDeliveries", "Commandes spéciales", "Custom Deliveries", "ContentType 25"),
        ("Navigation.Duty", "Missions", "Duty", "MainCommand 43"),
        ("Navigation.DeepDungeons", "Donjons sans fond", "Deep Dungeons", "ContentType 21"),
        ("Category.GoldSaucer", "Gold Saucer", "Gold Saucer", "MainCommand 65"),
        ("Category.Fashion", "Revue de mode", "Fashion Report", "Addon 8819"),
        ("Category.Triad", "Triple Triade", "Triple Triad", "ContentType 32"),
        ("Category.Cactpot", "Mini Cactpot", "Mini Cactpot", "Addon 9358"),
        ("Navigation.Help", "Aide", "Help", "Addon 7662"),
        ("Navigation.Settings", "Paramètres", "Settings", "MainCommand 49"),
        ("Navigation.Garden", "Jardinage", "Gardening", "ItemUICategory 82"),
        ("Navigation.FreeCompany", "Compagnie libre", "Free Company", "MainCommand 27"),
        ("Navigation.IslandSanctuary", "Félicité insulaire", "Island Sanctuary", "ContentType 36"),
        // More names: added by each version of X-601, above this line.
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Page families 1677: a side bar entry opens the tab last shown, else its first page that can be reached; none: no entry", () =>
        {
            Check(PageFamilies.Root(Made, P.Gear) == P.Character && PageFamilies.Root(Made, P.Home) == P.Home, "the entry that stays selected");
            Check(PageFamilies.Of(Made, P.Character).SequenceEqual([P.Character, P.Blue, P.Gear]), "the tabs, in the order of the list");
            Check(PageFamilies.Target(Made, P.Character, null, _ => true) == P.Character, "first visit: the first page");
            Check(PageFamilies.Target(Made, P.Character, P.Gear, _ => true) == P.Gear, "back on the family: the tab last shown");
            Check(PageFamilies.Target(Made, P.Character, P.Gear, page => page != P.Gear) == P.Character, "the tab last shown is gone: the first page");
            Check(PageFamilies.Target(Made, P.Character, null, page => page != P.Character) == P.Blue, "the first page stays in the game (desktop app): the next one");
            Check(PageFamilies.Target(Made, P.Character, P.Blue, _ => false) is null, "nothing can be reached: the entry is not drawn");
            Check(PageFamilies.Target(Made, P.Settings, P.Debug, page => page != P.Debug) == P.Settings, "developer mode off: the debug tab is not opened");
            Check(PageFamilies.Target(Made, P.Home, null, _ => true) == P.Home, "a page alone is a family of one");
            Check(PageFamilies.Target(Made, P.Character, P.Settings, _ => true) == P.Character, "a page of another family is never opened");
        }),
        ("Page families 1677: an orphan page, a parent out of the side bar and a family page without a tab are faults", () =>
        {
            Check(PageFamilies.Faults(Made).Count == 0, "the made list is sound: " + string.Join("; ", PageFamilies.Faults(Made)));
            PageFamilies.Member<P>[] orphan = [.. Made, new(P.Debug + 1, null, false, false)];
            Check(PageFamilies.Faults(orphan).Count == 1 && PageFamilies.Faults(orphan)[0].Contains("orphan"), "a page nothing opens");
            PageFamilies.Member<P>[] nested = [new(P.Home, null, true, true), new(P.Blue, P.Home, false, true), new(P.Gear, P.Blue, false, true)];
            Check(PageFamilies.Faults(nested).Any(fault => fault.Contains("is not a side bar entry")), "a tab of a tab");
            PageFamilies.Member<P>[] mute = [new(P.Home, null, true, false), new(P.Blue, P.Home, false, false)];
            Check(PageFamilies.Faults(mute).Count == 2, "a family without tabs: " + string.Join("; ", PageFamilies.Faults(mute)));
            PageFamilies.Member<P>[] lost = [new(P.Home, null, true, true), new(P.Blue, P.Gear, false, true)];
            Check(PageFamilies.Faults(lost).Count == 1, "a parent that is not in the list");
        }),
        ("Page families 1677: in the real list every page is a side bar entry or a tab of one, and the last page kept still opens its tab", () =>
        {
            var real = Real(out var names);
            var enumBody = Regex.Match(Read("src/TenshoAssistant/MainWindow.cs"), @"enum Page \{([^}]*)\}").Groups[1].Value;
            var pages = enumBody.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            Check(pages.Length >= 52 && pages.Order().SequenceEqual(names.Order()), "every page of the enum is read from the list: " + string.Join(",", pages.Except(names)));
            var faults = PageFamilies.Faults(real);
            Check(faults.Count == 0, string.Join("; ", faults.Select(fault => Regex.Replace(fault, @"\d+", match => names[int.Parse(match.Value)]))));
            // Configuration.LastPage keeps a page's name: every name saved by an older version is still a page, and its family is in the side bar.
            foreach (var page in pages)
            {
                var root = PageFamilies.Root(real, names.IndexOf(page));
                Check(real[root].InSideBar && PageFamilies.Of(real, root).Contains(names.IndexOf(page)), "last page kept: " + page);
            }
            foreach (var kept in new[] { "Home", "Items", "Gathering", "Maps", "Collections", "BlueMagic", "Settings", "Gear", "HuntingLog", "Beastmaster", "Chocobo", "MaskedCarnivale", "RelicWeapons", "Discoveries", "Wishlist" })
                Check(pages.Contains(kept), "a page name saved by an older version is gone: " + kept);
            var window = Read("src/TenshoAssistant/MainWindow.cs");
            Check(window.Contains("Enum.TryParse<Page>(configuration.LastPage,out var saved)") && window.Contains("configuration.LastPage=page.ToString();"), "the last page is kept by its name");
        }),
        ("Page families 1677: each page that groups holds the pages of D-199, in its order", () =>
        {
            var real = Real(out var names);
            string[] Family(string root) => PageFamilies.Of(real, names.IndexOf(root)).Select(at => names[at]).ToArray();
            Check(Family("Collections").SequenceEqual(["Collections", "BlueMagic", "MaskedCarnivale", "Beastmaster", "Chocobo", "HuntingLog", "Gear", "RelicWeapons"]), "Character: " + string.Join(",", Family("Collections")));
            Check(Read("src/TenshoAssistant/MainWindow.Pages.cs").Contains("new(Page.Collections,\"Progression\",PageLabel(\"Navigation.Character\"),"), "named « Character » in the side bar");
            Check(Family("Items").SequenceEqual(["Items", "Storage", "SortPreview", "Market"]), "Items: " + string.Join(",", Family("Items")));
            Check(Read("src/TenshoAssistant/MainWindow.Pages.cs").Contains("new(Page.Items,\"Objects\",PageLabel(\"Navigation.Items\"),"), "named « Items » in the side bar");
            Check(Family("CraftSimulation").SequenceEqual(["CraftSimulation", "CraftPresets", "CraftLive", "MacroChain"]), "Crafting: " + string.Join(",", Family("CraftSimulation")));
            Check(Read("src/TenshoAssistant/MainWindow.Pages.cs").Contains("new(Page.CraftSimulation,\"Production\",PageLabel(\"Navigation.Crafting\"),"), "named « Crafting » in the side bar");
            Check(Family("Gathering").SequenceEqual(["Gathering", "GatheringGp", "GatheringCollectables", "OceanFishing"]), "Gathering: " + string.Join(",", Family("Gathering")));
            Check(Read("src/TenshoAssistant/MainWindow.Pages.cs").Contains("new(Page.Gathering,\"Production\",PageLabel(\"Navigation.Gathering\"),"), "named « Gathering » in the side bar");
            Check(Family("Maps").SequenceEqual(["Maps", "Weather", "Fates", "NpcVendors", "DutyLoot", "MonsterLoot", "Discoveries"]), "World: " + string.Join(",", Family("Maps")));
            Check(Read("src/TenshoAssistant/MainWindow.Pages.cs").Contains("new(Page.Maps,\"World\",PageLabel(\"Navigation.World\"),"), "named « World » in the side bar");
            Check(Family("Quests").SequenceEqual(["Quests", "Leves", "Events", "AlliedSocieties", "GrandCompanies"]), "Quests: " + string.Join(",", Family("Quests")));
            Check(Read("src/TenshoAssistant/MainWindow.Pages.cs").Contains("new(Page.Quests,\"Progression\",PageLabel(\"Navigation.Quests\"),"), "named « Quests » in the side bar");
            Check(Family("SpecialDeliveries").SequenceEqual(["SpecialDeliveries", "CustomDeliveries"]), "Deliveries: " + string.Join(",", Family("SpecialDeliveries")));
            Check(Read("src/TenshoAssistant/MainWindow.Pages.cs").Contains("new(Page.SpecialDeliveries,\"Progression\",PageLabel(\"Navigation.Deliveries\"),"), "named « Deliveries » in the side bar");
            Check(Family("FieldContent").SequenceEqual(["FieldContent", "DeepDungeons"]), "Duty: " + string.Join(",", Family("FieldContent")));
            Check(Read("src/TenshoAssistant/MainWindow.Pages.cs").Contains("new(Page.FieldContent,\"Content\",PageLabel(\"Navigation.Duty\"),"), "named « Duty » in the side bar");
            Check(Family("Fashion").SequenceEqual(["Fashion", "TripleTriad", "MiniCactpot"]), "Gold Saucer: " + string.Join(",", Family("Fashion")));
            Check(Read("src/TenshoAssistant/MainWindow.Pages.cs").Contains("new(Page.Fashion,\"GoldSaucer\",PageLabel(\"Category.GoldSaucer\"),"), "named « Gold Saucer » in the side bar");
            Check(Family("Help").SequenceEqual(["Help", "Lexicon", "Credits"]), "Help: " + string.Join(",", Family("Help")));
            Check(Read("src/TenshoAssistant/MainWindow.Pages.cs").Contains("new(Page.Help,\"References\",PageLabel(\"Navigation.Help\"),"), "named « Help » in the side bar");
            Check(Family("Settings").SequenceEqual(["Settings", "Diagnostics"]), "Settings: " + string.Join(",", Family("Settings")));
            Check(Read("src/TenshoAssistant/MainWindow.Pages.cs").Contains("new(Page.Settings,\"Preferences\",PageLabel(\"Navigation.Settings\"),"), "named « Settings » in the side bar");
            // More families: added by each version of X-601, above this line.
        }),
        ("Page families 1677: the side bar, the page header, the quick search and the page places read the families", () =>
        {
            var window = Read("src/TenshoAssistant/MainWindow.cs");
            Check(window.Contains("if(FamilyTarget(entry.Page) is { } target)Navigation(entry.Label!(),entry.Page,target);"), "an entry is drawn while one of its pages can be reached");
            Check(window.Contains("PageFamilies.Target(FamilyMembers,root,familyLastTab.TryGetValue(root,out var last) ? last:null,Reachable)"), "its target");
            Check(window.Contains("entry.Visible?.Invoke()!=false && ListedHere(shown)"), "reachable: shown and listed on this side");
            var header = Read("src/TenshoAssistant/UI/Navigation/MainWindow.PageHeader.cs");
            Check(header.Contains("if(entry.Page!=current && !Reachable(entry.Page))continue;") && header.Contains("familyLastTab[root]=page;"), "the header hides a tab out of reach and remembers the tab shown");
            var search = Read("src/TenshoAssistant/MainWindow.QuickSearch.cs");
            Check(search.Contains("entry.Label is not null || entry.Tab is not null") && search.Contains("QuickPageLabel(entry)") && search.Contains("Where(entry=>Reachable(entry.Page))"), "the quick search finds a tab by its name");
            Check(search.Contains("case QuickSearchKind.Page when Enum.TryParse<Page>(hit.Key,out var target):page=target;break;"), "and opens that page, so its tab");
            Check(Read("src/TenshoAssistant/MainWindow.Advanced.cs").Contains("(entry.Label is not null || entry.Tab is not null) && entry.Visible?.Invoke()!=false"), "a tab can be listed on one side only, as a page could");
        }),
        ("Page families 1677: folded categories kept when the categories change", () =>
        {
            Check(PageFamilies.KeepFolded(null, ["Tracking"]) is null, "never chosen: the defaults still apply");
            var same = new HashSet<string> { "Tracking" };
            Check(ReferenceEquals(PageFamilies.KeepFolded(same, ["Tracking", "World"]), same), "nothing to drop: the same set");
            var kept = PageFamilies.KeepFolded(new HashSet<string> { "Tracking", "Content", "GoldSaucer" }, ["Tracking"]);
            Check(kept is not null && kept.SetEquals(["Tracking"]), "the category that stays keeps its fold");
            Check(PageFamilies.KeepFolded(new HashSet<string> { "Content" }, ["Tracking"]) is { Count: 0 }, "a choice was made: empty, not the defaults");
        }),
        ("Page families 1677: the side bar is flat but for « Tracking », in the order of D-199", () =>
        {
            Check(PageFamilies.SideBarCategory("Tracking") == "Tracking" && PageFamilies.SideBarCategory("World") is null && PageFamilies.SideBarCategory(null) is null, "one category left");
            var runs = PageFamilies.Runs<string>(["a", "b", "T1", "T2", "c", "T3"], name => name.StartsWith('T') ? "Tracking" : null);
            Check(runs.Count == 4 && runs[0].Key is null && runs[0].Items.SequenceEqual(["a", "b"]) && runs[1].Key == "Tracking" && runs[1].Items.SequenceEqual(["T1", "T2"])
                  && runs[2].Key is null && runs[2].Items.SequenceEqual(["c"]) && runs[3].Key == "Tracking", "entries that follow each other make a group; a key can come back");
            Check(PageFamilies.Runs<string>([], _ => null).Count == 0, "an empty list");
            var list = Read("src/TenshoAssistant/MainWindow.Pages.cs");
            var entries = Regex.Matches(list, @"^\s*new\(Page\.(\w+),(?:""(\w+)""|null),(?!null,)", RegexOptions.Multiline).Select(match => (Page: match.Groups[1].Value, Category: match.Groups[2].Value)).ToArray();
            Check(entries.Select(entry => entry.Page).SequenceEqual(["Home", "MyTools", "Collections", "Items", "Wishlist", "CraftSimulation", "Gathering", "IslandSanctuary", "Maps", "Alarms", "Reminders", "Garden", "FreeCompany",
                "Quests", "SpecialDeliveries", "FieldContent", "Fashion", "Help", "Settings"]), "the side bar: " + string.Join(",", entries.Select(entry => entry.Page)));
            var groups = PageFamilies.Runs(entries, entry => PageFamilies.SideBarCategory(entry.Category.Length == 0 ? null : entry.Category));
            Check(groups.Count == 3 && groups[1].Key == "Tracking" && groups[1].Items.Select(entry => entry.Page).SequenceEqual(["Alarms", "Reminders", "Garden", "FreeCompany"]), "« Tracking » keeps its four pages, unchanged");
            Check(list.Contains("PageFamilies.Runs(PageEntries.Where(entry=>entry.Label is not null).ToArray(),") && list.Contains("PageFamilies.SideBarCategory(entry.Category)"), "the groups of the side bar are read from the list");
            var window = Read("src/TenshoAssistant/MainWindow.cs");
            Check(window.Contains("configuration.NavigationCollapsed = TenshoAssistant.UI.PageFamilies.KeepFolded(configuration.NavigationCollapsed, TenshoAssistant.UI.PageFamilies.SideBarCategories);"), "the saved folds are migrated when the window is built");
            Check(window.Contains("if(group.Category is null && afterCategory){ImGui.Spacing();ImGui.Separator();}"), "the entries after a category are set apart");
        }),
        ("Page families 1677: the folds saved by an older version: « Tracking » keeps its state, the categories that are gone leave the set", () =>
        {
            // What 0.1676 could have saved: the defaults of then with « Tracking » folded by the player.
            var saved = new HashSet<string> { "Progression", "Content", "GoldSaucer", "References", "Tracking" };
            var kept = PageFamilies.KeepFolded(saved, PageFamilies.SideBarCategories);
            Check(kept is not null && kept.SetEquals(["Tracking"]), "« Tracking » stays folded");
            var open = PageFamilies.KeepFolded(new HashSet<string> { "Progression", "Content" }, PageFamilies.SideBarCategories);
            Check(open is { Count: 0 } && !TenshoAssistant.Guide.FirstScreen.Folded(open).Contains("Tracking"), "« Tracking » left open stays open");
            Check(TenshoAssistant.Guide.FirstScreen.Folded(PageFamilies.KeepFolded(null, PageFamilies.SideBarCategories)).Count == 0, "never chosen: nothing folded");
            // The saved set goes through the configuration file as Dalamud writes it and comes back the same.
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new TenshoAssistant.Configuration { NavigationCollapsed = kept, LastPage = "HuntingLog", OpenOnLastPage = true });
            var back = Newtonsoft.Json.JsonConvert.DeserializeObject<TenshoAssistant.Configuration>(json)!;
            Check(back.NavigationCollapsed is not null && back.NavigationCollapsed.SetEquals(["Tracking"]) && back.LastPage == "HuntingLog", "saved and read back");
        }),
        ("Page families 1677: the names are the game's own, in both languages, each cited in the evidence", () =>
        {
            var french = Texts("fr");
            var english = Texts("en");
            var evidence = Read("docs/evidence/0.1677-game-names.md");
            foreach (var (key, fr, en, source) in GameNames)
            {
                Check(french.TryGetValue(key, out var shownFr) && shownFr == fr, $"fr {key}: « {(french.TryGetValue(key, out var a) ? a : "")} », the game says « {fr} » ({source})");
                Check(english.TryGetValue(key, out var shownEn) && shownEn == en, $"en {key}: « {(english.TryGetValue(key, out var b) ? b : "")} », the game says « {en} » ({source})");
                Check(evidence.Contains($"| {source} | {fr} | {en} |"), "evidence row: " + source + " " + fr);
            }
            var probe = Read("tools/HelpTextProbe/Program.cs");
            Check(probe.Contains("if(args[0]==\"names\")") && Read("tools/README.md").Contains("`names"), "the probe that read them");
        }),
    ];
}
