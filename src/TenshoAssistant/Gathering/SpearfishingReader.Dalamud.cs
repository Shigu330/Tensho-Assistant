using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Gathering;

/// <summary>
/// R-15 (0.417): the game's spearfishing window (« SpearFishing », AddonSpearFishing.Fish: three slots, named fields of
/// FFXIVClientStructs), read on the framework thread five times a second while it is open and dropped when it closes. Read only.
/// </summary>
internal sealed class SpearfishingReader : IDisposable
{
    private readonly object gate=new();
    private readonly IFramework framework;private readonly IGameGui gui;private readonly IClientState client;private readonly IPluginLog log;private readonly Func<uint> point;
    private uint lastPoint;
    private bool disposed;private long nextRead;private int warned;
    private SpearfishFrame? frame;
    public SpearfishFrame? Frame=>Volatile.Read(ref frame);

    public SpearfishingReader(IFramework framework,IGameGui gui,IClientState client,IPluginLog log,Func<uint> point)
    {this.framework=framework;this.gui=gui;this.client=client;this.log=log;this.point=point;TenshoAssistant.Competitive.CompetitiveGate.Add(framework,Update);}

    private unsafe void Update(IFramework unused)
    {
        lock(gate)
        {
            if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
            var now=Stopwatch.GetTimestamp();if(now<nextRead)return;nextRead=now+Stopwatch.Frequency/5;
            try
            {
                var handle=gui.GetAddonByName("SpearFishing");
                if(!client.IsLoggedIn || handle.IsNull || !handle.IsReady || !handle.IsVisible || gui.GameUiHidden){Volatile.Write(ref frame,null);lastPoint=0;return;}
                // 0.891 (X-79): the node targeted (a GatheringPoint row), kept while the window stays open. 0.892 (user capture: the zone's list was shown): the first
                // node found is kept (no change while the window is open), else the nearest one; written once to the log.
                if(lastPoint==0 && point() is var target and not 0){lastPoint=target;log.Information("Spearfishing window: node base {0}.",target);}
                var addon=(AddonSpearFishing*)handle.Address;var unit=(AtkUnitBase*)handle.Address;
                if(unit->RootNode==null || addon->Fish.Length!=3){Volatile.Write(ref frame,null);return;}
                var slots=new SpearfishSlot[3];
                for(var index=0;index<3;index++)
                {
                    var fish=addon->Fish[index];
                    slots[index]=new(index,fish.Available,(int)fish.Size,fish.Speed,fish.GuaranteedLarge,fish.InverseDirection);
                }
                Volatile.Write(ref frame,new SpearfishFrame(slots,client.TerritoryType,unit->X,unit->Y,unit->RootNode->Width*unit->Scale,DateTimeOffset.UtcNow,BaseId:lastPoint));
            }
            catch(Exception error)
            {
                Volatile.Write(ref frame,null);nextRead=now+Stopwatch.Frequency*5;
                if(Interlocked.Exchange(ref warned,1)==0)log.Warning(error,"Spearfishing window unavailable; retry in five seconds.");
            }
        }
    }

    public void Dispose(){lock(gate){if(disposed)return;disposed=true;TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,Update);Volatile.Write(ref frame,null);}}
}
