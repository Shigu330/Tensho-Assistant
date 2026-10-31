# État courant (mis à jour 2026-10-31)

VERSION: 0.218.0.0 — Verify.ps1 (agent) 548/548, 0 avertissement, 0 erreur, langues et paquet verts. Détail par version : [CHANGELOG.md](../../CHANGELOG.md).
GOAL: COLL-004 — réduire les sources « inconnues » des objets (choix utilisateur 2026-10-31) ; 0.177 lignes parasites du wiki retirées.
QUOTA: réserve utilisateur 1 % (5 h et semaine). Relevé 2026-10-31 : 5 h à 93 % (réinit. 22:10), semaine 66 % — arrêt après 0.190 (5 h à 95 %). Dernier relevé connu : 5 h remis à 0 vers 12:10, semaine 41 % (réinit. jeudi 23:00).

## Validé en jeu (rapports utilisateur, 2026-10-30/31)
- Carte Tensho : image, icônes fixes, menus Région/Zone, colonne d'outils, M remplace la carte du jeu, drapeau (pose, centrage, sans fermeture),
  téléportation avec coût réel, quête du journal centrée avec icône de la cible, liste des icônes survolées (comme Mappy), priorités,
  points de récolte (minage, botanique, pêche) avec horaires, courants éthérés obtenus masqués, vendeurs et infobulles, noms de mandats.
- Carte 0.151–0.155 (rapport utilisateur 2026-10-31, « carte et rowena ok ») : carte du jeu hôte hors écran, zones par type et couleur,
  icônes des onglets du clic droit, zoom, Paramètres › Carte, icônes des commises de Rowena sur la carte et dans l'onglet Vendeurs.
- Collections : état obtenu/non obtenu des montures et mascottes, sources (vendeurs, échanges, Garland en dernier recours, wiki).
- Antérieur : tri natif, caches objets, PNJ vendeurs, survol des objets (accroche, stock, prix, monstres), thème.

- Icône 0.157 dans la liste des plugins de Dalamud (rapport utilisateur 2026-10-31 ; visible après relance du jeu).

- Triple Triade 0.160 : conseil en match (rapport utilisateur 2026-10-31, « test ok »).

- Carte : marchands des variantes de carte d'une même zone (Auxesia, 0.171 + 0.175), capture utilisateur 2026-10-31.

## À valider en jeu
- 0.158 Mini Cactpot : cadre vert sur les meilleures cases puis lignes, sur la fenêtre du jeu (sens des diagonales à confirmer).
- 0.161 Triple Triade : panneau à côté de la fenêtre de défi du PNJ (% de victoire par deck, génération de deck).
- 0.162 decks Tensho par PNJ (liste, suppression) et aide-mémoire des cartes à côté de l'éditeur de decks du jeu.
- 0.150 traduction des lignes wiki (hors carte, non confirmée explicitement).
- 0.156 carte Tensho dans le système de fenêtres Dalamud : ouverture (M, journal), fermeture (croix, M), agrandir/réduire, opacité,
  verrouillage, masquage en combat ; manifeste anglais dans l'installateur de plugins.
- Repères d'exploration : en pause (positions vérifiées contre le carnet publié ; météo via wiki).
- Antérieur non confirmé : caches recettes et récolte (0.82–0.83), critères de tri (0.84).

## Prochaine action
0a. À VALIDER EN JEU (0.197–0.199 : infobulle sur toutes les icônes de carte ; clic droit « Échange » → vendeur, PNJ LuminaSupplemental ; 0.200 icônes d'outils de « Objet en contexte » ; 0.201 météo + alarmes : cloche récolte/pêche/météo/repères, son et message à l'heure, rappel modifiable ; 0.202 module Alarmes groupé par objet ; 0.203–0.204 alarmes séparées par métier ; 0.205 création dans le jeu par /alarm (réglage ou « Envoyer au jeu ») à valider ; 0.206 accueil, horloge unique, onglet Avancé, aide contributeurs ; 0.207 infobulles, menu inventaire, butin ✓/✗ et filtres, mission aléatoire ; 0.208 menu inventaire = outils utiles de l'objet en contexte ; 0.209 usages VALIDÉ (« c'est ok ») ; 0.210 couches Tensho VALIDÉ ; 0.211 icônes de quêtes (EventIconType) dans les réglages ; 0.212 VALIDÉ ; 0.213–0.214 Revue de mode façon Avant-Garde (catégories lues : affichées en jeu d'après la capture ; infobulle non grisée à valider). 0.215 teintures. REPRISE (quota 94 %+ le 2026-11-01) : valider 0.214–0.216 en jeu (infobulle, teintures, liste en accordéon) ; PROCHAINE ACTION (demandée, quota 97 % le 2026-11-01) : boutons Tensho posés sur les icônes d'emplacement de la fenêtre « Revue de mode » du jeu (nœud de l'emplacement n = GetNodeById(8 + n), position × échelle) qui ouvrent la liste de cet emplacement ; valider 0.214–0.218 (0.218 : choix de la ressource au clic droit d'un point de récolte)) : infobulle « Comment l'obtenir » d'un cristal, d'une carte de Triple Triad (lot de cartes), d'un objet de Bozja/Eureka ; clic droit → objet source.
0. FAIT et validé en jeu (0.191–0.196 : copie, carnet, prévision, Macro Chain) — PRIORITÉ utilisateur (2026-10-31 18:47) : copier les macros de l'outil de génération de macros (CRAFT-MACRO-001, `Craft/CraftMacroExport.cs`)
   directement dans les macros du jeu, avec réglages : numéro de macro de départ, limite (dernier emplacement utilisable), individuel/partagé ;
   référence fonctionnelle Craftimizer (lecture seule, pas de copie de code). Écriture dans RaptureMacroModule = action sur clic du joueur ;
   à prouver par capture/sonde avant usage.
