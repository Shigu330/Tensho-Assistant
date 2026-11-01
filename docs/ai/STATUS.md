# État courant (mis à jour 2026-11-01)

VERSION: 0.242.0.0 (dernière release : Stable-0.234.0) — Verify.ps1 (agent) 557/557, 0 avertissement, 0 erreur, langues et paquet verts. Détail par version : [CHANGELOG.md](../../CHANGELOG.md).
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

## Validé en jeu (rapport utilisateur 2026-11-01 : « tous est ok »)
- Tout ce qui était « à valider » : Mini Cactpot 0.158, Triple Triade 0.161–0.162, traduction wiki 0.150, carte Dalamud 0.156, caches et
  tri anciens (0.82–0.84), sources d'objets et liens 0.197–0.199, objet en contexte 0.200, météo et alarmes 0.201–0.205 (dont `/alarm`
  dans le jeu), accueil / horloge / Avancé / contributeurs 0.206, infobulles, menu inventaire, butin ✓/✗ et mission aléatoire 0.207–0.208,
  icônes de quêtes 0.211, Revue de mode 0.213–0.223, choix de la ressource au clic droit d'un point de récolte 0.218.
- Commit et release (`Verify.ps1 -Commit/-Release`, `commit-release.bat`) : releases Stable-0.219.0 à Stable-0.223.0 publiées.

