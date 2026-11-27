// X-569 (0.1884, audit of 17/11, part C): the public members the plugin never used, kept only for their tests, are gone with those tests'
// checks; the tests that read them now read what the plugin uses (ProblemReport.Prepare, TimeLeft.Text, the duty loot client's
// CacheLifetime, ChatHistory.Trim). IDE0051 cannot see public members: this list keeps them from coming back unseen.
internal static class DeadCode1884Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Dead code 1884: the public members served only to the tests are gone", () =>
        {
            foreach (var (file, text) in new[]
            {
                ("Gathering/Spearfishing.cs", "SpearfishCandidates"),
                ("Gathering/Spearfishing.cs", "Candidates("),
                ("Community/GatherBuddyFish.cs", "IEnumerable<GatherBuddyFish> Spearfish"),
                ("Help/ProblemReport.cs", "ProblemLink"),
                ("Monsters/WikiMonsterSources.cs", "MonsterDropCandidates"),
                ("Character/StatFormulas.cs", "Useful("),
                ("Market/CurrencyValue.cs", "> Rank("),
                ("Localization/TenshoCulture.cs", "Hours("),
                ("Chat/ChatHistory.cs", "void Rewrite("),
                ("Duties/DutyLootDocument.cs", "IsExhaustive"),
                ("Duties/DutyLootService.cs", "FreshFor"),
                ("Gathering/OceanFishing.cs", "SlotOf("),
                ("Quests/QuestService.cs", "JoinLabel("),
                ("Reminders/GardenView.cs", "LeftText("),
                ("Npcs/NpcVendorCatalog.cs", "SalesOfShop("),
            })
                Check(!Read("src/TenshoAssistant/" + file).Contains(text, StringComparison.Ordinal), $"{Path.GetFileName(file)}: {text}");
        }),
        ("Dead code 1884: a duty loot table is never said to be whole (the property that always said no is gone, nothing claims it)", () =>
        {
            foreach (var file in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant/Duties"), "*.cs"))
                Check(!File.ReadAllText(file).Contains("Exhaustive", StringComparison.Ordinal), Path.GetFileName(file));
        }),
        // X-878 (0.1889, review of 0.1888, M2): the 12 members of the tooled survey of 0.1884 (names declared in the plugin, absent from
        // src/ but their declaration, present in the tests) kept for their tests, each named with its reason, so a later relay knows they
        // were examined. A member removed from the plugin leaves this list in the same version; a new test-only member is added here only
        // with its reason. The survey went by name: overloaded or common names were not seen by it.
        ("Dead code 1884: the 12 members kept for their tests are named one by one, with their reason, and still exist", () =>
        {
            foreach (var (file, declaration, reason) in Kept)
            {
                Check(reason.Length > 20, $"{declaration}: a reason");
                Check(Read("src/TenshoAssistant/" + file).Contains(declaration, StringComparison.Ordinal), $"{Path.GetFileName(file)}: {declaration} is gone: remove it from Kept");
            }
            Check(Kept.Length == 12 && Kept.Select(kept => kept.Declaration).Distinct().Count() == 12, "twelve members, once each");
        }),
    ];

    /// <summary>The members served only to the tests and kept (X-569, 0.1884), with why.</summary>
    internal static readonly (string File, string Declaration, string Reason)[] Kept =
    [
        ("Alarms/ServerBarEntries.cs", "public static readonly IReadOnlyList<string> LabelKeys=",
            "coverage accessor: ServerBarSettings1730 checks the ten server bar entries, one case each"),
        ("Competitive/CompetitiveGateHooks.cs", "internal static bool HoldsInstance=>",
            "proof of the unload: UnloadRoots1187, ReaderStart1199, CompileOrder1200 and SharedState1298 see the static hooks emptied"),
        ("Diagnostics/UnloadWatch.cs", "public static List<string> Statics(Assembly assembly,int max=40)",
            "proof of the unload: UnloadStatics1188, RaphaelStatic1194 and UnloadCrash1845 list the statics left holding the plugin"),
        ("Discoveries/DiscoveryDatabase.cs", "public long CountAside()",
            "proof of the rows set aside: AfterError1560, AsideGist1561, DamagedRow1596, GistRefused1597, MetaUnsaved1825 count them"),
        ("Discoveries/GistRotation.cs", "public static string NotesText(IEnumerable<(string Gist,string Revision)> notes)",
            "format shared with the merge tool: GistRotation1817, GistKept1820 and GistOwner1822 build the served notes with it"),
        ("Guide/GuideFlow.cs", "public static IReadOnlyDictionary<string, string> LeftOutOfVisit =>",
            "coverage accessor: PageCoverageTests and GuideVisit1045 check each page is visited or left out with a reason"),
        ("Guide/GuideFlow.cs", "public static IReadOnlyList<GuideStop> EveryStop =>",
            "coverage accessor: GuideFlowTests, GuideVisit1045, FirstScreen1222 and PageCoverageTests read every stop of the guide"),
        ("Guide/GuideFlow.cs", "public static IReadOnlyList<GuideWindowTask> EveryWindow =>",
            "coverage accessor: GuideFlowTests reads every game window task of the guide"),
        ("Help/Faq.cs", "public static string? PageOf(string key)",
            "coverage accessor: FaqTests, LocalLink1081, DesktopInstall1113, WishlistTool1106 and NavTargets1809 check an answer's page"),
        ("Help/HelpIndex.cs", "public static IEnumerable<string> AllDomains =>",
            "coverage accessor: HelpTests checks each help domain has its self-check line"),
        ("Inventory/InventorySnapshot.cs", "public long? ExactQuantity =>",
            "contract of the lower bound: InventoryTests, InventoryBankTests, InventoryReadEvidenceTests (19 checks) prove no exact count without a whole read"),
        ("Quests/QuestRewardIndex.cs", "public int SourceCount=>",
            "proof of the indexing: QuestRewardIndexTests counts the sources indexed per item"),
    ];
}
