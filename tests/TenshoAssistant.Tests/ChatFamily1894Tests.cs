// 0.1894 (X-153, GPT review of 11/11, accepted by the user): the rest of the chat family out of Plugin.cs, after its chat window reader and
// map links (0.1443). The game chat's layout probe and hider, the chat window's player actions and the link watch have exactly one install
// path and one release path across every Plugin*.cs file, all in Plugin.Chat.cs, inside the plugin's registrations; Plugin.cs calls each
// group where it stood and creates none. Pairing (X-885 M2, 0.1906): each service's install and its release given to one same registration.
using System.Text.RegularExpressions;
using TenshoAssistant;

internal static class ChatFamily1894Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static (string Name, string Text)[] PluginFiles() => Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "Plugin*.cs")
        .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n"))).ToArray();

    private static int Count(string text, string pattern) => Regex.Matches(text, pattern).Count;

    private static readonly (string Service, string Install, string Release)[] Paths =
    [
        ("game chat layout probe", @"chatLayoutProbe=new Chat\.ChatLogLayoutProbe\(", @"chatLayoutProbe\?\.Dispose\(\)"),
        ("game chat hider", @"chatHider=new Chat\.GameChatHider\(", @"chatHider\?\.Dispose\(\)"),
        ("chat player actions", @"chatPlayers=new Chat\.ChatPlayerActions\(", @"chatPlayers\?\.Dispose\(\)"),
        ("chat link watch", @"chatLinkWatch=new Chat\.ChatLinkWatch\(", @"chatLinkWatch\?\.Dispose\(\)"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat family 1894: one install and one release path for the layout probe, the hider, the player actions and the link watch, in Plugin.Chat.cs", () =>
        {
            var files = PluginFiles();
            var home = files.Single(file => file.Name == "Plugin.Chat.cs").Text;
            var last = -1;
            foreach (var (service, install, release) in Paths)
            {
                Check(files.Sum(file => Count(file.Text, install)) == 1 && files.Sum(file => Count(file.Text, release)) == 1, service + ": one install and one release path");
                Check(Count(home, install) == 1 && Count(home, release) == 1, service + ": in its family's file");
                var at = Regex.Match(home, install).Index;
                Check(at > last, service + ": in the order of before");
                last = at;
            }
            Check(Count(home, @"registrations\.Register\(") == 3 + Paths.Length, "every one through the plugin's registrations (with the reader's three)");
            var plugin = files.Single(file => file.Name == "Plugin.cs").Text;
            Check(!Regex.IsMatch(plugin, @"new Chat\.(ChatLogLayoutProbe|GameChatHider|ChatPlayerActions|ChatLinkWatch)\("), "Plugin.cs creates none of them");
        }),
        ("Chat family 1894: Plugin.cs calls each group once, where it stood, in the order of before", () =>
        {
            var plugin = PluginFiles().Single(file => file.Name == "Plugin.cs").Text;
            int At(string piece) { var at = plugin.IndexOf(piece, StringComparison.Ordinal); Check(at >= 0, "not found: " + piece); return at; }
            var game = At("RegisterGameChat(configuration,framework,gameGui);");
            var players = At("RegisterChatPlayers(configuration,interop,framework,client,objects,targets,partyList,data,chatSenders,gameConfig,seStrings,contextMenus);");
            Check(Count(plugin, @"RegisterGameChat\(") == 1 && Count(plugin, @"RegisterChatPlayers\(") == 1, "each called once");
            Check(At("RegisterBeastBoard(configuration,data,framework,gameGui,objects);") < game && game < At("RegisterSelfCheck(pluginInterface,configuration,data,framework,client,player);"),
                "the game chat after the beast board, before the self-check, as before");
            Check(At("RegisterChat(configuration,client,addonLifecycle,framework,player,") < players && players < At("FreeCompany.AetherialWheelReader? wheelReader=null;"),
                "the player actions right after the chat reader, before the aetherial wheels, as before");
        }),
        // X-885 M2 (0.1906): this case replaced « behaviour », the same RegistrationScope test copied in each family test, which touched nothing
        // of the family; each service's install and its release are now checked given to one same registration of Plugin.Chat.cs.
        ("Chat family 1894: each service's install and its release are given to one same registration of Plugin.Chat.cs (its own release, never another's)", () =>
            FamilyRegistrations.Paired("Chat family 1894", PluginFiles().Single(file => file.Name == "Plugin.Chat.cs").Text, Paths.Select(path => (path.Service, path.Install, path.Release)))),
    ];
}
