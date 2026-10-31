using TenshoAssistant.Items;

namespace TenshoAssistant.Inventory;

public enum InventoryQualityFilter { Any, Normal, HighQuality }
public enum InventoryPropertyFilter { Any, Yes, No }
// Null means unknown (item missing from the catalogue): it never satisfies a filter and sorts after known values.
public sealed record InventorySortMetadata(string Name,uint Category,bool? Marketable=null,bool? CanBeHq=null,EquipmentSlots? Equipment=null,
    int? ItemLevel=null,int? EquipLevel=null,int? Rarity=null,uint? SellPrice=null,ItemTraits? Traits=null)
{
    public static InventorySortMetadata FromItem(uint id,ItemDefinition? item)=>item is null || item.Id!=id ? new($"Objet {id}",0)
        :new(item.Name,item.UiCategoryId,item.IsMarketable,item.CanBeHq,item.Equipment,(int)Math.Min(item.ItemLevel,int.MaxValue),item.EquipLevel,item.Rarity,item.SellPrice,item.Traits);
}
public sealed record InventorySortCriterion(InventorySortField Field,bool Descending=false);
// CategoryId is the single choice saved before 0.77; it stays one member of the category union.
// ItemIds and the ';'-separated Query terms form one name-or-identifier selection (union).
public sealed record InventorySortZone(string Name,int Start,int Count,uint CategoryId=0,InventoryQualityFilter Quality=InventoryQualityFilter.Any,string Query="",bool Preserve=false,bool ReverseFill=false,
    InventoryPropertyFilter Marketable=InventoryPropertyFilter.Any,InventoryPropertyFilter CanBeHq=InventoryPropertyFilter.Any,EquipmentSlots Equipment=EquipmentSlots.None,
    IReadOnlyList<uint>? CategoryIds=null,IReadOnlyList<uint>? ItemIds=null,bool ShareUnused=false,
    // Range bounds: 0 means "no bound". Rarity: 0 means any, otherwise the exact game value.
    int ItemLevelMin=0,int ItemLevelMax=0,int EquipLevelMin=0,int EquipLevelMax=0,int Rarity=0,uint SellPriceMin=0,uint SellPriceMax=0,
    InventoryPropertyFilter Unique=InventoryPropertyFilter.Any,InventoryPropertyFilter Untradable=InventoryPropertyFilter.Any,InventoryPropertyFilter Dyeable=InventoryPropertyFilter.Any,
    InventoryPropertyFilter Repairable=InventoryPropertyFilter.Any,InventoryPropertyFilter Collectable=InventoryPropertyFilter.Any);
public sealed record InventorySortProfile(ulong CharacterId,BankStorageKind? Kind,ulong StorageId,IReadOnlyList<InventorySortCriterion> Criteria,IReadOnlyList<InventorySortZone> Zones);

