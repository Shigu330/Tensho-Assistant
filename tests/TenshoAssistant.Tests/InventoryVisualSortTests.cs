using TenshoAssistant.Inventory;

internal static class InventoryVisualSortTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Refuses(Action action){try{action();}catch(InvalidOperationException){return;}throw new Exception("Expected rejection");}
    private static readonly InventoryOwnerId Owner=new(7);
    private static InventorySnapshot Snapshot(DateTimeOffset now,IEnumerable<(int Bag,InventoryStack Stack)> rows,InventoryCoverage coverage=InventoryCoverage.Complete)
        =>new(now,"fixture",Enumerable.Range(0,4).Select(bag=>new InventoryContainerSnapshot(new(Owner,new(bag)),coverage,rows.Where(r=>r.Bag==bag).Select(r=>r.Stack))));
    private static readonly (int Bag,InventoryStack Stack)[] Rows=
    [ (0,new(new(new(42),false),8,12,89)), (1,new(new(new(42),true),2,0,3)), (3,new(new(new(43),false),8,34,15)) ];
    private static InventorySortResult Preview(InventorySnapshot snapshot)=>InventorySortPreview.Build(snapshot.Containers.Values,140,InventorySortField.Quantity,true,id=>("Item "+id,1));
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Visual sort is a full physical-slot bijection preserving separate piles and qualities",()=>
        {
            var now=DateTimeOffset.UtcNow;var snapshot=Snapshot(now,Rows);var preview=Preview(snapshot);
            var plan=InventoryVisualSortPlan.Build(snapshot,preview,7);
            Check(plan.Order.Count==140 && plan.Order.Distinct().Count()==140,"includes all empty slots exactly once");
            var occupied=plan.Order.Take(3).ToArray();
            Check(occupied.SequenceEqual(new InventoryPhysicalSlot[]{new(0,12),new(3,34),new(1,0)}),"requested quantity sort and deterministic tie; no merge of same-item HQ");
            Check(plan.Order.All(slot=>slot.Container is >=0 and <4 && slot.Slot is >=0 and <35),"personal bags only");
            Check(plan.Matches(snapshot,7,now) && snapshot.Containers.Values.SelectMany(c=>c.Stacks).Sum(s=>s.Quantity)==18,"original quantities untouched");
        }),
        ("Every inventory sort rule preserves each occupied slot across sparse full and empty bags",()=>
        {
            var now=DateTimeOffset.UtcNow;var rng=new Random(9014);
            foreach(var count in new[]{0,1,35,70,139,140})
            {
                var visual=Enumerable.Range(0,140).OrderBy(_=>rng.Next()).ToArray();
                var rows=Enumerable.Range(0,count).Select(i=>(i/35,new InventoryStack(new(new((uint)(42+i%3)),i%2==0),i%9+1,i%35,visual[i]))).ToArray();
                var snapshot=Snapshot(now,rows);
                foreach(var sortField in Enum.GetValues<InventorySortField>())foreach(var descending in new[]{false,true})
                {
                    var preview=InventorySortPreview.Build(snapshot.Containers.Values,140,sortField,descending,id=>("Item "+id,id%2));
                    var plan=InventoryVisualSortPlan.Build(snapshot,preview,7);
                    var physical=Enumerable.Range(0,140).Select(i=>new InventoryPhysicalSlot(i/35,i%35)).ToHashSet();
                    Check(physical.SetEquals(plan.Order) && plan.Order.Count==140,"independent full-coordinate oracle");
                    Check(plan.Order.Take(count).SequenceEqual(preview.Stacks.Select(row=>new InventoryPhysicalSlot(row.Origin.Container.Value,row.Stack.Slot))),"all preview stacks retain exact physical identity");
                    Check(plan.Matches(snapshot,7,now),"read-only plan");
                }
            }
        }),
        ("Visual sort refuses changed amounts qualities physical slots or visual order even at equal totals",()=>
        {
            var now=DateTimeOffset.UtcNow;var snapshot=Snapshot(now,Rows);var plan=InventoryVisualSortPlan.Build(snapshot,Preview(snapshot),7);
            var mutations=new[]{
                Rows.Select((r,i)=>i==0 ? (r.Bag,r.Stack with{Quantity=9}):r).ToArray(),
                Rows.Select((r,i)=>i==0 ? (r.Bag,r.Stack with{Item=new(new(42),true)}):r).ToArray(),
                Rows.Select((r,i)=>i==0 ? (r.Bag,r.Stack with{Slot=13}):r).ToArray(),
                Rows.Select((r,i)=>i==0 ? (r.Bag,r.Stack with{DisplayIndex=90}):r).ToArray(),
                Rows.Reverse().Skip(1).ToArray(),
            };
            foreach(var rows in mutations)Check(!plan.Matches(Snapshot(now,rows),7,now),"changed evidence rejected");
            Check(!plan.Matches(snapshot,8,now) && !plan.Matches(snapshot,0,now),"foreign or offline");
            Check(!plan.Matches(snapshot,7,now.AddSeconds(3).AddTicks(1)) && !plan.Matches(snapshot,7,now.AddTicks(-1)),"old or future preview");
            Check(plan.Matches(snapshot,7,now.AddSeconds(3)),"three-second inclusive boundary");
        }),
        ("Visual sort admission rejects incomplete ambiguous or forged previews",()=>
        {
            var now=DateTimeOffset.UtcNow;var snapshot=Snapshot(now,Rows);var preview=Preview(snapshot);
            Refuses(()=>InventoryVisualSortPlan.Build(Snapshot(now,Rows,InventoryCoverage.Partial),preview,7));
            Refuses(()=>InventoryVisualSortPlan.Build(snapshot,preview,8));
            Refuses(()=>InventoryVisualSortPlan.Build(snapshot,new(139,preview.Stacks),7));
            Refuses(()=>InventoryVisualSortPlan.Build(snapshot,new(140,preview.Stacks.Take(2).ToArray()),7));
            Refuses(()=>InventoryVisualSortPlan.Build(snapshot,new(140,[preview.Stacks[0],preview.Stacks[0],preview.Stacks[2]]),7));
            Refuses(()=>InventoryVisualSortPlan.Build(Snapshot(now,Rows.Select((r,i)=>i==0 ? (r.Bag,r.Stack with{DisplayIndex=-1}):r)),preview,7));
            Refuses(()=>InventoryVisualSortPlan.Build(Snapshot(now,Rows.Select((r,i)=>i==0 ? (r.Bag,r.Stack with{Slot=35}):r)),preview,7));
            Refuses(()=>InventoryVisualSortPlan.Build(Snapshot(now,Rows.Select((r,i)=>i==0 ? (r.Bag,r.Stack with{DisplayIndex=3}):r)),preview,7));
        }),
        ("Explicit visual sort queue never replaces pending work and ignores old completions",()=>
        {
            var now=DateTimeOffset.UtcNow;var snapshot=Snapshot(now,Rows);var plan=InventoryVisualSortPlan.Build(snapshot,Preview(snapshot),7);
            using var service=new InventoryVisualSortService();Check(!service.State.Pending && service.Take(7,now) is null,"no implicit request");
            Check(service.Request(plan,now) && !service.Request(plan,now),"single pending request");
            var request=service.Take(7,now)!;Check(request.Plan==plan && service.Take(7,now) is null,"single dispatch");
            service.Clear();Check(service.Request(plan,now),"new explicit generation");
            service.Complete(request,"obsolete");Check(service.State.Pending && service.State.Message!="obsolete","late completion discarded");
            var current=service.Take(7,now)!;service.Complete(current,"applied");Check(!service.State.Pending && service.State.Message=="applied","current completion accepted");
        }),
        ("Visual sort request expiry logout and disposal prevent deferred execution",()=>
        {
            var now=DateTimeOffset.UtcNow;var snapshot=Snapshot(now,Rows);var plan=InventoryVisualSortPlan.Build(snapshot,Preview(snapshot),7);
            foreach(var context in new[]{(Owner:0UL,Time:now),(Owner:8UL,Time:now),(Owner:7UL,Time:now.AddSeconds(4)),(Owner:7UL,Time:now.AddTicks(-1))})
            {
                using var service=new InventoryVisualSortService();service.Request(plan,now);
                Check(service.Take(context.Owner,context.Time) is null && !service.State.Pending,"reject changed context and expire");
                Check(service.Take(7,now) is null,"no replay");
            }
            var disposed=new InventoryVisualSortService();disposed.Request(plan,now);disposed.Clear();Check(disposed.Take(7,now) is null,"logout cleared");
            disposed.Request(plan,now);disposed.Dispose();disposed.Dispose();
            Check(!disposed.Request(plan,now) && disposed.Take(7,now) is null && !disposed.State.Pending,"terminal lifecycle");
        }),
    };
}
