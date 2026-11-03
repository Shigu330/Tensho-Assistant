namespace TenshoAssistant.Items;

/// <summary>
/// g, C-09 (0.360): the player's wishlist — item ids in the order they were added, at most <see cref="MaximumItems"/>. Saved like a setting;
/// the list is replaced whole on every change (read on the UI thread only). Original code (idea from GlamourWishlist, MIT, read only).
/// </summary>
public static class Wishlist
{
    public const int MaximumItems=500;

    /// <summary>The list with the item added (at the end) or removed; null when it is full; null list = empty.</summary>
    public static List<uint>? Toggle(IReadOnlyList<uint>? list,uint itemId)
    {
        var next=list?.Where(id=>id!=0).Distinct().ToList() ?? [];
        if(next.Remove(itemId))return next;
        if(itemId==0)return next;
        if(next.Count>=MaximumItems)return null;
        next.Add(itemId);
        return next;
    }
}
