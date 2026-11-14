# X-343 — Dalamud uploads the board the player sees to Universalis (0.1331)

Read on 2026-11-14 on the user's machine (Dalamud 15.0.4.0), before the fix.

## What was seen

- The user looked at an item on the game's market board, then hovered it with Ctrl+Alt (a forced Universalis request): the line of their
  world showed Universalis instead of the reading of the board they had just seen.

## What Dalamud does

- `Dalamud.dll` (15.0.4.0) holds a market board uploader for Universalis: the type
  `Dalamud.Game.Marketboard.Network.Internal.MarketBoardUploaders.Universalis.UniversalisMarketBoardUploader`, the address
  `https://universalis.app`, and the messages « Market board request resolved, starting upload: item#{CatalogId} listings#{…} » and
  « Universalis data upload for item#{CatalogId} completed ».
- Its setting is `IsMbCollect`, shown in Dalamud's settings as « Anonymously upload market board data ». On the user's machine,
  `%APPDATA%\XIVLauncher\dalamudConfig.json` has `"IsMbCollect": true` (Dalamud's default).
- So when a player opens an item at the board, Dalamud sends the offers and the history it received to Universalis as soon as the request is
  resolved; Universalis' upload time for that world then falls a few seconds after Tensho's own reading of the same board (`SeenMarketEntry.Seen`).

## Consequence and fix

X-292 and X-331's rule « the newest reading of the world wins » compared the two times and let that upload win, although it is the same
reading. Since 0.1331, an upload counts as newer only when it comes more than `SeenPrices.SameReading` (5 minutes, the clock tolerance already
used for sales) after the look; the same bound applies to the data center's cheapest offer, the market page and the retainer comparison.
Another player's upload after that replaces the look. Tests: `SameReading1331Tests`.
