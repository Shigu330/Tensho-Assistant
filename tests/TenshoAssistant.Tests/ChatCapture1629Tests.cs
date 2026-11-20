using System.Reflection;
using TenshoAssistant.Chat;

// 0.1629 (X-669, review of 0.1619-0.1627, R25): capture mode. M1: the stand-in made with a secret drawn at each start (an unkeyed hash let a
// guessed name be checked against a picture, and a player matched between two players' pictures); the names' colours follow it. M2: the
// message menu names the player by the stand-in. M3: the summaries of ChatCopy.NameOf and WorldOf in place.
internal static class ChatCapture1629Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly string[] Names=["Ann Doe","Bob Roe","Cid Moe","Dee Lin","Eve Sol","Fae Gor","Gus Hal","Ida Kel","Jon Wex","Kai Mur"];
    private static byte[] Key(byte seed)=>[..Enumerable.Range(0,32).Select(index=>(byte)(seed*31+index))];
    private static uint Fnv(string name){var hash=2166136261u;foreach(var letter in name.ToLowerInvariant()){hash^=letter;hash*=16777619u;}return hash&0xFFFFF;}

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat capture 1629: behaviour: a stand-in depends on the key: the same key, the same code (world and case aside); two keys (two starts, two PCs) give two codes for the same player",()=>
        {
            var one=Key(1);var two=Key(2);
            Check(ChatFormat.StandIn("Ann Doe@Moogle",one)==ChatFormat.StandIn(" ann doe ",one),"world and case aside");
            var differ=Names.Count(name=>ChatFormat.StandIn(name,one)!=ChatFormat.StandIn(name,two));
            Check(differ==Names.Length,"two keys, two codes: "+differ+" of "+Names.Length);
            Check(Names.Select(name=>ChatFormat.StandIn(name,one)).Distinct().Count()==Names.Length,"players told apart");
            Check(Names.All(name=>System.Text.RegularExpressions.Regex.IsMatch(ChatFormat.StandIn(name,one),"^#[0-9A-F]{5}$")),"five letters or digits");
        }),
        ("Chat capture 1629: behaviour: the plugin's stand-ins cannot be worked out from the name alone (no longer the unkeyed hash of 0.1623); the key is drawn at random, private, never a property",()=>
        {
            var same=Names.Count(name=>ChatFormat.StandIn(name)=="#"+Fnv(name).ToString("X5",System.Globalization.CultureInfo.InvariantCulture));
            Check(same<=1,"the unkeyed hash of 0.1623 gives the plugin's code for "+same+" of "+Names.Length+" names");
            Check(ChatFormat.StandIn("Ann Doe")==ChatFormat.StandIn("ann doe@Moogle"),"stable within a start");
            var secret=typeof(ChatFormat).GetField("CaptureKey",BindingFlags.NonPublic|BindingFlags.Static);
            Check(secret is { IsInitOnly:true } && secret.GetValue(null) is byte[] { Length:32 } key && key.Any(value=>value!=0),"a private key of 32 bytes");
            Check(!typeof(ChatFormat).GetProperties(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Any(property=>property.PropertyType==typeof(byte[])),"never given out");
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/ChatFormat.cs"));
            Check(source.Contains("CaptureKey=System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);",StringComparison.Ordinal),"drawn at random at each load");
        }),
        ("Chat capture 1629: behaviour: in capture mode a name's colour comes from its stand-in, not from the name; out of it, from the name as before",()=>
        {
            Check(Names.All(name=>ChatLook.SenderColour(name,false)==ChatLook.NameColour(name)),"out of capture mode: the name's colour");
            Check(Names.All(name=>ChatLook.SenderColour(name,true)==ChatLook.NameColour(ChatFormat.StandIn(name))),"capture mode: the stand-in's");
            var differ=Names.Count(name=>ChatLook.SenderColour(name,true)!=ChatLook.NameColour(name));
            Check(differ>=8,"the colour no longer tells the name: "+differ+" of "+Names.Length+" differ");
        }),
        ("Chat capture 1629: behaviour: the message menu names a player by the stand-in in capture mode, by the name otherwise; the menu's entries use it",()=>
        {
            Check(ChatCopy.MenuName("Ann Doe",true)==ChatFormat.StandIn("Ann Doe") && ChatCopy.MenuName("Ann Doe",false)=="Ann Doe","the name written");
            var window=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            var start=window.IndexOf("private void DrawChatSenderMenu(",StringComparison.Ordinal);
            var end=window.IndexOf("DrawChatPlayerGameEntries(said);",start,StringComparison.Ordinal);
            Check(start>0 && end>start,"the menu found");
            var menu=window[start..end];
            foreach(var key in new[]{"MenuCopyPlayer","MenuHidePlayer","MenuUnhighlightPlayer","MenuHighlightPlayer"})
                Check(menu.Contains("\"ChatWindow."+key+"\",shown)",StringComparison.Ordinal),key+" written with the stand-in");
            Check(!menu.Contains("\",player))",StringComparison.Ordinal),"no entry writes the real name");
            Check(window.Contains("ChatLook.SenderColour(record.Sender,options.CaptureNames)",StringComparison.Ordinal),"the colour follows capture mode");
        }),
        ("Chat capture 1629: each summary over its own method (NameOf, WorldOf)",()=>
        {
            var copy=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/ChatCopy.cs")).Replace("\r\n","\n");
            Check(copy.Contains("/// <summary>E3: the world written after « @ » in a sender's name, or nothing.</summary>\n    public static string WorldOf(",StringComparison.Ordinal),"WorldOf");
            Check(copy.Contains("Name@World » as the game writes it elsewhere).</summary>\n    public static string NameOf(",StringComparison.Ordinal),"NameOf");
            Check(!copy.Contains("</summary>\n    /// <summary>",StringComparison.Ordinal),"no method with two summaries");
        }),
    ];
}
