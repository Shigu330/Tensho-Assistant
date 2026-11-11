// 0.1106 (X-127, user request of 2026-11-11): the wishlist as a tool of its own, where its wishes are gathered, their timed points' alarms in one click.
using TenshoAssistant.Gathering;
using TenshoAssistant.Items;

internal static class WishlistTool1106Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath(file)).Replace("\r\n","\n");
    private static GatheringSource Source(uint type,string zone,string place,GatheringSchedule? timing=null,bool fishing=false,bool hidden=false)
        =>new(5,1,1,type,"Job"+type,0,50,10,zone,place,0,fishing,hidden,timing);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Wishlist tool: each place once, timed places first, the node named or left empty",() =>
        {
            var timed=GatheringSchedule.FromEphemeral(200,400);
            var places=WishlistGathering.Places([Source(2,"Zone A","Node 1"),Source(2,"Zone A","Node 1"),Source(3,"Zone B"," ",timed),Source(2,"Zone A","Node 1",hidden:true)]);
            Check(places.Count==3,"the same place twice is shown once: "+places.Count);
            Check(places[0].TypeId==3 && places[0].Timing.Kind==GatheringScheduleKind.Limited && places[0].Node=="","the timed place first, a blank node empty");
            Check(places[1].Node=="Node 1" && !places[1].IsHidden && places[2].IsHidden,"the others in the catalogue's order, a hidden item apart");
            Check(WishlistGathering.Places([]).Count==0 && WishlistGathering.Shown==3,"no place, three shown");
        }),
        ("Wishlist tool: the alarm lines are the wishes still to get, once each",() =>
        {
            var lines=WishlistGathering.AlarmLines([7,0,9,7,11],id=>id==9);
            Check(lines.SequenceEqual([(7u,1L),(11u,1L)]),"obtained, zero and duplicates left out: "+string.Join(",",lines));
            Check(WishlistGathering.AlarmLines(null,_=>false).Length==0,"no list");
        }),
        ("Wishlist tool: its own page, the footer line that opens it, the FAQ, the visit and the desktop app",() =>
        {
            var pages=Read("src/TenshoAssistant/MainWindow.Pages.cs");
            Check(pages.Contains("new(Page.Wishlist,\"Objects\",PageLabel(\"Navigation.Wishlist\"),DrawWishlistPage,"),"in the side bar, ITEMS");
            var collections=Read("src/TenshoAssistant/MainWindow.Collections.cs");
            Check(!collections.Contains("###CollectionWishlist") && !collections.Contains("DrawWishlist"),"no longer a Collections tab");
            var page=Read("src/TenshoAssistant/MainWindow.Wishlist.cs");
            Check(page.Contains("DrawListPanelAlarms(\"Wishlist\",WishlistGathering.AlarmLines(list,WishObtained));"),"the alarms in one click, by the list panels' rules (no duplicate)");
            Check(page.Contains("DrawWishPlaces(id);") && page.Contains("if(wishPlaces.TryGetValue(itemId,out var known))return known;"),"where to gather, worked out once, not in every frame");
            Check(page.Contains("if(added)SayWishAdded(") && page.Contains("{page=Page.Wishlist;wishAdded=null;}"),"« added to your wishlist » opens the list");
            Check(Read("src/TenshoAssistant/MainWindow.cs").Contains("if(DrawSaveError())return;\n        // X-127 (0.1106): « added to your wishlist », with a button that opens it.\n        if(DrawWishAdded())return;"),"in the footer, whatever the page");
            Check(TenshoAssistant.Help.Faq.PageOf("Wishlist")=="Wishlist","« Where is my wishlist? » in the FAQ, opening the page");
            Check(Read("src/TenshoAssistant/Guide/GuideFlow.cs").Contains("new(\"Wishlist\")"),"a stop of the visit");
            Check(TenshoAssistant.Desktop.DesktopPages.Kind("Wishlist")==TenshoAssistant.Desktop.DesktopPageKind.NeedsLink,"the desktop app classifies it");
            foreach(var language in new[]{"en","fr"})
            {
                var json=Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                foreach(var key in new[]{"\"QuestionWishlist\"","\"AnswerWishlist\"","\"Added\"","\"AddedMany\"","\"Open\"","\"GatherAt\"","\"MorePlaces\"","\"WishlistPurpose\""})
                    Check(json.Contains(key),language+": "+key);
            }
        }),
    ];
}
