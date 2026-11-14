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
    private bool HelpBannerShows(Page shown) => configuration.HelpBanners && shown != Page.Help && configuration.HelpSeen?.Contains(shown.ToString()) != true;

    private void DrawHelpBanner()
    {
        if (!HelpBannerShows(page)) return;
        ImGui.TextColored(Accent, localization.Get("Help.BannerTitle"));
        // 0.1222 (X-257, D5): on Home, where to start rather than what the page is.
        ImGui.TextWrapped(localization.Get(page == Page.Home ? "PageHelp.HomeStart" : $"PageHelp.{page}Purpose"));
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

    // 0.504 (lot 3, G5): a form in the game, then the GitHub form filled in, in the player's browser.
    // 0.1324 (X-340, user's decision of 2026-11-14, R04): with the discoveries' GitHub connection, the issue may instead be sent directly, on the
    // player's click under the preview of its exact text; without it (or by choice), the browser's form as before.
    private bool problemRequested;
    private int problemKind;
    private string problemTitle = string.Empty, problemWhat = string.Empty, problemExpected = string.Empty, problemReport = string.Empty;
    private bool problemLeftOut, problemDirect = true, problemSent, problemRefused;
    // 0.1125 (X-143 part 3, audit A08): the browser opens only from the preview, which shows the exact strings of the address and the clipboard;
    // a change to the draft after the preview drops it (the player confirms what is sent, not an older text). X-340: a change of the way to send
    // drops it too (the preview is that of the way chosen: the issue's title and body, or the form's fields and clipboard).
    private (ProblemDraft Draft, bool Direct, ProblemSend Send, ProblemIssue Issue)? problemPreview;
    private ProblemDraft ProblemDraftNow(ProblemKind kind)
        => new(kind, problemTitle, problemWhat, problemExpected, problemReport, BuildVersion, kind == ProblemKind.Translation ? localization.CurrentCode : $"Plugin language {localization.CurrentCode}");

    /// <summary>X-340: the step to do in the browser, framed so it is seen: the report is in the clipboard, to paste into the form.</summary>
    private void DrawPasteStep(string text, float width)
    {
        var start = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(0, 2));
        ImGui.Indent(8);
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + width - 16);
        ImGui.TextColored(Warning, text);
        ImGui.PopTextWrapPos();
        ImGui.Unindent(8);
        var end = new Vector2(start.X + width, ImGui.GetItemRectMax().Y + 4);
        ImGui.Dummy(new Vector2(0, 2));
        ImGui.GetWindowDrawList().AddRect(start, end, ImGui.GetColorU32(Warning), 4, ImDrawFlags.None, 1.5f);
    }

    /// <summary>X-340: the direct report's result: on its way, sent (with a button to open the issue) or the cause of a failure.</summary>
    private void DrawReportResult(Discoveries.GitHubExchange exchange)
    {
        if (exchange.ReportMessage is not { } said) { ImGui.TextDisabled(localization.Get("Help.ReportSending")); return; }
        if (said.Key == "ReportSent" && said.Args.Length >= 2)
        {
            ImGui.TextColored(Success, localization.Format("Help.ReportSent", said.Args[0]));
            ImGui.SameLine();
            if (ImGui.SmallButton(localization.Get("Help.ReportOpen"))) Dalamud.Utility.Util.OpenLink(said.Args[1]);
            Tip("Help.ReportOpenTip");
            return;
        }
        var text = said.Key switch
        {
            "ReportAppMissing" => localization.Get("Help.ReportAppMissing"),
            "ExchangeUnauthorized" or "ExchangeExpired" => localization.Get("DiscoveriesUi." + said.Key),
            _ => localization.Format("Help.ReportFailed", said.Args.Length > 0 ? said.Args[0] : said.Key),
        };
        ImGui.TextColored(Error, text);
    }

    private string ProblemReportText()
    {
        var text = new StringBuilder(BugReport());
        if (SelfCheck?.LastLines is { Count: > 0 } lines) foreach (var line in lines) text.AppendLine(ProblemReport.Clean(line.Format()));
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
        var exchange = DiscoveryExchange;
        var connected = exchange?.Connected == true;
        if (connected)
        {
            if (ImGui.RadioButton(localization.Format("Help.RouteDirect", exchange!.Login), problemDirect)) problemDirect = true;
            Tip("Help.RouteDirectTip");
            ImGui.SameLine();
            if (ImGui.RadioButton(localization.Get("Help.RouteBrowser"), !problemDirect)) problemDirect = false;
            Tip("Help.RouteBrowserTip");
        }
        var direct = connected && problemDirect;
        ImGui.BeginDisabled(problemTitle.Trim().Length == 0);
        try
        {
            if (ImGui.Button(localization.Get("Help.FormOpen")))
            {
                var draft = ProblemDraftNow(kind);
                problemPreview = (draft, direct, ProblemReport.Prepare(draft), ProblemReport.Issue(draft));
                problemLeftOut = problemSent = problemRefused = false;
            }
        }
        finally { ImGui.EndDisabled(); }
        Tip(direct ? "Help.FormOpenDirectTip" : "Help.FormOpenTip");
        if (!connected) ImGui.TextDisabled(localization.Get("Help.RouteConnectHint"));
        if (problemPreview is { } preview)
        {
            if (ProblemDraftNow(kind) != preview.Draft || preview.Direct != direct) problemPreview = null;
            else
            {
                ImGui.TextColored(Accent, localization.Get("Help.FormPreview"));
                var shown = direct ? preview.Issue.Preview() : preview.Send.Preview();
                ImGui.InputTextMultiline("##ProblemPreview", ref shown, Math.Max(shown.Length + 1, 16), new Vector2(size.X, 150), ImGuiInputTextFlags.ReadOnly);
                Tip(direct ? "Help.FormPreviewDirectTip" : "Help.FormPreviewTip");
                if (!direct && preview.Send.ReportLeftOut) DrawPasteStep(localization.Get("Help.FormPasteBefore"), size.X);
                if (direct)
                {
                    ImGui.BeginDisabled(exchange!.Busy);
                    try
                    {
                        if (ImGui.Button(localization.Get("Help.FormSendDirect")))
                        {
                            // The only place a report leaves by the connection: this click, with the issue whose text is shown just above.
                            if (exchange.Report(preview.Issue)) { problemSent = true; problemPreview = null; }
                            else problemRefused = true;
                        }
                    }
                    finally { ImGui.EndDisabled(); }
                    Tip("Help.FormSendDirectTip");
                }
                else
                {
                    if (ImGui.Button(localization.Get("Help.FormSend")))
                    {
                        problemLeftOut = preview.Send.ReportLeftOut;
                        if (preview.Send.Clipboard is { } clipboard) ImGui.SetClipboardText(clipboard);
                        Dalamud.Utility.Util.OpenLink(preview.Send.Url);
                        problemPreview = null;
                    }
                    Tip("Help.FormSendTip");
                }
                ImGui.SameLine();
                if (ImGui.SmallButton(localization.Get("Help.FormCancel"))) problemPreview = null;
                Tip("Help.FormCancelTip");
                if (problemRefused || direct && exchange!.Busy) ImGui.TextDisabled(localization.Get("Help.ReportBusy"));
            }
        }
        if (problemLeftOut) DrawPasteStep(localization.Get("Help.FormLeftOut"), size.X);
        if (problemSent && exchange is not null) DrawReportResult(exchange);
        if (ImGui.SmallButton(localization.Get("Help.FormSecurity"))) Dalamud.Utility.Util.OpenLink(ProblemReport.SecurityAddress);
        Tip("Help.FormSecurityTip");
    }
}
