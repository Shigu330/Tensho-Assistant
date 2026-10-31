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
        ("Map variants of one territory with the same scale and offset share positions",()=>
        {
            // Auxesia: territory 1319, maps 1246 (default) and 1255 (a later stage), same scale 100 and offset 0.
            var catalog=new MapCatalog([new MapDefinition(1246,1319,"???","Auxesia","",2,100),new MapDefinition(1255,1319,"???","Auxesia","",11,100),
                new MapDefinition(500,1319,"???","Auxesia","Sous-sol",3,200),new MapDefinition(12,128,"La Noscea","Limsa","",0,100)]);
            var equivalent=catalog.Equivalent(catalog.Find(1255)!);
            Check(equivalent.Contains(1246u) && equivalent.Contains(1255u) && !equivalent.Contains(500u) && !equivalent.Contains(12u),"same ground only");
        }),
        ("Vendor joins the game icon it stands by: shops at 16 pixels, unlabelled services at 8",()=>
        {
            // Positions from docs/evidence/0.155.0-vendor-markers-probe.log (Limsa: class/job clerk 63919 at 5 px, market board 60570 at 37).
            var limsa=new (float X,float Y,uint Icon,bool Labelled)[]{(783,1045,60570,false),(809,1014,63919,false)};
            Check(MapVendorIcons.Nearest(limsa,new(809,1019))==1,"Rowena's representative takes the class/job clerk icon");
            Check(MapVendorIcons.Nearest(limsa,new(809,1030))==-1,"service icon farther than 8 pixels stays apart");
            Check(MapVendorIcons.Nearest([(100,100,60412,false)],new(112,100))==0,"shop icon keeps its 16 pixel reach");
            Check(MapVendorIcons.Nearest([(100,100,60442,true),(100,104,60434,true)],new(100,101))==-1,"labelled or unknown icons never take a vendor");
            Check(MapVendorIcons.Nearest([(100,106,60412,false),(100,102,60434,false)],new(100,100))==1,"nearest qualifying icon wins");
            // Auxesia (docs/evidence/0.171.0-cosmic-vendors-probe.log): Échoppe 2.3 px, Échangeur 3.7 px from the credit exchange.
            var auxesia=new (float X,float Y,uint Icon,bool Labelled)[]{(1342,1403,60412,false),(1342,1397,60935,false)};
            Check(MapVendorIcons.Nearest(auxesia,new(1342,1400.7f),60935)==1 && MapVendorIcons.Nearest(auxesia,new(1342,1400.7f),60412)==0,"vendor joins the icon of its kind");
            Check(MapVendorIcons.Nearest([(1342,1403,60412,false)],new(1342,1400.7f),60935)==0,"no icon of its kind in reach: nearest");
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
            Check(!new GameMapRequest(696,0,0,696).Pointed && new GameMapRequest(696,0,0,696,QuestLog:true).Pointed && new GameMapRequest(696,0,0,696,QuestId:65539).Pointed,"journal or quest = pointed, never the M toggle");
            Check(new GameMapRequest(696,0,0,696,Target:new System.Numerics.Vector2(10,20)).Pointed,"a game target is pointed");
            Check(new GameMapRequest(696,697,0,696,Target:new System.Numerics.Vector2(10,20)).Resolve(catalog)==697,"journal target on another map wins over the selected zone");
        }),
        ("Zoom follow keeps icons readable when zoomed out and is clamped",()=>
        {
            var settings=new TenshoAssistant.MapViewSettings();
            Check(Near(settings.ZoomFactor(1),1) && Near(settings.ZoomFactor(0.4f),MathF.Sqrt(0.4f)),"default half follow: 40 % zoom keeps 63 % size");
            settings.ZoomFollow=0;Check(Near(settings.ZoomFactor(0.4f),1),"fixed size");
            settings.ZoomFollow=5;Check(Near(settings.ZoomFactor(0.4f),0.4f),"clamped to proportional");
            settings.ZoomFollow=float.NaN;Check(Near(settings.ZoomFactor(0.25f),0.5f),"invalid = half");
        }),
        ("Per-map icon overrides win over the global table, priorities clamp and reset per scope",()=>
        {
            var settings=new TenshoAssistant.MapViewSettings();
            settings.SetSize(60453,0,1.5f);settings.SetSize(60453,696,0);settings.SetPriority(60453,0,3);settings.SetPriority(60412,696,42);
            Check(settings.SizeOf(60453,1)==1.5f && settings.SizeOf(60453,696)==0 && settings.SizeOf(1,696)==1,"map override, global fallback, default");
            Check(settings.PriorityOf(60453,696)==3 && settings.PriorityOf(60412,696)==9 && settings.PriorityOf(60412,1)==0,"priority fallback and clamp");
            Check(settings.HasMapOverrides(696) && !settings.HasMapOverrides(1),"overrides known");
            var copy=System.Text.Json.JsonSerializer.Deserialize<TenshoAssistant.MapViewSettings>(System.Text.Json.JsonSerializer.Serialize(settings))!;
            Check(copy.SizeOf(60453,696)==0 && copy.PriorityOf(60412,696)==9 && copy.SizeOf(60453,2)==1.5f,"JSON roundtrip");
            settings.Reset(696);
            Check(!settings.HasMapOverrides(696) && settings.SizeOf(60453,696)==1.5f,"back to global for one map");
            settings.Reset(0);
            Check(settings.SizeOf(60453,696)==1 && settings.PriorityOf(60453,696)==0,"global reset");
            var old=System.Text.Json.JsonSerializer.Deserialize<TenshoAssistant.MapViewSettings>("{\"IconSizes\":{\"60453\":0}}")!;
            Check(old.SizeOf(60453,696)==0 && old.PriorityOf(60453,696)==0,"0.111 configuration still applies");
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
            // Rows from docs/evidence/0.105.0-zones-probe.log (Ilsabard).
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
