using System.Text;

namespace TenshoAssistant;

/// <summary>
/// X-76 H10 (0.923): one way to write a saved file: a temporary file with a unique name next to it, then moved over the old one, so a crash or
/// a full disk never leaves half a file; the temporary file is deleted in any case (some writers used a fixed « .tmp » name, two writers at
/// once could clash, and a failed write left it behind).
/// </summary>
public static class AtomicFile
{
    public static void Write(string path,byte[] bytes)
    {
        var folder=Path.GetDirectoryName(path);
        if(!string.IsNullOrEmpty(folder))Directory.CreateDirectory(folder);
        var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            File.WriteAllBytes(temporary,bytes);
            File.Move(temporary,path,true);
        }
        finally
        {
            try{if(File.Exists(temporary))File.Delete(temporary);}
            catch(IOException){}
            catch(UnauthorizedAccessException){}
        }
    }

    /// <summary>Text in UTF-8 without a byte order mark (as File.WriteAllText).</summary>
    public static void Write(string path,string text)=>Write(path,new UTF8Encoding(false).GetBytes(text));
}
