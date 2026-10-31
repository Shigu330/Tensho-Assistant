# MAP-016 — Carte Tensho : plan

2026-10-31. Plan préparé, aucune tranche validée en jeu. Demande utilisateur :
- une carte « exactement comme la carte de FF14 », avec en plus les infos de la boussole (mini-carte) ;
- les réglages permis par Mappy ;
- tout ce qui peut être trouvé de cohérent (courants éthérés, explorations…), chaque catégorie pouvant être désactivée si elle encombre ;
- en gardant l'outil Carte/Monde actuel (liste, recherche, favoris, ressources de zone, météo).

Mappy (MidoriKami) sert de référence fonctionnelle seulement. Licence AGPL : aucune reprise de code, comme pour SortaKinda.

## Tranches

1. **MAP-VIEW-016 — carte affichée.** Fenêtre « Carte Tensho » :
   - image de carte du jeu (`ui/map/…_m.tex`), zoom à la molette, déplacement à la souris ;
   - coordonnées sous le curseur ;
   - icônes fixes de la carte (`MapMarker`) ;
   - position et orientation du joueur ;
   - suit la carte courante, ou celle choisie dans Carte/Monde ;
   - clic droit : drapeau ou favori.
2. **MAP-LIVE-017 — boussole en direct** : groupe, PNJ ciblables, monstres (niveau, agressif ou non), points de récolte actifs, coffres, ALEA en cours (`IFateTable`), drapeau.
3. **MAP-DATA-018 — informations statiques**, chacune avec sa case à cocher.
4. **MAP-SETTINGS-019 — réglages façon Mappy** :
   - affichage par catégorie, taille des icônes, transparence ;
   - suivre le joueur, verrouiller la fenêtre, masquer en combat ou en instance ;
   - fondu quand on se déplace.

## Catégories prévues (source vérifiée par réflexion le 2026-10-31 · défaut)

| Catégorie | Source | Défaut |
|---|---|---|
| Icônes fixes du jeu (éthérites, boutiques, sorties…) | `MapMarker` (plage `Map.MapMarkerRange`) | activé |
| Joueur et orientation | objet joueur local | activé |
| Groupe | liste du groupe | activé |
| Drapeau | carte du jeu | activé |
| ALEA en cours | `IFateTable` | activé |
| Courants éthérés | `AetherCurrentCompFlgSet` → `AetherCurrent` ; positions par les objets `EObj` (`Data`) des décors de zone (lecteur LGB de NPC-VENDORS-014, avec contrôle d'en-tête) | activé, à valider |
| Repères d'exploration | `Adventure` (`Level`, icônes découvert/non découvert) | activé |
| Éthérites et éthernet | `Aetheryte` (`Level`, `AethernetName`) | inclus dans les icônes fixes, sinon activé |
| PNJ vendeurs | catalogue NPC-VENDORS-014 | désactivé (dense) |
| PNJ et monstres proches | table des objets | désactivé (dense) |
| Points de récolte, lieux de pêche | catalogue de récolte, `FishingSpot` | désactivé |
| Monstres sources d'un objet choisi | wiki (HOVER-TOOLTIP-013) | à la demande |
| Favoris Tensho | MapBookmarks | activé |

Toutes les catégories passent par le même mécanisme de filtres : rien n'est imposé sur la carte.

## Avancement (2026-10-31)

| Version | Contenu |
|---|---|
| 0.101 | Image de carte, zoom, icônes fixes, joueur, drapeau au clic droit (image et icônes conformes en jeu) |
| 0.102–0.103 | Noms de lieux, affichage et retrait du drapeau, cartes de région et du monde, état du vol |
| 0.104–0.105 | Menus Région / Zone / Niveau selon les règles du jeu, colonne d'outils, aile de vol (vus en jeu) |
| 0.106 | Quêtes (EventMarkers) et zones des objectifs, icônes qui suivent le zoom, premiers réglages |
| 0.107 | Coffres, récoltables (désactivés par défaut), téléportation au clic sur une éthérite, remplacement de la carte du jeu (M), réglages par sections |

Reste : groupe, courants éthérés, repères d'exploration, PNJ vendeurs, cartes non découvertes masquées comme le jeu, confirmation des zones de mandats. ALEA en cours : couche + onglet du clic droit en 0.114 (non validé en jeu) ; listes locales (quêtes par catégorie, récolte par type, éthérites) en 0.114, index de positions en cache « catalog-map-positions ».

## Reprise demandée (2026-10-31, après 0.117 — rien codé)

1. **Priorité des icônes réglable par carte** : ordre de dessin (et de survol) par type d'icône, enregistré par MapId ; repli sur l'ordre global.
2. **Réglages globaux des icônes** : taille/visibilité/priorité par défaut pour toutes les cartes (IconSizes actuel = global) + surcharge par carte (nouvelle table MapId → réglages), bouton « revenir au global ».
3. **Liens outils ↔ carte** : depuis les pages Quêtes et Récolte, bouton « voir sur la carte Tensho » (centre + drapeau, PlaceFlagFromMapView) ; depuis la carte, clic sur une quête/un point → ouvrir la fiche dans l'outil. **Nouvel outil ALEA** (page) : ALEA en cours de la zone (IFateTable, MapLiveReader.Fates), filtres niveau/état, lien carte. Rappel : aucune position fixe d'ALEA dans les données (0.114.0-local-probe.log).
Cache : réutiliser « catalog-map-positions », « quests », « catalog-gathering » ; tout nouveau réglage dans Configuration (pas de relecture Lumina).
