using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.Guide;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

// First-run guide (ONBOARD-001, lots 4 to 6, DEC-031). It can be left at any step and started again from Help. The player opens the game's windows;
// the guide only sees, from the published states, that the game has given the data. Nothing is clicked or sent for the player.
internal sealed partial class MainWindow
{
    internal bool GuideNewInstall { get; set; }
    private bool guideOpen;
    private bool guideLooked;
    internal bool GuideOpen => guideOpen;
    internal string GuideTitle() => localization.Get("Guide.Title");
    internal void GuideThemePush() => UI.FfxivTheme.Push();
    internal void GuideThemePop() => UI.FfxivTheme.Pop();
    internal bool GuideVisible() => guideOpen && mapLive?.State is not { InCombat: true } and not { InDuty: true };
    internal void CloseGuide()
    {
        if (!guideOpen) return;
        guideOpen = false;
        configuration.GuideDone = true;
        SaveGuide();
    }
    private void SaveGuide()
    {
        try { save(); } catch (Exception error) { log.Warning(error, "The guide's state could not be saved."); }
    }
    private IReadOnlySet<GuideInterest> GuideChosen => GuideFlow.ParseInterests(configuration.GuideInterests);
    private void StartGuide()
    {
        // A guide left half way carries on; a finished one starts again.
        if (configuration.GuideDone && configuration.GuideStep >= (int)GuideStep.End) { configuration.GuideStep = 0; configuration.GuideStop = 0; }
        configuration.GuideOffered = true;
        configuration.GuideDone = false;
        guideOpen = true;
        SaveGuide();
    }
    // A new install opens the guide the first time Tensho itself is opened, not when the plugin loads: never over a cutscene or a fight.
    private void OpenGuideOnce()
    {
        if (guideLooked) return;
        guideLooked = true;
        if (GuideFlow.OpensByItself(GuideNewInstall, configuration.GuideDone, configuration.GuideOffered)) StartGuide();
    }

    private bool GuideWindowDone(string key)
    {
        bool Bank(BankStorageKind kind) => bank.State.Entries.Any(entry => entry.Key.Kind == kind);
        bool Read(CollectionKind kind) => collections.State.Entries.Any(entry => entry.Kind == kind && (entry.ConfirmedUnlocked || entry.NegativeConfirmed));
        var seen = key switch
        {
            "Retainer" => Bank(BankStorageKind.Retainer),
            "Dresser" => Bank(BankStorageKind.GlamourDresser),
            "Armoire" => Bank(BankStorageKind.Armoire),
            "CompanyChest" => Bank(BankStorageKind.FreeCompanyChest),
            "Workshop" => configuration.Voyages is { Count: > 0 },
            "CraftingLog" => craftLog.Observation is not null,
            "Titles" => Read(CollectionKind.Title),
            "Achievements" => Read(CollectionKind.Achievement),
            "Bestiary" => Beasts?.State.Obtained is not null,
            _ => false,
        };
        // Read once is enough: what only shows while the window is open stays ticked.
        if (seen && (configuration.GuideWindowsDone ??= []).Add(key)) SaveGuide();
        return seen || configuration.GuideWindowsDone?.Contains(key) == true;
    }

    private long guideChecklistNext;
    private bool guideChecklistDone;
    // X-76 (0.956): the guide's « Windows » step works out each task's mark when one of the states it reads is published again (or the tasks,
    // or what is saved, change), not on every frame: the collections are thousands of rows.
    private readonly record struct GuideWindowsKey(object? Collections, object? Bank, object? CraftLog, object? Beasts, bool Voyages, HashSet<string>? Saved, int SavedCount, string Tasks);
    private GuideWindowsKey? guideWindowsKey;
    private bool[] guideWindowsDone = [];
    private void DrawGuideWindows(IReadOnlyList<GuideWindowTask> tasks, bool check = true)
    {
        if (check)
        {
            var key = new GuideWindowsKey(collections.State, bank.State, craftLog.Observation, Beasts?.State, configuration.Voyages is { Count: > 0 },
                configuration.GuideWindowsDone, configuration.GuideWindowsDone?.Count ?? 0, string.Join('|', tasks.Select(task => task.Key)));
            if (guideWindowsKey != key || guideWindowsDone.Length != tasks.Count)
            {
                guideWindowsDone = tasks.Select(task => GuideWindowDone(task.Key)).ToArray();
                // Read after the marks: a mark seen for the first time is saved by GuideWindowDone, and that must not ask for another pass.
                guideWindowsKey = key with { Saved = configuration.GuideWindowsDone, SavedCount = configuration.GuideWindowsDone?.Count ?? 0 };
            }
        }
        for (var index = 0; index < tasks.Count; index++)
        {
            var task = tasks[index];
            var done = check ? guideWindowsDone[index] : configuration.GuideWindowsDone?.Contains(task.Key) == true;
            DrawStateMark(done); ImGui.SameLine();
            ImGui.TextColored(done ? Success : Muted, localization.Get($"Guide.Window{task.Key}"));
            if (!done) { ImGui.Indent(); ImGui.PushTextWrapPos(0); ImGui.TextDisabled(localization.Get($"Guide.Window{task.Key}Where")); ImGui.PopTextWrapPos(); ImGui.Unindent(); }
        }
    }

