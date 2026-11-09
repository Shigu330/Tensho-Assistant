// Test audit of 2026-11-09, batch 2 (0.1021): Wishlist360Tests, OutfitLists989Tests merged into WishlistTests; every case is kept.
using TenshoAssistant.Items;
internal static class WishlistTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From Wishlist360Tests: 0.360 (g, C-09): the wishlist — toggle in order, bounded, kept on « reset settings », in every item menu and as a Collections tab.
        ("Wishlist: toggle adds at the end and removes, refuses when full, never keeps duplicates or zero",() =>
        {
            var list = Wishlist.Toggle(null,5)!;
            list = Wishlist.Toggle(list,7)!;
            Check(list.SequenceEqual([5u,7u]) && Wishlist.Toggle(list,5)!.SequenceEqual([7u]),"added in order, removed");
            Check(Wishlist.Toggle([3,3,0],0)!.SequenceEqual([3u]),"duplicates and zero dropped");
            var full = Enumerable.Range(1,Wishlist.MaximumItems).Select(id => (uint)id).ToList();
            Check(Wishlist.Toggle(full,9999) is null && Wishlist.Toggle(full,1)!.Count == Wishlist.MaximumItems - 1,"full: refused, removal still works");
            Check(TenshoAssistant.ConfigurationReset.Kept.Contains("Wishlist"),"kept on reset");
            Check(Read("MainWindow.DutyFinder.cs").Contains("DrawWishlistToggle(item);",StringComparison.Ordinal)
                && Read("MainWindow.Collections.cs").Contains("if(collectionWishlistTab){DrawWishlist();return;}",StringComparison.Ordinal),"menu entry and Collections tab");
        }),
        // From OutfitLists989Tests: X-104 (0.989): an outfit's missing pieces into the wishlist or a crafting list, in one click.
        ("Outfit lists 989: added at the end once each, refused when over the limit",() =>
        {
            Check(Wishlist.AddAll([5,7],[7,9,9,0]) is { } next && next.SequenceEqual([5u,7u,9u]),"added once");
            Check(Wishlist.AddAll(Enumerable.Range(1,Wishlist.MaximumItems).Select(id=>(uint)id).ToList(),[9999]) is null,"full");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Outfits.cs"));
            Check(page.Contains("DrawOutfitMissingActions(row);") && page.Contains("recipeCatalog.State.Catalog?.ForResult(piece) is { Count:>0 }"),"buttons");
        }),
    ];
}
