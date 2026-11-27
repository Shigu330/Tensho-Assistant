using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1931, GPT review of 11/11, accepted by the user): the free company's family out of Plugin.cs, after the character's family (0.1930).
// The aetherial wheels' reader and the workshop deliveries' reader are each created in one install path and released in one release path of
// the plugin's registrations, in this file; Plugin.cs calls the two blocks where they stood (the wheels' save given in), so the order of the
// registrations is the one of before. FreeCompanyFamily1931Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The aetherial wheels of the company's stand, read in its window; <paramref name="save"/> keeps them in the settings.</summary>
    private void RegisterAetherialWheels(Configuration configuration,IDataManager data,IFramework framework,IGameGui gameGui,Action save)
    {
        // X-225 (0.1719): the aetherial wheels of the company's stand, read in its window (the Reminders page and the automatic alarms follow).
        FreeCompany.AetherialWheelReader? wheelReader=null;
        registrations.Register(()=>wheelReader=new FreeCompany.AetherialWheelReader(framework,gameGui,data,log,configuration,save,()=>configuration.DeveloperMode),()=>wheelReader?.Dispose());
    }

    /// <summary>What the workshop already received, read in its delivery window.</summary>
    private void RegisterWorkshopDeliveries(IFramework framework,IGameGui gameGui)
    {
        // 0.787 (X-28): what the workshop already received, from its delivery window.
        FreeCompany.WorkshopDeliveryReader? workshopDeliveryReader=null;
        registrations.Register(()=>workshopDeliveryReader=new FreeCompany.WorkshopDeliveryReader(framework,gameGui,log,mainWindow.WorkshopDeliverySeen),()=>workshopDeliveryReader?.Dispose());
    }
}
