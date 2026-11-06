using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Character;
using TenshoAssistant.Reminders;

namespace TenshoAssistant;

// g, M-04 (0.374): the player's characters on the Reminders page (CharacterStatusWatcher, one file per character): retainer ventures,
// next treasure map, Wondrous Tails, hunt bills, daily roulettes done today, when each was last seen; another character can be erased
// (two clicks). The company's vessels stay on the Free company page and the « Voyages » row (they belong to the company, not a character).
internal sealed partial class MainWindow
{
    internal CharacterStatusWatcher? CharacterStatusSource { get; set; }
    private ulong characterErasePending;

    private void DrawCharacterStatuses()
    {
        if(CharacterStatusSource is not { } source)return;
        var all=source.All;
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("CharacterStatus.Title",all.Count)+"###CharacterStatuses",ImGuiTreeNodeFlags.DefaultOpen))return;
        ImGui.TextDisabled(localization.Get("CharacterStatus.Help"));
        if(all.Count==0){ImGui.TextDisabled(localization.Get("CharacterStatus.Empty"));return;}
        var now=DateTimeOffset.UtcNow;var lastDaily=ResetSchedule.PreviousDaily(now,ResetSchedule.DailyHour);var current=source.Current;
        if(!ImGui.BeginTable("CharacterStatuses",7,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH|ImGuiTableFlags.ScrollX,
               new Vector2(0,Math.Min(260,(all.Count+1)*ImGui.GetFrameHeightWithSpacing()+8))))return;
        try
        {
            foreach(var key in new[]{"Name","Seen","Ventures","Treasure","Bingo","HuntBills","Roulettes"})ImGui.TableSetupColumn(localization.Get("CharacterStatus.Column"+key));
            ImGui.TableSetupScrollFreeze(1,1);
            ImGui.TableHeadersRow();
            foreach(var status in all)
            {
                ImGui.PushID(status.ContentId.ToString(System.Globalization.CultureInfo.InvariantCulture));
                try
                {
                    ImGui.TableNextRow();ImGui.TableNextColumn();
                    var name=status.Name.Length>0 ? status.Name:localization.Get("CharacterStatus.Unnamed");
                    if(status.ContentId==current)ImGui.TextColored(Accent,name);else ImGui.TextUnformatted(name);
                    if(status.ContentId!=current)
                    {
                        ImGui.SameLine();
                        if(characterErasePending==status.ContentId)
                        {
                            if(ImGui.SmallButton(localization.Get("CharacterStatus.EraseConfirm"))){source.Erase(status.ContentId);characterErasePending=0;}
                            Tip("CharacterStatus.EraseConfirmTip");
                            ImGui.SameLine();
                            if(ImGui.SmallButton(localization.Get("CharacterStatus.EraseCancel")))characterErasePending=0;
                        }
                        else if(ImGui.SmallButton(localization.Get("CharacterStatus.Erase")))characterErasePending=status.ContentId;
                        Tip("CharacterStatus.EraseTip");
                    }
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(status.ContentId==current ? localization.Get("CharacterStatus.Now"):status.SeenAt.ToLocalTime().ToString("g"));
                    ImGui.TableNextColumn();
                    if(CharacterStatuses.Ventures(status,now) is not { } ventures)ImGui.TextDisabled(localization.Get("CharacterStatus.Unknown"));
                    else
                    {
                        if(ventures.Ready>0)ImGui.TextColored(Success,localization.Format("CharacterStatus.VenturesReady",ventures.Ready));
                        else ImGui.TextUnformatted(localization.Format("CharacterStatus.VenturesRunning",ventures.Running));
                        if(ventures.Next is { } next){ImGui.SameLine();ImGui.TextDisabled(next.ToLocalTime().ToString("g"));}
                    }
                    ImGui.TableNextColumn();
                    if(status.NextMap is not { } map)ImGui.TextDisabled(localization.Get("CharacterStatus.Unknown"));
                    else if(map<=now)ImGui.TextColored(Success,localization.Get("Reminders.TreasureReady"));
                    else ImGui.TextUnformatted(map.ToLocalTime().ToString("g"));
                    ImGui.TableNextColumn();
                    if(status.BingoHeld is null)ImGui.TextDisabled(localization.Get("CharacterStatus.Unknown"));
                    else if(status.BingoHeld==false || status.BingoExpires<=now)ImGui.TextDisabled(localization.Get("Reminders.BingoNone"));
                    else
                    {
                        ImGui.TextUnformatted(localization.Format("CharacterStatus.BingoState",status.BingoStickers ?? 0));
                        if(status.BingoExpires is { } expires && ImGui.IsItemHovered())WrappedTooltip(localization.Format("CharacterStatus.BingoExpires",expires.ToLocalTime().ToString("g")));
                    }
                    ImGui.TableNextColumn();
                    if(status.HuntBills is { } bills)ImGui.TextUnformatted(bills.ToString(System.Globalization.CultureInfo.CurrentCulture));
                    else ImGui.TextDisabled(localization.Get("CharacterStatus.Unknown"));
                    ImGui.TableNextColumn();
                    if(CharacterStatuses.RoulettesToday(status,lastDaily) is { } roulettes)
                    {
                        var text=$"{roulettes.Done}/{roulettes.Total}";
                        if(roulettes.Done>=roulettes.Total)ImGui.TextColored(Success,text);else ImGui.TextUnformatted(text);
                    }
                    else ImGui.TextDisabled(localization.Get(status.RoulettesAt is null ? "CharacterStatus.Unknown":"CharacterStatus.SinceReset"));
                }
                finally{ImGui.PopID();}
            }
        }
        finally{ImGui.EndTable();}
    }

    // Reminders « done » boxes: the logged-in character's (0.374, M-04), or the shared ones when no character is logged in.
    private DateTimeOffset? ReminderDoneAt(string key)
        =>CharacterStatusSource?.TryDone(key,out var at)==true ? at:configuration.ReminderDone?.GetValueOrDefault(key);
    private void SetReminderDone(string key,DateTimeOffset? at)
    {
        if(CharacterStatusSource?.SetDone(key,at)==true)return;
        var map=configuration.ReminderDone??=[];
        if(at is { } when)map[key]=when;else map.Remove(key);
        SaveReminders();
    }
}
