using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Help;

namespace TenshoAssistant;

// Help (ONBOARD-001, lot 1, DEC-031): a « ? » beside the title explains the open page, and Preferences › Help searches all of them. Every page has
// its four texts (PageHelp.<Page>Title, Purpose, Start, Empty): a test refuses a page without them.
internal sealed partial class MainWindow
{
    private string helpQuery = string.Empty;
    private string? helpKey;
    private IReadOnlyList<HelpTopic> helpView = [];
    private HelpTopic[]? helpTopics;

    private HelpTopic TopicOf(Page help)
    {
        string Text(string part) => localization.Get($"PageHelp.{help}{part}");
        return new(help.ToString(), Text("Title"), Text("Purpose"), Text("Start"), Text("Empty"));
    }

    // 0.503 (lot 2): what this page has read on this character. Published states only, refreshed every two seconds while the help is open.
    private (Page Page, long Next, HelpPageState State, string[] Raw) helpState;
    private bool helpPopupRequested;
    private void DrawHelpState()
    {
        var domains = HelpIndex.DomainsOf(page.ToString());
        if (domains.Count == 0) return;
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        if (helpState.Page != page || now >= helpState.Next)
        {
            var loggedIn = characterStats.State.CharacterId != 0;
            var lines = SelfCheckProbes(loggedIn).Where(probe => domains.Contains(probe.Domain)).Select(probe => Diagnostics.SelfCheck.Run(probe.Domain, probe.Probe)).ToArray();
            helpState = (page, now + System.Diagnostics.Stopwatch.Frequency * 2, HelpIndex.StateOf(lines), lines.Select(line => line.Format()).ToArray());
        }
        if (helpState.State == HelpPageState.Unknown) return;
        ImGui.TextColored(helpState.State == HelpPageState.Ready ? Success : helpState.State == HelpPageState.Waiting ? Muted : Accent, localization.Get("Help.State" + helpState.State));
        if (DeveloperMode) foreach (var raw in helpState.Raw) ImGui.TextDisabled(raw);
        ImGui.Spacing();
    }

    // 0.503 (lot 2, G8): the first time a page is opened, two lines on it; it does not come back. Setting in General.
    private void DrawHelpBanner()
    {
        if (!configuration.HelpBanners || page == Page.Help || configuration.HelpSeen?.Contains(page.ToString()) == true) return;
        ImGui.TextColored(Accent, localization.Get("Help.BannerTitle"));
        ImGui.TextWrapped(localization.Get($"PageHelp.{page}Purpose"));
        void Seen()
        {
            (configuration.HelpSeen ??= []).Add(page.ToString());
            try { save(); } catch (Exception error) { log.Warning(error, "The help seen could not be saved."); }
        }
        if (ImGui.SmallButton(localization.Get("Help.BannerSeen") + "##HelpBannerSeen")) Seen();
        Tip("Help.BannerSeenTip");
        ImGui.SameLine();
        if (ImGui.SmallButton(localization.Get("Help.BannerMore") + "##HelpBannerMore")) { Seen(); helpPopupRequested = true; }
        Tip("Help.ButtonTip");
        ImGui.Separator();
    }

    private void DrawHelpText(HelpTopic topic)
    {
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + Math.Min(460, Math.Max(240, ImGui.GetContentRegionAvail().X)));
        try
        {
            ImGui.TextUnformatted(topic.Purpose);
            ImGui.Spacing();
            ImGui.TextColored(Accent, localization.Get("Help.Start"));
            ImGui.TextUnformatted(topic.Start);
            ImGui.Spacing();
            ImGui.TextColored(Accent, localization.Get("Help.Empty"));
            ImGui.TextUnformatted(topic.Empty);
        }
        finally { ImGui.PopTextWrapPos(); }
    }

    private void DrawPageHelpButton()
    {
        ImGui.SameLine(Math.Max(ImGui.GetCursorPosX(), ImGui.GetWindowContentRegionMax().X - 28));
        if (ImGui.SmallButton("?##PageHelp") || helpPopupRequested) { helpPopupRequested = false; ImGui.OpenPopup("PageHelpPopup"); }
        Tip("Help.ButtonTip");
        if (!ImGui.BeginPopup("PageHelpPopup")) return;
        try
        {
            var topic = TopicOf(page);
            ImGui.TextColored(Accent, topic.Title);
            ImGui.Separator();
            DrawHelpState();
            DrawHelpText(topic);
            ImGui.Spacing();
            if (ImGui.SmallButton(localization.Get("Help.AllHelp"))) { page = Page.Help; ImGui.CloseCurrentPopup(); }
            Tip("Help.AllHelpTip");
        }
        finally { ImGui.EndPopup(); }
    }

    private void DrawHelpPage()
    {
        Heading(localization.Get("PageHelp.HelpTitle"), localization.Get("PageHelp.HelpPurpose"));
        ImGui.SetNextItemWidth(320); ImGui.InputTextWithHint("##HelpQuery", localization.Get("Help.Search"), ref helpQuery, 64); Tip("Help.SearchTip");
        if (TenshoAssistant.UI.UiComponents.Header(localization.Get("Help.Faq"), ImGuiTreeNodeFlags.DefaultOpen))
            foreach (var question in new[] { "EmptyPage", "WhyOpen", "Marks", "Internet", "Actions", "Report" })
            {
                if (!ImGui.TreeNode(localization.Get($"Help.Question{question}") + $"###HelpQuestion{question}")) continue;
                try { ImGui.TextWrapped(localization.Get($"Help.Answer{question}")); }
                finally { ImGui.TreePop(); }
            }
        if (TenshoAssistant.UI.UiComponents.Header(localization.Get("Help.Pages"), ImGuiTreeNodeFlags.DefaultOpen))
        {
            helpTopics ??= PageEntries.Select(entry => TopicOf(entry.Page)).ToArray();
            if (helpKey != helpQuery) { helpKey = helpQuery; helpView = HelpIndex.Search(helpTopics, helpQuery); }
            if (helpView.Count == 0) ImGui.TextDisabled(localization.Get("Help.NoResult"));
            foreach (var topic in helpView)
            {
                if (!ImGui.TreeNode(topic.Title + "###HelpTopic" + topic.Key)) continue;
                try
                {
                    DrawHelpText(topic);
                    if (Enum.TryParse<Page>(topic.Key, out var target) && target != Page.Help && ImGui.SmallButton(localization.Get("Help.OpenPage") + "##Open" + topic.Key)) page = target;
                }
                finally { ImGui.TreePop(); }
            }
        }
        if (TenshoAssistant.UI.UiComponents.Header(localization.Get("Help.Contribute")))
        {
            ImGui.TextWrapped(localization.Get("Help.ContributeText"));
            if (ImGui.SmallButton(localization.Get("Advanced.CopyReport") + "##HelpReport")) ImGui.SetClipboardText(BugReport());
            Tip("Advanced.CopyReportTip");
        }
    }
}
