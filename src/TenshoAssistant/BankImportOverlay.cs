using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;
using TenshoAssistant.Localization;
using TenshoAssistant.UI;

namespace TenshoAssistant;

/// <summary>Companion panels attached to visible game menus; native pointers never enter Draw.</summary>
internal sealed class BankImportOverlay(InventoryBankService bank, BankInventoryBridge bridge, LocalizationManager localization)
{
    public void Draw()
    {
        var menus = bridge.State;
        if (menus.Retainer is { } retainer) DrawPanel("Retainer", retainer, null, menus.Message);
        if (menus.Saddlebag is { } saddle) DrawPanel(localization.Get("BankUi.Chocobo"), saddle, menus.Premium, menus.Message);
    }

    private void DrawPanel(string id, BankMenuContext context, BankMenuContext? premium, string? message)
    {
        using var theme=FfxivTheme.Apply();
        var viewport = ImGui.GetMainViewport();
        var position = viewport.Pos + context.Anchor;
        var minimum = viewport.WorkPos;
        var maximum = Vector2.Max(minimum, viewport.WorkPos + viewport.WorkSize - new Vector2(330, premium is null ? 170 : 240));
        position = Vector2.Clamp(position, minimum, maximum);
        ImGui.SetNextWindowPos(position, ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(330, 0), ImGuiCond.Always);
        var visible = ImGui.Begin(localization.Get("BankUi.OverlayTitle")+$"###TenshoBankImport{id}",
            ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoFocusOnAppearing);
        try
        {
            if (!visible) return;
            ImGui.TextUnformatted("Tensho · "+localization.Get("BankUi.OverlayTitle"));
            DrawSource(context);
            if (premium is not null && premium.Loaded)
            {
                ImGui.Separator();
                DrawSource(premium);
            }
            if (message is not null) ImGui.TextWrapped(UiMessages.Get(localization,message,"ServiceErrors.Bank"));
            if (bank.State.Error is not null) ImGui.TextWrapped(localization.Get("ServiceErrors.Bank"));
        }
        finally { ImGui.End(); }
    }

    private void DrawSource(BankMenuContext context)
    {
        ImGui.TextWrapped(context.Name);
        var existing = bank.State.Entries.FirstOrDefault(entry => entry.Key == context.Key);
        ImGui.TextDisabled(existing is null ? localization.Get("BankUi.Never") : localization.Format("BankUi.Cache",existing.Snapshot.ObservedAt.ToLocalTime().ToString("g")));
        var disabled = !context.Loaded || bank.State.Loading || bank.State.Importing || bank.State.Error is not null;
        ImGui.BeginDisabled(disabled);
        try
        {
            if (ImGui.Button(localization.Get("BankUi.Import")+$"##{context.Key.Kind}", new Vector2(-1, 28))) bridge.RequestImport(context.Key);
        }
        finally { ImGui.EndDisabled(); }
        if (!context.Loaded) ImGui.TextWrapped(localization.Get("BankUi.InventoryWaiting"));
    }
}
