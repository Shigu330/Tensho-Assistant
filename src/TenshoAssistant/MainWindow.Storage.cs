using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// R-06 step 3 (0.280): « Storage », its own page — where the items are (character, retainers, saddlebags, armoury, glamour
// dresser, armoire: grids per container with origin tabs, category / quality filters and search), crystals, manual imports.
internal sealed partial class MainWindow
{
    private void DrawStorage()
    {
        // X-01 C5 (0.1206, analysis of 12/11: « page Storage 3 137 ms », no tab timed): each tab timed in developer mode (Slow frame line).
        // X-605 (0.1701): the tabs arranged with the mouse (OrderedTabs).
        var bank = focusBankTab; var currency = focusCurrencyTab; var value = focusCurrencyValueTab;
        focusBankTab = focusCurrencyTab = focusCurrencyValueTab = false;
        OrderedTabs("StorageTabs",
        [
            new("Bank", localization.Get("ItemsUi.Bank"), () => MeasureMap("storage bank", DrawBankBrowser), bank),
            new("Crystals", localization.Get("ItemsUi.Crystals"), () => MeasureMap("storage crystals", DrawCrystalsTab)),
            // 0.344 (g, I-02): currencies, caps and threshold alerts.
            new("Currencies", localization.Get("Currencies.Tab"), () => MeasureMap("storage currencies", DrawCurrenciesTab), currency),
            // 0.695 (user request): what a currency buys, in its own tab.
            new("CurrencyValue", localization.Get("CurrencyValue.Tab"), () => MeasureMap("storage currency value", DrawCurrencyValueTab), value),
            new("Imports", localization.Get("ItemsUi.Imports"), () => MeasureMap("storage imports", DrawImportManager)),
            // 0.345 (g, I-03): every character seen, with « See its storages » and « Erase this character ».
            new("Characters", localization.Get("CharactersUi.Tab"), () => MeasureMap("storage characters", DrawCharactersTab)),
            // 0.348 (g, I-10): storage report (stacks to merge, armoury pieces outside every gear set), nothing moved.
            new("Report", localization.Get("StorageReport.Tab"), () => MeasureMap("storage report", DrawStorageReport)),
            // 0.351 (g, I-04): gil journal (90 days).
            new("GilJournal", localization.Get("GilJournal.Tab"), () => MeasureMap("storage gil journal", DrawGilJournal)),
        ]);
    }
}
