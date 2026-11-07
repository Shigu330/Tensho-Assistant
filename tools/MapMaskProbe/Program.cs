using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;

// Read-only: which texture files exist for a map, and its discovery columns. Arguments: the sqpack directory, then map rows or a place name.
using var game=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=Language.English,CacheFileResources=false});
var maps=game.GetExcelSheet<Map>()!;
// 0.834 (X-64): « --item <name> »: an item's gathering points, their territory and map, and whether ExportedGatheringPoint places their base.
if(args.Length>2 && args[1]=="--item")
{
    var items=game.GetExcelSheet<Item>()!.Where(item=>item.Name.ExtractText().Equals(args[2],StringComparison.OrdinalIgnoreCase)).Select(item=>item.RowId).ToHashSet();
    var gatheringItems=game.GetExcelSheet<GatheringItem>()!.Where(row=>items.Contains(row.Item.RowId)).Select(row=>row.RowId).ToHashSet();
    var exported=game.GetExcelSheet<ExportedGatheringPoint>()!;
    // The GatheringItemPoint links (gathering item → points), as Tensho's catalog reads them too.
    var links=game.GetSubrowExcelSheet<GatheringItemPoint>()!;
    foreach(var gatheringItem in gatheringItems)
        if(links.TryGetRow(gatheringItem,out var linked))
            foreach(var link in linked)
                if(link.GatheringPoint.ValueNullable is { } point)
                    Console.WriteLine($"LINK item {gatheringItem} point {point.RowId} base {point.GatheringPointBase.RowId} territory {point.TerritoryType.RowId} {(point.PlaceName.ValueNullable?.Name.ExtractText() ?? "")} map {(point.TerritoryType.ValueNullable?.Map.RowId ?? 0)} exported {(exported.GetRowOrDefault(point.GatheringPointBase.RowId) is { } spot ? $"{spot.X},{spot.Y}":"none")}");
    foreach(var basis in game.GetExcelSheet<GatheringPointBase>()!)
    {
        if(!basis.Item.Any(entry=>gatheringItems.Contains(entry.RowId)))continue;
        var placed=exported.GetRowOrDefault(basis.RowId) is { } spot ? $"{spot.X},{spot.Y} radius {spot.Radius}":"none";
        Console.WriteLine($"BASE {basis.RowId} level {basis.GatheringLevel} type {basis.GatheringType.RowId} exported {placed}");
        foreach(var point in game.GetExcelSheet<GatheringPoint>()!)
            if(point.GatheringPointBase.RowId==basis.RowId)
                Console.WriteLine($"  POINT {point.RowId} territory {point.TerritoryType.RowId} {(point.PlaceName.ValueNullable?.Name.ExtractText() ?? "")} map {(point.TerritoryType.ValueNullable?.Map.RowId ?? 0)} type {point.Type}");
    }
    return;
}
// 0.833 (X-63): « --markers <place> »: a zone map's fixed markers (DataType 1 = link to a map, 3 = aetheryte, 4 = aethernet shard) and the territory's Aetheryte rows.
if(args.Length>2 && args[1]=="--markers")
{
    var markerRows=game.GetSubrowExcelSheet<MapMarker>()!;
    foreach(var map in maps)
    {
        var place=map.PlaceName.ValueNullable?.Name.ExtractText() ?? "";
        if(map.TerritoryType.RowId==0 || !place.Contains(args[2],StringComparison.OrdinalIgnoreCase))continue;
        Console.WriteLine($"MAP {map.RowId} {place} territory {map.TerritoryType.RowId} range {map.MapMarkerRange} scale {map.SizeFactor}");
        if(markerRows.TryGetRow(map.MapMarkerRange,out var rows))
            foreach(var row in rows)
                Console.WriteLine($"  type {row.DataType} key {row.DataKey.RowId} icon {row.Icon} at {row.X},{row.Y} label {(row.PlaceNameSubtext.ValueNullable?.Name.ExtractText() ?? "")}");
        foreach(var aetheryte in game.GetExcelSheet<Aetheryte>()!)
            if(aetheryte.Territory.RowId==map.TerritoryType.RowId)
                Console.WriteLine($"  AETHERYTE {aetheryte.RowId} isAetheryte {aetheryte.IsAetheryte} group {aetheryte.AethernetGroup} place {(aetheryte.PlaceName.ValueNullable?.Name.ExtractText() ?? "")} aethernet {(aetheryte.AethernetName.ValueNullable?.Name.ExtractText() ?? "")} level {aetheryte.Level[0].RowId}");
    }
    return;
}
var wanted=args.Skip(1).ToArray();
foreach(var map in maps)
{
    var name=map.PlaceName.ValueNullable?.Name.ExtractText() ?? "";
    if(!wanted.Any(word=>word==map.RowId.ToString() || name.Contains(word,StringComparison.OrdinalIgnoreCase)))continue;
    var id=map.Id.ExtractText();if(id.Length==0)continue;
    var flat=id.Replace("/","");
    Console.WriteLine($"MAP {map.RowId} {name} id {id} discoveryIndex {map.DiscoveryIndex} discoveryFlag {map.DiscoveryFlag} discoveryArrayByte {map.DiscoveryArrayByte}");
    foreach(var suffix in new[]{"_m","_s","m_m","_d","d","_m_m","m","_md","_dis","_disc","_mask","_r","_a"})
    {
        var path=$"ui/map/{id}/{flat}{suffix}.tex";
        if(game.FileExists(path)){var tex=game.GetFile<Lumina.Data.Files.TexFile>(path)!;Console.WriteLine($"  {path} {tex.Header.Width}x{tex.Header.Height} {tex.Header.Format}");}
    }
}

