using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// R-06 step 3 (0.280): « Storage », its own page — where the items are (character, retainers, saddlebags, armoury, glamour
// dresser, armoire: grids per container with origin tabs, category / quality filters and search), crystals, manual imports.
internal sealed partial class MainWindow
{
    private void DrawStorage()
    {
        Heading(localization.Get("StorageUi.Title"), localization.Get("StorageUi.Description"));
        if (!ImGui.BeginTabBar("StorageTabs")) return;
        try
        {
            var bankFlags = focusBankTab ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None; focusBankTab = false;
            if (ImGui.BeginTabItem(localization.Get("ItemsUi.Bank"), bankFlags)) { try { DrawBankBrowser(); } finally { ImGui.EndTabItem(); } }
            if (ImGui.BeginTabItem(localization.Get("ItemsUi.Crystals"))) { try { DrawCrystalsTab(); } finally { ImGui.EndTabItem(); } }
            // 0.344 (g, I-02): currencies, caps and threshold alerts.
            var currencyFlags = focusCurrencyTab ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None; focusCurrencyTab = false;
            if (ImGui.BeginTabItem(localization.Get("Currencies.Tab")+"###CurrenciesTab", currencyFlags)) { try { DrawCurrenciesTab(); } finally { ImGui.EndTabItem(); } }
            // 0.695 (user request): what a currency buys, in its own tab.
            var valueFlags = focusCurrencyValueTab ? ImGuiTabItemFlags.SetSelected : ImGuiTabItemFlags.None; focusCurrencyValueTab = false;
            if (ImGui.BeginTabItem(localization.Get("CurrencyValue.Tab")+"###CurrencyValueTab", valueFlags)) { try { DrawCurrencyValueTab(); } finally { ImGui.EndTabItem(); } }
            if (ImGui.BeginTabItem(localization.Get("ItemsUi.Imports"))) { try { DrawImportManager(); } finally { ImGui.EndTabItem(); } }
            // 0.345 (g, I-03): every character seen, with « See its storages » and « Erase this character ».
            if (ImGui.BeginTabItem(localization.Get("CharactersUi.Tab")+"###CharactersTab")) { try { DrawCharactersTab(); } finally { ImGui.EndTabItem(); } }
            // 0.348 (g, I-10): storage report (stacks to merge, armoury pieces outside every gear set), nothing moved.
            if (ImGui.BeginTabItem(localization.Get("StorageReport.Tab")+"###StorageReportTab")) { try { DrawStorageReport(); } finally { ImGui.EndTabItem(); } }
            // 0.351 (g, I-04): gil journal (90 days).
            if (ImGui.BeginTabItem(localization.Get("GilJournal.Tab")+"###GilJournalTab")) { try { DrawGilJournal(); } finally { ImGui.EndTabItem(); } }
        }
        finally { ImGui.EndTabBar(); }
    }
}
