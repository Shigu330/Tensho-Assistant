using TenshoAssistant.Items;

namespace TenshoAssistant.Market;

public enum SaleSource { Board, Universalis }
/// <summary>X-695 (0.1667): an item's market price per unit, where it was seen and when.</summary>
public sealed record SalePrice(long Unit,SaleSource Source,DateTimeOffset? At);
/// <summary>X-695 (0.1667): one line of the sale list: the stack (quantities of the same item and quality added), its market price (null:
/// unknown, never 0), the estimated total after the market tax, and what an NPC pays for it (null: not known, an HQ item).</summary>
public sealed record SaleRow(uint Item,bool Hq,long Quantity,SalePrice? Price,long? Total,long? Npc)
{
    /// <summary>An NPC pays at least as much as the market would (after tax).</summary>
    public bool NpcBetter=>Total is { } total && Npc is { } npc && npc>=total;
}
/// <summary>X-695 (0.1667): the list, the number of items that can be sold at the market without a known price, and the items to ask
/// Universalis about (marketable, at most <see cref="RetainerSaleList.Asked"/>). X-698 M2 (0.1671): the ones to ask first come first.</summary>
public sealed record SaleList(IReadOnlyList<SaleRow> Rows,int Unknown,IReadOnlyList<uint> ToAsk);

/// <summary>
/// X-695 (0.1667, the user's request of 20/11 around 23:00): a retainer's 20 most profitable items to sell, from its inventory as Tensho
/// recorded it when the retainer was opened. Profit = the market's price (the lowest offer of that quality seen at the game's board, or given
/// by Universalis on the player's click, D-82; the newer one) × quantity, minus the 5 % market tax the game's help states for a retainer's
/// sales (HowToPage 830, docs/evidence/0.1667-retainer-sale-list.md), set beside the NPC's price (Item.PriceLow × quantity, for normal
/// quality only). Items the market does not take (untradable, or no market category: ItemDefinition.IsMarketable) are left out; a price
/// not known is said unknown, never 0 (R06). Display only: nothing is put up for sale (R03.3, R04). Pure (tests).
/// </summary>
public static class RetainerSaleList
{
    public const int Shown=20;
    public const int TaxPercent=5;
    /// <summary>Universalis takes up to 100 items in one request.</summary>
    public const int Asked=100;
    /// <summary>The InventoryType pages of a retainer (10000 to 10006); its money and crystals are kept apart.</summary>
    public static bool IsRetainerPage(int container)=>container is >=10000 and <=10006;

    /// <summary>The market total after the tax (rounded down, as the gil a sale brings). X-698 M4 (0.1671): the city's rate when the game
    /// sent it (MarketTax.RateFor), the help's 5 % otherwise.</summary>
    public static long Net(long unit,long quantity,int taxPercent=TaxPercent)=>unit*quantity*(100-Math.Clamp(taxPercent,0,100))/100;

    /// <summary>X-719 (0.1726, the user's request of 21/11): the price the « Copy » button copies: the lowest seen minus 1 gil, never under 1
    /// (as the undercut's price to paste, Undercut.Price).</summary>
    public static long Paste(long lowest)=>Math.Max(1,lowest-1);

    /// <summary>X-719 (0.1726): when a price was seen, short for the table (the time today, the date another day; the whole time on hover).</summary>
    public static string Short(DateTimeOffset seen,DateTimeOffset now)=>seen.Date==now.Date ? seen.ToString("t",System.Globalization.CultureInfo.CurrentCulture):seen.ToString("d",System.Globalization.CultureInfo.CurrentCulture);

    /// <summary>The newer of two prices (either may be missing; the same age: the board, seen by the player).</summary>
    public static SalePrice? Newest(SalePrice? board,SalePrice? universalis)
        =>board is null ? universalis:universalis is null ? board:(universalis.At ?? DateTimeOffset.MinValue)>(board.At ?? DateTimeOffset.MinValue) ? universalis:board;

    /// <summary>X-698 M2 (0.1671): <paramref name="answered"/> says which items Universalis already answered (the sale list's own service keeps
    /// them): the items to ask are, in order, those not answered yet, those without any known price, then the most valuable (best total, then
    /// the largest quantity); 100 at most, so that a retainer with more distinct items is covered click after click.</summary>
    public static SaleList Build(IEnumerable<(uint Item,bool Hq,long Quantity)> stacks,Func<uint,ItemDefinition?> find,Func<uint,bool,SalePrice?> price,
        int taxPercent=TaxPercent,Func<uint,bool>? answered=null)
    {
        var merged=new Dictionary<(uint,bool),long>();
        foreach(var (item,hq,quantity) in stacks)
            if(item!=0 && quantity>0)merged[(item,hq)]=merged.GetValueOrDefault((item,hq))+quantity;
        var rows=new List<SaleRow>();
        var unknown=0;
        var candidates=new Dictionary<uint,(bool Known,long Best,long Quantity)>();
        foreach(var ((item,hq),quantity) in merged.OrderBy(pair=>pair.Key.Item1).ThenBy(pair=>pair.Key.Item2))
        {
            if(find(item) is not { IsMarketable:true } definition)continue;
            var npc=!hq && definition.SellPrice>0 ? definition.SellPrice*quantity:(long?)null;
            var seen=candidates.GetValueOrDefault(item);
            if(price(item,hq) is not { Unit:>0 } known){unknown++;candidates[item]=(seen.Known,seen.Best,seen.Quantity+quantity);continue;}
            var total=Net(known.Unit,quantity,taxPercent);
            candidates[item]=(true,Math.Max(seen.Best,total),seen.Quantity+quantity);
            rows.Add(new SaleRow(item,hq,quantity,known,total,npc));
        }
        var ask=candidates.OrderBy(pair=>answered?.Invoke(pair.Key)==true).ThenBy(pair=>pair.Value.Known).ThenByDescending(pair=>pair.Value.Best)
            .ThenByDescending(pair=>pair.Value.Quantity).ThenBy(pair=>pair.Key).Select(pair=>pair.Key).Take(Asked).ToList();
        return new SaleList(rows.OrderByDescending(row=>row.Total).ThenBy(row=>row.Item).Take(Shown).ToList(),unknown,ask);
    }
}
