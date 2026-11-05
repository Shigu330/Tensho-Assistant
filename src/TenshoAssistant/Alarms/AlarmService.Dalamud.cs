using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using TenshoAssistant.Gathering;

namespace TenshoAssistant.Alarms;

/// <summary>A game alarm (AgentAlarm, « Alarmes » window of the game), copied read-only.</summary>
public sealed record GameAlarm(string Name, int ClockTime, int Hour, int Minute, int Reminder, bool Repeat, int Index = 0, int Sound = 0, int Timestamp = 0, int Flags = 0);

/// <summary>
/// Rings Tensho alarms (0.201) on the framework thread, once a second: the chat line and the sound of the game's alarms
/// (AgentAlarm.PlayAlarmSoundEffect, the same bell/music box… as the game's « Alarmes » window), optionally silent while bound by duty.
/// Also copies the game's own alarms (AgentAlarm.Alarms, NumActiveAlarms) and can open the game's alarm window. Game alarms are created
/// (0.205, on the player's click or with the « game alarms » setting) through the game's own « /alarm » text command
/// (UIModule.ProcessChatBoxEntry), one command every half second, only commands built by GameAlarmCommand, never above 30 game alarms.
/// </summary>
internal sealed class AlarmService : IDisposable
{
    private readonly IFramework framework;
    private readonly ICondition condition;
    private readonly IPluginLog log;
    private readonly Configuration configuration;
    private readonly Action save;
    private readonly Func<WeatherCatalog?> weather;
    // R-12 (0.414): the ringing alarm goes with its line, so the chat message can carry its map link and item link.
    private readonly Action<TenshoAlarm,string> chat;
    // R-12 (0.319): whether a ringing alarm goes to Discord, and the hand-off (queued, posted on the notifier's worker).
    private readonly Func<TenshoAlarm, bool>? wantsDiscord;
    private readonly Action<TenshoAlarm, int, DateTimeOffset>? toDiscord;
    private readonly object gate = new();
    private IReadOnlyList<GameAlarm>? gameAlarms;
    private long nextCheck;
    private bool openGameWindow;
    private (int Sound, bool Pending) preview;
    private readonly Queue<(Guid Id, string Name, string Command)> outgoing = new();
    private long nextSend;
    private string? gameStatus;
    // 0.331: alarms of this batch not sent because another Tensho alarm holds their game name (« NameTaken » stays shown until the next batch).
    private int nameTaken;
    // Last failure of the game alarm copy, logged once per message (framework thread only).
    private string? gameAlarmsError;
    public string? GameStatus { get { lock (gate) return gameStatus; } }
    public int Pending { get { lock (gate) return outgoing.Count; } }

    /// <summary>Queues the « /alarm » commands of these daily ET alarms; the name is stored on each alarm once sent.</summary>
    public int SendToGame(IEnumerable<TenshoAlarm> alarms, int sound, bool mute)
    {
        var count = 0;
        lock (gate)
        {
            if (outgoing.Count == 0) nameTaken = 0;
            foreach (var alarm in alarms)
                if (GameAlarmCommand.Build(alarm, sound, mute) is { } command && !outgoing.Any(item => item.Id == alarm.Id))
                { outgoing.Enqueue((alarm.Id, GameAlarmCommand.Name(alarm.Label, alarm.EtMinute), command)); count++; }
        }
        return count;
    }
    public IReadOnlyList<GameAlarm>? GameAlarms { get { lock (gate) return gameAlarms; } }

    public AlarmService(IFramework framework, ICondition condition, IPluginLog log, Configuration configuration, Action save,
        Func<WeatherCatalog?> weather, Action<TenshoAlarm,string> chat, Func<TenshoAlarm, bool>? wantsDiscord = null, Action<TenshoAlarm, int, DateTimeOffset>? toDiscord = null)
    {
        this.framework = framework; this.condition = condition; this.log = log; this.configuration = configuration; this.save = save;
        this.weather = weather; this.chat = chat; this.wantsDiscord = wantsDiscord; this.toDiscord = toDiscord;
        framework.Update += OnUpdate;
    }

    public void OpenGameWindow() { lock (gate) openGameWindow = true; }
    public void PreviewSound(int sound) { lock (gate) preview = (sound, true); }

