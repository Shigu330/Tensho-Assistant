using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace TenshoAssistant.Gear;

/// <summary>
/// g, M-12 (0.383): while the Gear page asks (<see cref="Want"/>, 5 s window), copies the game's gear sets (RaptureGearsetModule
/// entries that exist: number, name, job, item level, pieces and materia) and the job levels (PlayerState.ClassJobLevels by the job's
/// ExpArrayIndex) every 2 seconds on the framework thread. Field reads only; a set is never changed. Cleared at logout; a failure is
/// logged once and publishes nothing. 0.385 (g, X-08): <see cref="Equip"/> wears a set on the player's click only, through the game's
/// EquipGearset (called only once resolved, the set checked with IsValidGearset, 2 seconds apart).
/// </summary>
/// </summary>
public sealed unsafe class GameGearsetReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPluginLog log;
    private readonly IReadOnlyDictionary<uint,int> expIndex;
    private readonly IReadOnlyList<(uint Job,int Index)> jobs;
    private readonly IReadOnlyDictionary<int,int> expToNext;
    private GearsetState? state;
    private volatile bool unavailable;
    /// <summary>0.492 (R-14): the last read failed (perhaps after a game update): State is then null, not the old sets.</summary>
    public bool Unavailable=>unavailable;
    private long wantedUntil;
    private DateTimeOffset nextRead;
    private bool disposed,warned;
    private int equipRequest=-1;
    private DateTimeOffset nextEquip;
    public GearsetState? State=>Volatile.Read(ref state);
    /// <summary>Asks to wear the set of this game number (1-based) at the next framework update (player's click only).</summary>
    public void Equip(int number)=>Interlocked.Exchange(ref equipRequest,number-1);

    public GameGearsetReader(IFramework framework,IClientState client,IPluginLog log,IReadOnlyDictionary<uint,int> expIndex,IReadOnlyDictionary<uint,int> jobOrder,IReadOnlyDictionary<int,int> expToNext)
    {
        this.framework=framework;this.client=client;this.log=log;this.expIndex=expIndex;this.expToNext=expToNext;
        jobs=JobProgressList.Jobs(expIndex,jobOrder);
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
        try{client.Logout+=OnLogout;}catch{TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);throw;}
    }

    public void Want()=>Volatile.Write(ref wantedUntil,DateTimeOffset.UtcNow.AddSeconds(5).UtcTicks);

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(!disposed && client.IsLoggedIn && now>=nextEquip && Interlocked.Exchange(ref equipRequest,-1) is var wanted and >=0)
        {
            nextEquip=now.AddSeconds(2);
            try
            {
                var gearsets=RaptureGearsetModule.Instance();
                if(gearsets!=null && (nint)RaptureGearsetModule.MemberFunctionPointers.EquipGearset!=0 && (nint)RaptureGearsetModule.MemberFunctionPointers.IsValidGearset!=0
                   && gearsets->IsValidGearset(wanted))gearsets->EquipGearset(wanted,0);
                else log.Information("Gear set {0} could not be worn (not valid or game function not resolved).",wanted+1);
            }
            catch(Exception error){log.Warning(error,"A gear set could not be worn.");}
        }
        if(disposed || now<nextRead || now.UtcTicks>Volatile.Read(ref wantedUntil) || !client.IsLoggedIn)return;
        nextRead=now.AddSeconds(2);
        try
        {
            var module=RaptureGearsetModule.Instance();
            if(module==null)return;
            var sets=new List<GameGearset>();
            for(var index=0;index<module->Entries.Length;index++)
            {
                ref var entry=ref module->Entries[index];
                if(!entry.Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists) || entry.ClassJob==0)continue;
                var pieces=new List<GearsetPiece>();
                for(var item=0;item<GameGearsets.SlotOrder.Length && item<entry.Items.Length;item++)
                {
                    if(GameGearsets.SlotOrder[item] is not { } slot)continue;
                    ref var piece=ref entry.Items[item];
                    if(piece.ItemId==0)continue;
                    var (id,hq)=GameGearsets.Split(piece.ItemId);
                    var materia=new List<MeldedMateria>();
                    for(var socket=0;socket<piece.Materia.Length && socket<piece.MateriaGrades.Length;socket++)
                        if(piece.Materia[socket]!=0)materia.Add(new(piece.Materia[socket],piece.MateriaGrades[socket]));
                    pieces.Add(new(slot,id,hq,materia));
                }
                sets.Add(new(entry.Id+1,entry.NameString,entry.ClassJob,entry.ItemLevel,pieces));
            }
            var levels=new Dictionary<uint,int>();
            var player=PlayerState.Instance();
            if(player!=null && player->IsLoaded)
                foreach(var (job,exp) in expIndex)
                    if(exp>=0 && exp<player->ClassJobLevels.Length && player->ClassJobLevels[exp] is > 0 and var level)levels[job]=level;
            // T21 (0.646): the same slots for the experience.
            var progress=new List<JobProgressRow>();
            if(player!=null && player->IsLoaded)
                foreach(var (job,slot) in jobs)
                    if(slot>=0 && slot<player->ClassJobLevels.Length && slot<player->ClassJobExperience.Length)
                    {
                        var level=(int)player->ClassJobLevels[slot];
                        progress.Add(new(job,Math.Max(level,0),Math.Max(player->ClassJobExperience[slot],0),expToNext.GetValueOrDefault(level)));
                    }
            // X-76 (0.955): an unchanged copy keeps the published object (GearsetState in Gear/Gearsets.cs), so the pages' caches keyed on it stay.
            var next=new GearsetState(sets,levels,progress);
            if(!next.SameAs(Volatile.Read(ref state)))Volatile.Write(ref state,next);
            unavailable=false;
            warned=false;
        }
        catch(Exception error)
        {
            nextRead=now.AddSeconds(30);
            unavailable=true;
            Volatile.Write(ref state,null);
            if(!warned){warned=true;log.Warning(error,"The game's gear sets could not be read.");}
        }
    }

    private void OnLogout(int type,int code){unavailable=false;Volatile.Write(ref state,null);}

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
        client.Logout-=OnLogout;
    }
}
