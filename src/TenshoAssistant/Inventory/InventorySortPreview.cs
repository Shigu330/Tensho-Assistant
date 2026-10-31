namespace TenshoAssistant.Inventory;

// Values are persisted as integers in profiles and templates: append only.
public enum InventorySortField { Name, Category, Quantity, Quality, ItemLevel, EquipLevel, Rarity, SellPrice, ItemId }
public sealed record InventoryPreviewStack(InventoryLocation Origin, InventoryStack Stack);
public sealed record InventorySortResult(int Capacity, IReadOnlyList<InventoryPreviewStack> Stacks,IReadOnlyList<InventoryPreviewStack?>? Slots=null);

public static class InventorySortPreview
{
    public const int MaximumCriteria=6;
    public static InventorySortResult Build(IEnumerable<InventoryContainerSnapshot> containers, int capacity,
        InventorySortField field, bool descending, Func<uint, (string Name, uint Category)> describe)
        =>Build(containers,capacity,new[]{new InventorySortCriterion(field,descending)},describe);

    public static InventorySortResult Build(IEnumerable<InventoryContainerSnapshot> containers,int capacity,
        IReadOnlyList<InventorySortCriterion> criteria,Func<uint,(string Name,uint Category)> describe)
    {
        ArgumentNullException.ThrowIfNull(describe);
        return Build(containers,capacity,criteria,id=>{var value=describe(id);return new InventorySortMetadata(value.Name,value.Category);});
    }

    public static InventorySortResult Build(IEnumerable<InventoryContainerSnapshot> containers,int capacity,
        IReadOnlyList<InventorySortCriterion> criteria,Func<uint,InventorySortMetadata> describe)
    {
        ArgumentNullException.ThrowIfNull(containers);
        ArgumentNullException.ThrowIfNull(describe);
        if(capacity<0 || criteria is null || criteria.Count is <1 or >MaximumCriteria || criteria.Any(c=>c is null || !Enum.IsDefined(c.Field))
            || criteria.Select(c=>c.Field).Distinct().Count()!=criteria.Count)throw new ArgumentOutOfRangeException(nameof(criteria));
        var source = containers.OrderBy(container => container.Location.Container.Value).ToArray();
        if (source.Any(container => container.Coverage != InventoryCoverage.Complete)
            || source.Select(container => container.Location.Owner).Distinct().Count() > 1
            || source.Select(container => container.Location).Distinct().Count() != source.Length)
            throw new InvalidOperationException("A complete, single-owner, nonduplicate source is required.");
        var values = source.SelectMany(container => container.Stacks.Select(stack => new InventoryPreviewStack(container.Location, stack))).ToArray();
        if (values.Length > capacity) throw new InvalidOperationException("Stacks exceed preview capacity.");
        var ordered = values.Select((value, index) => (Value: value, Index: index, Label: describe(value.Stack.Item.Id.Value))).ToArray();
        Array.Sort(ordered,(left,right)=>
        {
            foreach(var criterion in criteria)
            {
                // Unknown metadata always follows known values, whatever the direction.
                var known=criterion.Field switch
                {
                    InventorySortField.ItemLevel=>Unknown(left.Label.ItemLevel,right.Label.ItemLevel),
                    InventorySortField.EquipLevel=>Unknown(left.Label.EquipLevel,right.Label.EquipLevel),
                    InventorySortField.Rarity=>Unknown(left.Label.Rarity,right.Label.Rarity),
                    InventorySortField.SellPrice=>Unknown(left.Label.SellPrice,right.Label.SellPrice),
                    _=>0,
                };
                if(known!=0)return known;
                var comparison=criterion.Field switch
                {
                    InventorySortField.Quantity=>left.Value.Stack.Quantity.CompareTo(right.Value.Stack.Quantity),
                    InventorySortField.Quality=>left.Value.Stack.Item.IsHighQuality.CompareTo(right.Value.Stack.Item.IsHighQuality),
                    InventorySortField.Category=>left.Label.Category.CompareTo(right.Label.Category),
                    InventorySortField.ItemLevel=>Nullable.Compare(left.Label.ItemLevel,right.Label.ItemLevel),
                    InventorySortField.EquipLevel=>Nullable.Compare(left.Label.EquipLevel,right.Label.EquipLevel),
                    InventorySortField.Rarity=>Nullable.Compare(left.Label.Rarity,right.Label.Rarity),
                    InventorySortField.SellPrice=>Nullable.Compare(left.Label.SellPrice,right.Label.SellPrice),
                    InventorySortField.ItemId=>left.Value.Stack.Item.Id.Value.CompareTo(right.Value.Stack.Item.Id.Value),
                    _=>StringComparer.OrdinalIgnoreCase.Compare(left.Label.Name,right.Label.Name),
                };
                if(comparison!=0)return criterion.Descending ? -Math.Sign(comparison):Math.Sign(comparison);
            }
            var id=left.Value.Stack.Item.Id.Value.CompareTo(right.Value.Stack.Item.Id.Value);if(id!=0)return id;
            var quality=left.Value.Stack.Item.IsHighQuality.CompareTo(right.Value.Stack.Item.IsHighQuality);return quality!=0 ? quality:left.Index.CompareTo(right.Index);
        });
        return new(capacity,Array.AsReadOnly(ordered.Select(row=>row.Value).ToArray()));
    }

    private static int Unknown<T>(T? left,T? right) where T:struct=>left.HasValue==right.HasValue ? 0:left.HasValue ? -1:1;
}
