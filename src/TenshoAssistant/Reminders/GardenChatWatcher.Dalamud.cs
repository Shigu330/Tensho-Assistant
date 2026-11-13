using System.Text;
using Dalamud.Game.Chat;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Text.ReadOnly;

namespace TenshoAssistant.Reminders;

/// <summary>
/// 0.460 (user request, in-game campaign): detects a planting from the chat. It reads the game's chat lines (IChatGui.ChatMessageUnhandled, never changing them) and
/// the seed chosen in the planting window (AgentHousingPlant.SelectedItems, read only, on the framework thread while it is open), then tells the journal. While
/// inside a house or a plot, lines without a sender also go to the log, to find the lines of tending and harvesting. Nothing is sent or done in the game.
/// </summary>
internal sealed class GardenChatWatcher : IDisposable
{
    private const uint PlantedBedRow=4015,PlantedPotRow=3448;
    // 0.616: measured in game and in the sheet: tending and uprooting, bed then flowerpot.
    private static readonly uint[] TendedRows=[4017,3450],UprootedRows=[4025,3452];
    private readonly IChatGui chat;
    private readonly IFramework framework;
    private readonly IDataManager data;
    private readonly IClientState client;
    private readonly IPluginLog log;
    private readonly Func<bool> enabled;
    private readonly Func<uint,bool> isSeed;
    private readonly Action<GardenChatEvent,uint?,string?> planted;
    private GardenChat? rules;
    private bool rulesFailed,warned,disposed;
    private uint chosenSeed;
    private DateTimeOffset chosenAt,nextPoll;

