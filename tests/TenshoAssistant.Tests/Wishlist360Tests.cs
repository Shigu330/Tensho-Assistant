using TenshoAssistant.Items;

// 0.360 (g, C-09): the wishlist — toggle in order, bounded, kept on « reset settings », in every item menu and as a Collections tab.
internal static class Wishlist360Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Wishlist: toggle adds at the end and removes, refuses when full, never keeps duplicates or zero",() =>
        {
            var list = Wishlist.Toggle(null,5)!;
            list = Wishlist.Toggle(list,7)!;
            Check(list.SequenceEqual([5u,7u]) && Wishlist.Toggle(list,5)!.SequenceEqual([7u]),"added in order, removed");
            Check(Wishlist.Toggle([3,3,0],0)!.SequenceEqual([3u]),"duplicates and zero dropped");
            var full = Enumerable.Range(1,Wishlist.MaximumItems).Select(id => (uint)id).ToList();
            Check(Wishlist.Toggle(full,9999) is null && Wishlist.Toggle(full,1)!.Count == Wishlist.MaximumItems - 1,"full: refused, removal still works");
            Check(Read("Configuration.cs").Contains("\"Wishlist\",",StringComparison.Ordinal),"kept on reset");
            Check(Read("MainWindow.DutyFinder.cs").Contains("DrawWishlistToggle(item);",StringComparison.Ordinal)
                && Read("MainWindow.Collections.cs").Contains("if(collectionWishlistTab){DrawWishlist();return;}",StringComparison.Ordinal),"menu entry and Collections tab");
        }),
    ];
}
