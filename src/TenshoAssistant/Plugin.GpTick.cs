using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1938, GPT review of 11/11, accepted by the user): the GP tick's family out of Plugin.cs, after the events (0.1937). The GP
// regeneration tick's reader (the tick bar and the GP alert at the threshold) is created in one install path and released in one release
// path of the plugin's registrations, in this file; Plugin.cs calls it where its block stood, so the order of the registrations is the one
// of before; the reader is handed back. GpTickFamily1938Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The gatherer's GP regeneration tick (on-screen bar) and the GP alert at the threshold, said in the chat and rung. Returns the reader.</summary>
    private Gathering.RegenTickReader? RegisterGpTick(Configuration configuration,IFramework framework,IObjectTable objects,ICondition condition,IChatGui chat)
    {
        // g, X-06 (0.391): gatherer GP regeneration tick (on-screen bar, off by default).
        Gathering.RegenTickReader? regenTick=null;
        // g, R-08 (0.403): GP alert at the threshold (chat line and sound with the alarm options), once per rise.
        registrations.Register(()=>regenTick=new Gathering.RegenTickReader(framework,objects,condition,()=>configuration.GpTickBar || configuration.GatheringPanel,log)
        {
            Alert=()=>configuration.GpAlert ? configuration.GpAlertThreshold:null,
            Reached=(gp,max)=>
            {
                log.Information("GP alert: {0} / {1}.",gp,max);
                var options=configuration.AlarmOptions ?? new();
                var inDuty=options.MuteInDuties && condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty];
                var message=localization.Format("GpTick.Alert",gp,max);
                if(options.ChatAllowed(inDuty))chat.Print("[Tensho] "+message);
                if(options.SoundAllowed(inDuty))Alarms.AlarmService.PlaySound(options.Sound,log);
                if(options.ChatAllowed(inDuty) || options.SoundAllowed(inDuty))linkNotices.Add(message);
            },
        },()=>regenTick?.Dispose());
        return regenTick;
    }
}
