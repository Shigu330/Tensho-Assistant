using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using TenshoAssistant.Reminders;

namespace TenshoAssistant.Items;

/// <summary>
/// 0.469 (user request, in-game campaign): today's Grand Company request, read from the game's own supply missions window (ContentsInfoDetail, opened from its Timers window) once a second while
/// it is shown, on the framework thread. The window's layout has not been probed yet, so it is not assumed: its texts are compared with the exact names of the catalogue's delivery items
/// (GCSupplyDuty). What it held is written to the log once for each change, to document the layout. Read only: nothing is clicked or sent.
/// </summary>
internal sealed class GcSupplyReader : IDisposable
{
    private const string Window="ContentsInfoDetail";
    private const int MaxTexts=1500,MaxDepth=6,MaxLoggedTexts=80,MaxTextLength=512;
    private readonly IFramework framework;private readonly IGameGui gui;private readonly IClientState client;private readonly IPlayerState player;
    private readonly IDataManager data;private readonly IPluginLog log;private readonly Func<ItemUseCatalog?> catalog;private readonly GcSupplyToday today;private readonly Action<GcSupplySeen> learned;
    private ItemUseCatalog? indexedFor;private IReadOnlyDictionary<string,uint>? names;
    private long nextRead;private bool warned,disposed;private int loggedHash;

    public GcSupplyReader(IFramework framework,IGameGui gui,IClientState client,IPlayerState player,IDataManager data,IPluginLog log,Func<ItemUseCatalog?> catalog,GcSupplyToday today,Action<GcSupplySeen> learned)
    {
        this.framework=framework;this.gui=gui;this.client=client;this.player=player;this.data=data;this.log=log;this.catalog=catalog;this.today=today;this.learned=learned;
        framework.Update+=Update;
    }

    private void Update(IFramework _)
    {
        if(disposed)return;
        var now=Stopwatch.GetTimestamp();
        if(now<nextRead)return;
        nextRead=now+Stopwatch.Frequency;
        try{Read();}
        catch(Exception error)
        {
            if(warned)return;
            warned=true;log.Warning(error,"Today's Grand Company request could not be read from the game's window.");
        }
    }

    private unsafe void Read()
    {
        if(!client.IsLoggedIn || !player.IsLoaded || player.ContentId==0)return;
        var handle=gui.GetAddonByName(Window);
        if(handle.IsNull || !handle.IsVisible || Index() is not { } index)return;
        var addon=(AtkUnitBase*)handle.Address;
        var texts=new List<string>();
        if(addon->AtkValues!=null)
            for(var position=0;position<addon->AtkValuesCount && texts.Count<MaxTexts;position++)
            {
                var value=addon->AtkValues[position];
                if(value.Type is AtkValueType.String or AtkValueType.ManagedString or AtkValueType.ConstString && value.GetValueAsString() is {Length:>0 and <=MaxTextLength} text)texts.Add(text);
            }
        Walk(&addon->UldManager,0,texts);
        var found=GcSupplyToday.Match(texts,index);
        var distinct=texts.Select(GcSupplyToday.Normalize).Where(text=>text.Length>0).Distinct().ToArray();
        var hash=string.Join('\n',distinct).GetHashCode(StringComparison.Ordinal);
        if(hash!=loggedHash)
        {
            loggedHash=hash;
            log.Information("Grand Company supply window: {Values} values, {Texts} texts, delivery items {Found}; texts: {Sample}",addon->AtkValuesCount,distinct.Length,found,string.Join(" | ",distinct.Take(MaxLoggedTexts)));
        }
        if(today.Observe(player.ContentId,ResetSchedule.PreviousDaily(DateTimeOffset.UtcNow,ResetSchedule.SupplyHour),found) && today.Seen is { } seen)learned(seen);
    }

    private static unsafe void Walk(AtkUldManager* manager,int depth,List<string> texts)
    {
        if(manager==null || depth>MaxDepth || manager->NodeList==null || manager->NodeListCount>manager->NodeListSize || manager->LoadedState!=AtkLoadState.Loaded)return;
        for(var index=0;index<manager->NodeListCount && texts.Count<MaxTexts;index++)
        {
            var node=manager->NodeList[index];
            if(node==null)continue;
            if(node->Type==NodeType.Text)
            {
                var text=node->GetAsAtkTextNode();
                if(text!=null && text->NodeText.Length is >0 and <=MaxTextLength)texts.Add(Dalamud.Game.Text.SeStringHandling.SeString.Parse(text->NodeText.AsSpan()).TextValue);
            }
            else if((ushort)node->Type>=1000)
            {
                var component=node->GetAsAtkComponentNode();
                if(component!=null && component->Component!=null)Walk(&component->Component->UldManager,depth+1,texts);
            }
        }
    }

    private IReadOnlyDictionary<string,uint>? Index()
    {
        if(catalog() is not { } current)return null;
        if(ReferenceEquals(indexedFor,current))return names;
        var sheet=data.GetExcelSheet<Item>();
        names=GcSupplyToday.NameIndex(current.SupplyItems.Select(item=>(item,sheet.GetRowOrDefault(item)?.Name.ExtractText())));
        indexedFor=current;
        return names;
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;framework.Update-=Update;
    }
}
