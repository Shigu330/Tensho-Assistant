namespace TenshoAssistant;

// X-76 P5 (0.932): an island animal on the map: its size and the two leavings it gives in the pasture, from MJIAnimals (game data).
internal sealed partial class MainWindow
{
    private string IslandAnimalLabel(Maps.MapObjectMarker item)
    {
        if(mapLive?.IslandAnimalInfo.TryGetValue(item.BaseId,out var info)!=true)return item.Name;
        var lines=new List<string>{item.Name};
        if(info.Size is >= 1 and <= 3)lines.Add(localization.Get("MapView.AnimalSize"+info.Size));
        if(info.Rewards.Length>0)
            lines.Add(localization.Format("MapView.AnimalLeavings",string.Join(", ",info.Rewards.Select(id=>itemCatalog.State.Catalog?.Find(id)?.Name ?? $"#{id}"))));
        return string.Join("\n",lines);
    }
}
