using System.Text.Json;

namespace TenshoAssistant.Market;

/// <summary>X-52 (0.817): one offer as the game's market board shows it: unit price, quality, quantity. No retainer, seller or buyer is kept.</summary>
public sealed record SeenListing(decimal Price,bool Hq,int Quantity);
/// <summary>X-52 (0.817): one sale of the board's history (the game gives the last 20): unit price, quality, quantity, when.</summary>
public sealed record SeenSale(decimal Price,bool Hq,int Quantity,DateTimeOffset At);

/// <summary>What was seen for one item on one world: the offers of the last request, the last sales, and when each was seen.</summary>
public sealed class SeenMarketEntry
{
    public uint World { get; set; }
    public uint Item { get; set; }
    public List<SeenListing> Listings { get; set; }=[];
    public DateTimeOffset ListingsAt { get; set; }
    public uint ListingsRequest { get; set; }
    public List<SeenSale> Sales { get; set; }=[];
    public DateTimeOffset SalesAt { get; set; }
    public DateTimeOffset Seen=>ListingsAt>SalesAt ? ListingsAt:SalesAt;
}

/// <summary>
/// X-52 (0.817, user request: « the prices seen at the market must replace Universalis, they are more reliable, check we have as much »). For the player's world,
/// the board gives every offer and the last 20 sales, which is what Tensho takes from Universalis for that world (lowest offer, last sale, average sale, sales a day);
/// other worlds and items never opened stay with Universalis. A seen quote is used when it is newer than Universalis' data.
/// </summary>
public static class SeenPrices
{
    // X-73.2 (0.858): 200 sales kept (Universalis' own window for the speed), merged from one opening to the next, 30 days at most.
    public const int MaxListings=200,MaxSales=200;
    public static readonly TimeSpan SalesKept=TimeSpan.FromDays(30);
    /// <summary>X-73.2 (0.858): the sales already kept and those just seen, without twins (same time, price, quantity, quality), newest first, bounded.</summary>
    public static List<SeenSale> MergeSales(IEnumerable<SeenSale> kept,IEnumerable<SeenSale> seen,DateTimeOffset at)=>
        kept.Concat(seen).Where(sale=>sale.Price>0 && sale.At<=at.AddMinutes(5) && at-sale.At<=SalesKept).Distinct().OrderByDescending(sale=>sale.At).Take(MaxSales).ToList();

    public static MarketQualityPrice Quality(IReadOnlyList<SeenListing> listings,IReadOnlyList<SeenSale> sales,bool hq,DateTimeOffset now)
    {
        var offers=listings.Where(listing=>listing.Hq==hq && listing.Price>0).ToArray();
        var sold=sales.Where(sale=>sale.Hq==hq && sale.Price>0).OrderByDescending(sale=>sale.At).ToArray();
        decimal? velocity=null;
        if(sales.Count>0)
        {
            // Over the time the history covers (its oldest sale to now), at least a day: units of this quality sold a day.
            var days=Math.Max(1,(decimal)(now-sales.Min(sale=>sale.At)).TotalDays);
            velocity=Math.Round(sold.Sum(sale=>(decimal)Math.Max(1,sale.Quantity))/days,2);
        }
        return new(offers.Length>0 ? offers.Min(offer=>offer.Price):null,sold.FirstOrDefault()?.Price,sold.FirstOrDefault()?.At,
            sold.Length>0 ? Math.Round(sold.Average(sale=>sale.Price),0):null,velocity);
    }

    /// <summary>X-73.1 (0.857): a world's offers count for « cheapest of the data center » this long after they were seen.</summary>
    public static readonly TimeSpan CurrentOffers=TimeSpan.FromHours(24);
    /// <summary>X-73.1 (0.857): what is kept: entries seen within this long.</summary>
    public static readonly TimeSpan KeptFor=TimeSpan.FromDays(90);

