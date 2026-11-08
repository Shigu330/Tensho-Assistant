using TenshoAssistant;

// X-76 2b H10 (0.923): one atomic file write: replaces the old file, no temporary file left, even after a failure; no hand-written « .tmp »
// write left in the saved-file writers; an unreadable GitHub exchange file is kept aside.
internal static class AtomicFile923Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Atomic file 923: write, replace, nothing left behind, writers use it",() =>
        {
            var folder=Path.Combine(Path.GetTempPath(),"tensho-atomic-"+Guid.NewGuid().ToString("N"));
            try
            {
                var path=Path.Combine(folder,"sub","a.json");
                AtomicFile.Write(path,"one");AtomicFile.Write(path,"deux é");
                Check(File.ReadAllText(path)=="deux é" && File.ReadAllBytes(path)[0]!=0xEF,"replaced, no byte order mark");
                Directory.CreateDirectory(Path.Combine(folder,"blocked.json"));
                var failed=false;
                try{AtomicFile.Write(Path.Combine(folder,"blocked.json"),"x");}catch(Exception){failed=true;}
                Check(failed && !Directory.EnumerateFiles(folder,"*.tmp",SearchOption.AllDirectories).Any(),"no temporary file left after a failure");
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
            foreach(var file in new[]{"Craft/CraftLists.cs","FreeCompany/VoyageHistory.cs","Inventory/GilJournal.cs","MyTools/ToolTemplateStore.cs","Character/CharacterStatus.cs","Discoveries/DiscoveriesModel.cs","Market/SeenPrices.cs","Discoveries/GitHubExchange.cs"})
            {
                var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
                Check(code.Contains("AtomicFile.Write(") && !code.Contains("\".tmp\""),file);
            }
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/GitHubExchange.cs")).Contains("File.Move(MetaPath,MetaPath+\".broken\",true)"),"unreadable file kept aside");
        }),
    ];
}
