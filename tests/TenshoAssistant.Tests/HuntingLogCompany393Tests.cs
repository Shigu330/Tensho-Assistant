using TenshoAssistant.Hunting;

// 0.393 (user reports): the hunting log shows only the character's own Grand Company log; French name « Bestiaire » (not « chasse »).
internal static class HuntingLogCompany393Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Hunting log: own Grand Company only, none without one, all while unknown; class logs always; French name Bestiaire",() =>
        {
            Check(HuntingLog.Shown(100,1) && !HuntingLog.Shown(200,1) && !HuntingLog.Shown(300,1),"Maelstrom only");
            Check(!HuntingLog.Shown(100,0) && HuntingLog.Shown(1,0),"no company: class logs only");
            Check(HuntingLog.Shown(200,null) && HuntingLog.Shown(26,3),"unknown: all; class logs always");
            var french = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/fr.json"));
            Check(french.Contains("\"HuntingLog\": \"Bestiaire\"",StringComparison.Ordinal) && !french.Contains("Carnet de chasse",StringComparison.Ordinal),"Bestiaire");
        }),
    ];
}
