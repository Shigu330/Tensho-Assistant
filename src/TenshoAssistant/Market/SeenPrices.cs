using System.Text.Json;

namespace TenshoAssistant.Market;

/// <summary>X-52 (0.817): one offer as the game's market board shows it: unit price, quality, quantity. No retainer, seller or buyer is kept.</summary>
// X-73.14 (0.870): Own = one of your retainers' offers (the offer's retainer id compared with yours when it was read; only this yes/no is kept, never the id).
public sealed record SeenListing(decimal Price,bool Hq,int Quantity,bool Own=false);
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
    /// <summary>X-73.14 (0.870): your retainers were known when these offers were read, so their Own flags can be trusted.</summary>
    public bool OwnKnown { get; set; }
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
    /// counts while its offers are current (CurrentOffers). docs/project/audits/0.857-community-map.md (market report).
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

    /// <summary>
    /// X-73.12 (0.868, user request: « in the end use more than Universalis »): the market page's board completed by what was seen at the game's board. A world's
    /// offers seen stand in place of Universalis' for that world when seen after its latest review there (or Universalis has none there) and while current
    /// (CurrentOffers); the sales seen are added without twins. No board (source off or failed): what was seen stands alone, its speed worked out from its sales.
    /// The 5 % tax is worked out as Universalis gives it. No retainer is ever kept: the seen offers are marked Seen.
    /// </summary>
    public static MarketBoard? WithSeen(MarketBoard? board,string scope,uint item,IReadOnlyList<(string World,SeenMarketEntry Entry)> seen,DateTimeOffset now)
    {
        var used=seen.Where(pair=>pair.World.Length>0 && pair.Entry.Item==item && (pair.Entry.Listings.Count>0 || pair.Entry.Sales.Count>0)).ToArray();
        if(used.Length==0)return board;
        var offers=(board?.Offers ?? []).ToList();var sales=(board?.Sales ?? []).ToList();var changed=false;
        var known=sales.Select(sale=>(sale.UnitPrice,sale.Quantity,sale.Hq,sale.World,sale.At.ToUnixTimeSeconds())).ToHashSet();
        foreach(var (world,entry) in used)
        {
            if(entry.Listings.Count>0 && now-entry.ListingsAt<=CurrentOffers)
            {
                var latest=offers.Where(offer=>offer.World==world).Select(offer=>offer.ReviewedAt ?? DateTimeOffset.MinValue).DefaultIfEmpty(DateTimeOffset.MinValue).Max();
                if(entry.ListingsAt>latest)
                {
                    offers.RemoveAll(offer=>offer.World==world);
                    offers.AddRange(entry.Listings.Where(listing=>listing.Price>0 && listing.Quantity>0).Select(listing=>new MarketOffer((long)Math.Round(listing.Price),
                        listing.Quantity,listing.Hq,world,"",(long)Math.Round(listing.Price*listing.Quantity*0.05m),entry.ListingsAt,true,entry.OwnKnown ? listing.Own:null)));
                    changed=true;
                }
            }
            foreach(var sale in entry.Sales)
            {
                var row=new MarketSale((long)Math.Round(sale.Price),Math.Max(1,sale.Quantity),sale.Hq,world,sale.At);
                if(row.UnitPrice>0 && row.At<=now.AddMinutes(5) && known.Add((row.UnitPrice,row.Quantity,row.Hq,row.World,row.At.ToUnixTimeSeconds()))){sales.Add(row);changed=true;}
            }
        }
        if(!changed)return board;
        double? Speed(bool? hq)
        {
            if(sales.Count==0)return null;
            var days=Math.Max(1,(now-sales.Min(sale=>sale.At)).TotalDays);
            return Math.Round(sales.Where(sale=>hq is null || sale.Hq==hq).Sum(sale=>(double)sale.Quantity)/days,2);
        }
        return new MarketBoard(board?.Scope ?? scope,item,board?.FetchedAt ?? used.Max(pair=>pair.Entry.Seen),offers.OrderBy(offer=>offer.UnitPrice).ToArray(),
            sales.OrderByDescending(sale=>sale.At).Take(MaxSales).ToArray(),board?.UnitsPerDay ?? Speed(null),board?.NqUnitsPerDay ?? Speed(false),board?.HqUnitsPerDay ?? Speed(true));
    }

    /// <summary>
    /// X-73.14 (0.870): the other sellers' offers on your home world as seen at the game's board, for the retainer comparison: only when your retainers were known
    /// at the reading, while current (CurrentOffers) and newer than Universalis' upload; your own offers left out by their flag. Null otherwise.
    /// </summary>
    public static RetainerItemOffers? RetainerOffers(SeenMarketEntry? entry,string world,RetainerItemOffers? universalis,DateTimeOffset now)
    {
        if(entry is not { OwnKnown:true } || entry.Listings.Count==0 || now-entry.ListingsAt>CurrentOffers)return null;
        if(universalis?.DataAt is { } uploaded && uploaded>=entry.ListingsAt)return null;
        return new(entry.Listings.Where(listing=>!listing.Own && listing.Price>0 && listing.Quantity>0).Select(listing=>new MarketOffer((long)Math.Round(listing.Price),listing.Quantity,
            listing.Hq,world,"",(long)Math.Round(listing.Price*listing.Quantity*0.05m),entry.ListingsAt,true,false)).OrderBy(offer=>offer.UnitPrice).ToArray(),entry.ListingsAt);
    }

    /// <summary>
    /// The quote of what was seen; Universalis' other worlds and region kept from its own quote when there is one.
    /// X-292 (0.1291): marked SeenAtBoard, dated by the look; its fetch time is the later of the look and Universalis' request, so that
    /// « Fetched » moves with a Ctrl+Alt even when the look stays the newest reading of the world.
    /// </summary>
    public static MarketQuote Quote(SeenMarketEntry entry,MarketQuote? universalis,DateTimeOffset now)=>new(entry.World,entry.Item,
        universalis is { } asked && asked.FetchedAt>entry.Seen ? asked.FetchedAt:entry.Seen,entry.Seen,
        Quality(entry.Listings,entry.Sales,false,now),Quality(entry.Listings,entry.Sales,true,now),universalis?.NqCheapest,universalis?.HqCheapest,universalis?.NqRegion,universalis?.HqRegion,true);

    /// <summary>
    /// The seen quote when it is newer than Universalis' data for that world (its upload); otherwise null. X-331 (0.1326, review of 0.1318 I2): an
    /// answer without an upload for the world (none sent to Universalis, UploadedAt null) holds no reading of it: the board seen keeps the line (its
    /// fetch time, « now », used to win over any look).
    /// X-292 (0.1291, user rule of 13/11: for each line, the newest reading wins, the board seen or Universalis, with its date and source): one
    /// rule, whether the price was asked again (Ctrl+Alt) or not.
    /// 0.821 (user report: the other worlds vanished): with no Universalis quote yet, nothing while Universalis is asked (« waitForUniversalis »), so that the request
    /// is still made and its other worlds and region come back; only with that source off does the seen quote stand alone.
    /// </summary>
    public static MarketQuote? Prefer(SeenMarketEntry? entry,MarketQuote? universalis,DateTimeOffset now,bool waitForUniversalis=false)
    {
        if(entry is null || entry.Listings.Count==0 && entry.Sales.Count==0)return null;
        if(universalis is null && waitForUniversalis)return null;
        if(universalis?.UploadedAt is { } uploaded && uploaded>=entry.Seen)return null;
        return Quote(entry,universalis,now);
    }
}

