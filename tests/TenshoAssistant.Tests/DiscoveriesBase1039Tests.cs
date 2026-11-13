using TenshoAssistant.Discoveries;

// X-93 part 2 (0.1039): a Discoveries.json that cannot be read (over the bound, damaged, unknown version) is set aside under a dated name, never overwritten
// by the empty base of the next save; the plugin says it in the chat and on the page, and saves nothing when the file could not even be moved.
internal static class DiscoveriesBase1039Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,11,2,0,0,TimeSpan.Zero);

    private static void InFolder(Action<string> run)
    {
        var folder=Path.Combine(Path.GetTempPath(),"tensho-base-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try{run(Path.Combine(folder,"Discoveries.json"));}finally{try{Directory.Delete(folder,true);}catch(Exception){}}
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discoveries base 1039: missing file read as nothing, nothing set aside",() => InFolder(path =>
        {
            var read=DiscoveryFiles.ReadBase(path,At,(_,_)=>throw new Exception("no report"));
            Check(read.File is null && read.SetAside is null && !read.Unsafe,"missing");
        })),
        ("Discoveries base 1039: a valid base is read and left in place",() => InFolder(path =>
        {
            DiscoveryFiles.Write(path,new DiscoveryFile{Version=DiscoveryFile.CurrentVersion,Facts=[]});
            var read=DiscoveryFiles.ReadBase(path,At,(_,_)=>throw new Exception("no report"));
            Check(read.File is not null && read.SetAside is null && File.Exists(path),"valid base");
        })),
        ("Discoveries base 1039: a damaged base is set aside intact and reported",() => InFolder(path =>
        {
            File.WriteAllText(path,"{\"Version\":2,\"Facts\":[");
            string? reported=null;
            var read=DiscoveryFiles.ReadBase(path,At,(aside,_)=>reported=aside);
            Check(read.File is null && !read.Unsafe && read.SetAside==path+".broken-20261111-020000","dated name: "+read.SetAside);
            Check(reported==read.SetAside && !File.Exists(path) && File.ReadAllText(read.SetAside!)=="{\"Version\":2,\"Facts\":[","kept intact, original name free");
        })),
        ("Discoveries base 1039: over the bound or of an unknown version, set aside too; an earlier set-aside file never replaced",() => InFolder(path =>
        {
            DiscoveryFiles.Write(path,new DiscoveryFile{Version=DiscoveryFile.CurrentVersion,Facts=[]});
            var first=DiscoveryFiles.ReadBase(path,At,(_,_)=>{},max:4);
            Check(first.SetAside==path+".broken-20261111-020000" && !File.Exists(path),"over the bound");
            File.WriteAllText(path,"{\"Version\":999,\"Facts\":[]}");
            var second=DiscoveryFiles.ReadBase(path,At,(_,_)=>{});
            Check(second.SetAside==path+".broken-20261111-020000-2" && File.Exists(first.SetAside!) && File.Exists(second.SetAside!),"second name: "+second.SetAside);
        })),
        ("Discoveries base 1039: wired at load, said in the chat and on the page, no save when blocked",() =>
        {
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Discoveries.cs")); // X-153 (0.1250): the discoveries family
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Discoveries.cs"));
            Check(plugin.Contains("DiscoveryFiles.ReadBase(saved,") && !plugin.Contains("File.Move(saved,saved+\".broken\",true)"),"load reads through ReadBase");
            Check(plugin.Contains("chat.PrintError(\"[Tensho] \"+said)") && plugin.Contains("mainWindow.DiscoveriesWriteBlocked=true"),"chat and block");
            Check(page.Contains("|| DiscoveriesWriteBlocked ||") && page.Contains("\"DiscoveriesUi.BaseSetAside\""),"save blocked, page warning");
        }),
    ];
}
