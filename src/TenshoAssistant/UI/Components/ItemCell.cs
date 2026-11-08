using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// R-06 step 1 (0.247): the shared item templates. Every page draws items through them, so an item looks and answers the same
// everywhere: game icon; hover = the common item tooltip; right click = the useful tools (RelevantRoutes) and the item's sources.
// Icons that are not items (duties, actions, jobs, weather…) are passed as ItemDefinition with Id 0 and stay plain images.
internal sealed partial class MainWindow
{
    private int itemTooltipFrame=-1;private uint itemTooltipId;
    private int menuFrame=-1;
    private readonly Dictionary<uint,int> menuSeen=[];

    // 0.269: an icon and the name next to it used the same popup id; the open menu was then drawn twice (user report: the same menu several times).
    // 0.737 (user report: the right-click menu of a project's material closed at once, now and then): the serial counted every icon of the frame, in every
    // window, so one icon more or less anywhere before it changed the id and ImGui closed the menu. It now counts only the same item at the same place
    // (same id stack), which changes only if that very list changes.
    private string UniqueMenuId(string baseId)
    {
        var frame=ImGui.GetFrameCount();
        if(frame!=menuFrame){menuFrame=frame;menuSeen.Clear();}
        var key=ImGui.GetID(baseId);
        var seen=menuSeen.GetValueOrDefault(key);
        menuSeen[key]=seen+1;
        return seen==0 ? baseId:$"{baseId}#{seen}";
    }

    /// <summary>Game icon of an item; for a real item (Id &gt; 0) also its tooltip and right-click menu, unless <paramref name="interactive"/> is false.</summary>
    private void DrawItemIcon(ItemDefinition? item,Vector2 size,bool interactive=true)
    {
        DrawItemImage(item,size);
        if(!interactive || item is not { Id:>0 })return;
        // 0.275: the ✓/✗ collection badge on every collectible item icon (mount, minion, card, roll), not only in a few lists.
        DrawCollectionMark(item);
        if(ImGui.IsItemHovered())DrawItemInfoTooltip(item.Id);
        ItemLinksMenu(UniqueMenuId($"ItemCell{item.Id}"),item);
    }

    /// <summary>Icon then name on one line, the name selectable: click = item in context, hover and right click as the icon.</summary>
    private bool ItemCell(ItemDefinition? item,uint id,float size=0,string? label=null)
    {
        var side=size>0 ? size:ImGui.GetTextLineHeight();
        DrawItemIcon(item,new Vector2(side));
        var iconClicked=ImGui.IsItemClicked();
        ImGui.SameLine();
        var clicked=iconClicked | ImGui.Selectable($"{label ?? item?.Name ?? localization.Format("BankUi.Item",id)}##ItemCellName{id}",itemId==(int)id,ImGuiSelectableFlags.None,new Vector2(0,side));
        if(ImGui.IsItemHovered())DrawItemInfoTooltip(id);
        ItemLinksMenu($"ItemCellName{id}",item);
        if(clicked)SelectItem(id);
        return clicked;
    }

    // Borrowed for this UI frame only; ownership remains with Dalamud.
    private void DrawItemImage(ItemDefinition? item,Vector2 size)
    {
        if(item is not null && item.IconId!=0
           && textures.TryGetFromGameIcon(new GameIconLookup(item.IconId,false,true),out var shared)
           && shared.TryGetWrap(out var texture,out _) && texture is not null)
            ImGui.Image(texture.Handle,size);
        else ImGui.Dummy(size);
    }

    // One item tooltip per frame: an icon and its name next to it no longer stack two copies of the same tooltip.
    private bool ItemTooltipAlreadyDrawn(uint id)
    {
        var frame=ImGui.GetFrameCount();
        if(frame==itemTooltipFrame && id==itemTooltipId)return true;
        itemTooltipFrame=frame;itemTooltipId=id;return false;
    }
}
