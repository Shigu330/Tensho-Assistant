using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace TenshoAssistant.MiniGames;

internal sealed class MiniCactpotObservationReader : IDisposable
{
    private readonly object gate=new();private readonly MiniCactpotObservationService service;
    private readonly IFramework framework;private readonly IPlayerState player;private readonly IClientState client;private readonly IGameGui gui;private readonly IPluginLog log;
    private bool disposed;private long nextRead;private ulong owner;
    public MiniCactpotObservationReader(MiniCactpotObservationService service,IFramework framework,IPlayerState player,IClientState client,IGameGui gui,IPluginLog log)
    {this.service=service;this.framework=framework;this.player=player;this.client=client;this.gui=gui;this.log=log;framework.Update+=Update;}
    private ulong CurrentOwner=>client.IsLoggedIn && player.IsLoaded ? player.ContentId:0;
    private static unsafe int[] Copy(AddonLotteryDaily* addon)
    {
        var values=new int[9];for(var index=0;index<9;index++)values[index]=addon->GameNumbers[index];return values;
    }
    private unsafe void Update(IFramework unused)
    {
        lock(gate)
        {
            if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
            var current=CurrentOwner;if(current!=owner){owner=current;nextRead=0;}
            if(service.BeginRead(current) is not { } ticket)return;
            var now=Stopwatch.GetTimestamp();if(now<nextRead)return;nextRead=now+Stopwatch.Frequency/5;
            try
            {
                var handle=gui.GetAddonByName("LotteryDaily");
                if(handle.IsNull){service.Missing(ticket,current,MiniCactpotObservationStatus.AddonAbsent);return;}
                if(gui.GameUiHidden || !handle.IsReady || !handle.IsVisible){service.Missing(ticket,current,MiniCactpotObservationStatus.NotReady);return;}
                var addon=(AddonLotteryDaily*)handle.Address;var board=Copy(addon);
                if(CurrentOwner!=current){service.BeginRead(CurrentOwner);return;}
                var after=gui.GetAddonByName("LotteryDaily");
                if(after.IsNull || after.Address!=handle.Address || !after.IsReady || !after.IsVisible || gui.GameUiHidden || !board.SequenceEqual(Copy(addon)))
                {service.Missing(ticket,current,MiniCactpotObservationStatus.NotReady);return;}
                service.Publish(ticket,current,board);
            }
            catch(Exception error){service.Fail(ticket,current,error);nextRead=now+Stopwatch.Frequency*5;log.Warning(error,"Mini Cactpot observation unavailable; retry in five seconds.");}
        }
    }
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;framework.Update-=Update;service.Dispose();}}
}
