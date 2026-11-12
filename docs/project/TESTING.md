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
  `C:\ia-code\.tools\dotnet10\dotnet.exe`, sinon `dotnet`). Le test des infobulles de réglages lit `src/TenshoAssistant` et ses langues ; ceux de la compagnie libre lisent
  `src/TenshoAssistant/FreeCompany/Data/`.
- Dernier passage : ligne « Tests » de la dernière entrée du [CHANGELOG](../../CHANGELOG.md). En CI, à chaque push : `Build and test` (dont une compilation à la manière du dépôt Dalamud, [relevé](../evidence/0.512-d17-build.md)) et CodeQL.

Les tests couvrent la logique pure : 87 fichiers `*Tests.cs` (un par domaine : inventaire, banque et tri, artisanat et macros, récolte et
pêche, météo et alarmes, carte et projection, marchands PNJ, collections, quêtes, sources Garland / wiki / LuminaSupplemental, cache
disque, langues, Mini Cactpot, Triple Triade…) et des groupes écrits directement dans `Program.cs` (marché, rappels, compagnie libre,
équipement, quêtes, infobulles des réglages, score des cartes). Tous sont enregistrés dans `Program.cs` (contrôlé le 2026-11-02).
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
2. Suivre la liste ci-dessous, ou le protocole de la version dans [CHANGELOG.md](../../CHANGELOG.md) / le document d'objectif
   (`docs/goals/<ID>.md`).
3. Rapporter : version, ce qui a été observé, captures si possible, et `dalamud.log` / `dalamud.crashhandler.log` en cas d'erreur.
   Seul ce rapport permet d'écrire « validé en jeu » ; il se reporte en cochant la case de l'essai sur cette page.

Contrôles de base à refaire après un changement de cycle de vie : rechargement/déchargement répété sans double callback ni commande
orpheline ; préférence « ouvrir au chargement » respectée ; la carte du jeu (touche M) revient normale une fois le plugin déchargé.

### Essais en jeu à faire (liste au 2026-11-02, DLL 0.315.0.0)

Parcours de la campagne de la phase 6, les mêmes essais rangés par activité : [TESTING-ROUTE.md](TESTING-ROUTE.md) (2026-11-09).

Non validé en jeu au 2026-11-02 : 0.232–0.302, 0.307–0.309, 0.315. Dans cette plage, déjà rapporté OK par
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

**Paramètres, caches et langues (0.246, 0.248, 0.258, 0.260–0.261, 0.264, 0.281)**

- [ ] 0.258 — Paramètres › Avancé › « Tout remettre par défaut » (confirmation) : réglages remis ; gardés : langue, alarmes,
  préréglages d'artisanat, decks, quêtes suivies, favoris de carte, plan de macros, profils et modèles de tri, relevés, et depuis 0.315
  vaisseaux lus et plans d'équipement / de matérias ; la Banque n'est pas touchée.
- [ ] 0.246 (facultatif) — Hors ligne : `dalamud.log` ne montre pas de téléchargement LuminaSupplemental / GatherBuddy relancé à
  chaque image (5 min entre deux essais) ; les sources gardent leur copie expirée.

**Rappels et barre du serveur (0.300–0.302)**

- [ ] 0.300 — MONDE › Rappels, comparer au Calendrier du jeu : missions de ravitaillement, commandes spéciales (livraisons restantes),
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
- [ ] Icônes de quête 71141 / 71151 : seulement avec un relevé en jeu (STATUS, points décidés avant D17).

**Encore listés « à valider » dans STATUS avant 0.232**

- [ ] 0.224 — Mineur / botaniste, objet récolté en contexte : outil PR, « Estimé : +N » à côté du bonus, bouton « Utiliser ».
- [ ] 0.225 — Panneau « Aide à la récolte · Mineur / Botaniste » à droite de la fenêtre de récolte du jeu, un métier à la fois.
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

- [ ] Filtre (« MIT », « Garland »…) ; « Copier le lien » et « Copier la licence » remplissent le presse-papiers ; colonne Usage
  renvoyée à la ligne, sans texte coupé à une largeur de fenêtre réduite.

**Point d) — 0.321** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Paramètres › Modules › Artisanat : changer « Étapes », « Largeur », « Budget », « Attente » et « Écho », recharger le plugin : les
  valeurs restent ; le bouton ↺ remet chacune ; la case « Aide du carnet » s'enregistre au clic et montre son infobulle au survol.
- [x] Paramètres › Avancé › « Revue de mode (FFXIVGachaSpreadsheet) » décoché : la page Revue de mode dit « source désactivée » quand
  aucune copie n'est gardée, sans téléchargement.
- [ ] Marché hors ligne (réseau coupé) : le bouton de portée « Région » reste grisé, avec son infobulle ; pas de ralentissement.

**Point d) — 0.324 : interrupteurs de sources** — pendant la campagne de tests en jeu de la fin (phase 6)


**Point d) — 0.325 : infobulles, confirmations** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Survol des filtres, recherches, tris et champs de saisie des pages (Banque, Collections, Butin, Quêtes, Récolte, Objets,
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
- [ ] Paramètres › Général : « dernière page » coché, aller sur Carte, fermer, recharger le plugin → s'ouvre sur Carte ; décoché → Accueil.
- [ ] Survol : décocher chaque partie (collection, stock, usages, comment l'obtenir) → elle disparaît du panneau sous l'infobulle du jeu
  seulement ; « masquer en mission » coché → pas de panneau dans un donjon.
- [x] Triple Triad (observation activée) : chacun des trois interrupteurs coupe son aide (cadre du match, défi d'un PNJ, éditeur de decks).
- [ ] Carnet d'artisanat : « calcul automatique » décoché → bouton « Calculer » ; le décocher pendant un calcul garde « Calcul… ».
- [ ] Alarmes : « pas de ligne de discussion en mission » coché (sous « Pas de son en mission ») → aucune ligne [Tensho] en donjon.

**Étape e) — 0.328 : carte** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] « Remplacer la carte du jeu » et « Masquer en combat » cochés : M en combat montre la carte du jeu (plus d'écran sans carte).
- [x] Carte Tensho ouverte par M, bouton « Ouvrir la carte du jeu » : la carte du jeu revient à l'écran.
- [x] Zoom +, − et curseur sans suivre le personnage : la vue reste centrée au même endroit.
- [ ] Repère au bord du drapeau au-dessus d'une icône de vendeur : un clic centre la carte sans déplacer le drapeau.
- [x] Fenêtre de téléportation : le coût s'affiche (ou « indisponible » si la lecture échoue, jamais « Lecture… » sans fin).
- [x] Favoris : un nom en double dit « déjà utilisé », pas « Échec de sauvegarde » ; page Cartes, le bouton pose le drapeau.
- [ ] Liste des panoramas (clic droit) : clic droit sur une ligne → alarme proposée ; numéro identique à l'infobulle.
- [ ] Ville chargée : la carte reste fluide (vendeurs mis en cache), temps à relever dans Diagnostics si besoin.

**Étape e) — 0.329 : objets, banque, tri, marché** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Coffre à apparence très rempli : l'import s'enregistre et se recharge après un rechargement du plugin.
- [ ] Fiche d'un objet ouverte directement (onglet Aperçu) : le stock de l'en-tête est juste ; pour un cristal, les servants montrent « ≥ ».
- [ ] Survoler vite plusieurs objets avec prix au survol : pas de « prix indisponibles » pendant la pause de 3 s.
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

- [ ] « n bardes débloquées sur m » comparé au carnet de bardes du jeu ; « Seulement celles qui manquent » ; recherche par nom d'objet.

**Étape f) — 0.342 : événements saisonniers** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Boutiques de l'événement : le vendeur de la fête, ses villes, ce qu'il vend et le prix ; comparer avec la boutique en jeu.

**Étape g) — 0.343 : I-01 infobulle de prix complète** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Survol d'un objet vendable : sous chaque prix, « Dernière vente · moyenne · ventes par jour » ; les mêmes que la page de l'objet
  sur universalis.app pour votre monde.
- [x] Survol d'une pile de l'inventaire : « Pile de N : ~X » (N = la quantité de la case) ; pas de ligne pour un objet hors de vos sacs.
- [ ] Ctrl+Alt pendant le survol : la date « Récupéré à » change ; maintenu, une seule demande ; Paramètres › Avancé, l'éteindre.

**Étape g) — 0.344 : I-02 monnaies et alertes de plafond** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Rangements › Monnaies : soldes et plafonds identiques à la fenêtre Monnaies du jeu (sceaux de votre compagnie au plafond de votre
  rang, Centurio, gemmes bicolores, assignats, mémoquartz) ; « Mémoquartz limités cette semaine » égal au jeu.
- [ ] Changer de personnage : pas de fausse alerte ; l'onglet n'affiche pas l'ancien personnage.

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

- [ ] Mettre un même objet en deux piles incomplètes dans les sacs : Rangements › Rapport le liste, avec « 1 case libérable ».

**Étape g) — 0.349 : X-07 liens wiki et Lodestone** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Clic droit sur un objet › Tensho : « Wiki (anglais) » et « Lodestone » ouvrent la bonne page dans le navigateur.

**Étape g) — 0.350 : I-09 coffre de compagnie** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Ouvrir le coffre de compagnie : le panneau Tensho apparaît à côté ; tant qu'un onglet n'a pas été affiché, il le dit ; après les
  cinq onglets (et les cristaux), « Enregistrer » ; avec l'enregistrement automatique, c'est fait seul.
- [x] Banque : un onglet « Coffre de compagnie » ; le stock d'un objet du coffre (fiche d'objet, survol) compte la ligne « Coffre de
  compagnie ».

**Étape g) — 0.351 : I-04 journal des gils** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Acheter à un PNJ, vendre à un PNJ, réparer, se téléporter : Rangements › Gils montre chaque variation avec la bonne catégorie
  (Boutique PNJ, Réparation, Téléportation).
- [x] Ouvrir la cloche des servants : le solde des servants apparaît ; ouvrir le coffre de compagnie : son solde apparaît.

**Étape g) — 0.352 : I-11 conditions des articles de boutique** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Page Marchands, un marchand dont un article demande une quête : « Après la quête … (fait / à faire) » après le prix, juste
  selon votre journal ; un article lié à un haut fait affiche son nom.

**Étape g) — 0.353 : I-12 recherche d'objets avec filtres** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Page Objets › « Filtres » : catégorie « Tête », niveau d'équipement 90 à 100 : la liste ne montre que ces casques, sans rien
  écrire ; avec un mot en plus, la liste se réduit ; « Effacer les filtres ».

**Étape g) — 0.354 : I-13 valeur des monnaies** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Rangements › Monnaies › « Que faire ? » sur les sceaux de compagnie : la liste de ce qu'ils achètent, avec le prix en sceaux ;
  « Prix du marché » remplit les prix et « Gils par unité », classés du plus rentable ; une monture déjà possédée dit « possédée ».

**Étape g) — 0.355 : C-01 contenus débloqués / terminés** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Monde › Butin des contenus : la colonne « État » dit « Terminé » pour un donjon fait (ex. Sastasha), « Débloqué » pour un contenu
  ouvert mais pas fait, « Verrouillé » pour un contenu pas encore ouvert ; le compteur « X terminés sur Y lus » est plausible.
