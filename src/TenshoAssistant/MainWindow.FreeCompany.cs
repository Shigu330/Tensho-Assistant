using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Alarms;
using TenshoAssistant.FreeCompany;

namespace TenshoAssistant;

// R-07 (0.307): Free company page with three tabs. Vessels: the submarines and airships as last read in the company workshop
// (FreeCompany/VoyageWatcher copies them every 3 s on the framework thread and saves them in the configuration): rank with an experience
// bar, route, return (time left, one-off alarm) and the finds of the last voyage. Best routes (0.309, MainWindow.RoutePlanner.cs) and
// Builds (0.315, MainWindow.VesselBuilds.cs); a route's « Build » button selects the Builds tab once.
internal sealed partial class MainWindow
{
    // 0.331 (audit, R-14): the last workshop read failed in whole or in part (VoyageWatcher.ReadUnavailable); the saved read is shown.
    internal Func<bool>? VoyageReadUnavailable { get; set; }
    // 0.331 (audit): « Forget this company » armed for one house, dropped when not drawn on the previous frame (as the alarm groups, 0.326).
    private ulong? forgetHouseArmed;private int forgetHouseSeenFrame;

    private void DrawFreeCompany()
    {
        Heading(localization.Get("FreeCompany.Title"),localization.Get("FreeCompany.Description"));
        if(!ImGui.BeginTabBar("FreeCompanyTabs"))return;
        try
        {
            // 0.315: a route's « Build » button selects the Builds tab once.
            ImGuiTabItemFlags Request(int tab)=>freeCompanyTabRequest==tab ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None;
            var request=freeCompanyTabRequest;
            if(ImGui.BeginTabItem(localization.Get("FreeCompany.TabVessels")+"###FcVessels",Request(0))){try{DrawFreeCompanyVessels();}finally{ImGui.EndTabItem();}}
            if(ImGui.BeginTabItem(localization.Get("FreeCompany.TabRoutes")+"###FcRoutes",Request(1))){try{DrawRoutePlanner();}finally{ImGui.EndTabItem();}}
            if(ImGui.BeginTabItem(localization.Get("FreeCompany.TabBuilds")+"###FcBuilds",Request(2))){try{DrawVesselBuilds();}finally{ImGui.EndTabItem();}}
            if(ImGui.BeginTabItem(localization.Get("FreeCompany.TabAirships")+"###FcAirships",Request(3))){try{DrawAirshipSectors();}finally{ImGui.EndTabItem();}}
            if(ImGui.BeginTabItem(localization.Get("FreeCompany.TabWorkshop")+"###FcWorkshop")){try{DrawWorkshopProjects();}finally{ImGui.EndTabItem();}}
            // 0.379 (g, M-10): loot history.
            if(ImGui.BeginTabItem(localization.Get("VoyageHistory.Tab")+"###FcHistory")){try{DrawVoyageHistory();}finally{ImGui.EndTabItem();}}
            if(request>=0 && freeCompanyTabRequest==request)freeCompanyTabRequest=-1;
        }
        finally{ImGui.EndTabBar();}
    }

    private void DrawFreeCompanyVessels()
    {
        var vessels=configuration.Voyages ?? [];
        if(VoyageReadUnavailable?.Invoke()==true)ImGui.TextColored(Accent,localization.Get("FreeCompany.ReadUnavailable"));
        if(vessels.Count==0){ImGui.TextWrapped(localization.Get("FreeCompany.NoData"));return;}
        var now=DateTimeOffset.UtcNow;
        // 0.603 (T23): one frame per workshop: several companies are not mixed, each has the date of its own reading.
        var workshops=FreeCompany.Voyages.Workshops(vessels);
        for(var workshop=0;workshop<workshops.Count;workshop++)
        {
            ImGui.PushID(workshop);
            try{DrawWorkshop(workshops[workshop],workshop+1,workshops.Count,now);}
            finally{ImGui.PopID();}
        }
    }

