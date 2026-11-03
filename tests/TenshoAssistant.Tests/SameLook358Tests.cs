using TenshoAssistant.Inventory;

// 0.358 (g, C-07): glamour dresser items that look the same, by model and slot; dyes are not part of the look.
internal static class SameLook358Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Same look: same models and slot grouped, dyes ignored, no model or slot left out, largest first",() =>
        {
            var looks = new Dictionary<uint,ItemLook>
            {
                [1]=new(100,0,4,0),[2]=new(100,0,4,2),[3]=new(100,0,5,0),[4]=new(200,0,4,0),[5]=new(200,0,4,0),[6]=new(200,0,4,1),[7]=new(0,0,4,0),[8]=new(0,0,4,0),
            };
            var groups = StorageReport.SameLook([1,2,3,4,5,6,7,8,1],id => looks.TryGetValue(id,out var look) ? look : null);
            Check(groups.Count == 2 && groups[0].SequenceEqual([4u,5u,6u]) && groups[1].SequenceEqual([1u,2u]),"groups by models and slot");
            var page = Read("MainWindow.StorageReport.cs");
            Check(page.Contains("Where(stack=>stack.Slot<StorageReport.DresserSlots).Select(stack=>stack.Item.Id.Value),lookOf)",StringComparison.Ordinal)
                && !page.Contains("ItemLookOf?.Invoke(id)",StringComparison.Ordinal),"dresser slots only, no lookups in Draw");
        }),
    ];
}
