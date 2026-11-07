using TenshoAssistant.Community;
using TenshoAssistant.DeepDungeons;
using TenshoAssistant.Duties;

// W-15 (0.718): the deep dungeons.
internal static class DeepDungeons718Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Deep dungeons: the floor's word comes from the dungeon, as the sheet writes it",() =>
        {
            // DeepDungeonItem row 1, French, read with ToMacroString on 2026-11-07.
            const string macro="Désamorce tous les pièges <switch(gnum109,du sous-sol,de l'étage,du sous-sol,de la zone)> actuel<if([gnum109==4],le,)>.";
            Check(DeepDungeonBoard.Text(macro,1)=="Désamorce tous les pièges du sous-sol actuel.","palace");
            Check(DeepDungeonBoard.Text(macro,2)=="Désamorce tous les pièges de l'étage actuel.","heaven");
            Check(DeepDungeonBoard.Text(macro,4)=="Désamorce tous les pièges de la zone actuelle.","traverse");
            Check(DeepDungeonBoard.Text("a<br>b <x(1)> c",1)=="a\nb c","other macros");
        }),
        ("Deep dungeons: floors by name, loot kinds and stones by dungeon",() =>
        {
            DutyDefinition[] duties=[new(204,2,2,"le Palais des morts (sous-sols 51-60)","c",1),new(174,1,1,"le Palais des morts (sous-sols 1-10)","c",1),new(540,3,3,"le Pilier des Cieux (étages 1-10)","c",61)];
            Check(DeepDungeonBoard.Floors(duties,"Palais des morts").Select(duty=>duty.DutyId).SequenceEqual([174u,204u]),"the game's order");
            Check(DeepDungeonBoard.Floors(duties,"").Count==0,"no name, no floor");
            Check(new[]{1u,2u,3u,4u}.Select(DeepDungeonBoard.LootKind).SequenceEqual([8,9,10,17]) && DeepDungeonBoard.LootKind(9)==0,"loot kinds");
            Check(DeepDungeonBoard.Others(1,[0,0,0,0]).Stones.Count==0 && DeepDungeonBoard.Others(2,[1,2,3,4]).Stones.Count==4,"stones");
            Check(DeepDungeonBoard.Others(3,[1,2,3,0]).Demiclones.Count==3,"demiclones");
            var traverse=DeepDungeonBoard.Others(4,[5,4,5,0]);
            Check(traverse.Stones.SequenceEqual([5u]) && traverse.Demiclones.SequenceEqual([4u,5u]),"incenses");
        }),
        ("Deep dungeons: all the sources of one kind, with the whole count",() =>
        {
            var data=new LuminaSupplementalData(new Dictionary<string,string>{["ItemSupplement"]="ItemId,SourceItemId,ItemSupplementSource,Min,Max,Probability\n2,50,8,1,1,0\n3,50,8,1,1,0\n4,40,8,1,1,0\n5,60,9,1,1,0\n"});
            var palace=data.SourcesOf(8);
            Check(palace.Select(row=>row.Source).SequenceEqual([40u,50u]) && palace[1].Count==2 && palace[1].Items.SequenceEqual([2u,3u]),"palace");
            Check(data.SourcesOf(17).Count==0,"none");
        }),
        ("Deep dungeons: read off the frame, and the loot stays behind the community switch",() =>
        {
            var page=Read("MainWindow.DeepDungeons.cs");
            Check(page.Contains("deepDungeonTask=Task.Run(load)"),"off the frame");
            Check(page.Contains("if(!configuration.CommunityLumina)"),"switch");
        }),
    ];
}
