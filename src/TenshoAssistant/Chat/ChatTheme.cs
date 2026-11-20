using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TenshoAssistant.Chat;

/// <summary>
/// X-447 B16 (0.1648, the user's choice of 15/11, after Chattynator and ElvUI): the chat window's look as a share code « TENSHO-CHATLOOK1: » +
/// Base64 of a small JSON: opacity, text, margins, corners, frame, spacing, fading, tabs and scroll bar, time and channel shown, the channels'
/// colours and short names, the names' brackets and colours, item icons, outline. Never the tabs and their filters, the players hidden or put
/// forward, the alert words, the sounds nor the history: a code shared names no player and holds no word of the chat (R04). Pasted: a complete
/// look (a value the code lacks takes the default), each value bounded as the settings bound it, unknown channels left out; a text that is not
/// such a code changes nothing. Pure (tests).
/// </summary>
public static class ChatTheme
{
    public const string Prefix="TENSHO-CHATLOOK1:";
    /// <summary>The longest code read (a whole look is about 3 000 characters).</summary>
    public const int MaxCode=8000;
    private static readonly JsonSerializerOptions Options=new(){DefaultIgnoreCondition=JsonIgnoreCondition.WhenWritingNull};

    private sealed class Look
    {
        public int? Opacity{get;set;}
        public int? TextOpacity{get;set;}
        public int? FontScale{get;set;}
        public int? MarginX{get;set;}
        public int? MarginY{get;set;}
        public int? Rounding{get;set;}
        public int? Border{get;set;}
        public int? LineSpacing{get;set;}
        public int? FadeAfter{get;set;}
        public bool? Borderless{get;set;}
        public bool? HideTabs{get;set;}
        public bool? TabsAtBottom{get;set;}
        public bool? ThinScrollbar{get;set;}
        public bool? GameLinks{get;set;}
        public bool? GameTabs{get;set;}
        public bool? WrapIndent{get;set;}
        public bool? TextOutline{get;set;}
        public bool? ShowTimestamps{get;set;}
        public bool? Clock24{get;set;}
        public bool? ShowSeconds{get;set;}
        public bool? HourLeadingZero{get;set;}
        public bool? ShowChannel{get;set;}
        public bool? SenderBrackets{get;set;}
        public bool? NameColors{get;set;}
        public bool? ItemIcons{get;set;}
        public Dictionary<string,uint>? Colors{get;set;}
        public Dictionary<string,string>? Names{get;set;}
    }

