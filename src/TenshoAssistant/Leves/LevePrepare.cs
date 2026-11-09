namespace TenshoAssistant.Leves;

/// <summary>X-103 (0.988): what accepted crafting leves still need: per item, the quantities asked added up, minus what is held, items with a recipe only.</summary>
public static class LevePrepare
{
    public static IReadOnlyList<(uint Item,long Quantity)> Missing(IEnumerable<(uint Item,int? Quantity)> asked,Func<uint,long> held,Func<uint,bool> craftable)
        =>asked.Where(request=>request.Item!=0).GroupBy(request=>request.Item)
            .Select(group=>(Item:group.Key,Quantity:group.Sum(request=>(long)Math.Max(1,request.Quantity ?? 1))-Math.Max(0,held(group.Key))))
            .Where(row=>row.Quantity>0 && craftable(row.Item)).OrderBy(row=>row.Item).ToArray();
}
