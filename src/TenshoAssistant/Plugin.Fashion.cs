using Dalamud.Plugin.Services;
using TenshoAssistant.Fashion;

namespace TenshoAssistant;

// X-153 (0.1903, GPT review of 11/11, accepted by the user): the fashion family out of Plugin.cs, after the leves family (0.1902). The
// fashion hints' loader and the Fashion Report reader (with the outfit judged kept for the discoveries base) are each created in one install
// path and released in one release path of the plugin's registrations, in this file, with the main window's stains and weekly themes; Plugin.cs
// calls the family where its block stood, so the order of the registrations is the one of before. FashionFamily1903Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The fashion family: hints from the game sheets, the Fashion Report read while its window is open, stains and weekly themes.</summary>
    private void RegisterFashion(Configuration configuration,IDataManager data,IFramework framework,IGameGui gameGui)
    {
        FashionHintLoader? fashionLoader=null;
        registrations.Register(()=>fashionLoader=new FashionHintLoader(fashionHints,data,framework,log),()=>fashionLoader?.Dispose());
        var fashionCheck=new Fashion.FashionCheckService();
        Fashion.FashionCheckReader? fashionCheckReader=null;
        registrations.Register(()=>fashionCheckReader=new Fashion.FashionCheckReader(fashionCheck,framework,gameGui,log,()=>mainWindow.DeveloperMode)
        {
            // X-73.4 (0.861): the outfit judged, for the discoveries base.
            Judged=(theme,slots)=>
            {
                if(!configuration.Discovery.Enabled || !configuration.Discovery.FashionReport || mainWindow.DiscoveryStore is not { } store)return;
                var at=DateTimeOffset.UtcNow;
                foreach(var slot in slots)store.SeeSource(Discoveries.DiscoveryKind.FashionEvaluation,slot.Item,slot.Category,slot.Evaluation,at,theme);
            },
        },()=>fashionCheckReader?.Dispose());
        mainWindow.ConfigureFashionStains(id=>data.GetExcelSheet<Lumina.Excel.Sheets.Stain>().TryGetRow(id,out var stain) ? (stain.Name.ExtractText(),stain.Color):null);
        mainWindow.ConfigureFashionCheck(fashionCheck,row=>data.GetExcelSheet<Lumina.Excel.Sheets.FashionCheckWeeklyTheme>().TryGetRow(row,out var theme) ? theme.Name.ExtractText():null);
    }
}
