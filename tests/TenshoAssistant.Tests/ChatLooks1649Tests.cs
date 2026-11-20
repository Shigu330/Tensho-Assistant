using TenshoAssistant.Chat;

// 0.1649 (X-447 B16, the user's choice of 15/11, after Chattynator and ElvUI): ready-made looks for the chat window.
internal static class ChatLooks1649Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat looks 1649: behaviour: each ready-made look is a complete look applied through the code; the default one is the window's default; the rest is kept",()=>
        {
            Check(ChatTheme.Ready.Select(ready=>ready.Key).Distinct().Count()==ChatTheme.Ready.Length && ChatTheme.Ready.Length==4,"four looks, each once");
            Check(ChatTheme.ReadyCode("Game")==ChatTheme.Share(new ChatSettings()) && ChatTheme.ReadyCode("Nowhere") is null,"the default one; none of that name");
            foreach(var (key,_) in ChatTheme.Ready)
            {
                var settings=new ChatSettings{Opacity=40,Colors=new(){[ChatChannel.Party]=1},AlertWords="raid",HiddenPlayers="Ann Doe",Tabs=[new ChatTabSettings{Name="Mine"}]};
                Check(ChatTheme.ReadyCode(key) is { } code && ChatTheme.Apply(code,settings),"applied: "+key);
                Check(settings.Colors is null && settings.AlertWords=="raid" && settings.HiddenPlayers=="Ann Doe" && settings.Tabs is [{ Name:"Mine" }],"colours back, the rest kept: "+key);
            }
            var clear=new ChatSettings();ChatTheme.Apply(ChatTheme.ReadyCode("Clear"),clear);
            var compact=new ChatSettings();ChatTheme.Apply(ChatTheme.ReadyCode("Compact"),compact);
            Check(clear.Opacity==0 && clear.Borderless && clear.TextOutline && compact.FontScale==85 && compact.MarginX==2,"clear and compact");
        }),
        ("Chat looks 1649: wiring: the list after the paste button, each name a text of its own; texts",()=>
        {
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(window.Contains("if(ImGui.BeginCombo(\"###ChatReadyLooks\",localization.Get(\"ChatWindow.Looks\")))",StringComparison.Ordinal)
                && window.Contains("ChatTheme.ReadyCode(key) is not { } code || !ChatTheme.Apply(code,configuration.Chat))continue;",StringComparison.Ordinal),"the list");
            foreach(var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                foreach(var key in new[]{"Looks","LooksTip","LookGame","LookClear","LookDark","LookCompact","LookChosen"})Check(text.Contains($"\"{key}\":",StringComparison.Ordinal),language+" "+key);
            }
        }),
    ];
}
