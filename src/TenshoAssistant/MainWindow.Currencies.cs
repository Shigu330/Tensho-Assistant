using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

// g, I-02 (0.344): Storage › Currencies — balances, caps, percentage, per-currency alert threshold, this week's limited tomestones, and
// the two switches (alerts, server bar). The watcher reads on the framework thread (Inventory/CurrencyWatcher.Dalamud.cs); a threshold
// change replaces the whole dictionary (copy on write) and is saved.
internal sealed partial class MainWindow
{
    internal Func<CurrencyWatcher?>? Currencies { get; set; }
    private bool focusCurrencyTab;
    // Threshold being typed (saved once the field is left, as IntSetting does: not on every keystroke, 0.344 review).
    private readonly Dictionary<uint,int> currencyEdits=[];

    private void DrawCurrenciesTab()
    {
        ImGui.TextWrapped(localization.Get("Currencies.Help"));
        BoolSetting("Currencies.Alerts","Currencies.AlertsTip",()=>configuration.CurrencyAlerts,value=>configuration.CurrencyAlerts=value,c=>c.CurrencyAlerts);
        // X-76 step 3 (0.952): the weekly cap alert is raised only with the cap alerts (CurrencyWatcher), greyed while they are off.
        ImGui.Indent();
        ImGui.BeginDisabled(!configuration.CurrencyAlerts);
        try{BoolSetting("Currencies.WeeklyCap","Currencies.WeeklyCapTip",()=>configuration.CurrencyWeeklyCapAlert,value=>configuration.CurrencyWeeklyCapAlert=value,c=>c.CurrencyWeeklyCapAlert);}
        finally{ImGui.EndDisabled();ImGui.Unindent();}
        BoolSetting("Currencies.ServerBar","Currencies.ServerBarTip",()=>configuration.CurrencyServerBar,value=>configuration.CurrencyServerBar=value,c=>c.CurrencyServerBar);
        var watcher=Currencies?.Invoke();
        watcher?.Want();
        if(characterStats.State.CharacterId==0){ImGui.TextDisabled(localization.Get("Currencies.Offline"));return;}
        var state=watcher?.State;
        if(state?.Unavailable==true){ImGui.TextDisabled(localization.Get("Currencies.Unavailable"));return;}
        // 0.344 (review): a read of the previous character is not shown after a change of character.
        if(state?.Reading is not { } reading || reading.Character!=characterStats.State.CharacterId){ImGui.TextDisabled(localization.Get("Currencies.Loading"));return;}
        ImGui.TextDisabled(reading.WeeklyLimit==0 ? localization.Get("Currencies.WeeklyNone")
            :reading.WeeklyTomestones is { } got && reading.WeeklyLimit is { } limit
            ? localization.Format("Currencies.Weekly",got.ToString("N0"),limit.ToString("N0")):localization.Get("Currencies.WeeklyUnknown"));
        if(!reading.Balances.Any(balance=>balance.Currency.Kind==CurrencyKind.Seals))ImGui.TextDisabled(localization.Get("Currencies.NoSeals"));
        if(!ImGui.BeginTable("Currencies",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.BordersInnerH))return;
        try
        {
            foreach(var column in new[]{"Name","Amount","Percent","Threshold"})ImGui.TableSetupColumn(localization.Get("Currencies.Column"+column));
            ImGui.TableHeadersRow();
            var items=itemCatalog.State.Catalog;var size=new Vector2(ImGui.GetTextLineHeight());
            var thresholds=configuration.CurrencyThresholds;
            foreach(var balance in reading.Balances)
            {
                var id=balance.Currency.ItemId;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();DrawItemIcon(items?.Find(id),size);ImGui.SameLine();ImGui.TextUnformatted(balance.Currency.Name);
                // 0.354 (g, I-13): what this currency buys and what each unit is worth.
                DrawCurrencyValueButton(id);
                ImGui.TableNextColumn();
                var amount=balance.Amount is { } value ? value.ToString("N0"):localization.Get("Currencies.Unknown");
                // 0.344 (review): an unknown cap is « unknown », a cap that is not a real limit (gil-like) is « no cap ».
                var percentable=balance.Cap is { } cap && CurrencyAlertTracker.Percentable(cap);
                ImGui.TextUnformatted(balance.Cap is not { } known ? $"{amount} / {localization.Get("Currencies.Unknown")}"
                    :percentable ? $"{amount} / {known:N0}":$"{amount} ({localization.Get("Currencies.NoCap")})");
                ImGui.TableNextColumn();
                var percent=CurrencyAlertTracker.PercentFor(id,thresholds);
                if(percentable && balance.Percent is { } share)
                {
                    var over=percent>0 && share>=percent;
                    // Rounded down, so « 80 % » is shown only when the 80 % threshold is really reached (0.344 fix check).
                    var shown=$"{Math.Floor(share):0} %";
                    if(over)ImGui.TextColored(UI.FfxivTheme.Gold,shown);else ImGui.TextUnformatted(shown);
                }
                else ImGui.TextDisabled("—");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(90);
                var edited=currencyEdits.TryGetValue(id,out var typing) ? typing:percent;
                if(ImGui.InputInt($"%##Threshold{id}",ref edited,0))currencyEdits[id]=Math.Clamp(edited,0,100);
                if(ImGui.IsItemDeactivatedAfterEdit() && currencyEdits.Remove(id,out var done))SetCurrencyThreshold(id,done);
                // A value typed in a field that stopped being drawn (tab change, cutscene) is dropped, never shown as saved (0.344 fix check).
                else if(!ImGui.IsItemActive())currencyEdits.Remove(id);
                Tip("Currencies.ThresholdTip");
                if(DefaultButton($"Threshold{id}",percent!=CurrencyAlertTracker.DefaultPercent))SetCurrencyThreshold(id,null);
            }
        }
        finally{ImGui.EndTable();}
    }

    // Copy on write: the framework thread reads the dictionary without a lock (Configuration.CurrencyThresholds); saved like a setting
    // (put back if the save fails).
    private void SetCurrencyThreshold(uint item,int? percent)
    {
        currencyEdits.Remove(item);
        var copy=configuration.CurrencyThresholds is { } known ? new Dictionary<uint,int>(known):[];
        if(percent is { } value && value!=CurrencyAlertTracker.DefaultPercent)copy[item]=value;else copy.Remove(item);
        SaveSetting("Currencies.ColumnThreshold",()=>configuration.CurrencyThresholds,value=>configuration.CurrencyThresholds=value,copy.Count==0 ? null:copy);
    }
}
