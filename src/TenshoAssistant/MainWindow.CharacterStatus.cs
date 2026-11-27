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
        // X-119 (0.1151): in the desktop application (no watcher: no game), the characters read from its copy (MainWindow.DesktopData.cs), by name,
        // none logged in, without the erase button (erasing stays in the game).
        var source=CharacterStatusSource;
        IReadOnlyList<CharacterStatus> all;
        if(source is not null)all=source.All;
        else if(Volatile.Read(ref desktopCharacters) is { } copy)all=[.. copy.OrderBy(status=>status.Name)];
        else return;
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("CharacterStatus.Title",all.Count)+"###CharacterStatuses",ImGuiTreeNodeFlags.DefaultOpen))return;
        // X-435 M4 (0.1421): the help lines wrap at the page's edge (they were cut at 1280 px).
        ImGui.PushTextWrapPos(0);
        ImGui.TextDisabled(localization.Get("CharacterStatus.Help"));
        ImGui.PopTextWrapPos();
        if(all.Count==0){ImGui.TextDisabled(localization.Get("CharacterStatus.Empty"));return;}
        var now=DateTimeOffset.UtcNow;var lastDaily=ResetSchedule.PreviousDaily(now,ResetSchedule.DailyHour);var current=source?.Current ?? 0;
        var lastWeekly=ResetSchedule.PreviousWeekly(now,ResetSchedule.WeeklyDay,ResetSchedule.WeeklyHour);
        if(!ImGui.BeginTable("CharacterStatuses",8,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH|ImGuiTableFlags.ScrollX,
               new Vector2(0,Math.Min(260,(all.Count+1)*ImGui.GetFrameHeightWithSpacing()+8))))return;
        try
        {
            foreach(var key in new[]{"Name","Seen","Ventures","Treasure","Bingo","HuntBills","Roulettes","Other"})ImGui.TableSetupColumn(localization.Get("CharacterStatus.Column"+key));
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
                    if(source is not null && status.ContentId!=current)
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
                        if(status.BingoExpires is { } expires)TipText(()=>localization.Format("CharacterStatus.BingoExpires",expires.ToLocalTime().ToString("g")));
                    }
                    ImGui.TableNextColumn();
                    if(status.HuntBills is { } bills)ImGui.TextUnformatted(bills.ToString(Numbers));
                    else ImGui.TextDisabled(localization.Get("CharacterStatus.Unknown"));
                    ImGui.TableNextColumn();
                    if(CharacterStatuses.RoulettesToday(status,lastDaily) is { } roulettes)
                    {
                        var text=$"{roulettes.Done}/{roulettes.Total}";
                        if(roulettes.Done>=roulettes.Total)ImGui.TextColored(Success,text);else ImGui.TextUnformatted(text);
                    }
                    else ImGui.TextDisabled(localization.Get(status.RoulettesAt is null ? "CharacterStatus.Unknown":"CharacterStatus.SinceReset"));
                    // X-76 P9 (0.936): leves, deliveries, allied quests and squadron, as last read for this character.
                    ImGui.TableNextColumn();
                    DrawCharacterOther(status,now,lastDaily,lastWeekly);
                }
                finally{ImGui.PopID();}
            }
        }
        finally{ImGui.EndTable();}
    }

    // X-196 (0.1410): every retainer of every character, as last read at a summoning bell (0.1409); shown once one was read. In the desktop
    // application, from its copy of the character files, like the characters' table.
    private void DrawRetainerTable()
    {
        var source=CharacterStatusSource;
        IReadOnlyList<CharacterStatus> all;
        if(source is not null)all=source.All;
        else if(Volatile.Read(ref desktopCharacters) is { } copy)all=[.. copy.OrderBy(status=>status.Name)];
        else return;
        var rows=RetainerStates.Table(all);
        if(rows.Count==0)return;
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("CharacterStatus.RetainersTitle",rows.Count)+"###RetainerTable",ImGuiTreeNodeFlags.DefaultOpen))return;
        ImGui.PushTextWrapPos(0);
        ImGui.TextDisabled(localization.Get("CharacterStatus.RetainersHelp"));
        ImGui.PopTextWrapPos();
        var now=DateTimeOffset.UtcNow;
        if(!TenshoAssistant.UI.UiComponents.BeginDataTable("RetainerTable",8,scrollHeight:Math.Min(320,(rows.Count+1+rows.Sum(row=>RetainerListingLines(row.Retainer)))*ImGui.GetFrameHeightWithSpacing()+8)))return;
        try
        {
            // X-435 M4 (0.1421): Gil and Items fit their numbers (they took a stretched share and left the market town cut at 1280 px).
            foreach(var key in new[]{"Character","Name","Job","Gil","Items","Market","Venture","Town"})
                ImGui.TableSetupColumn(localization.Get("CharacterStatus.RetainerColumn"+key),key is "Gil" or "Items" ? ImGuiTableColumnFlags.WidthFixed:ImGuiTableColumnFlags.None);
            ImGui.TableSetupScrollFreeze(2,1);
            ImGui.TableHeadersRow();
            var index=0;
            foreach(var (character,retainer) in rows)
            {
                ImGui.PushID(index++);
                try
                {
                    ImGui.TableNextRow();ImGui.TableNextColumn();
                    ImGui.TextUnformatted(character.Name.Length>0 ? character.Name:localization.Get("CharacterStatus.Unnamed"));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(retainer.Name);
                    ImGui.TableNextColumn();
                    if(retainer.Job.Length>0 || retainer.Level>0)ImGui.TextUnformatted(localization.Format("CharacterStatus.RetainerJob",retainer.Job,retainer.Level));
                    else ImGui.TextDisabled(localization.Get("CharacterStatus.Unknown"));
                    ImGui.TableNextColumn();TenshoAssistant.UI.UiComponents.NumberCell(retainer.Gil.ToString("N0",Numbers));
                    ImGui.TableNextColumn();TenshoAssistant.UI.UiComponents.NumberCell(retainer.Items.ToString(Numbers));
                    TipText(retainer.Stacks is { } stacks ? localization.Format("CharacterStatus.RetainerStock",stacks,retainer.HqStacks ?? 0,retainer.Crystals ?? 0)
                        :localization.Get("CharacterStatus.RetainerStockUnknown"));
                    ImGui.TableNextColumn();DrawRetainerSale(retainer,now);
                    ImGui.TableNextColumn();
                    if(retainer.Venture==0)ImGui.TextDisabled(localization.Get("CharacterStatus.RetainerNone"));
                    else if(retainer.VentureDone is not { } done)ImGui.TextUnformatted(localization.Get("CharacterStatus.RetainerVentureRunning"));
                    else if(done<=now)ImGui.TextColored(Success,localization.Get("CharacterStatus.OtherDone"));
                    else ImGui.TextUnformatted(done.ToLocalTime().ToString("g"));
                    ImGui.TableNextColumn();
                    if(retainer.Town.Length>0)ImGui.TextUnformatted(retainer.Town);else ImGui.TextDisabled(localization.Get("CharacterStatus.Unknown"));
                    DrawRetainerListing(retainer);
                }
                finally{ImGui.PopID();}
            }
        }
        finally{ImGui.EndTable();}
        ImGui.TextDisabled(localization.Format("CharacterStatus.RetainersGil",rows.Sum(row=>row.Retainer.Gil).ToString("N0",Numbers)));
    }

    // X-469 (0.1762, D-174): under the retainer's row, its items on sale as last read in the game's sale list, dated (« not read yet » when
    // something is on sale and the list was never read; nothing when nothing is on sale and nothing was read).
    // The lines the row under the retainer takes (none when it is not drawn), for the table's height.
    private static int RetainerListingLines(RetainerState retainer)=>retainer.Listed is null && RetainerStates.NothingOnSale(retainer.MarketItems) ? 0
        :Math.Max(1,retainer.Listed?.Length ?? 0);

    private void DrawRetainerListing(RetainerState retainer)
    {
        if(RetainerListingLines(retainer)==0)return;
        ImGui.TableNextRow();ImGui.TableNextColumn();ImGui.TableNextColumn();
        if(retainer.Listed is not { } listed || retainer.ListedAt is not { } at)
        {
            ImGui.TextDisabled(localization.Get("CharacterStatus.ListedNever"));
            Tip("CharacterStatus.ListedNeverTip");
            return;
        }
        var date=at.ToLocalTime().ToString("g");
        if(listed.Length==0){ImGui.TextDisabled(localization.Format("CharacterStatus.ListedNone",date));return;}
        ImGui.TextDisabled(localization.Format("CharacterStatus.ListedAt",date));
        for(var column=0;column<4;column++)ImGui.TableNextColumn();
        var items=itemCatalog.State.Catalog;
        foreach(var item in listed)
            ImGui.TextUnformatted(localization.Format("CharacterStatus.ListedItem",(items?.Find(item.ItemId)?.Name ?? item.ItemId.ToString(Numbers))+(item.Hq ? " HQ":""),
                item.Quantity.ToString(Numbers),item.UnitPrice?.ToString("N0",Numbers) ?? "?"));
    }

    private void DrawRetainerSale(RetainerState retainer,DateTimeOffset now)
    {
        var sale=RetainerStates.Sale(retainer,now);
        if(sale==RetainerSale.None){ImGui.TextDisabled(localization.Get("CharacterStatus.RetainerNone"));return;}
        var end=retainer.MarketEnds?.ToLocalTime().ToString("g");
        var text=end is null ? localization.Format("CharacterStatus.RetainerMarketCount",retainer.MarketItems)
            :localization.Format(sale==RetainerSale.Ended ? "CharacterStatus.RetainerMarketEnded":"CharacterStatus.RetainerMarket",retainer.MarketItems,end);
        if(sale==RetainerSale.Ended)ImGui.TextColored(Error,text);
        else if(sale==RetainerSale.EndingSoon)ImGui.TextColored(Warning,text);
        else ImGui.TextUnformatted(text);
    }

    private void DrawCharacterOther(CharacterStatus status,DateTimeOffset now,DateTimeOffset lastDaily,DateTimeOffset lastWeekly)
    {
        var (allied,deliveries)=CharacterStatuses.DailyWeekly(status,lastDaily,lastWeekly);
        var parts=new List<string>();var detail=new List<string>();
        if(status.LeveAllowances is { } leves)
        {
            parts.Add(localization.Format("CharacterStatus.OtherLeves",leves));
            if(status.NextLeveAllowances is { } more)detail.Add(localization.Format("CharacterStatus.OtherLevesNext",more.ToLocalTime().ToString("g")));
        }
        if(deliveries is { } left)parts.Add(localization.Format("CharacterStatus.OtherDeliveries",left));
        if(allied is { } alliedLeft)parts.Add(localization.Format("CharacterStatus.OtherAllied",alliedLeft));
        foreach(var (done,key) in new[]{(status.SquadronMissionDone,"CharacterStatus.OtherMission"),(status.SquadronTrainingDone,"CharacterStatus.OtherTraining")})
            if(done is { } end)detail.Add(localization.Format(key,end<=now ? localization.Get("CharacterStatus.OtherDone"):end.ToLocalTime().ToString("g")));
        if(parts.Count==0 && detail.Count==0){ImGui.TextDisabled(localization.Get("CharacterStatus.Unknown"));return;}
        ImGui.TextUnformatted(parts.Count>0 ? string.Join(" · ",parts):localization.Get("CharacterStatus.OtherMore"));
        if(detail.Count>0)TipText(string.Join("\n",detail));
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
