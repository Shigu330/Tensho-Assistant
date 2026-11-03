// 0.387 (user report: the blue magic spell window never gives the monster): the English spell name comes from the English Action
// sheet by row (the AozAction → Action link followed the client's language), old cached answers are not read again, and a missing
// wiki page is said as such. The real Bad Breath page (docs/evidence/0.387-blue-spell-name.md) parses into its monsters.
internal static class BlueSpellName387Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Blue spell wiki: English Action sheet by row, new cache family, missing page said, a real table parsed",() =>
        {
            var plugin = Read("Plugin.cs");
            Check(plugin.Contains("GetExcelSheet<Lumina.Excel.Sheets.Action>(Dalamud.Game.ClientLanguage.English).GetRowOrDefault(aoz.Action.RowId)",StringComparison.Ordinal)
                && !plugin.Contains("?.Action.ValueNullable?.Name.ExtractText()",StringComparison.Ordinal),"English name by row");
            Check(plugin.Contains("family:\"wiki-blue-spells-2\"",StringComparison.Ordinal),"old empty answers dropped");
            Check(Read("MainWindow.BlueSpellSources.cs").Contains("document.Revision==0",StringComparison.Ordinal),"missing page said");
            var html = "<table class=\\\"item sortable stdt__table_v2\\\"><tr><th>Enemy</th><th>Level</th><th>Location</th></tr>"
                + "<tr><td>Capricious Cassie</td><td>43</td><td>East Shroud (23.1,20.4)</td></tr></table>";
            var json = "{\"parse\":{\"title\":\"Bad Breath\",\"revid\":1537705,\"text\":{\"*\":\"" + html + "\"}}}";
            var document = TenshoAssistant.Monsters.WikiMonsterParser.Parse(json,28,"Bad Breath",DateTimeOffset.UtcNow);
            Check(document.Monsters.Count==1 && document.Monsters[0].Name=="Capricious Cassie","parsed");
            var missing = TenshoAssistant.Monsters.WikiMonsterParser.Parse("{\"error\":{\"code\":\"missingtitle\"}}",28,"Mauvaise haleine",DateTimeOffset.UtcNow);
            Check(missing.Revision==0 && missing.Monsters.Count==0,"missing page = revision 0");
        }),
    ];
}
