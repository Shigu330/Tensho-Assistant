using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.Inventory;

/// <summary>The journal of the logged-in character as the page shows it (oldest first), or null when none is loaded.</summary>
public sealed record GilJournalState(ulong Character,IReadOnlyList<GilEntry> Entries,long? Gil,long? RetainersGil,long? CompanyGil,bool Unavailable);

/// <summary>
/// g, I-04 (0.351): watches the gil on the framework thread once a second while logged in — the character's (InventoryManager.GetGil),
/// the retainers' total once the game has sent them (RetainerManager, summoning bell), the company chest's while it is loaded
/// (GetFreeCompanyGil) — and records every change with the category of the game window open just before. The first read of a character
/// is a starting point, never a change. Kept per character (local character id, never an Account ID; since 0.1139 in TenshoData.db, X-173,
/// through <see cref="GilJournalStore"/>), written at most once a minute and on dispose. Functions called only when the game resolved them;
/// reads guarded (R-14).
/// </summary>
public sealed unsafe class GilWatcher : IDisposable
{
    private readonly IFramework framework;
    private readonly IPlayerState player;
    private readonly IGameGui gui;
    private readonly GilJournalStore store;
    private readonly IPluginLog log;
    private readonly Dictionary<string,DateTimeOffset> seen=[];
    private ulong character;
    private List<GilEntry> entries=[];
    private long? gil,retainers,company;
    private bool dirty,disposed,warned,loadFailed;
    private DateTimeOffset nextRead,nextSave;
    private GilJournalState? state;

    public GilJournalState? State=>Volatile.Read(ref state);

