using System.Numerics;
using System.Text.Json;
using TenshoAssistant.Discoveries;

// 0.1134 (X-167, GPT audit of 12/11, R03): what goes to GitHub (CommunityDiscoveries.Mine) holds only the fields the player read: an import that widens
// the dates, levels or keeps its own place never reaches the gist; the texts say that a fishing fact holds the character's exact place (DEC-045).
// Behaviour tests (R28): real stores, imports, sightings, a file written and read back, the exported file decoded.
internal static class DiscoveriesProvenance1134Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,10,12,0,0,TimeSpan.Zero);
    private static DiscoveryFact Imported(DiscoveryKind kind,uint subject,Vector3 place,int min,int max,DateTimeOffset first,DateTimeOffset last,uint detail=1)=>new()
    {
        Kind=kind,Subject=subject,Detail=detail,Territory=135,Map=16,CellX=DiscoveryStore.Cell(place.X,place.Z).X,CellZ=DiscoveryStore.Cell(place.X,place.Z).Z,
        X=place.X,Z=place.Z,LevelMin=min,LevelMax=max,Seen=2,First=first,Last=last,Values=new(){[min]=1,[max]=1}
    };
    private static DiscoveryFact Sent(DiscoveryStore store,DiscoveryKind kind,uint subject)
    {
        var encoded=CommunityDiscoveries.Encode(CommunityDiscoveries.Mine(store.Snapshot(At)));
        var back=CommunityDiscoveries.Decode(encoded)!;
        return back.Facts.Single(fact=>fact.Kind==kind && fact.Subject==subject);
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discoveries 1134: an imported fish you then catch sends your day, value and place only; a later import changes nothing that is sent",() =>
        {
            var store=new DiscoveryStore();
            var theirs=new Vector3(110,0,-30);var mine=new Vector3(130,0,-20);
            store.Merge([Imported(DiscoveryKind.FishCatch,4869,theirs,40,90,At.AddDays(-20),At.AddDays(-15))]);
            store.SeeSource(DiscoveryKind.FishCatch,4869,1,3,At,135,16,mine);
            var held=store.Find(DiscoveryKind.FishCatch,4869u,1).Single();
            Check(held.Seen==1 && held.LevelMin==3 && held.LevelMax==90 && held.First==At.AddDays(-20) && held.X==theirs.X,"the base keeps the widest fields for the pages");
            var sent=Sent(store,DiscoveryKind.FishCatch,4869);
            Check(sent.First==CommunityDiscoveries.Day(At) && sent.Last==CommunityDiscoveries.Day(At),"your day only, not the import's");
            Check(sent.LevelMin==3 && sent.LevelMax==3,"your value only, not the import's 40 to 90");
            Check(sent.X==mine.X && sent.Z==mine.Z,"your exact place, not the import's");
            Check(sent.Imported==0 && sent.ImportedValues is null && sent.Values is { Count:1 } && sent.Values[3]==1,"no imported count or value");
            store.Merge([Imported(DiscoveryKind.FishCatch,4869,theirs,1,200,At.AddDays(-40),At.AddHours(-1))]);
            var again=Sent(store,DiscoveryKind.FishCatch,4869);
            Check(again.LevelMin==3 && again.LevelMax==3 && again.First==CommunityDiscoveries.Day(At) && again.X==mine.X,"a later import changes nothing sent");
        }),
        ("Discoveries 1134: your monster fact widened by an import sends your levels and days; a later sighting of yours widens what is sent",() =>
        {
            var store=new DiscoveryStore();var place=new Vector3(-200,0,310);
            store.SeeMonster(10,20,135,16,place,12,At);
            store.Merge([Imported(DiscoveryKind.MonsterPlace,10,place,5,30,At.AddDays(-9),At.AddDays(-8),20)]);
            Check(store.Find(DiscoveryKind.MonsterPlace,10u,5).Single() is { Imported:1, LevelMin:5, LevelMax:30 },"the import joined to your fact");
            var sent=Sent(store,DiscoveryKind.MonsterPlace,10);
            Check(sent.LevelMin==12 && sent.LevelMax==12 && sent.First==CommunityDiscoveries.Day(At),"your level and day only (the import brought 5 to 30 and an earlier day)");
            store.SeeMonster(10,20,135,16,place,14,At.AddDays(1));
            var later=Sent(store,DiscoveryKind.MonsterPlace,10);
            Check(later.LevelMin==12 && later.LevelMax==14 && later.Last==CommunityDiscoveries.Day(At.AddDays(1)) && later.Seen==2,"your next sighting widens your part");
        }),
        ("Discoveries 1134: a sighting is counted from your own last one, never from a later imported date",() =>
        {
            var store=new DiscoveryStore();var place=new Vector3(40,0,40);
            store.Merge([Imported(DiscoveryKind.MonsterPlace,11,place,5,5,At.AddDays(-1),At.AddMinutes(5),21)]);
            store.SeeMonster(11,21,135,16,place,5,At);
            Check(store.Find(DiscoveryKind.MonsterPlace,11u,1).Single().Seen==1,"your first sighting counts though the import's last date is later");
            store.SeeMonster(11,21,135,16,place,5,At.AddMinutes(3));
            Check(store.Find(DiscoveryKind.MonsterPlace,11u,1).Single().Seen==1,"3 minutes after yours: the same sighting");
            store.SeeMonster(11,21,135,16,place,5,At.AddMinutes(14));
            Check(store.Find(DiscoveryKind.MonsterPlace,11u,1).Single().Seen==2,"11 minutes after your last: a new one (counted from your own times only)");
        }),
        ("Discoveries 1134: your part is kept in your file, read back and joined; never in a shared file nor in what is sent",() =>
        {
            var folder=Path.Combine(Path.GetTempPath(),"tensho-prov-1134-"+Guid.NewGuid().ToString("N"));
            try
            {
                var store=new DiscoveryStore();var theirs=new Vector3(110,0,-30);var mine=new Vector3(130,0,-20);
                store.Merge([Imported(DiscoveryKind.FishCatch,4869,theirs,40,90,At.AddDays(-20),At.AddDays(-15)),Imported(DiscoveryKind.FishCatch,4870,theirs,1,1,At,At)]);
                store.SeeSource(DiscoveryKind.FishCatch,4869,1,3,At,135,16,mine);
                var main=Path.Combine(folder,"Discoveries.json");
                DiscoveryFiles.WriteSplit(main,store.Snapshot(At),[]);
                Check(File.ReadAllText(main).Contains("\"Own\":{"),"your part is in your file");
                var shared=DiscoveryFiles.ReadSharedKind(main,DiscoveryKind.FishCatch,(_,e)=>throw e)!;
                Check(shared.Facts.Single().Subject==4870 && shared.Facts.All(fact=>fact.Own is null),"no part of yours in a shared file");
                var back=new DiscoveryStore();back.Load(DiscoveryFiles.Read(main)!);back.Load(shared);
                Check(Sent(back,DiscoveryKind.FishCatch,4869) is { LevelMin:3, LevelMax:3 } again && again.X==mine.X,"read back: the same is sent");
                var json=JsonSerializer.Serialize(CommunityDiscoveries.Mine(back.Snapshot(At)));
                Check(!json.Contains("\"Own\"",StringComparison.Ordinal),"never sent");
                // Two facts joined in one cell (0.855): yours with an imported one keeps your part; two of yours not mixed keep none.
                var a=new DiscoveryFact{Kind=DiscoveryKind.MonsterPlace,Subject=1,Map=16,X=10,Z=10,LevelMin=7,LevelMax=7,Seen=1,First=At,Last=At};
                var b=new DiscoveryFact{Kind=DiscoveryKind.MonsterPlace,Subject=1,Map=16,X=12,Z=12,LevelMin=2,LevelMax=50,Seen=0,Imported=1,First=At.AddDays(-5),Last=At.AddDays(-5)};
                DiscoveryStore.Join(a,b);
                Check(a.Own is { LevelMin:7, LevelMax:7, X:10 } && a.Own.First==At && a.LevelMin==2 && a.X==12,"yours joined with an imported one: your part kept apart");
                var c=new DiscoveryFact{Kind=DiscoveryKind.MonsterPlace,Subject=1,Map=16,X=10,Z=10,LevelMin=7,LevelMax=7,Seen=1,First=At,Last=At};
                DiscoveryStore.Join(c,new DiscoveryFact{Kind=DiscoveryKind.MonsterPlace,Subject=1,Map=16,X=12,Z=12,LevelMin=9,LevelMax=9,Seen=1,First=At,Last=At});
                Check(c.Own is null && c.LevelMax==9,"two of yours: no part apart");
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }),
        ("Discoveries 1134: a fact of yours from before 0.1134 with imports (no part apart) is sent as before; the texts tell about fishing places",() =>
        {
            var store=new DiscoveryStore();var place=new Vector3(60,0,60);
            var old=Imported(DiscoveryKind.MonsterPlace,12,place,4,6,At.AddDays(-3),At.AddDays(-2));old.Seen=3;old.Imported=1;
            store.Load(new DiscoveryFile{Facts=[old]});
            var sent=Sent(store,DiscoveryKind.MonsterPlace,12);
            Check(sent.LevelMin==4 && sent.LevelMax==6 && sent.Seen==3,"taken as yours, as it was sent before (only the common file of ticket 43 was ever imported)");
            string Read(string file)=>File.ReadAllText(Path.GetFullPath(file)).Replace("\r\n","\n");
            var en=Read("src/TenshoAssistant/Localization/Languages/en.json");var fr=Read("src/TenshoAssistant/Localization/Languages/fr.json");
            Check(!en.Contains("never holds a player's name, position or id") && en.Contains("your character's exact place"),"English help and warning");
            Check(!fr.Contains("le nom, la position ni l'identifiant d'un joueur") && fr.Contains("l'endroit exact de votre personnage"),"French help and warning");
            var readme=Read("community-discoveries/README.md");var security=Read("SECURITY.md");
            Check(readme.Contains("exact place (the precision chosen in\nDEC-045") && readme.Contains("l'endroit exact du personnage (la précision choisie par DEC-045"),"README, both halves");
            Check(security.Contains("a fish caught or a bite holds the character's exact place (DEC-045)") && security.Contains("un poisson pris ou une touche porte l'endroit exact du personnage (DEC-045)"),"SECURITY, both halves");
        }),
    ];
}
