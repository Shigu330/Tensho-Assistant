using System.IO.Compression;
using System.Text;
using TenshoAssistant.Caching;

// X-854 (review 0.1858, M1). Behaviour tests (R28.14) on real zips: a « cache » zip whose damage shows only when it is copied (an entry broken
// after the head the first reading takes, an entry that is not .json) was counted damaged as a whole by X-851 and the mark written: it kept its
// v1 market boards (other players' retainer names) for ever, still readable by any zip tool. Now it is written again without them, the entries
// nothing can read are left out, and the readable rest is kept byte for byte.
internal static class DamagedCacheZip1859Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Folder()=>Path.Combine(Path.GetTempPath(),"tensho-1859-zip-"+Guid.NewGuid().ToString("N"));
    private static void Clean(string folder){try{if(Directory.Exists(folder))Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}

    private const string Board="{\"Schema\":1,\"Family\":\"market-board\",\"Scope\":\"Odin:5057\",\"Version\":\"universalis-board-v1\",\"RecordedAt\":\"2026-11-01T12:00:00+00:00\",\"Value\":{\"listings\":[{\"retainerName\":\"Sellerofthings\"}]}}";
    private const string Notes="kept as it is";

    // A long cached file with a sound head (another family, not retired) and pseudo-random letters after it, so that its deflated data is long.
    private static string Long()
    {
        var random=new Random(1859);
        var text=new StringBuilder("{\"Schema\":1,\"Family\":\"item-prices\",\"Scope\":\"Odin\",\"Version\":\"v2\",\"Value\":\"");
        for(var index=0;index<300_000;index++)text.Append((char)('a'+random.Next(26)));
        return text.Append("\"}").ToString();
    }

    // A zip of the given entries (name, text), in that order, as BackupZip writes them (deflate).
    private static string Zip(string backups,string stamp,params (string Name,string Text)[] entries)
    {
        Directory.CreateDirectory(backups);
        var zip=Path.Combine(backups,"backup-"+stamp+"-cache.zip");
        using(var archive=ZipFile.Open(zip,ZipArchiveMode.Create))
            foreach(var (name,text) in entries)
            {
                using var stream=archive.CreateEntry(name,CompressionLevel.Optimal).Open();
                stream.Write(Encoding.UTF8.GetBytes(text));
            }
        return zip;
    }

    // The central record of an entry: its offset in the file (the record), its compressed size and its local header's offset.
    private static (int Central,int Size,int Local) Record(byte[] bytes,string name)
    {
        for(var at=0;at+46<bytes.Length;at++)
        {
            if(bytes[at]!=0x50 || bytes[at+1]!=0x4b || bytes[at+2]!=0x01 || bytes[at+3]!=0x02)continue;
            var length=BitConverter.ToUInt16(bytes,at+28);
            if(Encoding.UTF8.GetString(bytes,at+46,length)==name)return (at,(int)BitConverter.ToUInt32(bytes,at+20),(int)BitConverter.ToUInt32(bytes,at+42));
        }
        throw new Exception("no entry "+name);
    }

    // One entry's compression method (local and central headers) set to 14 (LZMA): no reader of .NET can open that entry.
    private static void UnknownMethod(string zip,string name)
    {
        var bytes=File.ReadAllBytes(zip);
        var (central,_,local)=Record(bytes,name);
        bytes[local+8]=14;bytes[local+9]=0;bytes[central+10]=14;bytes[central+11]=0;
        File.WriteAllBytes(zip,bytes);
    }

    // One entry's deflated data cut in its middle (its compressed size halved in both headers, as a copy cut short leaves it), far past the
    // 8 KB the first reading takes; the entries after it stay where the central directory says.
    private static void BreakMiddle(string zip,string name)
    {
        var bytes=File.ReadAllBytes(zip);
        var (central,size,local)=Record(bytes,name);
        Check(size>40_000,"the long entry's data is long: "+size);
        BitConverter.GetBytes((uint)(size/2)).CopyTo(bytes,central+20);
        BitConverter.GetBytes((uint)(size/2)).CopyTo(bytes,local+18);
        File.WriteAllBytes(zip,bytes);
    }

    // What any reader gets of each entry: its text, or null when it cannot be read whole.
    private static Dictionary<string,string?> Read(string zip)
    {
        using var archive=ZipFile.OpenRead(zip);
        var read=new Dictionary<string,string?>(StringComparer.Ordinal);
        foreach(var entry in archive.Entries)
        {
            try
            {
                using var stream=entry.Open();using var copy=new MemoryStream();stream.CopyTo(copy);
                read[entry.FullName]=copy.Length==entry.Length ? Encoding.UTF8.GetString(copy.ToArray()):null;
            }
            catch(Exception error) when(error is InvalidDataException or NotSupportedException){read[entry.FullName]=null;}
        }
        return read;
    }

    private static void After(string backups,string zip,string broken)
    {
        var before=Read(zip);
        Check(before.TryGetValue(broken,out var unread) && unread is null,"the damage is real: "+broken+" cannot be read whole");
        Check(before["board.json"]?.Contains("Sellerofthings",StringComparison.Ordinal)==true,"and the v1 board is readable by any zip tool");
        Check(RetiredCopies.Clean(backups,out var damaged,out var dropped)==1,"the zip is written again");
        Check(damaged==0 && dropped==1,$"one entry left out, no zip left as it is: damaged {damaged}, dropped {dropped}");
        var after=Read(zip);
        Check(!after.ContainsKey("board.json") && !after.Values.Any(text=>text?.Contains("Sellerofthings",StringComparison.Ordinal)==true),"no other player's name left");
        Check(!after.ContainsKey(broken),"the broken entry is left out");
        Check(after.TryGetValue("notes.txt",out var notes) && notes==Notes && after.Values.All(text=>text is not null),"the readable rest is kept, and readable");
        Check(File.Exists(Path.Combine(backups,RetiredCopies.MarkName)),"the pass ends with its mark");
        Check(Directory.GetFiles(backups,"*.tmp").Length==0,"no passing file left");
    }

    public static readonly (string Name,Action Run)[] Cases=
    [
        ("X-854: a cache zip broken in an entry far past its head is written again without its v1 board; the broken entry is left out, the rest kept",()=>
        {
            var folder=Folder();
            try
            {
                var backups=Path.Combine(folder,"Backups");
                var zip=Zip(backups,"20261101-120000",("board.json",Board),("long.json",Long()),("notes.txt",Notes));
                BreakMiddle(zip,"long.json");
                After(backups,zip,"long.json");
            }
            finally{Clean(folder);}
        }),
        ("X-854: a cache zip with an entry that is not .json and cannot be read is written again without its v1 board",()=>
        {
            var folder=Folder();
            try
            {
                var backups=Path.Combine(folder,"Backups");
                var zip=Zip(backups,"20261101-120000",("board.json",Board),("picture.bin",new string('x',5000)),("notes.txt",Notes));
                UnknownMethod(zip,"picture.bin");
                After(backups,zip,"picture.bin");
            }
            finally{Clean(folder);}
        }),
        ("X-854: a .json entry unreadable from its head does not keep the v1 board beside it; a zip with nothing to remove is left byte for byte",()=>
        {
            var folder=Folder();
            try
            {
                var backups=Path.Combine(folder,"Backups");
                var zip=Zip(backups,"20261101-120000",("board.json",Board),("other.json",Long()),("notes.txt",Notes));
                UnknownMethod(zip,"other.json");
                var alone=Zip(backups,"20261102-120000",("other.json",Long()),("notes.txt",Notes));
                UnknownMethod(alone,"other.json");
                var aloneBytes=File.ReadAllBytes(alone);
                Check(RetiredCopies.Clean(backups,out var damaged,out var dropped)==1,"the zip with a board is written again");
                Check(dropped==1 && damaged==1,$"its broken entry left out; the other zip counted as left as it is: damaged {damaged}, dropped {dropped}");
                var after=Read(zip);
                Check(!after.ContainsKey("board.json") && !after.ContainsKey("other.json") && after["notes.txt"]==Notes,"board and broken entry out, the rest kept");
                Check(File.ReadAllBytes(alone).SequenceEqual(aloneBytes),"nothing to remove: left byte for byte");
            }
            finally{Clean(folder);}
        }),
    ];
}