## À valider en jeu
- 0.224 : estimation du bonus de Récolte abondante II dans l'outil PR (en mineur/botaniste, objet récolté en contexte).
- 0.225 : panneau d'aide à la récolte en jeu (mineur / botaniste séparés).
- 0.226 : panneau d'aide à la pêche (poissons du lieu, appâts, créneau, météo) ; pêche en mer non couverte (pas de source).
- 0.227 : données de pêche de GatherBuddy (Apache-2.0) en premier, Garland en secours.
- 0.228 : ouverture manuelle de l'aide à la pêche (bouton, /tensho peche) ; détection « pêche en cours » vue en jeu (capture 2026-11-01).
- 0.229 : lecture des anciens fichiers GatherBuddy (2 616 poissons) — VALIDÉ en jeu (capture 2026-11-01).
- 0.230 : page Récolte réorganisée (GatherBuddy d'abord, liens vers les modules).
- 0.231 : temps de touche (Teamcraft) et chrono de pêche — VALIDÉ en jeu (capture 2026-11-01).
- 0.232 : aide à la pêche en tableau compact.
- 0.233 : clic droit (outils utiles) sur poissons et appâts ; menu « liens » commun aligné sur le menu de l'inventaire.

## Prochaine action
REPRISE (quota semaine 97 % le 2026-11-01, arrêt après 0.237) : 1) boutiques de compagnie libre ; 2) flèches boussole sur la carte ;
3) correctifs carte (cible de quête persistante = R-01) ; 4) demandes R-02 à R-10 de docs/project/USER-REQUESTS.md (ex-ISSUES.md, réorganisé),
dans l'ordre que l'utilisateur choisira ; 5) publication D17.
FAIT 0.237 (à valider en jeu) — retour en jeu 0.236 — onglet « Coffre à apparence » de la Banque affiche
« ordre visuel indisponible. Réimportez le rangement ouvert » : la vue par onglet attend une grille (DisplayIndex) ; pour Arsenal, Coffre
à apparence et Armoire, afficher une liste (objet, quantité) au lieu de la grille (MainWindow.Bank.cs, vue des onglets de rangement).
La lecture elle-même a fonctionné (onglets créés, relevé 10:46).
ÉTAPE AJOUTÉE (demande 2026-11-01, après le plan prévu : correctif Banque, boutiques de compagnie libre) : sur la carte Tensho, quand une
quête active ou le drapeau est hors de la vue, une flèche de direction posée sur le bord de la carte, orientée vers la cible (style boussole
de FF14 : icône de la quête ou du drapeau, distance éventuelle) ; clic = recentrer sur la cible.
DERNIERS CORRECTIFS DE CARTE (demande 2026-11-01, avant D17) : une quête ciblée (« cible » de la carte, mapGameTarget) puis accomplie garde
son icône de cible sur la carte et la minicarte : effacer la cible quand la quête n'est plus active ou que son étape change (relire l'état
des quêtes / marqueurs à chaque relevé) ; relever les autres défauts de carte signalés à ce moment. Capture utilisateur : l'icône « Cible indiquée
par le jeu (journal) » (quête « Kuiyki évolue ») reste à vie et ne peut pas être retirée : ajouter aussi un retrait manuel (clic droit sur
l'icône ou bouton « Retirer la cible ») et la retirer quand le journal du jeu n'indique plus de cible. Puis publication D17.
REPRISE — 0.235 arsenal et 0.236 coffre à apparence + armoire FAITS (lecture du jeu, sans autre plugin ; à valider en jeu). Suite du plan :
point 3 boutiques de compagnie libre, puis D17. Point 2 élargi par l'utilisateur : coffre à apparence + armoire +
arsenal, le tout relié au module Banque. API vérifiées (FFXIVClientStructs, lecture seule, réflexion 2026-11-01) :
- arsenal : InventoryType ArmoryMainHand/OffHand/Head/Body/Hands/Waist/Legs/Feets/Ear/Neck/Wrist/Rings/SoulCrystal — non lus aujourd'hui
  par Inventory/ (aucune occurrence « Armory ») ;
- coffre à apparence : MirageManager.Instance() → PrismBoxLoaded, PrismBoxItemIds (rempli seulement après ouverture du coffre en jeu) ;
- armoire : UIState.Cabinet → IsCabinetLoaded(), IsItemInCabinet(cabinetRowId) (lien objet → ligne de la feuille Cabinet à sonder).
Plan : lecture sur le thread framework (copie), conteneurs « Arsenal », « Coffre à apparence », « Armoire » ajoutés aux entrées de la
Banque (BankEntry/InventorySnapshot, Inventory/BankModels.cs) avec date de relevé ; « possédé » de la Revue de mode et stocks partout
les incluent ; inconnu tant que le coffre / l'armoire n'ont pas été ouverts.
PLAN AVANT PUBLICATION (décidé 2026-11-01, détail : docs/project/BACKLOG.md « Revue des points ouverts ») : 1) FAIT 0.224 (à valider en jeu) : bonus d'Abondance estimé
(règle communautaire citée, modifiable) ; 2) Revue de mode : coffre à apparence + armoire dans « possédé » ; 3) boutiques de compagnie libre
(FccShop) ; 4) icônes de quête seulement sur relevé en jeu ; puis publication D17. Non faits par décision : score de la revue, suppression
d'alarme du jeu, sources inconnues restantes, injection dans l'infobulle native.
0a. FAIT et validé en jeu (2026-11-01) : 0.197–0.223. Dépôt perso : https://raw.githubusercontent.com/Shigu330/Tensho-Assistant/main/repo.json
0. FAIT et validé en jeu (0.191–0.196 : copie, carnet, prévision, Macro Chain) — PRIORITÉ utilisateur (2026-10-31 18:47) : copier les macros de l'outil de génération de macros (CRAFT-MACRO-001, `Craft/CraftMacroExport.cs`)
   directement dans les macros du jeu, avec réglages : numéro de macro de départ, limite (dernier emplacement utilisable), individuel/partagé ;
   référence fonctionnelle Craftimizer (lecture seule, pas de copie de code). Écriture dans RaptureMacroModule = action sur clic du joueur ;
   à prouver par capture/sonde avant usage.
1. Essai en jeu 0.188 (butin des contenus récents via LuminaSupplemental, sources d'objets).
2. LuminaSupplemental, reste : ItemSupplement (désynthèse, coffres, réduction…) dans « Comment l'obtenir », (Butin des monstres : MobDrop en tête depuis 0.190). Fait en 0.189 : monstres, ALEA, entreprises, boutique dans l'infobulle.
3. Commit (GitHub Desktop). Publication D17 côté auteur.

## Blocages connus
- Bonus d'Abondance : règle communautaire trouvée (frozen_rabbit_tome), à appliquer comme estimation citée (plan avant publication).
- Repères d'exploration : clos, météo du wiki citée (affichée et utilisée par les alarmes depuis 0.201).
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
