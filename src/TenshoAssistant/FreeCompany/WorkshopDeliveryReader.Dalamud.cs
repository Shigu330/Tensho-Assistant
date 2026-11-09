using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using TenshoAssistant.UI;

namespace TenshoAssistant.FreeCompany;

/// <summary>
/// 0.787 (X-28, user request): what the workshop already received, read from its delivery window (SubmarinePartsMenu) once a second while it is shown,
/// on the framework thread. Read only: nothing is clicked or sent. Layout: docs/evidence/0.787-workshop-delivered.md.
/// </summary>
internal sealed class WorkshopDeliveryReader : IDisposable
{
    private const string Window="SubmarinePartsMenu";
    private const int Read132=132;
    private readonly IFramework framework;private readonly IGameGui gui;private readonly IPluginLog log;private readonly Action<uint,WorkshopDelivery> seen;
    private long nextRead;private bool warned,disposed;

    public WorkshopDeliveryReader(IFramework framework,IGameGui gui,IPluginLog log,Action<uint,WorkshopDelivery> seen)
    {
        this.framework=framework;this.gui=gui;this.log=log;this.seen=seen;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,Update);
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
            warned=true;log.Warning(error,"The workshop's delivery window could not be read.");
        }
    }

    private unsafe void Read()
    {
        var handle=gui.GetAddonByName(Window);
        if(handle.IsNull || !handle.IsVisible)return;
        var addon=(AtkUnitBase*)handle.Address;
        if(addon->AtkValues==null || addon->AtkValuesCount<=Read132)return;
        var numbers=new int[Read132];
        for(var index=0;index<numbers.Length;index++)numbers[index]=AtkValueRead.Number(addon->AtkValues[index]);
        var loaded=addon->AtkValues[10] is { Type:AtkValueType.Bool } flag && flag.Bool;
        if(WorkshopDelivery.FromWindow(numbers,loaded,DateTimeOffset.UtcNow) is { } read)seen(read.Result,read.Delivery);
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,Update);
    }
}