    // 0.768 (X-18, user request: fewer « Other »): in developer mode, every game window seen open lately, to tell what an « Other » change was.
    private readonly Func<IReadOnlyList<string>>? windowsOpen;
    private readonly Func<bool> developerMode;
    private readonly Dictionary<string,DateTimeOffset> seenAny=[];
    // X-320 (0.1319): when each log message of GilJournal.Messages was last seen (challenge log entry done, a defeat); the event may come off
    // the framework thread, hence the lock.
    private readonly Dictionary<uint,DateTimeOffset> messages=[];
    private readonly object messagesGate=new();
    private readonly IChatGui? chat;
    private long retainerDeposits;
    public GilWatcher(IFramework framework,IPlayerState player,IGameGui gui,GilJournalStore store,IPluginLog log,Func<IReadOnlyList<string>>? windowsOpen=null,Func<bool>? developerMode=null,IChatGui? chat=null)
    {
        this.framework=framework;this.player=player;this.gui=gui;this.store=store;this.log=log;this.windowsOpen=windowsOpen;this.developerMode=developerMode ?? (()=>false);this.chat=chat;
        if(chat is not null)chat.LogMessage+=OnLogMessage;
        try{TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);}
        catch{if(chat is not null)chat.LogMessage-=OnLogMessage;throw;}
    }

    private void OnLogMessage(Dalamud.Game.Chat.ILogMessage message)
    {
        if(disposed)return;
        // X-350 (0.1334, R03.2, DEC-038): nothing noted while Tensho is cut (PvP, ranked content, Triple Triad, Mahjong).
        lock(messagesGate)GilJournal.NoteMessage(messages,message.LogMessageId,DateTimeOffset.UtcNow,TenshoAssistant.Competitive.CompetitiveGate.Cut);
    }

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRead)return;
        nextRead=now.AddSeconds(1);
        try
        {
            if(developerMode() && windowsOpen?.Invoke() is { } open)
            {
                foreach(var name in open)seenAny[name]=now;
                if(seenAny.Count>200)foreach(var old in seenAny.Where(pair=>now-pair.Value>GilJournal.ContextWindow).Select(pair=>pair.Key).ToArray())seenAny.Remove(old);
            }
            foreach(var (addon,_) in GilJournal.Windows)
            {
                var found=gui.GetAddonByName(addon);
                if(!found.IsNull && found.IsVisible)seen[addon]=now;
            }
            var id=player.IsLoaded ? player.ContentId:0;
            if(id!=character){Save(now);Switch(id,now);}
            if(id==0)return;
            // 0.993 (« Slow reader: GilWatcher 128 ms » at each load): the journal is read off the frame; nothing is recorded until it is there.
            if(loading is { } read)
            {
                if(!read.IsCompleted)return;
                loading=null;
                if(read.IsCompletedSuccessfully)entries=read.Result;
                else{loadFailed=true;log.Warning(read.Exception?.GetBaseException(),"Gil journal could not be read; it is not written over.");}
                Volatile.Write(ref state,new GilJournalState(character,entries.ToArray(),null,null,null,loadFailed));
            }
            // R-14 (0.453): the balance only once the Currency container is loaded (as CurrencyWatcher, 0.344): before it GetGil gives 0, which
            // became a starting point at login, then a « + all the gil » entry. Each function checked: a source lost after an update stays unknown.
            var inventory=(nint)InventoryManager.StaticAddressPointers.pInstance!=0 ? InventoryManager.Instance():null;
            var canOpen=inventory!=null && (nint)InventoryManager.MemberFunctionPointers.GetInventoryContainer!=0;
            long? newGil=null;
            if(canOpen && (nint)InventoryManager.MemberFunctionPointers.GetGil!=0
                && inventory->GetInventoryContainer(InventoryType.Currency) is var wallet && wallet!=null && wallet->IsLoaded)newGil=inventory->GetGil();
            long? newCompany=null;
            if(canOpen && (nint)InventoryManager.MemberFunctionPointers.GetFreeCompanyGil!=0
                && inventory->GetInventoryContainer(InventoryType.FreeCompanyGil) is var chest && chest!=null && chest->IsLoaded)newCompany=inventory->GetFreeCompanyGil();
            long? newRetainers=null;
            var manager=(nint)RetainerManager.StaticAddressPointers.pInstance!=0 ? RetainerManager.Instance():null;
            if(manager!=null && manager->IsReady)
            {
                long sum=0;
                // 0.769 (X-19, user report: the retainers' total followed a deposit only after reopening): the open retainer's gil from its own container,
                // which the game updates at once; the list keeps the others'.
                var active=manager->GetActiveRetainer();
                long? live=null;
                if(active!=null && active->RetainerId!=0 && canOpen && inventory->GetInventoryContainer(InventoryType.RetainerGil) is var purse && purse!=null && purse->IsLoaded)
                {
                    long held=0;
                    for(var slot=0;slot<purse->Size;slot++)if(purse->GetInventorySlot(slot) is var item && item!=null && item->ItemId==1)held+=item->Quantity;
                    live=held;
                }
                foreach(ref var retainer in manager->Retainers)
                    if(retainer.RetainerId!=0)sum+=live is { } heldNow && active!=null && retainer.RetainerId==active->RetainerId ? heldNow:retainer.Gil;
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
            var delta=value-before;
            Dictionary<uint,DateTimeOffset> heard;
            lock(messagesGate)heard=new(messages);
            var category=source switch{GilSource.Retainers=>GilCategory.Retainer,GilSource.Company=>GilCategory.Company,_=>GilJournal.ForDelta(GilJournal.Classify(seen,at,heard),delta)};
            // 0.769 (X-19): a deposit to the retainers waits for their rise; a rise beyond the deposits is their market sales.
            if(source==GilSource.Character && category==GilCategory.Retainer && delta<0)retainerDeposits+=-delta;
            if(source==GilSource.Retainers && delta>0)
            {
                var (transfer,sales,left)=GilJournal.SplitRetainerRise(delta,retainerDeposits);
                retainerDeposits=left;
                if(sales>0)entries.Add(new GilEntry(at,sales,value-transfer,source,GilCategory.Market));
                delta=transfer;
            }
            if(delta!=0)entries.Add(new GilEntry(at,delta,value,source,category));
            if(category==GilCategory.Other && developerMode())
                log.Information("Gil other: {0:+#,0;-#,0}, windows seen in the last 15 s: {1}; log messages: {2}",value-before,
                    string.Join(", ",seenAny.Where(pair=>at-pair.Value<=GilJournal.ContextWindow).OrderByDescending(pair=>pair.Value).Select(pair=>pair.Key)),
                    string.Join(", ",heard.Where(pair=>at-pair.Value<=GilJournal.ContextWindow).Select(pair=>pair.Key)));
            if(entries.Count>GilJournal.MaximumEntries+500)entries=GilJournal.Prune(entries,at);
            dirty=true;
        }
        last=value;
        return true;
    }

    private Task<List<GilEntry>>? loading;
    private void Switch(ulong id,DateTimeOffset now)
    {
        character=id;gil=null;retainers=null;company=null;dirty=false;loadFailed=false;entries=[];
        loading=id==0 ? null:Task.Run(()=>GilJournal.Prune(store.Load(id,now),now));
        Volatile.Write(ref state,null);
    }

    private void Save(DateTimeOffset now)
    {
        nextSave=now.AddMinutes(1);
        if(!dirty || character==0 || loadFailed)return;
        // X-76 H10 (0.925): a copy is written off the framework thread.
        var id=character;var copy=GilJournal.Prune(entries,now).ToArray();
        (writer??=new((error,message)=>log.Warning(error,message))).Enqueue("Gil journal could not be saved.",()=>store.Save(id,copy));
        dirty=false;
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        if(chat is not null)chat.LogMessage-=OnLogMessage;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
        Save(DateTimeOffset.UtcNow);
        if(writer?.Flush(TimeSpan.FromSeconds(3))==false)log.Warning("Gil journal: the last write did not finish before the plugin closed.");
        // X-173 (0.1139): the data file's journal folded in, so the folder holds one file when the game is closed (SQLite does it at the next start otherwise).
        else try{store.Checkpoint();}catch(Exception error){log.Warning(error,"The Tensho data file's journal could not be folded in; SQLite does it at the next start.");}
    }
    private BackgroundWriter? writer;
}
