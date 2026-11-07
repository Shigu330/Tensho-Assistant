using Lumina;
using Lumina.Data;
using Lumina.Excel.Sheets;

// Read-only: which texture files exist for a map, and its discovery columns. Arguments: the sqpack directory, then map rows or a place name.
using var game=new GameData(args[0],new LuminaOptions{DefaultExcelLanguage=Language.English,CacheFileResources=false});
var maps=game.GetExcelSheet<Map>()!;
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
            Console.WriteLine($"{kind} layer {layer.LayerId} \"{layer.Name}\" festival {layer.FestivalID} phase {layer.FestivalPhaseID} npcs {npcs.Length}: "+string.Join(" ",npcs.Take(12).Select(o=>$"{((Lumina.Data.Parsing.Layer.LayerCommon.ENPCInstanceObject)o.Object).ParentData.ParentData.BaseId}@{o.Transform.Translation.X:F0},{o.Transform.Translation.Z:F0}")));
        }
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
