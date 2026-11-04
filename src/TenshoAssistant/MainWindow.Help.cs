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
        if (ImGui.SmallButton("?##PageHelp")) ImGui.OpenPopup("PageHelpPopup");
        Tip("Help.ButtonTip");
        if (!ImGui.BeginPopup("PageHelpPopup")) return;
        try
        {
            var topic = TopicOf(page);
            ImGui.TextColored(Accent, topic.Title);
            ImGui.Separator();
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
