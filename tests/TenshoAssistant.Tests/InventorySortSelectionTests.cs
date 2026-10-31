using System.Text.Json;
using TenshoAssistant.Inventory;

internal static class InventorySortSelectionTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Refuses(Action action){try{action();}catch(InvalidOperationException){return;}throw new Exception("Expected rejection");}
    private static readonly InventorySortScope Scope=InventorySortScope.Personal(7);
    // Items 42..46: categories 1,1,2,3,4 ; 42 is HQ.
    private static InventorySnapshot Snapshot()=>new(DateTimeOffset.UtcNow,"fixture",Enumerable.Range(0,4).Select(bag=>new InventoryContainerSnapshot(new(new(7),new(bag)),InventoryCoverage.Complete,
        bag==0 ? Enumerable.Range(0,5).Select(index=>new InventoryStack(new(new((uint)(42+index)),index==0),index+1,index,index)):[])));
    private static InventorySortMetadata Metadata(uint id)=>id switch
    {
        42=>new("Iron sword",1),43=>new("Bronze sword",1),44=>new("Fire shard",2),45=>new("Maple log",3),46=>new("Cotton yarn",4),_=>new("Unknown",0),
    };
    private static InventorySortProfile Profile(params InventorySortZone[] zones)=>InventorySortRules.Default(Scope) with{Zones=zones};
    private static HashSet<uint> Zone(InventorySortZone zone)
        =>InventorySortRules.Build(Snapshot().Containers.Values,Scope,Profile(zone),Metadata).Slots!.Skip(zone.Start).Take(zone.Count)
            .OfType<InventoryPreviewStack>().Select(value=>value.Stack.Item.Id.Value).ToHashSet();
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Zone categories form a union with the legacy single category and still combine with quality",()=>
        {
            Check(Zone(new("Multi",20,5,CategoryIds:new uint[]{2,3})).SetEquals(new uint[]{44,45}),"two categories");
            Check(Zone(new("Legacy+",20,5,CategoryId:1,CategoryIds:new uint[]{4})).SetEquals(new uint[]{42,43,46}),"legacy category stays in the union");
            Check(Zone(new("Legacy",20,5,CategoryId:3)).SetEquals(new uint[]{45}),"pre-0.77 single category unchanged");
            Check(Zone(new("HQ",20,5,Quality:InventoryQualityFilter.HighQuality,CategoryIds:new uint[]{1,2})).SetEquals(new uint[]{42}),"categories AND actual quality");
            Check(InventorySortRules.Categories(new("D",0,1,CategoryId:2,CategoryIds:new uint[]{2,3})).SequenceEqual(new uint[]{2,3}),"union without duplicates");
        }),
        ("Zone category list order is a priority that keeps criteria order inside each category and decides overflow",()=>
        {
            IEnumerable<uint?> Slots(InventorySortZone zone)=>InventorySortRules.Build(Snapshot().Containers.Values,Scope,Profile(zone),Metadata).Slots!
                .Skip(zone.Start).Take(zone.Count).Select(value=>value?.Stack.Item.Id.Value);
            Check(Slots(new("Prio",20,3,CategoryIds:new uint[]{3,1})).SequenceEqual(new uint?[]{45,43,42}),"category 3 first, then names inside category 1");
            Check(Slots(new("Swap",20,3,CategoryIds:new uint[]{1,3})).SequenceEqual(new uint?[]{43,42,45}),"reordering the list reorders the zone");
            Check(Slots(new("Overflow",20,2,CategoryIds:new uint[]{4,1})).SequenceEqual(new uint?[]{46,43}),"priority chooses which stacks fit");
            Check(Slots(new("Legacy first",20,3,CategoryId:3,CategoryIds:new uint[]{1})).SequenceEqual(new uint?[]{45,43,42}),"legacy category keeps first rank");
            Check(Slots(new("Reverse",20,3,ReverseFill:true,CategoryIds:new uint[]{3,1})).SequenceEqual(new uint?[]{42,43,45}),"reverse fill starts the priority from the last slot");
        }),
        ("A sharing zone lends its unused slots to other stacks without gaps while a default zone keeps them empty",()=>
        {
            var zone=new InventorySortZone("Meds",0,10,CategoryIds:new uint[]{3,1},ShareUnused:true);
            var shared=InventorySortRules.Build(Snapshot().Containers.Values,Scope,Profile(zone),Metadata).Slots!;
            Check(shared.Take(5).Select(value=>value?.Stack.Item.Id.Value).SequenceEqual(new uint?[]{45,43,42,46,44}),"zone first by priority, others right after by name");
            Check(shared.Skip(5).All(value=>value is null),"no hole and nothing beyond");
            var kept=InventorySortRules.Build(Snapshot().Containers.Values,Scope,Profile(zone with{ShareUnused=false}),Metadata).Slots!;
            Check(kept.Skip(3).Take(7).All(value=>value is null) && kept[10]?.Stack.Item.Id.Value==46,"default keeps reserved empties");
            var reverse=InventorySortRules.Build(Snapshot().Containers.Values,Scope,Profile(zone with{ReverseFill=true}),Metadata).Slots!;
            Check(reverse[9]?.Stack.Item.Id.Value==45 && reverse[0]?.Stack.Item.Id.Value==46,"reverse sharing gives the leading slots to others");
            Refuses(()=>InventorySortRules.Validate(Profile(zone with{Preserve=true}),Scope));
            Check(JsonSerializer.Deserialize<InventorySortZone>("{\"Name\":\"Old\",\"Start\":0,\"Count\":5}")!.ShareUnused==false,"saved zones keep their reserved empties");
        }),
        ("Chosen items and semicolon terms form one name-or-identifier selection",()=>
        {
            Check(Zone(new("Items",20,5,ItemIds:new uint[]{44,46})).SetEquals(new uint[]{44,46}),"exact chosen IDs");
            Check(Zone(new("Terms",20,5,Query:" sword ; log ;")).SetEquals(new uint[]{42,43,45}),"trimmed terms, empty terms ignored");
            Check(Zone(new("Both",20,5,Query:"yarn",ItemIds:new uint[]{44})).SetEquals(new uint[]{44,46}),"list and terms are a union");
            Check(Zone(new("Id term",20,5,Query:"45")).SetEquals(new uint[]{45}),"identifier term");
            Check(Zone(new("Filtered",20,5,CategoryIds:new uint[]{1},ItemIds:new uint[]{42,44})).SetEquals(new uint[]{42}),"selection AND categories");
            Check(Zone(new("All",20,5,Query:" ; ")).Count==5,"no selection matches every item");
            Check(InventorySortRules.QueryTerms("a;;b ; ").SequenceEqual(new[]{"a","b"}),"term parsing");
        }),
        ("Zone identifier lists reject zeros duplicates and overflow",()=>
        {
            var zone=new InventorySortZone("Z",20,5);
            foreach(var invalid in new[]{zone with{CategoryIds=new uint[]{0}},zone with{CategoryIds=new uint[]{2,2}},zone with{ItemIds=new uint[]{0}},zone with{ItemIds=new uint[]{42,42}},
                zone with{CategoryIds=Enumerable.Range(1,InventorySortRules.MaxZoneCategories+1).Select(value=>(uint)value).ToArray()},
                zone with{ItemIds=Enumerable.Range(1,InventorySortRules.MaxZoneItems+1).Select(value=>(uint)value).ToArray()}})
                Refuses(()=>InventorySortRules.Validate(Profile(invalid),Scope));
            InventorySortRules.Validate(Profile(zone with{CategoryIds=new uint[]{1,2},ItemIds=new uint[]{42}}),Scope);
        }),
        ("Selection lists serialize compatibly and are copied away from caller arrays",()=>
        {
            var zone=new InventorySortZone("Gear",20,5,Query:"a;b",CategoryIds:new uint[]{1,2},ItemIds:new uint[]{42});
            var copy=JsonSerializer.Deserialize<InventorySortZone>(JsonSerializer.Serialize(zone))!;
            Check(copy.CategoryIds!.SequenceEqual(new uint[]{1,2}) && copy.ItemIds!.SequenceEqual(new uint[]{42}) && copy.Query=="a;b","lists roundtrip");
            var old=JsonSerializer.Deserialize<InventorySortZone>("{\"Name\":\"Old\",\"Start\":0,\"Count\":5,\"CategoryId\":3}")!;
            Check(old.CategoryIds is null && old.ItemIds is null && InventorySortRules.Categories(old).SequenceEqual(new uint[]{3}),"v1 zone keeps its single category");
            var categories=new uint[]{1,2};var items=new uint[]{42};
            var profile=Profile(new InventorySortZone("Mutable",20,5,CategoryIds:categories,ItemIds:items));
            var saved=InventorySortRules.Save([],profile,Scope);
            var template=InventorySortTemplates.Create(profile,Scope,"T");
            categories[0]=9;items[0]=99;
            var loaded=InventorySortRules.Load(saved,Scope,InventorySortField.Name,false).Zones[0];
            Check(loaded.CategoryIds!.SequenceEqual(new uint[]{1,2}) && loaded.ItemIds!.SequenceEqual(new uint[]{42}),"saved profile detached");
            Check(template.Zones[0].CategoryIds!.SequenceEqual(new uint[]{1,2}) && template.Zones[0].ItemIds!.SequenceEqual(new uint[]{42}),"template detached");
        }),
    };
}
