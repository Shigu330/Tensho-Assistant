using System.Text.Json;
using TenshoAssistant;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;

internal static class InventorySortRulesTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Refuses(Action action){try{action();}catch(InvalidOperationException){return;}throw new Exception("Expected rejection");}
    private static readonly InventorySortScope Scope=InventorySortScope.Personal(7);
    private static readonly DateTimeOffset Now=DateTimeOffset.UtcNow;
    private static (string Name,uint Category) Describe(uint id)=>id switch{42=>("Apple",1),43=>("Banana",1),44=>("Ore",2),_=>("Unknown",0)};
    private static InventorySnapshot Snapshot()=>new(Now,"fixture",Enumerable.Range(0,4).Select(bag=>new InventoryContainerSnapshot(new(new(7),new(bag)),InventoryCoverage.Complete,
        bag==0 ? new InventoryStack[]{new(new(new(42),false),10,4,4),new(new(new(43),true),2,9,9),new(new(new(44),false),8,12,12),new(new(new(42),true),3,17,17)}:[])));
    private static InventorySortProfile Profile(InventorySortZone? first=null,InventorySortZone? second=null)
        =>InventorySortRules.Default(Scope) with{Zones=Array.AsReadOnly(new[]{first,second}.OfType<InventorySortZone>().ToArray())};
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Reserved zone filters combine native category HQ and text while keeping holes and all other piles",()=>
        {
            var snapshot=Snapshot();var profile=Profile(new("HQ fruit",35,7,1,InventoryQualityFilter.HighQuality,"apple"));
            var preview=InventorySortRules.Build(snapshot.Containers.Values,Scope,profile,Describe);
            Check(preview.Slots?.Count==140 && preview.Stacks.Count==4,"all stacks plus explicit empty positions");
            Check(preview.Slots![35]?.Stack.Item==new InventoryItem(new(42),true) && preview.Slots.Skip(36).Take(6).All(value=>value is null),"all filters must match, unmatched zone remains empty");
            Check(preview.Slots[0]?.Stack.Item==new InventoryItem(new(42),false) && preview.Slots[1]?.Stack.Item.Id.Value==43 && preview.Slots[2]?.Stack.Item.Id.Value==44,"other stacks fill only unreserved slots");
            var plan=InventoryVisualSortPlan.Build(snapshot,preview,Scope);
            Check(plan.Order[35]==new InventoryPhysicalSlot(0,17) && plan.Order.Distinct().Count()==140,"native permutation retains the reserved visual target including holes");
            Check(plan.Matches(snapshot,7,Now),"source evidence unchanged");
        }),
        ("Zone list priority decides which matching stacks are claimed and overflow remains visible",()=>
        {
            var snapshot=Snapshot();var first=new InventorySortZone("First fruit",10,1,1);var second=new InventorySortZone("Remaining fruit",20,1,1);
            var preview=InventorySortRules.Build(snapshot.Containers.Values,Scope,Profile(first,second),Describe);
            Check(preview.Slots![10]?.Stack.Item==new InventoryItem(new(42),false) && preview.Slots[20]?.Stack.Item==new InventoryItem(new(42),true),"first list entry claims first sorted matching pile");
            Check(preview.Slots[0]?.Stack.Item.Id.Value==43 && preview.Slots[1]?.Stack.Item.Id.Value==44,"matching overflow and unrelated material survive outside reserved slots");
            var reversed=InventorySortRules.Build(snapshot.Containers.Values,Scope,Profile(second,first),Describe);
            Check(reversed.Slots![20]?.Stack.Item==new InventoryItem(new(42),false),"changing priority changes placement without changing physical piles");
        }),
        ("Multiple sort criteria apply in declared order and keep deterministic independent pile identities",()=>
        {
            var snapshot=Snapshot();var profile=Profile() with{Criteria=new[]{new InventorySortCriterion(InventorySortField.Category),new InventorySortCriterion(InventorySortField.Quantity,true)}};
            var preview=InventorySortRules.Build(snapshot.Containers.Values,Scope,profile,Describe);
            Check(preview.Stacks.Select(value=>value.Stack.Quantity).SequenceEqual(new long[]{10,3,2,8}),"category primary, quantity descending secondary");
            var quality=profile with{Criteria=new[]{new InventorySortCriterion(InventorySortField.Quality,true),new InventorySortCriterion(InventorySortField.Name,true)}};
            Check(InventorySortRules.Build(snapshot.Containers.Values,Scope,quality,Describe).Stacks.Select(value=>value.Stack.Slot).SequenceEqual(new[]{9,17,12,4}),"HQ primary, reverse name secondary");
        }),
        ("Invalid zones overlap overflow bounds duplicate criteria and insufficient unreserved capacity refuse preview",()=>
        {
            var snapshot=Snapshot();
            foreach(var profile in new[]{Profile(new("A",0,3),new("B",2,3)),Profile(new("A",139,2)),Profile(new("A",-1,1)),Profile(new("A",0,int.MaxValue)),Profile(new("",0,1)),Profile(new("A",0,1,Quality:(InventoryQualityFilter)99)),
                Profile() with{Criteria=[]},Profile() with{Criteria=new[]{new InventorySortCriterion(InventorySortField.Name),new InventorySortCriterion(InventorySortField.Name,true)}}})
                Refuses(()=>InventorySortRules.Build(snapshot.Containers.Values,Scope,profile,Describe));
            Refuses(()=>InventorySortRules.Build(snapshot.Containers.Values,Scope,Profile(new("No fruit allowed",0,139,99)),Describe));
            Check(snapshot.Containers.Values.SelectMany(c=>c.Stacks).Sum(s=>s.Quantity)==23,"failure never changes input piles");
        }),
        ("Positioned previews cannot forge drop duplicate or change stacks before native permutation",()=>
        {
            var snapshot=Snapshot();var preview=InventorySortRules.Build(snapshot.Containers.Values,Scope,Profile(new("HQ",35,7,Quality:InventoryQualityFilter.HighQuality)),Describe);
            var missing=preview.Slots!.ToArray();missing[35]=null;
            Refuses(()=>InventoryVisualSortPlan.Build(snapshot,preview with{Slots=missing},Scope));
            var duplicate=preview.Slots!.ToArray();duplicate[36]=duplicate[35];
            Refuses(()=>InventoryVisualSortPlan.Build(snapshot,preview with{Slots=duplicate},Scope));
            var altered=preview.Slots!.ToArray();altered[35]=altered[35]! with{Stack=altered[35]!.Stack with{Quantity=999}};
            Refuses(()=>InventoryVisualSortPlan.Build(snapshot,preview with{Slots=altered},Scope));
            Refuses(()=>InventoryVisualSortPlan.Build(snapshot,preview with{Slots=preview.Slots!.Take(139).ToArray()},Scope));
        }),
        ("Profiles preserve foreign characters retainers and storage defaults across save and v1 serialization",()=>
        {
            var foreign=InventorySortRules.Default(InventorySortScope.Personal(8));var retainer=InventorySortRules.Default(InventorySortScope.FromStorage(new(7,BankStorageKind.Retainer,91)));
            var profile=Profile(new("Fruit",0,5,1));
            var saved=InventorySortRules.Save(new[]{foreign,retainer},profile,Scope);
            Check(saved.Count==3 && saved.Contains(foreign) && saved.Contains(retainer),"other scopes preserved");
            var configuration=new Configuration{InventorySortProfiles=saved};
            var decoded=JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(configuration))!;
            var loaded=InventorySortRules.Load(decoded.InventorySortProfiles,Scope,InventorySortField.Quantity,true);
            Check(loaded.Criteria[0].Field==InventorySortField.Name && loaded.Zones[0].CategoryId==1 && loaded.Zones[0].Count==5,"v1 nested profile roundtrip");
            var legacy=JsonSerializer.Deserialize<Configuration>("{\"Version\":1,\"SortPreviewField\":2,\"SortPreviewDescending\":true}")!;
            var fallback=InventorySortRules.Load(legacy.InventorySortProfiles,Scope,(InventorySortField)legacy.SortPreviewField,legacy.SortPreviewDescending);
            Check(fallback.Criteria.Single()==new InventorySortCriterion(InventorySortField.Quantity,true) && fallback.Zones.Count==0,"legacy rule defaults with no implicit zones");
            Refuses(()=>InventorySortRules.Load(new[]{profile,profile},Scope,InventorySortField.Name,false));
        }),
        ("Profile copy owns nested producer lists and categories come from immutable shared game metadata",()=>
        {
            var criteria=new List<InventorySortCriterion>{new(InventorySortField.Name)};var zones=new List<InventorySortZone>{new("Fruit",0,5,1)};
            var saved=InventorySortRules.Save(null,new(7,null,0,criteria,zones),Scope);criteria.Clear();zones.Clear();
            Check(saved[0].Criteria.Count==1 && saved[0].Zones.Count==1,"saved nested lists owned");
            var catalog=ItemCatalog.Build(new ItemDefinition[]{new(42,"Apple",true,UiCategoryId:12,UiCategoryName:"Fruits du jeu"),new(43,"Banana",true,UiCategoryId:12,UiCategoryName:"Fruits du jeu"),new(44,"Ore",false,UiCategoryId:22,UiCategoryName:"Minerais du jeu"),new(45,"Unknown",false)},new("fixture","French","v",1));
            Check(catalog.Categories.Count==2 && catalog.Categories.Single(value=>value.Id==12).Name=="Fruits du jeu","native IDs and labels deduplicated, unknown category not invented");
        }),
        ("Reserved holes map to truly empty physical slots across personal saddle and retainer scopes",()=>
        {
            var scopes=new[]{Scope,InventorySortScope.FromStorage(new(7,BankStorageKind.Saddlebag,0)),InventorySortScope.FromStorage(new(7,BankStorageKind.PremiumSaddlebag,0)),InventorySortScope.FromStorage(new(7,BankStorageKind.Retainer,91))};
            foreach(var scope in scopes)
            foreach(var count in new[]{0,1,scope.Capacity/3,scope.Capacity/2,scope.Capacity-5,scope.Capacity})
            {
                var random=new Random(scope.Capacity+count);var visual=Enumerable.Range(0,scope.Capacity).OrderBy(_=>random.Next()).ToArray();
                var snapshot=new InventorySnapshot(Now,"fixture",Enumerable.Range(0,scope.ContainerCount).Select(page=>new InventoryContainerSnapshot(new(new(scope.InventoryOwner),new(scope.FirstContainer+page)),InventoryCoverage.Complete,
                    Enumerable.Range(page*scope.SlotsPerContainer,scope.SlotsPerContainer).Where(i=>i<count)
                        .Select(i=>new InventoryStack(new(new((uint)(42+i%3)),i%2!=0),i+1,i%scope.SlotsPerContainer,visual[i])))));
                var zone=new InventorySortZone("HQ fruit",scope.Capacity-5,5,1,InventoryQualityFilter.HighQuality);
                var profile=InventorySortRules.Default(scope) with{Zones=new[]{zone}};
                var physical=snapshot.Containers.Values.SelectMany(c=>c.Stacks.Select(s=>(Key:new InventoryPhysicalSlot(c.Location.Container.Value,s.Slot),Stack:s))).ToDictionary(v=>v.Key,v=>v.Stack);
                var matching=physical.Values.Count(s=>s.Item.IsHighQuality && s.Item.Id.Value is 42 or 43);
                if(count-Math.Min(5,matching)>scope.Capacity-5)
                {Refuses(()=>InventorySortRules.Build(snapshot.Containers.Values,scope,profile,Describe));continue;}
                var preview=InventorySortRules.Build(snapshot.Containers.Values,scope,profile,Describe);var plan=InventoryVisualSortPlan.Build(snapshot,preview,scope);
                Check(plan.Order.Count==scope.Capacity && plan.Order.Distinct().Count()==scope.Capacity,"whole native table permutation");
                for(var target=0;target<scope.Capacity;target++)
                {
                    var actual=physical.GetValueOrDefault(plan.Order[target]);var expected=preview.Slots![target];
                    Check(expected is null ? !physical.ContainsKey(plan.Order[target]):actual==expected.Stack,"each rendered hole is physically empty and each occupied cell keeps its exact pile");
                }
                Check(preview.Stacks.Count==count && preview.Stacks.Sum(v=>v.Stack.Quantity)==(long)count*(count+1)/2,"independent occupied-count and triangular quantity oracle");
            }
        }),
    };
}
