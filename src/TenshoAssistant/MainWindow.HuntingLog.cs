using System.Text.RegularExpressions;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Hunting;

namespace TenshoAssistant;

// g, C-12 (0.364): Hunting log page — the nine class logs and three Grand Company logs of the game data, by rank, each entry's monsters
// with the kills needed, the kills read in game (Hunting/HuntingLogReader) and the zones; « not done only » filter.
internal sealed partial class MainWindow
{
    internal Func<IReadOnlyList<HuntEntry>>? HuntEntries { get; set; }
    internal Func<IReadOnlyList<HuntSlot>?>? HuntSlots { get; set; }
    internal Func<bool>? HuntUnavailable { get; set; }
    // 0.393: the character's Grand Company, read with the logs (null = not read yet).
    internal Func<uint?>? HuntGrandCompany { get; set; }
    private uint? huntLogsCompany;
    private uint huntLog;
    private int huntRank=-1;
    private bool huntMissingOnly;
    private object? huntLogsFor;
    private (uint Key,string Name)[] huntLogs=[];

    private void DrawHuntingLog()
    {
        Heading(localization.Get("HuntingLog.Title"),localization.Get("HuntingLog.Description"));
        var entries=HuntEntries?.Invoke() ?? [];
        if(entries.Count==0){ImGui.TextDisabled(localization.Get("HuntingLog.NoData"));return;}
        var company=HuntGrandCompany?.Invoke();
        if(!ReferenceEquals(huntLogsFor,entries) || huntLogsCompany!=company)
        {
            huntLogsFor=entries;huntLogsCompany=company;
            // The log's name is its first entry's name without the number (« Gladiateur 01 » → « Gladiateur »).
            huntLogs=entries.GroupBy(entry=>entry.LogKey).Where(group=>HuntingLog.MemorySlot(group.Key) is not null && HuntingLog.Shown(group.Key,company))
                .Select(group=>(group.Key,Regex.Replace(group.OrderBy(entry=>entry.RowId).First().Name,@"\s*\d+$",""))).ToArray();
            if(huntLogs.Length>0 && !huntLogs.Any(log=>log.Key==huntLog))huntLog=company is > 0 and var own && huntLogs.Any(log=>log.Key==own*100) ? own*100:huntLogs[0].Key;
        }
        var slots=HuntSlots?.Invoke();
        var slot=slots is not null && HuntingLog.MemorySlot(huntLog) is { } index && index<slots.Count ? slots[index]:null;
        if(slots is null)ImGui.TextDisabled(localization.Get(HuntUnavailable?.Invoke()==true ? "HuntingLog.Unavailable":"HuntingLog.Unknown"));
        ImGui.SetNextItemWidth(220);
        if(ImGui.BeginCombo("##HuntLog",huntLogs.FirstOrDefault(log=>log.Key==huntLog).Name ?? ""))
        {
            try{foreach(var (key,name) in huntLogs)if(ImGui.Selectable($"{name}##HuntLog{key}",key==huntLog)){huntLog=key;huntRank=-1;}}
            finally{ImGui.EndCombo();}
        }
        Tip("HuntingLog.LogTip");
        var logEntries=entries.Where(entry=>entry.LogKey==huntLog).ToArray();
        var ranks=logEntries.Select(entry=>entry.Rank).Distinct().Order().ToArray();
        if(ranks.Length==0)return;
        // By default the rank the game is on (the last one once every rank is done).
        if(huntRank<0)huntRank=slot is { } read ? Math.Min(read.Rank,ranks[^1]):ranks[0];
        ImGui.SameLine();
        ImGui.TextDisabled(slot is null ? "":slot.Rank>ranks[^1] ? localization.Get("HuntingLog.AllDone"):localization.Format("HuntingLog.CurrentRank",slot.Rank+1));
        foreach(var rank in ranks)
        {
            if(rank!=ranks[0])ImGui.SameLine();
            if(ImGui.RadioButton(localization.Format("HuntingLog.Rank",rank+1)+$"##HuntRank{rank}",huntRank==rank))huntRank=rank;
            Tip("HuntingLog.RankTip");
        }
        ImGui.SameLine();
        ImGui.Checkbox(localization.Get("HuntingLog.MissingOnly")+"##HuntMissing",ref huntMissingOnly);
        Tip("HuntingLog.MissingOnlyTip");
        // 0.796 (X-36): the map layer of these monsters, set from here too.
        BoolSetting("HuntingLog.NearbyOnMap","HuntingLog.NearbyOnMapTip",()=>configuration.MapView.ShowHuntLogMonsters,value=>configuration.MapView.ShowHuntLogMonsters=value,c=>c.MapView.ShowHuntLogMonsters);
        var shown=logEntries.Where(entry=>entry.Rank==huntRank && (!huntMissingOnly || HuntingLog.Done(entry,slot)!=true)).OrderBy(entry=>entry.RowId).ToArray();
        var rankEntries=logEntries.Where(entry=>entry.Rank==huntRank).ToArray();
        if(slot is not null)ImGui.TextDisabled(localization.Format("HuntingLog.Count",rankEntries.Count(entry=>HuntingLog.Done(entry,slot)==true),rankEntries.Length));
        if(shown.Length==0){ImGui.TextDisabled(localization.Get("HuntingLog.None"));return;}
        if(!ImGui.BeginTable("HuntingLog",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))return;
        try
        {
            foreach(var column in new[]{"Entry","Monster","Kills","Zones"})ImGui.TableSetupColumn(localization.Get("HuntingLog.Column"+column));
            ImGui.TableHeadersRow();
            foreach(var entry in shown)
                for(var target=0;target<entry.Targets.Count;target++)
                {
                    var hunt=entry.Targets[target];
                    ImGui.TableNextRow();ImGui.TableNextColumn();
                    if(target==0)ImGui.TextUnformatted(entry.Name);
                    ImGui.TableNextColumn();ImGui.TextUnformatted(hunt.Monster);
                    ImGui.TableNextColumn();
                    if(HuntingLog.Killed(entry,target,slot) is { } killed)
                    {
                        if(killed>=hunt.Needed)ImGui.TextColored(Success,$"{killed}/{hunt.Needed}");
                        else ImGui.TextColored(Accent,$"{killed}/{hunt.Needed}");
                    }
                    else ImGui.TextDisabled($"?/{hunt.Needed}");
                    ImGui.TableNextColumn();ImGui.TextWrapped(hunt.Zones);
                    // X-72 A (0.854): where you saw it, by its name row when known, else by its name.
                    var seenName=hunt.Monster;var seenRow=hunt.NameId;
                    DrawSeenPlaces($"Hunt{entry.RowId}.{target}",subject=>seenRow!=0 ? subject==seenRow:string.Equals(DiscoveryMonsterName(subject),seenName,StringComparison.CurrentCultureIgnoreCase),seenName);
                }
        }
        finally{ImGui.EndTable();}
    }
}
