# Vérifications

## Comment vérifier — relu le 2026-11-04 (0.512)

### Tests automatiques

```powershell
Set-Location C:\Plugins-ff14
.\scripts\Verify.ps1
```

`Verify.ps1` enchaîne : build Release de la solution (les avertissements s'affichent dans la sortie), exécutable de tests
`tests/TenshoAssistant.Tests` (`dotnet test` seul ne l'exécute pas), validation des langues (métadonnées, clés en double, jetons de format
compatibles ; traductions partielles admises), contrôle du paquet `latest.zip` (identité, API 15, version du manifeste = `<Version>` du
csproj = DLL ; une seule entrée par fichier ; SHA-256 de la DLL et du manifeste identiques aux fichiers produits ; aucun fichier de langue
livré à côté de la DLL depuis 0.248 ; aucune assembly de l'hôte redistribuée). Attendu : tout vert, 0 avertissement, puis la ligne
`VERSION x.y.z (DLL x.y.z.0)`.

- Même version et mêmes sources (hors `*.md`, `docs/`, `repo.json`) déjà passées au vert : les contrôles sont sautés (« SKIP checks »,
  `artifacts/verify-stamp.json`) ; `-Force` les relance.
- Exécutable seul, après un build : `dotnet run --project tests/TenshoAssistant.Tests -c Release --no-build`, **depuis la racine du
  dépôt**, avec le SDK que choisit Verify.ps1 (le plus récent `%USERPROFILE%\.codex\dotnet-10.*\dotnet.exe` s'il existe, sinon
  `C:\ia-code\.tools\dotnet10\dotnet.exe`, sinon `dotnet`). Le test des infobulles de réglages lit `src/TenshoAssistant` et ses langues ; ceux de la compagnie libre lisent
  `src/TenshoAssistant/FreeCompany/Data/`.
- Dernier passage : voir [STATUS](../ai/STATUS.md), première ligne. En CI, à chaque push : `Build and test` (dont une compilation à la manière du dépôt Dalamud, [relevé](../evidence/0.512-d17-build.md)) et CodeQL.

Les tests couvrent la logique pure : 87 fichiers `*Tests.cs` (un par domaine : inventaire, banque et tri, artisanat et macros, récolte et
pêche, météo et alarmes, carte et projection, marchands PNJ, collections, quêtes, sources Garland / wiki / LuminaSupplemental, cache
disque, langues, Mini Cactpot, Triple Triade…) et des groupes écrits directement dans `Program.cs` (marché, rappels, compagnie libre,
équipement, quêtes, infobulles des réglages, score des cartes). Tous sont enregistrés dans `Program.cs` (contrôlé le 2026-11-02).
Ils ne couvrent pas : lecture de la mémoire du jeu (sacs, atelier de la compagnie, barre du serveur, cabine d'essayage…), feuilles Excel
réelles (vérifiées par les sondes de `tools/`, sorties dans [docs/evidence/](../evidence/)), rendu ImGui et clics, requêtes HTTP réelles
(Universalis, Garland, wiki, GitHub, Teamcraft : transport simulé ou extraits enregistrés).

### Inventaire des tests (569 au 0.315.0 ; liste non reprise depuis, le nombre courant est dans STATUS)

Sortie : une ligne `PASS <nom>` (ou `FAIL <nom>: …`) par test, puis `N/N passed`. Depuis le dernier inventaire de cette page (0.154.0,
532 tests), **37 tests ajoutés**, aucun retiré : sortie actuelle comparée aux sources de `checkpoints/TenshoAssistant-0.153.0-source.zip`
(dossier local). Version = première version du CHANGELOG à ce nombre de tests, ou version citée par le CHANGELOG.

**0.232 → 0.315 : 12 tests, écrits dans `Program.cs`**

| Version | Domaine | Nom (sortie `PASS`) |
|---|---|---|
| 0.270 | Marché : annonces et ventes | Market board parses listings and sales, names the world of a world scope and keeps the tax |
| 0.270 | Marché : filtres | Market filter applies quality, quantity, price and world bounds |
| 0.270 | Marché : coût pour N unités | Cheapest plan buys whole listings, may skip the lowest unit price and says when units are missing |
| 0.277 | Triple Triade : score (cartes Ex. ajoutées en 0.278) | Triad card score follows the stated formula for each rule |
| 0.285 | Quêtes : cités de départ | Starting-city quests are those whose chain leads back to one city only; the played city is the one with a completed quest |
| 0.286 | Quêtes : options non prises | Options not taken (locks, branches) are unplayable, with propagation, and completed quests never are |
| 0.287 | Infobulles des réglages (en / fr) | Every setting tooltip named in the code exists in English and French (R-06 step 5) |
| 0.300 | Rappels : remises à zéro | Reminder resets follow the game's calendar times in UTC |
| 0.307 | Compagnie libre : expéditions | Free company voyages: state, experience, merge by house and change detection (R-07) |
| 0.309 | Compagnie libre : trajets | Submarine route planner: distance, time, best order, range and loot value (R-07) |
| 0.310 | Équipement (complété 0.311 mirage, 0.312 matérias et plafonds) | Gear planner: level window, candidates, totals, planned stats and build codes (R-11) |
| 0.315 | Compagnie libre : montages | Vessel builds: stats, capacity, ranks, breakpoints, wear, build names and best builds (R-11 step 3) |

**0.155 → 0.231 : 25 tests, dans les fichiers de domaine**

- `TriadMatchAdvisorTests` (0.160) :
  - Triad advice fills hidden NPC cards from the deck and plays the card imposed by Chaos
  - Triad advice maps rules and refuses unclear matches instead of guessing
  - Triad frame keeps match context copies and drops incomplete rectangles
- `TriadMatchSimulatorTests` :
  - Triad simulation is reproducible and ranks a strong deck above a weak one
  - Triad deck generator respects rarity limits and does not do worse than its start
- `TriadNpcCatalogTests` : Triad NPCs to face: not beaten or with missing cards first, unknown counts as not beaten
- `TriadCardTests` (+1) : Triad NPC decks persist with their NPC name and older configuration has none
- `MiniCactpotObservationTests` (+1) : Cactpot overlay keeps button rectangles only when complete and maps solver lines to game buttons
- `CompletionMemoryTests` :
  - Titles and achievements keep obtained states and keep not-obtained only while fresh
  - Retainers and saddlebags are saved automatically unless turned off, older configurations included
- `StatFormulaTests` : Combat stats give real values with the game rounding and only the stats of the job
- `MapProjectionTests` (+2) :
  - Map variants of one territory with the same scale and offset share positions
  - Vendor joins the game icon it stands by: shops at 16 pixels, unlabelled services at 8
- `DutyLootTests` (+1, 0.187) : Duty loot keeps the boss coffer of a real Garland raid document (A4S, 2026-10-31)
- `LuminaSupplementalTests` :
  - LuminaSupplemental gives boss coffers, boss drops and chests of a duty by its ContentFinderCondition id
  - LuminaSupplemental item uses: what an item gives, the quests asking for it, and the NPCs of unlinked shops (0.209)
  - GatherBuddy fish data: bait, mooch route, bite, ET window, weathers, lure and predators read from its source files (0.227 ; ancien
    format ajouté en 0.229)
  - Teamcraft bite times: range of the well reported seconds, usual second, few stray reports ignored (0.231)
- `MacroSlotPlanTests` (0.191) : Crafting macros only go into the chosen range, into empty or Tensho slots unless overwriting is allowed
- `AlarmScheduleTests` (à partir de 0.201) :
  - Daily ET alarms recur every 70 real minutes at the ET time and ring once per occurrence within their lead
  - Alarms with weather conditions skip occurrences without that weather and treat an unknown forecast as no match
  - Window alarms are one per ET start, single alarms expire, and the list is bounded
  - Game alarm commands use the game's /alarm syntax with an ET daily time, a 0-60 reminder, a sound and a 20-character name
  - Fashion Report weeks start on Tuesday 08:00 UTC and the slots follow the game window
  - Bountiful II estimate: +3 from 110 % of the base, +2 from 90 %, else +1, unknown without rating or base (0.224)

Tests existants mis à jour sans changer le nombre : 0.289 (nouvelles valeurs par défaut de la carte : suivi du zoom 15 %, priorités).

### Essai en jeu

1. Charger `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll` dans les plugins de développement Dalamud ;
   vérifier la version affichée dans la fenêtre (`/tensho`).
2. Suivre la liste ci-dessous, ou le protocole de la version dans [CHANGELOG.md](../../CHANGELOG.md) / le document d'objectif
   (`docs/goals/<ID>.md`).
3. Rapporter : version, ce qui a été observé, captures si possible, et `dalamud.log` / `dalamud.crashhandler.log` en cas d'erreur.
   Seul ce rapport permet d'écrire « validé en jeu » ; la liste à jour est dans [STATUS](../ai/STATUS.md).

Contrôles de base à refaire après un changement de cycle de vie : rechargement/déchargement répété sans double callback ni commande
orpheline ; préférence « ouvrir au chargement » respectée ; la carte du jeu (touche M) revient normale une fois le plugin déchargé.

### Essais en jeu à faire (liste au 2026-11-02, DLL 0.315.0.0)

Non validé en jeu d'après [STATUS](../ai/STATUS.md) : 0.232–0.302, 0.307–0.309, 0.315. Dans cette plage, déjà rapporté OK par
l'utilisateur le 2026-11-02 (ne pas refaire) : 0.265–0.271, 0.276–0.279, 0.283–0.289, 0.290–0.292 (légèreté seulement), 0.293–0.299 ;
hors plage : 0.303–0.306, 0.310–0.314. Une étape impossible (pas de compagnie, pas de sous-marin…) se rapporte « non essayé », pas « OK ».
0.234 ne change que l'outil de release : rien à essayer en jeu.

**Pêche (0.232–0.233, 0.252–0.254)**

- [x] 0.232 — Aide à la pêche (`/tensho peche` ou en pêchant) : une ligne par poisson (stock, ✓/✗), route d'appâts en icônes, touche
  « ! / !! / !!! » en couleur avec le ferrage abrégé, temps de touche « 9–16 s (12) » en vert quand le chrono est dans l'intervalle ;
  colonne Conditions seulement si un poisson en a ; textes complets en infobulle ; « Sources (i) » ; fenêtre à la largeur du contenu.
- [x] 0.233 — Clic droit sur un poisson et sur un appât de l'aide à la pêche, puis sur un appât de « Comment le pêcher » (page
  Récolte) : menu des outils de l'objet (usages, artisanat, marché, butin…), puis ses sources.
- [x] 0.252 — Clic droit sur un appât : un seul menu (plus deux superposés). Panneau d'aide à la récolte en jeu : le nom de l'objet a
  l'infobulle complète et le clic droit.
- [x] 0.253–0.254 — Récolte, poisson à créneau ET et météo : bouton alarme présent sur la ligne de pêche ; l'alarme porte la plage ET,
  la météo et la météo précédente, comme celle de « Comment le pêcher » ; poisson à météo sans plage horaire : une alarme par période
  météo (00, 08, 16 ET). Mineur / botaniste : alarme sur l'horaire seul.

**Rangements et Banque (0.235–0.240, 0.280)**

- [ ] 0.235–0.237 — Capture automatique active (Paramètres › Données), attendre au plus 2 min : Rangements › Banque montre « Arsenal ».
  Ouvrir une fois le coffre à apparence et l'armoire (auberge) : leurs sections apparaissent (avant : « Ouvrez une fois… »). Les trois
  s'affichent en pages de 35 cases, sans « ordre visuel indisponible ».
- [ ] 0.238–0.239 — La Banque se charge (pas de « Les relevés de stocks ne sont pas disponibles ») ; servants et sacoches toujours là.
- [x] 0.240 — Tenue complète rangée au coffre : ses pièces comptent comme possédées (stocks, « possédé » de la Revue de mode).
- [ ] 0.280 — Barre latérale › OBJETS : « Objets » (recherche + fiche) et « Rangements » (onglets Banque, Cristaux, Imports) ; le lien
  « Banque d'objets » de l'accueil ouvre l'onglet Banque.

**Collections ✓/✗ (0.241, 0.274–0.275)**

- [x] 0.241 — Butin des contenus : cartes de Triple Triade et rouleaux d'orchestrion avec ✓/✗ ; « Non obtenus seulement » les garde
  quand ils ne sont pas obtenus.
- [x] 0.274 — Collections › orchestrion : clic droit sur une ligne débloquée par un seul objet = même menu que l'objet (outils + sources).
- [x] 0.275 — Pastille ✓/✗ sur toute icône d'objet de collection (fiche objet, sources, butins, Revue de mode, quêtes, marchands,
  alarmes) ; collections pas encore lues : aucune pastille (jamais ✗ par défaut).

**Carte (0.242–0.245, 0.259, 0.262, 0.308)**

- [x] 0.242–0.243 — Quête dont le journal indique une cible : icône « Cible indiquée par le jeu (journal) » ; clic droit sur la carte ›
  « Retirer la cible indiquée par le jeu (journal) » la retire ; quête accomplie ou abandonnée : l'icône disparaît seule (environ 1 min).
- [x] 0.244 — Clic droit sur la carte › onglet « Triple Triad » : PNJ de la carte avec [x] / [ ] / [?], cartes manquantes, coordonnées ;
  clic = drapeau, clic droit = outil Triple Triade, survol = prix d'entrée et cartes de récompense.
- [x] 0.245 — Clic droit sur une icône : son menu (nom, « Placer le drapeau », « Se téléporter ici » sur une éthérite, « Ouvrir cette
  carte » sur un lien de carte, ressources d'un point de récolte, « Menu général de la carte… ») ; ailleurs, le menu général.
- [ ] 0.259 / 0.262 — Paramètres de la carte : chaque réglage a son infobulle ; un curseur ou une case changé fait apparaître ↺, qui
  remet la valeur par défaut.
- [ ] 0.308 — Option « Menus et outils par-dessus la carte » : le niveau de zoom sous la colonne d'outils est lisible (fond sombre
  arrondi, texte clair).

**Infobulles, menus et fiche objet (0.247–0.251, 0.255–0.257, 0.263, 0.272–0.273, 0.282, 0.292)**

- [ ] 0.247–0.250 — Dans plusieurs listes (butin des monstres, coût de préparation, sources de pêche et de récolte, usages,
  orchestrion, hauts faits, résultats de recherche, monnaie d'un marchand) : icône et nom d'un objet donnent la même infobulle complète,
  une seule à la fois ; clic = objet en contexte ; clic droit = outils. Mode dev : ID de l'objet dans l'infobulle.
- [ ] 0.251 — Clic droit sur un objet : sources en sous-menus par type avec leur nombre (Hauts faits, Contenus, À partir d'autres
  objets, Échanges, PNJ vendeurs, Quêtes) ; Fabrication et Récolte sur une ligne ; aucune ligne en double.
- [x] 0.255 — Récompenses de quête et Revue de mode : le nom de l'objet a l'infobulle complète et le clic droit.
- [x] 0.256 — Inventaire du jeu, clic droit sur un objet › Tensho : sous les outils, les mêmes sous-menus de sources que dans Tensho ;
  « ↳ Drapeau sur la carte » sous un PNJ placé.
- [ ] 0.257 — Survol d'un objet de l'inventaire du jeu : le panneau Tensho sous l'infobulle montre aussi « Comment l'obtenir ».
- [x] 0.263 — Clic droit sur une quête (liste, quêtes liées, onglet Quêtes de la carte), un PNJ (PNJ vendeurs, onglets Vendeurs /
  Triple Triad de la carte, PNJ de Triple Triade) et un monstre (monstres d'un objet) : mêmes entrées partout (ouvrir l'outil ; pour
  chaque position : drapeau, « Voir sur la carte Tensho », météo de la zone).
- [ ] 0.272 — Objets : choisir un objet = fiche complète (icône, stock total, dernier prix connu et sa date ; onglets Aperçu, Stocks,
  Craft lié, Usages, Récolte, Marché).
- [ ] 0.273 — Liens : objet d'une alarme, « Comment l'obtenir (fiche) », monnaie d'un échange → fiche sur Aperçu ; « Prix du marché » →
  Marché › Résumé ; « Ouvrir l'outil Triple Triade » depuis un PNJ → « PNJ et cartes » ; bouton « Deck » des réglages → « Mon deck ».
- [ ] 0.282 — Objet sans recette (ex. « Branchette de restauration (2e phase) ») : pas d'onglet Craft lié ; objet non vendable : pas
  d'onglet Marché ; l'outil s'appelle « Butin des monstres » dans la liste des outils.
- [ ] 0.292 (interface) — Fiche › Craft lié : onglets « Produit par (n) » / « Utilisé dans (n) », ouverts sur celui qui a des
  recettes ; recherche d'objets : « Résultats (n) » se replie.

**Paramètres, caches et langues (0.246, 0.248, 0.258, 0.260–0.261, 0.264, 0.281)**

- [ ] 0.248 — Le paquet installé ne contient que `TenshoAssistant.dll`, `TenshoAssistant.json` et `TenshoAssistant.deps.json` ;
  changer de langue dans Paramètres : textes traduits (langues dans la DLL).
- [ ] 0.258 — Paramètres › Avancé › « Tout remettre par défaut » (confirmation) : réglages remis ; gardés : langue, alarmes,
  préréglages d'artisanat, decks, quêtes suivies, favoris de carte, plan de macros, profils et modèles de tri, relevés, et depuis 0.315
  vaisseaux lus et plans d'équipement / de matérias ; la Banque n'est pas touchée.
- [ ] 0.260–0.261 — ↺ sur le délai entre commandes du jeu (Avancé) et sur le thème (Interface : revient à « Suivre le jeu »).
- [ ] 0.264 — Barre latérale en catégories repliables (OBJETS, PRODUCTION, MONDE, PROGRESSION, GOLD SAUCER, PRÉFÉRENCES) : clic sur le
  titre, état gardé ; catégorie repliée qui contient la page ouverte = titre doré ; même nom dans la barre, le titre et les menus.
- [ ] 0.281 — Marché d'un objet vendable, Universalis injoignable : la dernière lecture s'affiche (« Lecture impossible : données en
  cache du … »). Paramètres › Données, durée « Jamais (seulement sur demande) » : plus de relecture d'elle-même, « Actualiser » marche.
- [ ] 0.246 (facultatif) — Hors ligne : `dalamud.log` ne montre pas de téléchargement LuminaSupplemental / GatherBuddy relancé à
  chaque image (5 min entre deux essais) ; les sources gardent leur copie expirée.

**Rappels et barre du serveur (0.300–0.302)**

- [ ] 0.300 — MONDE › Rappels, comparer au Calendrier du jeu : missions de ravitaillement, commandes spéciales (livraisons restantes),
  Revue de mode, quêtes quotidiennes des peuples alliés (restantes), autorisations de mandats, cartes au trésor (« Carte ouverte »),
  tâches de servant (après ouverture de la liste à une sonnette) : état et « Prochaine fois » ; « Fait » coché se décoche seul à la
  remise suivante ; la cloche crée une alarme (page Alarmes).
- [ ] 0.300 — Avec une alarme Tensho active : icône d'alarme dans la barre du serveur ; clic = page Alarmes ; le réglage « Icône
  d'alarme dans la barre du serveur » (Paramètres › Général ; le CHANGELOG 0.300–0.301 dit « Avancé », le code le dessine dans Général) la
  masque.
- [ ] 0.301 — Survol de l'icône, carte Tensho ouverte : infobulle Tensho juste dessous, par-dessus la carte, avec les prochaines
  alarmes ; « Alarmes listées » (1–10) change leur nombre ; « Infobulle Tensho sous l'icône » décoché : infobulle du jeu. À confirmer :
  Tensho retrouve son entrée dans la barre (`_DTR`) ; sinon il garde l'infobulle du jeu.
- [ ] 0.302 — La barre affiche le temps restant (« 2 h 15 », « 45 min », « < 1 min ») et non l'heure ; l'infobulle : temps restant,
  puis l'heure entre parenthèses.

**Compagnie libre (0.307, 0.309, 0.315)**

- [x] 0.307 — Entrer dans l'atelier de la compagnie (ouvrir le panneau de contrôle des expéditions si rien n'apparaît) : MONDE ›
  Compagnie libre › Vaisseaux liste sous-marins et dirigeables (rang + barre d'expérience, trajet, retour : temps restant / « Rentré » /
  « Au port », butin du dernier voyage au clic, bouton d'alarme au retour) ; après la sortie, la dernière lecture reste (« Lu dans
  l'atelier : … »). Rappels › « Expéditions de la compagnie » : en mer / rentrés, prochain retour, « Détails ». Noter le moment où les
  données apparaissent (entrée dans l'atelier ou ouverture du panneau : non vérifié en jeu).
- [x] 0.309 — « Meilleurs trajets » : sous-marin lu, zone, tri (valeur par voyage, valeur par jour, expérience par jour) → au plus 25
  trajets (distance / autonomie, durée, valeur, gils par jour, expérience) ; survol = butin moyen, objet par objet. À comparer : durée
  calculée contre retour réel d'un voyage lancé.
- [ ] 0.315 — « Montages » : choisir un vaisseau lu → statistiques lues / prévues / écart, coût / capacité, rang des pièces, réparation,
  nom court (« SSUC++ ») ; **aucun ⚠ attendu** sur un vaisseau non modifié (⚠ = statistiques lues ≠ pièces + bonus de rang : capture à
  rapporter) ; clic sur une pièce = liste de l'emplacement ; un plan par vaisseau, gardé après rechargement.
- [ ] 0.315 — Meilleurs trajets : « Secteurs débloqués seulement » (activé par défaut) ne garde que les secteurs débloqués de la
  compagnie du sous-marin ; « Montage » sur un trajet → section Trajet (distance / autonomie, durée, paliers, usure ; « inconnu » si un
  secteur manque) et « Meilleurs montages pour ce trajet » → « Appliquer » ; « Montage prévu » reprend rang, vitesse et autonomie du
  plan ; un trajet au-dessus du rang est signalé, sans montage proposé.
- [ ] 0.315 — Dirigeable : statistiques, capacité et rang des pièces seulement. « Tout remettre par défaut » ne vide plus vaisseaux ni
  plans. Décharger le plugin pendant une recherche : pas d'erreur dans `dalamud.log`.

**Relevés en jeu demandés (preuves, pas des réglages)**

- [ ] Mandat accepté, carte ouverte : relever le type de marqueur d'un mandat (repères au bord « Mandats et autres événements », 0.298).
- [ ] Icônes de quête 71141 / 71151 : seulement avec un relevé en jeu (STATUS, points décidés avant D17).

**Encore listés « à valider » dans STATUS avant 0.232**

- [ ] 0.224 — Mineur / botaniste, objet récolté en contexte : outil PR, « Estimé : +N » à côté du bonus, bouton « Utiliser ».
- [ ] 0.225 — Panneau « Aide à la récolte · Mineur / Botaniste » à droite de la fenêtre de récolte du jeu, un métier à la fois.
- [ ] 0.226–0.228 — Aide à la pêche : poissons du lieu, appâts, créneau, météo ; GatherBuddy d'abord, Garland en secours ; bouton
  « Ouvrir l'aide à la pêche » et `/tensho peche` (détection « pêche en cours » déjà vue en jeu, capture 2026-11-01). Pêche en mer non
  couverte.
- [ ] 0.230 — Page Récolte : outils de l'objet en tête ; pour un poisson, « Comment le pêcher · GatherBuddy » en premier.

**Webhook Discord (R-12, 0.319)** — à faire pendant la campagne de tests en jeu de la fin (étape h de la file), avec un salon de test

- [x] Paramètres › Alarmes › Discord : coller l'adresse d'un webhook d'un salon de test, quitter le champ : « Adresse enregistrée :
  https://discord.com/api/webhooks/1234…/*** » ; une adresse d'un autre site reste dans le champ avec le message rouge, non enregistrée.
- [x] « Envoyer un test » : le message d'essai arrive dans le salon, avec le nom « Tensho » ; la ligne d'état dit « Message envoyé. ».
- [x] Activer l'envoi, créer une alarme manuelle (page Alarmes) à une heure ET proche : à la sonnerie, le message arrive sur Discord
  (et le chat et le son du jeu comme avant).
- [ ] Décocher une catégorie, ou la case « Discord » d'un groupe : ses alarmes ne partent plus ; message propre à un groupe (créneaux du
  groupe) : il remplace le modèle.
- [ ] Rôle à mentionner (identifiant ou mention collée) : le rôle est notifié ; un texte avec @everyone dans le message ne notifie personne.
- [ ] « Effacer » l'adresse pendant un envoi : rien de plus n'arrive ; le rapport de diagnostic (Diagnostics › Copier le rapport) ne
  contient pas l'adresse (ligne « Discord on · webhook valid… »).

**Page Crédits (R-13, 0.320)** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Préférences › Crédits : sept sections (le jeu, bibliothèques, données intégrées, données en ligne, règles reprises, plugins et
  sites lus, outils), 72 entrées ; textes en français et, langue anglaise choisie, en anglais (noms et auteurs identiques).
- [ ] Filtre (« MIT », « Garland »…) ; « Copier le lien » et « Copier la licence » remplissent le presse-papiers ; colonne Usage
  renvoyée à la ligne, sans texte coupé à une largeur de fenêtre réduite.
- [ ] Paramètres : couper « Universalis (prix du marché) » (Paramètres › Avancé) → la ligne Universalis passe à « Réglage désactivé » ;
  Discord suit Paramètres › Alarmes › Discord. Depuis 0.324, aucune ligne ne dit « réglage partiel ».
- [ ] « Copier l'adresse du dépôt » en bas de page.

**Point d) — 0.321** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Paramètres › Modules › Artisanat : changer « Étapes », « Largeur », « Budget », « Attente » et « Écho », recharger le plugin : les
  valeurs restent ; le bouton ↺ remet chacune ; la case « Aide du carnet » s'enregistre au clic et montre son infobulle au survol.
- [x] Paramètres › Avancé › « Revue de mode (FFXIVGachaSpreadsheet) » décoché : la page Revue de mode dit « source désactivée » quand
  aucune copie n'est gardée, sans téléchargement.
- [ ] « Tout remettre par défaut » : les groupes repliés de la barre latérale restent repliés.
- [ ] Marché hors ligne (réseau coupé) : le bouton de portée « Région » reste grisé, avec son infobulle ; pas de ralentissement.
- [ ] Cabine d'essayage et Rappels : fonctionnent comme avant (aucun changement visible attendu).

**Point d) — 0.324 : interrupteurs de sources** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Paramètres › Avancé › « Universalis (prix du marché) » décoché : « Prix au survol » est grisé ; la page Marché ne charge plus
  rien de neuf, montre les prix déjà en cache et dit « Source désactivée » sinon ; « Actualiser » est grisé avec son infobulle ;
  aucun prix au survol. Recoché : le tableau des annonces se recharge tout seul.
