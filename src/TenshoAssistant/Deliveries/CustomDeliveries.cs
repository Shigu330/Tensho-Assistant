namespace TenshoAssistant.Deliveries;

/// <summary>W-01 (0.675): a custom delivery client, from SatisfactionNpc. RankPoints[rank] is what that rank asks for before the next one; docs/evidence/0.675-custom-deliveries.md.</summary>
public sealed record DeliveryClient(uint Row,string Name,string Place,int LevelUnlock,int PerWeek,uint QuestRequired,IReadOnlyList<int> RankPoints);

public enum DeliveryReadStatus { Loaded,NotLoaded,Unavailable }

/// <summary>What the game holds, copied on the framework thread. The three lists are indexed by client row minus one.</summary>
public sealed record DeliverySnapshot(DeliveryReadStatus Status,DateTimeOffset ReadAt,int Remaining,IReadOnlyList<int> Ranks,IReadOnlyList<int> Points,IReadOnlyList<int> Used,IReadOnlyCollection<uint> QuestsDone)
{
    public static DeliverySnapshot Of(DeliveryReadStatus status,DateTimeOffset at)=>new(status,at,0,[],[],[],[]);
}

/// <summary>One line of the page. Needed is null at the last rank, where the sheet asks for nothing more.</summary>
public sealed record DeliveryRow(DeliveryClient Client,bool Unlocked,int Rank,int Points,int? Needed,int Used,int Left);

public static class DeliveryBoard
{
    public const int MaxRank=5;

    /// <summary>A list too short or a value the sheets cannot give means the game has changed: nothing is shown rather than a wrong line.</summary>
    public static DeliveryReadStatus Check(IReadOnlyList<DeliveryClient> clients,IReadOnlyList<int> ranks,IReadOnlyList<int> points,IReadOnlyList<int> used,int remaining)
    {
        if(remaining<0)return DeliveryReadStatus.Unavailable;
        foreach(var client in clients)
        {
            var index=(int)client.Row-1;
            if(index<0 || index>=ranks.Count || index>=points.Count || index>=used.Count)return DeliveryReadStatus.Unavailable;
            if(ranks[index]<0 || ranks[index]>MaxRank || points[index]<0 || used[index]<0)return DeliveryReadStatus.Unavailable;
        }
        return DeliveryReadStatus.Loaded;
    }

    /// <summary>What a client still takes: its own weekly limit and the week's allowances both apply.</summary>
    public static int Left(int perWeek,int used,int remaining)=>Math.Max(0,Math.Min(perWeek-used,remaining));

    /// <summary>A client is locked until its quest is done, or while the game gives it no rank.</summary>
    public static IReadOnlyList<DeliveryRow> Rows(IReadOnlyList<DeliveryClient> clients,DeliverySnapshot snapshot)
    {
        if(snapshot.Status!=DeliveryReadStatus.Loaded)return [];
        var rows=new List<DeliveryRow>(clients.Count);
        foreach(var client in clients)
        {
            var index=(int)client.Row-1;
            var rank=snapshot.Ranks[index];
            var unlocked=client.QuestRequired==0 || snapshot.QuestsDone.Contains(client.QuestRequired);
            int? needed=rank>=0 && rank<client.RankPoints.Count && client.RankPoints[rank]>0 ? client.RankPoints[rank]:null;
            var left=unlocked ? Left(client.PerWeek,snapshot.Used[index],snapshot.Remaining):0;
            rows.Add(new(client,unlocked,rank,snapshot.Points[index],needed,snapshot.Used[index],left));
        }
        return rows;
    }
}
