using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

// 0.388 (user request: « a button, small but not too small, on the inventory window to apply the sorting rules », placed « in the
// bottom-left area of the window, on the same line as the gil »): drawn inside the game's inventory window (InventoryWindowReader), it applies the saved rules of the bags as « Apply » does on the sort page (displayed
// order only; stacks and quantities kept). Nothing happens without the click; the outcome shows beside it for a few seconds.
internal sealed partial class MainWindow
{
    internal InventoryWindowReader? InventoryWindow { get; set; }
    private string? inventorySortButtonMessage;
    private DateTimeOffset inventorySortButtonUntil;

    internal void DrawInventorySortButton()
    {
        if(windowDisposed || !configuration.InventorySortButton || InventoryWindow?.Rect is not { } window)return;
        var character=bankBridge.State.CharacterId;
        if(character==0)return;
        Panels?.Show("TenshoInventorySort","",ImGuiWindowFlags.NoDecoration|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.AlwaysAutoResize
            |ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav|ImGuiWindowFlags.NoBackground,()=>
        {
            var viewport=ImGui.GetMainViewport();
            var height=ImGui.GetFrameHeight();
            // Bottom-left of the window, centred on the gil line. The window's root node reaches past its visible frame: first try in game
            // (user report « it is shifted ») put 14 units too low by about 43; the offsets are settings (game units, scaled with the window).
            var scale=Math.Clamp(window.Scale,0.5f,3f);
            var right=Math.Clamp(configuration.InventorySortButtonX,0,400);var up=Math.Clamp(configuration.InventorySortButtonY,0,400);
            var position=viewport.Pos+new Vector2(window.X+right*scale,window.Y+window.Height-up*scale-height/2);
            ImGui.SetNextWindowPos(position,ImGuiCond.Always);
        },()=>
        {
                ImGui.BeginDisabled(visualSort.State.Pending);
                try
                {
                    // 0.389 (user choice): the icon is inside the button, so the whole « ⇅ Sort » (SortButton.Label) is clickable.
                    var icon=FontAwesomeIcon.SortAmountDown.ToIconString();var label=localization.Get("SortButton.Label");
                    ImGui.PushFont(UiBuilder.IconFont);
                    Vector2 iconSize;
                    try{iconSize=ImGui.CalcTextSize(icon);}
                    finally{ImGui.PopFont();}
                    var labelSize=ImGui.CalcTextSize(label);var pad=ImGui.GetStyle().FramePadding;var gap=ImGui.GetStyle().ItemInnerSpacing.X;
                    var corner=ImGui.GetCursorScreenPos();
                    var clicked=ImGui.Button("##InventorySort",new Vector2(pad.X*2+iconSize.X+gap+labelSize.X,ImGui.GetFrameHeight()));
                    var draw=ImGui.GetWindowDrawList();var middle=corner.Y+ImGui.GetFrameHeight()/2;
                    draw.AddText(UiBuilder.IconFont,ImGui.GetFontSize(),new Vector2(corner.X+pad.X,middle-iconSize.Y/2),ImGui.GetColorU32(Accent),icon);
                    draw.AddText(new Vector2(corner.X+pad.X+iconSize.X+gap,middle-labelSize.Y/2),ImGui.GetColorU32(ImGuiCol.Text),label);
                    if(clicked)
                    {
                        inventorySortButtonMessage=ApplyBagSort(character);
                        inventorySortButtonUntil=DateTimeOffset.UtcNow.AddSeconds(6);
                    }
                }
                finally{ImGui.EndDisabled();}
                if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get("SortButton.Tip"));
                if(inventorySortButtonMessage is { } message && DateTimeOffset.UtcNow<inventorySortButtonUntil)
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled(message);
                }
        },padding:Vector2.Zero);
    }

    // The same plan as the sort page's « Apply to my bags »: the four bags, the saved rules of this character's bags.
    private string ApplyBagSort(ulong character)
    {
        try
        {
            if(itemCatalog.State.Catalog is not { } catalog)return localization.Get("SortButton.NoCatalog");
            var scope=InventorySortScope.Personal(character);
            var snapshot=inventory.Current;
            var containers=snapshot?.Containers.Values.Where(container=>container.Location.Owner.Value==scope.InventoryOwner
                && container.Location.Container.Value>=scope.FirstContainer && container.Location.Container.Value<scope.FirstContainer+scope.ContainerCount).ToArray() ?? [];
            if(snapshot is null || containers.Length!=scope.ContainerCount || containers.Any(container=>container.Coverage!=InventoryCoverage.Complete))
                return localization.Get("SortButton.NotRead");
            var rules=InventorySortRules.Load(configuration.InventorySortProfiles,scope,(InventorySortField)Math.Clamp(configuration.SortPreviewField,0,SortFieldLabels.Length-1),configuration.SortPreviewDescending);
            // 0.595 (I21): asked now, read within two seconds: the player presses again; nothing moves on what is unread.
            if(InventorySortRules.NeedsGearsets(rules)){GearsetSource?.Want();if(GearsetSource?.State is null)return localization.Get("SortButton.GearsetsNotRead");}
            if(InventorySortRules.NeedsGear(rules) && GearCatalog?.Invoke() is null)return localization.Get("SortButton.GearNotReady");
            var result=InventorySortRules.Build(containers,scope,rules,SortDescriber(catalog));
            return visualSort.Request(InventoryVisualSortPlan.Build(snapshot,result,scope),DateTimeOffset.UtcNow)
                ? localization.Get("SortButton.Done"):localization.Get("SortButton.Busy");
        }
        catch(Exception error)
        {
            log.Warning(error,"The bags could not be sorted from the inventory button.");
            return localization.Get("SortButton.Failed");
        }
    }
}
