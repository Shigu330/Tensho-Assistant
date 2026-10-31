using System.Text.Json;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;

internal static class InventorySortMetadataTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Refuses(Action action){try{action();}catch(InvalidOperationException){return;}throw new Exception("Expected rejection");}
    private static readonly InventorySortScope Scope=InventorySortScope.Personal(7);
    private static readonly DateTimeOffset Now=DateTimeOffset.UtcNow;
    private static InventorySnapshot Snapshot()=>new(Now,"fixture",Enumerable.Range(0,4).Select(bag=>new InventoryContainerSnapshot(new(new(7),new(bag)),InventoryCoverage.Complete,
        bag==0 ? Enumerable.Range(0,5).Select(index=>new InventoryStack(new(new((uint)(42+index)),index==0),index+1,index,index)):[])));
    private static InventorySortMetadata Metadata(uint id)=>id switch
    {
        42=>new("HQ sword",1,true,true,EquipmentSlots.MainHand),
        43=>new("Bound tool",1,false,true,EquipmentSlots.MainHand),
        44=>new("Soul crystal",2,false,false,EquipmentSlots.SoulCrystal),
        45=>new("Shield",3,true,false,EquipmentSlots.OffHand),
        _=>new("Unknown",0),
    };
    private static InventorySortProfile Profile(InventorySortZone zone)=>InventorySortRules.Default(Scope) with{Zones=new[]{zone}};
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Native item type filters combine equipment marketability HQ potential category and actual quality",()=>
        {
            var snapshot=Snapshot();var zone=new InventorySortZone("Sell HQ mainhand",35,5,1,InventoryQualityFilter.HighQuality,
                Marketable:InventoryPropertyFilter.Yes,CanBeHq:InventoryPropertyFilter.Yes,Equipment:EquipmentSlots.MainHand);
            var result=InventorySortRules.Build(snapshot.Containers.Values,Scope,Profile(zone),Metadata);
            Check(result.Slots![35]?.Stack.Item.Id.Value==42 && result.Slots.Skip(36).Take(4).All(value=>value is null),"AND across known metadata and actual HQ");
            Check(result.Stacks.Count==5 && result.Stacks.Sum(value=>value.Stack.Quantity)==15,"all other known and unknown objects remain outside zone");
            Check(InventoryVisualSortPlan.Build(snapshot,result,Scope).Order[35]==new InventoryPhysicalSlot(0,0),"filtered placement reaches common native permutation");
        }),
        ("Unknown metadata never satisfies No or Yes property filters and never masquerades as nonmarketable",()=>
        {
            var snapshot=Snapshot();
            foreach(var filter in new[]{InventoryPropertyFilter.Yes,InventoryPropertyFilter.No})
            {
                var zone=new InventorySortZone("Market",20,5,Marketable:filter);
                var result=InventorySortRules.Build(snapshot.Containers.Values,Scope,Profile(zone),Metadata);
                var accepted=result.Slots!.Skip(20).Take(5).OfType<InventoryPreviewStack>().Select(v=>v.Stack.Item.Id.Value).ToArray();
                Check(accepted.ToHashSet().SetEquals(filter==InventoryPropertyFilter.Yes ? new uint[]{42,45}:new uint[]{43,44}),"only positive or negative evidence matches");
                Check(!accepted.Contains(46U),"unknown is not false");
                var potential=InventorySortRules.Build(snapshot.Containers.Values,Scope,Profile(zone with{Marketable=InventoryPropertyFilter.Any,CanBeHq=filter}),Metadata);
                Check(potential.Slots!.Skip(20).Take(5).OfType<InventoryPreviewStack>().Select(v=>v.Stack.Item.Id.Value).ToHashSet()
                    .SetEquals(filter==InventoryPropertyFilter.Yes ? new uint[]{42,43}:new uint[]{44,45}),"potential HQ independent from actual NQ/HQ");
            }
            var legacy=InventorySortRules.Build(snapshot.Containers.Values,Scope,Profile(new("No",20,5,Marketable:InventoryPropertyFilter.No)),id=>("Legacy "+id,1U));
            Check(legacy.Slots!.Skip(20).Take(5).All(value=>value is null) && legacy.Stacks.Count==5,"old describe callback supplies no false metadata");
        }),
        ("Equipment masks preserve positive native hand slots and exclude negative blocked slots",()=>
        {
            var mask=EquipmentSlotMap.FromNative(-1,1,0,0,0,0,0,0,0,0,1,-1,1);
            Check(mask==(EquipmentSlots.Body|EquipmentSlots.MainHand|EquipmentSlots.SoulCrystal),"positive values only, blocked head and offhand excluded");
            var snapshot=Snapshot();
            foreach(var filter in new[]{EquipmentSlots.MainHand,EquipmentSlots.OffHand,EquipmentSlots.SoulCrystal,EquipmentSlots.MainHand|EquipmentSlots.OffHand})
            {
                var result=InventorySortRules.Build(snapshot.Containers.Values,Scope,Profile(new("Slots",20,5,Equipment:filter)),Metadata);
                var ids=result.Slots!.Skip(20).Take(5).OfType<InventoryPreviewStack>().Select(v=>v.Stack.Item.Id.Value).ToHashSet();
                var expected=filter switch{EquipmentSlots.MainHand=>new uint[]{42,43},EquipmentSlots.OffHand=>new uint[]{45},EquipmentSlots.SoulCrystal=>new uint[]{44},_=>new uint[]{42,43,45}};
                Check(ids.SetEquals(expected),"slot types and multi-slot union use copied native flags");
            }
        }),
        ("Catalogue metadata identity missing row and false values retain distinct evidence",()=>
        {
            var item=new ItemDefinition(42,"Known",false,IsMarketable:false,Equipment:EquipmentSlots.None);
            var known=InventorySortMetadata.FromItem(42,item);var absent=InventorySortMetadata.FromItem(42,null);var wrong=InventorySortMetadata.FromItem(43,item);
            Check(known.Marketable==false && known.CanBeHq==false && known.Equipment==EquipmentSlots.None,"known negatives");
            Check(absent.Marketable is null && absent.CanBeHq is null && absent.Equipment is null && wrong.Marketable is null,"missing or foreign item stays unknown");
            var calls=new Dictionary<uint,int>();var snapshot=Snapshot();var profile=InventorySortRules.Default(Scope) with{Zones=new[]{new InventorySortZone("A",20,3,Marketable:InventoryPropertyFilter.Yes),new InventorySortZone("B",30,3,CanBeHq:InventoryPropertyFilter.No)}};
            InventorySortRules.Build(snapshot.Containers.Values,Scope,profile,id=>{calls[id]=calls.GetValueOrDefault(id)+1;return Metadata(id);});
            Check(calls.Count==5 && calls.Values.All(count=>count==1),"metadata copied once per ID across criteria and zones");
        }),
        ("Advanced filters serialize compatibly and invalid values cannot silently alter an inventory",()=>
        {
            var zone=new InventorySortZone("Gear",20,5,Marketable:InventoryPropertyFilter.No,CanBeHq:InventoryPropertyFilter.Yes,Equipment:EquipmentSlots.MainHand|EquipmentSlots.OffHand);
            var copy=JsonSerializer.Deserialize<InventorySortZone>(JsonSerializer.Serialize(zone))!;
            Check(copy==zone,"all optional filters roundtrip");
            var old=JsonSerializer.Deserialize<InventorySortZone>("{\"Name\":\"Old\",\"Start\":0,\"Count\":5}")!;
            Check(old.Marketable==InventoryPropertyFilter.Any && old.CanBeHq==InventoryPropertyFilter.Any && old.Equipment==EquipmentSlots.None,"v1 defaults do not add new restrictions");
            foreach(var invalid in new[]{zone with{Marketable=(InventoryPropertyFilter)99},zone with{CanBeHq=(InventoryPropertyFilter)(-1)},zone with{Equipment=(EquipmentSlots)8192}})
                Refuses(()=>InventorySortRules.Validate(Profile(invalid),Scope));
            var snapshot=Snapshot();var protectedProfile=Profile(zone with{Start=0,Count=5,Preserve=true});
            var result=InventorySortRules.Build(snapshot.Containers.Values,Scope,protectedProfile,Metadata);
            Check(result.Slots!.Take(5).Select(v=>v!.Stack.Item.Id.Value).SequenceEqual(new uint[]{42,43,44,45,46}),"protected zone ignores inactive metadata filters and keeps unknown item at original position");
        }),
    };
}
