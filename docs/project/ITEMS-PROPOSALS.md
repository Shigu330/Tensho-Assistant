# Items, bank, sorting and market: improvement proposals (audit of 2026-11-02, step e)

> *Traduction française plus bas.*

List drawn up by a read-only multi-agent audit (0.328). **Nothing was implemented** at the time of the audit (re-read on 2026-11-03: the proposals made since then, at step g, carry a "State" line): you choose what we add
(rule of 2026-11-02, [ROADMAP](ROADMAP.md) "Decisions postponed to the end"). The defects found by the same audit are fixed in
0.329 ([CHANGELOG](../../CHANGELOG.md)). Several proposals overlap gaps already kept for step g)
([GAPS](GAPS.md), I-*): they say so.

Value and size are the audit's estimates. "Probe": a read of the game or an in-game capture is needed before coding.

## I0 — Count worn pieces in the stocks and in "owned"

- **State (re-read on 2026-11-05)**: DONE 0.549.
- **Value**: high; main file: `src/TenshoAssistant/Inventory/DalamudInventoryReader.Dalamud.cs`.
- **What the player gets**: Worn gear counts as owned. The Fashion Report no longer says "not owned" for the piece you are wearing. The hover stock and the item sheet include it, on a "Worn" line. The "owned" filters of the other tools count it too.
- **How, size, dependencies**: Add GameInventoryType.EquippedItems (1000) to the capture done on the framework thread, with the same IGameInventory reader and the same coverage. Add a "Worn" StorageLabel (PlayerBags category), translated in Inventory/StorageNames.cs. The sort (InventorySortScope, containers 0-3) and the Bank grid (MainWindow.Bank.cs:184-186, containers 0-3) are not touched: a test must verify this, with a survey of all the consumers of inventory.Current (Diagnostics, sort, Bank). Size S. Probe: none for the read; in-game check afterwards (put on then remove a piece). Decision: count worn gear everywhere as owned (recommended: yes, on a separate line to stay visible).
- **Source**: Inventory/DalamudInventoryReader.Dalamud.cs:11-13 captures only bags 1 to 4 and the crystals. Inventory/ItemHoldings.cs:34-43 counts only containers 0-3 and 2001. MainWindow.FashionWeek.cs:41 (FashionOwned) goes through GetRecipeHoldings, hence without the worn gear. Only the Gear page handles it separately (MainWindow.Gear.cs:512, IsWorn). docs/evidence/0.318-plugin-gaps.json (Allagan Tools): "Worn gear, currencies and crystals of the retainers counted in the stocks" = missing. EquippedItems is already read elsewhere: Gear/GearLoader.Dalamud.cs:127 and Craft/CraftLiveReader.Dalamud.cs:22.

## I1 — Flag and erase the record of a dismissed retainer or of an old storage

- **State (re-read on 2026-11-05)**: DONE 0.576.
- **Value**: high; main file: `src/TenshoAssistant/Inventory/BankService.cs`.
- **What the player gets**: A dismissed retainer no longer counts forever in your stocks. The Storage page flags the registered retainers that no longer appear at the summoning bell, and an "Erase this read" button (two clicks) removes them. Your totals become correct again: hover, sheet, the "missing" of crafting, Fashion Report.
- **How, size, dependencies**: Add InventoryBankService.DeleteAsync(BankKey), in the same bounded queue and with the same atomic write (FileInventoryBankStore). Button in "Imports" (DrawImportManager) with the two-click confirmation of MainWindow.Alarms.cs. An "absent from the summoning bell" mention when KnownRetainers is known and does not contain this retainer; never an automatic erasure. Size S-M. Probe: none. Decision: no new one (part of the I-03 choice, doable before g); to confirm: manual erasure only (recommended).
- **Source**: BACKLOG.md:124: "possible manual deletion of an old record" remains a separate extension. GAPS.md:114 (I-03), Choice: "Erase this character" button. Inventory/BankService.cs:30 has only ImportAsync, no deletion. Inventory/ItemHoldings.cs:65-76 counts every registered retainer record for the character. MainWindow.Bank.cs:428-441 compares the summoning bell list (BankInventoryBridge.Dalamud.cs:188-197, KnownRetainers) with the records only to find the retainers not imported, never the surplus records.

## I2 — I-01, anticipated part: last sale and refresh key on hover

