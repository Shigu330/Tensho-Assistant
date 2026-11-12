using TenshoAssistant.Fields;

// 0.1176 (X-188, user's request of 12/11 on the Occult Crescent, docs/evidence/0.1176-occult-jobs.md): the phantom jobs: each job's level from the
// game's state (by job index), what it learns next and at which level (MKDSupportJob actions, MKDTrait traits), the worn job first, its experience.
internal static class OccultJobs1176Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 12, 13, 57, 33, TimeSpan.Zero);
    // As the sheets give them (French client, 2026.11.03): the knight's actions at 1, 2, 3, 6 and its trait at 4; the freelancer's at 5, 10, 15, 20.
    private static readonly OccultJob Freelancer = new(0, "Freelance fantôme", 24, [new(5, "A5", "", 0, false), new(10, "A10", "", 0, false), new(15, "A15", "", 0, false), new(20, "A20", "", 0, false)]);
    private static readonly OccultJob Knight = new(1, "Paladin fantôme", 6, [new(1, "A1", "", 0, false), new(2, "A2", "", 0, false), new(3, "A3", "", 0, false), new(4, "Défense améliorée", "Augmente l'effet…", 0, true), new(6, "A6", "", 0, false)]);
    private static readonly OccultJob Berserker = new(2, "Berserker fantôme", 3, [new(1, "B1", "", 0, false), new(2, "B2", "", 0, false), new(2, "Fureur améliorée", "", 0, true)]);
    private static readonly OccultJob Monk = new(3, "Moine fantôme", 6, [new(1, "M1", "", 0, false)]);
    // The reading of 12/11 14:57:33: job 1 worn, levels 1 2 0 1 0 0 1 …
    private static OccultState State(int current = 1, uint needed = 2400) => new(At, 465, 2800, current, [1, 2, 0, 1, 0, 0, 1, .. new int[17]], [0, 900, 0, 0, .. new uint[20]], needed, 270, 12);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Occult jobs 1176: each job's level by its index, what it learns next above that level, nothing at its last level", () =>
        {
            var state = State();
            Check(OccultCrescent.LevelOf(state, 1) == 2 && OccultCrescent.LevelOf(state, 0) == 1 && OccultCrescent.LevelOf(state, 2) == 0, "levels by index");
            Check(OccultCrescent.LevelOf(state, 99) == 0, "an index past the state: 0, not a crash");
            Check(OccultCrescent.Next(Knight, 2)?.Name == "A3", "level 2: the action of level 3");
            Check(OccultCrescent.Next(Knight, 3) is { Trait: true, Level: 4 }, "level 3: the trait of level 4");
            Check(OccultCrescent.Next(Knight, 6) is null, "the last level: nothing more");
            Check(OccultCrescent.Next(Berserker, 1) is { Name: "B2", Trait: false }, "an action before a trait of the same level");
            Check(OccultCrescent.Next(Freelancer, 1)?.Level == 5, "the freelancer's next at 5");
            Check(OccultCrescent.Next(Monk, 0)?.Name == "M1", "a job never worn: its first action");
        }),
        ("Occult jobs 1176: the worn job first, then the jobs with levels, then the others in the game's order; the first zero reading is not a state", () =>
        {
            var ordered = OccultCrescent.Ordered([Freelancer, Knight, Berserker, Monk], State());
            Check(ordered.Select(job => job.Id).SequenceEqual([1u, 0u, 3u, 2u]), "worn, levelled, others: " + string.Join(",", ordered.Select(job => job.Id)));
            Check(OccultCrescent.Ordered([Freelancer, Knight, Berserker, Monk], State(current: 3)).First().Id == 3, "another job worn");
            Check(Math.Abs(OccultCrescent.Share(465, 2800) - 465f / 2800) < 1e-6 && OccultCrescent.Share(5, 0) == 0 && OccultCrescent.Share(9, 3) == 1, "knowledge share bounded");
            Check(OccultCrescent.Empty(new(At, 0, 0, 0, new int[24], new uint[24], 0, 0, 0)), "the zeros of the first frame");
            Check(!OccultCrescent.Empty(State()), "a real reading");
        }),
        ("Occult jobs 1176: wiring: read once a second from the island's director, the jobs from the sheets, the tab under the island's name, the game's words", () =>
        {
            string Read(string path) => File.ReadAllText(Path.GetFullPath(path));
            var reader = Read("src/TenshoAssistant/Fields/OccultCrescentReader.Dalamud.cs");
            Check(reader.Contains("GetPublicContentDirectorByType(PublicContentDirectorType.OccultCrescent)", StringComparison.Ordinal) && reader.Contains("nextRead=now+1_000;", StringComparison.Ordinal)
                && reader.Contains("if(OccultCrescent.Empty(read))return;", StringComparison.Ordinal) && reader.Contains("nextRead=now+60_000;", StringComparison.Ordinal), "reader: director, once a second, zeros skipped, failure said");
            var loader = Read("src/TenshoAssistant/Fields/FieldBoard.Dalamud.cs");
            Check(loader.Contains("GetExcelSheet<MKDSupportJob>()", StringComparison.Ordinal) && loader.Contains("GetExcelSheet<MKDTrait>()", StringComparison.Ordinal)
                && loader.Contains("new(Label(16607),Label(16611),Label(16608),Label(16618))", StringComparison.Ordinal), "sheets and Addon words");
            var page = Read("src/TenshoAssistant/MainWindow.FieldContent.cs");
            Check(page.Contains("tabs.Add((\"Occult\",island.Name,()=>DrawFieldOccult(catalog.OccultJobs,labels)));", StringComparison.Ordinal), "the tab under the island's name");
            Check(Read("src/TenshoAssistant/Plugin.cs").Contains("mainWindow.OccultStateRead=()=>occultReader?.State;", StringComparison.Ordinal), "the state given to the page");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.1176-occult-jobs.md")), "evidence");
        }),
    ];
}