// Second pass: what the mask holds.
if(args.Length>2 && args[2]=="--pixels")
foreach(var map in maps)
{
    var name=map.PlaceName.ValueNullable?.Name.ExtractText() ?? "";
    if(!name.Contains(args[1],StringComparison.OrdinalIgnoreCase))continue;
    var id=map.Id.ExtractText();var flat=id.Replace("/","");
    foreach(var suffix in new[]{"m_m","d"})
    {
        var tex=game.GetFile<Lumina.Data.Files.TexFile>($"ui/map/{id}/{flat}{suffix}.tex")!;
        var data=tex.ImageData;var w=tex.Header.Width;var h=tex.Header.Height;
        var counts=new Dictionary<uint,int>();
        for(var i=0;i<data.Length;i+=4)
        {
            var key=(uint)(data[i]|data[i+1]<<8|data[i+2]<<16|data[i+3]<<24);
            counts[key]=counts.GetValueOrDefault(key)+1;
        }
        if(args.Length>3)File.WriteAllBytes(Path.Combine(args[3],$"{flat}{suffix}.bgra"),data);
        Console.WriteLine($"{suffix}: {w}x{h}, {counts.Count} colours; top: "+string.Join(" ",counts.OrderByDescending(pair=>pair.Value).Take(40).Select(pair=>$"{pair.Key:X8}:{pair.Value}")));
    }
    break;
}

// Third pass (P11, 2026-11-07): PNG copies to look at: d.tex as is, the map and the parchment scaled to 512.
if(args.Length>3 && args[2]=="--png")
foreach(var map in maps)
{
    var name=map.PlaceName.ValueNullable?.Name.ExtractText() ?? "";
    if(!name.Contains(args[1],StringComparison.OrdinalIgnoreCase))continue;
    var id=map.Id.ExtractText();var flat=id.Replace("/","");
    foreach(var (suffix,scale) in new[]{("d",1),("_m",4),("m_m",4)})
    {
        var path=$"ui/map/{id}/{flat}{suffix}.tex";
        if(!game.FileExists(path))continue;
        var tex=game.GetFile<Lumina.Data.Files.TexFile>(path)!;
        var data=tex.ImageData;int w=tex.Header.Width,h=tex.Header.Height;int ow=w/scale,oh=h/scale;
        var rgb=new byte[ow*oh*3];
        for(var y=0;y<oh;y++)for(var x=0;x<ow;x++){var i=((y*scale)*w+x*scale)*4;var o=(y*ow+x)*3;rgb[o]=data[i+2];rgb[o+1]=data[i+1];rgb[o+2]=data[i];}
        Png.Write(Path.Combine(args[3],$"{flat}{suffix}.png"),ow,oh,rgb);
        Console.WriteLine($"PNG {flat}{suffix} {ow}x{oh}");
    }
    break;
}