- **State (re-read on 2026-11-03)**: DONE 0.343 (I-01: last sale, Ctrl+Alt to refresh).
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.MarketHover.cs`.
- **What the player gets**: Under an item's tooltip, you already see the lowest NQ/HQ price of the world and the cheapest of the data center. To these are added the last sale (price and date) and a key to hold to re-read the price, without opening the Market page. Afterwards, after a probe: region price, sales per day and average price.
- **How, size, dependencies**: Step 1 (S, no probe): display RecentSale/SaleAt NQ and HQ in DrawHoveredPrices. The key is read on the framework thread (IKeyState, in Items/ItemHoverReader.Dalamud.cs); it requests a forced read, at most every 3 s (cadence of Market/MarketService.cs:138). "Refresh key" setting (none / Alt / Ctrl / Shift) with tooltip and ↺. Step 2 (S): probe of an aggregated response (truncated response in docs/evidence), then read of minListing.region, dailySaleVelocity and averageSalePrice in MarketQuote.Parse (same request; raise the cache version "universalis-aggregated-v2"). The stack price waits for the probe of the hovered quantity (shared with I-06). Decision: the default key (recommended: Alt, like Price Insight, with "none" possible).
- **Source**: GAPS.md:112 (I-01), Choice: "price on hover only (no preloading), refresh key". Market/MarketQuote.cs:37 already reads recentPurchase.world, displayed only on the Market page (MainWindow.Market.cs:65-66). DrawHoveredPrices (MainWindow.MarketHover.cs:406-429) displays only Minimum. docs/evidence/0.318-plugin-gaps.json (Price Insight): "Last purchase on hover" and "Refresh with Alt" = partial. The fields region, dailySaleVelocity and averageSalePrice cited by GAPS are proven by no capture: docs/evidence/0.270-universalis-listings.md covers only /api/v2/{scope}/{item}.

## I3 — NPC vendor prices in the preparation cost and on the Market page

- **State (re-read on 2026-11-05)**: DONE 0.575.
- **Value**: high; main file: `src/TenshoAssistant/MainWindow.PreparationCost.cs`.
- **What the player gets**: For each ingredient sold for gil by an NPC vendor, the preparation cost shows the NPC price next to the market minimum and can keep the cheaper one. The Summary of the Market page says "Sold by an NPC: X gil" (name and place, link to the vendor). You no longer buy on the market what costs less at a vendor.
- **How, size, dependencies**: Pure function "NPC price in gil" (cheapest NpcShopKind.Gil offer, quality of the offer NpcShopSale.Hq) passed to PreparationCost.BuildDemands. New column, "NPC" status and a quality choice "cheapest, NPC included". Market page: a line in the Summary with DrawVendorLink. No network request. Size S-M. Probe: none (sheets already read); an in-game check on a known item. Dependency: part of A-06 (Crafting domain), doable before g. Decision: does the NPC enter the "cheapest" total (recommended: yes, on a line marked "NPC").
- **Source**: GAPS.md:77 (A-06) cites as data the "GilShopItem vendor prices (Npcs/NpcVendorLoader.Dalamud.cs)", Choice "Add". Market/PreparationCost.cs:86-99 reads only the market (a non-tradable item stays NotMarketable). Npcs/NpcVendorCatalog.cs:7-15: NpcShopKind.Gil and costs in item 1 (gil), already loaded. docs/evidence/0.318-plugin-gaps.json (SimpleMarketBoard): "highlight when an NPC sells cheaper" = missing.

## I4 — Batched Universalis prices: one request for a whole list

- **Value**: high; main file: `src/TenshoAssistant/Market/MarketService.cs`.
- **What the player gets**: "Read prices" of the preparation cost (and of future lists) answers in one request instead of one per ingredient: a few seconds instead of about 100 s for 32 ingredients, without risking the 2-minute stop, with far fewer requests to Universalis. It also makes it possible to price, on click, a storage or a submarine route at the market price.
- **How, size, dependencies**: Probe first: GET aggregated/{world}/{id1,id2,…} with curl (maximum number of items, field of the failed items), response truncated in docs/evidence. Then RequestBatchAsync(ids) in MarketService: same lock, same 3 s pause, bounded response, memory and disk cache per item. PreparationPriceSession uses it, with the limit raised to the proven value. Size M. Dependencies: none; it then serves I-13, A-06, f) 2, and a possible "storage value at the market price" (Retainer Inventory Price in 0.318-plugin-gaps.json, outside the 103 gaps: to decide). Decision: the new limit (recommended: the one proven by the probe, 100 if confirmed).
- **Source**: Market/MarketService.cs:138: 3 s between two requests, one at a time. Market/PreparationPriceSession.cs:22: 32 items at most; line 28: stop after 2 minutes. MainWindow.PreparationCost.cs:94 displays the limit notice. MarketQuote.Parse (Market/MarketQuote.cs:21-31) already walks a "results" array by itemId. ROADMAP "Decided on 2026-11-02", f) 2: market price of submarine routes. Cadence of the sources: docs/evidence/0.246-cache-audit.md.

## I5 — Preparation cost at the data center price (option of A-06)

- **State (re-read on 2026-11-05)**: DONE 0.574.
- **Value**: medium; main file: `src/TenshoAssistant/Market/PreparationCost.cs`.
- **What the player gets**: The preparation cost is computed at the current world or at the cheapest of the data center, with the world where to buy, for whoever changes world to do their shopping.
- **How, size, dependencies**: "Scope: world / data center" combo with tooltip, remembered with ↺. Each line states the world (MarketState.NameOf). Text copy completed. No new request. Size S. Probe: none. Dependency: part of A-06, doable before g. Decision: no new one (option already chosen).
- **Source**: GAPS.md:77 (A-06), Choice: "lowest listing of the world (data center as an option)". Market/MarketQuote.cs:41-44 already reads NqCheapest/HqCheapest (data center minimum and its world) in the same response. Market/PreparationCost.cs:93-96 uses only the world's Nq/Hq.Minimum.

## I6 — I-06, anticipated part: NPC sale price, per unit

- **State (re-read on 2026-11-03)**: DONE 0.346 (I-06, NQ price only).
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.MarketHover.cs`.
- **What the player gets**: Under the tooltip and in the header of the item sheet: "NPC sale: X gil each", to choose quickly between the market and the vendor. The total of the stack comes after the probe of the hovered quantity.
- **How, size, dependencies**: One line in the hover panel: fifth part "NPC price", with its box, its tooltip and its ↺, like the HoverShow*. Same line in the "facts" of the sheet. Value 0: nothing is displayed (the "unsellable" meaning is not proven). HQ: no price until it is captured, with no guessed coefficient. Size S. Probe: quantity of the hovered stack (AgentItemDetail or InventoryItem, shared with I-01) for the total; in-game capture for an HQ price. Decision: no new one.
- **Source**: GAPS.md:117 (I-06), Choice "Add", depends on the hovered quantity. Items/ItemCatalog.cs:11 (SellPrice) comes from Item.PriceLow (Items/ItemCatalogLoader.Dalamud.cs:138); only the sort uses it (MainWindow.SortRules.cs). The header of the sheet (MainWindow.ItemSheet.cs:50-60) does not display it.

## I7 — I-12, anticipated part: category and slot filters, levels in the results

