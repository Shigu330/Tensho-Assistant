using TenshoAssistant.Inventory;

internal static class InventorySortScopeTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Refuses(Action action){try{action();}catch(InvalidOperationException){return;}throw new Exception("Expected rejection");}
    private static InventorySortScope Scope(BankStorageKind kind)=>InventorySortScope.FromStorage(new(7,kind,kind==BankStorageKind.Retainer ? 91UL:0UL));
    private static InventorySnapshot Snapshot(InventorySortScope scope,DateTimeOffset now,int count)
    {
        var indices=Enumerable.Range(0,scope.Capacity).Reverse().ToArray();
        return new(now,"fixture/live",Enumerable.Range(0,scope.ContainerCount).Select(page=>new InventoryContainerSnapshot(
            new(new(scope.InventoryOwner),new(scope.FirstContainer+page)),InventoryCoverage.Complete,
            Enumerable.Range(page*scope.SlotsPerContainer,scope.SlotsPerContainer).Where(i=>i<count)
                .Select(i=>new InventoryStack(new(new((uint)(42+i%5)),i%2!=0),i%11+1,i%scope.SlotsPerContainer,indices[i])))));
    }
    private static InventorySortResult Preview(InventorySortScope scope,InventorySnapshot snapshot,InventorySortField sort=InventorySortField.Name,bool descending=false)
        =>InventorySortPreview.Build(snapshot.Containers.Values,scope.Capacity,sort,descending,id=>("Item "+id,id%3));
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Sort scopes separate logged character from native retainer owner and exact storage geometry",()=>
        {
            var personal=InventorySortScope.Personal(7);Check(personal.Capacity==140 && personal.FirstContainer==0 && personal.ContainerCount==4 && personal.InventoryOwner==7,"personal");
            var retainer=Scope(BankStorageKind.Retainer);Check(retainer.CharacterId==7 && retainer.InventoryOwner==91 && retainer.FirstContainer==10000 && retainer.ContainerCount==7 && retainer.SlotsPerContainer==25 && retainer.Capacity==175,"retainer seven native containers");
            var saddle=Scope(BankStorageKind.Saddlebag);var premium=Scope(BankStorageKind.PremiumSaddlebag);
            Check(saddle.FirstContainer==4000 && premium.FirstContainer==4100 && saddle.Capacity==70 && premium.Capacity==70 && saddle.InventoryOwner==7 && saddle!=premium,"separate saddle scopes");
            Refuses(()=>InventorySortScope.FromStorage(new(0,BankStorageKind.Saddlebag,0)));
            Refuses(()=>InventorySortScope.FromStorage(new(7,BankStorageKind.Retainer,0)));
            Refuses(()=>InventorySortScope.FromStorage(new(7,BankStorageKind.Saddlebag,91)));
            Refuses(()=>InventorySortScope.FromStorage(new(7,(BankStorageKind)99,0)));
        }),
        ("Scoped sort permutations preserve all coordinates and piles for every storage and criterion",()=>
        {
            var now=DateTimeOffset.UtcNow;
            foreach(var kind in Enum.GetValues<BankStorageKind>())
            {
                var scope=Scope(kind);
                foreach(var count in new[]{0,1,scope.SlotsPerContainer,scope.Capacity/2,scope.Capacity-1,scope.Capacity})
                foreach(var sortField in Enum.GetValues<InventorySortField>())foreach(var descending in new[]{false,true})
                {
                    var snapshot=Snapshot(scope,now,count);var preview=Preview(scope,snapshot,sortField,descending);
                    var plan=InventoryVisualSortPlan.Build(snapshot,preview,scope);
                    var all=Enumerable.Range(0,scope.ContainerCount).SelectMany(page=>Enumerable.Range(0,scope.SlotsPerContainer)
                        .Select(slot=>new InventoryPhysicalSlot(scope.FirstContainer+page,slot))).ToHashSet();
                    Check(plan.Order.Count==scope.Capacity && all.SetEquals(plan.Order),"independent full physical coordinate oracle");
                    Check(plan.Order.Take(count).SequenceEqual(preview.Stacks.Select(s=>new InventoryPhysicalSlot(s.Origin.Container.Value,s.Stack.Slot))),"separate original piles in preview order");
                    Check(plan.Matches(snapshot,7,now) && plan.Owner==7 && plan.Scope==scope,"native owner never mistaken for logged character");
                }
            }
        }),
        ("A bank cache cannot authorize a native sort even when its timestamp and source string look live",()=>
        {
            var now=DateTimeOffset.UtcNow;var scope=Scope(BankStorageKind.Retainer);var snapshot=Snapshot(scope,now,3);
            var plan=InventoryVisualSortPlan.Build(snapshot,Preview(scope,snapshot),scope);
            using var service=new InventoryVisualSortService();
            Check(!service.Request(plan,now) && !service.State.Pending,"no capture receipt, no request");
            service.RequestCapture(scope,now);var capture=service.TakeCapture(7,now)!;
            service.CompleteCapture(capture,snapshot,"Retainer",now);
            var copied=new InventorySnapshot(now,snapshot.Source,snapshot.Containers.Values);
            Check(!service.Request(InventoryVisualSortPlan.Build(copied,Preview(scope,copied),scope),now),"identical cache clone is not the direct snapshot receipt");
            Check(service.Request(plan,now),"exact published direct snapshot accepted");
        }),
        ("A direct preview permits inspection but is revalidated against a fresh identical storage snapshot",()=>
        {
            var now=DateTimeOffset.UtcNow;var scope=Scope(BankStorageKind.Saddlebag);var snapshot=Snapshot(scope,now,3);
            var plan=InventoryVisualSortPlan.Build(snapshot,Preview(scope,snapshot),scope);
            Check(plan.Matches(Snapshot(scope,now.AddSeconds(119),3),7,now.AddSeconds(119)),"two-minute inspection window with fresh actual evidence");
            Check(!plan.Matches(Snapshot(scope,now.AddSeconds(121),3),7,now.AddSeconds(121)),"preview expired");
            Check(!plan.Matches(snapshot,7,now.AddSeconds(119)),"old actual evidence never accepted");
            Check(!plan.Matches(Snapshot(scope,now.AddSeconds(119),4),7,now.AddSeconds(119)),"new pile since preview rejected");
            Check(!plan.Matches(Snapshot(Scope(BankStorageKind.PremiumSaddlebag),now,3),7,now),"normal and premium never exchange coordinates");
            Check(!plan.Matches(Snapshot(scope,now,3),91,now),"storage owner is not login owner");
        }),
        ("Capture queues reject foreign owners late completions changed storage and logout reuse",()=>
        {
            var now=DateTimeOffset.UtcNow;var first=Scope(BankStorageKind.Retainer);var other=Scope(BankStorageKind.Saddlebag);
            using var service=new InventoryVisualSortService();
            Check(!service.RequestCapture(InventorySortScope.Personal(7),now),"personal uses shared live reader");
            Check(service.RequestCapture(first,now) && !service.RequestCapture(other,now),"one capture in flight");
            var old=service.TakeCapture(7,now)!;service.Clear();service.RequestCapture(other,now);
            var current=service.TakeCapture(7,now)!;
            service.CompleteCapture(old,Snapshot(first,now,2),"Old",now);
            Check(service.State.Pending && service.State.Capture is null,"old capture cannot publish into newer scope");
            service.CompleteCapture(current,Snapshot(other,now,2),"Current",now);
            Check(service.State.Capture?.Scope==other && !service.State.Pending,"current scope published");
            var wrong=Snapshot(first,now,2);Check(!service.Request(InventoryVisualSortPlan.Build(wrong,Preview(first,wrong),first),now),"another storage has no current receipt");
            service.Clear();Check(service.State.Capture is null,"logout removes direct receipt");
            service.RequestCapture(first,now);Check(service.TakeCapture(91,now) is null && !service.State.Pending,"retainer ID cannot satisfy character scope");
        }),
        ("Capture failures incomplete inventory disposal and expiry never leave an applicable preview",()=>
        {
            var now=DateTimeOffset.UtcNow;var scope=Scope(BankStorageKind.PremiumSaddlebag);
            using var service=new InventoryVisualSortService();service.RequestCapture(scope,now);var capture=service.TakeCapture(7,now)!;
            service.CompleteCapture(capture,null,"",now,"Closed");Check(service.State.Capture is null && !service.State.Pending && service.State.Message=="Closed","native failure clears prior capture");
            service.RequestCapture(scope,now);var invalid=service.TakeCapture(7,now)!;
            var partial=new InventorySnapshot(now,"fixture",Snapshot(scope,now,2).Containers.Values.Select(c=>new InventoryContainerSnapshot(c.Location,InventoryCoverage.Partial,c.Stacks)));
            Refuses(()=>service.CompleteCapture(invalid,partial,"Bad",now));
            service.CompleteCapture(invalid,null,"",now,"Incomplete");Check(!service.State.Pending && service.State.Capture is null,"failed validation can be completed as error");
            service.RequestCapture(scope,now);Check(service.TakeCapture(7,now.AddSeconds(4)) is null && !service.State.Pending,"capture request expires");
            service.RequestCapture(scope,now);var disposed=service.TakeCapture(7,now)!;service.Dispose();
            service.CompleteCapture(disposed,Snapshot(scope,now,2),"Late",now);
            Check(!service.RequestCapture(scope,now) && service.State.Capture is null,"disposed bridge cannot publish snapshot");
        }),
    };
}
