using System.Text.RegularExpressions;

// 0.1701 (X-605, third part, the user's request of 18/11: « arrange the tabs of every page by dragging them »): every page's tab bar written
// by hand goes through one component the player arranges (OrderedTabs, the order kept by bar id and put back in Settings › Appearance);
// the detail tabs are that component with each body in a child window. Only the chat's tabs keep their own (X-687).
internal static class OrderedTabs1701Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    private static readonly (string File, string Id, int Tabs)[] Bars =
    [
        ("MainWindow.Storage.cs", "StorageTabs", 8), ("MainWindow.Market.cs", "MarketTabs", 3), ("MainWindow.FreeCompany.cs", "FreeCompanyTabs", 6),
        ("MainWindow.Maps.cs", "MapsTabs", 4), ("MainWindow.MapLocalLists.cs", "MapLocalTabs", 0), ("MainWindow.IslandSanctuary.cs", "IslandTabs", 2),
        ("MainWindow.CustomDeliveries.cs", "CustomDeliveriesTabs", 2), ("MainWindow.CraftSimulation.cs", "CraftPageTabs", 4),
        ("MainWindow.Collections.cs", "CollectionKinds", 7), ("MainWindow.Settings.cs", "SettingsSections", 0), ("MainWindow.TripleTriad.cs", "TriadNotebook", 5),
        ("MainWindow.TripleTriad.cs", "TriadSeries", 2), ("MainWindow.Items.cs", "StorageScope", 0), ("MainWindow.Items.cs", "RecipeDirections", 2),
        ("MainWindow.Bank.cs", "BankOriginTabs", 0),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Ordered tabs 1701: every page's tab bar written by hand is arranged with the mouse (OrderedTabs), its tabs known by a key", () =>
        {
            foreach (var (file, id, tabs) in Bars)
            {
                var code = Read(file);
                var at = code.IndexOf("OrderedTabs(\"" + id + "\",", StringComparison.Ordinal);
                Check(at > 0, "OrderedTabs for " + id + " in " + file);
                Check(!code.Contains("ImGui.BeginTabBar(\"" + id + "\"", StringComparison.Ordinal), "no bar written by hand left: " + id);
                if (tabs == 0) continue;
                var end = code.IndexOf("]);", at, StringComparison.Ordinal);
                var keys = Regex.Matches(code[at..end], @"(?:new(?: OrderedTab)?|Kind)\(""(\w+)""").Count;
                Check(keys >= tabs, id + ": " + keys + " keyed tabs of " + tabs);
            }
        }),
        ("Ordered tabs 1701: one component: reorderable, settled (X-709), drawn in the order kept, its open tab's body in the page; the detail tabs are it with a child window", () =>
        {
            var detail = Read("UI/Components/DetailPage.cs");
            Check(detail.Contains("internal readonly record struct OrderedTab(string Key,string Label,Action Draw,bool Select=false);", StringComparison.Ordinal), "a tab: key, label, body, asked");
            Check(detail.Contains("private bool OrderedTabs(string id,IReadOnlyList<OrderedTab> tabs,ImGuiTabBarFlags flags=ImGuiTabBarFlags.None)", StringComparison.Ordinal), "the component");
            Check(detail.Contains("TabOrder.Arrange(tabs,configuration.TabOrders.GetValueOrDefault(id),tab=>tab.Key)", StringComparison.Ordinal) && detail.Contains("TabBarAfter(id,drag,dragged);", StringComparison.Ordinal), "order kept, a drag kept");
            Check(detail.Contains("ordered.Add(new(key,label,()=>{if(ImGui.BeginChild(id+key+\"Body\",System.Numerics.Vector2.Zero,false))draw();ImGui.EndChild();},key==requested));", StringComparison.Ordinal)
                && detail.Contains("if(!OrderedTabs(id,ordered))focus??=requested;", StringComparison.Ordinal), "DetailTabs = OrderedTabs with a child window; the request kept when no bar is drawn");
            Check(Regex.Matches(detail, @"ImGui\.BeginTabBar\(").Count == 1, "one tab bar in the component");
        }),
        ("Ordered tabs 1701: the links that open a tab still do (a route's Build button, a map request, a collection jump, a settings tab, a Triple Triad tab)", () =>
        {
            Check(Read("MainWindow.FreeCompany.cs").Contains("new(\"Builds\",localization.Get(\"FreeCompany.TabBuilds\"),DrawVesselBuilds,request==2),", StringComparison.Ordinal)
                && Read("MainWindow.FreeCompany.cs").Contains("if(drawn && request>=0 && freeCompanyTabRequest==request)freeCompanyTabRequest=-1;", StringComparison.Ordinal), "Free Company");
            Check(Read("MainWindow.Maps.cs").Contains("if(drawn && request>=0 && mapsTabRequest==request)mapsTabRequest=-1;", StringComparison.Ordinal), "Maps");
            Check(Read("MainWindow.Collections.cs").Contains("() => { collectionKind = kind; if (focusCollectionKind == kind) focusCollectionKind = null; }, focusCollectionKind == kind);", StringComparison.Ordinal), "Collections: selected until open");
            var settings = Read("MainWindow.Settings.cs");
            Check(settings.Contains("if(requestedSettingsTab==key)requestedSettingsTab=null;", StringComparison.Ordinal) && settings.Contains("},selected);", StringComparison.Ordinal), "Settings");
            var triad = Read("MainWindow.TripleTriad.cs");
            Check(triad.Contains("new(\"Deck\", localization.Get(\"TriadUi.Deck\"), () => { DrawTriadSavedDecks(); DrawTriadDeck(); }),", StringComparison.Ordinal) && TenshoAssistant.Help.NavTargets.Bars["TripleTriad"] == "TriadNotebook" && !triad.Contains("TriadTabFlags", StringComparison.Ordinal), "Triple Triad");
            Check(Read("MainWindow.Bank.cs").Contains("origins.Add(new(entry.Key.Kind + \".\" + entry.Key.StorageId.ToString(System.Globalization.CultureInfo.InvariantCulture), BankEntryName(entry),", StringComparison.Ordinal), "bank: a storage's tab known by its kind and number, not its place");
        }),
    ];
}