- **State (re-read on 2026-11-03)**: DONE 0.353 (I-12).
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.Items.cs`.
- **What the player gets**: The item search accepts a filter by game category, equipment slot and level; each result shows its category and its levels. Finding "all level 90 rings", or a material among homonyms, becomes direct.
- **How, size, dependencies**: ItemCatalog.Search(query, limit, filter) with category, slot (EquipmentSlots) and level bounds; empty query allowed when a filter is set. Combos and fields with tooltip, "Clear filters" button. Recomputed only when the query or a filter changes (as today, MainWindow.Items.cs:679-684). Size S. Probe: none. The job filter (ClassJobCategory) and the key items (EventItem) stay in g: columns to add to the catalog. Decision: no new one.
- **Source**: GAPS.md:123 (I-12), Choice "Add". Items/ItemCatalog.cs:86-105 searches only by name or ID. MainWindow.Items.cs:702-716: a result is only an icon and a name. The data already exist: ItemCatalog.Categories (Items/ItemCatalog.cs:38, 45-47); Equipment, ItemLevel and EquipLevel in ItemDefinition.

## I8 — X-07, anticipated part: open the item on Universalis, Garland Tools, Teamcraft or the wiki

- **State (re-read on 2026-11-05)**: DONE 0.561.
- **State (re-read on 2026-11-03)**: partly 0.349 (X-07: wiki and Lodestone); Universalis, Garland Tools and Teamcraft not in this menu.
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.DutyFinder.cs`.
- **What the player gets**: Right click on an item, in the Tensho windows and in the "Tensho" menu of the game: "Open on…" opens the item's page in the browser.
- **How, size, dependencies**: "Open on" submenu: Universalis (/market/{id}), Garland Tools (#item/{id}), Teamcraft (item sheet by ID), consolegameswiki (English title). Lodestone: search by name only, because the game data give no Lodestone identifier. Util.OpenLink on click only. "External links" box with tooltip and ↺. Credits and manifest completed (sites named). Size S. Probe: check each URL form once (docs/evidence). Decision: the list of sites (recommended: the four above) and the /wiki, /lodestone commands (recommended: no, the menu is enough).
- **Source**: GAPS.md:199 (X-07), Choice "Add". No OpenLink in the code (search of 2026-11-02). Menus concerned: ItemLinksMenu (MainWindow.DutyFinder.cs:145-160) and Items/ItemContextMenuBridge.Dalamud.cs:60-80. The English name, already loaded (Items/ItemCatalog.cs:11, EnglishName), gives the wiki title.

## I9 — Copy the item name and item link in the chat (local)

- **State (re-read on 2026-11-05)**: DONE 0.561, with I8.
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.DutyFinder.cs`.
- **What the player gets**: Right click on an item: "Copy the name", to paste into the game's market search. "Link in my chat": Tensho writes into your own chat alone a clickable link of the item, which you then share yourself if you wish.
- **How, size, dependencies**: ImGui.SetClipboardText(item.Name). Link: SeStringBuilder.AddItemLink(id, hq) then IChatGui.Print, a local message: nothing is sent to the server. Same entries in the game menu. Size S. Probe: none. Decision: add these entries, not chosen in c) (recommended: "Copy the name" yes, the link as an option).
- **Source**: ItemLinksMenu (MainWindow.DutyFinder.cs:145-160) has neither copy nor link; no ItemPayload in the code. docs/evidence/0.318-plugin-gaps.json (Item Search): "Link the item in the chat without owning it" = missing. This gap is not part of the 103 of GAPS.

## I10 — Crystals and gil of the retainers in the retainer record

- **State (re-read on 2026-11-03)**: partly: retainers' gil in the gil journal (0.351); retainers' crystals not verified.
- **Value**: medium; main file: `src/TenshoAssistant/Inventory/BankInventoryBridge.Dalamud.cs`.
- **What the player gets**: The crystals entrusted to the retainers count in the Crystals tab (one column per retainer), in the "missing" of crafting and on hover. The gil of each retainer is kept, with the date of the record.
- **How, size, dependencies**: Probe first: are RetainerCrystals (12001) and RetainerGil (12000) loaded when InventoryRetainer is open (IsLoaded, size)? Capture by tools/ApiProbe or the log in developer mode, in docs/evidence. Then a separate container group (new BankStorageKind, or schema 2 of the file) so as to change neither the "exact group" validation (BankEntryValidator) nor the sort scope (10000-10006). Size M. Dependency: none; prepares I-03 and I-04. Decision: advance this part before g (recommended: yes, crystals first).
- **Source**: Inventory/BankModels.cs:9: a retainer covers only containers 10000-10006. The Crystals tab (MainWindow.Items.cs:54-89) reads only container 2001 of the character. BACKLOG.md:124: "equipment/crystals/market of the retainers" not included. docs/evidence/0.318-plugin-gaps.json: Allagan Tools "crystals of the retainers counted in the stocks" = missing, Crystal Terror = partial. GAPS.md:114-115 (I-03, I-04) rest on data of the same nature.

## I11 — Sortable list view of the Bank

- **State (re-read on 2026-11-05)**: DONE 0.594.
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.Bank.cs`.
- **What the player gets**: In addition to the 5 × 7 grids, a "List" view: one item per line, with NQ, HQ, total and the storages where it is, sortable by name, quantity or category. With several retainers, you finally see at a glance where your materials are.
- **How, size, dependencies**: "Grid / List" toggle, remembered, with tooltip and ↺. ImGui table sortable by header, with clipper, modelled on MainWindow.MarketBoard.cs. Rows recomputed only when the source or a filter changes. Search aligned with the catalog's normalisation. Size S-M. Probe: none. Decision: default view (recommended: the grid, as today).
- **Source**: MainWindow.Bank.cs:181-203 already computes the rows aggregated by item (bankSourceRows). The display exists only as grids (MainWindow.Bank.cs:303-370). The Bank search (MatchesBankFilters, MainWindow.Bank.cs:388-403) does not strip accents, unlike the item search (Items/ItemCatalog.cs:114-123). docs/evidence/0.318-plugin-gaps.json: Allagan Tools "saved filter lists" and AetherBags = partial.

## I12 — Glamour dresser report: pieces for the armoire and duplicates

- **State (re-read on 2026-11-03)**: DONE 0.357 (C-03, pieces for the armoire) and 0.358 (C-07, identical looks).
- **Value**: medium; main file: `src/TenshoAssistant/Inventory/BankInventoryBridge.Dalamud.cs`.
- **What the player gets**: A report, moving nothing, lists the glamour dresser pieces that the armoire can take, and the duplicate pieces between glamour dresser, armoire, bags and armoury chest. You know what to remove to free glamour dresser slots.
- **How, size, dependencies**: Pure projection on the records (BankEntry GlamourDresser and Armoire, bags, armoury chest): "accepted by the armoire" = item present in cabinetRows; duplicate = same item in two storages. A section of the Storage page, recomputed only when a record changes. Size S-M. Probe: none new; in-game validation of 0.236-0.240 first. Decision: add this report, new (recommended: yes, after this in-game validation).
- **Source**: BankInventoryBridge.Dalamud.cs:224-255 already copies the content of the glamour dresser and of the armoire; cabinetRows gives the items accepted by the armoire (Cabinet sheet). GAPS.md:121 (I-10) follows the same spirit: "report only, no move". STATUS, "To validate in game": the reading of the glamour dresser and of the armoire (0.234-0.240) is not yet validated in game.

## I13 — I-10, anticipated part: unmerged stacks

- **State (re-read on 2026-11-03)**: DONE 0.348 (I-10).
- **Value**: medium; main file: `src/TenshoAssistant/Items/ItemCatalogLoader.Dalamud.cs`.
- **What the player gets**: A report of the items spread over several incomplete stacks in the same storage (bags, saddlebags, retainers), for example: "3 stacks of Iron Ore (12, 40, 99) would fit in 2".
- **How, size, dependencies**: Add Item.StackSize to the catalog (ItemCatalogLoader.Dalamud.cs:135-142, with the item cache version raised). Pure projection per storage: same item, same quality, NQ and HQ separate. The "armoury chest outside the gear sets" part waits for a probe of RaptureGearsetModule and stays in g. Size S. Probe: none for the stacks. Decision: no new one.
- **Source**: GAPS.md:121 (I-10), Choice: "report only, no move". ItemDefinition (Items/ItemCatalog.cs:9-13) has no stack size. The stacks per slot are already captured (Inventory/InventorySnapshot.cs, InventoryStack).

## I14 — Undo the last sort

- **State (re-read on 2026-11-05)**: DISCARDED at the review with the user ([global plan](GLOBAL-PLAN.md)).
- **Value**: medium; main file: `src/TenshoAssistant/Inventory/InventoryVisualSortBridge.Dalamud.cs`.
- **What the player gets**: After "Apply", an "Undo the last sort" button restores the previous order of the bags or of the open storage, as long as nothing has moved since.
- **How, size, dependencies**: Keep the previous order (managed copy of the pages and slots) with the verified snapshot. "Undo" builds an inverse plan subject to the same checks (plan.Matches, RequireOpenForSort, SortFunctionIndex). The undo is lost when changing character or storage and at the slightest item move. Size S-M. Probe: none; in-game test. Decision: a single undo level, for the session (recommended).
- **Source**: InventoryVisualSortBridge.Dalamud.cs:75-84 copies the original native order before any write; lines 99-108: write after all the checks. ROADMAP, SORT-001 (SortaKinda reference). Project rule: a sort applies only on click, verified against what is open.

## I15 — Share a sort template as text (copy / paste)

- **State (re-read on 2026-11-05)**: DONE 0.566.
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.SortTemplates.cs`.
- **What the player gets**: A storage template is copied as text and pasted at another player's or on another PC.
- **How, size, dependencies**: Export: versioned JSON, compressed in base64, with a prefix like "Tensho-Tri-1:", put in the clipboard. Import: bounded size (for example 64 KiB), strict read then InventorySortTemplates.Validate, new identifier, unique name. The item and category identifiers are those of the game, independent of the language. Tooltips on both buttons. Size S. Probe: none. Decision: the format (recommended: this one).
- **Source**: Inventory/InventorySortTemplates.cs:3-5: a template contains neither character nor retainer. MainWindow.SortTemplates.cs:13-60: create, update, delete, load; neither export nor import. docs/evidence/0.318-plugin-gaps.json (SortaKinda): "import and export of the rules" = partial.

## I16 — Commands "/tensho objet" and "/tensho prix"

- **State (re-read on 2026-11-05)**: DONE 0.555.
- **Value**: medium; main file: `src/TenshoAssistant/Plugin.cs`.
- **What the player gets**: "/tensho objet Iron Ore" opens the item sheet, or the search if there are several results; "/tensho prix …" opens the Market page. Both also work in a game macro.
- **How, size, dependencies**: Read the arguments (French and English words), search with ItemCatalog.Search, then SelectItem and OpenItemSheet, or OpenRoute(Market). Translate the command help (Plugin.CommandHelp). Size S. Probe: none. Decision: the keywords (recommended: objet/item and prix/price; marchand/vendor as an option).
- **Source**: Plugin.cs:377-380: only "peche" is recognised. docs/evidence/0.318-plugin-gaps.json (Item Vendor Location): "Search command (/pvendor name)" = missing.

## I17 — I-13, anticipated part: gil value of a currency

- **State (re-read on 2026-11-03)**: DONE 0.354 (I-13).
- **Value**: medium; main file: `src/TenshoAssistant/MainWindow.ItemUses.cs`.
- **What the player gets**: In "Uses › Currency" of a currency item (seals, marks, scrip…), a "Read prices" button gives for each article its market value and the gain in gil per unit of currency, from the most profitable to the least profitable.
- **How, size, dependencies**: Bounded list launched on click: PreparationPriceSession, or better the batched request of the proposal "Batched Universalis prices". Value = world minimum ÷ cost. Non-tradable articles marked "not sellable". The balance and "how many can I buy" wait for I-02; the coded currencies stay "unknown". Size M. Dependency: the Universalis batches (recommended). Decision: no new one.
- **Source**: GAPS.md:124 (I-13), Choice "Add", depends on the balances (I-02) and on the coded currencies. MainWindow.ItemUses.cs:81-108 lists the articles and their cost, without value. docs/evidence/0.318-plugin-gaps.json: Currency Profit Scanner and Currency Spender.

## I18 — I-07, anticipated part: copy "lowest price − 1" from the Market page

- **State (re-read on 2026-11-05)**: DONE 0.567.
- **State (re-read on 2026-11-03)**: partly 0.346 (I-07: when the offers are opened in the game); not from the Market page.
- **Value**: low; main file: `src/TenshoAssistant/MainWindow.MarketBoard.cs`.
- **What the player gets**: On the Listings tab of Tensho, a button copies the lowest price minus 1 (NQ or HQ). Your own retainers are marked, so as not to undercut yourself.
- **How, size, dependencies**: Button with tooltip, which ignores the listings of your retainers (KnownRetainers) and displays the date of the listing, because Universalis can lag behind the game. The full version (offers of the game read when the list opens, IMarketBoard) stays in g. Size S. Probe: none. Decision: do this version early (recommended: only if you want it before g; otherwise wait for the full I-07, more reliable).
- **Source**: GAPS.md:118 (I-07), Choice: copy to the clipboard only. MainWindow.MarketBoard.cs:161-216 displays the listings with the retainer's name, without copy. The names of your retainers are known after the summoning bell is opened (BankInventoryBridge.Dalamud.cs:188-197).

## I19 — English item name in the sheet and the tooltip

- **State (re-read on 2026-11-05)**: DONE 0.562.
- **Value**: low; main file: `src/TenshoAssistant/MainWindow.ItemSheet.cs`.
- **What the player gets**: Optionally, the item's English name is displayed under its name, useful with English guides and wikis.
- **How, size, dependencies**: "English name" box, off by default, with tooltip and ↺. Displayed only if the client is not in English and if the name differs. Size S. Probe: none. Decision: add it (new).
- **Source**: Items/ItemCatalog.cs:11: EnglishName is loaded for the wiki. docs/evidence/0.318-plugin-gaps.json (BilingualTooltips): "EnglishName loaded for the wiki, never displayed". This gap is not part of the 103 of GAPS.

## I20 — Sort preview: moved slots flagged

- **State (re-read on 2026-11-05)**: DONE 0.565.
- **Value**: low; main file: `src/TenshoAssistant/MainWindow.SortPreview.cs`.
- **What the player gets**: The preview frames the slots that change and says how many items move. "Apply" is greyed when the sort would change nothing.
- **How, size, dependencies**: Compare the origin slot (DisplayIndex) with the target slot in the cached projection. Add a coloured frame and a counter; the computation follows the projection and is not redone at every frame. Size S. Probe: none. Decision: none.
- **Source**: MainWindow.SortPreview.cs:136-146: only the result is drawn. The plan is already built (InventoryVisualSortPlan.Build) and the projection is cached (MainWindow.SortPreview.cs:73-80).

## I21 — Job and gear set filter in the sort rules

- **State (re-read on 2026-11-05)**: DONE 0.595.
- **Value**: low; main file: `src/TenshoAssistant/Inventory/InventorySortRules.cs`.
- **What the player gets**: A sort zone can take only the gear of one job, or the pieces of your gear sets.
- **How, size, dependencies**: Copy ClassJobCategory (list of jobs) into the catalog, then add a job filter to the zone. For the sets: probe of RaptureGearsetModule first. Size M. Decision: add it, new (recommended: with or after I-14).
- **Source**: Inventory/InventorySortRules.cs:8-12: InventorySortMetadata has no job. docs/evidence/0.318-plugin-gaps.json (SortaKinda): "Filters by class or job and by gear set" = missing. The catalog does not read ClassJobCategory (Items/ItemCatalogLoader.Dalamud.cs:135-142).

## I22 — Computations done at every frame (postponed to e): preparation cost and market history

- **Value**: low; main file: `src/TenshoAssistant/MainWindow.PreparationCost.cs`.
- **What the player gets**: Nothing visible: the preparation cost and history pages stay light with long lists.
- **How, size, dependencies**: Measure first in game, with a chain of more than 30 ingredients. If the gain is real: a cache key like ChainCostViewKey (MainWindow.PreparationCost.cs:20, 65-71), and marketSalesKey extended to the filtered sales and to the statistics. Size S. Decision: none.
- **Source**: CHANGELOG.md:181 (0.325): "Postponed to step e): caching of the plan and of the preparation cost… to measure in game before changing". MainWindow.PreparationCost.cs:40-41: PreparationCost.Build at every frame. MainWindow.MarketBoard.cs:223-230: filter and statistics of the sales at every frame. docs/evidence/0.292-draw-times.md: under 0.5 ms per frame.

---

# Objets, banque, tri et marché : propositions d'amélioration (audit du 2026-11-02, étape e)

Liste établie par un audit multi-agents en lecture seule (0.328). **Rien n'était implémenté** à l'audit (relu le 2026-11-03 : les propositions faites depuis, à l'étape g, portent une ligne « État ») : vous choisissez ce qu'on ajoute
(règle du 2026-11-02, [ROADMAP](ROADMAP.md) « Décisions reportées à la fin »). Les défauts trouvés par le même audit sont corrigés en
0.329 ([CHANGELOG](../../CHANGELOG.md)). Plusieurs propositions recoupent des manques déjà retenus pour l'étape g)
([GAPS](GAPS.md), I-*) : elles le disent.

Valeur et taille sont les estimations de l'audit. « Sonde » : une lecture du jeu ou un relevé en jeu est nécessaire avant de coder.

## I0 — Compter les pièces portées dans les stocks et dans « possédé »

- **État (relu le 2026-11-05)** : FAIT 0.549.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/Inventory/DalamudInventoryReader.Dalamud.cs`.
- **Ce que le joueur obtient** : L'équipement porté compte comme possédé. La Revue de mode de la semaine ne dit plus « non possédé » pour la pièce que vous portez. Le stock au survol et la fiche objet l'incluent, sur une ligne « Porté ». Les filtres « possédé » des autres outils le comptent aussi.
- **Comment, taille, dépendances** : Ajouter GameInventoryType.EquippedItems (1000) à la capture faite sur le thread framework, avec le même lecteur IGameInventory et la même couverture. Ajouter une StorageLabel « Porté » (catégorie PlayerBags), traduite dans Inventory/StorageNames.cs. Le tri (InventorySortScope, conteneurs 0-3) et la grille de la Banque (MainWindow.Bank.cs:184-186, conteneurs 0-3) ne sont pas touchés : un test doit le vérifier, avec un relevé de tous les consommateurs de inventory.Current (Diagnostics, tri, Banque). Taille S. Sonde : aucune pour la lecture ; vérification en jeu ensuite (porter puis retirer une pièce). Décision : compter le porté partout comme possédé (recommandé : oui, sur une ligne à part pour rester visible).
- **Source** : Inventory/DalamudInventoryReader.Dalamud.cs:11-13 ne capture que les sacs 1 à 4 et les cristaux. Inventory/ItemHoldings.cs:34-43 ne compte que les conteneurs 0-3 et 2001. MainWindow.FashionWeek.cs:41 (FashionOwned) passe par GetRecipeHoldings, donc sans l'équipement porté. Seule la page Équipement le traite à part (MainWindow.Gear.cs:512, IsWorn). docs/evidence/0.318-plugin-gaps.json (Allagan Tools) : « Équipement porté, monnaies et cristaux des servants comptés dans les stocks » = missing. EquippedItems est déjà lu ailleurs : Gear/GearLoader.Dalamud.cs:127 et Craft/CraftLiveReader.Dalamud.cs:22.

## I1 — Signaler et effacer le relevé d'un servant renvoyé ou d'un ancien rangement

- **État (relu le 2026-11-05)** : FAIT 0.576.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/Inventory/BankService.cs`.
- **Ce que le joueur obtient** : Un servant renvoyé ne compte plus pour toujours dans vos stocks. La page Rangements signale les servants enregistrés qui n'apparaissent plus à la sonnette, et un bouton « Effacer ce relevé » (deux clics) les retire. Vos totaux redeviennent justes : survol, fiche, « manquant » de l'artisanat, Revue de mode.
- **Comment, taille, dépendances** : Ajouter InventoryBankService.DeleteAsync(BankKey), dans la même file bornée et avec la même écriture atomique (FileInventoryBankStore). Bouton dans « Imports » (DrawImportManager) avec la confirmation en deux clics de MainWindow.Alarms.cs. Mention « absent de la sonnette » quand KnownRetainers est connu et ne contient pas ce servant ; jamais d'effacement automatique. Taille S-M. Sonde : aucune. Décision : aucune nouvelle (partie du choix I-03, faisable avant g) ; à confirmer : effacement manuel seulement (recommandé).
- **Source** : BACKLOG.md:124 : « éventuelle suppression manuelle d'un ancien relevé » reste une extension séparée. GAPS.md:114 (I-03), Choix : bouton « Effacer ce personnage ». Inventory/BankService.cs:30 n'a que ImportAsync, aucune suppression. Inventory/ItemHoldings.cs:65-76 compte chaque relevé de servant enregistré pour le personnage. MainWindow.Bank.cs:428-441 compare la liste de la sonnette (BankInventoryBridge.Dalamud.cs:188-197, KnownRetainers) aux relevés seulement pour trouver les servants non importés, jamais les relevés en trop.

## I2 — I-01, partie anticipée : dernière vente et touche d'actualisation au survol

- **État (relu le 2026-11-03)** : FAIT 0.343 (I-01 : dernière vente, Ctrl+Alt pour actualiser).
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.MarketHover.cs`.
- **Ce que le joueur obtient** : Sous l'infobulle d'un objet, vous voyez déjà le prix le plus bas NQ/HQ du monde et le moins cher du centre de données. S'y ajoutent la dernière vente (prix et date) et une touche à tenir pour relire le prix, sans ouvrir la page Marché. Ensuite, après une sonde : prix de la région, ventes par jour et prix moyen.
- **Comment, taille, dépendances** : Étape 1 (S, aucune sonde) : afficher RecentSale/SaleAt NQ et HQ dans DrawHoveredPrices. La touche est lue sur le thread framework (IKeyState, dans Items/ItemHoverReader.Dalamud.cs) ; elle demande une lecture forcée, au plus toutes les 3 s (cadence de Market/MarketService.cs:138). Réglage « Touche d'actualisation » (aucune / Alt / Ctrl / Maj) avec infobulle et ↺. Étape 2 (S) : sonde d'une réponse aggregated (réponse tronquée dans docs/evidence), puis lecture de minListing.region, dailySaleVelocity et averageSalePrice dans MarketQuote.Parse (même requête ; monter la version de cache « universalis-aggregated-v2 »). Le prix de la pile attend la sonde de la quantité survolée (partagée avec I-06). Décision : la touche par défaut (recommandé : Alt, comme Price Insight, avec « aucune » possible).
- **Source** : GAPS.md:112 (I-01), Choix : « prix au survol seulement (pas de préchargement), touche de rafraîchissement ». Market/MarketQuote.cs:37 lit déjà recentPurchase.world, affiché seulement sur la page Marché (MainWindow.Market.cs:65-66). DrawHoveredPrices (MainWindow.MarketHover.cs:406-429) n'affiche que Minimum. docs/evidence/0.318-plugin-gaps.json (Price Insight) : « Dernier achat au survol » et « Rafraîchir avec Alt » = partial. Les champs region, dailySaleVelocity et averageSalePrice cités par GAPS ne sont prouvés par aucun relevé : docs/evidence/0.270-universalis-listings.md ne couvre que /api/v2/{scope}/{item}.

## I3 — Prix des marchands PNJ dans le coût de préparation et sur la page Marché

- **État (relu le 2026-11-05)** : FAIT 0.575.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.PreparationCost.cs`.
- **Ce que le joueur obtient** : Pour chaque ingrédient vendu en gils par un marchand PNJ, le coût de préparation montre le prix PNJ à côté du minimum du marché et peut retenir le moins cher. Le Résumé de la page Marché dit « Vendu par un PNJ : X gils » (nom et lieu, lien vers le marchand). Vous n'achetez plus au marché ce qui coûte moins cher chez un marchand.
- **Comment, taille, dépendances** : Fonction pure « prix PNJ en gils » (offre NpcShopKind.Gil la moins chère, qualité de l'offre NpcShopSale.Hq) passée à PreparationCost.BuildDemands. Nouvelle colonne, état « PNJ » et choix de qualité « moins cher, PNJ compris ». Page Marché : une ligne au Résumé avec DrawVendorLink. Aucune requête réseau. Taille S-M. Sonde : aucune (feuilles déjà lues) ; un contrôle en jeu sur un objet connu. Dépendance : partie de A-06 (domaine Artisanat), faisable avant g. Décision : le PNJ entre-t-il dans le total « moins cher » (recommandé : oui, sur une ligne marquée « PNJ »).
- **Source** : GAPS.md:77 (A-06) cite comme donnée les « prix des marchands GilShopItem (Npcs/NpcVendorLoader.Dalamud.cs) », Choix « Ajouter ». Market/PreparationCost.cs:86-99 ne lit que le marché (un objet non échangeable reste NotMarketable). Npcs/NpcVendorCatalog.cs:7-15 : NpcShopKind.Gil et coûts en objet 1 (gil), déjà chargés. docs/evidence/0.318-plugin-gaps.json (SimpleMarketBoard) : « surbrillance quand un PNJ vend moins cher » = missing.

## I4 — Prix Universalis par lots : une requête pour toute une liste

- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/Market/MarketService.cs`.
- **Ce que le joueur obtient** : « Lire les prix » du coût de préparation (et des listes à venir) répond en une requête au lieu d'une par ingrédient : quelques secondes au lieu d'environ 100 s pour 32 ingrédients, sans risquer l'arrêt à 2 minutes, avec beaucoup moins de requêtes vers Universalis. Cela permet aussi de chiffrer, sur clic, un rangement ou un trajet de sous-marin au prix du marché.
- **Comment, taille, dépendances** : Sonde d'abord : GET aggregated/{monde}/{id1,id2,…} avec curl (nombre maximal d'objets, champ des objets en échec), réponse tronquée dans docs/evidence. Puis RequestBatchAsync(ids) dans MarketService : même verrou, même pause de 3 s, réponse bornée, cache mémoire et disque par objet. PreparationPriceSession l'utilise, avec la limite portée à la valeur prouvée. Taille M. Dépendances : aucune ; sert ensuite I-13, A-06, f) 2, et une éventuelle « valeur du rangement au prix du marché » (Retainer Inventory Price dans 0.318-plugin-gaps.json, hors des 103 manques : à décider). Décision : la nouvelle limite (recommandé : celle prouvée par la sonde, 100 si confirmée).
- **Source** : Market/MarketService.cs:138 : 3 s entre deux requêtes, une à la fois. Market/PreparationPriceSession.cs:22 : 32 objets au plus ; ligne 28 : arrêt après 2 minutes. MainWindow.PreparationCost.cs:94 affiche l'avis de limite. MarketQuote.Parse (Market/MarketQuote.cs:21-31) parcourt déjà un tableau « results » par itemId. ROADMAP « Décidé le 2026-11-02 », f) 2 : prix du marché des trajets de sous-marin. Cadence des sources : docs/evidence/0.246-cache-audit.md.

