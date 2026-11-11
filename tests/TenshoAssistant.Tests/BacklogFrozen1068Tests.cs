// 0.1068 (audit A048): BACKLOG says in both halves that it is a frozen state, and its lines that the code has since contradicted
// (ocean fishing, R-09 events) and its pointers to missing sections are corrected.
internal static class BacklogFrozen1068Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Backlog: frozen banner and corrected lines in both halves",() =>
        {
            var text = File.ReadAllText(Path.GetFullPath("docs/project/BACKLOG.md")).Replace("\r\n","\n");
            Check(text.Contains("**Frozen state, no longer updated:**") && text.Contains("**État figé, plus mis à jour :**"),"frozen banner in both halves");
            foreach(var stale in new[]{"Ocean fishing not covered","Pêche en mer non couverte","R-09 (events): not started","R-09 (événements) : pas commencé","\"Tests to run in game\"","« Tests à faire en jeu »"})
                Check(!text.Contains(stale),"stale line left: " + stale);
            Check(text.Contains("R-09 (events): done in 0.342 (Events page)") && text.Contains("R-09 (événements) : fait en 0.342 (page Événements)"),"R-09 corrected in both halves");
        }),
    ];
}
