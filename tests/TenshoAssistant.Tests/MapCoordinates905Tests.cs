// 0.905 (X-76): one shared world-to-map conversion and one shared coordinate text, always with a dot as the game writes it.
using System.Globalization;
using System.Numerics;
using TenshoAssistant.Maps;
internal static class MapCoordinates905Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map coordinate text: one decimal with a dot, even on a French Windows",() =>
        {
            var before=CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture=new CultureInfo("fr-FR");
                Check(MapProjection.CoordinateNumber(12.34f)=="12.3","number");
                Check(MapProjection.CoordinateText(new(12.34f,4.5f))=="X 12.3 Y 4.5","text");
                Check(MapBookmarks.DefaultName("Zone",12.34f,4.5f)=="Zone · 12.3, 4.5","default bookmark name");
            }
            finally { CultureInfo.CurrentCulture=before; }
        }),
        ("Map coordinate from a world position: the shared helper equals pixel then coordinate",() =>
        {
            var map=new MapDefinition(11,128,"La Noscea","Limsa Lominsa","Le Tillac",0,200,0,"ui/map/s1t1/00/s1t100_m.tex",-25,13,3);
            var world=new Vector3(31.5f,7,-112.25f);
            Check(MapProjection.CoordinateFromWorld(world,map)==MapProjection.CoordinateFromPixel(MapProjection.PixelFromWorld(world,map),map),"same coordinate");
        }),
    ];
}
