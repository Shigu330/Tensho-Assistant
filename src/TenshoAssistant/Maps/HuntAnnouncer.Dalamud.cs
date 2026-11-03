using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace TenshoAssistant.Maps;

/// <summary>
/// X-13 (0.447): sends the hunt train conductor's call to a chat channel, on the player's click only, through the game's own chat box
/// (UIModule.ProcessChatBoxEntry, as the game alarms of 0.205). Only a line built by HuntTrain.ChatCommand is accepted: a fixed channel,
/// the cleaned call, then « &lt;flag&gt; », which the game turns into the flag's link. Framework thread only.
/// </summary>
internal static class HuntAnnouncer
{
    public static unsafe bool Send(string command,IPluginLog log)
    {
        if(!HuntTrain.IsChatCommand(command)){log.Warning("Hunt train call refused: not a call built by Tensho.");return false;}
        try
        {
            var ui=UIModule.Instance();
            if(ui==null){log.Warning("Hunt train call not sent: the game's UI module is unavailable.");return false;}
            var text=Utf8String.FromString(command);
            try{ui->ProcessChatBoxEntry(text);}
            finally{text->Dtor(true);}
            return true;
        }
        catch(Exception error){log.Warning(error,"Hunt train call could not be sent.");return false;}
    }
}