    private void DrawWorkshop(IReadOnlyList<VoyageVessel> vessels,int number,int count,DateTimeOffset now)
    {
        if(count>1)ImGui.TextColored(Accent,localization.Format("FreeCompany.Workshop",number));
        ImGui.TextDisabled(localization.Format("FreeCompany.ReadAt",vessels.Max(vessel=>vessel.ReadAt).ToLocalTime().ToString("g")));
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("FreeCompany.ReadAtTip"));
        foreach(var kind in new[]{VesselKind.Submarine,VesselKind.Airship})
        {
            var list=vessels.Where(vessel=>vessel.Kind==kind).ToArray();
            if(list.Length==0)continue;
            if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("FreeCompany."+kind),ImGuiTreeNodeFlags.DefaultOpen))continue;
            if(!ImGui.BeginTable("Voyages"+kind,5,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))continue;
            try
            {
                foreach(var key in new[]{"Vessel","Rank","Route","Return",""})
                    ImGui.TableSetupColumn(key.Length==0 ? "":localization.Get("FreeCompany.Column"+key),key.Length==0 ? ImGuiTableColumnFlags.WidthFixed:ImGuiTableColumnFlags.None,key.Length==0 ? 28:0);
                ImGui.TableHeadersRow();
                foreach(var vessel in list)DrawVesselRow(vessel,now);
            }
            finally{ImGui.EndTable();}
        }
        DrawReadCompanies(vessels);
    }

    // 0.331 (audit): one line per company read (house): its vessels, its own read date and « Forget this company » (two clicks, as
    // « Delete all » of the alarm groups) for a company the player left; nothing else removes its vessels, sectors or plans.
    private void DrawReadCompanies(IReadOnlyList<VoyageVessel> vessels)
    {
        if(forgetHouseArmed is not null && ImGui.GetFrameCount()>forgetHouseSeenFrame+1)forgetHouseArmed=null;
        var houses=vessels.GroupBy(vessel=>vessel.HouseId).ToArray();
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("FreeCompany.Companies",houses.Length)+"###FcCompanies"))return;
        ulong? forget=null;var number=0;
        foreach(var house in houses)
        {
            number++;
            ImGui.PushID($"FcHouse{house.Key}");
            try
            {
                ImGui.TextUnformatted(localization.Format("FreeCompany.Company",number,string.Join(", ",house.Select(vessel=>vessel.Name.Length>0 ? vessel.Name:$"#{vessel.Slot}"))));
                ImGui.SameLine();ImGui.TextDisabled(localization.Format("FreeCompany.ReadAt",house.Max(vessel=>vessel.ReadAt).ToLocalTime().ToString("g")));
                ImGui.SameLine();
                if(forgetHouseArmed!=house.Key)
                {
                    if(ImGui.SmallButton(localization.Get("FreeCompany.Forget"))){forgetHouseArmed=house.Key;forgetHouseSeenFrame=ImGui.GetFrameCount();}
                    Tip("FreeCompany.ForgetTip");
                }
                else
                {
                    forgetHouseSeenFrame=ImGui.GetFrameCount();
                    ImGui.PushStyleColor(ImGuiCol.Text,UI.FfxivTheme.Error);
                    bool confirmed;
                    try{confirmed=ImGui.SmallButton(localization.Get("FreeCompany.ForgetConfirm"));}
                    finally{ImGui.PopStyleColor();}
                    Tip("FreeCompany.ForgetTip");
                    ImGui.SameLine();
                    if(ImGui.SmallButton(localization.Get("Settings.ResetAllNo")))forgetHouseArmed=null;
                    if(confirmed){forgetHouseArmed=null;forget=house.Key;}
                }
            }
            finally{ImGui.PopID();}
        }
        if(forget is { } dropped)ForgetCompany(dropped);
    }

    // Drops that house's vessels, unlocked sectors and vessel plans; all three come back when saving fails.
    private void ForgetCompany(ulong house)
    {
        var key=house.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var (vessels,unlocked,plans)=(configuration.Voyages,configuration.SubmarineUnlocked,configuration.VesselPlans);
        configuration.Voyages=(vessels ?? []).Where(vessel=>vessel.HouseId!=house).ToList();
        configuration.SubmarineUnlocked=unlocked?.Where(entry=>entry.Key!=key).ToDictionary(entry=>entry.Key,entry=>entry.Value);
        configuration.VesselPlans=plans?.Where(entry=>!Voyages.IsPlanOf(entry.Key,house)).ToDictionary(entry=>entry.Key,entry=>entry.Value);
        try
        {
            save();saveError=null;
            // The Builds tab and the planner leave a forgotten vessel (Builds: first submarine read, else manual; planner: manual input).
            if(buildVesselKey is not null && Voyages.IsPlanOf(buildVesselKey,house)){buildVesselKey=null;buildCandidates=[];buildSearchKey=null;}
            if(plannerVesselKey is not null && Voyages.IsPlanOf(plannerVesselKey,house))plannerVesselKey=null;
        }
        catch(Exception error)
        {
            (configuration.Voyages,configuration.SubmarineUnlocked,configuration.VesselPlans)=(vessels,unlocked,plans);
            saveError=localization.Get("Common.SaveError");log.Warning(error,"Free company could not be forgotten.");
        }
    }

    private void DrawVesselRow(VoyageVessel vessel,DateTimeOffset now)
    {
        var id=$"{vessel.HouseId}-{vessel.Kind}-{vessel.Slot}";
        ImGui.TableNextRow();ImGui.TableNextColumn();
        ImGui.PushFont(UiBuilder.IconFont);
        try{ImGui.TextColored(Accent,(vessel.Kind==VesselKind.Submarine ? FontAwesomeIcon.Water:FontAwesomeIcon.Plane).ToIconString());}
        finally{ImGui.PopFont();}
        ImGui.SameLine();ImGui.TextUnformatted(vessel.Name.Length>0 ? vessel.Name:$"#{vessel.Slot}");
        if(vessel.RegisteredAt is { } registered && ImGui.IsItemHovered())WrappedTooltip(localization.Format("FreeCompany.Registered",registered.ToLocalTime().ToString("g")));
        // Rank and a bar of the experience to the next rank.
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(localization.Format("FreeCompany.Rank",vessel.Rank));
        ImGui.SameLine();
        var progress=(float)Voyages.ExpProgress(vessel);
        ImGui.ProgressBar(progress,new Vector2(-1,ImGui.GetTextLineHeight()),$"{progress*100:0} %");
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("FreeCompany.Exp",vessel.Exp.ToString("N0"),vessel.NextExp.ToString("N0")));
        ImGui.TableNextColumn();
        if(vessel.Route.Count>0)ImGui.TextWrapped(string.Join(" › ",vessel.Route));
        else ImGui.TextDisabled(vessel.Kind==VesselKind.Airship ? localization.Get("FreeCompany.RouteUnknown"):"—");
        // Return: time left, « returned » (finds listed in the tooltip), or « at the dock ».
        ImGui.TableNextColumn();
        var state=Voyages.StateOf(vessel,now);
        if(state==VoyageState.Voyaging && vessel.ReturnAt is { } at)ImGui.TextUnformatted(InText(at,now));
        else if(state==VoyageState.Returned)ImGui.TextColored(Accent,localization.Get("FreeCompany.Returned"));
        else ImGui.TextDisabled(localization.Get("FreeCompany.Idle"));
        if(vessel.Loot.Count>0)
        {
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            try{ImGui.TextDisabled(FontAwesomeIcon.BoxOpen.ToIconString());}
            finally{ImGui.PopFont();}
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("FreeCompany.LootHint"));
            if(ImGui.IsItemClicked())ImGui.OpenPopup("VesselLoot"+id);
            if(ImGui.BeginPopup("VesselLoot"+id))
            {
                try
                {
                    ImGui.TextColored(UI.FfxivTheme.Gold,localization.Get("FreeCompany.Loot"));
                    var items=itemCatalog.State.Catalog;
                    foreach(var group in vessel.Loot.GroupBy(found=>found.ItemId))
                    {
                        var item=items?.Find(group.Key);
                        ItemCell(item,group.Key,24,$"{item?.Name ?? group.Key.ToString()} ×{group.Sum(found=>found.Count):N0}");
                    }
                }
                finally{ImGui.EndPopup();}
            }
        }
        ImGui.TableNextColumn();
        if(state==VoyageState.Voyaging && vessel.ReturnAt is { } alarmAt)
            AlarmButton("Voyage"+id,localization.Format("FreeCompany.AlarmTip",vessel.Name),
                ()=>[new TenshoAlarm{Label=localization.Format("FreeCompany.AlarmLabel",vessel.Name),Group=localization.Get("FreeCompany.Title"),Source=AlarmSource.Voyage,Repeat=AlarmRepeat.Once,At=alarmAt,
                    LeadMinutes=AlarmSchedule.ClampLead(AlarmRepeat.Once,DefaultAlarmLead)}]);
    }
}
