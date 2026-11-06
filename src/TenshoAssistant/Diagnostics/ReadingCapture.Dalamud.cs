using System.Text;
using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// 0.650 (phase 5, C33 and T5): the in-game readings of the global plan, written to the log. Temporary: each part goes once its point is coded.
/// Developer mode only, read on the framework thread, nothing changed in the game, and a limit on the lines of each part.
/// </summary>
public sealed unsafe class ReadingCapture : IDisposable
{
    private const long SlowPartEvery=TimeSpan.TicksPerMillisecond*500;
    private readonly IFramework framework;
    private readonly IObjectTable objects;
    private readonly ICondition condition;
    private readonly IGameGui gui;
    private readonly IChatGui chat;
    private readonly Func<bool> enabled;
    private readonly IPluginLog log;
    private readonly Dictionary<string,int> lines=[];
    private string statuses="",synthesis="",leves="",retainer="",airship="";
    private bool fishing,disposed,warned,away;
    private uint lastGp,lastMaxGp;
    private long slowAt;

    public ReadingCapture(IFramework framework,IObjectTable objects,ICondition condition,IGameGui gui,IChatGui chat,Func<bool> enabled,IPluginLog log)
    {
        this.framework=framework;this.objects=objects;this.condition=condition;this.gui=gui;this.chat=chat;this.enabled=enabled;this.log=log;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
        chat.ChatMessageUnhandled+=OnMessage;
    }

    /// <summary>Writes a line, up to the limit of its part.</summary>
    private void Line(string part,int limit,string text)
    {
        var count=lines.GetValueOrDefault(part);
        if(count>=limit)return;
        lines[part]=count+1;
        log.Information("Reading {0}: {1}",part,text);
    }

    private void OnUpdate(IFramework unused)
    {
        if(disposed || !enabled())return;
        try
        {
            var local=objects.LocalPlayer;
            if(local is null){if(!away){away=true;Line("gp",150,"no player");}return;}
            if(away){away=false;Line("gp",150,$"player back: {local.CurrentGp} / {local.MaxGp}");}
            var job=local.ClassJob.RowId;
            if(job is 16 or 17 or 18)
            {
                ReadStatuses(local);
                ReadGp(local.CurrentGp,local.MaxGp);
            }
            var flag=condition[ConditionFlag.Fishing];
            if(flag!=fishing){fishing=flag;Line("fishing",200,$"fishing flag {flag}");}
            var now=DateTime.UtcNow.Ticks;
            if(now-slowAt<SlowPartEvery)return;
            slowAt=now;
            ReadSynthesis();
            ReadLeves();
            ReadRetainer();
            ReadAirships();
            warned=false;
        }
        catch(Exception error)
        {
            if(!warned){warned=true;log.Warning(error,"A reading could not be made.");}
        }
    }

    // C28, G2: the statuses of a gatherer, 568 Fisher's Intuition and 2891 Spearfisher's Intuition among them.
    private void ReadStatuses(Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter local)
    {
        var ids=string.Join(",",local.StatusList.Where(status=>status.StatusId!=0).Select(status=>$"{status.StatusId}:{status.Param}").OrderBy(id=>id,StringComparer.Ordinal));
        if(ids==statuses)return;
        statuses=ids;
        Line("statuses",200,string.Join(", ",local.StatusList.Where(status=>status.StatusId!=0).Select(status=>$"{status.StatusId} (param {status.Param}, {status.RemainingTime:F1} s)")));
    }

    // 0.656: GP around a change of zone, without the ordinary ticks.
    private void ReadGp(uint gp,uint max)
    {
        if(gp==lastGp && max==lastMaxGp)return;
        var tick=max==lastMaxGp && gp>lastGp && gp-lastGp<=20;
        if(!tick)Line("gp",150,$"{lastGp} / {lastMaxGp} -> {gp} / {max}, between areas {condition[ConditionFlag.BetweenAreas]}");
        lastGp=gp;lastMaxGp=max;
    }

    // 0.654: what the game says of the accepted leves, to tell a started, a finished and an abandoned one.
    private void ReadLeves()
    {
        var quests=QuestManager.Instance();
        if(quests==null)return;
        var text=new StringBuilder();
        foreach(ref var leve in quests->LeveQuests)if(leve.LeveId!=0)text.Append($" {leve.LeveId} (sequence {leve.Sequence}, flags {leve.Flags}, hidden {leve.IsHidden}, priority {leve.IsPriority})");
        var all=$"{quests->NumAcceptedLeveQuests} accepted:{text}";
        if(all==leves)return;
        leves=all;
        Line("leves",60,all);
    }