- [ ] « Garland Tools » décoché : le butin d'un contenu ou d'un monstre déjà consulté s'affiche encore (copie gardée) ; un contenu
  jamais consulté dit « Source désactivée » ; « Actualiser » est grisé, ainsi que l'actualisation du panneau du Duty Finder.
- [ ] Wiki décoché : « Monstres au survol » est grisé (Paramètres › Interface) ; la page Monstres et le carnet d'exploration de la
  carte n'interrogent plus le wiki (copie gardée montrée).
- [ ] Boutons ↺ : ouverture de la fenêtre, survol, Duty Finder, panneau du personnage, capture de la banque, durées de cache, options
  des alarmes, copie des macros, options de la Revue de mode, zones de la carte (la couleur a son infobulle) : changer une valeur
  fait apparaître ↺, un clic remet la valeur par défaut, et elle reste après rechargement du plugin.

**Point d) — 0.325 : infobulles, confirmations** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Survol des filtres, recherches, tris et champs de saisie des pages (Banque, Collections, Butin, Quêtes, Récolte, Objets,
  Artisanat, Marché, Cartes, Rangement, Triple Triad, Revue de mode, Équipement, Météo…) : chacun montre une infobulle, en
  français et en anglais, sans texte coupé.
- [ ] Panneau du Duty Finder : la recherche accepte l'identifiant exact d'un objet ; ↻ a son infobulle ; la liste d'origine du
  butin aussi ; aucun ralentissement en changeant de filtre.
- [ ] Alarmes : « Tout supprimer » d'un groupe demande un second clic (« Confirmer : supprimer N alarme(s) », ou Annuler) ;
  champs de l'alarme manuelle et bouton « Ajouter » avec infobulle.
- [ ] Carte › tailles des icônes : le bouton de remise à zéro porte un libellé exact, avec une infobulle par portée.

**Point d) — 0.326 : corrections de la revue** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Survoler plusieurs objets, puis couper le wiki ou Garland (Paramètres › Avancé) : plus de téléchargement (journal Dalamud) ;
  rallumer : les sources de ces objets reviennent au survol suivant.
- [ ] Duty Finder avec Garland coupé sur un contenu jamais vu (« Source désactivée »), rallumer : le butin se charge tout seul.
- [x] Duty Finder, « non obtenus seulement » coché juste après la connexion : la liste se remplit une fois les collections lues.
- [ ] Alarmes : le même objet dans deux sections (mineur et botaniste) : « Tout supprimer » n'arme que la section cliquée ;
  replier la section désarme le bouton.
- [x] Revue de mode coupée : « Actualiser » grisé avec son infobulle ; Accueil sans « service en erreur » quand Universalis est coupé.
- [ ] Banque (client anglais) : les titres des cadres de la sacoche et de l'arsenal sont en anglais.
- [ ] Première vraie release avec Verify.ps1 -Release : suivre la sortie de près (ordre et reprise vérifiés par l'analyse de syntaxe seulement).

**Étape e) — 0.327 : réglages** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Paramètres › Interface : nombre de résultats à 50 → la recherche d'objets en liste 50 et l'avis dit 50 ; ↺ remet 30.
- [ ] Paramètres › Général : « dernière page » coché, aller sur Carte, fermer, recharger le plugin → s'ouvre sur Carte ; décoché → Accueil.
- [ ] Marché : choisir Monde, recharger → Monde gardé ; Région avec Universalis coupé → le message dit pourquoi.
- [ ] Survol : décocher chaque partie (collection, stock, usages, comment l'obtenir) → elle disparaît du panneau sous l'infobulle du jeu
  seulement ; « masquer en mission » coché → pas de panneau dans un donjon.
- [x] Triple Triad (observation activée) : chacun des trois interrupteurs coupe son aide (cadre du match, défi d'un PNJ, éditeur de decks).
- [ ] Carnet d'artisanat : « calcul automatique » décoché → bouton « Calculer » ; le décocher pendant un calcul garde « Calcul… ».
- [ ] Alarmes : « pas de ligne de discussion en mission » coché (sous « Pas de son en mission ») → aucune ligne [Tensho] en donjon.

**Étape e) — 0.328 : carte** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] « Remplacer la carte du jeu » et « Masquer en combat » cochés : M en combat montre la carte du jeu (plus d'écran sans carte).
- [x] Carte Tensho ouverte par M, bouton « Ouvrir la carte du jeu » : la carte du jeu revient à l'écran.
- [x] Zoom +, − et curseur sans suivre le personnage : la vue reste centrée au même endroit.
- [ ] Repère au bord du drapeau au-dessus d'une icône de vendeur : un clic centre la carte sans déplacer le drapeau.
- [ ] Drapeau masqué (taille 0) : plus de repère au bord ; zone de récolte masquée : son survol ne liste plus l'icône.
- [ ] Icônes de la carte, ↺ d'une ligne : taille, priorité et mode du clic droit reviennent ; priorité grisée pour les couches Tensho.
- [x] Fenêtre de téléportation : le coût s'affiche (ou « indisponible » si la lecture échoue, jamais « Lecture… » sans fin).
- [x] Favoris : un nom en double dit « déjà utilisé », pas « Échec de sauvegarde » ; page Cartes, le bouton pose le drapeau.
- [ ] Liste des panoramas (clic droit) : clic droit sur une ligne → alarme proposée ; numéro identique à l'infobulle.
- [ ] Ville chargée : la carte reste fluide (vendeurs mis en cache), temps à relever dans Diagnostics si besoin.

**Étape e) — 0.329 : objets, banque, tri, marché** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] « Enregistrement automatique » décoché : ouvrir l'arsenal et le coffre à apparence → ils apparaissent quand même dans la Banque,
  sans message « import » dans le panneau des servants.
- [ ] Coffre à apparence très rempli : l'import s'enregistre et se recharge après un rechargement du plugin.
- [ ] Fiche d'un objet ouverte directement (onglet Aperçu) : le stock de l'en-tête est juste ; pour un cristal, les servants montrent « ≥ ».
- [ ] Survoler vite plusieurs objets avec prix au survol : pas de « prix indisponibles » pendant la pause de 3 s.
- [ ] Historique des objets : « Retour » revient à l'objet précédent ; le fil d'Ariane reste court.
- [ ] Aperçu du tri impossible : la raison est affichée.

**Étape e) — 0.332 : cadre du jeu** — retiré en 0.510 (DEC-033), plus rien à tester


**Étape f) — 0.333 : escouade** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Mission d'escouade lancée : Rappels › « Mission d'escouade » donne son nom et une heure de fin égale à celle du jeu ; la cloche crée
  une alarme qui sonne à ce moment ; une fois finie, « Terminé (à récupérer à la caserne) ».
- [ ] Même chose pour un entraînement d'escouade ; sans mission ni entraînement : « Aucun en cours ».

**Étape f) — 0.334 : seuils des sous-marins** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Montages, trajet passant par le secteur 86 ou 130 : la ligne « Palier non confirmé par les relevés » s'affiche avec son infobulle ;
  trajet sans ces secteurs : pas de ligne.

**Étape f) — 0.335 : valeur des trajets** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Meilleurs trajets, « Prix » sur « PNJ ou marché » : le bouton « Prix du marché (n sur m manquants) » récupère au plus 32 prix, puis
  le classement est refait ; les valeurs avec « ≥ » ont encore des prix manquants ; au survol, chaque objet donne son prix PNJ et marché.
- [ ] Universalis coupé (Paramètres › Avancé) : le bouton est grisé avec l'infobulle « source coupée » ; « Revente au PNJ » donne les
  mêmes valeurs qu'en 0.334.
- [ ] « Surveillance » sur « Palier 3 » : le classement change ; un trajet avec un secteur sans relevé à ce palier le dit au survol.

**Étape f) — 0.336 : secteurs de dirigeable** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Compagnie › « Secteurs de dirigeable » : 23 secteurs de la Mer de nuages (pas le Diadème), rang, céruleum, durée de relevé et
  expérience égaux au panneau de vol du jeu ; « Débloqué par » cohérent avec l'ordre de découverte en jeu.
- [ ] Survol de « n objets » : liste d'objets avec icône, sans quantité ; le filtre trouve un secteur par le nom d'un objet.
- [ ] Paramètres › Crédits : LuminaSupplemental apparaît deux fois (téléchargé ; embarqué pour les dirigeables).

**Étape f) — 0.337 : planificateur de dirigeables (appelle les fonctions de calcul du jeu)** — pendant la campagne de tests en jeu (étape h)

- [x] Secteurs de dirigeable, connecté : choisir un dirigeable lu à l'atelier ; plus de « Calcul des étapes par le jeu… » après une seconde ;
  aucun plantage, rien dans le journal Dalamud.
- [x] Cocher les secteurs d'un voyage réel, puis comparer avec le panneau de vol de l'atelier (même dirigeable, mêmes secteurs) :
  distance et durée identiques ? Sinon, noter l'écart et les valeurs « Brut » du mode développeur (unité, 12 h, retour).
- [ ] Déconnecté : « Connectez-vous avec un personnage » ; reconnecté : le calcul reprend.
- [ ] « Meilleurs trajets » : classés par expérience par jour ; les secteurs sans expérience n'apparaissent que sous « Plus court », avec « ≥ ».
- [ ] Sous-marins, « PNJ ou marché » : survoler des objets ailleurs entre deux clics « Prix du marché » ne fait pas revenir « ≥ » sur
  les prix déjà récupérés ; un objet sans offre NQ dit « aucune offre NQ » et n'est plus redemandé.

**Étape f) — 0.338 : taux de surmatérialisation** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Équipement, pièce avec surmatérialisation : dans l'éditeur, chaque grade d'un emplacement avancé donne son taux ; les mêmes que
  la fenêtre de pose du jeu (ex. : matéria IX, premier emplacement, pièce HQ : 17 %).
- [ ] Grades VI, VIII, X et XII proposés dans le premier emplacement avancé seulement ; XI et XII : taux « selon Teamcraft », égal à
  celui de la fenêtre de pose du jeu (ex. : XII, premier emplacement, pièce HQ : 17 %).

**Étape f) — 0.340 : barde de chocobo** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Équipement › « Barde de chocobo » : nom, rang et couleur du chocobo comme dans le jeu ; « Porte » donne la bonne barde pour la tête,
  le corps et les jambes (l'ordre est supposé ; s'il est faux, le noter).
- [ ] « n bardes débloquées sur m » comparé au carnet de bardes du jeu ; « Seulement celles qui manquent » ; recherche par nom d'objet.
- [ ] 0.341 : page « Chocobo » dans Progression (plus en bas d'Équipement) ; ✓ / ✗ bien dessinés ; la croix « possédé » sur la ligne de
  l'objet ; déconnecté : « hors ligne ». Plus de page Personnage ; le panneau à côté de la fenêtre Personnage du jeu est toujours là.

**Étape f) — 0.342 : événements saisonniers** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Progression › Événements, pendant une fête : son nom français (« Veillée des saints »…) et la ligne « Nom tiré du PNJ guide » ;
  plus de « Special Event Flag » ; les événements sans nom disent d'où viendrait le nom.
- [ ] Quêtes de la fête : ✓ pour celles faites, « Ouvrir » mène à la quête dans la page Quêtes.
- [ ] Boutiques de l'événement : le vendeur de la fête, ses villes, ce qu'il vend et le prix ; comparer avec la boutique en jeu.

**Étape g) — 0.343 : I-01 infobulle de prix complète** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Survol d'un objet vendable : sous chaque prix, « Dernière vente · moyenne · ventes par jour » ; les mêmes que la page de l'objet
  sur universalis.app pour votre monde.
- [x] Survol d'une pile de l'inventaire : « Pile de N : ~X » (N = la quantité de la case) ; pas de ligne pour un objet hors de vos sacs.
- [ ] « Le moins cher de la région » n'apparaît que s'il est moins cher que le centre de données, avec le nom du monde.
- [ ] Ctrl+Alt pendant le survol : la date « Récupéré à » change ; maintenu, une seule demande ; Paramètres › Avancé, l'éteindre.

**Étape g) — 0.344 : I-02 monnaies et alertes de plafond** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Rangements › Monnaies : soldes et plafonds identiques à la fenêtre Monnaies du jeu (sceaux de votre compagnie au plafond de votre
  rang, Centurio, gemmes bicolores, assignats, mémoquartz) ; « Mémoquartz limités cette semaine » égal au jeu.
- [ ] Connexion avec une monnaie déjà au-dessus de 80 % : listée en or, entrée « monnaies » dans la barre du serveur, AUCUN message.
- [ ] Gagner assez pour passer un seuil (ou baisser un seuil sous le solde puis le remonter) : un message dans le chat, le son des
  alarmes, et un message Discord dans la catégorie « Monnaies » si Discord est réglé ; un seul, pas à chaque gain.
- [ ] Changer de personnage : pas de fausse alerte ; l'onglet n'affiche pas l'ancien personnage.
- [ ] Taper un seuil : enregistré en quittant le champ ; ↺ revient à 80 % ; 0 = plus d'alerte pour cette monnaie.

**Étape g) — 0.345 : I-03 vue de tous ses personnages** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Au premier chargement : le dossier pluginConfigs/TenshoAssistant/InventoryBank/ contient un fichier par personnage, l'ancien
  InventoryBank.json est renommé « .migrated » ; la banque montre les mêmes rangements qu'avant.
- [ ] Se connecter avec deux personnages, puis Rangements › Personnages : les deux, avec gils, objets des sacs, cristaux, servants.
- [ ] « Voir » sur l'autre personnage : l'onglet Banque montre ses sacs, cristaux, monnaies et rangements enregistrés ; pas d'onglet
  « Personnage » (direct) pour lui. Pour le personnage connecté : pas de sacs en double.
- [ ] « Effacer » (deux clics) sur l'autre personnage : il disparaît et son fichier est supprimé.
- [ ] L'aperçu de tri ne propose que les servants et les sacoches.

**Étape g) — 0.346 : I-06 prix PNJ, I-07 sous-cotation** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Survol d'une pile vendable au PNJ : « Vente au PNJ (NQ) : X gils l'unité, pile de N : Y » ; comparer avec la vente à un PNJ.
- [ ] Paramètres › Avancé › « Copier l'offre la plus basse moins 1 » activé : ouvrir les offres d'un objet au tableau des ventes ; le
  chat dit « offre la plus basse … prix copié … » et Ctrl+V colle ce prix ; une offre de vos servants n'est pas prise ; désactivé :
  rien n'est copié.

**Étape g) — 0.347 : I-08 notes et étiquettes d'objets** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Fiche d'un objet › onglet « Note » : écrire une note, cocher « Important » et « À vendre au PNJ » ; la note s'affiche au survol
  de l'objet dans le jeu.
- [ ] Ouvrir une boutique de PNJ avec ces objets dans les sacs : une ligne « À vendre au PNJ … ×n » et une ligne « Attention, objets
  importants … » dans le chat ; une seule fois par ouverture ; avec « Son pour les objets importants », le son des alarmes.
- [ ] « Restaurer tous les réglages » garde les notes.

**Étape g) — 0.348 : I-10 rapport de rangement** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Mettre un même objet en deux piles incomplètes dans les sacs : Rangements › Rapport le liste, avec « 1 case libérable ».
- [ ] « Arsenal : pièces hors des sets » : le nombre de sets lu égale vos sets ; une pièce d'un set n'y est pas ; une pièce sans set y
  est (le rapport ne déplace rien).

**Étape g) — 0.349 : X-07 liens wiki et Lodestone** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Clic droit sur un objet › Tensho : « Wiki (anglais) » et « Lodestone » ouvrent la bonne page dans le navigateur.
- [ ] Fiche d'objet : les deux boutons font de même ; « /tensho wiki minerai de fer » et « /tensho lodestone fer » ouvrent la page ;
  un nom inconnu donne un message dans le chat.

**Étape g) — 0.350 : I-09 coffre de compagnie** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Ouvrir le coffre de compagnie : le panneau Tensho apparaît à côté ; tant qu'un onglet n'a pas été affiché, il le dit ; après les
  cinq onglets (et les cristaux), « Enregistrer » ; avec l'enregistrement automatique, c'est fait seul.
- [x] Banque : un onglet « Coffre de compagnie » ; le stock d'un objet du coffre (fiche d'objet, survol) compte la ligne « Coffre de
  compagnie ».

**Étape g) — 0.351 : I-04 journal des gils** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Acheter à un PNJ, vendre à un PNJ, réparer, se téléporter : Rangements › Gils montre chaque variation avec la bonne catégorie
  (Boutique PNJ, Réparation, Téléportation).
- [x] Ouvrir la cloche des servants : le solde des servants apparaît ; ouvrir le coffre de compagnie : son solde apparaît.
- [ ] Changer de personnage : chaque personnage a son journal ; la première lecture n'est pas comptée comme un gain.

**Étape g) — 0.352 : I-11 conditions des articles de boutique** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Page Marchands, un marchand dont un article demande une quête : « Après la quête … (fait / à faire) » après le prix, juste
  selon votre journal ; un article lié à un haut fait affiche son nom.
- [ ] Le premier chargement après la mise à jour reconstruit l'annuaire des marchands (une fois).

**Étape g) — 0.353 : I-12 recherche d'objets avec filtres** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Page Objets › « Filtres » : catégorie « Tête », niveau d'équipement 90 à 100 : la liste ne montre que ces casques, sans rien
  écrire ; avec un mot en plus, la liste se réduit ; « Effacer les filtres ».
- [ ] « Objets clés » (recherche vide) : un nom d'objet de quête le trouve, avec son icône.
- [ ] « /tensho wiki » avec un nom très courant (ex. « fer ») ouvre une page sans erreur.

**Étape g) — 0.354 : I-13 valeur des monnaies** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Rangements › Monnaies › « Que faire ? » sur les sceaux de compagnie : la liste de ce qu'ils achètent, avec le prix en sceaux ;
  « Prix du marché » remplit les prix et « Gils par unité », classés du plus rentable ; une monture déjà possédée dit « possédée ».

**Étape g) — 0.355 : C-01 contenus débloqués / terminés** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Monde › Butin des contenus : la colonne « État » dit « Terminé » pour un donjon fait (ex. Sastasha), « Débloqué » pour un contenu
  ouvert mais pas fait, « Verrouillé » pour un contenu pas encore ouvert ; le compteur « X terminés sur Y lus » est plausible.
- [ ] Filtre « Avancement › Non terminés » : les donjons faits disparaissent ; déconnecté, le filtre est grisé et l'état « — ».

**Étape g) — 0.356 : C-02 missions aléatoires du jour** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Monde › Rappels › « Missions aléatoires du jour » : faire une mission aléatoire (ex. gain de niveaux) ; elle passe à « Faite »,
  les autres restent « À faire » ; le compteur et l'heure de remise à zéro sont justes.
- [ ] Ouvrir l'outil de recherche de mission : la colonne « Bonus de rôle » montre les mêmes rôles que le jeu pour chaque mission
  aléatoire (sinon : l'index du bonus est faux, docs/evidence/0.356-roulettes.md).

**Étape g) — 0.357 : C-03 objets à ranger dans l'armoire** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Ouvrir l'armoire et le coffre à apparence une fois (auberge). Rangements › Rapport › « Peut aller dans l'armoire » : un objet
  d'armoire gardé dans les sacs ou le coffre y figure ; s'il est aussi dans l'armoire, il est marqué « copie en trop ».
- [ ] Une pièce utilisée par un set d'équipement n'est jamais proposée.

**Étape g) — 0.358 : C-07 apparences identiques du coffre** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Rangements › Rapport › « Coffre à apparence : apparences identiques » : chaque groupe montre des objets qui ont vraiment la même
  apparence en jeu (essayer deux d'entre eux) ; le nombre de teintures affiché est juste.

**Étape g) — 0.359 : C-08 filtre des collections par source** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Collections › Montures › Source « Marchands (gils) » puis « Succès » : les montures listées ont bien cette source dans leur menu
  du clic droit ; combiné avec « Non confirmés », la liste se réduit.
- [x] Le premier choix du filtre sur l'Orchestrion ne fait pas saccader le jeu de façon gênante (sinon : calcul à étaler).

**Étape g) — 0.360 : C-09 liste de souhaits** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Clic droit sur une monture dans un butin (Butin des contenus) › « Ajouter à mes souhaits » ; Collections › Souhaits la montre
  avec « Pas encore obtenu » ; clic droit sur la ligne donne ses sources ; « Retirer » l'enlève.
- [ ] Un objet ordinaire ajouté montre « Possédé ×N » ou « Pas possédé » ; la liste reste après un redémarrage et après
  « Réinitialiser les réglages ».

**Étape g) — 0.361 : C-11 carnet d'exploration** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Carte › listes › « Carnet d'exploration » : le compteur « X découverts sur Y » correspond au carnet du jeu ; une zone à gauche
  montre ses panoramas à droite avec leur numéro ; un clic pose le drapeau.
- [ ] « Possibles maintenant » : un panorama marqué « possible maintenant » peut vraiment être fait à cette heure (heure et météo).

**Étape g) — 0.362 : C-05 tenues du coffre à apparence** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Ouvrir le coffre à apparence une fois. Collections › Tenues : une tenue déjà rangée dit « rangée » ; une tenue dont toutes les
  pièces sont dans le coffre dit « prête à ranger » (le jeu propose bien de la créer) ; les pièces ouvertes disent où elles sont.
- [ ] Filtres « Incomplètes », « Toutes les pièces », « Rangées » et la recherche par nom.

**Étape g) — 0.363 : C-14 mandats** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Progression › Mandats : les allocations restantes et le nombre de mandats acceptés sont ceux du jeu ; un mandat déjà fait dit
  « Terminé » ; filtres type, mandateur, niveau min/max et état.
- [ ] Accepter un mandat d'artisanat : il dit « Accepté » et apparaît dans « Mandats d'artisanat acceptés » avec l'objet demandé et
  les copies possédées.

**Étape g) — 0.364 : C-12 carnet de chasse** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Progression › Carnet de chasse, carnet de votre classe : « Le jeu en est au rang N » est le rang du carnet du jeu ; les victimes
  d'une entrée en cours sont les mêmes que dans le jeu (tuer un monstre : le compteur monte en 2 secondes).
- [ ] Le carnet de votre grande compagnie et celui d'une autre classe montrent aussi le bon rang (sinon l'ordre en mémoire est faux,
  docs/evidence/0.364-hunting-log.md).

**Étape g) — 0.365 : C-04 nouveaux types de collection** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Collections : les onglets Coiffures, Accessoires de mode, Lunettes, Bardes, Portraits, Livres de maître, Objets de collection,
  Archives occultes se remplissent en quelques secondes ; une coiffure ou un accessoire que vous avez dit « Obtenu », un autre non.
- [ ] Les filtres Obtenu / Non confirmés et la recherche marchent dans ces onglets.

**Étape g) — 0.366 : C-10 alerte « mission prête »** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Alarmes › réglages › « Alerte quand une mission est prête » cochée : à l'ouverture de la mission, la ligne « Mission prête : … »
  et le son ; décochée, rien.
- [ ] Discord réglé avec « Mission prête » coché : le message arrive ; décoché : rien n'est envoyé.

