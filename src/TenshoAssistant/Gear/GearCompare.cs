namespace TenshoAssistant.Gear;

/// <summary>A hovered piece against one worn piece of the same slot: item level difference and stat differences (base values, no materia).</summary>
public sealed record GearComparison(GearItem Worn,bool WornHq,int ItemLevelDelta,IReadOnlyList<(int Param,int Delta)> Deltas);

/// <summary>
/// g, M-07 (0.377): the comparison shown under the game's item tooltip (ideas from SimpleCompare, MIT, read only; original code). Base
/// values of the NQ or HQ version on both sides: the hovered item's materia are not known, so the worn piece's are left out too.
/// Rings are compared with both worn rings. Nothing when the item is not gear, nothing is worn in its slot, or it is the worn piece.
/// </summary>
public static class GearCompare
{
    /// <summary>X-294 (0.1295): a piece's base stats (NQ or HQ values, no materia) in the stats' order, zeros left out: the item tooltip's first block.</summary>
    public static IReadOnlyList<(int Param,int Value)> BaseStats(GearItem piece,bool hq,IReadOnlyList<int> order)
    {
        var values=piece.ParamsFor(hq && piece.CanBeHq);
        return order.Where(param=>values.GetValueOrDefault(param)!=0).Select(param=>(param,values[param])).ToArray();
    }

    public static IReadOnlyList<GearComparison> Compare(GearItem candidate,bool hq,IReadOnlyList<EquippedPiece> worn,Func<uint,GearItem?> find,IReadOnlyList<int> order)
    {
        var slots=candidate.Slot is GearSlot.RingRight or GearSlot.RingLeft ? new[]{GearSlot.RingRight,GearSlot.RingLeft}:[candidate.Slot];
        var values=candidate.ParamsFor(hq && candidate.CanBeHq);
        var result=new List<GearComparison>();
        foreach(var piece in worn.Where(piece=>slots.Contains(piece.Slot)))
        {
            if(piece.ItemId==candidate.Id && piece.Hq==(hq && candidate.CanBeHq) || find(piece.ItemId) is not { } item)continue;
            var reference=item.ParamsFor(piece.Hq);
            var deltas=order.Select(param=>(Param:param,Delta:values.GetValueOrDefault(param)-reference.GetValueOrDefault(param)))
                .Where(entry=>entry.Delta!=0).ToArray();
            result.Add(new(item,piece.Hq,candidate.ItemLevel-item.ItemLevel,deltas));
        }
        return result;
    }
}
