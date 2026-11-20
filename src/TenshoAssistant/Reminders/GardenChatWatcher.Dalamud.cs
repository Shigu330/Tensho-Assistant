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
    // X-410 M1 (0.1378): a seed or a crop of the embedded garden data (null: not given, the menu rule alone).
    private readonly Func<uint,bool>? isGardenItem;
    private readonly Action<GardenChatEvent,uint?,string?> planted;
    // X-453 M4 (0.1457): the rules, built once in the background (BackgroundBuild: a line that comes first waits, a failure said once,
    // stopped by Dispose).
    private readonly TenshoAssistant.Caching.BackgroundBuild<GardenChat> rulesBuild;
    private bool warned,disposed;
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
    // X-581 (0.1572): the game's state line of a bed, kept for the menu that opens just after it, and who is told (place, patch, bed, state).
    private readonly GardenTalkMemory talkMemory=new();
    private readonly Action<string?,int,int,GardenGameState>? examined;
    public GardenChatWatcher(IChatGui chat,IFramework framework,IDataManager data,IClientState client,IPluginLog log,Func<bool> enabled,Func<uint,bool> isSeed,Action<GardenChatEvent,uint?,string?> planted,Func<uint>? target=null,IGameGui? gui=null,Func<bool>? developer=null,Func<uint,bool>? isGardenItem=null,Action<string?,int,int,GardenGameState>? examined=null)
    {this.target=target;this.gui=gui;this.developer=developer;this.isGardenItem=isGardenItem;this.examined=examined;
        this.chat=chat;this.framework=framework;this.data=data;this.client=client;this.log=log;this.enabled=enabled;this.isSeed=isSeed;this.planted=planted;
        rulesBuild=new(BuildRules,error=>log.Warning(error,"Garden detection unavailable: its game messages could not be read (perhaps after a game update)."));
        chat.ChatMessageUnhandled+=OnMessage;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
        // X-433 (0.1452): the rules' regular expressions built off the game's thread (their automaton tables: 3.8 MB in a frame at the first
        // chat line after a load, 0.1451's measure); with the garden off they are built at the first line, as before.
        if(enabled())rulesBuild.Start();
    }

    private GardenChat BuildRules()
    {
        var sheet=data.GetExcelSheet<Lumina.Excel.Sheets.LogMessage>();
        return new(Parts(sheet.GetRowOrDefault(PlantedBedRow)?.Text),Parts(sheet.GetRowOrDefault(PlantedPotRow)?.Text),GardenChat.BedComesFirst(client.ClientLanguage.ToString()),Texts(sheet,TendedRows),Texts(sheet,UprootedRows))
            {Talk=TalkRules()};
    }

    /// <summary>X-581 (0.1572): the bed's state lines from the game's event sheet (GardenTalk.Sheet); null when they cannot be read (said once):
    /// « Ripe » then stays the planting time plus the growing time.</summary>
    private GardenTalk? TalkRules()
    {
        try
        {
            var rows=new List<(string,IReadOnlyList<string?>?)>();
            foreach(var row in data.Excel.GetSheet<Lumina.Excel.RawRow>(null,GardenTalk.Sheet))
                if(row.ReadColumn(0) is ReadOnlySeString key && row.ReadColumn(1) is ReadOnlySeString text)rows.Add((key.ExtractText(),Parts(text)));
            var talk=GardenTalk.From(rows);
            if(talk is null)log.Warning("Garden: the bed state lines of {0} could not be understood (perhaps after a game update): « Ripe » stays the expected time.",GardenTalk.Sheet);
            return talk;
        }
        catch(Exception error)
        {
            log.Warning(error,"Garden: the bed state lines of {0} could not be read (perhaps after a game update): « Ripe » stays the expected time.",GardenTalk.Sheet);
            return null;
        }
    }

    // X-433 (0.1452): the rules built in the background, waited for only if a line comes before they are done (no line is lost).
    private GardenChat? Rules()=>rulesBuild.Get();

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
                    if(seen!=menuSeen)
                    {
                        menuSeen=seen;
                        if(developer?.Invoke()==true)log.Information("Garden menu read: window {0}, patch {1}, bed {2}.",name,slot.Patch,slot.Bed);
                        // X-581 (0.1572): the game's state line written just before this menu opened is this bed's.
                        if(talkMemory.Opened(place,now) is var said && said!=GardenGameState.None)
                        {
                            if(developer?.Invoke()==true)log.Information("Garden bed state read: patch {0}, bed {1}, {2}.",slot.Patch,slot.Bed,said);
                            examined?.Invoke(place,slot.Patch,slot.Bed,said);
                        }
                    }
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
            // X-382 (0.1360): or the bed's menu read here just before, the target being no longer the bed when the gain line comes.
            if(target is not null && (int)message.LogKind==62 && sender.Length==0 && InHousing())
            {
                var at=DateTimeOffset.UtcNow;
                var place=Place();var aimed=target();
                var gardenItem=isGardenItem is null ? (bool?)null:GardenChat.GardenItem(message.Message.Payloads.OfType<Dalamud.Game.Text.SeStringHandling.Payloads.ItemPayload>()
                    .Select(payload=>TenshoAssistant.Discoveries.ItemGains.BaseItem(payload.ItemId)).ToArray(),isGardenItem);
                if(GardenChat.IsHarvest((int)message.LogKind,sender,aimed,menuMemory.Peek(place,at,GardenMenuMemory.HarvestWindow) is not null,gardenItem))
                {
                    var slot=menuMemory.Take(place,at);
                    if(GardenHarvests.IsNew(at,lastHarvest,slot,lastHarvestSlot))
                    {
                        lastHarvest=at;lastHarvestSlot=slot;
                        if(developer?.Invoke()==true)log.Information("Garden harvest read: patch {0}, bed {1}, target {2}.",slot?.Patch ?? 0,slot?.Bed ?? 0,aimed);
                        planted(new(GardenChatKind.Harvested,slot?.Patch,slot?.Bed),null,place);
                    }
                    return;
                }
            }
            // X-581 (0.1572): the state line the game writes when a bed is opened: kept for the bed's menu that opens just after it.
            if(sender.Length==0 && InHousing() && Rules()?.Talk?.Read(sender,text) is { } said && said!=GardenGameState.None)
            {
                talkMemory.Said(said,Place(),DateTimeOffset.UtcNow);
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
            // X-395 M3 (0.1366): the menu noted for this planting is not a harvest of the next gain line.
            menuMemory.Forget();
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
        // X-453 M4 (0.1457): a build still running is waited for (2 s at most) and its result dropped. X-464 M1 (0.1466): one still running
        // after the wait ends by itself (it may read the game's data until then): said in the log.
        var ended=rulesBuild.Stop(TimeSpan.FromSeconds(2));
        if(!ended)log.Information("Unload: the garden rules' build was still running after 2 s; it ends by itself and its result is dropped.");
    }
}
