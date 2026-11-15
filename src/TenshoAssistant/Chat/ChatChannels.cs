namespace TenshoAssistant.Chat;

public enum ChatChannel
{
    Say,Shout,Yell,TellIn,TellOut,Party,Alliance,FreeCompany,Ls1,Ls2,Ls3,Ls4,Ls5,Ls6,Ls7,Ls8,Cwls1,Cwls2,Cwls3,Cwls4,Cwls5,Cwls6,Cwls7,Cwls8,Novice,Emote,
    Echo,System,Error,Npc,Loot,Crafting,Gathering,Progress,Retainer,Announcement,Combat,Other,
}

/// <summary>
/// X-403 phase 1 (D-166; back from 0.471): the game's chat kinds grouped into channels. The numbers are those of Dalamud's XivChatType, checked against
/// Dalamud API 15 on 15/11 (ApiProbe), and grouped as the game's own log filters group them (LogFilter sheet, English and French, read with XbmProbe on
/// 15/11): 58 is a battle system message (« you defeat … ») filed with the system messages, not an error; 73 (signs) and 74 (/random) are system
/// messages too (docs/evidence/0.1434-chat-logfilter.md). A kind not listed is <see cref="ChatChannel.Other"/>.
/// </summary>
public static class ChatChannels
{
    public static readonly IReadOnlyList<ChatChannel> All=Enum.GetValues<ChatChannel>();
    public static ChatChannel From(int kind)=>kind switch
    {
        10=>ChatChannel.Say,11=>ChatChannel.Shout,30=>ChatChannel.Yell,13=>ChatChannel.TellIn,12=>ChatChannel.TellOut,
        14 or 32 or 36=>ChatChannel.Party,15=>ChatChannel.Alliance,24=>ChatChannel.FreeCompany,
        >=16 and <=23=>ChatChannel.Ls1+(kind-16),37=>ChatChannel.Cwls1,>=101 and <=107=>ChatChannel.Cwls2+(kind-101),
        27=>ChatChannel.Novice,28 or 29=>ChatChannel.Emote,56=>ChatChannel.Echo,
        1 or 2 or 3 or 54 or 55 or 57 or 58 or 73 or 74 or 75 or 76 or 79 or (>=80 and <=94)=>ChatChannel.System,60=>ChatChannel.Error,
        61 or 68=>ChatChannel.Npc,62 or 65=>ChatChannel.Loot,66=>ChatChannel.Crafting,59 or 67=>ChatChannel.Gathering,64=>ChatChannel.Progress,
        71=>ChatChannel.Retainer,69 or 70 or 72 or 77 or 78=>ChatChannel.Announcement,>=41 and <=49=>ChatChannel.Combat,
        _=>ChatChannel.Other,
    };
    /// <summary>A person speaks on this channel, so its sender is written in front of the message.</summary>
    public static bool Spoken(ChatChannel channel)=>channel is <=ChatChannel.Novice;
    /// <summary>A new line of this channel counts as unread in a tab; what you send yourself does not.</summary>
    public static bool CountsAsUnread(ChatChannel channel)=>channel is not ChatChannel.TellOut;
    /// <summary>Tensho's own default colours (0xRRGGBB), changed in the settings; they are not read from the game.</summary>
    public static uint DefaultColor(ChatChannel channel)=>channel switch
    {
        ChatChannel.Say=>0xF0F0F0,ChatChannel.Shout=>0xFFA666,ChatChannel.Yell=>0xFFFF00,ChatChannel.TellIn or ChatChannel.TellOut=>0xFFB8DE,
        ChatChannel.Party=>0x66E5FF,ChatChannel.Alliance=>0xFF7F00,ChatChannel.FreeCompany=>0xABDBE5,
        >=ChatChannel.Ls1 and <=ChatChannel.Ls8=>0xD4FF7D,>=ChatChannel.Cwls1 and <=ChatChannel.Cwls8=>0xD4FF7D,
        ChatChannel.Novice=>0xD4FF7D,ChatChannel.Emote=>0xBAFFF0,ChatChannel.Echo=>0xCCCCCC,ChatChannel.Error=>0xFF4A4A,
        ChatChannel.Loot=>0xBAD97E,ChatChannel.Announcement=>0xABDBE5,ChatChannel.Combat=>0xB0B0B0,_=>0xDDDDDD,
    };
    /// <summary>A colour of the settings (0xRRGGBB) from its three parts between 0 and 1, and back.</summary>
    public static uint Pack(float red,float green,float blue)=>Part(red)<<16|Part(green)<<8|Part(blue);
    public static (float Red,float Green,float Blue) Unpack(uint rgb)=>(((rgb>>16)&255)/255f,((rgb>>8)&255)/255f,(rgb&255)/255f);
    private static uint Part(float value)=>(uint)Math.Clamp(MathF.Round((float.IsFinite(value) ? value:0)*255),0,255);
}
