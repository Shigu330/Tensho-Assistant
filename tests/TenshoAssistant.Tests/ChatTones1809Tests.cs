using TenshoAssistant.Chat;

// X-801, X-788, X-787 (0.1809): the Tensho chat window's combat lines coloured by kind and side, the type of damage's icon drawn, a status's
// glyph drawn as its icon; the item a « … uses <item>. » line names is an item link of the game (replayed from the developer-mode line of 22/11).
internal static class ChatTones1809Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static ChatRecord Line(int kind,byte source,byte target,IReadOnlyList<ChatSegment> body)
        =>new(1,DateTimeOffset.UnixEpoch,kind,ChatChannels.From(kind),"",body,source,target);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat tones 1809: damage and healing, done by you or to you, each in its colour; the channel's single colour when off",() =>
        {
            Check(ChatCombatTones.Of(41,ChatAlert.You,5)==ChatCombatTone.DamageDone && ChatCombatTones.Of(41,5,ChatAlert.You)==ChatCombatTone.DamageTaken,"damage dealt, damage taken");
            Check(ChatCombatTones.Of(45,ChatAlert.You,2)==ChatCombatTone.HealDone && ChatCombatTones.Of(45,2,ChatAlert.You)==ChatCombatTone.HealTaken,"healing given, received");
            Check(ChatCombatTones.Of(41,2,5)==ChatCombatTone.None && ChatCombatTones.Of(43,ChatAlert.You,5)==ChatCombatTone.None && ChatCombatTones.Of(10,ChatAlert.You,0)==ChatCombatTone.None,"others' lines, other kinds: the channel's colour");
            var settings=new ChatSettings();
            Check(settings.CombatTones,"on by default");
            var dealt=Line(41,ChatAlert.You,5,[]);
            Check(settings.ColorOf(dealt)==ChatCombatTones.DefaultColor(ChatCombatTone.DamageDone),"the tone's default colour");
            Check(ChatCombatTones.All.Select(ChatCombatTones.DefaultColor).Distinct().Count()==4,"four colours told apart");
            var (red,green,_)=ChatChannels.Unpack(ChatCombatTones.DefaultColor(ChatCombatTone.DamageTaken));
            Check(red>green,"damage is red");
            var (red2,green2,_)=ChatChannels.Unpack(ChatCombatTones.DefaultColor(ChatCombatTone.HealTaken));
            Check(green2>red2,"healing is green");
            settings.ToneColors=new(){[ChatCombatTone.DamageDone]=0x123456};
            Check(settings.ColorOf(dealt)==0x123456,"a colour chosen");
            settings.CombatTones=false;
            Check(settings.ColorOf(dealt)==settings.ColorOf(ChatChannel.Combat),"off: the combat channel's single colour");
            Check(new ChatSettings().ColorOf(Line(10,ChatAlert.You,0,[]))==new ChatSettings().ColorOf(ChatChannel.Say),"a spoken line keeps its channel's colour");
        }),
        ("Chat tones 1809: the type of damage's icon is kept (« text, icon, text » of a kind 41 line) and drawn, « · » in the copy",() =>
        {
            Check(ChatCombatTones.IsDamageIcon(117) && ChatCombatTones.IsDamageIcon(118) && ChatCombatTones.IsDamageIcon(188) && !ChatCombatTones.IsDamageIcon(88) && !ChatCombatTones.IsDamageIcon(120),"physical, magical, special, and the four of later patches; not the world icon");
            var body=ChatPayloads.Segments([new(ChatPayloadKind.Text,"TextPayload",Text:"The goblin takes "),new(ChatPayloadKind.Icon,"IconPayload",Icon:118),new(ChatPayloadKind.Text,"TextPayload",Text:"123 points of damage.")]);
            Check(body.Count==3 && body[1] is { Kind:ChatSegmentKind.FontIcon,Text:"·",Link.CommandId:118 },"the icon is a segment of its own: "+string.Join("|",body.Select(segment=>segment.Kind+":"+segment.Text)));
            var record=Line(41,5,ChatAlert.You,body);
            var settings=new ChatSettings{ShowTimestamps=false};
            var runs=ChatFormat.Runs(record,settings,_=>"",null,_=>0);
            Check(runs.Any(run=>run is { Role:ChatRole.GameIcon,Text:"",Link.CommandId:118 }),"drawn as an icon");
            Check(ChatFormat.Plain(record,settings,_=>"").Contains("takes ·123",StringComparison.Ordinal),"the copy keeps « · »: "+ChatFormat.Plain(record,settings,_=>""));
            var lines=ChatFormat.Wrap(runs,1000,text=>text.Length,0,10);
            Check(lines.Count==1 && lines[0].Any(run=>run.Role==ChatRole.GameIcon),"laid out as one block, as an item's icon");
            var history=ChatHistory.Encode(record);
            Check(!history.Contains("\"Link\":{",StringComparison.Ordinal),"the history keeps its text only");
            var window=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("if(DrawFontIcon(glyph.CommandId))") && window.Contains("var color=ChatColor(options.ColorOf(record));"),"drawn by the game font's renderer, « · » when it cannot; the line's tone");
            Check(!Read("src/TenshoAssistant/Chat/ChatCombatTones.cs").Contains("int.Parse") && !window.Contains("Damage total"),"display only: nothing counted (DEC-053)");
        }),
        ("Chat tones 1809: a status link's glyph becomes its icon when known; kept otherwise and in the copy",() =>
        {
            var status=new ChatLink(ChatSegmentKind.StatusLink,StatusId:217);
            // The pieces of the developer-mode line of 22/11 (kind 46): the link's glyph, one letter, the glyph before the name, the name.
            var body=ChatPayloads.Segments([new(ChatPayloadKind.Text,"TextPayload",Text:"You gain the effect of "),new(ChatPayloadKind.Status,"Status",Link:status),
                new(ChatPayloadKind.Text,"TextPayload",Text:ChatGlyphs.LinkMarker.ToString()),new(ChatPayloadKind.Text,"TextPayload",Text:" "),
                new(ChatPayloadKind.Text,"TextPayload",Text:""),new(ChatPayloadKind.Text,"TextPayload",Text:"Prospect"),new(ChatPayloadKind.LinkEnd,"LinkEnd"),new(ChatPayloadKind.Text,"TextPayload",Text:".")]);
            var record=Line(46,ChatAlert.You,ChatAlert.You,body);
            var settings=new ChatSettings{ShowTimestamps=false};
            var shown=ChatFormat.Runs(record,settings,_=>"",null,link=>link.Kind==ChatSegmentKind.StatusLink ? 10217u:0u);
            Check(shown.Count(run=>run.Role==ChatRole.ItemIcon && run.Link==status)==1 && !shown.Any(run=>run.Text.Contains('·')),"the icon in the glyph's place: "+string.Join("|",shown.Select(run=>run.Role+":"+run.Text)));
            Check(shown.Any(run=>run.Text.Contains("Prospect",StringComparison.Ordinal)),"the name stays");
            var unknown=ChatFormat.Runs(record,settings,_=>"",null,_=>0);
            Check(!unknown.Any(run=>run.Role==ChatRole.ItemIcon) && unknown.Any(run=>run.Text.Contains('·')),"icon not known: the glyph stays");
            Check(ChatFormat.Plain(record,settings,_=>"").Contains("·Prospect",StringComparison.Ordinal),"the copy keeps the glyph");
            Check(ChatFormat.StatusGlyph("") && !ChatFormat.StatusGlyph(ChatGlyphs.LinkMarker.ToString()) && !ChatFormat.StatusGlyph("A") && !ChatFormat.StatusGlyph("Prospect"),"only a lone glyph that is not the link's own");
            Check(Read("src/TenshoAssistant/MainWindow.ChatWindow.cs").Contains("ChatSegmentKind.StatusLink=>ChatPlayers?.Status(link.StatusId)?.Icon ?? 0,"),"the icon from the Status sheet, by the link's row");
        }),
        ("Chat tones 1809: an item used (kind 44) is an item link of the game: its common tooltip, no name matched",() =>
        {
            // The pieces of the developer-mode line of 22/11 14:27:22 (kind 44), texts made up of the same kinds.
            var body=ChatPayloads.Segments([new(ChatPayloadKind.Text,"TextPayload",Text:"You use "),new(ChatPayloadKind.Colour,"UIForegroundPayload",Size:500),
                new(ChatPayloadKind.Item,"Item",Link:new ChatLink(ChatSegmentKind.ItemLink,ItemId:52650)),new(ChatPayloadKind.Colour,"UIForegroundPayload",Size:501),
                new(ChatPayloadKind.Text,"TextPayload",Text:ChatGlyphs.LinkMarker.ToString()),new(ChatPayloadKind.Colour,"UIForegroundPayload"),
                new(ChatPayloadKind.Text,"TextPayload",Text:"Grade 4 Gemdraught of Dexterity"),new(ChatPayloadKind.LinkEnd,"LinkEnd"),new(ChatPayloadKind.Text,"TextPayload",Text:".")]);
            Check(body.Any(segment=>segment is { Kind:ChatSegmentKind.ItemLink,Link.ItemId:52650 } && segment.Text.Contains("Gemdraught",StringComparison.Ordinal)),"the item's name is its link's text");
            var window=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("ChatSegmentKind.ItemLink=>itemCatalog.State.Catalog?.Find(link.ItemId)?.IconId ?? 0,"),"an item link keeps its icon");
            Check(Read("docs/evidence/0.1809-chat-combat-tones.md").Contains("Item 52650"),"the evidence cited");
        }),
        ("Chat tones 1809: the setting, its colours and its texts; shared with the look",() =>
        {
            var window=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("BoolSetting(\"ChatWindow.CombatTones\",\"ChatWindow.CombatTonesTip\",()=>configuration.Chat.CombatTones") && window.Contains("Tip(\"ChatWindow.ToneColorTip\");"),"the setting and each colour");
            foreach(var language in new[]{"en","fr"})
            {
                var text=Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                foreach(var key in new[]{"CombatTones","CombatTonesTip","ToneDamageDone","ToneDamageTaken","ToneHealDone","ToneHealTaken","ToneColorTip"})Check(text.Contains($"\n    \"{key}\": \""),$"{language}: ChatWindow.{key}");
            }
            var settings=new ChatSettings{CombatTones=false,ToneColors=new(){[ChatCombatTone.HealTaken]=0x00FF00}};
            var other=new ChatSettings();
            Check(ChatTheme.Apply(ChatTheme.Share(settings),other) && !other.CombatTones && other.ColorOf(ChatCombatTone.HealTaken)==0x00FF00,"a shared look carries the tones");
        }),
    ];
}
