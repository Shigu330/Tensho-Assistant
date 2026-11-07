using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Plugin.Services;

namespace TenshoAssistant.UI;

/// <summary>
/// 0.539 (user report: moving a game window, the panels attached to it lagged behind): the readers refresh five times a second or less, which is enough for what a window contains, not for where it is.
/// Every framework tick, only the position of the windows a panel asked about in the last second; nothing is read when no panel asks. A copy: nothing native reaches Draw, and a failure leaves the readers' own position.
/// </summary>
internal sealed class GameWindowFollower : IDisposable
{
    private readonly IFramework framework;private readonly IGameGui gui;private readonly IPluginLog log;
    private readonly ConcurrentDictionary<string,long> wanted=new();
    private readonly ConcurrentDictionary<string,Vector2> live=new();
    private bool warned;

    public GameWindowFollower(IFramework framework,IGameGui gui,IPluginLog log)
    {
        this.framework=framework;this.gui=gui;this.log=log;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    /// <summary>Where the game window is now; the position its reader saw until it is known.</summary>
    public Vector2 At(string name,float x,float y)
    {
        wanted[name]=Stopwatch.GetTimestamp();
        return live.TryGetValue(name,out var position) ? position:new Vector2(x,y);
    }

    private void OnUpdate(IFramework unused)
    {
        if(wanted.IsEmpty)return;
        var now=Stopwatch.GetTimestamp();
        foreach(var (name,asked) in wanted)
        {
            if(now-asked>Stopwatch.Frequency){wanted.TryRemove(name,out _);live.TryRemove(name,out _);continue;}
            try
            {
                var handle=gui.GetAddonByName(name);
                if(handle.IsNull || !handle.IsVisible)live.TryRemove(name,out _);
                else live[name]=handle.Position;
            }
            catch(Exception error)
            {
                live.TryRemove(name,out _);
                if(!warned){warned=true;log.Warning(error,"The position of a game window could not be read.");}
            }
        }
    }

    public void Dispose()=>TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
}
