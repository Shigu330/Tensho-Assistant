namespace TenshoAssistant.Npcs;

/// <summary>
/// X-748 (0.1738, idea I-022 chosen by the user on 21/11, after test T-0.1374-1 of 17/11: about twenty rows « Vendor · Position not
/// available » for one item): the vendors of an item that stand on a map first, in their order; those on no map (the game gives them no
/// place: furnishings to place at home, vendors no longer reachable) apart, shown in one row that unfolds. Pure (tests).
/// </summary>
public static class NpcVendorRows
{
    /// <summary>The offers split by <paramref name="placed"/> (a vendor with at least one place), each part in the order given.</summary>
    public static (IReadOnlyList<T> Placed,IReadOnlyList<T> Unplaced) Split<T>(IReadOnlyList<T> offers,Func<T,bool> placed)
    {
        var on=new List<T>();var off=new List<T>();
        foreach(var offer in offers)(placed(offer) ? on:off).Add(offer);
        return (on,off);
    }

    /// <summary>X-757 M2 (0.1740, review of 0.1738): how many vendors (distinct NPCs) the offers name: a vendor of two shops counts once, as
    /// the page's header counts them.</summary>
    public static int Vendors<T>(IEnumerable<T> offers,Func<T,uint> npc)=>offers.Select(npc).Distinct().Count();

    /// <summary>The rows the table shows: the placed vendors, then one row for the others when there are any, then those when unfolded.</summary>
    public static int Rows(int placed,int unplaced,bool open)=>placed+(unplaced>0 ? 1+(open ? unplaced:0):0);
}