    // Home, until it is finished or hidden: what is left to open in the game.
    private void DrawGuideOnHome()
    {
        if (GuideFlow.Offered(GuideNewInstall, configuration.GuideDone, configuration.GuideOffered))
        {
            ImGui.TextColored(Accent, localization.Get("Guide.Offer"));
            ImGui.SameLine();
            if (ImGui.SmallButton(localization.Get("Guide.OfferYes"))) StartGuide();
            Tip("Guide.OfferYesTip");
            ImGui.SameLine();
            if (ImGui.SmallButton(localization.Get("Guide.OfferNo"))) { configuration.GuideOffered = true; SaveGuide(); }
            Tip("Guide.OfferNoTip");
        }
        if (configuration.GuideChecklistHidden || !configuration.GuideOffered) return;
        var tasks = GuideFlow.Windows(GuideChosen);
        // 0.511: checked every two seconds: the collections are thousands of rows, and this runs in Draw. Finished is read from what is saved.
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        if (now >= guideChecklistNext)
        {
            guideChecklistNext = now + System.Diagnostics.Stopwatch.Frequency * 2;
            guideChecklistDone = tasks.All(task => GuideWindowDone(task.Key));
        }
        if (guideChecklistDone) return;
        if (TenshoAssistant.UI.UiComponents.Header(localization.Get("Guide.Checklist")))
        {
            DrawGuideWindows(tasks, false);
            if (ImGui.SmallButton(localization.Get("Guide.ChecklistHide"))) { configuration.GuideChecklistHidden = true; SaveGuide(); }
            Tip("Guide.ChecklistHideTip");
        }
    }

    private void GuideGo(GuidePosition to, IReadOnlyList<GuideStop> stops)
    {
        configuration.GuideStep = (int)to.Step;
        configuration.GuideStop = to.Stop;
        // The visit opens each page in Tensho, in the player's sight.
        if (to.Step == GuideStep.Visit && to.Stop < stops.Count && Enum.TryParse<Page>(stops[to.Stop].Page, out var target)) { page = target; IsOpen = true; }
        if (to.Step == GuideStep.End || to.Step != GuideStep.Visit) SaveGuide();
    }

    internal void DrawGuideContents()
    {
        var chosen = GuideChosen;
        var stops = GuideFlow.Stops(chosen);
        var at = GuideFlow.Valid(configuration.GuideStep, configuration.GuideStop, stops.Count);
        ImGui.ProgressBar(GuideFlow.Progress(at, stops.Count), new Vector2(-1, 6), string.Empty);
        ImGui.TextColored(Accent, localization.Get($"Guide.Step{at.Step}Title"));
        var height = Math.Max(120, ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing() * 1.5f);
        var visible = ImGui.BeginChild("GuideStep", new Vector2(0, height));
        try { if (visible) DrawGuideStep(at, stops, chosen); }
        finally { ImGui.EndChild(); }
        ImGui.BeginDisabled(at.Step == GuideStep.Welcome);
        try { if (ImGui.Button(localization.Get("Guide.Back"))) GuideGo(GuideFlow.Previous(at, stops.Count), stops); }
        finally { ImGui.EndDisabled(); }
        Tip("Guide.BackTip");
        ImGui.SameLine();
        if (at.Step == GuideStep.End)
        {
            if (ImGui.Button(localization.Get("Guide.Finish"))) { CloseGuide(); page = Page.Home; }
            Tip("Guide.FinishTip");
        }
        else
        {
            if (ImGui.Button(localization.Get("Guide.Next"))) GuideGo(GuideFlow.Next(at, stops.Count), stops);
            Tip("Guide.NextTip");
            ImGui.SameLine();
            if (ImGui.Button(localization.Get("Guide.Leave"))) CloseGuide();
            Tip("Guide.LeaveTip");
        }
    }

