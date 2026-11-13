using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using TenshoAssistant.Discoveries;

namespace TenshoAssistant.Hunting;

/// <summary>
/// X-222 (0.1232): the game's « You defeat … » message (LogMessage 557, docs/evidence/0.1174-defeat-treasure.md) matched to its corpse among the dead
/// monsters near the player (same rule as the discoveries: by name, the nearest not counted yet, <see cref="ItemGains.Corpse"/>), only in a zone with an
/// S rank counted by kills (<see cref="SRankKills"/>); each match is handed to <see cref="Killed"/> on the framework thread. Nothing is read
/// elsewhere; a message not matched is left uncounted, never guessed. Cut in competitive content like the rest (CompetitiveGate).
/// </summary>
public sealed class SRankKillWatcher : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IObjectTable objects;
    private readonly IChatGui chat;
    private readonly object gate=new();
    private readonly List<(string Source,bool SourcePlayer,string Target,bool TargetPlayer)> defeats=[];
    private readonly OnceSet<ulong> counted=new(256);
    private bool disposed;

    /// <summary>A kill of this monster (BNpcName row) in this zone.</summary>
    public Action<uint,uint>? Killed { get; set; }

    public SRankKillWatcher(IFramework framework,IClientState client,IObjectTable objects,IChatGui chat)
    {
        this.framework=framework;this.client=client;this.objects=objects;this.chat=chat;
        chat.LogMessage+=OnLogMessage;
        try{TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);}
        catch{chat.LogMessage-=OnLogMessage;throw;}
    }

    private void OnLogMessage(Dalamud.Game.Chat.ILogMessage message)
    {
        if(disposed || message.LogMessageId!=ItemGains.DefeatMessage || TenshoAssistant.Competitive.CompetitiveGate.Cut)return;
        try
        {
            if(!SRankKills.AnyIn(client.TerritoryType))return;
            if(message.SourceEntity is { } from && message.TargetEntity is { } to)
                lock(gate)if(defeats.Count<32)defeats.Add((from.Name.ExtractText(),from.IsPlayer,to.Name.ExtractText(),to.IsPlayer));
        }
        catch(Exception error) when(error is not OutOfMemoryException){}
    }

    private void OnUpdate(IFramework _)
    {
        if(disposed)return;
        (string Source,bool SourcePlayer,string Target,bool TargetPlayer)[] seen;
        lock(gate){if(defeats.Count==0)return;seen=[..defeats];defeats.Clear();}
        if(!client.IsLoggedIn || objects.LocalPlayer is not { } me)return;
        var territory=client.TerritoryType;
        var name=me.Name.TextValue;
        foreach(var defeat in seen)
        {
            if(!ItemGains.DefeatedByMe(ItemGains.DefeatMessage,defeat.SourcePlayer,defeat.Source,defeat.TargetPlayer,defeat.Target,name))continue;
            var dead=objects.OfType<IBattleNpc>().Where(npc=>npc.BattleNpcKind==BattleNpcSubKind.Combatant && npc.CurrentHp==0 && npc.NameId!=0).ToList();
            var corpse=ItemGains.Corpse(dead,npc=>npc.Name.TextValue,npc=>System.Numerics.Vector3.DistanceSquared(npc.Position,me.Position),npc=>counted.Contains(npc.GameObjectId),defeat.Target);
            if(corpse is null)continue;
            counted.Add(corpse.GameObjectId);
            Killed?.Invoke(corpse.NameId,territory);
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        chat.LogMessage-=OnLogMessage;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