    // C13: the HQ chance beside the quality, and every number of the window.
    private void ReadSynthesis()
    {
        var handle=gui.GetAddonByName("Synthesis");
        if(handle.IsNull || !handle.IsVisible){synthesis="";return;}
        var addon=(AddonSynthesis*)handle.Address;
        var text=$"quality {Text(addon->CurrentQuality)} / {Text(addon->MaxQuality)}, HQ {Text(addon->HQPercentage)}";
        if(text==synthesis)return;
        synthesis=text;
        var values=new StringBuilder();
        for(var index=0;index<Math.Min((int)addon->AtkValuesCount,64) && addon->AtkValues!=null;index++)
        {
            var value=addon->AtkValues[index];
            if(value.Type==AtkValueType.UInt)values.Append($" {index}={value.UInt}");
            else if(value.Type==AtkValueType.Int)values.Append($" {index}={value.Int}");
            else if(value.Type==AtkValueType.Bool)values.Append($" {index}={value.Bool}");
        }
        Line("synthesis",150,$"{text}; values:{values}");
    }

    // I10: are the crystals and the gil of a retainer loaded while its inventory is open?
    private void ReadRetainer()
    {
        var open=gui.GetAddonByName("InventoryRetainer");
        var large=gui.GetAddonByName("InventoryRetainerLarge");
        if((open.IsNull || !open.IsVisible) && (large.IsNull || !large.IsVisible)){retainer="";return;}
        var text=$"gil {Container(InventoryType.RetainerGil)}; crystals {Container(InventoryType.RetainerCrystals)}";
        if(text==retainer)return;
        retainer=text;
        Line("retainer",30,text);
    }
    private static string Container(InventoryType type)
    {
        var manager=InventoryManager.Instance();
        var container=manager==null ? null:manager->GetInventoryContainer(type);
        if(container==null)return "missing";
        var used=0;var total=0L;
        if(container->IsLoaded && container->Size is >0 and <=64)
            for(var index=0;index<container->Size;index++)
            {
                var slot=container->GetInventorySlot(index);
                if(slot==null || slot->GetBaseItemId()==0)continue;
                used++;total+=slot->Quantity;
            }
        return $"loaded {container->IsLoaded}, size {container->Size}, used {used}, total {total}";
    }

    // T26: what the game keeps of an airship's voyage, in its logs and in the unnamed fields of its finds.
    private void ReadAirships()
    {
        var housing=HousingManager.Instance();
        if(housing==null || housing->WorkshopTerritory==null){airship="";return;}
        var data=&housing->WorkshopTerritory->Airship;
        var count=Math.Min((int)data->AirshipCount,data->Data.Length);
        var text=new StringBuilder($"{count} airships");
        for(var index=0;index<count;index++)
        {
            var found=data->Data[index].GatheredData;
            text.Append($"; airship {index + 1} return {data->Data[index].ReturnTime}, finds");
            for(var entry=0;entry<found.Length;entry++)
            {
                var item=found[entry];
                var raw=(uint*)&item;
                text.Append($" [{item.ItemIdPrimary}:{raw[6]},{raw[8]},{raw[10]} {item.ItemIdAdditional}:{raw[7]},{raw[9]},{raw[11]}]");
            }
        }
        var logs=new StringBuilder();var filled=0;
        for(var index=0;index<data->AirshipLogs.Length;index++)
        {
            fixed(FFXIVClientStructs.FFXIV.Client.System.String.Utf8String* entry=&data->AirshipLogs[index])
            {
                var line=TenshoAssistant.Maps.MapLiveReader.TooltipText(entry);
                if(line.Length==0)continue;
                if(++filled<=25)logs.Append($" #{index}={line}");
            }
        }
        text.Append($"; {filled} log lines:{logs}");
        var all=text.ToString();
        if(all==airship)return;
        airship=all;
        Line("airship",12,all);
    }

    // C30: which lines of the game say a catch.
    private void OnMessage(IChatMessage message)
    {
        if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;
        if(disposed || !enabled())return;
        try
        {
            if(objects.LocalPlayer?.ClassJob.RowId!=18 || message.Sender.TextValue.Trim().Length!=0)return;
            Line("fishing",200,$"chat [{(int)message.LogKind}] {message.Message.TextValue}");
        }
        catch(Exception error)
        {
            if(!warned){warned=true;log.Warning(error,"A reading could not be made.");}
        }
    }

    private static string Text(AtkTextNode* node)
    {
        if(node==null || node->NodeText.Length>256)return "";
        return SeString.Parse(node->NodeText.AsSpan()).TextValue;
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
        chat.ChatMessageUnhandled-=OnMessage;
    }
}
