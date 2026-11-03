using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.Inventory;

/// <summary>The journal of the logged-in character as the page shows it (oldest first), or null when none is loaded.</summary>
public sealed record GilJournalState(ulong Character,IReadOnlyList<GilEntry> Entries,long? Gil,long? RetainersGil,long? CompanyGil,bool Unavailable);

/// <summary>
/// g, I-04 (0.351): watches the gil on the framework thread once a second while logged in — the character's (InventoryManager.GetGil),
/// the retainers' total once the game has sent them (RetainerManager, summoning bell), the company chest's while it is loaded
/// (GetFreeCompanyGil) — and records every change with the category of the game window open just before. The first read of a character
/// is a starting point, never a change. One file per character (GilJournal/&lt;id&gt;.json, local character id, never an Account ID),
/// written at most once a minute and on dispose. Functions called only when the game resolved them; reads guarded (R-14).
/// </summary>
public sealed unsafe class GilWatcher : IDisposable
{
    private readonly IFramework framework;
    private readonly IPlayerState player;
    private readonly IGameGui gui;
    private readonly string folder;
    private readonly IPluginLog log;
    private readonly Dictionary<string,DateTimeOffset> seen=[];
    private ulong character;
    private List<GilEntry> entries=[];
    private long? gil,retainers,company;
    private bool dirty,disposed,warned,loadFailed;
    private DateTimeOffset nextRead,nextSave;
    private GilJournalState? state;

    public GilJournalState? State=>Volatile.Read(ref state);

    public GilWatcher(IFramework framework,IPlayerState player,IGameGui gui,string folder,IPluginLog log)
    {
        this.framework=framework;this.player=player;this.gui=gui;this.folder=folder;this.log=log;
        framework.Update+=OnUpdate;
    }

    public static string FileName(ulong character)=>character.ToString(System.Globalization.CultureInfo.InvariantCulture)+".json";

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRead)return;
        nextRead=now.AddSeconds(1);
        try
        {
            foreach(var (addon,_) in GilJournal.Windows)
            {
                var found=gui.GetAddonByName(addon);
                if(!found.IsNull && found.IsVisible)seen[addon]=now;
            }
            var id=player.IsLoaded ? player.ContentId:0;
            if(id!=character){Save(now);Switch(id,now);}
            if(id==0)return;
            var inventory=InventoryManager.Instance();
            long? newGil=inventory!=null && (nint)InventoryManager.MemberFunctionPointers.GetGil!=0 ? inventory->GetGil():null;
            long? newCompany=null;
            if(inventory!=null && (nint)InventoryManager.MemberFunctionPointers.GetFreeCompanyGil!=0
                && inventory->GetInventoryContainer(InventoryType.FreeCompanyGil) is var chest && chest!=null && chest->IsLoaded)newCompany=inventory->GetFreeCompanyGil();
            long? newRetainers=null;
            var manager=RetainerManager.Instance();
            if(manager!=null && manager->IsReady)
            {
                long sum=0;
                foreach(ref var retainer in manager->Retainers)if(retainer.RetainerId!=0)sum+=retainer.Gil;
                newRetainers=sum;
            }
            var changed=Record(ref gil,newGil,GilSource.Character,now) | Record(ref retainers,newRetainers,GilSource.Retainers,now)
                | Record(ref company,newCompany,GilSource.Company,now);
            // Published only when something changed (the journal may hold 20 000 entries: not copied every second).
            if(changed)Volatile.Write(ref state,new GilJournalState(character,entries.ToArray(),gil,retainers,company,loadFailed));
            if(now>=nextSave)Save(now);
            warned=false;
        }
        catch(Exception error)
        {
            if(!warned){warned=true;log.Warning(error,"Gil could not be read.");}
        }
    }

    // A known balance that changed becomes an entry; the first known value is only a starting point. An unknown value changes nothing.
    private bool Record(ref long? last,long? now,GilSource source,DateTimeOffset at)
    {
        if(now is not { } value || last==value)return false;
        if(last is { } before && before!=value)
        {
            var category=source switch{GilSource.Retainers=>GilCategory.Retainer,GilSource.Company=>GilCategory.Company,_=>GilJournal.Classify(seen,at)};
            entries.Add(new GilEntry(at,value-before,value,source,category));
            if(entries.Count>GilJournal.MaximumEntries+500)entries=GilJournal.Prune(entries,at);
            dirty=true;
        }
        last=value;
        return true;
    }

    private void Switch(ulong id,DateTimeOffset now)
    {
        character=id;gil=null;retainers=null;company=null;dirty=false;loadFailed=false;entries=[];
        if(id!=0)
            try{entries=GilJournal.Prune(GilJournal.Read(Path.Combine(folder,FileName(id))),now);}
            catch(Exception error){loadFailed=true;log.Warning(error,"Gil journal could not be read; it is not written over.");}
        Volatile.Write(ref state,id==0 ? null:new GilJournalState(id,entries.ToArray(),null,null,null,loadFailed));
    }

    private void Save(DateTimeOffset now)
    {
        nextSave=now.AddMinutes(1);
        if(!dirty || character==0 || loadFailed)return;
        try{GilJournal.Write(Path.Combine(folder,FileName(character)),GilJournal.Prune(entries,now));dirty=false;}
        catch(Exception error){log.Warning(error,"Gil journal could not be saved.");}
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        framework.Update-=OnUpdate;
        Save(DateTimeOffset.UtcNow);
    }
}
