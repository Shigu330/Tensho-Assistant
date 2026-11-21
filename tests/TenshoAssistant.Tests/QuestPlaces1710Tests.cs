using TenshoAssistant.Quests;

// X-606 (0.1710, the user's request of 18/11): the Quests page shows the giver and its place, the NPC the quest ends with, the jobs, the allied
// society or grand company (from the sheets, probe of 0.1708), puts the giver on Tensho's map from the sheet's Level row, and an accepted
// quest's current step from the game's own quest markers (X-57).
internal static class QuestPlaces1710Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static QuestEntry Entry(uint id) => new(id, "Q" + id, "", 0, QuestProgress.Active, 1);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Quest places 1710: the current step is the first point the game shows for the quest, by its number in the low 16 bits", () =>
        {
            (uint, uint, float, float)[] points = [(66000u, 128u, 1f, 2f), (2606u, 0u, 9f, 9f), (2606u, 129u, 10.5f, -20.25f), (2606u, 130u, 3f, 4f)];
            Check(QuestPlaces.Step(points, 68142) == (129u, 10.5f, -20.25f), "row 68142 is quest 2606: its first point with a map: " + QuestPlaces.Step(points, 68142));
            Check(QuestPlaces.Step(points, 2606) == (129u, 10.5f, -20.25f), "the number itself too");
            Check(QuestPlaces.Step(points, 65564) is null, "a quest the game shows no marker for: none");
            Check(QuestPlaces.Step(null, 68142) is null && QuestPlaces.Step(points, 0) is null, "nothing read, or no quest: none");
            Check(QuestPlaces.Step([(2606u, 129u, float.NaN, 1f)], 68142) is null, "a point that is not a number: left out");
        }),
        ("Quest places 1710: the giver is placed only with a map, a territory and a finite position", () =>
        {
            Check(QuestPlaces.HasGiverPlace(Entry(1) with { IssuerMapId = 25, IssuerTerritoryId = 156, IssuerX = 120f, IssuerZ = -340f }), "Raulf in Mor Dhona");
            Check(!QuestPlaces.HasGiverPlace(Entry(1) with { IssuerMapId = 25 }) && !QuestPlaces.HasGiverPlace(Entry(1)), "no Level row: not placed");
            Check(!QuestPlaces.HasGiverPlace(Entry(1) with { IssuerMapId = 25, IssuerTerritoryId = 156, IssuerX = float.PositiveInfinity }), "a position that is not a number: not placed");
            var fresh = Entry(1);
            Check(fresh.IssuerName == "" && fresh.EndName == "" && fresh.Jobs == "" && fresh.Society == "" && fresh.Company == "", "unknown is empty, never invented");
        }),
        ("Quest places 1710: the « Quests-details » picture: two trial quests, the shown one under way with every line, planned once before the Settings tabs", () =>
        {
            var trial = QuestTrial.Entries();
            var shown = trial.Single(entry => entry.Id == QuestTrial.Shown);
            Check(shown.Progress == QuestProgress.Active && QuestPlaces.HasGiverPlace(shown) && shown.IssuerName.Length > 0 && shown.EndName.Length > 0 && shown.Jobs.Length > 0 && shown.Society.Length > 0,
                "the shown quest has every line of the details");
            Check(shown.PreviousQuestIds.All(id => trial.Any(entry => entry.Id == id && entry.Progress == QuestProgress.Complete)), "its quest before is in the trial, done");
            Check(QuestTrial.Character != 0, "a character, so that « under way » is kept (no character: unconfirmed)");
            var plan = TenshoAssistant.Desktop.ReferencePlan.Plan(TenshoAssistant.Desktop.DesktopPages.AsIs.Concat(TenshoAssistant.Desktop.DesktopPages.NeedsLink));
            Check(plan.Count(shot => shot.Id == "Quests-details" && shot.Page == "Quests" && shot.Setup == "quest-trial") == 1, "planned once");
            var references = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant.Desktop/References.cs")).ReplaceLineEndings("\n");
            Check(references.Contains("Headless.QuestTrial(host,true)", StringComparison.Ordinal) && references.Contains("if(shot.Setup==\"quest-trial\")Headless.QuestTrial(host,false);", StringComparison.Ordinal), "given, then taken back");
            Check(Read("MainWindow.DesktopData.cs").Contains("if(!inDesktopApp)return;\n        questNavigation.Clear();", StringComparison.Ordinal), "never in the game");
        }),
        ("Quest places 1710: wiring: the reader copies the sheets' giver, end NPC, jobs, society and company; the page shows them and both map buttons", () =>
        {
            var reader = Read("Quests/QuestReader.Dalamud.cs");
            foreach (var part in new[] { "IssuerName = Resident(row.IssuerStart.RowId),", "EndName = Resident(row.TargetEnd.RowId),", "row.IssuerLocation.Value.X", "row.IssuerLocation.Value.Z",
                "row.ClassJobCategory0.Value.Name.ExtractText()", "row.BeastTribe.Value.Name.ExtractText()", "row.GrandCompany.Value.Name.ExtractText()", "QuestCacheShape=\"|quest-giver-1710\"" })
                Check(reader.Contains(part, StringComparison.Ordinal), "reader: " + part);
            var page = Read("MainWindow.Quests.cs");
            foreach (var part in new[] { "DrawQuestGiver(entry);", "\"QuestDetails.End\"", "\"QuestDetails.Jobs\"", "\"QuestDetails.Society\"", "\"QuestDetails.Company\"",
                "if (entry.Progress == QuestProgress.Active) DrawQuestStep(entry);", "ShowOnTenshoMap(issuerMap, world);", "QuestPlaces.Step(points, entry.Id)", "ShowOnTenshoMap(map, world);" })
                Check(page.Contains(part, StringComparison.Ordinal), "page: " + part);
            Check(!page.Contains("ShowMapOnTenshoMap(issuerMap.MapId)", StringComparison.Ordinal), "the giver is always placed from the sheet, no longer the whole map");
            Check(Read("MainWindow.SelfCheckPages.cs").Contains("Page(\"quest givers\"", StringComparison.Ordinal), "the self-check counts them");
            var en = Read("Localization/Languages/en.json"); var fr = Read("Localization/Languages/fr.json");
            foreach (var key in new[] { "Giver", "GiverName", "GiverPlace", "End", "Jobs", "Society", "Company", "CurrentStep", "StepMap", "StepMapTip", "StepNone" })
                Check(en.Contains($"    \"{key}\": ", StringComparison.Ordinal) && fr.Contains($"    \"{key}\": ", StringComparison.Ordinal), "text: " + key);
        }),
    ];
}
