using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace TenshoAssistant.Macros;

/// <summary>
/// Writes crafting macro pages into the game's macros on the player's click (0.191), on the framework thread: slots chosen by
/// MacroSlotPlan inside the configured range, then for each page the lines (RaptureMacroModule.ReplaceMacroLines), the name and the icon,
/// the module marked for saving (SetSavePendingFlag) and the hotbar slots reloaded (RaptureHotbarModule.ReloadMacroSlots) — the sequence
/// of FFXIVClientStructs calls Craftimizer also uses (functional reference only, no code copied).
/// </summary>
internal sealed class MacroWriter : IDisposable
{
    // Default macro icon of the game (« ? » icon of a new macro).
    private const uint DefaultIcon = 66001;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private readonly object gate = new();
    private (IReadOnlyList<IReadOnlyList<string>> Pages, MacroCopySettings Settings)? pending;
    private string? result;
    public string? Result { get { lock (gate) return result; } }
    // Slot states of both books copied once a second on the framework thread (0.194), so the panel predicts the slots before the click.
    private MacroSlotState[][]? slots;
    private long nextSnapshot;
    public IReadOnlyList<MacroSlotState>? SlotsOf(int book) { lock (gate) return slots is { } copy && book is 0 or 1 ? copy[book] : null; }

    // Status for the player (0.193): the outcome code (« Done:first:last », « NoRoom »…), shown in the chat by the plugin.
    private readonly Action<string>? notify;
    public MacroWriter(IFramework framework, IPluginLog log, Action<string>? notify = null) { this.framework = framework; this.log = log; this.notify = notify; framework.Update += OnUpdate; }

    public void Request(IReadOnlyList<IReadOnlyList<string>> pages, MacroCopySettings settings)
    {
        var copy = new MacroCopySettings { Book = settings.Book, Start = settings.Start, End = settings.End, Prefix = settings.Prefix, OverwriteAny = settings.OverwriteAny, Chain = settings.Chain };
        lock (gate) { pending = (pages.Select(page => (IReadOnlyList<string>)page.ToArray()).ToArray(), copy); result = "Writing"; }
    }

    private unsafe void OnUpdate(IFramework _)
    {
        if (!framework.IsInFrameworkUpdateThread) return;
        Snapshot();
        (IReadOnlyList<IReadOnlyList<string>> Pages, MacroCopySettings Settings) work;
        lock (gate) { if (pending is not { } next) return; work = next; pending = null; }
        string outcome;
        try
        {
            var module = RaptureMacroModule.Instance();
            if (module == null) throw new InvalidOperationException("Macro module unavailable.");
            var book = (uint)work.Settings.Book;
            var plan = MacroSlotPlan.Plan(work.Pages.Count, work.Settings, index =>
            {
                var macro = module->GetMacro(book, (uint)index);
                return macro == null ? new(false, "") : new(!macro->IsNotEmpty(), macro->Name.ToString());
            });
            if (plan.Problem is { } problem) outcome = problem;
            else
            {
                for (var page = 0; page < plan.Slots.Count; page++)
                {
                    var macro = module->GetMacro(book, (uint)plan.Slots[page]);
                    if (macro == null) throw new InvalidOperationException("Macro slot unavailable.");
                    var text = Utf8String.FromString(string.Join('\n', work.Pages[page].Take(15)));
                    try { module->ReplaceMacroLines(macro, text); }
                    finally { text->Dtor(true); }
                    macro->Name.SetString(MacroSlotPlan.Name(work.Settings.Prefix, page + 1, plan.Slots.Count));
                    if (macro->IconId == 0) macro->SetIcon(DefaultIcon);
                    RaptureHotbarModule.Instance()->ReloadMacroSlots((byte)book, (byte)plan.Slots[page]);
                }
                module->SetSavePendingFlag(true, book);
                outcome = $"Done:{plan.Slots[0]}:{plan.Slots[^1]}:{book}:{string.Join(",", plan.Slots)}";
            }
        }
        catch (Exception error) { log.Warning(error, "Crafting macros could not be written into the game."); outcome = "Failed"; }
        lock (gate) result = outcome;
        try { notify?.Invoke(outcome); } catch (Exception error) { log.Warning(error, "Macro copy status could not be shown."); }
    }

    private unsafe void Snapshot()
    {
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        if (now < nextSnapshot) return;
        nextSnapshot = now + System.Diagnostics.Stopwatch.Frequency;
        var module = RaptureMacroModule.Instance();
        if (module == null) { lock (gate) slots = null; return; }
        var copy = new MacroSlotState[2][];
        for (var book = 0; book < 2; book++)
        {
            copy[book] = new MacroSlotState[MacroSlotPlan.Slots];
            for (var index = 0; index < MacroSlotPlan.Slots; index++)
            {
                var macro = module->GetMacro((uint)book, (uint)index);
                copy[book][index] = macro == null ? new(false, "") : new(!macro->IsNotEmpty(), macro->Name.ToString());
            }
        }
        lock (gate) slots = copy;
    }

    public void Dispose() { framework.Update -= OnUpdate; }
}
