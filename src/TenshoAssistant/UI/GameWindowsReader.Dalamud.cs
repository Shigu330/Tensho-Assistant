using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.UI;

/// <summary>
/// 0.537 (user request: Tensho's panels and map step aside when a game window comes in front): ten times a second on the framework thread, the game's visible windows with their place in its display order.
/// In a layer the list goes front to back, higher layers are in front (user captures, 0.536). A copy only: nothing native reaches Draw, and a failure leaves the list empty: nothing is hidden.
/// </summary>
internal sealed unsafe class GameWindowsReader : IDisposable
{
    private readonly IFramework framework;private readonly IClientState client;private readonly IGameGui gui;private readonly IPluginLog log;
    private readonly Func<bool> developerMode;
    private IReadOnlyList<GameWindow> windows=[];
    private DateTimeOffset nextRead;private bool warned;private string? logged;

    public IReadOnlyList<GameWindow> Windows=>Volatile.Read(ref windows);

    public GameWindowsReader(IFramework framework,IClientState client,IGameGui gui,IPluginLog log,Func<bool> developerMode)
    {
        this.framework=framework;this.client=client;this.gui=gui;this.log=log;this.developerMode=developerMode;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private void OnUpdate(IFramework unused)
    {
        var now=DateTimeOffset.UtcNow;
        if(now<nextRead)return;
        nextRead=now.AddMilliseconds(100);
        try
        {
            var found=new List<GameWindow>();
            var manager=client.IsLoggedIn && !gui.GameUiHidden ? RaptureAtkUnitManager.Instance():null;
            if(manager!=null)
            {
                var layers=manager->AtkUnitManager.DepthLayers;
                for(var layer=0;layer<layers.Length;layer++)
                {
                    ref var list=ref layers[layer];
                    var count=Math.Min((int)list.Count,list.Entries.Length);
                    for(var index=0;index<count;index++)
                    {
                        AtkUnitBase* unit=list.Entries[index].Value;
                        if(unit==null || !unit->IsVisible || unit->RootNode==null)continue;
                        var name=unit->NameString;
                        if(name.Length==0 || GameWindows.Ignored(name))continue;
                        found.Add(new(name,unit->X,unit->Y,unit->RootNode->Width*unit->Scale,unit->RootNode->Height*unit->Scale,layer*1000+999-index));
                    }
                }
            }
            Volatile.Write(ref windows,found);
            warned=false;
            if(developerMode())
            {
                var said=string.Join(", ",found.Select(window=>$"{window.Name} {window.Width:0}x{window.Height:0} #{window.Rank}"));
                if(said!=logged){logged=said;log.Debug("Game windows: {Windows}",said);}
            }
        }
        catch(Exception error)
        {
            Volatile.Write(ref windows,[]);
            nextRead=now.AddSeconds(5);
            if(!warned){warned=true;log.Warning(error,"The game's windows could not be read.");}
        }
    }

    public void Dispose()=>TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
}
