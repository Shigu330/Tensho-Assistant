using Dalamud.Game;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Maps;

/// <summary>X-541 (17/11): the names of every zone of the Map sheet in the four languages of the game (PlaceName row → names), so
/// coordinates pasted from a list in another language than the client's find their zone.</summary>
internal static class ZoneNamesLoader
{
    public static IReadOnlyDictionary<uint, IReadOnlyList<string>> Read(IDataManager data)
    {
        var ids = data.GetExcelSheet<Map>().Select(row => row.PlaceName.RowId).Where(id => id != 0).ToHashSet();
        var names = ids.ToDictionary(id => id, _ => new List<string>());
        foreach (var language in new[] { ClientLanguage.English, ClientLanguage.French, ClientLanguage.German, ClientLanguage.Japanese })
        {
            Lumina.Excel.ExcelSheet<PlaceName>? sheet;
            try { sheet = data.GetExcelSheet<PlaceName>(language); }
            catch (Exception) { continue; }
            foreach (var id in ids)
                if (sheet.GetRowOrDefault(id) is { } row && row.Name.ExtractText().Trim() is { Length: > 0 } name && !names[id].Contains(name))
                    names[id].Add(name);
        }
        return names.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<string>)pair.Value.AsReadOnly());
    }
}
