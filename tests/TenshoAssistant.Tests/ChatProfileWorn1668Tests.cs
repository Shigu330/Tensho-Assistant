using TenshoAssistant.Chat;

// 0.1668 (X-691 M1 and M2, review of 0.1655): the context's profile followed by the settings too (the chat window closed or hidden), and the
// profile worn said in the sections of the look and the colours.
internal static class ChatProfileWorn1668Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat profile worn 1668: a change made after leaving the context, the window closed, goes to the window's own look (M1)",()=>
        {
            var settings=new ChatSettings{ContextProfiles=true,Opacity=70};
            ChatContexts.Edit(settings,ChatContext.Duty).Look=ChatTheme.Share(new ChatSettings{Opacity=20});
            ChatContexts.Follow(settings,ChatContext.Duty);
            Check(settings.Opacity==20 && ChatContexts.Worn(settings)==ChatContext.Duty,"the duty's look worn");
            // Out of the duty, the window closed: the settings follow the context before the player changes the look.
            ChatContexts.Follow(settings,ChatContext.None);
            Check(ChatContexts.Worn(settings) is null && settings.Opacity==70,"the window's own look back");
            settings.Opacity=55;
            ChatContexts.Follow(settings,ChatContext.None);
            Check(settings.Opacity==55,"the change kept for the window");
            ChatContexts.Follow(settings,ChatContext.Duty);
            Check(settings.Opacity==20,"the duty's profile untouched");
            ChatContexts.Follow(settings,ChatContext.None);
            Check(settings.Opacity==55,"and the window's change still there");
        }),
        ("Chat profile worn 1668: the profile worn said only while one is worn (M2)",()=>
        {
            var settings=new ChatSettings{ContextProfiles=true};
            Check(ChatContexts.Worn(settings) is null,"none");
            ChatContexts.Edit(settings,ChatContext.Town).Look=ChatTheme.Share(new ChatSettings{Opacity=30});
            ChatContexts.Follow(settings,ChatContext.Town);
            Check(ChatContexts.Worn(settings)==ChatContext.Town,"the town's");
            settings.ContextProfiles=false;
            Check(ChatContexts.Worn(settings) is null,"profiles off: none");
        }),
        ("Chat profile worn 1668: the look's and the colours' settings follow the context and say the profile worn",()=>
        {
            var window=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Split("DrawChatProfileWorn(").Length==4 && window.Contains("        FollowChatContext(options);\n        if(ChatContexts.Worn(options) is not { } context)return;"),"look, colours, and the helper");
            foreach(var key in new[]{"ProfileWorn","ProfileWornTip"})
                Check(Read("src/TenshoAssistant/Localization/Languages/en.json").Contains("\""+key+"\":") && Read("src/TenshoAssistant/Localization/Languages/fr.json").Contains("\""+key+"\":"),key);
        }),
    ];
}
