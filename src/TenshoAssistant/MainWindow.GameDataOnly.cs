namespace TenshoAssistant;

// X-76 step 3 (0.945, user request): one switch for « game data and what you observed only »: the seven community sources off at once, their own
// choices kept and given back when it is unticked. The discoveries you share are observed data and are not touched.
internal sealed partial class MainWindow
{
    // BoolSetting saves and, on a failed save, calls the setter back with the old value: the sources come back with it.
    private void DrawGameDataOnly()
        =>BoolSetting("Advanced.GameDataOnly","Advanced.GameDataOnlyTip",()=>configuration.GameDataOnly,value=>{CommunitySources.ApplyGameDataOnly(configuration,value);if(value)market.CancelBackground();},c=>c.GameDataOnly);
}
