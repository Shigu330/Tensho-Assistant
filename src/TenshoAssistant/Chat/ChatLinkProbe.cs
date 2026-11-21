using System.Globalization;

namespace TenshoAssistant.Chat;

/// <summary>
/// X-715 (0.1718, D-202 b, the user's choice of 21/11): the lines of the developer-mode probe of what the game's chat sends when a link (a
/// player's name) is clicked, and of the menu the game then opens from its chat (docs/evidence/0.1718-chat-link-click-probe.md). Never a name
/// nor a whole identifier (R04, other players' data): small values as they are, large ones by their last three hexadecimal digits; what a
/// value equals (the halves of the content id, the home world, the object id of the menu's target) is found by comparison in memory and only
/// named. At most <see cref="MaximumLines"/> lines per session. Pure (tests).
/// </summary>
public sealed class ChatLinkProbe
{
    public const int MaximumLines=40;
    /// <summary>A menu opened this long after a click (milliseconds) is that click's.</summary>
    public const long MenuWindowMs=1000;
    /// <summary>The game's id of no object (Dalamud's TargetObjectId when the menu aims at no character in range).</summary>
    private const ulong NoObject=0xE000_0000;

    private int lines,clicks;
    private (int Number,long At,int Value1,int Value2,int Value3,uint Group)? last;

    public bool Full=>lines>=MaximumLines;

    /// <summary>The line of a click on a link of the game's chat (its fields as the game gives them); null past the maximum.</summary>
    public string? Clicked(byte type,ushort id,ushort index,uint group,int value1,int value2,int value3,bool text,ushort textEnd,long at)
    {
        if(Full)return null;
        lines++;clicks++;
        last=(clicks,at,value1,value2,value3,group);
        return string.Create(CultureInfo.InvariantCulture,$"Chat link probe, click {clicks}: link type {type} (sent as {type+10}), id {id}, index {index}, group {Value(unchecked((int)group))}, values {Value(value1)}, {Value(value2)}, {Value(value3)}, text {(text ? "in the panel":"none")} (end {textEnd}).");
    }

    /// <summary>The line of a menu the game opened from its chat (<paramref name="addon"/>): what it aims at, compared with the click just
    /// before (within <see cref="MenuWindowMs"/>); a menu with no click just before is said as such. Null past the maximum.</summary>
    public string? MenuOpened(string addon,ulong contentId,uint homeWorld,ulong objectId,bool inRange,int nameLength,long at)
    {
        if(Full)return null;
        lines++;
        var target=string.Create(CultureInfo.InvariantCulture,$"content id {Masked(contentId)}, home world {homeWorld}, object {(inRange ? "in range":"none")} (id {Masked(objectId)}), name {nameLength} characters");
        if(last is not { } click || at<click.At || at-click.At>MenuWindowMs)
            return "Chat link probe, the game's menu opened from "+addon+" without a link click just before: "+target+".";
        last=null;
        return string.Create(CultureInfo.InvariantCulture,$"Chat link probe, menu after click {click.Number} ({at-click.At} ms): {target}; the click's values that match it: {Matches(click.Value1,click.Value2,click.Value3,click.Group,contentId,homeWorld,objectId)}.");
    }

    /// <summary>A value of a link: as it is from -99 999 to 99 999 (types, indexes, worlds), else only its last three hexadecimal digits.</summary>
    public static string Value(int value)
        =>value is >= -99_999 and <= 99_999 ? value.ToString(CultureInfo.InvariantCulture):"large (…"+((uint)value&0xFFF).ToString("x3",CultureInfo.InvariantCulture)+")";

    /// <summary>An identifier: « 0 » for none (0, or the game's id of no object), else « set » and its last three hexadecimal digits.</summary>
    public static string Masked(ulong id)=>id is 0 or NoObject ? "0":"set (…"+(id&0xFFF).ToString("x3",CultureInfo.InvariantCulture)+")";

    /// <summary>Which of the click's values (0 never) equal a half of the target's content id, its home world or its object id; « none ».</summary>
    public static string Matches(int value1,int value2,int value3,uint group,ulong contentId,uint homeWorld,ulong objectId)
    {
        var found=new List<string>();
        void Look(string name,uint value)
        {
            if(value==0)return;
            if(contentId!=0 && value==(uint)contentId)found.Add(name+" = content id (low half)");
            if((contentId>>32)!=0 && value==(uint)(contentId>>32))found.Add(name+" = content id (high half)");
            if(value==homeWorld)found.Add(name+" = home world");
            if(objectId is not (0 or NoObject) && value==(uint)objectId)found.Add(name+" = object id");
        }
        Look("value 1",unchecked((uint)value1));Look("value 2",unchecked((uint)value2));Look("value 3",unchecked((uint)value3));Look("group",group);
        return found.Count==0 ? "none":string.Join(", ",found);
    }
}
