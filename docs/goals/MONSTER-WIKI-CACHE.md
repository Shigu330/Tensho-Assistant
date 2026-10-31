# Wiki monster sources — 2026-10-30

In Monster sources, expand **Sources complémentaires · Console Games Wiki**.
**Consulter le wiki (cache)** reuses fresh data; **Actualiser le wiki** forces a
new lookup. No request is made by selecting an item or drawing a panel.

The shared Garland transport resolves the English item name with identity checks.
The wiki MediaWiki parse API is queried for that title. Only the table with
Enemy/Level/Location headers is interpreted. Foreign titles are rejected. HTML
processing uses bounded regex execution, row/text limits, and a bounded HTTP body.
No wiki source is converted into an invented native monster ID.

Cache: at most 16 documents in memory, plus the shared bounded DataCache on disk,
using the existing **Sources** retention preference. Results retain their fetched
time, wiki URL and revision. Force bypasses disk and memory reads. Expired data
remains visible as expired until explicit refresh. Selection changes and disposal
invalidate pending publication. Failed refresh retains earlier dated data.

Verified: Release build, 469/469 managed tests, localization and package checks.
The live `--probe-wiki-rroneek` command returned:

```text
Rroneek | 96 | Shaaloani (21.8,10.2)
```

Source: https://ffxiv.consolegameswiki.com/wiki/Rroneek_Chuck

Not validated: in-game rendering. Wiki zone names and coordinate text are displayed
as source information; automatic native map linking is not implemented. Missing
tables, changed markup or page-name ambiguity may limit coverage. Other source
types (vendors, recipes, duties) are intentionally not parsed by this component.
