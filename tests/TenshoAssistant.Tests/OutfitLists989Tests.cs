using TenshoAssistant.Items;

// X-104 (0.989): an outfit's missing pieces into the wishlist or a crafting list, in one click.
internal static class OutfitLists989Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Outfit lists 989: added at the end once each, refused when over the limit",() =>
        {
            Check(Wishlist.AddAll([5,7],[7,9,9,0]) is { } next && next.SequenceEqual([5u,7u,9u]),"added once");
            Check(Wishlist.AddAll(Enumerable.Range(1,Wishlist.MaximumItems).Select(id=>(uint)id).ToList(),[9999]) is null,"full");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Outfits.cs"));
            Check(page.Contains("DrawOutfitMissingActions(row);") && page.Contains("recipeCatalog.State.Catalog?.ForResult(piece) is { Count:>0 }"),"buttons");
        }),
    ];
}
