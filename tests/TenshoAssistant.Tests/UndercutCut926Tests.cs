// DEC-038 (0.926): the market offers read during a listing stop in PvP and ranked content, as every other reader.
internal static class UndercutCut926Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Undercut 926: offers not read while the competitive cut is on",() =>
        {
            var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Market/UndercutWatcher.Dalamud.cs"));
            var handler=code.IndexOf("private void OnOfferings(",StringComparison.Ordinal);
            Check(handler>0 && code.IndexOf("if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;",handler,StringComparison.Ordinal) is var cut && cut>handler && cut<code.IndexOf("try",handler,StringComparison.Ordinal),"cut first");
        }),
    ];
}
