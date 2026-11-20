using TenshoAssistant.Chat;

// 0.1620 (X-447 D1 and D8, the user's choices of 15/11): the clicks through the locked chat window, except on what answers them; the window
// hidden in a cutscene or a duty, by choice.
internal static class ChatWindowMoments1620Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath(Path.Combine("src","TenshoAssistant",name))).Replace("\r\n","\n");
    private static readonly (float X,float Y,float Width,float Height)[] Zones=[(100,100,400,24),(150,200,60,16)];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat window moments 1620: D1, the clicks go through only when chosen, locked, no menu open, off the zones",()=>
        {
            Check(new ChatSettings() is { MouseThrough:false, HideInCutscene:false, HideInDuty:false },"off by default");
            Check(ChatLook.Through(true,true,false,(300,300),Zones),"through, off the zones");
            Check(!ChatLook.Through(false,true,false,(300,300),Zones) && !ChatLook.Through(true,false,false,(300,300),Zones),"not chosen, or unlocked: the window takes the clicks");
            Check(!ChatLook.Through(true,true,true,(300,300),Zones),"a menu open takes them");
            Check(!ChatLook.Through(true,true,false,(120,110),Zones) && !ChatLook.Through(true,true,false,(160,210),Zones),"the tab bar and a link answer");
            Check(ChatLook.Through(true,true,false,(210,210),Zones) && ChatLook.Through(true,true,false,(300,300),[]),"just past a zone; no zone drawn yet");
        }),
        ("Chat window moments 1620: D8, hidden in the moments chosen only",()=>
        {
            var none=new ChatSettings();
            Check(!ChatLook.HiddenNow(none,true,true,true),"nothing chosen: shown");
            Check(ChatLook.HiddenNow(new ChatSettings{HideInCutscene=true},false,true,false) && !ChatLook.HiddenNow(new ChatSettings{HideInCutscene=true},true,false,true),"cutscene");
            Check(ChatLook.HiddenNow(new ChatSettings{HideInDuty=true},false,false,true) && !ChatLook.HiddenNow(new ChatSettings{HideInDuty=true},true,true,false),"duty");
            Check(ChatLook.HiddenNow(new ChatSettings{HideInCombat=true},true,false,false),"combat as before");
        }),
        ("Chat window moments 1620: wiring: the flags, the zones, the settings, the game's conditions",()=>
        {
            var chat=Read("MainWindow.ChatWindow.cs");
            Check(chat.Contains("|(ChatLook.Through(ChatOptions.MouseThrough,ChatOptions.Locked,chatMenuOpen,(ImGui.GetMousePos().X,ImGui.GetMousePos().Y),chatZones) ? ImGuiWindowFlags.NoInputs:ImGuiWindowFlags.None);"),"the flag");
            Check(chat.Contains("(chatZones,chatZonesNext)=(chatZonesNext,chatZones);") && chat.Contains("chatZonesNext.Add((barAt.X,barAt.Y,ImGui.GetContentRegionAvail().X,ImGui.GetFrameHeightWithSpacing()));"),"the zones swapped each frame, the bar");
            Check(chat.Split("ChatZone();").Length==3,"a link and « back to the bottom »");
            Check(chat.Contains("!gameGui.GameUiHidden && !ChatHiddenNow();") && chat.Contains("CompetitiveGate.Cut,gameGui.GameUiHidden);"),"shown by the same moments; the game's messages no longer given back by them (X-689, 0.1657)");
            Check(chat.Contains("BoolSetting(\"ChatWindow.MouseThrough\",") && chat.Contains("BoolSetting(\"ChatWindow.HideInCutscene\",") && chat.Contains("BoolSetting(\"ChatWindow.HideInDuty\","),"settings");
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("mainWindow.InCutsceneNow=") && plugin.Contains("ConditionFlag.OccupiedInCutSceneEvent]") && plugin.Contains("mainWindow.InDutyNow=()=>condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty];"),"conditions");
        }),
    ];
}
