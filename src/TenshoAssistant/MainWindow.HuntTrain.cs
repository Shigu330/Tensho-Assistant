using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

/// <summary>
/// N-15 (0.442): Maps › « Hunt train » (folded): while recording, the A and S marks (and B when asked) the map's live reading meets are
/// noted once per map with their position; « Next flag » puts the game's flag on the first stop not visited and marks it; the list
/// copies as « Name — Zone ( x , y ) » lines (the bookmark import reads them back). Kept for the session.
/// </summary>
internal sealed partial class MainWindow
{
    private IReadOnlyList<HuntTrainStop> huntTrain=[];
    private bool huntTrainRecording,huntTrainRankB;
    private DateTimeOffset huntTrainObservedAt;

    // 0.446 (N-09): marks already announced (map and name), so each A or S mark is said once per session (forgotten past 200).
    private readonly HashSet<string> huntMarksAnnounced=[];
    internal Action<string>? ChatNotice { get; set; }
    // X-13 (0.447): the stop whose flag was placed last from the train (the one the call names), the sender, and the last send (5 s apart).
    private int? huntTrainCalled;
    internal Action<string>? SendHuntCall { get; set; }
    private DateTimeOffset huntCallSentAt;
    // The channels of the call combo, built once (each channel is its own text).
    private static readonly (string Value,string Label)[] HuntChannelOptions=HuntTrain.AnnounceChannels.Select(channel=>(channel,channel)).ToArray();

    /// <summary>Called every frame: notes the marks met (once a second, while recording) and announces the A and S marks (setting).</summary>
    internal void ObserveHuntTrain()
    {
        if(!huntTrainRecording && !configuration.HuntMarkAlert)return;
        var now=DateTimeOffset.UtcNow;
        if(now<huntTrainObservedAt)return;
        huntTrainObservedAt=now.AddSeconds(1);
        if(mapLive?.State is not { Objects:{ } nearby } live || maps.State.Catalog?.Find(live.MapId) is not { } map)return;
        if(configuration.HuntMarkAlert)
            foreach(var item in nearby.Where(item=>item.Kind==MapObjectKind.HuntMark && item.Level>=2))
            {
                if(!huntMarksAnnounced.Add($"{map.MapId}/{item.Name}"))continue;
                var coordinate=MapProjection.CoordinateFromWorld(item.Position,map);
                ChatNotice?.Invoke(localization.Format("HuntTrain.MarkAlert",item.Name,item.Level==3 ? "S":"A",map.Zone,
                    MapProjection.CoordinateNumber(coordinate.X),MapProjection.CoordinateNumber(coordinate.Y)));
                if(configuration.AlarmOptions.PlaySound)Alarms.AlarmService.PlaySound(configuration.AlarmOptions.Sound,log);
            }
        if(huntMarksAnnounced.Count>200)huntMarksAnnounced.Clear();
        if(!huntTrainRecording)return;
        foreach(var item in nearby.Where(item=>item.Kind==MapObjectKind.HuntMark && (item.Level>=2 || huntTrainRankB)))
        {
            var coordinate=MapProjection.CoordinateFromWorld(item.Position,map);
            huntTrain=HuntTrain.Add(huntTrain,new(item.Name,item.Level,map.MapId,map.TerritoryId,map.Zone,coordinate.X,coordinate.Y,now));
        }
    }

