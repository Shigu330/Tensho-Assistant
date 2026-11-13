using Dalamud.Plugin.Services;
using TenshoAssistant.Reminders;

namespace TenshoAssistant;

// X-178 (0.1258): the garden's two catalogues, built by the window from the game's sheets so the plugin and the desktop application
// get them the same way (they were set by Plugin.cs alone: the application had neither the seed list nor the crossbreeds).
// 0.449 (DEC-027): the embedded garden seeds (Lotlab), named in the game's language, read once on first use.
// 0.450 (DEC-027): the crossbreeds (nick75g, English names linked through the English Item sheet), read once off the frame.
internal sealed partial class MainWindow
{
    /// <summary>Gives the garden its seed list and its crossbreeds from the game's sheets (the plugin at load, the desktop application at start).</summary>
    internal void ConfigureGarden(Dalamud.Plugin.Services.IDataManager data)
    {
        IReadOnlyList<(GardenSeed,string)>? gardenSeeds=null;
        GardenSeedCatalog=()=>gardenSeeds??=ReadGardenSeeds(data);
        Task<(GardenCrossData,IReadOnlyDictionary<uint,string>)>? gardenCrosses=null;
        GardenCrossCatalog=()=>gardenCrosses??=Task.Run(()=>ReadGardenCrosses(data));
    }

    private IReadOnlyList<(GardenSeed,string)> ReadGardenSeeds(IDataManager data)
    {
        try
        {
            using var stream=typeof(Plugin).Assembly.GetManifestResourceStream("TenshoAssistant.Reminders.Data.garden-seeds.csv")
                ?? throw new InvalidOperationException("Embedded garden seeds are missing.");
            using var reader=new StreamReader(stream);
            var items=data.GetExcelSheet<Lumina.Excel.Sheets.Item>();
            return GardenSeeds.Read(reader)
                .Select(seed=>(seed,items.GetRowOrDefault(seed.SeedId)?.Name.ExtractText() ?? ""))
                .Where(pair=>pair.Item2.Length>0).OrderBy(pair=>pair.Item2,StringComparer.CurrentCultureIgnoreCase).ToArray();
        }
        catch(Exception error){log.Warning(error,"Garden seeds unavailable.");return [];}
    }

    private (GardenCrossData,IReadOnlyDictionary<uint,string>) ReadGardenCrosses(IDataManager data)
    {
        try
        {
            var english=GardenCrosses.EnglishIndex(data.GetExcelSheet<Lumina.Excel.Sheets.Item>(Dalamud.Game.ClientLanguage.English)
                .Select(row=>(row.RowId,row.Name.ExtractText())));
            using var stream=typeof(Plugin).Assembly.GetManifestResourceStream("TenshoAssistant.Reminders.Data.garden-crosses.csv")
                ?? throw new InvalidOperationException("Embedded garden crossbreeds are missing.");
            using var reader=new StreamReader(stream);
            var crosses=GardenCrosses.Read(reader,english);
            if(crosses.Unmatched.Count>0)log.Information("Garden crossbreeds: {0} source names not in the English Item sheet: {1}.",crosses.Unmatched.Count,string.Join(", ",crosses.Unmatched));
            var items=data.GetExcelSheet<Lumina.Excel.Sheets.Item>();
            IReadOnlyDictionary<uint,string> names=crosses.Seeds.Concat([GardenTopsoil3,GardenTopsoil2]).Distinct().ToDictionary(id=>id,id=>items.GetRowOrDefault(id)?.Name.ExtractText() ?? "");
            return (crosses,names);
        }
        catch(Exception error){log.Warning(error,"Garden crossbreeds unavailable.");throw;}
    }
}
