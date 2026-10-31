using Dalamud.Game.Inventory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace TenshoAssistant.Inventory;

internal sealed class InventoryVisualSortBridge : IDisposable
{
    private readonly InventoryVisualSortService service;
    private readonly IGameInventory inventory;
    private readonly IPlayerState player;
    private readonly IClientState client;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private readonly BankInventoryBridge bank;
    private readonly RegistrationScope registrations=new();
    private bool disposed;
    public InventoryVisualSortBridge(InventoryVisualSortService service,IGameInventory inventory,IPlayerState player,IClientState client,IFramework framework,IPluginLog log,BankInventoryBridge bank)
    {
        this.service=service;this.inventory=inventory;this.player=player;this.client=client;this.framework=framework;this.log=log;this.bank=bank;
        try
        {
            registrations.Register(()=>framework.Update+=Update,()=>framework.Update-=Update);
            registrations.Register(()=>client.Logout+=Logout,()=>client.Logout-=Logout);
        }
        catch{Dispose();throw;}
    }
    private void Logout(int type,int code)=>service.Clear();
    private void Update(IFramework _)
    {
        if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        var owner=client.IsLoggedIn && player.IsLoaded ? player.ContentId:0;
        if(service.TakeCapture(owner,DateTimeOffset.UtcNow) is { } capture)
        {
            try
            {
                var entry=bank.CaptureOpenForSort(capture.Scope.Storage!.Value);
                service.CompleteCapture(capture,entry.Snapshot,entry.Name,DateTimeOffset.UtcNow);
            }
            catch(Exception error){service.CompleteCapture(capture,null,string.Empty,DateTimeOffset.UtcNow,"Lecture impossible : "+error.Message);log.Warning(error,"Tensho live sort preview capture rejected.");}
        }
        var request=service.Take(owner,DateTimeOffset.UtcNow);
        if(request is null)return;
        try
        {
            Apply(request.Plan,owner);
            service.Complete(request,request.Plan.Scope.Storage is null
                ? "Tri visuel appliqué aux quatre sacs ; actualisation sous une seconde."
                : "Tri visuel appliqué au rangement ouvert. Actualisez l’aperçu ; réimportez séparément pour mettre la banque à jour.");
        }
        catch(Exception error){service.Complete(request,"Tri non appliqué : "+error.Message);log.Warning(error,"Tensho visual sort request rejected.");}
    }

    private unsafe void Apply(InventoryVisualSortPlan plan,ulong owner)
    {
        var scope=plan.Scope;var capacity=scope.Capacity;
        if(scope.Storage is { } storage)bank.RequireOpenForSort(storage);
        var module=ItemOrderModule.Instance();
        var sorter=SelectSorter(module,scope);
        if(module==null || module->CharacterContentId!=owner || sorter==null || sorter->InventoryType!=(InventoryType)scope.FirstContainer
            || sorter->Items.First==null || sorter->Items.LongCount!=capacity || sorter->Items.LongCapacity<capacity || sorter->Items.LongCapacity>4096
            || sorter->ItemsPerPage!=scope.SlotsPerContainer || sorter->SortFunctionIndex!=-1)
            throw new InvalidOperationException("Ordre visuel natif indisponible ou tri du jeu en cours.");
        var vector=sorter->Items.First;
        var snapshot=scope.Storage is { } key ? bank.CaptureOpenForSort(key).Snapshot
            : InventoryNativeReader.Capture(inventory,new(scope.InventoryOwner),Enumerable.Range(scope.FirstContainer,scope.ContainerCount).Select(id=>(GameInventoryType)id).ToArray(),"Dalamud/visual-sort/verify");
        if(!plan.Matches(snapshot,owner,DateTimeOffset.UtcNow))
            throw new InvalidOperationException("Les sacs ou leur ordre ont changé depuis l’aperçu. Actualisez puis réessayez.");
        if(sorter->Items.First!=vector || sorter->Items.LongCount!=capacity || sorter->SortFunctionIndex!=-1)
            throw new InvalidOperationException("Table native modifiée pendant la capture.");
        var addresses=new HashSet<nint>();
        var entries=new Dictionary<InventoryPhysicalSlot,ItemOrderModuleSorterItemEntry>();
        var destinations=new nint[capacity];
        var original=new ItemOrderModuleSorterItemEntry[capacity];
        for(var index=0;index<capacity;index++)
        {
            var entry=sorter->Items[index].Value;
            if(entry==null || !addresses.Add((nint)entry) || entry->Page>=scope.ContainerCount || entry->Slot>=scope.SlotsPerContainer
                || !entries.TryAdd(new(scope.FirstContainer+entry->Page,entry->Slot),*entry))
                throw new InvalidOperationException("La table native n’est pas une permutation complète des sacs.");
            destinations[index]=(nint)entry;
            original[index]=*entry;
        }
        // Validate occupied entries against the captured visual mapping, not just a pile-count total.
        foreach(var container in snapshot.Containers.Values)
        foreach(var stack in container.Stacks)
        {
            var entry=(ItemOrderModuleSorterItemEntry*)destinations[stack.DisplayIndex];
            if(scope.FirstContainer+entry->Page!=container.Location.Container.Value || entry->Slot!=stack.Slot)
                throw new InvalidOperationException("Ordre natif modifié pendant la vérification.");
        }
        var ordered=plan.Order.Select(slot=>entries[slot]).ToArray();
        if(scope.Storage is { } recheck)bank.RequireOpenForSort(recheck);
        if(!client.IsLoggedIn || !player.IsLoaded || player.ContentId!=owner || ItemOrderModule.Instance()!=module
            || SelectSorter(module,scope)!=sorter || module->CharacterContentId!=owner || sorter->SortFunctionIndex!=-1 || sorter->Items.Count!=capacity)
            throw new InvalidOperationException("Contexte natif modifié avant application.");
        if(sorter->Items.First!=vector)throw new InvalidOperationException("Table native remplacée avant application.");
        for(var index=0;index<capacity;index++)
        {
            var entry=sorter->Items[index].Value;var previous=original[index];
            if((nint)entry!=destinations[index] || entry->Page!=previous.Page || entry->Slot!=previous.Slot
                || entry->Index!=previous.Index || entry->Flags!=previous.Flags)
                throw new InvalidOperationException("Entrée native modifiée avant application.");
        }
        // All allocations/lookups and validation precede the first write. Entries include native Index/Flags unchanged.
        for(var index=0;index<capacity;index++)
            *(ItemOrderModuleSorterItemEntry*)destinations[index]=ordered[index];
        module->HasChanges=true;
    }
    private static unsafe ItemOrderModuleSorter* SelectSorter(ItemOrderModule* module,InventorySortScope scope)
    {
        if(module==null)return null;
        return scope.Storage?.Kind switch
        {
            null=>module->InventorySorter,
            BankStorageKind.Saddlebag=>module->SaddleBagSorter,
            BankStorageKind.PremiumSaddlebag=>module->PremiumSaddleBagSorter,
            BankStorageKind.Retainer when module->ActiveRetainerId==scope.InventoryOwner=>module->GetActiveRetainerSorter(),
            _=>null,
        };
    }
    public void Dispose(){if(disposed)return;disposed=true;try{registrations.Dispose();}finally{service.Dispose();}}
}
