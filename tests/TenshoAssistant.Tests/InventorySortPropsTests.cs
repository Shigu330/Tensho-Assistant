using System.Text.Json;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;

internal static class InventorySortPropsTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Refuses(Action action){try{action();}catch(InvalidOperationException){return;}throw new Exception("Expected rejection");}
    private static readonly InventorySortScope Scope=InventorySortScope.Personal(7);
    private static InventorySnapshot Snapshot()=>new(DateTimeOffset.UtcNow,"fixture",Enumerable.Range(0,4).Select(bag=>new InventoryContainerSnapshot(new(new(7),new(bag)),InventoryCoverage.Complete,
        bag==0 ? Enumerable.Range(0,5).Select(index=>new InventoryStack(new(new((uint)(42+index)),false),1,index,index)):[])));
    // 45 is missing from the catalogue: every new property is unknown.
    private static InventorySortMetadata Metadata(uint id)=>id switch
    {
        42=>new("A",1,ItemLevel:100,EquipLevel:50,Rarity:2,SellPrice:10,Traits:ItemTraits.Unique|ItemTraits.Dyeable),
        43=>new("B",1,ItemLevel:300,EquipLevel:90,Rarity:3,SellPrice:500,Traits:ItemTraits.Untradable|ItemTraits.Repairable),
        44=>new("C",1,ItemLevel:1,EquipLevel:1,Rarity:1,SellPrice:0,Traits:ItemTraits.Collectable),
        46=>new("E",1,ItemLevel:300,EquipLevel:90,Rarity:4,SellPrice:50,Traits:ItemTraits.None),
        _=>new("D",0),
    };
    private static InventorySortCriterion C(InventorySortField value,bool descending=false)=>new(value,descending);
    private static uint[] Order(params InventorySortCriterion[] criteria)
        =>InventorySortRules.Build(Snapshot().Containers.Values,Scope,InventorySortRules.Default(Scope) with{Criteria=criteria},Metadata).Stacks.Select(v=>v.Stack.Item.Id.Value).ToArray();
    private static HashSet<uint> Zone(InventorySortZone zone)
        =>InventorySortRules.Build(Snapshot().Containers.Values,Scope,InventorySortRules.Default(Scope) with{Zones=new[]{zone}},Metadata).Slots!.Skip(zone.Start).Take(zone.Count)
            .OfType<InventoryPreviewStack>().Select(value=>value.Stack.Item.Id.Value).ToHashSet();
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("New sort criteria order known item values and keep unknown metadata last in both directions",()=>
        {
            Check(Order(C(InventorySortField.ItemLevel)).SequenceEqual(new uint[]{44,42,43,46,45}),"item level ascending, ties by ID, unknown last");
            Check(Order(C(InventorySortField.ItemLevel,true)).SequenceEqual(new uint[]{43,46,42,44,45}),"descending keeps unknown last");
            Check(Order(C(InventorySortField.SellPrice,true)).SequenceEqual(new uint[]{43,46,42,44,45}),"sell price");
            Check(Order(C(InventorySortField.Rarity,true)).SequenceEqual(new uint[]{46,43,42,44,45}),"rarity");
            Check(Order(C(InventorySortField.EquipLevel),C(InventorySortField.Rarity,true)).SequenceEqual(new uint[]{44,42,46,43,45}),"second criterion breaks ties");
            Check(Order(C(InventorySortField.ItemId,true)).SequenceEqual(new uint[]{46,45,44,43,42}),"identifier");
            var six=Enum.GetValues<InventorySortField>().Take(6).Select(value=>new InventorySortCriterion(value)).ToArray();
            InventorySortRules.Validate(InventorySortRules.Default(Scope) with{Criteria=six},Scope);
            Refuses(()=>InventorySortRules.Validate(InventorySortRules.Default(Scope) with{Criteria=Enum.GetValues<InventorySortField>().Take(7).Select(value=>new InventorySortCriterion(value)).ToArray()},Scope));
        }),
        ("Range rarity and property filters use known type values and never match unknown metadata",()=>
        {
            Check(Zone(new("Lvl",20,5,ItemLevelMin:100,ItemLevelMax:300)).SetEquals(new uint[]{42,43,46}),"item level range inclusive");
            Check(Zone(new("Min",20,5,ItemLevelMin:200)).SetEquals(new uint[]{43,46}),"open max");
            Check(Zone(new("Max",20,5,EquipLevelMax:50)).SetEquals(new uint[]{42,44}),"open min equip");
            Check(Zone(new("Price",20,5,SellPriceMin:10,SellPriceMax:100)).SetEquals(new uint[]{42,46}),"price range");
            Check(Zone(new("Rare",20,5,Rarity:3)).SetEquals(new uint[]{43}),"exact rarity");
            Check(Zone(new("Dye",20,5,Dyeable:InventoryPropertyFilter.Yes)).SetEquals(new uint[]{42}),"trait yes");
            Check(Zone(new("NoRepair",20,5,Repairable:InventoryPropertyFilter.No)).SetEquals(new uint[]{42,44,46}),"trait no excludes unknown 45");
            Check(Zone(new("Mix",20,5,Untradable:InventoryPropertyFilter.No,Collectable:InventoryPropertyFilter.No,ItemLevelMin:50)).SetEquals(new uint[]{42,46}),"filters combine");
            Check(Zone(new("Unique",20,5,Unique:InventoryPropertyFilter.Yes)).SetEquals(new uint[]{42}) && Zone(new("Collect",20,5,Collectable:InventoryPropertyFilter.Yes)).SetEquals(new uint[]{44}),"other traits");
            Check(Zone(new("Any",20,5)).Count==5,"inactive filters keep unknown items");
        }),
        ("Invalid ranges rarities and property filters are refused and old zones load without new restrictions",()=>
        {
            var zone=new InventorySortZone("Z",20,5);
            foreach(var invalid in new[]{zone with{ItemLevelMin=300,ItemLevelMax=100},zone with{ItemLevelMin=-1},zone with{EquipLevelMax=256},zone with{SellPriceMin=10,SellPriceMax=5},
                zone with{Rarity=5},zone with{Rarity=-1},zone with{Unique=(InventoryPropertyFilter)9},zone with{Collectable=(InventoryPropertyFilter)(-1)}})
                Refuses(()=>InventorySortRules.Validate(InventorySortRules.Default(Scope) with{Zones=new[]{invalid}},Scope));
            InventorySortRules.Validate(InventorySortRules.Default(Scope) with{Zones=new[]{zone with{ItemLevelMin=100,SellPriceMax=50,Rarity=7}}},Scope);
            var old=JsonSerializer.Deserialize<InventorySortZone>("{\"Name\":\"Old\",\"Start\":0,\"Count\":5}")!;
            Check(old.ItemLevelMin==0 && old.ItemLevelMax==0 && old.Rarity==0 && old.SellPriceMax==0 && old.Unique==InventoryPropertyFilter.Any && old.Collectable==InventoryPropertyFilter.Any,"v1 defaults");
            var copy=JsonSerializer.Deserialize<InventorySortZone>(JsonSerializer.Serialize(zone with{EquipLevelMin=10,Rarity=4,Dyeable=InventoryPropertyFilter.No}))!;
            Check(copy.EquipLevelMin==10 && copy.Rarity==4 && copy.Dyeable==InventoryPropertyFilter.No,"new filters roundtrip");
        }),
        ("Catalogue items carry the copied properties and the item cache refuses unknown trait bits",()=>
        {
            var item=new ItemDefinition(42,"Épée",true,ItemLevel:620,EquipLevel:90,Rarity:3,SellPrice:1234,Traits:ItemTraits.Unique|ItemTraits.Repairable);
            var label=InventorySortMetadata.FromItem(42,item);
            Check(label.ItemLevel==620 && label.EquipLevel==90 && label.Rarity==3 && label.SellPrice==1234 && label.Traits==(ItemTraits.Unique|ItemTraits.Repairable),"copied once into metadata");
            var missing=InventorySortMetadata.FromItem(42,null);
            Check(missing.ItemLevel is null && missing.Rarity is null && missing.SellPrice is null && missing.Traits is null,"missing item stays unknown");
            var game=new ItemCatalogMetadata("Lumina.Excel/Item","French","v",1);
            Check(ItemCatalogCache.Valid(ItemCatalogCache.Snapshot(game,[item]),game) && !ItemCatalogCache.Valid(ItemCatalogCache.Snapshot(game,[item with{Traits=(ItemTraits)64}]),game)
                && ItemCatalogCache.Shape.Contains("ItemTraits(",StringComparison.Ordinal) && ItemCatalogCache.Shape.Contains("SellPrice:UInt32",StringComparison.Ordinal),"cache shape and validation follow the new fields");
        }),
    };
}
