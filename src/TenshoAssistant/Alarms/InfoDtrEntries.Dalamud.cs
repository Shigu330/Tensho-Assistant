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
/// </summary>
public sealed class InfoDtrEntries : IDisposable
{
    private static readonly GameInventoryType[] Bags=[GameInventoryType.Inventory1,GameInventoryType.Inventory2,GameInventoryType.Inventory3,GameInventoryType.Inventory4];
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IGameInventory inventory;
    private readonly Configuration configuration;
    private readonly Func<IReadOnlyList<EquippedPiece>> equipped;
    private readonly Func<uint,string?> territoryName;
    private readonly Func<string,string> text;
    private readonly Func<string,object[],string> format;
    private readonly IDtrBarEntry bags,wear,place;
    private readonly string?[] shown=new string?[3];
    private DateTimeOffset nextRefresh;
    private bool instanceFailed,disposed;

    public InfoDtrEntries(IDtrBar bar,IFramework framework,IClientState client,IGameInventory inventory,Configuration configuration,
        Func<IReadOnlyList<EquippedPiece>> equipped,Func<uint,string?> territoryName,Func<string,string> text,Func<string,object[],string> format,
        Action openStorage,Action openGear,Action openMap)
    {
        this.framework=framework;this.client=client;this.inventory=inventory;this.configuration=configuration;this.equipped=equipped;
        this.territoryName=territoryName;this.text=text;this.format=format;
        bags=bar.Get("Tensho Assistant (Bags)");wear=bar.Get("Tensho Assistant (Gear condition)");place=bar.Get("Tensho Assistant (Place)");
        foreach(var entry in new[]{bags,wear,place})entry.Shown=false;
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
        if(line is not { } shownLine){entry.Shown=false;shown[index]=null;return;}
        entry.Shown=true;
        if(shown[index]==shownLine.Label+"\n"+shownLine.Tip)return;
        shown[index]=shownLine.Label+"\n"+shownLine.Tip;
        entry.Text=new SeStringBuilder().AddText(shownLine.Label).Build();
        entry.Tooltip=new SeStringBuilder().AddText(shownLine.Tip).Build();
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
