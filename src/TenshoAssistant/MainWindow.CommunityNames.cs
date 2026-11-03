namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    // 0.407 (user rule, 2026-11-03): names from the English wiki and Garland in the game's language when the game knows them.
    internal Items.GameNames? GameNames { get; set; }
    private string CommunityMonster(string name)=>Community.CommunityNames.Name(name,GameNames?.Monster(name));
    private string CommunityPlace(string location)=>Community.CommunityNames.Location(location,place=>GameNames?.Place(place));
    private string CommunityGarland(Community.GarlandItemSource source)
        =>Community.CommunityNames.Name(source.Name,GameNames?.Garland(source.Type,source.Id,source.Name));
}
