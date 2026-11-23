using System.IO.Compression;
using System.Text;
using TenshoAssistant.Caching;

// X-856 (review 0.1860, M1). Behaviour tests (R28.14) on real zips: an entry whose bytes were changed in silence, at the right length, was read
// as sound (.NET's zip reader does not check the CRC-32) and copied with a new, valid CRC: the damage could no longer be seen by any zip tool,
// and restoring the cache would give a wrong value. Now its CRC is checked against its bytes and the entry is left out, as other damage is.
internal static class DamagedCacheZip1862Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Folder()=>Path.Combine(Path.GetTempPath(),"tensho-1862-zip-"+Guid.NewGuid().ToString("N"));
    private static void Clean(string folder){try{if(Directory.Exists(folder))Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}

    private const string Board="{\"Schema\":1,\"Family\":\"market-board\",\"Scope\":\"Odin:5057\",\"Version\":\"universalis-board-v1\",\"RecordedAt\":\"2026-11-01T12:00:00+00:00\",\"Value\":{\"listings\":[{\"retainerName\":\"Sellerofthings\"}]}}";
    private const string Notes="kept as it is";
    private static readonly string Prices="{\"Schema\":1,\"Family\":\"item-prices\",\"Scope\":\"Odin\",\"Version\":\"v2\",\"Value\":\""+new string('7',4000)+"\"}";

    // A zip of the given entries; « stored » ones are written without compression, so that one byte of their data is one byte of their text.
    private static string Zip(string backups,string stamp,params (string Name,string Text,bool Stored)[] entries)
    {
        Directory.CreateDirectory(backups);
        var zip=Path.Combine(backups,"backup-"+stamp+"-cache.zip");
        using(var archive=ZipFile.Open(zip,ZipArchiveMode.Create))
            foreach(var (name,text,stored) in entries)
            {
                using var stream=archive.CreateEntry(name,stored ? CompressionLevel.NoCompression:CompressionLevel.Optimal).Open();
                stream.Write(Encoding.UTF8.GetBytes(text));
            }
        return zip;
    }

    // One byte in the middle of an entry's data changed (its sizes and CRC left as they were): damage at the right length.
    private static void ChangeOneByte(string zip,string name)
    {
        var bytes=File.ReadAllBytes(zip);
        for(var at=0;at+46<bytes.Length;at++)
        {
            if(bytes[at]!=0x50 || bytes[at+1]!=0x4b || bytes[at+2]!=0x01 || bytes[at+3]!=0x02)continue;
            var length=BitConverter.ToUInt16(bytes,at+28);
            if(Encoding.UTF8.GetString(bytes,at+46,length)!=name)continue;
            var size=(int)BitConverter.ToUInt32(bytes,at+20);
            var local=(int)BitConverter.ToUInt32(bytes,at+42);
            var data=local+30+BitConverter.ToUInt16(bytes,local+26)+BitConverter.ToUInt16(bytes,local+28);
            bytes[data+size/2]^=0x01;
            File.WriteAllBytes(zip,bytes);
            return;
        }
        throw new Exception("no entry "+name);
    }

    // What .NET's reader gives of each entry at its stated length (no CRC check), and whether its CRC matches.
    private static Dictionary<string,(string Text,bool Sound)> Read(string zip)
    {
        using var archive=ZipFile.OpenRead(zip);
        var read=new Dictionary<string,(string,bool)>(StringComparer.Ordinal);
        foreach(var entry in archive.Entries)
        {
            using var stream=entry.Open();using var copy=new MemoryStream();stream.CopyTo(copy);
            Check(copy.Length==entry.Length,entry.FullName+": read whole");
            read[entry.FullName]=(Encoding.UTF8.GetString(copy.ToArray()),RetiredCopies.Crc32(copy.ToArray())==entry.Crc32);
        }
        return read;
    }

    public static readonly (string Name,Action Run)[] Cases=
    [
        ("X-856: the zip's CRC-32 as any zip tool computes it", () =>
        {
            Check(RetiredCopies.Crc32(Encoding.ASCII.GetBytes("123456789"))==0xCBF43926u,"the standard check value");
            Check(RetiredCopies.Crc32(ReadOnlySpan<byte>.Empty)==0u,"nothing: 0");
            var folder=Folder();
            try
            {
                var zip=Zip(Path.Combine(folder,"Backups"),"20261101-120000",("prices.json",Prices,false),("notes.txt",Notes,true));
                Check(Read(zip).Values.All(entry=>entry.Sound),"each sound entry matches its CRC");
            }
            finally{Clean(folder);}
        }),
        ("X-856: an entry changed in silence at the right length is left out, never copied with a new CRC; the rest is kept", () =>
        {
            var folder=Folder();
            try
            {
                var backups=Path.Combine(folder,"Backups");
                var zip=Zip(backups,"20261101-120000",("board.json",Board,false),("prices.json",Prices,true),("notes.txt",Notes,true),("other.txt",Notes+" too",false));
                ChangeOneByte(zip,"prices.json");
                ChangeOneByte(zip,"notes.txt");
                var before=Read(zip);
                Check(before["prices.json"].Text!=Prices && before["prices.json"].Text.Length==Prices.Length && !before["prices.json"].Sound,"the damage is silent: read whole, at its length, a wrong value");
                Check(before["notes.txt"].Text!=Notes && !before["notes.txt"].Sound,"a non-.json entry changed in silence too");
                Check(RetiredCopies.Clean(backups,out var damaged,out var dropped)==1,"the zip is written again without its v1 board");
                Check(damaged==0 && dropped==2,$"the two changed entries left out: damaged {damaged}, dropped {dropped}");
                var after=Read(zip);
                Check(!after.ContainsKey("board.json") && !after.ContainsKey("prices.json") && !after.ContainsKey("notes.txt"),"board and changed entries out");
                Check(after.TryGetValue("other.txt",out var other) && other.Text==Notes+" too" && after.Values.All(entry=>entry.Sound),"the sound rest kept, its CRC true");
                Check(Directory.GetFiles(backups,"*.tmp").Length==0,"no passing file left");
            }
            finally{Clean(folder);}
        }),
        ("X-856: a zip with nothing to remove is left byte for byte, its changed entry still visible to a zip tool", () =>
        {
            var folder=Folder();
            try
            {
                var backups=Path.Combine(folder,"Backups");
                var zip=Zip(backups,"20261102-120000",("prices.json",Prices,true),("notes.txt",Notes,false));
                ChangeOneByte(zip,"prices.json");
                var bytes=File.ReadAllBytes(zip);
                Check(RetiredCopies.Clean(backups,out _,out var dropped)==0 && dropped==0,"nothing written again");
                Check(File.ReadAllBytes(zip).SequenceEqual(bytes) && !Read(zip)["prices.json"].Sound,"left byte for byte, its CRC still wrong");
            }
            finally{Clean(folder);}
        }),
    ];
}
