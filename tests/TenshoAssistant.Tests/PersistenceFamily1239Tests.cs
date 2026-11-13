// 0.1239 (X-153, second slice, GPT review of 11/11): the persistence family out of Plugin.cs, after the desktop family (0.1225). The bank
// and its bridge to the game's inventory have exactly one install path and one release path across every Plugin*.cs file, both in
// Plugin.Persistence.cs, inside the plugin's registrations (released in reverse order: the bridge before the bank it writes to); the player's
// data file is made there once; Plugin.cs calls the family once, before the main window, and creates none of them. Behaviour: the
// registrations release in reverse order and a failing release does not stop the others.
using System.Text.RegularExpressions;
using TenshoAssistant;

internal static class PersistenceFamily1239Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static (string Name,string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"Plugin*.cs")
        .Select(path => (Path.GetFileName(path),File.ReadAllText(path).Replace("\r\n","\n"))).ToArray();

    private static int Count(string text,string pattern) => Regex.Matches(text,pattern).Count;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Persistence family 1239: one install and one release path for each service, in Plugin.Persistence.cs only",() =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Persistence.cs").Text;
            var paths = new (string Service,string Install,string Release)[]
            {
                ("bank",@"bank=new InventoryBankService\(",@"bank\?\.Dispose\(\);bank=null;"),
                ("bank bridge",@"bridge=new BankInventoryBridge\(",@"bridge\?\.Dispose\(\);bridge=null;"),
            };
            foreach (var (service,install,release) in paths)
            {
                Check(files.Sum(file => Count(file.Text,install)) == 1 && files.Sum(file => Count(file.Text,release)) == 1,service + ": one install and one release path");
                Check(Count(home,install) == 1 && Count(home,release) == 1,service + ": in its family's file");
            }
            Check(Count(home,@"registrations\.Register\(") == 2,"both through the plugin's registrations");
            Check(home.IndexOf("bank=new InventoryBankService(",StringComparison.Ordinal) < home.IndexOf("bridge=new BankInventoryBridge(",StringComparison.Ordinal),"the bank first: released after its bridge");
            Check(files.Sum(file => Count(file.Text,@"new TenshoDataFile\(")) == 1 && Count(home,@"var tenshoData=new TenshoDataFile\(") == 1,"the player's data file made once, in the family");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            var call = plugin.IndexOf("var (tenshoData,bank,bankBridge)=RegisterPersistence(pluginInterface,configuration,data,inventory,gameGui,player,client,framework);",StringComparison.Ordinal);
            Check(call > 0 && Count(plugin,@"RegisterPersistence\(") == 1,"Plugin.cs calls the family once");
            Check(call < plugin.IndexOf("mainWindow = new MainWindow(",StringComparison.Ordinal),"before the main window, which shows them");
            Check(!plugin.Contains("new InventoryBankService(",StringComparison.Ordinal) && !plugin.Contains("new BankInventoryBridge(",StringComparison.Ordinal),"and creates none of them");
            Check(home.Contains("public sealed partial class Plugin",StringComparison.Ordinal),"one class in three files");
        }),
        ("Persistence family 1239: the registrations release in reverse order, a failing release does not stop the others",() =>
        {
            var order = new List<string>();
            var scope = new RegistrationScope();
            scope.Register(() => order.Add("+bank"),() => order.Add("-bank"));
            scope.Register(() => order.Add("+bridge"),() => throw new InvalidOperationException("bridge"));
            scope.Register(() => order.Add("+other"),() => order.Add("-other"));
            var thrown = false;
            try { scope.Dispose(); } catch (AggregateException error) { thrown = error.InnerExceptions.Single().Message == "bridge"; }
            Check(thrown,"the failure is thrown at the end");
            Check(order.SequenceEqual(["+bank","+bridge","+other","-other","-bank"]),"reverse order, the bank released after its bridge's failure: " + string.Join(",",order));
        }),
    ];
}
