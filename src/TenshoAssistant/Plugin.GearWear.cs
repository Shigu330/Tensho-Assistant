using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1939, GPT review of 11/11, accepted by the user): the gear wear's family out of Plugin.cs, after the GP tick (0.1938). The gear
// wear's watcher (the repair and materia extraction alert, once per crossing) is created in one install path and released in one release
// path of the plugin's registrations, in this file; Plugin.cs calls it where its block stood, so the order of the registrations is the one
// of before; the worn gear is given as a function, read when it is checked. GearWearFamily1939Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The repair and materia extraction alert, said in the chat and rung with the alarm options, once per crossing.</summary>
    private void RegisterGearWear(Configuration configuration,IFramework framework,IClientState client,ICondition condition,IChatGui chat,
        Func<IReadOnlyList<Gear.EquippedPiece>> equipped)
    {
        // g, M-06 (0.376): repair and materia extraction alert (chat and sound with the alarm options), once per crossing.
        Gear.GearWearWatcher? gearWear=null;
        registrations.Register(()=>gearWear=new Gear.GearWearWatcher(framework,client,equipped,()=>configuration.GearWear ?? new(),log){Crossed=(repair,extract,state)=>
        {
            var options=configuration.AlarmOptions ?? new();
            var inDuty=options.MuteInDuties && condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty];
            var repairText=repair ? localization.Format("GearWear.AlertRepair",state.ToRepair,state.LowestCondition ?? 0):null;
            var extractText=extract ? localization.Format("GearWear.AlertExtract",state.ToExtract):null;
            if(options.ChatAllowed(inDuty))
            {
                if(repairText is not null)chat.Print("[Tensho] "+repairText);
                if(extractText is not null)chat.Print("[Tensho] "+extractText);
            }
            if(options.SoundAllowed(inDuty))Alarms.AlarmService.PlaySound(options.Sound,log);
            if(options.ChatAllowed(inDuty) || options.SoundAllowed(inDuty))
            {
                if(repairText is not null)linkNotices.Add(repairText);
                if(extractText is not null)linkNotices.Add(extractText);
            }
        }},()=>gearWear?.Dispose());
    }
}
