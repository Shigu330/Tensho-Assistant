using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Alarms;
using TenshoAssistant.UI;

namespace TenshoAssistant;

/// <summary>
/// Home (0.206): one tile per module of the navigation, each with a live line (counts, next alarm, market world…), the context item, the
/// work in progress and the errors, then the tool directory.
/// </summary>
internal sealed partial class MainWindow
{
    private void DrawHome()
    {
        Heading(localization.Get("Home.Title"),localization.Get("Home.Description"));
        var items=itemCatalog.State;var recipes=recipeCatalog.State;
        var ready=items.Catalog is not null && recipes.Catalog is not null;
        UiComponents.StatusBadge(localization.Get(ready ? "Home.Ready":"Home.Loading"),ready ? Success:Accent);
        ImGui.SameLine();
        ImGui.TextDisabled(localization.Format("Home.Catalogues",items.Catalog?.Count ?? 0,recipes.Catalog?.Count ?? 0));
        DrawHomeAlerts();
        ImGui.Separator();
        DrawHomeTiles();
        if(SelectedItem is { } contextItem)
        {
            ImGui.Separator();
            ImGui.TextColored(Accent,localization.Format("Home.ContextItem",contextItem.Name));
            DrawContextTools();
        }
        ImGui.Separator();
        if(revealHomeTools){ImGui.SetNextItemOpen(true);revealHomeTools=false;}
        if(configuration.HomeShowTools && ImGui.CollapsingHeader(localization.Get("Plugin.AllTools")+"###HomeTools"))DrawTools();
    }

    // Work in progress, next alarm and errors, in one short block.
    private void DrawHomeAlerts()
    {
        if(craftSolve.State.Loading)ImGui.TextColored(Accent,localization.Get("CraftSearch.Loading"));
        if(craftChain.State.Loading)ImGui.TextColored(Accent,localization.Get("Home.CraftChain"));
        if(cactpotSolve.State.Loading)ImGui.TextColored(Accent,localization.Get("Home.Cactpot"));
        if(NextAlarm() is { } next)
        {
            ImGui.TextColored(Success,localization.Format("Home.NextAlarm",next.Alarm.Label,next.At.ToLocalTime().ToString("t"),Math.Max(0,(int)(next.At-DateTimeOffset.UtcNow).TotalMinutes)));
            if(ImGui.IsItemClicked())page=Page.Alarms;
        }
        var failed=new List<string>();
        if(itemCatalog.State.Error is not null)failed.Add(localization.Get("Navigation.Items"));
        if(recipeCatalog.State.Error is not null)failed.Add(localization.Get("Navigation.Crafting"));
        if(market.State.Error is not null)failed.Add(localization.Get("Navigation.Market"));
        if(quests.State.Error is not null)failed.Add(localization.Get("Navigation.Quests"));
        if(failed.Count==0)return;
        ImGui.TextColored(FfxivTheme.Error,localization.Get("Home.Errors")+" "+string.Join(", ",failed));
        if(developerMode && ImGui.SmallButton(localization.Get("Navigation.Diagnostics")))page=Page.Diagnostics;
    }

    private (TenshoAlarm Alarm,DateTimeOffset At)? NextAlarm()
    {
        var now=DateTimeOffset.UtcNow;
        return (configuration.Alarms ?? []).Where(alarm=>alarm.Enabled).Select(alarm=>(alarm,AlarmSchedule.Next(alarm,now)))
            .Where(pair=>pair.Item2 is not null).Select(pair=>((TenshoAlarm,DateTimeOffset)?)(pair.alarm,pair.Item2!.Value)).OrderBy(pair=>pair!.Value.Item2).FirstOrDefault();
    }

    // One tile per module: icon, name, live line; a click opens the module.
    private void DrawHomeTiles()
    {
        var alarmNext=NextAlarm();
        var tiles=new (FontAwesomeIcon Icon,string Name,string Line,Page Target)[]
        {
            (FontAwesomeIcon.Hammer,localization.Get("Navigation.Crafting"),localization.Format("Home.TileCraft",recipeCatalog.State.Catalog?.Count ?? 0),Page.CraftSimulation),
            (FontAwesomeIcon.Leaf,localization.Get("Navigation.Gathering"),localization.Format("Home.TileGathering",gatheringCatalog.State.Catalog?.Count ?? 0),Page.Gathering),
            (FontAwesomeIcon.Trophy,localization.Get("Navigation.Collection"),localization.Format("Home.TileCollections",collections.State.Entries.Count),Page.Collections),
            (FontAwesomeIcon.Tasks,localization.Get("Navigation.Quests"),localization.Format("Home.TileQuests",quests.State.Entries.Count),Page.Quests),
            (FontAwesomeIcon.MapMarkedAlt,localization.Get("Navigation.Map"),localization.Get("Home.TileMap"),Page.Maps),
            (FontAwesomeIcon.Coins,localization.Get("Navigation.Market"),market.State.WorldName.Length>0 ? localization.Format("Home.TileMarket",market.State.WorldName):localization.Get("Home.TileMarketNone"),Page.Market),
            (FontAwesomeIcon.Bell,localization.Get("Alarms.PageTitle"),alarmNext is { } next ? localization.Format("Home.TileAlarm",next.At.ToLocalTime().ToString("t")):localization.Format("Home.TileAlarmNone",configuration.Alarms?.Count ?? 0),Page.Alarms),
            (FontAwesomeIcon.Link,localization.Get("Navigation.Automation"),localization.Format("Home.TileMacros",configuration.MacroChainPlan?.Count ?? 0),Page.MacroChain),
            (FontAwesomeIcon.ChartBar,localization.Get("Navigation.Combat"),localization.Get("Home.TileCharacter"),Page.Character),
            (FontAwesomeIcon.Clone,localization.Get("Category.GoldSaucer"),localization.Get("Home.TileGoldSaucer"),Page.MiniCactpot),
        };
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
                    ImGui.PushFont(UiBuilder.IconFont);
                    bool clicked;
                    try{clicked=ImGui.Button(icon.ToIconString(),new Vector2(36,36));}
                    finally{ImGui.PopFont();}
                    ImGui.SameLine();
                    ImGui.BeginGroup();
                    try
                    {
                        if(ImGui.Selectable(name,false,ImGuiSelectableFlags.None,new Vector2(0,0)))clicked=true;
                        ImGui.TextDisabled(line);
                    }
                    finally{ImGui.EndGroup();}
                    if(clicked)page=target;
                }
                finally{ImGui.PopID();}
            }
        }
        finally{ImGui.EndTable();}
    }
}