    private void DrawHuntTrain(MapCatalog catalog)
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("HuntTrain.Title",huntTrain.Count)+"###HuntTrain",ImGuiTreeNodeFlags.DefaultOpen))return;
        ImGui.TextWrapped(localization.Get("HuntTrain.Help"));
        ImGui.Checkbox(localization.Get("HuntTrain.Record")+"###HuntTrainRecord",ref huntTrainRecording);
        Tip("HuntTrain.RecordTip");
        ImGui.SameLine();
        ImGui.Checkbox(localization.Get("HuntTrain.RankB")+"###HuntTrainB",ref huntTrainRankB);
        Tip("HuntTrain.RankBTip");
        BoolSetting("HuntTrain.MarkAlertSetting","HuntTrain.MarkAlertSettingTip",()=>configuration.HuntMarkAlert,value=>configuration.HuntMarkAlert=value,c=>c.HuntMarkAlert);
        if(huntTrain.Count==0){ImGui.TextDisabled(localization.Get("HuntTrain.Empty"));return;}
        if(huntTrainCalled>=huntTrain.Count)huntTrainCalled=null;
        if(HuntTrain.Next(huntTrain) is { } next && ImGui.SmallButton(localization.Get("HuntTrain.NextFlag")) && catalog.Find(huntTrain[next].MapId) is { } nextMap)
        {
            OpenMapPointFromPage(nextMap,huntTrain[next].X,huntTrain[next].Y);
            huntTrain=HuntTrain.MarkVisited(huntTrain,next);
            huntTrainCalled=next;
        }
        // X-13 (0.446): the conductor's announcement: the game turns « <flag> » into the flag's link. 0.447: it names the stop whose flag was
        // placed last from the train, else the next stop not visited, else the last one.
        var called=huntTrain[huntTrainCalled ?? HuntTrain.Next(huntTrain) ?? huntTrain.Count-1];
        var announce=localization.Format("HuntTrain.AnnounceText",called.Name,called.Rank switch{3=>"S",2=>"A",_=>"B"},called.Zone);
        ImGui.SameLine();
        CopyButton(localization.Get("HuntTrain.Announce"),()=>announce+" <flag>");
        Tip("HuntTrain.AnnounceTip");
        // X-13 (0.447, decided 2026-11-03): sent to the chosen channel on click only, once the flag is on that stop, 5 s apart.
        ImGui.SameLine();
        // X-76 H5 (0.915): shared combo row without label (rollback when saving fails, « ↺ »).
        ComboSetting("HuntCallChannel","HuntTrain.ChannelTip",()=>configuration.HuntAnnounceChannel,value=>configuration.HuntAnnounceChannel=value,
            HuntChannelOptions,DefaultConfiguration.HuntAnnounceChannel,70,showLabel:false);
        ImGui.SameLine();
        var command=HuntTrain.ChatCommand(configuration.HuntAnnounceChannel,announce);
        var ready=huntTrainCalled is not null && command is not null && SendHuntCall is not null && DateTimeOffset.UtcNow-huntCallSentAt>=TimeSpan.FromSeconds(5);
        ImGui.BeginDisabled(!ready);
        if(ImGui.SmallButton(localization.Get("HuntTrain.Send")) && ready){SendHuntCall!(command!);huntCallSentAt=DateTimeOffset.UtcNow;}
        ImGui.EndDisabled();
        Tip(huntTrainCalled is null ? "HuntTrain.SendNeedsFlag":"HuntTrain.SendTip");
        ImGui.SameLine();
        CopyButton(localization.Get("HuntTrain.Copy"),()=>HuntTrain.Export(huntTrain));
        ImGui.SameLine();
        if(DeleteButton("HuntTrain.Clear")){huntTrain=[];huntTrainCalled=null;return;}
        Tip("HuntTrain.ClearTip");
        for(var index=0;index<huntTrain.Count;index++)
        {
            var stop=huntTrain[index];
            ImGui.PushID($"Train{index}");
            try
            {
                var rank=stop.Rank switch{3=>"S",2=>"A",_=>"B"};
                ImGui.TextColored(stop.Visited ? Muted:Accent,localization.Format("HuntTrain.Stop",index+1,stop.Name,rank,stop.Zone,MapProjection.CoordinateNumber(stop.X),
                    MapProjection.CoordinateNumber(stop.Y),stop.SeenAt.ToLocalTime().ToString("t")));
                if(catalog.Find(stop.MapId) is { } stopMap)PlaceMenu("TrainPlace",stopMap,new(stop.X,stop.Y),stop.Name);
                ImGui.SameLine();
                if(ImGui.SmallButton(localization.Get("HuntTrain.Flag")) && catalog.Find(stop.MapId) is { } map)
                {OpenMapPointFromPage(map,stop.X,stop.Y);huntTrain=HuntTrain.MarkVisited(huntTrain,index);huntTrainCalled=index;}
            }
            finally{ImGui.PopID();}
        }
    }
}
