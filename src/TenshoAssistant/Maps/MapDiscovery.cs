namespace TenshoAssistant.Maps;

/// <summary>
/// P11 (0.803, user request): the parts of a map not discovered yet, drawn as blank parchment like the game's map (docs/evidence/0.803-map-discovery-layout.md).
/// The game's « {map}d.tex » (512×384) is a grid of 4×3 cells of 128 px; each cell is the whole map at 1/16; region r is in cell r / 3, colour channel r % 3
/// (red, green, blue). The parchment is « {map}m_m.tex », the same map left blank.
/// </summary>
public static class MapDiscovery
{
    public const int Cell=128,Columns=4,Rows=3;

    /// <summary>The regions a map uses: the bits of its DiscoveryFlag (bit r = region r).</summary>
    public static IReadOnlyList<int> Regions(uint flag)=>Enumerable.Range(0,32).Where(region=>(flag>>region&1)!=0).ToArray();

    /// <summary>Top-left of region r's cell in d.tex and its channel (0 red, 1 green, 2 blue); null past the grid.</summary>
    public static (int X,int Y,int Channel)? Slot(int region)
    {
        if(region<0 || region/3>=Columns*Rows)return null;
        var cell=region/3;
        return (cell%Columns*Cell,cell/Columns*Cell,region%3);
    }

    /// <summary>The regions of the flag not discovered; null when a region cannot be read (unknown is not « undiscovered »).</summary>
    public static IReadOnlyList<int>? Undiscovered(uint flag,Func<int,bool?> discovered)
    {
        var result=new List<int>();
        foreach(var region in Regions(flag))
        {
            if(discovered(region) is not { } found)return null;
            if(!found)result.Add(region);
        }
        return result;
    }

    /// <summary>The 128×128 coverage (0–255) of the given regions together, from d.tex in BGRA.</summary>
    public static byte[] Coverage(ReadOnlySpan<byte> bgra,int width,int height,IReadOnlyList<int> regions)
    {
        var mask=new byte[Cell*Cell];
        if(width<Columns*Cell || height<Rows*Cell || bgra.Length<width*height*4)return mask;
        foreach(var region in regions)
        {
            if(Slot(region) is not { } slot)continue;
            // BGRA: red is byte 2, green 1, blue 0.
            var offset=2-slot.Channel;
            for(var v=0;v<Cell;v++)
                for(var u=0;u<Cell;u++)
                {
                    var value=bgra[((slot.Y+v)*width+slot.X+u)*4+offset];
                    if(value>mask[v*Cell+u])mask[v*Cell+u]=value;
                }
        }
        return mask;
    }

    /// <summary>An RGBA image of size×size: the parchment's colour where the coverage (smoothed between cells) is, transparent elsewhere.</summary>
    public static byte[] Overlay(ReadOnlySpan<byte> parchmentBgra,int parchmentSize,byte[] coverage,int size)
    {
        var rgba=new byte[size*size*4];
        if(parchmentSize<=0 || parchmentBgra.Length<parchmentSize*parchmentSize*4 || coverage.Length<Cell*Cell)return rgba;
        for(var y=0;y<size;y++)
        {
            var fy=Math.Clamp((y+0.5f)*Cell/size-0.5f,0,Cell-1);var y0=(int)fy;var y1=Math.Min(Cell-1,y0+1);var ty=fy-y0;
            var py=Math.Min(parchmentSize-1,y*parchmentSize/size);
            for(var x=0;x<size;x++)
            {
                var fx=Math.Clamp((x+0.5f)*Cell/size-0.5f,0,Cell-1);var x0=(int)fx;var x1=Math.Min(Cell-1,x0+1);var tx=fx-x0;
                var top=coverage[y0*Cell+x0]*(1-tx)+coverage[y0*Cell+x1]*tx;var bottom=coverage[y1*Cell+x0]*(1-tx)+coverage[y1*Cell+x1]*tx;
                var alpha=(byte)Math.Clamp(top*(1-ty)+bottom*ty,0,255);
                if(alpha==0)continue;
                var px=Math.Min(parchmentSize-1,x*parchmentSize/size);
                var source=(py*parchmentSize+px)*4;var target=(y*size+x)*4;
                rgba[target]=parchmentBgra[source+2];rgba[target+1]=parchmentBgra[source+1];rgba[target+2]=parchmentBgra[source];rgba[target+3]=alpha;
            }
        }
        return rgba;
    }

    /// <summary>The parchment and discovery files beside a map texture (« …_m.tex »); empty when the path is not one.</summary>
    public static (string Parchment,string Discovery) Files(string mapTexture)=>mapTexture.EndsWith("_m.tex",StringComparison.Ordinal)
        ? (mapTexture[..^6]+"m_m.tex",mapTexture[..^6]+"d.tex"):("","");
}
