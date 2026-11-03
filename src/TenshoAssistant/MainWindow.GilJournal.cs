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
        foreach(var source in Enum.GetValues<GilSource>())
        {
            var totals=gilTotals[(int)source];
            if(totals.Count==0)continue;
            ImGui.TextColored(Accent,localization.Format("GilJournal.Source"+source,totals.Sum(total=>total.Gained-total.Spent).ToString("+#,0;-#,0;0")));
            foreach(var (category,gained,spent) in totals)
                ImGui.BulletText(localization.Format("GilJournal.Line",localization.Get("GilJournal.Category"+category),gained.ToString("N0"),spent.ToString("N0")));
        }
        if(gilTotals.All(totals=>totals.Count==0))ImGui.TextDisabled(localization.Get("GilJournal.Empty"));
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("GilJournal.Latest")))return;
        foreach(var entry in state.Entries.TakeLast(50).Reverse())
            ImGui.TextUnformatted($"{entry.At.ToLocalTime():g}  {entry.Delta:+#,0;-#,0}  → {entry.Balance:N0}  · {localization.Get("GilJournal.Category"+entry.Category)} · {localization.Get("GilJournal.Short"+entry.Source)}");
    }
}
