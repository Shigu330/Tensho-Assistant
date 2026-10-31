using System.Text.Json;
using TenshoAssistant.Inventory;

internal static class InventoryProtectedSortTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Refuses(Action action){try{action();}catch(InvalidOperationException){return;}throw new Exception("Expected rejection");}
    private static readonly InventorySortScope Scope=InventorySortScope.Personal(7);
    private static readonly DateTimeOffset Now=DateTimeOffset.UtcNow;
    private static (string Name,uint Category) Describe(uint id)=>id switch{42=>("Apple",1),43=>("Banana",1),_=>("Ore",2)};
    private static InventorySnapshot Snapshot()=>new(Now,"fixture",Enumerable.Range(0,4).Select(bag=>new InventoryContainerSnapshot(new(new(7),new(bag)),InventoryCoverage.Complete,
        bag==0 ? new InventoryStack[]{new(new(new(42),false),10,0,4),new(new(new(43),true),2,6,9),new(new(new(44),false),8,11,12),new(new(new(42),true),3,15,17)}:[])));
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Protected visual positions take precedence over earlier filters and preserve holes not guessed physical indices",()=>
        {
            var snapshot=Snapshot();var profile=InventorySortRules.Default(Scope) with{Zones=new[]{new InventorySortZone("Fruit",0,2,1),new InventorySortZone("Keep",8,6,Preserve:true)}};
            var preview=InventorySortRules.Build(snapshot.Containers.Values,Scope,profile,Describe);
            Check(preview.Slots![9]?.Stack.Item.Id.Value==43 && preview.Slots[12]?.Stack.Item.Id.Value==44,"protected objects kept at visual indices even when filters could claim them");
            foreach(var index in new[]{8,10,11,13})Check(preview.Slots[index] is null,"protected empty cell stays empty");
            Check(preview.Slots[0]?.Stack.Item==new InventoryItem(new(42),false) && preview.Slots[1]?.Stack.Item==new InventoryItem(new(42),true),"filter receives only unprotected fruit");
            var plan=InventoryVisualSortPlan.Build(snapshot,preview,Scope);
            Check(plan.Order[9]==new InventoryPhysicalSlot(0,6) && plan.Order[12]==new InventoryPhysicalSlot(0,11),"visual positions retain correct different physical slots");
            Check(plan.Order[11]!=new InventoryPhysicalSlot(0,11) && plan.Order.Distinct().Count()==140,"a visual empty cell maps to a true empty physical slot, never the occupied same-number slot");
        }),
        ("Reverse fill starts at the last reserved cell and preserves remaining leading holes",()=>
        {
            var snapshot=Snapshot();var profile=InventorySortRules.Default(Scope) with{Zones=new[]{new InventorySortZone("Reverse fruit",20,4,1,ReverseFill:true)}};
            var result=InventorySortRules.Build(snapshot.Containers.Values,Scope,profile,Describe);
            Check(result.Slots![23]?.Stack.Item==new InventoryItem(new(42),false) && result.Slots[22]?.Stack.Item==new InventoryItem(new(42),true)
                && result.Slots[21]?.Stack.Item.Id.Value==43 && result.Slots[20] is null,"sorted candidate order placed from final cell backward");
            Check(result.Slots[0]?.Stack.Item.Id.Value==44 && result.Stacks.Sum(value=>value.Stack.Quantity)==23,"unrelated material and complete quantities survive");
            var plan=InventoryVisualSortPlan.Build(snapshot,result,Scope);Check(plan.Order[23]==new InventoryPhysicalSlot(0,0),"reverse placement reaches shared native permutation");
        }),
        ("Protection refuses missing or ambiguous visual evidence instead of compacting an old cache",()=>
        {
            var snapshot=Snapshot();var profile=InventorySortRules.Default(Scope) with{Zones=new[]{new InventorySortZone("Keep",0,35,Preserve:true)}};
            foreach(var display in new[]{-1,140,9})
            {
                var containers=snapshot.Containers.Values.Select(c=>new InventoryContainerSnapshot(c.Location,c.Coverage,c.Stacks.Select(s=>s.Item==new InventoryItem(new(42),false) ? s with{DisplayIndex=display}:s))).ToArray();
                Refuses(()=>InventorySortRules.Build(containers,Scope,profile,Describe));
            }
            var missing=snapshot.Containers.Values.Take(3).ToArray();Refuses(()=>InventorySortRules.Build(missing,Scope,profile,Describe));
            var old=snapshot.Containers.Values.Select(c=>new InventoryContainerSnapshot(c.Location,c.Coverage,c.Stacks.Select(s=>s with{DisplayIndex=-1}))).ToArray();
            Check(InventorySortRules.Build(old,Scope,InventorySortRules.Default(Scope),Describe).Stacks.Count==4,"legacy unprotected read-only preview remains available");
        }),
        ("Protected and reverse settings survive JSON while old zone defaults stay compatible",()=>
        {
            var legacy=JsonSerializer.Deserialize<InventorySortZone>("{\"Name\":\"Old\",\"Start\":0,\"Count\":5}")!;
            Check(!legacy.Preserve && !legacy.ReverseFill,"old zones retain forward filtered behavior");
            var profile=InventorySortRules.Default(Scope) with{Zones=new[]{new InventorySortZone("Keep",0,5,Preserve:true),new InventorySortZone("Reverse",10,5,ReverseFill:true)}};
            var copy=JsonSerializer.Deserialize<InventorySortProfile>(JsonSerializer.Serialize(profile))!;
            InventorySortRules.Validate(copy,Scope);
            Check(copy.Zones[0].Preserve && copy.Zones[1].ReverseFill && !copy.Zones[1].Preserve,"independent modes roundtrip");
            Refuses(()=>InventorySortRules.Validate(profile with{Zones=new[]{new InventorySortZone("Impossible",0,5,Preserve:true,ReverseFill:true)}},Scope));
        }),
        ("Protected holes and reverse filters preserve exact native coordinates across all four storage scopes",()=>
        {
            var scopes=new[]{Scope,InventorySortScope.FromStorage(new(7,BankStorageKind.Saddlebag,0)),InventorySortScope.FromStorage(new(7,BankStorageKind.PremiumSaddlebag,0)),InventorySortScope.FromStorage(new(7,BankStorageKind.Retainer,91))};
            foreach(var scope in scopes)foreach(var count in new[]{0,1,scope.Capacity/2,scope.Capacity-5,scope.Capacity})
            {
                var random=new Random(scope.Capacity+count);var visual=Enumerable.Range(0,scope.Capacity).OrderBy(_=>random.Next()).ToArray();
                var containers=Enumerable.Range(0,scope.ContainerCount).Select(page=>new InventoryContainerSnapshot(new(new(scope.InventoryOwner),new(scope.FirstContainer+page)),InventoryCoverage.Complete,
                    Enumerable.Range(page*scope.SlotsPerContainer,scope.SlotsPerContainer).Where(i=>i<count).Select(i=>new InventoryStack(new(new((uint)(42+i%3)),i%2!=0),i+1,i%scope.SlotsPerContainer,visual[i])))).ToArray();
                var snapshot=new InventorySnapshot(Now,"fixture",containers);var keep=new InventorySortZone("Keep",scope.Capacity/2,5,Preserve:true);
                var zone=new InventorySortZone("Reverse HQ fruit",0,3,1,InventoryQualityFilter.HighQuality,ReverseFill:true);
                var profile=InventorySortRules.Default(scope) with{Zones=new[]{zone,keep}};
                var occupied=containers.SelectMany(c=>c.Stacks).ToDictionary(s=>s.DisplayIndex);
                bool Frozen(InventoryStack stack)=>stack.DisplayIndex>=keep.Start && stack.DisplayIndex<keep.Start+keep.Count;
                var fixedCount=occupied.Values.Count(Frozen);var matching=occupied.Values.Count(s=>!Frozen(s) && s.Item.IsHighQuality && s.Item.Id.Value is 42 or 43);
                if(count-fixedCount-Math.Min(zone.Count,matching)>scope.Capacity-keep.Count-zone.Count)
                {Refuses(()=>InventorySortRules.Build(containers,scope,profile,Describe));continue;}
                var result=InventorySortRules.Build(containers,scope,profile,Describe);var plan=InventoryVisualSortPlan.Build(snapshot,result,scope);
                for(var index=keep.Start;index<keep.Start+keep.Count;index++)
                    Check(occupied.TryGetValue(index,out var before) ? result.Slots![index]?.Stack==before:result.Slots![index] is null,"each originally occupied or empty protected visual cell unchanged");
                var physical=containers.SelectMany(c=>c.Stacks.Select(s=>(Key:new InventoryPhysicalSlot(c.Location.Container.Value,s.Slot),Stack:s))).ToDictionary(v=>v.Key,v=>v.Stack);
                for(var target=0;target<scope.Capacity;target++)
                    Check(result.Slots![target] is { } expected ? physical.TryGetValue(plan.Order[target],out var actual) && actual==expected.Stack:!physical.ContainsKey(plan.Order[target]),"every target maps to its exact original pile or true empty slot");
                Check(plan.Order.Distinct().Count()==scope.Capacity && result.Stacks.Sum(s=>s.Stack.Quantity)==(long)count*(count+1)/2,"independent permutation and quantity oracles");
            }
        }),
    };
}