- [ ] Filtre « Avancement › Non terminés » : les donjons faits disparaissent ; déconnecté, le filtre est grisé et l'état « — ».

**Étape g) — 0.356 : C-02 missions aléatoires du jour** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Monde › Rappels › « Missions aléatoires du jour » : faire une mission aléatoire (ex. gain de niveaux) ; elle passe à « Faite »,
  les autres restent « À faire » ; le compteur et l'heure de remise à zéro sont justes.

**Étape g) — 0.357 : C-03 objets à ranger dans l'armoire** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Une pièce utilisée par un set d'équipement n'est jamais proposée.

**Étape g) — 0.358 : C-07 apparences identiques du coffre** — pendant la campagne de tests en jeu de la fin (phase 6)


**Étape g) — 0.359 : C-08 filtre des collections par source** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Collections › Montures › Source « Marchands (gils) » puis « Succès » : les montures listées ont bien cette source dans leur menu
  du clic droit ; combiné avec « Non confirmés », la liste se réduit.
- [x] Le premier choix du filtre sur l'Orchestrion ne fait pas saccader le jeu de façon gênante (sinon : calcul à étaler).

**Étape g) — 0.360 : C-09 liste de souhaits** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Clic droit sur une monture dans un butin (Butin des contenus) › « Ajouter à mes souhaits » ; Collections › Souhaits la montre
  avec « Pas encore obtenu » ; clic droit sur la ligne donne ses sources ; « Retirer » l'enlève.
- [ ] Un objet ordinaire ajouté montre « Possédé ×N » ou « Pas possédé » ; la liste reste après un redémarrage et après
  « Réinitialiser les réglages ».

**Étape g) — 0.361 : C-11 carnet d'exploration** — pendant la campagne de tests en jeu de la fin (phase 6)


**Étape g) — 0.362 : C-05 tenues du coffre à apparence** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Ouvrir le coffre à apparence une fois. Collections › Tenues : une tenue déjà rangée dit « rangée » ; une tenue dont toutes les
  pièces sont dans le coffre dit « prête à ranger » (le jeu propose bien de la créer) ; les pièces ouvertes disent où elles sont.
- [ ] Filtres « Incomplètes », « Toutes les pièces », « Rangées » et la recherche par nom.

**Étape g) — 0.363 : C-14 mandats** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Progression › Mandats : les allocations restantes et le nombre de mandats acceptés sont ceux du jeu ; un mandat déjà fait dit
  « Terminé » ; filtres type, mandateur, niveau min/max et état.

**Étape g) — 0.364 : C-12 carnet de chasse** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Progression › Carnet de chasse, carnet de votre classe : « Le jeu en est au rang N » est le rang du carnet du jeu ; les victimes
  d'une entrée en cours sont les mêmes que dans le jeu (tuer un monstre : le compteur monte en 2 secondes).

**Étape g) — 0.365 : C-04 nouveaux types de collection** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Collections : les onglets Coiffures, Accessoires de mode, Lunettes, Bardes, Portraits, Livres de maître, Objets de collection,
  Archives occultes se remplissent en quelques secondes ; une coiffure ou un accessoire que vous avez dit « Obtenu », un autre non.
- [ ] Les filtres Obtenu / Non confirmés et la recherche marchent dans ces onglets.

**Étape g) — 0.366 : C-10 alerte « mission prête »** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Alarmes › réglages › « Alerte quand une mission est prête » cochée : à l'ouverture de la mission, la ligne « Mission prête : … »
  et le son ; décochée, rien.

**Étape g) — 0.367 : C-13 monstres des sorts de mage bleu** — pendant la campagne de tests en jeu de la fin (phase 6)


**Étape g) — 0.368 : C-15 progression des succès** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Ouvrir un succès en cours dans la fenêtre des succès du jeu : dans Tensho, sa fenêtre montre « Progression : x / y ».

**Étape g) — 0.369 : X-03 titres** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Collections › Titres › un titre obtenu › « Porter ce titre » : le titre change au-dessus du personnage ; « Aucun titre » le retire ;
  « Titre au hasard » en met un autre.
- [ ] « Titre de <job> » puis « Changer de titre avec le job » coché : changer de job porte le titre choisi ; décoché : rien ne change.

**Étape g) — 0.370 : X-02 musique en cours, rouleaux favoris** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Collections › Orchestrion : « Musique en cours » nomme la musique qu'on entend (en ville, puis en combat, puis dans une maison
  avec un orchestrion) ; « Fiche » ouvre son rouleau.
- [ ] Case « Favori » dans la fiche d'un rouleau : il apparaît dans « Rouleaux favoris ».

**Étape g) — 0.371 : M-01 alarmes automatiques** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Ouvrir la liste des servants à une cloche : dans les 30 secondes, une alarme « Venture de … terminée » par servant en venture
  apparaît dans Alarmes, à la bonne heure ; elle sonne à la fin.
- [x] Envoyer un vaisseau à l'atelier : une alarme « Retour de … » apparaît ; « Alarmes automatiques » décochée : plus aucune n'est créée.

**Étape g) — 0.372 : M-02 compteurs hebdomadaires** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Rappels : « Livre d'or des défis » montre les vignettes et points du livre de Khloe, avec son expiration ; « Contrats de chasse »
  le nombre de contrats en main ; « Cartes au trésor » l'heure de la prochaine carte du jeu (sans bouton « ouverte »).

**Étape g) — 0.373 : M-03 liens de carte du chat** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Envoyer un lien de carte (drapeau puis <flag>) en équipe et en compagnie libre : il apparaît dans Cartes › « Liens de carte du
  chat », avec le bon lieu, les bonnes coordonnées et le bon canal au survol ; le filtre de canal le cache et le remontre.
- [ ] « Drapeau » pose le drapeau du jeu au bon endroit ; « Éthérite » ouvre la carte Tensho sur le point et propose l'éthérite
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

- [ ] Survoler une pièce d'équipement dans l'inventaire et au marché : sous l'infobulle, « Comparé à l'équipement porté » donne le
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
- [ ] « Copier en CSV » puis coller dans un tableur : une ligne par objet, colonnes lisibles.

**Étape g) — 0.380 : M-09 paliers de statistiques** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Page Équipement (ou panneau près de la fenêtre Personnage) : la colonne « +N palier » est présente ; ajouter une matéria qui
  donne au moins N points de la statistique : la valeur réelle change ; moins de N : elle ne change pas.

**Étape g) — 0.381 : X-01 tâches libres** — pendant la campagne de tests en jeu de la fin (phase 6)


**Étape g) — 0.382 : M-11 autres compteurs** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Ouvrir le carnet d'objectifs du jeu : la ligne « Carnet d'objectifs » des Rappels donne le bon nombre de défis faits (indice des
  lignes à confirmer).
- [ ] Passer à l'Enclave de Doman, faire un don : la ligne « Enclave de Doman » montre le don et le plafond de la semaine.

**Étape g) — 0.383 : M-12 ensembles d'équipement** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Équipement › « Ensembles d'équipement du jeu » : tous vos ensembles, avec numéro, nom, job et niveau d'objet du jeu.

**Étape g) — 0.384 : M-14 carte au trésor** — pendant la campagne de tests en jeu de la fin (phase 6)

- [x] Déchiffrer une carte au trésor : dans les 5 secondes, Rappels › Cartes au trésor montre « Voir sur la carte » ; le bouton ouvre
  la carte Tensho sur le bon endroit (comparer avec la carte du jeu) et y pose le drapeau ; l'icône gemme est au même endroit.
- [ ] Ouvrir le coffre : l'icône et le bouton disparaissent.

**Étape g) — 0.385 : X-08 job au hasard** — pendant la campagne de tests en jeu de la fin (phase 6)


**Étape g) — 0.386 : X-10 horloges et alarme à date** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Alarmes › Horloges : ajouter « Tokyo » et « New York » : les heures sont justes ; « Retirer » enlève une horloge.

**0.387 : signalements corrigés** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Grimoire du mage bleu, « Mauvaise haleine » › « Chercher sur le wiki » : les monstres (Capricious Cassie, Halitostroper…) avec
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