## I5 — Coût de préparation au prix du centre de données (option de A-06)

- **État (relu le 2026-11-05)** : FAIT 0.574.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Market/PreparationCost.cs`.
- **Ce que le joueur obtient** : Le coût de préparation se calcule au monde actuel ou au moins cher du centre de données, avec le monde où acheter, pour qui change de monde pour faire ses achats.
- **Comment, taille, dépendances** : Combo « Portée : monde / centre de données » avec infobulle, mémorisé avec ↺. Chaque ligne indique le monde (MarketState.NameOf). Copie texte complétée. Aucune nouvelle requête. Taille S. Sonde : aucune. Dépendance : partie de A-06, faisable avant g. Décision : aucune nouvelle (option déjà choisie).
- **Source** : GAPS.md:77 (A-06), Choix : « plus basse annonce du monde (centre de données en option) ». Market/MarketQuote.cs:41-44 lit déjà NqCheapest/HqCheapest (minimum du centre de données et son monde) dans la même réponse. Market/PreparationCost.cs:93-96 n'utilise que Nq/Hq.Minimum du monde.

## I6 — I-06, partie anticipée : prix de vente au PNJ, à l'unité

- **État (relu le 2026-11-03)** : FAIT 0.346 (I-06, prix NQ seulement).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.MarketHover.cs`.
- **Ce que le joueur obtient** : Sous l'infobulle et dans l'en-tête de la fiche objet : « Vente au PNJ : X gils l'unité », pour choisir vite entre le marché et le marchand. Le total de la pile vient après la sonde de la quantité survolée.
- **Comment, taille, dépendances** : Une ligne dans le panneau de survol : cinquième partie « Prix PNJ », avec sa case, son infobulle et son ↺, comme les HoverShow*. Même ligne dans les « faits » de la fiche. Valeur 0 : rien n'est affiché (le sens « invendable » n'est pas prouvé). HQ : aucun prix tant qu'il n'est pas relevé, sans coefficient deviné. Taille S. Sonde : quantité de la pile survolée (AgentItemDetail ou InventoryItem, partagée avec I-01) pour le total ; relevé en jeu pour un prix HQ. Décision : aucune nouvelle.
- **Source** : GAPS.md:117 (I-06), Choix « Ajouter », dépend de la quantité survolée. Items/ItemCatalog.cs:11 (SellPrice) vient de Item.PriceLow (Items/ItemCatalogLoader.Dalamud.cs:138) ; seul le tri s'en sert (MainWindow.SortRules.cs). L'en-tête de la fiche (MainWindow.ItemSheet.cs:50-60) ne l'affiche pas.