**Étape g) — 0.367 : C-13 monstres des sorts de mage bleu** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Mage bleu › un sort (ex. Mauvaise haleine) › « Chercher sur le wiki » : la liste des monstres arrive ; « Drapeau » pose le drapeau
  au bon endroit (ex. Forêt de l'est 23.1, 20.4).
- [ ] Un sort appris par quête (Canon à eau) dit qu'aucun monstre n'est listé ; interrupteur du wiki coupé : rien n'est demandé.

**Étape g) — 0.368 : C-15 progression des succès** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Ouvrir un succès en cours dans la fenêtre des succès du jeu : dans Tensho, sa fenêtre montre « Progression : x / y ».
- [ ] « Demander » sur un autre succès : la progression arrive en une seconde ; deux clics rapides : le second demande d'attendre.
- [ ] « Suivre ce succès » : il apparaît dans « Succès suivis » en tête de l'onglet Succès ; « Retirer » l'enlève.

**Étape g) — 0.369 : X-03 titres** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Collections › Titres › un titre obtenu › « Porter ce titre » : le titre change au-dessus du personnage ; « Aucun titre » le retire ;
  « Titre au hasard » en met un autre.
- [ ] « Titre de <job> » puis « Changer de titre avec le job » coché : changer de job porte le titre choisi ; décoché : rien ne change.

**Étape g) — 0.370 : X-02 musique en cours, rouleaux favoris** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Collections › Orchestrion : « Musique en cours » nomme la musique qu'on entend (en ville, puis en combat, puis dans une maison
  avec un orchestrion) ; « Fiche » ouvre son rouleau.
- [ ] Case « Favori » dans la fiche d'un rouleau : il apparaît dans « Rouleaux favoris ».

**Étape g) — 0.371 : M-01 alarmes automatiques** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Ouvrir la liste des servants à une cloche : dans les 30 secondes, une alarme « Venture de … terminée » par servant en venture
  apparaît dans Alarmes, à la bonne heure ; elle sonne à la fin.
- [x] Envoyer un vaisseau à l'atelier : une alarme « Retour de … » apparaît ; « Alarmes automatiques » décochée : plus aucune n'est créée.

**Étape g) — 0.372 : M-02 compteurs hebdomadaires** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Rappels : « Livre d'or des défis » montre les vignettes et points du livre de Khloe, avec son expiration ; « Contrats de chasse »
  le nombre de contrats en main ; « Cartes au trésor » l'heure de la prochaine carte du jeu (sans bouton « ouverte »).
- [ ] Outil de recherche de mission, une mission aléatoire choisie : le panneau Tensho dit « Faite » ou « À faire » comme le jeu.

**Étape g) — 0.373 : M-03 liens de carte du chat** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Envoyer un lien de carte (drapeau puis <flag>) en équipe et en compagnie libre : il apparaît dans Cartes › « Liens de carte du
  chat », avec le bon lieu, les bonnes coordonnées et le bon canal au survol ; le filtre de canal le cache et le remontre.
- [ ] « Drapeau » pose le drapeau du jeu au bon endroit ; « Éthérite » ouvre la carte Tensho sur le point et propose l'éthérite
  la plus proche avec son coût ; « Non » ne téléporte pas ; « Oui » téléporte.
- [ ] Sur une carte sans éthérite (ou non harmonisée) : message clair au survol de la carte. Déconnexion : la liste est vide.

**Étape g) — 0.374 : M-04 vos personnages** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Se connecter avec deux personnages l'un après l'autre (ouvrir la liste des servants avec chacun) : Rappels › « Vos personnages »
  montre les deux, ventures, carte au trésor, Livre d'or, contrats et missions aléatoires justes, « Connecté » sur le bon.
- [ ] Cocher « fait » sur une ligne des Rappels avec le premier, se connecter avec le second : la case n'y est pas cochée ; revenir au
  premier : elle l'est encore. « Effacer » puis « Confirmer » sur l'autre personnage : sa ligne et son fichier disparaissent.

**Étape g) — 0.375 : M-05 vos tâches et panneau à l'écran** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Rappels › « Vos tâches » : ajouter une tâche par jour, une toutes les 2 heures et un compteur à 3 ; les cocher, monter le compteur
  avec « + » : chacune dit quand elle revient ; flèches pour changer l'ordre ; « Retirer ».
- [ ] Cocher « Panneau à faire à l'écran » : le panneau montre les tâches non faites, se déplace, se cache en combat ; cocher une tâche
  dans le panneau la fait disparaître ; fermer le panneau décoche le réglage.

**Étape g) — 0.376 : M-06 usure et symbiose** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Équipement › « Usure et symbiose » : l'état et la symbiose de chaque pièce sont ceux de la fenêtre du jeu (la symbiose surtout :
  échelle à confirmer).
- [ ] Mettre « Réparer sous » au-dessus de l'état actuel : un message dans le chat, une seule fois ; réparer puis redescendre : il
  revient. « Barre d'usure à l'écran » : la barre montre l'état le plus bas et la symbiose la plus haute, elle se déplace.

**Étape g) — 0.377 : M-07 comparaison au survol** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Survoler une pièce d'équipement dans l'inventaire et au marché : sous l'infobulle, « Comparé à l'équipement porté » donne le
  niveau d'objet et les statistiques en plus (vert) et en moins (rouge) par rapport à la pièce portée ; une bague : deux lignes.
- [ ] Survoler la pièce portée elle-même : pas de comparaison. Décocher la case dans les réglages du panneau : elle disparaît.

**Étape g) — 0.378 : M-08 favoris sur la carte** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Carte Tensho, clic droit › nom › « Ajouter aux favoris » : une étoile apparaît à cet endroit ; survol : nom, coordonnées ;
  clic : le drapeau du jeu se pose dessus.
- [x] Page Cartes : donner un groupe et une note à un favori, « Mettre à jour » : « [groupe] nom » dans la liste, note au survol.
- [x] « Copier le code de partage », le coller dans un autre client (ou après avoir retiré un favori) puis « Importer » : seuls les
  favoris absents sont ajoutés.

**Étape g) — 0.379 : M-10 historique du butin** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Après le retour d'un sous-marin, passer à l'atelier : Compagnie libre › Historique montre le voyage (retour, vaisseau, trajet,
  objets) ; le rendement par trajet compte un voyage ; un second passage n'ajoute pas de doublon.
- [ ] « Copier en CSV » puis coller dans un tableur : une ligne par objet, colonnes lisibles.

**Étape g) — 0.380 : M-09 paliers de statistiques** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Page Équipement (ou panneau près de la fenêtre Personnage) : la colonne « +N palier » est présente ; ajouter une matéria qui
  donne au moins N points de la statistique : la valeur réelle change ; moins de N : elle ne change pas.

**Étape g) — 0.381 : X-01 tâches libres** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Taper « /tensho todo Acheter des potions » : le chat confirme, la tâche apparaît dans Rappels › Vos tâches (type « Une fois ») ;
  cochée, elle reste faite le lendemain ; « /tensho todo » seul affiche l'utilisation.

**Étape g) — 0.382 : M-11 autres compteurs** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Ouvrir le carnet d'objectifs du jeu : la ligne « Carnet d'objectifs » des Rappels donne le bon nombre de défis faits (indice des
  lignes à confirmer).
- [ ] Passer à l'Enclave de Doman, faire un don : la ligne « Enclave de Doman » montre le don et le plafond de la semaine.

**Étape g) — 0.383 : M-12 ensembles d'équipement** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Équipement › « Ensembles d'équipement du jeu » : tous vos ensembles, avec numéro, nom, job et niveau d'objet du jeu.
- [ ] Mettre dans un sac une pièce meilleure pour un autre job : son ensemble dit « 1 amélioration possédée », déplié la pièce et le
  gain ; une pièce HQ est marquée HQ. « Copier le code » puis coller dans la page Équipement (import) : les pièces reviennent.

**Étape g) — 0.384 : M-14 carte au trésor** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Déchiffrer une carte au trésor : dans les 5 secondes, Rappels › Cartes au trésor montre « Voir sur la carte » ; le bouton ouvre
  la carte Tensho sur le bon endroit (comparer avec la carte du jeu) et y pose le drapeau ; l'icône gemme est au même endroit.
- [ ] Ouvrir le coffre : l'icône et le bouton disparaissent.

**Étape g) — 0.385 : X-08 job au hasard** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Équipement › Ensembles du jeu › « Job au hasard » plusieurs fois : des jobs différents, jamais un job au niveau le plus haut
  quand la case est cochée ; « Équiper » porte l'ensemble tiré (comme dans le jeu) ; en combat le jeu refuse, sans erreur.

**Étape g) — 0.386 : X-10 horloges et alarme à date** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Alarmes › Horloges : ajouter « Tokyo » et « New York » : les heures sont justes ; « Retirer » enlève une horloge.
- [ ] Taper une date dans deux minutes (« AAAA-MM-JJ HH:MM ») et « Ajouter à cette date » : l'alarme apparaît dans « Alarmes à date »
  et sonne à l'heure.

**0.387 : signalements corrigés** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Grimoire du mage bleu, « Mauvaise haleine » › « Chercher sur le wiki » : les monstres (Capricious Cassie, Halitostroper…) avec
  « Drapeau » pour ceux qui ont des coordonnées ; un sort appris par quête dit « aucun monstre » avec l'adresse de la page.
- [x] Collections › Coiffures, Accessoires de mode, Bardes, Livres de maître : le survol montre l'objet et comment l'obtenir ; clic droit
  : les sources. Lunettes, Portraits, Objets de collection, Archives : l'infobulle dit « obtention inconnue ». Tenues : survol d'une
  tenue ou d'une pièce = infobulle de l'objet.

**Étape g) — 0.391 : X-06 tic de PR** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Page PR › cocher « Barre du tic de PR à l'écran », en mineur avec des PR pas pleins : la barre se remplit en 3 secondes et repart
  à chaque gain de PR ; elle disparaît à PR pleins, en combat et sur un autre job.

**Étape g) — 0.392 : R-01 pêche en mer** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Page Pêche en mer : l'heure du prochain départ et les trajets (Indigo, Rubis) sont ceux du PNJ du port ; « Embarquement ouvert »
  pendant les 15 minutes ; filtre d'un trajet ; une alarme sonne avant le départ.

**0.537 : s'effacer devant les fenêtres du jeu** — à voir en jouant

