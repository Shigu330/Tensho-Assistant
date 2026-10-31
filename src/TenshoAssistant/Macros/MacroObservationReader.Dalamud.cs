using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;

namespace TenshoAssistant.Macros;

internal sealed unsafe class MacroObservationReader : IDisposable
{
    private readonly object gate = new();
    private readonly MacroObservationService service;
    private readonly IFramework framework;
    private readonly IPlayerState player;
    private readonly IClientState client;
    private readonly IPluginLog log;
    private bool disposed;
    private long retryAt;
    public MacroObservationReader(MacroObservationService service, IFramework framework, IPlayerState player, IClientState client, IPluginLog log)
    {
        this.service = service; this.framework = framework; this.player = player; this.client = client; this.log = log;
        framework.Update += Update;
    }
    private void Update(IFramework unusedFramework)
    {
        lock (gate)
        {
            if (disposed || !service.State.Enabled || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
            if (Stopwatch.GetTimestamp() < retryAt) return;
            try
            {
                var owner = client.IsLoggedIn && player.IsLoaded ? player.ContentId : 0;
                var shell = owner == 0 ? null : RaptureShellModule.Instance();
                var observation = shell == null
                    ? new MacroNativeObservation(owner, false, -1, false, 0, "", DateTimeOffset.UtcNow)
                    : new MacroNativeObservation(owner, true, shell->MacroCurrentLine, shell->MacroLocked,
                        shell->WaitTimeMs, shell->MacroName.ToString(), DateTimeOffset.UtcNow);
                if ((client.IsLoggedIn && player.IsLoaded ? player.ContentId : 0) != owner)
                    observation = new(0, false, -1, false, 0, "", DateTimeOffset.UtcNow);
                service.Publish(observation);
            }
            catch (Exception error)
            {
                service.Fail(error); retryAt = Stopwatch.GetTimestamp() + 5 * Stopwatch.Frequency;
                log.Warning(error, "Tensho passive macro observation unavailable.");
            }
        }
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true; framework.Update -= Update; service.SetEnabled(false);
        }
    }
}
