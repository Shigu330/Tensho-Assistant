using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;

namespace TenshoAssistant.Macros;

internal sealed unsafe class MacroChainBridge : IDisposable
{
    private readonly object gate = new();
    private readonly MacroChainService service;
    private readonly IFramework framework;
    private readonly IPlayerState player;
    private readonly IClientState client;
    private readonly ICommandManager commands;
    private readonly IGameInteropProvider interop;
    private readonly IPluginLog log;
    private readonly MacroSequence sequence = new();
    private readonly RegistrationScope registrations = new();
    private readonly List<MacroDefinition> copied = [];
    private IReadOnlyList<MacroDefinition> prepared = Array.Empty<MacroDefinition>();
    private Hook<ExecuteDelegate>? executeHook;
    private delegate void ExecuteDelegate(RaptureShellModule* shell, RaptureMacroModule.Macro* macro);
    // Comparison address valid only inside the synchronous native call, cleared before Update returns.
    private nint ownCallAddress;
    private int ownCallThread;
    private ulong owner;
    private int position = -1;
    private bool disposed;
    private long updateNumber;
    private long requestedAt;
    public MacroChainBridge(MacroChainService service, IFramework framework, IPlayerState player, IClientState client,
        ICommandManager commands, IGameInteropProvider interop, IPluginLog log)
    {
        this.service = service; this.framework = framework; this.player = player; this.client = client;
        this.commands = commands; this.interop = interop; this.log = log;
        try
        {
            registrations.Register(() =>
            {
                if (!commands.AddHandler(MacroDefinition.Marker, new CommandInfo(OnMarker) { HelpMessage = "Marqueur de dernière ligne pour les séquences Tensho.", ShowInHelp = false }))
                    throw new InvalidOperationException("Commande /tenshonext déjà enregistrée.");
            }, () => commands.RemoveHandler(MacroDefinition.Marker));
            registrations.Register(() => framework.Update += Update, () => framework.Update -= Update);
        }
        catch { registrations.Dispose(); throw; }
    }
    private void EnsureHook()
    {
        if (executeHook is not null) return;
        executeHook = interop.HookFromAddress<ExecuteDelegate>((nint)RaptureShellModule.MemberFunctionPointers.ExecuteMacro, OnExecute);
        try { executeHook.Enable(); }
        catch { executeHook.Dispose(); executeHook = null; throw; }
    }
    private void OnExecute(RaptureShellModule* shell, RaptureMacroModule.Macro* macro)
    {
        lock (gate)
        {
            if (ownCallAddress != (nint)macro || ownCallThread != Environment.CurrentManagedThreadId)
                sequence.ObserveExternalStart();
            else { ownCallAddress = 0; ownCallThread = 0; }
            executeHook!.Original(shell, macro);
        }
    }
    private ulong ReadOwner() => client.IsLoggedIn && player.IsLoaded ? player.ContentId : 0;
    private static MacroDefinition ReadMacro(RaptureMacroModule* module, uint book, uint index)
    {
        var macro = module->GetMacro(book, index);
        if (macro == null) throw new InvalidOperationException("Emplacement de macro indisponible.");
        var lines = new string[15];
        for (var line = 0; line < lines.Length; line++) lines[line] = macro->Lines[line].ToString();
        return MacroDefinition.Copy(new(book, index, macro->Name.ToString()), macro->IconId, lines);
    }
    private void Update(IFramework unusedFramework)
    {
        lock (gate)
        {
            if (disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
            updateNumber++;
            try
            {
                var currentOwner = ReadOwner();
                sequence.ValidateOwner(currentOwner);
                if (currentOwner != owner) { owner = currentOwner; copied.Clear(); position = -1; }
                service.Publish(owner, position >= 0, null, sequence.State);
                if (service.TakeCancel()) sequence.Cancel();
                var module = owner == 0 ? null : RaptureMacroModule.Instance();
                var shell = owner == 0 ? null : RaptureShellModule.Instance();
                if (service.TakeRefresh()) { position = 0; copied.Clear(); }
                if (position >= 0 && module != null)
                {
                    var end = Math.Min(200, position + 8);
                    while (position < end)
                    {
                        var id = position++;
                        var definition = ReadMacro(module, (uint)(id / 100), (uint)(id % 100));
                        if (definition.Lines.Any(line => !string.IsNullOrWhiteSpace(line))) copied.Add(definition);
                    }
                    if (position == 200)
                    {
                        if (ReadOwner() == owner) service.Publish(owner, false, copied, sequence.State);
                        copied.Clear(); position = -1;
                    }
                }
                if (service.TakeStart() is { } start)
                {
                    if (owner == 0 || start.CharacterId != owner || module == null || shell == null)
                        throw new InvalidOperationException("Personnage ou macros indisponibles.");
                    if (shell->MacroCurrentLine >= 0 || shell->MacroLocked || shell->IsTextCommandUnavailable)
                        throw new InvalidOperationException("Une macro est déjà active, verrouillée ou indisponible.");
                    var plan = start.Plan.Select(slot => ReadMacro(module, slot.Book, slot.Index)).ToArray();
                    foreach (var macro in plan)
                        if (macro.ValidationError() is { } error) throw new InvalidOperationException($"{macro.Slot.Name} : {error}");
                    if (ReadOwner() != owner) throw new InvalidOperationException("Le personnage a changé.");
                    EnsureHook(); prepared = Array.AsReadOnly(plan); sequence.Start(owner, plan.Select(macro => macro.Slot));
                }
                if (shell != null && module != null)
                {
                    var state = sequence.State;
                    // Absence of a marker is a failure, never a successful end inferred from idle.
                    if (state.Pending is not null && updateNumber > requestedAt && shell->MacroCurrentLine < 0)
                        sequence.Fail("Marqueur de fin non reçu : macro interrompue, refusée ou modifiée.");
                    var request = sequence.PrepareNext(owner, true, shell->MacroCurrentLine >= 0 || shell->IsTextCommandUnavailable, shell->MacroLocked);
                    if (request is not null)
                    {
                        var definition = ReadMacro(module, request.Slot.Book, request.Slot.Index);
                        var validationError = definition.ValidationError();
                        if (validationError is not null || definition.Slot.Name != request.Slot.Name
                            || sequence.State.Completed >= prepared.Count
                            || !definition.Lines.SequenceEqual(prepared[sequence.State.Completed].Lines, StringComparer.Ordinal))
                            sequence.Fail(validationError ?? "La macro a changé ; relancez la séquence après vérification.");
                        else
                        {
                            requestedAt = updateNumber;
                            var macro = module->GetMacro(request.Slot.Book, request.Slot.Index);
                            if (!service.TryDispatch(owner, () =>
                            {
                                if (ReadOwner() != owner) { sequence.ValidateOwner(0); return; }
                                ownCallAddress = (nint)macro; ownCallThread = Environment.CurrentManagedThreadId;
                                try { shell->ExecuteMacro(macro); }
                                finally { ownCallAddress = 0; ownCallThread = 0; }
                            })) sequence.Cancel();
                        }
                    }
                }
                else if (sequence.State.Status is MacroSequenceStatus.Ready or MacroSequenceStatus.AwaitingStart or MacroSequenceStatus.Running)
                    sequence.Fail("État natif indisponible.");
                service.Publish(owner, position >= 0, null, sequence.State);
            }
            catch (Exception error)
            {
                sequence.Fail(error.Message); service.SetError(error.Message); position = -1; copied.Clear();
                service.Publish(owner, false, null, sequence.State);
                log.Warning(error, "Tensho macro sequence request failed.");
            }
        }
    }
    private void OnMarker(string command, string arguments)
    {
        lock (gate)
        {
            try { AcceptMarker(arguments); }
            catch (Exception error)
            {
                sequence.Fail("Échec de lecture du marqueur ; enchaînement arrêté.");
                service.SetError(error.Message);
                log.Warning(error, "Tensho macro marker could not be validated.");
            }
        }
    }
    private void AcceptMarker(string arguments)
    {
            if (disposed || sequence.State.Pending is not { } pending) return;
            if (!framework.IsInFrameworkUpdateThread || arguments.Length != 0 || ReadOwner() != pending.CharacterId)
            { sequence.Fail("Marqueur reçu hors du contexte attendu."); return; }
            var shell = RaptureShellModule.Instance();
            if (shell == null) { sequence.Fail("État natif indisponible au marqueur."); return; }
            var nativeLines = new string[15];
            for (var index = 0; index < 15; index++)
                nativeLines[index] = shell->MacroLines[index].ToString();
            if (!MacroMarkerProtocol.Matches(pending, ReadOwner(), shell->MacroName.ToString(), shell->MacroCurrentLine, nativeLines, arguments))
            {
                var footer = Array.FindIndex(nativeLines, line => line.Trim() == MacroDefinition.Marker);
                sequence.Fail($"Marqueur non concordant : ligne native {shell->MacroCurrentLine}, marqueur {footer}, nom natif « {shell->MacroName} », attendu « {pending.Slot.Name} ». Enchaînement arrêté.");
                return;
            }
            if (sequence.State.Status == MacroSequenceStatus.AwaitingStart) sequence.ObserveStart(pending);
            sequence.ObserveEnd(pending, false);
            // Next macro is scheduled by Update only once the native shell is idle/unlocked.
            service.Publish(owner, position >= 0, null, sequence.State);
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            try { registrations.Dispose(); }
            finally
            {
                sequence.Dispose();
                try { executeHook?.Dispose(); }
                finally { service.Clear(); copied.Clear(); prepared = Array.Empty<MacroDefinition>(); ownCallAddress = 0; }
            }
        }
    }
}