- [ ] Aide à la récolte ou carnet : une autre fenêtre du jeu passe devant et touche le panneau : il se cache ; elle part : il revient. Une fenêtre derrière ne fait rien.
- [ ] Barre de PR, usure, suivi, tâches : une fenêtre du jeu les touche : elles se cachent. Le chat, les plaques de noms et le HUD ne les cachent pas.
- [x] (rapport de l'utilisateur, 2026-11-05, « fenêtre ok ») Carte, réglage « S'effacer devant les fenêtres du jeu » coché : une fenêtre la touche : elle se cache sans se fermer.
- [ ] Les trois réglages (Paramètres, carte). Rien ne disparaît sans raison ; sinon, le journal de débogage dit quelle fenêtre compte.
- [ ] (0.538) Une infobulle ou un menu du jeu qui touche la carte ou un panneau : il s'efface, puis revient. Passer la souris sur plusieurs objets ne le fait pas clignoter de façon gênante.

- [x] (rapport de l'utilisateur, 2026-11-05, « tous es ok ») (0.539) Déplacer une fenêtre du jeu qui a un panneau de Tensho (récolte, carnet, personnage, mission) : le panneau la suit sans retard visible.
- [x] (rapport de l'utilisateur, 2026-11-05, « tous es ok ») (0.539) Réglage de la carte activé : un panneau lié qui touche la carte la fait disparaître ; passer d'une infobulle à l'autre ne la fait pas clignoter, et elle revient environ 1 seconde après.

- [ ] (0.540) Journal du jardin : « Graines de centaurée » est dans la liste, avec 24 h ; une plantation de cette graine est reconnue dans le chat. Comparer la durée réelle.

- [ ] (0.541) Mes outils, bloc d'objets : colonne de prix, coût de ce qui manque, bouton « Demander les prix ». Un objet invendable affiche un tiret.

- [ ] (0.542) Carte, réglage « Zoom à l'ouverture » : régler un zoom, relancer le jeu : le dernier zoom revient. Tester aussi « Fixe » et « Carte entière », puis le zoom sur une quête.

- [ ] (0.543) Carte : les quatre réglages des noms et des étiquettes de récolte changent bien l'affichage ; aux valeurs par défaut, rien ne change.

- [ ] (0.544) Carte : clic droit sur un point, puis sur le drapeau : « Éthérite la plus proche » donne le nom et le coût, et demande confirmation.

- [ ] (0.545) Carte : la molette zoome aussi sur la colonne d'outils et le curseur ; le pas et l'inversion fonctionnent.

- [ ] (0.546) Carte, réglage activé : un clic gauche sur un endroit vide pose le drapeau ; un glisser ne le pose pas ; un clic sur une icône garde son effet.

- [ ] (0.547) Carte, bouton « Couches » : chaque case change la carte, comme dans les réglages ; « Tout masquer » puis « Tout afficher » ; la colonne tient dans une petite fenêtre.

- [ ] (0.548) Carte : une ALEA à bonus affiche « bonus d'expérience » ; comparer avec l'icône de bonus du jeu. La zone reste rose.

- [ ] (0.549) Porter puis retirer une pièce : la ligne « Équipement porté » suit ; la Revue de mode la dit possédée ; le tri ne la déplace pas.

- [ ] (0.550) Quêtes : colonnes Niveau et Zone, tri en cliquant les titres, plage de niveaux gardée après une relance.

- [ ] (0.551) Monstres : le bouton carte pose le drapeau à la position de la fiche ; comparer avec la position réelle en jeu.

- [ ] (0.552) Carnet d'artisanat : une séquence enregistrée sur une autre recette de même difficulté est proposée, avec « Enregistrée pour ».

- [ ] (0.553) Simulateur : les seuils affichés correspondent ; régler ces valeurs donne le même résultat. Comparer une macro en jeu aux seuils.

- [ ] (0.554) Porter un métier d'artisan, puis un autre : le carnet calcule la recette du premier avec « relevés le… » ; porter le métier reprend les valeurs actuelles.

- [ ] (0.555) Commandes : « /tensho prix » suivi d'un nom d'objet ouvre le marché de cet objet ; essayer aussi objet, recolte et recette, et un nom inconnu.

- [ ] (0.556) Alarmes, réglage activé : l'alarme sonne avec son avance puis à l'heure ; une seule fois chacune, en chat comme en son.

- [ ] (0.557) Alarmes : modifier un nom et une date ; « Dans 5 min » ajoute une alarme qui sonne ; une date passée ou mal écrite est refusée.

- [ ] (0.558) Rappels : finir les quêtes de tribu ou les commandes spéciales : la case se coche seule, et le compteur de la barre suit. Vérifier qu'une connexion ne coche rien à tort.

- [ ] (0.559) Préparation et chaîne : le survol d'un matériau donne le détail par rangement, avec la date pour un servant.

- [ ] (0.560) Calculer une chaîne, puis récolter un de ses matériaux : « Encore N pour… » apparaît et baisse.

- [ ] (0.561) Menu d'un objet : copier le nom ; le lien s'affiche dans le chat, seulement pour vous ; les trois pages s'ouvrent sur le bon objet.

- [ ] (0.562) Réglage activé : la fiche et l'infobulle montrent le nom anglais ; rien en client anglais.

- [ ] (0.563) Météo : ajouter deux zones au tableau, comparer aux prévisions du jeu, retirer une zone ; le tableau reste après une relance.

- [ ] (0.564) Discord : un message avec les cinq balises ; l'heure suit le fuseau du lecteur, et l'heure d'Éorzéa correspond.

- [ ] (0.565) Aperçu du tri : les cases encadrées sont bien celles qui bougent ; le nombre correspond après application.

**0.535 : interfaces** — à voir en jouant

- [ ] Équilibré grisé : pas de clic possible, l'infobulle dit pourquoi ; Sûr apparaît choisi.
- [ ] Carte › Paramètres : « Sans barre de titre » : plus de barre ; elle se déplace par sa colonne d'outils et se ferme avec la touche de la carte. Le déplacement par la colonne reste à confirmer.
- [ ] Panneaux à barre de titre (récolte, pêche, carnet, ventes…) : réduire et fermer marchent ; fermé, il revient en rouvrant la fenêtre du jeu.
- [ ] Trier : disparaît sous la fenêtre des filtres de tri ou toute autre fenêtre devant, revient quand elle part ; ne disparaît pas sans raison.

**0.534 : Équilibré grisé quand il revient au même** — à voir en jouant

- [ ] Nœud où Sûr et Équilibré donnent les mêmes chiffres : la balance est grisée, l'infobulle le dit, et elle reste cliquable. Là où ils diffèrent : elle est normale.
- [ ] Le panneau répond sans à-coup en changeant de cran ou de tour : trois calculs tournent maintenant en tâche de fond.

**0.533 : les coups de chaque cran** — à voir en jouant

- [ ] Chaque icône donne sa suite de coups et ses PR, et le premier coup est celui de « Prochaine action ». Après un tirage, la suite se recalcule.
- [ ] Le nombre de tentatives de la suite correspond à ce qui reste, plus les tentatives rendues.

**0.532 : panneau Collectionnable sans répétitions** — à voir en jouant

- [ ] Plus de chiffre dit deux fois ; au survol de « Comment est-ce calculé ? », la tentative gratuite et la règle d'Œil et de Toucher.
- [ ] D'un cran à l'autre, la moyenne ne baisse jamais à plancher égal.

**0.531 : description chiffrée des crans** — à voir en jouant

- [ ] Sur un même nœud, chaque icône change la phrase et ses chiffres. Sûr et Équilibré peuvent donner la même chose quand rien ne les sépare.
- [ ] Sûr n'a plus une moyenne plus basse qu'Équilibré à plancher égal.

**0.529 : trois crans de risque des collectionnables** — à voir en jouant

- [ ] Panneau Collectionnable (0.530) : trois icônes, main, balance, dé ; le nom au survol ; le cran choisi coloré, sa description dessous ; même choix dans Paramètres › Modules › Récolte.
- [ ] Sur « Sûr » : le coup, le plan sans chance, ses actions et ses PR. Sur les deux autres : le coup, la moyenne, le pire cas, et la ligne « Sur Sûr… garantis ».
- [ ] En changeant de cran, le coup conseillé suit.

**0.527 : Toucher d'amorçage dans le plan des collectionnables** — à voir en jouant

- [ ] Botaniste ou mineur niveau 95 et plus : commencer vers 623 PR, lancer Inspection soi-même (il reste 423 PR) : le panneau propose Toucher d'amorçage ; une fois lancé, il ne le repropose plus et compte 48 %. Depuis 727 PR, le plan passe par Inspection puis Sélection méthodique, sans Toucher : c'est prévu (capture du 2026-11-05).
- [ ] Sous le niveau 95 : jamais proposé.
- [x] (rapport de l'utilisateur, 2026-11-05) Toucher reste actif si une autre action passe avant la Sélection méthodique ; il ne retombe qu'après elle. Récolte non essayée.

**0.526 : légendes locales dans les Collections** — à voir en jouant

- [x] (rapport de l'utilisateur, 2026-11-04, « legendes local ok ») Collections › Légendes locales : 36 tomes, marqués lus ou non comme dans votre personnage. Si un tome est mal marqué, la correspondance des bits est fausse : la carte se trompe aussi.
- [ ] Clic droit ou survol d'un tome : comment l'obtenir.

**0.525 : groupe sur la carte, coût du chargement** — à voir en jouant

- [x] (rapport de l'utilisateur, 2026-11-04, « groupe parfait ») Membres du groupe sur la carte : déplacement fluide quand ils sont en vue ; à distance, ils glissent entre deux positions ; une téléportation saute d'un coup.
- [ ] Après une relance du jeu : l'agent lit les lignes « Load » de dalamud.log, pour le coût d'un vrai démarrage.

**0.524 : Ctrl+Alt actualise le prix survolé** — à voir en jouant

- [x] (rapport de l'utilisateur, 2026-11-04, « ctrl alt ok ») Survoler un objet vendable, attendre le prix, Ctrl+Alt : les secondes de « Récupéré à » changent. Maintenu : une seule demande. Relâché et rappuyé : au plus une toutes les 5 s.

**0.523 : assignats des collectionnables d'un nœud** — à voir en jouant

- [x] Sur un nœud à deux collectionnables : les assignats par palier sous chacun, identiques au tableau Récolte › Collectionnables, et « le plus au palier haut » sur le bon (rapport de l'utilisateur, 2026-11-04).
- [ ] Deux objets aux types d'assignats différents : la phrase « types différents », sans marquage.

**0.522 : temps de recharge de Bénédiction de la nature** — à voir en jouant

- [ ] Juste après l'avoir lancée, le panneau de récolte sur un cristal dit « prête dans N s », et N suit le compteur de la barre du jeu. Bénédiction des Douze reste proposée.
- [ ] Le temps écoulé, elle redevient proposée.

**0.520 : tentative gratuite dans le plan des collectionnables** — à voir en jouant

- [ ] Niveau 90 et plus, PR pour Sagesse du fermier ou Raison solide : la moyenne dépasse le pire cas, et la ligne « 50 % de chances d'une tentative gratuite » apparaît.
- [ ] Sous le niveau 90 : pas de cette ligne.
- [ ] Étalage des connaissances prêt à la dernière tentative : le panneau le propose avant « Récolte ».

**0.519 : éclats, cristaux et agrégats** — à voir en jouant

- [ ] Éclat, cristal ou agrégat : le panneau propose Bénédiction de la nature et Bénédiction des Douze (0.521), sans « ≈ N objets ».
- [ ] Les autres objets gardent leur plan de PR habituel.

**0.518 : carte, tomes et niveau d'un point** — à voir en jouant

- [ ] Point d'Ostrakon Dékaokto : « ouvert, mais pas encore pour vous », le nom du tome, horloge grise. Le journal écrit « Gathering knowledge: … » : les nombres entre crochets correspondent-ils aux tomes lus ?
- [ ] Un point légendaire dont le tome est lu : « ouvert maintenant », en vert, sans ligne de tome.
- [ ] Un point au-dessus du niveau du métier : la ligne « Mineur : niv. X, ce point : niv. Y ».

**0.517 : plan de PR** — à voir en jouant

- [ ] Sur un nœud à PR pleins : le panneau propose des actions, et elles existent toutes dans votre barre.
- [ ] Une action que votre personnage n'a pas appris (par exemple un métier bas niveau) n'est pas proposée.
- [ ] Cristal : « supp. 0 % ».

**0.516 : panneaux accrochés au jeu en fenêtres Dalamud (#38)** — à voir en jouant

- [ ] Chaque panneau s'affiche au même endroit, avec le même contenu : carnet d'artisanat, projection pendant une synthèse, récolte, collectionnable, pêche, harpon, pêche en mer, survol d'objet, outil de mission, Revue de mode, Personnage, ventes des servants, demande de Triple Triade, import de banque (si la capture automatique est coupée), bouton « Trier ».
- [ ] Il suit sa fenêtre du jeu quand elle bouge, et disparaît quand elle se ferme, sans rester une image de trop.
- [ ] Les croix : projection (coupe son réglage), Revue de mode (ferme l'emplacement), pêche ouverte à la main.
- [ ] Le bouton « Trier » trie, sans fond ni bord ; le panneau de survol ne prend pas les clics.
- [ ] Échap ne ferme aucun de ces panneaux, et ils ne prennent pas le focus en apparaissant.
- [ ] Une icône ou un menu Dalamud (épingle, opacité) n'apparaît pas sur ces panneaux.

**0.515 : aide à la récolte, plan du nœud et effet du lieu** — à voir en jouant

- [ ] Nœud avec « PR ≧ 770 → tentatives +1 » (capture du 2026-11-04) : avec 809 PR, un plan d'actions apparaît, et la ligne « Effet du lieu actif : tentatives +1, déjà comptées (5 au lieu de 4) ».
- [ ] Dépenser des PR sur ce nœud : le nombre de tentatives reste à 5 (la condition lue porte sur les PR maximum).
- [ ] Un job avec moins de PR max que le seuil, ou Collecte ou Savoir-faire trop bas : la ligne « il manque N » et le rendement avec l'effet ; les tentatives lues sont sans l'effet.
- [ ] Effet de quantité ou de chance supplémentaire actif : la fenêtre du jeu montre-t-elle déjà la valeur avec l'effet ? Sinon le plan la compte trop bas.
- [ ] Cristal : « supp. 0 % » au lieu de « ? », et un plan.
- [ ] Tous les effets : sur plusieurs lieux, chaque ligne de la case « Effet du lieu » du jeu a sa ligne dans le panneau, aussi sur un collectionnable.
- [ ] Avec une liste de fabrication : « il en manque N, environ X nœuds comme celui-ci » ; le plan ne change pas selon la liste.
- [ ] Paramètres › Modules › Récolte : plus de réglage de bonus ni de secondes entre deux nœuds ; le réglage de la liste cache sa ligne.

**0.514 : panneaux libres en fenêtres Dalamud (#38)** — à voir en jouant

- [ ] Fenêtre de suivi (réglage) : s'affiche à sa place habituelle, liste les alarmes, le nom ouvre l'objet, « Carte » ouvre le point ; la croix la ferme et décoche le réglage ; recocher la rouvre.
- [ ] Tâches à l'écran : s'affiche avec une tâche marquée, cocher une tâche la fait sortir, cachée en combat ; la croix la ferme et décoche le réglage.
- [ ] Barre d'usure : même place, mêmes valeurs ; sans titre ni croix ; le réglage l'affiche et la cache.
- [ ] Barre de PR en mineur : se remplit, le repère du tic avance ; disparaît à PR pleins et sur un autre job ; PR mesurés même barre coupée.
- [ ] Ces quatre panneaux ne prennent pas le focus en apparaissant, et Échap ne les ferme pas.
- [ ] Guide et carte : la croix les ferme du premier coup.

**0.510 : conseils de Triple Triade en JcE seulement** — à voir en jouant

- [ ] Match contre un PNJ : conseils comme avant. Au Battlehall, en tournoi ou contre un joueur : pas de cadre, et le panneau dit pourquoi.
- [ ] Paramètres › Interface : l'option « Cadre des fenêtres du jeu » n'existe plus.

**0.509 : coup conseillé pendant une synthèse (A-04)** — à voir en jouant

- [ ] Dans « Comparer les prochains coups », pendant une synthèse : « Conseillé : … » change à chaque coup et suit l'état (sur Bon, un coup de cet état devrait souvent sortir, sans que ce soit une règle). Rien n'est joué. Réglage dans Paramètres › Modules › Artisanat. Pas de saccade.
- [ ] Le coup suivi jusqu'au bout donne un résultat proche de ce qui est annoncé (écart normal si l'état change).

**0.508 : ventes des servants (I-05)** — à voir en jouant, à la sonnette

- [ ] Ouvrir la liste de ventes d'un servant : le panneau « Vos ventes » s'affiche à côté, avec objets, quantités et prix comme le jeu, HQ compris.
- [ ] « Comparer aux autres vendeurs » : l'autre vendeur le plus bas et l'état de chaque ligne. Vos autres servants ne comptent pas comme concurrents. Le bouton de prix copie la valeur ; aucun prix ne change dans le jeu.
- [ ] L'agent lit ensuite les lignes `Retainer sale list` de dalamud.log : docs/evidence/0.508-retainer-sales.md.

**0.506 : vidéos et guides de la communauté (ONBOARD-001, lots 7 et 8)** — après une release

- [ ] « ? » › Vidéos et guides : la liste se lit sur `main`, donc rien avant une release : « n'a pas pu être lue ». Après une release : « pas encore de vidéo ». Avec une ligne ajoutée : le titre, l'adresse au survol, la confirmation, le navigateur.

**0.505 : guide des premiers pas (ONBOARD-001, lots 4 à 6)** — à voir en jouant

- [ ] Aide › Lancer le guide : les sept étapes, Retour, Suivant, Quitter à chaque étape ; le rouvrir reprend au même endroit ; fini, il repart du début.
- [ ] Sur l'accueil, joueur existant : une proposition unique, « Non merci » la retire pour de bon. Vraie première installation : il s'ouvre seul à la première ouverture de Tensho (à tester avec une configuration vide ou par un testeur).
- [ ] Étape des fenêtres : ouvrir une fenêtre listée, son ✓ apparaît seul. Ce qui reste figure sur l'accueil, repliable, jusqu'à être masqué.
- [ ] Le guide disparaît en combat et en mission, puis revient. La visite ouvre chaque page dans Tensho.

**0.504 : signaler un problème (ONBOARD-001, lot 3)** — à voir en jouant

- [ ] Aide › Signaler un problème : remplir, « Ouvrir le formulaire GitHub ». Le navigateur s'ouvre sur le bon formulaire, tous les champs remplis. Ne pas l'envoyer pour un essai. Le rapport est visible et modifiable avant, sans nom de personnage ni dossier.

**0.502 : aide (ONBOARD-001, lot 1)** — à voir en jouant

- [ ] Un « ? » en haut à droite de la fenêtre Tensho explique la page ouverte, sur toutes les pages ; « Toute l'aide » mène à Préférences › Aide.
- [ ] Page Aide : la recherche trouve une page d'après ce qu'elle fait ; « Ouvrir cette page » y mène ; questions fréquentes.
- [ ] 0.503 : première ouverture d'une page, un bandeau de deux lignes ; « Compris » le retire pour de bon ; « En savoir plus » ouvre l'aide. Réglage dans Paramètres › Général. Le « ? » dit ce qui est lu ou non sur ce personnage.
- [ ] Relire les textes : chaque phrase fausse est un bug. Ils ont été écrits d'après le code et les tests, sans tout rejouer.

**0.501 : sonde des fenêtres du dresseur** — à faire en jeu, mode développeur

- [ ] Ouvrir le bestiaire et cliquer sur quelques bêtes obtenues, de rangs différents. Ouvrir « Formation de l'équipe » depuis un plateau. Ouvrir la fiche d'un ennemi d'un plateau. L'agent lit ensuite les lignes `Beast window` de dalamud.log : rang de chaque bête, faiblesse des ennemis.

**0.500 : page Dresseur** — à voir en jouant

- [ ] Progression › Dresseur › Bêtes : 50 bêtes, nom, classe, attaque auto. comme le bestiaire. Rang 6 : Cu sith = FOR 161, INT 100, Rés. phys. 90, Rés. mag. 86, VIT 131. Une autre bête, à comparer aussi.
- [ ] Le compteur « Bêtes obtenues » égale celui du bestiaire du jeu (35/50 sur la capture), et ✓ / ✗ suivent. Tri par colonne, recherche, classe et « Obtenues seulement ».
- [ ] Plateaux : niveau, équipe et rang comme « Disposition du plateau » ; les bonus comme « Liste des bonus ».
- [ ] « Faible : … » : comparer à la fiche d'un ennemi dans le jeu. **Lu comme tel, jamais vérifié.** Formes et cibles des actions aussi.

**0.499 : la carte au-dessus de Tensho** — à voir en jouant

- [ ] Depuis une page Tensho, cliquer un lien vers la carte (« Voir sur la carte », favori d'une autre carte) : la carte passe devant Tensho, qu'elle soit déjà ouverte ou non. Cliquer ensuite sur Tensho le remet devant.

**0.498 : Tensho replié, puis demandé** — à voir en jouant

- [ ] Replier la fenêtre Tensho par sa barre de titre, puis ouvrir quelque chose qui la demande (clic droit sur un objet › outil, `/tensho`, alarme) : elle se déplie sur la bonne page. Elle se replie encore normalement ensuite.

**0.497 : état de la synthèse dans « Comparer les prochains coups »** — à voir en jouant

- [ ] En haut de la fenêtre : l'état du jeu et son effet. Sur Bon ou Excellent, Touche précise, Synthèse intensive et Ficelles du métier (noms du jeu selon votre langue) passent en tête, marqués « Seulement dans cet état ». Sur Mauvais, la ligne le dit.
- [ ] Mode développeur : le journal donne `Craft condition …: game status …` à chaque état. Si un de ces coups manque sur Bon, cette ligne dit ce que le jeu a répondu.

**0.494 : auto-contrôle** — à voir en jouant

- [ ] Mode développeur : quelques secondes après un rechargement, le journal contient les lignes `Selftest …` et un résumé `Selftest done (auto)`. Sans saccade visible.
- [ ] `/tensho selftest` et le bouton de Diagnostics relancent une passe ; Diagnostics montre les lignes.
- [ ] 0.496 : fenêtre ouverte puis refermée, sans lancer de passe : la passe suivante donne OK_SEEN avec l'heure. Ça reste après un rechargement.
- [ ] 0.495 : carnet d'artisanat ouvert, la ligne « crafting log » donne une recette ; synthèse en cours, « synthesis » passe en OK ; point de récolte ouvert, « gathering node » aussi.

**0.493 : disponibilité des PNJ de Triple Triade** — à voir en jouant

- [ ] Triple Triade › PNJ et cartes : colonne « Disponible ». Un PNJ marqué « Quête requise » refuse bien le défi ou n'est pas là en jeu, et un PNJ marqué « Oui » accepte. Le survol donne les quêtes, avec ✓ pour celles faites. À signaler surtout : un PNJ à deux quêtes dont une seule est faite (lecture « toutes » déduite, voir docs/evidence/0.493-triad-npc-requirements.md).
- [ ] « Masquer les PNJ pas encore disponibles » les retire ; clic droit sur la carte › Triple Triad : même mention, ces PNJ en dernier.

**0.491 : R-14, valeurs inconnues** — à voir en jouant

- [ ] Défi Triple Triade et match : règles et conseils comme avant, jamais « les règles n'ont pas pu être lues ». Le journal donne la ligne `Triad request rule values` (preuve à verser dans docs/evidence, défis à 1, 2 et 4 règles si possible).
- [ ] Coffre à apparence ouvert avec une tenue complète : ses pièces comptent toujours comme possédées. Avec une tenue partielle : seules les pièces rangées comptent. Le journal donne `Dresser outfit … slots held` (preuve à verser dans docs/evidence).
- [ ] Carte : panoramas et courants éthérés comme avant.

**0.490 : onglet Favoris de la carte** — pendant la campagne de tests en jeu (étape h)

- [x] Clic droit sur la carte Tensho : « Favoris » est le premier onglet, avant Quêtes et ALÉA. Les favoris de cette carte d'abord, puis les autres. Un clic pose le drapeau ou ouvre l'autre carte ; « Retirer » enlève le favori et son étoile.

**0.489 : temps d'affichage de la carte** — pendant la campagne de tests en jeu (étape h)

- [x] Mode développeur, carte Tensho ouverte : Diagnostics a une ligne « Map », et elle reste une fois la carte fermée.

**0.488 : clignotement, Raison solide, temps avant le plein** — pendant la campagne de tests en jeu (étape h)

- [x] Panneau Collectionnable : il ne clignote plus au début ni entre deux actions.
- [x] Raison solide : conseillée seulement à la dernière tentative, puis le jeu l'accepte. Sinon le panneau dit d'attendre.
- [x] Barre de tic : quelques secondes après la remontée des PR, le temps avant le plein correspond à l'attente réelle. Paramètres › Modules › Récolte donne la base mesurée et les traits de chaque métier ; comparer sur deux métiers.
- [x] Aides : une action grisée dans la fenêtre du jeu ou non apprise par le métier n'est jamais conseillée (Toucher d'amorçage sous le niveau 95, action de quête non faite).

**0.486 : barre de tic** — pendant la campagne de tests en jeu (étape h)

- [x] La barre se remplit vers les PR pleins, une petite marque dorée défile en bas à chaque tic, le texte donne le temps avant le plein. Comparer ce temps à l'attente réelle. Tant que les PR par tic ne sont pas mesurés, le texte reste « tic dans… ». La bulle donne le prochain tic.

**0.486 : temps d'affichage des panneaux** — pendant la campagne de tests en jeu (étape h)

- [x] Mode développeur, Diagnostics : la section « panneaux à côté du jeu » liste le panneau de récolte, Collectionnable et les autres une fois ouverts, et leurs temps restent après la fermeture (0.487).

**0.484 : risque accepté** — pendant la campagne de tests en jeu (étape h)

- [x] Barre de tic : à l'ouverture d'une fenêtre de récolte, après quelques secondes, elle affiche « pause » ; en la quittant, elle repart. Paramètres › Modules › Récolte montre les PR par tic mesurés.
- [x] Carte Tensho ouverte, plugin rechargé : elle revient seule, sans M deux fois. Carte du jeu hors combat : pas d'autre effet.
- [x] Panneau Collectionnable : « Calcul du plan… » apparaît à peine, le jeu ne se fige pas au changement de curseur.

- [x] Panneau Collectionnable, curseur « Risque accepté » : à 0 %, plus de Sélection instinctive sans nécessité et un « au pire » élevé ; à 100 %, le plan vise la meilleure moyenne. Le réglage reste après rechargement, et existe aussi dans Paramètres › Modules › Récolte.

**0.483 : Raison solide selon le jeu** — pendant la campagne de tests en jeu (étape h)

- [ ] Plan avec Raison solide : quand les tentatives sont sous le maximum et que le jeu la refuse, elle n'est plus conseillée ni comptée. Mode développeur : la ligne `Collectable restore action` du journal donne la réponse du jeu.

**0.482 : ordre des actions du plan** — pendant la campagne de tests en jeu (étape h)

- [x] Début de passe avec des PR : Inspection rigoureuse est conseillée d'abord si le plan en a besoin. Le gain affiché est le gain réel, pas celui de la case du jeu.

**0.481 : Inspection, Œil et Intuition comptés** — pendant la campagne de tests en jeu (étape h)

- [x] Cocher une fois « Avec Inspection rigoureuse » et « Avec Intuition » : les deux avertissements disparaissent et le plan change. Les cases peuvent rester cochées, le conseil ne change pas.
- [x] Inspection rigoureuse conseillée quand elle rapporte plus ; après usage, l'action suivante n'est pas une deuxième Inspection et le gain affiché la contient.
- [x] Œil du collectionneur conseillé quand il rapporte plus ; après usage, le pourcentage d'Intuition du jeu monte et il n'est plus conseillé.
- [x] Panneau plus court, méthode dans la bulle de « Comment est-ce calculé ? ».

**0.480 : bonus dans le plan de collection** — pendant la campagne de tests en jeu (étape h)

- [x] Cocher « Avec Inspection rigoureuse » puis décocher, même chose pour « Avec Intuition » : les messages disparaissent, le plan ne change pas tant qu'une case reste cochée.
- [x] Plan avec Inspection : elle est conseillée seulement si elle rapporte plus ; les PR annoncés sont justes. Une fois utilisée, le gain affiché la contient et le conseil suivant passe à l'action.
- [x] Sélection méthodique à 8 % : plus conseillée pour son pourcentage. Comparer les conseils à votre expérience, et me donner les chiffres du panneau si l'un paraît faux.
- [x] Plus de plantage en mode collectionnable, y compris à la valeur maximale avec une Sélection méthodique.
- [x] Référence affichée : palier haut, assignats, ce qui manque. À la valeur voulue avec des PR restants : Raison solide conseillée pour une récolte de plus, et le jeu l'accepte.
- [x] Œil du collectionneur : proposé en option quand une Intuition atteindrait le palier suivant.
- [x] Mode développeur : le journal `Collectable window values` contient le pourcentage d'Intuition.

**0.479 : plan de collection, PR et bonus de nœud** — pendant la campagne de tests en jeu (étape h)

- [x] Fenêtre Collectionnable : barre de collectabilité juste, palier juste, prochaine action avec icône et gain ; le plan se refait après chaque action. Comparer les gains affichés à ceux du jeu.
- [x] Plus aucune chance d'atteindre le seuil bas (par exemple 0 avec une seule tentative) : le panneau le dit, sans parler de tirage.
- [x] Bien noter si un conseil paraît mauvais, avec les chiffres du panneau.
- [x] Liste de fabrication : nœud normal, « récolte simple » et PR gardés ; un nœud avec une tentative ou une quantité de plus : « Bonus majeur », les PR se dépensent. Décocher le réglage redonne le partage égal.

**0.478 : retours du lot 6, récolte** — pendant la campagne de tests en jeu (étape h)

- [x] Fenêtre Collectionnable sur « Minerai de durium noble collectionnable » : nom complet, seuils 400 / 700 / 1000. Rien à faire de plus. En mode développeur, faire quelques actions : le journal note les valeurs de la fenêtre.
- [x] Aide à la récolte : plus de texte coupé ; le nom, puis les taux dessous. Sur un collectionnable : pas de « plan non calculé » mais un message clair.
- [x] Récolte › onglet Collectionnables : tableau trié en cliquant sur chaque colonne, dans les deux sens.
- Plan réparti sur la liste de fabrication : retiré en 0.515 (DEC-036) ; la ligne « il en manque N » reste.
- [x] Paramètres › Modules › Récolte : actions interdites jamais proposées (panneau et page PR) ; PR par tic mesuré sur un nœud quand les PR remontent.

**0.476 : thème proposé par un outil** — TEMPLATES-001

- [x] Éditeur : choisir un thème proposé, enregistrer. Le thème ne change pas tant qu'on n'a pas cliqué « Utiliser le thème… ». Ensuite, en haut de Mes outils, « Revenir au thème d'origine » remet l'ancien (0.477).
- [x] Coller un outil avec `"settings":{"theme":"light"}` : l'aperçu le dit. Avec un autre réglage : refusé.

**0.475 : galerie de Mes outils** — TEMPLATES-001

- [ ] Après la prochaine release (la galerie est lue sur `main`) : Galerie › Charger la liste, Voir, l'aperçu, Installer. Avant cette release, le message « n'a pas pu être lue » est normal.
- [ ] Sans clic, aucune requête : rien dans le journal au démarrage du plugin.
- [ ] Remerciements : la ligne des outils communautaires est présente.

**0.474 : éditeur de Mes outils** — TEMPLATES-001

- [x] Créer un outil : ajouter chaque type de bloc, monter, descendre, retirer. Enregistrer un outil incomplet donne la raison sans rien perdre.
- [x] Bloc d'objets : sélectionner un objet dans la page Objets, revenir, « Ajouter… », régler la quantité.
- [x] Modifier un outil installé : les autres langues restent, la progression repart de zéro après enregistrement.

**0.473 : Mes outils (modèles sans code)** — TEMPLATES-001

- [x] « Mes outils » est juste sous l'accueil. Ajouter un outil : coller un JSON, Vérifier, l'aperçu montre tout, Installer.
- [x] Refus avec raison : champ inconnu, type de bloc inconnu, lien http, texte quelconque.
- [x] Cases, compteur, minuteurs : cocher, recharger le plugin, l'état revient ; une case quotidienne se décoche à la remise.
- [x] Bloc d'objets : icône, bulle, voulu, possédé, manque ; un identifiant inconnu est signalé.
- [x] Lien : le clic demande confirmation et montre l'adresse complète. Rien ne s'ouvre sans « Ouvrir ».
- [x] Copier pour partager, coller la ligne dans un autre personnage ou après retrait : même outil, progression à zéro.

**0.470 : liste unique des pages** — pendant la campagne de tests en jeu (étape h)

- [x] Navigation : mêmes groupes, mêmes pages, même ordre qu'avant ; chaque page s'ouvre. Artisanat reste sélectionné sur Synthèse en cours et Séquences, Récolte sur PR, Collection sur Magie bleue. Diagnostics seulement en mode développeur.

**0.469 : demande du jour de la Grande Compagnie, icônes du carnet** — pendant la campagne de tests en jeu (étape h)

- [ ] Ouvrir le calendrier du jeu puis les missions de ravitaillement : la bulle d'un objet demandé dit « demandée aujourd'hui ». Un autre objet possible dit « absente des demandes du jour que vous avez ouvertes ». Le journal Dalamud donne une ligne `Grand Company supply window`.
- [ ] Après 20:00 UTC, ou avec un autre personnage : retour au message qui invite à ouvrir la fenêtre.
- [ ] Carnet : panneau en sections (recette, votre état, cible, calcul, macro), chiffres en tableau, barre de qualité verte si la cible est atteinte ; explications dans la bulle de « (?) ».
- [ ] Carnet : Manipulation est une case et l'icône du skill, sans texte ; la bulle dit ce qui est détecté. Repas et remède : icône et bulle de l'objet, « aucun » pour un repas sans bonus d'artisanat.

**0.468 : carnet (cible, état, meilleure séquence) et cache de Raphael** — pendant la campagne de tests en jeu (étape h)

- [ ] Raphael choisi, carnet : une recette calculée puis re-sélectionnée revient tout de suite, le tampon dit « Cache disque ». Après un changement d'option de macro ou une nouvelle version de la DLL : « Solveur : Raphael (séquence déjà calculée…) ».
- [ ] Simulateur › Recherche avec Raphael : relancer la même recherche répond tout de suite, avec « déjà calculée » et un bouton « Recalculer ». La séquence calculée dans le carnet y est retrouvée à attributs et cible égaux.
- [ ] Carnet : « Qualité cible » change le calcul quand on quitte le champ, « Maximum » la remet, elle revient pour cette recette après rechargement. Recette à collectionner : boutons des paliers.
- [ ] Carnet : la ligne « Lu dans le jeu » donne repas, remède et spécialiste justes ; la case Manipulation et son « détectée : » sont justes pour ce métier. La case corrige le calcul si la détection se trompe (le journal Dalamud donne une ligne `Manipulation for job`).
- [ ] Carnet : « Enregistrer comme meilleure séquence », puis la séquence apparaît dans la section enregistrée et rien n'est calculé ; une cible plus haute relance la recherche ; « Recalculer » aussi.
- [ ] Réglages › Modules › Artisanat : la case de la fenêtre « Comparer les prochains coups » ; pendant une synthèse, la croix de la fenêtre la ferme et décoche le réglage.

**0.467 : Raphael dans le carnet** — pendant la campagne de tests en jeu (étape h)

- [ ] Raphael choisi : le panneau du carnet affiche « Solveur : Raphael » et sa proposition ; changer vite de recette n'accumule pas de calculs et le jeu reste fluide ; retour à Tensho : le panneau suit.

**0.465 : solveur Raphael** — pendant la campagne de tests en jeu (étape h)

- [x] Artisanat › Recherche : choisir « Raphael », lancer : une séquence arrive, « Annuler » fonctionne ; le jeu reste fluide pendant le calcul.
- [ ] La séquence jouée en jeu donne le résultat annoncé. Retour à « Tensho » : la recherche d'avant.
- [ ] Le plugin se recharge sans erreur après un calcul.

**0.464 : retours du lot 5** — pendant la campagne de tests en jeu (étape h)

- [ ] Simulateur, métier porté : attributs copiés d'office, ligne « Détecté » juste (repas, remède, Manipulation, spécialiste) ; valeurs en vert ou rouge selon la recette ; « Conserver la séquence » après la palette.
- [ ] Onglet « Liste de fabrication » : icônes, infobulle complète au survol, manquants en rouge.
- [ ] Pendant une synthèse : la fenêtre de projection s'ouvre à côté de celle du jeu, se ferme avec elle, et ses chiffres correspondent au résultat de l'action jouée.

**0.462 : retours du lot 3** — pendant la campagne de tests en jeu (étape h)

- [ ] Survol d'une pièce d'équipement : la comparaison est lisible, les statistiques en grille.
- [ ] Clic droit sur un objet › Tensho : plus de « Tous les outils avec cet objet » ; le reste du menu est là.
- [x] Ouvrir le coffre de compagnie, puis tous ses onglets : Banque › Coffre de compagnie liste les objets, plus de message « ordre visuel » (0.463) ; les quantités sont justes.

**0.461 : grille du journal** — pendant la campagne de tests en jeu (étape h)

- [x] Monde › Jardinage : le potager est une grille ; la plante est dans la bonne case par rapport au vrai potager (emplacement 6 en bas au milieu) ; sinon, dire où elle est.
- [ ] « Entretenu » et « Retirer » marchent depuis la case ; plus de « Replanter ». Les pots restent en liste.

**0.460 : plantations détectées depuis le chat** — pendant la campagne de tests en jeu (étape h)

- [x] Planter dans un potager : une ligne « Jardin : … noté dans le journal » arrive ; la page Jardinage montre le bon potager, le bon emplacement, la bonne graine et les durées.
- [ ] Replanter au même emplacement : la ligne est remplacée, pas doublée. Un pot : une ligne « pot ».
- [ ] Un joueur qui écrit la même phrase dans le chat n'ajoute rien. Réglage décoché : plus rien n'est noté.
- [ ] Entretenir et récolter : envoyer le résultat à l'agent, qui lit les lignes « Garden chat capture » de dalamud.log pour trouver leurs messages.

**0.459 : icône des terminés, page Jardinage** — pendant la campagne de tests en jeu (étape h)

- [x] Barre du serveur : trois icônes au plus : horloge (alarmes actives), rappels (reste à faire), terminés (visible seulement s'il y en a) ; chaque infobulle a sa liste.
- [x] Monde › Jardinage : le journal et les croisements y sont, ils ne sont plus dans Rappels ; les parcelles déjà notées sont toujours là.

**0.458 : icône des rappels, ce qui reste à faire** — pendant la campagne de tests en jeu (étape h)

- [x] Barre du serveur : le nombre et l'infobulle « Reste à faire » listent les rappels non cochés ; cocher « Fait » sur la page Rappels fait baisser le nombre (5 s au plus).
- [x] Décocher « Barre » sur une ligne : elle disparaît de l'infobulle ; recocher : elle revient ; le choix reste après un rechargement du plugin.
- [ ] Une tâche personnelle affichée à l'écran apparaît dans la liste ; une tâche servant ou un vaisseau fini aussi.

**0.457 : icônes des compétences** — pendant la campagne de tests en jeu (étape h)

- [x] Réglages › Artisanat › Actions interdites : chaque ligne a son icône, et la case se coche toujours.
- [ ] Enregistreur d'artisanat : les actions notées s'affichent en icônes dans l'ordre ; le survol donne le numéro et le nom ; la ligne revient à la ligne si besoin.

**0.456 : plan de plantation des croisements** — pendant la campagne de tests en jeu (étape h)

- [x] Croisements › « Ce qui donne cette graine » : les paires à « seulement cette graine » sont en tête ; le survol donne les autres graines.
- [x] Clic sur une paire : grille 3 × 3 sans centre ; la disposition correspond à votre potager de luxe (sinon, dire comment il est) ; noms des graines et de la terre en français.
- [ ] Planter une paire selon le plan : des graines croisées apparaissent à la récolte, sauf sur la parcelle 1.

**0.455 : retours du lot 1** — pendant la campagne de tests en jeu (étape h)

- [x] Journal de l'or (Stockage › Gils) : tableaux lisibles, plus de « = » ; une dépense apparaît en « - ».
- [x] Barre du serveur : deux icônes, l'horloge des alarmes (nombre d'alarmes actives) et celle des rappels (nombre de terminés) ; survol = la
  liste ; clic = la bonne page ; le réglage Avancé masque celle des rappels.
- [ ] Mission ou entraînement d'escouade lancé : une alarme automatique apparaît dans la page Alarmes et sonne à la fin ; l'icône des rappels monte de 1.

**0.454 : R-14, indisponible plutôt que « lecture »** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Titre par classe : réglé sur un personnage ; sur un autre qui ne le possède pas, changer de classe ne change pas le titre.
- [ ] Objets de collection et carnets : affichés comme avant, sans message d'indisponibilité.
- [ ] Carte Tensho à la place de la carte du jeu : fonctionne comme avant.
- [x] Inventaire d'un servant : l'ordre affiché suit toujours celui du jeu.

**0.453 : R-14, valeurs inconnues** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Journal de l'or : à la connexion, aucune fausse entrée « + tout l'or » ; un achat est bien noté.
- [ ] Rappels à l'écran titre ou de sélection : pas de zéros ni de rôle ; une fois connecté, les compteurs apparaissent.
- [x] Prix de sous-cotation : après une sonnette, le prix est copié ; sans aucune sonnette depuis le lancement du jeu, rien n'est copié.
- [x] Revue de mode : les catégories s'affichent comme avant.
- [ ] Téléportation : coûts affichés ; mandats, collections supplémentaires et chocobo lus une fois connecté.

**0.452 : R-14 et hooks (A-14)** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Alarme sur un objet : la laisser sonner deux fois de suite ; la ligne de chat, le son et Discord arrivent à chaque sonnerie.
- [ ] Enregistreur d'artisanat sur une classe autre que Charpentier : Veneration, Innovation, Great Strides, Waste Not, Manipulation et Final
  Appraisal sont notés ; une action lancée pendant l'animation (mise en file) est notée une seule fois ; une action refusée ne l'est pas.
- [ ] Synthèse en direct : la projection s'affiche maintenant (elle ne tournait pas avant) ; comparer ses chiffres à la synthèse.
- [ ] Chaîne de macros : /macrolock pendant une chaîne, puis lancer une autre macro : la chaîne continue.
- [ ] Try On depuis le menu d'un objet avec un personnage chargé ; à l'écran de sélection du personnage, rien ne se passe.
- [x] Musique en cours (orchestrion) et liste des cartes de l'éditeur de Triple Triade toujours lues.

**0.451 : N-06 missions d'escouade (sonde en jeu)** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Hors de la caserne : « Lire l'escouade » affiche « L'escouade n'est pas chargée… ».
- [x] À la caserne, escouade ouverte : « Lire l'escouade » liste les 8 membres (classe, niveau, « occupé » pour ceux en mission ou à
  l'entraînement) et le bonus d'entraînement ; comparer avec la fenêtre du jeu.
- [x] Attributs d'un membre : comparer « P / M / T » affiché avec la fiche du membre dans le jeu (classe, niveau). Un écart prouverait que
  les colonnes de GcArmyMemberGrow sont mal lues, ou que les affinités ou l'équipement comptent.
- [x] Choisir la mission de la fenêtre du jeu, puis le jeu d'exigences qui correspond à ses valeurs ; former le groupe proposé dans le
  jeu et comparer ses totaux et le taux de réussite affiché par le jeu avec « ≈ … % ».
- [x] « Écrire dans le journal » : copier les lignes « Squadron probe: » du journal de Dalamud dans docs/evidence/ (sonde N-06).

**0.450 : N-14 croisements** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Rappels › Journal de jardinage › Croisements : « Lecture des paires… » puis la liste ; les graines sont nommées dans la langue
  du jeu ; « Ce qui donne cette graine » et « Ce que cette graine peut donner » montrent des paires cohérentes avec un croisement connu.
- [x] La ligne grise annonce 3 768 paires et 1 nom écarté (« Gysahl Green Seeds » dans l'infobulle) ; le journal de Dalamud le dit aussi.

**0.449 : liste des graines du journal de jardinage** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Rappels › Journal de jardinage › « Liste des graines » : les noms sont dans la langue du jeu ; la recherche filtre ; choisir une
  graine remplit son nom, la pousse et l'entretien (par exemple 120 h et 48 h pour les graines de paprika) ; les valeurs restent modifiables.
- [ ] Comparer une graine plantée en jeu avec l'heure de récolte affichée (données Lotlab, non vérifiées par Tensho).

**0.448 : N-08 journal de jardinage** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Rappels › Journal de jardinage : ajouter une parcelle (lieu, graine, pousse 1 h, entretien 0) : « Mûr le … » une heure plus tard ;
  à l'heure dite, une seule ligne « Jardin : … est mûr » dans le chat ; « Replanter » repart de zéro ; « Retirer » l'enlève.
- [ ] Avec un entretien (par exemple 1 h sur une pousse de 3 h) : la ligne « entretenir » arrive une fois ; « Entretenu » repousse le suivant.
- [ ] Redémarrer le jeu après un rappel : il n'est pas répété.

**0.447 : décisions du 2026-11-03 (DEC-026)** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Train de chasse : « Envoyer » est grisé tant qu'aucun drapeau n'a été posé depuis le train ; après « Drapeau suivant », choisir
  /p (en groupe) puis « Envoyer » : une seule ligne dans le chat du groupe, avec le bon nom, le bon rang, la bonne zone, et <flag> devenu
  le lien du drapeau ; un second clic avant 5 s ne fait rien.
- [x] Page Événements : « Événements de joueurs (partake.gg) » ouvre le site dans le navigateur.
- [x] Réglages : la case « Chercher les monstres sur le wiki au survol » n'existe plus ; le panneau au survol montre toujours ses sources.
- [x] Tri : déplacer un objet dans les sacs pendant que la page Tri est ouverte : l'éditeur de règles reste affiché (ligne « Rangement
  modifié »), l'aperçu revient tout seul.

**0.446 : N-09 alerte des marques, X-13 annonce** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Cartes › Train de chasse : cocher « Annoncer les marques A et S proches » ; près d'une marque A ou S, une seule ligne dans le
  chat (nom, rang, zone, coordonnées justes) et le son des alarmes s'il est activé ; une marque B ne dit rien.
- [ ] Avec au moins une étape : « Copier l'annonce », coller dans le chat du jeu après avoir posé le drapeau : <flag> devient le lien.

**0.445 : N-05 terrains en vente** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Cartes › Terrains en vente › « Consulter » sur un monde NA : la liste correspond au jeu (panneau de vente des quartiers) ; cocher
  la surveillance : une parcelle nouvellement libre est annoncée dans le chat à la lecture suivante.

**0.444 : X-09 annuaire des lieux** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Cartes › Annuaire : ajouter une maison (monde, quartier, secteur, parcelle), un appartement ; « Copier » ; le lieu reste après
  un rechargement du plugin et après « Tout remettre par défaut ».

**0.443 : N-02 tâches du Livre d'or** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Rappels › Livre d'or : la liste « À faire » correspond aux cases du journal ; une tâche finie passe en « à réclamer » (vert), puis
  disparaît une fois la vignette posée.

**0.442 : N-15 train de chasse** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Pendant un train : « Enregistrer », chaque marque A/S apparaît une fois avec sa position ; « Drapeau suivant » ; « Copier la
  liste » puis coller dans l'import des favoris : les points tombent au bon endroit.

**0.441 : N-09 marques de chasse** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Près d'une marque B, A ou S : elle apparaît sur la carte Tensho avec la bonne couleur et son rang ; morte, elle disparaît ;
  décocher la couche la masque.

**0.440 : N-01 contrats de chasse** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Avec un contrat de chasse en main : Rappels montre ses cibles et les victimes (comparer au journal des contrats du jeu) ; tuer une
  cible fait monter le compteur ; « Carte » ouvre la bonne zone.

**0.439 : N-03 probabilités du Livre d'or** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Livre d'or avec 3 à 8 vignettes : Rappels affiche les chances de lignes, actuelles et après mélange ; poser une vignette change
  les chances ; à 9 vignettes, plus rien.

**0.438 : actions interdites, équiper depuis le carnet** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Interdire Œil de l'expert et Manipulation : ni la recherche, ni l'assistant du carnet, ni la synthèse en cours ne les proposent ;
  grisées dans le simulateur. Carnet ouvert avec le mauvais métier : « Équiper (métier) » change d'ensemble et le calcul démarre.

**0.437 : N-11 coordonnées collées** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Coller une liste de Siren ou Faloop dans l'import des favoris : un favori par ligne, au bon endroit sur la carte ; lieux inconnus
  comptés dans le message.

**0.436 : A-14 enregistreur de rotation** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] « Enregistrer », fabriquer à la main (avec un buff, une action de qualité, une de progression), « Arrêter » : la liste est
  exacte et dans l'ordre ; « Utiliser comme séquence » ; le jeu se comporte comme d'habitude pendant l'enregistrement (aucune action
  bloquée) ; après « Arrêter », rien n'est plus noté.

**0.435 : A-07 recettes expertes** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Recette experte : le simulateur l'accepte et affiche « chaque étape en condition normale ». Pendant une synthèse experte en jeu,
  mode développeur : noter la condition brute affichée pour souple, robuste, malléable, amorcée (pour ouvrir la projection en direct).

**0.434 : A-13 actions du simulateur** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Comparer au jeu : Appréciation finale (progression arrêtée à 1 de la fin), Synthèse intensive / Toucher précis après Cœur et âme
  (spécialiste), Innovation éclair, Ficelles du métier (+20 PS). Synthèse en cours avec Appréciation finale active : projection affichée.

**0.433 : A-05 listes de fabrication** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Créer une liste, y ajouter deux recettes depuis Préparation, changer une quantité, retirer un objet ; les ingrédients totaux et
  « à compléter » sont justes ; la liste est toujours là après un rechargement du plugin ; lien Teamcraft de la liste.

**0.432 : A-15 collectionnables des sacs** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Avec des collectionnables dans les sacs (récoltés et fabriqués) : « Dans vos sacs » montre leur collectionnabilité comme le jeu,
  le bon palier et les assignats du marchand.

**0.431 : A-06 bénéfice d'artisanat** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Préparation › « Consulter les prix de la liste » : le prix de l'objet fabriqué apparaît dans « Bénéfice », avec la dernière vente ;
  case « Prix du centre de données » : autre monde affiché ; bénéfice = prix × quantité − achats.

**0.428-0.429 : recette refusée par le carnet du jeu** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Carnet d'artisanat déjà ouvert sur une autre recette (ou un autre métier) : « Ouvrir dans le carnet » le ferme et le rouvre sur la
  bonne recette, sans message rouge. Objet fabriqué par plusieurs métiers : la recette du métier porté est prise.

- [ ] 0.430 : une recette un peu au-dessus du niveau du métier, visible dans le carnet du jeu, s'ouvre aussi depuis Tensho. Si le
  message rouge revient, noter le message complet (capture).

**0.427 : A-12 prérequis de la recette** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Page Artisanat : niveau, Habileté et Contrôle en vert / rouge selon le personnage ; métier non porté : « ? » et « Équiper (métier) »
  porte le bon ensemble.

**0.426 : fermer Tensho en ouvrant une fenêtre du jeu** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] « Ouvrir dans le carnet d'artisanat » : Tensho se ferme, le carnet s'ouvre sur la recette ; recommencer avec le carnet déjà
  ouvert : il reste ouvert et passe à la recette. Même chose pour le carnet de récolte et la carte du jeu. Réglage décoché : Tensho
  reste ouvert. « Y aller » : Tensho reste ouvert (question de téléportation).

**0.425 : A-08 import de macro** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Simulateur › « Importer une macro » : coller une macro du jeu ou de Teamcraft (en français), « Importer » ; la séquence est
  reprise et simulée ; une action mal écrite apparaît dans « Noms inconnus ».

**0.424 : Préparation, stocks et « Retour à l'objet »** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Ouvrir Artisanat directement (menu d'un objet) : la colonne « Recensé NQ + HQ » montre les stocks ; « Retour à l'objet » ouvre
  la fiche de l'objet de la recette.

**0.423 : lien Artisanat et A-09 Teamcraft** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Menu d'un objet › « Artisanat : (objet) » : la page s'ouvre sur l'onglet Préparation de sa recette. Préparation › « Ouvrir dans
  Teamcraft » : Teamcraft propose d'importer la liste avec l'objet et la quantité.

**0.422 : A-10 Macro Chain** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Macro Chain chargé, macros enchaînées : la dernière ligne de chaque page est « /nextmacro » et la suite s'exécute ; Macro Chain
  désactivé : retour au marqueur de Tensho.

**0.421 : A-11 recettes jamais fabriquées** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Fiche d'un objet › Craft lié : une recette déjà faite montre « ✓ déjà fabriquée », une jamais faite « jamais fabriquée » (comparer
  au carnet du jeu) ; livre des recettes, case « Jamais fabriquées seulement ».

**0.420 : A-02 repas et remède** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Simulateur : choisir un repas HQ et un remède ; les attributs affichés correspondent à ceux du personnage une fois le repas
  et le remède pris en jeu (sans autre bonus).

**0.419 : A-01, X-11, X-12 (artisanat)** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Recette d'un collectionnable : boutons « Bas / Moyen / Haut » dans la recherche, la rotation trouvée atteint le seuil en jeu.
  Assistant du carnet avec Manipulation apprise : la macro proposée peut l'utiliser. Réglage « Attente ajoutée aux buffs » : les
  lignes de buffs de la macro changent seules.

**0.418 : R-07 pêche en mer** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Pendant une traversée : le panneau montre l'escale, le moment, les poissons avec appât et touche ; pendant un courant spectral,
  le message et les poissons spectraux ; missions avec avancement ; points ; escale suivante. Comparer à l'interface du jeu.

**0.417 : R-15 aide au harpon** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Pêche au harpon : le panneau est à côté de la fenêtre ; pour chaque poisson, la taille et la vitesse ; le bon poisson de la zone
  en couleur. Mode développeur : noter la vitesse brute et la comparer à la vitesse affichée (valeurs GatherBuddy 100 à 600).

**0.416 : R-14 liste de récolte par zone** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Artisanat › Préparation d'une recette à plusieurs matières récoltées › « Calculer la chaîne » › « Liste de récolte par zone » :
  zones dans l'ordre, une ligne par matière, horaire, « Y aller » ; matières achetées listées à part.

**0.415 : R-13 fenêtre de suivi** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Paramètres › « Fenêtre de suivi épinglée » : la fenêtre liste les objets des alarmes actives, la plus proche en premier ;
  le temps restant baisse ; clic sur un nom ; « Carte » ; la croix la ferme ; masquée quand l'interface du jeu est cachée.

**0.414 : R-12 alarme cliquable** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Une alarme Tensho d'objet récolté sonne : dans le chat, le lien de carte ouvre la carte du jeu sur le point ; « Ouvrir dans
  Tensho » ouvre la fiche de l'objet (onglet Récolte). Une alarme sans objet : message sans lien.

**0.413 : R-11 tableau des collectionnables** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Page Récolte › « Collectionnables » : comparer quelques lignes (seuils, assignats) au marchand d'assignats du jeu ; filtre
  « Nv 91 » ; clic sur un objet ; vue Récolte d'un objet collectionnable : ses seuils sous son nom.

**0.411 : R-10 équiper le métier** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Fiche d'un objet récolté › lieux : « Équiper Mineur » (ou Botaniste, Pêcheur) porte l'ensemble de ce métier au plus petit numéro ;
  sans ensemble pour ce métier, pas de bouton. 0.412 : réglage « Y aller : équiper aussi le métier » coché, « Y aller » équipe aussi.

**0.410 : R-09 optimiseur PR** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Outil PR et panneau de récolte : avec beaucoup de PR, le plan propose Raison solide (ou Sagesse du fermier) et Vision claire
  quand elles rapportent ; au niveau 90 et plus, la ligne Eurêka s'affiche ; décocher la case de l'outil PR redonne l'ancien plan.

**0.409 : R-06 collectionnables** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Ouvrir la fenêtre Collectionnable sur un point (mineur ou botaniste) : le panneau « Collectionnable » est à côté, avec l'objet,
  PR et intégrité, les seuils bas/moyen/haut et les assignats par palier comparés au jeu (marchand d'assignats). Mode développeur :
  noter l'index des valeurs brutes qui suit la collectabilité à chaque action (capture), pour l'étape suivante (action conseillée).

**0.408 : préparation dans Artisanat** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Objets › Craft lié : recette et ingrédients seulement ; « Préparer dans Artisanat » ouvre l'onglet Préparation sur la recette ;
  calculer la chaîne, consulter les prix ; cliquer un ingrédient ouvre sa fiche dans Objets ; « Retour au carnet » revient.
  Alarmes et Vos personnages : « Tâches des servants ».

**0.407 : noms du wiki et de Garland dans la langue du jeu** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Client français : page Monstres (sources du wiki) et grimoire du mage bleu › les monstres et lieux connus du jeu sont en français,
  les coordonnées gardées ; survol d'un matériau lâché par un monstre et lignes Garland (quête, PNJ, ALÉA) en français ; un nom
  inconnu du jeu reste en anglais. Client anglais : rien ne change.

**0.406 : règle des langues** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Plugin en anglais, mode développeur coché : les messages et libellés de développeur sont en anglais ; plugin en français : en français.

**0.405 : carnets du jeu** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Clic droit sur un objet artisanal dans Tensho › « Ouvrir dans le carnet d'artisanat (job) » : le carnet du jeu s'ouvre sur la
  recette ; sur un objet récolté, « Ouvrir dans le carnet de récolte » l'ouvre sur l'objet. Même chose avec « Carnet » (page Récolte)
  et « Ouvrir dans le carnet d'artisanat » (page Artisanat).

**Étape g) — 0.403 : R-08 alerte PR** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Page PR › cocher « Alerte PR au seuil » (seuil 0) ; dépenser des PR en récolte puis attendre : un message et le son quand les
  PR sont pleins, une seule fois ; seuil 500 : l'alerte vient à 500.

**Étape g) — 0.402 : R-05 carnets** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Page Récolte : les compteurs des carnets sont ceux du jeu (pêche, harpon, récolte) ; un poisson attrapé et un objet récolté ont
  un ✓ ; « Masquer ce qui est déjà au carnet » les cache ; « Copier » et « Enregistrer en CSV » donnent un fichier lisible.

**0.401 : panoramas** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Survol d'un panorama sur la carte Tensho : plus d'erreur du wiki, la météo demandée est donnée ; le clic droit n'a plus d'onglet
  « Exploration », le « Carnet d'exploration » reste.

**0.400 : onglets du clic droit de la carte** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Clic droit sur la carte Tensho : les 9 onglets (Quêtes … Carnet d'exploration) ont leur nom entier ; sur un petit écran, des
  flèches font défiler les onglets.

**0.399 : « Y aller » sans question** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Réglages › Modules › Récolte, cocher « Y aller : téléporter tout de suite » : « Y aller » téléporte directement (drapeau posé) ;
  vers une zone dont l'éthérite n'est pas harmonisée : la question revient. Décoché : la question habituelle.

**Étape g) — 0.398 : R-04 appât en main** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] En pêcheur, aide à la pêche ouverte : « Appât en main » montre l'appât choisi dans le jeu et son stock ; changer d'appât : la
  ligne et les cadres verts suivent (confirme que FishingBait est l'objet).

**Étape g) — 0.397 : R-03 « Y aller »** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Fiche d'un objet de récolte, « Y aller » sur un lieu : le drapeau se pose sur le point, la carte Tensho s'ouvre dessus et propose
  l'éthérite la plus proche avec son coût ; « Oui » téléporte, « Non » ne fait rien. Même essai sur un lieu de pêche.

**Étape g) — 0.396 : R-02 fenêtres des poissons** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Fiche d'un gros poisson à météo (par exemple un poisson légendaire) : « Prochaines fenêtres » donne des heures où la page Météo
  montre bien la bonne météo et la bonne météo précédente, dans la bonne plage ET ; une alarme sonne au début d'une fenêtre.

**0.395 : grimoire trié par niveau de monstre** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Ouvrir la page Mage bleu : en quelques secondes « Livre des sorts du wiki : 124 sorts » ; « Ordre : niveau du monstre croissant » :
  la liste entière est triée, chaque sort montre « niv. N » ; Canon à eau et les sorts sans monstre sont à la fin. Fermer et rouvrir :
  aucune nouvelle lecture (copie).

**0.394 : polices** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Réglages › Interface › Polices : changer la famille et la taille des titres, des sections et du texte : la fenêtre Tensho change
  tout de suite ; ↺ remet la valeur par défaut ; « Restaurer tous les réglages » remet les trois.

**0.393 : Bestiaire et grande compagnie** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Page « Bestiaire » : seul le carnet de votre compagnie est dans la liste (Maelstrom pour l'utilisateur), choisi à l'ouverture ;
  ses victimes sont celles du jeu.

**0.390 : monstres du grimoire triés par niveau** — pendant la campagne de tests en jeu de la fin (étape h)

- [ ] Fiche de « Mauvaise haleine », monstres lus sur le wiki : « Niveau croissant » met Halitostroper (31) en premier ; « Niveau
  décroissant » Polluting Morbol (90) ; les monstres sans niveau restent à la fin.

**0.388 : bouton « Trier » de l'inventaire** — pendant la campagne de tests en jeu de la fin (étape h)

- [x] Ouvrir l'inventaire (les trois dispositions : normale, grande, étendue) : le bouton « Trier » est en bas à gauche, sur la ligne
  des gils, et suit la fenêtre quand on la déplace ou qu'on change son échelle (dire si le placement doit être ajusté).
- [x] Clic : « Tri demandé », les sacs prennent l'ordre des règles de la page Tri › Mes sacs ; décocher l'interrupteur de la page Tri :
  le bouton disparaît.

### Archive

> Les sections suivantes sont le journal de vérification par version jusqu'au 0.99 (2026-10-27 → 2026-10-31), conservé tel quel. Les
> chemins `checkpoints/…` qu'il cite sont des fichiers locaux, hors dépôt. À partir du 0.100, les résultats sont dans
> [CHANGELOG.md](../../CHANGELOG.md).

PNJ vendeurs 0.92–0.99 : liens InclusionShop, liens observés (Prêtresse à la robe noire, Officier de gestion du Maelstrom), monnaies codées par CostType, positions planevent/planner avec contrôle d'en-tête LGB. Verify.ps1 agent 509/509 à chaque version (checkpoints/0.9x.0-*). VALIDATED_IN_GAME: YES — rapport utilisateur 2026-10-31 « tous es ok ».

PNJ vendeurs 0.89 (liens FateShop) : Verify.ps1 agent 2026-10-30 20:33 — DLL 0.89.0.0, 509/509 ; test de lien FateShop ajouté. VALIDATED_IN_GAME: NO. Survol 0.87 (monstres wiki) et icône Gil : VALIDATED_IN_GAME (capture utilisateur).

PNJ vendeurs 0.88 : [NPC-VENDORS-014](../goals/NPC-VENDORS-014.md). COMPILED/TESTED : Verify.ps1 agent 2026-10-30 20:27 — DLL 0.88.0.0, 509/509, zéro avertissement/erreur. Quatre groupes NpcVendorTests (liens boutique, monnaies, coordonnées, ordre et cache disque) ; colonnes Lumina vérifiées par réflexion ; lecteur de feuilles et rendu non couverts. VALIDATED_IN_GAME: NO.

Wiki source principale des monstres 0.87 : [HOVER-TOOLTIP-013](../goals/HOVER-TOOLTIP-013.md). COMPILED/TESTED : Verify.ps1 agent 2026-10-30 20:16 — DLL 0.87.0.0, 505/505, zéro avertissement/erreur. MonsterHoverSourcesTests réécrits pour le wiki ; WikiMonsterTests : titre issu du jeu sans Garland, redirections, page absente, candidats. Survol 0.85/0.86 : accroche, stock et moins cher du centre de données VALIDATED_IN_GAME (capture utilisateur) ; 0.87 VALIDATED_IN_GAME: NO.

Monstres au survol 0.86 : [HOVER-TOOLTIP-013](../goals/HOVER-TOOLTIP-013.md). COMPILED/TESTED : Verify.ps1 agent 2026-10-30 20:07 — DLL 0.86.0.0, 504/504, zéro avertissement/erreur. Quatre groupes MonsterHoverSourcesTests (copie disque sans HTTP, attente de 1,5 s puis requête unique et écriture du cache, mode cache seul et attente après échec, copie expirée visible puis actualisée). VALIDATED_IN_GAME: NO.

Survol accroché à l’infobulle 0.85 : [HOVER-TOOLTIP-013](../goals/HOVER-TOOLTIP-013.md). COMPILED/TESTED : Verify.ps1 agent 2026-10-30 20:00 — DLL 0.85.0.0, 500/500, zéro avertissement/erreur. Quatre groupes MarketHoverDetailsTests ; CacheTests marché passé en clé v2 ; placement et rendu non couverts. VALIDATED_IN_GAME: NO.

Propriétés d’objet pour le tri 0.84 : [SORT-ITEM-PROPS-008](../goals/SORT-ITEM-PROPS-008.md). COMPILED/TESTED : Verify.ps1 agent 2026-10-30 19:47 — DLL 0.84.0.0, 496/496, zéro avertissement/erreur. Quatre groupes InventorySortPropsTests ; colonnes Lumina vérifiées localement par réflexion. VALIDATED_IN_GAME: NO.

Cache disque récolte/pêche 0.83 : [CATALOG-CACHE-012](../goals/CATALOG-CACHE-012.md). COMPILED/TESTED : Verify.ps1 agent 19:43 — DLL 0.83.0.0, 492/492. Trois groupes GatheringCatalogCacheTests (horaires exacts et refus des fenêtres non normalisées, aller-retour DataCache réel, clé/validation). VALIDATED_IN_GAME: NO.

Cache disque du catalogue des recettes 0.82 : [CATALOG-CACHE-012](../goals/CATALOG-CACHE-012.md). COMPILED/TESTED : Verify.ps1 agent 2026-10-30 19:40 — DLL 0.82.0.0, 489/489, zéro avertissement/erreur. Trois groupes RecipeCatalogCacheTests (aller-retour DataCache réel avec ingrédients et paramètres de simulation, clé version du jeu/forme et refus des lignes corrompues, clé des objets inchangée). VALIDATED_IN_GAME: NO.

Cache disque du catalogue des objets 0.81 : [CATALOG-CACHE-012](../goals/CATALOG-CACHE-012.md). COMPILED/TESTED : Verify.ps1 exécuté par l’agent sur le PC le 2026-10-30 19:34 — DLL 0.81.0.0, 486/486, langues et paquet verts, zéro avertissement/erreur. Trois groupes ItemCatalogCacheTests (aller-retour DataCache réel, clé version du jeu/langue/forme, refus des contenus corrompus) ; boucle du chargeur non exécutée par les tests. VALIDATED_IN_GAME: YES — rapport utilisateur 2026-10-30 (« tous est ok »), SORT-ZONE-SELECT-011 0.77–0.80 compris.

Sélection multiple des zones de tri 0.77 : [SORT-ZONE-SELECT-011](../goals/SORT-ZONE-SELECT-011.md). COMPILED/TESTED : Verify.ps1 exécuté par l’agent sur le PC le 2026-10-30 — DLL 0.77.0.0, 481/481, langues et paquet verts, zéro avertissement/erreur. Quatre groupes InventorySortSelectionTests ; menu de clic et rendu non couverts. 0.78 (priorité des catégories) : 482/482 (+1 groupe : ordre de liste, ordre interne par critères, débordement, champ historique, remplissage inversé), DLL 0.78.0.0, agent 2026-10-30 19:20. 0.79 (cases restantes partagées) : 483/483, DLL 0.79.0.0, agent 2026-10-30 19:24. 0.80 (filtre de recherche des catégories, UI seule, non couvert par les tests) : 483/483, DLL 0.80.0.0, agent 19:29. VALIDATED_IN_GAME: PARTIAL — captures utilisateur 0.77/0.78 : catégories multiples et numérotées, rendu OK ; cases vides réservées signalées (corrigé par l'option 0.79) ; clic et persistance non encore observés.

Progression des sources de quête 0.76 : [QUEST-ITEM-PROGRESS-007](../goals/QUEST-ITEM-PROGRESS-007.md). COMPILED/TESTED : Verify.ps1 exécuté par l’utilisateur le 2026-10-30 (assembly 0.75.0.0 avant passage à 0.76.0) — 477/477 tests, langues et paquet verts, zéro avertissement/erreur. Six groupes : jointure par ID complet, relevés hors ligne/non datés/en lecture/en erreur, ligne absente, filtre après compteurs et recherche, cycle du service (invalidation, changement de personnage, cache, clear), lecture seule. VALIDATED_IN_GAME: PARTIAL — Captures utilisateur 2026-10-30 18:43 (objet « Pièce d’argent allagoise ») : colonne Progression, compteurs 1 en cours · 173 terminées · 127 non confirmées (301/301), filtres Toutes et En cours, séquence affichée ; relevé issu du cache disque daté 29-10-26 21:40. Observation : la séquence native 255 était affichée brute (corrigée en « séquence finale (255) »). Verify.ps1 relancé par l’agent sur le PC le 2026-10-30 18:54 : DLL 0.76.0.0, 477/477, 0 avertissement/erreur. VALIDATED_IN_GAME: YES — rapport utilisateur 2026-10-30 : DLL 0.76.0.0 chargée, libellés « séquence finale (255) » / « 1 variante » et « Non confirmée » après déconnexion OK (non observé directement par l’agent).

Menu contextuel 0.53 : [USE-CONTEXT-004](../goals/USE-CONTEXT-004.md). Verify.ps1 : 349/349 tests/package, compilation sans avertissement/erreur. Six groupes supplémentaires : routes/qualités et remise unique ; remplacement de menus ; revalidation au dispatch ; identité/limites ; logout/reconnexion/Dispose ; option v1 indépendante. ConfigSerializationProbe : trois modes Newtonsoft passants. Signatures IContextMenu et source primaire contrôlées ; callback et rendu natifs NOT VALIDATED_IN_GAME. Aucun essai projection/Macro Chain redemandé.

État courant 0.75.0 (2026-10-29) : [cache partagé et péremption réglable](../goals/CACHE-001.md). Prix, quêtes, collections, sources communautaires, Revue de mode et calculs du carnet réutilisent des instantanés persistants datés. Durées indépendantes dans Réglages : 5 min, 1 h, 24 h ou 7 j ; actualisation forcée depuis les outils. Les imports restent manuels et signalent leur péremption ; les observations en direct gardent une invalidation courte. 466/466 tests/package/SHA-256 passants, zéro avertissement/erreur ; validation en jeu de ce cache encore à faire.

Index objet → quêtes 0.68 : huit groupes nouveaux, 434/434 tests/package verts, journal checkpoints/0.68.0-verify.log. Copies/variantes/IDs/couverture, bornes/annulation, worker réel, réponses tardives même/nouveau personnage, invalidate/clear/fail/dispose et erreurs isolées. Probe de production : 3 496 Items/8 288 associations, tous ensembles et variantes comparés à la projection directe ; Potion = 25 quêtes candidates, owner/progression hors ligne non confirmés. GatheringProbe recompilé. UI/callbacks en jeu non observés ; détails docs/goals/QUEST-ITEM-SOURCES-004.md.

Récompenses quête 0.67 : cinq groupes nouveaux, 426/426 tests/package verts, journal checkpoints/0.67.0-verify.log. Propriété/readonly/owner, alternatives non sommées, quantités et références inconnues, clé de monnaie ambiguë et bornes. QuestRewardsProbe : 5 373 snapshots/9 313 lignes, tous Item/teintes résolus ; 37 tables métier/538 sous-lignes ; quatre quantités et quinze monnaies directes inconnues conservées. Types mémoquartz 1→28 et 3→49 prouvés, pas une jointure par numéro de ligne. GatheringProbe recompilé. Aucun rendu/choix/réception en jeu déclaré. Détails : docs/goals/QUEST-REWARDS-003.md.

Titres/succès 0.66 : cinq groupes nouveaux, 421/421 tests/package verts, journal checkpoints/0.66.0-verify.log. Table de vérité chargement/positif, perte de couverture, owner/invalidation, contradiction atomique, récompense distincte de l’acquisition. CollectionProbe : 885 titres/530 variantes de genre, 4 003 succès/83 catégories, 192 récompenses Item/891 Title, identités/icônes et rejets de jointures étrangers vérifiés ; orchestrion toujours passant. API/UnlockDelegate source inspectées ; callbacks/chargement/acquisitions en jeu NOT VALIDATED_IN_GAME. Détails : docs/goals/COLL-003.md.

Orchestrion 0.65 : quatre groupes supplémentaires, 416/416 tests/package verts, journal checkpoints/0.65.0-verify.log. CollectionProbe avec projections de production : 886 morceaux/12 catégories/886 liens Item d’identités distinctes, icônes présentes, UI/catégories étrangères et propriétaires hors ligne refusés ; modèles utilisables après disposal GameData. API IUnlockState et source primaire inspectées. Callbacks, acquisitions réelles et navigation UI non validés en jeu ; essais craft/Macro Chain non redemandés. Détails : docs/goals/COLL-002.md.

Prévisions pêche 0.64 : sept groupes nouveaux, 412/412 tests/package verts, journal checkpoints/0.64.0-verify.log. Oracle indépendant BigInteger/horaire sur environ 5 000 dates, périodes précédente/courante, couverture partielle, logique ternaire, alternatives, minuit/ticks, bornes/copies/annulation. Probes réels avec fixture 2026-10-29 12:00 UTC : 8753, météo + précédente et horaire absent, un segment 13:13:20–13:36:40 ; 4915, horaire seul, quatre segments. Aucun rendu/prise/ciel observé. Détails : docs/goals/GATHER-FISH-FORECAST-010.md.

Météos pêche 0.63 : quatre groupes nouveaux, 405/405 tests/package verts. WeatherProbe : 214 lignes, 43 noms globalement ambigus, copies anglaises absentes/étrangères refusées et prévisions antérieures passantes. GatheringProbe réel 8753 : spot 107/territoire 137, noms source Clear Skies/Rain/Showers reliés à Weather 1/7/8 et icônes françaises. Aucun runtime déclaré. Voir docs/goals/GATHER-FISH-WEATHER-009.md.

Conditions pêche 0.62 : cinq groupes supplémentaires et 401/401 tests/package verts. Oracle indépendant sur 600 couples d’heures ; inconnus/vides, erreurs isolées, copies, UTC/minuit et météo précédente. Probes réels 4915 (10–18 ET), 8753 (Clear Skies après Rain/Showers), 7685 (during absent préservé). FashionProbe compilé ; aucun rendu en jeu validé. Détails : docs/goals/GATHER-FISH-WINDOWS-008.md.

Carnet pêche 0.60 : [GATHER-FISH-006](../goals/GATHER-FISH-006.md). Verify.ps1 : 389/389 tests/package/SHA-256, zéro avertissement/erreur ; quatre groupes identité/variantes, codes, copies/conflits et lifecycle. FishingNotebookProbe partage la projection : 2 461 fiches et notes concordantes, 296 tomes, aucun Item/spot non résolu ; 491 codes météo inconnus, notes absentes/étrangères rejetées. GatheringProbe antérieur exécuté sans option réseau et passant. Rendu/framework en jeu non observés ; essais craft/Macro Chain différés.

Modèles/livraison 0.59 : [SORT-TEMPLATES-007](../goals/SORT-TEMPLATES-007.md). Verify.ps1 : 385/385 tests/package, zéro avertissement/erreur ; six groupes bibliothèque/rebinding/invalides/mutations/v1 et modèle → permutation d’une autre sacoche. SHA-256 des entrées DLL/manifeste du ZIP identiques aux fichiers canoniques, entrées uniques et dépendances hôte exclues. Newtonsoft None/Auto/Objects avec bibliothèque imbriquée passants. Aucune validation runtime ; aucun essai craft/Macro Chain redemandé.

Propriétés 0.58 : [SORT-FILTERS-006](../goals/SORT-FILTERS-006.md). Verify.ps1 : 379/379 tests/package, zéro avertissement/erreur ; cinq groupes pour AND/inconnus/masques/identité/cache/JSON. Newtonsoft None/Auto/Objects et FashionProbe sans --fetch passants : 20 238 Wearable, 7 804 MainHand, 984 OffHand, 31 SoulCrystal, 51 vêtements bloquant Head ; OffHand négatifs exclus. API installée confirmée. Rendu/application/persistance en jeu non observés ; aucun essai craft/Macro Chain redemandé.

Protection/inversion 0.57 : [SORT-PROTECTED-005](../goals/SORT-PROTECTED-005.md). Verify.ps1 : 374/374 tests/package, zéro avertissement/erreur ; cinq groupes dont 20 scènes multi-scopes vérifiant positions vides/occupées et bijection physique/quantités. Newtonsoft None/Auto/Objects avec zones protégées et inversées passants. Rendu/application/persistance natifs NOT VALIDATED_IN_GAME ; aucun essai craft/Macro Chain redemandé.

Profils/zones 0.56 : [SORT-RULES-004](../goals/SORT-RULES-004.md). Verify.ps1 : 369/369 tests/package, zéro avertissement/erreur ; huit groupes nouveaux, 24 scènes multi-scopes avec oracle de coordonnées/quantités et trous réels, refus de positions forgées/chevauchement/place insuffisante. ConfigSerializationProbe : trois modes Newtonsoft avec profils imbriqués passants. Rendu/bridge mémoire/persistance en jeu non observés ; protocoles conservés, aucun test craft/Macro Chain redemandé.

Scopes 0.55 : [SORT-SCOPES-003](../goals/SORT-SCOPES-003.md). Verify.ps1 : 361/361 tests/package, zéro avertissement/erreur ; six groupes nouveaux dont 144 scénarios (trois rangements × six remplissages × quatre critères × deux sens) avec oracle indépendant. Cache sans reçu direct, propriétaires/scopes, capture tardive, fraîcheur et lifecycle vérifiés. Fenêtres/bridge mémoire/rendu/persistance NOT VALIDATED_IN_GAME ; aucun test craft/Macro Chain redemandé.

Tri personnel 0.54 : [SORT-NATIVE-002](../goals/SORT-NATIVE-002.md). Verify.ps1 : 355/355 tests/package, zéro avertissement/erreur. Six groupes nouveaux dont 48 scénarios avec oracle de coordonnées, refus des mutations et cycle de demande. SDK/source primaire inspectés ; bridge d’écriture natif, rendu et persistance VALIDATED_IN_GAME (rapport utilisateur 2026-10-30, DLL 0.76.0.0, protocole SORT-RUNTIME-VALIDATION de GOAL.md, « tout est OK »). Protocole dans le document, aucun essai craft/Macro Chain redemandé.

Survol 0.52 : [USE-HOVER-003](../goals/USE-HOVER-003.md). Verify.ps1 : 343/343 tests/package verts, zéro avertissement/erreur. Neuf groupes nouveaux : identité commune, propriétaire/âge/stabilité, horloge/lifetime, usages seuls sans HTTP, activation et cache, réponse tardive annulée, portée propriétaire/monde, backoff/priorité manuelle et options v1. Transport HTTP simulé ; aucune validation de survol ou rendu réelle. ConfigSerializationProbe (Newtonsoft None/Auto/Objects) et GatheringProbe exécutés. Protocole utilisateur conservé dans le document ; pas de nouvel essai craft/Macro Chain demandé.

Usages 0.51 : [USE-002](../goals/USE-002.md). Verify.ps1 : 334/334 tests/package verts, zéro avertissement/erreur. Sept groupes nouveaux pour index, variantes/lignes, copies, validité, tri/couverture, annulation et service. ItemUseProbe partage la projection Lumina de production : 1 240 mandats, 2 516 candidats GC, 263 poissons, 3 280 références Item et icônes de mandats résolus ; aucun lien de mandat perdu. Rendu et chargement via Dalamud non observés. Protocole dans le document ; aucun essai craft/Macro Chain redemandé.

PR0.50 : [GP-ACTIVE-005](../goals/GP-ACTIVE-005.md). Sept groupes nouveaux, dont128 scénarios couvrant16 combinaisons de familles × 4 bonus limités × 2 métiers contre un oracle indépendant par tentative. Verify.ps1 :327/327 tests/package verts, zéro avertissement/erreur ; probes natifs vérifient les anciens scénarios et un état explicitement déclaré (base 23 →32 objets pour300 PR). Capture des effets natifs et rendu de cette UI non validés. Protocole conservé dans le document ; aucun test projection/Macro Chain redemandé.

Point de récolte 0.49 : [GATHER-NATIVE-004](../goals/GATHER-NATIVE-004.md). Verify.ps1 : 320/320 tests/package verts, zéro avertissement/erreur ; GatheringUiProbe confirme les huit lignes, types de nœuds et limites dans Gathering.uld installé. Copie stricte, inconnus, ressources, âge, propriétaire et leases testés. Rendu/capture en jeu non validés. Protocole conservé : activer le panneau Point de récolte actuel dans GatherBuddy ou PR, comparer objets/taux/tentatives, sélectionner un objet puis copier les ressources ; vérifier que les bases restent à renseigner et la copie impossible en récolte rapide. Pas de nouvel essai projection/Macro Chain demandé.

## GATHER-001A — 0.6.0

Projection 0.46 et enveloppe 0.47 : voir [conditions/bonus](../goals/CRAFT-CONDITIONS-004.md) et [séquence fixe](../goals/CRAFT-CONDITION-RANGE-005.md). Verify.ps1 : 302/302 tests, zéro avertissement/erreur, package vert. ToolBonusProbe : 928 outils dont 136 bonus améliorés, 519 inconnus gardés inconnus. Les bornes de séquence sont comparées à une énumération exhaustive indépendante des transitions sur petits scénarios, y compris buffs, combos, plafonds et fin/casse. L’utilisateur a différé le test projection le 2026-10-29 ; Macro Chain aussi différé, ne pas redemander.

Mini Cactpot 0.48 : [CACT-LIVE-002](../goals/CACT-LIVE-002.md). 311/311 tests et package verts ; copies/bornes/zéros, leases, propriétaire, fermeture/erreur, âge, worker réel et résultats/erreurs tardifs testés. Mode natif non observé en jeu. Protocole conservé : activer l’observation dans Mini Cactpot, ouvrir un ticket, comparer les chiffres après chaque révélation, vérifier choix de ligne après quatre chiffres et retrait du conseil à la fermeture. Aucun clic automatique. Aucun nouveau test projection/Macro Chain demandé.

Séquences 0.44.0 : voir [CRAFT-PRESETS-001](../goals/CRAFT-PRESETS-001.md), 283/283 tests, roundtrip JSON managé, résolution exacte/bornes et rollback ajout/retrait. Persistance/rechargement réels dans Dalamud encore à vérifier ; aucun test natif déclaré réalisé.

Fenêtres 0.43.0 : voir [GATHER-WINDOWS-003](../goals/GATHER-WINDOWS-003.md), 280/280 tests, oracle sur deux jours ET et probe du point30999 (09–12 ET → époque+1575/+2100 s). Horloge/frontières/minuit et inconnus testés ; comparaison runtime différée.

Météo 0.42.0 : voir [GATHER-WEATHER-001](../goals/GATHER-WEATHER-001.md), 275/275 tests, oracle BigInteger/500 dates, frontières et inconnus, probe 1 290 territoires/72 météos/icônes. Empyrée 90/100 conserve l'inconnu. Présentation et comparaison avec le jeu différées.

Effets observés 0.41.0 : voir [CRAFT-LIVE-002](../goals/CRAFT-LIVE-002.md), 270/270 tests, douze statuts natifs/icônes vérifiés ; Param, couverture, doublons, conflits et utilisation unique inconnue testés. Comparaison des compteurs/combos/étapes en synthèse d'essai encore requise ; aucun conseil natif déduit.

Observation 0.40.0 : voir [CRAFT-LIVE-001](../goals/CRAFT-LIVE-001.md), 265/265 tests et métadonnées natives vérifiées. Activation temporaire explicite ; valeurs/effets partiels et aucun conseil. Capture en synthèse d'essai non réalisée ; protocole conservé sans redemander Macro Chain.

Actions 0.39.0 : voir [CRAFT-ACTIONS-002](../goals/CRAFT-ACTIONS-002.md), 260/260 tests, 208 variantes natives vérifiées et huit groupes d'interactions avancées. Contrôle natif niveau 100 à attributs fictifs : proposition terminée mais sous la cible, correctement distinguée ; aucune garantie d'optimalité ou validation en jeu.

Solveur 0.38.0 : voir [CRAFT-SOLVE-001](../goals/CRAFT-SOLVE-001.md), 252/252 tests, oracle indépendant sur 36 scénarios, témoin faisable multi-effets, annulation/invalidation et probe natif à attributs fictifs. Propositions à comparer en synthèse d'essai, aucune activation native validée.

Export 0.37.0 : voir [CRAFT-MACRO-001](../goals/CRAFT-MACRO-001.md), 243/243 tests et probe des noms natifs pour huit métiers ; copie par bloc seulement, essai dans les macros du jeu différé.

Historique : les vérifications ci-dessous restent datées de leurs versions. Pour la simulation 0.36.0, voir [CRAFT-SIM-001](../goals/CRAFT-SIM-001.md) : Verify.ps1 239/239, probe 136 actions/13 892 recettes et scénario Sirop de bouleau ; comparaison en jeu différée.

Sources statiques depuis GatheringItem, GatheringPointBase, GatheringPoint, GatheringItemPoint (objets cachés), TerritoryType/PlaceName/Map et FishingSpot. Accès depuis GatherBuddy, onglet Récolte, et bouton Lieux de chaque ingrédient préparé. Filtre par type, métier/niveau/zone/lieu, copie et ouverture du centre de la carte de zone. Ce repère ne représente pas les coordonnées exactes du nœud. Météo, horaires, prérequis et pêche sous-marine non couverts.

COMPILED/TESTED: Verify.ps1 vert, 66/66 tests et package 0.6.0.0 vérifié. Index managé, séparation des feuilles de pêche/récolte, zones distinctes, annulation, erreurs/clear testés.
LOCAL_DATA_VERIFIED: GatheringProbe sur le sqpack installé : 5 857 points ; 11 186 liens ordinaires, 2 420 liens pêche ; 4 199 sources dédupliquées après inclusion des liens directs. Marron sombre 12893 : Coupe niveau 55, Avant-pays dravanien, Terres fumantes, IsHidden=true. Sève de bouleau 12891 : Coupe, Écume des cieux d'Abalathia, Voor Sian Siran.
VALIDATED_IN_GAME: NON — lecture des fichiers et tests ne prouvent pas le rendu/clic en jeu.

## Sortie de compilation unifiée — 2026-10-27

Le csproj impose PlatformTarget x64 et OutputPath vers bin/x64/Configuration. Build direct du csproj et build de la solution vérifiés : tous deux produisent C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll. Verify.ps1 : 63/63 tests et package validés. Aucun changement fonctionnel ; version conservée 0.5.27. L'ancien dossier bin/Release (artefacts obsolètes) est archivé dans bin/legacy-Release-20261027, récupération possible ; aucune source ou banque supprimée.

## Confirmation préparation — 2026-10-27

Retour utilisateur « c'est ok relis les .md et continue le goal » après livraison 0.5.27 : préparation acceptée en jeu dans son périmètre courant. Pas de nouvelle capture ; ne pas étendre cette confirmation aux cas limites, erreurs ou changements de personnage. Catalogue, carnet et préparation clos ; GATHER-001A est le prochain objectif prêt. Aucune modification du code dans cette reprise documentaire.

## Préparation fabrication — 0.5.27

Outil intégré à Produit par et à la fiche sélectionnée du carnet Utilisé dans ; accès depuis Craftimizer. Quantité à produire 1..999999, nombre de crafts arrondi au rendement, quantité produite et surplus. Tableau des ingrédients : requis, stock recensé NQ+HQ, écart à compléter. Cristaux et imports sont inclus via ItemHoldings. Stocks inconnus restent Inconnu/À vérifier ; caches manuels signalés. La copie vers le presse-papiers conserve ces réserves. Aucun craft automatique ou simulation ajouté.

BUILD: Verify.ps1 Release 0.5.27.0, zéro avertissement/erreur.
TESTS: 63/63 ; deux nouveaux groupes couvrent arrondi au rendement, fusion des ingrédients répétés, NQ+HQ, pas de manque négatif, inconnus/caches et entrées invalides.
RUNTIME: nouveau panneau à vérifier en jeu.

CARNET_CONFIRMATION: Capture codex-clipboard-68433d3a-b304-4103-b5d5-5ab624267e21.png, v0.5.26 et retour « c'est ok ». Objet Marron sombre, six recettes, icône Cuisine, liste de résultats et recette sélectionnée Marrons glacés collectionnables avec cinq ingrédients et 51 crafts visibles.

## Carnet Utilisé dans — 0.5.26

Carnet affichant exclusivement les recettes ForIngredient de l'objet en contexte. Filtres métier via icônes de classe natives, correspondance CraftType 0..7 vers ClassJob 8..15 et icônes encadrées 62108..62115. Libellés métier issus du jeu, bouton Tous, recherche de recette, liste de résultats à gauche, fiche sélectionnée à droite. La fiche reprend ingrédients cliquables, NQ/HQ, rendement et nombre de crafts possibles. Les variantes sont sélectionnées par RecipeId.

BUILD: Verify.ps1 Release 0.5.26.0, zéro avertissement/erreur. TESTS: 61/61 et package validés ; identité métier conservée dans l'index inverse testée.
RUNTIME: contrôle du nouveau carnet requis en jeu.

BANQUE_CONFIRMATION: Capture utilisateur codex-clipboard-79f1aa8b-bc78-4fa2-810c-9e9ee37d91f0.png et retour « c'est ok » : filtre Appât de pêche sous Global, seul Personnage · Sac 3 visible. Ceci valide le masquage des sacs sans résultat ; aucune preuve supplémentaire de persistance des imports n'est inférée.

## Banque finalisation 0.5.25

Bouton Réinitialiser : efface catégorie, qualité et texte. Les piles NQ restent trouvables même si une pile HQ du même objet existe dans un autre sac. Une seule règle de filtrage est utilisée pour la présence de résultats et le rendu des cases. Les sacs filtrés sans résultat sont masqués, les autres restent à leurs positions internes. Les piles identiques ont des identifiants UI par case. Fonds des cases vides visibles. Les relevés importés affichent leur heure UTC ; indication de cache manuel et erreurs de chargement visibles.

BUILD: Release 0.5.25.0 compilée, zéro avertissement/erreur. TESTS: 61/61 et package vérifiés.
RUNTIME: validation finale à exécuter par l'utilisateur ; aucune confirmation visuelle 0.5.25 encore reçue.

## Banque — cadres filtrés 0.5.24

Un filtre de catégorie, qualité ou texte masque les sacs sans case correspondante.
Les cadres visibles se regroupent sur deux colonnes ; noms et positions internes restent conservés.
Sans filtre, les sacs vides restent affichés. Aucun résultat affiche un message explicite.
Verify.ps1 : compilation Release réussie, 61/61 tests et package validés. Rendu en jeu à confirmer.

## Banque — ordre visuel natif 0.5.23

Les essais précédents ligne/colonne ne validaient pas le tri de l'interface et sont abandonnés.
La capture lit désormais ItemOrderModuleSorter.Items : chaque position visuelle référence Page et Slot physiques.
La projection rassemble tous les conteneurs d'une origine puis découpe en pages visuelles de 35 cases
(4 personnage, 5 servant, 2 sacoche). Les filtres laissent des trous et ne compactent pas les cases.
Les imports sauvegardent les stacks séparés, leur slot physique et leur index visuel ; les quantités
agrégées restent contrôlées lors du chargement. Les anciens imports restent lisibles pour les quantités,
mais demandent une actualisation pour afficher leur ordre. Global inclut aussi le personnage.
Le personnage est relu au plus une fois par seconde pour détecter un changement de tri sans événement de quantité.

BUILD: Verify.ps1 Release 0.5.23.0, 0 avertissement/erreur.
TESTS: 61/61 ; tests nouveaux : permutation entre sacs avec trous/HQ, sauvegarde/rechargement de piles séparées et de leur ordre.
RUNTIME: NON VALIDÉ — recharger 0.5.23, comparer personnage trié, puis réimporter une sacoche et un servant.


Date : 2026-10-27 Europe/Brussels. Étape : CORE-000.

COMPILED: YES — solution Release, .NET SDK 10.0.403, Dalamud.NET.Sdk 15.0.0, assemblies Hooks/dev ; 0 avertissement, 0 erreur.

TESTED: YES — `scripts/Verify.ps1` terminé avec code 0. Cinq tests managés passent :

1. Paramètres par défaut et aller-retour JSON (Version et OpenOnLoad).
2. Nettoyage des acquisitions dans l'ordre inverse, Dispose idempotent et refus d'acquisition après Dispose.
3. Nettoyage des acquisitions déjà réussies après un échec d'initialisation, sans libérer une ressource non acquise.
4. Poursuite du nettoyage si une libération lève une exception.
5. Trois cycles d'abonnement/désabonnement sans callback conservé après déchargement.

Paquet : identité TenshoAssistant, API 15, version 0.1.0.0 ; ZIP contenant DLL et manifest ; absence des assemblies Dalamud/FFXIVClientStructs/Lumina/ImGui de l'hôte.

Les tests exécutent les fichiers managés de production Configuration.cs et RegistrationScope.cs. Ils n'instancient pas Plugin, WindowSystem, le sérialiseur de configuration Dalamud ni les bindings natifs ImGui. Les branches UI d'erreur de sauvegarde et la résolution des services nécessitent un essai dans l'hôte.

VALIDATED_IN_GAME: NO — aucun client FFXIV n'a été piloté et aucun chargement de plugin n'a été effectué.

## Smoke test en jeu restant

1. Ajouter `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll` aux plugins de développement Dalamud, puis charger.
2. `/tensho` ouvre la fenêtre ; fermer puis ouvrir depuis l'entrée principale et l'entrée de configuration Dalamud.
3. Cocher l'ouverture au chargement ; recharger : la fenêtre s'ouvre. Décocher ; recharger : elle reste fermée et `/tensho` l'ouvre.
4. Recharger/décharger plusieurs fois, vérifier l'absence de double callback, de commande orpheline ou d'erreur dans les logs.
5. Vérifier un conflit de commande : l'initialisation échoue proprement sans supprimer la commande de l'autre propriétaire.
6. Tester la sauvegarde/relecture via l'hôte et, dans un environnement de test, son échec : préférence restaurée et erreur visible/journalisée. Une version de configuration inconnue doit empêcher le chargement sans écrasement.

## Reproduire les tests automatiques

```powershell
Set-Location C:\Plugins-ff14
.\scripts\Verify.ps1
```

Ce projet utilise un petit exécutable de tests ; `dotnet test` seul ne l'exécute pas. Le script vérifie les codes de sortie et le paquet. Aucun benchmark ou test métier n'est revendiqué.

## CORE-001 — 2026-10-27

COMPILED: YES — Verify.ps1, Release, zéro avertissement et zéro erreur.
TESTED: YES — 13/13 tests managés (les cinq du socle plus huit groupes de tests inventaire), paquet vérifié.
VALIDATED_IN_GAME: NO — ce GOAL n'effectue aucune lecture du jeu.

Les tests exécutent directement InventorySnapshot.cs et InventoryService.cs de production. Ils vérifient : absence zéro uniquement dans une portée complète ; quantités partielles minimales ; séparation propriétaire/conteneur/NQ-HQ et agrégation ; portée mixte et déduplication ; copie des entrées et métadonnées UTC/source ; remplacement/clear sans rétention de l'ancien propriétaire ; rejets des entrées invalides/contradictoires ; dépassements arithmétiques détectés.

Le premier build des tests a révélé une ambiguïté C# entre new() cible et un argument params tableau. Les constructeurs des appels concernés ont été explicités ; le script complet a ensuite réussi. Aucune modification de l'API Dalamud nécessaire.

Ces tests établissent la sémantique managée, pas la complétude d'une future lecture native. Ils ne mesurent ni la contention multithread ni les performances sur un inventaire réel. L'adaptateur devra justifier toute déclaration Complete avec une preuve locale.

## INV-001 — 2026-10-27

COMPILED: YES — build Release via Verify.ps1, zéro avertissement/erreur.
TESTED: YES — 20/20 tests (13 existants plus sept groupes du coordinateur) ; paquet vérifié.
VALIDATED_IN_GAME: NO — un processus FFXIV et XIVLauncher ont été observés, mais aucun chargement ni interaction avec ce plugin n'a été effectué.

Les nouveaux tests exécutent InventoryCaptureCoordinator de production avec des fonctions de lecture managées : capture initiale, regroupement et délai de 250 ms, invalidation immédiate, déconnexion/changement de propriétaire, reprise Unknown après cinq secondes, erreurs avec limitation des logs et récupération, changement/invalidation pendant une collecte, rejet d'un lot pour un autre propriétaire, Dispose pendant une collecte et absence de travail après Dispose.

La compilation lie l'adaptateur aux véritables signatures installées de IGameInventory/IFramework/IPlayerState/IClientState. ApiProbe utilise MetadataLoadContext pour inspecter ces métadonnées sans exécuter le jeu. Les tests ne créent pas de GameInventoryItem natif et ne prouvent pas l'exactitude des observations live, la livraison des événements dans le client ou le comportement graphique.

### RUNTIME-001 : protocole d'acceptation restant

1. Charger la DLL Release comme plugin de développement et ouvrir `/tensho` ; vérifier les logs de chargement.
2. Personnage connecté : vérifier quatre sacs diagnostiqués, date UTC présente, états Partial ou Unknown uniquement. Aucune affirmation Complete/total exact ne doit apparaître.
3. Saisir l'identifiant de base d'un objet présent dans les sacs, comparer la quantité observée avec les piles visibles ; basculer HQ et vérifier la séparation. Un objet absent garde un total inconnu.
4. Déplacer ou scinder une pile dans les sacs ; vérifier une nouvelle date d'observation et le nouveau minimum. Les sacoches/servants restent exclus.
5. Vérifier que les sacs indisponibles restent Unknown puis sont relus après chargement, sans boucle d'erreurs dans les logs.
6. Se déconnecter : diagnostic inconnu, aucune quantité de l'ancien personnage. Reconnecter un autre personnage : ses observations remplacent intégralement les précédentes.
7. Recharger/décharger trois fois ; vérifier l'absence de callbacks et de commande orphelins. Tester aussi la préférence d'ouverture au chargement du socle.

Consigner les résultats réels et les logs d'erreur avant de marquer VALIDATED_IN_GAME. Les étapes qui modifient des piles et changent de personnage sont à effectuer au moment choisi par l'utilisateur.

### RUNTIME-001 : première preuve reçue

Source : capture utilisateur codex-clipboard-4f4c69b5-0d04-4b7f-822f-9dab6081658e.png, reçue le 2026-10-27. Analyse de l'image fournie, aucune interaction avec le client par l'agent.

VALIDATED_IN_GAME: PARTIAL — fenêtre Tensho--Assistant affichée dans FFXIV ; diagnostic présent, date d'observation affichée 2026-10-27 16:46:03 UTC ; sacs 1–4 tous « partiel » et avertissement de couverture visible. Cela atteste l'affichage et une première observation, sans prouver l'exactitude du contenu.

Champ identifiant à 0, HQ décoché et préférence d'ouverture décochée. Aucune requête de quantité n'est visible. La capture ne prouve ni l'emploi de /tensho pour ouvrir, ni la propreté des logs, ni une actualisation, ni le comportement Unknown, ni la persistance, ni les rechargements ou changements de personnage. Ces vérifications restent ouvertes.

Prochain test utile : saisir l'identifiant de base d'un objet connu dans les sacs, comparer NQ/HQ, déplacer une pile puis vérifier la nouvelle observation et la quantité minimale affichée.

### RUNTIME-001 : comparaison Potion 4551 reçue

Source : trois captures utilisateur reçues le 2026-10-27 : codex-clipboard-354c09fa-2577-4020-9612-c5ec4ce0e301.png (requête NQ), codex-clipboard-090999d5-1dab-478e-a563-d297d51cd574.png (requête HQ), codex-clipboard-6546417c-066f-43c8-83c8-8c39027687df.png (icône de pile).

Résultat visuel : ID 4551, HQ décoché → quantité observée 5 ; HQ coché → quantité observée 0 ; la pile montrée porte le compteur 5. Les deux requêtes conservent « minimum ; total inconnu ». Concordance visuelle sur cet objet et distinction des deux résultats NQ/HQ observées.

Limites : les captures ne recensent pas tous les sacs et ne démontrent pas une absence globale de HQ. Aucun exemple de quantité HQ positive n'a été testé. Elles ne montrent ni déplacement de pile ni nouvelle date, ni déconnexion/rechargement/persistance. VALIDATED_IN_GAME reste PARTIAL.

Prochain test : déplacer la pile normale dans un autre emplacement des quatre sacs. La date d'observation doit se renouveler et le minimum agrégé doit rester 5 si aucune potion n'est ajoutée/retirée. Puis poursuivre le protocole lifecycle au moment choisi par l'utilisateur.

### RUNTIME-001 : quantité HQ positive confirmée

Source : capture utilisateur codex-clipboard-0b675a1c-196f-487c-a3d2-fff41ba28eac.png, reçue le 2026-10-27, et message « ça fonctionne mais total inconnu alors que 0 nq 156 hq ».

Résultat visible : ID 12892 et HQ coché, quantité observée 156 ; l'utilisateur confirme 156 HQ et 0 NQ. Le résultat NQ est rapporté par l'utilisateur, sans capture NQ jointe. La quantité HQ positive concorde donc avec son inventaire déclaré.

Observation de présentation : « minimum ; total inconnu » reste affiché. Cause vérifiée dans le code : DalamudInventoryReader produit uniquement Partial/Unknown, sans vérifier le chargement natif des conteneurs ; MainWindow reprend ce statut. Ce libellé ne signifie pas que le compteur ne sait pas lire 156. Pour annoncer un total exact des quatre sacs, il faut vérifier leur complétude réelle ; les autres rangements restent une portée distincte.

VALIDATED_IN_GAME reste PARTIAL : quantités de deux objets concordantes, mais aucun nouveau résultat explicite sur l'horodatage après déplacement, la persistance ou le lifecycle.

## INV-002 — version 0.1.1, 2026-10-27

Consigne utilisateur appliquée : compilation et tests avant tout nouvel essai demandé.

COMPILED: YES — Verify.ps1 terminé avec code 0, Release, zéro avertissement/erreur.
TESTED: YES — 25/25 tests, paquet vérifié ; concordance version du manifest source, du manifest produit et des métadonnées DLL (0.1.1.0).
VALIDATED_IN_GAME: NO pour les nouvelles vérifications de complétude. Les captures antérieures restent des preuves de 0.1.0, pas de la version corrigée.

Cinq nouveaux tests managés : en-têtes chargés/stables et nombre complet de slots ; en-têtes manquants/non chargés/type incorrect/tailles invalides ; span tronqué ou vide et buffer/taille instables ; quatre sacs complets avec exactement 0 NQ/156 HQ et exclusion d'un rangement inconnu ; reprise d'un sac Partial jusqu'à Complete puis arrêt des scans sans événement. Les tests coordinateur précédents utilisent désormais des lots Complete pour le cas stabilisé, et conservent un scénario Unknown avec reprise.

La compilation vérifie l'usage des véritables membres FFXIVClientStructs installés ; les tests managés vérifient la décision de couverture. Ils n'exécutent pas les appels natifs à InventoryManager.

Essai à demander après ce build réussi : recharger la DLL 0.1.1, ouvrir /tensho, vérifier les quatre sacs « complet », saisir 12892 puis vérifier 156 HQ et 0 NQ si l'inventaire n'a pas changé. L'affichage doit annoncer « Total dans les quatre sacs » et exclure les autres rangements. Si un sac reste inconnu/partiel, ne pas forcer Complete : rapporter son état et les éventuelles erreurs Dalamud.

### INV-002 : résultat live reçu

Source : capture utilisateur codex-clipboard-82c08f54-cd4e-45ea-8616-ae48cd6cf3aa.png reçue le 2026-10-27, avec le message « et total bon », en réponse à l'essai de la DLL 0.1.1.

VALIDATED_IN_GAME: YES pour l'affichage des quatre sacs complets (capture) et le total correct (confirmation utilisateur). La capture montre seulement les états des sacs, pas le nombre ni le filtre de qualité ; les valeurs du total ne sont donc pas relues directement sur cette image. Le numéro de version n'est pas visible : attribution à 0.1.1 par le contexte de l'essai et le nouvel état « complet » absent de 0.1.0.

La correction de complétude/total est validée dans ce périmètre. RUNTIME-001 reste partiel : persistance, rechargements répétés, déconnexion/changement de personnage et actualisation horodatée restent à confirmer séparément. Aucune modification du code ni nouvelle compilation nécessaire pour consigner cette preuve.

## Retour sur actualisation, persistance et reconnexion

L'utilisateur a répondu « tous est OK sauf que une fois deco reco je doit l'ouvrir moi meme » au protocole en trois étapes : déplacement avec renouvellement de date/total stable, persistance après désactivation/réactivation, puis déconnexion/reconnexion. Actualisation et persistance sont donc confirmées par déclaration utilisateur ; réouverture automatique à la reconnexion signalée manquante. Le code 0.1.1 ne prévoyait l'ouverture qu'au chargement du plugin.

## UI-001 — version 0.1.2

Demande complémentaire explicite : séparer ouverture au chargement et ouverture à la connexion. Implémentation finale : deux booléens persistants indépendants, deux cases distinctes, abonnement Login libéré avec le plugin. L'option de connexion est désactivée par défaut pour les anciens fichiers de configuration.

COMPILED: YES — Verify.ps1 réexécuté après la séparation, Release 0.1.2.0, zéro avertissement/erreur.
TESTED: YES — 26/26 tests ; les quatre combinaisons des préférences sont vérifiées par aller-retour JSON et un test vérifie la compatibilité d'un ancien fichier ne contenant pas OpenOnLogin. Paquet et versions source/manifest/DLL concordants.

VALIDATED_IN_GAME: Confirmation utilisateur « Tous est OK » reçue après l'implémentation et le build des deux options. La déclaration est consignée comme acceptation du comportement demandé ; aucune capture de version, trace d'événement ou log supplémentaire n'a été fourni. Les tests automatisés ne déclenchent pas l'événement natif Login.

Le protocole utilisateur courant est accepté. Les cas avancés non explicitement exercés (échec de sauvegarde forcé, conflit volontaire de commande, rechargements répétés avec instrumentation, conteneur non chargé provoqué, second personnage) restent des vérifications complémentaires avant publication ; aucune réussite de ces cas n'est revendiquée.

## UI-002 — version 0.1.3 : NQ, HQ et total simultanés

Demande utilisateur : afficher un total HQ/NQ. MainWindow présente désormais NQ, HQ et NQ + HQ ensemble pour l'objet recherché ; le filtre HQ est remplacé par cet affichage simultané. Les deux requêtes utilisent la même instance immuable de snapshot et la même portée. Les lectures incomplètes restent explicitement des minimums, et Unknown n'est pas présenté comme zéro.

COMPILED: YES — Verify.ps1 terminé avec code 0 après modification, Release 0.1.3.0, zéro avertissement/erreur.
TESTED: YES — 26/26 tests existants, paquet et concordance des versions vérifiés. Les tests existants couvrent la séparation NQ/HQ et la complétude ; ils ne rendent pas la nouvelle présentation ImGui.
VALIDATED_IN_GAME: NO pour le nouvel affichage simultané. Essai ciblé après rechargement : ID 12892, vérifier NQ 0, HQ 156, total 156 si inventaire inchangé ; aucun filtre HQ à manipuler.

## ITEM-001 — version 0.1.4

Suite au retour « parfait continue », ajout de la recherche par nom et ID. Ce retour est une acceptation de la présentation précédente ; aucune nouvelle capture du rendu 0.1.3 n'est disponible.

COMPILED: YES — Verify.ps1 exécuté après intégration : Release 0.1.4.0, zéro avertissement/erreur.
TESTED: YES — 31/31 tests managés et paquet/version vérifiés. Cinq groupes nouveaux couvrent casse/accents/mots dans un ordre différent, ID exact/inconnu, résultats bornés/déterministes, copie/source/version/génération et annulation/états publication-erreur. Les tests exécutent ItemCatalog et ItemCatalogService de production ; les données des exemples sont des fixtures, pas une extraction du jeu.
VALIDATED_IN_GAME: NO pour le chargement du catalogue Lumina et l'interface de recherche.

Les signatures locales Item.Name/RowId/CanBeHq, ExcelSheet.Count/GetRowAt/Language et Repository.Version sont vérifiées par métadonnées/XML et compilation. Le premier sondage de métadonnées a utilisé un namespace Repository incorrect ; le type Lumina.Data.Repository a ensuite été inspecté avec succès. Cela n'a pas affecté le build du plugin.

Le loader copie au maximum 128 lignes par Update et s'arrête entre deux lignes après un budget souple de deux millisecondes. Une lecture individuelle de page Excel peut dépasser ce budget ; aucune mesure de latence réelle n'est revendiquée. L'indexation ne reçoit que des ItemDefinition managés et métadonnées copiées, avec annulation au Dispose. Publication exclusivement par Update après achèvement vérifié du worker. Désabonnement à la fin/erreur ; aucun nouvel abonnement au Login pour ces données statiques.

Essai ITEM-001-RUNTIME après le build réussi : recharger 0.1.4, ouvrir /tensho et attendre la fin du chargement du catalogue. Chercher « sirop bouleau », sélectionner « Sirop de bouleau — 12892 » et vérifier nom, identifiant, NQ/HQ/total. Chercher ensuite « potion », et tester 12892 directement dans la recherche. Les autres essais déjà acceptés ne sont pas à répéter. Si une erreur ou un chargement durable survient, conserver le message affiché et les logs Dalamud.

### ITEM-001-RUNTIME : résultat reçu

Source : capture utilisateur codex-clipboard-e454bd2e-7f41-4788-87bf-0ced362301f8.png reçue le 2026-10-27, accompagnée de « tous est OK ».

VALIDATED_IN_GAME: YES dans le périmètre visible : catalogue chargé de 50 880 objets, langue French ; recherche « sirop » donnant plusieurs résultats nommés ; « Sirop de bouleau — 12892 » sélectionné ; champ ID 12892 et nom cohérents ; quatre sacs complets ; NQ 0, HQ 156, total NQ + HQ 156. Horodatage de l'inventaire affiché : 2026-10-27 17:12:56 UTC. Les deux options d'ouverture sont distinctes et visibles.

L'utilisateur confirme globalement l'essai. La capture n'affiche pas une recherche par ID, le test « potion », les performances de construction ni les scénarios d'erreur/annulation ; ces comportements ne sont pas revendiqués comme directement observés par l'agent. Le protocole demandé est accepté sur la base de la capture et du retour utilisateur. Aucun changement de code pour cette consignation, donc aucun nouveau build requis.

## UI-003 — version 0.2.0 : espace de travail commun

Demande utilisateur : améliorer l'interface en tenant compte des différents outils. Fenêtre organisée en cinq pages réelles : Accueil, Objets & inventaire, Tous les outils, Réglages, Diagnostics. Navigation latérale permanente, objet sélectionné conservé entre les pages et quantités en trois colonnes. Les détails techniques des sources sont dans Diagnostics ; les préférences indépendantes dans Réglages. Un seul répertoire regroupe les 17 références futures dans sept domaines ; il ne simule pas des modules métier utilisables.

COMPILED: YES — Verify.ps1, Release 0.2.0.0, zéro avertissement/erreur.
TESTED: YES — 31/31 tests managés existants et paquet/version vérifiés. Vérification statique supplémentaire : les 17 noms de référence du répertoire UI correspondent exactement aux 17 lignes autoritaires de ROADMAP.md. Les services de données et le lifecycle ne sont pas modifiés.
VALIDATED_IN_GAME: NO pour cette nouvelle disposition. Aucun rendu ou test d'interaction native n'a été exécuté par l'agent. Les tests managés ne rendent pas les pages ImGui.

Le premier build a identifié l'absence de ImGuiChildFlags dans le binding installé. L'appel BeginChild avec le paramètre bool de bordure a compilé et la vérification complète a ensuite réussi. Les scopes Begin/End des tables, enfants et listes sont protégés par finally.

Taille de première utilisation : 1040 × 700 ; taille minimale 760 × 480. Les dimensions déjà enregistrées par ImGui restent respectées. Les deux panneaux défilent séparément ; recherche et sélection ne sont pas réinitialisées par le changement de page. Aucun nouvel event, worker, timer ou accès Excel/natif n'est ajouté par cette refonte.

Essai UI-003-RUNTIME après le build réussi : recharger 0.2.0, ouvrir /tensho, vérifier l'Accueil et son bouton d'accès aux objets. Dans Objets & inventaire, chercher puis sélectionner le sirop de bouleau et vérifier NQ/HQ/total. Aller dans Réglages puis revenir : l'objet doit rester sélectionné. Vérifier le répertoire Tous les outils et son filtre, puis Diagnostics. Redimensionner la fenêtre et vérifier l'accès aux pages/quantités. Ne pas recommencer les essais de données déjà acceptés au-delà de ce contrôle de présentation.

## UI-004 — version 0.2.1 : icônes et détails au clic

Demande utilisateur : dans Tous les outils, afficher des icônes et les détails seulement après un clic. La page présente désormais une grille de deux à cinq colonnes selon la largeur. Chaque tuile montre une icône FontAwesome fournie par Dalamud et le nom de référence de l'outil ; les descriptions, domaines et états individuels ne sont affichés que dans une fiche modale après clic. Aucun détail sur simple survol.

Les 17 références prévues sont conservées, chacune avec une icône distincte. Une tuile séparée Obj​​ets & inventaire représente la fondation disponible ; sa fiche dispose d'un bouton Ouvrir l'outil. Les fiches des 17 modules futurs indiquent À venir et ne simulent pas leur disponibilité. Le filtre reste applicable aux noms, références et domaines.

COMPILED: YES — Verify.ps1 réexécuté après la dernière correction, Release 0.2.1.0, zéro avertissement/erreur.
TESTED: YES — 31/31 tests existants, paquet et versions vérifiés ; comparaison statique des 17 références UI avec ROADMAP.md réussie. Les services de données sont inchangés.
VALIDATED_IN_GAME: NO pour la grille et les fiches. Les tests ne rendent pas ImGui et ne déclenchent pas de clic natif.

Vérifications de code : IconButton(string, FontAwesomeIcon, Vector2) et les 18 glyphes utilisés existent dans l'installation locale. L'ouverture de popup est différée après EndTable pour conserver le même niveau de pile d'identifiants que BeginPopupModal. Les scopes EndTable/EndPopup sont protégés par finally ; aucune police, texture, tâche ou ressource externe supplémentaire créée.

Essai ciblé après build : recharger 0.2.1, ouvrir /tensho puis Tous les outils. Vérifier la grille sans descriptions, cliquer une icône future pour afficher sa fiche, fermer et essayer une autre icône. Cliquer Obj​​ets & inventaire puis Ouvrir l'outil pour rejoindre la recherche. Vérifier le filtre et le redimensionnement. Les autres données déjà validées ne sont pas à retester.

## BANK-001 — version 0.3.0

Demande utilisateur : inventaires servant/chocobo, commandes attachées à leurs fenêtres pour import manuel dans une banque persistante, consultation dans les recherches.

COMPILED: YES — Verify.ps1 exécuté après toutes les modifications de code, Release 0.3.0.0, zéro avertissement/erreur.
TESTED: YES — 43/43 tests managés, paquet et concordance versions vérifiés. Douze nouveaux groupes de tests banque couvrent les groupes de conteneurs, séparation servant/personnage/NQ-HQ, remplacement sans double compte, import vide complet, rejets partiels/mauvais propriétaire/conteneurs inattendus, métadonnées UTC et JSON, fichiers corrompus/version inconnue/doublons/quantités impossibles, sauvegarde atomique et secours .bak, erreurs IO avec rollback et retry, chargement/import en cours et absence de publication après Dispose.
VALIDATED_IN_GAME: NO pour les imports servant/chocobo, les noms d'addons retenus et l'ancrage des panneaux. Les tests ne déclenchent pas les véritables menus ni les lectures natives de ces rangements.

La banque et le store ont été implémentés et testés par un worker de développement ; le parent a inspecté les trois fichiers de production et les tests, intégré l'adaptateur/UI, ajouté les bornes de quantités et exécuté la suite complète avec la toolchain .NET 10 locale. Les résultats ciblés du worker ne remplacent pas cette validation complète.

### Portée et sémantique

- Servant : sept pages 10000–10006, clé personnage + identifiant servant ; hors équipements, cristaux, objets en vente et gils.
- Chocobo : sacoche standard 4000/4001 ; extension 4100/4101 importée séparément seulement si disponible et chargée.
- Import explicite uniquement ; il remplace le dernier relevé du même rangement après sauvegarde réussie. Un groupe incomplet ou changeant n'écrase pas l'ancien relevé.
- Les sacs du joueur restent en direct ; les autres stocks sont datés. Le cumul « recensé » mêle stocks actuels et caches et peut être périmé après un transfert. Aucun total global exact de tous les rangements n'est revendiqué.
- JSON dans le dossier de configuration du plugin, InventoryBank.json, schéma 1 ; remplacement atomique et sauvegarde .bak du précédent fichier. Un fichier présent corrompu n'est jamais remplacé silencieusement ; imports bloqués pour le protéger.

### Lifecycle et limites des preuves

Lecture des contextes/positions toutes les 100 ms sur le framework, sans scan des objets tant qu'aucun import n'est demandé. RetainerManager.GetActiveRetainer désigne le dernier servant sélectionné : le code exige aussi inventaire visible, identité stable 500 ms, conteneurs chargés et contrôle avant/après copie. Cette combinaison reste à confirmer en jeu, notamment lors du passage d'un servant à l'autre.

Les panneaux d'import sont des fenêtres ImGui compagnons ancrées à la position gérée du menu, pas une modification de ses nœuds natifs. Draw ne conserve ni lit aucun pointeur natif. Une fermeture entre clic et collecte est revalidée au framework. Les noms InventoryRetainer/InventoryRetainerLarge/InventoryBuddy correspondent aux types locaux inspectés ; InventoryBuddy2 est un candidat de repli, sans observation runtime à ce stade.

Chargement et sauvegarde sont sérialisés sur un worker à file bornée, hors framework. Dispose ferme les futures demandes, termine les tâches publiques sans bloquer et empêche toute publication tardive. Une opération IO synchrone déjà entrée peut finir ; Completion représente la durée de vie publique, pas un arrêt forcé de l'IO. L'essai de rechargement doit attendre le message de sauvegarde avant de désactiver le plugin. Pas de test de crash machine, écriture multi-processus ou annulation forcée d'un appel système revendiqué.

### BANK-001-RUNTIME : essai demandé après build

1. Recharger la DLL 0.3.0. Ouvrir l'inventaire d'un servant : un panneau « TENSHO · Banque d'objets » doit suivre la fenêtre. Attendre son chargement et cliquer Importer / actualiser. Attendre « Import sauvegardé dans la banque ».
2. Fermer puis ouvrir un autre servant et importer. Vérifier que les noms/quantités restent distincts et qu'une nouvelle actualisation remplace le relevé sans doubler les nombres.
3. Ouvrir la sacoche chocobo, importer ; si l'extension est accessible/chargée, l'importer avec son bouton séparé. Ne pas conclure zéro pour une extension indisponible.
4. Fermer les inventaires, ouvrir /tensho → Banque d'objets ou Objets & inventaire, rechercher un objet connu des stocks importés ; vérifier emplacement, NQ/HQ et date. Vérifier que le cache reste consultable sans inventaire servant/chocobo ouvert.
5. Après confirmation de sauvegarde, désactiver/réactiver le plugin : les relevés doivent rester présents. Vérifier, au moment choisi, qu'un changement de personnage n'additionne pas les banques des deux personnages.

Si aucun bouton n'apparaît, vérifier dans Diagnostics les noms de fenêtres détectées et fournir la capture de l'inventaire concerné. Un résultat manquant n'autorise pas à forcer un import ni à retirer les vérifications de chargement.

### BANK-001 : preuve partielle reçue avec la demande de refonte

Source : capture utilisateur codex-clipboard-55b7452f-db39-4732-aca5-13ee28923f23.png, datée du 2026-10-27, fenêtre indiquant v0.3.0. Recherche « cordial », objet Breuvage cordial ID 6141, sacs complets : 182 NQ, 0 HQ, total 182, observation 17:58:29 UTC. Ligne cache Sacoche chocobo : 0 NQ, 26 HQ, total 26, dernier import 2026-10-27 17:58:37 UTC. Cumul recensé affiché : 208.

VALIDATED_IN_GAME: PARTIAL — présence et consultation d'un relevé chocobo et calcul du cumul visibles. La capture ne montre pas le bouton attaché, le processus de collecte, un servant, la réimportation ou un rechargement ; ces points ne sont pas revendiqués comme validés.

## UI-006 — version 0.4.1 : correction des tuiles d'outils

Capture utilisateur codex-clipboard-38906016-8368-4642-9c88-e8522f6f5170.png : boutons très larges et glyphes coupés à gauche dans Tous les outils ; icône du Breuvage cordial visible dans le contexte latéral. Cette preuve ne valide pas encore la page groupée des rangements.

Correction : dimensions positives des boutons (72 × 64 au maximum), dessin du glyphe à 28 pixels centré dans le rectangle réel, légende centrée et grille de 2 à 10 colonnes selon la largeur. Police empruntée à Dalamud uniquement pendant Draw ; fiches toujours ouvertes au clic.

COMPILED: YES — Verify.ps1 Release 0.4.1.0, zéro avertissement/erreur.
TESTED: YES — 55/55 tests, identité/version/contenu du paquet vérifiés, code de sortie 0.
VALIDATED_IN_GAME: YES — retour utilisateur après livraison 0.4.1 : « tous est OK continue ». Correction de l'interface acceptée ; aucune preuve spécifique supplémentaire concernant les imports natifs ou leur persistance n'est déduite de cette confirmation.

## CRAFT-001A — version 0.5.0 : recettes et ingrédients

COMPILED: YES — Verify.ps1 Release 0.5.0.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés. Les tests recette couvrent les variantes d'un résultat, les IDs d'ingrédients, le rendement et les lignes invalides.
LOCAL_VERIFIED: Recipe expose ItemResult, Ingredient, AmountResult, AmountIngredient, CraftType et RecipeLevelTable dans les assemblies locales ; CraftType expose Name et RecipeLevelTable expose ClassJobLevel.
VALIDATED_IN_GAME: NO. Essai demandé après compilation : recharger la DLL 0.5.0, sélectionner le sirop de bouleau, ouvrir « Recettes qui produisent cet objet », puis comparer les lignes affichées au carnet de recettes. Signaler toute différence de recette, rendement, métier, niveau ou ingrédient avec une capture.

Le catalogue est construit une seule fois sur le thread framework par lots de 128 lignes puis indexé sur un worker. Les données managées conservent seulement les IDs et valeurs utiles ; les variantes ne sont pas fusionnées. La possession des ingrédients n'est pas encore affichée dans cette tranche afin de ne pas transformer une absence d'observation en zéro.

## CRAFT-001A-UI — version 0.5.2 : panneau recette et chaîne illustrés

Le bloc recette est maintenant un panneau dédié inspiré de l'interface FF14 : icône du résultat, séparation visuelle, métier/niveau/rendement, icônes des ingrédients et boutons d'accès aux outils liés. Les sections « Produit par » et « Utilisé dans » sont séparées et minimisables. Cliquer sur l'icône ou le nom d'un ingrédient le place dans la recherche commune et empile l'objet précédent dans un historique ; le bouton Retour remonte la chaîne. Le résultat d'une recette liée est lui-même cliquable. Les boutons Craftimizer, Item Use, GatherBuddy et Price Insight ouvrent Tous les outils avec le filtre correspondant ; tous les futurs outils restent accessibles dans le répertoire général.

COMPILED: YES — Verify.ps1 Release 0.5.2.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier après rechargement le rendu du panneau, les icônes, les deux sections, un clic ingrédient et Retour.

## CRAFT-001A-STOCKS — version 0.5.3

Chaque ingrédient affiche désormais la disponibilité observée « NQ … · HQ … ». Les données sont calculées depuis les snapshots de sacs et les caches de banque déjà chargés, avec « stock inconnu » ou « partiel » lorsque la couverture ne permet pas une affirmation complète. Le cache d'affichage est invalidé quand le personnage, le snapshot live ou la banque changent.

COMPILED: YES — Verify.ps1 Release 0.5.3.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier les quantités affichées pour les ingrédients et l'absence de faux zéro.

## UI-005-ORGANIZATION — version 0.5.4

La page Objets & rangements est organisée en quatre onglets : Recherche, Stocks, Craft lié et Imports. L'objet sélectionné reste partagé entre les onglets ; la recherche ne pousse plus les tableaux de stocks, le craft et la gestion des imports dans une seule colonne. La séparation est vérifiable sans modifier les données ni la banque.

COMPILED: YES — Verify.ps1 Release 0.5.4.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier la navigation entre les quatre onglets avec le sirop de bouleau sélectionné.

## CRAFT-001A-UI-FIX — version 0.5.5

Les noms d'ingrédients utilisent maintenant une ligne sélectionnable alignée à gauche au lieu d'un bouton centré étiré. L'icône garde une taille carrée fixe ; la quantité de recette et la disponibilité NQ/HQ restent à droite.

COMPILED: YES — Verify.ps1 Release 0.5.5.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier le rendu des lignes avec le sirop de bouleau.

## CRAFT-001A-UI-FIX-2 — version 0.5.6

La zone de sélection d'un ingrédient est maintenant limitée à la largeur du nom et d'une marge de 20 pixels. L'espace de quantité et de disponibilité reste hors de la zone de recherche.

COMPILED: YES — Verify.ps1 Release 0.5.6.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier le survol et le clic autour du nom, de la quantité et du stock.

## UI-003-TOOLS — version 0.5.7

Accueil et Tous les outils partagent maintenant une seule destination : « Accueil & outils ». Elle conserve la grille d'icônes, le filtre et les fiches détaillées au clic. Les liens depuis le craft reviennent dans cette même page avec le filtre de l'outil concerné. Objets & rangements, Réglages et Diagnostics restent des destinations séparées.

COMPILED: YES — Verify.ps1 Release 0.5.7.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier le menu fusionné et un lien Craftimizer depuis Craft lié.

## CRAFT-001A-STOCKS-2 — version 0.5.8

Les lignes d'ingrédients indiquent maintenant `OK` quand NQ+HQ couvre la quantité requise, ou `Manque X` quand le stock observé est insuffisant. Un stock inconnu reste `stock inconnu` et un stock partiel reste `partiel`.

COMPILED: YES — Verify.ps1 Release 0.5.8.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier un ingrédient possédé et un ingrédient manquant.

## CRAFT-001A-CRAFTABLE — version 0.5.9

Chaque recette affiche maintenant le nombre de crafts possibles, calculé comme le minimum des stocks disponibles divisés par les quantités requises. Une couverture complète affiche un nombre exact ; une couverture partielle affiche `≥ X` car un rangement inconnu peut contenir davantage ; sans observation, le résultat reste inconnu.

COMPILED: YES — Verify.ps1 Release 0.5.9.0, zéro avertissement/erreur.
TESTED: YES — 57/57 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier le nombre affiché pour le sirop de bouleau et ses recettes liées.

## CRAFT-001A-CRYSTALS — version 0.5.10

Le lecteur live capture maintenant les quatre sacs et `GameInventoryType.Crystals` (2001). Les stocks de craft demandent explicitement ce rangement en plus des sacs et des caches ; l'affichage général des quatre sacs reste inchangé. Le diagnostic nomme séparément la ligne Cristaux.

COMPILED: YES — Verify.ps1 Release 0.5.10.0, zéro avertissement/erreur.
TESTED: YES — 58/58 tests, paquet et versions vérifiés, dont un test de projection avec cristaux.
VALIDATED_IN_GAME: NO — vérifier une recette contenant des cristaux et la variation du nombre de crafts possibles.

## UI-ITEMS-CRYSTALS — version 0.5.11

Un cinquième onglet « Cristaux » est ajouté à Objets & rangements. Il reprend le principe FF14 de grille compacte avec icône, quantité et infobulle ; les quantités HQ éventuelles sont affichées après la quantité NQ. Cliquer sur une icône sélectionne le cristal dans la recherche commune. Le rangement 2001 est distinct des quatre sacs et reste source du calcul de craft.

COMPILED: YES — Verify.ps1 Release 0.5.11.0, zéro avertissement/erreur.
TESTED: YES — 58/58 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier la grille, les quantités, l'infobulle et le clic vers Recherche.

## UI-ITEMS-CRYSTALS-GRID — version 0.5.12

L'onglet Cristaux est organisé en tableau : les six éléments Feu, Glace, Vent, Terre, Foudre et Eau sont en colonnes ; Éclats, Cristaux et Agrégats sont en lignes. Les noms localisés du catalogue français servent uniquement à classer l'affichage ; les IDs et quantités restent ceux du rangement natif. Les éléments non classés sont conservés sous une section séparée.

COMPILED: YES — Verify.ps1 Release 0.5.12.0, zéro avertissement/erreur.
TESTED: YES — 58/58 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier le tableau avec les quantités montrées dans la capture.

## UI-ITEMS-CRYSTALS-WATER — version 0.5.13

La classification des éléments accepte maintenant les formes françaises « de X », « d'X » et « d’X ». Les éclats, cristaux et agrégats d'eau ne doivent plus apparaître dans Cristaux non classés.

COMPILED: YES — Verify.ps1 Release 0.5.13.0, zéro avertissement/erreur.
TESTED: YES — 58/58 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier la colonne Eau après rechargement.

## UI-ITEMS-CRYSTALS-CLASSIFIER — version 0.5.14

La classification des cristaux est isolée dans `CrystalClassifier`, avec normalisation des accents et des apostrophes. Les tests couvrent « Éclat d'eau », « Cristal de foudre », « Agrégat de feu » et le rejet d'un objet sans élément.

COMPILED: YES — Verify.ps1 Release 0.5.14.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier le résultat dans le client.

## CRAFT-001A-YIELD — version 0.5.15

Chaque recette affiche désormais séparément le nombre de crafts possibles et la quantité finale produite, calculée avec le rendement de la recette. Une borne partielle conserve le signe `≥` sur les deux valeurs.

COMPILED: YES — Verify.ps1 Release 0.5.15.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier une recette à rendement supérieur à 1.

## CRAFT-001A-NAVIGATION — version 0.5.16

Le panneau Craft lié affiche maintenant un fil `PARCOURS` quand plusieurs objets ont été traversés. Chaque niveau précédent est cliquable et reconstruit l'historique restant ; le bouton Retour continue de remonter d'un seul niveau.

COMPILED: YES — Verify.ps1 Release 0.5.16.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier une chaîne de plusieurs ingrédients et le clic sur un niveau du fil.

## UI-ITEMS-TAB-ORDER — version 0.5.17

L'ordre des onglets est maintenant Recherche, Stocks, Craft lié, Cristaux, Imports. Cristaux se trouve juste avant Imports comme demandé.

COMPILED: YES — Verify.ps1 Release 0.5.17.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier l'ordre visuel des onglets.

## UI-ITEMS-TAB-ORDER-FIX — version 0.5.18

L'identifiant ImGui de la barre d'onglets est versionné afin d'abandonner l'état visuel persistant de l'ancien ordre. Le code soumet désormais Recherche, Stocks, Craft lié, Cristaux, Imports dans cet ordre.

COMPILED: YES — Verify.ps1 Release 0.5.18.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier après déchargement/rechargement complet.

## CRAFT-001A-DIAGNOSTICS — version 0.5.19

Diagnostics affiche maintenant l'état du catalogue de recettes : préparation, progression, erreur ou nombre total de recettes et source de données. Cet affichage n'invente aucune recette et reprend l'état publié par le loader existant.

COMPILED: YES — Verify.ps1 Release 0.5.19.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier la ligne Catalogue de recettes dans Diagnostics.

RUNTIME_EVIDENCE: Capture utilisateur codex-clipboard-a550722d-0718-4b88-951d-08981b984d0d.png — Diagnostics affiche Plugin 0.5.19, catalogue Item 50 880 objets en français, catalogue recettes 13 892 recettes, quatre sacs complets et Cristaux complets. Cette capture valide le chargement des catalogues et la couverture du rangement cristaux ; elle ne valide pas encore l'exactitude d'une recette affichée dans le panneau Craft lié.

RUNTIME_EVIDENCE_CRAFT: Capture utilisateur codex-clipboard-ca1ae29d-eb65-4082-876e-e4452498701a.png — onglet Craft lié chargé sur « Marrons glacés collectionnables », fil « Sirop de bouleau › Marrons glacés collectionnables », recette Cuisine niveau 60, rendement 1, cinq ingrédients avec icônes/stocks et « ≥ 51 craft(s) → ≥ 51 objet(s) ». La borne est cohérente avec la couverture partielle affichée.

RUNTIME_EVIDENCE_BANK: Capture utilisateur codex-clipboard-5206a2cd-c29f-4b5c-bca7-e949e23414a8.png — onglet Stocks montre Personnage en direct, Sacoche chocobo, Sacoche chocobo supplémentaire et deux servants Shirune/Zet-suna avec cache manuel, dates UTC et message « Import sauvegardé dans la banque ». Cette preuve valide les imports et leur séparation par rangement ; un transfert ultérieur nécessite toujours un nouvel import manuel.

## USE-001 — version 0.5.20

La fiche Item Use du répertoire ouvre maintenant Objets & rangements pour l'objet conservé en contexte, avec la section Utilisé dans du Craft lié. La capacité reste PARTIAL : elle couvre les usages présents dans le catalogue Recipe et ne revendique pas encore les usages hors recettes.

COMPILED: YES — Verify.ps1 Release 0.5.20.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier le bouton depuis la fiche Item Use avec le sirop de bouleau sélectionné.

RUNTIME_EVIDENCE_USE: Capture utilisateur codex-clipboard-a020a8da-a6b6-4f3e-8e63-edddd1b4bb3a.png — Accueil & outils affiche Item Use, l'objet en contexte « Sève de bouleau » est conservé, la fiche indique Fondation disponible et le bouton « Ouvrir les usages de l'objet » est visible. Le clic final vers Craft lié reste à vérifier séparément.

## USE-001-NAVIGATION — version 0.5.21

Le bouton Item Use demande maintenant à Objets & rangements de sélectionner directement l'onglet Craft lié à l'ouverture de la page. L'objet en contexte reste inchangé.

COMPILED: YES — Verify.ps1 Release 0.5.21.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier le clic final depuis la fiche Item Use.

## USE-001-PREVIEW — version 0.5.22

La fiche Item Use affiche les recettes connues qui utilisent l'objet en contexte, avec icône, nom, métier et niveau. Chaque résultat est cliquable et ouvre directement cet objet dans Craft lié. La quantité de crafts possibles reste affichée dans Craft lié.

COMPILED: YES — Verify.ps1 Release 0.5.22.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — vérifier l'aperçu et le clic d'un résultat depuis la fiche Item Use.

## BANK-001 — version 0.5.22

Un outil Banque d'objets est disponible dans Accueil & outils. Il reprend maintenant une grille de cases inspirée de l'inventaire FF14 : icônes larges, quantité sous chaque case et tooltip détaillé au survol. Il regroupe les objets présents dans les imports servant et chocobo du personnage connecté, avec NQ, HQ, total et rangements sources. Une recherche et un clic sur l'icône placent l'objet dans le contexte commun ; les onglets Recherche, Stocks et Craft lié peuvent ensuite être utilisés.

COMPILED: YES — Verify.ps1 Release 0.5.22.0, zéro avertissement/erreur.
TESTED: YES — 59/59 tests, paquet et versions vérifiés.
VALIDATED_IN_GAME: NO — ouvrir Banque d'objets après un import, vérifier les icônes et sélectionner une ligne.

## UI-005 — version 0.4.0 : icônes et rangements regroupés

Demande utilisateur : vraies icônes d'objets et interface groupée pour l'inventaire et les autres rangements. Une seule destination Objets & rangements remplace les deux vues séparées. Recherche illustrée et fiche de l'objet ; synthèse NQ/HQ/total recensé ; rubriques repliables Personnage, Chocobo, Servants ; filtres Tous/Personnage/Chocobo/Servants ; gestion des imports dans une section repliable.

COMPILED: YES — dernière exécution de Verify.ps1, Release 0.4.0.0, zéro avertissement/erreur.
TESTED: YES — 55/55 tests, paquet et versions vérifiés. Douze nouveaux groupes testent ItemHoldings de production : cas 182 NQ + 26 HQ = 208 sans exactitude globale, servants distincts, portée des quatre sacs seulement, isolation des personnages, personnage non connecté, sacs manquants, rangements non importés, zéro importé complet, extension optionnelle, couverture partielle, filtres, immutabilité et dépassements arithmétiques. Le test catalogue vérifie aussi la conservation d'IconId en recherche et lookup.
VALIDATED_IN_GAME: NO pour les nouveaux rendus d'icônes et la disposition 0.4.0. Les anciennes captures ne prouvent pas ces nouveaux chemins.

L'icône provient de Lumina Item.Icon, copiée comme identifiant managé dans le catalogue. ITextureProvider fournit une texture partagée pour la frame via TryGetFromGameIcon/TryGetWrap, sans attente, cache local de wraps ou ressource possédée à libérer. Si l'icône n'est pas encore disponible, son emplacement reste réservé ; la recherche et la sélection continuent de fonctionner.

Le modèle de regroupement a été implémenté et testé séparément par un worker, puis entièrement inspecté par le parent et testé avec le plugin intégré. Les essais isolés ne remplacent pas les 55 tests du projet principal. La dernière vérification complète a terminé avec code 0 ; un premier appel groupé avait fini sur le code 1 de rg parce que les anciens symboles Page.Bank/DrawBankHoldings n'existaient plus, alors que le build et ses tests avaient déjà réussi.

Le calcul utilise toujours le même personnage, les snapshots live et la banque persistante existante. Seul le filtre Personnage avec quatre sacs complets peut être déclaré exact au présent ; les stocks incluant du cache restent recensés à leurs dates et peuvent être périmés. Les sources jamais importées restent inconnues, sans faux zéro. Aucune migration ni modification des imports/persistance nécessaires.

Essai UI-005-RUNTIME après compilation : recharger 0.4.0, ouvrir Objets & rangements, chercher « cordial ». Vérifier icônes dans les résultats et la fiche sélectionnée. Contrôler les rubriques Personnage/Chocobo/Servants et les filtres ; le cas de la capture doit donner 182 NQ, 26 HQ, total recensé 208 si les données n'ont pas changé. L'affichage Personnage seul doit montrer le total exact des sacs ; les sources inconnues ne doivent pas devenir zéro. Vérifier une fenêtre large (recherche à gauche, fiche à droite) puis étroite (blocs empilés). Les imports manuels restent accessibles dans leur section et dans les panneaux attachés aux inventaires.