    /// <summary>The code of <paramref name="settings"/>'s look.</summary>
    public static string Share(ChatSettings settings)
    {
        var look=new Look
        {
            Opacity=settings.BoundedOpacity,TextOpacity=settings.BoundedTextOpacity,FontScale=settings.BoundedFontScale,MarginX=settings.BoundedMarginX,
            MarginY=settings.BoundedMarginY,Rounding=settings.BoundedRounding,Border=settings.BoundedBorder,LineSpacing=settings.BoundedLineSpacing,
            FadeAfter=settings.BoundedFadeAfter,Borderless=settings.Borderless,HideTabs=settings.HideTabs,TabsAtBottom=settings.TabsAtBottom,
            ThinScrollbar=settings.ThinScrollbar,GameLinks=settings.GameLinks,GameTabs=settings.GameTabs,WrapIndent=settings.WrapIndent,
            TextOutline=settings.TextOutline,ShowTimestamps=settings.ShowTimestamps,Clock24=settings.Clock24,ShowSeconds=settings.ShowSeconds,
            HourLeadingZero=settings.HourLeadingZero,ShowChannel=settings.ShowChannel,SenderBrackets=settings.SenderBrackets,NameColors=settings.NameColors,
            ItemIcons=settings.ItemIcons,
            Colors=settings.Colors is { Count:>0 } colors ? colors.ToDictionary(pair=>pair.Key.ToString(),pair=>pair.Value&0xFFFFFF):null,
            Names=ChatChannels.All.Where(ChatChannels.Spoken).Select(channel=>(channel,name:settings.ShortNameOf(channel))).Where(pair=>pair.name is not null)
                .ToDictionary(pair=>pair.channel.ToString(),pair=>pair.name!) is { Count:>0 } names ? names:null,
        };
        return Prefix+Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(look,Options)));
    }

    /// <summary>Puts the look of <paramref name="code"/> into <paramref name="settings"/> (nothing else of them); false, and nothing changed, when
    /// it is not such a code (wrong prefix, too long, broken).</summary>
    public static bool Apply(string? code,ChatSettings settings)
    {
        if(Read(code) is not { } look)return false;
        var fresh=new ChatSettings();
        settings.Opacity=Math.Clamp(look.Opacity ?? fresh.Opacity,0,100);
        settings.TextOpacity=Math.Clamp(look.TextOpacity ?? fresh.TextOpacity,20,100);
        settings.FontScale=Math.Clamp(look.FontScale ?? fresh.FontScale,60,200);
        settings.MarginX=Math.Clamp(look.MarginX ?? fresh.MarginX,0,24);
        settings.MarginY=Math.Clamp(look.MarginY ?? fresh.MarginY,0,24);
        settings.Rounding=Math.Clamp(look.Rounding ?? fresh.Rounding,0,16);
        settings.Border=Math.Clamp(look.Border ?? fresh.Border,0,4);
        settings.LineSpacing=Math.Clamp(look.LineSpacing ?? fresh.LineSpacing,0,12);
        settings.FadeAfter=Math.Clamp(look.FadeAfter ?? fresh.FadeAfter,0,600);
        settings.Borderless=look.Borderless ?? fresh.Borderless;
        settings.HideTabs=look.HideTabs ?? fresh.HideTabs;
        settings.TabsAtBottom=look.TabsAtBottom ?? fresh.TabsAtBottom;
        settings.ThinScrollbar=look.ThinScrollbar ?? fresh.ThinScrollbar;
        settings.GameLinks=look.GameLinks ?? fresh.GameLinks;
        settings.GameTabs=look.GameTabs ?? fresh.GameTabs;
        settings.WrapIndent=look.WrapIndent ?? fresh.WrapIndent;
        settings.TextOutline=look.TextOutline ?? fresh.TextOutline;
        settings.ShowTimestamps=look.ShowTimestamps ?? fresh.ShowTimestamps;
        settings.Clock24=look.Clock24 ?? fresh.Clock24;
        settings.ShowSeconds=look.ShowSeconds ?? fresh.ShowSeconds;
        settings.HourLeadingZero=look.HourLeadingZero ?? fresh.HourLeadingZero;
        settings.ShowChannel=look.ShowChannel ?? fresh.ShowChannel;
        settings.SenderBrackets=look.SenderBrackets ?? fresh.SenderBrackets;
        settings.NameColors=look.NameColors ?? fresh.NameColors;
        settings.ItemIcons=look.ItemIcons ?? fresh.ItemIcons;
        var colors=new Dictionary<ChatChannel,uint>();
        foreach(var (key,color) in look.Colors ?? [])if(Channel(key) is { } channel)colors[channel]=color&0xFFFFFF;
        settings.Colors=colors.Count>0 ? colors:null;
        var names=new Dictionary<ChatChannel,string>();
        foreach(var (key,name) in look.Names ?? [])
        {
            if(Channel(key) is not { } channel || !ChatChannels.Spoken(channel))continue;
            var clean=ChatFormat.Clean(name ?? "").Trim();
            if(clean.Length>0)names[channel]=clean.Length>ChatSettings.MaximumChannelName ? clean[..ChatSettings.MaximumChannelName]:clean;
        }
        settings.ChannelNames=names.Count>0 ? names:null;
        return true;
    }

    /// <summary>X-447 B16 (0.1649): the ready-made looks, each a complete look (the channels' colours and names the default ones): the window's
    /// default (close to the game's chat), clear (no background, no frame, letters outlined), dark, compact.</summary>
    public static readonly (string Key,Action<ChatSettings> Set)[] Ready=
    [
        ("Game",_=>{}),
        ("Clear",look=>{look.Opacity=0;look.Borderless=true;look.TextOutline=true;}),
        ("Dark",look=>{look.Opacity=85;look.Rounding=4;look.TextOutline=false;}),
        ("Compact",look=>{look.FontScale=85;look.LineSpacing=0;look.MarginX=2;look.MarginY=1;}),
    ];

    /// <summary>X-447 B16 (0.1649): the code of the ready-made look <paramref name="key"/>; null for none of that name.</summary>
    public static string? ReadyCode(string key)
    {
        foreach(var (name,set) in Ready)
        {
            if(name!=key)continue;
            var look=new ChatSettings();
            set(look);
            return Share(look);
        }
        return null;
    }

    private static Look? Read(string? code)
    {
        var text=(code ?? "").Trim();
        if(!text.StartsWith(Prefix,StringComparison.Ordinal) || text.Length>MaxCode)return null;
        try{return JsonSerializer.Deserialize<Look>(Encoding.UTF8.GetString(Convert.FromBase64String(text[Prefix.Length..])));}
        catch(Exception error) when (error is FormatException or JsonException or ArgumentException or DecoderFallbackException or NotSupportedException){return null;}
    }

    // A channel by its name (never a number: the names stay right if the list ever changes).
    private static ChatChannel? Channel(string key)
        =>key.Length>0 && char.IsLetter(key[0]) && Enum.TryParse<ChatChannel>(key,false,out var channel) && Enum.IsDefined(channel) ? channel:null;
}
