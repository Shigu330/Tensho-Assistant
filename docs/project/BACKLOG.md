# Targeted backlog

> *Traduction française plus bas.*

## Open — state at 0.315.0 (2026-11-02), corrections of 2026-11-04 (0.512)

The queue has been dealt with since (ROADMAP, CHANGELOG); a line may be done without being marked here. What remains for 1.0.0: [PUBLISHING](PUBLISHING.md).

The points waiting for a decision or an in-game survey are grouped in the [ROADMAP](ROADMAP.md), section "To decide / to
review"; the in-game tests waiting, in its section "Tests to run in game". The order of work is in
the maintainer's status file ("Next action").

Map (MAP-001, plan: MAP-016-PLAN (maintainer's notes, not in the repository))
- Icons of the representatives (e.g. Rowena's representatives): fixed in 0.155 (MAP-017), validated in game 2026-10-31.
- Exploration markers: **closed** (was "paused" at 0.154); wiki weather cited, displayed and used by the alarms since 0.201.
- Quest type icon approximate (71141 calculated against 71151 seen in game); `ObjectiveId` correspondence partly unproven.
  Still open: in-game survey awaited, not blocking for D17 (the setting says "variant n").
- Marker type of the guildleves (markers at the edge of the map, 0.298): surveyed and dealt with in 0.652 ([evidence](../evidence/0.652-leve-markers.md)).
- Frames and textures of the game taken from the ULDs (R-06 step 6): frame removed in 0.510 (DEC-033), closed.

Items and collections
- Sources still unknown for some items (Alsvid, Vegetal Voucher NPC): unknown documented (decision 2026-11-01);
  other collection types to cover (COLL-001).
- NPC vendors: coded currencies and InclusionShop already dealt with (line outdated at 0.154); free company shops (FccShop)
  **done in 0.295**, validated in game. Line closed.
- Injection of text into the game's native tooltip: **abandoned** (user decision 2026-11-01).
- Quest currencies without a direct item: unknown documented (QUEST-CURRENCIES-005-STUDY (maintainer's notes, not in the repository)).

Inventory and sort: armoury, glamour dresser and armoire read as storages (0.235–0.240, to validate in game); their sort
(SORT-ARMOURY-009, HEAVY) and SORT-AUTO-010 (opt-in): after publication. Disk cache of the small catalogues (maps, dungeons,
uses) not done, small gain.

Gathering: Bountiful bonus **done in 0.224** as an estimate (community rule cited, displayed "estimated", editable), to
validate in game; the old note "do not invent a formula" remains true: no official formula. Ocean fishing not covered
(no usable data source).

Free company, Reminders, Gear (R-07, R-08, R-11): remainders and open choices in the ROADMAP ("To decide / to review"):
table of submarine thresholds, airship unlock tree (LuminaSupplemental, GPL-3.0), airship route
planner, capacity rule under rank 50, squadron and Planeurs d'Ehcatl, step 4 of R-11. R-09 (events): not started.

Publication (PUBLISH-018, see [PUBLISHING](PUBLISHING.md)): no submission since DEC-039 (custom repository only). Code side done
(0.156–0.157): icon `src/TenshoAssistant/images/icon.png` (AI-generated, declared), `manifest.toml` ready, PR draft with the
AI declaration. The old mention "`images/icon.png`, `manifest.toml` and the AI declaration are missing" is outdated since 0.157.
What remains for the author: commit and push, commit hash in `manifest.toml`, PR text and declaration rewritten by him (see
[CONTRIBUTING](../../CONTRIBUTING.md#publication-dalamud)); German and Japanese translations partial, not reviewed.

The 17 lines of the [ROADMAP](ROADMAP.md) remain PARTIAL.

## Review of the open points (2026-11-01, before D17 publication)
User decision: publish after resolution; the injection of text into the game's native tooltip is **not** done.
Follow-up at 0.315 (2026-11-02) added at the end of each decision: only the quest icons point remains open.

| Point | Finding | Decision |
|---|---|---|
| Bountiful bonus (Bountiful Yield II / Bountiful Harvest II, actions 272/273) | GatheringOptimizer (MIT) asks for it by hand; frozen_rabbit_tome (no licence, fact surveyed only): +3 if gathering ≥ ⌊base × 1.1⌋, +2 if ≥ ⌊base × 0.9⌋, otherwise +1, base = "Gathering" value of the point's item level (Teamcraft item-level table, from the game's ItemLevel sheet). Community rule, not official. | **Do**: prefill the bonus with this rule (gathering stat read, point's item level by probe), displayed "estimated", editable. → **DONE 0.224** ([evidence](../evidence/0.224-bountiful-base.md)), to validate in game. |
| Fashion Report: glamour dresser and armoire | Readable in the client (glamour dresser, armoire); gives a more accurate "owned". | **Do** (probe before use). → **DONE 0.235–0.240** (widened to the armoury, read without another plugin, linked to the Bank), to validate in game. |
| Vendors: free company shops | FccShop sheet not read (InclusionShop and coded currencies already dealt with, backlog line outdated). | **Do** (small). → **DONE 0.295** ([evidence](../evidence/0.295-fcc-shops.md)), validated in game. |
| Quest icons: name of the variants, 71141 vs 71151 | Meaning of the variants absent from the data; only observation in game gives it. | **Do only with an in-game survey** from the user; otherwise leave "variant n". → **Open**: no survey at 0.315. |
| Map: journal quest target that stays (request 2026-11-01, R-01) | The "Target shown by the game (journal)" icon could not be removed. | **Do**. → **DONE 0.242–0.243** (removal by right click, then automatic), to validate in game. |
| Map: "compass arrows" towards a target out of view (request 2026-11-01) | — | **Do**. → **DONE 0.297** as icons at the edge of the map, one setting per type; validated in game. |
| Fashion Report: score | No per-piece score data in the sources. | **Do not do** (would be invented). |
| Deleting a game alarm from Tensho | No per-alarm /alarm subcommand (only "clear" = everything); DeleteAlarm(index) unproven. | **Do not do** (risk of erasing the wrong alarm); manual reminder kept. |
| Remaining unknown sources (Alsvid, Vegetal Voucher NPC) | Absent from the game, from Garland and from LuminaSupplemental. | **Do not do**: unknown documented. |
| Exploration markers (wiki weather) | Wiki weather already displayed and used by the alarms (0.201). | **Closed** (wiki source cited). |
| Add-ons of the 17 tools (adaptive rotation, armoury sort, native Triad rules…) | Big undertakings, not necessary to publish. | **After publication**, case by case. → **Taken up** in plan c) ([GAPS](GAPS.md), step g of the [ROADMAP](ROADMAP.md)): adaptive rotation A-04 retained, armoury sort I-14 later; native Triple Triad rules not surveyed by the comparison, remain here. |
| Injection into the native tooltip | — | **Abandoned** (user decision). |

## Archive

> Historical entries (2026-10-27 → 2026-10-30), from the most recent to the oldest; their "NEXT" and counters are those of their date.

- QUEST-ITEM-PROGRESS-007 CLOSED (2026-10-30): TESTED 477/477 + VALIDATED_IN_GAME reported by the user on DLL 0.76.0.0 (disconnection, sequence 255, "1 variant" included). SORT-RUNTIME-VALIDATION CLOSED (native sort validated in game, user report 2026-10-30). SORT-ZONE-SELECT-011 TESTED in 0.77 (multi-categories, chosen items, "; " terms, click on the preview); in-game validation to do. Next: SORT-ITEM-PROPS-008.
- CATALOG-CACHE-012: items 0.81 (validated in game), recipes 0.82, gathering/fishing 0.83 (TESTED). Map, Duty, ItemUse not done (small catalogues, small gain).
- NPC-VENDORS-014 TESTED in 0.88 (NPC vendors tool, map flag, hover). Possible follow-ups: verified table of coded currencies other than tomestones (scrips), InclusionShop, free company shops.
- HOVER-TOOLTIP-013 TESTED in 0.85 (panel under the native tooltip, cheapest in the data centre, stock/obtaining/uses). Future HEAVY option: injection of text into the native tooltip.
- SORT-ITEM-PROPS-008 TESTED in 0.84 (levels, rarity, price, ID, unique/untradable/dye/repairable/collectable). SortaKinda follow-ups: SORT-ARMOURY-009 (HEAVY), SORT-AUTO-010 opt-in.

- QUEST-CURRENCIES-005-STUDY closed on documented UNKNOWN: fifteen currencies without a reward Item/GC key, no unique relation found; quantities kept, no company invented. See the QUEST-CURRENCIES-005-STUDY study (maintainer's notes, not in the repository). BASE-AUDIT-003 finished: consistency/delivery 0.69 and improved diagnostics; independent follow-up depending on quota, with no global blocking.

- BASE-AUDIT-002 finished within its checking scope: 17 references present, 17 coverages PARTIAL, 71 local links/61 Markdown resolved, DLL and package 0.59.0.0 matching. See the BASE-AUDIT-002 study (maintainer's notes, not in the repository). Next independent study: GatherBuddy/fishing catalogue, after checking the quota; do not invent the missing baits/prerequisites.

- USE-CONTEXT-004 IMPLEMENTED / TESTED in 0.53: opt-in inventory menu, five shared routes, owner/revision/age guard, no network request by the menu. Native rendering to confirm; discussion links and Default targets outside this slice.
- SORT-NATIVE-002-STUDY OBSOLETE (2026-10-30): native application delivered in 0.54–0.59. Replaced by the SORT-GAP-008-STUDY study (maintainer's notes, not in the repository): 1) in-game validation of the native sort (never observed), 2) SORT-ITEM-PROPS-008 (LIGHT, static Item properties), 3) SORT-ARMOURY-009, 4) SORT-AUTO-010 opt-in.

State at 0.75.0 (2026-10-29, archive): shared cache and adjustable staleness (CACHE-001 (maintainer's notes, not in the repository)). Prices, quests, collections, community sources, Fashion Report and notebook calculations reuse dated persistent snapshots. Independent durations in Settings: 5 min, 1 h, 24 h or 7 d; forced refresh from the tools. Imports remain manual and flag their staleness; live observations keep a short invalidation. 466/466 tests/package/SHA-256 passing, zero warnings/errors; in-game validation of this cache still to do.

- GATHER-FISH-006 IMPLEMENTED / TESTED in 0.60: native notebook, 2,461 sheets, restrictions and tomes; 389 tests/package and native probes passing. 491 weather codes remain unknown.
- GATHER-FISH-SOURCES-007 IMPLEMENTED / TESTED in 0.61: baits/chains per place, common cache/transport, 396 tests and real consultation 4869 passing. Native weather code 2 unexplained, Unknown kept.
- GATHER-FISH-WINDOWS-008 IMPLEMENTED / TESTED in 0.62: conditions per place, ET/UTC time ranges, previous weather distinct; 401 tests and real probes passing. Availability not deduced.
- GATHER-FISH-WEATHER-009 IMPLEMENTED / TESTED in 0.63: native weather names/icons per territory, ambiguities and incomplete metadata unknown; 405 tests and real probes passing.
- GATHER-FISH-FORECAST-010 IMPLEMENTED / TESTED in 0.64: ranges of the filled criteria, coverage and ternary logic explicit; 412 tests and real probes/dated fixtures passing. No guaranteed catch.
- COLL-002 IMPLEMENTED / TESTED in 0.65: native orchestrion, positive confirmations, categories and bidirectional Item/context links, category errors isolated; 416 tests and native probe passing.
- COLL-003 IMPLEMENTED / TESTED in 0.66: titles/achievements, categories/sheets/rewards, three-state proof of acquisition and coalesced refresh; 421 tests and native probe passing, no runtime claimed.
- QUEST-REWARDS-003 IMPLEMENTED / TESTED in 0.67: native conditional rewards and Item/stocks link; 426 tests and probe 5,373 quests/9,313 rows passing. Four quantities, fifteen currencies and current-rank association explicitly unknown.
- QUEST-ITEM-SOURCES-004 IMPLEMENTED / TESTED in 0.68: shared Item → quests index, variants/coverage kept and atomic/cancellable publication; 434 tests, 3,496 Items/8,288 associations verified.
- NEXT QUEST-CURRENCIES-005-STUDY: fifteen currencies without a direct Item, GC keys and variants on evidence; do not choose company/currency by convention. The older NEXT entries below are historical; the maintainer's notes determine the current next step.

- SORT-TEMPLATES-007 IMPLEMENTED / TESTED in 0.59: bounded common library, compatible geometry, loading as a draft, copies without observations; 385 tests/package, Newtonsoft three modes and ZIP identical to the canonical outputs.
- NEXT BASE-AUDIT-002: targeted audit of the scope of the 17 tools, documents/paths and limits. Counter to check before any new big slice; 5% reserve.

- SORT-FILTERS-006 IMPLEMENTED / TESTED in 0.58: equipment, market and HQ potential, explicit unknowns, 379 tests/package, Newtonsoft and native probe passing.
- NEXT SORT-TEMPLATES-007: rule templates independent of the character, compatible geometry, loading as a draft without native application.

- SORT-PROTECTED-005 IMPLEMENTED / TESTED in 0.57: protected positions and inversion, refusal of absent/ambiguous indices, 374 tests/package and Newtonsoft three modes.
- NEXT SORT-FILTERS-006: equipment/market from the existing Item metadata; do not turn an absence of metadata into false.

- SORT-RULES-004 IMPLEMENTED / TESTED in 0.56: profiles per scope, four criteria, 32 reserved zones with native category/quality/search, positioned preview and refusal of losses; 369 tests/package and Newtonsoft three modes.
- NEXT SORT-PROTECTED-005: zones preserved at their current positions and inverted filling, with a guard on the visual indices and no new native engine.

- SORT-SCOPES-003 IMPLEMENTED / TESTED in 0.55: direct reading distinct from the bank, saddlebags/retainer, exact scope and recapture before application. 361 tests/package; runtime not observed.
- NEXT SORT-RULES-004: cell zones and filters of the native categories, multiple criteria; preview/swap and explicit application shared.

- SORT-NATIVE-002 IMPLEMENTED / TESTED in 0.54: explicit visual application of the four bags, recent snapshot revalidated, full swap. 355 tests/package; native/rendering/persistence not observed.
- NEXT SORT-SCOPES-003: capture live the opened saddlebags/retainers before proposing the application to the storage; the imported cache alone allows no writing.

UPDATE 2026-10-27 / 0.5.27: the old READY/TODO states of CRAFT-001A below are outdated. Catalogue, notebook and preparation implemented, 63 tests passed and user confirmation received. This old checkpoint is closed; the maintainer's notes describe the current slice. The unobserved runtime scenarios remain kept in TESTING.md.

- CORE-001 DONE: managed snapshot and InventoryService with owner/containers/NQ-HQ scope, coverage and observations; eight inventory tests pass.
- INV-001 DONE (implementation/tests): adapter of the four bags integrated, framework reading, invalidation and Partial/Unknown snapshots. No Complete assumed.
- INV-002 DONE (implementation/tests, 0.1.1): proof of loading/stability and complete reading, display of an exact total of the four bags if complete; new live test required.
- RUNTIME-001: current user protocol accepted; advanced cases of unloading/error/second character kept in TESTING.md, with no success assumed.
- ITEM-001 DONE on the code/tests side: shared Item catalogue, generation metadata and search by name/ID linked to the bags diagnostic.
- ITEM-001-RUNTIME DONE: French catalogue 50,880 items, search/selection and NQ/HQ/total confirmed by capture and user feedback.
- CRAFT-001A READY: Recipe + RecipeLevelTable + ingredients catalogue; first slice recipe → ingredients, based on the same Item catalogue.
- UI-003 DONE on the code/tests side: common space with navigation, directory of the 17 tools, settings and diagnostics separated. UI-003-RUNTIME ACTIVE: visual check 0.2.0 before resuming CRAFT-001A.
- UI-004 DONE on the code/tests side: directory replaced by a grid of native icons; details only in a sheet after a click. The UI-003-RUNTIME test must use the new DLL 0.2.1.
- BANK-001 DONE on the code/tests side: persistent cache per character/retainer/saddlebag, manual imports attached to menus and dated search results. BANK-001-RUNTIME ACTIVE: check anchoring/import/reimport/persistence in 0.3.0 before resuming the recipes.
- Bank: retainers' equipment/crystals/market and possible manual deletion of an old survey remain separate extensions, without claiming to be included in the survey of the seven pages.
- UI-005 DONE on the code/tests side: real icons and unified Character/Chocobo/Retainers grouping. UI-005-RUNTIME ACTIVE: check 0.4.0 visually. The 0.3.0 capture proves a consultable chocobo cache but does not yet validate the retainer imports or their attachment/persistence.
- CRAFT-001 TODO after ITEM-001: Recipe + RecipeLevelTable + ingredients catalogue; recipe → needs path.
- Runtime CORE-000 to perform: loading, command, preference, reloading/unloading and logs in an appropriate FFXIV client.
- The following phases of each plugin remain in ROADMAP.md. The repositories not provided must be identified and their licences checked at the time of their targeted study (NEEDS_STUDY).
- Combat/ACT/IINACT: removed from the project (user decision 2026-10-31).

---

# Backlog ciblé

## Ouvert — état au 0.315.0 (2026-11-02), corrections du 2026-11-04 (0.512)

La file a été traitée depuis (ROADMAP, CHANGELOG) ; une ligne peut être faite sans être marquée ici. Ce qui reste pour la 1.0.0 : [PUBLISHING](PUBLISHING.md).

Les points qui attendent une décision ou un relevé en jeu sont regroupés dans la [ROADMAP](ROADMAP.md), section « À décider / à
revoir » ; les tests en jeu en attente, dans sa section « Tests à faire en jeu ». L'ordre de passage est dans
le fichier d'état du mainteneur (« Prochaine action »).

Carte (MAP-001, plan : MAP-016-PLAN, notes du mainteneur (hors du dépôt))
- Icônes des commissionnaires (ex. commises de Rowena) : corrigé en 0.155 (MAP-017), validé en jeu 2026-10-31.
- Repères d'exploration : **clos** (était « en pause » au 0.154) ; météo du wiki citée, affichée et utilisée par les alarmes depuis 0.201.
- Icône de type de quête approximative (71141 calculé contre 71151 vu en jeu) ; correspondance `ObjectiveId` en partie non prouvée.
  Toujours ouvert : relevé en jeu attendu, pas bloquant pour D17 (le réglage dit « variante n »).
- Type de marqueur des mandats (repères au bord de la carte, 0.298) : relevé et traité en 0.652 ([preuve](../evidence/0.652-leve-markers.md)).
- Cadres et textures du jeu tirés des ULD (R-06 étape 6) : cadre retiré en 0.510 (DEC-033), clos.

Objets et collections
- Sources encore inconnues pour certains objets (Alsvid, PNJ du Certificat végétal) : inconnu documenté (décision 2026-11-01) ;
  autres types de collection à couvrir (COLL-001).
- Marchands PNJ : monnaies codées et InclusionShop déjà traités (ligne périmée au 0.154) ; boutiques de compagnie libre (FccShop)
  **faites en 0.295**, validées en jeu. Ligne close.
- Injection du texte dans l'infobulle native du jeu : **abandonnée** (décision utilisateur 2026-11-01).
- Monnaies de quête sans objet direct : inconnu documenté (QUEST-CURRENCIES-005-STUDY, notes du mainteneur (hors du dépôt)).

Inventaire et tri : arsenal, coffre à apparence et armoire lus comme rangements (0.235–0.240, à valider en jeu) ; leur tri
(SORT-ARMOURY-009, HEAVY) et SORT-AUTO-010 (opt-in) : après publication. Cache disque des petits catalogues (cartes, donjons,
usages) non fait, gain faible.

Récolte : bonus d'Abondance **fait en 0.224** comme estimation (règle communautaire citée, affichée « estimé », modifiable), à
valider en jeu ; l'ancienne note « ne pas inventer de formule » reste vraie : aucune formule officielle. Pêche en mer non couverte
(pas de source de données utilisable).

Compagnie libre, Rappels, Équipement (R-07, R-08, R-11) : restes et choix ouverts dans la ROADMAP (« À décider / à revoir ») :
table des seuils de sous-marin, arbre de déblocage des dirigeables (LuminaSupplemental, GPL-3.0), planificateur de trajets de
dirigeable, règle de capacité sous le rang 50, escouade et Planeurs d'Ehcatl, étape 4 de R-11. R-09 (événements) : pas commencé.

Publication (PUBLISH-018, voir [PUBLISHING](PUBLISHING.md)) : pas de soumission depuis DEC-039 (dépôt personnalisé seulement). Côté code fait
(0.156–0.157) : icône `src/TenshoAssistant/images/icon.png` (générée par IA, déclarée), `manifest.toml` prêt, brouillon de PR avec la
déclaration d'IA. L'ancienne mention « il manque `images/icon.png`, `manifest.toml` et la déclaration d'IA » est périmée depuis 0.157.
Reste à l'auteur : commit et push, hash du commit dans `manifest.toml`, texte de PR et déclaration réécrits par lui (voir
[CONTRIBUTING](../../CONTRIBUTING.md#publication-dalamud)) ; traductions allemande et japonaise partielles, non relues.

Les 17 lignes de la [ROADMAP](ROADMAP.md) restent PARTIAL.

## Revue des points ouverts (2026-11-01, avant publication D17)
Décision utilisateur : publier après résolution ; l'injection de texte dans l'infobulle native du jeu n'est **pas** faite.
Suivi au 0.315 (2026-11-02) ajouté en fin de chaque décision : seul le point des icônes de quête reste ouvert.

| Point | Constat | Décision |
|---|---|---|
| Bonus d'Abondance (Récolte abondante II, actions 272/273) | GatheringOptimizer (MIT) le demande à la main ; frozen_rabbit_tome (sans licence, fait relevé seulement) : +3 si récolte ≥ ⌊base × 1,1⌋, +2 si ≥ ⌊base × 0,9⌋, sinon +1, base = valeur « Gathering » de l'item level du point (table item-level de Teamcraft, issue de la feuille du jeu ItemLevel). Règle communautaire, non officielle. | **Faire** : préremplir le bonus avec cette règle (stat de récolte lue, item level du point par sonde), affiché « estimé », modifiable. → **FAIT 0.224** ([preuve](../evidence/0.224-bountiful-base.md)), à valider en jeu. |
| Revue de mode : coffre à apparence et armoire | Lisibles dans le client (dresser d'apparence, armoire) ; donne un « possédé » plus juste. | **Faire** (sonde avant usage). → **FAIT 0.235–0.240** (élargi à l'arsenal, lu sans autre plugin, relié à la Banque), à valider en jeu. |
| Vendeurs : boutiques de compagnie libre | Feuille FccShop non lue (InclusionShop et monnaies codées déjà traitées, ligne du backlog périmée). | **Faire** (petit). → **FAIT 0.295** ([preuve](../evidence/0.295-fcc-shops.md)), validé en jeu. |
| Icônes de quête : nom des variantes, 71141 vs 71151 | Sens des variantes absent des données ; seule l'observation en jeu le donne. | **Faire seulement avec un relevé en jeu** de l'utilisateur ; sinon laisser « variante n ». → **Ouvert** : aucun relevé au 0.315. |
| Carte : cible de quête du journal qui reste (demande 2026-11-01, R-01) | L'icône « Cible indiquée par le jeu (journal) » ne pouvait pas être retirée. | **Faire**. → **FAIT 0.242–0.243** (retrait au clic droit, puis automatique), à valider en jeu. |
| Carte : « flèches boussole » vers une cible hors de la vue (demande 2026-11-01) | — | **Faire**. → **FAIT 0.297** sous forme d'icônes au bord de la carte, un réglage par type ; validé en jeu. |
| Revue de mode : score | Aucune donnée de score par pièce dans les sources. | **Ne pas faire** (serait inventé). |
| Suppression d'une alarme du jeu depuis Tensho | Pas de sous-commande /alarm par alarme (seulement « clear » = tout) ; DeleteAlarm(index) non prouvé. | **Ne pas faire** (risque d'effacer la mauvaise alarme) ; rappel manuel conservé. |
| Sources inconnues restantes (Alsvid, PNJ du Certificat végétal) | Absentes du jeu, de Garland et de LuminaSupplemental. | **Ne pas faire** : inconnu documenté. |
| Repères d'exploration (météo du wiki) | Météo du wiki déjà affichée et utilisée par les alarmes (0.201). | **Clos** (source wiki citée). |
| Compléments des 17 outils (rotation adaptative, tri d'arsenal, règles Triad natives…) | Gros chantiers, non nécessaires pour publier. | **Après publication**, au cas par cas. → **Repris** dans le plan c) ([GAPS](GAPS.md), étape g de la [ROADMAP](ROADMAP.md)) : rotation adaptative A-04 retenue, tri de l'arsenal I-14 plus tard ; règles natives de Triple Triade non relevées par la comparaison, restent ici. |
| Injection dans l'infobulle native | — | **Abandonné** (décision utilisateur). |

## Archive

> Entrées historiques (2026-10-27 → 2026-10-30), de la plus récente à la plus ancienne ; leurs « NEXT » et compteurs sont ceux de leur date.

- QUEST-ITEM-PROGRESS-007 CLOSE (2026-10-30) : TESTED 477/477 + VALIDATED_IN_GAME rapporté par l’utilisateur sur la DLL 0.76.0.0 (déconnexion, séquence 255, « 1 variante » inclus). SORT-RUNTIME-VALIDATION CLOSE (tri natif validé en jeu, rapport utilisateur 2026-10-30). SORT-ZONE-SELECT-011 TESTED en 0.77 (multi-catégories, objets choisis, termes « ; », clic sur l'aperçu) ; validation en jeu à faire. Suite : SORT-ITEM-PROPS-008.
- CATALOG-CACHE-012 : objets 0.81 (validé en jeu), recettes 0.82, récolte/pêche 0.83 (TESTED). Map, Duty, ItemUse non faits (petits catalogues, gain faible).
- NPC-VENDORS-014 TESTED en 0.88 (outil PNJ vendeurs, drapeau carte, survol). Suites possibles : table vérifiée des monnaies codées hors mémoquartz (assignats), InclusionShop, boutiques de compagnie libre.
- HOVER-TOOLTIP-013 TESTED en 0.85 (panneau sous l'infobulle native, moins cher du centre de données, stock/obtention/usages). Option future HEAVY : injection du texte dans l'infobulle native.
- SORT-ITEM-PROPS-008 TESTED en 0.84 (niveaux, rareté, prix, ID, unique/non échangeable/teinture/réparable/collectionnable). Suite SortaKinda : SORT-ARMOURY-009 (HEAVY), SORT-AUTO-010 opt-in.

- QUEST-CURRENCIES-005-STUDY clôturée sur UNKNOWN documenté : quinze monnaies sans Item/clé GC de récompense, aucune relation unique trouvée ; quantités conservées, pas de compagnie inventée. Voir QUEST-CURRENCIES-005-STUDY (notes du mainteneur (hors du dépôt)). BASE-AUDIT-003 terminé : cohérence/livraison 0.69 et diagnostics améliorés ; suite indépendante selon quota, sans blocage global.

- BASE-AUDIT-002 terminé dans son périmètre de contrôle : 17 références présentes, 17 couvertures PARTIAL, 71 liens locaux/61 Markdown résolus, DLL et paquet 0.59.0.0 concordants. Voir BASE-AUDIT-002 (notes du mainteneur (hors du dépôt)). Prochaine étude indépendante : catalogue GatherBuddy/pêche, après contrôle du quota ; ne pas inventer les appâts/prérequis absents.

- USE-CONTEXT-004 IMPLEMENTED / TESTED en 0.53 : menu d’inventaire opt-in, cinq routes partagées, garde de propriétaire/révision/âge, aucune requête réseau par le menu. Rendu natif à confirmer ; liens de discussion et cibles Default hors de cette tranche.
- SORT-NATIVE-002-STUDY OBSOLÈTE (2026-10-30) : application native livrée en 0.54–0.59. Remplacée par SORT-GAP-008-STUDY (notes du mainteneur (hors du dépôt)) : 1) validation en jeu du tri natif (jamais observé), 2) SORT-ITEM-PROPS-008 (LIGHT, propriétés Item statiques), 3) SORT-ARMOURY-009, 4) SORT-AUTO-010 opt-in.

État au 0.75.0 (2026-10-29, archive) : cache partagé et péremption réglable (CACHE-001, notes du mainteneur (hors du dépôt)). Prix, quêtes, collections, sources communautaires, Revue de mode et calculs du carnet réutilisent des instantanés persistants datés. Durées indépendantes dans Réglages : 5 min, 1 h, 24 h ou 7 j ; actualisation forcée depuis les outils. Les imports restent manuels et signalent leur péremption ; les observations en direct gardent une invalidation courte. 466/466 tests/package/SHA-256 passants, zéro avertissement/erreur ; validation en jeu de ce cache encore à faire.

- GATHER-FISH-006 IMPLEMENTED / TESTED en 0.60 : carnet natif, 2 461 fiches, restrictions et tomes ; 389 tests/package et probes natifs passants. 491 codes météo restent inconnus.
- GATHER-FISH-SOURCES-007 IMPLEMENTED / TESTED en 0.61 : appâts/chaînes par lieu, cache/transport communs, 396 tests et consultation réelle 4869 passants. Code météo natif 2 non expliqué, Unknown conservé.
- GATHER-FISH-WINDOWS-008 IMPLEMENTED / TESTED en 0.62 : conditions par lieu, plages horaires ET/UTC, météo précédente distincte ; 401 tests et probes réels passants. Disponibilité non déduite.
- GATHER-FISH-WEATHER-009 IMPLEMENTED / TESTED en 0.63 : noms/icônes météo natifs par territoire, ambiguïtés et métadonnées incomplètes inconnues ; 405 tests et probes réels passants.
- GATHER-FISH-FORECAST-010 IMPLEMENTED / TESTED en 0.64 : plages des critères renseignés, couverture et logique ternaire explicites ; 412 tests et probes réels/fixtures datées passants. Pas de capture garantie.
- COLL-002 IMPLEMENTED / TESTED en 0.65 : orchestrion natif, confirmations positives, catégories et liens Item/contexte bidirectionnels, erreurs de catégorie isolées ; 416 tests et probe natif passants.
- COLL-003 IMPLEMENTED / TESTED en 0.66 : titres/succès, catégories/fiches/récompenses, preuve d’acquisition à trois états et refresh coalescé ; 421 tests et probe natif passants, aucun runtime revendiqué.
- QUEST-REWARDS-003 IMPLEMENTED / TESTED en 0.67 : récompenses conditionnelles natives et lien Item/stocks ; 426 tests et probe 5 373 quêtes/9 313 lignes passants. Quatre quantités, quinze monnaies et association de rang courant inconnus explicitement.
- QUEST-ITEM-SOURCES-004 IMPLEMENTED / TESTED en 0.68 : index partagé Item → quêtes, variantes/couverture conservées et publication atomique/cancellable ; 434 tests, 3 496 Items/8 288 associations vérifiées.
- NEXT QUEST-CURRENCIES-005-STUDY : quinze monnaies sans Item direct, clés GC et variantes sur preuve ; ne pas choisir compagnie/monnaie par convention. Les anciennes entrées NEXT ci-dessous sont historiques ; les notes du mainteneur déterminent la suite courante.

- SORT-TEMPLATES-007 IMPLEMENTED / TESTED en 0.59 : bibliothèque commune bornée, géométrie compatible, chargement en brouillon, copies sans observations ; 385 tests/package, Newtonsoft trois modes et ZIP identique aux sorties canoniques.
- NEXT BASE-AUDIT-002 : audit ciblé du périmètre des 17 outils, documents/chemins et limites. Compteur à vérifier avant toute nouvelle grosse tranche ; réserve 5 %.

- SORT-FILTERS-006 IMPLEMENTED / TESTED en 0.58 : équipement, marché et potentiel HQ, inconnus explicites, 379 tests/package, Newtonsoft et probe natif passants.
- NEXT SORT-TEMPLATES-007 : modèles de règles indépendants du personnage, géométrie compatible, chargement en brouillon sans application native.

- SORT-PROTECTED-005 IMPLEMENTED / TESTED en 0.57 : positions protégées et inversion, refus des indices absents/ambigus, 374 tests/package et Newtonsoft trois modes.
- NEXT SORT-FILTERS-006 : équipement/marché depuis les métadonnées Item existantes ; ne pas transformer une absence de métadonnées en false.

- SORT-RULES-004 IMPLEMENTED / TESTED en 0.56 : profils par scope, quatre critères, 32 zones réservées avec catégorie native/qualité/recherche, aperçu positionné et refus des pertes ; 369 tests/package et Newtonsoft trois modes.
- NEXT SORT-PROTECTED-005 : zones préservées à leurs positions actuelles et remplissage inversé, avec garde sur les indices visuels et sans nouveau moteur natif.

- SORT-SCOPES-003 IMPLEMENTED / TESTED en 0.55 : lecture directe distincte de la banque, sacoches/servant, scope exact et recapture avant application. 361 tests/package ; runtime non observé.
- NEXT SORT-RULES-004 : zones de cases et filtres des catégories natives, critères multiples ; aperçu/permutation et application explicite partagés.

- SORT-NATIVE-002 IMPLEMENTED / TESTED en 0.54 : application visuelle explicite des quatre sacs, snapshot récent revalidé, permutation intégrale. 355 tests/package ; natif/rendu/persistance non observés.
- NEXT SORT-SCOPES-003 : capturer en direct les sacoches/servants ouverts avant de proposer l’application au rangement ; le cache importé seul ne permet aucune écriture.

MISE À JOUR 2026-10-27 / 0.5.27 : les anciens états READY/TODO de CRAFT-001A ci-dessous sont dépassés. Catalogue, carnet et préparation implémentés, 63 tests réussis et confirmation utilisateur reçue. Cet ancien checkpoint est clos ; les notes du mainteneur décrivent la tranche actuelle. Les scénarios runtime non observés restent conservés dans TESTING.md.

- CORE-001 DONE : snapshot managé et InventoryService avec portée propriétaire/conteneurs/NQ-HQ, couverture et observations ; huit tests inventaire passent.
- INV-001 DONE (implémentation/tests) : adaptateur des quatre sacs intégré, lecture framework, invalidation et snapshots Partial/Unknown. Aucun Complete supposé.
- INV-002 DONE (implémentation/tests, 0.1.1) : preuve de chargement/stabilité et lecture complète, affichage d'un total exact des quatre sacs si complet ; nouvel essai live requis.
- RUNTIME-001 : protocole utilisateur courant accepté ; cas avancés de déchargement/erreur/second personnage conservés dans TESTING.md, sans réussite supposée.
- ITEM-001 DONE côté code/tests : catalogue Item partagé, métadonnées de génération et recherche par nom/ID reliée au diagnostic des sacs.
- ITEM-001-RUNTIME DONE : catalogue français 50 880 objets, recherche/sélection et NQ/HQ/total confirmés par capture et retour utilisateur.
- CRAFT-001A READY : catalogue Recipe + RecipeLevelTable + ingrédients ; première tranche recette → ingrédients, basée sur le même catalogue Item.
- UI-003 DONE côté code/tests : espace commun avec navigation, répertoire des 17 outils, réglages et diagnostics séparés. UI-003-RUNTIME ACTIVE : contrôle visuel 0.2.0 avant reprise de CRAFT-001A.
- UI-004 DONE côté code/tests : répertoire remplacé par une grille d'icônes natives ; détails seulement dans une fiche après clic. L'essai UI-003-RUNTIME doit utiliser la nouvelle DLL 0.2.1.
- BANK-001 DONE côté code/tests : cache persistant par personnage/servant/sacoche, imports manuels attachés aux menus et résultats de recherche datés. BANK-001-RUNTIME ACTIVE : vérifier ancrage/import/réimport/persistance en 0.3.0 avant reprise des recettes.
- Banque : équipements/cristaux/marché des servants et éventuelle suppression manuelle d'un ancien relevé restent des extensions séparées, sans prétendre être inclus dans le relevé des sept pages.
- UI-005 DONE côté code/tests : icônes réelles et regroupement unifié Personnage/Chocobo/Servants. UI-005-RUNTIME ACTIVE : vérifier visuellement 0.4.0. La capture 0.3.0 prouve un cache chocobo consultable mais ne valide pas encore les imports servant ou leur attache/persistance.
- CRAFT-001 TODO après ITEM-001 : catalogue Recipe + RecipeLevelTable + ingrédients ; chemin recette → besoins.
- Runtime CORE-000 à effectuer : chargement, commande, préférence, rechargement/déchargement et logs dans un client FFXIV approprié.
- Les phases suivantes de chaque plugin restent dans ROADMAP.md. Les dépôts non fournis doivent être identifiés et leurs licences vérifiées au moment de leur étude ciblée (NEEDS_STUDY).
- Combat/ACT/IINACT : retiré du projet (décision utilisateur 2026-10-31).