// X-45 (0.811): « --lgb <territory row> »: each layer of planevent.lgb and planner.lgb with its festival and its event NPCs, to tell which NPCs only an event places.
if(args.Length>2 && args[1]=="--lgb" && uint.TryParse(args[2],out var territoryRow) && game.GetExcelSheet<TerritoryType>()!.GetRowOrDefault(territoryRow) is { } territory)
{
    var bg=territory.Bg.ExtractText();var cut=bg.IndexOf("/level/",StringComparison.Ordinal);
    foreach(var kind in new[]{"planevent","planner","bg"})
    {
        var path=$"bg/{bg[..(cut+1)]}level/{kind}.lgb";
        if(!game.FileExists(path)){Console.WriteLine($"{path}: missing");continue;}
        var file=game.GetFile<Lumina.Data.Files.LgbFile>(path)!;
        foreach(var layer in file.Layers)
        {
            var npcs=layer.InstanceObjects.Where(o=>o.AssetType==Lumina.Data.Parsing.Layer.LayerEntryType.EventNPC).ToArray();
            if(npcs.Length==0)continue;
            Console.WriteLine($"{kind} layer {layer.LayerId} \"{layer.Name}\" festival {layer.FestivalID} phase {layer.FestivalPhaseID} npcs {npcs.Length}: "+string.Join(" ",npcs.Select(o=>$"{((Lumina.Data.Parsing.Layer.LayerCommon.ENPCInstanceObject)o.Object).ParentData.ParentData.BaseId}@{o.Transform.Translation.X:F0},{o.Transform.Translation.Z:F0}")));
        }
    }
}


// X-45 (0.829): « --npc <id> »: every territory whose planevent.lgb or planner.lgb places that event NPC, with the layer, its festival and the territory's map.
if(args.Length>2 && args[1]=="--npc" && uint.TryParse(args[2],out var npcWanted))
    foreach(var place in game.GetExcelSheet<TerritoryType>()!)
    {
        var bgPath=place.Bg.ExtractText();var cutAt=bgPath.IndexOf("/level/",StringComparison.Ordinal);
        if(cutAt<0)continue;
        foreach(var kind in new[]{"planevent","planner"})
        {
            var path=$"bg/{bgPath[..(cutAt+1)]}level/{kind}.lgb";
            if(!game.FileExists(path))continue;
            Lumina.Data.Files.LgbFile? file;try{file=game.GetFile<Lumina.Data.Files.LgbFile>(path);}catch{continue;}
            foreach(var layer in file!.Layers)
                foreach(var o in layer.InstanceObjects)
                    if(o.AssetType==Lumina.Data.Parsing.Layer.LayerEntryType.EventNPC && ((Lumina.Data.Parsing.Layer.LayerCommon.ENPCInstanceObject)o.Object).ParentData.ParentData.BaseId==npcWanted)
                        Console.WriteLine($"territory {place.RowId} map {place.Map.RowId} {kind} \"{layer.Name}\" festival {layer.FestivalID}");
        }
    }

static class Png
{
    public static void Write(string path,int w,int h,byte[] rgb)
    {
        using var file=File.Create(path);
        file.Write([137,80,78,71,13,10,26,10]);
        void Chunk(string type,byte[] body)
        {
            var len=BitConverter.GetBytes(body.Length);if(BitConverter.IsLittleEndian)Array.Reverse(len);file.Write(len);
            var t=System.Text.Encoding.ASCII.GetBytes(type);file.Write(t);file.Write(body);
            var crc=Crc(t.Concat(body).ToArray());var c=BitConverter.GetBytes(crc);if(BitConverter.IsLittleEndian)Array.Reverse(c);file.Write(c);
        }
        var head=new byte[13];void Be(int at,int v){head[at]=(byte)(v>>24);head[at+1]=(byte)(v>>16);head[at+2]=(byte)(v>>8);head[at+3]=(byte)v;}
        Be(0,w);Be(4,h);head[8]=8;head[9]=2;
        Chunk("IHDR",head);
        var raw=new MemoryStream();
        using(var z=new System.IO.Compression.ZLibStream(raw,System.IO.Compression.CompressionLevel.Fastest,true))
            for(var y=0;y<h;y++){z.WriteByte(0);z.Write(rgb,y*w*3,w*3);}
        Chunk("IDAT",raw.ToArray());Chunk("IEND",[]);
    }
    static uint Crc(byte[] bytes){uint c=0xFFFFFFFF;foreach(var b in bytes){c^=b;for(var k=0;k<8;k++)c=(c&1)!=0 ? 0xEDB88320^(c>>1):c>>1;}return ~c;}
}