## I7 — I-12, partie anticipée : filtres de catégorie et d'emplacement, niveaux dans les résultats

- **État (relu le 2026-11-03)** : FAIT 0.353 (I-12).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.Items.cs`.
- **Ce que le joueur obtient** : La recherche d'objets accepte un filtre de catégorie du jeu, d'emplacement d'équipement et de niveau ; chaque résultat montre sa catégorie et ses niveaux. Trouver « toutes les bagues de niveau 90 », ou un matériau parmi des homonymes, devient direct.
- **Comment, taille, dépendances** : ItemCatalog.Search(query, limit, filtre) avec catégorie, emplacement (EquipmentSlots) et bornes de niveau ; requête vide permise quand un filtre est posé. Combos et champs avec infobulle, bouton « Effacer les filtres ». Recalcul seulement quand la requête ou un filtre change (comme aujourd'hui, MainWindow.Items.cs:679-684). Taille S. Sonde : aucune. Le filtre de job (ClassJobCategory) et les objets clés (EventItem) restent en g : colonnes à ajouter au catalogue. Décision : aucune nouvelle.
- **Source** : GAPS.md:123 (I-12), Choix « Ajouter ». Items/ItemCatalog.cs:86-105 ne cherche que par nom ou ID. MainWindow.Items.cs:702-716 : un résultat n'est qu'une icône et un nom. Les données existent déjà : ItemCatalog.Categories (Items/ItemCatalog.cs:38, 45-47) ; Equipment, ItemLevel et EquipLevel dans ItemDefinition.

## I8 — X-07, partie anticipée : ouvrir l'objet sur Universalis, Garland Tools, Teamcraft ou le wiki

- **État (relu le 2026-11-05)** : FAIT 0.561.
- **État (relu le 2026-11-03)** : en partie 0.349 (X-07 : wiki et Lodestone) ; Universalis, Garland Tools et Teamcraft pas dans ce menu.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.DutyFinder.cs`.
- **Ce que le joueur obtient** : Clic droit sur un objet, dans les fenêtres Tensho et dans le menu « Tensho » du jeu : « Ouvrir sur… » ouvre la page de l'objet dans le navigateur.
- **Comment, taille, dépendances** : Sous-menu « Ouvrir sur » : Universalis (/market/{id}), Garland Tools (#item/{id}), Teamcraft (fiche de l'objet par ID), consolegameswiki (titre anglais). Lodestone : recherche par nom seulement, car les données du jeu ne donnent aucun identifiant Lodestone. Util.OpenLink sur clic seulement. Case « Liens externes » avec infobulle et ↺. Crédits et manifeste complétés (sites nommés). Taille S. Sonde : vérifier une fois chaque forme d'URL (docs/evidence). Décision : la liste des sites (recommandé : les quatre ci-dessus) et les commandes /wiki, /lodestone (recommandé : non, le menu suffit).
- **Source** : GAPS.md:199 (X-07), Choix « Ajouter ». Aucun OpenLink dans le code (recherche du 2026-11-02). Menus concernés : ItemLinksMenu (MainWindow.DutyFinder.cs:145-160) et Items/ItemContextMenuBridge.Dalamud.cs:60-80. Le nom anglais, déjà chargé (Items/ItemCatalog.cs:11, EnglishName), donne le titre du wiki.

## I9 — Copier le nom de l'objet et lien d'objet dans le chat (local)

- **État (relu le 2026-11-05)** : FAIT 0.561, avec I8.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.DutyFinder.cs`.
- **Ce que le joueur obtient** : Clic droit sur un objet : « Copier le nom », à coller dans la recherche du marché du jeu. « Lien dans le chat » : Tensho écrit dans votre seul chat un lien cliquable de l'objet, que vous partagez ensuite vous-même si vous le voulez.
- **Comment, taille, dépendances** : ImGui.SetClipboardText(item.Name). Lien : SeStringBuilder.AddItemLink(id, hq) puis IChatGui.Print, un message local : rien n'est envoyé au serveur. Mêmes entrées dans le menu du jeu. Taille S. Sonde : aucune. Décision : ajouter ces entrées, non choisies en c) (recommandé : « Copier le nom » oui, le lien en option).
- **Source** : ItemLinksMenu (MainWindow.DutyFinder.cs:145-160) n'a ni copie ni lien ; aucun ItemPayload dans le code. docs/evidence/0.318-plugin-gaps.json (Item Search) : « Lier l'objet dans le chat sans le posséder » = missing. Ce manque ne fait pas partie des 103 de GAPS.

## I10 — Cristaux et gils des servants dans le relevé du servant

- **État (relu le 2026-11-03)** : en partie : gils des servants dans le journal des gils (0.351) ; cristaux des servants non vérifiés.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Inventory/BankInventoryBridge.Dalamud.cs`.
- **Ce que le joueur obtient** : Les cristaux confiés aux servants comptent dans l'onglet Cristaux (une colonne par servant), dans le « manquant » de l'artisanat et au survol. Les gils de chaque servant sont gardés, avec la date du relevé.
- **Comment, taille, dépendances** : Sonde d'abord : RetainerCrystals (12001) et RetainerGil (12000) sont-ils chargés quand InventoryRetainer est ouvert (IsLoaded, taille) ? Relevé par tools/ApiProbe ou le journal en mode développeur, dans docs/evidence. Puis un groupe de conteneurs à part (nouveau BankStorageKind, ou schéma 2 du fichier) pour ne changer ni la validation « groupe exact » (BankEntryValidator) ni la portée du tri (10000-10006). Taille M. Dépendance : aucune ; prépare I-03 et I-04. Décision : avancer cette partie avant g (recommandé : oui, cristaux d'abord).
- **Source** : Inventory/BankModels.cs:9 : un servant ne couvre que les conteneurs 10000-10006. L'onglet Cristaux (MainWindow.Items.cs:54-89) ne lit que le conteneur 2001 du personnage. BACKLOG.md:124 : « équipements/cristaux/marché des servants » non inclus. docs/evidence/0.318-plugin-gaps.json : Allagan Tools « cristaux des servants comptés dans les stocks » = missing, Crystal Terror = partial. GAPS.md:114-115 (I-03, I-04) reposent sur des données de même nature.