    /// <summary>Weather id of the period containing that time in the territory, or null when unknown.</summary>
    public static uint? WeatherAt(WeatherCatalog? catalog, uint territory, DateTimeOffset time)
    {
        if (catalog is null || territory == 0) return null;
        try { return catalog.Forecast(territory, time, 1)[0].Weather?.Id; }
        catch (ArgumentOutOfRangeException) { return null; }
    }

    private unsafe void OnUpdate(IFramework _)
    {
        if (!framework.IsInFrameworkUpdateThread) return;
        bool open; (int Sound, bool Pending) play;
        lock (gate) { open = openGameWindow; openGameWindow = false; play = preview; preview = default; }
        try
        {
            var agent = AgentAlarm.Instance();
            if (open && agent != null) agent->Show();
            if (play.Pending && agent != null) agent->PlayAlarmSoundEffect((AlarmSoundEffect)Math.Clamp(play.Sound, 0, 5));
        }
        catch (Exception error) { log.Warning(error, "Game alarm window or sound unavailable."); }
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        if (now >= nextSend)
        {
            var delay = Math.Clamp(configuration.AlarmOptions?.GameCommandDelayMs ?? 500, 250, 5000);
            nextSend = now + System.Diagnostics.Stopwatch.Frequency * delay / 1000; SendNext();
        }
        if (now < nextCheck) return;
        nextCheck = now + System.Diagnostics.Stopwatch.Frequency;
        CopyGameAlarms();
        Ring(DateTimeOffset.UtcNow);
    }

    private unsafe void CopyGameAlarms()
    {
        try
        {
            var agent = AgentAlarm.Instance();
            if (agent == null) { lock (gate) gameAlarms = null; return; }
            var list = new List<GameAlarm>();
            var alarms = agent->Alarms;
            for (var index = 0; index < Math.Min((int)agent->NumActiveAlarms, alarms.Length); index++)
            {
                ref var alarm = ref alarms[index];
                list.Add(new(alarm.Name.ToString(), (int)alarm.ClockTime, alarm.Hour, alarm.Minute, alarm.Reminder, alarm.Flags.HasFlag(AlarmFlag.Repeat),
                    alarm.Index, (int)alarm.SoundEffect, alarm.Timestamp, (int)alarm.Flags));
            }
            lock (gate) gameAlarms = list.AsReadOnly();
            gameAlarmsError = null;
        }
        // 0.331: a lasting failure (after a game update) is logged once per message, not every second.
        catch (Exception error)
        {
            if (gameAlarmsError != error.Message) log.Warning(error, "Game alarms could not be read.");
            gameAlarmsError = error.Message; lock (gate) gameAlarms = null;
        }
    }

    // Under the gate: the state after one command of the queue.
    private string SendState() => nameTaken > 0 ? "NameTaken" : outgoing.Count == 0 ? "Done" : "Sending";

    private unsafe void SendNext()
    {
        (Guid Id, string Name, string Command) next;
        lock (gate) { if (outgoing.Count == 0) return; next = outgoing.Dequeue(); }
        // 0.331: nothing reaches the game for an alarm deleted while its command waited (Tensho would no longer know that game alarm),
        // nor for one whose game name another Tensho alarm already holds (same start of label, same ET minute): it keeps ringing in Tensho.
        var owner = configuration.Alarms?.FirstOrDefault(alarm => alarm.Id == next.Id);
        if (owner is null || configuration.Alarms!.Any(alarm => alarm.Id != next.Id && alarm.GameName == next.Name))
        {
            lock (gate) { if (owner is not null) nameTaken++; gameStatus = SendState(); }
            return;
        }
        try
        {
            var agent = AgentAlarm.Instance(); var ui = UIModule.Instance();
            if (agent == null || ui == null) { lock (gate) { gameStatus = "Unavailable"; outgoing.Clear(); } return; }
            var alarms = agent->Alarms; var active = Math.Min((int)agent->NumActiveAlarms, alarms.Length);
            var exists = false;
            for (var index = 0; index < active; index++) if (alarms[index].Name.ToString() == next.Name) exists = true;
            if (!exists && active >= GameAlarmCommand.GameMaximum) { lock (gate) { gameStatus = "Full"; outgoing.Clear(); } return; }
            if (!exists)
            {
                if (!GameAlarmCommand.IsAlarmCommand(next.Command)) throw new InvalidOperationException("Not an alarm command.");
                var text = Utf8String.FromString(next.Command);
                try { ui->ProcessChatBoxEntry(text); }
                finally { text->Dtor(true); }
            }
            owner.GameName = next.Name; save();
            lock (gate) gameStatus = SendState();
        }
        catch (Exception error) { log.Warning(error, "Game alarm command could not be sent."); lock (gate) { gameStatus = "Failed"; outgoing.Clear(); } }
    }