    /// <summary>
    /// X-73.1 (0.857, user request: prices seen on the other worlds of the data center, when one goes there, complete Universalis): the cheapest offer of the data
    /// center from Universalis' and from each visited world's lowest offer seen. Universalis' world gives way to what was seen there later; another world seen
    /// counts while its offers are current (CurrentOffers). docs/evidence/0.857-community-map.md (market report).
    /// </summary>
    public static MarketListing? Cheapest(MarketListing? universalis,IEnumerable<(uint World,decimal? Price,DateTimeOffset At)> seen,DateTimeOffset now)
    {
        var worlds=seen.Where(entry=>entry.World!=0).ToList();
        var candidates=new List<MarketListing>();
        if(universalis is { } listing)
        {
            var there=worlds.FirstOrDefault(entry=>entry.World==listing.WorldId);
            if(there.World!=0 && there.At>(listing.UploadedAt ?? DateTimeOffset.MinValue)){if(there.Price is { } price)candidates.Add(new(price,listing.WorldId,there.At));}
            else candidates.Add(listing);
        }
        foreach(var (world,price,at) in worlds)
            if(price is { } offer && now-at<=CurrentOffers && world!=universalis?.WorldId)candidates.Add(new(offer,world,at));
        return candidates.OrderBy(candidate=>candidate.Price).ThenByDescending(candidate=>candidate.UploadedAt).FirstOrDefault();
    }

    /// <summary>The quote of what was seen; Universalis' other worlds and region kept from its own quote when there is one.</summary>
    public static MarketQuote Quote(SeenMarketEntry entry,MarketQuote? universalis,DateTimeOffset now)=>new(entry.World,entry.Item,entry.Seen,entry.Seen,
        Quality(entry.Listings,entry.Sales,false,now),Quality(entry.Listings,entry.Sales,true,now),universalis?.NqCheapest,universalis?.HqCheapest,universalis?.NqRegion,universalis?.HqRegion);

    /// <summary>
    /// The seen quote when it is newer than Universalis' data for that world (its upload, else its fetch); otherwise null.
    /// 0.821 (user report: the other worlds vanished): with no Universalis quote yet, nothing while Universalis is asked (« waitForUniversalis »), so that the request
    /// is still made and its other worlds and region come back; only with that source off does the seen quote stand alone.
    /// </summary>
    public static MarketQuote? Prefer(SeenMarketEntry? entry,MarketQuote? universalis,DateTimeOffset now,bool waitForUniversalis=false)
    {
        if(entry is null || entry.Listings.Count==0 && entry.Sales.Count==0)return null;
        if(universalis is null && waitForUniversalis)return null;
        if(universalis is not null && (universalis.UploadedAt ?? universalis.FetchedAt)>=entry.Seen)return null;
        return Quote(entry,universalis,now);
    }
}

/// <summary>X-52 (0.817): the prices seen, by world and item, safe from any thread, kept in a file beside the discoveries.</summary>
public sealed class SeenPriceStore
{
    public const int MaxEntries=50_000;
    private readonly object gate=new();
    private readonly Dictionary<(uint World,uint Item),SeenMarketEntry> entries=[];
    public bool Dirty { get; private set; }
    public int Count{get{lock(gate)return entries.Count;}}

    /// <summary>A page of offers: a new request replaces the last offers, a later page of the same request adds to them.</summary>
    public void SeeListings(uint world,uint item,uint request,IEnumerable<SeenListing> page,DateTimeOffset at)
    {
        if(world==0 || item==0)return;
        lock(gate)
        {
            var entry=Entry(world,item);if(entry is null)return;
            if(entry.ListingsRequest!=request || at-entry.ListingsAt>TimeSpan.FromMinutes(2)){entry.Listings=[];entry.ListingsRequest=request;}
            foreach(var listing in page)if(entry.Listings.Count<SeenPrices.MaxListings && listing.Price>0)entry.Listings.Add(listing);
            entry.ListingsAt=at;Dirty=true;
        }
    }

    public void SeeSales(uint world,uint item,IEnumerable<SeenSale> sales,DateTimeOffset at)
    {
        if(world==0 || item==0)return;
        lock(gate)
        {
            var entry=Entry(world,item);if(entry is null)return;
            // X-73.2 (0.858, user request: learn in game what comes from Universalis): the game shows the last 20 sales; each opening adds to those kept.
            entry.Sales=SeenPrices.MergeSales(entry.Sales,sales,at);
            entry.SalesAt=at;Dirty=true;
        }
    }

    private SeenMarketEntry? Entry(uint world,uint item)
    {
        if(entries.TryGetValue((world,item),out var entry))return entry;
        if(entries.Count>=MaxEntries)return null;
        return entries[(world,item)]=new SeenMarketEntry{World=world,Item=item};
    }