/// <summary>X-52 (0.817): the prices seen, by world and item, safe from any thread, kept in a file beside the discoveries.</summary>
public sealed class SeenPriceStore
{
    public const int MaxEntries=50_000;
    private readonly object gate=new();
    private readonly Dictionary<(uint World,uint Item),SeenMarketEntry> entries=[];
    // X-142 (0.1121, audit A04): changes counted, acknowledged only once written (MarkSaved): a failed write keeps them to save.
    private long changes,saved;
    public bool Dirty
    {
        get{lock(gate)return changes!=saved;}
        private set{if(value)changes++;}
    }
    public int Count{get{lock(gate)return entries.Count;}}
    /// <summary>X-73.12 (0.868): grows at each change, so that a page works its view out again only then.</summary>
    public long Revision{get{lock(gate)return revision;}}
    private long revision;

    /// <summary>A page of offers: a new request replaces the last offers, a later page of the same request adds to them.</summary>
    public void SeeListings(uint world,uint item,uint request,IEnumerable<SeenListing> page,DateTimeOffset at,bool ownKnown=false)
    {
        if(world==0 || item==0)return;
        lock(gate)
        {
            var entry=Entry(world,item);if(entry is null)return;
            if(entry.ListingsRequest!=request || at-entry.ListingsAt>TimeSpan.FromMinutes(2)){entry.Listings=[];entry.ListingsRequest=request;entry.OwnKnown=ownKnown;}
            else entry.OwnKnown&=ownKnown;
            foreach(var listing in page)if(entry.Listings.Count<SeenPrices.MaxListings && listing.Price>0)entry.Listings.Add(listing);
            entry.ListingsAt=at;Dirty=true;revision++;
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
            entry.SalesAt=at;Dirty=true;revision++;
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
    /// <summary>X-75.5 (0.879): the same for the region's cheapest offers, from every world of the region seen (Data Center Travel), newer than Universalis' and current.</summary>
    public MarketQuote? WithRegion(MarketQuote? quote,IReadOnlyCollection<uint> worlds,DateTimeOffset now)
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
        return quote with {NqRegion=SeenPrices.Cheapest(quote.NqRegion,nq,now),HqRegion=SeenPrices.Cheapest(quote.HqRegion,hq,now)};
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
            ListingsRequest=entry.ListingsRequest,OwnKnown=entry.OwnKnown,Sales=[..entry.Sales],SalesAt=entry.SalesAt}:null;
    }

