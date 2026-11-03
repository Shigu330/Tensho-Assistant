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
    public GardenChatWatcher(IChatGui chat,IFramework framework,IDataManager data,IClientState client,IPluginLog log,Func<bool> enabled,Func<uint,bool> isSeed,Action<GardenChatEvent,uint?,string?> planted)
    {
        this.chat=chat;this.framework=framework;this.data=data;this.client=client;this.log=log;this.enabled=enabled;this.isSeed=isSeed;this.planted=planted;
        chat.ChatMessageUnhandled+=OnMessage;
        framework.Update+=OnUpdate;
    }

    private GardenChat? Rules()
    {
        if(rules is not null || rulesFailed)return rules;
        try
        {
            var sheet=data.GetExcelSheet<Lumina.Excel.Sheets.LogMessage>();
            rules=new(Parts(sheet.GetRowOrDefault(PlantedBedRow)?.Text),Parts(sheet.GetRowOrDefault(PlantedPotRow)?.Text),GardenChat.BedComesFirst(client.ClientLanguage.ToString()));
        }
        catch(Exception error){rulesFailed=true;log.Warning(error,"Garden detection unavailable: its game messages could not be read (perhaps after a game update).");}
        return rules;
    }

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
        if(disposed || now<nextPoll || !framework.IsInFrameworkUpdateThread)return;
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
        if(disposed || !enabled())return;
        try
        {
            var sender=message.Sender.TextValue.Trim();var text=message.Message.TextValue;
            if(sender.Length==0 && InHousing())log.Information("Garden chat capture: [{0}] {1}",(int)message.LogKind,text);
            if(Rules()?.Read(sender,text) is not { } found)return;
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
        framework.Update-=OnUpdate;
    }
}