## I11 — Vue en liste triable de la Banque

- **État (relu le 2026-11-05)** : FAIT 0.594.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.Bank.cs`.
- **Ce que le joueur obtient** : En plus des grilles 5 × 7, une vue « Liste » : un objet par ligne, avec NQ, HQ, total et les rangements où il se trouve, triable par nom, quantité ou catégorie. Avec plusieurs servants, vous voyez enfin d'un coup d'œil où sont vos matériaux.
- **Comment, taille, dépendances** : Bascule « Grille / Liste », mémorisée, avec infobulle et ↺. Table ImGui triable par en-tête, avec clipper, sur le modèle de MainWindow.MarketBoard.cs. Lignes recalculées seulement quand la source ou un filtre change. Recherche alignée sur la normalisation du catalogue. Taille S-M. Sonde : aucune. Décision : vue par défaut (recommandé : la grille, comme aujourd'hui).
- **Source** : MainWindow.Bank.cs:181-203 calcule déjà les lignes agrégées par objet (bankSourceRows). L'affichage n'existe qu'en grilles (MainWindow.Bank.cs:303-370). La recherche de la Banque (MatchesBankFilters, MainWindow.Bank.cs:388-403) ne retire pas les accents, contrairement à la recherche d'objets (Items/ItemCatalog.cs:114-123). docs/evidence/0.318-plugin-gaps.json : Allagan Tools « listes de filtres enregistrées » et AetherBags = partial.

## I12 — Rapport du coffre à apparence : pièces pour l'armoire et doublons

- **État (relu le 2026-11-03)** : FAIT 0.357 (C-03, pièces pour l’armoire) et 0.358 (C-07, apparences identiques).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Inventory/BankInventoryBridge.Dalamud.cs`.
- **Ce que le joueur obtient** : Un rapport, sans rien déplacer, liste les pièces du coffre à apparence que l'armoire peut accueillir, et les pièces en double entre coffre à apparence, armoire, sacs et arsenal. Vous savez quoi retirer pour libérer des places du coffre.
- **Comment, taille, dépendances** : Projection pure sur les relevés (BankEntry GlamourDresser et Armoire, sacs, arsenal) : « accepté par l'armoire » = objet présent dans cabinetRows ; doublon = même objet dans deux rangements. Une section de la page Rangements, recalculée seulement quand un relevé change. Taille S-M. Sonde : aucune nouvelle ; validation en jeu de 0.236-0.240 d'abord. Décision : ajouter ce rapport, nouveau (recommandé : oui, après cette validation en jeu).
- **Source** : BankInventoryBridge.Dalamud.cs:224-255 copie déjà le contenu du coffre à apparence et de l'armoire ; cabinetRows donne les objets acceptés par l'armoire (feuille Cabinet). GAPS.md:121 (I-10) suit le même esprit : « rapport seul, aucun déplacement ». STATUS, « À valider en jeu » : la lecture du coffre et de l'armoire (0.234-0.240) n'est pas encore validée en jeu.

