using Dalamud.Plugin.Services;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// Runs the self-check on the framework thread, one probe per frame. On its own in developer mode, once per login and after each reload of
/// the plugin, when the catalogues are ready (or after two minutes); otherwise on request (« /tensho selftest », Diagnostics). Read-only:
/// no game action, no network request. 0.496: in developer mode the windows of the game are watched once a second, so that an OK is recorded
/// while the window is open, without a pass at that moment; kept in SelfCheckSeen.json (SelfCheckMemory).
/// </summary>
internal sealed class SelfCheckRunner : IDisposable
{
    private static readonly TimeSpan MinimumWait = TimeSpan.FromSeconds(10), MaximumWait = TimeSpan.FromMinutes(2), PrepareWait = TimeSpan.FromSeconds(4);
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPlayerState player;
    private readonly IPluginLog log;
    private readonly Func<bool> developerMode;
    private readonly Func<bool> ready;
    private readonly Action prepare;
    private readonly Func<bool, IReadOnlyList<(string Domain, Func<SelfCheckLine> Probe)>> probes;
    private volatile string? requested;
    private string reason = "";
    private DateTimeOffset? loadedAt;
    private DateTimeOffset? startAt;
    private bool autoDone;
    private SelfCheckRun? run;
    private IReadOnlyList<SelfCheckLine> lastLines = [];
    private readonly Func<IReadOnlyList<(string Domain, Func<SelfCheckLine> Probe)>> windows;
    private readonly string memoryFile;
    private readonly SelfCheckMemory memory;
    private DateTimeOffset nextWatch;
    private bool saveFailed;

    public SelfCheckRunner(IFramework framework, IClientState client, IPlayerState player, IPluginLog log, Func<bool> developerMode, Func<bool> ready,
        Action prepare, Func<bool, IReadOnlyList<(string Domain, Func<SelfCheckLine> Probe)>> probes,
        Func<IReadOnlyList<(string Domain, Func<SelfCheckLine> Probe)>> windows, string memoryFile, string gameVersion)
    {
        this.framework = framework; this.client = client; this.player = player; this.log = log;
        this.developerMode = developerMode; this.ready = ready; this.prepare = prepare; this.probes = probes;
        this.windows = windows; this.memoryFile = memoryFile;
        string? saved = null;
        try { if (File.Exists(memoryFile)) saved = File.ReadAllText(memoryFile); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { log.Warning(error, "Self-check memory could not be read."); }
        memory = SelfCheckMemory.Load(saved, gameVersion);
        framework.Update += OnUpdate;
    }

    private void Watch(DateTimeOffset now)
    {
        if (now < nextWatch || !client.IsLoggedIn || !player.IsLoaded || !developerMode()) return;
        nextWatch = now + TimeSpan.FromSeconds(1);
        var changed = false;
        foreach (var (domain, probe) in windows()) changed |= memory.Record(SelfCheck.Run(domain, probe), now);
        if (!changed) return;
        try { File.WriteAllText(memoryFile, memory.Save()); saveFailed = false; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            if (!saveFailed) { saveFailed = true; log.Warning(error, "Self-check memory could not be saved."); }
        }
    }

    public IReadOnlyList<SelfCheckLine> LastLines => Volatile.Read(ref lastLines);
    public bool Running => requested is not null || Volatile.Read(ref run) is not null || startAt is not null;
    public void Start(string why) { if (!Running) requested = why; }

    private void OnUpdate(IFramework _)
    {
        if (!framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
        try
        {
            var now = DateTimeOffset.UtcNow;
            Watch(now);
            var loggedIn = client.IsLoggedIn && player.IsLoaded;
            if (!loggedIn) { loadedAt = null; autoDone = false; }
            else if (!autoDone && developerMode())
            {
                loadedAt ??= now;
                var waited = now - loadedAt.Value;
                if (waited >= MinimumWait && (waited >= MaximumWait || ready())) { autoDone = true; Start("auto"); }
            }
            if (run is null && startAt is null && requested is { } why)
            {
                requested = null; reason = why;
                // Some readers read only while asked: asked first, read a moment later.
                prepare();
                startAt = now + PrepareWait;
            }
            if (startAt is { } start && now >= start) { startAt = null; Volatile.Write(ref run, new SelfCheckRun(probes(loggedIn))); }
            if (run is not { } current) return;
            if (current.Step() is { } line) log.Information("{0}", memory.Apply(line).Format());
            if (!current.Done) return;
            var lines = current.Lines.Select(memory.Apply).ToArray();
            log.Information("{0}", SelfCheck.Summary(reason, lines));
            Volatile.Write(ref lastLines, lines);
            Volatile.Write(ref run, null);
        }
        catch (Exception error)
        {
            startAt = null; Volatile.Write(ref run, null);
            log.Warning(error, "Self-check stopped.");
        }
    }

    public void Dispose() => framework.Update -= OnUpdate;
}
