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
/// Universalis about (marketable, at most <see cref="RetainerSaleList.Asked"/>).</summary>
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

    /// <summary>The market total after the tax (rounded down, as the gil a sale brings).</summary>
    public static long Net(long unit,long quantity)=>unit*quantity*(100-TaxPercent)/100;

    /// <summary>The newer of two prices (either may be missing; the same age: the board, seen by the player).</summary>
    public static SalePrice? Newest(SalePrice? board,SalePrice? universalis)
        =>board is null ? universalis:universalis is null ? board:(universalis.At ?? DateTimeOffset.MinValue)>(board.At ?? DateTimeOffset.MinValue) ? universalis:board;

    public static SaleList Build(IEnumerable<(uint Item,bool Hq,long Quantity)> stacks,Func<uint,ItemDefinition?> find,Func<uint,bool,SalePrice?> price)
    {
        var merged=new Dictionary<(uint,bool),long>();
        foreach(var (item,hq,quantity) in stacks)
            if(item!=0 && quantity>0)merged[(item,hq)]=merged.GetValueOrDefault((item,hq))+quantity;
        var rows=new List<SaleRow>();
        var unknown=0;
        var ask=new List<uint>();
        foreach(var ((item,hq),quantity) in merged.OrderBy(pair=>pair.Key.Item1).ThenBy(pair=>pair.Key.Item2))
        {
            if(find(item) is not { IsMarketable:true } definition)continue;
            if(!ask.Contains(item) && ask.Count<Asked)ask.Add(item);
            var npc=!hq && definition.SellPrice>0 ? definition.SellPrice*quantity:(long?)null;
            if(price(item,hq) is not { Unit:>0 } known){unknown++;continue;}
            rows.Add(new SaleRow(item,hq,quantity,known,Net(known.Unit,quantity),npc));
        }
        return new SaleList(rows.OrderByDescending(row=>row.Total).ThenBy(row=>row.Item).Take(Shown).ToList(),unknown,ask);
    }
}
