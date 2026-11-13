using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.MiniGames;

/// <summary>
/// X-204 (0.1235): copies the racing chocobo on the framework thread every 2 seconds while the Chocobo page asks (5 s window), only when the game
/// has it loaded (RaceChocoboManager.State == Loaded) and the player is loaded, not between areas. The game's own getters are called (rating,
/// pedigree, attributes current and maximum), each only once its address is resolved; RequestParameters (a server request) never is. The last
/// reading stays shown with its time (the game unloads the chocobo later); a failure is logged once and keeps nothing new (R-14).
/// </summary>
public sealed unsafe class RaceChocoboReader : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPlayerState player;
    private readonly ICondition condition;
    private readonly IPluginLog log;
    private RaceChocoboSnapshot? state;
    private DateTimeOffset nextRead;
    private long wantedUntilTicks;
    private bool disposed,warned;

    public RaceChocoboSnapshot? State=>Volatile.Read(ref state);
    public bool Unavailable { get; private set; }

    public RaceChocoboReader(IFramework framework,IClientState client,IPlayerState player,ICondition condition,IPluginLog log)
    {
        this.framework=framework;this.client=client;this.player=player;this.condition=condition;this.log=log;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
        try{client.Logout+=OnLogout;}catch{TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);throw;}
    }

    public void Want()=>Volatile.Write(ref wantedUntilTicks,DateTimeOffset.UtcNow.AddSeconds(5).UtcTicks);

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRead || now.UtcTicks>Volatile.Read(ref wantedUntilTicks))return;
        nextRead=now.AddSeconds(2);
        if(!client.IsLoggedIn || !player.IsLoaded || condition[ConditionFlag.BetweenAreas] || condition[ConditionFlag.BetweenAreas51])return;
        try
        {
            if((nint)RaceChocoboManager.StaticAddressPointers.pInstance==0)return;
            var manager=RaceChocoboManager.Instance();
            if(manager==null || manager->State!=RaceChocoboManager.RaceChocoboState.Loaded)return;
            if((nint)RaceChocoboManager.MemberFunctionPointers.GetRating==0 || (nint)RaceChocoboManager.MemberFunctionPointers.GetPedigreeLevel==0
               || (nint)RaceChocoboManager.MemberFunctionPointers.GetAttributesCurrent==0 || (nint)RaceChocoboManager.MemberFunctionPointers.GetAttributesMaximum==0)
            {Unavailable=true;return;}
            RaceChocoboAttributeValues current,maximum;
            manager->GetAttributesCurrent(&current);
            manager->GetAttributesMaximum(&maximum);
            var snapshot=new RaceChocoboSnapshot(manager->GetRating(),manager->GetPedigreeLevel(),manager->IsFemale,
                current.Values.ToArray().Select(value=>(int)Math.Min(value,int.MaxValue)).ToArray(),maximum.Values.ToArray().Select(value=>(int)Math.Min(value,int.MaxValue)).ToArray(),
                manager->AbilityHereditary,manager->AbilityLearned,now);
            if(!RaceChocobo.Plausible(snapshot))
            {
                if(!warned){warned=true;log.Warning("The racing chocobo read looks wrong (perhaps after a game update): rating {0}, pedigree {1}; not shown.",snapshot.Rating,snapshot.Pedigree);}
                Unavailable=true;return;
            }
            Volatile.Write(ref state,snapshot);
            Unavailable=false;warned=false;
        }
        catch(Exception error)
        {
            Unavailable=true;nextRead=now.AddSeconds(30);
            if(!warned){warned=true;log.Warning(error,"The racing chocobo could not be read.");}
        }
    }

    private void OnLogout(int type,int code)=>Volatile.Write(ref state,null);

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
        client.Logout-=OnLogout;
    }
}
