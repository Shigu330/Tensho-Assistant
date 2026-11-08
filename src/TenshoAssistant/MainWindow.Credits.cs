using Dalamud.Bindings.ImGui;
using TenshoAssistant.Credits;

namespace TenshoAssistant;

/// <summary>
/// R-13 (0.320, user request 2026-11-02): the Credits page, for every player. Everything Tensho uses, from one list in the code
/// (Credits/CreditCatalog.cs): the game, libraries, data embedded in Tensho, data read online (with the setting tied to each, and what a
/// partial setting turns off), community rules taken as facts, plugins and sites read to understand (never copied) and the build tools.
/// The rows shown are kept and rebuilt only when the filter or the language changes; links are copied to the clipboard (Tensho opens
/// no browser).
/// </summary>
internal sealed partial class MainWindow
{
    private string creditsFilter="";
    private (string Filter,string Language,(CreditKind Kind,Credit[] Entries)[] Sections)? creditsView;

    private void DrawCredits()
    {
        Heading(localization.Get("Credits.Title"),localization.Format("Credits.Count",CreditCatalog.All.Count));
        ImGui.TextWrapped(localization.Get("Credits.Intro"));
        ImGui.SetNextItemWidth(Math.Min(320,ImGui.GetContentRegionAvail().X));
        ImGui.InputTextWithHint("###CreditsFilter",localization.Get("Credits.Filter"),ref creditsFilter,60);
        Tip("Credits.FilterTip");
        var filter=creditsFilter.Trim();
        if(creditsView is not { } view || view.Filter!=filter || view.Language!=localization.CurrentCode)
            creditsView=view=(filter,localization.CurrentCode,Enum.GetValues<CreditKind>()
                .Select(kind=>(kind,CreditCatalog.All.Where(credit=>credit.Kind==kind && (filter.Length==0 || CreditMatches(credit,filter))).ToArray()))
                .Where(section=>section.Item2.Length>0).ToArray());
        foreach(var (kind,entries) in view.Sections)
        {
            ImGui.Spacing();
            if(!TenshoAssistant.UI.UiComponents.Header($"{localization.Get("Credits.Kind."+kind)} ({entries.Length})###CreditKind{kind}",ImGuiTreeNodeFlags.DefaultOpen))continue;
            ImGui.TextDisabled(localization.Get("Credits.KindHelp."+kind));
            if(!ImGui.BeginTable("###CreditTable"+kind,3,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerH|ImGuiTableFlags.SizingStretchProp))continue;
            try
            {
                ImGui.TableSetupColumn(localization.Get("Credits.Column.Name"),ImGuiTableColumnFlags.WidthStretch,0.9f);
                ImGui.TableSetupColumn(localization.Get("Credits.Column.License"),ImGuiTableColumnFlags.WidthStretch,0.7f);
                ImGui.TableSetupColumn(localization.Get("Credits.Column.Use"),ImGuiTableColumnFlags.WidthStretch,2f);
                ImGui.TableHeadersRow();
                foreach(var credit in entries)
                {
                    ImGui.PushID(credit.Id);
                    try{DrawCreditRow(credit);}
                    finally{ImGui.PopID();}
                }
            }
            finally{ImGui.EndTable();}
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextWrapped(localization.Get("Credits.Footer"));
        CopyButton(localization.Get("Advanced.CopyRepository")+"###CreditsRepo",()=>RepositoryUrl);
        Tip("Advanced.CopyRepositoryTip");
    }

    // Name shown: the localized one when the entry is not a proper name (a rule, a guide), else the catalogue's.
    private string CreditName(Credit credit)=>localization.Has("Credits.Names."+credit.Id) ? localization.Get("Credits.Names."+credit.Id):credit.Name;

    private string CreditLicense(Credit credit)=>credit.License switch
    {
        "none"=>localization.Get("Credits.NoLicense"), "unknown"=>localization.Get("Credits.UnknownLicense"),
        "proprietary"=>localization.Get("Credits.ProprietaryLicense"), var name=>name,
    };

    private bool CreditMatches(Credit credit,string filter)=>
        CreditName(credit).Contains(filter,StringComparison.OrdinalIgnoreCase) || credit.Author.Contains(filter,StringComparison.OrdinalIgnoreCase)
        || CreditLicense(credit).Contains(filter,StringComparison.OrdinalIgnoreCase) || localization.Get("Credits.Items."+credit.Id).Contains(filter,StringComparison.OrdinalIgnoreCase);

    private void DrawCreditRow(Credit credit)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.PushTextWrapPos(0);
        try
        {
            ImGui.TextColored(Accent,CreditName(credit));
            if(credit.Author.Length>0)ImGui.TextDisabled(credit.Author);
        }
        finally{ImGui.PopTextWrapPos();}
        if(credit.Url.Length>0)
        {
            CopyButton(localization.Get("Credits.CopyLink"),()=>credit.Url);
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("Credits.CopyLinkTip",credit.Url));
        }
        ImGui.TableNextColumn();
        ImGui.TextWrapped(CreditLicense(credit));
        if(credit.LicenseUrl.Length>0)
        {
            CopyButton(localization.Get("Credits.CopyLicense"),()=>credit.LicenseUrl);
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("Credits.CopyLinkTip",credit.LicenseUrl));
        }
        ImGui.TableNextColumn();
        ImGui.TextWrapped(localization.Get("Credits.Items."+credit.Id));
        ImGui.PushTextWrapPos(0);
        try
        {
            var copied=!credit.Copied ? "Credits.ReadOnly":credit.Kind switch { CreditKind.Embedded=>"Credits.Copied.Embedded", CreditKind.Rule=>"Credits.Copied.Rule", _=>"Credits.Copied.Other" };
            ImGui.TextDisabled(localization.Get(copied));
            if(credit.Setting.Length>0)
            {
                var on=CreditSettingOn(credit.Setting);
                var line=localization.Get(on ? "Credits.SettingOn":"Credits.SettingOff")
                    +(credit.SettingPartial ? " · "+localization.Format("Credits.SettingPartial",localization.Get("Credits.SettingScope."+credit.Id)):"");
                ImGui.TextColored(on ? Success:Muted,line);
                Tip("Credits.SettingTip");
            }
            if(credit.Files.Count>0)
            {
                ImGui.TextDisabled(localization.Format("Credits.Files",credit.Files.Count));
                if(ImGui.IsItemHovered())WrappedTooltip(string.Join("\n",credit.Files));
            }
        }
        finally{ImGui.PopTextWrapPos();}
    }

    // Whether the setting tied to a source is on now: a dotted path on Configuration (« Discord.Enabled »), its properties looked up
    // once; only a true value reads as on (a missing path reads as off, and a test forbids such a path).
    private static readonly Dictionary<string,System.Reflection.PropertyInfo[]?> CreditSettings=[];
    private bool CreditSettingOn(string setting)
    {
        if(!CreditSettings.TryGetValue(setting,out var path))CreditSettings[setting]=path=CreditCatalog.SettingPath(typeof(Configuration),setting);
        if(path is null)return false;
        object? value=configuration;
        foreach(var property in path)value=value is null ? null:property.GetValue(value);
        return value is true;
    }
}
