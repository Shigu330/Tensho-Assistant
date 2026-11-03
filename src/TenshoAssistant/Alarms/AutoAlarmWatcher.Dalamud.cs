using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.Alarms;

/// <summary>
/// g, M-01 (0.371): every 30 seconds on the framework thread, while logged in and when « automatic alarms » is on (default): reads the
/// retainer ventures (RetainerManager, known once the retainer list was opened at a summoning bell) and the saved vessel returns
/// (Configuration.Voyages, kept by VoyageWatcher), then syncs the automatic alarms (AutoAlarms) and saves when they changed.
/// </summary>
public sealed unsafe class AutoAlarmWatcher : IDisposable
{
    private readonly IFramework framework;
    private readonly IClientState client;
    private readonly IPluginLog log;
    private readonly Configuration configuration;
    private readonly Action save;
    private readonly Func<string,string> text;
    private DateTimeOffset nextRead;
    private bool disposed,warned;

    public AutoAlarmWatcher(IFramework framework,IClientState client,IPluginLog log,Configuration configuration,Action save,Func<string,string> text)
    {
        this.framework=framework;this.client=client;this.log=log;this.configuration=configuration;this.save=save;this.text=text;
        framework.Update+=OnUpdate;
    }

    private void OnUpdate(IFramework _)
    {
        if(disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        var now=DateTimeOffset.UtcNow;
        if(now<nextRead)return;
        nextRead=now.AddSeconds(30);
        var options=configuration.AlarmOptions ?? new();
        if(!client.IsLoggedIn || !options.AutoAlarms)return;
        try
        {
            var events=new List<DueEvent>();
            var retainers=RetainerManager.Instance();
            if(retainers!=null && retainers->IsReady)
                for(var index=0u;index<retainers->GetRetainerCount();index++)
                {
                    var retainer=retainers->GetRetainerBySortedIndex(index);
                    if(retainer==null || retainer->RetainerId==0 || retainer->VentureId==0 || retainer->VentureComplete==0)continue;
                    events.Add(new($"venture:{retainer->RetainerId}",string.Format(text("AutoAlarms.Venture"),retainer->NameString),text("AutoAlarms.VentureGroup"),
                        DateTimeOffset.FromUnixTimeSeconds(retainer->VentureComplete),AlarmSource.Reminder));
                }
            foreach(var vessel in configuration.Voyages ?? [])
                if(vessel.ReturnAt is { } back)
                    events.Add(new($"vessel:{vessel.HouseId}/{vessel.Kind}/{vessel.Slot}",string.Format(text("AutoAlarms.Vessel"),vessel.Name),
                        text("AutoAlarms.VesselGroup"),back,AlarmSource.Voyage));
            configuration.Alarms??=[];
            if(AutoAlarms.Sync(configuration.Alarms,events,now,options.LeadMinutes))save();
            warned=false;
        }
        catch(Exception error)
        {
            nextRead=now.AddMinutes(5);
            if(!warned){warned=true;log.Warning(error,"Automatic alarms could not be updated.");}
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        framework.Update-=OnUpdate;
    }
}
