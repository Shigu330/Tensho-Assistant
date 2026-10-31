using TenshoAssistant.Community;

internal static class WikiItemAcquisitionTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    // Shape of the raw wiki page « Rocket Punch Identification Key » (read 2026-10-31).
    private const string Page="{{Item infobox\n| name = Rocket Punch Identification Key\n}}\n__NOTOC__\n==Acquisition==\n===PvP===\n*{{seriesmalmstonesreward|12|25}}\n* Sold by [[Mogmul Mogbelly|the mog]] for 5 ''tokens''\n==Notes==\n* not acquisition\n";
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Wiki item acquisition keeps the section bullets with their sub-heading",()=>
        {
            var page=WikiItemAcquisitionParser.Parse(Page,52262,"Rocket Punch Identification Key",DateTimeOffset.UnixEpoch);
            Check(page.Lines.Count==2,"two bullets, the Notes section ignored");
            Check(page.Lines[0]==new WikiAcquisitionLine("PvP","PvP series reward (rank 12)") && page.Lines[1].Text=="Sold by the mog for 5 tokens","templates and links readable");
            // Bennu Horn (read 2026-10-31): infobox online-store-id and a plain {{onlinestore|24}} line; Alsvid: an external link.
            var bennu=WikiItemAcquisitionParser.Parse("{{Item infobox\n| online-store-id = 197\n}}\n==Acquisition==\n===Purchase===\n{{onlinestore|24}}\n* [https://na.finalfantasyxiv.com/lodestone/x Twitch Support-A-Streamer Campaign] (July 28)\n",1,"Bennu Horn",DateTimeOffset.UnixEpoch);
            Check(bennu.Lines.Select(line=>line.Text).SequenceEqual(new[]{"Online Store","Online Store (24 USD)","Twitch Support-A-Streamer Campaign (July 28)"}),"online store and external link text");
            // Aerith miniature (capture 0.148): a {{clear}} layout template must not become a source line.
            Check(WikiItemAcquisitionParser.Parse("==Acquisition==\n===Purchase===\n{{onlinestore|6.99}}\n{{clear}}\n",1,"t",DateTimeOffset.UnixEpoch).Lines.Count==1,"clear dropped");
            Check(WikiItemAcquisitionParser.Parse("==Notes==\n* x",1,"t",DateTimeOffset.UnixEpoch).Lines.Count==0,"no acquisition section = nothing");
            Check(WikiItemAcquisitionParser.Url("Rocket Punch Identification Key").EndsWith("title=Rocket_Punch_Identification_Key&action=raw",StringComparison.Ordinal),"raw page URL");
        }),
    };
}
