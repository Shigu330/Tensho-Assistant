using System.Numerics;

namespace TenshoAssistant.Maps;

/// <summary>
/// Game map geometry: the map texture spans 2048 pixels; pixel = (world + offset) × scale + 1024 with scale = SizeFactor/100,
/// and the coordinate shown in game = 41/scale × pixel/2048 + 1 (same formula as NpcShopLinks.MapCoordinate).
/// </summary>
public static class MapProjection
{
    public const float TextureSize=2048;
    /// <summary>"s1t1/00" → "ui/map/s1t1/00/s1t100_m.tex"; empty when the id is not a map path.</summary>
    public static string TexturePath(string id)
    {
        if(string.IsNullOrWhiteSpace(id) || id.Count(ch=>ch=='/')!=1 || id.Any(ch=>!(char.IsAsciiLetterOrDigit(ch) || ch=='/')))return "";
        return $"ui/map/{id}/{id.Replace("/","",StringComparison.Ordinal)}_m.tex";
    }
    private static float Scale(int sizeFactor)=>Math.Max(1,sizeFactor)/100f;
    public static Vector2 PixelFromWorld(Vector3 world,MapDefinition map)
    {var scale=Scale(map.SizeFactor);return new((world.X+map.OffsetX)*scale+1024,(world.Z+map.OffsetY)*scale+1024);}
    public static Vector2 CoordinateFromPixel(Vector2 pixel,MapDefinition map)
    {var scale=Scale(map.SizeFactor);return new(41f/scale*pixel.X/TextureSize+1,41f/scale*pixel.Y/TextureSize+1);}
    public static Vector2 PixelFromCoordinate(Vector2 coordinate,MapDefinition map)
    {var scale=Scale(map.SizeFactor);return new((coordinate.X-1)*TextureSize*scale/41f,(coordinate.Y-1)*TextureSize*scale/41f);}
    public static bool OnMap(Vector2 pixel)=>pixel.X>=0 && pixel.Y>=0 && pixel.X<=TextureSize && pixel.Y<=TextureSize;
    /// <summary>Zoom kept between 0.15× and 4× of the 2048-pixel texture.</summary>
    public static float ClampZoom(float zoom)=>Math.Clamp(float.IsFinite(zoom) ? zoom:1,0.15f,4f);
}
