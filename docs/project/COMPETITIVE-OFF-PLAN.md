# Plan : Tensho se coupe en JcJ et en contenu classé

État : **plan, rien n'est codé.** Demande de l'utilisateur, 2026-11-05 : « prevoir que en pvp le plugin se deactive et reactive apres pvp pas de reglages fais obligatoirement pareil pour match classer triad ou dresseur ou autre tous ce qui pourrais etre detecter par SE ou cree un impact sur des classements ». Décision : DEC-038. Données : [docs/evidence/0.600-competitive-inventory.md](../evidence/0.600-competitive-inventory.md).

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
8. **Essai en jeu**, un point à la fois : voir plus bas.

Taille : gros chantier, surtout l'étape 5. Place dans le plan global : avant la phase 5, pour que les essais en jeu se fassent déjà avec cette coupure.

## Tests

- Règle pure : chaque territoire de la preuve, un territoire inconnu, un état inconnu.
- Aucun réglage : aucune propriété de `Configuration` ni clé de langue ne permet de l'éviter.
- Code : tout abonnement, hook et appel d'action passe par le gardien.

Essais en jeu : quai de Wolves' Den ; match personnalisé de Crystalline Conflict ; Frontline ; Triple Triad Battlehall et tournoi ; Lord of Verminion ; Dédale ; Mahjong. Pour chacun : entrer avec la carte et des panneaux ouverts, tout doit disparaître et revenir à la sortie. Aussi : recharger le plugin en plein match ; se déconnecter en plein match.

## Décisions prises (2026-11-05, liste à cocher)

| N° | Question | Réponse de l'utilisateur |
|---|---|---|
| D1 | Quels contenus coupent tout Tensho, en plus du JcJ, de la Battlehall et de Lord of Verminion ? | Tournois de Triple Triade et Mahjong (classé et non classé, même territoire). Dédale bestial. Si l'inventaire établit leur classement : donjons profonds. **Pas** la pêche en mer (pas de classement, selon l'utilisateur), ni Ishgard et l'exploration cosmique (l'utilisateur pense qu'il n'y en a pas), ni les courses de chocobos et le reste du Gold Saucer |
| D2 | Une alarme pendant une coupure ? | Rien : ni son, ni chat, ni Discord, et pas de rattrapage à la sortie |
| D3 | Tensho prévient-il ? | Une ligne dans le chat à l'entrée, rien à la sortie |
| D4 | Quai de Wolves' Den ? | Coupé |
| D5 | Triple Triade contre un joueur en zone normale ? | Seule la partie Triple Triade se coupe |

Panneau du Dédale : abandonné. La page du dresseur reste une liste d'informations, hors du Dédale.

Question à l'équipe Dalamud sur ce qui compte comme compétitif : pas retenue pour le moment. La règle n'en donne pas de liste : l'examen de la soumission peut donc juger autrement.

Code : pas commencé, à la demande de l'utilisateur. À lancer sur son feu vert, avant la phase 5.
