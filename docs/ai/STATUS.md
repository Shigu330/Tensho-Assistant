# État courant (mis à jour 2026-10-31)

VERSION: 0.176.0.0 — Verify.ps1 (agent) 544/544, 0 avertissement, 0 erreur, langues et paquet verts. Détail par version : [CHANGELOG.md](../../CHANGELOG.md).
GOAL: [TRIAD-003](GOAL.md) — Triple Triade comme TriadBuddy : conseil en match validé (0.160) ; % de victoire par deck et génération de deck (0.161) à valider.
QUOTA: réserve utilisateur 1 % (5 h et semaine). Relevé 2026-10-31 ~17:20 : 5 h à 15 % (réinit. 22:10), semaine 56 %. Dernier relevé connu : 5 h remis à 0 vers 12:10, semaine 41 % (réinit. jeudi 23:00).

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
1. FAIT (2026-10-31) : capture de l'éditeur de decks, 44 lignes, [preuve](../evidence/0.163.0-triad-editdeck-capture.log). Constat : chaque carte
   de la liste du jeu est un nœud image (id 8, 40×40 px) dont la texture est `ui/icon/088000/088NNN_hr1.tex`, soit l'icône 88000 + id de
   carte (= TriadCardDefinition.IconId) ; cadre 48×48 (id 6) autour ; rectangles écran lus.
2. FAIT en 0.164 (à valider en jeu) : avec cette preuve, encadrer dans la liste du jeu les cartes du deck Tensho choisi (TRIAD-003) ; retirer ensuite la capture.
3. À valider en jeu : 0.164 (surbrillance dans l'éditeur), 0.165–0.166 (% dans « Choix du jeu »), 0.166 titres/succès conservés (réglage Données), 0.167 servants et sacoches enregistrés à l'ouverture, 0.168 Triple Triade › PNJ et cartes (PNJ battus, lots prioritaires).
4. Tests restants : 0.161–0.162 (panneau de défi, liste de decks), Mini Cactpot 0.158.

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
