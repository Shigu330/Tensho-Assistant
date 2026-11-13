using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Alarms;
using TenshoAssistant.UI;

namespace TenshoAssistant;

/// <summary>
/// Home (0.206): catalogue status, work in progress (craft search and chain, Mini Cactpot), the next alarm and the failing modules, then ten
/// tiles (crafting, gathering, collections, quests, map, market, alarms, macro chain, character, Gold Saucer) each with a live line, the
/// context item with its tools, and the tool directory (setting HomeShowTools). Reads only states other services already copied.
/// </summary>
internal sealed partial class MainWindow
{
    private void DrawHome()
    {
        // 0.1222 (X-257, D5): one description: while the first-visit banner shows, it says where to start, and the heading has none.
        Heading(localization.Get("Home.Title"),HelpBannerShows(Page.Home) ? "" : localization.Get("Home.Description"));
        MeasureMap("home guide",DrawGuideOnHome);
        var items=itemCatalog.State;var recipes=recipeCatalog.State;
        var ready=items.Catalog is not null && recipes.Catalog is not null;
        // X-154 (0.1253): loading is the shared « waiting » state; ready keeps its line, with the shared state mark.
        if(ready){DrawStateMark(true);ImGui.SameLine();ImGui.TextColored(Success,localization.Get("Home.Ready"));}
        else StateLine(UiState.Waiting,localization.Get("Home.Loading"));
        ImGui.SameLine();
        ImGui.TextDisabled(localization.Format("Home.Catalogues",items.Catalog?.Count ?? 0,recipes.Catalog?.Count ?? 0));
        // X-95 (0.951): each part of Home timed in developer mode (the page reached 119 ms in the slow frame log).
        MeasureMap("home alerts",DrawHomeAlerts);
        ImGui.Separator();
        MeasureMap("home today",DrawHomeToday);
        ImGui.Separator();
        MeasureMap("home tiles",DrawHomeTiles);
        if(SelectedItem is { } contextItem)
        {
            ImGui.Separator();
            UiComponents.SubTitle(localization.Format("Home.ContextItem",contextItem.Name));
            MeasureMap("home context tools",DrawContextTools);
        }
        ImGui.Separator();
        if(revealHomeTools){ImGui.SetNextItemOpen(true);revealHomeTools=false;}
        if(configuration.HomeShowTools && TenshoAssistant.UI.UiComponents.Header(localization.Get("Plugin.AllTools")+"###HomeTools"))MeasureMap("home tools",DrawTools);
    }

    // Work in progress, next alarm and errors, in one short block.
    private void DrawHomeAlerts()
    {
        // X-154 (0.1254): running computations are the shared « waiting » state.
        if(craftSolve.State.Loading)StateLine(UiState.Waiting,localization.Get("CraftSearch.Loading"));
        if(craftChain.State.Loading)StateLine(UiState.Waiting,localization.Get("Home.CraftChain"));
        if(cactpotSolve.State.Loading)StateLine(UiState.Waiting,localization.Get("Home.Cactpot"));
        // 0.599 (T10): the three next alarms are in « Today »; this line only while that one is hidden.
        if(configuration.HomeTodayHidden?.Contains("Alarms")==true && NextAlarm() is { } next)
        {
            ImGui.TextColored(Success,localization.Format("Home.NextAlarm",next.Alarm.Label,next.At.ToLocalTime().ToString("t"),Math.Max(0,(int)(next.At-DateTimeOffset.UtcNow).TotalMinutes)));
            if(ImGui.IsItemClicked())page=Page.Alarms;
        }
        var failed=new List<string>();
        if(itemCatalog.State.Error is not null)failed.Add(localization.Get("Navigation.Items"));
        if(recipeCatalog.State.Error is not null)failed.Add(localization.Get("Navigation.Crafting"));
        if(market.State.Error is { } marketError && !SourceOff(marketError))failed.Add(localization.Get("Navigation.Market"));
        if(quests.State.Error is not null)failed.Add(localization.Get("Navigation.Quests"));
        if(failed.Count==0)return;
        ImGui.TextColored(FfxivTheme.Error,localization.Get("Home.Errors")+" "+string.Join(", ",failed));
        if(developerMode && ImGui.SmallButton(localization.Get("Navigation.Diagnostics")))page=Page.Diagnostics;
    }

    private (TenshoAlarm Alarm,DateTimeOffset At)? NextAlarm()
    {
        var now=DateTimeOffset.UtcNow;
        return (configuration.Alarms ?? []).Where(alarm=>alarm.Enabled).Select(alarm=>(alarm,NextAlarm(alarm,now)))
            .Where(pair=>pair.Item2 is not null).Select(pair=>((TenshoAlarm,DateTimeOffset)?)(pair.alarm,pair.Item2!.Value)).OrderBy(pair=>pair!.Value.Item2).FirstOrDefault();
    }