    /// <summary>X-73.1 (0.857): the quote with its data-center cheapest offers completed by the worlds seen (SeenPrices.Cheapest).</summary>
    public MarketQuote? WithDataCenter(MarketQuote? quote,IReadOnlyCollection<uint> worlds,DateTimeOffset now)
    {
        if(quote is null || worlds.Count==0)return quote;
        var nq=new List<(uint,decimal?,DateTimeOffset)>();var hq=new List<(uint,decimal?,DateTimeOffset)>();
        lock(gate)
            foreach(var world in worlds)
            {
                if(!entries.TryGetValue((world,quote.ItemId),out var entry) || entry.Listings.Count==0)continue;
                if(!lowest.TryGetValue((world,quote.ItemId),out var known) || known.At!=entry.ListingsAt)
                    lowest[(world,quote.ItemId)]=known=(entry.ListingsAt,Lowest(entry.Listings,false),Lowest(entry.Listings,true));
                nq.Add((world,known.Nq,known.At));hq.Add((world,known.Hq,known.At));
            }
        if(nq.Count==0)return quote;
        return quote with {NqCheapest=SeenPrices.Cheapest(quote.NqCheapest,nq,now),HqCheapest=SeenPrices.Cheapest(quote.HqCheapest,hq,now)};
    }
    private static decimal? Lowest(IReadOnlyList<SeenListing> listings,bool hq)=>listings.Where(listing=>listing.Hq==hq && listing.Price>0).Select(listing=>(decimal?)listing.Price).Min();
    private readonly Dictionary<(uint World,uint Item),(DateTimeOffset At,decimal? Nq,decimal? Hq)> lowest=[];

    /// <summary>The seen quote when newer than Universalis' (SeenPrices.Prefer), without copying the entry; computed again only when the entry or Universalis' quote changes.</summary>
    public MarketQuote? Prefer(uint world,uint item,MarketQuote? universalis,DateTimeOffset now,bool waitForUniversalis=false)
    {
        lock(gate)
        {
            if(!entries.TryGetValue((world,item),out var entry))return null;
            if(universalis is null && waitForUniversalis)return null;
            if(quotes.TryGetValue((world,item),out var known) && known.Seen==entry.Seen && ReferenceEquals(known.Universalis,universalis))return known.Quote;
            var quote=SeenPrices.Prefer(entry,universalis,now);
            quotes[(world,item)]=(entry.Seen,universalis,quote);
            return quote;
        }
    }
    private readonly Dictionary<(uint World,uint Item),(DateTimeOffset Seen,MarketQuote? Universalis,MarketQuote? Quote)> quotes=[];

    public SeenMarketEntry? Get(uint world,uint item)
    {
        lock(gate)return entries.TryGetValue((world,item),out var entry) ? new SeenMarketEntry{World=entry.World,Item=entry.Item,Listings=[..entry.Listings],ListingsAt=entry.ListingsAt,
            ListingsRequest=entry.ListingsRequest,Sales=[..entry.Sales],SalesAt=entry.SalesAt}:null;
    }

    public List<SeenMarketEntry> Snapshot(){lock(gate){Dirty=false;return entries.Values.Select(entry=>Get(entry.World,entry.Item)!).ToList();}}

    public void Load(IEnumerable<SeenMarketEntry> saved)
    {
        lock(gate)
            foreach(var entry in saved)
                // X-73.1 (0.857): nothing older than KeptFor is loaded.
                if(entry.World!=0 && entry.Item!=0 && entries.Count<MaxEntries && DateTimeOffset.UtcNow-entry.Seen<=SeenPrices.KeptFor)
                    entries[(entry.World,entry.Item)]=new SeenMarketEntry{World=entry.World,Item=entry.Item,ListingsRequest=entry.ListingsRequest,
                        Listings=entry.Listings.Where(listing=>listing.Price>0).Take(SeenPrices.MaxListings).ToList(),ListingsAt=entry.ListingsAt,
                        Sales=entry.Sales.Where(sale=>sale.Price>0).Take(SeenPrices.MaxSales).ToList(),SalesAt=entry.SalesAt};
    }

    public static List<SeenMarketEntry>? Read(string path)=>File.Exists(path) && new FileInfo(path).Length<=64L*1024*1024 ? JsonSerializer.Deserialize<List<SeenMarketEntry>>(File.ReadAllText(path)):null;
    public static void Write(string path,List<SeenMarketEntry> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary=path+".tmp";File.WriteAllText(temporary,JsonSerializer.Serialize(entries));File.Move(temporary,path,true);
    }
}