    public List<SeenMarketEntry> Snapshot()=>SnapshotToSave(out _);

    /// <summary>X-142 (0.1121): the entries to write and the change stamp they hold; MarkSaved(stamp) once written.</summary>
    public List<SeenMarketEntry> SnapshotToSave(out long stamp){lock(gate){stamp=changes;return entries.Values.Select(entry=>Get(entry.World,entry.Item)!).ToList();}}

    /// <summary>X-142: the copy holding this stamp is written.</summary>
    public void MarkSaved(long stamp){lock(gate)if(stamp>saved)saved=stamp;}

    public void Load(IEnumerable<SeenMarketEntry> saved)
    {
        lock(gate)
        {
            revision++;
            foreach(var entry in saved)
                // X-73.1 (0.857): nothing older than KeptFor is loaded.
                if(entry.World!=0 && entry.Item!=0 && entries.Count<MaxEntries && DateTimeOffset.UtcNow-entry.Seen<=SeenPrices.KeptFor)
                    entries[(entry.World,entry.Item)]=new SeenMarketEntry{World=entry.World,Item=entry.Item,ListingsRequest=entry.ListingsRequest,OwnKnown=entry.OwnKnown,
                        Listings=entry.Listings.Where(listing=>listing.Price>0).Take(SeenPrices.MaxListings).ToList(),ListingsAt=entry.ListingsAt,
                        Sales=entry.Sales.Where(sale=>sale.Price>0).Take(SeenPrices.MaxSales).ToList(),SalesAt=entry.SalesAt};
        }
    }

    public static List<SeenMarketEntry>? Read(string path)=>File.Exists(path) && new FileInfo(path).Length<=64L*1024*1024 ? JsonSerializer.Deserialize<List<SeenMarketEntry>>(File.ReadAllText(path)):null;
    /// <summary>
    /// X-171 (0.1216, audit of 12/11, R02): two game clients on one profile each held the file's entries of the start and wrote back their whole
    /// copy: the second write erased what the first client had seen since. The write now holds the profile's lock file (« .lock » beside it) and
    /// joins what the file holds at that moment (Joined) before replacing it; a file that cannot be read is replaced by this client's entries, as before.
    /// </summary>
    public static void Write(string path,List<SeenMarketEntry> entries)
    {
        using var held=HoldLock(path+".lock");
        List<SeenMarketEntry>? onDisk;
        try{onDisk=Read(path);}
        catch(Exception error) when(error is IOException or JsonException or UnauthorizedAccessException){onDisk=null;}
        AtomicFile.Write(path,JsonSerializer.Serialize(Joined(entries,onDisk,DateTimeOffset.UtcNow)));
    }

    /// <summary>
    /// X-171 (0.1216): this client's entries joined with the file's: an item and world seen by one client only is kept; seen by both, the offers of the
    /// newer reading and the sales of both without twins (SeenPrices.MergeSales: writing the same entries again never doubles a sale); nothing older
    /// than KeptFor taken from the file; at most MaxEntries, the most recently seen.
    /// </summary>
    public static List<SeenMarketEntry> Joined(IReadOnlyList<SeenMarketEntry> mine,IReadOnlyList<SeenMarketEntry>? onDisk,DateTimeOffset now)
    {
        var all=new Dictionary<(uint World,uint Item),SeenMarketEntry>();
        foreach(var entry in mine)all[(entry.World,entry.Item)]=entry;
        foreach(var other in onDisk ?? [])
        {
            if(other is null || other.World==0 || other.Item==0 || now-other.Seen>SeenPrices.KeptFor)continue;
            if(!all.TryGetValue((other.World,other.Item),out var kept)){all[(other.World,other.Item)]=other;continue;}
            var newer=other.ListingsAt>kept.ListingsAt ? other:kept;
            all[(other.World,other.Item)]=new SeenMarketEntry{World=kept.World,Item=kept.Item,Listings=[..newer.Listings],ListingsAt=newer.ListingsAt,
                ListingsRequest=newer.ListingsRequest,OwnKnown=newer.OwnKnown,Sales=SeenPrices.MergeSales(kept.Sales,other.Sales,now),
                SalesAt=kept.SalesAt>other.SalesAt ? kept.SalesAt:other.SalesAt};
        }
        return all.Values.OrderByDescending(entry=>entry.Seen).ThenBy(entry=>entry.World).ThenBy(entry=>entry.Item).Take(MaxEntries).ToList();
    }

    // The lock file held for the read and the write (FileShare.None): the other client waits for it, 10 s at most, then the save fails and is kept.
    // X-310 (0.1309): the fair lock (FairFileLock): a client saving in a row lets the waiting client go first.
    private static FileStream HoldLock(string path)=>FairFileLock.Hold(path,10_000,error=>error);
}
