using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1937, GPT review of 11/11, accepted by the user): the events' family out of Plugin.cs, after the reminders (0.1936). The festivals'
// reader is created in one install path and released in one release path of the plugin's registrations, in this file; Plugin.cs calls it
// where its block stood, so the order of the registrations is the one of before; the reader is handed back. EventsFamily1937Tests counts
// those paths.
public sealed partial class Plugin
{
    /// <summary>The running festivals (GameMain), their quests (Quest.Festival) and English names (LuminaSupplemental, embedded). Returns the reader.</summary>
    private Events.FestivalReader? RegisterFestivals(IFramework framework,IClientState client,IDataManager data)
    {
        // 0.342 (R-09): running festivals (GameMain), their quests (Quest.Festival) and English names (LuminaSupplemental, embedded).
        Events.FestivalReader? festivals=null;
        registrations.Register(()=>festivals=new Events.FestivalReader(framework,client,data,log),()=>festivals?.Dispose());
        return festivals;
    }
}
