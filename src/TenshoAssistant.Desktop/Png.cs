using System.IO.Compression;

/// <summary>A minimal PNG writer (RGBA, 8 bits, filter 0 on each row), from the PNG specification: enough for a window capture.</summary>
internal static class Png
{
    public static void Write(string path,int width,int height,byte[] rgba)
    {
        using var file=File.Create(path);
        file.Write([0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A]);
        var header=new byte[13];
        WriteBig(header,0,(uint)width);WriteBig(header,4,(uint)height);header[8]=8;header[9]=6;
        Chunk(file,"IHDR",header);
        using var raw=new MemoryStream();
        using(var z=new ZLibStream(raw,CompressionLevel.Optimal,true))
            for(var y=0;y<height;y++){z.WriteByte(0);z.Write(rgba,y*width*4,width*4);}
        Chunk(file,"IDAT",raw.ToArray());
        Chunk(file,"IEND",[]);
    }

    /// <summary>
    /// X-271 (R37): a PNG read back as RGBA rows, top row first: 8 bits, RGBA or RGB, not interlaced, the five row filters of the
    /// specification (what this writer and common tools write). Anything else throws InvalidDataException, never a wrong picture.
    /// </summary>
    public static (int Width,int Height,byte[] Rgba) Read(string path)=>Read(File.ReadAllBytes(path));

    public static (int Width,int Height,byte[] Rgba) Read(byte[] file)
    {
        if(file.Length<8 || file[0]!=0x89 || file[1]!=0x50 || file[2]!=0x4E || file[3]!=0x47)throw new InvalidDataException("not a PNG file");
        int width=0,height=0,channels=0;
        using var idat=new MemoryStream();
        for(var at=8;at+8<=file.Length;)
        {
            var length=(int)ReadBig(file,at);var type=System.Text.Encoding.ASCII.GetString(file,at+4,4);
            if(length<0 || at+12+length>file.Length)throw new InvalidDataException("PNG chunk past the end of the file");
            if(type=="IHDR")
            {
                width=(int)ReadBig(file,at+8);height=(int)ReadBig(file,at+12);
                var depth=file[at+16];var colour=file[at+17];var interlace=file[at+20];
                if(depth!=8 || interlace!=0 || colour is not (2 or 6))throw new InvalidDataException($"PNG not read here: depth {depth}, colour type {colour}, interlace {interlace}");
                channels=colour==6 ? 4 : 3;
            }
            else if(type=="IDAT")idat.Write(file,at+8,length);
            else if(type=="IEND")break;
            at+=12+length;
        }
        if(width<=0 || height<=0 || channels==0)throw new InvalidDataException("PNG without a header");
        idat.Position=0;
        using var z=new ZLibStream(idat,CompressionMode.Decompress);
        var stride=width*channels;var raw=new byte[(stride+1)*height];
        var read=0;
        while(read<raw.Length){var n=z.Read(raw,read,raw.Length-read);if(n==0)break;read+=n;}
        if(read!=raw.Length)throw new InvalidDataException("PNG data shorter than its size");
        var rows=new byte[stride*height];
        for(var y=0;y<height;y++)
        {
            var filter=raw[y*(stride+1)];var src=y*(stride+1)+1;var dst=y*stride;
            for(var x=0;x<stride;x++)
            {
                int a=x>=channels ? rows[dst+x-channels] : 0,b=y>0 ? rows[dst-stride+x] : 0,c=x>=channels && y>0 ? rows[dst-stride+x-channels] : 0;
                var value=raw[src+x];
                rows[dst+x]=(byte)(filter switch
                {
                    0=>value,
                    1=>value+a,
                    2=>value+b,
                    3=>value+((a+b)>>1),
                    4=>value+Paeth(a,b,c),
                    _=>throw new InvalidDataException($"PNG row filter {filter} unknown"),
                });
            }
        }
        if(channels==4)return (width,height,rows);
        var rgba=new byte[width*height*4];
        for(int p=0,q=0;p<rows.Length;p+=3,q+=4){rgba[q]=rows[p];rgba[q+1]=rows[p+1];rgba[q+2]=rows[p+2];rgba[q+3]=255;}
        return (width,height,rgba);
    }

    private static int Paeth(int a,int b,int c)
    {
        var p=a+b-c;var pa=Math.Abs(p-a);var pb=Math.Abs(p-b);var pc=Math.Abs(p-c);
        return pa<=pb && pa<=pc ? a : pb<=pc ? b : c;
    }

    private static uint ReadBig(byte[] source,int at)=>(uint)(source[at]<<24|source[at+1]<<16|source[at+2]<<8|source[at+3]);

    private static void Chunk(Stream stream,string type,byte[] data)
    {
        var length=new byte[4];WriteBig(length,0,(uint)data.Length);stream.Write(length);
        var body=new byte[4+data.Length];System.Text.Encoding.ASCII.GetBytes(type,0,4,body,0);data.CopyTo(body,4);
        stream.Write(body);
        var crc=new byte[4];WriteBig(crc,0,Crc(body));stream.Write(crc);
    }

    private static void WriteBig(byte[] target,int at,uint value){target[at]=(byte)(value>>24);target[at+1]=(byte)(value>>16);target[at+2]=(byte)(value>>8);target[at+3]=(byte)value;}

    private static uint Crc(byte[] data)
    {
        var crc=0xFFFFFFFFu;
        foreach(var b in data){crc^=b;for(var k=0;k<8;k++)crc=(crc&1)!=0 ? 0xEDB88320u^(crc>>1):crc>>1;}
        return crc^0xFFFFFFFFu;
    }
}
