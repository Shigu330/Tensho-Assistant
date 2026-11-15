using TenshoAssistant.Chat;

// 0.1441 (X-403 phase 2, first part; D-166, DEC-053): the map and item links of a message are kept with the text that shows them, laid out
// as link runs (never joined with another link nor with plain text), and the Tensho chat window draws them with Tensho's menus.
internal static class ChatLinks1441Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).Replace("\r\n","\n");
    private static readonly DateTimeOffset Noon = new(2026,11,15,12,0,0,TimeSpan.Zero);
    private static readonly ChatLink Limsa = new(ChatSegmentKind.MapLink,129,12,9.5f,11.2f,"Limsa Lominsa Lower Decks");
    private static readonly ChatLink Potion = new(ChatSegmentKind.ItemLink,ItemId:4552,Hq:true);
    private static float Measure(string text) => text.Length;
    private static string Text(IReadOnlyList<ChatRun> line) => string.Concat(line.Select(run => run.Text));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat links 1441: the texts between a link's start and its end are the link's; the rest is plain",() =>
        {
            var body = ChatPieces.Segments([new("Meet at "),new(null,Limsa),new("\uE0BB"),new("Limsa Lominsa Lower Decks ( 9.5  , 11.2 )"),new(null,End:true),new(" now")]);
            Check(body.Count == 4,"four segments: " + body.Count);
            Check(body[0] == new ChatSegment("Meet at ") && body[3] == new ChatSegment(" now"),"plain text around the link");
            Check(body[1].Link == Limsa && body[2].Link == Limsa && body[2].Kind == ChatSegmentKind.MapLink,"the link's own texts");
        }),
        ("Chat links 1441: an open link runs to the end; a new start replaces it; an end alone and empty texts change nothing",() =>
        {
            var open = ChatPieces.Segments([new(null,Potion),new("Hi-Potion")]);
            Check(open.Single().Link == Potion && open.Single().Kind == ChatSegmentKind.ItemLink,"no terminator: linked to the end");
            var replaced = ChatPieces.Segments([new(null,Potion),new("a"),new(null,Limsa),new("b"),new(null,End:true),new(null,End:true),new(""),new(null),new("c")]);
            Check(replaced.Select(segment => segment.Link).SequenceEqual([Potion,Limsa,null]),"a then b then c");
            Check(ChatPieces.Segments([new(null,null),new("plain")]).Single().Link is null,"a link that could not be read: its text stays plain");
        }),
        ("Chat links 1441: link runs keep their link, are not joined with plain text nor with another link, and keep it when cut",() =>
        {
            var record = new ChatRecord(1,Noon,10,ChatChannel.Say,"Ann",[new("see "),new("\uE0BBLimsa",ChatSegmentKind.MapLink,Limsa),new(" and "),new("Hi-Potion",ChatSegmentKind.ItemLink,Potion)]);
            var settings = new ChatSettings { ShowTimestamps = false,ShowChannel = false };
            var runs = ChatFormat.Runs(record,settings,_ => "");
            var link = runs.Single(run => run.Link == Limsa);
            Check(link.Role == ChatRole.Link && link.Text == "Limsa","the game's glyph cleaned, role Link");
            Check(runs.Single(run => run.Link == Potion).Role == ChatRole.Link && runs.Count(run => run.Role == ChatRole.Body) == 2,"plain parts stay Body");
            var lines = ChatFormat.Wrap(runs,200,Measure);
            Check(lines.Single().Count(run => run.Link is not null) == 2,"two links, apart: " + string.Join("|",lines.Single().Select(run => run.Text)));
            var adjacent = ChatFormat.Wrap([new("ab",ChatRole.Link,Limsa),new("cd",ChatRole.Link,Potion),new("ef",ChatRole.Link,Potion)],40,Measure).Single();
            Check(adjacent.Count == 2 && adjacent[1].Text == "cdef","two different links are not joined; one link's pieces are");
            var cut = ChatFormat.Wrap([new("abcdefghij",ChatRole.Link,Potion)],4,Measure);
            Check(cut.Count == 3 && cut.All(line => line.Single().Link == Potion),"a link cut over lines keeps its link on each");
            Check(ChatFormat.Wrap([new("one two",ChatRole.Body)],40,Measure).Single().Single() == new ChatRun("one two",ChatRole.Body),"plain runs as before");
        }),
        ("Chat links 1441: the reader keeps the links; the window draws them with Tensho's menus; texts in both languages",() =>
        {
            var reader = Read("Chat/ChatReader.Dalamud.cs");
            Check(reader.Contains("case MapLinkPayload map:pieces.Add(new(null,MapLink(map)));break;") && reader.Contains("case RawPayload raw when raw.Equals(RawPayload.LinkTerminator):pieces.Add(new(null,End:true));break;")
                && reader.Contains("case ItemPayload item:pieces.Add(new(null,new ChatLink(ChatSegmentKind.ItemLink,ItemId:item.ItemId,Hq:item.IsHQ)));break;"),"map, item and end read");
            Check(reader.Contains("if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return;"),"still not read during a cut");
            var window = Read("MainWindow.ChatWindow.cs");
            Check(window.Contains("if(run.Link is { } link)DrawChatLink(run,link,$\"{record.Id}_{line}_{index}\");"),"links drawn as links");
            Check(window.Contains("if(ImGui.IsItemClicked(ImGuiMouseButton.Left))OpenMapPointFromPage(map,link.X,link.Y);"),"map: left click, flag and map");
            Check(window.Contains("PlaceEntries(place.Map,place.Coordinate,null,place.Place);") && window.Contains("ImGui.OpenPopup(\"###TenshoChatWindowPlace\");"),"map: right click, Tensho's place menu");
            Check(window.Contains("ItemLinksMenu(\"##ChatItem\"+id,item);"),"item: Tensho's item menu");
            Check(!window.Contains("SendMessage") && !window.Contains("ProcessChatBox"),"nothing sent");
            foreach(var lang in new[]{"en","fr"})
            {
                var text = Read("Localization/Languages/" + lang + ".json");
                foreach(var key in new[]{"MapLinkTip","ItemLinkTip","LinkUnknownTip"})Check(text.Contains("    \"" + key + "\": "),lang + ": " + key);
            }
        }),
    ];
}
