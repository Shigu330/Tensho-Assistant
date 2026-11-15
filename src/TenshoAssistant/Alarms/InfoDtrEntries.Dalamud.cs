using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Inventory;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using TenshoAssistant.Competitive;
using TenshoAssistant.Gear;

namespace TenshoAssistant.Alarms;

/// <summary>
/// X-217 (0.1226, user request of 12/11, plugin comparison K-30): three optional entries of the server info bar, each off by default and
/// built like the gil and currency entries: the four bags' free slots, the lowest condition of the worn gear, and the zone with its instance
/// number. Read on the framework thread every 2 s (only the entries turned on), shown text changed only when it changes; gone during a
/// competitive cut (DEC-038); removed on dispose. A click opens Storage, Gear or the map.
/// The instance is UIState.PublicInstance.InstanceId (fields only, no game function), shown only while its TerritoryTypeId is the current
/// territory (members checked with tools/ApiProbe); a read that fails turns that line off for the session (R08), never a wrong number.
/// X-276 (0.1388): their tooltip is Tensho's, as the other entries' (setting « Tensho's tooltip », above or below the entry): the game's is
/// left empty once the entry's place on screen is known, and Tensho's drawn from the plugin's draw (DrawTooltip).
/// </summary>
public sealed class InfoDtrEntries : IDisposable
{
    private static readonly GameInventoryType[] Bags=[GameInventoryType.Inventory1,GameInventoryType.Inventory2,GameInventoryType.Inventory3,GameInventoryType.Inventory4];
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly IClientState client;
    private readonly IGameInventory inventory;
    private readonly Configuration configuration;
    private readonly Func<IReadOnlyList<EquippedPiece>> equipped;
    private readonly Func<uint,string?> territoryName;
    private readonly Func<string,string> text;
    private readonly Func<string,object[],string> format;
    private readonly IDtrBarEntry bags,wear,place;
    private readonly IDtrBarEntry[] entries;
    private readonly string?[] shown=new string?[3];
    /// <summary>X-276: what each entry shows and where it is on screen (null when hidden or not found), for the draw thread.</summary>
    private sealed record Shown(string Label,string Tip,(Vector2 Min,Vector2 Max)? Bounds);
    private readonly Shown?[] lines=new Shown?[3];
    private DateTimeOffset nextRefresh;
    private bool instanceFailed,disposed;

    public InfoDtrEntries(IDtrBar bar,IFramework framework,IGameGui gui,IClientState client,IGameInventory inventory,Configuration configuration,
        Func<IReadOnlyList<EquippedPiece>> equipped,Func<uint,string?> territoryName,Func<string,string> text,Func<string,object[],string> format,
        Action openStorage,Action openGear,Action openMap)
    {
        this.framework=framework;this.gui=gui;this.client=client;this.inventory=inventory;this.configuration=configuration;this.equipped=equipped;
        this.territoryName=territoryName;this.text=text;this.format=format;
        bags=bar.Get("Tensho Assistant (Bags)");wear=bar.Get("Tensho Assistant (Gear condition)");place=bar.Get("Tensho Assistant (Place)");
        entries=[bags,wear,place];
        foreach(var entry in entries)entry.Shown=false;
        bags.OnClick=_=>openStorage();wear.OnClick=_=>openGear();place.OnClick=_=>openMap();
        CompetitiveGate.AddAlways(framework,OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        if(CompetitiveGate.Cut){bags.Shown=wear.Shown=place.Shown=false;return;}
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRefresh)return;
        nextRefresh=now.AddSeconds(2);
        var loggedIn=client.IsLoggedIn;
        Show(bags,0,loggedIn && configuration.BagsServerBar ? BagsLine():null);
        Show(wear,1,loggedIn && configuration.WearServerBar ? WearLine():null);
        Show(place,2,loggedIn && configuration.PlaceServerBar ? PlaceLine():null);
    }

    private void Show(IDtrBarEntry entry,int index,(string Label,string Tip)? line)
    {
        if(line is not { } shownLine){entry.Shown=false;shown[index]=null;lines[index]=null;return;}
        entry.Shown=true;
        var bounds=AlarmDtrEntry.FindTextNode(gui,shownLine.Label);
        lines[index]=new(shownLine.Label,shownLine.Tip,bounds);
        // X-276: Tensho's tooltip once the entry is found on screen (the game's stays until then, or when Tensho's is turned off).
        var own=configuration.AlarmBarTooltip && bounds is not null;
        var key=shownLine.Label+"\n"+shownLine.Tip+(own ? "\nown":"");
        if(shown[index]==key)return;
        shown[index]=key;
        entry.Text=new SeStringBuilder().AddText(shownLine.Label).Build();
        entry.Tooltip=configuration.AlarmBarTooltip && bounds is not null ? null:new SeStringBuilder().AddText(shownLine.Tip).Build();
    }

    /// <summary>X-276 (0.1388): Tensho's tooltip while the mouse is over one of the entries (ImGui, from the plugin's draw).</summary>
    public void DrawTooltip()
    {
        if(disposed || !configuration.AlarmBarTooltip)return;
        for(var index=0;index<lines.Length;index++)
        {
            // Hidden (turned off, or a competitive cut, DEC-038): no tooltip.
            if(lines[index] is not { } line || !entries[index].Shown || !TooltipScope.UnderBox(line.Bounds,configuration.ServerBarTooltipAbove))continue;
            using(TooltipScope.Begin())
            {
                ImGui.TextColored(UI.FfxivTheme.Gold,line.Label);
                foreach(var part in line.Tip.Split('\n'))ImGui.TextUnformatted(part);
            }
            return;
        }
    }

    private (string,string)? BagsLine()
    {
        int free=0,size=0;
        foreach(var bag in Bags)
        {
            var slots=inventory.GetInventoryItems(bag);
            size+=slots.Length;
            foreach(ref readonly var slot in slots)if(slot.IsEmpty || slot.Quantity==0)free++;
        }
        // Bags not given yet (just logged in): nothing rather than « 0 ».
        return size==0 ? null:(format("ServerBarInfo.Bags",[free]),format("ServerBarInfo.BagsTip",[free,size]));
    }

    private (string,string)? WearLine()
    {
        var state=GearWear.Evaluate(equipped(),configuration.GearWear ?? new());
        if(state.LowestCondition is not { } lowest)return null;
        var percent=TenshoAssistant.Localization.TenshoCulture.Percent(lowest);
        return (format("ServerBarInfo.Wear",[percent]),format("ServerBarInfo.WearTip",[percent,state.ToRepair,state.ToExtract]));
    }

    private (string,string)? PlaceLine()
    {
        var territory=(uint)client.TerritoryType;
        if(territory==0 || territoryName(territory) is not { Length:>0 } name)return null;
        var instance=Instance(territory);
        var label=instance is { } number ? name+" "+(char)((int)SeIconChar.Instance1+number-1):name;
        return (label,instance is { } shownNumber ? format("ServerBarInfo.PlaceInstanceTip",[name,shownNumber]):format("ServerBarInfo.PlaceTip",[name]));
    }

    private unsafe int? Instance(uint territory)
    {
        if(instanceFailed)return null;
        try
        {
            var state=FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance();
            if(state==null)return null;
            return InfoBarRules.Instance(state->PublicInstance.TerritoryTypeId,state->PublicInstance.InstanceId,territory);
        }
        catch(Exception){instanceFailed=true;return null;}
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        CompetitiveGate.RemoveAlways(framework,OnUpdate);
        bags.Remove();wear.Remove();place.Remove();
    }
}
