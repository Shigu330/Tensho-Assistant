using TenshoAssistant.Maps;

// 0.401 (user report): a vista whose wiki coordinates carry a stray dot (« 25.2, 27.2. Z:1.0 », Sightseeing Log, 2026-11-03) is read,
// and a coordinate that is not a number skips its row instead of failing the whole page.
internal static class WikiVistas401Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Wiki vistas: stray dot read, odd row skipped, page kept",() =>
        {
            var page = "==Heavensward Vistas==\n"
                + "| 1 || [[A]] || [[Zone]] || {{coords|25.2, 27.2. Z:1.0}} || Scrute || note\n"
                + "| 2 || [[B]] || [[Zone]] || {{coords|x, y}} || Scrute || note\n"
                + "| 3 || [[C]] || [[Zone]] || {{coords|10.0, 11.5}} || Scrute || note\n";
            var document = WikiVistaParser.Parse(page,DateTimeOffset.UtcNow);
            Check(document.Rows.Count==2 && document.Rows[0].X==25.2f && document.Rows[0].Y==27.2f && document.Rows[1].Number==3,"stray dot read, bad row skipped");
        }),
    ];
}
