// 0.844 (X-67, user request): up and down arrows on map icons above or below the player.
using TenshoAssistant.Maps;
internal static class HeightArrows844Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Height arrows: above, below, within the threshold, unknown",() =>
        {
            Check(MapHeight.Arrow(33.1f,-5.2f,10)==1 && MapHeight.Arrow(-5.2f,33.1f,10)==-1,"38 yalms apart");
            Check(MapHeight.Arrow(33.1f,31.9f,10)==0,"same floor");
            Check(MapHeight.Arrow(float.NaN,30,10)==0 && MapHeight.Arrow(30,float.NaN,10)==0,"unknown: none");
            var settings=new TenshoAssistant.MapViewSettings();
            Check(settings.HeightArrows && settings.HeightArrowYalms==10,"defaults");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(view.Contains("height:marker.Position.Y);") && view.Contains("height:fate.Position.Y);"),"quests, events, FATEs");
            // 0.849 (X-71): the game's chevron, turned down for « below ».
            var (upFrom,upTo)=MapHeight.ChevronUv(1);var (downFrom,downTo)=MapHeight.ChevronUv(-1);
            Check(upFrom.Y<upTo.Y && downFrom.Y>downTo.Y && upFrom.X==downFrom.X,"turned down");
            Check(MapHeight.ChevronTexture=="ui/uld/NaviMap.tex" && view.Contains("textures.GetFromGame(MapHeight.ChevronTexture)"),"the game's texture");
        }),
    ];
}