1. Essai en jeu 0.188 (butin des contenus récents via LuminaSupplemental, sources d'objets).
2. LuminaSupplemental, reste : ItemSupplement (désynthèse, coffres, réduction…) dans « Comment l'obtenir », (Butin des monstres : MobDrop en tête depuis 0.190). Fait en 0.189 : monstres, ALEA, entreprises, boutique dans l'infobulle.
3. Commit (GitHub Desktop). Publication D17 côté auteur.

## Blocages connus
- Bonus natif d'Abondance inconnu (sources divergentes) : ne pas inventer de formule.
- Repères d'exploration : la météo exigée n'est pas dans les données du jeu (source wiki).
- Plantage 08:19 (2026-10-31) causé par la lecture d'une infobulle de marqueur : lecture bornée depuis 0.133 ; surveiller ([extrait](../evidence/0.133.0-crash-excerpt.log)).

## Couverture des 17 plugins de référence
| Plugin | Coverage |
| Avant-Garde | PARTIAL — répertoire manuel d'indices natifs, équipements signalés, emplacements/stock/contexte et source datée ; thème courant/teintures/score/attache non déduits |
| Blue Mage Helper | PARTIAL — grimoire, numéros, descriptions, lieux renseignés et acquisition confirmée |
| Character Panel Refined | PARTIAL — attributs natifs, sans formules dérivées |
| Collections | PARTIAL — montures, mascottes, emotes, cartes, orchestrion, titres et succès ; preuve de chargement, catégories/fiches/récompenses, runtime à confirmer |
| Craftimizer | PARTIAL — carnet natif, collectionnables ordinaires, qualité initiale et recherche orientée attentes ; optimum global, rotation adaptative et recettes spéciales à compléter |
| Duty Loot Preview | PARTIAL — consultation manuelle par contenu, coffres/récompenses, icônes/contexte, provenance et cache ; drops directs non interprétés, données externes partielles |
| ezMiniCactpot | PARTIAL — solveur exact manuel et mode observé opt-in LotteryDaily ; conseil actualisé, aucune commande ; adaptation/rendu natifs non validés en jeu |
| GatherBuddy | PARTIAL — sources/maps/horaires/météo statiques, point courant MIN/BTN opt-in, carnet natif pêche, appâts/conditions explicites et plages ET/UTC et météo localisée ; plages des critères renseignés, présence/prérequis/harpon et runtime à compléter |
| Item Use | PARTIAL — recettes, mandats, candidats GC/aquarium, orchestrion et sources de quête, survol et menu Inventory opt-in ; autres usages/disponibilité, liens de discussion et runtime à compléter |
| LazyGatherer | PARTIAL — optimiseur manuel avec effets explicitement déclarés, observation du point et copie de ressources ; décodeur natif/bonus/régénération/procs/collectionnables et validation runtime à compléter |
| Macro Chain | PARTIAL — séquences à marqueur final, lancement explicite, arrêt, persistance ; natif à confirmer |
| Mappy | PARTIAL — navigateur natif, repères favoris persistants, ressources de zone et prévisions météo statiques ; météo forcée non observée |
| Monster Loot Hunter | PARTIAL — associations matériau/monstre, fiches/niveaux/lieux et carte de zone, transport/cadence partagés, contexte ; données communautaires partielles |
| Price Insight | PARTIAL — consultation manuelle et prix au survol opt-in, NQ/HQ/monde, cache/cadence/priorités partagés ; contexte mutualisé et scope owner/monde, runtime à confirmer |
| QuestTracker | PARTIAL — états natifs, séquence, fiches/liens et récompenses conditionnelles vers Item ; sources inverses managées, couverture/inconnus et disponibilité non calculée |
| SortaKinda | PARTIAL — profils/modèles, critères, zones protégées/filtrées et inversion, application visuelle après capture directe ; autres filtres/automatisations choisies et validation runtime à compléter |
| TriadBuddy | PARTIAL — carnet/deck/solveur manuel, observation des faces opt-in expérimentale ; identités/règles/trait natifs et autres règles non établis |
