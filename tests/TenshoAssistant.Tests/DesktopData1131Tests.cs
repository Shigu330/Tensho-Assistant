using System.Security.Cryptography;
using TenshoAssistant.Desktop;
using TenshoAssistant.Link;

// 0.1131 (X-119 step 2 part 2, X-151): the desktop application reads the plugin's saved data from its own copy. Behaviour test (R28,
// concurrency and data safety): a real plugin folder with a file held open for writing by « the plugin », copied while held; the
// plugin's folder is never changed; a file gone from it goes from the copy; an overlapping mirror is refused; a too large file is not
// copied. The stores are built by the plugin's own MainWindow.UseDesktopData, the one name the application calls.
internal static class DesktopData1131Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    private static Dictionary<string,(string Hash,DateTime Written)> Snapshot(string folder)
        => Directory.GetFiles(folder,"*",SearchOption.AllDirectories).ToDictionary(path=>Path.GetRelativePath(folder,path),
            path=>(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),File.GetLastWriteTimeUtc(path)));

    public static readonly (string Name, Action Run)[] Cases =
    [
        ("Desktop data: the plugin's folder is copied, never changed, even while the plugin writes a file",() =>
        {
            var root=Path.Combine(Path.GetTempPath(),"tensho-1131-"+Guid.NewGuid().ToString("N"));
            var source=Path.Combine(root,"pluginConfigs","TenshoAssistant");var mirror=Path.Combine(root,"Desktop","mirror");
            Directory.CreateDirectory(Path.Combine(source,"InventoryBank"));
            try
            {
                File.WriteAllText(Path.Combine(source,"InventoryBank","1001.json"),"{\"a\":1}");
                File.WriteAllText(Path.Combine(source,"InventoryBank","1002.json"),"{\"b\":2}");
                File.WriteAllText(Path.Combine(source,"InventoryBank.json.migrated"),"old");
                File.WriteAllText(Path.Combine(source,"CraftLists.json"),"{\"lists\":[]}");
                File.WriteAllText(Path.Combine(source,"TenshoAssistant.json"),"{\"secret\":\"x\"}");
                var before=Snapshot(source);
                (int Copied,IReadOnlyList<string> Problems) bank;
                // « The plugin » holds a bank file open for writing (it lets others read, as AtomicFile's writers do).
                using(var held=new FileStream(Path.Combine(source,"InventoryBank","1002.json"),FileMode.Open,FileAccess.ReadWrite,FileShare.Read))
                    bank=DesktopMirror.Copy(source,mirror,"bank");
                Check(bank.Copied==2 && bank.Problems.Count==0,"two bank files copied while one is held: "+string.Join("; ",bank.Problems));
                Check(File.ReadAllText(Path.Combine(mirror,"InventoryBank","1002.json"))=="{\"b\":2}","the held file's content");
                var lists=DesktopMirror.Copy(source,mirror,"craft-lists");
                Check(lists.Copied==1 && File.Exists(Path.Combine(mirror,"CraftLists.json")),"a file category");
                Check(!File.Exists(Path.Combine(mirror,"TenshoAssistant.json")) && !File.Exists(Path.Combine(mirror,"InventoryBank.json.migrated")),"only the category's own files, never the settings");
                var after=Snapshot(source);
                Check(before.Count==after.Count && before.All(pair=>after.TryGetValue(pair.Key,out var now) && now==pair.Value),"the plugin's folder is unchanged");
                File.Delete(Path.Combine(source,"InventoryBank","1001.json"));
                DesktopMirror.Copy(source,mirror,"bank");
                Check(!File.Exists(Path.Combine(mirror,"InventoryBank","1001.json")) && File.Exists(Path.Combine(mirror,"InventoryBank","1002.json")),"a character gone from the plugin goes from the copy");
                Check(Directory.GetFiles(mirror,"*.tmp",SearchOption.AllDirectories).Length==0,"no temporary file left");
                var inside=DesktopMirror.Copy(source,Path.Combine(source,"mirror"),"bank");
                Check(inside.Copied==0 && inside.Problems.Count==1 && !Directory.Exists(Path.Combine(source,"mirror")),"a mirror inside the plugin's folder is refused, nothing written there");
                using(var big=new FileStream(Path.Combine(source,"MyTools.json"),FileMode.Create)){big.SetLength(DesktopMirror.MaxFile+1);}
                var tools=DesktopMirror.Copy(source,mirror,"my-tools");
                Check(tools.Copied==0 && tools.Problems.Count==1 && !File.Exists(Path.Combine(mirror,"MyTools.json")),"a too large file is not copied");
            }
            finally{Directory.Delete(root,true);}
        }),
        ("Desktop data: one entry in the plugin for the stores, the categories from the link's closed list",() =>
        {
            Check(LocalLinkProtocol.DesktopCategories.All(category=>LocalLinkProtocol.Categories.Values.Contains(category)) && LocalLinkProtocol.DesktopCategories.Count==8,"desktop categories are link categories (0.1147: « data »; 0.1151: « characters », « duties »)");
            foreach(var category in LocalLinkProtocol.DesktopCategories)Check(DesktopMirror.Stems(category).Count>0,"stems of "+category);
            var data=Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            Check(data.Contains("internal void UseDesktopData(string folder,string category,Action<Exception,string> warn)"),"the entry's signature");
            foreach(var category in LocalLinkProtocol.DesktopCategories)Check(data.Contains($"case \"{category}\":"),"built: "+category);
            var host=Read("src/TenshoAssistant.Desktop/Host.cs");
            Check(host.Contains("GetMethod(\"UseDesktopData\",BindingFlags.Instance|BindingFlags.NonPublic,[typeof(string),typeof(string),typeof(Action<Exception,string>)])"),"the app calls that entry, by that signature");
            var program=Read("src/TenshoAssistant.Desktop/Program.cs");
            Check(program.IndexOf("status.ReadData();",StringComparison.Ordinal)>program.IndexOf("if(headless)return Headless.Run(",StringComparison.Ordinal),"never in a headless run (Verify does not copy the user's files)");
        }),
    ];
}
