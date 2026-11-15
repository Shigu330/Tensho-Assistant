// 0.1432, X-433 (burst reduced, docs/evidence/0.1432-background-collection-stall.md): every managed thread stopped during a background
// collection right after a burst of large allocations by Tensho's cache reads (a 10 MB cache file read at once, its JsonElement's two copies
// of about 16 MB) and a catalogue grown by doubling. The burst is not the only cause of the stop (0.1433: the stop came back without it;
// X-433 stays open). The cache is read as a stream into a typed envelope: a 4 MB entry is read back the same without any byte[] of 1 MB or
// more (the runtime's allocation ticks); the item catalogue is built sized once and sorted in place, in the same order.
using System.Diagnostics.Tracing;
using System.Reflection;
using TenshoAssistant.Caching;
using TenshoAssistant.Items;

[SerialTests("counts the large allocations of the whole process while a cache entry is read")]
internal static class CacheStream1432Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static T Wait<T>(Task<T> task) => task.GetAwaiter().GetResult();
    public sealed record Row(uint Id, string Name, string Note);

    /// <summary>The large byte arrays the runtime reports (allocation ticks), with their time.</summary>
    private sealed class LargeBytes : EventListener
    {
        public readonly List<(DateTime At, long Size)> Seen = [];
        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name == "Microsoft-Windows-DotNETRuntime") EnableEvents(source, EventLevel.Verbose, (EventKeywords)0x1);
        }
        protected override void OnEventWritten(EventWrittenEventArgs written)
        {
            if (written.EventName?.StartsWith("GCAllocationTick", StringComparison.Ordinal) != true || written.PayloadNames is not { } names || written.Payload is not { } values) return;
            var type = names.IndexOf("TypeName") is int at && at >= 0 ? values[at] as string : null;
            var size = names.IndexOf("ObjectSize") is int sized && sized >= 0 && values[sized] is ulong objectSize ? (long)objectSize : 0;
            if (type == "System.Byte[]" && size >= 1024 * 1024) lock (Seen) Seen.Add((written.TimeStamp.ToUniversalTime(), size));
        }
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Cache stream 1432: a 4 MB entry is read back the same, without any byte[] of 1 MB or more", () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "Tensho-CacheStream1432-" + Guid.NewGuid().ToString("N"));
            try
            {
                var rows = Enumerable.Range(1, 40_000).Select(i => new Row((uint)i, "Row " + i, new string('x', 60))).ToList();
                var date = DateTimeOffset.UtcNow.AddMinutes(-3);
                using (var writer = new DataCache(root))
                {
                    Check(Wait(writer.WriteAsync("stream", "rows", "v1", rows, date)), "written");
                    Wait(writer.FlushAsync().ContinueWith(_ => true));
                }
                var file = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories).Single(path => new FileInfo(path).Length > 3_000_000);
                Check(new FileInfo(file).Length > 3_000_000, "a large entry: " + new FileInfo(file).Length);
                using var listener = new LargeBytes();
                // The listener works: a 2 MB marker is seen before the read.
                var marker = new byte[2 * 1024 * 1024];
                var waited = System.Diagnostics.Stopwatch.StartNew();
                while (waited.Elapsed < TimeSpan.FromSeconds(20)) { lock (listener.Seen) if (listener.Seen.Count > 0) break; Thread.Sleep(20); GC.KeepAlive(new byte[2 * 1024 * 1024]); }
                GC.KeepAlive(marker);
                Check(listener.Seen.Count > 0, "the listener sees a large byte[]");
                Thread.Sleep(200);
                DateTime from;
                lock (listener.Seen) { listener.Seen.Clear(); from = DateTime.UtcNow; }
                using var reader = new DataCache(root);
                var back = Wait(reader.ReadAsync<List<Row>>("stream", "rows", "v1"));
                var to = DateTime.UtcNow;
                Thread.Sleep(300);
                Check(back is not null && back.RecordedAt == date && back.Value.Count == rows.Count && back.Value.SequenceEqual(rows), "read back the same");
                lock (listener.Seen)
                {
                    var during = listener.Seen.Where(seen => seen.At >= from && seen.At <= to).ToList();
                    Check(during.Count == 0, "no byte[] of 1 MB or more during the read: " + string.Join(", ", during.Select(seen => seen.Size)));
                }
                Check(Wait(reader.ReadAsync<List<Row>>("stream", "rows", "v2")) is null && Wait(reader.ReadAsync<List<Row>>("other", "rows", "v1")) is null, "another identity: a miss");
                Wait(reader.FlushAsync().ContinueWith(_ => true));
            }
            finally { try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        }),
        ("Cache stream 1432: the item catalogue keeps the order of OrderBy(name, ordinal).ThenBy(id), sized from its source", () =>
        {
            var random = new Random(1432);
            var names = new[] { "Potion", "potion", "Éclat de feu", "Eclat de feu", "Sirop", "Objet", "Objet 2", "Objet 10" };
            var items = Enumerable.Range(1, 3000).Select(i => new ItemDefinition((uint)i, names[random.Next(names.Length)] + (i % 7 == 0 ? "" : " " + random.Next(50)), false))
                .OrderBy(_ => random.Next()).ToList();
            var catalog = ItemCatalog.Build(items, new ItemCatalogMetadata("test/Item", "French", "game=test", 1));
            var searchable = ((ItemDefinition Item, string SearchName)[])typeof(ItemCatalog).GetField("searchable", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(catalog)!;
            var expected = searchable.OrderBy(entry => entry.SearchName, StringComparer.Ordinal).ThenBy(entry => entry.Item.Id).ToArray();
            Check(searchable.Length == items.Count && searchable.SequenceEqual(expected), "the same order as OrderBy.ThenBy");
            var source = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Items/ItemCatalog.cs"));
            Check(source.Contains("new Dictionary<uint, ItemDefinition>(known)", StringComparison.Ordinal) && source.Contains("new List<(ItemDefinition Item, string SearchName)>(known)", StringComparison.Ordinal), "sized from the source");
        }),
    ];
}
