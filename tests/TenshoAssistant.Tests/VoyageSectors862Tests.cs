// 0.862 (X-73.8): submersible loot by sector and sector unlocks, once per voyage.
internal static class VoyageSectors862Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Voyage sectors: once per voyage, by the entry's own sector",() =>
        {
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/FreeCompany/VoyageWatcher.Dalamud.cs"));
            Check(watcher.Contains("voyagesNoted.Add((houseId,slot,sub.RegisterTime))"),"once per voyage");
            Check(watcher.Contains("SectorLoot?.Invoke(found.ItemIdPrimary%1_000_000,found.Point,found.ItemCountPrimary);") && watcher.Contains("SectorUnlock?.Invoke(found.UnlockedPoint,found.Point);"),"by sector");
            Check(Enum.IsDefined(TenshoAssistant.Discoveries.DiscoveryKind.VoyageDrop) && !TenshoAssistant.Discoveries.DiscoveryStore.HasPlace(TenshoAssistant.Discoveries.DiscoveryKind.VoyageDrop),"no place");
        }),
    ];
}