## I13 — I-10, partie anticipée : piles non regroupées

- **État (relu le 2026-11-03)** : FAIT 0.348 (I-10).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Items/ItemCatalogLoader.Dalamud.cs`.
- **Ce que le joueur obtient** : Un rapport des objets répartis en plusieurs piles incomplètes dans un même rangement (sacs, sacoches, servants), par exemple : « 3 piles de minerai de fer (12, 40, 99) tiendraient en 2 ».
- **Comment, taille, dépendances** : Ajouter Item.StackSize au catalogue (ItemCatalogLoader.Dalamud.cs:135-142, avec une version du cache d'objets montée). Projection pure par rangement : même objet, même qualité, NQ et HQ séparés. La partie « arsenal hors des sets d'équipement » attend une sonde de RaptureGearsetModule et reste en g. Taille S. Sonde : aucune pour les piles. Décision : aucune nouvelle.
- **Source** : GAPS.md:121 (I-10), Choix : « rapport seul, aucun déplacement ». ItemDefinition (Items/ItemCatalog.cs:9-13) n'a pas de taille de pile. Les piles par case sont déjà capturées (Inventory/InventorySnapshot.cs, InventoryStack).

## I14 — Annuler le dernier tri

- **État (relu le 2026-11-05)** : ÉCARTÉE à la revue avec l'utilisateur ([plan global](GLOBAL-PLAN.md)).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Inventory/InventoryVisualSortBridge.Dalamud.cs`.
- **Ce que le joueur obtient** : Après « Appliquer », un bouton « Annuler le dernier tri » remet l'ordre précédent des sacs ou du rangement ouvert, tant que rien n'a bougé depuis.
- **Comment, taille, dépendances** : Garder l'ordre d'avant (copie managée des pages et des cases) avec l'instantané vérifié. « Annuler » construit un plan inverse soumis aux mêmes contrôles (plan.Matches, RequireOpenForSort, SortFunctionIndex). L'annulation est perdue au changement de personnage ou de rangement et au moindre mouvement d'objet. Taille S-M. Sonde : aucune ; test en jeu. Décision : un seul niveau d'annulation, pour la session (recommandé).
- **Source** : InventoryVisualSortBridge.Dalamud.cs:75-84 copie l'ordre natif d'origine avant toute écriture ; lignes 99-108 : écriture après toutes les vérifications. ROADMAP, SORT-001 (référence SortaKinda). Règle du projet : un tri ne s'applique que sur clic, vérifié contre ce qui est ouvert.

## I15 — Partager un modèle de tri en texte (copier / coller)

