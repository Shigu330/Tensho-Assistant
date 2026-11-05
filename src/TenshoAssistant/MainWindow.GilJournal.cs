using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

// g, I-04 (0.351): Storage › Gil — balances (character, retainers, company chest), gains and spendings per category over a chosen
// period, and the latest changes. From the journal the watcher keeps (Inventory/GilWatcher.Dalamud.cs); totals rebuilt only when the
// journal or the period changes.
internal sealed partial class MainWindow
{
    internal Func<GilJournalState?>? GilJournalState { get; set; }
    private int gilPeriod=1;
    private static readonly int[] GilPeriods=[1,7,30,90];
    private (GilJournalState State,int Period)? gilViewKey;
    private IReadOnlyList<(GilCategory Category,long Gained,long Spent)>[] gilTotals=[];

    private void DrawGilJournal()
    {
        ImGui.TextWrapped(localization.Get("GilJournal.Help"));
        if(GilJournalState?.Invoke() is not { } state){ImGui.TextDisabled(localization.Get("CharacterUi.Offline"));return;}
        if(state.Unavailable)ImGui.TextColored(Accent,localization.Get("GilJournal.Unreadable"));
        string Amount(long? value)=>value is { } known ? known.ToString("N0"):localization.Get("Currencies.Unknown");
        ImGui.TextUnformatted(localization.Format("GilJournal.Balances",Amount(state.Gil),Amount(state.RetainersGil),Amount(state.CompanyGil)));
        Tip("GilJournal.BalancesTip");
        ImGui.SetNextItemWidth(160);
        if(ImGui.BeginCombo(localization.Get("GilJournal.Period")+"##GilPeriod",localization.Format("GilJournal.Days",GilPeriods[gilPeriod])))
        {
            try{for(var index=0;index<GilPeriods.Length;index++)if(ImGui.Selectable(localization.Format("GilJournal.Days",GilPeriods[index]),index==gilPeriod))gilPeriod=index;}
            finally{ImGui.EndCombo();}
        }
        Tip("GilJournal.PeriodTip");
        if(gilViewKey is not { } key || !ReferenceEquals(key.State,state) || key.Period!=gilPeriod)
        {
            var since=DateTimeOffset.UtcNow.AddDays(-GilPeriods[gilPeriod]);
            gilTotals=Enum.GetValues<GilSource>().Select(source=>GilJournal.Totals(state.Entries,source,since)).ToArray();
            gilViewKey=(state,gilPeriod);
        }
        // 0.455 (user request, in-game campaign): tables instead of « +0 / −1 276 » lines; a zero is a dash.
        static string Part(long value)=>value==0 ? "—":value.ToString("N0");
        foreach(var source in Enum.GetValues<GilSource>())
        {
            var totals=gilTotals[(int)source];
            if(totals.Count==0)continue;
            ImGui.TextColored(Accent,localization.Format("GilJournal.Source"+source,totals.Sum(total=>total.Gained-total.Spent).ToString("+#,0;-#,0;0")));
            if(!ImGui.BeginTable("GilTotals"+source,4,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))continue;
            try
            {
                foreach(var column in new[]{"ColumnCategory","ColumnGained","ColumnSpent","ColumnNet"})ImGui.TableSetupColumn(localization.Get("GilJournal."+column));
                ImGui.TableHeadersRow();
                foreach(var (category,gained,spent) in totals)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Get("GilJournal.Category"+category));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(Part(gained));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(Part(spent));
                    ImGui.TableNextColumn();ImGui.TextUnformatted((gained-spent).ToString("+#,0;-#,0;0"));
                }
            }
            finally{ImGui.EndTable();}
        }
        if(gilTotals.All(totals=>totals.Count==0))ImGui.TextDisabled(localization.Get("GilJournal.Empty"));
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("GilJournal.Latest")))return;
        if(!ImGui.BeginTable("GilLatest",5,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))return;
        try
        {
            foreach(var column in new[]{"ColumnDate","ColumnChange","ColumnBalance","ColumnCategory","ColumnSource"})ImGui.TableSetupColumn(localization.Get("GilJournal."+column));
            ImGui.TableHeadersRow();
            foreach(var entry in state.Entries.TakeLast(50).Reverse())
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted(entry.At.ToLocalTime().ToString("g"));
                ImGui.TableNextColumn();ImGui.TextUnformatted(entry.Delta.ToString("+#,0;-#,0"));
                ImGui.TableNextColumn();ImGui.TextUnformatted(entry.Balance.ToString("N0"));
                ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Get("GilJournal.Category"+entry.Category));
                ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Get("GilJournal.Short"+entry.Source));
            }
        }
        finally{ImGui.EndTable();}
    }
}
