namespace TenshoAssistant.Inventory;

public sealed record InventorySortScope
{
    public ulong CharacterId {get;}
    public BankKey? Storage {get;}
    public ulong InventoryOwner=>Storage is {Kind:BankStorageKind.Retainer} key ? key.StorageId:CharacterId;
    public int FirstContainer=>Storage?.Kind switch{BankStorageKind.Saddlebag=>4000,BankStorageKind.PremiumSaddlebag=>4100,BankStorageKind.Retainer=>10000,_=>0};
    public int ContainerCount=>Storage?.Kind switch{BankStorageKind.Retainer=>7,null=>4,_=>2};
    public int SlotsPerContainer=>Storage?.Kind==BankStorageKind.Retainer ? 25:35;
    public int Capacity=>ContainerCount*SlotsPerContainer;
    public TimeSpan PreviewLifetime=>Storage is null ? TimeSpan.FromSeconds(3):TimeSpan.FromMinutes(2);
    private InventorySortScope(ulong character,BankKey? storage){CharacterId=character;Storage=storage;}
    public static InventorySortScope Personal(ulong character)=>character!=0 ? new(character,null):throw new InvalidOperationException("Personnage non observé.");
    public static InventorySortScope FromStorage(BankKey key)
    {
        if(key.CharacterId==0 || !Enum.IsDefined(key.Kind) || (key.Kind==BankStorageKind.Retainer ? key.StorageId==0:key.StorageId!=0))
            throw new InvalidOperationException("Identité de rangement invalide.");
        return new(key.CharacterId,key);
    }
    internal Dictionary<InventoryPhysicalSlot,InventoryStack> Extract(InventorySnapshot snapshot)
        =>Extract(snapshot.Containers.Values);
    internal Dictionary<InventoryPhysicalSlot,InventoryStack> Extract(IEnumerable<InventoryContainerSnapshot> source)
    {
        var all=source.ToArray();
        var containers=all.Where(c=>c.Location.Container.Value>=FirstContainer && c.Location.Container.Value<FirstContainer+ContainerCount).ToArray();
        if(containers.Length!=ContainerCount || containers.Any(c=>c.Location.Owner.Value!=InventoryOwner || c.Coverage!=InventoryCoverage.Complete)
            || Storage is not null && all.Length!=ContainerCount)
            throw new InvalidOperationException("Tous les conteneurs du rangement doivent être entièrement observés pour le bon propriétaire.");
        var values=new Dictionary<InventoryPhysicalSlot,InventoryStack>();var display=new HashSet<int>();
        foreach(var container in containers)
        foreach(var stack in container.Stacks)
        {
            if(stack.Slot<0 || stack.Slot>=SlotsPerContainer || stack.DisplayIndex<0 || stack.DisplayIndex>=Capacity || !display.Add(stack.DisplayIndex))
                throw new InvalidOperationException("Ordre visuel du rangement incomplet ou incohérent.");
            values.Add(new(container.Location.Container.Value,stack.Slot),stack);
        }
        return values;
    }
}

public sealed record InventorySortCapture(InventorySortScope Scope,InventorySnapshot Snapshot,string Name)
{
    public bool Fresh(ulong character,DateTimeOffset now)=>character==Scope.CharacterId && now>=Snapshot.ObservedAt && now-Snapshot.ObservedAt<=Scope.PreviewLifetime;
}
public sealed record InventorySortCaptureRequest(long Revision,InventorySortScope Scope,DateTimeOffset RequestedAt);
