# Plan: Tensho switches itself off in PvP and in ranked content

> *Traduction française plus bas.*

State: **coded in 0.610, compiled and tested, not tried in game.** User's request, 2026-11-05: « prevoir que en pvp le plugin se deactive et reactive apres pvp pas de reglages fais obligatoirement pareil pour match classer triad ou dresseur ou autre tous ce qui pourrais etre detecter par SE ou cree un impact sur des classements » ("plan that in PvP the plugin deactivates and reactivates after PvP, no settings, make it compulsory, same for ranked match, triad or beastmaster or anything else that could be detected by SE or create an impact on rankings"). Decision: DEC-038. Data: [docs/evidence/0.600-competitive-inventory.md](../evidence/0.600-competitive-inventory.md).

## What is wanted

- In PvP and in ranked content, Tensho stops by itself. On leaving, it starts again by itself, as it was.
- No setting, no checkbox, no command to avoid it.
- Dalamud rule concerned: no advantage in a competitive or PvP environment ([CONTRIBUTING.md](../../CONTRIBUTING.md#publication-dalamud)).

**A limit to state clearly:** "detectable by SE" (« détectable par SE ») cannot be measured. This plan reduces what Tensho does in this content to almost nothing, it cannot promise that nothing is detectable.

## Dalamud clarifications (reread on 2026-11-05)

Sources: [restrictions](https://dalamud.dev/plugin-publishing/restrictions), Dalamud code (`Dalamud/Game/ClientState/ClientState.cs`).

- **PvP:** the page lists everything done in PvP among the outright refusals ("Anything in PvP: Not allowed"). Switching all of Tensho off goes in this direction.
- **"Competitive":** the rule forbids any advantage in a competitive environment, without giving a list or a definition. The review is subjective. For a doubt, the page asks to contact the team on the Dalamud Discord: that is for the author to do, not the agent.
- **"Detectable by SE":** this is not a Dalamud criterion. The closest: no exchange with the game's servers that is automatic or outside what the game allows. This rule applies everywhere, not only in PvP: the inventory of step 1 covers it too, including the per-job title setting (DEC-035).
- **No switch-off offered by Dalamud:** it gives `IsPvP`, `EnterPvP` and `LeavePvP`, nothing that stops a plugin in its place. The guard remains to be written.
- **`IsPvP` is not enough:** Dalamud deduces it from `TerritoryType.IsPvpZone`. It is therefore false at 579 (Battlehall) and at the Lord of Verminion territories, which are absent from the 24 zones: `ContentFinderCondition.PvP` is also needed. It is true at the Wolves' Den dock (250), the only territory that `IsPvPExcludingDen` removes.

## What already exists

Only the Triple Triad advice is switched off (`TriadMatchAdvisor.Competitive`: 579, 940, 941; DEC-033). Everything else in Tensho runs in PvP today: no code reads `IsPvP`.

## Where Tensho switches off

| Level | Content | How it is known |
|---|---|---|
| 1. Game flags | PvP, Triple Triad Battlehall, Lord of Verminion | `IClientState.IsPvP` with `EnterPvP` and `LeavePvP`, `TerritoryType.IsPvpZone`, `ContentFinderCondition.PvP`: new PvP maps are covered without an update |
| 1 bis. During the match | Triple Triad Battlehall (Triple Triad Battlehall, 579): the user tried it, the place has PvE. The public hall has PvE matches: neither the place nor the match is enough. Tensho only switches off inside the arena instance (0.612) | `GameMain.IsInPvPInstance`. Measured in the public hall, PvE duel included: false, while `BoundByDuty` and the content row are true there. **True in an arena match: to confirm in game**, the log writes these values (`Competitive arena check`) |
| 2. Listed | Triple Triad tournaments (940, 941), Crucible of the Unbroken (1339-1343), Mahjong (831, ranked or not) | No flag: list in the code, with its evidence |
| 3. To establish | Deep dungeons | Switched off if step 1 establishes that they are ranked (D1). Any other ranked content found there, Ishgard or cosmic exploration included, goes back to the user with its evidence: nothing is switched off without their agreement |
| 4. Outside a zone | Triple Triad against a player, in a normal zone | The zone is not enough: only the Triple Triad part switches off (D5) |

## What "switched off" means

Tensho stays loaded: that is what lets it start again by itself. A single guard does everything, on the framework thread, and only keeps the reading of the zone.

| Part | In a switched-off zone |
|---|---|
| Display | Nothing: window, panels, map, tooltips, context menus, server bar |
| The game's map | Given back to the game if Tensho replaces it (`NativeMapReplacer`) |
| Game readings | Stopped, `Framework.Update` subscriptions included |
| Hooks | Disabled (`CraftRecorder`, `MacroChainBridge`) |
| Actions on the game | Refused: macros, sort, gear sets, flag, teleport, opening of windows |
| Network | No new request; requests in progress cancelled |
| Alarms and Discord | Nothing during the switch-off, no catching up (D2) |
| `/tensho` commands | One line saying that Tensho is switched off |
| Data | Nothing is erased; no writing of the settings file |

On leaving: the windows come back as they were, the readings resume, the game's map is replaced again if the setting asks for it.

**In case of doubt, Tensho stays switched off:** unknown zone during a loading, sheet unreadable after a game update. When the plugin loads while already in PvP, it starts switched off.

## Steps

1. **Inventory, without the game.** Look for the other rankings (game windows, ClientStructs, data). List every `Framework.Update` subscription, hook, call that acts on the game and network request of the plugin: 118 registrations in `Plugin.cs`, 93 `.Dalamud.cs` files.
2. **Decisions** D1 to D5: taken, see below. What remains is what the inventory will find new.
3. **Guard and detection.** A pure, tested rule, plugged into the game's events. DEC updated.
4. **Display.** Switch-off at the entry of `Draw`, game map given back, server bar, menus and tooltips.
5. **Readings, hooks, actions, network.** Each subscription goes through the guard. A test walks through the code and refuses any new subscription that does not go through it, like the one for the tooltips.
6. **Alarms and Discord**, according to D2, and the chat line of D3.
7. **Documents.** `PUBLISHING.md`, `CONTRIBUTING.md`, help and getting started; the Triple Triad advice goes through the same rule.
8. **In-game test**, one point at a time: see below. **To do.**

Size: big undertaking, mostly step 5. Place in the global plan: before phase 5, so that the in-game tests are already done with this switch-off.

## Tests

- Pure rule: each territory of the evidence, an unknown territory, an unknown state.
- No setting: no property of `Configuration` nor language key allows avoiding it.
- Code: every subscription, hook and action call goes through the guard.

Tests already done (2026-11-05, user's report and log, 0.611): Wolves' Den dock, territory 250: switched off on entry, chat line, `/tensho` answers with the same line, resumed by itself on leaving. Battlehall hall (0.614, user's report): no switch-off, Triple Triad panel displayed in a duel against an NPC. Still to try: an arena match against a player, a content detected by the ContentFinderCondition flag (Lord of Verminion), one from the list (Mahjong, tournament, Crucible of the Unbroken), a deep dungeon, a Frontline or Crystalline Conflict.

In-game tests: Wolves' Den dock; custom Crystalline Conflict match; Frontline; Triple Triad Battlehall and tournament; Lord of Verminion; Crucible of the Unbroken; Mahjong. For each: enter with the map and panels open, everything must disappear and come back on leaving. Also: reload the plugin in the middle of a match; disconnect in the middle of a match.

## Decisions taken (2026-11-05, checklist)

| No. | Question | User's answer |
|---|---|---|
| D1 | Which content switches off all of Tensho, in addition to PvP, the Battlehall and Lord of Verminion? | Triple Triad tournaments and Mahjong (ranked and unranked, same territory). Crucible of the Unbroken. If the inventory establishes that they are ranked: deep dungeons. **Not** ocean fishing (no ranking, according to the user), nor Ishgard and cosmic exploration (the user thinks there is none), nor chocobo races and the rest of the Gold Saucer |
| D2 | An alarm during a switch-off? | Nothing: no sound, no chat, no Discord, and no catching up on leaving |
| D3 | Does Tensho warn? | One line in the chat on entry, nothing on leaving |
| D4 | Wolves' Den dock? | Switched off |
| D5 | Triple Triad against a player in a normal zone? | Only the Triple Triad part switches off |

Crucible of the Unbroken panel: abandoned here, then taken up again as a limited exception by DEC-047 (X-109): the team panel in front of the board, between
fights only, switched off from the start of each fight. The rest of the beastmaster remains a list of information, outside the Crucible of the Unbroken.

Question to the Dalamud team on what counts as competitive: not retained for the moment. The rule gives no list: the review of the submission may therefore judge otherwise.

Inventory: the deep dungeons have a ranking (Lodestone), so they switch Tensho off; ocean fishing, Ishgard and cosmic exploration have none according to this page.

## What is coded (0.610)

`Competitive/CompetitiveGate.Dalamud.cs` and `Competitive/CompetitiveZones.cs`. During the switch-off, the framework subscriptions of 77 files go through the guard, and a test refuses any that would bypass it. Since 0.615, the chat readers too: they still read in 0.610 to 0.614, an oversight corrected. Still active, without reading the game in combat: the modified-inventory notice, which only marks a reread for later, and the change of the system theme setting. Display, commands, context menu, server bar: switched off. The game's map: given back. Discord: what was waiting is abandoned. One line in the chat on entry.

Differences from the plan, to know about:

- The hooks stay in place (macro, sequence recording), but let the game's call through without doing anything; they are not removed.
- A web request that has already left finishes normally; no new one leaves, for lack of display and ticks. It is not cancelled.
- An inventory sort or a macro write already in progress stops in the middle and resumes on leaving. Not tried.
- An alarm whose time falls during the match does not ring afterwards: the calculation looks at the next occurrence. An alarm whose advance notice begins during the match and whose time falls after rings on leaving.
- If the game sheets become unreadable after an update, Tensho stays switched off everywhere as long as one is connected, with a warning in the log only.

---

# Plan : Tensho se coupe en JcJ et en contenu classé

État : **codé en 0.610, compilé et testé, pas essayé en jeu.** Demande de l'utilisateur, 2026-11-05 : « prevoir que en pvp le plugin se deactive et reactive apres pvp pas de reglages fais obligatoirement pareil pour match classer triad ou dresseur ou autre tous ce qui pourrais etre detecter par SE ou cree un impact sur des classements ». Décision : DEC-038. Données : [docs/evidence/0.600-competitive-inventory.md](../evidence/0.600-competitive-inventory.md).

## Ce qui est voulu

- En JcJ et en contenu classé, Tensho s'arrête tout seul. À la sortie, il repart tout seul, comme il était.
- Aucun réglage, aucune case, aucune commande pour l'éviter.
- Règle Dalamud concernée : pas d'avantage en environnement compétitif ou JcJ ([CONTRIBUTING.md](../../CONTRIBUTING.md#publication-dalamud)).

**Limite à dire clairement :** « détectable par SE » ne se mesure pas. Ce plan réduit ce que Tensho fait dans ces contenus à presque rien, il ne peut pas promettre que rien n'est détectable.

## Précisions Dalamud (relues le 2026-11-05)

Sources : [restrictions](https://dalamud.dev/plugin-publishing/restrictions), code de Dalamud (`Dalamud/Game/ClientState/ClientState.cs`).

- **JcJ :** la page classe tout ce qui se fait en JcJ parmi les refus d'office (« Anything in PvP: Not allowed »). Couper tout Tensho va dans ce sens.
- **« Compétitif » :** la règle interdit tout avantage en environnement compétitif, sans en donner de liste ni de définition. L'examen est subjectif. Pour un doute, la page demande de contacter l'équipe sur le Discord de Dalamud : c'est à l'auteur de le faire, pas à l'agent.
- **« Détectable par SE » :** ce n'est pas un critère de Dalamud. Le plus proche : pas d'échange avec les serveurs du jeu qui soit automatique ou hors de ce que le jeu permet. Cette règle vaut partout, pas seulement en JcJ : l'inventaire de l'étape 1 la couvre aussi, y compris le réglage du titre par job (DEC-035).
- **Pas de coupure offerte par Dalamud :** il donne `IsPvP`, `EnterPvP` et `LeavePvP`, rien qui arrête un plugin à sa place. Le gardien reste à écrire.
- **`IsPvP` ne suffit pas :** Dalamud le déduit de `TerritoryType.IsPvpZone`. Il est donc faux à 579 (Battlehall) et aux territoires de Lord of Verminion, absents des 24 zones : il faut aussi `ContentFinderCondition.PvP`. Il est vrai au quai de Wolves' Den (250), le seul territoire que retire `IsPvPExcludingDen`.

## Ce qui existe déjà

Seuls les conseils de Triple Triade sont coupés (`TriadMatchAdvisor.Competitive` : 579, 940, 941 ; DEC-033). Tout le reste de Tensho tourne en JcJ aujourd'hui : aucun code ne lit `IsPvP`.

## Où Tensho se coupe

| Niveau | Contenu | Comment on le sait |
|---|---|---|
| 1. Drapeaux du jeu | JcJ, Triple Triad Battlehall, Lord of Verminion | `IClientState.IsPvP` avec `EnterPvP` et `LeavePvP`, `TerritoryType.IsPvpZone`, `ContentFinderCondition.PvP` : les nouvelles cartes JcJ sont couvertes sans mise à jour |
| 1 bis. Pendant le match | Triple Triad Battlehall (Arène Triple Triade, 579) : l'utilisateur l'a essayée, le lieu a du JcE. Le hall public a des matchs JcE : ni le lieu ni le match ne suffisent. Tensho ne se coupe que dans l'instance de l'arène (0.612) | `GameMain.IsInPvPInstance`. Mesuré dans le hall public, duel JcE compris : faux, alors que `BoundByDuty` et la ligne de contenu y sont vrais. **Vrai dans un match de l'arène : à confirmer en jeu**, le journal écrit ces valeurs (`Competitive arena check`) |
| 2. Liste relevée | Tournois de Triple Triade (940, 941), Dédale bestial (1339-1343), Mahjong (831, classé ou non) | Pas de drapeau : liste dans le code, avec sa preuve |
| 3. À établir | Donjons profonds | Coupés si l'étape 1 établit leur classement (D1). Un autre contenu classé trouvé là, Ishgard ou l'exploration cosmique compris, revient à l'utilisateur avec sa preuve : rien n'est coupé sans son accord |
| 4. Hors zone | Triple Triade contre un joueur, dans une zone normale | La zone ne suffit pas : seule la partie Triple Triade se coupe (D5) |

## Ce que « coupé » veut dire

Tensho reste chargé : c'est ce qui lui permet de repartir seul. Un seul gardien fait tout, sur le thread framework, et ne garde que la lecture de la zone.

| Partie | En zone coupée |
|---|---|
| Affichage | Rien : fenêtre, panneaux, carte, infobulles, menus contextuels, barre de serveur |
| Carte du jeu | Rendue au jeu si Tensho la remplace (`NativeMapReplacer`) |
| Lectures du jeu | Arrêtées, abonnements `Framework.Update` compris |
| Hooks | Désactivés (`CraftRecorder`, `MacroChainBridge`) |
| Actions sur le jeu | Refusées : macros, tri, sets d'équipement, drapeau, téléportation, ouverture de fenêtres |
| Réseau | Pas de nouvelle requête ; requêtes en cours annulées |
| Alarmes et Discord | Rien pendant la coupure, pas de rattrapage (D2) |
| Commandes `/tensho` | Une ligne qui dit que Tensho est coupé |
| Données | Rien n'est effacé ; pas d'écriture du fichier de réglages |

À la sortie : les fenêtres reviennent comme elles étaient, les lectures reprennent, la carte du jeu est de nouveau remplacée si le réglage le demande.

**En cas de doute, Tensho reste coupé :** zone inconnue pendant un chargement, feuille illisible après une mise à jour du jeu. Au chargement du plugin déjà en JcJ, il démarre coupé.

## Étapes

1. **Inventaire, sans le jeu.** Chercher les autres classements (fenêtres du jeu, ClientStructs, données). Lister chaque abonnement `Framework.Update`, hook, appel qui agit sur le jeu et requête réseau du plugin : 118 enregistrements dans `Plugin.cs`, 93 fichiers `.Dalamud.cs`.
2. **Décisions** D1 à D5 : prises, voir plus bas. Reste ce que l'inventaire trouvera de nouveau.
3. **Gardien et détection.** Une règle pure et testée, branchée sur les événements du jeu. DEC mise à jour.
4. **Affichage.** Coupure à l'entrée de `Draw`, carte du jeu rendue, barre de serveur, menus et infobulles.
5. **Lectures, hooks, actions, réseau.** Chaque abonnement passe par le gardien. Un test parcourt le code et refuse tout nouvel abonnement qui n'y passe pas, comme celui des infobulles.
6. **Alarmes et Discord**, selon D2, et la ligne de chat de D3.
7. **Documents.** `PUBLISHING.md`, `CONTRIBUTING.md`, aide et prise en main ; les conseils de Triple Triade passent par la même règle.
8. **Essai en jeu**, un point à la fois : voir plus bas. **À faire.**

Taille : gros chantier, surtout l'étape 5. Place dans le plan global : avant la phase 5, pour que les essais en jeu se fassent déjà avec cette coupure.

## Tests

- Règle pure : chaque territoire de la preuve, un territoire inconnu, un état inconnu.
- Aucun réglage : aucune propriété de `Configuration` ni clé de langue ne permet de l'éviter.
- Code : tout abonnement, hook et appel d'action passe par le gardien.

Essais déjà faits (2026-11-05, rapport de l'utilisateur et journal, 0.611) : quai de Wolves' Den, territoire 250 : coupé à l'entrée, ligne de chat, `/tensho` répond par la même ligne, repris seul à la sortie. Hall de la Battlehall (0.614, rapport de l'utilisateur) : pas de coupure, panneau Triple Triade affiché en duel contre un PNJ. Reste à essayer : un match de l'arène contre un joueur, un contenu détecté par le drapeau de ContentFinderCondition (Lord of Verminion), un de la liste (Mahjong, tournoi, Dédale), un donjon profond, un Frontline ou Crystalline Conflict.

Essais en jeu : quai de Wolves' Den ; match personnalisé de Crystalline Conflict ; Frontline ; Triple Triad Battlehall et tournoi ; Lord of Verminion ; Dédale ; Mahjong. Pour chacun : entrer avec la carte et des panneaux ouverts, tout doit disparaître et revenir à la sortie. Aussi : recharger le plugin en plein match ; se déconnecter en plein match.

## Décisions prises (2026-11-05, liste à cocher)

| N° | Question | Réponse de l'utilisateur |
|---|---|---|
| D1 | Quels contenus coupent tout Tensho, en plus du JcJ, de la Battlehall et de Lord of Verminion ? | Tournois de Triple Triade et Mahjong (classé et non classé, même territoire). Dédale bestial. Si l'inventaire établit leur classement : donjons profonds. **Pas** la pêche en mer (pas de classement, selon l'utilisateur), ni Ishgard et l'exploration cosmique (l'utilisateur pense qu'il n'y en a pas), ni les courses de chocobos et le reste du Gold Saucer |
| D2 | Une alarme pendant une coupure ? | Rien : ni son, ni chat, ni Discord, et pas de rattrapage à la sortie |
| D3 | Tensho prévient-il ? | Une ligne dans le chat à l'entrée, rien à la sortie |
| D4 | Quai de Wolves' Den ? | Coupé |
| D5 | Triple Triade contre un joueur en zone normale ? | Seule la partie Triple Triade se coupe |

Panneau du Dédale : abandonné ici, puis repris en exception limitée par DEC-047 (X-109) : le panneau d'équipe devant le plateau, entre
les combats seulement, coupé dès le début de chaque combat. Le reste du dresseur reste une liste d'informations, hors du Dédale.

Question à l'équipe Dalamud sur ce qui compte comme compétitif : pas retenue pour le moment. La règle n'en donne pas de liste : l'examen de la soumission peut donc juger autrement.

Inventaire : les donjons profonds ont un classement (Lodestone), donc ils coupent Tensho ; la pêche en mer, Ishgard et l'exploration cosmique n'en ont pas d'après cette page.

## Ce qui est codé (0.610)

`Competitive/CompetitiveGate.Dalamud.cs` et `Competitive/CompetitiveZones.cs`. Pendant la coupure, les abonnements au framework de 77 fichiers passent par le gardien, et un test refuse celui qui le contournerait. Depuis 0.615, les lecteurs du chat aussi : ils lisaient encore en 0.610 à 0.614, un oubli corrigé. Restent actifs, sans lire le jeu en combat : l'avis d'inventaire modifié, qui ne fait que marquer une relecture pour après, et le changement de réglage système du thème. Affichage, commandes, menu contextuel, barre de serveur : coupés. Carte du jeu : rendue. Discord : ce qui attendait est abandonné. Une ligne dans le chat à l'entrée.

Écarts avec le plan, à connaître :

- Les hooks restent posés (macro, enregistrement de séquence), mais laissent passer l'appel du jeu sans rien faire ; ils ne sont pas retirés.
- Une requête web déjà partie finit normalement ; aucune nouvelle ne part, faute d'affichage et de ticks. Elle n'est pas annulée.
- Un tri d'inventaire ou une écriture de macro déjà en cours s'arrête au milieu et reprend à la sortie. Pas essayé.
- Une alarme dont l'heure tombe pendant le match ne sonne pas ensuite : le calcul regarde la prochaine occurrence. Une alarme dont le préavis commence pendant le match et dont l'heure tombe après sonne à la sortie.
- Si les feuilles du jeu deviennent illisibles après une mise à jour, Tensho reste coupé partout tant qu'on est connecté, avec un avertissement dans le journal seulement.