    // Ten tiles (main modules): icon, name, live line; a click opens the module. 0.599 (T10): each one hidden from « Customize ». The full list is the tool directory.
    private (FontAwesomeIcon Icon,string Name,string Line,Page Target)[] HomeTiles()
    {
        var alarmNext=NextAlarm();
        return new (FontAwesomeIcon Icon,string Name,string Line,Page Target)[]
        {
            (FontAwesomeIcon.Hammer,localization.Get("Navigation.Crafting"),localization.Format("Home.TileCraft",recipeCatalog.State.Catalog?.Count ?? 0),Page.CraftSimulation),
            (FontAwesomeIcon.Leaf,localization.Get("Navigation.Gathering"),localization.Format("Home.TileGathering",gatheringCatalog.State.Catalog?.Count ?? 0),Page.Gathering),
            (FontAwesomeIcon.Trophy,localization.Get("Navigation.Collection"),localization.Format("Home.TileCollections",collections.State.Entries.Count),Page.Collections),
            (FontAwesomeIcon.Tasks,localization.Get("Navigation.Quests"),localization.Format("Home.TileQuests",quests.State.Entries.Count),Page.Quests),
            (FontAwesomeIcon.MapMarkedAlt,localization.Get("Navigation.Map"),localization.Get("Home.TileMap"),Page.Maps),
            (FontAwesomeIcon.Coins,localization.Get("Navigation.Market"),market.State.WorldName.Length>0 ? localization.Format("Home.TileMarket",market.State.WorldName):localization.Get("Home.TileMarketNone"),Page.Market),
            (FontAwesomeIcon.Bell,localization.Get("Alarms.PageTitle"),alarmNext is { } next ? localization.Format("Home.TileAlarm",next.At.ToLocalTime().ToString("t")):localization.Format("Home.TileAlarmNone",configuration.Alarms?.Count ?? 0),Page.Alarms),
            (FontAwesomeIcon.Link,localization.Get("Category.MacroChain"),localization.Format("Home.TileMacros",configuration.MacroChainPlan?.Count ?? 0),Page.MacroChain),
            (FontAwesomeIcon.ChartBar,localization.Get("Navigation.Gear"),localization.Get("Home.TileGear"),Page.Gear),
            (FontAwesomeIcon.Clone,localization.Get("Category.GoldSaucer"),localization.Get("Home.TileGoldSaucer"),Page.MiniCactpot),
        };
    }
    private void DrawHomeTiles()
    {
        var tiles=HomeTiles().Where(tile=>configuration.HomeTilesHidden?.Contains(tile.Target.ToString())!=true).ToArray();
        if(tiles.Length==0)return;
        var width=ImGui.GetContentRegionAvail().X;
        var columns=Math.Clamp((int)(width/220),1,4);
        if(!ImGui.BeginTable("HomeTiles",columns,ImGuiTableFlags.SizingStretchSame))return;
        try
        {
            foreach(var (icon,name,line,target) in tiles)
            {
                ImGui.TableNextColumn();
                ImGui.PushID($"Tile{target}");
                try
                {
                    // X-155 (0.1261): one tile, one action: the icon, the name and the live line are one element (it had two: the icon's
                    // button and the name), with one focus frame; the size follows the text.
                    var side=MathF.Max(36*Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale,ImGui.GetTextLineHeightWithSpacing()*2);
                    var clicked=ImGui.Selectable("##Tile",false,ImGuiSelectableFlags.None,new Vector2(0,side));
                    var min=ImGui.GetItemRectMin();var max=ImGui.GetItemRectMax();
                    var draw=ImGui.GetWindowDrawList();
                    draw.PushClipRect(min,max,true);
                    try
                    {
                        draw.AddRect(min,min+new Vector2(side),ImGui.GetColorU32(ImGuiCol.Border),2);
                        DrawGlyphIn(icon,min,min+new Vector2(side),Text,ImGui.GetFontSize());
                        var left=min.X+side+ImGui.GetStyle().ItemSpacing.X;
                        var top=min.Y+MathF.Max(0,(side-ImGui.GetTextLineHeightWithSpacing()*2)/2);
                        draw.AddText(new Vector2(left,top),ImGui.GetColorU32(ImGuiCol.Text),name);
                        draw.AddText(new Vector2(left,top+ImGui.GetTextLineHeightWithSpacing()),ImGui.GetColorU32(ImGuiCol.TextDisabled),line);
                    }
                    finally{draw.PopClipRect();}
                    if(clicked)page=target;
                }
                finally{ImGui.PopID();}
            }
        }
        finally{ImGui.EndTable();}
    }
}