public static class InventorySortRules
{
    public static bool MatchesScope(InventorySortProfile profile,InventorySortScope scope)
        =>profile.CharacterId==scope.CharacterId && profile.Kind==scope.Storage?.Kind && profile.StorageId==(scope.Storage?.StorageId ?? 0);
    public static InventorySortProfile Default(InventorySortScope scope,InventorySortField sort=InventorySortField.Name,bool descending=false)
        =>new(scope.CharacterId,scope.Storage?.Kind,scope.Storage?.StorageId ?? 0,Array.AsReadOnly(new[]{new InventorySortCriterion(sort,descending)}),Array.AsReadOnly(Array.Empty<InventorySortZone>()));
    public const int MaxZoneCategories=64,MaxZoneItems=128;
    public static InventorySortProfile Copy(InventorySortProfile value)
        =>value with{Criteria=Array.AsReadOnly(value.Criteria.ToArray()),Zones=CopyZones(value.Zones)};
    internal static IReadOnlyList<InventorySortZone> CopyZones(IReadOnlyList<InventorySortZone> zones)
        =>Array.AsReadOnly(zones.Select(zone=>zone is null ? zone! : zone with{CategoryIds=CopyIds(zone.CategoryIds),ItemIds=CopyIds(zone.ItemIds)}).ToArray());
    private static IReadOnlyList<uint>? CopyIds(IReadOnlyList<uint>? ids)=>ids is null ? null:Array.AsReadOnly(ids.ToArray());
    public static IReadOnlyList<uint> Categories(InventorySortZone zone)
        =>(zone.CategoryId==0 ? [] : new[]{zone.CategoryId}).Concat(zone.CategoryIds ?? []).Distinct().ToArray();
    public static IReadOnlyList<string> QueryTerms(string? query)
        =>(query ?? "").Split(';',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
    public static void Validate(InventorySortProfile profile,InventorySortScope scope)
    {
        if(!MatchesScope(profile,scope))throw new InvalidOperationException("Le profil appartient à un autre personnage ou rangement.");
        ValidateContent(profile.Criteria,profile.Zones,scope.Capacity);
    }
    internal static void ValidateContent(IReadOnlyList<InventorySortCriterion> criteria,IReadOnlyList<InventorySortZone> zones,int capacity)
    {
        if(criteria is null || zones is null || criteria.Count is <1 or >InventorySortPreview.MaximumCriteria || criteria.Any(c=>c is null || !Enum.IsDefined(c.Field))
            || criteria.Select(c=>c.Field).Distinct().Count()!=criteria.Count || zones.Count>32)
            throw new InvalidOperationException("Critères ou nombre de zones invalides.");
        var reserved=new bool[capacity];
        foreach(var zone in zones)
        {
            if(zone is null || string.IsNullOrWhiteSpace(zone.Name) || zone.Name.Length>80 || zone.Query is null || zone.Query.Length>128
                || !Enum.IsDefined(zone.Quality) || !Enum.IsDefined(zone.Marketable) || !Enum.IsDefined(zone.CanBeHq) || (zone.Equipment&~EquipmentSlots.All)!=0
                || !ValidIds(zone.CategoryIds,MaxZoneCategories) || !ValidIds(zone.ItemIds,MaxZoneItems)
                || !ValidRange(zone.ItemLevelMin,zone.ItemLevelMax,9999) || !ValidRange(zone.EquipLevelMin,zone.EquipLevelMax,255)
                || zone.SellPriceMax!=0 && zone.SellPriceMin>zone.SellPriceMax || !Rarities.Contains(zone.Rarity)
                || new[]{zone.Unique,zone.Untradable,zone.Dyeable,zone.Repairable,zone.Collectable}.Any(filter=>!Enum.IsDefined(filter))
                || zone.Start<0 || zone.Count<=0 || zone.Start>capacity-zone.Count || zone.Preserve && (zone.ReverseFill || zone.ShareUnused))
                throw new InvalidOperationException("Zone invalide : nom, filtres ou limites de cases.");
            for(var slot=zone.Start;slot<zone.Start+zone.Count;slot++)
            {
                if(reserved[slot])throw new InvalidOperationException("Les zones de cases ne doivent pas se chevaucher.");
                reserved[slot]=true;
            }
        }
    }
    public static InventorySortProfile Load(IEnumerable<InventorySortProfile>? saved,InventorySortScope scope,InventorySortField legacy,bool descending)
    {
        var matches=(saved ?? []).Where(profile=>profile is not null && MatchesScope(profile,scope)).Take(2).ToArray();
        if(matches.Length>1)throw new InvalidOperationException("Plusieurs profils existent pour ce rangement ; réinitialisez son brouillon puis enregistrez-le.");
        var profile=matches.SingleOrDefault() ?? Default(scope,legacy,descending);Validate(profile,scope);return Copy(profile);
    }
    public static List<InventorySortProfile> Save(IEnumerable<InventorySortProfile>? saved,InventorySortProfile profile,InventorySortScope scope)
    {
        Validate(profile,scope);
        var next=(saved ?? []).Where(value=>value is not null && !MatchesScope(value,scope)).ToList();
        if(next.Count>=128)throw new InvalidOperationException("La limite de 128 profils de rangement est atteinte.");
        next.Add(Copy(profile));return next;
    }

    public static InventorySortResult Build(IEnumerable<InventoryContainerSnapshot> containers,InventorySortScope scope,InventorySortProfile profile,
        Func<uint,(string Name,uint Category)> describe)
    {
        ArgumentNullException.ThrowIfNull(describe);
        return Build(containers,scope,profile,id=>{var value=describe(id);return new InventorySortMetadata(value.Name,value.Category);});
    }
    public static InventorySortResult Build(IEnumerable<InventoryContainerSnapshot> containers,InventorySortScope scope,InventorySortProfile profile,
        Func<uint,InventorySortMetadata> describe)
    {
        ArgumentNullException.ThrowIfNull(describe);
        Validate(profile,scope);
        var metadata=new Dictionary<uint,InventorySortMetadata>();
        InventorySortMetadata Describe(uint id)
        {if(!metadata.TryGetValue(id,out var value)){value=describe(id);metadata.Add(id,value);}return value;}
        var source=containers.ToArray();
        var all=InventorySortPreview.Build(source,scope.Capacity,profile.Criteria,Describe);
        if(profile.Zones.Count==0)return all;
        var slots=new InventoryPreviewStack?[scope.Capacity];var reserved=new bool[scope.Capacity];
        var remaining=all.Stacks.ToList();
        foreach(var zone in profile.Zones)
            for(var slot=zone.Start;slot<zone.Start+zone.Count;slot++)reserved[slot]=true;
        if(profile.Zones.Any(zone=>zone.Preserve))
        {
            // A protected zone needs actual visual indices, never a guessed packed order from an old cache.
            var physical=scope.Extract(source);
            if(physical.Count!=all.Stacks.Count)throw new InvalidOperationException("Les positions visuelles ne couvrent pas exactement le rangement protégé.");
            foreach(var zone in profile.Zones.Where(zone=>zone.Preserve))
            foreach(var value in all.Stacks.Where(value=>value.Stack.DisplayIndex>=zone.Start && value.Stack.DisplayIndex<zone.Start+zone.Count))
            {slots[value.Stack.DisplayIndex]=value;remaining.Remove(value);}
        }
        foreach(var zone in profile.Zones.Where(zone=>!zone.Preserve))
        {
            // Category list order is the zone priority; the stable sort keeps the criteria order inside each category.
            var categories=Categories(zone);
            var matches=remaining.Where(value=>Matches(value,zone,Describe(value.Stack.Item.Id.Value)))
                .OrderBy(value=>categories.Count==0 ? 0:IndexOf(categories,Describe(value.Stack.Item.Id.Value).Category)).Take(zone.Count).ToArray();
            for(var index=0;index<matches.Length;index++)
            {slots[zone.ReverseFill ? zone.Start+zone.Count-1-index:zone.Start+index]=matches[index];remaining.Remove(matches[index]);}
            if(zone.ShareUnused)
                for(var slot=zone.Start;slot<zone.Start+zone.Count;slot++)if(slots[slot] is null)reserved[slot]=false;
        }
        var free=Enumerable.Range(0,scope.Capacity).Where(slot=>!reserved[slot]).ToArray();
        if(remaining.Count>free.Length)
            throw new InvalidOperationException("Les cases non réservées ne suffisent pas aux autres piles. Réduisez les zones ou libérez des cases.");
        for(var index=0;index<remaining.Count;index++)slots[free[index]]=remaining[index];
        return new(scope.Capacity,Array.AsReadOnly(slots.OfType<InventoryPreviewStack>().ToArray()),Array.AsReadOnly(slots));
    }
    private static int IndexOf(IReadOnlyList<uint> values,uint value)
    {for(var index=0;index<values.Count;index++)if(values[index]==value)return index;return values.Count;}
    // Game rarity values: white, green, blue, purple (relic) and pink (aetherial); 0 is the "any" filter.
    public static readonly IReadOnlyList<int> Rarities=Array.AsReadOnly(new[]{0,1,2,3,4,7});
    private static bool ValidRange(int min,int max,int limit)=>min>=0 && max>=0 && min<=limit && max<=limit && (max==0 || min<=max);
    private static bool RangeMatches(long? value,long min,long max)=>min==0 && max==0 || value is { } known && (min==0 || known>=min) && (max==0 || known<=max);
    private static bool TraitMatches(ItemTraits? traits,ItemTraits trait,InventoryPropertyFilter filter)
        =>PropertyMatches(traits is { } known ? (known&trait)!=0:null,filter);
    private static bool ValidIds(IReadOnlyList<uint>? ids,int limit)
        =>ids is null || ids.Count<=limit && !ids.Contains(0u) && ids.Distinct().Count()==ids.Count;
    private static bool PropertyMatches(bool? value,InventoryPropertyFilter filter)
        =>filter==InventoryPropertyFilter.Any || value is { } known && known==(filter==InventoryPropertyFilter.Yes);
    private static bool SelectionMatches(uint id,InventorySortZone zone,string name)
    {
        var terms=QueryTerms(zone.Query);var items=zone.ItemIds ?? [];
        if(terms.Count==0 && items.Count==0)return true;
        var text=id.ToString();
        return items.Contains(id) || terms.Any(term=>name.Contains(term,StringComparison.OrdinalIgnoreCase) || text.Contains(term,StringComparison.Ordinal));
    }
    private static bool Matches(InventoryPreviewStack value,InventorySortZone zone,InventorySortMetadata label)
    {
        var categories=Categories(zone);
        return (categories.Count==0 || categories.Contains(label.Category))
          && PropertyMatches(label.Marketable,zone.Marketable) && PropertyMatches(label.CanBeHq,zone.CanBeHq)
          && (zone.Equipment==EquipmentSlots.None || label.Equipment is { } slots && (slots&zone.Equipment)!=0)
          && (zone.Quality==InventoryQualityFilter.Any || value.Stack.Item.IsHighQuality==(zone.Quality==InventoryQualityFilter.HighQuality))
          && SelectionMatches(value.Stack.Item.Id.Value,zone,label.Name)
          && RangeMatches(label.ItemLevel,zone.ItemLevelMin,zone.ItemLevelMax) && RangeMatches(label.EquipLevel,zone.EquipLevelMin,zone.EquipLevelMax)
          && RangeMatches(label.SellPrice,zone.SellPriceMin,zone.SellPriceMax) && (zone.Rarity==0 || label.Rarity==zone.Rarity)
          && TraitMatches(label.Traits,ItemTraits.Unique,zone.Unique) && TraitMatches(label.Traits,ItemTraits.Untradable,zone.Untradable)
          && TraitMatches(label.Traits,ItemTraits.Dyeable,zone.Dyeable) && TraitMatches(label.Traits,ItemTraits.Repairable,zone.Repairable)
          && TraitMatches(label.Traits,ItemTraits.Collectable,zone.Collectable);
    }
}
