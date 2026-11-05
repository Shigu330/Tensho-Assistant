using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace TenshoAssistant;

/// <summary>
/// 0.516 (issue #38): the panels attached to a game window go through the Dalamud window system. A panel decides each frame whether it shows, before the windows are drawn, and asks for it
/// with <see cref="Show"/>; its window exists from the first request on and stays closed on the frames nobody asks. The id keeps the name the panel always had. A panel that throws turns off alone.
/// </summary>
internal sealed class AttachedPanels : IDisposable
{
    private readonly WindowSystem windows;
    private readonly MainWindow owner;
    private readonly Dictionary<string, AttachedWindow> all = [];
    private readonly HashSet<string> broken = [];
    // 0.535 (user request): closed by the player until the game window goes away; its last request, so that a short gap in a reading does not bring it back.
    private readonly Dictionary<string, long> dismissed = [];
    private const double DismissForgetSeconds = 3;

    public AttachedPanels(WindowSystem windows, MainWindow owner)
    {
        this.windows = windows;
        this.owner = owner;
    }

    // 0.537 (user request): each panel steps aside when a game window comes in front of the one it belongs to. Names: the readers of each game window.
    private static readonly Dictionary<string, string[]> Anchors = new()
    {
        ["TenshoGatheringPanel"] = ["Gathering"], ["TenshoCollectablePanel"] = ["GatheringMasterpiece"], ["TenshoCraftLog"] = ["RecipeNote"], ["TenshoCraftLivePanel"] = ["Synthesis"],
        ["TenshoSpearfishingPanel"] = ["SpearFishing"], ["TenshoCharacterPanel"] = ["Character"], ["TenshoDutyFinder"] = ["ContentsFinder"], ["TenshoFashionSlotWindow"] = ["FashionCheck"],
        ["TenshoRetainerSales"] = ["RetainerSellList"], ["TenshoTriadRequest"] = ["TripleTriad", "GSInfoEditDeck"], ["TenshoInventorySort"] = ["Inventory"],
        ["TenshoBankImportRetainer"] = ["Inventory", "Retainer"], ["TenshoBankImportFreeCompany"] = ["FreeCompanyChest", "Inventory"],
    };
    /// <summary>The hover panel goes with its tooltip; the sort button leaves whatever the setting.</summary>
    private const string HoverPanel = "TenshoItemHover", SortButton = "TenshoInventorySort";

    private bool Covered(string id, Vector2? at, Vector2 size)
    {
        if (at is not { } position || id == HoverPanel || !owner.HidePanelsBehindGame && id != SortButton) return false;
        var screen = ImGui.GetMainViewport().Size;
        return UI.GameWindows.Covers(owner.GameWindowList, position.X, position.Y, size.X, size.Y, screen.X, screen.Y, Anchors.GetValueOrDefault(id));
    }

    /// <param name="prepare">The window's position and size (SetNextWindow…), run just before the window opens.</param>
    /// <param name="close">What the close button does; none means no close button.</param>
    /// <param name="padding">The window's padding, when it is not the theme's.</param>
    public void Show(string id, string title, ImGuiWindowFlags flags, Action? prepare, Action contents, Action? close = null, Vector2? padding = null)
    {
        if (broken.Contains(id)) return;
        if (dismissed.ContainsKey(id)) { dismissed[id] = System.Diagnostics.Stopwatch.GetTimestamp(); return; }
        if (!all.TryGetValue(id, out var window))
        {
            window = new AttachedWindow(this, id);
            windows.AddWindow(window);
            all[id] = window;
        }
        window.Request = new(title, flags, prepare, contents, close, padding);
    }

    /// <summary>After every panel has asked, before the windows are drawn.</summary>
    public void Apply()
    {
        foreach (var window in all.Values)
        {
            window.IsOpen = window.Request is not null;
            window.Hidden = window.Request is not null && Covered(window.Key, window.LastPosition, window.LastSize);
        }
    }

    /// <summary>After the windows are drawn: a panel asks again on the next frame, or closes.</summary>
    public void EndFrame()
    {
        foreach (var window in all.Values) window.Request = null;
        foreach (var (id, last) in dismissed.ToArray())
            if (System.Diagnostics.Stopwatch.GetElapsedTime(last).TotalSeconds >= DismissForgetSeconds) dismissed.Remove(id);
    }

    public void Dispose()
    {
        foreach (var window in all.Values) windows.RemoveWindow(window);
        all.Clear();
    }

    private void TurnOff(string id, Exception error)
    {
        broken.Add(id);
        owner.PanelFailed(id, error);
    }

    private sealed record PanelRequest(string Title, ImGuiWindowFlags Flags, Action? Prepare, Action Contents, Action? Close, Vector2? Padding);

    private sealed class AttachedWindow : Window
    {
        private readonly AttachedPanels host;
        private readonly string id;
        private bool paddingPushed;

        public PanelRequest? Request { get; set; }
        public bool Hidden { get; set; }
        public string Key => id;
        public Vector2? LastPosition { get; private set; }
        public Vector2 LastSize { get; private set; }

        public AttachedWindow(AttachedPanels host, string id) : base("###" + id)
        {
            this.host = host;
            this.id = id;
            // As before the window system: Escape closes nothing, and a panel placed by its game window is neither pinned nor made click-through.
            RespectCloseHotkey = false;
            DisableWindowSounds = true;
            AllowPinning = false;
            AllowClickthrough = false;
        }

        public override bool DrawConditions() => Request is not null && !Hidden;

        public override void PreDraw()
        {
            if (Request is not { } request) return;
            WindowName = request.Title + "###" + id;
            // 0.535 (user request): whatever has a title bar can be collapsed and closed.
            Flags = request.Flags & ~ImGuiWindowFlags.NoCollapse;
            ShowCloseButton = (request.Flags & ImGuiWindowFlags.NoTitleBar) == 0;
            UI.FfxivTheme.Push();
            try { request.Prepare?.Invoke(); }
            catch (Exception error) { host.TurnOff(id, error); }
            if (request.Padding is { } padding) { ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, padding); paddingPushed = true; }
        }

        public override void Draw()
        {
            PopPadding();
            LastPosition = ImGui.GetWindowPos();
            LastSize = ImGui.GetWindowSize();
            if (Request is not { } request) return;
            var started = host.owner.DeveloperMode ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            try { request.Contents(); }
            catch (Exception error) { host.TurnOff(id, error); }
            if (started != 0) host.owner.RecordOverlayDraw(id, System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }

        public override void PostDraw()
        {
            PopPadding();
            UI.FfxivTheme.Pop();
            // Apply copies the request into IsOpen before OnClose could run: the close button is answered here, in the same frame.
            if (!IsOpen && Request is { } request)
            {
                if (request.Close is { } close) close();
                else host.dismissed[id] = System.Diagnostics.Stopwatch.GetTimestamp();
            }
        }

        private void PopPadding()
        {
            if (paddingPushed) { ImGui.PopStyleVar(); paddingPushed = false; }
        }
    }
}
