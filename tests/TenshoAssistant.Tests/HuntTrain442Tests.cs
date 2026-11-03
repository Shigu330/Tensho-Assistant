using TenshoAssistant.Maps;

// N-15 (0.442): hunt train recorder — once per map, next stop, export read back by the bookmark import.
internal static class HuntTrain442Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Hunt train: each mark once per map, next stop, export read back as bookmarks",() =>
        {
            var at=DateTimeOffset.UnixEpoch;
            IReadOnlyList<HuntTrainStop> stops=[];
            stops=HuntTrain.Add(stops,new("Forneus",2,20,140,"Thanalan occidental",12.3f,7.4f,at));
            stops=HuntTrain.Add(stops,new("forneus",2,20,140,"Thanalan occidental",13f,8f,at));
            stops=HuntTrain.Add(stops,new("Laideronnette",3,30,150,"Kozama'uka",30.1f,15.2f,at));
            Check(stops.Count==2,"same name and map: once");
            Check(HuntTrain.Next(stops)==0 && HuntTrain.Next(HuntTrain.MarkVisited(stops,0))==1 && HuntTrain.Next(HuntTrain.MarkVisited(HuntTrain.MarkVisited(stops,0),1)) is null,"next stop");
            var text=HuntTrain.Export(stops);
            Check(text=="Forneus — Thanalan occidental ( 12.3 , 7.4 )\nLaideronnette — Kozama'uka ( 30.1 , 15.2 )","export lines");
            var zones=new Dictionary<string,MapDefinition>{["Thanalan occidental"]=new(20,140,"","Thanalan occidental","",0,100),["Kozama'uka"]=new(30,150,"","Kozama'uka","",0,100)};
            var back=MapBookmarks.ParseCoordinateList(text,name=>zones.GetValueOrDefault(name),"Train");
            Check(back.Bookmarks.Count==2 && back.Unknown==0 && back.Bookmarks[1].X==30.1f,"read back by the bookmark import");
            var full=Enumerable.Range(0,70).Aggregate((IReadOnlyList<HuntTrainStop>)[],(list,index)=>HuntTrain.Add(list,new($"M{index}",2,1,1,"Z",1,1,at)));
            Check(full.Count==HuntTrain.Capacity,"bounded");
        }),
    ];
}
