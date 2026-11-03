using TenshoAssistant.Collections;

// 0.365 (g, C-04): eight more collection kinds, read apart from the cached eight, shown as Collections tabs in the user's order.
internal static class ExtraCollections365Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Extra collections: kinds appended after Achievement, read with IUnlockState, shown in the user's order",() =>
        {
            Check((int)CollectionKind.Achievement == 7 && (int)CollectionKind.Hairstyle == 8 && (int)CollectionKind.OccultLore == 15,"cached kinds keep their numbers");
            var reader = Read("Collections/ExtraCollectionReader.Dalamud.cs");
            foreach (var call in new[]{"IsCharaMakeCustomizeUnlocked","IsOrnamentUnlocked","IsGlassesUnlocked","IsBuddyEquipUnlocked","IsBannerFrameUnlocked",
                "IsBannerBgUnlocked","IsBannerDecorationUnlocked","IsSecretRecipeBookUnlocked","IsMcGuffinUnlocked","IsMKDLoreUnlocked"})
                Check(reader.Contains("unlocks."+call+"(",StringComparison.Ordinal),call);
            Check(reader.Contains("framework.IsInFrameworkUpdateThread",StringComparison.Ordinal) && reader.Contains("TimeSpan.FromMilliseconds(2)",StringComparison.Ordinal),"framework thread, 2 ms");
            var page = Read("MainWindow.Collections.cs");
            Check(page.Contains("[CollectionKind.Hairstyle,CollectionKind.Ornament,CollectionKind.Glasses,CollectionKind.Barding,",StringComparison.Ordinal)
                && page.Contains("(ExtraKinds.Contains(kind) ? extra ?? [] : state.Entries)",StringComparison.Ordinal),"tabs in order, rows from the extra reader");
            Check(Read("Plugin.cs").Contains("()=>extraCollections?.Dispose()",StringComparison.Ordinal),"reader disposed");
        }),
    ];
}
