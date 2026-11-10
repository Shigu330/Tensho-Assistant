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
