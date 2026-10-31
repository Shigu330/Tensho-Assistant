# HOVER-TOOLTIP-013 — Panneau de survol accroché à l'infobulle du jeu

0.85.0.0 — 2026-10-30 — IMPLEMENTED / COMPILED / TESTED (Verify.ps1 exécuté par l'agent sur le PC, 20:00 : 500/500, 0 avertissement/erreur, langues et paquet verts). VALIDATED_IN_GAME: NO.

Demande utilisateur du 2026-10-30, avec capture : « revoir le survol, ça doit être à la suite du tooltip existant ; mettre l'objet le moins cher du centre de données (on peut y aller) ; ajouter les infos pertinentes : usages, stock… ».

## Constat avant changement

Le panneau était une fenêtre ImGui posée à la souris (+24 px), indépendante de l'infobulle native et pouvant la recouvrir. Seul le prix du monde actuel était lu dans la réponse Universalis agrégée. Les lignes « 0 recette / 0 mandat » et une note de provenance générique prenaient de la place. Un prix manquant s'affichait « Non renseigné gils ».

## Comportement 0.85

- **Accroche** : à chaque tick framework où un objet est survolé, `ItemHoverReader` copie le rectangle de l'addon `ItemDetail` visible (`IGameGui.GetAddonByName`, `Position`, `ScaledSize` d'`AtkUnitBasePtr` : API managée, aucun pointeur conservé). Le panneau prend la largeur de l'infobulle (min. 260 px) et se place juste en dessous ; à défaut de place, au-dessus, puis à droite, puis à gauche. Sans infobulle visible : position à la souris, comme avant. Sous l'infobulle, l'icône et le nom (déjà affichés par le jeu) ne sont plus répétés.
- **Ce n'est pas une injection dans l'infobulle native** : le texte n'est pas ajouté aux nœuds du jeu. Une vraie insertion (modification d'AtkTextNode, redimensionnement) serait une tranche HEAVY à part.
- **Stock** : `ItemHoldings` (sacs observés, sacoches, servants importés, cristaux compris), total NQ/HQ, au plus 4 rangements, « au moins » si un rangement n'est pas observé. Calcul mis en cache par objet et par référence d'état.
- **Obtention** : fabricable (nombre de recettes et métiers), récolte/pêche (première source et nombre de sources supplémentaires), récompense de quêtes (index existant). Monstres : voir la section 0.86 ci-dessous ; les contenus (donjons) ne sont pas interrogés au survol.
- **Usages** : seules les lignes non nulles (ingrédient de recettes, mandats, livraison GC, aquarium).
- **Prix** : monde actuel (NQ, HQ seulement si l'objet existe en HQ, « aucune annonce » sinon) ; **le moins cher du centre de données** (`minListing.dc` de la même réponse agrégée, sans requête en plus) avec le nom du monde, ou « ce monde ». La date de relevé de ce monde vient de `worldUploadTimes` ; elle est signalée si elle est ancienne. Un minimum sans `worldId` n'est jamais attribué au monde actuel.
- **Noms des mondes** : `MarketWorldReader` copie une fois par changement de monde le nom du centre de données et des mondes publics de ce centre (feuille World : `Name`, `DataCenter`, `IsPublic`, vérifiés localement par réflexion).
- **Cache du marché** : version disque `universalis-aggregated-v2`. Les prix enregistrés en v1 sont ignorés une fois, puis consultés à nouveau.

## Monstres au survol (0.86.0.0)

Demande utilisateur 2026-10-30 : afficher les monstres s'ils sont en cache ; sinon, après 1 à 2 s sur l'objet, lancer la recherche. `MonsterHoverSources` (Monsters/) :
- lit immédiatement la copie disque Garland (`sources`, `Item:{id}`, même clé que la fenêtre Matériaux des monstres) via `GarlandClient.ReadCachedAsync`, quel que soit son âge ;
- copie fraîche (durée « Sources » des Réglages) : affichée telle quelle. Copie expirée : affichée avec la mention « copie ancienne », puis actualisée ;
- sans copie ou copie expirée : une requête `item/fr/3` seulement après **1,5 s** de survol stable du même objet ; une seule à la fois ; cadence Garland partagée de 3 s ; réponse analysée hors du thread de rendu, puis écrite dans le cache disque partagé ;
- échec réseau : rien n'est inventé, nouvelle tentative pour cet objet après 1 min (3 s si la cadence partagée a refusé) ; copie illisible = absente ;
- 128 objets au plus en mémoire.

Affichage dans « Obtention » : « Lâché par : Coeurl (niv. 50) · … (+N) », ou l'état de la recherche. Option des Réglages « Chercher les monstres sur Garland Tools au survol », **activée par défaut** à la demande de l'utilisateur et active seulement avec l'affichage des usages ; décochée, seul le cache est utilisé. Verify.ps1 (agent, 20:07) : 504/504, 0 avertissement/erreur, DLL 0.86.0.0. `MonsterHoverSourcesTests` (4 groupes, faux serveur HTTP et vrai DataCache).

## Retour en jeu 0.86 et wiki comme source principale (0.87.0.0)

Capture utilisateur 2026-10-30 (Épaule de rroneek, Moogle/Chaos) : panneau collé sous l'infobulle native à la même largeur, stock « 222 possédés · Sacs du personnage 222 », usages non nuls, prix Moogle et « Le moins cher · Chaos … ce monde » affichés → accroche, stock et minimum du centre de données VALIDATED_IN_GAME (rapport + capture). Problèmes signalés : la recherche partait pour des objets sans lien avec les monstres ; Garland ne fournit aucun monstre pour cet objet alors que le wiki donne « Rroneek · 96 · Shaaloani (21.8,10.2) ».

Changements 0.87 :
- `ItemDefinition.EnglishName` copié depuis la feuille Item anglaise du client (même passe que les bonus d'outils ; invalide une fois le cache des objets). Le wiki n'appelle plus Garland pour résoudre le titre.
- `WikiMonsterSources` : `ReadCachedAsync` (mémoire/disque, sans réseau), `FetchAsync` (API MediaWiki `parse`, `redirects=1`, cadence partagée de 3 s), copie sélectionnée affichée dès la sélection. Redirection acceptée seulement si `from` = titre demandé ; page absente (`missingtitle`) = document vide mis en cache, pas une erreur. Clé disque `wiki-monsters`/`wiki-parse-v1` inchangée (copies existantes réutilisées).
- Survol : `MonsterHoverSources` passe au wiki. Seuls les **candidats** sont cherchés (`MonsterDropCandidates`) : catégories ItemUICategory 45 Ingrédient, 48 Pierre, 49 Métal, 50 Bois, 51 Tissu, 52 Cuir, 53 Os, 54 Réactif (identifiants vérifiés dans la copie locale du catalogue, 50 880 objets, le 2026-10-30), non équipables, non fabricables, non récoltables, nom anglais connu. Ligne affichée : premier monstre avec niveau et lieu, puis « (+N) ».
- Fenêtre Matériaux des monstres : section wiki en premier, ouverte par défaut ; Garland replié en « Complément » (fiches, positions, lien carte). Garland n'est pas supprimé : il reste la seule source d'identifiants de monstres pour ouvrir la carte et de la liste des autres butins.

Verify.ps1 (agent, 20:16) : 505/505, 0 avertissement/erreur, DLL 0.87.0.0. Tests : `MonsterHoverSourcesTests` réécrits pour le wiki (4 groupes), `WikiMonsterTests` (titre issu du jeu, redirections, page absente, candidats). VALIDATED_IN_GAME: NO pour 0.87.

## Tests

`MarketHoverDetailsTests` (4 groupes) : minimum du centre de données avec monde et date propres ; pas d'attribution sans monde ou sans prix positif ; noms des mondes copiés et effacés à la déconnexion ; accroche stockée, refusée si invalide, abandonnée avec l'objet. `CacheTests` passe à la clé v2. Placement et rendu ImGui non couverts.

## Protocole en jeu (DLL 0.85.0.0)

1. Survoler un objet de l'inventaire : le panneau apparaît collé sous l'infobulle du jeu, à la même largeur.
2. Objet près du bas de l'écran : le panneau passe au-dessus ou à côté.
3. Objet vendable : ligne « Le moins cher · Chaos » avec le monde, ou « ce monde ».
4. Stock affiché (sacs, servants importés) ; obtention et usages seulement s'ils existent.
