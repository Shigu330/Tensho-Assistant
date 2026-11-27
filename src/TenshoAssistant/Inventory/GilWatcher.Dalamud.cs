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
    // X-658 (0.1604): the balances compared and how a change becomes entries (pure, tested: GilRecorder).
    private readonly GilRecorder recorder=new();
    private bool disposed,warned,loadFailed;
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
    public GilWatcher(IFramework framework,IPlayerState player,IGameGui gui,GilJournalStore store,IPluginLog log,Func<IReadOnlyList<string>>? windowsOpen=null,Func<bool>? developerMode=null,IChatGui? chat=null)
    {
        this.framework=framework;this.player=player;this.gui=gui;this.store=store;this.log=log;this.windowsOpen=windowsOpen;this.developerMode=developerMode ?? (()=>false);this.chat=chat;
        if(chat is not null)chat.LogMessage+=OnLogMessage;
        if(chat is not null)chat.ChatMessageUnhandled+=OnChatMessage;
        try{TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);}
        catch{if(chat is not null){chat.LogMessage-=OnLogMessage;chat.ChatMessageUnhandled-=OnChatMessage;}throw;}
    }

    /// <summary>X-557 (0.1774): whether the character is in a duty (a gain no window or message explains is then the duty's, GilJournal.InDuty).</summary>
    public Func<bool>? InDuty{get;set;}
    /// <summary>X-557 (0.1774, R25): the character's own names (character, worlds, company tag) kept out of the « Other » context; retainers' names added here.</summary>
    public Func<IEnumerable<string?>>? OwnNames{get;set;}
    // X-557 (0.1774): what surrounds an « Other » change, in developer mode only (GilOtherContext); messages may come off the framework thread.
    private readonly GilOtherContext context=new();
    private readonly HashSet<string> retainerNames=new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<string> personal=[];

    private void OnLogMessage(Dalamud.Game.Chat.ILogMessage message)
    {
        if(disposed)return;
        // X-350 (0.1334, R03.2, DEC-038): nothing noted while Tensho is cut (PvP, ranked content, Triple Triad, Mahjong).
        lock(messagesGate)GilJournal.NoteMessage(messages,message.LogMessageId,DateTimeOffset.UtcNow,TenshoAssistant.Competitive.CompetitiveGate.Cut);
        // X-557 (0.1774): every log message row (a number), for the « Other » context.
        if(!TenshoAssistant.Competitive.CompetitiveGate.Cut && developerMode())lock(context)context.Row(DateTimeOffset.UtcNow,message.LogMessageId);
    }

    /// <summary>
    /// X-557 (0.1774): a chat message, for the « Other » context: its kind and text (items written as their row), only when the game speaks
    /// and no player nor the character's own names are in it (GilOtherContext.Safe, R25); never while Tensho is cut (R03.2).
    /// </summary>
    private void OnChatMessage(Dalamud.Game.Chat.IChatMessage message)
    {
        if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;
        if(disposed || !developerMode())return;
        try
        {
            var text=new System.Text.StringBuilder();
            var namesPlayer=false;
            foreach(var payload in message.Message.Payloads)
                switch(payload)
                {
                    case Dalamud.Game.Text.SeStringHandling.Payloads.PlayerPayload:namesPlayer=true;break;
                    case Dalamud.Game.Text.SeStringHandling.Payloads.ItemPayload item:text.Append(System.Globalization.CultureInfo.InvariantCulture,$"[item {item.ItemId}]");break;
                    case Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload words:text.Append(words.Text);break;
                }
            var own=Volatile.Read(ref personal);
            // The character's names not known yet (before the first reading): nothing kept, never a message that might name it.
            if(own.Count==0)return;
            lock(context)context.Message(DateTimeOffset.UtcNow,(int)message.LogKind,message.Sender.TextValue.Trim().Length!=0,namesPlayer,text.ToString(),own);
        }
        catch(Exception error)
        {
            if(!warned){warned=true;log.Warning(error,"Gil journal: a game message could not be noted.");}
        }
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
                // X-557 (0.1774): the windows for the « Other » context, the character's own names for its messages, and the lines now due.
                // X-773 M2 (review of 0.1775): the company's whole name too, not only its tag (kept whole: its words are often common ones).
                Volatile.Write(ref personal,GilOtherContext.PersonalNames((OwnNames?.Invoke() ?? []).Concat(retainerNames),[player.IsLoaded ? CompanyName():null]));
                IReadOnlyList<string> due;
                lock(context){context.Windows(now,open);due=context.Due(now);}
                // The game's texts go as a value, never as the log's template (a brace in them would be read as a placeholder).
                foreach(var line in due)log.Information("{0}",line);
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
                // X-658 (0.1604): the retainers' total starts from the journal's last balance (sales made meanwhile are counted).
                if(read.IsCompletedSuccessfully){entries=read.Result;recorder.Seed(entries);}
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
                // which the game updates at once; the list keeps the others'. X-431 M2 (0.1416): one reading, shared with the retainers' table (RetainerPurse).
                var livePurse=RetainerPurse.Open(manager);
                foreach(ref var retainer in manager->Retainers)
                    if(retainer.RetainerId!=0)
                    {
                        sum+=RetainerPurse.Gil(retainer.RetainerId,retainer.Gil,livePurse);
                        // X-557 (0.1774, R25): the retainers' names, kept out of the « Other » context (developer mode only, at most ten: the
                        // character's; X-773 M2: emptied at each change of character, so a second character's are added too).
                        if(developerMode() && retainerNames.Count<10 && retainer.NameString is { Length:>0 } name)retainerNames.Add(name);
                    }
                newRetainers=sum;
            }
            // The messages heard lately, copied once per reading and only when a change of the character's gil needs them.
            Dictionary<uint,DateTimeOffset>? heard=null;
            GilCategory Category(long delta)
            {
                if(heard is null)lock(messagesGate)heard=new(messages);
                // X-557 (0.1774): a gain no window or message explains, inside a duty, is the duty's.
                return GilJournal.InDuty(GilJournal.ForDelta(GilJournal.Classify(seen,now,heard),delta),delta,InDuty?.Invoke()==true);
            }
            var changed=recorder.Read(newGil,newRetainers,newCompany,now,entries,Category,(category,change)=>Changed(category,change,now,heard));
            if(entries.Count>GilJournal.MaximumEntries+500)entries=GilJournal.Prune(entries,now);
            // Published only when something changed (the journal may hold 20 000 entries: not copied every second).
            if(changed)Volatile.Write(ref state,new GilJournalState(character,entries.ToArray(),recorder.Gil,recorder.Retainers,recorder.Company,loadFailed));
            if(now>=nextSave)Save(now);
            warned=false;
        }
        catch(Exception error)
        {
            if(!warned){warned=true;log.Warning(error,"Gil could not be read.");}
        }
    }

    // A change recorded (GilRecorder): saved at the next minute; in developer mode, what an « Other » change was.
    private void Changed(GilCategory category,long change,DateTimeOffset at,Dictionary<uint,DateTimeOffset>? heard)
    {
        if(category==GilCategory.Other && developerMode())
            log.Information("Gil other: {0:+#,0;-#,0}, windows seen in the last 15 s: {1}; log messages: {2}",change,
                string.Join(", ",seenAny.Where(pair=>at-pair.Value<=GilJournal.ContextWindow).OrderByDescending(pair=>pair.Value).Select(pair=>pair.Key)),
                string.Join(", ",(heard ?? []).Where(pair=>at-pair.Value<=GilJournal.ContextWindow).Select(pair=>pair.Key)));
        // X-557 (0.1774): its context (messages, rows and windows 3 s before and after) written once, 3 s later.
        if(category==GilCategory.Other && developerMode())lock(context)context.Other(at,change);
        Saves.Changed();
    }

    /// <summary>X-773 M2 (review of 0.1775): the character's company's whole name (InfoProxyFreeCompany, read on the framework thread with the
    /// player loaded; its UI module checked first); null when there is none or it cannot be read.</summary>
    private static string? CompanyName()
    {
        var game=FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
        if(game==null || game->UIModule==null)return null;
        var company=FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyFreeCompany.Instance();
        return company==null || company->Id==0 ? null:company->NameString;
    }

    private Task<List<GilEntry>>? loading;
    private void Switch(ulong id,DateTimeOffset now)
    {
        character=id;recorder.Reset();loadFailed=false;entries=[];
        // X-773 M2 (review of 0.1775): the retainers' names are the character's: the next one's are read again (its ten places free).
        retainerNames.Clear();
        Saves.Switch();
        // X-587 (external audit A14 of 18/11): a journal of this character whose write failed is newer than the data file's: the one used.
        if(id!=0 && Saves.Failed(id) is { } kept)loading=Task.FromResult(GilJournal.Prune(kept,now));
        else loading=id==0 ? null:Task.Run(()=>GilJournal.Prune(store.Load(id),now));
        Volatile.Write(ref state,null);
    }

    private void Save(DateTimeOffset now)
    {
        nextSave=now.AddMinutes(1);
        var pending=Saves;var dirty=pending.Due;var queue=writer??=new((error,message)=>log.Warning(error,message));
        // X-587 (external audit A14 of 18/11): a journal whose write failed is written again at each save, after the character changed too.
        foreach(var (other,kept) in pending.Again(character,dirty && character!=0 && !loadFailed))
            queue.Enqueue("Gil journal could not be saved again; tried at the next save.",pending.Done(other,kept,null,()=>store.Save(other,kept)));
        if(!dirty || character==0 || loadFailed)return;
        // X-76 H10 (0.925): a copy is written off the framework thread. X-587 (A14): marked written only once the write is done.
        var id=character;var copy=GilJournal.Prune(entries,now).ToArray();
        queue.Enqueue("Gil journal could not be saved.",pending.Done(id,copy,pending.Stamp,()=>store.Save(id,copy)));
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        if(chat is not null)chat.ChatMessageUnhandled-=OnChatMessage;
        if(chat is not null)chat.LogMessage-=OnLogMessage;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
        Save(DateTimeOffset.UtcNow);
        if(writer?.Flush(TimeSpan.FromSeconds(3))==false)log.Warning("Gil journal: the last write did not finish before the plugin closed.");
        // X-587 (A14): a journal that could not be written even at the close is said; the data file keeps its last whole save.
        else if(saves is { FailedCount:>0 } left)log.Warning("Gil journal: {0} character journal(s) could not be written before the plugin closed; the data file keeps the last whole save.",left.FailedCount);
        // X-173 (0.1139): the data file's journal folded in, so the folder holds one file when the game is closed (SQLite does it at the next start otherwise).
        else try{store.Checkpoint();}catch(Exception error){log.Warning(error,"The Tensho data file's journal could not be folded in; SQLite does it at the next start.");}
    }
    private BackgroundWriter? writer;
    private GilSaves? saves;
    private GilSaves Saves=>saves??=new();
}