- **État (relu le 2026-11-05)** : FAIT 0.566.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.SortTemplates.cs`.
- **Ce que le joueur obtient** : Un modèle de rangement se copie en texte et se colle chez un autre joueur ou sur un autre PC.
- **Comment, taille, dépendances** : Export : JSON versionné, compressé en base64, avec un préfixe du type « Tensho-Tri-1: », mis dans le presse-papiers. Import : taille bornée (par exemple 64 Kio), lecture stricte puis InventorySortTemplates.Validate, nouvel identifiant, nom unique. Les identifiants d'objets et de catégories sont ceux du jeu, indépendants de la langue. Infobulles sur les deux boutons. Taille S. Sonde : aucune. Décision : le format (recommandé : celui-ci).
- **Source** : Inventory/InventorySortTemplates.cs:3-5 : un modèle ne contient ni personnage ni servant. MainWindow.SortTemplates.cs:13-60 : créer, mettre à jour, supprimer, charger ; ni export ni import. docs/evidence/0.318-plugin-gaps.json (SortaKinda) : « import et export des règles » = partial.

## I16 — Commandes « /tensho objet » et « /tensho prix »

- **État (relu le 2026-11-05)** : FAIT 0.555.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Plugin.cs`.
- **Ce que le joueur obtient** : « /tensho objet minerai de fer » ouvre la fiche de l'objet, ou la recherche s'il y a plusieurs résultats ; « /tensho prix … » ouvre la page Marché. Les deux marchent aussi dans une macro du jeu.
- **Comment, taille, dépendances** : Lire les arguments (mots français et anglais), chercher avec ItemCatalog.Search, puis SelectItem et OpenItemSheet, ou OpenRoute(Market). Traduire l'aide de la commande (Plugin.CommandHelp). Taille S. Sonde : aucune. Décision : les mots-clés (recommandé : objet/item et prix/price ; marchand/vendor en option).
- **Source** : Plugin.cs:377-380 : seul « peche » est reconnu. docs/evidence/0.318-plugin-gaps.json (Item Vendor Location) : « Commande de recherche (/pvendor nom) » = missing.

## I17 — I-13, partie anticipée : valeur en gils d'une monnaie

- **État (relu le 2026-11-03)** : FAIT 0.354 (I-13).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.ItemUses.cs`.
- **Ce que le joueur obtient** : Dans « Usages › Monnaie » d'un objet-monnaie (sceaux, marques, écus…), un bouton « Lire les prix » donne pour chaque article sa valeur au marché et le gain en gils par unité de monnaie, du plus rentable au moins rentable.
- **Comment, taille, dépendances** : Liste bornée lancée au clic : PreparationPriceSession, ou mieux la requête par lots de la proposition « Prix Universalis par lots ». Valeur = minimum du monde ÷ coût. Articles non échangeables marqués « non vendable ». Le solde et « combien je peux en acheter » attendent I-02 ; les monnaies codées restent « inconnues ». Taille M. Dépendance : les lots Universalis (recommandé). Décision : aucune nouvelle.
- **Source** : GAPS.md:124 (I-13), Choix « Ajouter », dépend des soldes (I-02) et des monnaies codées. MainWindow.ItemUses.cs:81-108 liste les articles et leur coût, sans valeur. docs/evidence/0.318-plugin-gaps.json : Currency Profit Scanner et Currency Spender.

## I18 — I-07, partie anticipée : copier « prix le plus bas − 1 » depuis la page Marché

- **État (relu le 2026-11-05)** : FAIT 0.567.
- **État (relu le 2026-11-03)** : en partie 0.346 (I-07 : à l’ouverture des offres dans le jeu) ; pas depuis la page Marché.
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/MainWindow.MarketBoard.cs`.
- **Ce que le joueur obtient** : Sur l'onglet Annonces de Tensho, un bouton copie le prix le plus bas moins 1 (NQ ou HQ). Vos propres servants sont marqués, pour ne pas vous sous-coter.
- **Comment, taille, dépendances** : Bouton avec infobulle, qui ignore les annonces de vos servants (KnownRetainers) et affiche la date de l'annonce, car Universalis peut être en retard sur le jeu. La version complète (offres du jeu lues à l'ouverture de la liste, IMarketBoard) reste en g. Taille S. Sonde : aucune. Décision : faire cette version tôt (recommandé : seulement si vous la voulez avant g ; sinon attendre I-07 complet, plus fiable).
- **Source** : GAPS.md:118 (I-07), Choix : copie dans le presse-papiers seulement. MainWindow.MarketBoard.cs:161-216 affiche les annonces avec le nom du servant, sans copie. Les noms de vos servants sont connus après l'ouverture de la sonnette (BankInventoryBridge.Dalamud.cs:188-197).

## I19 — Nom anglais de l'objet dans la fiche et l'infobulle

- **État (relu le 2026-11-05)** : FAIT 0.562.
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/MainWindow.ItemSheet.cs`.
- **Ce que le joueur obtient** : En option, le nom anglais de l'objet s'affiche sous son nom, utile avec les guides et les wikis en anglais.
- **Comment, taille, dépendances** : Case « Nom anglais », désactivée par défaut, avec infobulle et ↺. Affiché seulement si le client n'est pas en anglais et si le nom diffère. Taille S. Sonde : aucune. Décision : l'ajouter (nouveau).
- **Source** : Items/ItemCatalog.cs:11 : EnglishName est chargé pour le wiki. docs/evidence/0.318-plugin-gaps.json (BilingualTooltips) : « EnglishName chargé pour le wiki, jamais affiché ». Ce manque ne fait pas partie des 103 de GAPS.

## I20 — Aperçu du tri : cases déplacées signalées

- **État (relu le 2026-11-05)** : FAIT 0.565.
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/MainWindow.SortPreview.cs`.
- **Ce que le joueur obtient** : L'aperçu encadre les cases qui changent et dit combien d'objets bougent. « Appliquer » est grisé quand le tri ne changerait rien.
- **Comment, taille, dépendances** : Comparer la case d'origine (DisplayIndex) à la case cible dans la projection en cache. Ajouter un cadre de couleur et un compteur ; le calcul suit la projection et ne se refait pas à chaque image. Taille S. Sonde : aucune. Décision : aucune.
- **Source** : MainWindow.SortPreview.cs:136-146 : seul le résultat est dessiné. Le plan est déjà construit (InventoryVisualSortPlan.Build) et la projection est mise en cache (MainWindow.SortPreview.cs:73-80).

## I21 — Filtre de job et de set d'équipement dans les règles de tri

- **État (relu le 2026-11-05)** : FAIT 0.595.
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/Inventory/InventorySortRules.cs`.
- **Ce que le joueur obtient** : Une zone de tri peut ne prendre que l'équipement d'un job, ou les pièces de vos sets d'équipement.
- **Comment, taille, dépendances** : Copier ClassJobCategory (liste de jobs) dans le catalogue, puis ajouter un filtre de job à la zone. Pour les sets : sonde de RaptureGearsetModule d'abord. Taille M. Décision : l'ajouter, nouveau (recommandé : avec ou après I-14).
- **Source** : Inventory/InventorySortRules.cs:8-12 : InventorySortMetadata n'a pas de job. docs/evidence/0.318-plugin-gaps.json (SortaKinda) : « Filtres par classe ou job et par set d'équipement » = missing. Le catalogue ne lit pas ClassJobCategory (Items/ItemCatalogLoader.Dalamud.cs:135-142).

## I22 — Calculs faits à chaque image (reportés à e) : coût de préparation et historique du marché

- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/MainWindow.PreparationCost.cs`.
- **Ce que le joueur obtient** : Rien de visible : les pages du coût de préparation et de l'historique restent légères avec de longues listes.
- **Comment, taille, dépendances** : Mesurer d'abord en jeu, avec une chaîne de plus de 30 ingrédients. Si le gain est réel : une clé de cache comme ChainCostViewKey (MainWindow.PreparationCost.cs:20, 65-71), et marketSalesKey étendu aux ventes filtrées et aux statistiques. Taille S. Décision : aucune.
- **Source** : CHANGELOG.md:181 (0.325) : « Reporté à l'étape e) : mise en cache du plan et du coût de préparation… à mesurer en jeu avant de changer ». MainWindow.PreparationCost.cs:40-41 : PreparationCost.Build à chaque image. MainWindow.MarketBoard.cs:223-230 : filtre et statistiques des ventes à chaque image. docs/evidence/0.292-draw-times.md : sous 0,5 ms par image.
