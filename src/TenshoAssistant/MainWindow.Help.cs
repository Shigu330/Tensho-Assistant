using System.Numerics;
using System.Text;
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

    // 0.506 (lot 7, G6, G7): the community's videos and guides for a page. The list is read on a click; a link goes to the browser after showing its address.
    internal CommunityHelpIndex? CommunityHelp { get; set; }
    private CommunityGuide? guideToOpen;
    private void DrawCommunityGuides(string key)
    {
        if (CommunityHelp is not { } index) return;
        ImGui.Spacing();
        var state = index.State;
        if (state.Guides is not { } guides)
        {
            ImGui.BeginDisabled(state.Loading);
            try { if (ImGui.SmallButton(localization.Get("Help.GuidesLoad") + "##Guides" + key)) _ = index.LoadAsync(PageEntries.Select(entry => entry.Page.ToString()).ToHashSet()); }
            finally { ImGui.EndDisabled(); }
            Tip("Help.GuidesLoadTip");
            if (state.Problem is { } problem) ImGui.TextDisabled(localization.Get("Help.Guides" + problem));
            return;
        }
        var shown = CommunityHelpIndex.For(guides, key, localization.CurrentCode);
        if (shown.Count == 0) { ImGui.TextDisabled(localization.Get("Help.GuidesNone")); return; }
        ImGui.TextColored(Accent, localization.Get("Help.GuidesTitle"));
        foreach (var guide in shown)
        {
            if (ImGui.Selectable($"{guide.Title}{(guide.Author is { } author ? " · " + author : "")}{(guide.ForVersion is { } version ? " · " + version : "")}##{guide.Address}")) guideToOpen = guide;
            if (ImGui.IsItemHovered()) WrappedTooltip(guide.Address);
        }
        if (guideToOpen is { } asked && asked.Page == key)
        {
            ImGui.TextWrapped(localization.Format("Help.GuidesConfirm", asked.Address));
            if (ImGui.SmallButton(localization.Get("Help.GuidesOpen") + "##Guide" + key)) { Dalamud.Utility.Util.OpenLink(asked.Address); guideToOpen = null; }
            Tip("Help.GuidesOpenTip");
            ImGui.SameLine();
            if (ImGui.SmallButton(localization.Get("Help.GuidesCancel") + "##Guide" + key)) guideToOpen = null;
            Tip("Help.GuidesCancelTip");
        }
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
            DrawCommunityGuides(topic.Key);
            ImGui.Spacing();
            if (ImGui.SmallButton(localization.Get("Help.AllHelp"))) { page = Page.Help; ImGui.CloseCurrentPopup(); }
            Tip("Help.AllHelpTip");
            ImGui.SameLine();
            if (ImGui.SmallButton(localization.Get("Help.Contribute") + "##HelpProblem")) { problemRequested = true; problemReport = string.Empty; page = Page.Help; ImGui.CloseCurrentPopup(); }
            Tip("Help.FormOpenTip");
        }
        finally { ImGui.EndPopup(); }
    }

    // System plan 4.19 (F3): the questions are one list (Help/Faq.cs), searched like the pages; docs/FAQ.md is written from it (tools/faq.py).
    private string? faqKey;
    private IReadOnlyList<HelpTopic> faqView = [];
    private void DrawFaq()
    {
        var language = localization.CurrentCode + "|" + helpQuery;
        if (faqKey != language)
        {
            faqKey = language;
            faqView = HelpIndex.Search(Faq.Entries.Select(entry => new HelpTopic(entry.Key, localization.Get($"Help.Question{entry.Key}"), localization.Get($"Help.Answer{entry.Key}"), "", "")), helpQuery);
        }
        if (!TenshoAssistant.UI.UiComponents.Header(localization.Get("Help.Faq"), ImGuiTreeNodeFlags.DefaultOpen)) return;
        if (faqView.Count == 0) ImGui.TextDisabled(localization.Get("Help.FaqNoResult"));
        foreach (var topic in faqView)
        {
            if (!ImGui.TreeNode(topic.Title + $"###HelpQuestion{topic.Key}")) continue;
            try
            {
                ImGui.TextWrapped(topic.Purpose);
                if (Faq.PageOf(topic.Key) is { } name && Enum.TryParse<Page>(name, out var target) && ImGui.SmallButton(localization.Get("Help.OpenPage") + "##OpenFaq" + topic.Key)) page = target;
            }
            finally { ImGui.TreePop(); }
        }
    }

    private void DrawHelpPage()
    {
        Heading(localization.Get("PageHelp.HelpTitle"), localization.Get("PageHelp.HelpPurpose"));
        if (ImGui.SmallButton(localization.Get("Guide.Start"))) StartGuide();
        Tip("Guide.StartTip");
        ImGui.SetNextItemWidth(320); ImGui.InputTextWithHint("##HelpQuery", localization.Get("Help.Search"), ref helpQuery, 64); Tip("Help.SearchTip");
        DrawFaq();
        if (TenshoAssistant.UI.UiComponents.Header(localization.Get("Help.Pages"), ImGuiTreeNodeFlags.DefaultOpen))
        {
            // 0.511: rebuilt when the language changes, or the help stayed in the old one.
            var language = localization.CurrentCode + "|" + helpQuery;
            if (helpKey != language)
            {
                helpKey = language;
                helpTopics = PageEntries.Select(entry => TopicOf(entry.Page)).ToArray();
                helpView = HelpIndex.Search(helpTopics, helpQuery);
            }
            if (helpView.Count == 0) ImGui.TextDisabled(localization.Get("Help.NoResult"));
            foreach (var topic in helpView)
            {
                if (!ImGui.TreeNode(topic.Title + "###HelpTopic" + topic.Key)) continue;
                try
                {
                    DrawHelpText(topic);
                    DrawCommunityGuides(topic.Key);
                    if (Enum.TryParse<Page>(topic.Key, out var target) && target != Page.Help && ImGui.SmallButton(localization.Get("Help.OpenPage") + "##Open" + topic.Key)) page = target;
                }
                finally { ImGui.TreePop(); }
            }
        }
        if (problemRequested) { problemRequested = false; ImGui.SetNextItemOpen(true); }
        if (TenshoAssistant.UI.UiComponents.Header(localization.Get("Help.Contribute"))) DrawProblemForm();
    }

    // 0.504 (lot 3, G5): a form in the game, then the GitHub form filled in, in the player's browser. The plugin sends nothing and holds no token.
    private bool problemRequested;
    private int problemKind;
    private string problemTitle = string.Empty, problemWhat = string.Empty, problemExpected = string.Empty, problemReport = string.Empty;
    private bool problemLeftOut;

    private string ProblemReportText()
    {
        var text = new StringBuilder(BugReport());
        if (SelfCheck?.LastLines is { Count: > 0 } lines) foreach (var line in lines) text.AppendLine(line.Format());
        text.AppendLine("Sent from the in-game form.");
        return ProblemReport.Clean(text.ToString());
    }

    private void DrawProblemForm()
    {
        ImGui.TextWrapped(localization.Get("Help.ContributeText"));
        for (var index = 0; index < 3; index++)
        {
            if (index > 0) ImGui.SameLine();
            if (ImGui.RadioButton(localization.Get(((ProblemKind)index) switch { ProblemKind.Idea => "Help.KindIdea", ProblemKind.Translation => "Help.KindTranslation", _ => "Help.KindBug" }), problemKind == index)) problemKind = index;
            Tip("Help.KindTip");
        }
        var kind = (ProblemKind)problemKind;
        ImGui.SetNextItemWidth(Math.Min(520, ImGui.GetContentRegionAvail().X));
        ImGui.InputTextWithHint("##ProblemTitle", localization.Get("Help.FormTitle"), ref problemTitle, 200); Tip("Help.FormTitleTip");
        var size = new Vector2(Math.Min(520, ImGui.GetContentRegionAvail().X), 70);
        ImGui.TextUnformatted(localization.Get(kind == ProblemKind.Bug ? "Help.FormWhat" : kind == ProblemKind.Idea ? "Help.FormNeed" : "Help.FormTexts"));
        ImGui.InputTextMultiline("##ProblemWhat", ref problemWhat, 3000, size); Tip("Help.FormWhatTip");
        if (kind != ProblemKind.Translation)
        {
            ImGui.TextUnformatted(localization.Get(kind == ProblemKind.Bug ? "Help.FormExpected" : "Help.FormExample"));
            ImGui.InputTextMultiline("##ProblemExpected", ref problemExpected, 3000, size); Tip("Help.FormExpectedTip");
        }
        if (kind == ProblemKind.Bug)
        {
            ImGui.TextUnformatted(localization.Get("Help.FormReport"));
            if (problemReport.Length == 0) problemReport = ProblemReportText();
            ImGui.InputTextMultiline("##ProblemReport", ref problemReport, 8000, new Vector2(size.X, 130)); Tip("Help.FormReportTip");
            if (ImGui.SmallButton(localization.Get("Help.FormRefresh"))) problemReport = ProblemReportText();
            Tip("Help.FormRefreshTip");
        }
        ImGui.Spacing();
        if (ImGui.SmallButton(localization.Get("Help.FormSearch")))
            Dalamud.Utility.Util.OpenLink(ProblemReport.SearchAddress(problemTitle));
        Tip("Help.FormSearchTip");
        ImGui.SameLine();
        ImGui.BeginDisabled(problemTitle.Trim().Length == 0);
        try
        {
            if (ImGui.SmallButton(localization.Get("Help.FormOpen")))
            {
                var link = ProblemReport.Link(new(kind, problemTitle, problemWhat, problemExpected, problemReport, BuildVersion, kind == ProblemKind.Translation ? localization.CurrentCode : $"Plugin language {localization.CurrentCode}"));
                problemLeftOut = link.ReportLeftOut;
                if (link.ReportLeftOut) ImGui.SetClipboardText(ProblemReport.Clean($"{problemWhat}\n\n{problemExpected}\n\n{problemReport}"));
                Dalamud.Utility.Util.OpenLink(link.Url);
            }
        }
        finally { ImGui.EndDisabled(); }
        Tip("Help.FormOpenTip");
        if (problemLeftOut) ImGui.TextColored(Accent, localization.Get("Help.FormLeftOut"));
        if (ImGui.SmallButton(localization.Get("Help.FormSecurity"))) Dalamud.Utility.Util.OpenLink(ProblemReport.SecurityAddress);
        Tip("Help.FormSecurityTip");
    }
}
