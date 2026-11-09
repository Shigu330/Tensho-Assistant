// X-110 (0.984): where to tame a beast: its tames with a place, then its monster's places seen (the wiki's enemy too), with the place menu.
internal static class BeastPlaces984Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Beast places 984: tames first, then the monster's places, the menu on them, the tame kind still optional on place",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(page.Contains("store.Find(Discoveries.DiscoveryKind.BeastTamed, beast, 8)") && page.Contains("store.Find(Discoveries.DiscoveryKind.MonsterPlace, monster, 3)"),"sources");
            Check(page.Contains("NpcMenu($\"BeastPlaces{beast.Id}\", beast.Name, 0, places);"),"menu");
            Check(!TenshoAssistant.Discoveries.DiscoveryStore.HasPlace(TenshoAssistant.Discoveries.DiscoveryKind.BeastTamed),"tames without a monster stay valid");
        }),
    ];
}
