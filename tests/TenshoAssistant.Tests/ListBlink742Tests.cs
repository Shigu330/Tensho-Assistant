using TenshoAssistant.Inventory;

// 0.742 (user report): the blink came back after 0.741: a reading made during the change found the bags unreadable.
internal static class ListBlink742Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly InventoryOwnerId Owner=new(1);
    private static InventoryContainerSnapshot Bag(int id,InventoryCoverage coverage,long quantity)=>
        new(new(Owner,new(id)),coverage,quantity>0 ? [new InventoryStack(new(new(5111),false),quantity,0)]:[]);
    private static InventorySnapshot Read(params InventoryContainerSnapshot[] bags)=>new(DateTimeOffset.UtcNow,"test",bags);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("List blink: an unreadable bag keeps its last complete reading on screen; what acts sees what was read",() =>
        {
            var service=new InventoryService();
            service.Replace(Read(Bag(0,InventoryCoverage.Complete,345),Bag(1,InventoryCoverage.Complete,0)));
            service.Replace(Read(Bag(0,InventoryCoverage.Unknown,0),Bag(1,InventoryCoverage.Complete,0)));
            var shown=service.Shown!.Containers[new(Owner,new(0))];
            Check(shown.Coverage==InventoryCoverage.Complete && shown.Quantities.Values.Sum()==345,"345 stays, not 0");
            Check(service.Current!.Containers[new(Owner,new(0))].Coverage==InventoryCoverage.Unknown,"the sort sees it unknown");
            service.Replace(Read(Bag(0,InventoryCoverage.Complete,346),Bag(1,InventoryCoverage.Complete,0)));
            Check(service.Shown!.Containers[new(Owner,new(0))].Quantities.Values.Sum()==346,"the next complete reading replaces it");
        }),
        ("List blink: nothing is kept from before a clear, and a bag never read stays unknown",() =>
        {
            var service=new InventoryService();
            service.Replace(Read(Bag(0,InventoryCoverage.Complete,345)));
            service.Clear();
            service.Replace(Read(Bag(0,InventoryCoverage.Unknown,0)));
            Check(service.Shown!.Containers.Values.Single().Coverage==InventoryCoverage.Unknown,"after a logout or another character");
            var fresh=Read(Bag(2,InventoryCoverage.Partial,3));
            Check(ReferenceEquals(InventoryService.ForDisplay(Read(Bag(0,InventoryCoverage.Complete,1)),fresh),fresh),"no complete reading of that bag: as read");
        }),
    ];
}
