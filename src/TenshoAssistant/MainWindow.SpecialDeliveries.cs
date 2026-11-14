using Dalamud.Bindings.ImGui;
using TenshoAssistant.Deliveries;
using TenshoAssistant.Quests;

namespace TenshoAssistant;

// W-12 (0.715): the delivery quest lines of crafters and gatherers, from the quest journal Tensho already reads. docs/evidence/0.715-special-deliveries.md.
internal sealed partial class MainWindow
{
    private string? specialDeliveryFocus;
    private bool specialDeliveryOnlyLeft;
    private long specialDeliveryAsked=-60001;

    private void DrawSpecialDeliveries()
    {
        Heading(localization.Get("SpecialDeliveries.Title"),localization.Get("SpecialDeliveries.Subtitle"));
        var state=quests.State;
        if(state.Entries.Count==0 && Environment.TickCount64-specialDeliveryAsked>60000){specialDeliveryAsked=Environment.TickCount64;quests.RequestRefresh();}
        if(state.Loading || state.Entries.Count==0){ImGui.TextDisabled(LoadingText("SpecialDeliveries.Loading"));return;}
        ImGui.Checkbox(localization.Get("SpecialDeliveries.OnlyLeft")+"###SpecialDeliveryLeft",ref specialDeliveryOnlyLeft);
        Tip("SpecialDeliveries.OnlyLeftTip");
        var tabs=new List<(string Key,string Label,Action Draw)>();
        foreach(var genre in SpecialDeliveries.Genres)
        {
            var (done,total)=SpecialDeliveries.Count(state.Entries,genre);
            if(total==0)continue;
            var name=state.Entries.First(entry=>entry.GenreId==genre).Category;
            tabs.Add((genre.ToString(),$"{name} ({done}/{total})",()=>DrawSpecialDeliveryList(state.Entries,genre)));
        }
        if(tabs.Count==0){ImGui.TextDisabled(localization.Get("SpecialDeliveries.None"));return;}
        DetailTabs("SpecialDeliveryTabs",tabs,ref specialDeliveryFocus);
    }

    private void DrawSpecialDeliveryList(IReadOnlyList<QuestEntry> entries,uint genre)
    {
        var rows=SpecialDeliveries.Of(entries,genre,specialDeliveryOnlyLeft);
        if(rows.Count==0){ImGui.TextDisabled(localization.Get("SpecialDeliveries.AllDone"));return;}
        if(!ImGui.BeginTable($"###SpecialDeliveryTable{genre}",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))return;
        try
        {
            foreach(var column in new[]{"ColumnQuest","ColumnLevel","ColumnPlace","ColumnState"})ImGui.TableSetupColumn(localization.Get("SpecialDeliveries."+column));
            ImGui.TableHeadersRow();
            foreach(var row in rows)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                if(ImGui.Selectable($"{row.Name}##SpecialDelivery{row.Id}")){SelectQuest(row.Id);questShowDetails=true;page=Page.Quests;}
                QuestMenu($"SpecialDeliveryMenu{row.Id}",row.Id,row.Name);
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Level.ToString());
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Zone);
                ImGui.TableNextColumn();
                switch(row.Progress)
                {
                    case QuestProgress.Complete:ImGui.TextColored(Success,localization.Get("SpecialDeliveries.Done"));break;
                    case QuestProgress.Active:ImGui.TextColored(Accent,localization.Get("SpecialDeliveries.Active"));break;
                    default:ImGui.TextDisabled(localization.Get("SpecialDeliveries.Left"));break;
                }
            }
        }
        finally{ImGui.EndTable();}
    }
}
