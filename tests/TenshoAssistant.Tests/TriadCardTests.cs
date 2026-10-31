using System.Text.Json;
using TenshoAssistant;
using TenshoAssistant.Collections;
using TenshoAssistant.Items;
using TenshoAssistant.MiniGames;

internal static class TriadCardTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static TriadCardDefinition Card(uint id, int stars = 1) => new(id, (int)id + 1000, stars, 4, 2, 3, 4, 0, "", "Description");
    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Triad cards preserve native identity separately from notebook number and side orientation", () =>
        {
            var card = Card(1);
            Check(card.IsValid && card.Id == 1 && card.Number == 1001 && card.IconId == 88001, "identity not replaced with notebook number");
            Check(card.Top == 4 && card.Right == 2 && card.Bottom == 3 && card.Left == 4, "native Dodo orientation retained");
            Check(TriadCardDefinition.Value(10) == "A" && TriadCardDefinition.Value(1) == "1", "display ten as A");
            Check(!(card with { Top = 11 }).IsValid && !(card with { Left = 0 }).IsValid
                && !(card with { Stars = 6 }).IsValid && !(card with { Id = 0 }).IsValid, "invalid native or manual values rejected");
        }),
        ("Triad deck permits exactly the verified rare card limits", () =>
        {
            var cards = Enumerable.Range(1, 6).ToDictionary(id => (uint)id, id => Card((uint)id));
            cards[1] = Card(1, 5); cards[2] = Card(2, 4);
            Check(TriadDeck.Analyze([1,2,3,4,5], cards.GetValueOrDefault).Valid, "one five plus one four");
            cards[1] = Card(1, 4);
            Check(TriadDeck.Analyze([1,2,3,4,5], cards.GetValueOrDefault).Valid, "two four stars allowed");
            cards[3] = Card(3, 4);
            Check(!TriadDeck.Analyze([1,2,3,4,5], cards.GetValueOrDefault).RarityAllowed, "three high rarity forbidden");
            cards[1] = Card(1, 5); cards[2] = Card(2, 5); cards[3] = Card(3);
            Check(!TriadDeck.Analyze([1,2,3,4,5], cards.GetValueOrDefault).RarityAllowed, "two five stars forbidden");
        }),
        ("Triad deck preserves incomplete unknown duplicate and oversized evidence", () =>
        {
            TriadCardDefinition? Find(uint id) => id is >= 1 and <= 5 ? Card(id) : null;
            Check(!TriadDeck.Analyze([1,2], Find).IsComplete, "two is not five");
            Check(TriadDeck.Analyze([1,2,3,4,4], Find).HasDuplicates, "duplicate identity rejected");
            Check(TriadDeck.Analyze([1,2,3,4,5,6], Find).TooManyCards, "oversize persisted deck not silently truncated");
            var input = new List<uint> { 1,2,3,4,900 };
            var result = TriadDeck.Analyze(input, Find); input.Clear();
            Check(!result.Valid && result.UnknownIds.SequenceEqual([900u]), "unavailable saved cards stay unknown");
            Check(!TriadDeck.Analyze([1,2,3,4,5], _ => Card(1)).Valid, "resolver identity mismatch rejected");
            try { ((IList<uint>)result.UnknownIds).Clear(); throw new Exception("mutable evidence"); }
            catch (NotSupportedException) { }
        }),
        ("Triad item joins use AdditionalData identity without conflating card and item IDs", () =>
        {
            var catalog = ItemCatalog.Build([new(9772, "Carte dodo", false, TripleTriadCardId: 1),
                new(1, "Gil", false), new(70000, "Alternate", false, TripleTriadCardId: 1)], new("test", "French", "test", 1));
            var joined = catalog.ForTriadCard(1);
            Check(joined.Select(item => item.Id).SequenceEqual([9772u,70000u]) && catalog.ForTriadCard(9772).Count == 0,
                "all exact typed joins retained and native row IDs distinct");
            Check(catalog.Find(1)!.TripleTriadCardId == 0 && catalog.Find(9772)!.TripleTriadCardId == 1, "shared lookup retains copied identity");
            try { ((IList<ItemDefinition>)joined).Clear(); throw new Exception("mutable join"); }
            catch (NotSupportedException) { }
        }),
        ("Triad acquisitions clear previous owner proof without erasing card values", () =>
        {
            var service = new CollectionService();
            var card = Card(1);
            var entry = new CollectionEntry(CollectionKind.TripleTriad, 1, "Dodo", card.IconId, true, Card: card);
            service.Publish(10, [entry]);
            Check(service.State.Entries.Single().ConfirmedUnlocked && service.State.Entries.Single().Card == card, "copied native identity and proof");
            service.Invalidate();
            Check(service.State.CharacterId == 0 && !service.State.Entries.Single().ConfirmedUnlocked
                && service.State.Entries.Single().Card!.Right == 2, "logout clears acquisition only");
            service.Publish(0, [entry]);
            Check(!service.State.Entries.Single().ConfirmedUnlocked && service.State.ObservedAt is null, "offline never confirms");
        }),
        ("Triad draft deck persists native card IDs and old configuration defaults to empty", () =>
        {
            var config = new Configuration { TripleTriadDeck = [1,2,3,4,5] };
            var loaded = JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(config))!;
            Check(loaded.TripleTriadDeck.SequenceEqual([1u,2u,3u,4u,5u]), "native identities persisted in exact selection order");
            Check(JsonSerializer.Deserialize<Configuration>("{\"Version\":1}")!.TripleTriadDeck.Count == 0, "legacy defaults empty without native modification");
        }),
        ("Triad NPC decks persist with their NPC name and older configuration has none", () =>
        {
            var config = new Configuration { TripleTriadDecks = [new TriadSavedDeck { Name = "Munglig", NpcId = 1019323, Cards = [53,58,196,42,89] }] };
            var loaded = JsonSerializer.Deserialize<Configuration>(JsonSerializer.Serialize(config))!;
            Check(loaded.TripleTriadDecks is [{ Name: "Munglig", NpcId: 1019323 } deck] && deck.Cards.SequenceEqual([53u,58u,196u,42u,89u]), "name, NPC and card order kept");
            Check(JsonSerializer.Deserialize<Configuration>("{\"Version\":1}")!.TripleTriadDecks.Count == 0, "no deck in an older configuration");
        }),
    ];
}
