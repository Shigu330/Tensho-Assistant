// 0.1089 (audit A084, D-138): the translation guide lives next to the language files, src/TenshoAssistant/Localization/README.md;
// the root folder Localization/ is gone and every pointer (FAQ answer in both languages, form, README, docs map) names the new path.
internal static class LocalizationGuide1089Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private const string Guide = "src/TenshoAssistant/Localization/README.md";

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Translation guide: next to the language files, every pointer follows",() =>
        {
            Check(File.Exists(Path.GetFullPath(Guide)),"the guide is next to the language files");
            Check(!Directory.Exists(Path.GetFullPath("Localization")),"the root folder Localization/ is gone");
            foreach (var path in new[] { "src/TenshoAssistant/Localization/Languages/en.json","src/TenshoAssistant/Localization/Languages/fr.json",
                                         ".github/ISSUE_TEMPLATE/translation.yml","README.md","docs/README.md","docs/FAQ.md" })
            {
                var text = File.ReadAllText(Path.GetFullPath(path));
                Check(text.Contains(Guide),path + " names " + Guide);
                foreach (var old in new[] { "](Localization/README.md)","../Localization/README.md","Devloppement/Localization/README.md",
                                            "├── Localization/"," Localization/README.md on GitHub"," Localization/README.md sur GitHub" })
                    Check(!text.Contains(old),path + " no longer names the old path (" + old + ")");
            }
        }),
    ];
}
