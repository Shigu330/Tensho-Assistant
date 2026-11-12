using System.Reflection;
using System.Text.RegularExpressions;
using TenshoAssistant.Desktop;

// 0.1157 (X-151, GPT review of 11/11): the members of the plugin the desktop application reaches by name are a written contract
// (DesktopContract), resolved by Verify's headless run against the plugin it packs (a missing one fails it). Here: the list is complete
// (every name the application's host reflects on is in it), the resolver finds a member renamed or with other parameters, and each
// MainWindow member of the list is declared in the plugin's source with those parameters.
internal static class DesktopContract1157Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly string[] HostFiles=["Host.cs","Headless.cs","Program.cs","StatusBar.cs","Ui.cs"];

    // Stand-ins of the plugin's types, to see the resolver at work on real reflection (the plugin's DLL is not loaded by the tests).
    private sealed class Window
    {
#pragma warning disable CS0169, CS0414, IDE0051
        private int page;
        private bool DesktopDueAlarms(DateTimeOffset now,bool front)=>front;
        internal static void Copy(string a,string b){}
#pragma warning restore CS0169, CS0414, IDE0051
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Desktop contract 1157: every name the application's host reaches by reflection is in the contract",() =>
        {
            var names=DesktopContract.Members.Select(member=>member.Name).ToHashSet(StringComparer.Ordinal);
            var types=DesktopContract.Members.Select(member=>member.Type).ToHashSet(StringComparer.Ordinal);
            var found=new List<string>();
            foreach(var file in HostFiles)
            {
                var text=Read("src/TenshoAssistant.Desktop/"+file);
                foreach(Match match in Regex.Matches(text,"Get(?:Method|Field|Property|NestedType)\\(\"([^\"]+)\""))found.Add(file+": "+match.Groups[1].Value);
                foreach(Match match in Regex.Matches(text,"GetType\\(\"(TenshoAssistant\\.[^\"]+)\""))
                    Check(types.Contains(match.Groups[1].Value),$"{file}: type {match.Groups[1].Value} not in DesktopContract.Members");
            }
            Check(found.Count>=20,"the scan finds the host's lookups: "+found.Count);
            var missing=found.Where(item=>!names.Contains(item[(item.IndexOf(": ",StringComparison.Ordinal)+2)..]) && !item.EndsWith(": Page",StringComparison.Ordinal)).ToArray();
            // « Page » is both the nested enum (a type of the list) and the entry's property (a member of the list).
            Check(names.Contains("Page") && types.Contains("TenshoAssistant.MainWindow+Page"),"Page listed");
            Check(missing.Length==0,"not in DesktopContract.Members: "+string.Join(", ",missing));
        }),
        ("Desktop contract 1157: the resolver finds a missing type, a renamed member, other parameters, a static asked as instance",() =>
        {
            var window=typeof(Window);
            Type? Of(string name)=>name=="TenshoAssistant.MainWindow" ? window : null;
            var all=DesktopContract.Missing(Of);
            Check(all.Any(item=>item.StartsWith("TenshoAssistant.Configuration (",StringComparison.Ordinal)),"a missing type is said");
            Check(!all.Any(item=>item.StartsWith("TenshoAssistant.MainWindow.page ",StringComparison.Ordinal)),"a private field found");
            Check(!all.Any(item=>item.StartsWith("TenshoAssistant.MainWindow.DesktopDueAlarms ",StringComparison.Ordinal)),"a private method with its parameters found");
            Check(all.Any(item=>item.StartsWith("TenshoAssistant.MainWindow.DesktopLinkNotices ",StringComparison.Ordinal)),"a missing method is said");
            var member=new DesktopContract.Member("X",DesktopContract.Kind.Method,"DesktopDueAlarms",false,["System.DateTimeOffset"],"test");
            Check(typeof(DesktopContract).GetMethod("Has",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[window,member]) is false,"other parameters: missing");
            var asInstance=new DesktopContract.Member("X",DesktopContract.Kind.Method,"Copy",false,null,"test");
            Check(typeof(DesktopContract).GetMethod("Has",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,[window,asInstance]) is false,"a static method asked as an instance one: missing");
        }),
        ("Desktop contract 1157: each MainWindow method of the contract is declared in the plugin with those parameters; checked in Verify and at start",() =>
        {
            var source=string.Join("\n",Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"MainWindow*.cs").Select(Read));
            string Short(string type)=>type switch
            {
                "System.String"=>"string","System.Boolean"=>"bool","System.DateTimeOffset"=>"DateTimeOffset","System.String[]"=>"string[]",
                "System.UInt32[]"=>"uint[]","System.Int64[]"=>"long[]","System.Action`2[System.Exception,System.String]"=>"Action<Exception,string>",_=>type,
            };
            foreach(var member in DesktopContract.Members.Where(member=>member.Type=="TenshoAssistant.MainWindow" && member.Kind==DesktopContract.Kind.Method && member.Parameters is not null))
            {
                var pattern=$"\\b{Regex.Escape(member.Name)}\\(\\s*"+string.Join("\\s*,\\s*",member.Parameters!.Select(p=>Regex.Escape(Short(p))+"\\s+\\w+"))+"\\s*\\)";
                Check(Regex.IsMatch(source,pattern),$"MainWindow.{member.Name}({string.Join(",",member.Parameters!.Select(Short))}) declared");
            }
            var headless=Read("src/TenshoAssistant.Desktop/Headless.cs");
            Check(headless.Contains("var broken=DesktopContract.Missing(host.Plugin);") && headless.Contains("if(broken.Count>0){report.Fail(\"contract: the plugin lacks \"+string.Join(\"; \",broken));return 1;}"),"Verify's headless run fails on a missing member");
            Check(headless.IndexOf("DesktopContract.Missing(host.Plugin)",StringComparison.Ordinal)<headless.IndexOf("var worker=new Thread(",StringComparison.Ordinal),"before any page is drawn");
            Check(Read("src/TenshoAssistant.Desktop/Program.cs").Contains("foreach(var missing in DesktopContract.Missing(host.Plugin))Console.WriteLine(\"Contract: the plugin lacks \"+missing+\"; that part is off\");"),"said at start");
        }),
    ];
}
