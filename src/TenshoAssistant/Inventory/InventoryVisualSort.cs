namespace TenshoAssistant.Inventory;

public readonly record struct InventoryPhysicalSlot(int Container,int Slot);

/// <summary>A complete visual permutation within one live storage scope. Physical piles remain unchanged.</summary>
public sealed class InventoryVisualSortPlan
{
    public ulong Owner=>Scope.CharacterId;
    public InventorySortScope Scope {get;}
    public InventorySnapshot Expected {get;}
    public IReadOnlyList<InventoryPhysicalSlot> Order {get;}
    private InventoryVisualSortPlan(InventorySortScope scope,InventorySnapshot expected,InventoryPhysicalSlot[] order)
    {Scope=scope;Expected=expected;Order=Array.AsReadOnly(order);}

    public static InventoryVisualSortPlan Build(InventorySnapshot expected,InventorySortResult preview,ulong owner)
        =>Build(expected,preview,InventorySortScope.Personal(owner));
    public static InventoryVisualSortPlan Build(InventorySnapshot expected,InventorySortResult preview,InventorySortScope scope)
    {
        var physical=scope.Extract(expected);
        if(preview.Capacity!=scope.Capacity || preview.Stacks.Count!=physical.Count)throw new InvalidOperationException("Aperçu incompatible avec le rangement.");
        var order=new List<InventoryPhysicalSlot>(scope.Capacity);var seen=new HashSet<InventoryPhysicalSlot>();
        foreach(var value in preview.Stacks)
        {
            var slot=new InventoryPhysicalSlot(value.Origin.Container.Value,value.Stack.Slot);
            if(value.Origin.Owner.Value!=scope.InventoryOwner || !physical.TryGetValue(slot,out var stack) || stack!=value.Stack || !seen.Add(slot))
                throw new InvalidOperationException("L’aperçu ne correspond plus aux piles observées.");
            order.Add(slot);
        }
        var empty=new Queue<InventoryPhysicalSlot>();
        for(var container=scope.FirstContainer;container<scope.FirstContainer+scope.ContainerCount;container++)
        for(var slot=0;slot<scope.SlotsPerContainer;slot++)
        {
            var value=new InventoryPhysicalSlot(container,slot);
            if(!physical.ContainsKey(value))empty.Enqueue(value);
        }
        if(preview.Slots is { } slots)
        {
            if(slots.Count!=scope.Capacity)throw new InvalidOperationException("Positions d’aperçu incomplètes.");
            order.Clear();var placed=new HashSet<InventoryPhysicalSlot>();
            foreach(var value in slots)
            {
                if(value is null)
                {
                    if(!empty.TryDequeue(out var free))throw new InvalidOperationException("Nombre de cases vides incohérent.");
                    order.Add(free);continue;
                }
                var slot=new InventoryPhysicalSlot(value.Origin.Container.Value,value.Stack.Slot);
                if(value.Origin.Owner.Value!=scope.InventoryOwner || !seen.Contains(slot) || !physical.TryGetValue(slot,out var stack)
                    || stack!=value.Stack || !placed.Add(slot))throw new InvalidOperationException("Positions d’aperçu différentes des piles observées.");
                order.Add(slot);
            }
            if(!placed.SetEquals(seen) || empty.Count!=0)throw new InvalidOperationException("L’aperçu ne conserve pas toutes les piles et cases vides.");
        }
        else while(empty.TryDequeue(out var free))order.Add(free);
        if(order.Count!=scope.Capacity || order.Distinct().Count()!=scope.Capacity)throw new InvalidOperationException("Permutation incomplète.");
        return new(scope,expected,order.ToArray());
    }

