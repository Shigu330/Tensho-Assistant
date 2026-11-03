using Dalamud.Bindings.ImGui;
using TenshoAssistant.Reminders;

namespace TenshoAssistant;

// N-06 (0.451, DEC-027): the squadron mission planner on the Reminders page. On the player's click, the squadron is read from the game
// (only loaded in the barracks); the missions, requirements and member attributes come from the game sheets. The success percent is the
// sheet rule (PercentBase + parts met), shown as an estimate until checked in game; chemistry is not counted. « Write to the log » records
// the raw values for the in-game probe (docs/evidence/0.448-n06-n14-sources.md).
internal sealed partial class MainWindow
{
    internal Func<Task<SquadronRaw?>>? ReadSquadron { get; set; }
    internal Func<SquadronSheets?>? SquadronSheetData { get; set; }
    internal Func<uint,string?>? SquadronMemberName { get; set; }
    private Task<SquadronRaw?>? squadronRead;
    private SquadronRaw? squadron;
    private bool squadronReadDone,squadronReadFailed;
    private uint squadronMission;
    private int squadronSet;

    private void DrawSquadronPlanner()
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("Squadron.Title")+"###SquadronPlanner"))return;
        ImGui.TextDisabled(localization.Get("Squadron.Help"));
        if(squadronRead is { IsCompleted:true } read)
        {
            squadron=read.IsCompletedSuccessfully ? read.Result:null;
            squadronReadDone=true;
            squadronReadFailed=!read.IsCompletedSuccessfully;
            squadronRead=null;
            if(squadron is { CurrentExpedition:>0 } current && squadronMission==0)squadronMission=current.CurrentExpedition;
        }
        ImGui.BeginDisabled(ReadSquadron is null || squadronRead is not null);
        try{if(ImGui.Button(localization.Get("Squadron.Read")))squadronRead=ReadSquadron!();}
        finally{ImGui.EndDisabled();}
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get("Squadron.ReadTip"));
        if(squadron is null)
        {
            if(squadronReadFailed)ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Squadron.Unavailable"));
            else if(squadronReadDone)ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("Squadron.NotLoaded"));
            return;
        }
        if(SquadronSheetData?.Invoke() is not { } sheets){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Squadron.SheetsUnavailable"));return;}
        var members=SquadronPlanner.Members(squadron,sheets);
        string ClassName(uint job)=>sheets.ClassNames.GetValueOrDefault(job) ?? "#"+job;
        string MemberLabel(SquadronMember member)
            =>(SquadronMemberName?.Invoke(squadron.Members.First(raw=>raw.Slot==member.Slot).ENpcResidentId) is { Length:>0 } name ? name+" · ":"")
              +localization.Format("Squadron.MemberClass",ClassName(member.ClassJob),member.Level);
        foreach(var member in members)
        {
            var stats=member.Stats is { } known ? localization.Format("Squadron.Stats",known.Physical,known.Mental,known.Tactical):localization.Get("Squadron.StatsUnknown");
            ImGui.BulletText(MemberLabel(member)+" — "+stats+(member.Busy ? " · "+localization.Get("Squadron.Busy"):""));
        }
        ImGui.TextUnformatted(localization.Format("Squadron.Bonus",squadron.Bonus.Physical,squadron.Bonus.Mental,squadron.Bonus.Tactical));
        var missions=sheets.Missions;
        var mission=missions.FirstOrDefault(row=>row.Id==squadronMission);
        ImGui.SetNextItemWidth(320);
        var open=ImGui.BeginCombo("##SquadronMission",mission is null ? localization.Get("Squadron.PickMission"):localization.Format("Squadron.MissionRow",mission.Name,mission.Level));
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("Squadron.PickMissionTip"));
        if(open)
        {
            try
            {
                foreach(var row in missions)
                    if(ImGui.Selectable(localization.Format("Squadron.MissionRow",row.Name,row.Level)+"##SquadronMission"+row.Id,row.Id==squadronMission)){squadronMission=row.Id;squadronSet=0;}
            }
            finally{ImGui.EndCombo();}
        }
        if(mission is null || mission.Sets.Count==0)return;
        squadronSet=Math.Clamp(squadronSet,0,mission.Sets.Count-1);
        for(var index=0;index<mission.Sets.Count;index++)
        {
            var required=mission.Sets[index].Required;
            if(index>0)ImGui.SameLine();
            if(ImGui.RadioButton(localization.Format("Squadron.SetRow",required.Physical,required.Mental,required.Tactical)+"##SquadronSet"+index,squadronSet==index))squadronSet=index;
            Tip("Squadron.SetTip");
        }
        var set=mission.Sets[squadronSet];
        var parties=SquadronPlanner.Best(members,squadron.Bonus,mission,set);
        if(parties.Count==0)ImGui.TextColored(UI.FfxivTheme.Warning,localization.Get("Squadron.NoParty"));
        foreach(var party in parties)
        {
            ImGui.TextColored(party.Met==3 ? Success:party.Met==2 ? Accent:UI.FfxivTheme.Warning,
                localization.Format("Squadron.PartyPercent",party.Percent)+"  "+localization.Format("Squadron.Stats",party.Total.Physical,party.Total.Mental,party.Total.Tactical));
            ImGui.Indent();
            ImGui.TextDisabled(string.Join(" · ",party.Members.Select(MemberLabel)));
            ImGui.Unindent();
        }
        ImGui.TextDisabled(localization.Get("Squadron.Estimate"));
        if(ImGui.SmallButton(localization.Get("Squadron.Log")))LogSquadronProbe(squadron,members,mission,set,parties);
        Tip("Squadron.LogTip");
    }

    /// <summary>The in-game probe: writes the raw squadron, the sheet attributes and the planner's result to the Dalamud log (English).</summary>
    private void LogSquadronProbe(SquadronRaw raw,IReadOnlyList<SquadronMember> members,SquadronMission mission,SquadronRequirement set,IReadOnlyList<SquadronParty> parties)
    {
        log.Information($"Squadron probe: progress {raw.Progress}, current expedition {raw.CurrentExpedition}, training bonus {raw.Bonus.Physical}/{raw.Bonus.Mental}/{raw.Bonus.Tactical}.");
        foreach(var member in raw.Members)
        {
            var stats=members.First(planned=>planned.Slot==member.Slot).Stats;
            log.Information($"Squadron probe: slot {member.Slot} npc {member.ENpcResidentId} race {member.Race} class {member.ClassJob} level {member.Level} exp {member.Experience} flags {member.Flags}"
                +$" trait {member.ActiveTrait}/{member.ActiveTraitCond} inactive {member.InactiveTrait}/{member.InactiveTraitCond} sheet stats {(stats is { } known ? $"{known.Physical}/{known.Mental}/{known.Tactical}":"unknown")}.");
        }
        log.Information($"Squadron probe: mission {mission.Id} level {mission.Level} type {mission.Type} base {mission.PercentBase}%, set {squadronSet+1} requires {set.Required.Physical}/{set.Required.Mental}/{set.Required.Tactical}"
            +$" (+{set.PercentPhysical}/+{set.PercentMental}/+{set.PercentTactical}, all +{set.PercentAll}).");
        foreach(var party in parties)
            log.Information($"Squadron probe: party slots {string.Join(',',party.Members.Select(member=>member.Slot))} total {party.Total.Physical}/{party.Total.Mental}/{party.Total.Tactical} met {party.Met} estimate {party.Percent}%.");
        ChatNotice?.Invoke(localization.Get("Squadron.Logged"));
    }
}