    private void DrawGuideStep(GuidePosition at, IReadOnlyList<GuideStop> stops, IReadOnlySet<GuideInterest> chosen)
    {
        if (at.Step != GuideStep.Visit) { ImGui.PushTextWrapPos(0); ImGui.TextUnformatted(localization.Get($"Guide.Step{at.Step}Text")); ImGui.PopTextWrapPos(); ImGui.Spacing(); }
        switch (at.Step)
        {
            case GuideStep.Welcome:
                DrawLanguageSetting();
                break;
            case GuideStep.Sources:
                BoolSetting("Advanced.Universalis", "Advanced.UniversalisTip", () => configuration.CommunityUniversalis, value => { configuration.CommunityUniversalis = value; if (!value) market.CancelBackground(); }, c => c.CommunityUniversalis);
                BoolSetting("Advanced.Garland", "Advanced.GarlandTip", () => configuration.CommunityGarland, value => configuration.CommunityGarland = value, c => c.CommunityGarland);
                BoolSetting("Advanced.Wiki", "Advanced.WikiTip", () => configuration.CommunityWiki, value => configuration.CommunityWiki = value, c => c.CommunityWiki);
                BoolSetting("Advanced.Lumina", "Advanced.LuminaTip", () => configuration.CommunityLumina, value => configuration.CommunityLumina = value, c => c.CommunityLumina);
                BoolSetting("Advanced.GatherBuddy", "Advanced.GatherBuddyTip", () => configuration.CommunityGatherBuddy, value => configuration.CommunityGatherBuddy = value, c => c.CommunityGatherBuddy);
                BoolSetting("Advanced.Teamcraft", "Advanced.TeamcraftTip", () => configuration.CommunityTeamcraftBites, value => configuration.CommunityTeamcraftBites = value, c => c.CommunityTeamcraftBites);
                BoolSetting("Advanced.Fashion", "Advanced.FashionTip", () => configuration.CommunityFashion, value => configuration.CommunityFashion = value, c => c.CommunityFashion);
                break;
            case GuideStep.Settings:
                // X-76 H5 (0.915): « ↺ » on these rows too, and the map row has the same tooltip as in Settings › Map.
                BoolSetting("MapView.ReplaceGameMap", "MapView.ReplaceGameMapHelp", () => configuration.MapView.ReplaceGameMap, value => configuration.MapView.ReplaceGameMap = value, c => c.MapView.ReplaceGameMap);
                BoolSetting("Settings.UsageHover", "InterfaceHelp.UsageHelp", () => configuration.ItemUsagesOnHover, value => configuration.ItemUsagesOnHover = value, c => c.ItemUsagesOnHover);
                BoolSetting("Settings.ContextMenu", "InterfaceHelp.MenuHelp", () => configuration.ItemContextMenuEnabled, value => configuration.ItemContextMenuEnabled = value, c => c.ItemContextMenuEnabled);
                BoolSetting("BankUi.AutoSetting", "BankUi.AutoSettingHelp", () => configuration.AutoBankCapture, value => configuration.AutoBankCapture = value, c => c.AutoBankCapture);
                BoolSetting("Settings.OpenOnLogin", "Settings.OpenOnLoginHelp", () => configuration.OpenOnLogin, value => configuration.OpenOnLogin = value, c => c.OpenOnLogin);
                BoolSetting("Settings.HelpBanners", "SettingTips.HelpBanners", () => configuration.HelpBanners, value => configuration.HelpBanners = value, c => c.HelpBanners);
                break;
            case GuideStep.Interests:
                foreach (var interest in Enum.GetValues<GuideInterest>())
                {
                    var on = chosen.Contains(interest);
                    if (ImGui.Checkbox(localization.Get($"Guide.Interest{interest}") + $"###GuideInterest{interest}", ref on))
                    {
                        var saved = configuration.GuideInterests ??= [];
                        if (on) saved.Add(interest.ToString()); else saved.Remove(interest.ToString());
                        SaveGuide();
                    }
                    Tip("Guide.InterestTip");
                }
                break;
            case GuideStep.Windows:
                DrawGuideWindows(GuideFlow.Windows(chosen));
                break;
            case GuideStep.Visit:
                if (at.Stop < stops.Count && Enum.TryParse<Page>(stops[at.Stop].Page, out var target))
                {
                    if (page != target) { if (ImGui.SmallButton(localization.Get("Help.OpenPage"))) { page = target; IsOpen = true; } Tip("Guide.OpenPageTip"); }
                    var topic = TopicOf(target);
                    ImGui.TextColored(Accent, $"{topic.Title} · {at.Stop + 1}/{stops.Count}");
                    DrawHelpText(topic);
                }
                break;
            default:
                if (ImGui.SmallButton(localization.Get("Help.AllHelp"))) { page = Page.Help; IsOpen = true; }
                Tip("Help.AllHelpTip");
                break;
        }
    }
}
