# Plan : Tensho se coupe en JcJ et en contenu classé

État : **plan, rien n'est codé.** Demande de l'utilisateur, 2026-11-05 : « prevoir que en pvp le plugin se deactive et reactive apres pvp pas de reglages fais obligatoirement pareil pour match classer triad ou dresseur ou autre tous ce qui pourrais etre detecter par SE ou cree un impact sur des classements ». Décision : DEC-038. Données : [docs/evidence/0.600-competitive-inventory.md](../evidence/0.600-competitive-inventory.md).

## Ce qui est voulu

- En JcJ et en contenu classé, Tensho s'arrête tout seul. À la sortie, il repart tout seul, comme il était.
- Aucun réglage, aucune case, aucune commande pour l'éviter.
- Règle Dalamud concernée : pas d'avantage en environnement compétitif ou JcJ ([CONTRIBUTING.md](../../CONTRIBUTING.md#publication-dalamud)).

**Limite à dire clairement :** « détectable par SE » ne se mesure pas. Ce plan réduit ce que Tensho fait dans ces contenus à presque rien, il ne peut pas promettre que rien n'est détectable.

## Ce qui existe déjà

Seuls les conseils de Triple Triade sont coupés (`TriadMatchAdvisor.Competitive` : 579, 940, 941 ; DEC-033). Tout le reste de Tensho tourne en JcJ aujourd'hui : aucun code ne lit `IsPvP`.

## Où Tensho se coupe

| Niveau | Contenu | Comment on le sait |
|---|---|---|
| 1. Drapeaux du jeu | JcJ, Triple Triad Battlehall, Lord of Verminion | `IClientState.IsPvP` avec `EnterPvP` et `LeavePvP`, `TerritoryType.IsPvpZone`, `ContentFinderCondition.PvP` : les nouvelles cartes JcJ sont couvertes sans mise à jour |
| 2. Liste relevée | Tournois de Triple Triade (940, 941), Dédale bestial (1339-1343), Mahjong (831) | Pas de drapeau : liste dans le code, avec sa preuve |
| 3. À établir | Donjons profonds, courses de chocobos, pêche océanique, Restauration d'Ishgard, exploration cosmique, tout autre contenu avec un classement | Étape 1, puis décision D1 |
| 4. Hors zone | Triple Triade contre un joueur, dans une zone normale | La zone ne suffit pas : décision D5 |

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
| Alarmes et Discord | Décision D2 |
| Commandes `/tensho` | Une ligne qui dit que Tensho est coupé |
| Données | Rien n'est effacé ; pas d'écriture du fichier de réglages |

À la sortie : les fenêtres reviennent comme elles étaient, les lectures reprennent, la carte du jeu est de nouveau remplacée si le réglage le demande.

**En cas de doute, Tensho reste coupé :** zone inconnue pendant un chargement, feuille illisible après une mise à jour du jeu. Au chargement du plugin déjà en JcJ, il démarre coupé.

## Étapes

1. **Inventaire, sans le jeu.** Chercher les autres classements (fenêtres du jeu, ClientStructs, données). Lister chaque abonnement `Framework.Update`, hook, appel qui agit sur le jeu et requête réseau du plugin : 118 enregistrements dans `Plugin.cs`, 93 fichiers `.Dalamud.cs`.
2. **Décisions** D1 à D5 avec l'utilisateur.
3. **Gardien et détection.** Une règle pure et testée, branchée sur les événements du jeu. DEC mise à jour.
4. **Affichage.** Coupure à l'entrée de `Draw`, carte du jeu rendue, barre de serveur, menus et infobulles.
5. **Lectures, hooks, actions, réseau.** Chaque abonnement passe par le gardien. Un test parcourt le code et refuse tout nouvel abonnement qui n'y passe pas, comme celui des infobulles.
6. **Alarmes et Discord**, selon D2.
7. **Documents.** `PUBLISHING.md`, `CONTRIBUTING.md`, aide et prise en main ; les conseils de Triple Triade passent par la même règle.
8. **Essai en jeu**, un point à la fois : voir plus bas.

Taille : gros chantier, surtout l'étape 5. Place dans le plan global : avant la phase 5, pour que les essais en jeu se fassent déjà avec cette coupure.

## Tests

- Règle pure : chaque territoire de la preuve, un territoire inconnu, un état inconnu.
- Aucun réglage : aucune propriété de `Configuration` ni clé de langue ne permet de l'éviter.
- Code : tout abonnement, hook et appel d'action passe par le gardien.

Essais en jeu : quai de Wolves' Den ; match personnalisé de Crystalline Conflict ; Frontline ; Triple Triad Battlehall et tournoi ; Lord of Verminion ; Dédale ; Mahjong. Pour chacun : entrer avec la carte et des panneaux ouverts, tout doit disparaître et revenir à la sortie. Aussi : recharger le plugin en plein match ; se déconnecter en plein match ; vérifier `IsPvP` à 579, 506 et 589 à 591.

## Décisions à prendre

| N° | Question | Recommandé |
|---|---|---|
| D1 | Quels contenus du niveau 3 coupent tout Tensho ? | Après l'étape 1 : tout contenu dont le classement est établi. Attention, cela peut retirer des outils que vous utilisez, par exemple le panneau de pêche océanique si elle a un classement |
| D2 | Une alarme qui tombe pendant un match ? | Ni son, ni chat, ni Discord pendant le match, et pas de rattrapage à la sortie : la prochaine sonne normalement. L'alarme « mission prête » du Duty Finder avant d'entrer reste normale |
| D3 | Tensho dit-il qu'il se coupe ? | Une ligne dans le chat à l'entrée, aucune à la sortie, en anglais et français selon la langue du plugin |
| D4 | Le quai de Wolves' Den (zone sociale, `IsPvP` vrai) ? | Coupé aussi : c'est la règle la plus simple et elle ne laisse pas de cas à part |
| D5 | Triple Triade contre un joueur dans une zone normale ? | Seule la partie Triple Triade se coupe (conseils et panneau de match, comme aujourd'hui), pas tout Tensho |

Conséquence à trancher avec D1 : si tout le Dédale coupe Tensho, le panneau du Dédale prévu par DEC-032 n'a plus d'objet. Recommandé : l'abandonner ; il n'y a alors plus à contacter l'équipe Dalamud pour lui, et la page du dresseur reste une liste d'informations hors du Dédale.