    public bool Matches(InventorySnapshot actual,ulong owner,DateTimeOffset now)
    {
        if(owner!=Owner || now<Expected.ObservedAt || now-Expected.ObservedAt>Scope.PreviewLifetime
            || now<actual.ObservedAt || now-actual.ObservedAt>TimeSpan.FromSeconds(3))return false;
        try
        {
            var before=Scope.Extract(Expected);var current=Scope.Extract(actual);
            return before.Count==current.Count && before.All(pair=>current.TryGetValue(pair.Key,out var value) && value==pair.Value);
        }
        catch(InvalidOperationException){return false;}
    }
}

public sealed record InventoryVisualSortRequest(long Revision,InventoryVisualSortPlan Plan,DateTimeOffset RequestedAt);
public sealed record InventoryVisualSortState(bool Pending,string? Message,InventorySortCapture? Capture=null);

/// <summary>One explicit request; no sorting on preference save, login, inventory events or worker threads.</summary>
public sealed class InventoryVisualSortService : IDisposable
{
    private readonly object gate=new();private long revision;private bool disposed;
    private InventoryVisualSortRequest? pending;
    private InventorySortCaptureRequest? capturing;
    private InventoryVisualSortState state=new(false,null);
    public InventoryVisualSortState State=>Volatile.Read(ref state);
    public bool Request(InventoryVisualSortPlan plan,DateTimeOffset now)
    {
        lock(gate)
        {
            if(disposed || state.Pending)return false;
            if(plan.Scope.Storage is not null && (state.Capture is not { } capture || capture.Scope!=plan.Scope
                || !ReferenceEquals(capture.Snapshot,plan.Expected) || !capture.Fresh(plan.Owner,now)))return false;
            pending=new(++revision,plan,now);state=state with{Pending=true,Message="Tri visuel demandé ; vérification du rangement en cours."};return true;
        }
    }
    public InventoryVisualSortRequest? Take(ulong owner,DateTimeOffset now)
    {
        lock(gate)
        {
            if(disposed || pending is not { } request)return null;
            pending=null;
            if(owner==0 || owner!=request.Plan.Owner || now<request.RequestedAt || now-request.RequestedAt>TimeSpan.FromSeconds(3))
            {state=state with{Pending=false,Message="Demande annulée : personnage ou date modifiés."};revision++;return null;}
            return request;
        }
    }
    public void Complete(InventoryVisualSortRequest request,string message)
    {lock(gate)if(!disposed && state.Pending && request.Revision==revision)state=state with{Pending=false,Message=message};}
    public bool RequestCapture(InventorySortScope scope,DateTimeOffset now)
    {
        lock(gate)
        {
            if(disposed || state.Pending || scope.Storage is null)return false;
            capturing=new(++revision,scope,now);state=new(true,"Lecture du rangement ouvert…");return true;
        }
    }
    public InventorySortCaptureRequest? TakeCapture(ulong owner,DateTimeOffset now)
    {
        lock(gate)
        {
            if(disposed || capturing is not { } request)return null;
            capturing=null;
            if(owner!=request.Scope.CharacterId || now<request.RequestedAt || now-request.RequestedAt>TimeSpan.FromSeconds(3))
            {revision++;state=new(false,"Lecture annulée : personnage ou date modifiés.");return null;}
            return request;
        }
    }
    public void CompleteCapture(InventorySortCaptureRequest request,InventorySnapshot? snapshot,string name,DateTimeOffset now,string? error=null)
    {
        lock(gate)
        {
            if(disposed || request.Revision!=revision || !state.Pending)return;
            InventorySortCapture? capture=null;
            if(snapshot is not null && error is null)
            {
                request.Scope.Extract(snapshot);
                capture=new(request.Scope,snapshot,name);
                if(!capture.Fresh(request.Scope.CharacterId,now))throw new InvalidOperationException("Capture périmée ou datée dans le futur.");
            }
            state=new(false,error ?? "Aperçu lu en direct. Vous pouvez comparer la règle avant de l’appliquer.",capture);
        }
    }
    public void Clear(){lock(gate){pending=null;capturing=null;revision++;state=new(false,null);}}
    public void Dispose(){lock(gate){disposed=true;pending=null;capturing=null;revision++;state=new(false,null);}}
}
