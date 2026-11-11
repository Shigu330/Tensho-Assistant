// 0.1071 (audit A061): the pending in-game readings live in one table, the reminders table of the global plan; the test route's
// step 0 points to it instead of listing them, and the table holds every reading the route used to list.
internal static class Readings1071Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Readings: one table in the global plan, the test route points to it",() =>
        {
            var route = File.ReadAllText(Path.GetFullPath("docs/project/TESTING-ROUTE.md")).Replace("\r\n","\n");
            var line = route.Split('\n').First(l => l.Contains("Relevés en attente, au fil du jeu"));
            Check(line.StartsWith("- [ ] Relevés en attente"),"the step 0 box keeps its start (q.py anchors on it)");
            Check(route.Contains("(GLOBAL-PLAN.md#rappels-de-relevés-en-jeu-par-moment)"),"the route links to the reminders table");
            Check(!route.Contains("coffres des cartes au trésor ; retour d'aéronef"),"the route no longer lists the readings");
            var plan = File.ReadAllText(Path.GetFullPath("docs/project/GLOBAL-PLAN.md")).Replace("\r\n","\n");
            Check(plan.Contains("This table is the one list of the pending in-game readings") && plan.Contains("Ce tableau est la seule liste des relevés en jeu en attente"),"the table says it is the one list, in both halves");
            var fr = plan[plan.IndexOf("<!-- gen:plan:reminders -->")..plan.IndexOf("<!-- /gen:plan:reminders -->")];
            var en = plan[plan.IndexOf("<!-- gen:plan:reminders-en -->")..plan.IndexOf("<!-- /gen:plan:reminders-en -->")];
            foreach (var word in new[]{"matéria","Submarine voyage check","X-108","Carte au trésor","aéronef","X-86","X-90","Item gain not attributed"})
                Check(fr.Contains(word),$"French reminders table has {word}");
            foreach (var word in new[]{"Materia","Submarine voyage check","X-108","Treasure map","airship","X-86","X-90","Item gain not attributed"})
                Check(en.Contains(word),$"English reminders table has {word}");
        }),
    ];
}
