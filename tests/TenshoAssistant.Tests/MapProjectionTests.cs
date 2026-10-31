using System.Numerics;
using TenshoAssistant.Maps;
using TenshoAssistant.Npcs;

internal static class MapProjectionTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static bool Near(float a,float b)=>Math.Abs(a-b)<0.01f;
    private static MapDefinition Map(int scale,int offsetX=0,int offsetY=0)=>new(11,128,"La Noscea","Limsa Lominsa","Le Tillac",0,scale,0,"ui/map/s1t1/00/s1t100_m.tex",offsetX,offsetY,3);
    private static MapDefinition Menu(uint id,string zone,string sub,uint territory,int category,int priority,int index=0)=>new(id,territory,"Ilsabard",zone,sub,index,100,0,"",0,0,0,0,category,priority);
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Map settings default to visible layers, clamp their values and survive JSON",()=>
        {
            var settings=new TenshoAssistant.MapViewSettings();
            Check(settings.ShowFixedIcons && settings.ShowPlaceNames && settings.ShowQuests && settings.ShowFlag && settings.ShowPlayer && !settings.Locked && !settings.HideInCombat,"defaults");
            Check(TenshoAssistant.MapViewSettings.ClampScale(9)==2 && TenshoAssistant.MapViewSettings.ClampScale(float.NaN)==1 && TenshoAssistant.MapViewSettings.ClampOpacity(0)==0.2f,"clamps");
            settings.ShowQuests=false;settings.IconScale=1.5f;settings.HideInDuty=true;
            var copy=System.Text.Json.JsonSerializer.Deserialize<TenshoAssistant.MapViewSettings>(System.Text.Json.JsonSerializer.Serialize(settings))!;
            Check(!copy.ShowQuests && copy.IconScale==1.5f && copy.HideInDuty && copy.ShowFlag,"roundtrip");
            Check(System.Text.Json.JsonSerializer.Deserialize<TenshoAssistant.MapViewSettings>("{}")!.ShowFixedIcons,"older configuration keeps the defaults");
        }),
        ("Game map request opens the asked zone, not the world map selected at that frame",()=>
        {
            var catalog=new MapCatalog([Menu(696,"Radz-at-Han","",819,0,0),Menu(697,"Thavnair","",957,0,0),Menu(698,"Thavnair","Sous-sol",957,0,0,1)]);
            const uint world=13;
            Check(new GameMapRequest(world,697,957,696).Resolve(catalog)==697,"quest: world selected, zone requested");
            Check(new GameMapRequest(world,0,957,696).Resolve(catalog)==697,"territory only: first floor of the zone");
            Check(new GameMapRequest(696,0,0,696).Resolve(catalog)==0,"M on the current zone follows the player");
            Check(new GameMapRequest(697,696,0,696).Resolve(catalog)==697,"a zone selection wins");
            Check(new GameMapRequest(697,0,0,696).Resolve(catalog)==697,"another zone selected");
            Check(new GameMapRequest(world,0,0,696).Resolve(catalog)==world,"nothing else known: the selection stays");
        }),
        ("Per-icon sizes default to 100 %, hide at 0, clamp and survive JSON",()=>
        {
            var settings=new TenshoAssistant.MapViewSettings();
            Check(settings.SizeOf(60453)==1,"absent icon keeps 100 %");
            settings.IconSizes[60453]=0;settings.IconSizes[60412]=9;settings.IconSizes[60414]=float.NaN;
            Check(settings.SizeOf(60453)==0 && settings.SizeOf(60412)==2 && settings.SizeOf(60414)==1,"hidden, clamped, invalid kept at 100 %");
            var copy=System.Text.Json.JsonSerializer.Deserialize<TenshoAssistant.MapViewSettings>(System.Text.Json.JsonSerializer.Serialize(new TenshoAssistant.MapViewSettings{IconSizes={[60453]=0,[60412]=1.5f}}))!;
            Check(copy.SizeOf(60453)==0 && copy.SizeOf(60412)==1.5f && copy.SizeOf(1)==1,"roundtrip");
            Check(new TenshoAssistant.MapViewSettings{IconSizes=null!}.SizeOf(60453)==1,"missing dictionary keeps 100 %");
        }),
        ("Zone menu follows the game's category and priority with floors grouped",()=>
        {
            // Rows from checkpoints/0.105.0-zones-probe.log (Ilsabard).
            var region=Menu(733,"Ilsabard","",0,17,1);
            var maps=new[]{region,Menu(697,"Garlemald","",958,17,4),Menu(694,"Radz-at-Han","",963,17,2),Menu(696,"Thavnair","",957,17,3),
                Menu(703,"Tour de Zott","Niveau supérieur",952,17,5,3),Menu(701,"Tour de Zott","Niveau terrestre",952,17,5,1),Menu(702,"Tour de Zott","Niveau intermédiaire",952,17,5,2),
                Menu(750,"Garlemald","",958,0,0),Menu(747,"Porte des Dieux","",1024,0,0),Menu(9,"Limsa","",128,3,2)};
            var zones=MapMenus.Zones(maps,region);
            Check(zones.Select(entry=>entry[0].Zone).SequenceEqual(new[]{"Radz-at-Han","Thavnair","Garlemald","Tour de Zott"}),"order and one entry per priority");
            Check(zones[3].Select(floor=>floor.MapId).SequenceEqual(new uint[]{701,702,703}),"floors ordered by map index");
            Check(MapMenus.Zones(maps,null).Count==0 && MapMenus.Zones(maps,Menu(1,"x","",0,0,0)).Count==0,"no category, no menu");
        }),
        ("Map texture paths follow the game layout and refuse unexpected ids",()=>
        {
            Check(MapProjection.TexturePath("s1t1/00")=="ui/map/s1t1/00/s1t100_m.tex","city map");
            Check(MapProjection.TexturePath("")=="" && MapProjection.TexturePath("s1t1")=="" && MapProjection.TexturePath("../x/00")=="" && MapProjection.TexturePath("a/b/c")=="","invalid ids");
        }),
        ("World pixel and coordinate conversions agree with the vendor formula",()=>
        {
            foreach(var (scale,ox,oy) in new[]{(100,0,0),(200,-50,30),(400,12,-8)})
            {
                var map=Map(scale,ox,oy);var world=new Vector3(37.5f,0,-120f);
                var pixel=MapProjection.PixelFromWorld(world,map);var coordinate=MapProjection.CoordinateFromPixel(pixel,map);
                Check(Near(coordinate.X,NpcShopLinks.MapCoordinate(world.X,ox,(uint)scale)) && Near(coordinate.Y,NpcShopLinks.MapCoordinate(world.Z,oy,(uint)scale)),$"same coordinates as vendors at scale {scale}");
                var back=MapProjection.PixelFromCoordinate(coordinate,map);
                Check(Near(back.X,pixel.X) && Near(back.Y,pixel.Y),"coordinate round trip");
            }
            var centre=MapProjection.PixelFromWorld(Vector3.Zero,Map(100));
            Check(Near(centre.X,1024) && Near(centre.Y,1024) && MapProjection.OnMap(centre) && !MapProjection.OnMap(new Vector2(-1,5)),"centre and bounds");
            Check(Near(MapProjection.ClampZoom(10),4) && Near(MapProjection.ClampZoom(0),0.15f) && Near(MapProjection.ClampZoom(float.NaN),1),"zoom limits");
        }),
    };
}
