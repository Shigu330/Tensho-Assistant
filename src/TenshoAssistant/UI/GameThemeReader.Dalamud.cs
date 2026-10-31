using Dalamud.Game.Config;
using Dalamud.Plugin.Services;

namespace TenshoAssistant.UI;

/// <summary>Copies the game's interface theme option (System ColorThemeType) at start and on every change.</summary>
internal sealed class GameThemeReader : IDisposable
{
    private readonly IGameConfig config;
    private readonly IPluginLog log;
    private long value=-1;
    private bool disposed;
    public uint? GameValue=>Interlocked.Read(ref value) is var raw && raw>=0 ? (uint)raw:null;

    public GameThemeReader(IGameConfig config,IPluginLog log)
    {
        this.config=config;this.log=log;
        Read();
        config.SystemChanged+=OnChanged;
    }
    private void OnChanged(object? sender,ConfigChangeEvent change)
    {
        if(!disposed && change.Option is SystemConfigOption.ColorThemeType)Read();
    }
    private void Read()
    {
        try{Interlocked.Exchange(ref value,config.TryGet(SystemConfigOption.ColorThemeType,out uint theme) ? theme:-1);}
        catch(Exception error){Interlocked.Exchange(ref value,-1);log.Warning(error,"Game interface theme unavailable; the plugin keeps its dark theme.");}
    }
    public void Dispose(){if(disposed)return;disposed=true;config.SystemChanged-=OnChanged;}
}
