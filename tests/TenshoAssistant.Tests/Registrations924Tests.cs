using TenshoAssistant;

// X-76 2b H10 (0.924): watchers with several subscriptions own them through RegistrationScope (a failure part-way undoes the ones made).
internal static class Registrations924Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Registrations 924: a failed subscription undoes the earlier ones; five watchers use the scope",() =>
        {
            var log=new List<string>();
            var scope=new RegistrationScope();
            try
            {
                scope.Register(()=>log.Add("a+"),()=>log.Add("a-"));
                scope.Register(()=>log.Add("b+"),()=>log.Add("b-"));
                scope.Register(()=>throw new InvalidOperationException("c"),()=>log.Add("c-"));
            }
            catch(InvalidOperationException){scope.Dispose();}
            Check(string.Join(",",log)=="a+,b+,b-,a-","undone in reverse order, the failed one not undone: "+string.Join(",",log));
            foreach(var file in new[]{"Collections/CollectionReader.Dalamud.cs","Maps/ChatMapLinkWatcher.Dalamud.cs","Items/ItemContextMenuBridge.Dalamud.cs","Discoveries/ItemDiscoveryWatcher.Dalamud.cs","Diagnostics/ReadingCapture.Dalamud.cs"})
            {
                var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
                Check(code.Contains("RegistrationScope registrations") && code.Contains("catch{registrations.Dispose();throw;}") && code.Contains("registrations.Dispose();"),file);
            }
        }),
    ];
}