    private unsafe void Ring(DateTimeOffset now)
    {
        var alarms = configuration.Alarms;
        if (alarms is null || alarms.Count == 0) return;
        var settings = configuration.AlarmOptions ?? new();
        var catalog = weather();
        var changed = AlarmSchedule.RemoveExpired(alarms, now) > 0;
        foreach (var alarm in alarms)
        {
            // Alarms copied into the game ring there (the game's own sound and message), not twice; only Discord still gets them (R-12).
            var inGame = alarm.GameName.Length > 0;
            var discord = toDiscord is not null && (wantsDiscord?.Invoke(alarm) ?? false);
            if (inGame && !discord) continue;
            // 0.331: a weather condition is not checked before the weather catalogue is known; the occurrence is left for later, not skipped.
            if (catalog is null && alarm.Weathers.Count + alarm.PreviousWeathers.Count > 0) continue;
            if (settings.RingOnTime && !inGame && AlarmSchedule.OnTimeDue(alarm, now) is { } exact)
            {
                alarm.LastOnTime = exact; changed = true;
                var dutyNow = settings.MuteInDuties && condition[ConditionFlag.BoundByDuty];
                if (settings.ChatAllowed(dutyNow))
                    try { chat(alarm,$"{alarm.Label}|0|{exact.ToLocalTime():HH:mm}"); }
                    catch (Exception error) { log.Warning(error, "Alarm chat line could not be written."); }
                if (settings.SoundAllowed(dutyNow)) PlaySound(settings.Sound, log);
            }
            var (check, occurrence) = AlarmSchedule.Check(alarm, now, time => WeatherAt(catalog, alarm.TerritoryId, time));
            if (check == AlarmCheck.None) continue;
            alarm.LastOccurrence = occurrence; changed = true;
            if (check != AlarmCheck.Fire) continue;
            alarm.LastRung = occurrence;
            var minutes = Math.Max(0, (int)Math.Ceiling((occurrence - now).TotalMinutes));
            if (!inGame)
            {
                // Silence in duties: the sound, and the chat line too with MuteChatInDuties. The duty flag (Dalamud's ICondition, framework
                // thread) is read only while the silence is on; Discord below and the alarms copied into the game are not affected.
                var inDuty = settings.MuteInDuties && condition[ConditionFlag.BoundByDuty];
                // 0.452: a chat failure leaves the sound and Discord, as a Discord failure leaves the chat.
                if (settings.ChatAllowed(inDuty))
                    try { chat(alarm,$"{alarm.Label}|{minutes}|{occurrence.ToLocalTime():HH:mm}"); }
                    catch (Exception error) { log.Warning(error, "Alarm chat line could not be written."); }
                if (settings.SoundAllowed(inDuty)) PlaySound(settings.Sound, log);
            }
            if (discord)
                try { toDiscord!(alarm, minutes, occurrence); }
                catch (Exception error) { log.Warning("Discord message could not be queued ({Error}).", error.GetType().Name); }
        }
        if (!changed) return;
        try { save(); } catch (Exception error) { log.Warning(error, "Alarm state could not be saved."); }
    }

    /// <summary>The game's alarm sound (0 to 5), framework thread; shared with the currency alerts (0.344).</summary>
    public static unsafe void PlaySound(int sound, IPluginLog log)
    {
        try { var agent = AgentAlarm.Instance(); if (agent != null) agent->PlayAlarmSoundEffect((AlarmSoundEffect)Math.Clamp(sound, 0, 5)); }
        catch (Exception error) { log.Warning(error, "Game alarm sound could not be played."); }
    }

    public void Dispose() { framework.Update -= OnUpdate; }
}
