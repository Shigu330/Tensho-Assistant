using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// R-06 step 1 (0.247): the shared item templates. Every page draws items through them, so an item looks and answers the same
// everywhere: game icon; hover = the common item tooltip; right click = the useful tools (RelevantRoutes) and the item's sources.
// Icons that are not items (duties, actions, jobs, weather…) go through DrawGameIcon (X-76 H8, 0.920) and stay plain images.
internal sealed partial class MainWindow
{
    private int itemTooltipFrame=-1;private uint itemTooltipId;
    private readonly UI.IconWait itemIconWait=new();
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

    private void DrawItemImage(ItemDefinition? item,Vector2 size)=>DrawGameIcon(item?.IconId ?? 0,size);

    /// <summary>
    /// X-178 (0.1257): an item as an element the keyboard and the gamepad reach (ImGui.Image is not one): an invisible button of the icon's
    /// size with the game icon drawn on it. Without an icon (unknown item, or a texture that has not come after 60 frames: UI/IconWait.cs):
    /// the name as a selectable text in the given colour; while the texture loads: the empty button. The caller draws the tooltip, the menu
    /// and what goes on the icon from the last item. Returns whether it was clicked.
    /// </summary>
    private bool DrawItemIconOrText(ItemDefinition? item,Vector2 size,string name,Vector4 colour,out bool asIcon)
    {
        var iconId=item?.IconId ?? 0;
        var frame=ImGui.GetFrameCount();
        if(iconId!=0 && textures.TryGetFromGameIcon(new GameIconLookup(iconId,false,true),out var shared)
           && shared.TryGetWrap(out var texture,out _) && texture is not null)
        {
            itemIconWait.ShowText(iconId,true,frame);
            asIcon=true;
            var clicked=ImGui.InvisibleButton("##ItemIconOrText",size);
            ImGui.GetWindowDrawList().AddImage(texture.Handle,ImGui.GetItemRectMin(),ImGui.GetItemRectMax());
            return clicked;
        }
        if(itemIconWait.ShowText(iconId,false,frame))
        {
            asIcon=false;
            ImGui.PushStyleColor(ImGuiCol.Text,colour);
            // As wide as the name: several of them can share a line.
            try{return ImGui.Selectable(name+"##ItemIconOrText",false,ImGuiSelectableFlags.None,new Vector2(ImGui.CalcTextSize(name).X,0));}
            finally{ImGui.PopStyleColor();}
        }
        asIcon=true;
        return ImGui.InvisibleButton("##ItemIconOrText",size);
    }

    /// <summary>
    /// X-76 H8 (0.920): a game icon that is not an item (duty, action, job, weather, card…), a plain image. Missing icon: an empty space of the same
    /// size, or nothing when <paramref name="keepSpace"/> is false; returns whether the image was drawn. Borrowed for this UI frame only;
    /// ownership remains with Dalamud.
    /// </summary>
    private bool DrawGameIcon(uint iconId,Vector2 size,bool keepSpace=true)
    {
        if(iconId!=0 && textures.TryGetFromGameIcon(new GameIconLookup(iconId,false,true),out var shared)
           && shared.TryGetWrap(out var texture,out _) && texture is not null)
        {ImGui.Image(texture.Handle,size);return true;}
        if(keepSpace)ImGui.Dummy(size);
        return false;
    }

    // One item tooltip per frame: an icon and its name next to it no longer stack two copies of the same tooltip.
    private bool ItemTooltipAlreadyDrawn(uint id)
    {
        var frame=ImGui.GetFrameCount();
        if(frame==itemTooltipFrame && id==itemTooltipId)return true;
        itemTooltipFrame=frame;itemTooltipId=id;return false;
    }
}
