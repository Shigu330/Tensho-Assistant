# Vérifications

## Comment vérifier (relu le 2026-11-04)

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
  `dotnet`). Le test des infobulles de réglages lit `src/TenshoAssistant` et ses langues ; ceux de la compagnie libre lisent
  `src/TenshoAssistant/FreeCompany/Data/`.
- Dernier passage : ligne « Tests » de la dernière entrée du [CHANGELOG](../../CHANGELOG.md). En CI, à chaque push : `Build and test` (dont une compilation à la manière du dépôt Dalamud, [relevé](../evidence/0.512-d17-build.md)) et CodeQL.

Les tests couvrent la logique pure : des fichiers `*Tests.cs`, un par sujet ou par version (inventaire, banque et tri, artisanat et
macros, récolte et pêche, météo et alarmes, carte et projection, marchands PNJ, collections, quêtes, sources Garland / wiki /
LuminaSupplemental, cache disque, langues, Mini Cactpot, Triple Triade…), plus un groupe écrit directement dans `Program.cs`. Rien ne
s'inscrit à la main : chaque classe statique qui a des `Cases` est trouvée seule par réflexion (`TestRegistry`, `TestRunner.cs`, depuis
0.1275) ; leur nombre et celui des tests : ligne « Tests » de la dernière entrée du CHANGELOG. Un test qui ne peut pas tourner sur le
système (lien local de l'application, Windows seulement) est compté à part, « sauté », jamais comme réussi (depuis 0.1611).
Ils ne couvrent pas : lecture de la mémoire du jeu (sacs, atelier de la compagnie, barre du serveur, cabine d'essayage…), feuilles Excel
réelles (vérifiées par les sondes de `tools/`, sorties dans [docs/evidence/](../evidence/)), rendu ImGui et clics, requêtes HTTP réelles
(Universalis, Garland, wiki, GitHub, Teamcraft : transport simulé ou extraits enregistrés).

### Inventaire des tests

Sortie : une ligne `PASS <nom>` (ou `FAIL <nom>: …`) par test, puis `N/N passed`. Le nombre courant est donné par `Verify.ps1` et
repris dans la ligne « Tests » de chaque entrée du [CHANGELOG](../../CHANGELOG.md). L'ancien inventaire (569 tests au 0.315.0) a
été retiré en 0.1075 : il reste dans l'historique git (version 0.1074.0 : `git show 8bda2ca:docs/project/TESTING.md`).

### Essai en jeu

1. Charger `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll` dans les plugins de développement Dalamud ;
   vérifier la version affichée dans la fenêtre (`/tensho`).
2. Suivre la liste ci-dessous, ou le protocole de la version dans [CHANGELOG.md](../../CHANGELOG.md).
3. Rapporter : version, ce qui a été observé, captures si possible, et `dalamud.log` / `dalamud.crashhandler.log` en cas d'erreur.
   Seul ce rapport permet d'écrire « validé en jeu » ; il se reporte en cochant la case de l'essai sur cette page.

Contrôles de base à refaire après un changement de cycle de vie : rechargement/déchargement répété sans double callback ni commande
orpheline ; préférence « ouvrir au chargement » respectée ; la carte du jeu (touche M) revient normale une fois le plugin déchargé.

### Essais en jeu (liste produite le 2026-11-27, version 0.1882.0)

<!-- q:public -->
_In English: this page is the author's in-game test list, in French only. Produced on 2026-11-27 for version 0.1882.0: 506 tests done, 592 open, 8 could not be tried, 2 failed; the boxes of these tests follow that count. 183 other ticked boxes come from before the list was kept as data and are not counted._

Compte de la source des essais le 2026-11-27 (version 0.1882.0) : 506 faits, 592 ouverts, 8 pas pu, 2 en échec ; les cases de ces essais suivent ce compte. 183 autres cases cochées viennent d'avant la source (essais rapportés avant qu'elle existe, gardés tels quels) : elles ne comptent pas.
<!-- /q:public -->

Parcours de la campagne de la phase 6, les mêmes essais rangés par activité : [TESTING-ROUTE.md](TESTING-ROUTE.md) (2026-11-09).

Une étape impossible (pas de compagnie, pas de sous-marin…) se rapporte « non essayé », pas « OK ».
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

- [x] 0.235–0.237 — Capture automatique active (Paramètres › Données), attendre au plus 2 min : Rangements › Banque montre « Arsenal ».
  Ouvrir une fois le coffre à apparence et l'armoire (auberge) : leurs sections apparaissent (avant : « Ouvrez une fois… »). Les trois
  s'affichent en pages de 35 cases, sans « ordre visuel indisponible ».
- [x] 0.240 — Tenue complète rangée au coffre : ses pièces comptent comme possédées (stocks, « possédé » de la Revue de mode).

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
- [x] 0.259 / 0.262 — Paramètres de la carte : chaque réglage a son infobulle ; un curseur ou une case changé fait apparaître ↺, qui
  remet la valeur par défaut.
- [x] 0.308 — Option « Menus et outils par-dessus la carte » : le niveau de zoom sous la colonne d'outils est lisible (fond sombre
  arrondi, texte clair).

**Infobulles, menus et fiche objet (0.247–0.251, 0.255–0.257, 0.263, 0.272–0.273, 0.282, 0.292)**

- [ ] 0.247–0.250 — Dans plusieurs listes (butin des monstres, coût de préparation, sources de pêche et de récolte, usages,
  orchestrion, hauts faits, résultats de recherche, monnaie d'un marchand) : icône et nom d'un objet donnent la même infobulle complète,
  une seule à la fois ; clic = objet en contexte ; clic droit = outils. Mode dev : ID de l'objet dans l'infobulle.
- [x] 0.251 — Clic droit sur un objet : sources en sous-menus par type avec leur nombre (Hauts faits, Contenus, À partir d'autres
  objets, Échanges, PNJ vendeurs, Quêtes) ; Fabrication et Récolte sur une ligne ; aucune ligne en double.
- [x] 0.255 — Récompenses de quête et Revue de mode : le nom de l'objet a l'infobulle complète et le clic droit.
- [x] 0.256 — Inventaire du jeu, clic droit sur un objet › Tensho : sous les outils, les mêmes sous-menus de sources que dans Tensho ;
  « ↳ Drapeau sur la carte » sous un PNJ placé.
- [x] 0.257 — Survol d'un objet de l'inventaire du jeu : le panneau Tensho sous l'infobulle montre aussi « Comment l'obtenir ».
- [x] 0.263 — Clic droit sur une quête (liste, quêtes liées, onglet Quêtes de la carte), un PNJ (PNJ vendeurs, onglets Vendeurs /
  Triple Triad de la carte, PNJ de Triple Triade) et un monstre (monstres d'un objet) : mêmes entrées partout (ouvrir l'outil ; pour
  chaque position : drapeau, « Voir sur la carte Tensho », météo de la zone).
- [x] 0.272 — Objets : choisir un objet = fiche complète (icône, stock total, dernier prix connu et sa date ; onglets Aperçu, Stocks,
  Craft lié, Usages, Récolte, Marché).
- [ ] 0.273 — Liens : objet d'une alarme, « Comment l'obtenir (fiche) », monnaie d'un échange → fiche sur Aperçu ; « Prix du marché » →
  Marché › Résumé ; « Ouvrir l'outil Triple Triade » depuis un PNJ → « PNJ et cartes » ; bouton « Deck » des réglages → « Mon deck ».

**Paramètres, caches et langues (0.246, 0.248, 0.258, 0.260–0.261, 0.264, 0.281)**

- [x] 0.258 — Paramètres › Avancé › « Tout remettre par défaut » (confirmation) : réglages remis ; gardés : langue, alarmes,
  préréglages d'artisanat, decks, quêtes suivies, favoris de carte, plan de macros, profils et modèles de tri, relevés, et depuis 0.315
  vaisseaux lus et plans d'équipement / de matérias ; la Banque n'est pas touchée.
- [ ] 0.246 (facultatif) — Hors ligne : `dalamud.log` ne montre pas de téléchargement LuminaSupplemental / GatherBuddy relancé à
  chaque image (5 min entre deux essais) ; les sources gardent leur copie expirée.

**Rappels et barre du serveur (0.300–0.302)**

- [x] 0.300 — MONDE › Rappels, comparer au Calendrier du jeu : missions de ravitaillement, commandes spéciales (livraisons restantes),
  Revue de mode, quêtes quotidiennes des peuples alliés (restantes), autorisations de mandats, cartes au trésor (« Carte ouverte »),
  tâches de servant (après ouverture de la liste à une sonnette) : état et « Prochaine fois » ; « Fait » coché se décoche seul à la
  remise suivante ; la cloche crée une alarme (page Alarmes).

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

**Relevés en jeu demandés (preuves, pas des réglages)**

- [x] Mandat accepté, carte ouverte : relever le type de marqueur d'un mandat (repères au bord « Mandats et autres événements », 0.298).

**Encore listés « à valider » dans STATUS avant 0.232**

- [x] 0.224 — Mineur / botaniste, objet récolté en contexte : outil PR, « Estimé : +N » à côté du bonus, bouton « Utiliser ».
- [x] 0.225 — Panneau « Aide à la récolte · Mineur / Botaniste » à droite de la fenêtre de récolte du jeu, un métier à la fois.
- [ ] 0.226–0.228 — Aide à la pêche : poissons du lieu, appâts, créneau, météo ; GatherBuddy d'abord, Garland en secours ; bouton
  « Ouvrir l'aide à la pêche » et `/tensho peche` (détection « pêche en cours » déjà vue en jeu, capture 2026-11-01). Pêche en mer non
  couverte.
- [ ] 0.230 — Page Récolte : outils de l'objet en tête ; pour un poisson, « Comment le pêcher · GatherBuddy » en premier.

**Webhook Discord (R-12, 0.319)** — à faire pendant la campagne de tests en jeu de la fin (phase 6), avec un salon de test

- [x] Paramètres › Alarmes › Discord : coller l'adresse d'un webhook d'un salon de test, quitter le champ : « Adresse enregistrée :
  https://discord.com/api/webhooks/1234…/*** » ; une adresse d'un autre site reste dans le champ avec le message rouge, non enregistrée.
- [x] « Envoyer un test » : le message d'essai arrive dans le salon, avec le nom « Tensho » ; la ligne d'état dit « Message envoyé. ».
- [x] Activer l'envoi, créer une alarme manuelle (page Alarmes) à une heure ET proche : à la sonnerie, le message arrive sur Discord
  (et le chat et le son du jeu comme avant).

**Page Crédits (R-13, 0.320)** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Filtre (« MIT », « Garland »…) ; « Copier le lien » et « Copier la licence » remplissent le presse-papiers ; colonne Usage
  renvoyée à la ligne, sans texte coupé à une largeur de fenêtre réduite.

**Point d) — 0.321** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Paramètres › Modules › Artisanat : changer « Étapes », « Largeur », « Budget », « Attente » et « Écho », recharger le plugin : les
  valeurs restent ; le bouton ↺ remet chacune ; la case « Aide du carnet » s'enregistre au clic et montre son infobulle au survol.
- [x] Paramètres › Avancé › « Revue de mode (FFXIVGachaSpreadsheet) » décoché : la page Revue de mode dit « source désactivée » quand
  aucune copie n'est gardée, sans téléchargement.

**Point d) — 0.324 : interrupteurs de sources** — pendant la campagne de tests en jeu de la fin (phase 6)


**Point d) — 0.325 : infobulles, confirmations** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Survol des filtres, recherches, tris et champs de saisie des pages (Banque, Collections, Butin, Quêtes, Récolte, Objets,
  Artisanat, Marché, Cartes, Rangement, Triple Triad, Revue de mode, Équipement, Météo…) : chacun montre une infobulle, en
  français et en anglais, sans texte coupé.
- [ ] Panneau du Duty Finder : la recherche accepte l'identifiant exact d'un objet ; ↻ a son infobulle ; la liste d'origine du
  butin aussi ; aucun ralentissement en changeant de filtre.

**Point d) — 0.326 : corrections de la revue** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Duty Finder, « non obtenus seulement » coché juste après la connexion : la liste se remplit une fois les collections lues.
- [x] Revue de mode coupée : « Actualiser » grisé avec son infobulle ; Accueil sans « service en erreur » quand Universalis est coupé.
- [ ] Banque (client anglais) : les titres des cadres de la sacoche et de l'arsenal sont en anglais.

**Étape e) — 0.327 : réglages** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Paramètres › Interface : nombre de résultats à 50 → la recherche d'objets en liste 50 et l'avis dit 50 ; ↺ remet 30.
- [x] Paramètres › Général : « dernière page » coché, aller sur Carte, fermer, recharger le plugin → s'ouvre sur Carte ; décoché → Accueil.
- [ ] Survol : décocher chaque partie (collection, stock, usages, comment l'obtenir) → elle disparaît du panneau sous l'infobulle du jeu
  seulement ; « masquer en mission » coché → pas de panneau dans un donjon.
- [x] Triple Triad (observation activée) : chacun des trois interrupteurs coupe son aide (cadre du match, défi d'un PNJ, éditeur de decks).
- [ ] Carnet d'artisanat : « calcul automatique » décoché → bouton « Calculer » ; le décocher pendant un calcul garde « Calcul… ».
- [ ] (0.327) Page Alerte › onglet Réglages : cochez « Pas de son en mission » puis, dessous, « Pas de message dans le chat non plus ». En donjon, quand une alarme sonne, aucune ligne [Tensho] n'apparaît dans le chat.

**Étape e) — 0.328 : carte** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] « Remplacer la carte du jeu » et « Masquer en combat » cochés : M en combat montre la carte du jeu (plus d'écran sans carte).
- [x] Carte Tensho ouverte par M, bouton « Ouvrir la carte du jeu » : la carte du jeu revient à l'écran.
- [x] Zoom +, − et curseur sans suivre le personnage : la vue reste centrée au même endroit.
- [x] Repère au bord du drapeau au-dessus d'une icône de vendeur : un clic centre la carte sans déplacer le drapeau.
- [x] Fenêtre de téléportation : le coût s'affiche (ou « indisponible » si la lecture échoue, jamais « Lecture… » sans fin).
- [x] Favoris : un nom en double dit « déjà utilisé », pas « Échec de sauvegarde » ; page Cartes, le bouton pose le drapeau.
- [x] Liste des panoramas (clic droit) : clic droit sur une ligne → alarme proposée ; numéro identique à l'infobulle.
- [x] Ville chargée : la carte reste fluide (vendeurs mis en cache), temps à relever dans Diagnostics si besoin.

**Étape e) — 0.329 : objets, banque, tri, marché** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Coffre à apparence très rempli : l'import s'enregistre et se recharge après un rechargement du plugin.
- [ ] Fiche d'un objet ouverte directement (onglet Aperçu) : le stock de l'en-tête est juste ; pour un cristal, les servants montrent « ≥ ».
- [x] Survoler vite plusieurs objets avec prix au survol : pas de « prix indisponibles » pendant la pause de 3 s.
- [ ] Historique des objets : « Retour » revient à l'objet précédent ; le fil d'Ariane reste court.

**Étape e) — 0.332 : cadre du jeu** — retiré en 0.510 (DEC-033), plus rien à tester


**Étape f) — 0.333 : escouade** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Mission d'escouade lancée : Rappels › « Mission d'escouade » donne son nom et une heure de fin égale à celle du jeu ; la cloche crée
  une alarme qui sonne à ce moment ; une fois finie, « Terminé (à récupérer à la caserne) ».

**Étape f) — 0.334 : seuils des sous-marins** — pendant la campagne de tests en jeu de la fin (phase 6)


**Étape f) — 0.335 : valeur des trajets** — pendant la campagne de tests en jeu de la fin (phase 6)


**Étape f) — 0.336 : secteurs de dirigeable** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Compagnie › « Secteurs de dirigeable » : 23 secteurs de la Mer de nuages (pas le Diadème), rang, céruleum, durée de relevé et
  expérience égaux au panneau de vol du jeu ; « Débloqué par » cohérent avec l'ordre de découverte en jeu.
- [x] Paramètres › Crédits : LuminaSupplemental apparaît deux fois (téléchargé ; embarqué pour les dirigeables).

**Étape f) — 0.337 : planificateur de dirigeables (appelle les fonctions de calcul du jeu)** — pendant la campagne de tests en jeu (phase 6)

- [x] Secteurs de dirigeable, connecté : choisir un dirigeable lu à l'atelier ; plus de « Calcul des étapes par le jeu… » après une seconde ;
  aucun plantage, rien dans le journal Dalamud.
- [x] Cocher les secteurs d'un voyage réel, puis comparer avec le panneau de vol de l'atelier (même dirigeable, mêmes secteurs) :
  distance et durée identiques ? Sinon, noter l'écart et les valeurs « Brut » du mode développeur (unité, 12 h, retour).

**Étape f) — 0.338 : taux de surmatérialisation** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Équipement, pièce avec surmatérialisation : dans l'éditeur, chaque grade d'un emplacement avancé donne son taux ; les mêmes que
  la fenêtre de pose du jeu (ex. : matéria IX, premier emplacement, pièce HQ : 17 %).

**Étape f) — 0.340 : barde de chocobo** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] « n bardes débloquées sur m » comparé au carnet de bardes du jeu ; « Seulement celles qui manquent » ; recherche par nom d'objet.

**Étape f) — 0.342 : événements saisonniers** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Boutiques de l'événement : le vendeur de la fête, ses villes, ce qu'il vend et le prix ; comparer avec la boutique en jeu.

**Étape g) — 0.343 : I-01 infobulle de prix complète** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Survol d'un objet vendable : sous chaque prix, « Dernière vente · moyenne · ventes par jour » ; les mêmes que la page de l'objet
  sur universalis.app pour votre monde.
- [x] Survol d'une pile de l'inventaire : « Pile de N : ~X » (N = la quantité de la case) ; pas de ligne pour un objet hors de vos sacs.
- [ ] (0.343) Survoler un objet vendable et garder Ctrl+Alt enfoncés : une seule nouvelle demande (« Consulté » ne change qu'une fois). Paramètres › Données et sources › Sources communautaires : décocher « Ctrl+Alt actualise le prix survolé » : Ctrl+Alt ne change plus rien.

**Étape g) — 0.344 : I-02 monnaies et alertes de plafond** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] (0.344) Objets › Rangements, onglet « Devises » : soldes et plafonds identiques à la fenêtre Monnaies du jeu (sceaux de votre compagnie au plafond de votre rang, Centurio, gemmes bicolores, assignats, mémoquartz) ; « Mémoquartz limités cette semaine » égal au jeu.
  rang, Centurio, gemmes bicolores, assignats, mémoquartz) ; « Mémoquartz limités cette semaine » égal au jeu.
- [ ] (0.344) Avec deux personnages : connectez-vous sur l'un, puis changez de personnage. À l'arrivée, aucun message d'alerte de plafond de devise dans le chat ; Tensho › Rangements › Devises montre les devises du nouveau personnage, pas celles du premier.

**Étape g) — 0.345 : I-03 vue de tous ses personnages** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Se connecter avec deux personnages, puis Rangements › Personnages : les deux, avec gils, objets des sacs, cristaux, servants.

**Étape g) — 0.346 : I-06 prix PNJ, I-07 sous-cotation** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Survol d'une pile vendable au PNJ : « Vente au PNJ (NQ) : X gils l'unité, pile de N : Y » ; comparer avec la vente à un PNJ.
- [ ] Paramètres › Avancé › « Copier l'offre la plus basse moins 1 » activé : ouvrir les offres d'un objet au tableau des ventes ; le
  chat dit « offre la plus basse … prix copié … » et Ctrl+V colle ce prix ; une offre de vos servants n'est pas prise ; désactivé :
  rien n'est copié.

**Étape g) — 0.347 : I-08 notes et étiquettes d'objets** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Fiche d'un objet › onglet « Note » : écrire une note, cocher « Important » et « À vendre au PNJ » ; la note s'affiche au survol
  de l'objet dans le jeu.
- [ ] Ouvrir une boutique de PNJ avec ces objets dans les sacs : une ligne « À vendre au PNJ … ×n » et une ligne « Attention, objets
  importants … » dans le chat ; une seule fois par ouverture ; avec « Son pour les objets importants », le son des alarmes.

**Étape g) — 0.348 : I-10 rapport de rangement** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Mettre un même objet en deux piles incomplètes dans les sacs : Rangements › Rapport le liste, avec « 1 case libérable ».

**Étape g) — 0.349 : X-07 liens wiki et Lodestone** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Clic droit sur un objet › Tensho : « Wiki (anglais) » et « Lodestone » ouvrent la bonne page dans le navigateur.

**Étape g) — 0.350 : I-09 coffre de compagnie** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Ouvrir le coffre de compagnie : le panneau Tensho apparaît à côté ; tant qu'un onglet n'a pas été affiché, il le dit ; après les
  cinq onglets (et les cristaux), « Enregistrer » ; avec l'enregistrement automatique, c'est fait seul.
- [x] Banque : un onglet « Coffre de compagnie » ; le stock d'un objet du coffre (fiche d'objet, survol) compte la ligne « Coffre de
  compagnie ».

**Étape g) — 0.351 : I-04 journal des gils** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Acheter à un PNJ, vendre à un PNJ, réparer, se téléporter : Rangements › Gils montre chaque variation avec la bonne catégorie
  (Boutique PNJ, Réparation, Téléportation).
- [x] Ouvrir la cloche des servants : le solde des servants apparaît ; ouvrir le coffre de compagnie : son solde apparaît.

**Étape g) — 0.352 : I-11 conditions des articles de boutique** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] (0.352) Page PNJ marchands, un marchand dont un article demande une quête : après le prix, « Après la quête « … » (fait) » ou « (à faire) », comme dans votre journal de quêtes ; un article lié à un haut fait affiche « Après le haut fait « … » ».
  selon votre journal ; un article lié à un haut fait affiche son nom.

**Étape g) — 0.353 : I-12 recherche d'objets avec filtres** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Page Objets › « Filtres » : catégorie « Tête », niveau d'équipement 90 à 100 : la liste ne montre que ces casques, sans rien
  écrire ; avec un mot en plus, la liste se réduit ; « Effacer les filtres ».

**Étape g) — 0.354 : I-13 valeur des monnaies** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] (0.354) Objets › Rangements › Devises : sur les sceaux de compagnie, cliquez « Que faire ? ». La liste montre ce qu'on peut acheter avec, et le prix en sceaux. « Prix du marché » remplit les prix et la colonne « Gils par unité » ; le plus rentable est en haut. Une monture que vous avez déjà est marquée « possédée ».
  « Prix du marché » remplit les prix et « Gils par unité », classés du plus rentable ; une monture déjà possédée dit « possédée ».

**Étape g) — 0.355 : C-01 contenus débloqués / terminés** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Monde › Butin des contenus : la colonne « État » dit « Terminé » pour un donjon fait (ex. Sastasha), « Débloqué » pour un contenu
  ouvert mais pas fait, « Verrouillé » pour un contenu pas encore ouvert ; le compteur « X terminés sur Y lus » est plausible.
- [ ] (0.355) Monde › Butin des missions : filtre « Avancement » sur « Non terminées » : les donjons déjà faits disparaissent de la liste. Déconnecté (écran de sélection des personnages), le filtre est grisé et la colonne « État » montre « — ».

**Étape g) — 0.356 : C-02 missions aléatoires du jour** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] (0.356) Calendrier › Rappels, onglet « Missions aléatoires », cadre « Missions aléatoires du jour » : faites une mission aléatoire (ex. Gain de niveaux). Elle passe à « Faite », les autres restent « À faire ». Le compteur et l'heure de remise à zéro sont justes.
  les autres restent « À faire » ; le compteur et l'heure de remise à zéro sont justes.

**Étape g) — 0.357 : C-03 objets à ranger dans l'armoire** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] (0.357) Rangements › Rapport, bloc « Peut aller dans le bahut personnel » : une pièce qui fait partie d'un de vos ensembles d'équipement du jeu n'y est jamais proposée.

**Étape g) — 0.358 : C-07 apparences identiques du coffre** — pendant la campagne de tests en jeu de la fin (phase 6)


**Étape g) — 0.359 : C-08 filtre des collections par source** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Collections › Montures › Source « Marchands (gils) » puis « Succès » : les montures listées ont bien cette source dans leur menu
  du clic droit ; combiné avec « Non confirmés », la liste se réduit.
- [x] Le premier choix du filtre sur l'Orchestrion ne fait pas saccader le jeu de façon gênante (sinon : calcul à étaler).

**Étape g) — 0.360 : C-09 liste de souhaits** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Clic droit sur une monture dans un butin (Butin des contenus) › « Ajouter à mes souhaits » ; Collections › Souhaits la montre
  avec « Pas encore obtenu » ; clic droit sur la ligne donne ses sources ; « Retirer » l'enlève.
- [x] Un objet ordinaire ajouté montre « Possédé ×N » ou « Pas possédé » ; la liste reste après un redémarrage et après
  « Réinitialiser les réglages ».

**Étape g) — 0.361 : C-11 carnet d'exploration** — pendant la campagne de tests en jeu de la fin (phase 6)


**Étape g) — 0.362 : C-05 tenues du coffre à apparence** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Ouvrir le coffre à apparence une fois. Collections › Tenues : une tenue déjà rangée dit « rangée » ; une tenue dont toutes les
  pièces sont dans le coffre dit « prête à ranger » (le jeu propose bien de la créer) ; les pièces ouvertes disent où elles sont.
- [ ] (0.362) Personnage › Collections › onglet « Ensembles » : essayer les filtres « Incomplètes », « Toutes les pièces » et « Rangées », puis chercher une tenue par son nom : la liste suit chaque filtre et la recherche.

**Étape g) — 0.363 : C-14 mandats** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Progression › Mandats : les allocations restantes et le nombre de mandats acceptés sont ceux du jeu ; un mandat déjà fait dit
  « Terminé » ; filtres type, mandateur, niveau min/max et état.

**Étape g) — 0.364 : C-12 carnet de chasse** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] (0.364) Tensho › PROGRESSION › Bestiaire, le bestiaire de votre classe : « Le jeu en est au rang N » est le même rang que dans le carnet de chasse du jeu ; les monstres tués d'une ligne en cours sont les mêmes que dans le jeu (tuez un monstre : le compteur monte en 2 secondes).
  d'une entrée en cours sont les mêmes que dans le jeu (tuer un monstre : le compteur monte en 2 secondes).

**Étape g) — 0.365 : C-04 nouveaux types de collection** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Collections : les onglets Coiffures, Accessoires de mode, Lunettes, Bardes, Portraits, Livres de maître, Objets de collection,
  Archives occultes se remplissent en quelques secondes ; une coiffure ou un accessoire que vous avez dit « Obtenu », un autre non.
- [ ] (0.365) Tensho › Collections, onglets Coiffures, Accessoires de mode, Lunettes, Bardes de chocobo, Portraits, Livres de maître, Objets de collection, Archives occultes : dans chacun, cochez « Obtenu », puis « Non confirmés », puis tapez un nom dans la recherche : la liste ne garde que ce qui correspond.

**Étape g) — 0.366 : C-10 alerte « mission prête »** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] (0.366) Page Alerte › onglet Réglages, case « Alerte quand une mission est prête » cochée : quand l'outil de mission trouve votre groupe, la ligne « Mission prête : … » apparaît dans le chat avec le son ; case décochée, rien.
  et le son ; décochée, rien.

**Étape g) — 0.367 : C-13 monstres des sorts de mage bleu** — pendant la campagne de tests en jeu de la fin (phase 6)


**Étape g) — 0.368 : C-15 progression des succès** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Ouvrir un succès en cours dans la fenêtre des succès du jeu : dans Tensho, sa fenêtre montre « Progression : x / y ».

**Étape g) — 0.369 : X-03 titres** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Collections › Titres › un titre obtenu › « Porter ce titre » : le titre change au-dessus du personnage ; « Aucun titre » le retire ;
  « Titre au hasard » en met un autre.
- [x] « Titre de <job> » puis « Changer de titre avec le job » coché : changer de job porte le titre choisi ; décoché : rien ne change.

**Étape g) — 0.370 : X-02 musique en cours, rouleaux favoris** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Collections › Orchestrion : « Musique en cours » nomme la musique qu'on entend (en ville, puis en combat, puis dans une maison
  avec un orchestrion) ; « Fiche » ouvre son rouleau.
- [ ] Case « Favori » dans la fiche d'un rouleau : il apparaît dans « Rouleaux favoris ».

**Étape g) — 0.371 : M-01 alarmes automatiques** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Ouvrir la liste des servants à une cloche : dans les 30 secondes, une alarme « Venture de … terminée » par servant en venture
  apparaît dans Alarmes, à la bonne heure ; elle sonne à la fin.
- [x] Envoyer un vaisseau à l'atelier : une alarme « Retour de … » apparaît ; « Alarmes automatiques » décochée : plus aucune n'est créée.

**Étape g) — 0.372 : M-02 compteurs hebdomadaires** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Rappels : « Livre d'or des défis » montre les vignettes et points du livre de Khloe, avec son expiration ; « Contrats de chasse »
  le nombre de contrats en main ; « Cartes au trésor » l'heure de la prochaine carte du jeu (sans bouton « ouverte »).

**Étape g) — 0.373 : M-03 liens de carte du chat** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Envoyer un lien de carte (drapeau puis <flag>) en équipe et en compagnie libre : il apparaît dans Cartes › « Liens de carte du
  chat », avec le bon lieu, les bonnes coordonnées et le bon canal au survol ; le filtre de canal le cache et le remontre.
- [x] « Drapeau » pose le drapeau du jeu au bon endroit ; « Éthérite » ouvre la carte Tensho sur le point et propose l'éthérite
  la plus proche avec son coût ; « Non » ne téléporte pas ; « Oui » téléporte.

**Étape g) — 0.374 : M-04 vos personnages** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Se connecter avec deux personnages l'un après l'autre (ouvrir la liste des servants avec chacun) : Rappels › « Vos personnages »
  montre les deux, ventures, carte au trésor, Livre d'or, contrats et missions aléatoires justes, « Connecté » sur le bon.

**Étape g) — 0.375 : M-05 vos tâches et panneau à l'écran** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Rappels › « Vos tâches » : ajouter une tâche par jour, une toutes les 2 heures et un compteur à 3 ; les cocher, monter le compteur
  avec « + » : chacune dit quand elle revient ; flèches pour changer l'ordre ; « Retirer ».

**Étape g) — 0.376 : M-06 usure et symbiose** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Équipement › « Usure et symbiose » : l'état et la symbiose de chaque pièce sont ceux de la fenêtre du jeu (la symbiose surtout :
  échelle à confirmer).

**Étape g) — 0.377 : M-07 comparaison au survol** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Survoler une pièce d'équipement dans l'inventaire et au marché : sous l'infobulle, « Comparé à l'équipement porté » donne le
  niveau d'objet et les statistiques en plus (vert) et en moins (rouge) par rapport à la pièce portée ; une bague : deux lignes.

**Étape g) — 0.378 : M-08 favoris sur la carte** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Carte Tensho, clic droit › nom › « Ajouter aux favoris » : une étoile apparaît à cet endroit ; survol : nom, coordonnées ;
  clic : le drapeau du jeu se pose dessus.
- [x] Page Cartes : donner un groupe et une note à un favori, « Mettre à jour » : « [groupe] nom » dans la liste, note au survol.
- [x] « Copier le code de partage », le coller dans un autre client (ou après avoir retiré un favori) puis « Importer » : seuls les
  favoris absents sont ajoutés.

**Étape g) — 0.379 : M-10 historique du butin** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Après le retour d'un sous-marin, passer à l'atelier : Compagnie libre › Historique montre le voyage (retour, vaisseau, trajet,
  objets) ; le rendement par trajet compte un voyage ; un second passage n'ajoute pas de doublon.
- [ ] (0.379) Compagnie libre, onglet « Historique » (voyages des vaisseaux) : cliquer « Copier en CSV », puis coller dans un tableur : une ligne par objet rapporté, colonnes lisibles.

**Étape g) — 0.380 : M-09 paliers de statistiques** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Page Équipement (ou panneau près de la fenêtre Personnage) : la colonne « +N palier » est présente ; ajouter une matéria qui
  donne au moins N points de la statistique : la valeur réelle change ; moins de N : elle ne change pas.

**Étape g) — 0.381 : X-01 tâches libres** — pendant la campagne de tests en jeu de la fin (phase 6)


**Étape g) — 0.382 : M-11 autres compteurs** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Ouvrir le carnet d'objectifs du jeu : la ligne « Carnet d'objectifs » des Rappels donne le bon nombre de défis faits (indice des
  lignes à confirmer).
- [ ] (0.382) Où : Tensho › CALENDRIER › Rappels, ligne « Quartier enclavé de Doma » ; en jeu : le Quartier enclavé de Doma. Faire : 1) regardez d'abord la ligne dans Rappels ; 2) allez au Quartier enclavé de Doma et faites un don à la reconstruction ; 3) rouvrez Rappels. Regarder : avant le passage, « Passez au Quartier enclavé de Doma une fois » ; après, « X / Y gils donnés », avec X vos dons de la semaine et Y le plafond que montre le jeu.

**Étape g) — 0.383 : M-12 ensembles d'équipement** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Équipement › « Ensembles d'équipement du jeu » : tous vos ensembles, avec numéro, nom, job et niveau d'objet du jeu.

**Étape g) — 0.384 : M-14 carte au trésor** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Déchiffrer une carte au trésor : dans les 5 secondes, Rappels › Cartes au trésor montre « Voir sur la carte » ; le bouton ouvre
  la carte Tensho sur le bon endroit (comparer avec la carte du jeu) et y pose le drapeau ; l'icône gemme est au même endroit.
- [ ] Ouvrir le coffre : l'icône et le bouton disparaissent.

**Étape g) — 0.385 : X-08 job au hasard** — pendant la campagne de tests en jeu de la fin (phase 6)


**Étape g) — 0.386 : X-10 horloges et alarme à date** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] (0.386) Page Alerte › onglet Horloges : ajoutez « Tokyo » et « New York » : leurs heures sont justes ; « Retirer » enlève une horloge.

**0.387 : signalements corrigés** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Grimoire du mage bleu, « Mauvaise haleine » › « Chercher sur le wiki » : les monstres (Capricious Cassie, Halitostroper…) avec
  « Drapeau » pour ceux qui ont des coordonnées ; un sort appris par quête dit « aucun monstre » avec l'adresse de la page.
- [x] Collections › Coiffures, Accessoires de mode, Bardes, Livres de maître : le survol montre l'objet et comment l'obtenir ; clic droit
  : les sources. Lunettes, Portraits, Objets de collection, Archives : l'infobulle dit « obtention inconnue ». Tenues : survol d'une
  tenue ou d'une pièce = infobulle de l'objet.

**Étape g) — 0.391 : X-06 tic de PR** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Page PR › cocher « Barre du tic de PR à l'écran », en mineur avec des PR pas pleins : la barre se remplit en 3 secondes et repart
  à chaque gain de PR ; elle disparaît à PR pleins, en combat et sur un autre job.

**Étape g) — 0.392 : R-01 pêche en mer** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Page Pêche en mer : l'heure du prochain départ et les trajets (Indigo, Rubis) sont ceux du PNJ du port ; « Embarquement ouvert »
  pendant les 15 minutes ; filtre d'un trajet ; une alarme sonne avant le départ.

**0.537 : s'effacer devant les fenêtres du jeu** — à voir en jouant

- [x] Barre de PR, usure, suivi, tâches : une fenêtre du jeu les touche : elles se cachent. Le chat, les plaques de noms et le HUD ne les cachent pas.
- [x] (rapport de l'utilisateur, 2026-11-05, « fenêtre ok ») Carte, réglage « S'effacer devant les fenêtres du jeu » coché : une fenêtre la touche : elle se cache sans se fermer.
**0.538 et suivantes : un essai par ligne, marqué de sa version** — à voir en jouant


- [x] (rapport de l'utilisateur, 2026-11-05, « tous es ok ») (0.539) Déplacer une fenêtre du jeu qui a un panneau de Tensho (récolte, carnet, personnage, mission) : le panneau la suit sans retard visible.
- [x] (rapport de l'utilisateur, 2026-11-05, « tous es ok ») (0.539) Réglage de la carte activé : un panneau lié qui touche la carte la fait disparaître ; passer d'une infobulle à l'autre ne la fait pas clignoter, et elle revient environ 1 seconde après.

- [ ] (0.540) Journal du jardin : « Graines de centaurée » est dans la liste, avec 24 h ; une plantation de cette graine est reconnue dans le chat. Comparer la durée réelle.

- [ ] (0.541) Mes outils, bloc d'objets : colonne de prix, coût de ce qui manque, bouton « Demander les prix ». Un objet invendable affiche un tiret.

- [ ] (0.542) Carte, réglage « Zoom à l'ouverture » : régler un zoom, relancer le jeu : le dernier zoom revient. Tester aussi « Fixe » et « Carte entière », puis le zoom sur une quête.

- [x] (0.543) Carte : les quatre réglages des noms et des étiquettes de récolte changent bien l'affichage ; aux valeurs par défaut, rien ne change.

- [x] (0.544) Carte : clic droit sur un point, puis sur le drapeau : « Éthérite la plus proche » donne le nom et le coût, et demande confirmation.

- [ ] (0.545) Carte : la molette zoome aussi sur la colonne d'outils et le curseur ; le pas et l'inversion fonctionnent.

- [x] (0.546) Carte, réglage activé : un clic gauche sur un endroit vide pose le drapeau ; un glisser ne le pose pas ; un clic sur une icône garde son effet.

- [x] (0.547) Carte, bouton « Couches » : chaque case change la carte, comme dans les réglages ; « Tout masquer » puis « Tout afficher » ; la colonne tient dans une petite fenêtre.

- [ ] (0.548) Carte : une ALEA à bonus affiche « bonus d'expérience » ; comparer avec l'icône de bonus du jeu. La zone reste rose.

- [ ] (0.549) Porter puis retirer une pièce : la ligne « Équipement porté » suit ; la Revue de mode la dit possédée ; le tri ne la déplace pas.

- [x] (0.550) Quêtes : colonnes Niveau et Zone, tri en cliquant les titres, plage de niveaux gardée après une relance.

- [x] (0.551) Monstres : le bouton carte pose le drapeau à la position de la fiche ; comparer avec la position réelle en jeu.

- [ ] (0.552) Carnet d'artisanat : une séquence enregistrée sur une autre recette de même difficulté est proposée, avec « Enregistrée pour ».

- [ ] (0.553) Production › Artisanat, onglet Simulation : après une simulation finie, la ligne « Cette séquence tient, à cette qualité, avec au moins … d'Habileté, … de Contrôle et … PS (marge …) » s'affiche. Mettez ces valeurs à la place des vôtres : la qualité finale reste la même. En jeu, avec des attributs juste au-dessus de ces minimums, la macro de la séquence atteint la même qualité.

- [ ] (0.554) Porter un métier d'artisan, puis un autre : le carnet calcule la recette du premier avec « relevés le… » ; porter le métier reprend les valeurs actuelles.

- [ ] (0.555) Commandes : « /tensho prix » suivi d'un nom d'objet ouvre le marché de cet objet ; essayer aussi objet, recolte et recette, et un nom inconnu.

- [ ] (0.556) (0.556) Page Alerte › onglet Réglages : cochez « Sonner aussi à l'heure exacte ». Créez une alarme avec quelques minutes d'avance. Regarder : un message dans le chat et un son à l'avance, puis encore à l'heure exacte, une seule fois chacun ; Discord ne reçoit que le premier.

- [x] (0.557) Alarmes : modifier un nom et une date ; « Dans 5 min » ajoute une alarme qui sonne ; une date passée ou mal écrite est refusée.

- [ ] (0.558) Rappels : finir les quêtes de tribu ou les commandes spéciales : la case se coche seule, et le compteur de la barre suit. Vérifier qu'une connexion ne coche rien à tort.

- [ ] (0.559) Préparation et chaîne : le survol d'un matériau donne le détail par rangement, avec la date pour un servant.

- [x] (0.560) Calculer une chaîne, puis récolter un de ses matériaux : « Encore N pour… » apparaît et baisse.

- [ ] (0.561) (0.561) Clic droit sur un objet de vos sacs › « Tensho — objet » : « Copier le nom », puis collez-le dans le chat : c'est bien son nom ; « Insérer le lien dans mon chat » : le lien de l'objet s'affiche dans votre chat, vu par vous seul ; les liens vers Universalis, Garland Tools et Teamcraft ouvrent la page de cet objet.

- [ ] (0.562) (0.562) Client en français : dans Paramètres, cochez « Afficher le nom anglais des objets » (cherchez « nom anglais »). Ouvrez la fiche d'un objet dans Tensho, puis survolez cet objet dans vos sacs : le nom anglais s'écrit sous le nom français. En client anglais : rien de plus.

- [x] (0.563) Météo : ajouter deux zones au tableau, comparer aux prévisions du jeu, retirer une zone ; le tableau reste après une relance.


- [x] (0.565) Aperçu du tri : les cases encadrées sont bien celles qui bougent ; le nombre correspond après application.



- [ ] (0.568) Aide à la pêche : comparer « créneau », « météo » et « météo d'avant » à l'heure et à la météo du jeu, sur un poisson à conditions.


- [x] (0.570) Rappels : déplier les tâches de servants après la cloche ; les heures correspondent, et les deux cloches créent leur alarme.

- [ ] (0.571) Mini Cactpot : comparer la table affichée au ticket du jeu, somme par somme.

- [ ] (0.572) Simulateur : chercher une recette et l'ouvrir ; ajouter une action : le résultat suit sans clic.

- [ ] (0.573) Macros : régler le son et « dernière page seulement » ; la macro copiée suit, et le son s'entend en jeu.



- [ ] (0.576) Rangements › Banque, onglet d'un servant : « Effacer ce relevé » (deux clics) : ses objets quittent vos stocks ; rouvrir ce servant à la sonnette le relit.

- [x] (0.577) Alarmes : créer une alarme chaque jour et une à la remise à zéro : l'heure annoncée est juste, et elle revient après avoir sonné.


- [ ] (0.579) Équipement : cocher « Tous les jobs (mirage) » sur la page : la liste des pièces s'élargit, et la case des filtres suit.

- [ ] (0.580) Équipement : choisir un autre job dans « Plan pour » : la liste de pièces est la sienne, le plan se garde, et revenir au job porté retrouve son plan.

- [ ] (0.581) Revue de mode : cocher une pièce par emplacement ; les pièces possédées sont justes ; « Essayer la tenue » les met dans la cabine.

- [x] (0.582) Quêtes : « Disponibles maintenant » et « Cette zone » : comparer à quelques quêtes du jeu ; une quête refusée par le jeu malgré le filtre est à signaler.

- [ ] (0.583) Quêtes : sur une quête d'épopée lointaine, la liste et « À prendre ensuite » correspondent au journal du jeu.



- [x] (0.586) Carte : menu de Noscea, Thanalan, forêts : l'aile s'affiche, colorée si vous pouvez voler ; comparer en essayant de voler dans une de ces zones.

- [x] (rapport de l'utilisateur, 2026-11-05, « re ok ») (0.587) Carte : la roue dentée ouvre Paramètres, onglet Carte, au premier plan ; la fenêtre surgissante a disparu ; un réglage changé s'applique à la carte.

- [x] (0.588) Récolte, section « Ouverts maintenant ou bientôt » : comparer les heures à un point ouvert dans le jeu ; la cloche crée l'alarme.

- [ ] (0.589) Lancer une synthèse sur une recette avec une séquence enregistrée : l'action suivante avance à chaque coup, « Conforme au plan » tant que les chiffres sont égaux, avertissement après un état Bonne ou une autre action.

- [ ] (0.590) Page des séquences : chercher un mot, changer l'ordre, renommer, dupliquer, copier en texte puis coller dans « Importer une macro » du simulateur.

- [x] (0.591) Préparation d'un objet à sous-recettes : calculer la chaîne, vérifier une ligne par recette et l'ordre, ouvrir le carnet et le simulateur depuis une ligne, cocher une étape.

- [x] (0.592) Artisanat, onglet « Que puis-je fabriquer ? » : lancer la recherche, comparer quelques lignes à vos sacs, filtrer par métier et niveau.


- [x] (0.594) Banque : cocher « Afficher en liste », trier chaque colonne, filtrer, changer d'onglet, cliquer un objet.


- [x] (0.596) Comparer une carte Tensho et celle du jeu : les noms doivent être du même côté de leurs icônes. Essayer la case « Noms des icônes » et le zoom minimal.

- [x] (0.597) Section Carte : chercher un lieu, une éthérite, un vendeur, un PNJ de Triple Triad, une quête et une ressource, cliquer chacun et vérifier le centre de la carte.

- [ ] (0.598) Paramètres › Alerte, partie Discord : cochez « Un salon par catégorie » et mettez une autre adresse de salon pour une catégorie. Faites sonner une alarme de cette catégorie et une alarme d'une autre catégorie : chacune arrive dans son salon Discord.

- [x] (0.599) Accueil : vérifier chaque ligne d'« Aujourd'hui » contre Rappels, Compagnie, Quêtes et Alarmes, cliquer chacune, masquer une ligne et une tuile dans « Personnaliser ».

- [ ] (0.600) (0.600) Tensho › Objets : ouvrez un objet fabriqué avec un livre de maître, onglet « Synthèse liée ». Comparez la ligne de la recette (livre de maître, HQ possible, synthèse rapide, spécialiste) à ce que dit le carnet d'artisanat du jeu pour cette recette.


- [ ] (0.602) Collections : ouvrir une fois, obtenir une monture ou une emote, actualiser : elle apparaît dans « Obtenus récemment ». Clic droit sur une emote.

- [ ] (0.603) Calendrier › Compagnie libre : avec deux personnages dans deux compagnies libres différentes, ouvrez l'atelier de chacune. La page montre un cadre par atelier, chacun avec sa date de relevé.

- [ ] (0.604) Pêcher : le panneau de pêche trouve bien le poste ; panneau de récolte : la ligne d'Abondance II s'affiche.





- [ ] (0.609) Après vous être connecté une fois avec votre autre personnage : Objets › fiche d'un objet qu'il possède : ligne « Sur vos autres personnages : » avec son nom, ses quantités NQ et HQ et la date du relevé.

- [x] (0.610) D'abord hors JcJ : tout marche comme avant, barre de serveur et carte comprises. Puis, un à la fois : quai de Wolves' Den, Crystalline Conflict personnalisé, Battlehall, Mahjong, donjon profond, avec la carte et des panneaux ouverts : tout disparaît, une ligne de chat, tout revient à la sortie.




- [x] (0.614) Hall de l'Arène, duel contre un PNJ : le panneau Triple Triade s'affiche.

- [ ] (0.615) Avec un lien de carte dans le chat pendant une coupure : il n'apparaît pas dans la liste des liens.



- [x] (0.618) Carte agrandie : ouvrir l'inventaire, elle reste affichée. Taille normale : elle s'efface quand une fenêtre la touche, puis revient.



- [x] (0.621) Marcher dans L'Entrepont : les deux lignes de Tensho doivent suivre celles de la carte du jeu.


- [x] (0.623) Dézoomer et zoomer sur Limsa à côté de la carte du jeu : comparer quels noms apparaissent et quand.


- [x] (0.625) Carte ouverte, parler à un PNJ de Triple Triade, refermer son menu : la carte revient. Fermer la carte avec sa touche : elle reste fermée.

- [x] (0.626) Cliquer sur « Limsa Lominsa - L'Entrepont » : la carte change. Glisser depuis le nom ne change pas de carte.


- [x] (0.628) Cliquer « Un forgeron fantomatique » dans le journal : la carte de la Ville fantôme s'ouvre, l'icône près de Gerolt. Rouvrir la carte avec sa touche : elle s'ouvre sur toi.


- [x] (0.630) Cliquer une fois « Un forgeron fantomatique » dans le journal, loin de la Ville fantôme.

- [x] (0.631) Ouvrir la carte de Yok Tural : les zones apparaissent sur le parchemin, comme dans le jeu, sans taches blanches. Comparer aussi une région d'Éorzéa.

- [x] (0.632) Comparer La Noscea et Yok Tural avec le jeu : zones, ton, cristaux. Ouvrir le monde : Tural est là, et le second menu propose Hydaelyn et Premier reflet.




- [x] (0.636) Cliquer une quête, la recliquer plusieurs fois : pas de clignotement, l'icône reste. Puis touche de carte : la carte se ferme ; encore la touche : elle s'ouvre sur toi.

- [x] (0.637) Cliquer plusieurs quêtes de zones différentes, dont une de donjon, depuis le journal et la liste des quêtes actives : la carte se centre à chaque fois, sans changer de zoom.

- [x] (0.638) Cliquer une quête de donjon : la carte de l'entrée s'ouvre, centrée sur le marqueur, comme pour la Ville fantôme.

- [x] (0.639) Sur Shaaloani, cliquer la Vallée des Pampas : sa carte s'ouvre. Sur Tuliyollal, cliquer la Ville fantôme : sa carte s'ouvre.

- [x] (0.640) Sur Solution Neuf, cliquer les Locaux de la Société de l'Arcadion : leur carte s'ouvre. Sur l'Écume des cieux d'Abalathia, cliquer le Paddock : sa carte s'ouvre, même avec l'icône de quête dessus.

- [x] (0.641) Cliquer « La combattante féline aux griffes acérées », ouvrir les Locaux de la Société de l'Arcadion : le menu des zones finit par ces locaux et le Ring des Griffes félines, comme le jeu.

- [x] (0.642) Cliquer une quête, remonter à la région puis au monde : la cible reste. Dézoomer une ville : les noms en noir rétrécissent avec la carte.

- [x] (0.643) Cliquer une quête, remonter au monde, cliquer la cible : la région s'ouvre ; la recliquer : la carte de la cible s'ouvre.

- [x] (0.644) Carte : vos coordonnées restent en bas à gauche et le curseur donne sa distance. Poser un drapeau hors de la vue : ses coordonnées et sa distance s'affichent sous son repère de bord. Comparer une distance avec le jeu.

- [x] (0.645) Survoler une éthérite harmonisée : son coût s'affiche, le même que dans la fenêtre de téléportation. Une éthérite non harmonisée est grisée et le dit au survol.

- [x] (0.646) Page Équipement, section Jobs : niveaux et expérience identiques à la fenêtre du jeu, classes et jobs sans doublon, jobs non débloqués grisés.

- [x] (0.647) Survoler un point légendaire, un éphémère et un à horaire sur la carte : la classe dite correspond au jeu.

- [ ] (0.648) Page Butin des contenus, le Palais des morts : « Débloqué par » donne la quête, son état et son lien. La fiche de cette quête dit « Débloque ».

- [ ] (0.649) Page Récolte : chercher un poisson au harpon, son lieu s'affiche. Comparer avec le carnet du jeu. Ses alarmes et sa carte fonctionnent comme les autres.


- [x] (0.651) Rappels, ligne Bienfaits de compagnie : les noms s'affichent sans rien ouvrir. Ouvrir l'onglet Bienfaits : le temps restant apparaît, comme dans le jeu. Poser la cloche : l'alarme est à l'heure de fin.

- [x] (0.652) Lancer un mandat, ouvrir la carte : ses zones sont vertes au même endroit que dans le jeu. Réglages de la carte : « Zones des mandats » et le repère « Mandats » agissent seuls. Essayer si possible un mandat de combat.

- [x] (0.653) Mandat lancé, carte ouverte, zone verte visible : cliquer le mandat dans le journal, puis recliquer. La zone et les autres marqueurs restent. Terminer le mandat : la zone disparaît une fois la carte refermée.

- [x] (0.654) Mandat lancé, zone vue une fois : s'éloigner, changer de zone, ouvrir la carte de la zone du mandat, la zone verte reste. Cliquer le mandat dans le journal : elle reste. Abandonner le mandat : elle disparaît.

- [x] (0.655) Accepter un mandat, le cliquer dans le journal : la cible et son repère au bord apparaissent. L'abandonner : ils disparaissent en moins d'une seconde.

- [x] (0.656) Alerte de PR activée, PR pleins : se téléporter, attendre une minute, aucune alarme. Dépenser des PR, se téléporter, attendre la remontée : une seule alarme.

- [x] (0.657) Depuis une autre zone, cliquer le mandat dans le journal : la zone verte s'affiche et reste. Abandonner : la zone et le repère noir partent. PR pleins, se téléporter : aucune alarme, puis dépenser des PR : une alarme à la remontée.

- [x] (0.658) Carte ouverte, se téléporter vers une autre zone : la carte se rouvre à l'arrivée. Vers la même zone aussi. Fermer la carte avec sa touche puis se téléporter : elle reste fermée. Entrer dans un contenu carte ouverte : elle reste fermée.

- [x] (0.659) Carte ouverte, entrer dans un contenu : elle se rouvre une fois la scène d'ouverture finie. Fermée avec sa touche avant d'entrer : elle reste fermée.

- [x] (0.660) Vieille Sharlayan, carte du jeu et Tensho côte à côte, au même zoom : les noms ont presque la même largeur et apparaissent à peu près en même temps.

- [x] (0.661) Incanter une téléportation et fermer la carte avec sa touche : elle reste fermée. Carte ouverte, se téléporter sans y toucher : elle se rouvre à l'arrivée. Dézoomer une ville au maximum : seuls les noms de liens restent, comme le jeu.

- [x] (0.662) Carte Tensho ouverte, se téléporter plusieurs fois de suite : plus de plantage. Le coût au survol d'une éthérite s'affiche quelques secondes après l'arrivée.

- [ ] (0.663) (0.663) Pêche : un poisson qui demande des prises préalables affiche « Instinct du pêcheur : N s ». Pêchez ces prises : le temps restant s'affiche (« … restantes ») et sa valeur de départ égale la durée annoncée.

- [ ] (0.664) Compagnie libre, onglet Atelier : chercher un projet, comparer ses matériaux et ses quantités à la fenêtre de l'atelier du jeu. Le clic sur un objet ouvre sa fiche.

- [ ] (0.665) Réglages des icônes de la carte : les quêtes disent « disponible », « non disponible » ou « étape en cours » sans numéro. Comparer une quête grisée sur la carte du jeu : elle ne peut pas être prise.

- [ ] (0.666) Exploration cosmique : ouvrir une recette dans le carnet. Tensho ne la refuse plus, et progrès, qualité et solidité sont ceux de la fenêtre du jeu. Lancer une séquence : les gains correspondent à la synthèse. Les seuils affichés correspondent à la mission.


- [ ] (0.668) Faire une synthèse et s'arrêter à plusieurs niveaux de qualité : le pourcentage HQ du jeu correspond à celui du simulateur pour la même qualité.

- [x] (0.669) Simulateur : choisir des ingrédients HQ, la qualité initiale doit égaler celle du carnet du jeu avec les mêmes ingrédients. Coût de préparation et historique du marché : mêmes valeurs qu'avant, filtres et tri réagissent à l'instant.

- [ ] (0.670) (0.670) Page Carte › onglet Chasse › « Conditions des rangs S » : les noms sont lisibles dans la langue du jeu (aucun « # ») ; les marques de votre zone viennent en premier ; la recherche filtre la liste. À l'occasion, comparez une condition avec ce qui se passe en jeu.

- [ ] (0.671) (0.671) Sur votre île, page Félicité insulaire : cliquer « Lire l'île » avant d'avoir ouvert le carnet de fabrication du jeu (Tensho doit vous demander de l'ouvrir), puis après l'avoir ouvert : rang, fabriques, enthousiasme, popularité, disponibilité et demande doivent correspondre au jeu. Comparer la valeur unitaire aux coquillages d'une création terminée.




- [ ] (0.675) Production › Commandes spéciales : comparer rang, points et livraisons de chaque client à la fenêtre du jeu ; un client non débloqué doit être marqué ; livrer une fois puis Lire de nouveau.


- [ ] (0.677) (0.677) Tensho › Monde › Carte : les onglets Cartes, Repères, Chasse (et Terrains) s'ouvrent avec leur contenu. Dans Repères, cliquez un favori : la carte s'ouvre à ses coordonnées. Dans Paramètres, le bouton « Favoris des cartes » ouvre l'onglet Repères.






- [ ] (0.683) (0.683) Personnage › La Grande Mascarade : comparer les niveaux réussis, les ennemis et les temps à la fenêtre du jeu, surtout le niveau 1 et le niveau 32.





- [ ] (0.689) Mandats : survoler un mandat doit donner ses informations, surtout en fenêtre étroite.




- [ ] (0.693) (0.693) Ouvrez à côté du jeu un panneau de liste (préparation, liste de fabrication ou projet d'atelier). Entrez en combat, puis ouvrez une fenêtre du jeu par-dessus : le panneau reste. Cochez « Cacher ces panneaux en combat » : il se cache en combat. Cochez « Cacher ces panneaux derrière les fenêtres du jeu » : il s'écarte sous une fenêtre du jeu. La croix du panneau le ferme.




- [ ] (0.697) (0.697) Objets › Rangements › Que faire avec ?, choisir une devise : décocher « Chercher les prix du marché » pendant que les prix arrivent arrête la recherche ; essayer chaque filtre (Tout, Collection seulement, Collection manquante, Vendable au marché) ; dire s'il manque des types de collection.


- [ ] (0.699) (0.699) Panneau à côté du jeu de l'atelier, de la préparation ou d'une liste d'artisanat : cocher « Seulement ce qui reste à récupérer, par provenance » : ce qui se fabrique est remplacé par ses matières de base, rangées par provenance (Récolte, PNJ marchands, Monstres, Autres), avec le stock en face du requis ; décocher une provenance la cache ; pour une pièce d'atelier, comparer avec Artisanat › Préparation.


- [ ] (0.701) Options des panneaux de listes (liste d'artisanat, atelier, préparation), « Seulement ce qui reste à récupérer, par provenance » coché : « Devises des marchands » : aucun numéro, rien de coché au départ, la recherche filtre par nom ; cocher Gil : le groupe « PNJ marchands » se remplit ; « Tout décocher » : plus aucun PNJ.



- [ ] (0.704) (0.704) Clic droit sur une ligne avec des coordonnées (Butin des monstres, ligne du wiki ; puis un vendeur, un favori, le train de chasse, un animal de l'île) : le menu propose l'éthérite la plus proche, pose le drapeau au bon endroit et pose la question de téléportation habituelle.


- [x] (0.706) Sur l'île : clic droit sur un animal ou sur la carte Tensho, plus d'entrée éthérite, le drapeau reste ; ailleurs, l'entrée reste. VALIDATED_IN_GAME (rapport de l'utilisateur, 2026-11-07). Étages sans éthérite : à voir quand le cas se présentera.


- [ ] (0.708) (0.708) Personnage › Armes antiques : comparer les étapes d'une arme ; avec un livre en cours, comparer les objectifs et les compteurs au livre du jeu.





- [x] (0.713) Progression › Peuples alliés : comparer rangs et réputation à la fenêtre de réputation du jeu ; si chaque ligne montre le rang du voisin, le dire.

- [ ] (0.714) Progression › Grandes compagnies : comparer grade, sceaux et prix de la promotion au jeu ; vérifier le genre du nom de grade ; comparer quelques articles à la boutique du jeu.

- [ ] (0.715) Progression › Livraisons spéciales : comparer les quêtes faites au journal ; dire si l'onglet du Cristarium contient bien ce qu'il faut.



- [ ] (0.718) (0.718) Missions › Donjons sans fond : dans l'onglet « Objets », lisez le « Texte du jeu » de deux poteries et comparez-le au jeu. Dans l'onglet « Étages », comparez l'« État » d'un groupe d'étages à la fenêtre du jeu.

- [x] (0.719) Objets › Lexique : chercher « eureka » puis un mot en anglais ; dire si la première ouverture se sent.


- [x] (0.721) Monde › ALEA : comparer le lieu de deux ALEA en cours à la carte du jeu.


- [ ] (0.723) Masked Carnivale : l'auto-contrôle lit 0 épreuve faite ; dire si c'est juste pour votre personnage.


- [x] (0.725) Que puis-je fabriquer ? › tri par marché : relancez les prix, les ventes par jour doivent s'afficher et le haut de la liste doit se vendre.



- [x] (0.728) À un point de récolte, suivez le plan : la ligne « Ensuite » et le cadre sur la barre doivent suivre chaque étape.

- [x] (0.729) À un point de récolte : une ligne doit séparer « Ensuite » du plan.


- [x] (0.731) À un point de récolte, suivez le plan jusqu'au bout : avec 50 PR ou plus et des essais restants, « Ensuite » doit proposer une action avec les PR restants.


- [x] (0.733) À un point de récolte : un cadre doré doit clignoter sur la case de l'action « Ensuite » ; Sagesse du fermier seulement après une récolte.

- [x] (0.734) À un point de récolte : le cadre doit se poser sur Récolte abondante, même placée en version I sur la barre.










- [x] (0.744) Récoltez quelques objets de la liste : plus de clignotement ; je vérifie qu'aucun « List zero » ne revient.



- [x] (0.747) À un point de récolte : le plan apparaît après « Calcul du plan… », sans à-coup ; je relis « Slow frame ».




- [x] (0.751) Point de récolte avec un objet de votre liste d'atelier : ligne « Vos listes en demandent encore » et cadre vert dans la fenêtre du jeu.

- [x] (0.752) Liste de l'atelier : sous Récolte, une ligne par zone avec ses objets.

- [x] (0.753) Point de récolte : cochez « Récolter automatiquement » ; le plan change et le dit.

- [x] (0.754) Carte : survolez un point à horaire fermé ; une ligne doit dire dans combien de temps il ouvre.


- [x] (0.756) Ouvrez un point qui a un objet de vos listes : l'aide doit se placer dessus.

- [x] (0.757) Liste de l'atelier : repliez une zone ; changez de zone, elle doit passer en tête ; les prix de téléportation s'affichent.


- [x] (0.759) Cliquez « Ouvrir dans Tensho » sur une alarme, même affichée avant un rechargement : la fiche de l'objet doit s'ouvrir.




- [ ] (0.763) (0.763) À un point de récolte (mineur ou botaniste), regardez le plan du panneau Tensho de récolte. Quand deux plans donnent autant d'objets, Tensho propose celui qui demande le moins de clics (une action qui dure tout le point plutôt qu'une action à chaque coup), puis celui qui coûte le moins de PR. Un plan qui donne plus d'objets passe toujours en premier.











- [x] (0.774) À la prochaine alarme : clic droit sur le nom de l'objet dans le chat ; le menu doit proposer Tensho.





- [x] (0.779) Rechargez le plugin : les objets de monstres sont dans « Monstres » dès l'ouverture de la liste.

- [x] (0.780) À la prochaine alarme : clic droit n'importe où sur « Minerai de sombracier (clic droit : Tensho) ».

- [x] (0.781) À la prochaine alarme : clic droit sur la ligne de l'objet ; je relis « Context menu opened ».

- [ ] (0.782) Où : en jeu, le carnet d'artisanat, et le panneau « Tensho · Carnet d'artisanat » à côté ; réglage : Paramètres › Panneaux à côté du jeu › Aide à l'artisanat › « Utiliser le solveur Raphael ». Faire : 1) cochez « Utiliser le solveur Raphael » ; 2) portez Cuisinier ; 3) ouvrez le carnet d'artisanat, passez sur Menuisier (ou cherchez « Madrier de ginseng ») ; 4) sélectionnez « Madrier de ginseng » (niveau 91). Regarder : le jeu ne gèle pas ; le panneau Tensho affiche une « Macro proposée » ou « Aucune séquence terminée compatible. ».

- [x] (0.783) Carnet d'artisan, Kit de réparation d'appareil : « Raphael non utilisé ».




- [ ] (0.787) Ouvrez la livraison à l'établi : le panneau d'atelier ne garde que le reste à livrer, la page dit « Livré : phase 2/3 en cours ».

- [x] (0.788) Carnet en Menuisier sur le Lingot de mithril (Métallurgie) : « Mauvais métier » en orange et le bouton pour porter Forgeron.











- [ ] (0.799) En mage bleu, près d'un monstre qui apprend un sort manquant (après « Lire les sorts manquants ») : icône du mage bleu au-dessus de sa tête.








- [ ] (0.807) Promenez-vous parmi des monstres, puis Monde › Découvertes : cherchez un nom, ses lieux s'affichent ; Exporter, puis déposez le fichier dans Import et Importer.

- [x] (0.808) Carnet d'artisanat : clic droit sur une recette puis sur un matériau : entrée « Tensho ».

- [x] (0.809) Vainquez quelques monstres et ramassez leur butin (en mage bleu : un sort ; en dresseur : une capture) ; je relis « Reading messages ».



- [x] (0.812) À la prochaine alarme : clic droit sur le nom de l'objet : menu de l'objet avec « Tensho ».


- [ ] (0.814) Une cible hors de la vue, en haut à gauche : son icône de bord passe au-dessus du bandeau des noms.


- [x] (0.816) Récolte d'un collectionnable avec seuils : le bouton de l'action du plan encadré dans la fenêtre du jeu.

- [x] (0.817) Ouvrez un objet au tableau des ventes, puis regardez son prix dans Tensho (survol, Que faire avec ?) : celui que vous venez de voir.


- [x] (0.819) Près d'un yak laineux avec du lait de yak demandé : patte sur la carte et à côté de l'objet dans la liste.

- [ ] (0.820) Ouvrez l'inventaire d'un servant qui garde un objet de votre liste : sa case est encadrée.






- [x] (0.826) Clic droit sur un objet fabricable : « Ajouter à « Liste 1 » » ; il apparaît dans la liste.



- [x] (0.829) Limsa, la Dunette : plus de marchand fantôme ; ailleurs dans Limsa, les marchands permanents sont là.


- [ ] (0.831) Enchaîner deux points du même genre en dépensant des PR au premier : le second propose un plan.



- [x] (0.834) Clic droit sur Haricots émeraude dans la liste : « Y aller » et « Placer le drapeau ».



- [ ] (0.837) (0.837) Dans le chat du jeu, faites un clic droit sur un objet en lien (par exemple la morille dans la ligne [Tensho] d'une alarme) : le menu contient l'entrée Tensho. Faites un clic droit sur le nom d'un joueur : pas d'entrée Tensho.




- [ ] (0.841) (0.841) Panneau d'une liste d'artisanat, « Seulement ce qui reste à récupérer, par provenance » coché : un ingrédient fabriqué que vous avez déjà (par exemple du sirop de bouleau) reste une ligne, avec la quantité prise dans le stock ; s'il est chez un servant, il porte l'icône d'entrepôt.



- [x] (0.844) Mor Dhona, quête de cuisinier : flèche vers le bas sur l'icône et la zone plus basse ; Carte › Alentours : le réglage.


- [x] (0.846) Journal, « Carte » sur une quête : la carte centre dessus sans changer le zoom.






- [ ] (0.852) Fenêtre d'une quête, clic droit sur une récompense : entrée Tensho ; survol : panneau ou ligne « Item hover without panel » au journal.

- [x] (0.853) Tillac de Limsa, clic droit sur la cible de quête : « Éthérite la plus proche », puis Limsa et l'éthernet jusqu'à la guilde.






- [ ] (0.859) Pêcher à un lieu avec un poisson commun : « ni heure ni météo (jeu) » ; un poisson à fenêtre garde sa fenêtre.


- [ ] (0.861) Fashion Report, fenêtre ouverte : les catégories et le thème ; après un jugement, Monde › Découvertes les compte.









- [ ] (0.870) Ouvrir au tableau des ventes un objet que vous vendez, puis la liste de ventes du servant : la colonne État donne le verdict sans cliquer « Comparer aux autres vendeurs » ; survol du prix le plus bas : « Vu au tableau des ventes du jeu le … ».

- [ ] (0.871) Tensho › Marché, un objet choisi, onglet « Annonces » : dans le choix de portée, « Région (Europe) » (ou Japon…) a son nom tout de suite et n'est pas grisée.

- [ ] (0.872) Monde › Découvertes : noter « Marchands placés » ; ouvrir la boutique d'un marchand jamais ouverte, puis revenir : le nombre « vus par vous » a augmenté de 1.


- [ ] (0.874) Relevés en jeu par sonde (mode développeur, journal Dalamud « Reading <partie> » ou Monde › Découvertes) : pêche (fishing, fishline, statuts sous Pêche lente/Amorce/Prise forcée), réduction (purify), remise de quête (npctrade), île (island), aéronef revenu (airship), atelier (workshop) ; servant revenu de tâche → « Tâches des servants » ; coffre de donjon (fenêtre de butin) → « coffre de mission » ; ALÉA avec récompense → « récompense d'ALÉA » ; cible vaincue avec butin hors mission → « butin du monstre » ; Eurêka ou Bozja, même chose (en dernier) ; dresseur, bête apprivoisée → « apprivoisée de ».

- [x] (0.875) Désynthétiser un objet, puis sa fiche › Usages : « Vu donner » ; vaincre un monstre et ramasser un objet, puis Monde › Monstres sur cet objet : « Vu le laisser ».

- [x] (0.876) Après un coffre de mission ouvert : la fiche de la mission montre « Coffre (vu par vous) » ; clic droit sur l'objet : la mission proposée.


- [ ] (0.878) Combattez un monstre qui lance un sort de mage bleu (sa barre d'incantation s'affiche). Monde › Découvertes : une ligne « <monstre> vu lançant <sort> ». Personnage › Grimoire de magie bleue, fiche du sort : section « Vu en jeu » avec ce monstre.







- [x] (0.885) Monde › Découvertes : plus de « Récupérer » ni « Partager » ; « Échanger maintenant » marche toujours.


- [ ] (0.887) (0.887) Pêchez deux ou trois poissons. Panneau de pêche, détail du poisson : ligne « Vu en jeu (×N) : … » avec la force de touche et les temps (ex. « ! 13,6–18,8 s »). Monde › Découvertes : « Touches de poissons vues ».


- [ ] (0.889) À la prochaine quête qui demande un objet : remettre l'objet, puis Objets › fiche de cet objet › Usages : ligne « Demandé par des quêtes (vu en jeu : …) » avec cette quête.


- [ ] (0.891) Harpon : la liste du point reste affichée ; un poisson de la bonne ombre en vue passe en couleur « en vue ». Carte : les trous de pêche en poisson orange. Carnet de pêche › un trou › afficher sur la carte (pour la zone).














- [ ] (0.905) Carte et listes : toutes les coordonnées s'écrivent « 12.3 » avec un point (curseur, joueur, PNJ, ALÉA, signets, triple triade, île).






- [x] (0.911) Monde › Découvertes : les faits de pêche portent un libellé de pêche ; les ALÉA à (0,0) ont disparu.


- [x] (0.913) Survoler un bouton grisé (galerie d'outils pendant un chargement, carte d'un point sans carte) : l'info-bulle s'affiche ; les info-bulles longues ont toutes la même largeur.

- [x] (0.914) Glisser un curseur de la carte ou de l'équipement : l'effet suit en direct ; les réglages sont bien gardés après un rechargement.

- [x] (0.915) Réglages : chaque case a son bouton ↺ ; survoler un réglage : dernière ligne « Par défaut : … ».


- [ ] (0.917) Page Pêche en mer : une ligne d'état par port (« pas encore vérifié » tant que vous n'avez pas embarqué). À bord : l'aide à la pêche ordinaire ne s'ouvre pas, le panneau de voyage oui ; dalamud.log contient « Ocean schedule sighting ». Après un voyage de chaque port : l'état change.

- [ ] (0.918) En pêche en mer : le panneau de voyage montre les icônes, l'appât en main encadré en vert, la touche en couleur, le temps de touche et le chronomètre ; « Poissons du courant spectral » en vert, à ouvrir.

- [ ] (0.919) (0.919) Atelier de la compagnie, après avoir consulté les résultats de l'aéronef : sur l'onglet des aéronefs, la ligne « Dernière expédition : Mer de nuages - secteur 1 › … » reprend les secteurs du voyage.

- [x] (0.920) Pages avec une erreur de source (source coupée ou hors ligne) : ligne rouge ; pages vides : ligne grise ; un bouton Copier : « Copié » à côté pendant deux secondes.

- [x] (0.921) Parcourir quelques pages avec des icônes (contenus, actions, cartes de Triple Triade, carte du monde, vues panoramiques) : les icônes sont là et à la même taille.






- [ ] (0.927) Survoler un objet qui apprend une emote ou une coiffure : ligne « Enseigne : déjà appris » en vert si vous l'avez, « Enseigne : pas encore appris » en rouge sinon.


- [x] (0.929) Carte d'une zone de Stormblood ou plus récente : survol d'une vue panoramique, « Carnet Stormblood n° 0xx » ; la liste des vues de la carte a ses numéros.







- [ ] (0.936) Rappels, « Vos personnages » : colonne « Autres » avec mandats, livraisons et quêtes tribales du personnage connecté ; survol pour l'escouade.








- [x] (0.944) Paramètres : info-bulles relues ; carte › réglages : une seule case « zones de quête » (section Zones) ; craft › attente des bonus : le bouton par défaut revient à la valeur des actions.















- [ ] (0.959) Aller au Détroit ouest de Zorgor (ou une zone de Shadowbringers), attendre 10 s : l'aide à la pêche montre « météo : … · météo d'avant : … ».

- [ ] (0.960) Carte du Détroit ouest de Zorgor : survol du trou de pêche, plus de « point légendaire » ni de tome de mineur.










- [x] (0.970) Quêtes : survoler une quête, ligne « Extension : … ». Survoler un objet récolté (ex. un minerai de Heavensward) : ligne « Extension (d'après ses lieux de récolte) : Heavensward ».





- [ ] (0.975) Page Eurêka et Bozja : onglets ou zones du Croissant occulte présents, avec des ALÉA et des affrontements placés.


- [x] (0.977) Cibler un lieu d'une autre zone, se téléporter dans la zone, ouvrir la carte Tensho sans ouvrir celle du jeu : les marqueurs rouges des quêtes pas encore acceptées sont là.



- [ ] (0.980) Dresseur › Équipe › 1er plateau : 10 bêtes listées, puis pour chaque combat les ennemis avec leur faiblesse et 3 bêtes ; onglet Plateaux : étoiles des ennemis (Chevalier du plateau : For 2 · Int 1 · Rés. phys. 2 · Rés. mag. 2 · Vit 3).


- [ ] (0.982) Aide à la pêche dans un lieu avec un poisson collectionnable : ligne « Collectionnable : … / … / … », survol : assignats de chaque palier.







- [ ] (0.989) Collections › Tenues : déplier une tenue incomplète, « Pièces manquantes → souhaits » : elles sont dans Souhaits ; « Fabricables → … » : dans la liste d'artisanat.

- [ ] (0.990) (0.990) Ouvrir Monde › Butin des missions, se déconnecter, puis rouvrir la page : les états sont toujours affichés, avec la ligne « État enregistré de votre dernier personnage (lu le …) ».




- [ ] (0.994) (0.994) Monde › Butin des missions › un donjon › « Mécaniques et stratégie (wiki) » › « Lire sur le wiki » : on voit les sections du wiki (Objectives, boss…). En combat, le bloc dit « Masqué pendant un combat. »

- [ ] (0.995) (0.995) Monde › Butin des missions › choisir un raid › « Vos notes et schéma d'arène » : écrire une note, ajouter T1 et A, les glisser, copier le code, puis le coller dans une autre mission.




- [x] (0.999) Paramètres : sept onglets ; « Chercher un réglage » : taper « wiki » : la ligne Wiki · Données et sources, un clic ouvre l'onglet.












- [ ] (0.1011) Pêcheur : lancer, attendre une touche, relancer sans cliquer sur « Relancer » : le chrono repart de 0 à chaque lancer (aussi au vif) ; un leurre ne le relance pas ; le bouton le remet à zéro à la main. Même chose dans le panneau de la pêche en mer.


- [x] (0.1014) Accueil, « Tous les outils » : chaque page a sa ligne (Champs de Bozja et Eurêka, Lexique, Île, Jardin, etc.), « Ouvrir » mène à la page, « Détails » montre son but ; les anciennes lignes (Craftimizer, Banque, Item Use) ouvrent toujours leur onglet


- [ ] (0.1015) Aide, « Questions fréquentes » : 31 questions en français (puis en anglais après un changement de langue) ; taper « dépôt » ou « langue » dans la recherche de l'Aide ne garde que les questions qui en parlent ; « Comment changer la langue ? » montre un bouton « Ouvrir cette page » qui mène aux Paramètres ; une recherche sans réponse dit « Aucune question pour cette recherche. »

- [ ] (0.1031) Application de bureau, jeu fermé : décompresser TenshoAssistant.Desktop.zip (ou ouvrir src/TenshoAssistant.Desktop/bin/package) et lancer TenshoAssistant.Desktop.exe par un double-clic, sans rien installer : la fenêtre s'ouvre sans console, les catalogues se chargent (« Lecture des données du jeu : N catalogues prêts sur 12 »)





- [ ] (0.1033) Application de bureau 0.1033 : la barre d'état a une ligne de mise à jour (boutons « Mise à jour automatique : active », « Chercher une mise à jour » ; texte « cette application est en 0.1033.0, la version du plugin installé » ou « pas encore de release ») ; un clic sur « Mise à jour automatique » la coupe et le garde au démarrage suivant

- [ ] (0.1033) Après la première release qui porte l'application : une application plus ancienne lancée se met à jour seule vers la version du plugin installé (téléchargement, puis ouverture au démarrage suivant ou sur « Redémarrer maintenant ») ; « Version d'avant » revient à la précédente


- [x] (0.1038) Thème clair (Paramètres › Général › Thème de l'interface) : titres de catégorie de la barre latérale (CONTENUS, GOLD SAUCER, RÉFÉRENCES, PRÉFÉRENCES) et « OBJET EN CONTEXTE » bien lisibles ; en thème sombre aussi ; une catégorie repliée qui contient la page ouverte reste dorée







- [ ] (0.1041) Carte de Tensho, réglage « Points de récolte à horaire ouverts » (repères au bord) : les points de récolte à horaire ouverts en ce moment ont toujours leur repère au bord ; un point dont le tome n'est pas lu n'en a pas ; aucun lieu de pêche n'apparaît ou ne disparaît à tort.

- [ ] (0.1042) Paramètres › Alerte, « Créer aussi les alarmes dans le jeu » cochée : la case « Envoyer aussi sur Discord » apparaît dessous, décochée ; une alarme copiée dans le jeu ne part plus sur Discord ; case cochée (webhook activé), elle y part à l'heure, sans sonner deux fois dans le jeu.

- [x] (0.1043) Alarmes : « Supprimer » sur une alarme : le bouton devient « Cliquer encore pour supprimer » en rouge, l'alarme reste ; un second clic dans les 5 secondes la supprime ; après 5 secondes, le bouton revient et un clic ne fait que l'armer de nouveau.




- [ ] (0.1044) Paramètres › Données et sources : sous Garland Tools, le wiki et Universalis, les interrupteurs d'usage sont en retrait, activés, chacun avec son infobulle (en et fr) ; couper Garland Tools grise ses trois usages ; cocher « Données du jeu et vos observations seulement » grise tout.

- [ ] (0.1044) Paramètres › Données et sources, sous Garland Tools : couper seulement « Butin des missions » : Monde › Butin des missions et le panneau à côté de l'outil de mission du jeu ne demandent plus le butin (bouton grisé, « Source désactivée ») ; « Butin des monstres » reste actif dans Monde › Butin des monstres.





- [ ] (0.1045) Aide › Lancer le guide, étape « À quoi jouez-vous ? » : trois cases (Contenus et progression, Gold Saucer, Jardinage et Félicité insulaire), en anglais et en français. Ne rien cocher montre seulement les arrêts pour tous ; cocher « Artisanat » seul ajoute les arrêts de l'artisanat (Synthèse en direct, Préréglages d'artisanat, Enchaînement de macros), sans les mandats ni le jardinage.


































- [ ] (0.1046) Où : n'importe où, le jour du prochain patch du jeu. Faire : 1) après le patch, lancez le jeu avec Tensho et connectez un personnage ; 2) attendez une minute que Tensho finisse de charger ; 3) cliquez « C'est fait » sur cette page. Regarder : rien à l'écran : l'agent lit lui-même le journal de Dalamud (les lignes du rapport des changements de catalogue : objets, recettes, récolte, positions, marchands).



- [x] (0.1049) Carte de Tensho, carte d'une zone ouverte (par exemple La Noscea centrale) : souris sur la carte → icône de la météo actuelle dans le coin bas-droit, même météo que la boussole du jeu ; info-bulle « Météo actuelle de la zone : … » ; souris hors de la carte → l'icône disparaît. Carte de région (par exemple La Noscea) : une icône par zone au-dessus de son image, chacune juste ; zone à météo individuelle (histoire non faite) : pas d'icône ; aucune erreur dans dalamud.log.

- [ ] (0.1050) Après un chargement du plugin (rechargement ou démarrage du jeu) : dalamud.log contient une fois la ligne « JIT warm-up: N methods of the window and the plugin compiled ahead in T ms, K skipped. » (N vers 27 000, K petit), aucune erreur ; pas d'à-coup sensible du jeu dans les secondes qui suivent. Au balayage des pages suivant (lignes « Selftest draw »), la première image de Gear, SortPreview, Reminders et Storage plus courte qu'avant (161, 131, 92 et 113 ms le 11/11 à 2 h 14).


- [ ] (0.1081) Paramètres › Avancé › « Lien avec l'application de bureau » : désactivé au départ, son état dit « Lien : désactivé ». L'activer : « Lien : en attente de l'application de bureau. » ; dalamud.log : « Local link: Waiting. »






- [ ] (0.1094) Pêche en mer : en plein voyage, recharger Tensho (Dalamud › Plugins installés, désactiver puis réactiver) : la fenêtre du voyage garde les « … pts » des poissons déjà pris et les missions ; dalamud.log n'écrit pas une deuxième ligne « Ocean schedule check » pour ce voyage.

- [ ] (0.1095) Pêche en mer : la dernière ligne de la fenêtre du voyage (« Points : … · poissons pris : … ») suit les points du jeu pendant le voyage (somme des « … pts » du tableau) ; en fin de voyage, dalamud.log contient « Ocean voyage result » avec des points non nuls et « Tensho counted ».

- [x] (0.1095) Pêche en mer, mode développeur : les lignes « Slow frame » ne nomment plus TenshoOceanVoyage au-delà de 100 ms (mesure d'avant : 160 à 260 ms, OceanVoyage 75 à 90 ms) ; « ocean fish table » donne le coût du tableau ; « Discovery: bite strength unknown » devient rare.

- [x] (0.1096) Retours d'aéronef et de sous-marin, tâches des servants, missions d'escouade (mode développeur, journal des découvertes) : dalamud.log écrit « Item gain attributed: … from AirshipReturn / SubmarineReturn / Venture / Squadron » au lieu de « Item gain not attributed » ; une nouvelle destination ou un rang donne « Discovery: airship new destination … » ou « submarine rank … ».

- [x] (0.1097) Paramètres › Artisanat, « Son de la ligne de fin de macro » : « Écouter » joue le même son qu'un <se.N> tapé dans le chat pour N = 1, 8 et 16 ; à 0 le bouton est grisé.

- [x] (0.1098) Carte : poser un drapeau dans une zone, se téléporter dans une autre zone, ouvrir la carte (M) : la carte de Tensho montre la zone du joueur, pas celle de l'ancien drapeau ; retirer le drapeau puis rouvrir : idem. Un lien de quête du journal ouvre toujours sur la quête. dalamud.log : « Game map request kept from before ».



- [ ] (0.1102) Pêche en mer : réussir une mission du voyage (gils gagnés) : Rangements › Gils, la ligne est dans « Pêche en mer (missions) », pas dans « Autre ».


- [x] (0.1106) Barre latérale, OBJETS › Souhaits : la page s'ouvre (plus d'onglet Souhaits dans Collections) ; clic droit sur un objet › « Ajouter à mes souhaits » : la ligne « ajouté à vos souhaits » en bas de la fenêtre, son bouton ouvre la liste

- [x] (0.1106) Souhaits avec un objet récolté sur un point limité (ex. un objet de légende) : sous son nom, « Récolte : métier · zone · point · heures » ; « Ajouter les alarmes (N) » les pose, un deuxième clic n'en ajoute pas


- [x] (0.1107) Carte de Tensho, clic droit › onglet Quêtes : la case « Quêtes pas encore disponibles » y est (plus au-dessus des onglets) ; décochée, les quêtes pas encore disponibles quittent la liste et leurs icônes la carte

- [ ] (0.1108) Récolte avec un objet caché (ex. Graines de pommier luisant) révélé après une dépense de PR : le panneau donne un plan pour lui, avec la ligne « Objet révélé après une dépense de PR : plan établi avec ce qui restait… »

- [ ] (0.1109) Calendrier › Jardinage, onglet Jardin, « Vue détaillée » cochée, avec un potager planté (au moins deux emplacements voisins de graines qui se croisent) : sous l'emplacement planté en second, « Peut donner : … (avec l'emplacement N, …) ». Une graine pas possédée ou souhaitée est redite en couleur (« Pas possédé ou souhaité : … »). La ligne « Prévisions : … » est sous la grille.

- [ ] (0.1113) Paramètres › Avancé › Application de bureau : l'état dit « pas installée » (ou la version installée) ; « Installer l'application de bureau » : la ligne passe par « demande à GitHub », « téléchargement », « dépaquetage », puis « installée » ; l'application ne s'ouvre pas seule






- [x] (0.1115) Dédale bestial (dresseur) : à l'entrée, pas de ligne « Tensho en pause » dans le chat ; les pages et panneaux de Tensho restent ouverts pendant le plateau et les combats ; en JcJ, la coupure se fait toujours

- [x] (0.1116) Mode développeur activé, jouer un plateau du Dédale jusqu'au résultat : dalamud.log contient « Labyrinth reading: … windows: … » avec le nom de la fenêtre du résultat et des lignes « Labyrinth window XBM…: values=… » (nombres seulement) ; les donner à l'agent pour coder X-135





- [ ] (0.1119) Lien activé avec une application de bureau plus ancienne (0.1116) : la nouvelle dit « le jeu offre le protocole de lien 2 / 3 » selon le côté, sans planter

- [ ] (0.1120) Lien activé, application de bureau ouverte : changer plusieurs réglages de suite dans le jeu ; l'application dit une fois « Réglages du plugin relus à … » et ses pages suivent ; la page Discord de l'application ne montre aucune adresse

- [ ] (0.1122) Paramètres › Cache : « Vider le cache », Oui : le message dit le nombre de fichiers retirés et la copie (dossier Backups) ; les pages qui lisent le cache se rechargent ensuite normalement


- [ ] (0.1125) Aide › « Signaler un problème ou aider » : remplir un bug avec un chemin « C:\Users\Nom Prénom\x » dans le titre, choisir « Par mon navigateur », puis « Envoyer sur GitHub… » : l'aperçu montre <path> à la place du chemin et rien ne s'ouvre ; changer un champ ferme l'aperçu ; « Ouvrir dans mon navigateur » ouvre le formulaire avec exactement le texte de l'aperçu (ne pas l'envoyer).


- [x] (0.1128) Carte › Carnet d'exploration : un panorama dont la fenêtre est ouverte dit « possible maintenant, jusqu'à HH:MM » et le jeu l'accepte ; un panorama hors de son heure n'est pas dans « Possibles maintenant » ; son infobulle dit ce qui manque et la prochaine fenêtre (Le Viaduc 020 : 05:00–07:59 ET, Ciel dégagé ou Beau temps)

- [ ] (0.1128) Infobulle d'un panorama à météo rare (La Cité invisible 019, Nuages) : une prochaine fenêtre est donnée, ou « Aucune fenêtre dans les 30 prochains jours », jamais rien

- [x] (0.1129) Barre d'infos du serveur avec les alarmes, les rappels, les monnaies et les gils : chaque infobulle s'ouvre juste sous son entrée, sans sortir de l'écran ; avec « Au-dessus de l'entrée » coché et la barre en haut de l'écran, elles restent en dessous (pas de place au-dessus)

- [x] (0.1130) Carte de Tensho : les icônes de la colonne d'outils sont centrées dans leurs boutons ; cliquer le pourcentage, taper 150 puis Entrée : zoom à 150 % ; « 100% » sous le pourcentage remet 100 % ; la colonne reste lisible sur la carte (superposée) et dans une petite fenêtre

- [ ] (0.1131) Application de bureau, jeu lancé, lien activé : Stockage, Listes d'artisanat, Mes outils, Historique des voyages montrent les données du personnage ; changer une liste d'artisanat en jeu : l'application la montre quelques secondes après ; le dossier pluginConfigs\TenshoAssistant n'est pas modifié par l'application

- [ ] (0.1132) Application de bureau ouverte, jeu derrière : une alarme Tensho (non copiée dans le jeu) sonne dans l'application (son Windows, ligne « Alarme : … » dans la barre d'état) ; le jeu au premier plan : rien dans l'application (réglage par défaut) ; décocher « seulement si le jeu n'est pas au premier plan » : elle sonne des deux côtés

- [ ] (0.1133) Découvertes › Importer : un fichier posé dans le dossier Import est joint ; il garde son nom « .json » jusqu'à la sauvegarde suivante (une minute au plus), puis devient « .json.imported » ; un deuxième clic sur Importer entre-temps ne le joint pas deux fois




- [ ] (0.1137) Monde › Découvertes, connecté à GitHub, cliquez « Échanger maintenant » : message sans erreur ; sur github.com, votre gist contient index.json et des fichiers kind-NN.jsonl.gz.b64 ; un deuxième échange sans nouveau relevé dit « Rien de nouveau à envoyer. »


- [x] (0.1139) Stockage › Journal des gils, après la mise à jour : les entrées d'avant sont toutes là ; dalamud.log dit « Gil journal: SQLite … data file TenshoData.db » puis « a character's file of before moved to TenshoData.db » ; GilJournal/before-sqlite-<date>/ contient l'ancien fichier


- [x] (0.1140) Collection › Succès, après la mise à jour : la progression vue avant est toujours là ; dalamud.log dit « Tensho data: the achievement progress file of before moved to TenshoData.db » ; before-sqlite-<date>/AchievementProgress.json existe à la racine du dossier du plugin



- [x] (0.1141) Salle de jeu de la Triple Triade (Gold Saucer, 579) : Tensho reste allumé dans la salle publique et se coupe dans un match de l'arène ; dalamud.log montre « Competitive arena check » sans avertissement

- [ ] (0.1142) Deux clients du jeu sur le même profil : lancer le second pendant que le premier enregistre la banque ; Stockage › Banque du second dit d'abord « attend », puis se charge seul en quelques secondes



- [x] (0.1144) Collection › Magie bleue : la liste du grimoire se remplit comme avant ; « Lire les pages manquantes » avance page par page ; dalamud.log sans erreur


- [x] (0.1148) Atelier de la compagnie › historique des voyages : après le rechargement, la même liste qu'avant ; dalamud.log dit le fichier d'avant déplacé vers TenshoData.db ; au prochain retour d'un sous-marin, le voyage s'ajoute


- [x] (0.1149) Stockage › Banque (tous les personnages, servants, sacoches, arsenal, coffre de compagnie) : après le rechargement, tout ce qui était enregistré est là ; dalamud.log dit le fichier d'avant déplacé vers TenshoData.db ; before-sqlite-<date>/InventoryBank/ existe à la racine du dossier du plugin ; une nouvelle lecture d'un servant s'enregistre




- [x] (0.1150) Jeu : Collection › Succès, le bouton « Demander » est toujours là et marche comme avant








- [x] (0.1158) Paramètres › Avancé › Application de bureau › « Pages dans le jeu et dans l'application de bureau » : mettre « Objets » sur « Application seulement » : la page quitte la navigation du jeu et reste dans celle de l'application ; « Marché » sur « Jeu seulement » : l'inverse ; Accueil, Paramètres, la carte, l'artisanat en direct et la Revue de mode ne sont pas proposés ; « ↺ » remet « Jeu et application »

- [x] (0.1159) Rappels › onglet « Alarmes (N) » : les alarmes posées avec une cloche des minuteurs et les alarmes automatiques (servants, voyages, escouade) y sont, avec leurs boutons ; « Rappels (N) » dans le menu ; l'outil Alarmes ne les liste plus et son chiffre ne les compte plus ; elles sonnent toujours

- [x] (0.1160) Jardinage › onglet « Alarmes (N) » : chaque lit noté au journal montre son prochain entretien et sa récolte avec l'heure ; « Jardinage (N) » dans le menu ; « Me le rappeler dans le chat » décoché : l'onglet le dit et le chiffre disparaît

- [ ] (0.1161) Découvertes › échange GitHub : un joueur déjà connecté voit « Tensho se connecte maintenant à GitHub par son application GitHub… » et se reconnecte ; la page d'autorisation de GitHub nomme l'application de Tensho et ne demande que les tickets du dépôt et les gists ; un envoi crée le gist secret et le ticket « Discoveries: <login> »

- [ ] (0.1163) Où : Gold Saucer, le Dédale bestial ; dans Tensho : CONTENUS › Dresseur › onglet « Équipe ». Faire : 1) finissez un plateau du Dédale bestial ; 2) laissez la fenêtre de résultat du jeu ouverte quelques secondes (Tensho la lit à ce moment) ; 3) ouvrez /tensho › Dresseur › Équipe ; 4) cliquez la ligne « Dernier plateau… » pour la déplier. Regarder : « Dernier plateau (date) : score… · bonus… · total… » avec les mêmes chiffres que la fenêtre du jeu ; dépliée, une ligne par bête « Nom : rang A (x/100) → rang B (y/100) », et « +N rang(s) » en vert si elle monte.


- [ ] (0.1164) Où : Tensho › Paramètres. Faire : 1) cochez puis décochez deux ou trois cases (par exemple dans l'onglet Carte) ; 2) déplacez une fenêtre de Tensho ; 3) cliquez « C'est fait ». Regarder : le jeu ne fige pas au clic ; l'agent vérifie dans le journal de Dalamud qu'aucun enregistrement des réglages n'a été lent.


- [x] (0.1165) Jardin avec plusieurs emplacements notés : clic droit sur une plante (menu « Potager N, emplacement M »), Entretenir : dans Jardinage, seul cet emplacement passe à « entretenu » (heure d'entretien suivante repoussée), les autres et l'onglet Alarmes suivent ; en mode développeur, dalamud.log a « Garden menu read: window …, patch N, bed M. »



- [x] (0.1166) Carte de Tensho sur la Forêt centrale : clic droit, onglet Éthérites : chaque éthérite a son nom (plus d'icône seule) ; survol de l'icône de l'éthérite de Bentbranch : son nom ; la recherche de la carte trouve « Bentbranch »

- [x] (0.1167) Point de récolte avec un objet « RARE » (une seule prise) : le prendre, puis l'objet suivant proposé : son plan dit « Plan établi sur ce qui reste à ce point (N PR, 5 essais) » et compte sur les essais restants, plus sur ceux de l'ouverture ; l'action suivante proposée correspond au plan

- [x] (0.1168) Application de bureau, page Mes outils : boutons Créer, Importer, Galerie, Modifier, Retirer et cases de progression grisés ; barre d'état « Lecture seule : ce que vous créez ou changez ici n'est pas enregistré »

- [x] (0.1168) Application de bureau, page Alarmes : alarme manuelle grisée ; page Paramètres : changer un réglage le remet aussitôt et le bas de la fenêtre dit « Non enregistré : … faites ce changement dans le jeu »

- [x] (0.1168) Dans le jeu : créer une alarme, un outil et un préréglage d'artisanat fonctionne comme avant (rien de grisé)

- [x] (0.1169) Atelier de la compagnie (sous-marins revenus) : l'historique des voyages garde les voyages d'avant après un rechargement du plugin, et les nouveaux s'ajoutent

- [x] (0.1169) Hauts faits suivis : la progression lue reste après un rechargement ; dalamud.log sans erreur « Tensho data »

- [ ] (0.1171) Monde › Découvertes, échange GitHub connecté : « Échanger » envoie toujours ; « Déconnecter » efface la connexion (plus de nom affiché) ; dalamud.log sans erreur GitHub

- [x] (0.1172) Objets › Organisation, sacs du personnage : l'aperçu remplit le sac 1, puis le sac 3, puis le 2, puis le 4 ; « Appliquer » range les sacs du jeu de la même façon ; une sacoche ou un servant se range comme avant

- [x] (0.1174) Mode développeur, en extérieur : vaincre un monstre qui laisse un objet, même sans le cibler : dalamud.log montre « Discovery: defeat of … read from the game's message » puis « Discovery: MonsterDrop item … » (plus de « Item gain not attributed » pour ce butin)

- [ ] (0.1174) Où : une zone extérieure, avec une carte au trésor déchiffrée qui n'ouvre pas de portail ; dans Tensho : MONDE › Découvertes. Faire : 1) creusez à l'endroit de la carte, battez les monstres, ouvrez le coffre ; 2) ouvrez /tensho › Découvertes ; 3) dans le champ « Monstre, ALÉA, objet… », tapez le nom de votre carte. Regarder : une ligne « <objet reçu> ← <nom de la carte> ouvert » pour chaque objet du coffre, et le compte « Objets ouverts » qui augmente.

- [x] (0.1175) Mode développeur, sur un point de récolte où un objet porte « RARE » (terreau III, par exemple) : ouvrir le point, faire une récolte sur l'objet RARE puis une autre ; dalamud.log a des lignes « addons » Gathering: …, à garder pour le relevé (logread.py)

- [x] (0.1176) Sur l'île de Lunule : Tensho › Eurêka et Bozja › onglet de l'île : le savoir (465 / 2800 par exemple) et les pièces comme dans le jeu ; le job de soutien porté en premier, en doré, avec le même niveau que la fenêtre des jobs de soutien du jeu ; « appris ensuite » donne l'action ou le trait du niveau suivant (info-bulle : sa description) ; l'expérience du job porté comme dans le jeu

- [x] (0.1176) Hors de l'île, après y être passé pendant la séance : l'onglet garde le dernier relevé avec son heure ; sans passage : « Pas encore lus pendant cette séance »

- [x] (0.1176) Mode développeur, sur l'île : dalamud.log, ligne « fields » : « experience … needed … » à comparer avec l'expérience du jeu (pour savoir si elle compte depuis le début du niveau)

- [ ] (0.1177) Barre d'usure et page Équipement › Usure et symbiose : une pièce peu portée affiche le même pourcentage que la fiche du personnage du jeu (1 % et non 0 %) ; l'info-bulle de la page donne la valeur exacte ; près de 100 %, comparer avec le jeu (99 % tant que l'extraction n'est pas possible)

- [x] (0.1178) Sur l'île de Lunule : le panneau apparaît (job de soutien porté en doré, niveau, barre d'expérience, « Ensuite : … », barre du savoir, pièces) et disparaît en quittant l'île ; ses nombres comme ceux du jeu ; il se déplace à la souris

- [x] (0.1178) Eurêka et Bozja › onglet de l'île : décocher « Panneau sur l'île » le cache ; les boutons « Ce que permet d'acheter : pièce d'argent / d'or des douze cités » ouvrent Rangements › Que faire avec ? sur la bonne pièce

- [x] (0.1178) Eurêka et Bozja › onglet de l'île : « Archives occultes lues : N / M » comme dans le jeu ; « Voir ce qui manque » ouvre Collections › Archives occultes, filtre non confirmées

- [x] (0.1179) Sur l'île de Lunule, hors combat : à l'annonce du jeu « Nouvelle confrontation critique ! Début dans 1:07 », une ligne [Tensho] « Confrontation critique : … — inscriptions ouvertes, encore 1:07 — zone ( X , Y ) » ; le temps et le lieu comme sur la carte du jeu ; une seule fois

- [x] (0.1179) Sur l'île : un ALÉA qui commence donne une ligne « ALÉA : nom (niveau N) — encore m:ss — zone ( X , Y ) », une seule fois ; l'application de bureau reçoit les mêmes lignes

- [x] (0.1179) Sur l'île, en combat : aucune ligne ni panneau ; à la fin du combat, la confrontation encore ouverte est dite

- [ ] (0.1179) Mode développeur, sur l'île : dalamud.log, lignes « Reading occultevents » à chaque nouvelle confrontation et changement d'étape (inscription, préparation, combat) : comparer « left » au compte à rebours du jeu

- [ ] (0.1180) Monde › Découvertes : l'avertissement avant « Connecter GitHub » parle de l'endroit exact d'un poisson pris ; l'info-bulle de « Échange automatique » dit « rien n'est envoyé automatiquement »

- [ ] (0.1183) Après Paramètres › Avancé › « Tout remettre par défaut » (ou une installation neuve) : la première touche M ouvre la carte de Tensho, et une ligne [Tensho] dans le chat dit comment retrouver celle du jeu ; la deuxième touche M ne la répète pas.

- [x] (0.1183) Aide › FAQ : « M ouvre une autre carte : comment retrouver celle du jeu ? » ouvre les Paramètres ; « Tensho joue-t-il à ma place ? » cite la carte sur M et le titre selon le job

- [ ] (0.1184) Mode développeur : après deux ou trois rechargements de Tensho, dalamud.log, lignes « Load created » et « Load 10 s later » : « heap … MB, Tensho load contexts N of M, previous instance … » ; si N grandit à chaque rechargement ou si l'instance d'avant reste « still alive », une fuite est vue (à rapporter)

- [x] (0.1186) Après un rechargement de Tensho, carte ouverte : les vendeurs (et ceux des fêtes en cours) apparaissent sur la carte comme avant ; page Événements : les PNJ des fêtes se chargent toujours

- [ ] (0.1187) Mode développeur : après trois rechargements de Tensho ou plus, dalamud.log, lignes « Earlier instance … » dix secondes après un chargement : pour les instances de deux chargements ou plus en arrière, « 0 of N parts alive » (le contexte peut rester « alive ») ; si des parties restent vivantes, rapporter leurs noms. Aucune ligne « framework subscriptions were still registered at unload »

- [x] (0.1188) Mode développeur : après un rechargement de Tensho, carte ouverte, dalamud.log : la première image de la carte (« Slow frame … map canvas ») n'a plus « canvas vendors » ni « icon list » en tête ; lignes « Earlier instance … » : parties collectées, état du déchargement, champs statiques

- [x] (0.1189) Hors mode développeur, en plein air avec des monstres autour : icônes et notes des monstres (listes, souhaits, journal de chasse) comme avant. Mode développeur : lignes « Nearby monsters » toujours écrites ; après trois rechargements, lignes « Earlier instance … hubs emptied, N of M parts alive » : rapporter les parties encore vivantes

- [x] (0.1190) Mode développeur : jouer quelques minutes (ville, plein air, carte ouverte), puis dalamud.log, lignes « Steady frames over 60 s » : rapporter les postes dont la moyenne dépasse 1 ms ou dont le 95e centile dépasse 5 ms

- [x] (0.1191) En plein air avec des monstres autour, juste après un rechargement de Tensho : icônes au-dessus des monstres et notes « journal de chasse » comme avant ; mode développeur : la première image des overlays (« Slow frame ») n'a plus « MonsterIcons » à 50 ms ou plus


- [ ] (0.1193) Page Tenues du jeu : « Équiper » une tenue fonctionne comme avant ; mode développeur : plus de ligne « Slow reader: GameGearsetReader… » au moment où la tenue change

- [x] (0.1194) Carnet d'artisanat et simulateur : une rotation calculée par Raphael s'affiche comme avant, et un second calcul de la même recette revient vite (séquence gardée sur le disque). Mode développeur : après trois rechargements de Tensho à partir de cette version, dalamud.log, ligne « Earlier instance … » de l'instance d'avant la dernière (sans « hubs emptied ») : quelques parties vivantes seulement, au lieu de 463 sur 471 ; rapporter le nombre

- [x] (0.1202) Panneau des collectables (point de récolte collectionnable) : le plan conseillé et ses étapes s'affichent comme avant, un peu plus vite ; Prudent, Équilibré et Tenter sa chance donnent les mêmes conseils qu'avant


- [x] (0.1206) Carte : quelques secondes après le chargement (plus de 12 s), ouvrir une carte de région (Nocturne, Forêt…) pour la première fois : elle s'affiche sans à-coup ; noms des liens et éthérites présents comme avant

- [x] (0.1206) Mode développeur : ouvrir Rangements puis Paramètres et changer d'onglet : si une image lente est notée dans dalamud.log, la ligne « Slow frame » nomme l'onglet (« storage … », « settings … »)

- [x] (0.1207) Application de bureau ouverte, jeu lancé : ouvrir puis fermer des pages qui lisent les gils ou les hauts faits sans rien changer : l'application ne recopie plus le fichier de données à chaque fois (pas de rechargement visible) ; un vrai changement (gils gagnés) arrive toujours

- [ ] (0.1211) Jardin : récolter deux emplacements à moins de 5 secondes d'écart (ouvrir le menu de chacun) : les deux sont retirés du journal comme récoltés

- [ ] (0.1212) Rangements › Personnages : « Effacer » un personnage qui n'est pas connecté : il disparaît de la liste, son journal des gils et ses hauts faits ne s'affichent plus pour lui

- [x] (0.1220) Paramètres › Général › Langue : Deutsch et 日本語 marqués « (partiel) » ; Tensho en anglais : pourcentages sans espace (usure de l'équipement, statistiques du personnage) et nombres « 1,234 » ; en français : « 1 234 » et « 12,5 % »

- [x] (0.1220) Barre serveur des monnaies et prochaine alarme : « 3h 05m » en anglais, « 3 h 05 » en français

- [x] (0.1221) Pied de page de la fenêtre Tensho : « Servants et chocobo : enregistrés à l'ouverture (datés) » ; avec l'enregistrement automatique des rangements coupé : « enregistrés sur demande (datés) »

- [x] (0.1222) Barre latérale d'une configuration sans groupe replié encore : Progression, Contenus, Gold Saucer et Références repliés ; un clic sur un groupe garde les autres comme ils étaient

- [x] (0.1222) Guide (Aide › Lancer le guide) : étape « Panneaux à côté des fenêtres du jeu » ; « Tout masquer » coupe les panneaux (fiche du personnage, outil de mission, synthèse, récolte, barre serveur…), « Tout afficher » les remet ; PaissaDB dans l'étape des sources

- [x] (0.1222) Guide sans intérêt coché : la visite ne montre que les pages pour tous (pas Triple Triade ni Artisanat)


- [x] (0.1222) Prise en main : relancer le guide (Aide, « Relancer le guide ») et vérifier : guide : nouvelle étape « Panneaux à côté des fenêtres du jeu » (tout masquer ou afficher, puis chaque panneau) et visite réduite aux pages pour tous sans case cochée ; aide : quatre nouvelles questions de la FAQ ; liste des outils de l'accueil : aucun changement (pas de nouvelle page)

- [x] (0.1223) Tensho en anglais : pages Compagnie libre (Submersible), Grandes compagnies (rank), infobulles « On: / Off: », orthographe britannique ; rien de cassé dans les infobulles et les boutons « Refresh »

- [x] (0.1226) Paramètres › Général : activer « Sacs », « État de l'équipement » et « Zone et instance » dans la barre serveur ; les trois entrées apparaissent, se mettent à jour (ramasser un objet, changer de zone), clic : Rangements, Équipement, carte

- [x] (0.1226) Zone à instances (instance 2 par exemple) : la marque d'instance du jeu suit le nom de la zone ; hors instance : le nom seul

- [x] (0.1227) Fiche d'un objet récent (onglet des sources ouvert une fois) : « Ajouté au patch 7.x (Garland Tools) » dans l'en-tête ; Garland coupé dans Paramètres › Données et sources : la ligne n'apparaît pas

- [x] (0.1228) Rangements › Personnages, 15 s après la connexion : colonne « Jobs » remplie pour le personnage connecté ; clic : niveaux des jobs et ensembles (numéro, nom, job, niveau d'objet) sous le tableau, mêmes valeurs que le jeu

- [ ] (0.1228) Changer de personnage puis revenir : les jobs et ensembles de l'autre personnage restent affichés ; monter un niveau : la valeur change dans les 5 minutes

- [ ] (0.1228) Objets › Rangements › Personnages : sur la ligne d'un personnage autre que celui connecté, cliquez « Effacer » et confirmez : sa colonne « Jobs » montre « — ».

- [x] (0.1229) Collections › Montures, survol de la baleine lunaire : « Porte 8 personnes », « Sa propre musique de monture » ; survol d'un gobbue : son action de monture, avec le nom du jeu ; survol d'une monture volante courante : « Musique de monture partagée avec 89 autres montures »

- [x] (0.1230) Fenêtre principale, champ « Chercher dans Tensho… » en haut de la navigation : taper « mine » : pages, objets, quêtes et lieux listés sous le champ ; clic sur un objet : sa page s'ouvre ; clic sur un lieu : la carte de Tensho s'ouvre sur ce lieu ; une lettre seule : rien n'est listé

- [x] (0.1230) Application de bureau : le même champ ; une page placée « en jeu seulement » n'est pas proposée

- [x] (0.1231) Mode développeur, artisan ou récolteur : lire un manuel (militaire, commercial ou révisé) puis faire une synthèse ou une récolte : dalamud.log a une ligne « Reading statuses » avec 45 ou 46 et son param à chaque changement (noter le param avant et après quelques gains d'expérience)

- [ ] (0.1232) Dans une zone d'un rang S « vaincre 100 monstres » (Heavensward, Stormblood, Shadowbringers ou Endwalker) : vaincre un monstre de la liste : Cartes › Conditions des rangs S, ligne « Vos monstres vaincus » passe à 1 ; un monstre vaincu par un autre joueur ne compte pas ; « Effacer » remet ce rang à 0 sans toucher aux autres

- [x] (0.1233) Page Mini Cactpot : « Prochain JACTA » à la prochaine heure :00, :20 ou :40 de l'horloge du PC ; « Tirage du Méga Cactpot » le bon jour à l'heure du PC (Europe : samedi 20:00 en heure d'hiver française) ; cloche du tirage : une alarme hebdomadaire apparaît dans Alarmes

- [x] (0.1234) Mode développeur, maison de la compagnie : ouvrir la base de tour éthéréen (un tour posé au moins) : dalamud.log a « Game windows » avec le nom de la fenêtre et une ligne « Reading addons: …Wheel…: » avec ses nombres ; noter l'heure à laquelle le tour a été posé et le temps restant que montre le jeu

- [x] (0.1235) Page Chocobo, section « Chocobo de course » avant toute course : « Pas encore lu » ; ouvrir la fiche du chocobo de course dans le jeu (Gold Saucer, guichet des courses) : catégorie, cote, pedigree, caractéristiques et aptitudes identiques à la fenêtre du jeu

- [x] (0.1236) Tensho en anglais : la navigation et les titres des pages disent « Allied Societies » et « Custom Deliveries »

- [x] (0.1237) Événements (une fête en cours) et une boutique dont un objet demande une quête : la coche des quêtes faites est la même qu'avant ; mode développeur : pas de ligne « slow frame » pour Events au changement du journal

- [x] (0.1238) Application de bureau : elle s'ouvre, la navigation n'a pas les pages laissées au jeu, une page se choisit, une alarme due sonne avec sa notification Windows ; sa console ne dit ni « Contract » ni « Typed contract »

- [x] (0.1239) Chargement et déchargement du plugin (désactiver puis réactiver dans Dalamud) : aucune erreur Tensho dans dalamud.log ; la banque (Rangements) garde ses sacs et ses servants

- [x] (0.1240) Barre serveur : alarmes, rappels, monnaies, gils et entrées facultatives au même endroit qu'avant, info-bulles et clics comme avant ; désactiver puis réactiver le plugin : aucune erreur Tensho dans dalamud.log

- [x] (0.1241) Eurêka et Bozja › Météo et nuit : les quatre zones d'Eurêka, noms d'ALEA et de météo du jeu, une prochaine fenêtre par ligne ; en Eurêka, la fenêtre « maintenant » concorde avec la météo affichée par le jeu

- [x] (0.1241) Bouton « Alarme » d'un monstre à météo (ex. Pagos, morbole de neige) : alarmes ajoutées dans Alarmes, qui ne sonnent que pendant la météo

- [x] (0.1241) Paramètres › Avancé › « Données du jeu et observées seulement » coché : l'onglet dit que ces conditions sont masquées

- [x] (0.1241) dalamud.log : ligne Selftest « Eureka notorious monsters » ok

- [x] (0.1242) Page Équipement : choisir une pièce, poser une matéria, changer de job prévu, cocher « supposer HQ » : statistiques, niveau d'objet moyen et valeurs dérivées changent aussitôt comme avant ; mode développeur : pas de ligne « slow frame » pour Gear

- [x] (0.1243) Planificateur des aéronefs, objectif « Butin : un objet » : choisir un objet (recherche), « Peut tomber à » nomme des secteurs, chaque trajet listé passe par l'un d'eux

- [x] (0.1243) Objectif « Débloquer un secteur » : choisir un secteur, la ligne dit par quel secteur, le rang et l'ordre d'exploration ; chaque trajet passe par ce secteur

- [ ] (0.1244) Mode développeur, Paramètres › Diagnostics : la section « Tableau de bord » est en tête ; ses jauges et compteurs bougent une fois par seconde ; section repliée, plus rien ne change.

- [x] (0.1245) Paramètres › Général : cocher « État de Tensho dans la barre serveur » : « Tensho : ok · … Mo » dans la barre du jeu, info-bulle avec les lignes, clic qui ouvre les paramètres ; le tableau de bord s'affiche sous le réglage

- [x] (0.1245) « Seulement en cas de souci » coché : l'entrée disparaît tant que tout va bien

- [x] (0.1246) Paramètres › Général : cocher « État de Tensho dans un panneau » : petit panneau « Tensho : ok · … Mo » déplaçable, bouton Détails qui ouvre les paramètres ; avec « Seulement en cas de souci », le panneau disparaît tant que tout va bien

- [x] (0.1246) Application de bureau reliée au jeu (Paramètres › Avancé) : la barre d'état dit « Tensho : ok · … Mo » après la zone ; Paramètres › Général de l'application montre « En jeu : … » dans le tableau de bord

- [x] (0.1247) Paramètres › Général, état de Tensho activé : le tableau de bord dit « Démarré en … ms », « Panneaux : aucun coupé » ; après quelques prix du marché affichés, la ligne des requêtes compte universalis.app

- [x] (0.1248) Compagnie › Aéronefs, après avoir ouvert la liste des aéronefs à l'atelier : ouvrir « Vos aéronefs : meilleur trajet de chacun » : une ligne par aéronef avec son trajet ; changer l'objectif change les lignes ; « Choisir » remplit le planificateur ; aucun à-coup visible à l'ouverture

- [ ] (0.1252) Fenêtre de Tensho rétrécie sous environ 620 px de large : la liste des pages disparaît, un bouton à trois barres en haut à gauche l'ouvre dans une fenêtre surgissante ; choisir une page la referme ; l'en-tête nomme la page

- [ ] (0.1252) Paramètres › Général › « Liste des pages » : « Toujours visible » garde la colonne dans une fenêtre étroite, « Toujours repliée » la replie dans une fenêtre large ; ↺ revient à « Automatique »

- [x] (0.1252) Page choisie dans la liste : fond de sélection et barre dorée sur le bord gauche, texte de la couleur normale ; sous-pages de Récolte, Artisanat, Collections : barre sous le bouton choisi ; à vérifier aussi en thème clair

- [ ] (0.1252) Échelle de Dalamud à 150 % : la colonne des pages s'élargit avec le texte (plus de noms coupés)

- [ ] (0.1253) Erreur de données (par exemple Marché avec Universalis coupé dans Paramètres › Données et sources) : ligne « Source désactivée » avec un glyphe d'arrêt, grise, puis la phrase d'explication ; une vraie panne réseau : « Échec » en rouge avec un triangle

- [ ] (0.1253) Accueil juste après le chargement du plugin : « En attente » avec un sablier tant que les catalogues se chargent, puis la coche verte « Catalogues d'objets et de recettes prêts »

- [ ] (0.1254) Jardinage › Journal : le bouton « Planter maintenant » se détache (fond de sélection, bord doré) ; le titre de chaque potager est dans la police des sections


- [x] (0.1255) Rangements › journal des gils : les colonnes de montants (gagné, dépensé, net, variation, solde) sont alignées à droite, les chiffres les uns sous les autres

- [ ] (0.1255) Page Météo (choisissez une zone) et page Souhaits : les tableaux ont une ligne sur deux d'une autre teinte et un trait fin entre les lignes ; aucun texte n'est coupé.

- [ ] (0.1257) Jardinage › Jardin : chaque parcelle d'un potager est l'icône de sa graine avec une barre de pousse, le temps restant et une pastille ; survol d'une graine : infobulle avec lieu, état, plantation, récolte, entretien ; clic : la graine devient l'objet en contexte ; clic droit : son menu

- [ ] (0.1257) Parcelle à entretenir : pastille pleine orange avec « ! » ; parcelle mûre : pastille verte avec une coche, « Mûr » et barre pleine verte ; entretien à venir : pastille au contour gris avec une goutte (vérifier que la goutte et la coche se lisent)

- [ ] (0.1257) Bouton main avec goutte « Entretenu » : la pastille repasse au contour ; bouton corbeille « Retirer » : premier clic, le glyphe passe en rouge et l'infobulle dit de cliquer encore ; second clic, la parcelle disparaît

- [ ] (0.1257) « Vue détaillée » cochée : le texte d'avant (graine, dates, boutons « Entretenu » et « Retirer », croisements) ; décochée : les icônes

- [ ] (0.1257) Parcelle ajoutée à la main avec une graine au nom libre : le nom s'écrit à la place de l'icône, avec « À entretenir » en toutes lettres quand c'est dû

- [ ] (0.1257) Manette ou clavier (navigation de Dalamud activée) : le focus passe d'icône en icône dans l'ordre 1, 2, 3, 8, 4, 7, 6, 5 puis sur les boutons ; l'infobulle s'affiche au focus ; noter si le menu de l'objet s'ouvre

- [ ] (0.1258) Application de bureau › Jardinage : les potagers montrent leurs prévisions de croisement, la section Croisements liste les paires, la liste des graines n'est plus vide (elle reste grisée : rien ne s'enregistre dans l'application)

- [ ] (0.1258) En jeu, après le chargement de cette version : Jardinage › liste des graines et section Croisements comme avant (le code a changé de place)

- [ ] (0.1259) Jardinage, potager avec deux graines voisines compatibles : petites icônes des graines possibles sous la parcelle ; survol : graine, parcelle voisine, possession, « Prévision, non vérifiée en jeu » ; clic : objet en contexte

- [ ] (0.1259) Graine possible dans la liste de souhaits : étoile dorée sur son icône ; graine absente des sacs et des stocks lus : disque orange ; servants jamais ouverts sur ce personnage : ni disque ni « pas possédé », l'infobulle dit « Possession inconnue »

- [ ] (0.1259) Parcelle sans voisine compatible : glyphe barré avec la phrase en infobulle ; graine hors des paires : point d'interrogation

- [x] (0.1259) Bouton à icône (double coche) à côté du titre du potager : toutes les parcelles à entretenir passent en « entretenu » ; son infobulle l'explique ; vérifier que la double coche se lit

- [x] (0.1260) Météo d'une zone : bandeau de trois blocs (Maintenant, Ensuite, Après) au-dessus du tableau, avec icône, nom, heures ET ; « Ensuite » et « Après » ont un compte à rebours qui avance à la seconde et l'heure du PC ; la cloche crée la même alarme que celle du tableau

- [ ] (0.1260) Rester sur la page au changement de période (toutes les 23 min 20 s) : le bloc courant change, avec un fondu très bref de son fond ; avec « mouvement réduit » coché dans les réglages de Dalamud : aucun fondu

- [ ] (0.1260) Fenêtre de Tensho étroite : les trois blocs du bandeau s'empilent, rien de coupé

- [ ] (0.1261) Accueil › Aujourd'hui : première ligne « Prochaine échéance : …, dans … » (alarme, retour de vaisseau ou jardin, la plus proche) ; un clic ouvre l'outil ; sous une heure le temps avance à la seconde ; « Personnaliser » permet de la masquer

- [x] (0.1261) Accueil : chaque tuile réagit d'un seul bloc au survol et au clic (icône, nom et ligne ensemble) ; au clavier ou à la manette, un seul arrêt par tuile

- [x] (0.1261) Fiche d'un objet (page Objets) : bouton « Ajouter à mes souhaits » sous le nom ; clic : il devient « ✓ Dans mes souhaits » sans changer de largeur, avec un contour vert très bref, et le pied de page dit « ajouté » ; nouveau clic : l'objet est retiré

- [ ] (0.1261) Avec « mouvement réduit » coché dans les réglages de Dalamud : aucun contour bref sur l'Accueil ni sur le bouton des souhaits

- [ ] (0.1262) Carte de Tensho dans une zone avec des monstres suivis (liste, journal de chasse ou ALÉA) : leurs marques sont moitié moins grandes qu'avant ; les repères d'exploration (jumelles) un peu plus petits

- [x] (0.1262) Carte › tailles des icônes : ligne « Monstres proches » à 50 % et « Repères d'exploration » à 80 % ; changer une taille la garde après rechargement ; 0 masque les marques ; ↺ revient à la valeur par défaut

- [x] (0.1262) Si vous aviez déjà réglé la taille des repères d'exploration : elle n'a pas changé

- [ ] (0.1263) Grande maison avec deux ou trois potagers plantés : Jardinage montre une grille par potager, titrée « lieu · potager 1 », « potager 2 », « potager 3 », chacune avec ses parcelles

- [ ] (0.1263) Bouton « tout le potager entretenu » d'un potager : seules les parcelles de ce potager passent en « entretenu », celles des autres potagers de la maison ne changent pas

- [x] (0.1264) Objets › Marché, un objet choisi : pendant la recherche, ligne « En attente » avec son sablier ; un prix au cache expiré ou à l'envoi ancien : ligne « Donnée ancienne » (glyphe d'horloge) ; prix alignés à droite

- [ ] (0.1264) Rangements : un rangement jamais ouvert (sacoche, coiffeuse mirage, bahut personnel, servant) affiche « En attente » et sa phrase ; rien d'autre n'a changé dans les listes.

- [x] (0.1264) Survol d'un objet avec un prix ancien : « Donnée ancienne » se lit dans l'infobulle sans la déformer

- [ ] (0.1265) Où : Tensho › PRODUCTION › Artisanat, petit bouton « Synthèse en cours » en haut de la page. Faire : 1) lancez une synthèse dans le jeu ; 2) ouvrez /tensho › Artisanat › « Synthèse en cours » ; 3) jouez une action ; 4) finissez ou quittez la synthèse. Regarder : pendant la synthèse, un œil, « Mesuré » et « Observation à HH:MM:SS (heure PC) · récente », l'heure qui avance à chaque action ; après la fin : « Aucune fenêtre de synthèse ouverte. ».

- [ ] (0.1265) Où : la fenêtre Tensho « Comparer les prochains coups · expérimental », collée à la fenêtre de synthèse du jeu (réglage : Paramètres › Panneaux à côté du jeu › Aide à l'artisanat, case « Fenêtre « Comparer les prochains coups » à côté de la synthèse »). Faire : 1) cochez ce réglage ; 2) lancez une synthèse ; 3) regardez la fenêtre Tensho à côté. Regarder : juste au-dessus du tableau (colonnes « Action », « Progression après »…), une calculatrice, « Estimation » et la phrase « Le tri porte sur un gain immédiat ; il ne choisit pas un plan de synthèse viable. ».

- [ ] (0.1265) Où : Tensho › PRODUCTION › Artisanat, onglet « Simulation ». Faire : 1) dans « Chercher une recette… », tapez un objet et cliquez-le ; 2) dans « Ajouter une action », cliquez « Travail rapide » ou « Ouvrage hâtif » ; 3) lisez sous le résultat ; 4) retirez ces actions. Regarder : une calculatrice, « Estimation » et la phrase « Actions aléatoires dans la séquence (Travail rapide 50 %, Ouvrage hâtif 60 %) : leur réussite est supposée. » ; la ligne disparaît quand ces actions sont retirées.

- [x] (0.1265) « Que puis-je fabriquer ? » après avoir déplacé des objets : « Donnée ancienne » à côté du bouton de recherche

- [x] (0.1266) Outil PR avec un objet de récolte choisi, en mineur ou botaniste : petite calculatrice après « Estimé : +N » ; au survol « Estimation · calculé par Tensho… »

- [x] (0.1266) Panneau de récolte devant un point : calculatrice après la ligne du rendement attendu du plan

- [ ] (0.1266) Production › Récolte, section « Point de récolte actuel · expérimental » (réglage « Observer le point MIN/BTN ouvert dans le jeu » activé) : en mineur ou botaniste, ouvrez un point de récolte. La ligne « Mesuré » montre le métier, le niveau, les PR et l'heure. Fermez la fenêtre de récolte : la ligne passe à « En attente ».

- [ ] (0.1267) Cartes, juste après le chargement : « En attente » avec le compte des positions lues, puis les listes

- [ ] (0.1267) Défi d'un PNJ à la Triple Triade : ligne « Estimation » au-dessus des chances de vos decks ; pendant une partie, un conseil « approximatif » porte une petite calculatrice

- [ ] (0.1267) Escouade, classement des équipes : ligne « Estimation » avec la phrase sur le pourcentage

- [ ] (0.1268) ALÉA en cours, carte de Tensho : ses monstres portent l'icône de l'ALÉA (épées, boss, collecte…) au lieu de l'étoile ; survol : « ALÉA : nom »

- [ ] (0.1268) ALÉA avec des alliés (escorte, défense avec gardes) : les alliés portent la marque verte « mains tendues », survol « Allié de l'ALÉA : nom » ; les ennemis gardent l'icône de l'ALÉA, même avant d'être attaqués

- [ ] (0.1268) Paramètres › Carte › « Icônes au-dessus des monstres proches », « ALÉA » coché, pendant un ALÉA : dans la vue du jeu, l'icône de l'ALÉA au-dessus des ennemis et la marque verte « mains tendues » au-dessus des alliés, comme sur la carte de Tensho.

- [x] (0.1269) Paramètres › Avancé, « état de Tensho » coché (ou page Diagnostic en mode développeur) : sous « Tableau de bord », une rangée de jauges (mémoire, page la plus lente, requêtes par minute) et des compteurs ; chaque jauge a sa valeur écrite et une aide au survol

- [ ] (0.1269) Fenêtre rétrécie : les jauges passent à la ligne sans se chevaucher ; thème clair : cadran et chiffres lisibles

- [ ] (0.1269) Mode développeur : jauge « Image de Tensho » après une minute, compteur « Images lentes », ligne « Historique : N points ; dernier point lu en X ms »

- [ ] (0.1270) Tableau de bord en jeu : un clic sur la jauge « Mémoire utilisée » montre sa courbe sous les jauges (« il y a N min » à gauche, « maintenant » à droite) ; un second clic la cache ; clic sur une autre jauge : sa courbe remplace la première

- [ ] (0.1270) Paramètres › Général › En-tête et accueil, « État de Tensho » coché : sous le tableau de bord, cliquer la jauge « Mémoire » : sa courbe s'affiche ; passer la souris sur la courbe : « Plus bas …, plus haut …, dernier … ».

- [ ] (0.1271) Application de bureau reliée au jeu (les deux en 0.1271 ou plus), page Diagnostic ou Paramètres › Avancé : dix secondes après le lien, les jauges du jeu (mémoire, page la plus lente, requêtes par minute) et les compteurs ; après vingt secondes, les courbes se tracent et s'allongent

- [ ] (0.1271) Jeu fermé ou lien coupé : « L'état de Tensho en jeu n'est pas connu ici » ; les courbes déjà vues reviennent au lien suivant tant que l'application reste ouverte

- [ ] (0.1271) En jeu : tableau de bord inchangé (jauges, courbe sur un clic), compteurs sur une ligne sous les jauges

- [ ] (0.1272) Paramètres › Général : cocher « Suivre le style de Dalamud » : la fenêtre de Tensho, la carte et les panneaux prennent les couleurs et espacements des autres fenêtres Dalamud ; les titres restent en couleur d'accent de Tensho ; tout reste lisible (lignes d'état, jardin en icônes, jauges)

- [ ] (0.1272) Paramètres › Général : décocher « Suivre le style de Dalamud » : le thème de Tensho revient aussitôt, sans décalage ni couleur restée.

- [ ] (0.1272) Dans les réglages de Dalamud › Apparence, choisir un style clair ; dans Tensho › Paramètres › Général, cocher « Suivre le style de Dalamud » : les titres et repères de Tensho se lisent bien sur le fond clair.


- [ ] (0.1279) Au lancement du jeu (ou après une mise à jour de Tensho), les panneaux et barres de Tensho apparaissent 1 à 6 s après, sans petit gel de l'image au moment où ils arrivent.

- [x] (0.1285) Stockage › Banque, onglet d'un servant relu à la cloche (deux servants) : ses sacs s'affichent dans l'ordre du jeu, ses gils et cristaux sur une page après les sacs, plus de « réimportez ».

- [x] (0.1286) Atelier de compagnie : ouvrir le panneau des sous-marins puis celui des dirigeables (puis l'inverse) : Atelier › Vaisseaux liste les deux vaisseaux partis, la barre serveur et Alarmes donnent les deux retours ; dalamud.log dit « Workshop read without … » une fois par visite.

- [x] (0.1287) Survol d'un objet déjà vu au tableau du marché, puis Ctrl+Alt : « Consulté » passe à l'heure présente, les lignes du monde, du centre de données et de la région changent avec ; dalamud.log dit « Hover price refresh (Ctrl+Alt): item …, Success » ; Paramètres › Général, le tableau de bord compte universalis.app.

- [x] (0.1288) Alarmes › Alarmes à date : créer une alarme quotidienne, « Modifier » : le champ « Date et heure » montre la prochaine sonnerie ; changer l'heure et quitter le champ : la ligne de l'alarme dit la nouvelle heure, une seule alarme dans le groupe, toujours là après un rechargement.

- [x] (0.1289) En dresseur, page Équipement : les cases Arme et Main secondaire montrent la hache (ou le tabar) et le bouclier portés, le niveau d'objet moyen les compte ; recherche d'une pièce (Tête) au niveau du dresseur : des pièces du dresseur sont proposées.

- [x] (0.1290) En dresseur 45, page Équipement, recherche d'une pièce (Tête) : la liste dit « Niveaux 40 à 45 » et propose des pièces ; au niveau 50 (ou un autre job 50/60/…/100) : seulement le niveau exact ; Paramètres, le réglage s'appelle « Niveau exact aux paliers ».

- [x] (0.1291) Survol d'un objet regardé au tableau des ventes il y a peu, puis Ctrl+Alt : la ligne du monde dit « Vu au tableau des ventes <date> » et « Consulté » prend l'heure de l'appui ; sur un objet jamais ouvert au tableau : « Relevé Universalis <date> » (une date ancienne veut dire que personne ne l'a envoyé depuis).

- [x] (0.1292) Page Alarmes › Alarmes à date, une alarme du 01/12 : la ligne dit « prochaine 01/12/2026 00:00 (dans 17 j … h) » ; une alarme d'aujourd'hui : l'heure seule et « dans 3 h 05 » ou « dans 45 min ».

- [x] (0.1293) Stockage › Banque, onglet d'un servant (deux servants) puis vue Globale : une ligne « <servant> · gils : <montant> » avec l'icône de gil, plus de case gil dans un « Sac 6 » ; vue en liste : pas de ligne Gil.

- [x] (0.1294) Page Équipement, clic sur un emplacement (Main secondaire, niveaux 40 à 45), fenêtre étroite puis large : aucun texte coupé au bout des lignes ; survol d'une pièce : « Statistiques de base », puis « Comparé à l'équipement porté » avec les écarts verts et rouges deux par deux et « Valeurs de base, sans matérias », puis niveau, stock et obtention.

- [x] (0.1295) Page Équipement : survol du nom de la pièce portée d'un emplacement (boucles d'oreilles) : « Statistiques de base » puis « Valeurs de base, sans matérias » ; survol d'une pièce de la liste : même infobulle avec « Comparé à l'équipement porté ».

- [x] (0.1295) Rangements (Banque), Objets, Marché et un PNJ vendeur : survol d'une pièce d'équipement : statistiques de base et comparaison avec la pièce portée ; survol d'un objet qui n'est pas de l'équipement (cristal) : pas de bloc de statistiques.

- [x] (0.1296) Alarmes › Ajouter à cette date : deux alarmes nommées « A » et « B » : deux blocs titrés « A » et « B » (l'alarme « test » déjà enregistrée garde son nom comme titre) ; une troisième sans nom : bloc « Alarme à date · » et sa date.

- [x] (0.1222) Accueil à la première visite : une seule description (le bandeau dit par où commencer) ; Aide : FAQ commençant par « Par où commencer ? »

- [x] (0.1254) Titre de chaque page : plus de trait juste sous la description, un espace à la place ; rien ne se chevauche (à voir sur Accueil, Récolte, Marché, Paramètres)

- [ ] (0.1273) Application de bureau : les flèches des catégories de la navigation (OBJETS, PRODUCTION…) sont des triangles, plus des rectangles ; Jardinage : la goutte et la coche des parcelles se lisent ; tableau de bord : le « ? » des lignes à regarder se lit

- [ ] (0.1300) Application de bureau : Pêche en mer, ALÉA, Eurêka et Bozja, Donjons sans fond, Lexique et Armes antiques montrent leur contenu. Peuples alliés, Grandes compagnies, Félicité insulaire, Commandes spéciales et La Grande Mascarade ne disent plus « … n'ont pas pu être lues », mais « Lu dans le jeu » sous leur bouton. Collections, Quêtes, Triple Triade, Dresseur, Livraisons spéciales et Découvertes disent « Lu dans le jeu ».

- [ ] (0.1300) En jeu : Peuples alliés, Grandes compagnies, Félicité insulaire, Commandes spéciales, La Grande Mascarade, Armes antiques, Pêche en mer, ALÉA, Eurêka et Bozja, Donjons sans fond et Lexique se remplissent comme avant.

- [ ] (0.1302) En jeu, Dédale bestial : après un plateau (ou une lecture des rangs dans la fenêtre d'équipe), l'équipe proposée reste la même après un rechargement du plugin (plans enregistrés hors du fil du jeu)

- [ ] (0.1304) En jeu, Accueil : rétrécir la fenêtre ; les tuiles dont le texte ne tient pas finissent par « … » (jamais coupées au milieu d'un mot sans « … »), et l'infobulle de la tuile donne le texte entier

- [ ] (0.1305) Découvertes : après une tâche de servant (exploration), la ligne « … ← tâche : » montre le nom de l'exploration (par exemple « Exploration en forêt XXI »), plus « n° 584 » ; une tâche ciblée montre « tâche de <classe> niv. <niveau> »

- [x] (0.1306) Peuples alliés : après « Actualiser », le choix « Genre » montre seulement les peuples de combat, d'artisanat ou de récolte ; les Namazu sont sous artisanat et sous récolte ; la colonne « Genre » le dit ; le choix reste après fermeture et réouverture de Tensho

- [x] (0.1307) Carte, Mor Dhona (X 21.4 Y 5.5) : sans la quête « Les cristaux d'Ardashir » (67748) prise, Cristiana (« Matériaux pour armes anima ») n'est plus sur la carte de Tensho ; les autres marchands de Mor Dhona (Kakalan…) restent

- [x] (0.1307) Carte : marchand d'une quête : une fois sa quête prise, il apparaît sur la carte de Tensho sans recharger (la carte se refait)

- [ ] (0.1308) Palais des morts (entrée, Gridania de l'Est … ou par l'outil des missions) : Tensho reste actif (pas de ligne « Tensho est en pause » dans le chat, pages ouvertes) ; touche M : la carte du jeu s'affiche (celle du donjon), pas celle de Tensho ; la fenêtre de carte de Tensho, si elle était ouverte, est cachée et revient à la sortie

- [ ] (0.1308) Donjon profond : en combat, l'affichage de Tensho suit les mêmes réglages que dans un donjon normal (caché en combat s'il l'est ailleurs)

- [ ] (0.1308) JcJ (Wolves' Den ou Crystalline Conflict) : Tensho se coupe toujours avec sa ligne dans le chat

- [x] (0.1316) Eurêka et Bozja › Météo et nuit : la prochaine fenêtre d'un monstre notoire à plus d'une heure s'écrit « dans 2 h 05 » (pas « dans 125 min »)

- [ ] (0.1316) Alarmes : une alarme unique avec 90 min d'avance sonne « Alarme : … — dans 1 h 30 (…) » dans le chat ; le bouton de répétition dit « Dans … » ; Gold Saucer : tirage du Méga Cactpot « (dans 2 j 3 h) »

- [ ] (0.1316) Discord (si un salon est réglé) : le bouton d'essai envoie « … dans 5 min (…) » ; un message personnel avec [minutes] garde le nombre seul

- [x] (0.1317) Objets : ouvrir la fiche d'un objet récent (le patch s'affiche dans l'en-tête), puis survoler cet objet dans une liste de Tensho : l'infobulle dit « Ajouté au patch 7.x (Garland Tools) » ; Paramètres › Données et sources › Garland coupé : la ligne disparaît

- [x] (0.1318) Eurêka et Bozja › Météo et nuit : chaque monstre « nuit » montre une prochaine fenêtre (« maintenant » ou dans moins de 70 min) ; les monstres à météo montrent la leur quand la météo vient dans les 28 h ; comparer une ligne avec la météo annoncée en jeu dans la zone

- [x] (0.1319) Rangements › Gils, période 1 jour : finir une ALÉA (ligne « ALÉA (récompenses) »), vaincre quelques monstres qui donnent des gils (« Monstres vaincus »), achever une rubrique du carnet d'objectifs (« Carnet d'objectifs ») ; acheter, vendre à un PNJ, réparer chez un PNJ, se téléporter : chacun dans sa catégorie, aucun dans « Autre »

- [x] (0.1320) Mor Dhona, carte de Tensho : plus d'icône « Échangeur » seule en X 21.4 Y 5.5 (à l'ouest de la Maison des splendeurs) ; les autres icônes de marchands et d'échange que la carte du jeu montre sont toujours là ; ouvrir la carte du jeu puis regarder la ligne « map icon unlocks » de l'auto-contrôle

- [x] (0.1321) Mor Dhona, après avoir ouvert la carte du jeu : la carte de Tensho montre exactement les icônes d'échange et de marchands que la carte du jeu montre (plus d'« Échangeur » seul en X 21.4 Y 5.5 ; l'icône vers X 28 Y 13.7 est là si le jeu la montre)

- [x] (0.1322) Objets › Organisation, sacs du personnage, avec la zone qui couvre les 140 cases : l'aperçu remplit le sac 1, puis le sac 3, puis le sac 2 ; « Appliquer » (ou le bouton Tensho de l'inventaire) : dans le jeu, la colonne de gauche (sacs 1 et 3) se remplit avant celle de droite

- [x] (0.1323) Aide › Signaler un problème : préparer un bug de test (titre « essai ») et ouvrir le formulaire : le titre est « [Bug] essai », l'étiquette player-report est posée à l'envoi (puis fermer le ticket)

- [x] (0.1324) Aide › Signaler un problème, GitHub connecté (Monde › Découvertes) : choisir « Par ma connexion GitHub », titre « essai X-340 », « Envoyer sur GitHub… » : l'aperçu montre le titre puis le texte avec le rapport entier ; « Envoyer maintenant » : « Envoyé : ticket n° N », « Ouvrir le ticket » montre le même texte, avec les étiquettes bug et player-report posées en moins d'une minute (puis fermer le ticket)

- [x] (0.1324) Aide › Signaler un problème, voie « Par mon navigateur » avec un long texte (plus de 6 000 caractères) : l'aperçu montre l'encadré « Trop long pour l'adresse… collez-le (Ctrl+V) » ; après l'ouverture, l'encadré « Votre rapport est copié » reste visible

- [x] (0.1326) Marché, un objet rare jamais envoyé à Universalis sur votre monde : ouvrir ses offres au tableau du jeu, puis survoler l'objet avec Ctrl+Alt : la ligne de votre monde garde le prix vu au tableau (marqué « vu au tableau »), avec les autres mondes d'Universalis

- [x] (0.1327) Monde › Découvertes, avec des tâches de servant notées : ouvrir le tableau et chercher « servant » : les noms des tâches s'affichent (un numéro au plus un instant) ; dalamud.log sans ligne « slow frame » pendant ce temps

- [x] (0.1327) Sociétés alliées : première ouverture de la page après un rechargement : « Lecture des données du jeu… » un instant, puis les sociétés et leurs genres ; dalamud.log sans ligne « slow frame » à cette ouverture

- [x] (0.1330) Après la prochaine release, installée depuis le dépôt (mode normal) : /xlplugins, onglet des plugins installés : Tensho Assistant montre son icône (plus le « ? » de Dalamud).

- [x] (0.1331) Ouvrir un objet au tableau du marché du jeu, le refermer, puis le survoler avec Ctrl+Alt : la ligne de votre monde garde le prix vu au tableau (source : tableau du marché, heure du relevé), pas celle d'Universalis.

- [x] (0.1335) Ouvrir la carte du jeu à Mor Dhona, puis la carte de Tensho sur une autre zone, puis revenir à Mor Dhona sur la carte de Tensho sans rouvrir celle du jeu : l'icône à X 28.5 Y 14.1 (dessinée par le jeu) reste, l'icône « Échangeur » seule à X 21.4 Y 5.5 reste absente.

- [x] (0.1338) Depuis n'importe quelle zone, ouvrir la carte du jeu et afficher Mor Dhona, puis 4 ou 5 autres zones (quelques secondes chacune) : dalamud.log a une ligne « Map icon unlocks on map … » pour chaque carte qui a des icônes à déblocage (vérifié par l'agent).

- [x] (0.1340) Ouvrir la carte de Tensho à Ul'dah (Faubourg de Thal ou Quartier de Nald) puis à Mor Dhona : les icônes fixes présentes sur la carte du jeu y sont, l'icône « Échangeur » seule à Mor Dhona (X 21.4 Y 5.5) reste absente.

- [x] (0.1342) Ouvrir la carte de Tensho au Faubourg de Thal (Ul'dah) : un seul masque bleu (celui que montre la carte du jeu), pas trois.

- [x] (0.1343) Carte de Tensho au Faubourg de Thal (Ul'dah), « Quêtes pas encore disponibles » décoché : les 4 icônes rouges de quêtes de déblocage que montre la carte du jeu y sont aussi ; une quête dont les quêtes préalables ne sont pas faites reste cachée.

- [x] (0.1344) Onglet Quêtes de la carte de Tensho au Gold Saucer, « Quêtes pas encore disponibles » décoché : seule la quête de l'événement en cours (« Avalanche de mots ») y figure parmi les quêtes d'événement, comme sur la carte.

- [x] (0.1345) Onglet Quêtes de la carte de Tensho au Gold Saucer, « Quêtes pas encore disponibles » décoché : « Toutes les cartes en main » n'y est plus (la carte du jeu ne la montre pas) ; les quêtes que la carte montre y sont.

- [x] (0.1346) Carte, clic droit, onglet Quêtes, « Quêtes pas encore disponibles » décoché, dans une zone où une quête est acceptée et où un donneur propose une autre quête (au même endroit si possible) : les quêtes que le jeu propose restent listées, et une zone où seules des quêtes acceptées figurent ne vide plus la liste des quêtes non commencées.

- [x] (0.1349) Monde › Découvertes › Échanger maintenant : l'échange finit « fait » et le fichier commun est dit récupéré ou à jour (lu dans community-discoveries/v3/ seulement).

- [x] (0.1350) Objets : ouvrir la fiche de la Bague d'attaquant de grand champion (ou un autre objet récent vendu par un PNJ) : l'en-tête dit « Ajouté au patch 7.4 (Garland Tools) » après un instant ; survoler l'objet dans une liste de Tensho et garder la souris dessus : l'infobulle dit la même ligne ; Paramètres › Données et sources › Garland coupé : la ligne disparaît des deux.

- [x] (0.1351) Chat : un lien de carte (par exemple le vôtre, tapé <flag> dans Dire) ; clic gauche : le jeu pose son drapeau, comme avant ; clic droit sur le lien : le menu « Tensho — lieu » s'ouvre à la souris (nom du lieu et coordonnées, Drapeau, Éthérite la plus proche, Afficher sur la carte, Météo de la zone) ; « Éthérite la plus proche » ouvre la carte Tensho sur le point avec la question de téléportation et son coût ; clic droit sur un lien d'objet : toujours le menu du jeu avec « Tensho — objet », pas le menu de lieu ; mode développeur : dalamud.log dit « Chat link hovered (ChatLogPanel_n): map link, map N » au survol.

- [ ] (0.1351) Carte › réglages › « Liens de carte du chat » coupé : le clic droit sur un lien de carte n'ouvre plus rien de Tensho.

- [x] (0.1352) Plugin en français : la barre latérale dit « ALÉA », « Eurêka et Bozja », « Donjons sans fond », « Armes antiques » ; Rappels : onglets « Missions aléatoires » et « Escouade » ; Rangements : onglet « Devises » ; Banque : « Coiffeuse mirage » et « Bahut personnel » ; Collections : onglet « Hauts faits » ; Compagnie libre : « Aéronefs » et « Secteurs d'aéronef » ; Équipement : « Tenues du jeu » ; aucun texte coupé ni débordant.

- [ ] (0.1352) Plugin en anglais : Dresseur, infobulle d'une bête « Trick / Tempered Release », colonne « CON », « Boss of the Board », filtre « Classification » ; Récolte : « logs » ; Triple Triade : « Card List ».

- [x] (0.1353) Plugin en français : la barre latérale dit « Accueil » ; l'aide de première visite des Livraisons spéciales cite « Lire de nouveau », comme le bouton de la page ; aucun texte coupé.

- [x] (0.1354) Plugin en français : Découvertes, ligne « ALÉA vus » ; Dresseur, colonne des lieux « (1 lieu(x) vu(s)) » ; recherche d'un objet avec une apostrophe tapée au clavier (« d'Ehcatl » par exemple) : il est trouvé.

- [x] (0.1355) Plugin en français : en-tête et page Météo, l'heure du jeu s'écrit « HE » ; Paramètres › Panneaux, réglage « Fenêtre « Modifier le jeu » » de Triple Triade ; Marché, colonne « Servant » et portée « Centre de traitement de données ».

- [x] (0.1356) Plugin en français : Carte, réglage « Lieux notoires » et alarme par clic droit sur un lieu notoire ; Objets, onglet « Synthèse liée » ; Compagnie libre, onglet « Appareils » ; Rappels, « Aventures imaginaires de Khloe ».

- [x] (0.1357) Plugin en français : Accueil, liste des outils, une fiche d'outil (Carte, Marché) commence sans « Disponible : » ; Paramètres, info-bulle du cache des quêtes cite « Actualiser les quêtes ».

- [x] (0.1358) Plugin en français : survol d'un objet (« Possédés : … », « déjà dans la collection ») ; Équipement, ligne des points perdus au plafond (« {0} points de … »).

- [ ] (0.1360) Jardin, potager avec un carreau mûr : ouvrir le menu du carreau et récolter (avec graines) : le carreau quitte le journal de Tensho, un message « récolté » dans le chat ; en mode développeur, dalamud.log dit « Garden harvest read: patch …, bed …, target … » et « Item gain attributed: … from Garden … ».

- [ ] (0.1360) Jardin : planter une graine à côté d'une plante déjà là, puis retirer ou replanter cette voisine : la prévision de croisement de la première ne change pas.

- [x] (0.1361) Organisation de l'inventaire, aperçu du tri des sacs : les cadres dans l'ordre Sac 1, Sac 3, Sac 2, Sac 4 (numéros de cases inchangés) ; appliquer le tri en mode développeur : « Tri visuel appliqué aux quatre sacs… » une seule fois sous « Rangement ».

- [x] (0.1362) Plugin en anglais : menu des pages (Ocean Fishing, Hunting Log, Deep Dungeons) ; plugin en français : Paramètres, onglet « Débogage » en mode développeur ; Aide › Signaler un problème : textes « issue » en anglais.

- [ ] (0.1363) Plugin en français et en anglais : infobulles de Paramètres › Avancé (tableau de bord, ventes des servants, lien avec l'application de bureau) et du simulateur d'artisanat (« Bénédiction de Byregot »), lisibles et justes.

- [ ] (0.1364) Plugin en français : Découvertes, recherche par deux lettres (monstre, ALÉA, objet) ; page Donjons sans fond, sous-titre ; jardin vide : phrase sur l'entretien et la récolte lus dans le chat.

- [ ] (0.1366) Chat : un lien de carte d'une autre zone (par exemple <flag> posé ailleurs, tapé dans Dire), la carte Tensho ouverte : clic gauche : le jeu pose son drapeau et la carte Tensho va sur la carte du lien, drapeau visible ; mode développeur : dalamud.log dit « Chat link clicked (ChatLogPanel_n): map link, map N, left button » puis « Chat link click: the Tensho map shows map N ».



- [x] (0.1367) Objets (page Objets ou recherche rapide de l'accueil) : taper « d'Ehcatl » avec l'apostrophe du clavier : la poignée de plumes et la paire de gants d'Ehcatl sont trouvées ; Découvertes : une recherche ne montre que les objets dont une source a été notée (un objet jamais obtenu n'y est pas).

- [x] (0.1368) Plugin en français : page Listes d'artisanat (titre), onglet « Liste d'artisanat » du simulateur, panneau à l'écran et ligne « Liste d'artisanat : il en manque … » de l'aide à la récolte : un seul nom partout.

- [ ] (0.1369) Plugin en français : page Bestiaire (colonne « Vaincus »), page Mandats (colonne et filtre « Client »), réglages de la carte Tensho (« Cibles d'élite proches », « Monstres de vos Chroniques du zodiaque »), page Rangs S (champ « Chercher une cible d'élite… »), Aide › questions « mettre à jour » et « installer » (« Mettre à jour les plugins », « Dalamud › Paramètres › Expérimental › Dépôts de plugins personnalisés »).

- [ ] (0.1370) Découvertes › Échange, jeu lancé, réseau coupé juste avant « Échanger maintenant » (ou pare-feu sur le jeu) : le message dit « GitHub injoignable ou fichier commun pas encore publié, réessayez plus tard », pas « Fichier commun illisible ».

- [ ] (0.1372) Mandats, en français puis en anglais : infobulle d'un mandat « Client : <nom> » une seule fois ; colonne Client et filtre des clients sans « Client : » devant chaque nom ; clic droit : « Éthérite la plus proche du PNJ du mandat » pose le drapeau près du PNJ qui donne ou reçoit le mandat.

- [ ] (0.1373) Dédale bestial, panneau d'équipe à côté du plateau (Tensho en français, puis en anglais) : aucune ligne coupée au bord (« Rang pas encore lu pour … bête(s)… », noms d'ennemis longs comme « Succube épéiste »). Les lignes passent à la ligne et gardent leur retrait. « faiblesse touchée : 0 sur 1 ennemi » au singulier, « 2 sur 3 ennemis » au pluriel.

- [x] (0.1374) En français : barre de navigation « Butin des missions » et « PNJ marchands » ; carte › réglage « Masquer en mission » ; infobulle d'un objet vendu : « (outil PNJ marchands) » ; panneau des servants au tableau des ventes : « Comparer aux autres vendeurs » inchangé.

- [x] (0.1375) Objets annotés, Collection, aide et carte : taper un nom avec une apostrophe courbe (d'Ehcatl tapé avec l'apostrophe courbe, Alt+0146) trouve le même résultat qu'avec l'apostrophe droite

- [ ] (0.1376) Chat : survoler un long lien de carte par son début, puis clic droit sur ses coordonnées à l'autre bout : le menu de Tensho s'ouvre (mode développeur : « Chat link right click: map N, menu opened »)


- [ ] (0.1378) Jardin : ouvrir le menu d'un emplacement, le fermer sans rien faire, puis acheter un objet à un marchand dans les 4 s : aucune récolte notée (mode développeur : pas de « Garden harvest read »)


- [x] (0.1379) Rappels : un rappel à plus de 24 h dit « Dans N j H h » (et l'heure seule pour aujourd'hui) ; la barre du serveur dit aussi « N j H h » pour une alarme à plusieurs jours

- [ ] (0.1380) Page Alerte : juste après qu'une alarme quotidienne (heure réelle) a sonné, ouvrez « Modifier » et validez la date sans la changer : l'alarme ne sonne pas une deuxième fois.

- [x] (0.1380) Équipement : ouvrir le choix d'une pièce, faire défiler une longue liste : les rangées gardent la même hauteur, le niveau reste sur la ligne

- [x] (0.1387) Collection › Titres et Hauts faits : ouvrir une fois la fenêtre Titres et la fenêtre Hauts faits du jeu, puis recharger Tensho (ou relancer le jeu) sans les rouvrir : les deux pages montrent le même compte d'obtenus qu'avant, et la ligne rouge dit « Relevé du <date> ».

- [x] (0.1388) Barre du serveur du jeu (Paramètres : entrées Sacs, État de l'équipement, Zone et instance cochées) : survoler chacune : l'infobulle est celle de Tensho (titre doré, même style que les alarmes), au-dessus ou en dessous selon le réglage « Au-dessus de l'entrée ».

- [ ] (0.1389) Chat : survoler un lien de carte en entrant par son bout gauche, puis clic droit à deux largeurs de lien à gauche du lien (sans passer ailleurs) : le menu de Tensho ne s'ouvre pas ; le même clic sur le lien l'ouvre. À une seule largeur à gauche, le menu s'ouvre encore : c'est voulu (la boîte est symétrique autour du point d'entrée, une largeur de lien plus 8 px de chaque côté ; X-419 M3).

- [x] (0.1389) Barre du serveur du jeu, entrée des alarmes en anglais : le temps restant s'écrit « 2h 15 » (et non plus « 2h 15m »), « 6d 0h » au-delà d'un jour.

- [ ] (0.1401) Barre du serveur (X-419 M2, 0.1393) : afficher une entrée de Tensho qui change souvent (objets dans les sacs, ou zone), survoler l'entrée et la garder survolée pendant qu'elle change (ramasser un objet, changer de zone) : l'infobulle reste celle de Tensho, elle ne repasse pas à celle du jeu le temps d'un rafraîchissement.

- [x] (0.1402) Carte de Tensho, dans l'atelier de compagnie (Brumée, Lavandière, Gobelet, Shirogane ou Empyrée) : la sonnette d'appel et le coffre de compagnie sont sur la carte avec les icônes du jeu ; le survol donne leur nom ; dans le tableau des icônes de la carte, la taille 0 de la sonnette les cache. Dans une ville (Limsa basse, Ul'dah Thal, Vieille Gridania), les sonnettes ne sont pas en double.

- [x] (0.1405) Carte de Tensho, à Eulmore : la sonnette d'appel n'apparaît qu'une fois, sur la carte où le jeu la marque (pas de deuxième sonnette sur l'autre carte d'Eulmore). Journal : « Map positions: 10 bell(s) and company chest(s) … longest step … ms ».

- [x] (0.1410) Rappels › Statut, après avoir ouvert la liste des servants à une sonnette d'appel (attendre 30 secondes) : le tableau « Vos servants » montre chaque servant avec sa classe et son niveau, ses gils, ses objets (au survol : piles, haute qualité, cristaux d'après la copie du jeu), ses ventes (orange si elles finissent dans la journée, rouge si finies), sa tâche et sa ville du marché ; le total des gils est juste. Avec un autre personnage, ses servants s'ajoutent après sa propre visite à la sonnette.

- [x] (0.1411) Alarmes : à la sonnette d'appel, liste des servants ouverte, un servant avec des objets en vente : une alarme automatique « Fin des ventes de <servant> » (groupe « Ventes des servants ») apparaît à l'heure de fin donnée par le jeu ; elle correspond à la fin indiquée dans le tableau Rappels › Statut. Si l'heure semble fausse (le jeu ne fait pas expirer les ventes), le noter.

- [x] (0.1415) Rappels › Statut, tableau des servants : la date de fin des ventes de chaque servant est le jour que la liste des servants du jeu écrit après « jusqu'au » (Addon 2314).

- [ ] (0.1416) Sonnette : mettre en vente puis retirer tous les objets d'un servant, rouvrir la liste des servants : l'alarme « fin des ventes » de ce servant disparaît de Rappels › Alarmes.

- [x] (0.1416) Sonnette : déposer des gils chez un servant ouvert : sa ligne du tableau des servants (Rappels › Statut) suit tout de suite, comme le journal des gils.

- [ ] (0.1418) Journal des gils (Rangements › Gils) : rendre une quête qui donne des gils : le gain est « Quêtes et mandats (récompenses) », pas « Autre ».

- [ ] (0.1418) Journal des gils : livrer une commande spéciale : le gain est « Commandes spéciales ».

- [ ] (0.1418) Journal des gils : finir un plateau du Dédale bestial qui donne des gils : le gain est « Dresseur (résultats des plateaux) ».

- [x] (0.1421) Rappels › État du personnage : le tableau des servants tient dans la fenêtre (colonne « Ville du marché » entière, Gils et Objets serrés sur leurs nombres) et les lignes d'aide se replient au bord.

- [x] (0.1434) Paramètres › Panneaux à côté du jeu › « Fenêtre de chat Tensho » : cocher « Afficher la fenêtre de chat Tensho ». La fenêtre s'ouvre à côté du chat du jeu, qui reste visible ; Dire, linkshell, système et emotes arrivent avec l'heure et leur couleur.

- [x] (0.1434) Onglets « Général » et « Social » : un message dit pendant que Général est affiché donne « Social (1) » ; afficher Social efface le compte. Un murmure envoyé ne compte pas.

- [ ] (0.1434) Remonter dans les messages : la fenêtre ne suit plus ; revenir en bas : elle suit de nouveau. Redimensionner la fenêtre : les lignes se recoupent.

- [ ] (0.1434) Réglages de la fenêtre : opacité, taille du texte, heure 12/24 h, secondes, nom court du canal ; ajouter, renommer ou retirer un onglet ; changer une couleur puis ↺.

- [x] (0.1434) La croix ferme la fenêtre et décoche le réglage ; /tensho chat l'affiche et la masque.

- [ ] (0.1434) Fenêtre de chat de Tensho, « Garder l'historique du chat sur ce PC » décoché (par défaut) : se déconnecter puis se reconnecter : la fenêtre repart vide.

- [ ] (0.1434) Fenêtre de chat Tensho ouverte : entrez en JcJ (par exemple au quai de Wolves' Den) : la fenêtre de chat disparaît, puis revient à la sortie. Masquez l'interface du jeu (raccourci du jeu) : elle se masque aussi.

- [x] (0.1434) Prise en main : relancer le guide (Aide, « Relancer le guide ») et vérifier : Guide : aucun (une fenêtre en plus, désactivée par défaut ; l'étape Panneaux du guide ne règle que les panneaux actifs par défaut). Aide : question de la FAQ « Qu'est-ce que la fenêtre de chat Tensho ? », qui ouvre Paramètres. Liste des outils de l'accueil : aucun (une fenêtre, pas une page ; ouverte depuis Paramètres › Panneaux à côté du jeu ou par /tensho chat).

- [x] (0.1441) Fenêtre de chat Tensho (Paramètres › Panneaux à côté du jeu, ou /tensho chat) : un lien de carte d'un message (par exemple un drapeau partagé en équipe ou en linkshell) apparaît souligné ; au survol, son lieu et ses coordonnées ; clic gauche : le drapeau du jeu est posé à cet endroit et la carte s'ouvre ; clic droit : menu « Tensho — lieu » (éthérite la plus proche avec son coût, carte Tensho, météo)

- [x] (0.1441) Fenêtre de chat Tensho : un lien d'objet d'un message (objet lié dans le chat, ou butin obtenu) apparaît souligné ; au survol, son nom ; clic gauche : la fiche de l'objet s'ouvre dans Tensho ; clic droit : menu d'objet de Tensho (pages, essayer, listes, copier le nom, sites)

- [ ] (0.1441) Fenêtre de chat Tensho : un message avec un lien au milieu d'une phrase se lit d'un seul tenant (texte avant et après le lien à leur place, la petite flèche du jeu absente) ; rien n'est envoyé dans le chat par un clic

- [x] (0.1442) Fenêtre de chat Tensho : un lien de quête d'un message (quête liée dans le chat) apparaît souligné ; au survol, son nom ; clic gauche : la page Quêtes de Tensho s'ouvre sur cette quête

- [x] (0.1442) Fenêtre de chat Tensho : clic droit sur le texte d'un message : menu « Copier ce message » et « Copier tous les messages de cet onglet » ; le texte collé ailleurs a l'heure, le canal, l'auteur et le message, un message par ligne

- [x] (0.1453) Fenêtre de chat de Tensho : une adresse web dans un message (https://… ou www.…, par exemple dite par vous en /dire) est soulignée ; au survol, l'adresse entière ; un clic ouvre un menu avec l'adresse : « Ouvrir dans le navigateur » ouvre la page dans le navigateur, « Copier l'adresse » la copie ; un point ou une virgule après l'adresse n'en fait pas partie.

- [x] (0.1454) Fenêtre de chat de Tensho : le bouton au bout des onglets (cadre) retire la barre de titre et le cadre, la fenêtre ne garde que les onglets et les messages ; le même bouton les remet ; le bouton du cadenas empêche de la déplacer et de la redimensionner, un second clic la libère ; « Opacité du fond » à 0 % rend le fond transparent ; après un rechargement, la fenêtre garde sa place, sa taille et ces deux réglages ; « /tensho chat » la masque et la réaffiche, aussi sans bordure.

- [x] (0.1456) Fenêtre de chat de Tensho : quelqu'un écrit (ou vous écrivez en Dire) « https://www.mogstation.com@example.org » : le texte n'est pas souligné, aucun menu ; une adresse normale (par exemple https://na.finalfantasyxiv.com) : l'info-bulle et le menu disent « Site : na.finalfantasyxiv.com » avant « Ouvrir dans le navigateur ».

- [ ] (0.1458) Fenêtre de chat de Tensho : les onglets sont Général, Combat, Événement et Social, comme les trois premiers onglets du chat du jeu ; en combat, vos dégâts et « Vous avez vaincu … » sont dans Combat et pas dans Général ; un dialogue de PNJ (une quête) est dans Événement ; vos onglets d'avant sont gardés, Combat et Événement ajoutés une seule fois (pas après un rechargement) ; comparer avec les onglets du chat du jeu.

- [x] (0.1459) Mode développeur, relevé pour X-433 : fermer le jeu et le relancer, se connecter, puis ne plus recharger Tensho pendant deux minutes (pas de version publiée pendant ce temps) ; jouer normalement ; dire « C'est fait » sur la page des essais : je compare dans le journal les à-coups de ce premier chargement (tas frais, « load 1 in this game process ») à ceux des chargements répétés.

- [ ] (0.1460) Mode développeur, fenêtre de chat de Tensho collée en bas, quand elle se remet à sauter (un message de deux lignes visible, ou en tirant le bord de la fenêtre) : dire l'heure ; le journal a une ligne « Chat window jumps » qui dit comment (mises en page, rangées, défilement).

- [x] (0.1462) Fenêtre de chat de Tensho collée en bas (onglet Combat, puis Général) : elle ne bouge plus quand rien n'arrive, avec des messages d'une et de plusieurs lignes, avec et sans cadre, puis après l'avoir agrandie et rétrécie ; on ne peut plus descendre sous la dernière ligne. En mode développeur, plus de ligne « Chat window jumps » dans dalamud.log. Les lignes en double de la capture du 15/11 : comparer au chat du jeu (deux messages du jeu, ou un message copié deux fois).

- [ ] (0.1463) Fenêtre de chat de Tensho, mode développeur : dire « test <item> test » (un objet lié) puis lier une quête et un lieu au milieu d'une phrase : chaque lien s'affiche à sa place, souligné, entre les deux « test » ; un clic l'ouvre dans Tensho ; la copie du message (clic droit) l'a aussi. Dans dalamud.log, la ligne « Chat window, pieces of a message with a link » dit ce que le jeu a envoyé (sortes et longueurs seulement).

- [x] (0.1464) Carte de Tensho à Eulmore : ouvrir Les Racines, Le Tronc et La Voûte l'une après l'autre, à côté de la carte du jeu : chaque couche ne montre que ses marchands, PNJ, sonnettes et sacs de vendeurs, comme le jeu (aucune icône d'une autre couche). Puis un donjon à étages (par exemple le Phare de Sirius) : chaque étage garde ses icônes.

- [ ] (0.1465) Dédale bestial : finir un plateau. Pendant que la fenêtre de résultat du jeu est ouverte, ouvrir Tensho › Dresseur › Équipe : « Dernier plateau » montre le même score que le jeu, et chaque bête son rang et son expérience avant → après, comme le jeu.

- [ ] (0.1466) Fenêtre de chat de Tensho : pendant un combat, l'onglet Combat n'affiche plus de compteur « (N) » et la barre d'onglets ne bouge plus ; un dialogue de PNJ n'est plus que dans l'onglet Événement (plus dans Général) ; en raid d'alliance, les messages de Général restent tous là après le combat.

- [x] (0.1467) Fenêtre de chat de Tensho : la molette fait défiler une ligne par cran ; remonter plus haut : le bouton « En bas » apparaît en bas à droite ; pendant que des messages arrivent, il devient « N nouveaux » ; un clic ramène au dernier message, et la fenêtre suit de nouveau les messages.

- [x] (0.1468) Mode développeur, fenêtre de chat Tensho ouverte : dire « test <item> test » (un objet lié) dans /dire : le lien s'affiche entre les deux « test », souligné ; dans dalamud.log, une ligne « Chat window, pieces of a message with a link (kind 10) » dit ce que le jeu a envoyé (et, si le lien s'affiche sans son nom, une ligne « a link without text of its own »).

- [x] (0.1469) Carte de Tensho, villes à étages : à Eulmore, chaque couche (Les Racines, Le Tronc, La Voûte) ne montre que ses marchands et sonnettes ; à Ul'dah (Faubourg de Thal, Faubourg de Nald) et à Limsa Lominsa (Le Tillac), un marchand de l'étage du haut s'affiche sur la carte de cet étage, à sa place ; aucun marchand connu ne manque.

- [x] (0.1470) Dédale bestial : finir un plateau ; Tensho › Dresseur › Équipe, résultat du dernier plateau : « score S · bonus B (n obtenu(s)) · total T » avec les mêmes nombres que la fenêtre de résultat du jeu (Score, Bonus, Total). En mode développeur, une capture de la fenêtre de résultat du jeu (lignes du score avec leurs points) sur la page des essais : elle dira quelle part du score est laquelle.

- [x] (0.1471) Fenêtre de chat Tensho : un message en lettres encadrées du jeu (annonce d'un joueur, « THE … ») se lit en lettres simples ; un joueur d'un autre monde s'affiche « Nom@Monde » comme dans le jeu ; un objet HQ lié a « HQ » après son nom ; la copie du message (clic droit) garde le même texte.

- [x] (0.1472) Fenêtre de chat Tensho : en lisant plus haut, cliquer « En bas » sur un lien n'ouvre pas le lien (seulement le retour en bas) ; un pavé tactile fait défiler doucement, une ligne par cran de molette ; avec un seul nouveau message le bouton dit « 1 nouveau ».

- [x] (0.1476) Fenêtre de chat de Tensho : laisser un onglet non lu dépasser 100 messages (par exemple Combat pendant un combat, en regardant Général) : son nom montre « (100+) » et sa largeur ne bouge plus ; le lire jusqu'en bas remet le compte à zéro.

- [x] (0.1476) Fenêtre de chat de Tensho : les deux icônes au bout des onglets (cadre et cadenas) sont centrées dans leur bouton, de la même hauteur que les onglets, avec et sans cadre, à 60 % et 200 % de taille du texte (réglage du chat), en français puis en anglais.

- [x] (0.1476) Paramètres › Fenêtre de chat › Onglets : donner une couleur au nom de deux onglets (une claire, une foncée) : les noms prennent leur couleur, restent lisibles sur l'onglet actif et inactif, en thème sombre puis clair ; « ↺ » rend la couleur normale ; la couleur reste après un rechargement.

- [x] (0.1477) Fenêtre de chat de Tensho, mode développeur : au chargement de Dalamud, le message « Developer Mode is enabled » montre « Learn more here » souligné ; un clic gauche dessus ouvre la même fenêtre de Dalamud que le même clic dans le chat du jeu ; l'info-bulle nomme Dalamud.

- [x] (0.1478) Fenêtre de chat de Tensho, thème clair puis sombre : un onglet avec une couleur de nom foncée (puis claire) ; cliquer dans le jeu (la fenêtre perd le focus), survoler l'onglet : le nom reste lisible sur l'onglet actif et inactif ; les onglets sans focus ont les couleurs du thème.

- [ ] (0.1478) Fenêtre de chat de Tensho, mode développeur : clic gauche sur « Learn more here » du message « Developer Mode is enabled » : la fenêtre de Dalamud s'ouvre comme depuis le chat du jeu ; clic droit dessus : le menu du message (copier).

- [x] (0.1479) Fenêtre de chat de Tensho ouverte, puis la fenêtre principale de Tensho par-dessus : la fenêtre principale reste devant, même après un clic dans le chat.

- [x] (0.1479) Réglage « Sous une fenêtre du jeu » : « Se cacher » : ouvrir l'inventaire puis la carte du jeu par-dessus le chat : il disparaît, puis revient seul à leur fermeture. « S'effacer » : le chat devient pâle sans fond et un clic dessus va à la fenêtre du jeu. « Rester » : rien ne change.

- [x] (0.1480) Fenêtre de chat de Tensho, réglage « Sous une fenêtre du jeu » sur « Se cacher », remplacement de la carte du jeu coupé : ouvrir la carte du jeu par-dessus le chat : il disparaît, puis revient à sa fermeture. Sur « S'effacer » : toute la fenêtre pâlit, barre de titre et cadre compris (cadre affiché).

- [x] (0.1481) Fenêtre de chat de Tensho à côté du chat du jeu : heure entre crochets [22:47], lettres avec un contour sombre, fond léger, marges serrées ; réglage « Onglets sous les messages » : les onglets passent dessous, la dernière ligne reste visible.

- [x] (0.1481) Au bout des onglets du chat de Tensho : « + » ajoute un onglet ; la roue dentée ouvre les réglages du chat ; changer l'opacité ou un canal là, puis ouvrir la page Paramètres de Tensho : le même changement y est.

- [x] (0.1482) Fenêtre de chat de Tensho, réglages d'un onglet : « Utiliser les filtres du jeu » : les groupes Discussion, Annonces et Combat (par sujet) apparaissent avec les noms du jeu ; « Comme Combat du jeu » sur un onglet, puis un combat : seules vos lignes et celles de l'équipe y sont, comme dans l'onglet Combat du jeu ; décocher « Dégâts infligés par vous » les retire.

- [x] (0.1482) Fenêtre de chat de Tensho : « Cacher avec » un mot, « Seulement avec » un autre ; « Joueurs cachés » avec le nom d'un joueur qui parle : ses messages disparaissent de Tensho (pas du jeu) ; « Joueurs mis en avant » : son nom ressort.

- [x] (0.1483) Remplacement de la carte du jeu actif, fenêtre de chat de Tensho ouverte en bas à gauche : ouvrir la carte (touche de la carte). La carte de Tensho s'ouvre ; la fenêtre de chat reste visible et ne pâlit pas. Puis réglage du remplacement coupé : la carte du jeu s'ouvre au-dessus du chat, et le chat se cache (ou pâlit).

- [x] (0.1484) Fenêtre de chat de Tensho avec deux onglets qui reçoivent des messages : remonter dans l'onglet A, puis ouvrir l'onglet B. B s'ouvre en bas (sans le bouton « retour en bas ») et ses messages sont marqués lus ; revenir à A : il est toujours remonté à l'endroit laissé.

- [x] (0.1485) Fenêtre de chat de Tensho en mode « S'effacer » derrière une fenêtre du jeu, contour du texte actif : ouvrir une fenêtre du jeu par-dessus le chat. Tout le chat pâlit, texte et contour noir compris (plus de lettres noires bien nettes sur un chat pâle).

- [x] (0.1486) Fenêtre de chat de Tensho : ouvrir la roue dentée, puis un clic droit sur une ligne. Le panneau des réglages et le menu ont des marges normales (pas serrées contre le bord) ; la fenêtre de chat garde ses marges étroites.

- [x] (0.1486) Paramètres › rechercher « joueurs » puis « mots » : les réglages de la fenêtre de chat (joueurs cachés, mis en avant, mots gardés ou cachés) sont trouvés.

- [x] (0.1487) Fenêtre de chat de Tensho, à la sonnerie d'une alarme de récolte avec un lieu (ou une alarme d'essai) : le message montre une flèche puis le nom de l'objet, comme le chat du jeu, sans le nom de la zone collé devant ; un clic sur la flèche ouvre la carte au lieu de l'alarme.

- [ ] (0.1488) Fenêtre de chat de Tensho à côté du chat du jeu : onglets en bas, gris et arrondis, l'onglet affiché marqué d'un point ; fond presque invisible, texte lisible par son contour ; barre de défilement fine ; heure écrite [9:21] ; un objet lié montre une flèche orange puis son nom, sans soulignement ; un long message continue en retrait sur la ligne suivante. Chaque point se remet comme avant par son réglage (roue dentée).

- [x] (0.1488) Clic sur la flèche orange d'un objet lié puis sur son nom : les deux ouvrent l'objet (clic droit : son menu).

- [ ] (0.1489) Fenêtre de chat de Tensho en mode « S'effacer » sous une fenêtre du jeu : le contour sombre des lettres pâlit autant que le texte (pas de halo noir autour de lettres pâles).

- [ ] (0.1489) Fenêtre de chat de Tensho : défiler l'onglet Combat vers le haut, retirer l'onglet Général (roue dentée › Onglets › Retirer) : l'onglet Combat garde sa position de défilement.

- [x] (0.1490) Fenêtre de chat de Tensho : le décor du jeu se voit à travers le fond, sur toute la fenêtre (messages et onglets) ; Opacité à 0 % : fond invisible, texte lisible par son contour ; à 100 % : fond plein.

- [x] (0.1491) Alarme de Tensho sur un objet récoltable au lieu connu : quand elle sonne, le chat du jeu montre la flèche, le nom du lieu et ses coordonnées (« Zone ( x , y ) »), puis la flèche et le nom de l'objet ; un clic sur le lieu place le drapeau et ouvre la carte.

- [x] (0.1491) Fenêtre de chat de Tensho, même alarme : le lieu avec sa flèche orange et ses coordonnées, puis l'objet avec sa propre flèche orange ; une adresse web dite dans le chat est soulignée (couleur d'accent), comme un lien nommé par Tensho.

- [x] (0.1492) Fenêtre de chat de Tensho › roue dentée : cocher « Garder l'historique du chat sur ce PC » ouvre une question (ce qui est gardé, le dossier, 2 000 messages et 7 jours, comment l'effacer) ; « Annuler » laisse la case décochée ; « Garder l'historique » la coche.

- [x] (0.1492) Historique activé : dire quelques messages, puis recharger Tensho ou relancer le jeu : les messages d'avant reviennent dans les onglets, avant ceux de la session, sans doublon (dalamud.log : « Chat history: N messages read back »).

- [ ] (0.1492) Historique activé, deux personnages : se connecter sur un autre personnage : ses onglets ne montrent pas les messages du premier ; revenir au premier : les siens reviennent.

- [ ] (0.1492) « Effacer l'historique » : une question demande confirmation ; « Effacer » vide la fenêtre et affiche « Historique effacé. » ; après un rechargement de Tensho, rien d'avant ne revient.

- [ ] (0.1492) Prise en main : relancer le guide (Aide, « Relancer le guide ») et vérifier : Aide : nouvelle question de la FAQ « Tensho garde-t-il mes messages du chat ? » (et la réponse sur la fenêtre de chat mise à jour) ; guide et liste des outils de l'accueil : aucun changement (un réglage d'une fenêtre existante, désactivé par défaut, expliqué par sa propre question d'accord).

- [ ] (0.1493) Fenêtre de chat, historique coché : faire sonner une alarme de Tensho (son message dans le chat porte un lien), puis recharger Tensho : relu depuis l'historique, ce lien est du texte simple (ni souligné ni cliquable) ; les liens de lieu et d'objet relus restent cliquables.

- [ ] (0.1493) Fenêtre de chat, historique coché : recharger Tensho, puis tout de suite (dans la seconde) Effacer l'historique et confirmer : la fenêtre reste vide, les anciens messages ne reviennent pas ; les nouveaux messages arrivent.

- [ ] (0.1493) Application de bureau, réglages de la fenêtre de chat : la case de l'historique, ses nombres et Effacer l'historique sont grisés, avec la phrase qui dit que l'historique se garde et s'efface dans le jeu seulement.

- [ ] (0.1494) Fenêtre de chat › roue dentée, historique coché : cochez « Sans limite » à côté de « Messages gardés », puis à côté de « Jours gardés » : chaque nombre se grise. Décochez : une question s'affiche d'abord (les messages en trop seront supprimés) ; après l'accord, le nombre d'avant revient. Une configuration déjà réglée garde ses jours (7 restent 7) ; une nouvelle a 30 par défaut.

- [ ] (0.1494) Fenêtre de chat, historique coché : sous les réglages, la place sur le disque pour ce personnage (en Ko ou Mo) ; elle grandit quand des messages arrivent, revient à 0 Ko après « Effacer l'historique ».

- [x] (0.1494) Fenêtre de chat, historique coché avec « Sans limite » : recharger Tensho : les messages reviennent vite et en entier dans la fenêtre (jusqu'à son nombre de lignes), sans saccade du jeu au chargement.

- [x] (0.1496) Paramètres, chaque onglet (Général, Panneaux à côté du jeu, Carte, Suivi et notifications, Données et sources, Avancé ; Débogage si le mode développeur est coché) : une liste de catégories à gauche, les réglages de la catégorie cliquée à droite ; changer d'onglet puis revenir (ou fermer et rouvrir la page) garde la catégorie choisie ; la fenêtre du jeu reste lisible à sa plus petite taille.

- [x] (0.1496) Paramètres, recherche : taper « police » puis « carte » : chaque résultat indique l'onglet et la catégorie (Onglet › Catégorie) ; choisir un résultat ouvre cet onglet et cette catégorie.

- [x] (0.1496) Un outil avec son bouton « Paramètres » (aide du journal d'artisanat, observation de la récolte, mini-jeux) : le bouton ouvre Paramètres > Panneaux à côté du jeu, sur la catégorie de l'outil ; la roue de la carte ouvre l'onglet Carte.

- [x] (0.1497) Paramètres, recherche : taper « liste des pages » et choisir « Liste des pages · Général › Langue et thème » : l'onglet Général et la catégorie Langue et thème s'ouvrent, la ligne trouvée est encadrée un instant et visible sans défiler de plus.

- [x] (0.1497) Paramètres, recherche : choisir un réglage tout en bas d'une longue catégorie (par exemple taper « servants » et choisir « Panneau à côté de la liste de ventes des servants », dans Données et sources › Sources communautaires) : le volet défile jusqu'à lui et le cadre s'estompe en deux secondes.

- [x] (0.1497) Paramètres, recherche : choisir un réglage de filtres d'un onglet du chat (taper « filtres du jeu » : « Utiliser les filtres du jeu », dans Panneaux à côté du jeu › Onglets du chat) ou la place d'une page de l'application de bureau : la section repliée autour s'ouvre, le volet défile et le réglage est encadré.

- [x] (0.1498) Paramètres, recherche : taper « filtres du jeu » et choisir « Utiliser les filtres du jeu » : seule la section de l'onglet du chat qui le contient s'ouvre ; les listes de canaux et de filtres des autres onglets restent repliées.

- [x] (0.1498) Paramètres : choisir une catégorie dans un onglet (par exemple Carte › Icônes), recharger le plugin (ou relancer le jeu) puis rouvrir les Paramètres : la même catégorie est choisie.

- [x] (0.1498) Paramètres › Panneaux à côté du jeu › Macros d'artisanat : la plage de macros et « Écraser » s'y trouvent bien, comme le disent les bulles d'aide du bouton « Copier dans les macros du jeu ».

- [x] (0.1499) Fenêtre de chat, avec un pavé tactile ou une molette fine : faire défiler un peu vers le haut puis donner un cran de souris vers le bas : les lignes descendent tout de suite d'une ligne.


- [x] (0.1500) Carte de Tensho, colonne d'outils : le texte des deux cases « 100% » (le pourcentage et le bouton dessous) est centré dans son cadre, à 100 % puis à 150 % de zoom, carte superposée puis en petite fenêtre, en français puis en anglais.

- [ ] (0.1501) Fenêtre de chat, onglet du réseau des novices : un message qui contient une icône de classe ou du réseau des novices dans son texte la montre « · » ; le nom de l'expéditeur reste sans « · » devant.

- [ ] (0.1503) En mode développeur, après quelques messages de joueurs avec une icône devant leur nom (mentor, débutant, classe, jeu de rôle) : dalamud.log a des lignes « Chat window, icons in a sender's name » avec des codes seulement ; la fenêtre de chat Tensho montre leur nom sans « · » devant pour un mentor ou une classe.

- [x] (0.1504) Chat Tensho ouvert, cocher « Cacher les messages du chat du jeu » : les messages du chat du jeu disparaissent, sa zone de saisie, son menu des canaux et ses onglets restent ; la fenêtre Tensho est juste au-dessus de la saisie du jeu, sans la couvrir ; taper un message dans la saisie du jeu (Dire, Équipe, /tell) : il part comme d'habitude et s'affiche dans la fenêtre Tensho

- [x] (0.1504) Option cochée : changer d'onglet du jeu, recevoir des messages, changer de zone, voir une scène cinématique : les messages du jeu restent cachés ; redimensionner ou déplacer le chat du jeu (son bouton de taille, le mode HUD) : la fenêtre Tensho suit

- [ ] (0.1504) Décocher l'option, puis la recocher et fermer la fenêtre Tensho, puis la recocher et décharger Tensho : à chaque fois les messages du jeu reviennent tout de suite, la fenêtre Tensho retourne à sa place d'avant

- [ ] (0.1504) Option cochée, bouton aux deux fenêtres au bout des onglets Tensho : les deux chats sont affichés, la fenêtre Tensho à sa place ; le bouton de réduction la ramène à ses onglets et la rouvre ; recliquer le bouton aux deux fenêtres : les messages du jeu sont de nouveau cachés

- [ ] (0.1504) Option cochée, passer en JcJ (ou en contenu classé) : les messages du jeu reviennent pendant la coupure de Tensho et sont recachés à la sortie

- [ ] (0.1506) Option « Cacher les messages du chat du jeu » cochée : voir une scène cinématique, puis entrer en GPose et en sortir : les messages du jeu ne réapparaissent jamais pendant la scène ni en GPose ; après, la fenêtre Tensho revient à leur place et ils restent cachés.


- [x] (0.1507) Option « Cacher les messages du chat du jeu » cochée, « Sous une fenêtre du jeu » sur « Se cacher » : ouvrir une fenêtre du jeu par-dessus la fenêtre Tensho (inventaire, carte) : la fenêtre Tensho part et les messages du jeu reviennent sous la fenêtre du jeu ; la fermer : la fenêtre Tensho revient à leur place et ils se cachent.

- [x] (0.1507) Option cochée, un onglet du chat du jeu détaché ailleurs sur l'écran : cet onglet détaché reste affiché ; seuls les messages sous la fenêtre Tensho sont cachés.

- [ ] (0.1507) Option décochée, réduire la fenêtre Tensho à ses onglets, puis cocher l'option dans les Paramètres : la fenêtre prend la place des messages du jeu en entier ; entrer en combat avec « Masquer en combat » puis en sortir : elle revient entière, pas réduite.

- [x] (0.1508) Reprend T-0.1504-1. Chat Tensho ouvert, « Cacher les messages du chat du jeu » cochée : les messages et la barre d'onglets du chat du jeu sont cachés ; la fenêtre Tensho occupe leur place, juste au-dessus de la ligne de saisie du jeu, sans couvrir le nom du canal (« Dire ») ni la saisie ; taper un message dans la saisie du jeu (Dire, Équipe, /tell) : il part comme d'habitude et s'affiche dans la fenêtre Tensho.

- [x] (0.1508) Option cochée : cliquer sur le nom du canal (« Dire ») à gauche de la saisie du jeu : le menu des canaux s'ouvre, la fenêtre Tensho s'efface, le menu est entièrement lisible et cliquable ; choisir un canal : le menu se ferme, la fenêtre Tensho revient. Mode développeur : dalamud.log dit « Game chat: channel menu open (drop-down owner …, list …) » puis « closed ».

- [x] (0.1508) Option cochée puis décochée (ou fermer la fenêtre Tensho, ou « les deux chats ») : la barre d'onglets du chat du jeu revient telle qu'avant, onglets cliquables ; recharger Tensho option cochée : elle revient aussi.

- [x] (0.1508) Si vos onglets du chat du jeu sont en haut (réglage du jeu) : option cochée, la fenêtre Tensho couvre aussi la ligne des onglets du haut, jusqu'à la saisie.

- [x] (0.1510) Option « Cacher les messages du chat du jeu » cochée, « Sous une fenêtre du jeu » sur « Se cacher » : ouvrir puis fermer une fenêtre du jeu par-dessus la fenêtre Tensho (inventaire) : à la fermeture, la fenêtre Tensho revient directement à la place des messages du jeu, sans apparaître un instant ailleurs ; pareil à la fin d'un combat avec « Masquer en combat ».

- [ ] (0.1511) Écran titre puis sélection du personnage, avec Tensho chargé et des fenêtres de Tensho laissées ouvertes à la dernière session (par exemple la fenêtre de chat Tensho) : aucune fenêtre ni panneau de Tensho n'apparaît ; après la connexion du personnage, les mêmes fenêtres reviennent ; Échap › Retour à l'écran titre : elles disparaissent, puis reviennent à la connexion suivante.

- [x] (0.1512) Option « Cacher les messages du chat du jeu » décochée (son défaut), mode développeur : en ouvrant et fermant des fenêtres du jeu et en déplaçant le chat du jeu, le journal de Dalamud n'écrit plus de ligne « Game chat: Tensho's place » ; option cochée : la fenêtre de chat Tensho se pose toujours sur les messages du jeu et y revient après une fenêtre du jeu posée dessus ou un combat.

- [ ] (0.1517) Réglages du chat (roue de la fenêtre de chat Tensho), onglet d'un chat, liste des canaux : survolez cinq cases de canaux différents (par exemple Dire, Crier, Linkshell inter-Monde [3], Réseau des novices, Écho) : chaque infobulle commence par le nom entier du canal, puis explique ce canal (sa commande du jeu, qui le voit), et elles sont toutes différentes

- [ ] (0.1517) Même onglet, « Utiliser les filtres du jeu » coché : ouvrez le groupe Discussion et survolez deux filtres (par exemple Dire et Emotes personnalisées) : l'infobulle donne le nom du filtre, l'exemple de ligne du jeu et les onglets du jeu qui le montrent par défaut

- [ ] (0.1518) Réglages du chat (roue de la fenêtre de chat Tensho), un onglet avec « Utiliser les filtres du jeu » : ouvrez Combat puis « Par genre de ligne » ; décochez « Dégâts subis » : son compte passe à 0/11 ; ouvrez « Par sujet (qui agit) » puis « Vous » : « Dégâts subis par vous » est décoché aussi ; recochez « Dégâts subis » : les onze reviennent

- [ ] (0.1518) Même onglet, en combat contre un monstre : avec « Dégâts infligés » coché et « Dégâts subis » décoché, l'onglet montre vos coups mais plus les coups que vous recevez

- [ ] (0.1519) Réglages du chat, un onglet qui montre Hurler ou Crier : cochez « Replier les messages répétés » ; dans une cité où quelqu'un répète la même annonce, la fenêtre de chat Tensho ne la montre qu'une fois, avec « ×2 », « ×3 »… à sa fin ; survolez « ×3 » : l'infobulle dit combien de fois il est venu

- [x] (0.1519) Réglages du chat, Joueurs : écrivez le nom d'un joueur qui parle près de vous dans « Joueurs mis en avant » : chacun de ses messages est posé sur une bande colorée, son nom dans la couleur d'avertissement, et le texte reste lisible

- [ ] (0.1520) Réglages du chat, Joueurs : écrivez « raid, vente » dans « Mots d'alerte » ; quand un message de Hurler ou Crier contient l'un de ces mots, il est posé sur une bande colorée ; si son onglet n'est pas celui affiché, le nom de l'onglet porte « ! » devant, qui disparaît quand vous ouvrez l'onglet et descendez en bas

- [ ] (0.1521) Réglages du chat, Joueurs : mettez un mot dans « Mots d'alerte », puis écrivez-le vous-même en /dire ou /équipe : votre message n'est pas posé sur la bande et l'onglet ne porte pas « ! » ; le même mot écrit par un autre joueur alerte toujours

- [ ] (0.1521) Avec un mot d'alerte qui apparaît dans vos lignes de combat (par exemple le nom d'un ennemi) : l'onglet qui montre le combat ne porte jamais « ! » et ces lignes ne sont pas surlignées

- [ ] (0.1522) Au Dédale bestial, 1er plateau, devant « Formation de l'équipe » : le panneau Tensho montre le nom du 1er plateau suivi de « (lu dans le jeu) » et « Les 10 à emmener ». Il ne dit pas « Rang pas encore lu » pour des bêtes dont le jeu montre le rang.

- [ ] (0.1522) Avant d'entrer, sur un autre plateau (2e ou plus), ouvrez sa disposition : le panneau Tensho prend ce plateau seul (nom et « (lu dans le jeu) ») ; s'il garde la liste, notez-le (le journal dit « Beastmaster board read in the game's windows: content … »)

- [x] (0.1522) Page Dresseur, onglet Équipe, après un plateau : sous « Dernier plateau », plus de « Rangs pas encore lus » contradictoire avec les rangs montrés

- [x] (0.1523) Au Dédale bestial, 1er plateau, devant la disposition du plateau : le panneau Tensho appelle Piège-volant (Glace) au combat 4 (Sarcopte, Glace) ; sous le combat 2 (Archidémon, Foudre) une ligne dit qu'aucune bête de l'équipe ne touche Foudre et nomme celles qui la toucheraient

- [x] (0.1523) Page Dresseur, onglet Bêtes : la colonne de l'attaque automatique montre l'élément des bêtes magiques (Feu, Glace…) comme le bestiaire du jeu, « Magique » seulement pour une attaque neutre

- [ ] (0.1525) Après un plateau du Dédale bestial, page Dresseur, onglet Équipe, « Dernier plateau » ouvert : les lignes (chemin parcouru, affrontements, cibles d'élite, gardien du plateau, PV restants, score, bonus, total) ont les mêmes nombres que la fenêtre de résultat du jeu



- [ ] (0.1527) Au Dédale bestial, avec une bête de rang supérieur à la limite du 1er plateau (rang 7 par exemple) : ouvrez « Formation de l'équipe » du 1er plateau ; la page Dresseur garde le rang 7 de cette bête (pas 5), même en rouvrant la fenêtre plusieurs fois

- [ ] (0.1527) Devant « Formation de l'équipe », hors du Dédale : le nom du plateau dans le panneau Tensho est suivi de « (lu dans la fenêtre du jeu ; son numéro reste à confirmer en jeu) » ; dans dalamud.log, la ligne « Beastmaster board read in the game's windows: content N » donne le numéro à relever

- [x] (0.1528) Fenêtre de chat Tensho : un onglet qui ne montre que le combat (par exemple « Combat ») ne porte jamais de nombre de non lus pendant un combat, et « nouveaux messages plus bas » ne compte pas les lignes de combat

- [ ] (0.1528) Réglages du chat › Onglets › canaux : réduisez la fenêtre petit à petit ; les cases passent de trois colonnes à deux puis à une, aucun nom n'est coupé (« Linkshell inter-Monde [3] », « Messages d'erreur système », « Objets vendus aux marchés » en entier) et rien ne passe sous la barre de défilement ; même chose pour les filtres du jeu et les genres de lignes de combat

- [x] (0.1529) Au Dédale bestial, devant « Formation de l'équipe » d'une étape : le panneau Tensho montre d'abord le ou les combats de l'étape (« Combat N · montré dans la disposition du plateau du jeu ») avec leurs ennemis, leurs actions et les trois bêtes avec la raison ; « Tout le plateau » est replié en dessous

- [ ] (0.1529) Passez à l'étape suivante du plateau : le bloc du combat en tête change avec l'étape ; dans dalamud.log, la ligne « Beastmaster board window entries: … fights … » change aussi (à relever : une étape ou tout le plateau)

- [x] (0.1529) Si la disposition du plateau liste tous les combats à la fois, le panneau reste comme avant (tout le plateau), sans bloc de combat en tête

- [ ] (0.1530) Au Dédale bestial, devant « Formation de l'équipe » : sous chaque bête conseillée du combat en tête, une ligne « Technique de type féroce (Griffure) ; combo instinctif II : Hache rafale puis la technique (Assaut vital), ou la technique puis Hache mistral (Assaut létal) » pour le cu sith ; faites le combo en combat et vérifiez que le jeu l'annonce

- [x] (0.1530) En mode développeur, au Dédale bestial, appelez une bête : dalamud.log a une ligne de la sonde pour la fenêtre « XBMActivePet » (à relever : le nom de la bête appelée y est-il ?)

- [ ] (0.1531) Au Dédale bestial, après un plateau : dans dalamud.log, la ligne « Beastmaster board result, numbers 47 to 62 » donne des nombres à comparer aux bonus que montre la fenêtre de résultat (lignes de XBMScoreBonus ?)

- [x] (0.1532) Rechargez Tensho avec un long historique de chat (fenêtre de chat ouverte) : pas d'à-coup ; dans dalamud.log (mode développeur), plus de « Slow frame » où « Chat » dépasse quelques ms, et « Chat layout » reste vers 3 ms

- [x] (0.1532) Remontez loin dans l'historique avec la molette : les anciennes lignes apparaissent sans que la vue saute, dans l'ordre

- [x] (0.1533) Rechargez Tensho avec la fenêtre de chat ouverte : « Chargement… » une ou deux secondes, puis les lignes ; dans dalamud.log (mode développeur), plus de « Slow frame » où « Chat » dépasse 16 ms juste après le rechargement

- [ ] (0.1534) Au Dédale bestial, devant « Formation de l'équipe » : le panneau Tensho n'est pas plus large que la fenêtre du jeu à côté, l'équipe conseillée est un tableau (icône et nom, rang, attaque) comme dans le jeu, aucun texte coupé

- [x] (0.1535) Fenêtre de chat avec un long historique : remonter à la molette, puis en tirant la barre ; la vue ne saute pas quand des lignes plus anciennes arrivent au-dessus (elles arrivent par paquets)

- [x] (0.1535) Après un rechargement en mode développeur : dalamud.log a la ligne « JIT warm-up: … Early phase (map, overlays, chat; …) ready N ms after the load » (logread.py)

- [ ] (0.1543) Dédale bestial, 1er plateau, équipe vide (Formation de l'équipe à 0/10) : le panneau propose 10 bêtes qui couvrent Foudre, Feu, Eau, Vent, Glace, Contondant, Perforant et Tranchant ; chaque combat dit « faiblesse touchée » pour tous ses ennemis, sans ligne orange

- [ ] (0.1543) Dédale bestial, 1er plateau, avec l'équipe choisie de la capture du 17/11 : au gardien Écureuil est appelé (3 sur 3), au combat 4 Piège-volant ou Uragnite compté (2 sur 2), au combat 5 Salamandre appelée ; pas de ligne orange pour une faiblesse qu'une bête de l'équipe touche

- [ ] (0.1544) Dédale bestial, 1er plateau : sous chaque combat, « Ordre d'appel, la meilleure d'abord : » donne trois bêtes numérotées, chacune avec sa raison. La première vise la faiblesse du monstre marqué « (le plus fort) » : au Gardien du plateau une bête Perforant, au combat 5 une bête Eau.



- [ ] (0.1546) Dédale bestial, 2e plateau, une bête de rang 5 ou moins dans l'équipe : le panneau dit « rang bas pour ce plateau » (6 à 10) à côté d'elle ; une bête de rang 6 à 10 ne le dit plus

- [ ] (0.1546) si vous atteignez le 3e plateau du Dédale bestial : la fenêtre d'avant l'entrée dit « Rang de bête recommandé : 11 à 15 », comme Tensho

- [ ] (0.1547) Dédale bestial, 1er plateau, Gardien du plateau : l'ordre d'appel met d'abord une bête Perforant (Cu sith, Pampa ou Acarus) qui vise « Pas de Seul (le plus fort) », puis Écureuil pour les deux Succubes


- [ ] (0.1548) Dresseur : ouvrir le bestiaire de dresseur du jeu et cliquer sur quelques bêtes (les deux pages) : la page Dresseur de Tensho donne à chacune le rang entre parenthèses de sa fiche (« Rang 5 (8) » : 8)

- [ ] (0.1548) Dédale bestial : après avoir ouvert les fiches des bêtes de l'équipe dans le bestiaire, le panneau ne dit plus « rang pas encore lu » pour elles

- [ ] (0.1549) Dédale bestial, panneau du plateau (ou page Dresseur, Équipe) : « Les 10 à emmener » sont rangées par numéro du bestiaire, « N° 1 » devant Cu sith, et chaque numéro est celui de la bête dans le bestiaire du jeu ; les trois de chaque combat restent dans l'ordre d'appel

- [x] (0.1550) Page Dresseur, onglet Équipe : l'option « Surligner les combos de la bête » est là, coupée par défaut

- [x] (0.1550) Dédale bestial, option activée, en combat : quand une bête est appelée, les deux techniques du dresseur de son combo (celle d'avant sa technique et celle d'après) sont surlignées sur la barre d'actions ; elles changent quand une autre bête est appelée

- [x] (0.1558) Dédale bestial, page Dresseur › Équipe, sur un plateau où l'équipe a moins de places que de faiblesses : la faiblesse de l'ennemi le plus fort de chaque combat (le gardien d'abord) est dans les 10 ; si une faiblesse est laissée, c'est celle de monstres plus faibles.

- [x] (0.1558) Dédale bestial, option « surligner les combos » cochée, une bête appelée : sur vos barres d'actions, la technique à faire avant la technique de la bête porte « 1 », celle d'après porte « 2 ».

- [x] (0.1558) Page Dresseur, plateau 3 ou plus, une bête de rang bas : le texte dit « recommandé : … à …, à confirmer en jeu » ; aux plateaux 1 et 2, sans « à confirmer ».

- [x] (0.1559) Dédale bestial, page Dresseur › Plateaux, 1er plateau : sous un ennemi, une action dit « inflige : … » (par exemple Poison ou Brûlure du vent) quand elle pose un effet ; une action sans effet n'a rien de plus.

- [x] (0.1562) Dédale bestial, onglet des plateaux ou panneau du combat : une action ennemie qui donne un bonus ou une barrière (Bonus de dégâts, Couvert, Pointes de glace) dit « confère : … », une qui donne Poison ou Glas dit « inflige : … ».

- [x] (0.1565) Réglages › Panneaux à côté › Onglets du chat, fenêtre réduite vers 760 pixels de large : la ligne de chaque onglet (nom, messages non lus, couleur, retirer) va à la ligne, rien n'est coupé ; les cases des canaux tiennent dans la fenêtre, une ou plusieurs colonnes selon la largeur, chaque linkshell avec son numéro

- [x] (0.1566) Réglages › Panneaux à côté › Joueurs du chat : « Joueurs cachés » montre une ligne par joueur ; taper deux noms séparés par une virgule puis Entrée ajoute deux lignes ; le bouton × d'une ligne la retire ; un nom déjà là n'est pas ajouté deux fois ; les noms enregistrés avant (avec des virgules) apparaissent chacun sur sa ligne

- [x] (0.1566) Fenêtre de chat Tensho : clic droit sur le message d'un joueur : « Cacher <nom> » fait disparaître ses messages de tous les onglets et l'ajoute aux joueurs cachés ; « Mettre <nom> en avant » colore son nom, puis le même menu propose « Ne plus mettre <nom> en avant »

- [x] (0.1567) Objets › un objet fabriqué (par exemple un lingot) › onglet des recettes › ligne « Outils liés » : « Recettes, simulation et macros d'artisanat » ouvre la page de simulation d'artisanat sur la recette de cet objet ; pour un objet sans recette (un minerai brut), le bouton n'est pas dans la ligne

- [x] (0.1568) Objets › Fibre alchimique › « Comment l'obtenir » : une ligne « Explorations en aéronef » (deux secteurs) et une ligne « Explorations en sous-marin » (secteurs avec leur mer), la source en gris dessous, et plus « Aucune source connue » ; un objet sans aucune source garde « Aucune source connue »

- [ ] (0.1571) Chat, Paramètres › Onglets du chat : ouvrir deux onglets, taper un mot dans « Seulement avec » du premier sans l'ajouter : la case du second reste vide ; taper un mot puis Entrée : il s'ajoute et le curseur reste dans la case pour en taper un autre.

- [ ] (0.1571) Fenêtre de chat : clic droit sur le message d'un joueur mis en avant, « Ne plus mettre … en avant » : il n'est plus mis en avant, même si son nom avait été écrit avec son monde (« Nom@Monde ») ; dans les réglages d'un onglet, cochez « Respecter les majuscules » et ajoutez « Foo » à côté de « foo » : les deux lignes restent.

- [ ] (0.1572) Jardin, au potager de la maison : ouvrir un emplacement dont le semis n'est pas encore mûr après l'heure prévue par Tensho (ou attendre ce cas) : le jeu écrit « Le semis de … se porte bien. » ; la page Jardin montre « Inconnu » pour cet emplacement (pas « Mûr »), avec au survol l'heure où le jeu l'a dit ; les autres emplacements gardent leur temps restant.

- [ ] (0.1572) Jardin : ouvrir un emplacement mûr (le jeu écrit « … est arrivé(e) à maturité ! ») : la page montre « Mûr » pour cet emplacement, même avant l'heure prévue ; récolter : il est retiré du journal.

- [ ] (0.1573) Artisanat, puis Récolte : chaque page montre son titre, une description et une barre d'onglets ; cliquez chaque onglet : il ouvre sa sous-page ; ouvrez une sous-page par un autre chemin (bouton « Séquences sauvegardées » du simulateur, recherche rapide, liste des outils) : le bon onglet est choisi, sans revenir en arrière.

- [ ] (0.1573) Artisanat : « Retour à l'objet », au bout de la barre d'onglets, ouvre la fiche de l'objet de la recette simulée, onglet Artisanat, depuis les trois sous-pages.

- [ ] (0.1574) Dédale bestial, fenêtre « Disposition du plateau » ouverte, panneau Tensho à côté : chaque ennemi du combat montré a son icône et son nom, puis « Faiblesse » suivie de la même icône d'élément que dans la fenêtre du jeu et du mot (Feu, Perforant…) ; ses actions commencent par l'icône de leur type de dégâts ; un trait sépare les ennemis ; le panneau n'est ni plus large ni plus haut que la fenêtre du jeu et aucun texte n'est coupé.

- [ ] (0.1574) Dédale bestial, fenêtre « Formation de l'équipe » : dans le panneau, sous « Tout le plateau », chaque bête à emmener est sur une ligne (icône, numéro et nom, rang, type de dégâts précédé de l'icône du jeu) ; une bête à l'attaque magique neutre montre « Magique » sans icône.

- [ ] (0.1576) Jardin : ouvrez un emplacement dont la récolte prévue est passée et que le jeu dit encore en pousse ; sur la page Jardin, au survol de « Inconnu », la phrase même du jeu entre guillemets avec son heure.

- [ ] (0.1580) Fenêtre de chat Tensho, onglet qui replie les répétitions avec un mot d'alerte : regarder l'onglet, passer à un autre, puis faire redire le même message contenant le mot dans la minute : l'onglet reprend « ! » et son compte de non-lus.

- [ ] (0.1580) Fenêtre de chat Tensho : appuyer sur un lien de carte ou d'objet et faire glisser pour déplacer la fenêtre : la fenêtre bouge, aucun drapeau posé ni objet ouvert ; un simple clic sur le lien agit toujours.

- [ ] (0.1580) Fenêtre de chat Tensho : survoler un lien d'objet de haute qualité : la bulle dit « (HQ) » après le nom.

- [ ] (0.1583) Carte › Repères › Repères favoris : collez trois lignes dans le champ d'import, « Western Thanalan ( 12.3 , 7.4 ) », « Thanalan central ( 20.5 , 25.3 ) » et « Nulle part ( 10.0 , 10.0 ) », puis « Importer » : deux favoris ajoutés (le premier nommé « Thanalan occidental »), une ligne au lieu inconnu.

- [ ] (0.1584) Dédale bestial, un plateau entier en survolant objets, actions et monstres : dalamud.log n'a que quelques lignes « Labyrinth reading » (chargement, arène, plateau, combat), aucune à chaque infobulle (lecture par l'agent avec logread.py).

- [ ] (0.1585) Fenêtre de chat de Tensho, onglet qui replie les répétitions : un message long qui remplit la largeur, répété trois fois en moins d'une minute : « ×3 » reste entier dans la fenêtre (le message passe à la ligne plus tôt), et le message se remet en page quand le compte passe de ×2 à ×3.

- [ ] (0.1586) Paramètres › Panneaux › Historique du chat, historique gardé, « Sans limite » coché sur les messages : décochez-le : une question dit que les messages au-delà seront supprimés ; « Annuler » laisse la case cochée, « Limiter » la décoche. Même chose pour les jours.

- [ ] (0.1586) Avec « Sans limite » coché sur les messages et sur les jours, décochez puis recochez « Garder l'historique » : la question d'accord dit « rien n'est retiré » ; avec une limite, elle dit « les plus anciens sont retirés ».

- [ ] (0.1588) Page Carte › onglet Repères › « Repères favoris » : collez une liste de lieux avec leurs coordonnées (« Lieu ( x , y ) ») et cliquez tout de suite « Importer » : le jeu ne fait aucun à-coup ; « Lecture des noms de zones… » peut s'afficher un instant, puis les favoris sont ajoutés.

- [ ] (0.1589) Carte › Favoris : collez une liste « Lieu ( x , y ) », cliquez « Importer » puis changez aussitôt le texte ou passez à un autre onglet : rien n'est importé, « Lecture des noms de zones… » disparaît ; sans rien toucher, les favoris sont ajoutés.

- [ ] (0.1590) Île › Atelier, première ouverture après le chargement du plugin : aucun à-coup du jeu ; « Lecture des données des fabriques… » peut s'afficher un instant, puis la page de l'atelier.

- [ ] (0.1598) Rechargez Tensho en cours de jeu (mode développeur, ou une version publiée) avec un panneau libre ouvert : le panneau revient en 6 s au plus, sans à-coup visible ; dans dalamud.log, la ligne « Panels opened … ms after the load » dit « their code compiled » (mesuré au rechargement automatique de 0.1598 : ouvert à 5,5 s, première image 103 ms, contre 248 à 1244 ms à l'ancienne borne de 3 s)

- [x] (0.1599) Dresseur, option « Surligner les combos de la bête » cochée (page Dresseur) : au Dédale bestial, appelez une bête et entrez en combat : « 1 » et « 2 » apparaissent sur les deux techniques du dresseur (ex. Opo-opo ou cu sith : 1 sur Hache rafale, 2 sur Hache mistral) ; dans dalamud.log, les lignes « Beastmaster beast called: beast n (Familiar) … » et « Beast combo highlight: … 1 on _ActionBar… slot …, 2 on … » ; si rien n'apparaît, la ligne « Beast combo highlight: nothing drawn: … » dit pourquoi

- [x] (0.1599) Hors du Dédale (zone ouverte, en JcE), bête appelée, même option : « 1 » et « 2 » sur les mêmes techniques ; en changeant de bête par une autre Corne d'appel, les chiffres suivent la nouvelle bête

- [x] (0.1601) Mineur ou botaniste, un objet de collection de livraison spéciale (ex. Charbon, Carrière de pierre niv. 85) : le panneau Collectionnable de Tensho montre les seuils 240 / 450 / 600 et un plan, au lieu de « Aucun seuil de remise connu » ; le tableau des objets de collection (page Récolte) liste Charbon avec « livraison spéciale »

- [ ] (0.1602) Fenêtre de chat de Tensho, clic droit sur le nom d'un joueur près de vous (Dire) : « Voir la carte de personnage » ouvre sa carte ; « Cibler » le prend pour cible.

- [ ] (0.1602) Fenêtre de chat de Tensho, clic droit sur le message d'un joueur proche de vous : « Menu du jeu… » ouvre le menu du jeu de ce personnage (requête d'ami, liste noire…) et « Cibler » le cible. Même clic sur un joueur loin : ces deux entrées sont absentes ou grisées.

- [x] (0.1602) Sur un message repris de l'historique (après un redémarrage, historique activé) : les entrées qui demandent l'identifiant du joueur (Murmurer, carte, liste muette) sont grisées, sans erreur

- [x] (0.1603) Mode développeur, fenêtre de chat de Tensho ouverte : un joueur vous dit quelque chose (dire ou équipe), puis clic droit sur son message : Murmurer et Voir la carte de personnage ne sont pas grisés et agissent sur ce joueur ; dalamud.log porte « Chat window, a sender's identifiers: Given (kind …) » (si c'est « LineUnread » ou « Kind » à la place, les entrées restent grisées : le dire)

- [ ] (0.1603) Envoyez un murmure à un joueur, puis clic droit sur votre ligne « >> » : Murmurer vise ce joueur (jamais vous-même) ou reste grisé ; journal : « Given » ou « Own », jamais d'erreur


- [x] (0.1604) Même page : tous les montants s'écrivent de la même façon (espaces entre les milliers en français, aussi au net en tête et dans « Derniers changements ») ; les titres Gains, Dépenses, Net, Variation et Solde sont à droite, au-dessus de leurs montants

- [ ] (0.1605) Récolte › tableau des objets de collection, recherche « Charbon » : une seule ligne (livraison spéciale, Anden, 240/450/600)

- [ ] (0.1606) Stockage › Gils : vendre un objet par un servant, recharger le plugin (ou relancer le jeu) sans toucher aux gils du personnage, ouvrir le servant puis retirer : la vente sous « Marché », le retrait en transfert des deux côtés

- [x] (0.1606) Fenêtre de chat de Tensho : clic droit sur le nom d'un joueur qui vient de parler : les entrées du jeu (murmurer, inviter, carte de personnage) sont actives ; au journal, « Chat window, a sender's identifiers: Given » une fois

- [x] (0.1608) Alarmes : six onglets (Horloges, Alarmes par job, Autres alarmes, Alarme personnelle, Alarmes du jeu, Réglages) ; le nombre d'alarmes dans les deux onglets d'alarmes égale celui du titre de la page ; activer une alarme, changer son avance, la supprimer : gardé après un rechargement

- [x] (0.1609) Jardin : trois onglets (Jardin, Croisements, Alarmes) ; Croisements : choisir une graine, « donne » puis « utilise », une paire et son plan de plantation ; Jardin : les prévisions sur les carrés toujours là

- [ ] (0.1610) Dédale bestial : finir un plateau, la fenêtre de résultat ouverte jusqu'au bout : la page montre le bonus et le total de la fenêtre ; au journal, « Beastmaster board result read » (une ou deux lignes) sans avertissement

- [x] (0.1616) Fenêtre de chat ouverte, recharger le plugin (ou relancer le jeu) : les lignes du chat s'affichent en une à deux secondes, sans « Chargement… » qui dure ; au journal, « Chat part (X-564): … ms after the load » sous 3000 et pas de « Slow frame » du chat juste après

- [x] (0.1617) Fenêtre de chat au premier plan : survoler un message puis Ctrl+C, coller ailleurs : le message ; Ctrl+A montre une bande « Tout l'onglet est choisi », Ctrl+C copie tout l'onglet, Échap la retire

- [x] (0.1617) Clic droit sur le message d'un joueur d'un autre monde : « Copier le nom … » colle « Nom@Monde » ; d'un joueur de votre monde : « Nom@Monde » aussi

- [ ] (0.1617) Clic droit sur un lien de lieu : « Copier le lieu et ses coordonnées » ; sur un lien d'objet : « Copier le nom de l'objet » ; sur un lien de quête : un menu « Copier le nom de la quête »

- [x] (0.1617) Clic droit sur un onglet, « Vider cet onglet » : il se vide, un autre onglet garde les mêmes messages, un nouveau message s'y montre ; après un rechargement il reste vidé ; « Remontrer les messages vidés » les rend

- [x] (0.1618) Roue de la fenêtre de chat, « Masquer les noms des onglets » ou l'œil au bout de la barre : les noms disparaissent, l'onglet affiché reste, l'œil les remontre

- [ ] (0.1618) Fenêtre de chat de Tensho, roue des réglages : changer « Marges en largeur », « Marges en hauteur », « Coins arrondis » et « Épaisseur du cadre » : la fenêtre change aussitôt ; remises par défaut (↺), elle redevient comme avant.

- [x] (0.1618) Opacité loin de la souris à 40 % : la fenêtre (texte compris) pâlit quand la souris s'en va, redevient pleine dessous

- [ ] (0.1618) Fenêtre de chat de Tensho, libre (pas collée sur les messages du jeu), roue dentée › Place et taille : taper X, Y, Largeur, Hauteur puis « Appliquer » : la fenêtre va à cette place. Taper un X qui la sort de l'écran : l'avertissement « Une partie de la fenêtre sort de l'écran » s'affiche, et « Ramener au centre » la remet au milieu.

- [x] (0.1619) Fenêtre de chat sur la boîte des messages du jeu (ou réduite) : roue, taper une place puis « Appliquer » : l'avertissement « La fenêtre suit les messages du jeu… » s'affiche et la fenêtre ne saute pas plus tard quand elle redevient libre

- [x] (0.1620) Roue de la fenêtre de chat : « Verrouiller la fenêtre » et « Clics à travers la fenêtre verrouillée » : un clic sur les lignes du chat agit dans le jeu derrière (choisir une cible, tourner la caméra, molette pour le zoom) ; les onglets, leurs boutons, un lien et « En bas » répondent toujours ; le cadenas déverrouille

- [ ] (0.1620) Fenêtre de chat de Tensho, roue des réglages : cocher « Masquer pendant les cinématiques » : la fenêtre disparaît pendant une cinématique (aussi une scène qui garde l'interface du jeu) et revient seule après.

- [ ] (0.1620) « Masquer en mission » coché : la fenêtre de chat Tensho disparaît en entrant dans un donjon et revient en sortant ; avec aussi « Cacher les messages du chat du jeu » coché, les messages du jeu restent cachés pendant la mission (aucun chat visible).

- [ ] (0.1621) Réglages du chat, couleurs : taper « Dire » dans la case du nom court de /dire : les messages de /dire montrent [Dire] ; vider la case : le nom court du jeu revient

- [x] (0.1621) « Noms des joueurs entre crochets » : les messages montrent [Nom] : texte, murmures compris ; un PNJ garde son nom sans crochets

- [x] (0.1621) « Une couleur pour le nom de chaque joueur » : chaque nom a sa couleur, la même à chaque message et après un rechargement ; un joueur mis en avant reste en couleur d'avertissement

- [x] (0.1622) Réglages du chat : « S'effacer après » à 10 s : sans nouveau message, la fenêtre s'efface en douceur et un clic à sa place agit dans le jeu ; la souris dessus ou un nouveau message la ramène

- [ ] (0.1622) Fenêtre de chat de Tensho, réglages d'apparence : mettez « Espace entre les lignes » à 6 px. Les lignes s'écartent ; le défilement et le bouton « En bas » marchent toujours bien.

- [ ] (0.1622) Réglages des onglets : « Taille à part » sur un onglet, 150 % : seul cet onglet grossit ; décocher : il reprend la taille de la fenêtre

- [x] (0.1623) Réglages du chat : « Mode capture » : les noms des joueurs avant leurs messages deviennent des codes #XXXXX, le même pour le même joueur ; les PNJ gardent leur nom ; décocher : les noms reviennent

- [x] (0.1624) Clic droit sur un message de la fenêtre de chat, « Choisir du texte… » : la boîte s'ouvre avec les messages de l'onglet ; choisir à la souris un morceau sur deux messages, Ctrl+C, coller ailleurs : le morceau exact ; « Tout copier » ; Échap ferme

- [ ] (0.1627) Survoler le nom d'un joueur dans la fenêtre de chat : son nom et son monde ; un membre du groupe ou un joueur proche : sa classe et son niveau aussi ; en mode capture : pas d'info-bulle


- [x] (0.1629) Fenêtre de chat Tensho, Paramètres › Chat, mode capture coché : chaque nom devant un message est un code #XXXXX ; un clic droit sur un message écrit « Copier le nom #XXXXX », « Cacher #XXXXX », « Mettre #XXXXX en avant » (aucun vrai nom) ; « Copier le nom » copie bien le vrai nom ; avec « couleur propre des noms », les couleurs changent entre deux rechargements de Tensho, comme les codes

- [x] (0.1630) Recharger Tensho (ou relancer le jeu) avec la fenêtre de chat ouverte : « Chargement… » disparaît en moins de 3 s et les lignes apparaissent sans à-coup

- [x] (0.1638) Fenêtre de chat Tensho : clic sur la loupe au bout de la barre des onglets : une case « Chercher dans l'onglet… » s'ouvre ; taper un mot (essayer sans accent pour un mot qui en a) : seuls les messages qui le contiennent restent, avec leur nombre ; un nouveau message qui le contient s'ajoute ; re-clic sur la loupe : tous les messages reviennent

- [x] (0.1640) Fenêtre de chat, recherche (loupe) : taper un mot lettre par lettre dans un onglet chargé (réglage de lignes au plus haut si possible) : aucune saccade du jeu ; « Recherche… » peut paraître un instant, puis le nombre de messages ; un nouveau message qui contient le mot s'ajoute ; fermer la loupe puis la rouvrir : la boîte est vide.

- [ ] (0.1640) Fenêtre de chat, mode capture allumé : dans la recherche, taper le code d'un joueur vu à l'écran (« #… ») trouve ses messages ; taper son vrai nom ne les trouve plus.

- [ ] (0.1641) Fenêtre de chat, réglages (roue) : allumer « Icônes des objets dans le texte », puis lier un objet en /dire (ou attendre un lien d'objet) : l'icône de l'objet paraît devant son nom, de la hauteur de la ligne ; un message long se coupe proprement autour ; clic droit, copier : le texte sans icône ; éteindre le réglage : plus d'icône.

- [x] (0.1642) Fenêtre de chat : clic droit sur un message, « Épingler en haut de cet onglet » : il paraît en haut de l'onglet, sur une ligne, dans la couleur de son canal ; au survol le message entier ; remonter ou descendre dans l'onglet puis cliquer l'épingle : l'onglet revient au message ; la croix le retire ; à 5 épingles, l'entrée du menu est grisée ; se déconnecter puis revenir : plus d'épingle.

- [ ] (0.1643) Fenêtre de chat : dans ses réglages, mots d'alerte, ajouter un mot ; afficher un autre onglet ; quand un message avec ce mot arrive dans l'onglet non affiché, cet onglet clignote (et porte « ! ») ; l'afficher : il cesse ; éteindre « Onglet qui clignote sur une alerte » : plus de clignotement, le « ! » reste.

- [ ] (0.1644) Fenêtre de chat, avec un historique déjà gardé d'une séance d'avant : décochez « Garder l'historique du chat sur ce PC », épinglez un message, puis recochez le réglage (les anciens messages reviennent avant ceux de la séance) : un clic sur l'épingle va toujours au message ; son clic droit propose « Retirer l'épingle », et pas « Épingler » une deuxième fois.

- [ ] (0.1646) Fenêtre de chat : dans ses réglages, « Votre prénom en alerte » activé ; demander à quelqu'un (ou un second compte) de dire votre prénom en /dire : la ligne ressort et l'onglet non affiché porte « ! » ; une ligne de butin qui vous nomme ne le fait pas.

- [ ] (0.1646) Fenêtre de chat : « Son des alertes » réglé sur un son (bouton « Écouter » pour l'entendre) : un message avec un mot d'alerte ou votre prénom joue ce son ; dans les couleurs des canaux, choisir un son pour Groupe : un message de groupe d'un autre joueur le joue, le vôtre non ; deux messages rapprochés ne jouent qu'un son.

- [ ] (0.1647) Fenêtre de chat : activer « Alertes dans la barre du serveur » ; quand un message avec un mot d'alerte ou votre prénom arrive dans un onglet non affiché, la barre du serveur montre « ! Chat 1 » ; un clic dessus affiche cet onglet et le compte revient à rien quand il est vu.

- [x] (0.1648) Fenêtre de chat, réglages d'apparence : changer l'opacité et une couleur de canal, « Copier l'apparence », remettre d'autres valeurs, puis « Coller une apparence » : la fenêtre reprend l'opacité et la couleur copiées, ses onglets et mots d'alerte restent ; avec un autre texte dans le presse-papiers, « Coller une apparence » dit qu'il n'y a pas de code et rien ne change.

- [x] (0.1649) Fenêtre de chat, réglages d'apparence : « Apparences toutes faites », choisir « Transparente » : plus de fond ni de cadre, lettres avec contour ; puis « Compacte » : texte plus petit et serré ; puis « Par défaut » : l'apparence d'origine ; les onglets et mots d'alerte restent à chaque fois.

- [x] (0.1650) Fenêtre de chat ouverte : recharger le plugin (ou relancer le jeu) ; « Chargement… » dure moins longtemps qu'avant, et le jeu ne saccade pas plus pendant ces secondes.

- [ ] (0.1651) Fenêtre de chat : clic droit sur un message, « Choisir du texte… » ; attendre 10 secondes : la fenêtre garde sa taille et la zone de texte reste visible ; on peut y choisir un morceau de texte et le copier par Ctrl+C.

- [ ] (0.1652) Fenêtre de chat, alertes dans la barre du serveur activées, onglets masqués (bouton en forme d'œil) : un message avec un mot d'alerte arrive dans un onglet qui n'est pas montré ; un clic sur « ! Chat 1 » dans la barre du serveur montre cet onglet.

- [ ] (0.1654) Fenêtre de chat, réglage « Conversations privées » activé : recevoir un murmure (ou en envoyer un) ; un onglet au nom du joueur s'ouvre au bout des onglets, marqué comme nouveau, avec les murmures échangés avec lui seulement.

- [ ] (0.1654) Clic droit sur le message d'un joueur, « Conversation avec … » : son onglet s'ouvre et se montre ; clic droit sur l'onglet, « Fermer cette conversation » : il part, ses murmures restent dans les autres onglets.

- [ ] (0.1654) Mode capture activé avec un onglet de conversation ouvert : l'onglet porte le nom de remplacement, pas le vrai nom.

- [ ] (0.1655) Réglages du chat, « Profils par contexte » : activer « Changer selon le contexte » ; pour « En ville », choisir un aspect sombre puis « Prendre l'aspect actuel ici », remettre l'aspect habituel, et décocher un onglet ; entrer dans une ville : l'aspect sombre et les onglets choisis viennent seuls ; en sortir : l'aspect habituel et tous les onglets reviennent.

- [ ] (0.1655) Profil « En mission » avec un aspect à lui : entrer en mission, changer un peu l'aspect sur place, sortir puis revenir : le changement est gardé pour la mission, l'aspect habituel revient dehors.

- [ ] (0.1655) Profil « En artisanat » : ouvrir le carnet d'artisanat et fabriquer : la fenêtre prend l'aspect et les onglets de l'artisanat, puis les rend en fermant le carnet.

- [ ] (0.1656) Réglages du chat, section Onglets : cliquer « Retirer » sur l'onglet « Combat » : il disparaît et la ligne « Onglet « Combat » retiré (encore N s pour l'annuler) » apparaît au-dessus des onglets ; cliquer « Annuler » avant 10 secondes : « Combat » revient à sa place avec ses réglages.

- [ ] (0.1656) Retirer « Combat » et laisser passer 10 secondes : sous la liste, le bouton « Remettre « Combat » » le remet seul à sa place ; puis « Remettre les onglets par défaut » : une question s'ouvre ; « Remettre » rend Général, Combat, Événement et Social, et l'historique du chat reste affiché.

- [ ] (0.1657) Avec « Cacher les messages du chat du jeu » et « Sous une fenêtre du jeu : se cacher » : ouvrir l'inventaire par-dessus la fenêtre de chat de Tensho : la fenêtre de Tensho se cache, mais le chat du jeu ne revient pas ; fermer l'inventaire : la fenêtre de Tensho revient à sa place.

- [ ] (0.1657) Survoler un lien d'objet dans le chat de Tensho (qui remplace celui du jeu) : seule l'info-bulle de Tensho s'affiche, aucune info-bulle d'objet du jeu ; la fenêtre ne disparaît pas et les réglages du chat de Tensho restent accessibles.

- [ ] (0.1657) Avec « Masquer en combat » : engager un combat : la fenêtre de Tensho se cache et le chat du jeu reste caché ; décocher « Cacher les messages du chat du jeu » : le chat du jeu revient aussitôt.

- [ ] (0.1658) Recharger Tensho (ou relancer le jeu), puis, dans les 30 secondes, recevoir un message d'un joueur (Dire ou Hurler) ; une minute plus tard, clic droit sur son nom dans le chat de Tensho : les entrées du jeu sont là (Murmurer actif, Carte de personnage…), comme pour un message reçu plus tard.

- [ ] (0.1659) Fenêtre de chat, réglages des onglets : retirer l'onglet « Combat », cliquer « Remettre « Combat » », puis « Annuler » avant la fin des 10 secondes : un seul onglet « Combat », pas deux.

- [ ] (0.1659) Réglages de la fenêtre de chat : cocher « Cacher les messages du chat du jeu » et « Masquer en combat » : une ligne orange dit qu'aucun chat ne sera visible en combat ou en mission ; cocher « Afficher les deux chats » : la ligne disparaît.

- [ ] (0.1660) Fenêtre de chat : clic droit sur le nom d'un joueur dans un message « Hurler » et dans un « Dire » neufs, puis dans un message ancien (de l'historique, d'avant le dernier chargement) : les entrées du jeu sont toujours là ; celles qui ne peuvent pas servir sont grisées et leur info-bulle dit pourquoi.

- [ ] (0.1660) Fenêtre de chat : juste après un rechargement de Tensho, « Murmurer » sur un message reçu (même ancien) : la ligne de saisie du jeu passe en murmure vers ce joueur, rien n'est envoyé ; écrire un mot et l'envoyer : le murmure arrive bien (sinon, le dire).

- [ ] (0.1660) Fenêtre de chat : clic droit sur votre propre message : aucune entrée du jeu (seulement celles de Tensho).

- [ ] (0.1661) Au Dédale bestial, devant « Formation de l'équipe » : le panneau de Tensho montre « Votre équipe (lue dans le jeu) : 10 bêtes », les trois bêtes de chaque combat prises parmi les vôtres, et « Sans X (conseillée) : Y à la place » quand une conseillée vous manque.

- [ ] (0.1661) Au Dédale bestial, après un combat où une bête est tombée ou a perdu beaucoup de PV, rouvrir « Formation de l'équipe » : le panneau dit « … est à terre : pas appelée » ou « … : PV bas (a/b) » et ne l'appelle plus si elle est à terre.

- [ ] (0.1662) Fenêtre de chat : clic droit sur le nom d'un joueur dans un « Hurler » puis dans un « Dire », et comparer au menu du chat du jeu sur le même message : mêmes entrées dans le même ordre (Fonctions de blocage en sous-menu) ; Recherche d'équipe ouvre l'annonce du joueur s'il en a une ; les entrées grisées disent pourquoi.

- [ ] (0.1663) Fenêtre de chat : faire glisser un onglet à une autre place, puis recharger Tensho (ou relancer le jeu) : l'ordre est gardé.

- [ ] (0.1663) Fenêtre de chat : clic droit sur un onglet, « Déplacer à droite » : l'onglet change de place, celui affiché reste affiché ; dans les réglages des onglets, les boutons « Déplacer à gauche / à droite » font de même.

- [ ] (0.1664) Fenêtre de chat : survoler un objet lié (par exemple un cristal ou un agrégat) : nom en haut, description du jeu, puis collection, stock, prix de vente aux PNJ, usages et « Comment l'obtenir » comme dans l'info-bulle d'objet de Tensho, la ligne des clics en bas ; comparer avec le survol du même objet dans l'inventaire.

- [ ] (0.1665) Chat de Tensho, profils par contexte : dans un contexte qui a un profil (en ville par exemple), glisser un onglet : hors du contexte, l'ordre de la fenêtre n'a pas changé ; revenir dans le contexte : l'ordre du profil est gardé, aussi après un rechargement

- [ ] (0.1665) Chat de Tensho : clic droit sur un joueur dans un message Hurler, puis de compagnie libre, puis de linkshell : « Répondre avec le même mode » est actif, et la zone de saisie du jeu passe sur ce canal (la bonne linkshell)

- [ ] (0.1666) Chat de Tensho en équipe (et si possible en alliance) : les noms des membres sont à la couleur de leur rôle (tank bleu, soigneur vert, DPS rouge) et ont l'icône de leur classe devant, comme dans le chat du jeu ; un joueur proche qui parle en Dire aussi ; un joueur loin et hors équipe : ni icône ni couleur de rôle

- [ ] (0.1666) Chat de Tensho, réglages : couper « Nom à la couleur du rôle du joueur » puis « Icône de la classe devant le nom » : la couleur puis l'icône disparaissent ; en mode capture, l'icône et la couleur restent mais le nom est remplacé

- [ ] (0.1667) Ouvrir un servant, puis sa liste de vente : dans le panneau de Tensho à côté, cliquer « 20 meilleurs à vendre » : la liste de l'inventaire du servant, triée par total après taxe, avec prix unitaire, source et date ; cliquer « Demander à Universalis » : les prix inconnus se remplissent ; comparer deux ou trois prix avec le tableau des ventes du jeu

- [ ] (0.1667) Même liste : un objet que le marché ne prend pas (objet de quête, intransférable) n'y est pas ; un objet sans prix connu est compté en bas comme « inconnu », jamais à 0 ; changer de servant dans la liste déroulante

- [ ] (0.1668) Chat de Tensho, profils par contexte avec un aspect pour « En ville » : en ville, ouvrir les réglages Apparence : la ligne « Aspect du profil « En ville » porté maintenant » s'affiche ; fermer la fenêtre de chat, sortir de la ville, changer l'opacité dans les réglages, rouvrir la fenêtre : le changement est gardé

- [ ] (0.1669) Survoler un objet dans l'aperçu du rangement (tri), une récompense d'événement et un appât de la pêche : l'info-bulle commune de l'objet (catégorie, niveau, usages, comment l'obtenir), avec l'emplacement et la quantité en bas pour le rangement, la remontée et le clic droit pour l'appât, sans le nom écrit deux fois

- [ ] (0.1670) Survoler une quête dans la page Quêtes, dans les sources d'un objet (onglet des quêtes qui le donnent) et un lien de quête dans le chat de Tensho : la même info-bulle (icône, section et genre, niveau, zone, extension, avancement), avec « Clic gauche… » en bas pour le chat

- [ ] (0.1671) Au carillon, ouvrir un servant puis sa liste de vente, « 20 meilleurs à vendre », « Demander à Universalis » : les prix arrivent, l'heure s'affiche à côté ; puis « Comparer » dans le panneau : les prix de la liste restent

- [ ] (0.1671) Servant avec plus de 100 objets différents : cliquer deux fois « Demander à Universalis » (3 s d'écart) : le nombre d'objets sans prix baisse la deuxième fois

- [ ] (0.1671) Liste de vente : le bas dit « taxe de 5 % (taux de l'aide du jeu…) » ; si le journal du plugin écrit « Market tax rates received », il dit le taux de la ville du servant ouvert

- [ ] (0.1672) Survoler un lien de carte dans le chat de Tensho, un favori de la page Cartes et une adresse du carnet d'adresses : la même info-bulle (zone, région, coordonnées, éthérite la plus proche avec son coût, météo actuelle), avec « Clic gauche… » en bas pour le chat et la note pour le favori

- [ ] (0.1673) Survoler un monstre proche sur la carte Tensho, l'icône au-dessus d'un monstre en jeu et un monstre de la page Monstres : la même info-bulle (nom, niveau, zone sur la page Monstres, raisons : magie bleue, carnet de chasse…)

- [ ] (0.1674) Survoler une action dans la palette du simulateur d'artisanat, une étape du journal d'artisanat et un effet de l'artisanat en direct : la même info-bulle (icône, nom, niveau et PS, solidité, description du jeu), avec le numéro de l'étape dans le titre pour le journal

- [ ] (0.1675) Pendant une récolte, survoler l'action conseillée du panneau de récolte, puis une action du plan des PR (page Récolte) : la même info-bulle (icône, nom, niveau et PR, description du jeu)

- [ ] (0.1676) Survoler une ALEA de la page ALEA, puis une ALEA en cours dans la liste de la carte Tensho (onglet des ALEA) : la même info-bulle (icône, nom, ALEA et niveau, lieu, progression et temps restant pour celle en cours, description)

- [ ] (0.1677) Barre de côté : « Personnage » remplace Collections, Équipement, Chocobo, Bestiaire, La Grande Mascarade, Armes antiques et Dresseur ; ses huit onglets s'ouvrent ; quitter la page puis recliquer « Personnage » : l'onglet quitté revient

- [x] (0.1677) Recherche rapide : taper « bestiaire » puis « équipement » : le résultat « … · Personnage » ouvre le bon onglet ; depuis l'accueil, la tuile Équipement ouvre l'onglet Équipement

- [x] (0.1678) Barre de côté : « Objets » ouvre quatre onglets (Recherche d'objets, Rangements, Trier, Marchés) ; « Souhaits » reste une entrée à part ; clic droit sur un objet du jeu › prix : l'onglet Marchés s'ouvre ; « Voir les quantités » de l'objet en contexte ouvre l'onglet Recherche d'objets

- [ ] (0.1679) Barre de côté : « Monde » ouvre sept onglets (Carte, Météo, ALÉA, Échoppes, Butin des missions, Butin des monstres, Découvertes) ; « Artisanat » a l'onglet Macros et « Récolte » l'onglet Pêche en mer ; un lien de carte du chat ou « Voir sur la carte » ouvre l'onglet Carte ; la météo ouverte depuis une zone montre le nom de la zone sous le titre

- [ ] (0.1680) Barre de côté : « Quêtes » (Journal, Mandats, Événements saisonniers, Peuples alliés, Grandes compagnies), « Livraisons » (2 onglets), « Missions » (Eurêka et Bozja, Donjons sans fond) et « Gold Saucer » (Revue de mode, Triple Triade, Mini Cactpot) ouvrent leurs onglets ; un lien vers une quête depuis une autre page ouvre l'onglet Journal sur cette quête

- [ ] (0.1681) Barre de côté : 19 entrées, une seule catégorie « Calendrier » (Alerte, Rappels, Jardinage, Compagnie libre) ; si elle était repliée avant la mise à jour, elle l'est encore, sinon elle est ouverte ; la replier, recharger le plugin : elle reste repliée.

- [ ] (0.1681) Avec « Ouvrir sur la dernière page » coché : fermer la fenêtre sur un onglet (par exemple Personnage › Bestiaire), recharger le plugin, /tensho : le même onglet s'ouvre

- [ ] (0.1681) « Aide » a trois onglets (Aide, Lexique, Crédits) ; « Paramètres » un seul hors mode développeur, deux avec ; le « ? » d'une page ouvre l'onglet Aide sur cette page ; fenêtre étroite : le bouton de menu montre la même liste courte

- [ ] (0.1681) Aide › « Lancer le guide » : le guide montre les mêmes étapes qu'avant, sans page ni réglage nouveau ; seule la place des pages dans la barre de côté a changé.

- [x] (0.1683) Barre de côté : l'entrée « Alerte (n) » et la catégorie « CALENDRIER » remplacent « Alarmes » et « SUIVI » ; si la catégorie était repliée avant la mise à jour, elle l'est encore ; la page s'ouvre sur le titre « Alerte »

- [x] (0.1683) Recherche rapide : taper « alerte » propose la page Alerte et l'ouvre ; en anglais, « alarm » fait de même

- [ ] (0.1685) Pêche normale : lancer, attendre la touche : le chrono s'arrête à la touche (« Touche à N s » avec « ! », « !! » ou « !!! ») et reste figé pendant le ferrage, que le poisson soit pris ou non ; au lancer suivant il repart de zéro ; le poisson pris a sa case « Temps de touche » marquée « » »

- [ ] (0.1685) Pêche en mer : lancez et attendez la touche. Le chrono s'arrête à la touche (« Touche à N s »). Le bouton « Relancer » après la touche remet le chrono à zéro.

- [ ] (0.1685) Sous Amorçage : utiliser Amorçage puis lancer : « Amorçage » s'affiche à côté du chrono, les temps de touche sont divisés par deux (par exemple 2–4,5 s au lieu de 4–8 s), l'info-bulle d'une case le dit, et le temps figé tombe dans la fenêtre du poisson pris

- [ ] (0.1686) Liste de vente d'un servant (bouton de la fenêtre de vente), après avoir ouvert le tableau des ventes une fois cette semaine : choisir dans la liste un autre servant que celui ouvert : le pied de liste dit « le taux de sa ville, tel que le jeu l'a envoyé », avec le taux de la ville de ce servant (et pas 5 %).

- [ ] (0.1687) Pêche sous Amorçage (panneau de pêche) : survoler le badge « Amorçage » puis une case de la colonne « Temps de touche » : les deux disent « règle de Tensho (ni le jeu ni la source ne donnent de chiffre) »

- [ ] (0.1688) Fenêtre de chat de Tensho, clic droit sur le nom d'un joueur près de vous (Dire) : « Envoyer une requête d'ami », « Fonctions de blocage › Ajouter à la liste noire » et « Signaler ce joueur » ne sont plus grisés ; leur info-bulle dit qu'ils ouvrent le menu du jeu ; un clic sur l'un d'eux ouvre le menu du jeu de ce personnage, où se trouve l'entrée

- [ ] (0.1688) Fenêtre de chat de Tensho, clic droit sur un Hurler d'un joueur loin : les trois mêmes entrées restent grisées ; leur info-bulle dit de passer par le chat du jeu ou de s'approcher du joueur ; dans le chat du jeu, le clic droit sur ce joueur donne bien ces entrées

- [ ] (0.1689) Survoler un marchand dans la liste « Marchands » de la carte Tensho (clic droit sur la carte), sur la page Marchands (colonne PNJ) et parmi les sources d'un objet vendu : même info-bulle (icône, nom, lieux, boutiques avec leur nombre d'objets), puis l'effet du clic

- [ ] (0.1689) Survoler un joueur de Triple Triade sur la page Triple Triade puis dans la liste de la carte : même info-bulle (lieu, mise et cartes, cartes manquantes, battu ou non, quêtes demandées)

- [ ] (0.1690) En équipe, carte Tensho ouverte : survoler un membre de l'équipe sur la carte puis son repère au bord de la carte : nom et monde, puis classe et niveau, comme en survolant son nom dans la fenêtre de chat de Tensho

- [ ] (0.1691) Monde › Butin des missions : survolez un donjon de la liste : icône, nom, genre et niveau, zone, avancement, quête qui le débloque, puis « Clic : son butin et ses détails » ; même info-bulle sur un étage de Missions › Donjons sans fond et sur le bouton du contenu d'un monstre (Monde › Butin des monstres).

- [ ] (0.1692) Objets › fiche d'un objet qui sert d'ingrédient, liste des recettes : survolez une recette : icône de la classe, objet et quantité, classe et niveau, durabilité, progression et qualité, minimum, faits (HQ, travail rapide…), déjà fabriquée, ingrédients ; même info-bulle sur l'icône de classe d'une fabrication dans un panneau de liste, et dans Artisanat › onglet « Que puis-je fabriquer ? ».

- [ ] (0.1694) Carte Tensho : survoler un point de récolte (à horaire si possible) sur la carte, puis dans la liste « Récolte » (clic droit sur la carte) : même contenu (genre et niveau, métier et sorte, lieu, ouvert ou fermé et pour combien de temps, objets avec icône), puis l'effet du clic

- [ ] (0.1694) Aide à la pêche au harpon : survoler le nom d'un poisson : info-bulle complète de l'objet, puis l'ombre et « vu » s'il l'a été

- [x] (0.1695) Jardin : survoler une plantation puis une graine des croisements : lignes du jardin, puis collection, stock, usages et comment obtenir la graine

- [ ] (0.1695) Eurêka et Bozja, onglet des actions : survoler une action : icône, nom, catégorie et maximum, description du jeu

- [ ] (0.1696) Paramètres › Général › Autres infobulles : couper « Météo actuelle » (Lieux) puis survoler un lien de carte du chat ou un favori de la carte : la météo n'y est plus ; la remettre : elle revient. Même chose avec « Boutiques » (PNJ) sur la page Marchands


- [x] (0.1698) Alarmes (ou Rappels, Jardin) : tirer un onglet à une autre place, quitter la page puis revenir, puis recharger le plugin : l'ordre reste ; un lien qui ouvre un onglet précis (par exemple une alarme depuis la barre du serveur) ouvre toujours le bon ; Paramètres › Apparence › « Remettre les onglets des pages dans l'ordre » : l'ordre d'origine revient

- [ ] (0.1700) Objets : ouvrir plusieurs objets de suite dans la fiche (un avec recette, un récoltable, un sans marché) : les onglets suivent sans clignoter ni montrer un corps vide, dans l'ordre rangé ; faire défiler l'aperçu d'un objet puis changer d'onglet et revenir : le défilement reste

- [ ] (0.1701) Rangements, Marché, Compagnie libre, Cartes, Paramètres, Collections, Triple Triade, artisanat : tirer un onglet à une autre place, changer de page puis revenir, recharger le plugin : l'ordre reste ; un lien qui ouvre un onglet précis (« Construire » d'une route, une collection depuis la recherche, l'engrenage de la carte vers Paramètres › Carte) ouvre toujours le bon ; « Remettre les onglets des pages dans l'ordre » remet tout

- [ ] (0.1701) Objets › Rangements › Banque : tirez l'onglet d'un servant à une autre place : il y reste ; ouvrez l'onglet d'un servant, puis ouvrez la liste des servants à une sonnette d'appel pour qu'elle change : l'onglet ouvert montre toujours le même servant.

- [ ] (0.1702) Barre de côté : tirer une entrée (par exemple Carte) au-dessus d'une autre du même groupe : un trait doré montre la place, au relâchement l'entrée y reste et la page ne change pas ; un simple clic ouvre toujours la page ; recharger le plugin : l'ordre reste ; Paramètres › Apparence › « Remettre les onglets des pages dans l'ordre » remet aussi la barre de côté

- [ ] (0.1703) Compagnie libre › Meilleurs trajets › Sous-marins : choisir un sous-marin, le but « Débloquer un secteur », puis un « Secteur à débloquer » encore fermé : la suite des secteurs à explorer s'affiche avec leurs rangs, à partir du dernier déjà débloqué ; un secteur déjà débloqué ou le premier de la carte est dit tel quel

- [ ] (0.1705) Barre de côté : cliquer une entrée en bougeant un peu la souris pendant le clic : la page s'ouvre ; commencer à glisser une entrée, fermer la fenêtre par son raccourci en tenant le bouton, relâcher, rouvrir : l'entrée n'a pas bougé

- [ ] (0.1706) Compagnie libre › Meilleurs trajets : choisir Sous-marins puis Aéronefs en tête : chaque genre garde son vaisseau, son rang, sa vitesse et son autonomie ; un aéronef lu à l'atelier avec un montage prévu montre le bouton « Montage prévu » ; les meilleurs trajets et « Vos aéronefs » s'affichent comme avant

- [ ] (0.1706) Compagnie libre › Secteurs d'aéronef : cocher deux secteurs puis « Planifier les trajets d'aéronef » : l'onglet Meilleurs trajets s'ouvre sur les aéronefs et mesure le trajet coché

- [ ] (0.1707) Compagnie libre › Meilleurs trajets › Sous-marins : but « Butin : un objet », choisir un objet : seuls les trajets par un secteur où il peut tomber, les plus courts d'abord ; but « Débloquer un secteur », choisir un secteur fermé : les trajets passent par le prochain secteur à explorer

- [ ] (0.1707) Meilleurs trajets › Sous-marins : ouvrir « Secteurs de cette carte », cocher deux secteurs : la ligne du trajet mesuré s'affiche (distance, durée, expérience, valeur) ; « Choisir » sur un meilleur trajet le coche ; « Vider » décoche

- [ ] (0.1707) Meilleurs trajets › Sous-marins : ouvrir « Vos sous-marins » : un meilleur trajet par sous-marin lu à l'atelier ; « Choisir » reprend ce sous-marin et coche son trajet

- [ ] (0.1709) Meilleurs trajets › Sous-marins : but « Débloquer un secteur », choisir un secteur déjà débloqué à l'atelier, ouvrir « Vos sous-marins » : chaque sous-marin de cette compagnie dit que le secteur est déjà débloqué ; avec un secteur encore fermé, chacun passe par le prochain secteur à explorer de sa compagnie (sous-marins de deux compagnies : chacun suit la sienne)

- [ ] (0.1710) Quêtes : ouvrir la fiche d'une quête : le donneur avec son lieu et ses coordonnées, le PNJ de fin, les classes et jobs (et le peuple allié pour une quête de peuple allié) ; « Placer le donneur sur la carte » ouvre la carte de Tensho sur le donneur avec le drapeau, au même endroit que le donneur sur la carte du jeu

- [ ] (0.1710) Quêtes : fiche d'une quête acceptée à étapes (par exemple une quête de classe dans Limsa Lominsa) : « Étape en cours » donne le lieu de l'étape ; « Placer l'étape en cours sur la carte » pose le drapeau au même endroit que le repère de la carte du jeu ; refaire après l'étape suivante

- [ ] (0.1711) Carte de Tensho près de points de récolte, en mineur puis en botaniste : les points présents ont la pioche (mineur) ou la feuille (botaniste) ; un point à horaire présent a l'icône à étincelles avec une petite horloge verte ; sa zone est rose, celle d'un point normal verte, sur une carte claire et une carte sombre

- [ ] (0.1711) Paramètres › Carte : « Zones de récolte à horaire » (case et couleur) ; dans la liste des icônes, survoler l'icône de la ligne des points de récolte proches : la légende montre les quatre icônes et les deux couleurs de zone

- [ ] (0.1713) Carte de Tensho › réglages des zones colorées : décocher « Zones de récolte » : la case « Zones de récolte à horaire » se grise et aucune zone de récolte n'est dessinée, ni verte ni rose ; recocher : les deux reviennent

- [x] (0.1714) Fenêtre de chat Tensho : clic droit sur un onglet › « Détacher dans sa propre fenêtre » : l'onglet quitte la barre et s'ouvre dans sa propre fenêtre, avec son nom et ses messages ; les messages nouveaux y arrivent, les liens, le clic droit sur un message (copier, épingler, choisir du texte) et la molette y marchent ; la déplacer et la redimensionner, puis /xlplugins › recharger Tensho : elle revient à la même place

- [x] (0.1714) Fenêtre de chat Tensho : avec un onglet détaché, le bouton en haut de sa fenêtre (flèche vers une boîte), puis sa croix une autre fois : l'onglet revient dans la barre de la fenêtre de chat, affiché ; avec un seul onglet restant dans la fenêtre de chat, « Détacher » est grisé et son info-bulle dit pourquoi

- [ ] (0.1714) Fenêtre de chat Tensho : avec un onglet détaché, taper « /tensho chat hide » : toutes les fenêtres de chat Tensho disparaissent ; recevoir un message, puis taper « /tensho chat » : elles reviennent, le message est là ; clic droit sur un onglet › « Masquer toutes les fenêtres de chat un moment » fait de même

- [ ] (0.1715) Fenêtre de chat Tensho, onglet du combat : manger un plat (ou boire une potion), puis survoler « Repu » (ou l'effet de la potion) dans la ligne « Vous bénéficiez de l'effet … » : l'info-bulle montre l'icône et le nom, puis les bonus comme l'info-bulle du statut dans le jeu (« Effets du repas (exp +n%) », puis « Collecte +n% (max m) »…, valeurs HQ pour un plat HQ) ; un autre effet montre sa description ; si un repas ne montre que « Sous l'effet d'un repas. », le dire

- [x] (0.1715) Mode développeur, après quelques lignes de combat (actions, dégâts, effets) dans le chat du jeu : dalamud.log a des lignes « Chat window, pieces of a combat line (kind …) » et « pieces of a message with a link » avec « Status … » (rien d'autre à faire : l'agent les lit)

- [x] (0.1716) Fenêtre de chat Tensho › roue dentée › Joueurs : ajouter plus de 8 noms à « Joueurs cachés » : la liste reste haute de 8 lignes avec sa barre de défilement, et la croix d'une ligne la retire toujours

- [ ] (0.1717) Fenêtre de chat Tensho, profils de contexte : dans un contexte dont le profil ne montre que des onglets détachés, la fenêtre de chat garde le premier de ces onglets (sa fenêtre détachée se cache) ; hors de ce contexte, il retourne dans sa fenêtre

- [ ] (0.1717) Fenêtre de chat Tensho › réglages des onglets : avec tous les onglets sauf un détachés dans leur propre fenêtre, le bouton « Retirer » de l'onglet resté dans la fenêtre de chat est grisé et son info-bulle dit pourquoi

- [ ] (0.1717) Réglages du chat › Joueurs mis en avant : après l'avis « liste pleine », retirer un joueur par le menu d'un message (« Ne plus mettre en avant ») ou changer le texte tapé : l'avis disparaît

- [x] (0.1718) Mode développeur : dans le chat du jeu, cliquer sur le nom d'un joueur, du bouton gauche puis du droit : un joueur près de vous, un joueur loin sur votre monde, un joueur d'un autre monde ; le journal (dalamud.log) doit montrer une ligne « Chat link probe, click N » par clic et, quand le menu du jeu s'ouvre, une ligne « menu after click N » (ou « without a link click »), sans nom de joueur

- [ ] (0.1719) Maison de la compagnie, base de tour éthéréen : installer un tour (fenêtre ouverte) : Tensho › Rappels, ligne « Tours éthéréens » : « 1 en charge », et « Tour par tour » donne le tour, son emplacement, sa charge et « chargé à » l'heure d'installation plus 20, 45 ou 70 heures selon le grade ; une alarme automatique « … chargé » est créée à cette heure (page Alertes)

- [ ] (0.1719) Rouvrir la base plus tard (le pourcentage a changé) : la ligne du tour garde la même heure de fin ; pour un tour posé sans que Tensho l'ait vu, la ligne dit « chargé entre … et … » et l'écart se resserre quand le pourcentage change

- [ ] (0.1719) Quand un tour est chargé : la ligne dit « chargé » (ou « chargé depuis … au plus tard »), l'alarme a sonné ; retirer un tour de la base (fenêtre ouverte) : il quitte la ligne et son alarme à venir disparaît

- [ ] (0.1720) Fenêtre de chat Tensho, onglet de combat : se téléporter, utiliser une compétence de votre job (et laisser un allié en utiliser une), puis survoler le nom de l'action dans la ligne « Vous utilisez … » ou « … utilise … » : l'info-bulle montre son icône, son nom et sa description ; survoler aussi le nom d'un effet (« Vous bénéficiez de l'effet … ») : son info-bulle ; le nom d'une action d'ennemi qui porte le nom d'une action de joueur n'a pas d'info-bulle

- [x] (0.1722) Mode développeur, atelier de la compagnie, panneau de contrôle aérien : liste des aéronefs, puis le détail de l'aéronef (ses pièces) : le journal a des lignes « Workshop vessel read: Airship 1: rank …, speed …, range … » ; dire si vitesse, distance et pièces sont à 0 avant le détail et lues après

- [x] (0.1722) Compagnie libre › Trajets › Aéronefs : choisir l'aéronef : si ses valeurs n'ont pas été lues, une ligne dit « Vitesse et distance de ce vaisseau non lues… » et aucun trajet n'est cherché ; sinon vitesse et distance sont celles du jeu (ou « d'après ses pièces lues à l'atelier ») et des trajets sont proposés ; saisir une vitesse et une distance retire la ligne

- [x] (0.1723) Jardin, un emplacement dont l'heure calculée est passée sans avoir ouvert son menu en jeu : la grille dit « Mûr ? » (marque en sablier), l'info-bulle « Mûr selon le calcul, à confirmer en jeu » avec l'explication ; le message du chat dit « devrait être mûr selon le calcul » ; ouvrir le menu de l'emplacement en jeu quand il est mûr : « Mûr » (marque verte)

- [ ] (0.1723) Mode développeur : planter une graine puis ouvrir le menu d'un emplacement : le journal a « Garden planting noted: patch …, bed …, seed item …, planted …, growing … h (…) » et « Garden bed state kept: … (… min now) … »

- [x] (0.1724) En équipe, quelques messages dans le chat d'équipe : la fenêtre de chat de Tensho écrit « [1] [icône SAG] [Nom] » (chiffre, puis icône et abréviation du job, puis le nom) dans la couleur du rôle ; quitter l'équipe : ces lignes gardent couleur et icône ; un message du même joueur après (s'il n'est plus proche) n'a pas de couleur

- [ ] (0.1724) Mode développeur, en équipe : le journal a « Chat window, pieces of a sender's name (kind 14): … » ; dire d'où viennent le chiffre et l'abréviation (glyphe U+…, mot en capitales, icône)

- [x] (0.1725) Barre du serveur, entrée « Reste à faire » activée : tout cocher dans Rappels (ou rien à faire) : l'entrée disparaît de la barre du serveur ; un rappel qui revient (remise à zéro) ou une tâche affichée : elle revient seule en quelques secondes

- [ ] (0.1726) Vos ventes, liste à vendre d'un servant avec des prix vus : le bouton « Copier » montre et copie le prix le plus bas vu moins 1 (coller dans le jeu) ; la colonne « Prix vu » dit « tableau, 13:45 » (l'heure entière au survol) ; à la largeur par défaut de la fenêtre, les colonnes « Prix vu » et « Copier » se voient en entier

- [ ] (0.1727) Jardin, un emplacement dont l'heure calculée est passée sans que le jeu l'ait dit mûr : survoler l'icône des rappels terminés de la barre du serveur : la ligne dit « Emplacements mûrs selon le calcul, à confirmer en jeu : 1 », pas « Emplacements mûrs » ; ouvrir le menu de l'emplacement en jeu (mûr) : la ligne devient « Emplacements mûrs : 1 » ; l'accueil, ligne « Terminé », dit la même chose

- [ ] (0.1727) si une récolte est à venir et les rappels du jardin activés : l'accueil, « Prochaine échéance », dit « <graine> : mûr (calcul) », comme l'onglet Alarmes du jardin

- [ ] (0.1728) Planificateur de trajets, aéronef dont vitesse et distance sont inconnues (ligne « non lues ») : saisir la vitesse seule : la ligne reste et rien n'est cherché ; saisir aussi la distance : la ligne disparaît et la recherche donne des trajets

- [ ] (0.1729) Fenêtre de discussion Tensho, en équipe, une ligne d'un membre (« [1] [SAG] Nom ») : chercher « [1] », puis « 1 SAG » : la ligne est trouvée les deux fois

- [ ] (0.1730) Paramètres › Suivi et notifications › Barre du serveur : on y trouve toutes les cases de la barre du serveur (alarme, rappels, rappels terminés, devises, gils du jour, sacs, équipement, zone, alertes du chat, état de Tensho et son panneau), puis l'infobulle ; Général › En-tête et accueil n'en a plus ; Panneaux › Joueurs du chat n'a plus les alertes dans la barre

- [ ] (0.1730) N'afficher que les sacs dans la barre du serveur : la case « Infobulle Tensho sur ses entrées » n'est pas grisée ; tout couper : elle l'est

- [ ] (0.1730) Tableau de bord (état de Tensho), bouton « Détails », et clic sur l'entrée de l'état dans la barre du serveur : la page s'ouvre sur Suivi et notifications › Barre du serveur

- [ ] (0.1731) Paramètres, Général : « Ouverture et fermeture de la fenêtre » a « Fermer Tensho quand il ouvre une fenêtre du jeu » ; « En-tête et accueil » n'a plus que les deux heures et l'annuaire ; « Survol et clic droit des objets » a « Afficher les prix au survol des objets en jeu » (grisé si Universalis est coupé) ; « Objets et marché » a « Tableau des ventes : copier l'offre la plus basse moins 1 »

- [ ] (0.1731) Paramètres, Suivi et notifications › Alerte : « Fenêtre de suivi épinglée » y est ; Panneaux › À côté des fenêtres du jeu : « Panneau à côté de la liste de ventes des servants » ; Données › Sources communautaires : sous Universalis, la ligne dit où sont les prix au survol ; chercher « prix au survol » dans la recherche des réglages mène au bon endroit

- [ ] (0.1732) Carnet de récolte du jeu, clic droit sur un objet (un objet de collection aussi) : le menu du jeu a l'entrée « Tensho » et ses outils portent sur cet objet

- [ ] (0.1732) Carnet de pêche du jeu, clic droit sur un poisson : l'entrée « Tensho » apparaît ; sinon, en mode développeur, lire la ligne « Context menu opened » du journal (nom de la fenêtre, objet survolé)

- [ ] (0.1733) Fenêtre « Suivi Tensho » avec une alarme d'objet créée sans icône (terreau, minerai) : la ligne montre l'icône de l'objet ; survoler l'icône ou le nom : l'info-bulle de l'objet, avec sous son nom le lieu et la prochaine alarme

- [ ] (0.1733) Clic droit sur une ligne du suivi : « Ouvrir le module Alerte », « Couper cette alarme » (la ligne disparaît, l'alarme est coupée dans Alerte), « Carte », puis le menu habituel de l'objet

- [ ] (0.1736) Fenêtre de chat Tensho, « S'effacer après » à 10 s (réglages de la fenêtre de chat) : attendre qu'elle s'efface, puis qu'un message arrive dans l'onglet affiché (un /dire d'un joueur proche, ou /echo dans un onglet qui le montre) : la fenêtre revient ; un message d'un autre onglet ne la fait pas revenir

- [ ] (0.1736) Même réglage, avec « Revenir aussi pour un message privé ou une alerte » coché : effacée, un message privé reçu (ou un message avec votre prénom) dans un autre onglet la fait revenir ; décoché, non

- [ ] (0.1737) Mode capture de la fenêtre de chat coché, page Cartes, liens de carte du chat : survoler un lien envoyé par un joueur : l'info-bulle montre « #XXXXX », le même code que la fenêtre de chat pour ce joueur, jamais son nom ; décoché : son nom avec son monde (Nom@Monde)

- [ ] (0.1738) Page Marchands, un objet vendu par beaucoup de PNJ dont certains à placer chez soi (un meuble ou une teinture courante) : les marchands sur une carte d'abord, puis une seule ligne « Marchands sans position : N (cliquez pour les afficher) » ; un clic les montre, un autre les cache

- [ ] (0.1739) Fenêtre de chat de Tensho, déjà Repu d'un repas : manger un autre repas ; survoler la nouvelle ligne « Vous bénéficiez de l'effet Repu » : les bonus du nouveau repas (pas de l'ancien) ; à la fin de l'effet, la ligne « Vous perdez l'effet Repu » montre seulement la description

- [ ] (0.1740) Fenêtre de chat, « S'effacer après » à 10 s et historique gardé : recharger Tensho, attendre que la fenêtre s'efface sans écrire : elle ne revient pas quand les anciens messages sont remis (dans les premières secondes) ; un nouveau message de l'onglet la fait revenir

- [ ] (0.1744) Chat de Tensho : manger un repas alors qu'on est déjà Repu d'un autre repas : l'info-bulle de la nouvelle ligne « Repu » montre les bonus du nouveau repas ; puis lire le journal : la ligne « status put on N frame(s) (M ms) after its line » donne le délai du jeu, s'il y en a un

- [ ] (0.1745) Premier chargement de cette version : les réglages sont les mêmes qu'avant (thème, alarmes, chat, carte, tailles d'icônes) ; le journal dit une fois « Settings moved from Dalamud's settings file to Tensho's own (Settings.json) » ; le fichier pluginConfigs\TenshoAssistant\Settings.json existe

- [ ] (0.1745) Changer un réglage, recharger Tensho : le réglage est gardé

- [ ] (0.1745) Après quelques rechargements (mode développeur) : les lignes « context alive » ne nomment aucune copie chargée depuis cette version, et la mémoire gérée ne monte plus d'une copie à chaque rechargement

- [ ] (0.1748) Chat de Tensho, un autre joueur à portée mange un repas (ligne « … bénéficie de l'effet Repu ») : l'info-bulle de « Repu » montre les bonus de ce repas, ou seulement la description du statut ; jamais les bonus d'un repas plus ancien ; le journal dit l'issue (« another player's, … »).

- [ ] (0.1750) Tensho, page Dresseur (bestiaire) et panneau du Dédale bestial (équipe, bêtes appelées pour un combat) : après le texte de chaque bête, les icônes des effets qu'elle inflige, dans l'ordre de la fenêtre « Formation de l'équipe » du jeu, chacune nommée au survol (Mouton : Sommeil ; Mandragore : Pesanteur puis Poison ; Cu sith : aucune).

- [x] (0.1751) Tensho, page Banque, un onglet avec des objets (filtres au choix) : « Copier en CSV », puis collez dans un tableur : une ligne par rangement, objet et qualité, avec la quantité ; le message dit le nombre de lignes copiées.

- [ ] (0.1752) Barre du serveur, entrée « Tensho » : après une séance avec plusieurs mises à jour de Tensho, la mémoire affichée ne grimpe plus d'environ 100 Mo à chaque mise à jour ; dites le nombre affiché après le démarrage du jeu et après cinq mises à jour.

- [x] (0.1756) Tensho, page Marché, un objet, onglet Historique (filtres au choix) : « Copier en CSV », puis collez dans un tableur : une ligne par vente affichée, dans l'ordre du tableau, avec la date, le monde, le prix unitaire, la quantité et la qualité ; le message dit le nombre de ventes copiées.

- [ ] (0.1757) Près d'un autre joueur qui n'est pas Repu, quand il prend un repas : survolez l'effet « Repu » de sa ligne dans la fenêtre de chat de Tensho : l'info-bulle montre les bonus de ce repas ; si elle n'en montre aucun, la ligne du journal « … s from its full duration » dit l'écart vu (à rapporter).

- [x] (0.1758) Tensho, page Marché, un objet, onglet Historique : « Copier en CSV », la note « N vente(s) copiée(s) » s'affiche ; changez de filtre ou d'objet : la note disparaît. De même à la Banque après un changement d'onglet ou de filtre.

- [ ] (0.1759) Près d'un autre joueur qui prend deux repas différents à moins de 3 s d'écart : survolez l'effet « Repu » de la deuxième ligne dans la fenêtre de chat de Tensho : l'info-bulle montre les bonus du deuxième repas, ou aucun, jamais ceux du premier ; le journal dit « the meal shown for that player's line of the 4 s before » quand c'est ce cas.

- [ ] (0.1760) Utilisez un manuel d'artisan (ou de récolteur) pendant que Tensho tourne, puis fabriquez (ou récoltez) : page Équipement, sous le dernier artisan (ou récolteur), « Grâce de l'artisan — environ N points de bonus restants » ; N baisse à chaque ligne d'expérience de la part du manuel (sans autre bonus : 60 % des points gagnés) ; le journal dit « Grace: an experience line read ».

- [ ] (0.1760) Avec un manuel compté : rechargez Tensho (/xlplugins) : la ligne garde son nombre. Un manuel déjà actif quand Tensho se charge sans compte gardé (ou après de l'expérience gagnée sans Tensho) : « Points restants inconnus ».

- [ ] (0.1760) Quand le jeu retire la Grâce (plafond atteint), la ligne disparaît ; le dernier nombre vu était proche de 0 (à rapporter s'il en était loin).

- [x] (0.1761) Survolez dans le jeu un objet recyclable (une pièce d'équipement fabriquée) : sous l'infobulle, « Recyclage : <artisan>, niveau de l'objet N, votre niveau M : au-dessus / en dessous de votre niveau » ; M est celui de la fenêtre de recyclage du jeu pour cet artisan.

- [x] (0.1761) Survolez un coffret ou un paquet de cartes : « Collections : les N obtenus » quand vous avez tout, sinon « n sur N pas encore obtenus » ; coupez chaque ligne dans Paramètres › infobulles : elle disparaît.

- [ ] (0.1762) À une sonnette, ouvrez la liste des servants puis la liste de vente d'un servant qui vend quelque chose : page Personnages, tableau des servants, sous ce servant, « Objets en vente (relevés le …) » et chaque objet avec sa quantité et son prix (HQ dit) ; un servant jamais ouvert qui vend dit « pas encore relevés ».

- [ ] (0.1762) Rechargez Tensho : la liste du servant reste, avec sa date ; vendez ou retirez un objet puis rouvrez la liste de vente : la liste et sa date changent.

- [ ] (0.1765) Avec un manuel compté (page Équipement, « environ N points de bonus restants ») : téléportez-vous ou changez de zone, puis fabriquez (ou récoltez) : la ligne garde son nombre et continue de baisser, elle ne revient jamais au plafond entier.

- [ ] (0.1765) Si vous utilisez un manuel de compagnie (I ou II) ou un manuel révisé : le journal dit « Grace: 45 (Param N, … » (46 pour un manuel de récolte) ; N doit être 40, 50 ou 80 selon le manuel (à rapporter sinon), et la page Équipement part du plafond de ce manuel (250 000, 500 000 ou 2 000 000).

- [ ] (0.1766) Page Dresseur, bestiaire : après la famille de chaque bête, une petite icône ; au survol « Assimilation (Nv 22) : Cuir de thérien » (ou l'action de sa famille) et son effet. Ouvrez le filtre des familles : chaque famille avec son action ; choisissez-en une : la ligne sous le filtre montre l'icône, le nom et l'effet. Comparez avec l'action que le jeu donne quand vous utilisez Assimilation avec une bête de cette famille.

- [ ] (0.1767) Page Dresseur, onglet « À capturer » : les bêtes pas encore capturées, d'abord « Dans la nature » puis « En mission » ; pour chacune le lieu de capture du jeu, le premier lieu vu (zone et coordonnées) et ses deux techniques. Cliquez un lieu vu : la carte de Tensho s'ouvre sur ce lieu ; clic droit : drapeau et éthérite la plus proche. Décochez la case : les bêtes déjà capturées reviennent, marquées.

- [ ] (0.1768) Page Dresseur, onglet « Objets temporaires » : le guide du jeu sur les cases du plateau, puis l'équipement de dressage, les objets du Dédale et les aliments, chacun avec son icône, son nom et son effet ; chaque aliment dit les familles qui peuvent le manger. Choisissez une famille : seuls ses aliments restent. Au Dédale bestial, comparez avec un marchand ou un coffre au trésor.

- [ ] (0.1769) Avec un manuel compté (page Équipement, « environ N points de bonus restants »), si le jeu retire l'effet (plafond atteint) ou si vous le retirez : une demi-minute après, la ligne n'est plus montrée et le journal dit « Grace: 45 not held at 30 readings in a row … its count is dropped » (46 pour un manuel de récolte) ; un manuel utilisé ensuite repart de son plafond entier s'il est utilisé pendant que Tensho tourne.

- [ ] (0.1770) Au Dédale bestial, panneau à côté du plateau, « Plateau entier » puis « Campements, marchands et coffres du plateau » : les cases et leurs étapes (« Étape N ») doivent correspondre à la fenêtre « Disposition du plateau » du jeu, les campements avec le même nombre de bêtes.

- [ ] (0.1770) Sur une case de campement, après un combat (fenêtre de l'équipe ouverte une fois) : le panneau dit « À reposer : … » ; comparez avec « Vous et N de vos bêtes » du jeu et les PV de la fenêtre de l'équipe (les plus blessées d'abord, pas plus que N).

- [ ] (0.1770) Ouvrez dans le bestiaire du jeu la page de trois bêtes de familles différentes : le journal dit « Beastmaster satiety checked » pour chacune (à rapporter s'il dit « differs ») ; la satiété du panneau (« Aliments de l'équipe ») est celle du bestiaire (« Satiété 0/N »).

- [ ] (0.1771) Finissez un plateau du Dédale bestial : page Dresseur, onglet de l'équipe, « Dernier plateau » : la ligne « Récompenses » (objet, icône, ×N) doit être celle de la fenêtre de résultat du jeu ; le journal dit « Beastmaster board result, rewards: item … xN » (à rapporter si la quantité diffère de la fenêtre).

- [ ] (0.1772) Mode développeur actif, entrez dans votre maison (où sont vos sonnettes) et attendez 10 s : le journal (dalamud.log) a une ligne « Housing furniture probe (X-747, developer mode): … » ; notez où sont les sonnettes dans la pièce. Rien ne change encore sur la carte.

- [ ] (0.1773) Dédale bestial, panneau à côté du plateau, section « Campements, marchands et coffres du plateau » : sous chaque marchand, la ligne « Ses offres ne sont pas lues… » ; sous chaque coffre, « Un objet temporaire au choix ; deux avec « Dague de voleur » » ; au survol, le texte du guide du jeu.

- [ ] (0.1773) Ouvrez la page d'une bête de l'équipe dans le bestiaire du jeu, puis le panneau : sa ligne d'« Aliments de l'équipe » donne la satiété de la page (« (bestiaire) »), les autres bêtes « (à confirmer…) », et chaque ligne nomme les aliments de sa famille.

- [ ] (0.1774) Faites une mission (donjon) jusqu'au bout : page Journal des gils, les gains de la fin et des coffres sont dans « Missions (récompenses) », plus dans « Autre ».

- [ ] (0.1774) Mode développeur actif, faites un gain ou une dépense que Tensho classe « Autre » : 3 s après, dalamud.log a une ligne « Gil other context: … » avec les messages et fenêtres autour, sans aucun nom de personnage, de servant, de monde ni de compagnie.

- [ ] (0.1775) Pendant que vous jouez, fenêtre de chat Tensho ouverte avec un long historique, rechargez Tensho : le chat montre ses lignes en moins de 3 s ; une minute après, dalamud.log a la ligne « Chat window in the first minute after the load (X-558): … » (combien d'images de 16 ms ou plus) et « Chat window: lines first drawn … ms after the load » (à rapporter si plus de 3000 ms ou des images de 16 ms ou plus).

- [ ] (0.1776) Chat Tensho, historique gardé : rechargez Tensho avec un autre onglet affiché, et faites-vous envoyer un message (ou dites un mot d'alerte) dans les secondes qui suivent : l'onglet qui le montre garde son non-lu et son « ! ».

- [ ] (0.1777) Réglages d'un onglet du chat Tensho qui montre les actions des autres joueurs : cochez « Cacher les téléportations des autres » ; dans une cité animée, les lignes « … prépare Téléportation » et « … utilise Téléportation » des autres joueurs ne s'affichent plus dans cet onglet ; les vôtres restent, et un onglet sans la case les montre encore.

- [ ] (0.1778) Hors combat, mangez un plat : quand il reste 5 minutes à l'effet Repas, une ligne [Tensho] « Repas : l'effet finit dans 5 min » et le son des alarmes, une seule fois ; rien de plus à la fin de l'effet.

- [ ] (0.1778) Appelez votre compagnon chocobo (légume de Gysahl) et laissez son temps descendre sous 5 minutes hors combat : « Votre compagnon chocobo part dans 5 min », une fois ; renvoyez-le avant : aucun rappel.

- [ ] (0.1778) En combat ou dans une mission, laissez le repas passer sous 5 minutes : rien pendant le combat ou la mission ; le rappel vient une fois sorti, si l'effet tient encore.

- [ ] (0.1780) Où : une zone ouverte, hors combat et hors mission. Faire : 1) sortez votre compagnon chocobo ; 2) attendez le message de Tensho dans le chat qui dit que votre compagnon part bientôt (pour ne pas attendre longtemps, montez le seuil dans les réglages des rappels de Tensho) ; 3) montez sur une monture puis descendez, et refaites-le une seconde fois. Regarder : après chaque descente, Tensho ne redit pas ce message.

- [ ] (0.1780) Réglez le rappel du compagnon à 0 min, appelez-le et montez sur une monture quand il lui reste 1 ou 2 minutes ; restez monté plus longtemps, puis descendez : « Votre compagnon chocobo est parti » une fois, une vingtaine de secondes après la descente.

- [ ] (0.1781) Ouvrez la fenêtre ALÉA du jeu (menu principal › Déplacements › ALÉA) puis la page ALÉA de Tensho, section « Rangs de réussite des ALÉA » : chaque zone vue dans la fenêtre du jeu a le même rang et le même compte « x/y ALÉA » que la fenêtre ; les zones jamais montrées disent « inconnu ». Dans dalamud.log, la ligne « Shared FATE window read (X-213) » (à rapporter si les comptes diffèrent).

- [ ] (0.1781) Survolez « n objets » d'une zone d'Endwalker ou de Dawntrail dont vous n'avez pas le rang maximal : les articles du comptoir de la zone que vous ne pouvez pas encore acheter, avec leur rang et leur prix, et le haut fait « Héros de grand chemin » ; comparez avec ce que le comptoir de la zone vous propose.

- [x] (0.1782) Rangements › Rapport, section « Valeur estimée des stocks », Universalis activé : cliquez « Estimer avec les prix du marché » : l'avancement « Prix demandés : n objets sur m » monte, puis un tableau par rangement de ce personnage et un par personnage ; les objets sans prix sont comptés à part, jamais dans la valeur ; Arrêter coupe les demandes.

- [x] (0.1782) Survolez dans un sac un objet vendable au PNJ et sur le marché, après que son prix a été lu : sous les prix du marché, « Le marché paie n fois le prix du PNJ » ou « Le PNJ paie plus que le marché ».

- [x] (0.1783) Survolez une pièce d'équipement dont vous avez la même apparence dans la coiffeuse mirage ou le bahut personnel (après les avoir ouverts une fois) : sous la partie collection, une coche « Même apparence déjà détenue : Coiffeuse mirage (nom de la pièce) » ; une pièce dont vous n'avez aucune apparence : une croix « Aucune pièce de même apparence dans vos rangements lus ».

- [ ] (0.1784) Planificateur d'équipement d'un job de combat : choisissez un repas que vous connaissez (bouton HQ ou NQ comme le vôtre) : une colonne « Avec repas » apparaît ; mangez ce repas en jeu et comparez la colonne aux stats de la fenêtre Personnage du jeu avec le même équipement (à rapporter si une stat diffère).

- [ ] (0.1786) Avec votre compagnon chocobo sorti (plus de 5 min restantes) : renvoyez-le, puis téléportez-vous vers une autre zone et restez-y plus longtemps que le temps qu'il lui restait : aucun « est parti » ne doit être dit.

- [x] (0.1786) Compagnon sorti : montez sur une monture, chevauchez un moment puis descendez : il revient ; dans dalamud.log, les lignes « Chocobo companion out of sight (X-777) » puis « Chocobo companion back (X-777) » disent le minuteur du jeu au départ et au retour (à rapporter : le minuteur a-t-il continué pendant la monture ?).

- [ ] (0.1787) Mangez un repas de combat, puis ouvrez le planificateur d'équipement du job porté et choisissez un autre repas : l'info-bulle de l'en-tête « Avec repas » nomme le repas mangé retiré, et la colonne montre vos stats avec le seul repas choisi (sans repas choisi différent, choisissez le même repas : la colonne doit égaler la colonne Prévu sans changement d'équipement).

- [x] (0.1788) Rangements › Rapport › Valeur estimée des stocks : cliquez « Estimer » ; sous les tableaux, « Dort et se vend » liste vos objets qui se vendent, avec prix vendu, ventes par jour, gils par jour et prix du PNJ ; comparez deux lignes au tableau du marché du jeu (ou à la fenêtre des prix de Tensho).

- [ ] (0.1789) Collections › Mirage : vos pièces de la coiffeuse mirage, du bahut personnel, des sacs et des servants sont listées une fois chacune, avec où elles sont ; une pièce que vous avez en double, ou deux pièces de même apparence, montre « n autre(s) » dans « Même apparence » ; essayez la recherche et le filtre d'emplacement.

- [ ] (0.1790) Planificateur d'équipement, un repas choisi : mangez un repas (ou laissez-le finir) : la colonne « Avec le repas » montre « — » une à deux secondes, puis la bonne valeur, jamais une valeur trop basse ou deux repas additionnés.

- [ ] (0.1790) Compagnon chocobo dehors : téléportez-vous, attendez qu'il revienne, renvoyez-le, puis montez plus longtemps que le temps qu'il lui restait : aucun « est parti » à la descente.

- [x] (0.1793) Rechargement du plugin : vos données sont toutes là (banque et inventaires, journal des gils, avancement des hauts faits, historique des voyages, découvertes, onglets et apparence de la fenêtre de discussion, alarmes et leur envoi Discord) ; aucune ligne « left aside » attendue dans le journal pour vos fichiers.

- [ ] (0.1794) Après un rechargement : dans dalamud.log, la ligne « Tensho data: a save took … ms with the journal folded in at its close (each save before X-750), … ms with the file kept open (X-750). » ; vos données toutes là après un second rechargement (banque, gils, découvertes).

- [ ] (0.1795) Magie bleue : cochez quelques sorts pas encore appris ; « n sort(s) voulu(s), m pas encore appris » s'affiche ; « Copier les sorts voulus, par lieu » colle une ligne par lieu ; la case « Par lieu » trie la liste par lieu (le lieu après chaque sort).

- [ ] (0.1795) Magie bleue : ouvrez la fiche d'un sort appris dans une zone (ex. une zone de La Noscea) : le bouton « Carte » ouvre la bonne carte de Tensho ; un sort appris dans une mission : le bouton « Mission » ouvre sa page ; un lieu partagé par plusieurs cartes : aucun bouton.

- [ ] (0.1797) Mode développeur, page Découvertes : « Vider la base », puis confirmez : la base est vidée, une copie backup-…-discoveries.zip apparaît dans le dossier Backups du plugin (avec Discoveries.db, sans jeton GitHub ni fichier -wal) ; rien dans le journal sur « nothing was removed ».

- [ ] (0.1806) Monde › Découvertes : cherchez un monstre ou un objet que vous avez noté ; la dernière colonne s'appelle « Joueurs » et dit « vu par un seul joueur » (ou « vu par N joueurs » pour un fait partagé par plusieurs) ; survolez ensuite cet objet ailleurs dans Tensho : sa ligne « vu en jeu » le dit aussi.

- [ ] (0.1807) Aide › Questions fréquentes › « Comment changer la langue ? » › « Ouvrir cette page » : Paramètres s'ouvre sur Général › Langue et thème (même si une autre catégorie était ouverte avant).

- [ ] (0.1807) Avec un sous-marin en voyage : Accueil › Aujourd'hui, cliquer « Prochaine échéance » (retour du sous-marin) : la page Compagnie libre s'ouvre sur l'onglet des sous-marins, pas Alarmes › Horloges.

- [ ] (0.1807) Accueil › Aujourd'hui : cliquer la ligne des alarmes ouvre Alarmes sur l'onglet de la première (par classe ou autres) ; Rappels › Minuteurs › voyages « Détails » ouvre l'onglet des sous-marins.

- [ ] (0.1808) Dédale bestial (ou avant d'y entrer), fenêtre « Formation de l'équipe » ouverte : le panneau Tensho a trois onglets « Les 10 à emmener », « Équipe actuelle », « Meilleure équipe », chacun avec sa ligne d'explication ; « Équipe actuelle » montre l'équipe lue et, par combat, ses trois bêtes et « Sans X (conseillée) » ; « Meilleure équipe » marque « pas encore à vous » les bêtes que vous n'avez pas ; campements, marchands et coffres restent repliables en dessous.

- [ ] (0.1809) Fenêtre de chat Tensho, onglet Combat, pendant un combat : vos dégâts en rouge clair, les dégâts subis en rouge soutenu, vos soins en vert clair, les soins reçus en vert soutenu ; l'icône du type de dégâts (physique, magique) devant le nombre, comme dans le chat du jeu ; Paramètres › Panneaux › Couleurs du chat › « Lignes de combat en couleur » décoché : la couleur unique revient.

- [ ] (0.1809) Fenêtre de chat Tensho : une ligne « Vous bénéficiez de l'effet ·Prospecteur » (ou un autre effet) montre la petite icône du statut à la place du « · » ; au survol, l'info-bulle du statut.

- [ ] (0.1809) Fenêtre de chat Tensho : en utilisant un objet (une potion, un repas), la ligne « Vous utilisez <objet>. » montre au survol du nom l'info-bulle commune de l'objet.

- [ ] (0.1810) Survol de la Matière sombre grade VI (ou d'un autre objet vendu par un PNJ) avec les prix du marché : « Vendu par les PNJ : … gils » (120 gils pour la matière sombre) et « Moins cher chez un PNJ que sur le marché : … gils de moins l'unité » ; la ligne « Le marché paie N fois le prix de vente au PNJ » dit bien « vente au PNJ ».

- [ ] (0.1810) Stockage › valeur, liste « Dort et se vend » : colonnes « Vente au PNJ » et « Achat au PNJ » ; un objet moins cher chez un PNJ a son prix d'achat en vert, la différence au survol.

- [ ] (0.1811) Compagnon chocobo sorti avec peu de temps (moins de 5 minutes), rappel du compagnon activé : monter et chevaucher plus longtemps que ce temps, puis descendre : le compagnon revient avec son temps, et aucun « est parti » ni « finit » faux n'est dit.

- [ ] (0.1816) Monde › Découvertes, avant de connecter GitHub : l'avertissement en rouge finit par « Ce que vous partagez reste dans la base commune, sans votre nom, même si vous arrêtez de partager. »

- [ ] (0.1817) Monde › Découvertes, connecté à GitHub, après la prochaine release qui fusionne votre ticket : « Échanger maintenant » ; sur github.com, votre ancien gist est supprimé, un nouveau ne contient que vos nouveautés et votre ticket « Discoveries » renvoie vers lui (ou, sans nouveauté, le message « Tout ce que vous avez vu est dans la base commune »).

- [ ] (0.1822) Monde › Découvertes, sans être connecté à GitHub : l'avertissement avant de connecter et l'aide de déconnexion disent que le mainteneur retire le lien avec votre nom GitHub sur demande, et que le titre du ticket et l'historique du dépôt le montrent encore.

- [ ] (0.1835) Triple Triade contre un PNJ (par exemple Munglig au Gold Saucer), avec la surimpression du conseil : le défi montre le nom du PNJ, son deck et les chances ; pendant le match, le meilleur coup est encadré comme avant

- [ ] (0.1835) Triple Triade contre un PNJ en ciblant autre chose (un autre PNJ ou personne) au moment où le match s'ouvre : aucun coup encadré, sans erreur dans le journal

- [ ] (0.1836) Après quelques changements dans les listes d'artisanat et Mes outils, le dossier de Tensho contient CraftLists.json.bak et le fichier de Mes outils avec .bak ; les pages s'affichent comme avant après un rechargement du plugin

- [ ] (0.1839) Application de bureau : dans le jeu, activer « Données du jeu et ce que vous avez observé seulement » ; dans l'application, relire les réglages puis cliquer « Fetch » sur un donjon ou la mode : la page dit que la source est coupée, aucune donnée n'arrive

- [ ] (0.1840) Marché : ouvrir le tableau des ventes d'un objet (Universalis), recharger Tensho dans les 5 minutes et rouvrir le même objet : la colonne Servant montre « — » avec son aide, « Copier … (le plus bas moins 1) » ne compte que les offres vues en jeu ; « Actualiser » remet les noms

- [ ] (0.1843) Deux clients du jeu sur le même profil, Mes outils : cocher une case ou changer un compteur dans l'un, attendre 2 s : l'autre le montre ; « + » dans l'autre part de la nouvelle valeur

- [ ] (0.1845) Mode développeur, six rechargements de suite (publications) : aucun plantage ; dans dalamud.log, dix secondes après chaque chargement, une ligne « Earlier instance … » par instance précédente et plus aucune ligne « Earlier instance … static … » ni « registrations disposed »

- [ ] (0.1846) Barre du serveur : « Tensho : ok · Dalamud N Mo » (le chiffre est nommé Dalamud) ; la carte de Tensho ouverte quelques minutes : icônes, info-bulles au survol et clic droit sur une icône comme avant

- [ ] (0.1847) Compagnie libre › Meilleurs trajets, sous-marin, une mer avec des prix manquants : « Prix du marché (N sur M manquants) » lit les prix ; s'il en manque, la ligne dit en français combien manquent (« N prix manquent : Universalis ne vend pas ces objets ou les a refusés ; les autres ont été lus. »), jamais « Prices could not be read » en anglais

- [ ] (0.1848) Stocks › « Dort et se vend » : une ligne HQ d'un objet aussi vendu par un PNJ montre le prix PNJ sans couleur verte ni info-bulle « moins cher chez un PNJ » ; la même ligne NQ garde la comparaison

- [ ] (0.1849) Sous-marins › trajets › « Prix du marché » : les prix des objets du trajet arrivent comme avant ; si une ligne dit « N manquent », un nouveau clic relit les manquants

- [ ] (0.1851) Sous-marins › trajets › « Prix du marché » : si une ligne « N manquent » paraît, N ne dépasse jamais le nombre d'objets demandés, et les prix déjà lus servent au classement sans nouveau clic

- [ ] (0.1852) Liens vers un onglet, un par famille : clic droit sur un objet › « Stocks » ouvre sa fiche sur Stocks ; Monnaies (barre du serveur ou alerte) ouvre Stockage › Monnaies ; « Valeur » d'une monnaie ouvre Stockage › Valeur d'une monnaie ; un rouleau d'orchestrion ouvert depuis un objet ouvre Collections › Musique ; un haut fait d'une source ouvre Collections › Hauts faits ; « Paramètres » d'un outil de fabrication ouvre Paramètres › Panneaux › Fabrication ; « Détails » de l'état de Tensho ouvre Paramètres › Suivi › Barre du serveur

- [ ] (0.1856) Triple Triad : depuis la carte, menu d'un PNJ de Triple Triad › outil Triple Triad : la page s'ouvre sur l'onglet « PNJ et cartes » ; depuis Paramètres › bouton Deck : onglet Deck ; depuis le menu d'une carte, « Afficher dans Triple Triad » : onglet Carnet, carte choisie

- [ ] (0.1856) Artisanat : depuis la fiche d'un objet, ouvrir sa recette en simulation puis en préparation : la page d'artisanat s'ouvre chaque fois sur le bon onglet

- [ ] (0.1859) Cartes : Paramètres › bouton « Favoris des cartes » : la page Cartes s'ouvre sur l'onglet Repères ; sur la carte de Tensho, clic droit › « Utiliser ces coordonnées dans la page Carte » et, dans la page Monstres, le lieu d'un monstre sans position : la page Cartes s'ouvre sur l'onglet Cartes, même si elle était fermée ou sur un autre onglet

- [ ] (0.1859) Carte de Tensho : l'engrenage ouvre Paramètres sur l'onglet Carte, à la catégorie choisie en dernier

- [ ] (0.1859) Compagnie libre : onglet « Secteurs d'aéronef » › « Planifier les trajets d'aéronef » ouvre l'onglet « Meilleurs trajets » ; là, « Montage » d'un trajet ouvre l'onglet Montages

- [ ] (0.1861) Menu d'un objet (clic droit dans l'inventaire) › Prix du marché, après avoir laissé la page Marché sur l'onglet Historique : la page Marché s'ouvre sur l'onglet Résumé

**0.535 : interfaces** — à voir en jouant

- [x] Carte › Paramètres : « Sans barre de titre » : plus de barre ; elle se déplace par sa colonne d'outils et se ferme avec la touche de la carte. Le déplacement par la colonne reste à confirmer.

**0.534 : Équilibré grisé quand il revient au même** — à voir en jouant


**0.533 : les coups de chaque cran** — à voir en jouant


**0.532 : panneau Collectionnable sans répétitions** — à voir en jouant


**0.531 : description chiffrée des crans** — à voir en jouant


**0.529 : trois crans de risque des collectionnables** — à voir en jouant

- [x] Panneau Collectionnable (0.530) : trois icônes, main, balance, dé ; le nom au survol ; le cran choisi coloré, sa description dessous ; même choix dans Paramètres › Modules › Récolte.

**0.527 : Toucher d'amorçage dans le plan des collectionnables** — à voir en jouant

- [x] Botaniste ou mineur niveau 95 et plus : commencer vers 623 PR, lancer Inspection soi-même (il reste 423 PR) : le panneau propose Toucher d'amorçage ; une fois lancé, il ne le repropose plus et compte 48 %. Depuis 727 PR, le plan passe par Inspection puis Sélection méthodique, sans Toucher : c'est prévu (capture du 2026-11-05).
- [x] (rapport de l'utilisateur, 2026-11-05) Toucher reste actif si une autre action passe avant la Sélection méthodique ; il ne retombe qu'après elle. Récolte non essayée.

**0.526 : légendes locales dans les Collections** — à voir en jouant

- [x] (rapport de l'utilisateur, 2026-11-04, « legendes local ok ») Collections › Légendes locales : 36 tomes, marqués lus ou non comme dans votre personnage. Si un tome est mal marqué, la correspondance des bits est fausse : la carte se trompe aussi.
- [ ] (0.526) Collections › onglet « Légendes locales » (tomes de mineur, botaniste, pêcheur) : survoler un tome, puis clic droit dessus : on voit comment l'obtenir.

**0.525 : groupe sur la carte, coût du chargement** — à voir en jouant

- [x] (rapport de l'utilisateur, 2026-11-04, « groupe parfait ») Membres du groupe sur la carte : déplacement fluide quand ils sont en vue ; à distance, ils glissent entre deux positions ; une téléportation saute d'un coup.

**0.524 : Ctrl+Alt actualise le prix survolé** — à voir en jouant

- [x] (rapport de l'utilisateur, 2026-11-04, « ctrl alt ok ») Survoler un objet vendable, attendre le prix, Ctrl+Alt : les secondes de « Récupéré à » changent. Maintenu : une seule demande. Relâché et rappuyé : au plus une toutes les 5 s.

**0.523 : assignats des collectionnables d'un nœud** — à voir en jouant

- [x] Sur un nœud à deux collectionnables : les assignats par palier sous chacun, identiques au tableau Récolte › Collectionnables, et « le plus au palier haut » sur le bon (rapport de l'utilisateur, 2026-11-04).
- [ ] Deux objets aux types d'assignats différents : la phrase « types différents », sans marquage.

**0.522 : temps de recharge de Bénédiction de la nature** — à voir en jouant


**0.520 : tentative gratuite dans le plan des collectionnables** — à voir en jouant

- [x] Niveau 90 et plus, PR pour Sagesse du fermier ou Raison solide : la moyenne dépasse le pire cas, et la ligne « 50 % de chances d'une tentative gratuite » apparaît.

**0.519 : éclats, cristaux et agrégats** — à voir en jouant

- [x] Éclat, cristal ou agrégat : le panneau propose Bénédiction de la nature et Bénédiction des Douze (0.521), sans « ≈ N objets ».

**0.518 : carte, tomes et niveau d'un point** — à voir en jouant

- [ ] Point d'Ostrakon Dékaokto : « ouvert, mais pas encore pour vous », le nom du tome, horloge grise. Le journal écrit « Gathering knowledge: … » : les nombres entre crochets correspondent-ils aux tomes lus ?

**0.517 : plan de PR** — à voir en jouant

- [ ] (0.517) Mineur ou botaniste, devant un point de récolte, PR au maximum : le panneau de récolte de Tensho, à côté de la fenêtre du jeu, propose un plan d'actions. Chaque action proposée est une action que votre personnage a apprise (sur vos barres ou dans la liste des actions du jeu).

**0.516 : panneaux accrochés au jeu en fenêtres Dalamud (#38)** — à voir en jouant

- [ ] Chaque panneau s'affiche au même endroit, avec le même contenu : carnet d'artisanat, projection pendant une synthèse, récolte, collectionnable, pêche, harpon, pêche en mer, survol d'objet, outil de mission, Revue de mode, Personnage, ventes des servants, demande de Triple Triade, import de banque (si la capture automatique est coupée), bouton « Trier ».
- [ ] (0.516) Ouvrir l'inventaire du jeu : le bouton « Trier » de Tensho, sur la fenêtre, n'a ni fond ni bord, et un clic range les sacs. Survoler un objet : le panneau Tensho sous l'infobulle ne prend pas les clics (un clic dessus va au jeu).

**0.515 : aide à la récolte, plan du nœud et effet du lieu** — à voir en jouant

- [ ] (0.515) Point de récolte avec l'effet « PR ≧ 770 → tentatives +1 » (comme sur la capture du 04/11) : avec 809 PR, le panneau de récolte de Tensho propose un plan d'actions et une ligne « Effet du lieu actif : tentatives +1, déjà compté dans le plan (5 au lieu de 4) ».

**0.514 : panneaux libres en fenêtres Dalamud (#38)** — à voir en jouant

- [x] Fenêtre de suivi (réglage) : s'affiche à sa place habituelle, liste les alarmes, le nom ouvre l'objet, « Carte » ouvre le point ; la croix la ferme et décoche le réglage ; recocher la rouvre.
- [x] Barre de PR en mineur : se remplit, le repère du tic avance ; disparaît à PR pleins et sur un autre job ; PR mesurés même barre coupée.
- [x] Guide et carte : la croix les ferme du premier coup.

**0.510 : conseils de Triple Triade en JcE seulement** — à voir en jouant

- [ ] Match contre un PNJ : conseils comme avant. Au Battlehall, en tournoi ou contre un joueur : pas de cadre, et le panneau dit pourquoi.

**0.509 : coup conseillé pendant une synthèse (A-04)** — à voir en jouant

- [x] Dans « Comparer les prochains coups », pendant une synthèse : « Conseillé : … » change à chaque coup et suit l'état (sur Bon, un coup de cet état devrait souvent sortir, sans que ce soit une règle). Rien n'est joué. Réglage dans Paramètres › Modules › Artisanat. Pas de saccade.

**0.508 : ventes des servants (I-05)** — à voir en jouant, à la sonnette

- [x] Ouvrir la liste de ventes d'un servant : le panneau « Vos ventes » s'affiche à côté, avec objets, quantités et prix comme le jeu, HQ compris.

**0.506 : vidéos et guides de la communauté (ONBOARD-001, lots 7 et 8)** — après une release

- [ ] (0.506) Aide (ou le « ? » d'une page) : cliquer « Vidéos et guides » : « Pas encore de vidéo ni de guide pour cette page ». Quand une ligne sera ajoutée à la liste et publiée : son titre, l'adresse au survol, la question de confirmation, puis le navigateur s'ouvre.

**0.505 : guide des premiers pas (ONBOARD-001, lots 4 à 6)** — à voir en jouant

- [x] Sur l'accueil, joueur existant : une proposition unique, « Non merci » la retire pour de bon. Vraie première installation : il s'ouvre seul à la première ouverture de Tensho (à tester avec une configuration vide ou par un testeur).

**0.504 : signaler un problème (ONBOARD-001, lot 3)** — à voir en jouant


**0.502 : aide (ONBOARD-001, lot 1)** — à voir en jouant

- [ ] (0.502) Le « ? » à côté du titre de la fenêtre Tensho explique la page ouverte, sur toutes les pages ; « Toute l'aide » ouvre la page Aide.
- [x] Page Aide : la recherche trouve une page d'après ce qu'elle fait ; « Ouvrir cette page » y mène ; questions fréquentes.
- [ ] 0.503 : première ouverture d'une page, un bandeau de deux lignes ; « Compris » le retire pour de bon ; « En savoir plus » ouvre l'aide. Réglage dans Paramètres › Général. Le « ? » dit ce qui est lu ou non sur ce personnage.

**0.501 : sonde des fenêtres du dresseur** — à faire en jeu, mode développeur


**0.500 : page Dresseur** — à voir en jouant

- [ ] (0.500) Personnage › Dresseur › Bêtes : 50 bêtes avec nom, classe et attaque auto., comme le bestiaire de dresseur du jeu. Choisissez « Rang » 6 : Cu sith = FOR 161, INT 100, Rés. phys. 90, Rés. mag. 86, VIT 131. Comparez aussi une autre bête au bestiaire du jeu.

**0.499 : la carte au-dessus de Tensho** — à voir en jouant

- [x] Depuis une page Tensho, cliquer un lien vers la carte (« Voir sur la carte », favori d'une autre carte) : la carte passe devant Tensho, qu'elle soit déjà ouverte ou non. Cliquer ensuite sur Tensho le remet devant.

**0.498 : Tensho replié, puis demandé** — à voir en jouant

- [x] Replier la fenêtre Tensho par sa barre de titre, puis ouvrir quelque chose qui la demande (clic droit sur un objet › outil, `/tensho`, alarme) : elle se déplie sur la bonne page. Elle se replie encore normalement ensuite.

**0.497 : état de la synthèse dans « Comparer les prochains coups »** — à voir en jouant


**0.494 : auto-contrôle** — à voir en jouant

- [x] `/tensho selftest` et le bouton de Diagnostics relancent une passe ; Diagnostics montre les lignes.
- [x] 0.496 : fenêtre ouverte puis refermée, sans lancer de passe : la passe suivante donne OK_SEEN avec l'heure. Ça reste après un rechargement.

**0.493 : disponibilité des PNJ de Triple Triade** — à voir en jouant

- [x] Triple Triade › PNJ et cartes : colonne « Disponible ». Un PNJ marqué « Quête requise » refuse bien le défi ou n'est pas là en jeu, et un PNJ marqué « Oui » accepte. Le survol donne les quêtes, avec ✓ pour celles faites. À signaler surtout : un PNJ à deux quêtes dont une seule est faite (lecture « toutes » déduite, voir docs/evidence/0.493-triad-npc-requirements.md).

**0.491 : R-14, valeurs inconnues** — à voir en jouant

- [x] Défi Triple Triade et match : règles et conseils comme avant, jamais « les règles n'ont pas pu être lues ». Le journal donne la ligne `Triad request rule values` (preuve à verser dans docs/evidence, défis à 1, 2 et 4 règles si possible).
- [x] Coffre à apparence ouvert avec une tenue complète : ses pièces comptent toujours comme possédées. Avec une tenue partielle : seules les pièces rangées comptent. Le journal donne `Dresser outfit … slots held` (preuve à verser dans docs/evidence).

**0.490 : onglet Favoris de la carte** — pendant la campagne de tests en jeu (phase 6)

- [x] Clic droit sur la carte Tensho : « Favoris » est le premier onglet, avant Quêtes et ALÉA. Les favoris de cette carte d'abord, puis les autres. Un clic pose le drapeau ou ouvre l'autre carte ; « Retirer » enlève le favori et son étoile.

**0.489 : temps d'affichage de la carte** — pendant la campagne de tests en jeu (phase 6)

- [x] Mode développeur, carte Tensho ouverte : Diagnostics a une ligne « Map », et elle reste une fois la carte fermée.

**0.488 : clignotement, Raison solide, temps avant le plein** — pendant la campagne de tests en jeu (phase 6)

- [x] Panneau Collectionnable : il ne clignote plus au début ni entre deux actions.
- [x] Raison solide : conseillée seulement à la dernière tentative, puis le jeu l'accepte. Sinon le panneau dit d'attendre.
- [x] Barre de tic : quelques secondes après la remontée des PR, le temps avant le plein correspond à l'attente réelle. Paramètres › Modules › Récolte donne la base mesurée et les traits de chaque métier ; comparer sur deux métiers.
- [x] Aides : une action grisée dans la fenêtre du jeu ou non apprise par le métier n'est jamais conseillée (Toucher d'amorçage sous le niveau 95, action de quête non faite).

**0.486 : barre de tic** — pendant la campagne de tests en jeu (phase 6)

- [x] La barre se remplit vers les PR pleins, une petite marque dorée défile en bas à chaque tic, le texte donne le temps avant le plein. Comparer ce temps à l'attente réelle. Tant que les PR par tic ne sont pas mesurés, le texte reste « tic dans… ». La bulle donne le prochain tic.

**0.486 : temps d'affichage des panneaux** — pendant la campagne de tests en jeu (phase 6)

- [x] Mode développeur, Diagnostics : la section « panneaux à côté du jeu » liste le panneau de récolte, Collectionnable et les autres une fois ouverts, et leurs temps restent après la fermeture (0.487).

**0.484 : risque accepté** — pendant la campagne de tests en jeu (phase 6)

- [x] Barre de tic : à l'ouverture d'une fenêtre de récolte, après quelques secondes, elle affiche « pause » ; en la quittant, elle repart. Paramètres › Modules › Récolte montre les PR par tic mesurés.
- [x] Carte Tensho ouverte, plugin rechargé : elle revient seule, sans M deux fois. Carte du jeu hors combat : pas d'autre effet.
- [x] Panneau Collectionnable : « Calcul du plan… » apparaît à peine, le jeu ne se fige pas au changement de curseur.

- [x] Panneau Collectionnable, curseur « Risque accepté » : à 0 %, plus de Sélection instinctive sans nécessité et un « au pire » élevé ; à 100 %, le plan vise la meilleure moyenne. Le réglage reste après rechargement, et existe aussi dans Paramètres › Modules › Récolte.

**0.483 : Raison solide selon le jeu** — pendant la campagne de tests en jeu (phase 6)

- [ ] Plan avec Raison solide : quand les tentatives sont sous le maximum et que le jeu la refuse, elle n'est plus conseillée ni comptée. Mode développeur : la ligne `Collectable restore action` du journal donne la réponse du jeu.

**0.482 : ordre des actions du plan** — pendant la campagne de tests en jeu (phase 6)

- [x] Début de passe avec des PR : Inspection rigoureuse est conseillée d'abord si le plan en a besoin. Le gain affiché est le gain réel, pas celui de la case du jeu.

**0.481 : Inspection, Œil et Intuition comptés** — pendant la campagne de tests en jeu (phase 6)

- [x] Cocher une fois « Avec Inspection rigoureuse » et « Avec Intuition » : les deux avertissements disparaissent et le plan change. Les cases peuvent rester cochées, le conseil ne change pas.
- [x] Inspection rigoureuse conseillée quand elle rapporte plus ; après usage, l'action suivante n'est pas une deuxième Inspection et le gain affiché la contient.
- [x] Œil du collectionneur conseillé quand il rapporte plus ; après usage, le pourcentage d'Intuition du jeu monte et il n'est plus conseillé.
- [x] Panneau plus court, méthode dans la bulle de « Comment est-ce calculé ? ».

**0.480 : bonus dans le plan de collection** — pendant la campagne de tests en jeu (phase 6)

- [x] Cocher « Avec Inspection rigoureuse » puis décocher, même chose pour « Avec Intuition » : les messages disparaissent, le plan ne change pas tant qu'une case reste cochée.
- [x] Plan avec Inspection : elle est conseillée seulement si elle rapporte plus ; les PR annoncés sont justes. Une fois utilisée, le gain affiché la contient et le conseil suivant passe à l'action.
- [x] Sélection méthodique à 8 % : plus conseillée pour son pourcentage. Comparer les conseils à votre expérience, et me donner les chiffres du panneau si l'un paraît faux.
- [x] Plus de plantage en mode collectionnable, y compris à la valeur maximale avec une Sélection méthodique.
- [x] Référence affichée : palier haut, assignats, ce qui manque. À la valeur voulue avec des PR restants : Raison solide conseillée pour une récolte de plus, et le jeu l'accepte.
- [x] Œil du collectionneur : proposé en option quand une Intuition atteindrait le palier suivant.
- [x] Mode développeur : le journal `Collectable window values` contient le pourcentage d'Intuition.

**0.479 : plan de collection, PR et bonus de nœud** — pendant la campagne de tests en jeu (phase 6)

- [x] Fenêtre Collectionnable : barre de collectabilité juste, palier juste, prochaine action avec icône et gain ; le plan se refait après chaque action. Comparer les gains affichés à ceux du jeu.
- [x] Plus aucune chance d'atteindre le seuil bas (par exemple 0 avec une seule tentative) : le panneau le dit, sans parler de tirage.
- [x] Bien noter si un conseil paraît mauvais, avec les chiffres du panneau.
- [x] Liste de fabrication : nœud normal, « récolte simple » et PR gardés ; un nœud avec une tentative ou une quantité de plus : « Bonus majeur », les PR se dépensent. Décocher le réglage redonne le partage égal.

**0.478 : retours du lot 6, récolte** — pendant la campagne de tests en jeu (phase 6)

- [x] Fenêtre Collectionnable sur « Minerai de durium noble collectionnable » : nom complet, seuils 400 / 700 / 1000. Rien à faire de plus. En mode développeur, faire quelques actions : le journal note les valeurs de la fenêtre.
- [x] Aide à la récolte : plus de texte coupé ; le nom, puis les taux dessous. Sur un collectionnable : pas de « plan non calculé » mais un message clair.
- [x] Récolte › onglet Collectionnables : tableau trié en cliquant sur chaque colonne, dans les deux sens.
- Plan réparti sur la liste de fabrication : retiré en 0.515 (DEC-036) ; la ligne « il en manque N » reste.
- [x] Paramètres › Modules › Récolte : actions interdites jamais proposées (panneau et page PR) ; PR par tic mesuré sur un nœud quand les PR remontent.

**0.476 : thème proposé par un outil** — TEMPLATES-001

- [x] Éditeur : choisir un thème proposé, enregistrer. Le thème ne change pas tant qu'on n'a pas cliqué « Utiliser le thème… ». Ensuite, en haut de Mes outils, « Revenir au thème d'origine » remet l'ancien (0.477).
- [x] Coller un outil avec `"settings":{"theme":"light"}` : l'aperçu le dit. Avec un autre réglage : refusé.

**0.475 : galerie de Mes outils** — TEMPLATES-001

- [ ] (0.475) Tensho › Mes outils › Galerie : cliquez « Charger la liste », puis « Voir » sur un outil : son contenu s'affiche en entier ; « Installer » l'ajoute à vos outils.

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

**0.470 : liste unique des pages** — pendant la campagne de tests en jeu (phase 6)

- [x] Navigation : mêmes groupes, mêmes pages, même ordre qu'avant ; chaque page s'ouvre. Artisanat reste sélectionné sur Synthèse en cours et Séquences, Récolte sur PR, Collection sur Magie bleue. Diagnostics seulement en mode développeur.

**0.469 : demande du jour de la Grande Compagnie, icônes du carnet** — pendant la campagne de tests en jeu (phase 6)

- [x] Ouvrir le calendrier du jeu puis les missions de ravitaillement : la bulle d'un objet demandé dit « demandée aujourd'hui ». Un autre objet possible dit « absente des demandes du jour que vous avez ouvertes ». Le journal Dalamud donne une ligne `Grand Company supply window`.
- [x] Carnet : panneau en sections (recette, votre état, cible, calcul, macro), chiffres en tableau, barre de qualité verte si la cible est atteinte ; explications dans la bulle de « (?) ».

**0.468 : carnet (cible, état, meilleure séquence) et cache de Raphael** — pendant la campagne de tests en jeu (phase 6)

- [x] Raphael choisi, carnet : une recette calculée puis re-sélectionnée revient tout de suite, le tampon dit « Cache disque ». Après un changement d'option de macro ou une nouvelle version de la DLL : « Solveur : Raphael (séquence déjà calculée…) ».

**0.467 : Raphael dans le carnet** — pendant la campagne de tests en jeu (phase 6)


**0.465 : solveur Raphael** — pendant la campagne de tests en jeu (phase 6)

- [x] Artisanat › Recherche : choisir « Raphael », lancer : une séquence arrive, « Annuler » fonctionne ; le jeu reste fluide pendant le calcul.
- [ ] La séquence jouée en jeu donne le résultat annoncé. Retour à « Tensho » : la recherche d'avant.
- [x] Le plugin se recharge sans erreur après un calcul.

**0.464 : retours du lot 5** — pendant la campagne de tests en jeu (phase 6)

- [ ] Simulateur, métier porté : attributs copiés d'office, ligne « Détecté » juste (repas, remède, Manipulation, spécialiste) ; valeurs en vert ou rouge selon la recette ; « Conserver la séquence » après la palette.
- [x] Onglet « Liste de fabrication » : icônes, infobulle complète au survol, manquants en rouge.
- [x] Pendant une synthèse : la fenêtre de projection s'ouvre à côté de celle du jeu, se ferme avec elle, et ses chiffres correspondent au résultat de l'action jouée.

**0.462 : retours du lot 3** — pendant la campagne de tests en jeu (phase 6)

- [x] Clic droit sur un objet › Tensho : plus de « Tous les outils avec cet objet » ; le reste du menu est là.
- [x] Ouvrir le coffre de compagnie, puis tous ses onglets : Banque › Coffre de compagnie liste les objets, plus de message « ordre visuel » (0.463) ; les quantités sont justes.

**0.461 : grille du journal** — pendant la campagne de tests en jeu (phase 6)

- [x] Monde › Jardinage : le potager est une grille ; la plante est dans la bonne case par rapport au vrai potager (emplacement 6 en bas au milieu) ; sinon, dire où elle est.

**0.460 : plantations détectées depuis le chat** — pendant la campagne de tests en jeu (phase 6)

- [x] Planter dans un potager : une ligne « Jardin : … noté dans le journal » arrive ; la page Jardinage montre le bon potager, le bon emplacement, la bonne graine et les durées.

**0.459 : icône des terminés, page Jardinage** — pendant la campagne de tests en jeu (phase 6)

- [x] Barre du serveur : trois icônes au plus : horloge (alarmes actives), rappels (reste à faire), terminés (visible seulement s'il y en a) ; chaque infobulle a sa liste.
- [x] Monde › Jardinage : le journal et les croisements y sont, ils ne sont plus dans Rappels ; les parcelles déjà notées sont toujours là.

**0.458 : icône des rappels, ce qui reste à faire** — pendant la campagne de tests en jeu (phase 6)

- [x] Barre du serveur : le nombre et l'infobulle « Reste à faire » listent les rappels non cochés ; cocher « Fait » sur la page Rappels fait baisser le nombre (5 s au plus).
- [x] Décocher « Barre » sur une ligne : elle disparaît de l'infobulle ; recocher : elle revient ; le choix reste après un rechargement du plugin.

**0.457 : icônes des compétences** — pendant la campagne de tests en jeu (phase 6)

- [x] Réglages › Artisanat › Actions interdites : chaque ligne a son icône, et la case se coche toujours.

**0.456 : plan de plantation des croisements** — pendant la campagne de tests en jeu (phase 6)

- [x] Croisements › « Ce qui donne cette graine » : les paires à « seulement cette graine » sont en tête ; le survol donne les autres graines.
- [x] Clic sur une paire : grille 3 × 3 sans centre ; la disposition correspond à votre potager de luxe (sinon, dire comment il est) ; noms des graines et de la terre en français.

**0.455 : retours du lot 1** — pendant la campagne de tests en jeu (phase 6)

- [x] Journal de l'or (Stockage › Gils) : tableaux lisibles, plus de « = » ; une dépense apparaît en « - ».
- [x] Barre du serveur : deux icônes, l'horloge des alarmes (nombre d'alarmes actives) et celle des rappels (nombre de terminés) ; survol = la
  liste ; clic = la bonne page ; le réglage Avancé masque celle des rappels.

**0.454 : R-14, indisponible plutôt que « lecture »** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Inventaire d'un servant : l'ordre affiché suit toujours celui du jeu.

**0.453 : R-14, valeurs inconnues** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Journal de l'or : à la connexion, aucune fausse entrée « + tout l'or » ; un achat est bien noté.
- [ ] Rappels à l'écran titre ou de sélection : pas de zéros ni de rôle ; une fois connecté, les compteurs apparaissent.
- [x] Prix de sous-cotation : après une sonnette, le prix est copié ; sans aucune sonnette depuis le lancement du jeu, rien n'est copié.
- [x] Revue de mode : les catégories s'affichent comme avant.
- [x] Téléportation : coûts affichés ; mandats, collections supplémentaires et chocobo lus une fois connecté.

**0.452 : R-14 et hooks (A-14)** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Alarme sur un objet : la laisser sonner deux fois de suite ; la ligne de chat, le son et Discord arrivent à chaque sonnerie.
- [ ] Try On depuis le menu d'un objet avec un personnage chargé ; à l'écran de sélection du personnage, rien ne se passe.
- [x] Musique en cours (orchestrion) et liste des cartes de l'éditeur de Triple Triade toujours lues.

**0.451 : N-06 missions d'escouade (sonde en jeu)** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Hors de la caserne : « Lire l'escouade » affiche « L'escouade n'est pas chargée… ».
- [x] À la caserne, escouade ouverte : « Lire l'escouade » liste les 8 membres (classe, niveau, « occupé » pour ceux en mission ou à
  l'entraînement) et le bonus d'entraînement ; comparer avec la fenêtre du jeu.
- [x] Attributs d'un membre : comparer « P / M / T » affiché avec la fiche du membre dans le jeu (classe, niveau). Un écart prouverait que
  les colonnes de GcArmyMemberGrow sont mal lues, ou que les affinités ou l'équipement comptent.
- [x] Choisir la mission de la fenêtre du jeu, puis le jeu d'exigences qui correspond à ses valeurs ; former le groupe proposé dans le
  jeu et comparer ses totaux et le taux de réussite affiché par le jeu avec « ≈ … % ».
- [x] « Écrire dans le journal » : copier les lignes « Squadron probe: » du journal de Dalamud dans docs/evidence/ (sonde N-06).

**0.450 : N-14 croisements** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Rappels › Journal de jardinage › Croisements : « Lecture des paires… » puis la liste ; les graines sont nommées dans la langue
  du jeu ; « Ce qui donne cette graine » et « Ce que cette graine peut donner » montrent des paires cohérentes avec un croisement connu.
- [x] La ligne grise annonce 3 768 paires et 1 nom écarté (« Gysahl Green Seeds » dans l'infobulle) ; le journal de Dalamud le dit aussi.

**0.449 : liste des graines du journal de jardinage** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Rappels › Journal de jardinage › « Liste des graines » : les noms sont dans la langue du jeu ; la recherche filtre ; choisir une
  graine remplit son nom, la pousse et l'entretien (par exemple 120 h et 48 h pour les graines de paprika) ; les valeurs restent modifiables.

**0.448 : N-08 journal de jardinage** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] (0.448) Jardinage, « Me le rappeler dans le chat » coché : attendre un rappel du jardin dans le chat (« Jardin : … est mûr » ou un entretien), puis redémarrer le jeu : ce rappel ne revient pas.

**0.447 : décisions du 2026-11-03 (DEC-026)** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Page Événements : « Événements de joueurs (partake.gg) » ouvre le site dans le navigateur.
- [x] Réglages : la case « Chercher les monstres sur le wiki au survol » n'existe plus ; le panneau au survol montre toujours ses sources.
- [x] Tri : déplacer un objet dans les sacs pendant que la page Tri est ouverte : l'éditeur de règles reste affiché (ligne « Rangement
  modifié »), l'aperçu revient tout seul.

**0.446 : N-09 alerte des marques, X-13 annonce** — pendant la campagne de tests en jeu de la fin (phase 6)


**0.445 : N-05 terrains en vente** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Cartes › Terrains en vente › « Consulter » sur un monde NA : la liste correspond au jeu (panneau de vente des quartiers) ; cocher
  la surveillance : une parcelle nouvellement libre est annoncée dans le chat à la lecture suivante.

**0.444 : X-09 annuaire des lieux** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] (0.444) Page Carte › onglet Repères › « Annuaire des lieux » : ajoutez une maison (monde, quartier, secteur, parcelle) puis un appartement ; « Copier » marche ; le lieu est toujours là après un rechargement du plugin et après « Tout remettre par défaut ».
  un rechargement du plugin et après « Tout remettre par défaut ».

**0.443 : N-02 tâches du Livre d'or** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Rappels › Livre d'or : la liste « À faire » correspond aux cases du journal ; une tâche finie passe en « à réclamer » (vert), puis
  disparaît une fois la vignette posée.

**0.442 : N-15 train de chasse** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Pendant un train : « Enregistrer », chaque marque A/S apparaît une fois avec sa position ; « Drapeau suivant » ; « Copier la
  liste » puis coller dans l'import des favoris : les points tombent au bon endroit.

**0.441 : N-09 marques de chasse** — pendant la campagne de tests en jeu de la fin (phase 6)


**0.440 : N-01 contrats de chasse** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Avec un contrat de chasse en main : Rappels montre ses cibles et les victimes (comparer au journal des contrats du jeu) ; tuer une
  cible fait monter le compteur ; « Carte » ouvre la bonne zone.

**0.439 : N-03 probabilités du Livre d'or** — pendant la campagne de tests en jeu de la fin (phase 6)


**0.438 : actions interdites, équiper depuis le carnet** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Interdire Œil de l'expert et Manipulation : ni la recherche, ni l'assistant du carnet, ni la synthèse en cours ne les proposent ;
  grisées dans le simulateur. Carnet ouvert avec le mauvais métier : « Équiper (métier) » change d'ensemble et le calcul démarre.

**0.437 : N-11 coordonnées collées** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Coller une liste de Siren ou Faloop dans l'import des favoris : un favori par ligne, au bon endroit sur la carte ; lieux inconnus
  comptés dans le message.

**0.436 : A-14 enregistreur de rotation** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] « Enregistrer », fabriquer à la main (avec un buff, une action de qualité, une de progression), « Arrêter » : la liste est
  exacte et dans l'ordre ; « Utiliser comme séquence » ; le jeu se comporte comme d'habitude pendant l'enregistrement (aucune action
  bloquée) ; après « Arrêter », rien n'est plus noté.

**0.435 : A-07 recettes expertes** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] (0.435) Recette experte dans le simulateur : elle est acceptée et la ligne dit « Recette avancée : chaque étape est simulée en état Normal ». Pendant une synthèse experte en jeu, en mode développeur, notez l'état brut affiché pour chaque condition spéciale (modulable, robuste, malléable, apprêté).
  mode développeur : noter la condition brute affichée pour souple, robuste, malléable, amorcée (pour ouvrir la projection en direct).

**0.434 : A-13 actions du simulateur** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Comparer au jeu : Appréciation finale (progression arrêtée à 1 de la fin), Synthèse intensive / Toucher précis après Cœur et âme
  (spécialiste), Innovation éclair, Ficelles du métier (+20 PS). Synthèse en cours avec Appréciation finale active : projection affichée.

**0.433 : A-05 listes de fabrication** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Créer une liste, y ajouter deux recettes depuis Préparation, changer une quantité, retirer un objet ; les ingrédients totaux et
  « à compléter » sont justes ; la liste est toujours là après un rechargement du plugin ; lien Teamcraft de la liste.

**0.432 : A-15 collectionnables des sacs** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Avec des collectionnables dans les sacs (récoltés et fabriqués) : « Dans vos sacs » montre leur collectionnabilité comme le jeu,
  le bon palier et les assignats du marchand.

**0.431 : A-06 bénéfice d'artisanat** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] (0.431) Artisanat › onglet Préparation, cliquez « Consulter les prix de la liste » : sous « Bénéfice », le prix de l'objet fabriqué et la dernière vente ; cochez « Prix du centre de traitement de données » : le prix d'un autre monde s'affiche avec son nom ; bénéfice = prix × quantité − achats.
  case « Prix du centre de données » : autre monde affiché ; bénéfice = prix × quantité − achats.

**0.428-0.429 : recette refusée par le carnet du jeu** — pendant la campagne de tests en jeu de la fin (phase 6)



**0.427 : A-12 prérequis de la recette** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Page Artisanat : niveau, Habileté et Contrôle en vert / rouge selon le personnage ; métier non porté : « ? » et « Équiper (métier) »
  porte le bon ensemble.

**0.426 : fermer Tensho en ouvrant une fenêtre du jeu** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] « Ouvrir dans le carnet d'artisanat » : Tensho se ferme, le carnet s'ouvre sur la recette ; recommencer avec le carnet déjà
  ouvert : il reste ouvert et passe à la recette. Même chose pour le carnet de récolte et la carte du jeu. Réglage décoché : Tensho
  reste ouvert. « Y aller » : Tensho reste ouvert (question de téléportation).

**0.425 : A-08 import de macro** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Simulateur › « Importer une macro » : coller une macro du jeu ou de Teamcraft (en français), « Importer » ; la séquence est
  reprise et simulée ; une action mal écrite apparaît dans « Noms inconnus ».

**0.424 : Préparation, stocks et « Retour à l'objet »** — pendant la campagne de tests en jeu de la fin (phase 6)


**0.423 : lien Artisanat et A-09 Teamcraft** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Menu d'un objet › « Artisanat : (objet) » : la page s'ouvre sur l'onglet Préparation de sa recette. Préparation › « Ouvrir dans
  Teamcraft » : Teamcraft propose d'importer la liste avec l'objet et la quantité.

**0.422 : A-10 Macro Chain** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Macro Chain chargé, macros enchaînées : la dernière ligne de chaque page est « /nextmacro » et la suite s'exécute ; Macro Chain
  désactivé : retour au marqueur de Tensho.

**0.421 : A-11 recettes jamais fabriquées** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Fiche d'un objet › Craft lié : une recette déjà faite montre « ✓ déjà fabriquée », une jamais faite « jamais fabriquée » (comparer
  au carnet du jeu) ; livre des recettes, case « Jamais fabriquées seulement ».

**0.420 : A-02 repas et remède** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Simulateur : choisir un repas HQ et un remède ; les attributs affichés correspondent à ceux du personnage une fois le repas
  et le remède pris en jeu (sans autre bonus).

**0.419 : A-01, X-11, X-12 (artisanat)** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Recette d'un collectionnable : boutons « Bas / Moyen / Haut » dans la recherche, la rotation trouvée atteint le seuil en jeu.
  Assistant du carnet avec Manipulation apprise : la macro proposée peut l'utiliser. Réglage « Attente ajoutée aux buffs » : les
  lignes de buffs de la macro changent seules.

**0.418 : R-07 pêche en mer** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Pendant une traversée : le panneau montre l'escale, le moment, les poissons avec appât et touche ; pendant un courant spectral,
  le message et les poissons spectraux ; missions avec avancement ; points ; escale suivante. Comparer à l'interface du jeu.

**0.417 : R-15 aide au harpon** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Pêche au harpon : le panneau est à côté de la fenêtre ; pour chaque poisson, la taille et la vitesse ; le bon poisson de la zone
  en couleur. Mode développeur : noter la vitesse brute et la comparer à la vitesse affichée (valeurs GatherBuddy 100 à 600).

**0.416 : R-14 liste de récolte par zone** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Artisanat › Préparation d'une recette à plusieurs matières récoltées › « Calculer la chaîne » › « Liste de récolte par zone » :
  zones dans l'ordre, une ligne par matière, horaire, « Y aller » ; matières achetées listées à part.

**0.415 : R-13 fenêtre de suivi** — pendant la campagne de tests en jeu de la fin (phase 6)


**0.414 : R-12 alarme cliquable** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Une alarme Tensho d'objet récolté sonne : dans le chat, le lien de carte ouvre la carte du jeu sur le point ; « Ouvrir dans
  Tensho » ouvre la fiche de l'objet (onglet Récolte). Une alarme sans objet : message sans lien.

**0.413 : R-11 tableau des collectionnables** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Page Récolte › « Collectionnables » : comparer quelques lignes (seuils, assignats) au marchand d'assignats du jeu ; filtre
  « Nv 91 » ; clic sur un objet ; vue Récolte d'un objet collectionnable : ses seuils sous son nom.

**0.411 : R-10 équiper le métier** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Fiche d'un objet récolté › lieux : « Équiper Mineur » (ou Botaniste, Pêcheur) porte l'ensemble de ce métier au plus petit numéro ;
  sans ensemble pour ce métier, pas de bouton. 0.412 : réglage « Y aller : équiper aussi le métier » coché, « Y aller » équipe aussi.

**0.410 : R-09 optimiseur PR** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Outil PR et panneau de récolte : avec beaucoup de PR, le plan propose Raison solide (ou Sagesse du fermier) et Vision claire
  quand elles rapportent ; au niveau 90 et plus, la ligne Eurêka s'affiche ; décocher la case de l'outil PR redonne l'ancien plan.

**0.409 : R-06 collectionnables** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Ouvrir la fenêtre Collectionnable sur un point (mineur ou botaniste) : le panneau « Collectionnable » est à côté, avec l'objet,
  PR et intégrité, les seuils bas/moyen/haut et les assignats par palier comparés au jeu (marchand d'assignats). Mode développeur :
  noter l'index des valeurs brutes qui suit la collectabilité à chaque action (capture), pour l'étape suivante (action conseillée).

**0.408 : préparation dans Artisanat** — pendant la campagne de tests en jeu de la fin (phase 6)


**0.407 : noms du wiki et de Garland dans la langue du jeu** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Client français : page Monstres (sources du wiki) et grimoire du mage bleu › les monstres et lieux connus du jeu sont en français,
  les coordonnées gardées ; survol d'un matériau lâché par un monstre et lignes Garland (quête, PNJ, ALÉA) en français ; un nom
  inconnu du jeu reste en anglais. Client anglais : rien ne change.

**0.406 : règle des langues** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Plugin en anglais, mode développeur coché : les messages et libellés de développeur sont en anglais ; plugin en français : en français.

**0.405 : carnets du jeu** — pendant la campagne de tests en jeu de la fin (phase 6)


**Étape g) — 0.403 : R-08 alerte PR** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Page PR › cocher « Alerte PR au seuil » (seuil 0) ; dépenser des PR en récolte puis attendre : un message et le son quand les
  PR sont pleins, une seule fois ; seuil 500 : l'alerte vient à 500.

**Étape g) — 0.402 : R-05 carnets** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Page Récolte : les compteurs des carnets sont ceux du jeu (pêche, harpon, récolte) ; un poisson attrapé et un objet récolté ont
  un ✓ ; « Masquer ce qui est déjà au carnet » les cache ; « Copier » et « Enregistrer en CSV » donnent un fichier lisible.

**0.401 : panoramas** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Survol d'un panorama sur la carte Tensho : plus d'erreur du wiki, la météo demandée est donnée ; le clic droit n'a plus d'onglet
  « Exploration », le « Carnet d'exploration » reste.

**0.400 : onglets du clic droit de la carte** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Clic droit sur la carte Tensho : les 9 onglets (Quêtes … Carnet d'exploration) ont leur nom entier ; sur un petit écran, des
  flèches font défiler les onglets.

**0.399 : « Y aller » sans question** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Réglages › Modules › Récolte, cocher « Y aller : téléporter tout de suite » : « Y aller » téléporte directement (drapeau posé) ;
  vers une zone dont l'éthérite n'est pas harmonisée : la question revient. Décoché : la question habituelle.

**Étape g) — 0.398 : R-04 appât en main** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] En pêcheur, aide à la pêche ouverte : « Appât en main » montre l'appât choisi dans le jeu et son stock ; changer d'appât : la
  ligne et les cadres verts suivent (confirme que FishingBait est l'objet).

**Étape g) — 0.397 : R-03 « Y aller »** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Fiche d'un objet de récolte, « Y aller » sur un lieu : le drapeau se pose sur le point, la carte Tensho s'ouvre dessus et propose
  l'éthérite la plus proche avec son coût ; « Oui » téléporte, « Non » ne fait rien. Même essai sur un lieu de pêche.

**Étape g) — 0.396 : R-02 fenêtres des poissons** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Fiche d'un gros poisson à météo (par exemple un poisson légendaire) : « Prochaines fenêtres » donne des heures où la page Météo
  montre bien la bonne météo et la bonne météo précédente, dans la bonne plage ET ; une alarme sonne au début d'une fenêtre.

**0.395 : grimoire trié par niveau de monstre** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] (0.395) Personnage › Grimoire de magie bleue : en quelques secondes « Livre des sorts du wiki : N sorts, lu le … » ; « Ordre : niveau du monstre croissant » : toute la liste est triée, chaque sort montre « niv. N » ; Canon à eau et les sorts sans monstre sont à la fin.
  la liste entière est triée, chaque sort montre « niv. N » ; Canon à eau et les sorts sans monstre sont à la fin. Fermer et rouvrir :
  aucune nouvelle lecture (copie).

**0.394 : polices** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Réglages › Interface › Polices : changer la famille et la taille des titres, des sections et du texte : la fenêtre Tensho change
  tout de suite ; ↺ remet la valeur par défaut ; « Restaurer tous les réglages » remet les trois.

**0.393 : Bestiaire et grande compagnie** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Page « Bestiaire » : seul le carnet de votre compagnie est dans la liste (Maelstrom pour l'utilisateur), choisi à l'ouverture ;
  ses victimes sont celles du jeu.

**0.390 : monstres du grimoire triés par niveau** — pendant la campagne de tests en jeu de la fin (phase 6)


**0.388 : bouton « Trier » de l'inventaire** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Ouvrir l'inventaire (les trois dispositions : normale, grande, étendue) : le bouton « Trier » est en bas à gauche, sur la ligne
  des gils, et suit la fenêtre quand on la déplace ou qu'on change son échelle (dire si le placement doit être ajusté).
- [x] Clic : « Tri demandé », les sacs prennent l'ordre des règles de la page Tri › Mes sacs ; décocher l'interrupteur de la page Tri :
  le bouton disparaît.

### Archive

> Le journal de vérification par version jusqu'au 0.99 (2026-10-27 → 2026-10-31) a été retiré en 0.1073 : il reste dans
> l'historique git (version 0.1072.0 : `git show 1171209:docs/project/TESTING.md`). À partir du 0.100, les résultats sont dans
> [CHANGELOG.md](../../CHANGELOG.md).
