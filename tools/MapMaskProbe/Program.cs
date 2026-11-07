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