- [ ] Barre de PR, usure, suivi, tâches : une fenêtre du jeu les touche : elles se cachent. Le chat, les plaques de noms et le HUD ne les cachent pas.
- [x] (rapport de l'utilisateur, 2026-11-05, « fenêtre ok ») Carte, réglage « S'effacer devant les fenêtres du jeu » coché : une fenêtre la touche : elle se cache sans se fermer.
**0.538 et suivantes : un essai par ligne, marqué de sa version** — à voir en jouant


- [x] (rapport de l'utilisateur, 2026-11-05, « tous es ok ») (0.539) Déplacer une fenêtre du jeu qui a un panneau de Tensho (récolte, carnet, personnage, mission) : le panneau la suit sans retard visible.
- [x] (rapport de l'utilisateur, 2026-11-05, « tous es ok ») (0.539) Réglage de la carte activé : un panneau lié qui touche la carte la fait disparaître ; passer d'une infobulle à l'autre ne la fait pas clignoter, et elle revient environ 1 seconde après.

- [ ] (0.540) Journal du jardin : « Graines de centaurée » est dans la liste, avec 24 h ; une plantation de cette graine est reconnue dans le chat. Comparer la durée réelle.

- [ ] (0.541) Mes outils, bloc d'objets : colonne de prix, coût de ce qui manque, bouton « Demander les prix ». Un objet invendable affiche un tiret.

- [ ] (0.542) Carte, réglage « Zoom à l'ouverture » : régler un zoom, relancer le jeu : le dernier zoom revient. Tester aussi « Fixe » et « Carte entière », puis le zoom sur une quête.

- [ ] (0.543) Carte : les quatre réglages des noms et des étiquettes de récolte changent bien l'affichage ; aux valeurs par défaut, rien ne change.

- [x] (0.544) Carte : clic droit sur un point, puis sur le drapeau : « Éthérite la plus proche » donne le nom et le coût, et demande confirmation.

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


- [ ] (0.565) Aperçu du tri : les cases encadrées sont bien celles qui bougent ; le nombre correspond après application.



- [ ] (0.568) Aide à la pêche : comparer « créneau », « météo » et « météo d'avant » à l'heure et à la météo du jeu, sur un poisson à conditions.


- [ ] (0.570) Rappels : déplier les tâches de servants après la cloche ; les heures correspondent, et les deux cloches créent leur alarme.

- [ ] (0.571) Mini Cactpot : comparer la table affichée au ticket du jeu, somme par somme.

- [ ] (0.572) Simulateur : chercher une recette et l'ouvrir ; ajouter une action : le résultat suit sans clic.

- [ ] (0.573) Macros : régler le son et « dernière page seulement » ; la macro copiée suit, et le son s'entend en jeu.



- [ ] (0.576) Rangements : effacer le relevé d'un servant (deux clics) : ses objets quittent les stocks, et rouvrir le servant le relit.

- [ ] (0.577) Alarmes : créer une alarme chaque jour et une à la remise à zéro : l'heure annoncée est juste, et elle revient après avoir sonné.


- [ ] (0.579) Équipement : cocher « Tous les jobs (mirage) » sur la page : la liste des pièces s'élargit, et la case des filtres suit.

- [ ] (0.580) Équipement : choisir un autre job dans « Plan pour » : la liste de pièces est la sienne, le plan se garde, et revenir au job porté retrouve son plan.

- [ ] (0.581) Revue de mode : cocher une pièce par emplacement ; les pièces possédées sont justes ; « Essayer la tenue » les met dans la cabine.

- [ ] (0.582) Quêtes : « Disponibles maintenant » et « Cette zone » : comparer à quelques quêtes du jeu ; une quête refusée par le jeu malgré le filtre est à signaler.

- [ ] (0.583) Quêtes : sur une quête d'épopée lointaine, la liste et « À prendre ensuite » correspondent au journal du jeu.



- [ ] (0.586) Carte : menu de Noscea, Thanalan, forêts : l'aile s'affiche, colorée si vous pouvez voler ; comparer en essayant de voler dans une de ces zones.

- [x] (rapport de l'utilisateur, 2026-11-05, « re ok ») (0.587) Carte : la roue dentée ouvre Paramètres, onglet Carte, au premier plan ; la fenêtre surgissante a disparu ; un réglage changé s'applique à la carte.

- [ ] (0.588) Récolte, section « Ouverts maintenant ou bientôt » : comparer les heures à un point ouvert dans le jeu ; la cloche crée l'alarme.

- [ ] (0.589) Lancer une synthèse sur une recette avec une séquence enregistrée : l'action suivante avance à chaque coup, « Conforme au plan » tant que les chiffres sont égaux, avertissement après un état Bonne ou une autre action.

- [ ] (0.590) Page des séquences : chercher un mot, changer l'ordre, renommer, dupliquer, copier en texte puis coller dans « Importer une macro » du simulateur.

- [ ] (0.591) Préparation d'un objet à sous-recettes : calculer la chaîne, vérifier une ligne par recette et l'ordre, ouvrir le carnet et le simulateur depuis une ligne, cocher une étape.

- [ ] (0.592) Artisanat, onglet « Que puis-je fabriquer ? » : lancer la recherche, comparer quelques lignes à vos sacs, filtrer par métier et niveau.


- [ ] (0.594) Banque : cocher « Afficher en liste », trier chaque colonne, filtrer, changer d'onglet, cliquer un objet.


- [ ] (0.596) Comparer une carte Tensho et celle du jeu : les noms doivent être du même côté de leurs icônes. Essayer la case « Noms des icônes » et le zoom minimal.

- [ ] (0.597) Section Carte : chercher un lieu, une éthérite, un vendeur, un PNJ de Triple Triad, une quête et une ressource, cliquer chacun et vérifier le centre de la carte.

- [ ] (0.598) Réglages › Alarmes › Discord › Un salon par catégorie : mettre une autre adresse pour une catégorie, faire sonner une alarme de cette catégorie et une autre, vérifier les deux salons.

- [x] (0.599) Accueil : vérifier chaque ligne d'« Aujourd'hui » contre Rappels, Compagnie, Quêtes et Alarmes, cliquer chacune, masquer une ligne et une tuile dans « Personnaliser ».

- [ ] (0.600) Fiche objet d'une recette de livre de maître, onglet Craft : comparer la ligne à ce que dit le carnet du jeu.


- [ ] (0.602) Collections : ouvrir une fois, obtenir une monture ou une emote, actualiser : elle apparaît dans « Obtenus récemment ». Clic droit sur une emote.

- [ ] (0.603) Avec deux compagnies relevées : deux cadres, chacun avec sa date.

- [ ] (0.604) Pêcher : le panneau de pêche trouve bien le poste ; panneau de récolte : la ligne d'Abondance II s'affiche.





- [ ] (0.609) Fiche d'un objet qu'un autre personnage possède : la ligne donne ses quantités.

- [ ] (0.610) D'abord hors JcJ : tout marche comme avant, barre de serveur et carte comprises. Puis, un à la fois : quai de Wolves' Den, Crystalline Conflict personnalisé, Battlehall, Mahjong, donjon profond, avec la carte et des panneaux ouverts : tout disparaît, une ligne de chat, tout revient à la sortie.




- [x] (0.614) Hall de l'Arène, duel contre un PNJ : le panneau Triple Triade s'affiche.

- [ ] (0.615) Avec un lien de carte dans le chat pendant une coupure : il n'apparaît pas dans la liste des liens.



- [ ] (0.618) Carte agrandie : ouvrir l'inventaire, elle reste affichée. Taille normale : elle s'efface quand une fenêtre la touche, puis revient.



- [ ] (0.621) Marcher dans L'Entrepont : les deux lignes de Tensho doivent suivre celles de la carte du jeu.


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

- [ ] (0.644) Carte : vos coordonnées restent en bas à gauche et le curseur donne sa distance. Poser un drapeau hors de la vue : ses coordonnées et sa distance s'affichent sous son repère de bord. Comparer une distance avec le jeu.

- [ ] (0.645) Survoler une éthérite harmonisée : son coût s'affiche, le même que dans la fenêtre de téléportation. Une éthérite non harmonisée est grisée et le dit au survol.

- [x] (0.646) Page Équipement, section Jobs : niveaux et expérience identiques à la fenêtre du jeu, classes et jobs sans doublon, jobs non débloqués grisés.

- [ ] (0.647) Survoler un point légendaire, un éphémère et un à horaire sur la carte : la classe dite correspond au jeu.

- [ ] (0.648) Page Butin des contenus, le Palais des morts : « Débloqué par » donne la quête, son état et son lien. La fiche de cette quête dit « Débloque ».

- [ ] (0.649) Page Récolte : chercher un poisson au harpon, son lieu s'affiche. Comparer avec le carnet du jeu. Ses alarmes et sa carte fonctionnent comme les autres.

- [ ] (0.650) Mode développeur : récolter sans action pendant quelques tics, le journal écrit « Reading gp ». Les relevés suivants sont donnés un par un.

- [x] (0.651) Rappels, ligne Bienfaits de compagnie : les noms s'affichent sans rien ouvrir. Ouvrir l'onglet Bienfaits : le temps restant apparaît, comme dans le jeu. Poser la cloche : l'alarme est à l'heure de fin.

- [x] (0.652) Lancer un mandat, ouvrir la carte : ses zones sont vertes au même endroit que dans le jeu. Réglages de la carte : « Zones des mandats » et le repère « Mandats » agissent seuls. Essayer si possible un mandat de combat.

- [x] (0.653) Mandat lancé, carte ouverte, zone verte visible : cliquer le mandat dans le journal, puis recliquer. La zone et les autres marqueurs restent. Terminer le mandat : la zone disparaît une fois la carte refermée.

- [x] (0.654) Mandat lancé, zone vue une fois : s'éloigner, changer de zone, ouvrir la carte de la zone du mandat, la zone verte reste. Cliquer le mandat dans le journal : elle reste. Abandonner le mandat : elle disparaît.

- [x] (0.655) Accepter un mandat, le cliquer dans le journal : la cible et son repère au bord apparaissent. L'abandonner : ils disparaissent en moins d'une seconde.

- [x] (0.656) Alerte de PR activée, PR pleins : se téléporter, attendre une minute, aucune alarme. Dépenser des PR, se téléporter, attendre la remontée : une seule alarme.

- [x] (0.657) Depuis une autre zone, cliquer le mandat dans le journal : la zone verte s'affiche et reste. Abandonner : la zone et le repère noir partent. PR pleins, se téléporter : aucune alarme, puis dépenser des PR : une alarme à la remontée.

- [x] (0.658) Carte ouverte, se téléporter vers une autre zone : la carte se rouvre à l'arrivée. Vers la même zone aussi. Fermer la carte avec sa touche puis se téléporter : elle reste fermée. Entrer dans un contenu carte ouverte : elle reste fermée.

- [ ] (0.659) Carte ouverte, entrer dans un contenu : elle se rouvre une fois la scène d'ouverture finie. Fermée avec sa touche avant d'entrer : elle reste fermée.

- [ ] (0.660) Vieille Sharlayan, carte du jeu et Tensho côte à côte, au même zoom : les noms ont presque la même largeur et apparaissent à peu près en même temps.

- [ ] (0.661) Incanter une téléportation et fermer la carte avec sa touche : elle reste fermée. Carte ouverte, se téléporter sans y toucher : elle se rouvre à l'arrivée. Dézoomer une ville au maximum : seuls les noms de liens restent, comme le jeu.

- [ ] (0.662) Carte Tensho ouverte, se téléporter plusieurs fois de suite : plus de plantage. Le coût au survol d'une éthérite s'affiche quelques secondes après l'arrivée.

- [ ] (0.663) Pêche : un poisson à proies affiche sa durée d'intuition. Attraper ses proies : le temps restant s'affiche, et sa valeur de départ correspond à la durée annoncée.

- [ ] (0.664) Compagnie libre, onglet Atelier : chercher un projet, comparer ses matériaux et ses quantités à la fenêtre de l'atelier du jeu. Le clic sur un objet ouvre sa fiche.

- [ ] (0.665) Réglages des icônes de la carte : les quêtes disent « disponible », « non disponible » ou « étape en cours » sans numéro. Comparer une quête grisée sur la carte du jeu : elle ne peut pas être prise.

- [ ] (0.666) Exploration cosmique : ouvrir une recette dans le carnet. Tensho ne la refuse plus, et progrès, qualité et solidité sont ceux de la fenêtre du jeu. Lancer une séquence : les gains correspondent à la synthèse. Les seuils affichés correspondent à la mission.


- [ ] (0.668) Faire une synthèse et s'arrêter à plusieurs niveaux de qualité : le pourcentage HQ du jeu correspond à celui du simulateur pour la même qualité.

- [ ] (0.669) Simulateur : choisir des ingrédients HQ, la qualité initiale doit égaler celle du carnet du jeu avec les mêmes ingrédients. Coût de préparation et historique du marché : mêmes valeurs qu'avant, filtres et tri réagissent à l'instant.

- [ ] (0.670) Cartes › Conditions des rangs S : les noms sont lisibles dans la langue du jeu (aucun « # »), les marques de la zone viennent en premier, la recherche filtre. À l'occasion, comparer une condition à ce qui se passe en jeu.

- [ ] (0.671) Sur l'île, Production › Île sanctuaire : Lire avant d'ouvrir l'agenda (doit le demander), puis après : rang, ateliers, entrain, popularité et offre doivent correspondre au jeu. Comparer la valeur estimée aux coquillages d'une création terminée.




- [ ] (0.675) Production › Commandes spéciales : comparer rang, points et livraisons de chaque client à la fenêtre du jeu ; un client non débloqué doit être marqué ; livrer une fois puis Lire de nouveau.


- [ ] (0.677) Cartes & repères : les 4 onglets et leur contenu ; cliquer un favori dans Repères doit ramener sur Cartes avec ses coordonnées ; Paramètres › Favoris doit ouvrir l'onglet Repères ; rien ne doit avoir disparu.






- [ ] (0.683) Progression › La Grande Mascarade : comparer les niveaux réussis, les ennemis et les temps à la fenêtre du jeu ; le niveau 1 et le niveau 32 surtout.





- [ ] (0.689) Mandats : survoler un mandat doit donner ses informations, surtout en fenêtre étroite.




- [ ] (0.693) Panneau affiché : entrer en combat et ouvrir une fenêtre du jeu par-dessus, il doit rester ; cocher chaque option, il doit se cacher ; la croix le coupe.




- [ ] (0.697) Que faire avec ? : décocher la case pendant une recherche doit l'arrêter ; essayer chaque filtre ; dire s'il manque des types de collection.


- [ ] (0.699) Cocher l'option sur un panneau : les matières de base par provenance, le stock sur le requis ; décocher une provenance ; comparer une pièce d'atelier à Préparation › Calculer.


- [ ] (0.701) Monnaies des vendeurs : plus de numéros, tout décoché au départ, la recherche filtre ; cocher Gil doit remplir Vendeurs PNJ ; rien de coché : plus aucun PNJ.



- [ ] (0.704) Butin des monstres : clic droit sur une ligne du wiki avec coordonnées ; essayer aussi un vendeur, un favori, le train de chasse, un animal de l'île ; le bon endroit doit être visé.


- [x] (0.706) Sur l'île : clic droit sur un animal ou sur la carte Tensho, plus d'entrée éthérite, le drapeau reste ; ailleurs, l'entrée reste. VALIDATED_IN_GAME (rapport de l'utilisateur, 2026-11-07). Étages sans éthérite : à voir quand le cas se présentera.


- [ ] (0.708) Progression › Armes reliques : comparer les étapes d'une arme ; avec un livre en cours, comparer les objectifs et les compteurs au livre du jeu.





- [ ] (0.713) Progression › Peuples alliés : comparer rangs et réputation à la fenêtre de réputation du jeu ; si chaque ligne montre le rang du voisin, le dire.

- [ ] (0.714) Progression › Grandes compagnies : comparer grade, sceaux et prix de la promotion au jeu ; vérifier le genre du nom de grade ; comparer quelques articles à la boutique du jeu.

- [ ] (0.715) Progression › Livraisons spéciales : comparer les quêtes faites au journal ; dire si l'onglet du Cristarium contient bien ce qu'il faut.



- [ ] (0.718) Progression › Donjons profonds : lire deux textes de poteries et comparer l'état d'un groupe d'étages à la fenêtre du jeu.

- [x] (0.719) Objets › Lexique : chercher « eureka » puis un mot en anglais ; dire si la première ouverture se sent.


- [ ] (0.721) Monde › ALEA : comparer le lieu de deux ALEA en cours à la carte du jeu.


- [ ] (0.723) Masked Carnivale : l'auto-contrôle lit 0 épreuve faite ; dire si c'est juste pour votre personnage.


- [x] (0.725) Que puis-je fabriquer ? › tri par marché : relancez les prix, les ventes par jour doivent s'afficher et le haut de la liste doit se vendre.



- [x] (0.728) À un point de récolte, suivez le plan : la ligne « Ensuite » et le cadre sur la barre doivent suivre chaque étape.

- [x] (0.729) À un point de récolte : une ligne doit séparer « Ensuite » du plan.


- [ ] (0.731) À un point de récolte, suivez le plan jusqu'au bout : avec 50 PR ou plus et des essais restants, « Ensuite » doit proposer une action avec les PR restants.


- [ ] (0.733) À un point de récolte : un cadre doré doit clignoter sur la case de l'action « Ensuite » ; Sagesse du fermier seulement après une récolte.

- [x] (0.734) À un point de récolte : le cadre doit se poser sur Récolte abondante, même placée en version I sur la barre.










- [ ] (0.744) Récoltez quelques objets de la liste : plus de clignotement ; je vérifie qu'aucun « List zero » ne revient.



- [ ] (0.747) À un point de récolte : le plan apparaît après « Calcul du plan… », sans à-coup ; je relis « Slow frame ».




- [x] (0.751) Point de récolte avec un objet de votre liste d'atelier : ligne « Vos listes en demandent encore » et cadre vert dans la fenêtre du jeu.

- [x] (0.752) Liste de l'atelier : sous Récolte, une ligne par zone avec ses objets.

- [ ] (0.753) Point de récolte : cochez « Récolter automatiquement » ; le plan change et le dit.

- [ ] (0.754) Carte : survolez un point à horaire fermé ; une ligne doit dire dans combien de temps il ouvre.


- [ ] (0.756) Ouvrez un point qui a un objet de vos listes : l'aide doit se placer dessus.

- [ ] (0.757) Liste de l'atelier : repliez une zone ; changez de zone, elle doit passer en tête ; les prix de téléportation s'affichent.


- [ ] (0.759) Cliquez « Ouvrir dans Tensho » sur une alarme, même affichée avant un rechargement : la fiche de l'objet doit s'ouvrir.




- [ ] (0.763) À un point où deux plans se valent, l'aide doit proposer celui avec le moins d'actions.











- [x] (0.774) À la prochaine alarme : clic droit sur le nom de l'objet dans le chat ; le menu doit proposer Tensho.





- [ ] (0.779) Rechargez le plugin : les objets de monstres sont dans « Monstres » dès l'ouverture de la liste.

- [x] (0.780) À la prochaine alarme : clic droit n'importe où sur « Minerai de sombracier (clic droit : Tensho) ».

- [x] (0.781) À la prochaine alarme : clic droit sur la ligne de l'objet ; je relis « Context menu opened ».

- [ ] (0.782) Rouvrez le carnet d'artisan sur la recette 5623 en cuisinier : plus de gel.

- [ ] (0.783) Carnet d'artisan, Kit de réparation d'appareil : « Raphael non utilisé ».




- [ ] (0.787) Ouvrez la livraison à l'établi : le panneau d'atelier ne garde que le reste à livrer, la page dit « Livré : phase 2/3 en cours ».

- [ ] (0.788) Carnet en Menuisier sur le Lingot de mithril (Métallurgie) : « Mauvais métier » en orange et le bouton pour porter Forgeron.











- [ ] (0.799) En mage bleu, près d'un monstre qui apprend un sort manquant (après « Lire les sorts manquants ») : icône du mage bleu au-dessus de sa tête.








- [ ] (0.807) Promenez-vous parmi des monstres, puis Monde › Découvertes : cherchez un nom, ses lieux s'affichent ; Exporter, puis déposez le fichier dans Import et Importer.

- [ ] (0.808) Carnet d'artisanat : clic droit sur une recette puis sur un matériau : entrée « Tensho ».

- [ ] (0.809) Vainquez quelques monstres et ramassez leur butin (en mage bleu : un sort ; en dresseur : une capture) ; je relis « Reading messages ».



- [x] (0.812) À la prochaine alarme : clic droit sur le nom de l'objet : menu de l'objet avec « Tensho ».


- [ ] (0.814) Une cible hors de la vue, en haut à gauche : son icône de bord passe au-dessus du bandeau des noms.


- [ ] (0.816) Récolte d'un collectionnable avec seuils : le bouton de l'action du plan encadré dans la fenêtre du jeu.

- [ ] (0.817) Ouvrez un objet au tableau des ventes, puis regardez son prix dans Tensho (survol, Que faire avec ?) : celui que vous venez de voir.


- [ ] (0.819) Près d'un yak laineux avec du lait de yak demandé : patte sur la carte et à côté de l'objet dans la liste.

- [ ] (0.820) Ouvrez l'inventaire d'un servant qui garde un objet de votre liste : sa case est encadrée.






- [ ] (0.826) Clic droit sur un objet fabricable : « Ajouter à « Liste 1 » » ; il apparaît dans la liste.



- [ ] (0.829) Limsa, la Dunette : plus de marchand fantôme ; ailleurs dans Limsa, les marchands permanents sont là.


- [ ] (0.831) Enchaîner deux points du même genre en dépensant des PR au premier : le second propose un plan.



- [ ] (0.834) Clic droit sur Haricots émeraude dans la liste : « Y aller » et « Placer le drapeau ».



- [ ] (0.837) Clic droit sur la morille d'une ligne d'alarme ou de « /tensho linktest » : l'entrée Tensho ; clic droit sur le nom d'un joueur : pas d'entrée.




- [ ] (0.841) Liste 1 : sirop de bouleau 157/1 dans « Autres », icône d'entrepôt s'il est chez le servant.



- [ ] (0.844) Mor Dhona, quête de cuisinier : flèche vers le bas sur l'icône et la zone plus basse ; Carte › Alentours : le réglage.


- [ ] (0.846) Journal, « Carte » sur une quête : la carte centre dessus sans changer le zoom.






- [ ] (0.852) Fenêtre d'une quête, clic droit sur une récompense : entrée Tensho ; survol : panneau ou ligne « Item hover without panel » au journal.

- [ ] (0.853) Tillac de Limsa, clic droit sur la cible de quête : « Éthérite la plus proche », puis Limsa et l'éthernet jusqu'à la guilde.






- [ ] (0.859) Pêcher à un lieu avec un poisson commun : « ni heure ni météo (jeu) » ; un poisson à fenêtre garde sa fenêtre.


- [ ] (0.861) Fashion Report, fenêtre ouverte : les catégories et le thème ; après un jugement, Monde › Découvertes les compte.









- [ ] (0.870) Ouvrir le tableau du jeu sur un objet que vous vendez, puis la liste de ventes du servant : le verdict sans cliquer « Comparer » ; survol du prix le plus bas : « Vu au tableau du jeu à … ».

- [ ] (0.871) Tensho › Marché › Offres : l'étendue « Région » est nommée tout de suite (Europe, Japon…).

- [ ] (0.872) Ouvrir la boutique d'un marchand, puis Monde › Découvertes : « Marchands placés : 1 ».


- [ ] (0.874) Relevés en jeu par sonde (mode développeur, journal Dalamud « Reading <partie> » ou Monde › Découvertes) : pêche (fishing, fishline, statuts sous Pêche lente/Amorce/Prise forcée), réduction (purify), remise de quête (npctrade), île (island), aéronef revenu (airship), atelier (workshop) ; servant revenu de tâche → « Tâches des servants » ; coffre de donjon (fenêtre de butin) → « coffre de mission » ; ALÉA avec récompense → « récompense d'ALÉA » ; cible vaincue avec butin hors mission → « butin du monstre » ; Eurêka ou Bozja, même chose (en dernier) ; dresseur, bête apprivoisée → « apprivoisée de ».

- [ ] (0.875) Désynthétiser un objet, puis sa fiche › Usages : « Vu donner » ; vaincre un monstre et ramasser un objet, puis Monde › Monstres sur cet objet : « Vu le laisser ».

- [ ] (0.876) Après un coffre de mission ouvert : la fiche de la mission montre « Coffre (vu par vous) » ; clic droit sur l'objet : la mission proposée.


- [ ] (0.878) Combattre un monstre qui lance un sort de mage bleu (barre d'incantation) : Monde › Découvertes « vu lançant » ; Collections › Magie bleue, fenêtre du sort : « Vu par vous ».







- [x] (0.885) Monde › Découvertes : plus de « Récupérer » ni « Partager » ; « Échanger maintenant » marche toujours.


- [ ] (0.887) Pêcher deux ou trois poissons : panneau de pêche, détail du poisson : « Vu par vous … ! 13,6–18,8 s » ; Monde › Découvertes : « Touches de poissons vues ».


- [ ] (0.889) À la prochaine quête qui demande un objet : remettre l'objet, puis la fiche de l'objet › Usages : « Demandé par des quêtes (vu par vous) ».


- [ ] (0.891) Harpon : la liste du point reste affichée ; un poisson de la bonne ombre en vue passe en couleur « en vue ». Carte : les trous de pêche en poisson orange. Carnet de pêche › un trou › afficher sur la carte (pour la zone).














- [ ] (0.905) Carte et listes : toutes les coordonnées s'écrivent « 12.3 » avec un point (curseur, joueur, PNJ, ALÉA, signets, triple triade, île).






- [ ] (0.911) Monde › Découvertes : les faits de pêche portent un libellé de pêche ; les ALÉA à (0,0) ont disparu.


- [ ] (0.913) Survoler un bouton grisé (galerie d'outils pendant un chargement, carte d'un point sans carte) : l'info-bulle s'affiche ; les info-bulles longues ont toutes la même largeur.

- [ ] (0.914) Glisser un curseur de la carte ou de l'équipement : l'effet suit en direct ; les réglages sont bien gardés après un rechargement.

- [x] (0.915) Réglages : chaque case a son bouton ↺ ; survoler un réglage : dernière ligne « Par défaut : … ».


- [ ] (0.917) Page Pêche en mer : une ligne d'état par port (« pas encore vérifié » tant que vous n'avez pas embarqué). À bord : l'aide à la pêche ordinaire ne s'ouvre pas, le panneau de voyage oui ; dalamud.log contient « Ocean schedule sighting ». Après un voyage de chaque port : l'état change.

- [ ] (0.918) En pêche en mer : le panneau de voyage montre les icônes, l'appât en main encadré en vert, la touche en couleur, le temps de touche et le chronomètre ; « Poissons du courant spectral » en vert, à ouvrir.

- [ ] (0.919) Atelier de la compagnie, après avoir consulté les résultats de l'aéronef : onglet des navires, « Dernier voyage : Mer de nuages - secteur 1 › … ».

- [ ] (0.920) Pages avec une erreur de source (source coupée ou hors ligne) : ligne rouge ; pages vides : ligne grise ; un bouton Copier : « Copié » à côté pendant deux secondes.

- [ ] (0.921) Parcourir quelques pages avec des icônes (contenus, actions, cartes de Triple Triade, carte du monde, vues panoramiques) : les icônes sont là et à la même taille.






- [ ] (0.927) Survoler un objet qui apprend une emote ou une coiffure, déjà apprise puis non apprise : ligne « Apprend quelque chose : … » verte ou rouge.


- [ ] (0.929) Carte d'une zone de Stormblood ou plus récente : survol d'une vue panoramique, « Carnet Stormblood n° 0xx » ; la liste des vues de la carte a ses numéros.







- [ ] (0.936) Rappels, « Vos personnages » : colonne « Autres » avec mandats, livraisons et quêtes tribales du personnage connecté ; survol pour l'escouade.








- [ ] (0.944) Paramètres : info-bulles relues ; carte › réglages : une seule case « zones de quête » (section Zones) ; craft › attente des bonus : le bouton par défaut revient à la valeur des actions.















- [ ] (0.959) Aller au Détroit ouest de Zorgor (ou une zone de Shadowbringers), attendre 10 s : l'aide à la pêche montre « météo : … · météo d'avant : … ».

- [ ] (0.960) Carte du Détroit ouest de Zorgor : survol du trou de pêche, plus de « point légendaire » ni de tome de mineur.










- [ ] (0.970) Quêtes : survoler une quête, ligne « Extension : … ». Survoler un objet récolté (ex. un minerai de Heavensward) : ligne « Extension (d'après ses lieux de récolte) : Heavensward ».





- [ ] (0.975) Page Eurêka et Bozja : onglets ou zones du Croissant occulte présents, avec des ALÉA et des affrontements placés.


- [ ] (0.977) Cibler un lieu d'une autre zone, se téléporter dans la zone, ouvrir la carte Tensho sans ouvrir celle du jeu : les marqueurs rouges des quêtes pas encore acceptées sont là.

- [ ] (0.978) Planter (le journal note l'emplacement), à maturité récolter : ligne « Jardin : … récolté, retiré du journal ».


- [ ] (0.980) Dresseur › Équipe › 1er plateau : 10 bêtes listées, puis pour chaque combat les ennemis avec leur faiblesse et 3 bêtes ; onglet Plateaux : étoiles des ennemis (Chevalier du plateau : For 2 · Int 1 · Rés. phys. 2 · Rés. mag. 2 · Vit 3).


- [ ] (0.982) Aide à la pêche dans un lieu avec un poisson collectionnable : ligne « Collectionnable : … / … / … », survol : assignats de chaque palier.







- [ ] (0.989) Collections › Tenues : déplier une tenue incomplète, « Pièces manquantes → souhaits » : elles sont dans Souhaits ; « Fabricables → … » : dans la liste d'artisanat.

- [ ] (0.990) Ouvrir Butin des contenus (lecture), se déconnecter, rouvrir la page : états affichés et ligne « État enregistré de votre dernier personnage ».




- [ ] (0.994) Butin des contenus › un donjon › « Mécaniques et stratégie (wiki) » › Lire sur le wiki : sections Objectives, boss… ; en combat : « Masqué pendant un combat ».

- [ ] (0.995) Butin des contenus › un raid › « Vos notes et schéma d'arène » : écrire une note, ajouter T1 et A, les glisser, copier le code, coller dans un autre contenu.




- [x] (0.999) Paramètres : sept onglets ; « Chercher un réglage » : taper « wiki » : la ligne Wiki · Données et sources, un clic ouvre l'onglet.












- [ ] (0.1011) Pêcheur : lancer, attendre une touche, relancer sans cliquer sur « Relancer » : le chrono repart de 0 à chaque lancer (aussi au vif) ; un leurre ne le relance pas ; le bouton le remet à zéro à la main. Même chose dans le panneau de la pêche en mer.


- [x] (0.1014) Accueil, « Tous les outils » : chaque page a sa ligne (Champs de Bozja et Eurêka, Lexique, Île, Jardin, etc.), « Ouvrir » mène à la page, « Détails » montre son but ; les anciennes lignes (Craftimizer, Banque, Item Use) ouvrent toujours leur onglet


- [ ] (0.1015) Aide, « Questions fréquentes » : 15 questions en français (puis en anglais après changement de langue) ; taper « dépôt » ou « langue » dans la recherche de l'Aide ne garde que les questions qui en parlent ; « Comment changer la langue ? » montre un bouton « Ouvrir cette page » qui mène aux Paramètres ; une recherche sans réponse dit « Aucune question pour cette recherche. »

- [ ] (0.1031) Application de bureau, jeu fermé : décompresser TenshoAssistant.Desktop.zip (ou ouvrir src/TenshoAssistant.Desktop/bin/package) et lancer TenshoAssistant.Desktop.exe par un double-clic, sans rien installer : la fenêtre s'ouvre sans console, les catalogues se chargent (« Lecture des données du jeu : N catalogues prêts sur 12 »)





- [ ] (0.1033) Application de bureau 0.1033 : la barre d'état a une ligne de mise à jour (boutons « Mise à jour automatique : active », « Chercher une mise à jour » ; texte « cette application est en 0.1033.0, la version du plugin installé » ou « pas encore de release ») ; un clic sur « Mise à jour automatique » la coupe et le garde au démarrage suivant

- [ ] (0.1033) Après la première release qui porte l'application : une application plus ancienne lancée se met à jour seule vers la version du plugin installé (téléchargement, puis ouverture au démarrage suivant ou sur « Redémarrer maintenant ») ; « Version d'avant » revient à la précédente


- [ ] (0.1038) Thème clair (Paramètres › Général › Thème de l'interface) : titres de catégorie de la barre latérale (CONTENUS, GOLD SAUCER, RÉFÉRENCES, PRÉFÉRENCES) et « OBJET EN CONTEXTE » bien lisibles ; en thème sombre aussi ; une catégorie repliée qui contient la page ouverte reste dorée







- [ ] (0.1041) Carte de Tensho, réglage « Points de récolte à horaire ouverts » (repères au bord) : les points de récolte à horaire ouverts en ce moment ont toujours leur repère au bord ; un point dont le tome n'est pas lu n'en a pas ; aucun lieu de pêche n'apparaît ou ne disparaît à tort.

- [ ] (0.1042) Paramètres › Alarmes, « Créer aussi les alarmes dans le jeu » cochée : la case « Envoyer aussi sur Discord » apparaît dessous, décochée ; une alarme copiée dans le jeu ne part plus sur Discord ; case cochée (webhook activé), elle y part à l'heure, sans sonner deux fois dans le jeu.

- [ ] (0.1043) Alarmes : « Supprimer » sur une alarme : le bouton devient « Cliquer encore pour supprimer » en rouge, l'alarme reste ; un second clic dans les 5 secondes la supprime ; après 5 secondes, le bouton revient et un clic ne fait que l'armer de nouveau.




- [ ] (0.1044) Paramètres › Données et sources : sous Garland Tools, le wiki et Universalis, les interrupteurs d'usage sont en retrait, activés, chacun avec son infobulle (en et fr) ; couper Garland Tools grise ses trois usages ; cocher « Données du jeu et observées seulement » grise tout.

- [ ] (0.1044) Couper « Butin des contenus » seulement : la page Contenus et le panneau de la recherche des contenus ne demandent plus le butin (bouton grisé, « Source désactivée ») ; « Butin des monstres » reste actif sur la page Monstres.





- [ ] (0.1045) Visite guidée, étape « Centres d'intérêt » : trois cases nouvelles (Contenus et progression, Gold Saucer, Jardinage et Félicité insulaire), en anglais et en français ; ne rien cocher montre les 49 arrêts, cocher « Artisanat » seul garde les arrêts communs et ceux de l'artisanat (Artisanat en direct, Préréglages, Chaîne de macros) sans les mandats ni le jardinage.


































- [ ] (0.1046) Après le prochain patch du jeu, au premier chargement de Tensho : dalamud.log contient une ligne « Patch changes: catalog-… : ancienne -> nouvelle version, N added, N removed, N changed » par catalogue refait (objets, recettes, récolte, positions, marchands) ; au chargement suivant, les mêmes lignes en « Patch changes (kept) ». Le dossier DataCache-v1\previous du plugin contient une copie par catalogue et patch-reports.json.



- [ ] (0.1049) Carte de Tensho, carte d'une zone ouverte (par exemple La Noscea centrale) : souris sur la carte → icône de la météo actuelle dans le coin bas-droit, même météo que la boussole du jeu ; info-bulle « Météo actuelle de la zone : … » ; souris hors de la carte → l'icône disparaît. Carte de région (par exemple La Noscea) : une icône par zone au-dessus de son image, chacune juste ; zone à météo individuelle (histoire non faite) : pas d'icône ; aucune erreur dans dalamud.log.

- [ ] (0.1050) Après un chargement du plugin (rechargement ou démarrage du jeu) : dalamud.log contient une fois la ligne « JIT warm-up: N methods of the window and the plugin compiled ahead in T ms, K skipped. » (N vers 27 000, K petit), aucune erreur ; pas d'à-coup sensible du jeu dans les secondes qui suivent. Au balayage des pages suivant (lignes « Selftest draw »), la première image de Gear, SortPreview, Reminders et Storage plus courte qu'avant (161, 131, 92 et 113 ms le 11/11 à 2 h 14).


- [ ] (0.1081) Paramètres › Avancé › « Lien avec l'application de bureau » : désactivé au départ, son état dit « Lien : désactivé ». L'activer : « Lien : en attente de l'application de bureau. » ; dalamud.log : « Local link: Waiting. »






- [ ] (0.1094) Pêche en mer : en plein voyage, recharger Tensho (Dalamud › Plugins installés, désactiver puis réactiver) : la fenêtre du voyage garde les « … pts » des poissons déjà pris et les missions ; dalamud.log n'écrit pas une deuxième ligne « Ocean schedule check » pour ce voyage.

- [ ] (0.1095) Pêche en mer : la dernière ligne de la fenêtre du voyage (« Points : … · poissons pris : … ») suit les points du jeu pendant le voyage (somme des « … pts » du tableau) ; en fin de voyage, dalamud.log contient « Ocean voyage result » avec des points non nuls et « Tensho counted ».

- [ ] (0.1095) Pêche en mer, mode développeur : les lignes « Slow frame » ne nomment plus TenshoOceanVoyage au-delà de 100 ms (mesure d'avant : 160 à 260 ms, OceanVoyage 75 à 90 ms) ; « ocean fish table » donne le coût du tableau ; « Discovery: bite strength unknown » devient rare.

- [ ] (0.1096) Retours d'aéronef et de sous-marin, tâches des servants, missions d'escouade (mode développeur, journal des découvertes) : dalamud.log écrit « Item gain attributed: … from AirshipReturn / SubmarineReturn / Venture / Squadron » au lieu de « Item gain not attributed » ; une nouvelle destination ou un rang donne « Discovery: airship new destination … » ou « submarine rank … ».

- [ ] (0.1097) Paramètres › Artisanat, « Son de la ligne de fin de macro » : « Écouter » joue le même son qu'un <se.N> tapé dans le chat pour N = 1, 8 et 16 ; à 0 le bouton est grisé.

- [ ] (0.1098) Carte : poser un drapeau dans une zone, se téléporter dans une autre zone, ouvrir la carte (M) : la carte de Tensho montre la zone du joueur, pas celle de l'ancien drapeau ; retirer le drapeau puis rouvrir : idem. Un lien de quête du journal ouvre toujours sur la quête. dalamud.log : « Game map request kept from before ».



- [ ] (0.1102) Pêche en mer : réussir une mission du voyage (gils gagnés) : Rangements › Gils, la ligne est dans « Pêche en mer (missions) », pas dans « Autre ».


- [ ] (0.1106) Barre latérale, OBJETS › Souhaits : la page s'ouvre (plus d'onglet Souhaits dans Collections) ; clic droit sur un objet › « Ajouter à mes souhaits » : la ligne « ajouté à vos souhaits » en bas de la fenêtre, son bouton ouvre la liste

- [ ] (0.1106) Souhaits avec un objet récolté sur un point limité (ex. un objet de légende) : sous son nom, « Récolte : métier · zone · point · heures » ; « Ajouter les alarmes (N) » les pose, un deuxième clic n'en ajoute pas


- [ ] (0.1107) Carte de Tensho, clic droit › onglet Quêtes : la case « Quêtes pas encore disponibles » y est (plus au-dessus des onglets) ; décochée, les quêtes pas encore disponibles quittent la liste et leurs icônes la carte

- [ ] (0.1108) Récolte avec un objet caché (ex. Graines de pommier luisant) révélé après une dépense de PR : le panneau donne un plan pour lui, avec la ligne « Objet révélé après une dépense de PR : plan établi avec ce qui restait… »

- [ ] (0.1109) Jardin › journal, un potager planté (au moins deux parcelles voisines de graines qui se croisent) : sous la parcelle plantée en second, « Peut donner : … (avec la parcelle N, …) » ; une graine pas possédée ou souhaitée est redite en couleur ; la ligne « Prévisions : … » sous la grille

- [ ] (0.1113) Paramètres › Avancé › Application de bureau : l'état dit « pas installée » (ou la version installée) ; « Installer l'application de bureau » : la ligne passe par « demande à GitHub », « téléchargement », « dépaquetage », puis « installée » ; l'application ne s'ouvre pas seule






- [ ] (0.1115) Dédale bestial (dresseur) : à l'entrée, pas de ligne « Tensho en pause » dans le chat ; les pages et panneaux de Tensho restent ouverts pendant le plateau et les combats ; en JcJ, la coupure se fait toujours

- [x] (0.1116) Mode développeur activé, jouer un plateau du Dédale jusqu'au résultat : dalamud.log contient « Labyrinth reading: … windows: … » avec le nom de la fenêtre du résultat et des lignes « Labyrinth window XBM…: values=… » (nombres seulement) ; les donner à l'agent pour coder X-135





- [ ] (0.1119) Lien activé avec une application de bureau plus ancienne (0.1116) : la nouvelle dit « le jeu offre le protocole de lien 2 / 3 » selon le côté, sans planter

- [ ] (0.1120) Lien activé, application de bureau ouverte : changer plusieurs réglages de suite dans le jeu ; l'application dit une fois « Réglages du plugin relus à … » et ses pages suivent ; la page Discord de l'application ne montre aucune adresse

- [ ] (0.1122) Paramètres › Cache : « Vider le cache », Oui : le message dit le nombre de fichiers retirés et la copie (dossier Backups) ; les pages qui lisent le cache se rechargent ensuite normalement


- [ ] (0.1125) Aide › Signaler un problème : remplir un bug avec un chemin « C:\Users\Nom Prénom\x » dans le titre, « Préparer le formulaire GitHub » : l'aperçu montre <path>, rien ne s'ouvre ; changer un champ ferme l'aperçu ; « Ouvrir dans mon navigateur » ouvre le formulaire avec exactement le texte de l'aperçu (ne pas l'envoyer)


- [ ] (0.1128) Carte › Carnet d'exploration : un panorama dont la fenêtre est ouverte dit « possible maintenant, jusqu'à HH:MM » et le jeu l'accepte ; un panorama hors de son heure n'est pas dans « Possibles maintenant » ; son infobulle dit ce qui manque et la prochaine fenêtre (Le Viaduc 020 : 05:00–07:59 ET, Ciel dégagé ou Beau temps)

- [ ] (0.1128) Infobulle d'un panorama à météo rare (La Cité invisible 019, Nuages) : une prochaine fenêtre est donnée, ou « Aucune fenêtre dans les 30 prochains jours », jamais rien

- [ ] (0.1129) Barre d'infos du serveur avec les alarmes, les rappels, les monnaies et les gils : chaque infobulle s'ouvre juste sous son entrée, sans sortir de l'écran ; avec « Au-dessus de l'entrée » coché et la barre en haut de l'écran, elles restent en dessous (pas de place au-dessus)

- [ ] (0.1130) Carte de Tensho : les icônes de la colonne d'outils sont centrées dans leurs boutons ; cliquer le pourcentage, taper 150 puis Entrée : zoom à 150 % ; « 100% » sous le pourcentage remet 100 % ; la colonne reste lisible sur la carte (superposée) et dans une petite fenêtre

- [ ] (0.1131) Application de bureau, jeu lancé, lien activé : Stockage, Listes d'artisanat, Mes outils, Historique des voyages montrent les données du personnage ; changer une liste d'artisanat en jeu : l'application la montre quelques secondes après ; le dossier pluginConfigs\TenshoAssistant n'est pas modifié par l'application

- [ ] (0.1132) Application de bureau ouverte, jeu derrière : une alarme Tensho (non copiée dans le jeu) sonne dans l'application (son Windows, ligne « Alarme : … » dans la barre d'état) ; le jeu au premier plan : rien dans l'application (réglage par défaut) ; décocher « seulement si le jeu n'est pas au premier plan » : elle sonne des deux côtés

- [ ] (0.1133) Découvertes › Importer : un fichier posé dans le dossier Import est joint ; il garde son nom « .json » jusqu'à la sauvegarde suivante (une minute au plus), puis devient « .json.imported » ; un deuxième clic sur Importer entre-temps ne le joint pas deux fois


- [ ] (0.1136) Après le chargement de 0.1136 : dalamud.log dit « Discoveries: SQLite 3.54… database, N facts of yours… the files of before kept in before-sqlite-… » ; Monde › Découvertes montre les mêmes totaux qu'avant ; le dossier Discoveries contient Discoveries.db et before-sqlite-<date> (Discoveries.json d'avant intact)


- [ ] (0.1137) Monde › Découvertes, connecté à GitHub, « Échanger maintenant » : message sans erreur ; sur github.com, votre gist contient index.json et des fichiers kind-NN.jsonl.gz.b64, plus l'ancien tensho-discoveries.b64 ; un deuxième échange sans nouveau relevé n'envoie rien (« déjà envoyé »)


- [ ] (0.1139) Stockage › Journal des gils, après la mise à jour : les entrées d'avant sont toutes là ; dalamud.log dit « Gil journal: SQLite … data file TenshoData.db » puis « a character's file of before moved to TenshoData.db » ; GilJournal/before-sqlite-<date>/ contient l'ancien fichier


- [ ] (0.1140) Collection › Succès, après la mise à jour : la progression vue avant est toujours là ; dalamud.log dit « Tensho data: the achievement progress file of before moved to TenshoData.db » ; before-sqlite-<date>/AchievementProgress.json existe à la racine du dossier du plugin



- [ ] (0.1141) Salle de jeu de la Triple Triade (Gold Saucer, 579) : Tensho reste allumé dans la salle publique et se coupe dans un match de l'arène ; dalamud.log montre « Competitive arena check » sans avertissement

- [ ] (0.1142) Deux clients du jeu sur le même profil : lancer le second pendant que le premier enregistre la banque ; Stockage › Banque du second dit d'abord « attend », puis se charge seul en quelques secondes



- [ ] (0.1144) Collection › Magie bleue : la liste du grimoire se remplit comme avant ; « Lire les pages manquantes » avance page par page ; dalamud.log sans erreur


- [ ] (0.1148) Atelier de la compagnie › historique des voyages : après le rechargement, la même liste qu'avant ; dalamud.log dit le fichier d'avant déplacé vers TenshoData.db ; au prochain retour d'un sous-marin, le voyage s'ajoute


- [ ] (0.1149) Stockage › Banque (tous les personnages, servants, sacoches, arsenal, coffre de compagnie) : après le rechargement, tout ce qui était enregistré est là ; dalamud.log dit le fichier d'avant déplacé vers TenshoData.db ; before-sqlite-<date>/InventoryBank/ existe à la racine du dossier du plugin ; une nouvelle lecture d'un servant s'enregistre




- [ ] (0.1150) Jeu : Collection › Succès, le bouton « Demander » est toujours là et marche comme avant








- [ ] (0.1158) Paramètres › Avancé › Application de bureau › « Pages dans le jeu et dans l'application de bureau » : mettre « Objets » sur « Application seulement » : la page quitte la navigation du jeu et reste dans celle de l'application ; « Marché » sur « Jeu seulement » : l'inverse ; Accueil, Paramètres, la carte, l'artisanat en direct et la Revue de mode ne sont pas proposés ; « ↺ » remet « Jeu et application »

- [ ] (0.1159) Rappels › onglet « Alarmes (N) » : les alarmes posées avec une cloche des minuteurs et les alarmes automatiques (servants, voyages, escouade) y sont, avec leurs boutons ; « Rappels (N) » dans le menu ; l'outil Alarmes ne les liste plus et son chiffre ne les compte plus ; elles sonnent toujours

- [ ] (0.1160) Jardinage › onglet « Alarmes (N) » : chaque lit noté au journal montre son prochain entretien et sa récolte avec l'heure ; « Jardinage (N) » dans le menu ; « Me le rappeler dans le chat » décoché : l'onglet le dit et le chiffre disparaît

- [ ] (0.1161) Découvertes › échange GitHub : un joueur déjà connecté voit « Tensho se connecte maintenant à GitHub par son application GitHub… » et se reconnecte ; la page d'autorisation de GitHub nomme l'application de Tensho et ne demande que les tickets du dépôt et les gists ; un envoi crée le gist secret et le ticket « Discoveries: <login> »

- [ ] (0.1163) Dédale bestial, à la fin d'un plateau : Dresseur › Équipe montre « Dernier plateau (date) : score N » avec le même score que la fenêtre du jeu, et pour chaque bête « rang avant (exp/100) → rang après (exp/100) » comme le jeu ; dalamud.log a la ligne « Beastmaster board result read: score N, 10 beasts »


- [ ] (0.1164) Après quelques changements de réglage (fenêtre, alarmes) : dalamud.log n'a plus de ligne « Slow settings save » au-delà de 50 ms ni « Long UiBuilder(Tensho Assistant) » au moment d'un clic sur un réglage ; aucune ligne « saved on the game's thread, as before »


- [ ] (0.1165) Jardin avec plusieurs emplacements notés : clic droit sur une plante (menu « Potager N, emplacement M »), Entretenir : dans Jardinage, seul cet emplacement passe à « entretenu » (heure d'entretien suivante repoussée), les autres et l'onglet Alarmes suivent ; en mode développeur, dalamud.log a « Garden menu read: window …, patch N, bed M. »



- [ ] (0.1166) Carte de Tensho sur la Forêt centrale : clic droit, onglet Éthérites : chaque éthérite a son nom (plus d'icône seule) ; survol de l'icône de l'éthérite de Bentbranch : son nom ; la recherche de la carte trouve « Bentbranch »

- [ ] (0.1167) Point de récolte avec un objet « RARE » (une seule prise) : le prendre, puis l'objet suivant proposé : son plan dit « Plan établi sur ce qui reste à ce point (N PR, 5 essais) » et compte sur les essais restants, plus sur ceux de l'ouverture ; l'action suivante proposée correspond au plan

- [ ] (0.1168) Application de bureau, page Mes outils : boutons Créer, Importer, Galerie, Modifier, Retirer et cases de progression grisés ; barre d'état « Lecture seule : ce que vous créez ou changez ici n'est pas enregistré »

- [ ] (0.1168) Application de bureau, page Alarmes : alarme manuelle grisée ; page Paramètres : changer un réglage le remet aussitôt et le bas de la fenêtre dit « Non enregistré : … faites ce changement dans le jeu »

- [ ] (0.1168) Dans le jeu : créer une alarme, un outil et un préréglage d'artisanat fonctionne comme avant (rien de grisé)

- [ ] (0.1169) Atelier de la compagnie (sous-marins revenus) : l'historique des voyages garde les voyages d'avant après un rechargement du plugin, et les nouveaux s'ajoutent

- [ ] (0.1169) Hauts faits suivis : la progression lue reste après un rechargement ; dalamud.log sans erreur « Tensho data »

- [ ] (0.1171) Monde › Découvertes, échange GitHub connecté : « Échanger » envoie toujours ; « Déconnecter » efface la connexion (plus de nom affiché) ; dalamud.log sans erreur GitHub

- [ ] (0.1172) Objets › Organisation, sacs du personnage : l'aperçu remplit le sac 1, puis le sac 3, puis le 2, puis le 4 ; « Appliquer » range les sacs du jeu de la même façon ; une sacoche ou un servant se range comme avant

**0.535 : interfaces** — à voir en jouant

- [ ] Carte › Paramètres : « Sans barre de titre » : plus de barre ; elle se déplace par sa colonne d'outils et se ferme avec la touche de la carte. Le déplacement par la colonne reste à confirmer.

**0.534 : Équilibré grisé quand il revient au même** — à voir en jouant


**0.533 : les coups de chaque cran** — à voir en jouant


**0.532 : panneau Collectionnable sans répétitions** — à voir en jouant


**0.531 : description chiffrée des crans** — à voir en jouant


**0.529 : trois crans de risque des collectionnables** — à voir en jouant

- [ ] Panneau Collectionnable (0.530) : trois icônes, main, balance, dé ; le nom au survol ; le cran choisi coloré, sa description dessous ; même choix dans Paramètres › Modules › Récolte.

**0.527 : Toucher d'amorçage dans le plan des collectionnables** — à voir en jouant

- [ ] Botaniste ou mineur niveau 95 et plus : commencer vers 623 PR, lancer Inspection soi-même (il reste 423 PR) : le panneau propose Toucher d'amorçage ; une fois lancé, il ne le repropose plus et compte 48 %. Depuis 727 PR, le plan passe par Inspection puis Sélection méthodique, sans Toucher : c'est prévu (capture du 2026-11-05).
- [x] (rapport de l'utilisateur, 2026-11-05) Toucher reste actif si une autre action passe avant la Sélection méthodique ; il ne retombe qu'après elle. Récolte non essayée.

**0.526 : légendes locales dans les Collections** — à voir en jouant

- [x] (rapport de l'utilisateur, 2026-11-04, « legendes local ok ») Collections › Légendes locales : 36 tomes, marqués lus ou non comme dans votre personnage. Si un tome est mal marqué, la correspondance des bits est fausse : la carte se trompe aussi.
- [ ] Clic droit ou survol d'un tome : comment l'obtenir.

**0.525 : groupe sur la carte, coût du chargement** — à voir en jouant

- [x] (rapport de l'utilisateur, 2026-11-04, « groupe parfait ») Membres du groupe sur la carte : déplacement fluide quand ils sont en vue ; à distance, ils glissent entre deux positions ; une téléportation saute d'un coup.

**0.524 : Ctrl+Alt actualise le prix survolé** — à voir en jouant

- [x] (rapport de l'utilisateur, 2026-11-04, « ctrl alt ok ») Survoler un objet vendable, attendre le prix, Ctrl+Alt : les secondes de « Récupéré à » changent. Maintenu : une seule demande. Relâché et rappuyé : au plus une toutes les 5 s.

**0.523 : assignats des collectionnables d'un nœud** — à voir en jouant

- [x] Sur un nœud à deux collectionnables : les assignats par palier sous chacun, identiques au tableau Récolte › Collectionnables, et « le plus au palier haut » sur le bon (rapport de l'utilisateur, 2026-11-04).
- [ ] Deux objets aux types d'assignats différents : la phrase « types différents », sans marquage.

**0.522 : temps de recharge de Bénédiction de la nature** — à voir en jouant


**0.520 : tentative gratuite dans le plan des collectionnables** — à voir en jouant

- [ ] Niveau 90 et plus, PR pour Sagesse du fermier ou Raison solide : la moyenne dépasse le pire cas, et la ligne « 50 % de chances d'une tentative gratuite » apparaît.

**0.519 : éclats, cristaux et agrégats** — à voir en jouant

- [ ] Éclat, cristal ou agrégat : le panneau propose Bénédiction de la nature et Bénédiction des Douze (0.521), sans « ≈ N objets ».

**0.518 : carte, tomes et niveau d'un point** — à voir en jouant

- [ ] Point d'Ostrakon Dékaokto : « ouvert, mais pas encore pour vous », le nom du tome, horloge grise. Le journal écrit « Gathering knowledge: … » : les nombres entre crochets correspondent-ils aux tomes lus ?

**0.517 : plan de PR** — à voir en jouant

- [ ] Sur un nœud à PR pleins : le panneau propose des actions, et elles existent toutes dans votre barre.

**0.516 : panneaux accrochés au jeu en fenêtres Dalamud (#38)** — à voir en jouant

- [ ] Chaque panneau s'affiche au même endroit, avec le même contenu : carnet d'artisanat, projection pendant une synthèse, récolte, collectionnable, pêche, harpon, pêche en mer, survol d'objet, outil de mission, Revue de mode, Personnage, ventes des servants, demande de Triple Triade, import de banque (si la capture automatique est coupée), bouton « Trier ».
- [ ] Le bouton « Trier » trie, sans fond ni bord ; le panneau de survol ne prend pas les clics.

**0.515 : aide à la récolte, plan du nœud et effet du lieu** — à voir en jouant

- [ ] Nœud avec « PR ≧ 770 → tentatives +1 » (capture du 2026-11-04) : avec 809 PR, un plan d'actions apparaît, et la ligne « Effet du lieu actif : tentatives +1, déjà comptées (5 au lieu de 4) ».

**0.514 : panneaux libres en fenêtres Dalamud (#38)** — à voir en jouant

- [ ] Fenêtre de suivi (réglage) : s'affiche à sa place habituelle, liste les alarmes, le nom ouvre l'objet, « Carte » ouvre le point ; la croix la ferme et décoche le réglage ; recocher la rouvre.
- [ ] Barre de PR en mineur : se remplit, le repère du tic avance ; disparaît à PR pleins et sur un autre job ; PR mesurés même barre coupée.
- [ ] Guide et carte : la croix les ferme du premier coup.

**0.510 : conseils de Triple Triade en JcE seulement** — à voir en jouant

- [ ] Match contre un PNJ : conseils comme avant. Au Battlehall, en tournoi ou contre un joueur : pas de cadre, et le panneau dit pourquoi.

**0.509 : coup conseillé pendant une synthèse (A-04)** — à voir en jouant

- [ ] Dans « Comparer les prochains coups », pendant une synthèse : « Conseillé : … » change à chaque coup et suit l'état (sur Bon, un coup de cet état devrait souvent sortir, sans que ce soit une règle). Rien n'est joué. Réglage dans Paramètres › Modules › Artisanat. Pas de saccade.

**0.508 : ventes des servants (I-05)** — à voir en jouant, à la sonnette

- [x] Ouvrir la liste de ventes d'un servant : le panneau « Vos ventes » s'affiche à côté, avec objets, quantités et prix comme le jeu, HQ compris.

**0.506 : vidéos et guides de la communauté (ONBOARD-001, lots 7 et 8)** — après une release

- [ ] « ? » › Vidéos et guides : la liste se lit sur `main`, donc rien avant une release : « n'a pas pu être lue ». Après une release : « pas encore de vidéo ». Avec une ligne ajoutée : le titre, l'adresse au survol, la confirmation, le navigateur.

**0.505 : guide des premiers pas (ONBOARD-001, lots 4 à 6)** — à voir en jouant

- [ ] Sur l'accueil, joueur existant : une proposition unique, « Non merci » la retire pour de bon. Vraie première installation : il s'ouvre seul à la première ouverture de Tensho (à tester avec une configuration vide ou par un testeur).

**0.504 : signaler un problème (ONBOARD-001, lot 3)** — à voir en jouant


**0.502 : aide (ONBOARD-001, lot 1)** — à voir en jouant

- [ ] Un « ? » en haut à droite de la fenêtre Tensho explique la page ouverte, sur toutes les pages ; « Toute l'aide » mène à Préférences › Aide.
- [x] Page Aide : la recherche trouve une page d'après ce qu'elle fait ; « Ouvrir cette page » y mène ; questions fréquentes.
- [ ] 0.503 : première ouverture d'une page, un bandeau de deux lignes ; « Compris » le retire pour de bon ; « En savoir plus » ouvre l'aide. Réglage dans Paramètres › Général. Le « ? » dit ce qui est lu ou non sur ce personnage.

**0.501 : sonde des fenêtres du dresseur** — à faire en jeu, mode développeur


**0.500 : page Dresseur** — à voir en jouant

- [ ] Progression › Dresseur › Bêtes : 50 bêtes, nom, classe, attaque auto. comme le bestiaire. Rang 6 : Cu sith = FOR 161, INT 100, Rés. phys. 90, Rés. mag. 86, VIT 131. Une autre bête, à comparer aussi.

**0.499 : la carte au-dessus de Tensho** — à voir en jouant

- [ ] Depuis une page Tensho, cliquer un lien vers la carte (« Voir sur la carte », favori d'une autre carte) : la carte passe devant Tensho, qu'elle soit déjà ouverte ou non. Cliquer ensuite sur Tensho le remet devant.

**0.498 : Tensho replié, puis demandé** — à voir en jouant

- [ ] Replier la fenêtre Tensho par sa barre de titre, puis ouvrir quelque chose qui la demande (clic droit sur un objet › outil, `/tensho`, alarme) : elle se déplie sur la bonne page. Elle se replie encore normalement ensuite.

**0.497 : état de la synthèse dans « Comparer les prochains coups »** — à voir en jouant


**0.494 : auto-contrôle** — à voir en jouant

- [x] `/tensho selftest` et le bouton de Diagnostics relancent une passe ; Diagnostics montre les lignes.
- [ ] 0.496 : fenêtre ouverte puis refermée, sans lancer de passe : la passe suivante donne OK_SEEN avec l'heure. Ça reste après un rechargement.
- [ ] 0.495 : carnet d'artisanat ouvert, la ligne « crafting log » donne une recette ; synthèse en cours, « synthesis » passe en OK ; point de récolte ouvert, « gathering node » aussi.

**0.493 : disponibilité des PNJ de Triple Triade** — à voir en jouant

- [ ] Triple Triade › PNJ et cartes : colonne « Disponible ». Un PNJ marqué « Quête requise » refuse bien le défi ou n'est pas là en jeu, et un PNJ marqué « Oui » accepte. Le survol donne les quêtes, avec ✓ pour celles faites. À signaler surtout : un PNJ à deux quêtes dont une seule est faite (lecture « toutes » déduite, voir docs/evidence/0.493-triad-npc-requirements.md).

**0.491 : R-14, valeurs inconnues** — à voir en jouant

- [ ] Défi Triple Triade et match : règles et conseils comme avant, jamais « les règles n'ont pas pu être lues ». Le journal donne la ligne `Triad request rule values` (preuve à verser dans docs/evidence, défis à 1, 2 et 4 règles si possible).
- [ ] Coffre à apparence ouvert avec une tenue complète : ses pièces comptent toujours comme possédées. Avec une tenue partielle : seules les pièces rangées comptent. Le journal donne `Dresser outfit … slots held` (preuve à verser dans docs/evidence).

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

- [ ] Après la prochaine release (la galerie est lue sur `main`) : Galerie › Charger la liste, Voir, l'aperçu, Installer. Avant cette release, le message « n'a pas pu être lue » est normal.

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

- [ ] Ouvrir le calendrier du jeu puis les missions de ravitaillement : la bulle d'un objet demandé dit « demandée aujourd'hui ». Un autre objet possible dit « absente des demandes du jour que vous avez ouvertes ». Le journal Dalamud donne une ligne `Grand Company supply window`.
- [ ] Carnet : panneau en sections (recette, votre état, cible, calcul, macro), chiffres en tableau, barre de qualité verte si la cible est atteinte ; explications dans la bulle de « (?) ».

**0.468 : carnet (cible, état, meilleure séquence) et cache de Raphael** — pendant la campagne de tests en jeu (phase 6)

- [ ] Raphael choisi, carnet : une recette calculée puis re-sélectionnée revient tout de suite, le tampon dit « Cache disque ». Après un changement d'option de macro ou une nouvelle version de la DLL : « Solveur : Raphael (séquence déjà calculée…) ».

**0.467 : Raphael dans le carnet** — pendant la campagne de tests en jeu (phase 6)


**0.465 : solveur Raphael** — pendant la campagne de tests en jeu (phase 6)

- [x] Artisanat › Recherche : choisir « Raphael », lancer : une séquence arrive, « Annuler » fonctionne ; le jeu reste fluide pendant le calcul.
- [ ] La séquence jouée en jeu donne le résultat annoncé. Retour à « Tensho » : la recherche d'avant.
- [ ] Le plugin se recharge sans erreur après un calcul.

**0.464 : retours du lot 5** — pendant la campagne de tests en jeu (phase 6)

- [ ] Simulateur, métier porté : attributs copiés d'office, ligne « Détecté » juste (repas, remède, Manipulation, spécialiste) ; valeurs en vert ou rouge selon la recette ; « Conserver la séquence » après la palette.
- [ ] Onglet « Liste de fabrication » : icônes, infobulle complète au survol, manquants en rouge.
- [ ] Pendant une synthèse : la fenêtre de projection s'ouvre à côté de celle du jeu, se ferme avec elle, et ses chiffres correspondent au résultat de l'action jouée.

**0.462 : retours du lot 3** — pendant la campagne de tests en jeu (phase 6)

- [ ] Clic droit sur un objet › Tensho : plus de « Tous les outils avec cet objet » ; le reste du menu est là.
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
- [ ] Téléportation : coûts affichés ; mandats, collections supplémentaires et chocobo lus une fois connecté.

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

- [ ] Redémarrer le jeu après un rappel : il n'est pas répété.

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

- [ ] Cartes › Annuaire : ajouter une maison (monde, quartier, secteur, parcelle), un appartement ; « Copier » ; le lieu reste après
  un rechargement du plugin et après « Tout remettre par défaut ».

**0.443 : N-02 tâches du Livre d'or** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Rappels › Livre d'or : la liste « À faire » correspond aux cases du journal ; une tâche finie passe en « à réclamer » (vert), puis
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

- [ ] Interdire Œil de l'expert et Manipulation : ni la recherche, ni l'assistant du carnet, ni la synthèse en cours ne les proposent ;
  grisées dans le simulateur. Carnet ouvert avec le mauvais métier : « Équiper (métier) » change d'ensemble et le calcul démarre.

**0.437 : N-11 coordonnées collées** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Coller une liste de Siren ou Faloop dans l'import des favoris : un favori par ligne, au bon endroit sur la carte ; lieux inconnus
  comptés dans le message.

**0.436 : A-14 enregistreur de rotation** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] « Enregistrer », fabriquer à la main (avec un buff, une action de qualité, une de progression), « Arrêter » : la liste est
  exacte et dans l'ordre ; « Utiliser comme séquence » ; le jeu se comporte comme d'habitude pendant l'enregistrement (aucune action
  bloquée) ; après « Arrêter », rien n'est plus noté.

**0.435 : A-07 recettes expertes** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Recette experte : le simulateur l'accepte et affiche « chaque étape en condition normale ». Pendant une synthèse experte en jeu,
  mode développeur : noter la condition brute affichée pour souple, robuste, malléable, amorcée (pour ouvrir la projection en direct).

**0.434 : A-13 actions du simulateur** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Comparer au jeu : Appréciation finale (progression arrêtée à 1 de la fin), Synthèse intensive / Toucher précis après Cœur et âme
  (spécialiste), Innovation éclair, Ficelles du métier (+20 PS). Synthèse en cours avec Appréciation finale active : projection affichée.

**0.433 : A-05 listes de fabrication** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Créer une liste, y ajouter deux recettes depuis Préparation, changer une quantité, retirer un objet ; les ingrédients totaux et
  « à compléter » sont justes ; la liste est toujours là après un rechargement du plugin ; lien Teamcraft de la liste.

**0.432 : A-15 collectionnables des sacs** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Avec des collectionnables dans les sacs (récoltés et fabriqués) : « Dans vos sacs » montre leur collectionnabilité comme le jeu,
  le bon palier et les assignats du marchand.

**0.431 : A-06 bénéfice d'artisanat** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Préparation › « Consulter les prix de la liste » : le prix de l'objet fabriqué apparaît dans « Bénéfice », avec la dernière vente ;
  case « Prix du centre de données » : autre monde affiché ; bénéfice = prix × quantité − achats.

**0.428-0.429 : recette refusée par le carnet du jeu** — pendant la campagne de tests en jeu de la fin (phase 6)



**0.427 : A-12 prérequis de la recette** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Page Artisanat : niveau, Habileté et Contrôle en vert / rouge selon le personnage ; métier non porté : « ? » et « Équiper (métier) »
  porte le bon ensemble.

**0.426 : fermer Tensho en ouvrant une fenêtre du jeu** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] « Ouvrir dans le carnet d'artisanat » : Tensho se ferme, le carnet s'ouvre sur la recette ; recommencer avec le carnet déjà
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

- [ ] Fiche d'un objet › Craft lié : une recette déjà faite montre « ✓ déjà fabriquée », une jamais faite « jamais fabriquée » (comparer
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

- [ ] Survol d'un panorama sur la carte Tensho : plus d'erreur du wiki, la météo demandée est donnée ; le clic droit n'a plus d'onglet
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

- [ ] Ouvrir la page Mage bleu : en quelques secondes « Livre des sorts du wiki : 124 sorts » ; « Ordre : niveau du monstre croissant » :
  la liste entière est triée, chaque sort montre « niv. N » ; Canon à eau et les sorts sans monstre sont à la fin. Fermer et rouvrir :
  aucune nouvelle lecture (copie).

**0.394 : polices** — pendant la campagne de tests en jeu de la fin (phase 6)

- [ ] Réglages › Interface › Polices : changer la famille et la taille des titres, des sections et du texte : la fenêtre Tensho change
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
