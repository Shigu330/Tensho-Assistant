// 0.1236 (remark of X-260): the English page titles « Allied Societies » and « Custom Deliveries » take the game's capitals (Addon 5700
// « Custom Deliveries », Addon 17052 « Allied Society », docs/evidence/0.1236-page-titles.md); no title value keeps the lower case.
internal static class GameTitles1236Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Game titles 1236: Allied Societies and Custom Deliveries with the game's capitals",() =>
        {
            var english=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            Check(!english.Contains("\": \"Allied societies\"") && !english.Contains("\": \"Custom deliveries\""),"no lower-case title left");
            Check(english.Contains("\": \"Allied Societies\"") && english.Contains("\": \"Custom Deliveries\""),"titles with capitals");
            Check(File.Exists(Path.GetFullPath("docs/evidence/0.1236-page-titles.md")),"evidence");
        }),
    ];
}