    /// <param name="planted">Event, seed item when known, and the place (« zone ward-plot ») when readable.</param>
    // #36 (0.978): the target's object id (0: none), to see a harvest.
    private readonly Func<uint>? target;
    private DateTimeOffset lastHarvest;
    private (int Patch,int Bed)? lastHarvestSlot;
    // X-179 (0.1165): the game's menu on a bed (its title names the patch and the bed), read while it is open, for the care line that follows.
    private static readonly string[] MenuWindows=["SelectString","SelectIconString"];
    private readonly IGameGui? gui;
    private readonly Func<bool>? developer;
    private readonly GardenMenuMemory menuMemory=new();
    private GardenMenuTitle? menuTitle;
    private bool menuTitleFailed;
    private string? menuSeen;
    public GardenChatWatcher(IChatGui chat,IFramework framework,IDataManager data,IClientState client,IPluginLog log,Func<bool> enabled,Func<uint,bool> isSeed,Action<GardenChatEvent,uint?,string?> planted,Func<uint>? target=null,IGameGui? gui=null,Func<bool>? developer=null)
    {this.target=target;this.gui=gui;this.developer=developer;
        this.chat=chat;this.framework=framework;this.data=data;this.client=client;this.log=log;this.enabled=enabled;this.isSeed=isSeed;this.planted=planted;
        chat.ChatMessageUnhandled+=OnMessage;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private GardenChat? Rules()
    {
        if(rules is not null || rulesFailed)return rules;
        try
        {
            var sheet=data.GetExcelSheet<Lumina.Excel.Sheets.LogMessage>();
            rules=new(Parts(sheet.GetRowOrDefault(PlantedBedRow)?.Text),Parts(sheet.GetRowOrDefault(PlantedPotRow)?.Text),GardenChat.BedComesFirst(client.ClientLanguage.ToString()),Texts(sheet,TendedRows),Texts(sheet,UprootedRows));
        }
        catch(Exception error){rulesFailed=true;log.Warning(error,"Garden detection unavailable: its game messages could not be read (perhaps after a game update).");}
        return rules;
    }

    private static IEnumerable<string?> Texts(Lumina.Excel.ExcelSheet<Lumina.Excel.Sheets.LogMessage> sheet,uint[] rows)
        =>rows.Select(row=>Parts(sheet.GetRowOrDefault(row)?.Text) is { } parts ? string.Concat(parts):null).ToArray();

    private static IReadOnlyList<string?>? Parts(ReadOnlySeString? text)
    {
        if(text is not { } line)return null;
        var parts=new List<string?>();
        foreach(var payload in line)
            parts.Add(payload.Type==ReadOnlySePayloadType.Text ? Encoding.UTF8.GetString(payload.Body.Span):null);
        return parts;
    }

    // The seed chosen in the planting window: the first of its two items that is a known seed (the other is the soil).
    private unsafe void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || !framework.IsInFrameworkUpdateThread)return;
        ReadMenu(now);
        if(now<nextPoll)return;
        nextPoll=now.AddMilliseconds(500);
        if(!enabled() || !client.IsLoggedIn)return;
        try
        {
            var agent=AgentHousingPlant.Instance();
            if(agent==null || !agent->AgentInterface.IsAgentActive())return;
            foreach(var item in agent->SelectedItems)
                if(item.ItemId!=0 && isSeed(item.ItemId)){chosenSeed=item.ItemId;chosenAt=now;break;}
            warned=false;
        }
        catch(Exception error)
        {
            nextPoll=now.AddSeconds(60);
            if(!warned){warned=true;log.Warning(error,"The planting window could not be read (perhaps after a game update); the seed stays unknown.");}
        }
    }

    /// <summary>
    /// X-179 (0.1165): each frame in housing, the game's choice window when it is open: a title that reads as « patch N, bed M » (Addon 6420 in the
    /// client's language) is remembered with the place. Texts are only compared, never kept; in developer mode the window's name is written once per opening.
    /// </summary>
    private unsafe void ReadMenu(DateTimeOffset now)
    {
        if(gui is null || menuTitleFailed || !enabled() || !client.IsLoggedIn || !InHousing())return;
        try
        {
            foreach(var name in MenuWindows)
            {
                var addon=gui.GetAddonByName(name);
                if(addon.IsNull || !addon.IsVisible)continue;
                if(menuTitle is null)
                {
                    var macro=data.GetExcelSheet<Lumina.Excel.Sheets.Addon>().GetRowOrDefault(GardenMenuTitle.AddonRow)?.Text.ToMacroString();
                    menuTitle=GardenMenuTitle.From(macro);
                    if(menuTitle is null){menuTitleFailed=true;log.Warning("Garden: the bed menu's title could not be understood (Addon {0}; perhaps after a game update): care lines keep their old rule.",GardenMenuTitle.AddonRow);return;}
                }
                var unit=(FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase*)addon.Address;
                var texts=new List<string>();
                TenshoAssistant.Beastmaster.BeastWindowProbe.Texts(&unit->UldManager,0,texts);
                foreach(var text in texts)
                {
                    if(menuTitle.Read(text) is not { } slot)continue;
                    var place=Place();
                    menuMemory.Seen(slot.Patch,slot.Bed,place,now);
                    var seen=$"{name}:{place}:{slot.Patch}:{slot.Bed}";
                    if(seen!=menuSeen){menuSeen=seen;if(developer?.Invoke()==true)log.Information("Garden menu read: window {0}, patch {1}, bed {2}.",name,slot.Patch,slot.Bed);}
                    return;
                }
            }
            menuSeen=null;
        }
        catch(Exception error)
        {
            menuTitleFailed=true;
            log.Warning(error,"Garden: the bed menu could not be read (perhaps after a game update): care lines keep their old rule.");
        }
    }

    private unsafe string? Place()
    {
        var manager=HousingManager.Instance();
        if(manager==null || (nint)HousingManager.MemberFunctionPointers.GetCurrentWard==0 || (nint)HousingManager.MemberFunctionPointers.GetCurrentPlot==0)return null;
        var ward=manager->GetCurrentWard();var plot=manager->GetCurrentPlot();
        var zone=data.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>().GetRowOrDefault(client.TerritoryType)?.PlaceName.ValueNullable?.Name.ExtractText() ?? "";
        return ward>=0 && plot>=0 ? $"{zone} {ward+1}-{plot+1}".Trim():zone.Length>0 ? zone:null;
    }

    private unsafe bool InHousing()
    {
        var manager=HousingManager.Instance();
        return manager!=null && (manager->OutdoorTerritory!=null || manager->IndoorTerritory!=null);
    }

    private void OnMessage(IChatMessage message)
    {
        if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;
        if(disposed || !enabled())return;
        try
        {
            var sender=message.Sender.TextValue.Trim();var text=message.Message.TextValue;
            // #36 (0.1110): the capture of every housing chat line is gone, tending and harvesting being recognized (0.616, 0.978).
            // 0.997 (review): one harvest can print several gain lines (crop, seeds): the first one counts, the others within 5 s are the same harvest.
            // X-185 (0.1211, N23): the bed read in the menu is taken first: another bed than the last one harvested is a new harvest, even within 5 s.
            if(target is not null && InHousing() && GardenChat.IsHarvest((int)message.LogKind,sender,target()))
            {
                var at=DateTimeOffset.UtcNow;
                var place=Place();
                var slot=menuMemory.Take(place,at);
                if(GardenHarvests.IsNew(at,lastHarvest,slot,lastHarvestSlot))
                {
                    lastHarvest=at;lastHarvestSlot=slot;
                    planted(new(GardenChatKind.Harvested,slot?.Patch,slot?.Bed),null,place);
                }
                return;
            }
            if(Rules()?.Read(sender,text) is not { } found)return;
            if(found.Kind is GardenChatKind.Tended or GardenChatKind.Uprooted)
            {
                var place=Place();
                // X-179 (0.1165): the bed of the menu just before, when it was read at this place.
                if(menuMemory.Take(place,DateTimeOffset.UtcNow) is { } slot)found=found with{Patch=slot.Patch,Bed=slot.Bed};
                planted(found,null,place);
                return;
            }
            var seed=chosenSeed!=0 && DateTimeOffset.UtcNow-chosenAt<TimeSpan.FromSeconds(60) ? chosenSeed:(uint?)null;
            chosenSeed=0;
            planted(found,seed,Place());
        }
        catch(Exception error)
        {
            if(!warned){warned=true;log.Warning(error,"A garden line in the chat could not be read.");}
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        chat.ChatMessageUnhandled-=OnMessage;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
