using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Event;

namespace TenshoAssistant.Gathering;

/// <summary>
/// R-07 (0.418): the ocean fishing voyage in progress, read twice a second on the framework thread through the game's event framework
/// (GetInstanceContentOceanFishing; the director functions behind it are used only once their addresses are resolved). Fields only;
/// nothing is sent. Dropped outside the voyage; a route or stop out of range is not published.
/// </summary>
internal sealed class OceanVoyageReader : IDisposable
{
    private readonly object gate=new();
    private readonly IFramework framework;private readonly IClientState client;private readonly IPluginLog log;
    private bool disposed;private long nextRead;private int warned;
    private OceanVoyageFrame? frame;
    public OceanVoyageFrame? Frame=>Volatile.Read(ref frame);

    public OceanVoyageReader(IFramework framework,IClientState client,IPluginLog log)
    {this.framework=framework;this.client=client;this.log=log;TenshoAssistant.Competitive.CompetitiveGate.Add(framework,Update);}

    private unsafe void Update(IFramework unused)
    {
        lock(gate)
        {
            if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
            var now=Stopwatch.GetTimestamp();if(now<nextRead)return;nextRead=now+Stopwatch.Frequency/2;
            try
            {
                if(!client.IsLoggedIn || (nint)EventFramework.MemberFunctionPointers.GetContentDirector==0
                   || (nint)EventFramework.MemberFunctionPointers.GetInstanceContentDirector==0){Volatile.Write(ref frame,null);return;}
                var events=EventFramework.Instance();
                var voyage=events==null ? null:events->GetInstanceContentOceanFishing();
                if(voyage==null){Volatile.Write(ref frame,null);return;}
                var zone=(int)voyage->CurrentZone;var status=(int)voyage->Status;
                if(zone is < 0 or > 2 || status is < 0 or > 4 || voyage->CurrentRoute is 0 or > 1000){Volatile.Write(ref frame,null);return;}
                var player=voyage->LocalPlayerAllResult;
                Volatile.Write(ref frame,new OceanVoyageFrame(voyage->CurrentRoute,zone,status,voyage->SpectralCurrentActive,voyage->WeatherId,
                    [(voyage->Mission1Type,voyage->Mission1Progress),(voyage->Mission2Type,voyage->Mission2Progress),(voyage->Mission3Type,voyage->Mission3Progress)],
                    player.TotalPoints,player.CaughtFish,DateTimeOffset.UtcNow));
            }
            catch(Exception error)
            {
                Volatile.Write(ref frame,null);nextRead=now+Stopwatch.Frequency*5;
                if(Interlocked.Exchange(ref warned,1)==0)log.Warning(error,"Ocean fishing voyage unavailable; retry in five seconds.");
            }
        }
    }

    public void Dispose(){lock(gate){if(disposed)return;disposed=true;TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,Update);Volatile.Write(ref frame,null);}}
}
