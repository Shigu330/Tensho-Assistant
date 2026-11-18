using TenshoAssistant.Beastmaster;

// 0.1549 (X-580, the user's request of 18/11 around 00:20, capture of « The 10 to bring » in 0.1545: « it should also be in the order of the
// bestiary's numbers, to make selecting easier »). The bestiary's number is the beast's XBMPet row (docs/evidence/0.1548-beast-book.md).
internal static class BeastBookOrder1549Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast book order 1549: the capture's ten (Salamandre, Écureuil, Piège-volant, Opo-opo, Mante, Gespenst, Vautour, Cu sith, Mandragore, Pampa) by bestiary number", () =>
        {
            uint[] capture = [41, 2, 20, 5, 16, 13, 11, 1, 12, 22];
            var ordered = BeastTeams.BookOrder(capture.Select(id => new BeastPick(id, 5, true))).Select(pick => pick.Beast).ToArray();
            Check(ordered.SequenceEqual(new uint[] { 1, 2, 5, 11, 12, 13, 16, 20, 22, 41 }), "Cu sith, Écureuil, Opo-opo, Vautour, Mandragore, Gespenst, Mante, Piège-volant, Pampa, Salamandre: " + string.Join(",", ordered));
        }),
        ("Beast book order 1549: the plan's ten are shown by number with « N° », each fight's three keep their calling order", () =>
        {
            var board = BeastPlan1543Tests.FirstBoard();
            var beasts = BeastPlan1543Tests.Held.Select(BeastPlan1543Tests.Make).ToArray();
            var plan = BeastTeams.Plan(board, beasts, BeastPlan1543Tests.Held.Select(row => row.Id).ToHashSet(), null);
            Check(BeastTeams.BookOrder(plan.Team).Select(pick => pick.Beast).SequenceEqual(plan.Team.Select(pick => pick.Beast).Order()), "the ten by number");
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("foreach (var pick in BeastTeams.BookOrder(plan.Team))") && page.Contains("localization.Format(\"BeastUi.BookNumber\", beast.Id, beast.Name)"), "the panel's ten");
            Check(page.Contains("DrawBeastCallOrder(fight, battle.Team.Select(pick => pick.Beast).ToArray(), catalog);"), "the trios in calling order");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains(language == "en" ? "\"BookNumber\": \"No. {0} {1}\"" : "\"BookNumber\": \"N° {0} {1}\""), language);
            }
        }),
    ];
}
