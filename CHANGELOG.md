# Journal des versions / Changelog

Résumé par version, tiré des notes de vérification (historique détaillé : commits git ; preuves citées : docs/evidence/).
Chaque version a été compilée et testée par `scripts/Verify.ps1` ; la validation en jeu est indiquée quand l'utilisateur l'a rapportée.

## 0.451.0
- Release Stable-0.451.0 (demande de l'utilisateur, 2026-11-03) : les notes précisent qu'il reste beaucoup de tests en jeu à faire.
- N-06 (DEC-027, point 1) : section « Missions d'escouade » dans la page Rappels. Tout vient du jeu, aucune source communautaire :
  - « Lire l'escouade » (sur clic, sur le thread du jeu, lecture de champs seulement) copie les membres (classe, niveau, état, affinités)
    et le bonus d'entraînement depuis GcArmyManager.Data (FFXIVClientStructs). Le jeu ne les charge qu'à la caserne : sinon, un message
    l'explique.
  - Les missions et leurs 6 jeux d'exigences viennent de GcArmyExpedition. Le joueur choisit celui que montre la fenêtre du jeu (le jeu
    actif n'est pas encore connu).
  - Les attributs d'un membre viennent de GcArmyMemberGrow, sur la ligne de sa classe (colonne ClassJob) et à l'indice de son niveau.
    Le niveau 60 est dans les quatre colonnes qui suivent la collection (Unknown0 à 3 chez Lumina) : les valeurs suivent celles du
    niveau 59 (ex. 129/31/52 puis 130/31/53).
  - Les 5 meilleurs groupes de quatre membres libres sont classés par pourcentage, puis par attributs atteints, puis par manque le plus
    petit. Pourcentage = PercentBase + chaque attribut atteint + PercentAll si les trois le sont ; il est affiché « ≈ », comme une
    estimation non vérifiée en jeu. Les affinités ne sont pas comptées (colonnes sans nom).
  - « Écrire dans le journal » est la sonde en jeu : l'escouade lue, les attributs utilisés et les groupes vont dans le journal de
    Dalamud (en anglais), pour comparer avec la fenêtre du jeu.
- Tests : Squadron451Tests (4 cas). Verify.ps1 (agent) : 823/823, 0 avertissement, 0 erreur ; DLL 0.451.0.0. Non validé en jeu.
### English
- **Important: much in-game testing is still to do.** The versions since 0.431 (this release included) are compiled and pass
  the automated tests, but most of their features have not been validated in game yet; a guided in-game test campaign is planned
  (checklist: docs/project/TESTING.md). Please report anything that does not behave as described.
- N-06: a « Squadron missions » section on the Reminders page: reads your squadron in the barracks on click and ranks the parties of
  four for a mission from the game's sheets (requirements, success percent parts, attributes by class and level, training bonus); the
  percent is shown as an estimate until checked in game, chemistry is not counted; « Write to the log » records the values for the probe.

## 0.450.0
- N-14 (DEC-027, point 2) : section « Croisements » sous le journal de jardinage. On choisit une graine pour voir les paires qui la
  donnent, ou ce qu'elle peut donner et avec quelle graine la planter. Les paires viennent de nick75g/FFXIV-Crossbreed-Helper (MIT,
  avis complet dans Reminders/Data/garden-crosses.csv, construit par tools/garden-data.py ; crédit « FFXIV Crossbreed Helper »).
- La source ne donne que des noms anglais. Chaque nom est relié à son objet par la feuille Item anglaise du jeu, puis affiché dans la
  langue du jeu (choix de l'utilisateur : « corrélation vers le fr »). Un nom inconnu de la feuille n'est pas deviné : ses lignes sont
  écartées, comptées et listées dans l'infobulle et le journal de Dalamud (attendu : « Gysahl Green Seeds »).
- Les 3 886 lignes de la source donnent 3 768 paires distinctes : 118 lignes sont des doublons exacts, comptés une fois. 49 graines
  ont des paires. Les graines de Dawntrail ne sont pas couvertes (la source date de 2024), ce que la section indique.
- Lecture une seule fois, hors de l'image (tâche de fond), à la première ouverture de la section. Aucune requête réseau, rien n'est
  lu dans la mémoire du jeu.
- Tests : GardenCrosses450Tests (3 cas). Verify.ps1 (agent) : 819/819, 0 avertissement, 0 erreur ; DLL 0.450.0.0. Non validé en jeu.
### English
- N-14: a « Crossbreeding » section under the garden journal: for a seed, the pairs that give it, or what it can give and with which
  seed. Pairs from nick75g's FFXIV Crossbreed Helper (MIT, embedded with its notice and credited), English names linked to the game's
  items and shown in the game's language; unknown names are left out and reported; Dawntrail seeds are not covered.

## 0.449.0
- Recherche des sources de N-06 et N-14 (DEC-026, point 11), soumise et décidée le 2026-11-03 (DEC-027), preuve
  docs/evidence/0.448-n06-n14-sources.md. Escouade : la règle de réussite est dans la feuille GcArmyExpedition (PercentBase,
  Percent…Met, PercentAllMet), donc aucune source communautaire n'est nécessaire. Croisements : paires MIT de nick75g. Jardin : durées
  GPL-3.0 de Lotlab. Squadronista et SquadronSolver n'ont pas de licence : lus pour comprendre, rien copié.
- N-08 complété (DEC-027, point 3) : dans le journal de jardinage, une « Liste des graines » (noms dans la langue du jeu, avec
  recherche) remplit la graine, ses heures de pousse et ses heures d'entretien. Les données viennent de Lotlab/FFXIV-Gardening-Tracker
  (GPL-3.0, 107 graines par identifiant d'objet) ; l'entretien est le délai avant flétrissement, 0 = pas de rappel. Les valeurs restent
  modifiables avant de planter. Données embarquées (Reminders/Data/garden-seeds.csv, construites par tools/garden-data.py avec l'avis
  GPL-3.0) et crédit « FFXIV Gardening Tracker ». Aucune requête réseau.
- Tests : GardenSeeds449Tests (3 cas). Verify.ps1 (agent) : 816/816, 0 avertissement, 0 erreur ; DLL 0.449.0.0. Non validé en jeu.
### English
- Source search for N-06 and N-14 (DEC-027): the squadron success rule is in the game's GcArmyExpedition sheet; crossbreeds will use
  nick75g's MIT data, garden durations Lotlab's GPL-3.0 data.
- Garden journal: a seed list (game language, searchable) fills the seed and its growing and tending hours from Lotlab's Gardening
  Tracker data (GPL-3.0, embedded and credited); the values stay editable.

## 0.448.0
- N-08 (décision du 2026-11-03, DEC-026) : « Journal de jardinage » dans la page Rappels. Pour chaque parcelle ou pot : lieu et graine
  (vos mots), durée de pousse et intervalle d'entretien tapés en heures (le jeu ne les donne pas : rien n'est deviné, 0 = pas de rappel
  d'entretien). Tensho affiche l'heure de récolte et le prochain entretien ; boutons « Entretenu », « Replanter », « Retirer » ;
  64 parcelles au plus. Réglage « Me le rappeler dans le chat » (activé) : une ligne dans le chat, et le son des alarmes s'il est activé,
  quand une parcelle est mûre ou à entretenir, une fois chacune (gardé sur disque : pas de répétition après un redémarrage), vérifié
  toutes les 10 s. Le journal est gardé par « Tout remettre par défaut ». Rien n'est lu dans le jeu.
- CI : le premier passage du workflow « Build and test » (0.447) a trouvé un test qui dépendait des fins de ligne (Paissa445Tests lisait
  Configuration.cs sans normaliser CRLF) : corrigé.
- Tests : Garden448Tests (3 cas). Verify.ps1 (agent) : 813/813, 0 avertissement, 0 erreur ; DLL 0.448.0.0. Non validé en jeu.
### English
- N-08: garden journal on the Reminders page: beds and pots with the growing and tending hours you type (the game does not give
  them), harvest and tending times, optional chat reminders once each.
- CI: a test no longer depends on the files' line endings.

## 0.447.0
- Décisions du 2026-11-03 (DEC-026), premier lot appliqué :
  - X-13 : bouton « Envoyer » à côté de « Copier l'annonce » dans le train de chasse : l'annonce part dans le canal choisi
    (/sh, /y, /p, /fc, /ls1, /cwl1), avec le lien du drapeau, seulement sur clic, une fois le drapeau posé sur l'étape depuis le train,
    5 s d'écart ; texte nettoyé (ni barre oblique, ni chevrons, ni caractère de contrôle), ligne vérifiée de nouveau juste avant le jeu
    (UIModule.ProcessChatBoxEntry, comme les alarmes du jeu). L'annonce nomme maintenant l'étape dont le drapeau a été posé en dernier
    (en 0.446, après « Drapeau suivant », elle nommait l'étape d'après).
  - X-04 : bouton « Événements de joueurs (partake.gg) » en haut de la page Événements (lien ouvert dans le navigateur, rien de lu) ;
    crédit et description du plugin mis à jour.
  - Réglage « Monstres au survol » retiré avec son service (MonsterHoverSources) et ses textes : il ne commandait plus rien.
  - Tri : pendant la relecture de 250 ms qui suit un mouvement d'objet (DEC-004, gardée), l'éditeur de règles reste affiché avec
    « Rangement modifié : nouvelle lecture » ; l'aperçu et « Appliquer » attendent toujours une liste complète.
- GitHub : branches main et Devloppement protégées (ni push forcé ni suppression), tags Stable-* protégés ; workflow « Build and test »
  (Verify.ps1 sur Windows à chaque push) et Dependabot (actions GitHub, chaque semaine) activés depuis les brouillons de 0.323.
- Fins de ligne : `git add --renormalize .` ne change rien (dépôt déjà en LF) ; décision de 0.323 close.
- Tests : Decisions447Tests (5 cas) ; MonsterHoverSourcesTests retirés avec le service. Verify.ps1 (agent) : 810/810, 0 avertissement,
  0 erreur ; DLL 0.447.0.0. Non validé en jeu (l'envoi au chat et le lien <flag> envoyé sont à essayer en jeu).
### English
- X-13: the hunt train call can now be sent to a chosen chat channel (/sh, /y, /p, /fc, /ls1, /cwl1) on click, with the flag link,
  once the flag was placed from the train, 5 s apart; the text is cleaned and checked again before reaching the game. The call names
  the stop flagged last.
- X-04: « Player events (partake.gg) » link on the Events page; nothing is read from the site.
- Removed the « Look up monsters on the wiki when hovering » setting, which no longer did anything.
- Sorting: the rules editor stays on screen while the bags are read again after a move; preview and sorting still wait for a full read.
- GitHub: protected branches and Stable tags; « Build and test » workflow and Dependabot enabled.

## 0.446.0
- Feuille de route, domaine 6, N-09 (suite) : réglage « Annoncer les marques A et S proches » (Cartes › Train de chasse, désactivé
  par défaut) : quand la lecture de la carte voit une marque A ou S autour de toi, une ligne dans ton chat (« Marque S en vue : … —
  zone ( x , y ) »), une fois par marque, et le son des alarmes si elles en jouent un. Rien n'est envoyé aux autres joueurs.
- X-13 en partie : bouton « Copier l'annonce » dans le train de chasse : « Prochaine marque : nom (rang) — zone <flag> » copié dans le
  presse-papiers pour l'étape suivante ; collé dans le chat du jeu, <flag> devient le lien du drapeau. Tensho n'envoie rien lui-même
  (envoyer au chat sur clic : laissé à ta décision, G-PROPOSALS).
- Documentation relue (demande du 2026-11-03) : README (pages, commandes et fonctions jusqu'à 0.446, crédit PaissaDB), GOAL, STATUS
  (prochaine action, plan « Git », dernière release Stable-0.430.0), ROADMAP, PROJECT, USER-REQUESTS, PUBLISHING ; propositions déjà
  faites marquées : 36 lignes « État » dans MAP, ITEMS, CRAFT-GATHER et TOOLS-PROPOSALS, alternatives faites dans G-PROPOSALS.
- Git confié à l'agent (accord du 2026-11-03) : CLAUDE.md, RULES et STATUS « Git » ; commit et push par `Verify.ps1 -Commit`, release
  seulement sur ta demande. Release.ps1 : variable facultative `TENSHO_COMMIT_TRAILER`, ajoutée en fin de message de commit.
- GitHub (demande du 2026-11-03) : milestone 2 renommée, milestones 3 (résistance aux mises à jour, travaux en séance) et 1.0.0
  (publication Dalamud), labels in-game-test, probe, decision, ultracode, état à 0.446 commenté sur #10–#17, issues #22–#31.
  CodeQL corrigé : le workflow visait une branche « Stable » inexistante et la compilation échouait faute de Dalamud sur le runner
  (branches main et Devloppement, distribution officielle de Dalamud téléchargée pour ses assemblies de référence).
- Tests : HuntAlert446Tests (1 cas). Verify.ps1 (agent) : 809/809, 0 avertissement, 0 erreur ; DLL 0.446.0.0. Non validé en jeu.
### English
- N-09: optional local chat line and alarm sound when an A or S hunt mark appears nearby (off by default).
- X-13 (partly): copy the hunt train conductor's call for the next stop, with <flag>, to the clipboard; nothing is sent by Tensho.
- Documentation pass: README pages, commands and features up to 0.446; status, roadmap and proposals marked with what is already done.
- Git is now handled by the agent (commit and push per version); releases stay on the maintainer's request. Release.ps1: optional
  TENSHO_COMMIT_TRAILER appended to the commit message.
- GitHub: milestones 3 (update resilience and deep work) and 1.0.0 (Dalamud publication), new labels, issues #22–#31, state
  comments on #10–#17. CodeQL fixed: it targeted a missing « Stable » branch and failed without Dalamud on the runner.

## 0.445.0
- Feuille de route, domaine 6, N-05 (ton choix dans GAPS : PaissaDB accepté, surveillance régulière par réglage, lecture seule, rien
  envoyé d'autre). Cartes › « Terrains en vente (PaissaDB) » :
  - « Consulter (monde) » : les parcelles libres du monde où tu te trouves (quartier dans la langue du jeu, secteur et parcelle comptés
    à partir de 1, taille, prix, loterie ou premier arrivé, inscrits) ; filtre de taille.
  - Réglage « Surveiller les terrains en vente » (désactivé par défaut, intervalle 10 à 240 min, 30 par défaut) : à chaque lecture,
    une ligne dans le chat pour chaque parcelle nouvellement libre de la taille choisie (rien à la première lecture).
  - Seul le numéro du monde est envoyé, réponse bornée à 2 Mo, une requête à la fois. Crédit PaissaDB (aucune licence indiquée par le
    projet) ajouté, manifeste mis à jour. Serveurs publics NA seulement (limite de PaissaDB).
- Laissé pour ton retour (G-PROPOSALS) : N-08 journal de jardinage (les feuilles du jeu ne donnent pas la durée de pousse).
- Tests : Paissa445Tests (1 cas) ; test du manifeste complété. Verify.ps1 (agent) : 808/808, 0 avertissement, 0 erreur ; DLL 0.445.0.0. Non validé en jeu.
### English
- N-05: open plots of the current world from PaissaDB (read only, world id only), with an optional watch announcing new plots in chat.

## 0.444.0
- Feuille de route, domaine 6, X-09 : annuaire des lieux. Cartes › « Annuaire des lieux » (replié) : adresses résidentielles (nom, monde,
  quartier du jeu, secteur, parcelle ou appartement, note), ajoutées par un petit formulaire, copiées en texte, retirées avec Ctrl.
  Gardées dans la configuration (100 au plus), conservées par « Tout remettre par défaut ». Quartiers lus dans la feuille du jeu
  (usage résidentiel), dans la langue du jeu.
- Pas fait (noté dans G-PROPOSALS) : N-14 planificateur de croisements, faute de source des paires (les données LuminaSupplemental
  donnent « graine → résultat » par parent seulement).
- Preuve : docs/evidence/0.444-housing.md.
- Tests : PlaceDirectory444Tests (1 cas). Verify.ps1 (agent) : 807/807, 0 avertissement, 0 erreur ; DLL 0.444.0.0. Non validé en jeu.
### English
- X-09: a place directory of residential addresses (world, district, ward, plot or apartment, note), copied as text.

## 0.443.0
- Feuille de route, domaine 6, N-02 : contenus du Livre d'or. Rappels › Livre d'or : sous l'état et l'expiration, chaque tâche encore
  à faire (« À faire : … ») et chaque vignette à réclamer (en vert), nommées dans la langue du jeu (la mission pour les tâches d'une
  instance, sinon le texte du jeu : « donjons (nv 50) »…). Les chances de lignes (0.439) suivent.
  - Lecture : tâches et états du jeu (GetWeeklyBingoTaskStatus, adresse vérifiée). Sans synchronisation de groupe (ton choix).
- Preuve : docs/evidence/0.443-bingo-tasks.md.
- Tests : BingoTasks443Tests (1 cas). Verify.ps1 (agent) : 806/806, 0 avertissement, 0 erreur ; DLL 0.443.0.0. Non validé en jeu.
### English
- N-02: Wondrous Tails tasks still to do and stickers to claim, named in the game's language.

## 0.442.0
- Feuille de route, domaine 6, N-15 : enregistreur de train de chasse. Cartes › « Train de chasse » (replié).
  - « Enregistrer » : chaque marque A ou S chargée par le jeu autour de toi (et B si coché) est notée une fois par carte, avec sa
    position et l'heure.
  - « Drapeau suivant » pose le drapeau du jeu sur la première étape pas encore faite ; « Drapeau » sur une étape précise.
  - « Copier la liste » : lignes « Nom — Zone ( x , y ) », que l'import des favoris (0.437) et les sites de chasse relisent.
  - Liste gardée pour la session (64 étapes au plus) ; « Effacer » avec Ctrl.
  - Lecture : la carte en direct (marques de la 0.441), une fois par seconde, seulement pendant l'enregistrement.
- Tests : HuntTrain442Tests (1 cas, export relu par l import des favoris). Verify.ps1 (agent) : 805/805, 0 avertissement, 0 erreur ; DLL 0.442.0.0. Non validé en jeu.
### English
- N-15: hunt train recorder (A/S marks met, once per map, next flag, export as « Name — Zone ( x , y ) » lines).

## 0.441.0
- Feuille de route, domaine 6, N-09 : radar des marques de chasse. Sur la carte Tensho, nouvelle couche « Marques de chasse proches »
  (réglage de la carte, activée par défaut) : les marques vivantes que le jeu a chargées autour de toi, icône crâne bleue (rang B),
  orange (A) ou rouge (S), nom et rang au survol.
  - Rang lu dans la feuille du jeu NotoriousMonster par BNpcBase ; seulement les marques vivantes et ciblables de la table d'objets.
  - Pas encore : points d'apparition fixes et alertes (noté dans G-PROPOSALS).
- Preuve : docs/evidence/0.441-hunt-marks.md.
- Tests : HuntMarks441Tests (1 cas). Verify.ps1 (agent) : 804/804, 0 avertissement, 0 erreur ; DLL 0.441.0.0. Non validé en jeu.
### English
- N-09: nearby living hunt marks (B, A, S) on the Tensho map, coloured by rank, from the game's NotoriousMonster sheet.

## 0.440.0
- Feuille de route, domaine 6, N-01 : contrats de chasse. Rappels › Contrats de chasse : sous le nombre de contrats, chaque contrat
  détenu, avec ses cibles.
  - Pour chaque cible : nom, lieu, victimes faites / demandées, mention « ALÉA » pour les marques d'élite ; en vert quand c'est fini,
    et « terminé (à rendre) » quand tout le contrat l'est.
  - « Carte » ouvre la carte Tensho sur la zone de la cible. Les données du jeu donnent la zone et le lieu, pas de position : pas de
    drapeau ni de téléportation directe (noté dans G-PROPOSALS).
  - Lecture : le contrat détenu de chaque index (fonction du jeu GetObtainedHuntOrderRowId, adresse vérifiée) et les compteurs
    (MobHunt.CurrentKills) ; noms et lieux dans la langue du jeu (MobHuntOrder, MobHuntTarget, MobHuntOrderType).
- À confirmer en jeu : le compteur de chaque cible (index de sous-ligne).
- Preuve : docs/evidence/0.440-hunt-bills.md.
- Tests : HuntBills440Tests (1 cas) ; test 0.372 mis à jour. Verify.ps1 (agent) : 803/803, 0 avertissement, 0 erreur ; DLL 0.440.0.0. Non validé en jeu.
### English
- N-01: held hunt bills with their targets (name, place, kills / needed, FATE marks) and the Tensho map on the target's zone.

## 0.439.0
- Feuille de route, domaine 6, N-03 : probabilités de lignes du Livre d'or. Rappels › Livre d'or (moins de 9 vignettes posées) :
  chances d'avoir au moins 1, 2 et 3 lignes à 9 vignettes, d'abord avec la grille actuelle, puis après un mélange (seconde chance).
  - Calcul exact : toutes les répartitions des vignettes restantes sur les cases vides sont comptées (au plus 11 440) ; les 10 lignes
    sont les 4 rangées, les 4 colonnes et les 2 diagonales.
  - Lecture des 16 cases : fonction du jeu IsWeeklyBingoStickerPlaced, adresse vérifiée avant l'appel. L'ordre des cases (par rangée
    ou par colonne) ne change pas le résultat : une transposition échange rangées et colonnes et garde les diagonales.
- Tests : WondrousTails439Tests (1 cas, comparé à un comptage indépendant case par case). Verify.ps1 (agent) : 802/802,
  0 avertissement, 0 erreur ; DLL 0.439.0.0. Non validé en jeu.
### English
- N-03: Wondrous Tails line odds (at least 1, 2, 3 lines with 9 stickers), now and after a shuffle, exact count.

## 0.438.0
- Demande de l'utilisateur : des réglages pour interdire certaines actions, dans toutes les interfaces liées ; et le panneau lié au
  carnet d'artisanat qui disait seulement « mauvais métier », sans moyen de changer.
  - Paramètres › Artisanat › « Actions interdites » : une case par action (nom du jeu, niveau), « Tout autoriser ». Une action interdite
    n'est jamais proposée :
    - recherche de rotation ;
    - assistant du carnet, y compris ses séquences sauvegardées (une séquence qui en contient est écartée) ;
    - synthèse en cours (lignes masquées).
  - Simulateur manuel : actions interdites grisées (infobulle) ; une séquence importée, enregistrée ou chargée qui en contient est
    signalée.
  - Panneau du carnet d'artisanat : avec le mauvais métier, bouton « Équiper (métier) » (ensemble de ce métier au plus petit numéro, sur
    clic, même demande vérifiée qu'en 0.385) ; « aucun ensemble » dit s'il n'y en a pas. Même bouton partagé avec la page Artisanat.
- Tests : CraftRules438Tests (2 cas, dont une recherche qui évite toutes les actions d'une première solution) ; test 0.427 mis à jour.
  Verify.ps1 (agent) : 801/801, 0 avertissement, 0 erreur ; DLL 0.438.0.0. Non validé en jeu.
### English
- User request: forbidden crafting actions (setting), never proposed by the search, the crafting log assistant (saved sequences too)
  or the live synthesis, greyed out in the simulator; « Equip (job) » in the crafting log panel when the wrong job is worn.

## 0.437.0
- Feuille de route, domaine 6, N-11 : coordonnées collées vers les favoris de la carte. Dans Cartes › favoris, le champ d'import
  accepte, en plus des codes TENSHO-MAP1, des lignes « Lieu ( x , y ) » collées depuis Siren, Bear, Faloop ou le chat.
  - Le lieu est reconnu par le nom de zone du jeu, à la fin du texte avant la parenthèse ; les icônes du jeu et les préfixes
    (« Siren: », « B rank – »…) sont ignorés.
  - Chaque ligne devient un favori « Zone (x, y) » dans le groupe « Coordonnées collées ».
  - Les lieux inconnus sont comptés, jamais devinés ; 200 lignes au plus, limite de 200 favoris inchangée.
- Tests : PastedCoordinates437Tests (1 cas). Verify.ps1 (agent) : 799/799, 0 avertissement, 0 erreur ; DLL 0.437.0.0. Non validé en jeu.
### English
- N-11: pasted « Place ( x , y ) » lines (Siren, Bear, Faloop, chat) imported as map bookmarks, zones matched by the game's names.

## 0.436.0
- Feuille de route, A-14 (ton choix dans GAPS : hook en lecture de l'action utilisée) : Artisanat › Simulation › « Enregistrer une
  rotation ».
  - « Enregistrer », fabriquer à la main dans le jeu, « Arrêter » : les actions d'artisanat acceptées par le jeu sont notées dans
    l'ordre (100 au plus) ; « Utiliser comme séquence » les met dans le simulateur (simulation, macro, séquence sauvegardée).
  - Hook sur ActionManager.UseAction du jeu : adresse vérifiée avant ; la fonction du jeu est toujours appelée d'abord et son résultat
    rendu tel quel ; rien n'est modifié ni envoyé. Le hook n'est actif que pendant l'enregistrement (désactivé à l'arrêt, retiré au
    déchargement du plugin).
  - Les actions d'artisanat hors du modèle (Toucher audacieux…) sont seulement comptées.
- Tests : CraftRecorder436Tests (2 cas). Verify.ps1 (agent) : 798/798, 0 avertissement, 0 erreur ; DLL 0.436.0.0. Non validé en jeu.
### English
- A-14: crafting rotation recorder (read-only hook on the game's UseAction, only while recording; the game's function always runs
  first, unchanged); the recorded actions become the simulator's sequence.

## 0.435.0
- Feuille de route, A-07 : recettes expertes et conditions étendues.
  - Les recettes expertes ne sont plus refusées par le simulateur. Chaque étape est simulée en condition normale, et c'est dit à
    l'écran (en jeu, les conditions spéciales changent le résultat).
  - Le moteur connaît les conditions des recettes expertes, avec les arrondis de Craftimizer (MIT, lu seulement) :
    - souple : PS / 2 ;
    - robuste et solide : solidité / 2, même avec Parcimonie ;
    - malléable : progression × 1,5 ;
    - amorcée : buffs + 2 étapes ;
    - centrée et bon présage : sans effet ici.
  - La synthèse en direct refuse toujours les recettes expertes : l'ordre des conditions 4 à 10 dans le jeu n'est pas prouvé (à
    vérifier en jeu, valeurs brutes en mode développeur).
- Preuve : docs/evidence/0.434-craft-actions.md (partie 0.435).
- Tests : CraftConditions435Tests (2 cas) ; deux anciens tests mis à jour (recettes expertes acceptées). Verify.ps1 (agent) :
  796/796, 0 avertissement, 0 erreur ; DLL 0.435.0.0. Non validé en jeu.
### English
- A-07: expert recipes simulated (normal condition at every step, said on screen) and the expert conditions' effects in the engine
  (Pliant, Sturdy, Robust, Malleable, Primed); live synthesis still refuses expert recipes until the condition ids are proven in game.

## 0.434.0
- Feuille de route, A-13 : neuf actions ajoutées au simulateur d'artisanat (niveaux et PS lus dans les feuilles du jeu, effets tels que
  Craftimizer, MIT, les modélise, lus seulement) :
  - Appréciation finale : arrête la progression juste avant la fin, une fois, pendant 5 étapes ; ne compte pas comme une étape ;
  - Synthèse intensive, Toucher précis, Ficelles du métier : condition Bonne ou Excellente, ou Cœur et âme ;
  - Cœur et âme, Innovation éclair, Observation méticuleuse : actions de spécialiste ;
  - Synthèse rapide (50 %) et Toucher précipité (60 %) : le simulateur suppose leur réussite et le dit ; la recherche de rotation ne
    les utilise pas.
- Nouvelle case « Spécialiste » dans les attributs : elle ouvre les actions de spécialiste et les recettes de spécialiste (refusées
  avant).
- Synthèse en cours : Appréciation finale et Cœur et âme observés sont pris en compte dans la projection (avant : projection refusée).
- Laissé de côté : Toucher audacieux (la feuille du jeu le donne sans métier ; il dépend d'un succès aléatoire).
- Preuve : docs/evidence/0.434-craft-actions.md.
- Tests : CraftActions434Tests (4 cas) ; trois anciens tests mis à jour (recettes de spécialiste acceptées, effets observés modélisés,
  énumération sans actions aléatoires). Verify.ps1 (agent) : 794/794, 0 avertissement, 0 erreur ; DLL 0.434.0.0. Non validé en jeu.
### English
- A-13: nine more crafting actions in the simulator (Final Appraisal, Intensive Synthesis, Precise Touch, Tricks of the Trade, Heart
  and Soul, Quick Innovation, Careful Observation, Rapid Synthesis, Hasty Touch) and a « Specialist » setting; chance actions assumed
  successful in the manual simulator and left out of the search.

## 0.433.0
- Feuille de route, A-05 (ton choix dans GAPS : un fichier à part, comme la Banque) : listes de fabrication à plusieurs objets.
  - Page Artisanat › « Listes de fabrication » (repliée, visible avec ou sans recette) : créer une liste par son nom, la choisir, la
    supprimer (Ctrl + clic) ; ses objets avec leur quantité (modifiable), un clic ouvre la Préparation de la recette, « Retirer ».
  - Total des ingrédients directs de toute la liste, face aux stocks recensés (requis, recensé, à compléter) ; toute la liste en lien
    Teamcraft (copie).
  - Préparation › « Ajouter à la liste « … » » : l'objet de la recette et la quantité à produire vont dans la liste choisie.
  - Fichier « CraftLists.json » dans le dossier du plugin, écrit par fichier temporaire ; un fichier illisible n'est jamais écrasé.
    32 listes de 64 objets au plus.
- Pas fait : la chaîne des sous-recettes pour toute une liste (une recette à la fois, comme avant).
- Tests : CraftLists433Tests (2 cas). Verify.ps1 (agent) : 790/790, 0 avertissement, 0 erreur ; DLL 0.433.0.0. Non validé en jeu.
### English
- A-05: crafting lists of several items (own file), with quantities, the whole list's direct ingredients against stocks, a Teamcraft
  link, and « Add to the list » from Preparation.

## 0.432.0
- Feuille de route, A-15 : valeur des collectionnables de l'inventaire. Page Récolte › « Collectionnables » › « Dans vos sacs ».
  - Pour chaque collectionnable des sacs : sa collectionnabilité, le palier atteint et les assignats de ce palier ; le total en haut.
  - Les objets que le jeu ne reprend pas contre des assignats sont seulement comptés à part.
  - Lecture : les 4 sacs, toutes les 5 s et seulement pendant que le tableau est ouvert. La collectionnabilité vient d'un champ de
    l'objet (lu directement, aucune fonction du jeu appelée).
- À confirmer en jeu : la valeur lue pour un collectionnable fabriqué (collectionnabilité attendue, comme pour la récolte).
- Tests : Collectables409Tests (1 cas de plus). Verify.ps1 (agent) : 788/788, 0 avertissement, 0 erreur ; DLL 0.432.0.0. Non validé en jeu.
### English
- A-15: collectables in the bags with their collectability, tier reached and scrips, and the total, in the collectables table.

## 0.431.0
- Feuille de route, A-06 (ton choix dans GAPS : plus basse annonce du monde, centre de données en option, dernière vente à côté).
  Artisanat › Préparation › Coût indicatif des achats : nouvelle partie « Bénéfice ».
  - Prix de l'objet fabriqué : plus basse annonce de ton monde, ou du centre de données (case, avec le monde) ; NQ ou HQ (case).
  - Prix × quantité à produire, moins le coût des achats ; dernière vente affichée à côté.
  - Coût des achats incomplet ou prix inconnu : rien n'est calculé (dit à l'écran). Taxes et profondeur des offres non comptées.
  - Le prix de l'objet fabriqué est demandé avec ceux des ingrédients (« Consulter les prix de la liste », même cache Universalis).
- Tests : CraftProfit431Tests (1 cas). Verify.ps1 (agent) : 787/787, 0 avertissement, 0 erreur ; DLL 0.431.0.0. Non validé en jeu.
### English
- A-06: crafting profit in the preparation cost: the crafted item's lowest listing (world, or data center) × quantity minus the
  purchases, last sale beside it; nothing computed when a price or the cost is incomplete.

## 0.430.0
- Remarque de l'utilisateur : le carnet du jeu montre parfois des recettes au-dessus du niveau du métier (par tranches de niveaux).
  Le blocage par niveau ajouté en 0.428 refusait donc à tort des recettes que le jeu sait ouvrir. Il est retiré : le jeu décide.
  La vraie cause du message rouge (carnet déjà ouvert) reste corrigée par la 0.429. Le niveau ne sert plus qu'à choisir la recette
  quand plusieurs métiers fabriquent l'objet.
- Tests : GameLogLevel428Tests mis à jour. Verify.ps1 (agent) : 786/786, 0 avertissement, 0 erreur ; DLL 0.430.0.0. Non validé en jeu.
### English
- User remark: the game's log shows recipes above the job's level too; the 0.428 level check before opening the log is removed.

## 0.429.0
- Suite du signalement de la 0.428. Tu as précisé que ton orfèvre fabrique bien le Lingot de lar : le niveau n'était pas la cause.
  Ton premier message le disait : le refus du jeu arrive quand le carnet est déjà ouvert (pour certaines recettes seulement).
  - Correction : si le carnet du jeu (artisanat ou récolte) est ouvert, Tensho le ferme d'abord (fonction Hide de l'agent du jeu), puis
    le rouvre sur l'entrée à l'image suivante ; au plus 2 s, sinon le message « indisponible » de Tensho.
  - Ta remarque sur les recettes multiples : pour un objet fabriqué par plusieurs métiers, Tensho prenait la première recette de la
    liste. Il prend maintenant celle du métier porté, sinon celle d'un métier qui a le niveau requis, sinon la plus basse.
  - Le contrôle du niveau de la 0.428 reste (niveau connu et trop bas : Tensho le dit sans appeler le jeu).
- Tests : GameLogLevel428Tests complété ; tests 0.423 et 0.426 mis à jour. Verify.ps1 (agent) : 786/786, 0 avertissement, 0 erreur ; DLL 0.429.0.0. Non validé en jeu.
### English
- Follow-up: an open game log is closed first, then opened on the entry (the game refused some recipes with its log open); for items
  made by several jobs, the worn job's recipe (or one at the job's level) is chosen.

## 0.428.0
- Signalement de l'utilisateur : « Ouvrir dans le carnet d'artisanat » donne parfois le message rouge du jeu « Impossible d'afficher une
  recette à laquelle vous… », pour certaines recettes seulement (exemple : Lingot de lar).
  - Sonde : Lingot de lar n'a qu'une recette (ligne 5638, niveau 91, ni secrète ni experte) ; ce n'est pas une mauvaise ligne.
  - Cause probable (à confirmer, la fin du message n'a pas pu être lue) : le jeu n'ouvre pas une recette au-dessus du niveau du métier.
  - Correction : quand le niveau du métier est connu et trop bas, Tensho le dit (« {métier} niveau 91 demandé, votre métier est
    niveau N ») au lieu d'appeler le jeu ; niveau inconnu : le jeu est appelé comme avant.
  - Un premier essai (fermer le carnet ouvert avant de le rouvrir) a été retiré : ce n'était pas la cause.
- Tests : GameLogLevel428Tests (1 cas). Verify.ps1 (agent) : 786/786, 0 avertissement, 0 erreur ; DLL 0.428.0.0. Non validé en jeu.
### English
- User report: the game refuses to open some recipes in its crafting log; when the job level is known and below the recipe, Tensho now
  says so instead of calling the game (probable cause, to confirm).

## 0.427.0
- Feuille de route, A-12 : en haut de la page Artisanat, ce que la recette demande face au personnage, en vert si atteint, en rouge
  sinon, en gris si inconnu (« ? ») :
  - le niveau du métier (niveaux des métiers lus avec les ensembles d'équipement) ;
  - l'Habileté et le Contrôle minimaux, comparés seulement quand le métier est porté ;
  - « Équiper (métier) » : l'ensemble de ce métier au plus petit numéro, sur clic seulement (même demande vérifiée qu'en 0.385).
- Tests : CraftReadiness427Tests (1 cas). Verify.ps1 (agent) : 785/785, 0 avertissement, 0 erreur ; DLL 0.427.0.0. Non validé en jeu.
### English
- A-12: the recipe's level, Craftsmanship and Control needs against the character on the Crafting page, and « Equip (job) » on click.

## 0.426.0
- Demande de l'utilisateur : Tensho prend tout l'écran ; quand il ouvre une fenêtre du jeu, il doit se fermer par défaut, avec un réglage.
  - Nouveau réglage « Fermer Tensho quand il ouvre une fenêtre du jeu » (Paramètres, activé par défaut, ↺, liste de remise à zéro).
  - Concerné : « Ouvrir dans le carnet d'artisanat », « … de récolte », et la carte du jeu ouverte depuis une page de Tensho (Cartes,
    favoris, liens du chat, fenêtre de suivi). Tensho ne se ferme que si le jeu a bien ouvert sa fenêtre.
  - Pas concerné : le drapeau posé par « Y aller », dont la question de téléportation reste dans Tensho.
- « Vérifier que ça ne ferme pas s'il est ouvert » : les fonctions du jeu utilisées ouvrent le carnet sur l'entrée ; elles ne sont pas
  des bascules. À confirmer en jeu avec le carnet déjà ouvert (point ajouté aux tests).
- Tests : CloseOnGameWindow426Tests (1 cas). Verify.ps1 (agent) : 784/784, 0 avertissement, 0 erreur ; DLL 0.426.0.0. Non validé en jeu.
### English
- User request: Tensho closes once it has opened a game window (crafting or gathering log, game map from a page); setting on by default.

## 0.425.0
- Feuille de route, A-08 (première partie) : Artisanat › Simulation › « Importer une macro ». On colle une macro d'artisanat ; les lignes
  « /ac … » (avec ou sans guillemets et attentes) deviennent la séquence du simulateur, les noms étant reconnus dans la langue du jeu.
  Les autres commandes (/mlock, /echo, /nextmacro…) sont ignorées et comptées ; les noms inconnus sont listés, jamais devinés.
- Pas fait : la lecture des liens Teamcraft et Craftingway sur clic (ton choix dans GAPS) ; leur format en ligne reste à étudier (noté
  dans G-PROPOSALS).
- Tests : CraftMacroImport425Tests (1 cas). Verify.ps1 (agent) : 783/783, 0 avertissement, 0 erreur ; DLL 0.425.0.0. Non validé en jeu.
### English
- A-08 (part): pasted crafting macro imported into the simulator (/ac lines, names in the game's language); online links not yet read.

## 0.424.0
- Signalements de l'utilisateur sur la page Artisanat :
  - « Recensé : Inconnu » partout dans Préparation. Cause : le cache des stocks n'était rafraîchi que par les vues Objets ; ouverte seule,
    la page Artisanat lisait un cache vide et le gardait. Correction : la Préparation le rafraîchit avant de compter.
  - « Retour au carnet » ramenait en fait à la fiche de l'objet : renommé « Retour à l'objet » (en anglais « Back to the item »), et il
    ouvre bien l'objet de la recette affichée (bouton de la page et barre du haut).
- Tests : CraftReports424Tests (1 cas). Verify.ps1 (agent) : 782/782, 0 avertissement, 0 erreur ; DLL 0.424.0.0. Non validé en jeu.
### English
- Fixes (user reports): Crafting › Preparation showed unknown holdings when opened on its own; « Back to recipes » renamed « Back to
  the item » and opens the shown recipe's item.

## 0.423.0
- Signalement de l'utilisateur : « Artisanat : Lingot de lar » (menu d'un objet) ouvrait la page Artisanat sans recette (« Choisissez
  une recette… »). Cause : le lien choisissait l'objet et la page, pas la recette. Correction : il ouvre l'onglet Préparation sur la
  recette de l'objet (la première s'il y en a plusieurs) ; et la page Artisanat sans recette prend celle de l'objet en contexte.
- Feuille de route, A-09 : Artisanat › Préparation › « Copier le lien Teamcraft » et « Ouvrir dans Teamcraft ». C'est le lien d'import
  de liste de Teamcraft pour l'objet et la quantité à produire (format lu dans le code de Teamcraft, MIT ; lien construit localement,
  rien n'est envoyé avant le clic). Crédit Teamcraft complété (hôte ffxivteamcraft.com).
- Tests : Teamcraft423Tests (2 cas). Verify.ps1 (agent) : 781/781, 0 avertissement, 0 erreur ; DLL 0.423.0.0. Non validé en jeu.
### English
- Fix (user report): « Crafting: item » from the item menu now opens the item's recipe on Preparation. A-09: Teamcraft list import link
  (copy or open) from the Preparation tab.

## 0.422.0
- Feuille de route, A-10 (ton choix dans GAPS : s'appuyer sur Macro Chain quand il est actif). Quand le plugin Macro Chain (Caraxi,
  nom interne « MacroChain », manifeste lu localement) est chargé, les pages de macros d'artisanat enchaînées finissent par sa commande
  « /nextmacro » au lieu du marqueur de Tensho ; une ligne le dit sous l'export. Sans Macro Chain, rien ne change.
- Détection : liste des plugins chargés, au plus toutes les 5 s.
- Tests : MacroChain422Tests (1 cas). Verify.ps1 (agent) : 779/779, 0 avertissement, 0 erreur ; DLL 0.422.0.0. Non validé en jeu.
### English
- A-10: with Caraxi's Macro Chain loaded, chained craft macro pages end with « /nextmacro » instead of Tensho's marker.

## 0.421.0
- Feuille de route, A-11 : recettes jamais fabriquées. Tensho demande au jeu, pour chaque recette, si le personnage l'a déjà fabriquée
  (QuestManager.IsRecipeComplete, fonction du jeu utilisée seulement une fois son adresse résolue ; toutes les 10 s, seulement pendant
  qu'une vue d'artisanat le demande).
  - Fiche d'un objet › Craft lié : « ✓ déjà fabriquée » ou « jamais fabriquée » à côté de chaque recette (bonus d'expérience à la
    première fabrication).
  - Livre des recettes qui utilisent l'objet : case « Jamais fabriquées seulement ».
- À confirmer en jeu : l'index attendu par la fonction est la ligne Recipe.
- Tests : RecipeLog421Tests (1 cas). Verify.ps1 (agent) : 778/778, 0 avertissement, 0 erreur ; DLL 0.421.0.0. Non validé en jeu.
### English
- A-11: recipes never crafted (read from the game) marked on the item's recipes, with a « never crafted only » filter in the craft book.

## 0.420.0
- Feuille de route, A-02 : repas et remède dans le simulateur d'artisanat (Artisanat › Simulation › Attributs).
  - Deux listes, triées par bonus maximal : les repas (statut « Repu ») et les remèdes (« Médicamenté ») du jeu qui donnent Habileté,
    Contrôle ou PS. Une case HQ pour chacun.
  - Les attributs avec repas et remède sont affichés et servent à la simulation et à la recherche de rotation. Calcul du jeu : un
    pourcentage plafonné de l'attribut sans repas ni remède, chacun calculé à part, puis ajoutés.
  - Les actions de compagnie ne sont pas comptées (taille de l'effet non lue) ; si les attributs copiés comptent déjà un repas, il ne
    faut pas le choisir (dit sous les listes).
- Preuve : docs/evidence/0.420-craft-food.md (sonde PROBE_CRAFTFOOD).
- Tests : CraftConsumables420Tests (2 cas). Verify.ps1 (agent) : 777/777, 0 avertissement, 0 erreur ; DLL 0.420.0.0. Non validé en jeu.
### English
- A-02: crafting food and medicine (NQ or HQ) in the simulator, added to the attributes for the simulation and the rotation search.

## 0.419.0
- Feuille de route, domaine 5 (Artisanat), trois petits écarts :
  - A-01 : pour un objet artisanal collectionnable, la recherche de rotation propose « Bas / Moyen / Haut » comme qualité cible. Ce
    sont les seuils de remise du jeu (feuilles lues en 0.409), avec collectionnabilité = qualité / 10, règle du jeu telle que Teamcraft
    (MIT) et Craftimizer la codent (lus seulement). La cible ne dépasse pas la qualité maximale de la recette.
  - X-11 : l'assistant du carnet n'interdit plus Manipulation. Elle est permise quand le métier l'a apprise (état de déblocage du jeu,
    IUnlockState) ; inconnu = non apprise.
  - X-12 : nouveau réglage « Attente ajoutée aux buffs », séparé de celui des autres actions. Tant qu'il n'est pas changé, il vaut celui
    des actions (comportement d'avant).
- Tests : Craft419Tests (3 cas). Verify.ps1 (agent) : 775/775, 0 avertissement, 0 erreur ; DLL 0.419.0.0. Non validé en jeu.
### English
- A-01: collectable turn-in thresholds as quality targets in the rotation search; X-11: Manipulation allowed in the crafting log
  assistant when learned; X-12: separate extra wait for buffs in exported macros.

## 0.418.0
- Feuille de route, R-07 (ton choix dans GAPS : tout d'un coup). Pendant une traversée de pêche en mer, un panneau (même réglage que
  l'aide à la pêche) montre :
  - l'escale (lieu, numéro, moment : jour, crépuscule, nuit) et l'escale suivante ;
  - « Courant spectral ! » quand il est actif, avec les poissons du courant spectral au lieu des poissons habituels ;
  - les poissons de l'escale avec l'appât (ou la chaîne de mouchage) et la touche selon GatherBuddy, « ? » si inconnus ;
  - les trois missions avec leur avancement et leur objectif (texte du jeu), en vert une fois atteintes ;
  - les points et le nombre de poissons pris.
- Lecture : l'instance de pêche en mer du jeu (champs nommés de FFXIVClientStructs), deux fois par seconde, adresses des fonctions du
  jeu vérifiées avant l'appel, rien n'est envoyé. Données : feuilles IKDRoute, IKDSpot, FishingSpot, IKDPlayerMissionCondition.
- Inconnu : certains textes de mission contiennent des icônes (étoiles) qui ne s'affichent pas.
- Preuve : docs/evidence/0.418-ocean-voyage.md.
- Tests : OceanVoyage418Tests (2 cas). Verify.ps1 (agent) : 772/772, 0 avertissement, 0 erreur ; DLL 0.418.0.0. Non validé en jeu.
### English
- R-07: ocean fishing voyage panel: stop, moment, spectral current, the stop's fish with GatherBuddy bait and bite, next stop, the
  three missions with progress and target, points and fish caught.

## 0.417.0
- Feuille de route, R-15 : aide au harpon. Pendant la pêche au harpon, un panneau à côté de la fenêtre du jeu (même réglage que l'aide
  à la pêche). Pour chaque poisson en vue :
  - sa taille et sa vitesse ;
  - les poissons GatherBuddy qui ont cette taille et cette vitesse : ceux de la zone en couleur d'abord, puis trois autres au plus.
- Données : fenêtre du jeu lue en lecture seule (AddonSpearFishing, 5 fois par seconde) ; tailles et vitesses de GatherBuddy
  (Apache-2.0), dont la lecture des fichiers sait maintenant lire « .Spear(...) ».
- À confirmer en jeu : que la vitesse de la fenêtre a les mêmes valeurs que celles de GatherBuddy (mode développeur : valeurs brutes).
- Preuve : docs/evidence/0.417-spearfishing.md.
- Tests : Spearfishing417Tests (2 cas). Verify.ps1 (agent) : 770/770, 0 avertissement, 0 erreur ; DLL 0.417.0.0. Non validé en jeu.
### English
- R-15: spearfishing help beside the game's window: each fish's size and speed and the matching GatherBuddy fish, the zone's first.

## 0.416.0
- Feuille de route, R-14 : Artisanat › Préparation › après le calcul de la chaîne, « Liste de récolte par zone » (repliée).
  - Les matières brutes de la chaîne sont groupées par zone. La zone qui en récolte le plus vient d'abord, puis les suivantes ; chaque
    matière n'apparaît qu'une fois.
  - Dans une zone, un point sans fenêtre horaire passe avant un point à horaire, puis le niveau le plus bas.
  - Chaque ligne : quantité, lieu, horaire ET, « Y aller » (avec les réglages de « Y aller » : téléportation directe, équiper le métier).
  - Les matières sans lieu de récolte connu sont listées à part.
- Météo, déblocages et niveau ne sont pas vérifiés (dit dans l'aide de la section).
- Tests : GatheringRoute416Tests (2 cas). Verify.ps1 (agent) : 768/768, 0 avertissement, 0 erreur ; DLL 0.416.0.0. Non validé en jeu.
### English
- R-14: the crafting chain's raw materials as a gathering list by zone (greedy: the zone gathering the most first; untimed points
  first), with place, ET time and « Go there »; materials with no gathering place listed apart.

## 0.415.0
- Feuille de route, R-13 : nouvelle fenêtre de suivi épinglée (réglage dans Paramètres, désactivé par défaut). Elle liste les objets
  de tes alarmes Tensho actives (récolte, pêche), la plus proche en premier, avec le temps restant (douze lignes au plus).
  - « * » : l'alarme attend une condition de météo.
  - Clic sur un nom : la fiche de l'objet. « Carte » : la carte du jeu sur le point, quand sa position est connue.
  - La croix ferme la fenêtre (et décoche le réglage) ; elle est masquée avec l'interface du jeu ; recalcul une fois par seconde.
- Choix provisoire : la fenêtre suit les alarmes d'objets, pas une liste à part (voir G-PROPOSALS).
- Tests : Tracker415Tests (2 cas). Verify.ps1 (agent) : 766/766, 0 avertissement, 0 erreur ; DLL 0.415.0.0. Non validé en jeu.
### English
- R-13: optional pinned tracking window listing the items of the active Tensho alarms, soonest first, with the time left, a link to
  the item and to the game map on its point.

## 0.414.0
- Feuille de route, R-12 : le message d'une alarme Tensho dans le chat est cliquable.
  - Lien de carte du jeu vers le point de récolte ou le lieu de pêche de l'objet dans la zone de l'alarme, quand sa position est
    connue (mêmes positions que « Y aller »).
  - « Ouvrir dans Tensho » : ouvre la fenêtre sur la fiche de l'objet, onglet Récolte.
  - Sans objet ou sans position connue, le message reste comme avant. Les alarmes copiées dans le jeu sonnent toujours par le jeu.
- Un gestionnaire de lien par objet (au plus 256, puis remis à zéro), tous retirés au déchargement du plugin.
- Tests : AlarmLinks414Tests (1 cas). Verify.ps1 (agent) : 764/764, 0 avertissement, 0 erreur ; DLL 0.414.0.0. Non validé en jeu.
### English
- R-12: a Tensho alarm's chat line carries the game's map link to the item's gathering point (when its position is known) and an
  « Open in Tensho » link to the item's Gathering tab.

## 0.413.0
- Feuille de route, R-11 : tableau des collectionnables, en haut de la page Récolte (replié). Pour chaque objet que le jeu accepte
  comme collectionnable :
  - le groupe et le niveau de remise ;
  - les seuils de collectabilité bas / moyen / haut ;
  - les assignats par palier (boutiques d'assignats ; les autres sont marquées « objets ») ;
  - filtre par mots sur l'objet ou le groupe ; un clic ouvre l'objet.
- Sur la vue Récolte d'un objet collectionnable, ses seuils et assignats sous son nom.
- Données : les feuilles du jeu déjà lues en 0.409 (docs/evidence/0.409-collectables.md), plus le nom du groupe. Le type d'assignat
  reste non relié.
- Tests : Collectables409Tests (1 cas de plus). Verify.ps1 (agent) : 763/763, 0 avertissement, 0 erreur ; DLL 0.413.0.0. Non validé en jeu.
### English
- R-11: collectables table on the Gathering page (turn-in level, low / mid / high thresholds, scrips per tier, filter, click to open),
  and the item's thresholds on its Gathering view; game data only.

## 0.412.0
- R-10, suite. Dans GAPS, ton choix était « bouton à part **et** option avec « Y aller » » ; la 0.411 n'avait que le bouton.
- Nouveau réglage « « Y aller » : équiper aussi le métier », désactivé par défaut, avec son infobulle et ↺, dans la liste des réglages
  remis à zéro. Coché, un clic sur « Y aller » porte aussi l'ensemble du métier du lieu, par la même demande vérifiée.
- Tests : GatheringGearset411Tests complété. Verify.ps1 (agent) : 762/762, 0 avertissement, 0 erreur ; DLL 0.412.0.0. Non validé en jeu.
### English
- R-10 completed as chosen in GAPS: an optional setting (off by default) makes « Go there » also wear the place's job gear set.

## 0.411.0
- Feuille de route, R-10 : sur chaque lieu de récolte et de pêche de la fiche d'un objet, un bouton « Équiper (métier) » porte
  l'ensemble d'équipement du jeu de ce métier (celui de plus petit numéro). Il s'affiche seulement quand un tel ensemble existe.
  Il marche sur clic seulement, par la même demande vérifiée qu'en 0.385 (adresse de EquipGearset et ensemble valide, 2 s d'écart).
- Le métier du lieu vient du type de point de récolte du jeu : extraction et taille pour le mineur, coupe et fauche pour le botaniste,
  lieu de pêche pour le pêcheur. Un type sans nom lisible (type 5) n'a pas de bouton. Preuve : docs/evidence/0.411-gathering-jobs.md.
- Choix provisoire : bouton séparé de « Y aller », non couplé (voir G-PROPOSALS).
- Tests : GatheringGearset411Tests (2 cas). Verify.ps1 (agent) : 762/762, 0 avertissement, 0 erreur ; DLL 0.411.0.0. Non validé en jeu.
### English
- R-10: « Equip (job) » on each gathering and fishing source wears that job's gear set with the lowest number, on click only (same
  checked equip request as 0.385); the source's job comes from the game's gathering point type.

## 0.410.0
- Feuille de route, R-09 : l'optimiseur PR (outil PR et panneau de récolte en jeu) compte maintenant deux actions de plus.
  - Raison solide (mineur) / Sagesse du fermier (botaniste) : 300 PR, un essai de plus. À partir du niveau 90, le trait du jeu donne
    50 % de chance d'Eurêka, donc un essai gratuit avec Étalage des connaissances : compté en moyenne et signalé sous le plan.
  - Vision claire / Maîtrise florale : 50 PR, +15 % de réussite sur un essai. Placée d'abord sur les essais avec Récolte abondante.
- Outil PR : case « Compter aussi Raison solide… et Vision claire… », cochée par défaut ; décochée, le calcul est celui d'avant.
  Le panneau de récolte en jeu les compte toujours.
- Données sondées dans le jeu (coûts, niveaux, trait Eurêka) : docs/evidence/0.410-gp-actions.md. Le catalogue est toujours vérifié
  action par action contre la feuille du jeu.
- Non modélisé : la régénération des PR pendant la récolte, et Eurêka obtenu sur un essai déjà rendu par Étalage des connaissances.
- Tests : GpActions410Tests (3 cas, dont une comparaison avec une recherche essai par essai de toutes les répartitions) ; le test du
  catalogue compte 24 actions. Verify.ps1 (agent) : 760/760, 0 avertissement, 0 erreur ; DLL 0.410.0.0. Non validé en jeu.
### English
- R-09: the GP optimizer (GP tool and in-game gathering panel) also counts Solid Reason / Ageless Words (another attempt; Eureka Moment
  at 50 % from level 90, counted on average) and Clear Vision / Flora Mastery (+15 % on one attempt, Bountiful attempts first).

## 0.409.0
- Feuille de route, R-06 (aide aux collectionnables), première étape. Avant : le panneau de récolte disparaissait dès que la fenêtre
  « Collectionnable » du jeu s'ouvrait.
- Maintenant, à côté de cette fenêtre (réglage du panneau de récolte), un panneau « Collectionnable » affiche :
  - l'objet ;
  - les PR et l'intégrité, tels que le jeu les affiche ;
  - les seuils de collectabilité bas / moyen / haut de l'objet, lus dans les feuilles du jeu (CollectablesShopRefine) ;
  - les assignats par palier pour les boutiques d'assignats (CollectablesShopRewardScrip) ;
  - le niveau de remise.
- Inconnu : la collectabilité en cours n'a pas de champ nommé dans FFXIVClientStructs. Elle n'est pas lue, et aucune action suivante
  n'est conseillée. En mode développeur, le panneau liste les valeurs brutes de la fenêtre (64 au plus), pour la trouver pendant les
  tests en jeu. Le type d'assignat (colonne Currency) n'est pas relié : il n'est pas affiché.
- Lecture : sur le fil du jeu, 5 fois par seconde tant que la fenêtre est ouverte, rien n'est envoyé au jeu. Les seuils sont copiés une
  fois, hors du fil du jeu.
- Preuve : docs/evidence/0.409-collectables.md (sonde PROBE_COLLECTABLES de tools/ShopCurrencyProbe, métadonnées ApiProbe).
- Tests : Collectables409Tests (3 cas). Verify.ps1 (agent) : 757/757, 0 avertissement, 0 erreur ; DLL 0.409.0.0. Non validé en jeu.
### English
- R-06 step 1: while the game's Collectable window is open, a panel beside it shows the item's collectability thresholds and scrip
  rewards from the game's sheets, GP and integrity; the current collectability is not read yet (developer mode lists the window's
  raw values to find it in game), so no next action is advised.

## 0.408.0
- Demande de l'utilisateur : l'onglet « Craft lié » de la fiche d'un objet est trop lourd ; une version simple dans Objets, une
  complète dans Artisanat, avec un lien entre les deux.
- Objets › Craft lié : la recette, ses ingrédients et les stocks, les boutons « Simuler cette recette », « Simulateur détaillé » et
  le nouveau « Préparer dans Artisanat ». La préparation (quantités, matières à compléter, coût indicatif, chaîne des sous-recettes)
  n'y est plus.
- Artisanat : deux onglets, « Simulation » (l'ancien contenu) et « Préparation » (la préparation complète, même si le simulateur ne
  prend pas la recette en charge). « Retour au carnet » ramène à la fiche de l'objet. Un objet nommé dans la préparation ouvre sa
  fiche dans Objets, sur ses recettes s'il en a.
- La chaîne et les prix de la préparation suivent l'objet de la recette, et non l'objet choisi ailleurs entre-temps.
- Signalement de l'utilisateur : « ventures » dans les textes français (alarmes automatiques, vos personnages, sources). Remplacé par
  « tâches de servant », le terme du jeu français.
- Tests : CraftPreparation408Tests (1 cas). Verify.ps1 (agent) : 754/754, 0 avertissement, 0 erreur ; DLL 0.408.0.0. Non validé en jeu.
### English
- The item sheet's Craft tab keeps a short view (recipe, ingredients, stocks, links); the full preparation (quantities, cost,
  sub-recipe chain) moves to a « Preparation » tab of the Crafting page, linked both ways. French texts say « tâches de servant »
  instead of « ventures ».

## 0.407.0
- Demande de l'utilisateur (suite de 0.406) : les noms de monstres, de lieux et de missions venus du wiki et de Garland (anglais) affichés
  dans la langue du jeu.
- Correction : Tensho lit une fois, hors du fil du jeu, les noms anglais et ceux de la langue du client des feuilles BNpcName (monstres),
  PlaceName (lieux) et ContentFinderCondition (missions) ; un nom du wiki connu du jeu est remplacé par celui du jeu, les coordonnées
  qui le suivent sont gardées telles quelles. Lignes Garland portant un identifiant du jeu (quête, ALÉA, succès, mandat, PNJ) : nom lu
  par l'identifiant, gardé seulement si le nom anglais du jeu est celui de Garland. Un nom inconnu du jeu reste celui de la page.
  Rien n'est construit avec un client anglais.
- Appliqué : page Monstres (sources du wiki), monstres des sorts de mage bleu, survol d'un objet (monstre du wiki, lignes Garland).
  Le texte libre du wiki (section « Acquisition ») reste traduit par les termes connus, comme avant.
- Inconnu : marqueurs de grammaire des noms allemands (« [a] ») retirés à l'affichage, non vérifié sur un client allemand.
- Tests : CommunityNames407Tests (3 cas). Verify.ps1 (agent) : 753/753, 0 avertissement, 0 erreur ; DLL 0.407.0.0. Non validé en jeu.
### English
- Monster, place and duty names from the English wiki and Garland are shown in the game client's language when the game's own sheets
  know them (coordinates kept; Garland id rows kept only when the English names match); unknown names stay as the page wrote them.

## 0.406.0
- Demande de l'utilisateur : les données du jeu dans la langue du jeu, les textes de Tensho dans la langue du plugin (l'anglais étant
  l'original), mode développeur et journal couverts, journal en anglais ; mettre en place de quoi continuer ainsi.
- Audit : les 178 lectures de feuilles du jeu prennent déjà la langue du client (l'anglais ne sert qu'aux rapprochements avec le wiki
  et Garland) ; les 210 messages du journal sont en anglais. Mais 254 messages, erreurs et états étaient écrits en français dans le code :
  ils arrivaient en français dans le journal et en mode développeur, même avec le plugin en anglais.
- Correction :
  - ces textes sont maintenant en anglais dans le code ; leur texte français d'origine est dans les fichiers de langue (147 clés,
    UI/InternalMessages.cs), et les tables de traduction existantes s'y reportent ;
  - le mode développeur traduit ses messages et ses libellés (recette, mission, donneur de quête) ;
  - le journal reste en anglais.
  - Gardés en français : noms des cristaux et des fêtes du client français (reconnus dans le texte du jeu), alias de commande « pêche ».
- Pour la suite : règle écrite dans docs/ai/RULES.md, et test Languages406Tests qui refuse tout texte français écrit dans le code (hors
  fichiers de langue et exceptions listées) et vérifie que chaque clé a ses deux textes.
- Sauvegarde avant changement : checkpoints/backup-20261103-105245-avant-messages-anglais-0406.zip. Preuve : docs/evidence/0.406-languages.md.
- Tests : Languages406Tests (2 cas) ; les tests existants suivent les textes anglais. Verify.ps1 (agent) : 750/750, 0 avertissement,
  0 erreur ; DLL 0.406.0.0. Non validé en jeu.
### English
- Language rule applied to the whole code: internal messages, errors and states are written in English and translated through the
  language files (developer mode included); the log stays English; a guard test forbids French text written in the code.

## 0.405.0
- Demande de l'utilisateur : pour la récolte et l'artisanat, ouvrir la recette ou la ressource dans le carnet du jeu, au bon endroit.
- Clic droit sur un objet dans Tensho : « Ouvrir dans le carnet d'artisanat (job) », une entrée par recette (plusieurs artisans
  possibles), et « Ouvrir dans le carnet de récolte » pour un objet récolté.
- Page Récolte : bouton « Carnet » sur chaque lieu de récolte. Page Artisanat : « Ouvrir dans le carnet d'artisanat » sous la recette.
- Fonctions du jeu AgentRecipeNote.OpenRecipeByRecipeId et AgentGatheringNote.OpenGatherableByItemId, appelées seulement sur clic et
  si leur adresse est résolue ; elles ouvrent une fenêtre du jeu, rien n'est envoyé au serveur. Pêche : aucune fonction trouvée pour
  ouvrir le carnet de pêche sur un poisson, pas de bouton.
- Preuve : docs/evidence/0.405-game-logs.md.
- Tests : GameLogs405Tests (1 cas). Verify.ps1 (agent) : 748/748, 0 avertissement, 0 erreur ; DLL 0.405.0.0. Non validé en jeu.
### English
- User request: open the game's crafting log on a recipe or its gathering log on an item, from Tensho's item menu, the Gathering page
  and the Crafting page (click only; no fishing log function found).

## 0.404.0
- Signalement de l'utilisateur : la page Récolte restait sur « Carnets en cours de lecture (connectez-vous) » alors qu'il était en jeu.
- Cause (dalamud.log) : « index 10000 must be less than or equal to 304 » sur le carnet de pêche au harpon. Les 15 poissons au harpon les
  plus récents (lignes 30000 à 30014) sortent de la table du jeu que lit IsSpearfishCaught ; cette erreur faisait échouer toute la
  lecture.
- Correction : chaque ligne est demandée à part ; une ligne refusée est mise de côté (inconnue) et notée une fois dans le journal, les
  autres carnets sont lus. Les 15 poissons concernés restent sans ✓ (leur place réelle n'est pas connue ; GatherBuddy a la même limite).
- Preuve : docs/evidence/0.402-notebooks.md (section 0.404, sonde PROBE_LOGS).
- Tests : NotebookRows404Tests (1 cas). Verify.ps1 (agent) : 747/747, 0 avertissement, 0 erreur ; DLL 0.404.0.0. Non validé en jeu.
### English
- Fix: the notebooks never loaded because 15 recent spearfishing rows are outside the game's table; each row is now read on its own and
  a refused row stays unknown.

## 0.403.0
- Étape g), domaine 4 (Récolte), R-08 : alerte quand les PR remontent au seuil.
- Page PR : « Alerte PR au seuil » (désactivée par défaut) et « Seuil des PR » (0 = PR pleins), avec ↺.
- Un message dans le chat et le son des alarmes quand les PR d'un récolteur atteignent le seuil, une fois par remontée (réarmée sous le
  seuil, jamais à la connexion) ; pas en combat ; options des alarmes appliquées (silence en mission compris).
- Lecture : celle de la barre du tic de PR (0.391), sur le thread du jeu.
- Tests : GpAlert403Tests (1 cas). Verify.ps1 (agent) : 746/746, 0 avertissement, 0 erreur ; DLL 0.403.0.0. Non validé en jeu.
### English
- Queue step g, R-08: an optional chat message and sound when a gatherer's GP rises to a threshold (0 = full); off by default.

## 0.402.0
- Étape g), domaine 4 (Récolte), R-05 : suivi des carnets de pêche, de pêche au harpon et de récolte. Choix de l'utilisateur : export
  dans le presse-papiers et en fichier CSV.
- Page Récolte :
  - « Carnets : pêche x/y · pêche au harpon x/y · récolte x/y », lus dans le jeu pour le personnage connecté ;
  - ✓ à côté des ressources déjà au carnet ; case « Masquer ce qui est déjà au carnet » ;
  - « Copier les carnets (CSV) » et « Enregistrer en CSV » (dossier du plugin, Exports ; champs protégés contre les formules).
- Lecture (Gathering/NotebookReader) : toutes les 5 s pendant que la page est ouverte ; carnets de pêche par champs de bits (même indice
  que GatherBuddy, Apache-2.0), carnet de récolte par la fonction du jeu IsGatheringItemGathered, appelée seulement si son adresse est
  résolue (indice à confirmer en jeu).
- Preuve : docs/evidence/0.402-notebooks.md.
- Tests : Notebooks402Tests (1 cas). Verify.ps1 (agent) : 745/745, 0 avertissement, 0 erreur ; DLL 0.402.0.0. Non validé en jeu.
### English
- Queue step g, R-05: the Gathering page shows the fishing, spearfishing and gathering notebooks (counts, ✓, « hide what is done »)
  and exports them as CSV (clipboard and file).

## 0.401.0
- Deux signalements de l'utilisateur sur les panoramas.
- Erreur du wiki au survol d'un panorama (« The input string '27.2.' was not in a correct format ») :
  - cause : sur la page « Sightseeing Log » du wiki, une seule entrée écrit ses coordonnées « 25.2, 27.2. Z:1.0 » (un point en trop) ;
    sa lecture faisait échouer toute la page ;
  - correction : un nombre suivi d'un point en trop est lu sans lui, et une coordonnée qui n'est pas un nombre fait sauter sa ligne au
    lieu de toute la page ; relevé sur la vraie page : les 363 coordonnées se lisent.
- Clic droit sur la carte : l'onglet « Exploration » (panoramas de la zone) est retiré, le « Carnet d'exploration » (0.361) le couvre.
- Tests : WikiVistas401Tests (1 cas) ; le test de 0.400 compte maintenant 8 onglets. Verify.ps1 (agent) : 744/744, 0 avertissement,
  0 erreur ; DLL 0.401.0.0. Non validé en jeu.
### English
- Fix: the wiki vista page failed to read because of one stray dot in a coordinate; such a value is now read, an odd row is skipped.
- The map's right-click « Sightseeing » tab is removed (the Sightseeing log tab covers it).

## 0.400.0
- Signalement de l'utilisateur : clic droit sur la carte, problème de cadrage — les onglets des listes étaient coupés (« Triple Tri··· »,
  « Explorati··· », « Carnet d'··· »).
- Cause : le cadre avait une largeur fixe de 640 pixels, trop étroite pour les 9 onglets depuis l'onglet « Carnet d'exploration »
  (0.361).
- Correction : largeur calculée sur les noms des onglets (dans la langue choisie), au plus 90 % de l'écran ; au-delà, les onglets
  défilent avec des flèches au lieu d'être coupés.
- Tests : MapMenuWidth400Tests (1 cas, la liste des largeurs suit les onglets dessinés). Verify.ps1 (agent) : 743/743, 0 avertissement,
  0 erreur ; DLL 0.400.0.0. Non validé en jeu.
### English
- Fix: the Tensho map's right-click lists are now as wide as their tab names (tabs scroll on small screens instead of being cut).

## 0.399.0
- Demande de l'utilisateur : un réglage pour se téléporter dès le clic sur « Y aller ».
- Réglages › Modules › Récolte : « « Y aller » : téléporter tout de suite » (désactivé par défaut, avec ↺). Coché, un clic sur
  « Y aller » pose le drapeau et téléporte directement vers l'éthérite la plus proche, sans la question ; si le coût de cette éthérite
  n'est pas connu (liste de téléportation pas lue, éthérite non harmonisée), la question habituelle revient.
- Le clic sur « Y aller » reste le seul déclencheur ; le bouton « Éthérite » des liens du chat garde sa question.
- Les tests de 0.373 et 0.397 vérifient maintenant que la téléportation directe n'existe que pour cette demande, avec un coût connu.
- Verify.ps1 (agent) : 742/742, 0 avertissement, 0 erreur ; DLL 0.399.0.0. Non validé en jeu.
### English
- User request: an optional setting (off by default) makes « Go there » teleport at once to the nearest attuned aetheryte, without
  the question.

## 0.398.0
- Étape g), domaine 4 (Récolte), R-04 : appât en main dans l'aide à la pêche.
- En haut du tableau des poissons du lieu : « Appât en main », avec son icône et son stock, et le nombre de poissons du lieu
  possibles avec cet appât ; ces poissons ont un cadre vert autour de leur premier appât.
- Lecture (Gathering/FishingBaitReader) : PlayerState.FishingBait, toutes les 0,5 s sur le thread du jeu, pêcheur seulement ; champ lu
  sans fonction du jeu ; sens de la valeur à confirmer en jeu (docs/evidence/0.398-fishing-bait.md).
- Tests : FishingBait398Tests (1 cas). Verify.ps1 (agent) : 742/742, 0 avertissement, 0 erreur ; DLL 0.398.0.0. Non validé en jeu.
### English
- Queue step g, R-04: the fishing help shows the bait in hand with its stock and frames the fish it can catch at this spot.

## 0.397.0
- Étape g), domaine 4 (Récolte), R-03 : bouton « Y aller ».
- Fiche d'un objet de récolte, chaque lieu (récolte et pêche) : « Y aller » pose le drapeau du jeu sur le point, ouvre la carte Tensho
  centrée dessus et propose l'éthérite la plus proche de la carte (parmi votre liste de téléportation) avec son coût ; la téléportation
  se fait seulement sur « Oui ». Grisé quand la position du point n'est pas connue.
- Même mécanique que l'éthérite des liens du chat (0.373), généralisée à n'importe quel point.
- Tests : GoThere397Tests (1 cas). Verify.ps1 (agent) : 741/741, 0 avertissement, 0 erreur ; DLL 0.397.0.0. Non validé en jeu.
### English
- Queue step g, R-03: « Go there » on each gathering and fishing spot: flag, Tensho map, nearest aetheryte with its cost (teleport on
  confirmation only).

## 0.396.0
- Étape g), domaine 4 (Récolte), R-02 : prochaines fenêtres réelles des poissons.
- Fiche d'un poisson (données GatherBuddy) : nouvelle section « Prochaines fenêtres » — les 5 prochaines heures réelles où la plage ET,
  la météo et la météo précédente sont réunies en même temps, dans la zone du premier lieu de pêche, cherchées jusqu'à 35 jours devant ;
  pour chacune : début, fin, durée, compte à rebours (« ouverte maintenant ») et alarme unique.
- Calcul (Gathering/FishWindows) depuis les identifiants de météo de GatherBuddy et les tables météo du jeu ; une fenêtre à cheval sur
  minuit ou sur deux périodes est réunie ; une météo impossible à prévoir donne « inconnu », jamais « aucune fenêtre ».
- Tests : FishWindows396Tests (2 cas, chaque fenêtre vérifiée contre les tables). Verify.ps1 (agent) : 740/740, 0 avertissement,
  0 erreur ; DLL 0.396.0.0. Non validé en jeu.
### English
- Queue step g, R-02: a fish sheet now lists its next real windows (ET time, weather and previous weather together, up to 35 days ahead)
  with countdowns and one-off alarms.

## 0.395.0
- Demandes de l'utilisateur : le tri par niveau doit porter sur tous les sorts, pas seulement sur la fiche, avec un cache à l'ouverture
  de la page ; puis « ça prend 6 min ? ».
- Page Mage bleu : choix de l'ordre de la liste — numéro du sort, niveau du monstre croissant ou décroissant ; chaque sort montre le
  niveau de son monstre (« niv. 46 ») ; les sorts sans monstre (par défaut, quête…) restent à la fin.
- Source : le tableau « Blue Magic Spellbook » du wiki, une seule page pour les 124 sorts (au lieu de 124 pages, environ 6 minutes à
  3 secondes d'écart, version écartée avant d'être livrée) ; lu à l'ouverture de la page, gardé sur disque selon la durée de cache des
  sources, interrupteur du wiki respecté ; un échec réessaie au bout d'une minute.
- Niveau du monstre = le plus bas entre monstre de zone et mission, comme le tableau du wiki.
- Preuve : docs/evidence/0.395-blue-spellbook.md.
- Tests : BlueSpellIndex395Tests (1 cas). Verify.ps1 (agent) : 738/738, 0 avertissement, 0 erreur ; DLL 0.395.0.0. Non validé en jeu.
### English
- User request: the whole blue magic list can be sorted by the teaching monster's level, from the wiki's single spellbook table read
  once when the page opens (cached), instead of one page per spell.

## 0.394.0
- Demande de l'utilisateur : un réglage pour changer les polices.
- Réglages › Interface › Polices : famille et taille (10 à 40 pixels) des titres de pages, des titres de sections (et catégories du
  menu) et du texte de la fenêtre Tensho, chacun avec ↺.
  - Familles : les polices du jeu fournies par Dalamud (Axis, Jupiter, Meidinger, MiedingerMid, TrumpGothic) ; pour le texte, aussi la
    police de Dalamud (par défaut).
  - Défauts inchangés : TrumpGothic 24 et 19, police de Dalamud pour le texte.
- Les polices sont reconstruites au début de l'image suivante quand un réglage change (UI/FontManager), sinon rien n'est refait ;
  « Restaurer tous les réglages » les remet par défaut.
- Tests : Fonts394Tests (1 cas). Verify.ps1 (agent) : 737/737, 0 avertissement, 0 erreur ; DLL 0.394.0.0. Non validé en jeu.
### English
- User request: Settings › Interface › Fonts — family and size of page titles, section titles and text (game fonts, or Dalamud's for
  the text), applied at once.

## 0.393.0
- Deux signalements de l'utilisateur sur la page de 0.364.
- Nom : en français, le jeu l'appelle « Bestiaire » (« chasse » désigne les contrats de chasse). Le menu, le titre et les textes disent
  maintenant « Bestiaire ».
- Grande compagnie : la page proposait les carnets des trois compagnies. Seul celui de la compagnie du personnage est maintenant montré
  (lu dans le jeu avec les carnets, PlayerState.GrandCompany), et choisi à l'ouverture ; aucun sans compagnie ; les trois tant qu'elle
  n'est pas lue. Comme le jeu et Hunty (MIT, lu seulement : il ne montre que la compagnie actuelle).
- Tests : HuntingLogCompany393Tests (1 cas). Verify.ps1 (agent) : 736/736, 0 avertissement, 0 erreur ; DLL 0.393.0.0. Non validé en jeu.
### English
- Fix: the French hunting log page is now called « Bestiaire », and it shows only the character's own Grand Company log.

## 0.392.0
- Étape g), domaine 4 (Récolte), R-01 : calendrier de la pêche en mer. Choix de l'utilisateur : onglet « Pêche en mer » (fait en page
  à part dans Production, à côté de Récolte).
- Prochains départs (toutes les 2 heures) : heure, compte à rebours, « Embarquement ouvert » pendant 15 minutes, trajet depuis Limsa
  Lominsa (Indigo) et depuis Kugane (Rubis), nommés comme dans le jeu avec leur moment ; survol : les trois escales et leur moment.
- Filtre sur un trajet (prochains départs de ce trajet, depuis l'un ou l'autre port) ; alarme Tensho avant un départ.
- Horaires calculés depuis la boucle du jeu (IKDRouteTable, 144 départs) ; début de boucle et sens des moments lus dans GatherBuddy
  (Apache-2.0, faits seulement, code original).
- Preuve : docs/evidence/0.392-ocean-fishing.md (sonde PROBE_OCEAN).
- Tests : OceanFishing392Tests (1 cas). Verify.ps1 (agent) : 735/735, 0 avertissement, 0 erreur ; DLL 0.392.0.0. Non validé en jeu.
### English
- Queue step g, R-01: new Ocean fishing page — next departures, Indigo and Ruby voyages with their stops, countdown, alarm.

## 0.391.0
- Étape g), domaine 3 (Monde), X-06 : barre du tic de régénération, pour les PR des récolteurs.
- Barre à l'écran (désactivée par défaut, interrupteur en haut de la page PR) : PR actuels, maximum et temps avant le prochain tic
  (toutes les 3 secondes). Mineur, botaniste et pêcheur seulement ; cachée en combat et à PR pleins ; déplaçable.
- Le tic est repéré quand les PR montent sans être pleins ; un maximum qui monte (équipement changé) à PR pleins n'est pas un tic
  (trouvé par le test). PV et PM laissés de côté : le combat est hors périmètre (DEC-009).
- Code original (idée de Tick Tracker, AGPL, lu seulement).
- Tests : RegenTick391Tests (1 cas). Verify.ps1 (agent) : 734/734, 0 avertissement, 0 erreur ; DLL 0.391.0.0. Non validé en jeu.
### English
- Queue step g, X-06: an optional on-screen GP tick bar for gatherers (time to the next 3-second regeneration tick); off by default.

## 0.390.0
- Demande de l'utilisateur : trier par niveau les monstres d'un sort dans le grimoire du mage bleu.
- Fiche d'un sort, liste « Monstres qui l'enseignent » : choix de l'ordre — niveau croissant (par défaut), niveau décroissant ou ordre
  du wiki. Une plage « 43-48 » compte pour 43 ; un monstre sans niveau reste à la fin.
- Tests : BlueMonsterOrder390Tests (1 cas). Verify.ps1 (agent) : 733/733, 0 avertissement, 0 erreur ; DLL 0.390.0.0. Non validé en jeu.
### English
- User request: the monsters that teach a blue magic spell can be sorted by level (lowest or highest first) or kept in wiki order.

## 0.389.0
- Bouton « Trier » de la fenêtre d'inventaire : l'icône de tri, qui était dessinée à côté et ne faisait rien au clic, est maintenant dans
  le bouton (choix de l'utilisateur) : tout « ⇅ Trier » est cliquable.
- Verify.ps1 (agent) : 732/732, 0 avertissement, 0 erreur ; DLL 0.389.0.0. Non validé en jeu.
### English
- The inventory « Sort » button now carries its icon inside, so the whole button is clickable (the icon beside it did nothing).

## 0.388.0
- Demande de l'utilisateur : un bouton dans la fenêtre d'inventaire du jeu pour appliquer les règles de tri, « petit mais pas trop »,
  en bas à gauche sur la même ligne que les gils.
- Bouton « Trier » (avec son icône) dessiné dans la fenêtre d'inventaire du jeu, quelle que soit sa disposition (normale, grande,
  étendue), à son échelle ; un clic applique vos règles enregistrées de « Mes sacs », exactement comme « Appliquer » sur la page
  Rangements › Tri (seul l'ordre affiché change, piles et quantités gardées) ; un message court s'affiche à côté pendant 6 secondes.
- Rien ne se fait sans le clic. Position de la fenêtre copiée sur le thread du jeu (Inventory/InventoryWindowReader).
- Interrupteur « Bouton « Trier » sur la fenêtre d'inventaire du jeu » en haut de la page Tri, activé par défaut, avec ↺.
- Premier essai en jeu de l'utilisateur : bouton trop bas, sous la fenêtre (le cadre de la fenêtre du jeu dépasse sa partie visible).
  Position relevée en deux essais (57 puis 42 unités au-dessus du bas, sur la ligne des gils ; 24 depuis la gauche) et rendue réglable : « Bouton : depuis la gauche » et « Bouton :
  depuis le bas » sous l'interrupteur, avec ↺ ; elles suivent l'échelle de la fenêtre.
- Tests : SortButton388Tests (1 cas). Verify.ps1 (agent) : 732/732, 0 avertissement, 0 erreur ; DLL 0.388.0.0. Non validé en jeu
  (placement sur la ligne des gils à confirmer).
### English
- User request: a « Sort » button inside the game's inventory window (bottom left, on the gil line) applies your saved bag sorting
  rules on click, like « Apply » on the Sort page; on by default, switch at the top of that page.

## 0.387.0
- Deux signalements de l'utilisateur corrigés.
- Grimoire du mage bleu : « Chercher sur le wiki » ne donnait jamais de monstre.
  - Cause : le nom anglais du sort passait par le lien AozAction → Action, résolu dans la langue du client ; le wiki recevait
    « Mauvaise_haleine », répondait « page inexistante », et Tensho disait « aucun monstre ».
  - Correction : nom pris dans la feuille Action anglaise par numéro de ligne ; nouveau cache disque pour ne pas relire les réponses
    vides ; une page absente est maintenant dite comme telle, avec son adresse.
  - Relevé : la page « Bad Breath » du wiki donne 19 monstres, lus par les règles de Tensho.
  - Autres plugins regardés, comme demandé : Blue Mage Helper n'a toujours pas de licence (vérifié le 2026-11-03 ; table de sorts en
    lecture seule), BeastieBuddy est sous AGPL : le wiki reste la source, sur clic.
- Collections, onglets de 0.365 : pas d'infobulle « obtention ».
  - Coiffures, Accessoires de mode, Bardes et Livres de maître ont maintenant l'objet qui les apprend (données du jeu : objet de la
    coiffure, ItemAction 20086 et 1013 avec Data[0], objet du livre) : survol = infobulle de l'objet avec son obtention, clic droit =
    sources.
  - Lunettes, Portraits, Objets de collection et Archives occultes : les données du jeu ne relient aucun objet (sonde) ; l'infobulle le
    dit au lieu d'être vide.
  - Tenues : survol d'une tenue ou d'une pièce = infobulle de l'objet.
- Preuves : docs/evidence/0.387-blue-spell-name.md, docs/evidence/0.387-collection-items.md (sonde PROBE_UNLOCKS).
- Le test de 0.367 suit le nouveau nom du cache.
- Tests : BlueSpellName387Tests, CollectionItems387Tests (1 cas chacun). Verify.ps1 (agent) : 731/731, 0 avertissement, 0 erreur ;
  DLL 0.387.0.0. Non validé en jeu.
### English
- Fix: the blue magic spell window never found the teaching monsters (the French spell name was sent to the wiki); it now uses the
  English Action sheet, drops the old cached answers and says when the wiki has no page.
- Fix: the collection tabs added in 0.365 had no « how to obtain » tooltip; hairstyles, accessories, bardings and master books now
  show their teaching item, the others say it is unknown, outfits show the item tooltip.

## 0.386.0
- Étape g), domaine 3 (Monde), X-10 : horloges sur plusieurs fuseaux et alarmes à la main.
- Alarmes, nouvelle section « Horloges » : l'heure de fuseaux horaires choisis (6 au plus, liste des fuseaux de Windows avec recherche),
  chacun retirable.
- Alarme unique à une date réelle : à côté de l'alarme quotidienne ET, une date « AAAA-MM-JJ HH:MM » à l'heure du PC (dans l'année qui
  vient) et « Ajouter à cette date » ; le nom tapé pour l'alarme ET sert aussi ; groupe « Alarmes à date ».
- Tests : WorldClocks386Tests (1 cas). Verify.ps1 (agent) : 729/729, 0 avertissement, 0 erreur ; DLL 0.386.0.0. Non validé en jeu.
### English
- Queue step g, X-10: clocks for up to 6 chosen time zones on the Alarms page, and a one-off alarm at a typed local date.

## 0.385.0
- Étape g), domaine 3 (Monde), X-08 : job au hasard pour les quotidiens.
- Équipement › Ensembles d'équipement du jeu : bouton « Job au hasard » (un tirage par job, son ensemble de plus petit numéro) et case
  « Seulement les jobs pas au niveau le plus haut » (cochée) ; le job tiré s'affiche avec son ensemble.
- « Équiper » porte l'ensemble tiré, sur ce clic seulement, par la fonction du jeu EquipGearset (appelée seulement si son adresse est
  résolue et l'ensemble valide, 2 secondes d'écart) ; rien n'est changé dans les ensembles.
- Le test de 0.383 vérifie maintenant qu'aucune fonction qui modifie un ensemble n'est utilisée (mise à jour, suppression, nom).
- Tests : RandomJob385Tests (1 cas). Verify.ps1 (agent) : 728/728, 0 avertissement, 0 erreur ; DLL 0.385.0.0. Non validé en jeu.
### English
- Queue step g, X-08: « Random job » among your gear sets (one draw per job, optionally only jobs below the top level).
  - « Equip » wears it on that click only, through the game's own gear set function.

## 0.384.0
- Étape g), domaine 3 (Monde), M-14 : emplacement de la carte au trésor déchiffrée.
- Carte Tensho : une gemme à l'endroit de la carte au trésor que vous avez déchiffrée (avec la couche des coffres au trésor) ;
  survol : son nom ; clic : drapeau du jeu.
- Rappels, ligne « Cartes au trésor » : bouton « Voir sur la carte », qui ouvre la carte Tensho sur l'emplacement et y pose le drapeau.
- Lecture (Maps/TreasureMapReader) : toutes les 5 secondes, fonctions du jeu GetTreasureHuntRank et GetTreasureSpotSubKey appelées
  seulement si leur adresse est résolue ; emplacement tiré des données du jeu (TreasureSpot puis Level).
- Preuve : docs/evidence/0.384-treasure-map.md.
- Tests : TreasureMap384Tests (1 cas). Verify.ps1 (agent) : 727/727, 0 avertissement, 0 erreur ; DLL 0.384.0.0. Non validé en jeu.
### English
- Queue step g, M-14: the deciphered treasure map's spot on the Tensho map (click = flag), and « Show on the map » in Reminders.

## 0.383.0
- Étape g), domaine 3 (Monde), M-12 : ensembles d'équipement du jeu. Choix de l'utilisateur : partage par le code TENSHO-GEAR1
  seulement, aucun site tiers.
- Équipement, nouvelle section « Ensembles d'équipement du jeu » : chaque ensemble (numéro, nom, job, niveau d'objet), groupé par job.
  - Déplié : pour chaque emplacement, une pièce que vous possédez déjà (sacs, arsenal, sacoches, servants enregistrés) avec un meilleur
    niveau d'objet, que le job peut porter à son niveau ; le gain en niveaux d'objet.
  - « Copier le code » : l'ensemble en code TENSHO-GEAR1 (pièces et matérias), le même format que les plans de la page.
- Lecture (Gear/GameGearsetReader) : champs des ensembles et niveaux des jobs, seulement pendant que la page les montre ; aucune
  fonction du jeu appelée, aucun ensemble changé.
- Preuve : docs/evidence/0.383-gearsets.md.
- Tests : Gearsets383Tests (2 cas). Verify.ps1 (agent) : 726/726, 0 avertissement, 0 erreur ; DLL 0.383.0.0. Non validé en jeu.
### English
- Queue step g, M-12: the game's gear sets on the Gear page, with the better pieces you already own for each set's job.
  - Each set copies as a TENSHO-GEAR1 code; Tensho never changes a set.

## 0.382.0
- Étape g), domaine 3 (Monde), M-11, en partie : autres compteurs.
- Rappels, deux lignes de plus (remise hebdomadaire) :
  - « Carnet d'objectifs » : défis faits sur le total, lus dans le jeu une fois le carnet ouvert (le jeu ne le charge qu'alors) ;
  - « Enclave de Doman » : dons de la semaine sur le plafond, lus après un passage à l'enclave.
- Déjà là : escouade (0.333), mémoquartz plafonnés (onglet Monnaies, 0.344). Laissés de côté, rien de lisible trouvé : Mini et Jumbo
  Cactpot, objectifs hebdomadaires du Carnaval masqué.
- Lectures de champs seulement (aucune fonction du jeu appelée), protégées à part.
- Preuve : docs/evidence/0.382-counters.md. Le test de 0.356 suit le nouvel appel des compteurs.
- Tests : Counters382Tests (1 cas). Verify.ps1 (agent) : 724/724, 0 avertissement, 0 erreur ; DLL 0.382.0.0. Non validé en jeu.
### English
- Queue step g, M-11 (part): Reminders show the challenge log (once opened) and the Doman Enclave donations of the week.
  - Mini / Jumbo Cactpot and Masked Carnivale weekly targets are not readable yet.

## 0.381.0
- Étape g), domaine 3 (Monde), X-01 : liste de tâches libre.
- Vos tâches (Rappels) : nouveau type « Une fois », une tâche libre qui reste faite jusqu'à ce qu'on la retire.
- Nouvelle commande « /tensho todo <tâche> » (ou « tache ») : ajoute une tâche « Une fois » en fin de liste et le dit dans le chat ;
  refusée avec un message si le nom est vide ou trop long, ou si la liste est pleine.
- Tests : FreeTasks381Tests (1 cas). Verify.ps1 (agent) : 723/723, 0 avertissement, 0 erreur ; DLL 0.381.0.0. Non validé en jeu.
### English
- Queue step g, X-01: free « Once » tasks in Reminders › Your tasks, and « /tensho todo <task> » to add one from the chat.

## 0.380.0
- Étape g), domaine 3 (Monde), M-09, en partie : paliers de statistiques. Choix de l'utilisateur : gardé comme information sur son
  propre personnage (DEC-009).
- Tableau des valeurs réelles (page Équipement, panneau près de la fenêtre Personnage) : nouvelle colonne « +N palier », les points
  encore nécessaires pour que la valeur passe à la suivante ; l'aide au survol dit aussi les points au-delà du palier actuel qui ne
  changent rien.
- Calcul exact sur les formules déjà utilisées (aucune nouvelle donnée) ; temps de recharge global cherché pas à pas.
- Non fait : statistiques en contenu synchronisé (fonctions du jeu à sonder en jeu).
- Tests : StatTiers380Tests (2 cas, vérifiés pas à pas contre les valeurs affichées). Verify.ps1 (agent) : 722/722, 0 avertissement,
  0 erreur ; DLL 0.380.0.0. Non validé en jeu.
### English
- Queue step g, M-09 (part): the real-value table shows the points to the next stat tier, with the wasted points in its help.
  - Item-level sync stats are not done (game functions to probe in game).

## 0.379.0
- Étape g), domaine 3 (Monde), M-10 : historique du butin des vaisseaux. Choix de l'utilisateur : 180 jours, affiché à part de la
  moyenne communautaire, export.
- Compagnie libre, nouvel onglet « Historique » :
  - chaque voyage lu à l'atelier avec son butin (un enregistrement par voyage, mis à jour tant que c'est le même voyage) ;
  - « Rendement réel par trajet » : nombre de voyages et objets rapportés en moyenne par voyage, sans estimation ;
  - « Derniers voyages » : les 50 derniers ;
  - « Copier en CSV » : tout l'historique dans le presse-papiers, une ligne par objet (champs protégés contre les formules des tableurs).
- Un fichier local (VoyageHistory.json), 180 jours et 5 000 voyages au plus ; un fichier abîmé n'est jamais réécrit.
- Code original (idées de Submarine Tracker, MIT, lu seulement).
- Tests : VoyageHistory379Tests (3 cas). Verify.ps1 (agent) : 720/720, 0 avertissement, 0 erreur ; DLL 0.379.0.0. Non validé en jeu.
### English
- Queue step g, M-10: Free company › History keeps each voyage's loot read in the workshop (180 days).
  - Real yield per route (average per voyage) and a CSV copy to the clipboard.

## 0.378.0
- Étape g), domaine 3 (Monde), M-08 : favoris dessinés sur la carte Tensho, avec note, groupe et partage.
- Carte Tensho :
  - nouvelle couche « Vos favoris » (activée) : une étoile par favori de la carte affichée ; survol : nom, groupe, coordonnées et
    note ; clic : drapeau du jeu sur le favori ;
  - clic droit sur la carte : « Ajouter aux favoris », avec un nom (vide : la zone et les coordonnées).
- Page Cartes :
  - un favori peut avoir un groupe (montré entre crochets) et une note (au survol) ; les favoris d'avant se chargent tels quels ;
  - « Copier le code de partage » met tous vos favoris (200 au plus) dans le presse-papiers sous la forme d'un code TENSHO-MAP1 ;
    « Importer » ajoute ceux d'un code collé qui ne sont pas déjà là. Aucun site tiers.
- Tests : MapBookmarks378Tests (3 cas). Verify.ps1 (agent) : 717/717, 0 avertissement, 0 erreur ; DLL 0.378.0.0. Non validé en jeu.
### English
- Queue step g, M-08: bookmarks drawn as stars on the Tensho map (click = flag), added from the map's right-click menu.
  - Optional note and group; shared as a TENSHO-MAP1 text code (clipboard), imported without duplicates.

## 0.377.0
- Étape g), domaine 3 (Monde), M-07 : comparaison avec la pièce portée au survol d'un objet.
- Sous l'infobulle d'une pièce d'équipement (inventaire, marché, partout où le panneau Tensho s'affiche), en tête du panneau :
  « Comparé à l'équipement porté » : niveau d'objet et statistiques en plus (vert) ou en moins (rouge) que la pièce portée au même
  emplacement ; une bague est comparée aux deux bagues portées.
- Valeurs de base, NQ ou HQ selon l'objet, sans matérias des deux côtés (celles de l'objet survolé ne sont pas connues).
- Nouvelle case « Comparaison avec la pièce portée » dans les parties du panneau (réglages, activée par défaut).
- Code original (idée de SimpleCompare, MIT, lu seulement).
- Tests : GearCompare377Tests (2 cas). Verify.ps1 (agent) : 714/714, 0 avertissement, 0 erreur ; DLL 0.377.0.0. Non validé en jeu.
### English
- Queue step g, M-07: under a gear piece's tooltip, item level and stat differences with the worn piece of that slot.
  - Both rings for a ring; base values without materia; its own switch, on by default.

## 0.376.0
- Étape g), domaine 3 (Monde), M-06 : usure et symbiose de l'équipement porté. Choix de l'utilisateur : alerte Tensho et barre à
  l'écran, chacune en option, seuils réglables.
- Équipement, nouvelle section « Usure et symbiose » : l'état et la symbiose de chaque pièce portée ; réglages :
  - « Alerte réparation et extraction » (activée) : un message dans le chat et le son des alarmes quand une pièce passe sous le seuil
    de réparation ou atteint le seuil de symbiose, une fois par passage (options des alarmes appliquées, silence en mission compris) ;
  - « Barre d'usure à l'écran » (désactivée) : l'état le plus bas et la symbiose la plus haute, déplaçable ;
  - seuils « Réparer sous » (10 %) et « Extraire à partir de » (100 %), avec ↺.
- Lecture : l'état par la fonction du jeu GetConditionPercentage (appelée seulement si son adresse est résolue), la symbiose par le
  champ de l'objet (10 000 points = 100 %, à confirmer en jeu) ; chaque lecture protégée à part ; objets de collection exclus.
- Preuve : docs/evidence/0.376-gear-wear.md.
- Tests : GearWear376Tests (3 cas). Verify.ps1 (agent) : 712/712, 0 avertissement, 0 erreur ; DLL 0.376.0.0. Non validé en jeu.
### English
- Queue step g, M-06: worn gear condition and spiritbond on the Gear page, with a repair / extraction chat alert (on by default).
  - An optional on-screen bar (off by default) and adjustable thresholds.

## 0.375.0
- Étape g), domaine 3 (Monde), M-05 : tâches personnelles et panneau « à faire ». Choix de l'utilisateur : jour, semaine, toutes les
  n heures, compteur ; panneau à l'écran en option, désactivé par défaut.
- Rappels, nouvelle section « Vos tâches » (50 au plus) :
  - chaque jour ou chaque semaine (aux remises à zéro du jeu), toutes les n heures après avoir été faite, ou compteur jusqu'à un
    objectif (boutons « - » et « + », « Remettre à 0 ») ;
  - chaque tâche dit quand elle revient ; flèches pour l'ordre ; « Retirer » ;
  - case « À l'écran » par tâche.
- Panneau « Tensho — À faire » à l'écran (réglage dans la section, désactivé par défaut) : les tâches non faites cochées « À l'écran »,
  cochables dans le panneau ; caché en combat ; le fermer décoche le réglage.
- Les tâches sont gardées par « Restaurer tous les réglages » ; le panneau, lui, revient à désactivé.
- Tests : PersonalTasks375Tests (3 cas). Verify.ps1 (agent) : 709/709, 0 avertissement, 0 erreur ; DLL 0.375.0.0. Non validé en jeu.
### English
- Queue step g, M-05: your own tasks on the Reminders page (daily, weekly, every n hours, counter).
  - Optional on-screen « to do » panel, off by default and hidden in combat.

## 0.374.0
- Étape g), domaine 3 (Monde), M-04 : suivi de plusieurs personnages. Choix de l'utilisateur : un fichier par personnage, comme I-03.
- Rappels, nouvelle section « Vos personnages » : pour chaque personnage, ce que Tensho a lu à sa dernière connexion :
  - ventures des servants (terminées, en cours, prochaine fin ; connues après l'ouverture de la liste des servants) ;
  - prochaine carte au trésor, Livre d'or des défis (vignettes, expiration), contrats de chasse en main ;
  - missions aléatoires faites aujourd'hui (« remis à zéro » quand la lecture date d'avant la remise quotidienne) ;
  - heure de la dernière lecture, « Connecté » pour le personnage en jeu.
- Les cases « fait » des Rappels sont maintenant propres au personnage connecté ; les anciennes cases communes vont une fois au premier
  personnage vu (après l'écriture de son fichier seulement).
- Un fichier par personnage (Characters/<identifiant local du personnage>.json, jamais d'identifiant de compte), écrit quand quelque
  chose change ; un fichier qui contient un autre personnage est refusé et jamais écrasé. « Effacer » puis « Confirmer » supprime le
  fichier d'un autre personnage (le personnage connecté ne peut pas l'être).
- Les vaisseaux restent sur la page Compagnie libre : ils sont à la compagnie, pas à un personnage.
- Tests : CharacterStatus374Tests (3 cas). Verify.ps1 (agent) : 706/706, 0 avertissement, 0 erreur ; DLL 0.374.0.0. Non validé en jeu.
### English
- Queue step g, M-04: Reminders show all your characters (ventures, treasure map, Wondrous Tails, hunt bills, today's roulettes).
  - One file per character, local character id only; a character can be erased.
  - The Reminders « done » boxes now belong to the logged-in character.

## 0.373.0
- Étape g), domaine 3 (Monde), M-03 : liens de carte du chat.
- Page Cartes, nouvelle section « Liens de carte du chat » :
  - les liens de carte vus dans le chat depuis la connexion, tous les canaux, du plus récent au plus ancien (100 au plus) ;
  - filtre par canal (dire, crier, hurler, équipe, alliance, compagnie libre, linkshells, linkshells inter-mondes, messages privés,
    réseau des novices, autres) ;
  - « Drapeau » pose le drapeau du jeu sur le point ;
  - « Éthérite » ouvre la carte Tensho sur le point et propose l'éthérite la plus proche de la carte parmi votre liste de
    téléportation, avec son coût ; la téléportation se fait seulement après « Oui ».
- Gardés en mémoire seulement, jamais enregistrés ; effacés à la déconnexion. Le message du chat n'est jamais modifié.
- Réglage « Liens de carte du chat » dans la carte Tensho (section Jeu), activé par défaut.
- Preuve : docs/evidence/0.373-chat-map-links.md.
- Tests : ChatMapLinks373Tests (4 cas). Verify.ps1 (agent) : 703/703, 0 avertissement, 0 erreur ; DLL 0.373.0.0. Non validé en jeu.
### English
- Queue step g, M-03: the Maps page lists the map links seen in the chat this session, filterable by channel.
  - A click places the flag; another offers the nearest teleportable aetheryte with its cost, teleporting only on confirmation.
  - Memory only, cleared at logout.

## 0.372.0
- Étape g), domaine 3 (Monde), M-02 : compteurs hebdomadaires lus dans le jeu.
- Rappels :
  - nouvelle ligne « Livre d'or des défis » : vignettes posées sur 9, points de seconde chance, expiration du livre ;
  - nouvelle ligne « Contrats de chasse » : nombre de contrats en main ;
  - « Cartes au trésor » : l'heure de la prochaine carte vient du jeu quand elle est lue (le bouton « ouverte » reste en secours).
- Panneau Tensho de l'outil de recherche de mission : sous une mission aléatoire choisie, « Faite » ou « À faire » aujourd'hui.
- Chaque lecture est protégée à part ; les fonctions du jeu sont appelées seulement si leur adresse est résolue.
- Les roulettes du jour étaient déjà dans Rappels depuis 0.356.
- Preuve : docs/evidence/0.372-weekly-counters.md.
- Tests : WeeklyCounters372Tests (1 cas). Verify.ps1 (agent) : 699/699, 0 avertissement, 0 erreur ; DLL 0.372.0.0. Non validé en jeu.
### English
- Queue step g, M-02: Reminders now read Wondrous Tails, the hunt bills held and the next treasure map allowance from the game.
  - The Duty Finder panel says whether the chosen roulette was done today.

## 0.371.0
- Étape g), domaine 3 (Monde), M-01 : alarmes automatiques.
- Tensho crée lui-même une alarme Tensho :
  - à la fin de chaque venture de servant (connue après l'ouverture de la liste des servants) ;
  - au retour de chaque vaisseau de la compagnie (connu après l'atelier).
- L'heure suit les relevés ; l'alarme disparaît une heure après avoir sonné ; les alarmes faites par le joueur ne sont jamais touchées.
- Aucune alarme du jeu (/alarm) n'est créée d'office ; la limite de 60 alarmes est gardée.
- Réglage « Alarmes automatiques (ventures, vaisseaux) » dans Alarmes, activé par défaut (choix fait dans GAPS.md).
- Lecture (Alarms/AutoAlarmWatcher) : toutes les 30 secondes sur le thread du jeu, servants en mémoire et vaisseaux déjà enregistrés.
- Tests : AutoAlarms371Tests (1 cas). Verify.ps1 (agent) : 698/698, 0 avertissement, 0 erreur ; DLL 0.371.0.0. Non validé en jeu.
### English
- Queue step g, M-01: automatic Tensho alarms at each retainer venture's end and each vessel's return.
  - The time follows the readings; the player's own alarms are never touched.
  - On by default, as chosen.

## 0.370.0
- Étape g), domaine 2 (Collections), X-02 (en partie) : musique en cours et rouleaux favoris.
- Collections › Orchestrion, en tête :
  - « Musique en cours : … », avec son rouleau obtenu ou non et un bouton « Fiche » ;
  - « Rouleaux favoris », ajoutés par la case « Favori » de la fiche d'un rouleau (300 au plus, gardés par la réinitialisation).
- Lecture (Collections/BgmReader) : scènes de BGMSystem, en mémoire seulement, une fois par seconde pendant que l'onglet est ouvert.
  - Rouleau trouvé par le chemin du fichier de la musique (feuilles BGM et OrchestrionPath).
- Changer la musique côté client n'est pas fait : la fonction du jeu a des paramètres non documentés (noté).
- Preuve : docs/evidence/0.370-orchestrion-player.md.
- Tests : OrchestrionPlayer370Tests (1 cas). Verify.ps1 (agent) : 697/697, 0 avertissement, 0 erreur ; DLL 0.370.0.0. Non validé en jeu.
### English
- Queue step g, X-02 (partly): the Orchestrion tab shows the music playing now and its roll, plus favourite rolls.
  - Changing the music is not done.

## 0.369.0
- Étape g), domaine 2 (Collections), X-03 : changer de titre.
- Fenêtre d'un titre obtenu :
  - « Porter ce titre » ;
  - « Titre de <job> » : le choisit pour le job actuel.
- Onglet Titres, en tête « Changer de titre » :
  - « Titre au hasard » (parmi les titres obtenus), « Aucun titre » ;
  - case « Changer de titre avec le job » (désactivée par défaut) et la liste job → titre, avec « Retirer ».
- Le changement passe par la fonction du jeu (TitleController.SendTitleIdUpdate), sur le thread du jeu.
  - Adresse vérifiée, 2 secondes au moins entre deux changements, jamais à la connexion.
- La liste job → titre est gardée par « Réinitialiser les réglages » ; la case revient à « désactivée ».
- Preuve : docs/evidence/0.369-titles.md.
- Tests : Titles369Tests (1 cas). Verify.ps1 (agent) : 696/696, 0 avertissement, 0 erreur ; DLL 0.369.0.0. Non validé en jeu.
### English
- Queue step g, X-03: wear a title, a random title or none.
  - Optional title by job, off by default, applied on a job change only.
  - Uses the game's TitleController with its address checked.

## 0.368.0
- Étape g), domaine 2 (Collections), C-15 : progression des succès et succès suivis.
- Fenêtre d'un succès :
  - case « Suivre ce succès » ;
  - progression « x / y » quand le jeu l'a reçue (en ouvrant le succès dans la fenêtre du jeu, ou avec « Demander ») ;
  - « Demander » envoie une requête au serveur, seulement sur ce clic, 3 secondes d'écart au moins.
- Onglet Succès : « Succès suivis » en tête (pas encore terminés d'abord), avec leur progression, « Demander » et « Retirer ».
- Lecture (Collections/AchievementProgressWatcher) : sur le thread du jeu, une fois par seconde, en mémoire par personnage.
  - L'adresse de la fonction de demande est vérifiée avant l'appel.
- Les succès suivis (100 au plus) sont gardés par « Réinitialiser les réglages ».
- Preuve : docs/evidence/0.368-achievement-progress.md.
- Tests : AchievementProgress368Tests (1 cas). Verify.ps1 (agent) : 695/695, 0 avertissement, 0 erreur ; DLL 0.368.0.0. Non validé en jeu.
### English
- Queue step g, C-15: achievement progress and tracked achievements.
  - Progress is read when the game receives it, or asked from the server on the player's click only.
  - Tracked achievements are listed at the top of the Achievements tab.

## 0.367.0
- Étape g), domaine 2 (Collections), C-13 : monstres qui enseignent un sort de mage bleu.
- Mage bleu › fenêtre d'un sort, nouvelle partie « Monstres qui l'enseignent (wiki) » :
  - un clic lit la page du sort sur le wiki (seul le nom anglais du sort est envoyé ; interrupteur du wiki respecté) ;
  - chaque monstre avec son niveau et son lieu ;
  - un bouton « Drapeau » quand le lieu a des coordonnées et que la zone est reconnue (nom anglais du lieu → zone du jeu).
- Le lecteur des tableaux du wiki est celui des butins de monstres, avec un cache à part (« wiki-blue-spells »).
- Test 0.324 mis à jour : quatre services suivent maintenant l'interrupteur du wiki.
- Preuve : docs/evidence/0.367-blue-spell-wiki.md.
- Tests : BlueSpellSources367Tests (1 cas). Verify.ps1 (agent) : 694/694, 0 avertissement, 0 erreur ; DLL 0.367.0.0. Non validé en jeu.
### English
- Queue step g, C-13: the blue magic spell window lists the monsters that teach it, from the wiki on click.
  - Each monster comes with its level and place.
  - A Flag button appears when the place has coordinates.

## 0.366.0
- Étape g), domaine 2 (Collections), C-10 : alerte quand une mission est prête.
- Alarmes › réglages, nouvelle case « Alerte quand une mission est prête » (désactivée par défaut).
  - Quand l'outil de recherche de mission trouve le groupe, Tensho écrit « Mission prête : … » dans la discussion et joue le son des
    alarmes, avec leurs règles de silence en mission.
- Discord : nouvelle catégorie « Mission prête » ; le message part seulement si l'alerte est activée et l'adresse réglée.
- Lecture : événement IClientState.CfPop de Dalamud, retiré à la fermeture du plugin.
- Tests : DutyPop366Tests (1 cas). Verify.ps1 (agent) : 693/693, 0 avertissement, 0 erreur ; DLL 0.366.0.0. Non validé en jeu.
### English
- Queue step g, C-10: an optional « duty ready » alert (chat line and alarm sound, same silence rules).
  - Off by default.
  - Discord gets its own « Duty ready » category.

## 0.365.0
- Étape g), domaine 2 (Collections), C-04 : huit types de collection en plus, dans l'ordre choisi.
- Nouveaux onglets de Collections :
  - coiffures (vendues comme objet), accessoires de mode, lunettes ;
  - bardes de chocobo ;
  - portraits (cadres, fonds, décorations), livres de maître artisan ;
  - objets de collection, archives occultes.
- Chaque entrée est obtenue ou non selon Dalamud (IUnlockState). Les filtres et la recherche sont ceux des autres onglets.
- Lecture (Collections/ExtraCollectionReader) : à part du lecteur des collections et de son cache.
  - Sur le thread du jeu, 2 ms par image au plus, pendant que la page est ouverte ; relu toutes les 30 secondes.
- Familiers Beastmaster non faits : leurs colonnes n'ont pas de nom dans les données (noté).
- Tests : ExtraCollections365Tests (1 cas). Verify.ps1 (agent) : 692/692, 0 avertissement, 0 erreur ; DLL 0.365.0.0. Non validé en jeu.
### English
- Queue step g, C-04: eight more Collections tabs.
  - Hairstyles, fashion accessories, glasses, bardings, portraits, master books, collectibles, Occult records.
  - Read with Dalamud IUnlockState apart from the cached collections.

## 0.364.0
- Étape g), domaine 2 (Collections), C-12 : carnet de chasse.
- Progression › Carnet de chasse : les carnets des 9 classes et des 3 grandes compagnies (feuille MonsterNote).
  - Choix du carnet et du rang (le rang en cours par défaut), filtre « Non terminés ».
  - Pour chaque entrée : ses monstres, les victimes faites sur le nombre demandé, et les zones.
- Lecture (Hunting/HuntingLogReader) : MonsterNoteManager, en mémoire, sur le thread du jeu, seulement pendant que la page est ouverte.
  - Le jeu ne garde que le rang en cours : les rangs d'avant comptent comme terminés, ceux d'après à zéro.
- Ordre des carnets en mémoire : faits lus dans Hunty (MIT, lecture seule), à confirmer en jeu. Sonde : docs/evidence/0.364-hunting-log.md.
- Pas de coordonnées ni de drapeau : proposition notée.
- Tests : HuntingLog364Tests (1 cas). Verify.ps1 (agent) : 691/691, 0 avertissement, 0 erreur ; DLL 0.364.0.0. Non validé en jeu.
### English
- Queue step g, C-12: a Hunting log page for the 9 class logs and 3 Grand Company logs, by rank.
  - Each entry lists its monsters, the kills read in game and the zones.
  - The log order in memory comes from facts read in Hunty (MIT) and is to be confirmed in game.

## 0.363.0
- Étape g), domaine 2 (Collections), C-14 : page Mandats.
- Progression › Mandats : chaque mandat du jeu (feuille Leve) avec son niveau, son type, son mandateur, sa zone et ses gils.
  - L'état est lu dans le jeu : terminé, accepté, pas terminé, ou « — » s'il n'est pas lu.
  - Filtres : recherche, type, mandateur, niveau min et max, état (« Non terminés » par défaut).
  - En tête : les allocations restantes et le nombre de mandats acceptés.
  - « Mandats d'artisanat acceptés » : l'objet que demande chaque mandat accepté, la quantité et les copies possédées.
- Lecture (Leves/LeveProgressReader) : sur le thread du jeu, seulement pendant que la page est ouverte.
  - Terminé : IUnlockState.IsLeveCompleted de Dalamud, 2 ms par image au plus.
  - Acceptés : QuestManager.LeveQuests, lu en mémoire.
  - Un échec laisse l'état « indisponible ».
- Aucune automatisation des mandats.
- Preuve : docs/evidence/0.363-leves.md.
- Tests : Leves363Tests (1 cas). Verify.ps1 (agent) : 690/690, 0 avertissement, 0 erreur ; DLL 0.363.0.0. Non validé en jeu.
### English
- Queue step g, C-14: a Guildleves page.
  - Every leve with its level, type, client, zone and gil, completed or accepted as read in game.
  - Filters: level, type, client and state; allowances left.
  - Accepted crafting leves list the item they ask and the copies held.

## 0.362.0
- Étape g), domaine 2 (Collections), C-05 : tenues du coffre à apparence.
- Collections, nouvel onglet « Tenues » : chaque tenue du jeu (feuille MirageStoreSetItem) avec :
  - ses pièces possédées sur le total, et son état : incomplète, toutes les pièces possédées, prête à ranger, rangée ;
  - « prête à ranger » = toutes les pièces dans le coffre : le jeu peut alors en faire une tenue ;
  - un clic ouvre ses pièces (dans le coffre, possédée ailleurs, manquante), avec leurs sources au clic droit.
- Filtres : toutes, incomplètes, toutes les pièces, rangées ; recherche par nom.
- « Possédée » = sacs en direct et rangements enregistrés du personnage connecté. Une tenue rangée compte toutes ses pièces.
- Sauvegarde avant ce travail : checkpoints/backup-20261103-013747-avant-C05-0362.zip.
- Tests : Outfits362Tests (1 cas). Verify.ps1 (agent) : 689/689, 0 avertissement, 0 erreur ; DLL 0.362.0.0. Non validé en jeu.
### English
- Queue step g, C-05: an Outfits tab in Collections.
  - Each glamour dresser outfit shows its pieces owned and its state: incomplete, every piece owned, ready to store, stored.
  - Each piece shows where it is, with its sources on right click.

## 0.361.0
- Étape g), domaine 2 (Collections), C-11 : carnet d'exploration complet.
- Carte › listes, nouvel onglet « Carnet d'exploration » : tous les panoramas, classés par zone (zones à gauche, panoramas à droite) :
  - numéro du carnet, nom, découvert ou non (lu dans le jeu) ;
  - « possible maintenant » : fenêtre d'heure d'Éorzéa ouverte et météo demandée (wiki) égale à la météo actuelle de la zone ;
  - météo inconnue : « heure OK, météo inconnue », jamais « impossible ».
- Filtres « Non découverts » et « Possibles maintenant ».
- Un clic pose le drapeau ; l'infobulle (prochaine fenêtre) et le clic droit (alarme) sont ceux de la carte.
- Lignes recalculées quand une source change, et une fois par minute réelle.
- Tests : VistaLog361Tests (1 cas). Verify.ps1 (agent) : 688/688, 0 avertissement, 0 erreur ; DLL 0.361.0.0. Non validé en jeu.
### English
- Queue step g, C-11: the whole sightseeing log, grouped by zone, as a tab of the map lists.
  - Each vista shows whether it was discovered and whether it is possible now (Eorzea time window and current weather).
  - Filters: not discovered, possible now. A click places the flag.

## 0.360.0
- Étape g), domaine 2 (Collections), C-09 : liste de souhaits.
- Le menu du clic droit de chaque objet dans Tensho a « Ajouter à mes souhaits » (ou « Retirer »).
- Collections, nouvel onglet « Souhaits » : chaque objet avec son icône et son état :
  - obtenu ou pas encore, pour une monture, une mascotte, un rouleau ou une carte ;
  - sinon les copies possédées (sacs et rangements enregistrés) ;
  - ses sources au clic droit, et un bouton « Retirer ».
- 500 objets au plus. La liste est gardée par « Réinitialiser les réglages » (liste « Kept »).
- Tests : Wishlist360Tests (1 cas). Verify.ps1 (agent) : 687/687, 0 avertissement, 0 erreur ; DLL 0.360.0.0. Non validé en jeu.
### English
- Queue step g, C-09: a wishlist.
  - « Add to my wishlist » in every item's right-click menu.
  - A Wishlist tab in Collections shows each item's collection state or held copies, with its sources on right click.
  - 500 items at most; kept on settings reset.

## 0.359.0
- Étape g), domaine 2 (Collections), C-08 : filtre des collections par source.
- Collections › Montures, Mascottes, Orchestrion : nouveau filtre « Source ». Choix possibles :
  - succès, contenus, coffres et autres objets, échanges (monnaies), marchands (gils) ;
  - quêtes, artisanat, récolte ;
  - vendable au marché, ou aucune source connue.
- Le filtre se combine avec l'état (obtenu, non confirmé) et la recherche.
- Les sources sont celles du menu du clic droit, réduites à un masque par entrée :
  - calculées au premier usage du filtre ;
  - gardées jusqu'au changement d'un catalogue ;
  - sans aucune requête au wiki ou à Garland : seulement ce qui est déjà en cache.
  - Avant ce correctif, la première version du filtre aurait demandé une page par monture.
- « Aucune source connue » ne veut pas dire « impossible à obtenir » (infobulle).
- Tests : CollectionSources359Tests (1 cas). Verify.ps1 (agent) : 686/686, 0 avertissement, 0 erreur ; DLL 0.359.0.0. Non validé en jeu.
### English
- Queue step g, C-08: a Source filter on mounts, minions and orchestrion rolls.
  - Sources: achievement, duty, coffers, exchange, vendor, quest, craft, gathering, market, or none known.
  - They are the right-click menu's sources, cached per entry, with no wiki or Garland request.

## 0.358.0
- Étape g), domaine 2 (Collections), C-07 : apparences identiques dans le coffre à apparence.
- Rangements › Rapport, nouveau bloc « Coffre à apparence : apparences identiques » :
  - groupe les objets du coffre qui ont les mêmes modèles (Item.ModelMain et ModelSub) au même emplacement ;
  - un seul suffit souvent pour le mirage ; le nombre de canaux de teinture de chacun est indiqué ;
  - les pièces d'une tenue rangée ne comptent pas ;
  - les recolorations (autre variante d'un même modèle) ne sont pas groupées.
- Les teintures sont copiées avec les groupes : aucune lecture des données du jeu dans le dessin.
- Tests : SameLook358Tests (1 cas). Verify.ps1 (agent) : 685/685, 0 avertissement, 0 erreur ; DLL 0.358.0.0. Non validé en jeu.
### English
- Queue step g, C-07: the storage report groups glamour dresser items that look the same (same models, same slot).
  - The dye channels of each item are shown.
  - Recolours are not grouped.

## 0.357.0
- Étape g), domaine 2 (Collections), C-03 : objets qui peuvent aller dans l'armoire.
- Rangements › Rapport, nouveau bloc « Peut aller dans l'armoire » : les objets que l'armoire accepte (feuille Cabinet) rangés ailleurs :
  - lieux couverts : sacs, sacoches, servants, coffre à apparence, et l'arsenal quand les sets d'équipement sont lus ;
  - les copies déjà dans l'armoire sont listées d'abord, marquées « en trop » ;
  - armoire pas encore ouverte : l'état reste inconnu, jamais « absent » ;
  - les pièces d'un set d'équipement et les pièces d'une tenue rangée ne sont pas proposées.
- Rapport seulement : rien n'est déplacé.
- Preuve : docs/evidence/0.357-armoire.md (coffre à apparence de 800 cases).
- Tests : Armoire357Tests (1 cas). Verify.ps1 (agent) : 684/684, 0 avertissement, 0 erreur ; DLL 0.357.0.0. Non validé en jeu.
### English
- Queue step g, C-03: the storage report lists items the armoire takes that are kept elsewhere.
  - Spare copies already in the armoire come first.
  - Gear set pieces and outfit pieces are left out.
  - Nothing is moved.

## 0.356.0
- Étape g), domaine 2 (Collections), C-02 : missions aléatoires du jour.
- Monde › Rappels, nouvelle section « Missions aléatoires du jour », lue dans le jeu avec les compteurs du Calendrier :
  - chaque mission aléatoire quotidienne : faite aujourd'hui, à faire, ou « — » si l'état n'est pas lu ;
  - le rôle qui a le bonus (tank, soigneur, DPS), tel que l'outil de recherche de mission le garde ;
  - le compteur « X faites sur Y » et l'heure de la remise à zéro quotidienne.
- Liste des missions : feuille ContentRoulette, celles de l'outil de recherche de mission qui ont un index de complétion. Les courses
  de chocobos et Crystalline Conflict n'en ont pas (sonde : docs/evidence/0.356-roulettes.md).
- Lecture :
  - « faite » : InstanceContent.IsRouletteIncomplete, appelée seulement si son adresse est résolue ;
  - bonus : AgentContentsFinder.ContentRouletteRoleBonuses (11 entrées), indexé par la ligne de bonus de la mission ;
  - les deux sont protégés à part : un échec laisse l'état inconnu, le reste des Rappels continue.
- L'alerte de bonus de rôle n'est pas faite : elle touche les alarmes et Discord (proposition notée pour vous).
- Sondes : `tools/ShopCurrencyProbe` mode PROBE_ROULETTE ; `tools/ApiProbe` montre maintenant aussi les champs internes et leur décalage.
- Tests : Roulettes356Tests (1 cas). Verify.ps1 (agent) : 683/683, 0 avertissement, 0 erreur ; DLL 0.356.0.0. Non validé en jeu.
### English
- Queue step g, C-02: daily duty roulettes under Reminders.
  - Each roulette shows whether it was done today and which role gets the bonus, with a count and the reset time.
  - Native calls are made only once their address is resolved, and each read is guarded on its own.
  - No role-bonus alert yet: it touches alarms and Discord.

## 0.355.0
- Étape g), domaine 2 (Collections), C-01 : état débloqué / terminé de chaque contenu.
- Monde › Butin des contenus :
  - nouvelle colonne « État » : Terminé, Débloqué, Verrouillé, ou « — » quand l'état n'est pas connu ;
  - filtre « Avancement » : tous, non terminés, débloqués non terminés, terminés ;
  - compteur « X terminés sur Y lus » pour la catégorie choisie ;
  - l'état aussi en tête de la fiche d'un contenu.
- Lecture (Duties/DutyProgressReader) :
  - sur le thread du jeu, 2 ms par image au plus, toutes les 5 secondes ;
  - seulement connecté et pendant que la page est ouverte ;
  - terminé : UIState.IsInstanceContentCompleted, appelé seulement si son adresse est résolue ;
  - débloqué : IUnlockState.IsInstanceContentUnlocked de Dalamud ;
  - un échec laisse l'état « indisponible », jamais « verrouillé » ;
  - la déconnexion efface l'état.
- Un contenu dont l'état est inconnu reste dans les listes « non terminés » : une absence de preuve n'est pas une preuve.
- Tests : DutyProgress355Tests (1 cas). Verify.ps1 (agent) : 682/682, 0 avertissement, 0 erreur ; DLL 0.355.0.0. Non validé en jeu.
### English
- Queue step g, C-01: unlocked / completed status of each duty on the Duty loot page.
  - A Status column, a Progress filter (not completed, unlocked not completed, completed) and a completed count.
  - Read on the framework thread while the page is open; the native call is made only once its address is resolved.
  - Unknown statuses are never shown as locked.

## 0.354.0
- Étape g), domaine 1, I-13 : valeur des monnaies (« que faire de mes sceaux ou de mes marques »).
- Rangements › Monnaies, bouton « Que faire ? » sur chaque monnaie : ce qu'elle achète chez les marchands connus, avec :
  - le prix dans cette monnaie ;
  - le prix minimum au marché de votre monde (Universalis, récent) ;
  - les gils obtenus par unité de monnaie en revendant (prix × quantité ÷ coût) ;
  - le classement du plus rentable au moins rentable, les prix inconnus à la fin et jamais comptés zéro ;
  - les montures et mascottes marquées « possédée » ou « manquante ».
- « Prix du marché » récupère les prix qui manquent, 32 au plus par clic, en respectant l'interrupteur Universalis.
- Les monnaies codées (mémoquartz, assignats) restent non résolues, comme avant (noté).
- Domaine 1 (inventaire et marché) terminé, sauf I-05 et I-15, gardés pour une séance en ultracode avec l'utilisateur.
- Tests : CurrencyValue354Tests (1 cas). Verify.ps1 (agent) : 681/681, 0 avertissement, 0 erreur ; DLL 0.354.0.0. Non validé en jeu.
### English
- Queue step g, I-13: « What to do? » per currency — what it buys, its price in that currency, the market price and the gil per unit
  of currency (best first, unknown last), mounts and minions marked owned or missing; market prices fetched on a click.

## 0.353.0
- Étape g), domaine 1, I-12 : recherche d'objets avec filtres, et objets clés.
- Page Objets, « Filtres » sous la recherche, combinables avec le nom ou seuls (sans rien écrire) : catégorie de l'objet, niveau
  d'équipement de … à …, niveau d'objet de … à …, emplacement d'équipement, échangeable seulement, vendable au marché seulement.
  - Chaque filtre a son infobulle, et « Effacer les filtres » les remet à zéro.
- « Objets clés » : recherche par nom dans les objets de quête (feuille EventItem), avec leur icône ; ils n'ont pas de fiche.
- Pas encore de filtre par job : le catalogue ne garde pas cette donnée (noté pour la fin).
- Correctif de 0.349 : « /tensho wiki | lodestone » demandait 200 résultats à la recherche, qui en accepte 100 au plus et lève une
  exception au-delà. La commande échouait donc toujours ; elle cherche maintenant dans les 100 premiers.
- Tests : ItemFilters353Tests (1 cas). Verify.ps1 (agent) : 680/680, 0 avertissement, 0 erreur ; DLL 0.353.0.0. Non validé en jeu.
### English
- Queue step g, I-12: item search filters (category, equip and item level ranges, slot, tradable, sold on the market), alone or with a
  name; key item search by name. Fix: the 0.349 wiki/Lodestone command asked the search for 200 results and always failed.

## 0.352.0
- Étape g), domaine 1, I-11 : conditions de déblocage des articles de boutique.
- Les articles gardent la quête et le haut fait qui les débloquent (données du jeu : GilShopItem.QuestRequired et
  AchievementRequired, SpecialShop Quest et AchievementUnlock).
  - L'annuaire des marchands est reconstruit une fois (nouvelle version de son cache).
- Partout où un prix d'article est affiché (marchands, fiche d'objet, survol, boutiques d'événement), la condition suit le prix :
  « Après la quête « … » (fait / à faire / non lu) », « Après le haut fait « … » ».
  - Un haut fait non confirmé reste « non lu », jamais « à faire ».
- Les rangs de tribu ne sont pas encore lus (noté pour la fin).
- Tests : ShopRequirements352Tests (1 cas). Verify.ps1 (agent) : 679/679, 0 avertissement, 0 erreur ; DLL 0.352.0.0. Non validé en jeu.
### English
- Queue step g, I-11: the quest and achievement that unlock a shop article are read from the game data and shown after its price,
  with done / to do from the quest journal and the achievements read (vendor cache rebuilt once).

## 0.351.0
- Étape g), domaine 1, I-04 : journal des gils. Choix de l'utilisateur : gardé 90 jours.
- Lecture des gils une fois par seconde sur le thread du jeu, une fois connecté :
  - les vôtres (GetGil) ;
  - ceux de vos servants, en somme, une fois que le jeu les a donnés (cloche des servants) ;
  - ceux du coffre de compagnie, pendant qu'il est chargé.
  - Chaque fonction n'est appelée que si le jeu l'a trouvée.
- Chaque variation est notée avec sa catégorie, déduite de la fenêtre du jeu vue dans les 15 dernières secondes : boutique PNJ,
  marché, réparation, téléportation, servants, compagnie, sinon « autre ». La première lecture d'un personnage n'est qu'un point de
  départ.
- Un fichier par personnage (GilJournal/<identifiant local>.json) : 90 jours et 20 000 lignes au plus, écrit au plus une fois par
  minute et à la fermeture, via un fichier temporaire. Un fichier illisible n'est jamais réécrit.
- Nouvel onglet Rangements › Gils : les trois soldes, gains et dépenses par catégorie sur 1, 7, 30 ou 90 jours, et les 50 dernières
  variations.
- SECURITY à jour (nouveau fichier local).
- Tests : GilJournal351Tests (2 cas). Verify.ps1 (agent) : 678/678, 0 avertissement, 0 erreur ; DLL 0.351.0.0. Non validé en jeu.
### English
- Queue step g, I-04: gil journal (character, retainers once known, company chest while open), each change categorized from the game
  window seen just before (shop, market, repair, teleport…), kept 90 days in one file per character; new Storage › Gil tab.

## 0.350.0
- Étape g), domaine 1, I-09 : le coffre de compagnie libre dans les rangements.
- Nouveau type de rangement (ajouté en dernier) : les cinq pages du coffre, ses gils et ses cristaux (InventoryType 20000 à 20004,
  22000, 22001), enregistré pour le personnage qui l'ouvre.
- Le jeu n'envoie le coffre qu'à l'ouverture, page par page. Tensho ne le lit que pendant que sa fenêtre est ouverte, et n'enregistre
  que lorsque toutes les pages sont reçues (le panneau le dit).
- Enregistrement par le bouton du panneau Tensho à côté de la fenêtre du coffre, ou d'office avec « Enregistrer automatiquement les
  servants, sacoches et le coffre de compagnie » (texte du réglage mis à jour).
- La banque a un onglet « Coffre de compagnie », et les stocks d'un objet comptent le coffre (une ligne seulement une fois ouvert).
  L'aperçu de tri ne le propose pas.
- Tests : CompanyChest350Tests (1 cas). Verify.ps1 (agent) : 676/676, 0 avertissement, 0 erreur ; DLL 0.350.0.0. Non validé en jeu.
### English
- Queue step g, I-09: the free company chest (five pages, gil, crystals) as a storage — recorded from its open window once every page
  has been received (button or automatic), shown in the Bank and counted in item stocks.

## 0.349.0
- Étape g), domaine 1, X-07 : liens vers le wiki et le Lodestone.
- Clic droit sur un objet › Tensho, et fiche d'objet : « Wiki (anglais) » et « Lodestone ».
  - Le wiki (FFXIV Console Games Wiki) est ouvert sur le nom anglais de l'objet.
  - Le Lodestone est ouvert sur la recherche de sa base de données, dans la langue de Tensho (fr, de, jp, sinon na).
- Commandes « /tensho wiki <objet> » et « /tensho lodestone <objet> » : le nom exact d'abord, sinon le plus court qui contient le texte ;
  un nom inconnu est dit dans le chat.
  - Pas de /wiki ni de /lodestone : un autre plugin peut déjà les tenir. Choix noté pour la fin.
- Les pages s'ouvrent dans votre navigateur sur votre clic ou votre commande ; Tensho n'envoie rien.
  - Adresses du Lodestone déclarées avec le crédit du jeu.
  - Description du plugin et SECURITY à jour (aussi pour les alertes Discord des monnaies de 0.344 et les fichiers par personnage de
    0.345).
- Tests : ExternalLinks349Tests (1 cas) ; le test du manifeste connaît le Lodestone. Verify.ps1 (agent) : 675/675, 0 avertissement, 0 erreur ;
  DLL 0.349.0.0. Non validé en jeu.
### English
- Queue step g, X-07: an item's page on the wiki (English name) or the Lodestone (Tensho's language) from the right-click menu, the
  item sheet or « /tensho wiki|lodestone <item> », opened in the browser on the player's click only.

## 0.348.0
- Étape g), domaine 1, I-10 : rapport de rangement. Choix de l'utilisateur : rapport seul, aucun déplacement.
- Nouvel onglet Rangements › Rapport :
  - « Piles à regrouper » : pour vos sacs (en direct) et vos sacoches et servants enregistrés, les objets d'une même qualité répartis
    en plus de piles qu'il n'en faut, avec les cases libérables (taille de pile tirée des données du jeu ; une taille de 1 ou inconnue
    n'est jamais proposée) ;
  - « Arsenal : pièces hors des sets d'équipement » : les pièces de l'arsenal enregistré qu'aucun set n'utilise (cristaux d'âme exclus),
    avec un rappel qu'une pièce peut servir de mirage.
- Les sets d'équipement sont lus dans le jeu (RaptureGearsetModule) sur le thread du jeu, seulement quand l'onglet est ouvert. Un set
  est reconnu par son drapeau « existe » : lecture de mémoire seulement, aucune fonction du jeu n'est appelée.
- Tests : StorageReport348Tests (2 cas). Verify.ps1 (agent) : 674/674, 0 avertissement, 0 erreur ; DLL 0.348.0.0. Non validé en jeu.
### English
- Queue step g, I-10: Storage › Report — stacks of one item that fit in fewer slots (bags live, saved saddlebags and retainers) and
  armoury pieces used by no gear set (gear sets read on request, memory only). Report only: nothing is moved.

## 0.347.0
- Étape g), domaine 1, I-08 : notes, étiquettes et liste « à vendre au PNJ » sur les objets. Choix de l'utilisateur : son optionnel,
  désactivé par défaut.
- Fiche d'objet, nouvel onglet « Note » :
  - une note libre (500 caractères, enregistrée en quittant le champ) ;
  - les étiquettes « Important » et « À vendre au PNJ » ;
  - deux réglages : rappel à l'ouverture d'une boutique (activé) et son pour les objets importants (désactivé), avec infobulles et ↺.
- La note et les étiquettes s'affichent en tête de l'information de l'objet (survol et fiche).
- À l'ouverture d'une boutique de PNJ (fenêtre « Shop » du jeu), le chat liste une fois :
  - les objets « à vendre » de vos quatre sacs, avec leur quantité ;
  - les objets « importants », avec le son des alarmes s'il est activé.
  - Rien n'est vendu pour vous.
- Les notes sont des données gardées (« Restaurer tous les réglages » ne les efface pas), bornées à 2 000 objets, et remplacées en
  entier à chaque changement (lues sans verrou sur le thread du jeu).
- Idées et choix provisoires ajoutés à docs/project/G-PROPOSALS.md.
- Tests : ItemNotes347Tests (2 cas). Verify.ps1 (agent) : 672/672, 0 avertissement, 0 erreur ; DLL 0.347.0.0. Non validé en jeu.
### English
- Queue step g, I-08: item notes and labels (« important », « to sell to an NPC ») in a new item sheet tab, shown in the item
  information; a chat reminder when an NPC shop opens (optional alarm sound for important items, off by default).

## 0.346.0
- Étape g), domaine 1, deux petits écarts.
- I-06, prix de vente au PNJ :
  - l'information d'objet (survol et fiche) affiche « Vente au PNJ (NQ) : X gils l'unité » ;
  - au survol d'une case de vos sacs, elle ajoute la valeur de la pile ;
  - donnée du jeu (Item.PriceLow), rien pour un objet qui ne se vend pas au PNJ ;
  - pour NQ seulement, la feuille ne donne pas le prix HQ.
- I-07, aide à la sous-cotation (choix de l'utilisateur : copie dans le presse-papiers seulement) :
  - quand vous ouvrez les offres d'un objet au tableau des ventes, Tensho copie l'offre la plus basse moins 1 gil et le dit dans le chat ;
  - les offres de vos propres servants sont exclues ;
  - vous collez et confirmez vous-même : aucune mise en vente automatique ;
  - offres lues par l'événement Dalamud IMarketBoard.OfferingsReceived, rien n'est demandé au serveur.
  - Réglage Paramètres › Avancé, désactivé par défaut (il remplace le presse-papiers), avec infobulle et ↺.
- Choix provisoires ajoutés à docs/project/G-PROPOSALS.md.
- Tests : Market346Tests (2 cas). Verify.ps1 (agent) : 670/670, 0 avertissement, 0 erreur ; DLL 0.346.0.0. Non validé en jeu.
### English
- Queue step g, I-06: NPC sale price (unit and hovered stack) in the item information. I-07: when you open an item's market offers,
  the lowest offer minus 1 (your retainers left out) is copied to the clipboard and announced; you paste and confirm yourself (off by
  default).

## 0.345.0
- Étape g), domaine 1, I-03 : vue de tous ses personnages. Choix de l'utilisateur : un fichier par personnage, vue d'ensemble, bouton
  « Effacer ce personnage », jamais d'identifiant de compte.
- Stockage :
  - la banque passe en un fichier par personnage (InventoryBank/<identifiant local du personnage>.json), chacun écrit par la même
    classe éprouvée (bornes, copie .bak, refus d'écraser un fichier abîmé) ;
  - l'ancien fichier commun est découpé une fois, dans un dossier temporaire déplacé en place seulement une fois complet, puis gardé
    à côté en « .migrated » ;
  - un fichier qui contient un autre personnage est refusé ;
  - un personnage effacé voit son fichier supprimé.
- Relevés : les sacs, les cristaux et les monnaies du personnage connecté sont enregistrés d'office comme l'arsenal (toutes les
  2 minutes, sans message), sous le nom « Prénom Nom (Monde) ».
  - Ce sont trois nouveaux types de rangement, ajoutés en dernier pour que les fichiers existants gardent leurs numéros.
  - Pour le personnage connecté, ils ne s'affichent qu'en direct : pas de double compte, et les stocks des objets ne changent pas.
- Nouvel onglet Rangements › Personnages :
  - par personnage : nom, dernier relevé, gils, objets des sacs, cristaux, servants enregistrés ;
  - « Voir » ouvre la banque sur ce personnage ;
  - « Effacer » (deux clics) supprime tout ce qui le concerne.
- Banque : un choix « Personnage » montre les rangements enregistrés d'un autre personnage (sans l'onglet direct).
- Aperçu de tri : il ne propose plus que les servants et les sacoches (liste de ce qui se trie, et non plus de ce qui ne se trie pas).
- Propositions et choix provisoires de l'étape g) réunis dans docs/project/G-PROPOSALS.md (demande de l'utilisateur : les revoir
  ensemble).
- Tests : Characters345Tests (3 cas) ; un doublon d'inclusion de 0.344 retiré du projet de tests. Verify.ps1 (agent) : 668/668, 0 avertissement, 0 erreur ;
  DLL 0.345.0.0. Non validé en jeu.
### English
- Queue step g, I-03: the bank is now one file per character (the former shared file is split once and kept as .migrated); bags,
  crystals and currencies are recorded like the armoury; new Storage › Characters tab (gil, bag items, crystals, retainers, « See »,
  two-click « Erase »); the Bank can show another character's saved storages.

## 0.344.0
- Étape g), domaine 1, I-02 : soldes des monnaies et alertes de plafond. Choix de l'utilisateur : barre du serveur et alarme Tensho,
  Discord si R-12 est réglé. Fait en ultracode.
- Préparation : sonde des données et des fonctions du jeu (docs/evidence/0.344-currencies.md), puis un workflow de conception.
  - Il a étudié ce que font les autres plugins (CurrencyAlert MIT, Currency Watchdog sans licence, LootView, Altoholic : idées
    seulement, rien de copié) et vérifié l'intégration dans le code.
- Nouvel onglet Rangements › Monnaies :
  - solde, plafond, pourcentage et seuil d'alerte par monnaie ;
  - mémoquartz limités de la semaine ;
  - interrupteurs « Alertes de plafond » et « Dans la barre du serveur », avec infobulles et ↺.
- Lecture sur le thread du jeu toutes les 5 s, une fois connecté :
  - sceaux de la compagnie actuelle au plafond de son rang (GetCompanySeals / GetMaxCompanySeals) ;
  - les monnaies tenues par le CurrencyManager (GetItemCount / GetItemMaxCount) ;
  - les autres (GetInventoryItemCount, plafond = taille de pile) ;
  - chaque fonction n'est appelée que si le jeu l'a trouvée, et chaque lecture est gardée.
- Alerte : seuil par défaut à 80 % du plafond (0 = aucune alerte).
  - Une seule alerte quand le solde passe le seuil vers le haut ; elle revient après être repassé 2 points sous le seuil.
  - Un message dans le chat et le son des alarmes (silence en mission compris), un message Discord dans la nouvelle catégorie
    « Monnaies », et une entrée « Tensho Assistant · Monnaies » dans la barre du serveur ; un clic ouvre l'onglet.
- Relecture adverse (4 angles, 2 vérificateurs par remarque) : 6 remarques confirmées, 3 contestées, toutes corrigées.
  - À la connexion, le jeu répond 0 avant le vrai solde : cela faisait une fausse alerte (chat et Discord). Les soldes sont maintenant
    inconnus tant que le conteneur Monnaies n'est pas chargé, et un 0 n'arme plus l'alerte.
  - Seuil enregistré en quittant le champ, et non à chaque frappe.
  - Plafond inconnu affiché « inconnu » (et non « pas de plafond »).
  - L'onglet n'affiche plus l'ancien personnage.
  - Seuils de 1 % et 2 % réarmés.
  - Changer un seuil ne réarme plus que cette monnaie.
  - Une erreur qui se répète n'est écrite qu'une fois dans le journal.
  - « Pas de mémoquartz limité en ce moment » au lieu de « inconnu ».
- Une seconde passe sur ces correctifs a trouvé 3 cas de plus, corrigés :
  - un vrai solde à 0 (deux lectures à 0 de suite) arme l'alerte, sans réveiller le faux 0 de la connexion ;
  - un seuil tapé dans un champ qui a disparu n'est plus affiché comme enregistré ;
  - le pourcentage est arrondi vers le bas, en accord avec l'alerte.
- Choix provisoires notés pour la fin (ROADMAP) : 80 % partout, rien sans compagnie, rien au chargement, liste des monnaies suivies.
- Tests : Currencies344Tests (4 cas). Verify.ps1 (agent) : 665/665, 0 avertissement, 0 erreur ; DLL 0.344.0.0. Non validé en jeu.
### English
- Queue step g, I-02: Storage › Currencies (balances, caps, per-currency threshold, weekly limited tomestones) with cap alerts — once
  per upward crossing (80 % by default), chat and alarm sound, Discord « Currencies » category, server bar entry. Reads on the
  framework thread, pointer-checked; balances unknown until the Currency container is loaded (no false alert at login).

## 0.343.0
- Étape g) de la file, domaine 1 (inventaire et marché), I-01 : infobulle de prix complète (choix de l'utilisateur : prix au survol
  seulement, pas de préchargement, touche d'actualisation).
- La réponse Universalis déjà demandée donnait plus que ce qui était affiché ; Tensho lit maintenant :
  - le prix moyen de vente et les ventes par jour, sur votre monde ;
  - l'offre la moins chère de la région, avec son monde.
  - Le cache disque passe en v3 : les anciennes copies, qui n'ont pas ces champs, sont redemandées.
- Infobulle du survol :
  - sous chaque prix minimum, « Dernière vente (date) · moyenne · ventes par jour », avec « — » quand c'est inconnu ;
  - « Pile de N : ~X », pour une case de vos sacs (quantité lue dans le jeu, case vérifiée : elle doit contenir l'objet survolé) ;
  - « Le moins cher de la région », seulement quand c'est moins cher que le centre de données.
- Ctrl+Alt pendant le survol redemande le prix, même récent. C'est une action du joueur : une demande par appui, 5 s au moins entre deux.
  - Réglage dans Paramètres › Avancé, avec infobulle et ↺, activé par défaut.
  - Choix de la combinaison noté pour la fin (ROADMAP).
- Tests : HoverPrices343Tests (2 cas) ; deux anciens tests suivent l'étiquette v3 et la nouvelle ligne du lecteur de survol.
  Verify.ps1 (agent) : 661/661, 0 avertissement, 0 erreur ; DLL 0.343.0.0. Non validé en jeu.
### English
- Queue step g, inventory and market, I-01: the hover price now shows last sale, average sale price, sales per day, the region's
  cheapest listing and the hovered stack's value; Ctrl+Alt while hovering refreshes the price (one request per press, setting).

## 0.342.0
- Étape f) de la file, point 7 (DEC-023, R-09) : événements saisonniers. Demandes de l'utilisateur pendant la version : les boutiques
  d'événement, les bons noms, et vérifier si un plugin fait déjà pareil.
- Sondes (docs/evidence/0.342-festivals.md) :
  - le jeu donne les événements en cours (GameMain.ActiveFestivals) et leurs quêtes (Quest.Festival), mais ni nom ni date ;
  - les calques d'événement des décors (planevent.lgb, FestivalID) donnent les PNJ de chaque fête, et l'annuaire des marchands leurs
    boutiques (ex. « vendeuse de la Veillée des saints », boutique 263293).
- Noms :
  - LuminaSupplemental et Brio (GPL-3.0) ont les mêmes noms anglais, et aucun pour 256, 257, 261 et 263 ;
  - en français, le nom vient du PNJ guide de la fête (dialogue interne « Fes…Guide ») : « démarcheur de la Veillée des saints » donne
    « Veillée des saints » ;
  - sinon le nom anglais, sinon « nom inconnu ». La page dit toujours d'où vient le nom.
  - Les drapeaux techniques (« Special Event Flag », ou ni quête ni PNJ) sont masqués ; leur nombre est affiché en mode développeur.
- Nouvelle page Progression › « Événements » :
  - les événements en cours selon le jeu ;
  - leurs quêtes, avec ✓ / en cours / à faire, gils et objets en récompense, et « Ouvrir » vers la page Quêtes ;
  - « Boutiques de l'événement » : chaque vendeur de la fête, ses villes, et ce qu'il vend avec le prix.
  - Pas de dates à venir : aucune source fiable.
- Lecture sur le thread du jeu toutes les 10 s, une fois connecté et seulement pendant que la page est ouverte.
  - Les décors sont lus une fois (environ 1 100 fichiers, environ 1 s), sur un worker, à la première ouverture.
  - Chaque fichier est vérifié avant d'être lu, comme pour l'annuaire des marchands.
- Les noms anglais (LuminaSupplemental, GPL-3.0) sont embarqués avec leur avis ; crédit mis à jour.
- Tests : Events342Tests (2 cas). Verify.ps1 (agent) : 659/659, 0 avertissement, 0 erreur ; DLL 0.342.0.0. Non validé en jeu.
### English
- Queue step f, point 7 (R-09): new Events page — running festivals from the game, their quests with progress and rewards, and the
  event shops (festival NPCs from the planevent layouts, matched in the vendor directory). French names come from each festival's guide
  NPC, else English names from LuminaSupplemental; technical flags are hidden. No upcoming dates (no reliable source).

## 0.341.0
- Demande de l'utilisateur (capture d'écran) : un onglet propre au chocobo, et retirer l'outil Personnage, qui ne sert plus puisque
  Équipement fait la même chose.
- Nouvelle page « Chocobo » (Progression), qui reprend la barde de 0.340 jusque-là en bas d'Équipement.
  - Le tableau prend toute la hauteur.
  - Hors connexion, la page le dit (au lieu de « Lecture du chocobo… »).
- Page Personnage retirée de la navigation et de l'accueil. La tuile d'accueil mène à Équipement ; l'annuaire des outils
  (« Character Panel Refined ») aussi.
  - Gardé : le panneau à côté de la fenêtre Personnage du jeu (réglage existant, validé en jeu), qu'Équipement n'a pas.
  - Décision de l'utilisateur (2026-11-02) : le panneau est gardé.
  - Une configuration qui rouvrait la page Personnage s'ouvre sur l'accueil.
- Corrections vues sur la capture :
  - « ✗ » s'affichait « = » (caractère absent de la police) : ✓ et ✗ viennent maintenant de la police d'icônes ;
  - la croix « possédé » passait sous le nom de l'objet : elle reste sur sa ligne.
- Tests : Chocobo341Tests (1 cas). Verify.ps1 (agent) : 657/657, 0 avertissement, 0 erreur ; DLL 0.341.0.0. Non validé en jeu.
### English
- User request: chocobo barding moved to its own « Chocobo » page; the Character page is removed (the Gear page shows the same real
  values; the panel beside the game's Character window stays). Unlocked mark now from the icon font (« ✗ » showed as « = »), owned
  mark kept on the item's line.

## 0.340.0
- Étape f) de la file, point 5 (DEC-023 point 5, R-11 étape 4) : barde de chocobo.
- Sonde (docs/evidence/0.340-chocobo-barding.md) :
  - BuddyEquip n'a que l'apparence (noms, modèles, icônes), aucune caractéristique ;
  - les 101 objets qui apprennent une barde ont l'action 1013, et Data[0] est la ligne de la barde ;
  - le jeu donne la barde portée (CompanionInfo) et Dalamud l'état de déblocage (IUnlockState).
- Page Équipement, nouvelle section « Barde de chocobo » :
  - nom, rang et couleur du chocobo ;
  - barde portée sur la tête, le corps et les jambes (ordre supposé, dit dans l'infobulle) ;
  - « n bardes débloquées sur m » ;
  - liste avec icônes, Grande Compagnie, ✓ / ✗ (« ? » tant que ce n'est pas lu) et l'objet qui l'apprend, avec la marque de possession ;
  - recherche et « Seulement celles qui manquent ».
  - Lu sur le thread du jeu toutes les 2 s, une fois connecté et seulement pendant que la section est ouverte. Lecture gardée.
- Escouade (même étape) : apparence seulement, rien à planifier. Le défaut recommandé (la retirer) est appliqué ; la décision reste
  dans la liste de la fin (ROADMAP).
- Tests : Barding340Tests (2 cas). Verify.ps1 (agent) : 656/656, 0 avertissement, 0 erreur ; DLL 0.340.0.0. Non validé en jeu.
### English
- Queue step f, point 5 (R-11 step 4): chocobo barding section on the gear page (worn barding, unlocked bardings, the item that teaches
  each, read on the framework thread while shown). Squadron gear is appearance only and was left out (recommended default).

## 0.339.0
- Demande de l'utilisateur (capture d'écran) : les matérias XI et XII, celles qu'on pose aujourd'hui, affichaient « taux inconnu ».
- Cause : les feuilles du jeu MateriaJoinRate et MateriaJoinRateGatherCraft s'arrêtent au grade X. La copie de xivapi n'a aussi que
  les lignes 0 à 9.
- Correction : pour XI et XII, Tensho prend les taux de FFXIV Teamcraft (MIT) et l'affiche « selon Teamcraft ».
  - Les taux I à X de Teamcraft sont identiques à ceux du jeu.
  - XI suit les grades impairs (12/6/3/2 % NQ, 17/10/7/5 % HQ) et XII les grades pairs (12 % NQ, 17 % HQ, puis 0).
  - Les grades I à X restent lus dans le jeu ; l'infobulle explique la différence.
  - Teamcraft est crédité pour ces fichiers. Preuve : docs/evidence/0.338-overmeld-rates.md.
- Tests : Overmeld338Tests mis à jour (XI et XII). Verify.ps1 (agent) : 654/654, 0 avertissement, 0 erreur ; DLL 0.339.0.0. Non validé en jeu.
### English
- Overmeld rates of grades XI and XII (the game sheets stop at X) now come from FFXIV Teamcraft, whose rates I to X match the game,
  and are labelled « per Teamcraft » instead of « rate unknown ».

## 0.338.0
- Étape f) de la file, point 4 (DEC-023, point 8) : taux de réussite de la surmatérialisation.
- Sonde (docs/evidence/0.338-overmeld-rates.md) : les feuilles du jeu MateriaJoinRate (combat) et MateriaJoinRateGatherCraft
  (artisanat et récolte) donnent, par grade (lignes 0 à 9 = I à X), le taux NQ et HQ des quatre emplacements de surmatérialisation.
  - Exemple, grade I : 80/40/20/10 % (NQ), 90/48/28/16 % (HQ).
  - Pas de ligne pour XI et XII : taux affiché « inconnu ».
- Équipement :
  - l'éditeur de matérias affiche le taux de chaque grade dans un emplacement avancé, puis celui de la matéria choisie, avec une infobulle ;
  - l'infobulle des emplacements donne le taux des matérias déjà posées en surmatérialisation.
- Règle corrigée : les grades VI, VIII, X et XII vont dans le premier emplacement de surmatérialisation seulement, et non « jamais »
  (règle de 0.312, sans preuve).
  - Preuve : 12 % / 17 % au premier emplacement et 0 % ensuite dans la feuille, et le wiki pour XII.
  - Ces grades sont maintenant proposés dans le premier emplacement avancé.
- Tests : Overmeld338Tests (2 cas) ; le test de 0.312 suit la nouvelle règle. Verify.ps1 (agent) : 654/654, 0 avertissement, 0 erreur ; DLL 0.338.0.0.
  Non validé en jeu.
### English
- Queue step f, point 4: overmeld success rates read from the game sheets (MateriaJoinRate, MateriaJoinRateGatherCraft) and shown per
  advanced socket and piece quality; grades VI, VIII, X and XII are now allowed in the first advanced socket only (was « never »).

## 0.337.0
- Étape f) de la file, point 3 (DEC-023), seconde partie : planificateur de trajets de dirigeable par distance et durée. Accord de
  l'utilisateur pour appeler les fonctions de calcul du jeu (ultracode).
- Calcul des étapes (FreeCompany/AirshipVoyageTable.Dalamud.cs) :
  - le jeu calcule lui-même chaque étape (HousingManager.GetAirshipVoyageTimeAndDistance) et chaque relevé (GetAirshipSurveyDuration),
    sans rien envoyer au serveur ;
  - appel seulement sur le thread du jeu, une fois connecté, après vérification que les deux fonctions sont trouvées, une table par vitesse ;
  - un échec rend le calcul « indisponible » pour la session ; une étape ou un relevé à 0 reste inconnu.
- Assemblage d'un voyage : étapes depuis le départ, relevés et leur distance, 12 h fixes, sans retour, en secondes.
  - Non confirmé en jeu, et la page le dit.
  - Le jeu ne garde pas le trajet d'un dirigeable, donc aucune vérification automatique n'est possible.
  - Valeurs brutes affichées en mode développeur.
- Page Compagnie › Secteurs de dirigeable :
  - dirigeable lu (rang, vitesse, autonomie) ou saisie manuelle ;
  - colonne « Choisir » : jusqu'à 5 secteurs, mesurés dans l'ordre le plus court (distance, durée, expérience, céruleum) ;
  - « Meilleurs trajets » : expérience par jour, expérience par voyage ou plus court, cherchés sur un worker.
  - Butin non chiffré. Les secteurs sans expérience dans les données du jeu (0) donnent « ≥ » et ne sont classés que par « Plus court ».
- Relecture adverse de 0.335 à 0.337 (4 angles, 2 vérificateurs par remarque) : 8 remarques confirmées et 1 contestée, toutes corrigées ; une seconde passe sur les correctifs a trouvé 1 cas de plus (liste de prix finie entre deux images), corrigé.
  - Prix des trajets de sous-marin gardés à part, pour que le cache partagé de 128 prix ne les fasse pas revenir « manquants ».
  - Un objet sans offre NQ n'est plus redemandé en boucle.
  - Le compte des prix manquants suit le palier de surveillance.
  - Expérience inconnue jamais comptée 0.
  - Infobulles manquantes ajoutées (y compris sur une case grisée).
  - Hauteur minimale du tableau des secteurs.
  - Recherche annulée à la fermeture.
  - Deux protections en plus : demande filtrée avant comparaison, relevé à 0 tenu pour inconnu.
- Tests : Airships337Tests (5 cas). Verify.ps1 (agent) : 652/652, 0 avertissement, 0 erreur ; DLL 0.337.0.0. Non validé en jeu.
### English
- Queue step f, point 3, second part: airship route planner by distance and duration. Leg and survey times come from the game's own
  functions (framework thread, logged in, pointer-checked, one table per speed); how a voyage adds them up is not confirmed in game and
  is labelled so. Adversarial review of 0.335–0.337: 9 findings fixed (route price cache, no-listing items, band-aware missing count,
  unknown airship experience, tooltips, table height, cancellation on close).

## 0.336.0
- Étape f) de la file, point 3 (DEC-023), première partie : secteurs de dirigeable.
- Données embarquées : l'arbre de déblocage (AirshipUnlock, 24 lignes) et le butin possible (AirshipDrop, 261 lignes, sans quantité
  ni taux) de LuminaSupplemental (Critical-Impact, GPL-3.0, même licence que Tensho).
  - Commit bd265c2b ; contenu publié gardé tel quel.
  - En-tête de chaque fichier : source, empreinte SHA-256 et avis GPL-3.0.
  - Crédit embarqué ajouté (Paramètres › Crédits), README et REFERENCES à jour.
- Nouvel onglet Compagnie › « Secteurs de dirigeable », pour les 23 secteurs de la Mer de nuages (le Diadème est écarté) :
  - rang, réservoirs de céruleum, durée de relevé de base et expérience, tirés des données du jeu ;
  - « Débloqué par » (avec la surveillance demandée) et, au survol, le chemin depuis le départ ;
  - « n objets » de butin possible, listés avec leur icône au survol ;
  - un filtre par nom de secteur ou d'objet et un filtre par rang.
  - La valeur d'un voyage reste inconnue : aucune quantité n'est publiée.
- Sonde de la feuille AirshipExplorationPoint : PROBE_AIRSHIP dans tools/ShopCurrencyProbe. Preuve :
  docs/evidence/0.336-airship-sectors.md.
- Planificateur de distance et de durée : pas encore fait.
  - Les seules formules publiques sont des messages de forum de 2015, non confirmés.
  - Le jeu a ses propres fonctions de calcul (HousingManager.GetAirshipVoyageTimeAndDistance, GetAirshipSurveyDuration), mais les
    appeler exécute du code du jeu : version suivante, après accord de l'utilisateur.
- Tests : Airships336Tests (3 cas). Verify.ps1 (agent) : 647/647, 0 avertissement, 0 erreur ; DLL 0.336.0.0. Non validé en jeu.
### English
- Queue step f, point 3, first part: the airship unlock tree and possible loot of LuminaSupplemental (GPL-3.0) are embedded with their
  notice and shown in a new « Airship sectors » tab (rank, ceruleum, base survey time, experience, unlock, loot list without quantity).
  The distance/duration planner waits for the user's go-ahead, as it would call the game's own voyage functions.

## 0.335.0
- Étape f) de la file, point 2 (DEC-023) : valeur des trajets de sous-marin (Compagnie › Meilleurs trajets).
- Prix du marché : nouveau choix « Prix » : « Revente au PNJ » (par défaut, comme avant) ou « PNJ ou marché (le plus haut) ».
  - Pour chaque objet vendable au marché, on prend le prix minimum NQ de votre monde (Universalis) s'il est connu, frais et plus haut que
    le prix PNJ ; sinon le prix PNJ.
  - Un objet sans prix connu garde le prix PNJ et est compté comme manquant : la valeur s'affiche alors « ≥ … ». Jamais de prix inventé.
  - Le bouton « Prix du marché (n sur m manquants) » récupère les prix qui manquent pour le butin des trajets listés, les plus fréquents
    d'abord, 32 au plus par clic. Il suit la même cadence et le même cache que le coût de préparation, respecte l'interrupteur
    Universalis, et le classement est refait quand la liste se termine.
  - Au survol d'un trajet, chaque objet donne son prix PNJ et son prix de marché.
- Paliers (sonde d'abord, docs/evidence/0.335-submarine-tiers.md) : la source sépare bien ses relevés par surveillance du sous-marin
  (Min / Mid / High, dans chaque pool Tier1 à Tier3 ; les comptes se recoupent sur les 143 secteurs).
  - Nouveau choix « Surveillance » : tous les relevés (par défaut, comme avant), sous le palier 2, palier 2 ou palier 3.
  - Un secteur sans relevé à ce palier garde la moyenne de tous ses relevés, signalé au survol.
  - Récupération non modélisée : la source ne donne par palier que des fourchettes de quantité, pas des totaux.
- Données : tools/submarine-loot.py garde aussi les pools par palier. Même fichier source qu'en 0.309 (même SHA-256), donc base
  inchangée ; submarine-loot.json passe de 24 à 55 Ko.
- PreparationPriceSession gagne StartListAsync (liste bornée hors plan d'artisanat).
- Tests : Routes335Tests (4 cas). Verify.ps1 (agent) : 644/644, 0 avertissement, 0 erreur ; DLL 0.335.0.0. Non validé en jeu.
### English
- Queue step f, point 2: submarine route value can use the best of the NPC price and your world's lowest market price (Universalis,
  fetched on a click, 32 items at most, missing prices shown as « ≥ »), and the loot of one surveillance tier, since the community
  source splits its records by tier. Retrieval tiers are not modelled (the source only gives ranges).

## 0.334.0
- Étape f) de la file, point 1 (DEC-023) : la table des seuils de butin des sous-marins (SubmarineTracker) est gardée ; sur les 19 secteurs
  où elle est plus basse que toutes les observations de FFXIVGachaSpreadsheet (81, 86, 92, 98, 105, 109, 111, 112, 121, 125, 126, 130,
  132, 133, 144 à 148, docs/evidence/0.315-vessel-builds.md), la page Montages affiche « Palier non confirmé par les relevés sur ce
  trajet », avec une infobulle qui explique et liste les secteurs.
- Point 6, seconde moitié : livraisons des Planeurs d'Ehcatl laissées de côté, rien de lisible dans le jeu (sonde,
  docs/evidence/0.333-ehcatl-probe.md), comme DEC-023 le prévoit.
- Tests : Sectors334Tests (1 cas : 19 secteurs, tous présents dans la table embarquée). Verify.ps1 (agent) : 640/640, 0 avertissement,
  0 erreur ; DLL 0.334.0.0. Non validé en jeu.
### English
- Queue step f, point 1: the community submarine breakpoints are kept, and the 19 sectors where they are below every record are
  flagged « tier not confirmed by records » on the Builds page. Ehcatl Nine deliveries left aside: nothing readable in the game.

## 0.333.0
- Étape f) de la file, point 6 (DEC-023) : rappels de l'escouade de Grande Compagnie, mission ET entraînement (demande de l'utilisateur).
- Sonde : l'heure de fin n'est pas dans le gestionnaire de l'escouade mais dans PlayerState (SquadronMissionCompletionTimestamp,
  SquadronTrainingCompletionTimestamp, ActiveGcArmyExpedition, ActiveGcArmyTraining), disponible toute la session. Trouvé en regardant
  les autres plugins (MogHouse Companion, Accountant : lecture seule, rien de copié). Preuve : docs/evidence/0.332-squadron-probe.md.
- Rappels : deux lignes, « Mission d'escouade » et « Entraînement d'escouade » : nom tiré des données du jeu, « en cours » ou « terminé
  (à récupérer à la caserne) », heure de fin, et la cloche pour créer une alarme (et Discord, catégorie Rappels). Lecture gardée à part :
  si elle échoue, ces deux lignes disent « inconnu » et le reste des Rappels continue (R-14).
- Reporté à la fin : « équipement de l'escouade » (R-11 étape 4) n'a pas d'objet, ce n'est que de l'apparence ; recommandé : le retirer
  et garder la barde de chocobo (ROADMAP).
- Tests : Squadron333Tests (1 cas). Verify.ps1 (agent) : 639/639, 0 avertissement, 0 erreur ; DLL 0.333.0.0. Non validé en jeu (valeurs
  des heures à vérifier).
### English
- Queue step f, point 6: Grand Company squadron reminders, mission and training. Their end times are read from PlayerState (found by
  looking at other plugins, nothing copied) and shown in the Reminders with an alarm button; a failed read leaves them unknown.

## 0.332.0
- Étape e) de la file, dernier point : cadres et textures du jeu (ULD), première version, avant D17.
- Nouvelle sonde en lecture seule tools/UldProbe : quels fichiers d'interface du jeu existent, les textures qu'ils utilisent, leurs
  tailles et le découpage que le jeu déclare. Preuve : docs/evidence/0.332-uld-window-frame.md (cadre des fenêtres WindowA_BgNormal,
  coins, bords et centre, versions _hr1 au double).
- Nouveau réglage « Cadre des fenêtres du jeu » (Paramètres › Interface, infobulle et ↺), désactivé par défaut : il dessine derrière la
  fenêtre principale le cadre des fenêtres du jeu, découpé en neuf morceaux comme le jeu le fait. Si une texture manque (mise à jour du
  jeu), rien n'est dessiné et la fenêtre garde son aspect (R-14).
- Reporté à la fin : l'activer par défaut, l'étendre à la carte et aux panneaux, ajouter les séparateurs du jeu, après l'avoir vu en jeu
  (ROADMAP).
- Tests : GameFrame332Tests (1 cas). Verify.ps1 (agent) : 638/638, 0 avertissement, 0 erreur ; DLL 0.332.0.0. Non validé en jeu (aspect
  jamais vu).
### English
- Queue step e, last item: game frames (ULD), first version. New read-only probe tools/UldProbe proves the game's window frame
  textures and their nine-piece cut. New opt-in setting « Game window frame » draws that frame behind the main window; nothing is
  drawn if a texture is missing. Turning it on by default and extending it waits for an in-game look.

## 0.331.0
- Étape e) de la file : outils restants (alarmes et Discord, compagnie libre, quêtes, collections, butin, équipement, Revue de mode,
  mini-jeux, accueil). Audit en lecture seule, un relecteur par domaine, puis un vérificateur : 57 défauts confirmés sur 58, corrigés par
  5 agents sur des fichiers séparés. Sauvegarde : checkpoints/backup-20261102-212918-avant-outils-0331.zip.
- Alarmes :
  - une commande /alarm en file n'est plus envoyée au jeu après la suppression de son alarme ; un nom de jeu déjà pris n'est plus
    envoyé deux fois ;
  - une alarme copiée dans le jeu dont la copie a disparu peut de nouveau sonner par Tensho (bouton « Faire sonner par Tensho ») ;
  - une alarme météo n'est plus sautée quand le catalogue de météo n'est pas encore chargé ; une alarme sans avance ne se rate plus
    sur une image lente ;
  - Discord : la pause demandée par Discord est affichée, et « Effacer » ou une nouvelle adresse l'annulent ; les retours des vaisseaux
    suivent la catégorie « Compagnie libre » ;
  - infobulles des cloches avec le vrai délai ; barre du serveur qui signale les créneaux soumis à la météo.
- Compagnie libre : lectures de l'atelier isolées (R-14) ; catalogues en échec dits ; sous-marin choisi stable ; une compagnie
  quittée peut être oubliée (deux clics) ; rang du plan enregistré une seule fois.
- Quêtes, collections, butin : le panneau du Duty Finder a son propre service et ne change plus le contenu choisi sur la page Butin ;
  cache « non obtenus seulement » de la page Butin à jour ; états après la déconnexion corrigés ; lectures gardées (R-14).
- Équipement : un import sans pièce utilisable n'efface plus le plan ; matérias suivies quand la pièce change ; « Vider le plan » vide
  aussi les matérias ; anneau unique pas deux fois ; liste des pièces mise en cache.
- Mini-jeux et coque : « Tout remettre par défaut » remet aussi le mode développeur affiché ; lecture des PNJ battus gardée (R-14) ;
  textes corrigés (dont un « tu »).
- Relecture (3 relecteurs et un vérificateur) : 8 remarques confirmées sur 12, 7 corrigées : rappel par défaut enregistré une fois
  (plus à chaque frappe), mode développeur annulé si l'enregistrement échoue et bouton ↺, orchestrion illisible laissé « inconnu », fenêtre
  de l'orchestrion alignée sur le badge, cartes Triple Triad ignorées signalées, « encore en lecture » seulement pendant une lecture,
  description des Collections corrigée.
- Reporté : rappels « fait » et carte au trésor par personnage (format de sauvegarde à changer, prévu dans les propositions T1).
- Reporté aussi : un anneau unique importé à côté du même anneau porté peut encore compter deux fois (relecture 0.331, faible).
  30 propositions à choisir dans docs/project/TOOLS-PROPOSALS.md (recommandées : T0 à T4).
- Tests : Tools331Tests (3 cas) ; un test 0.326 adapté. Verify.ps1 (agent) : 637/637, 0 avertissement, 0 erreur ; DLL 0.331.0.0.
  Non validé en jeu.
### English
- Queue step e: remaining tools audit (57 confirmed defects), fixed. Alarms: a queued /alarm command is no longer sent after its alarm
  is deleted; a game copy that disappeared can ring in Tensho again; weather alarms wait for the weather catalogue; Discord pauses are
  shown and cleared. Free company reads isolated (R-14). The Duty Finder panel has its own loot service. Gear plans are no longer
  erased by an empty import. Restore-all resyncs developer mode. 30 improvement proposals in docs/project/TOOLS-PROPOSALS.md.

## 0.330.0
- Étape e) de la file : artisanat et récolte. Audit en lecture seule : 28 défauts confirmés (environ 20 distincts), corrigés par 4 agents.
  Le plus grave : « Lancer la chaîne de macros » pouvait envoyer au jeu les macros d'une autre recette ; il n'apparaît plus que sous les
  pages écrites par la dernière copie réussie, pour le même personnage. Le plan des séquences est rechargé (plus ajouté) ; seules les
  macros nommées exactement « préfixe page/pages » sont considérées comme celles de Tensho (vos macros ne sont plus écrasées).
- Récolte : le plan de PR n'invente plus une valeur non lue et ne se recalcule plus en cours de point ; horaires traduits ; une fenêtre
  qui passe minuit ne crée plus d'alarme à 00:00 ; téléchargement GatherBuddy arrêté par son interrupteur et réessayé après un délai.
- Paramètres : ↺ des observations ; observation de récolte décochée qui reste décochée ; « Tout remettre par défaut » efface le texte
  de macros préparé ; message du chat avec les vrais emplacements.
- Relecture (3 relecteurs et un vérificateur) : 3 remarques confirmées, soit 2 défauts distincts, corrigés :
  - une copie faite sans enchaînement ne remplace plus le plan de séquences enregistré ;
  - un bonus d'Abondance inconnu (niveau 68 et plus) ne bloque plus le plan : il compte le minimum de la règle, +1, et le panneau le dit.
- 37 propositions à choisir dans docs/project/CRAFT-GATHER-PROPOSALS.md. Tests : CraftGather330Tests (3 cas). Verify.ps1 (agent) :
  634/634, 0 avertissement, 0 erreur ; DLL 0.330.0.0. Non validé en jeu.
### English
- Queue step e: crafting and gathering audit, fixed. « Start the macro chain » only runs the pages written by the last successful copy;
  only macros named exactly « prefix page/pages » are Tensho's. The GP plan never guesses an unread value; schedules are translated;
  midnight windows ring once; an unknown Bountiful bonus uses the rule's +1 floor and says so. Review findings fixed.

## 0.329.0
- Étape e) de la file : objets, banque, tri et marché. Audit en lecture seule (4 agents, puis un vérificateur) : 41 défauts confirmés,
  environ 33 distincts. Corrigés par 4 agents sur des fichiers séparés, puis relus (3 relecteurs et un vérificateur : 6 remarques
  confirmées, corrigées). Sauvegarde : checkpoints/backup-20261102-173157-avant-objets-0329.zip.
- Le plus important (R-14) : une lecture secondaire du jeu en échec (coffre à apparence, armoire, liste des servants…) remettait le
  personnage à 0 dans tout le plugin : banque vide, stocks inconnus, tri refusé, chaîne de craft effacée. Le personnage reste maintenant
  connu, chaque lecture est isolée, et un avis durable dit qu'un rangement est illisible. La lecture de l'ordre visuel est gardée aussi :
  les quantités restent relevées, et le tri refuse toujours un ordre absent.
- Banque :
  - « Enregistrement automatique » ne concerne plus que les servants et les sacoches, comme son libellé le dit ; l'arsenal, le coffre
    à apparence et l'armoire (sans bouton d'import) sont relevés dans tous les cas, sans message « import » parasite ;
  - un coffre à apparence plein s'enregistre et se recharge (limite par rangement relevée, identique à l'enregistrement et au
    chargement) ;
  - titres des cadres avec la date, numéro de sac en tête ; états vides qui disent la vraie raison ; aide de l'import corrigée.
- Stocks : en-tête de la fiche à jour ; « TOTAL » exact seulement sans copie en cache ; les cristaux comptent leur sac, et ceux des
  servants (jamais importés) sont une borne basse (≥) au lieu d'un zéro connu, sur la fiche, au survol, dans les recettes et la chaîne.
- Marché : la pause de 3 s d'Universalis n'affiche plus « prix indisponibles » au survol ni n'efface un prix affiché ; dates des
  prix exactes ; ligne du plan surlignée une seule fois ; durée du cache lisible.
- Objets : historique et fil d'Ariane dans le bon ordre et bornés ; « Utilisé dans » mis en cache avec un clipper ; message d'import
  limité ; aperçu du tri qui dit pourquoi il échoue (sans recalcul à chaque image) ; clic sur une case qui ne remplace plus la recherche.
- Textes et documents : aide du survol (Garland et le wiki sont bien interrogés), menu contextuel, page Diagnostics, ARCHITECTURE et
  GAPS corrigés.
- Reporté à la fin : inventaire vidé 250 ms à chaque mouvement (DEC-004, ROADMAP). 23 propositions à choisir dans
  docs/project/ITEMS-PROPOSALS.md (recommandées : I0, I1, I3, puis I4 et I14).
- Tests : Items329Tests (2 cas) ; un test du marché adapté (la pause n'est plus une erreur). Verify.ps1 (agent) : 631/631,
  0 avertissement, 0 erreur ; DLL 0.329.0.0. Non validé en jeu.
### English
- Queue step e: items, bank, sorting and market audit (about 33 distinct defects), fixed and reviewed. R-14: a failed secondary game
  read no longer signs the character out of every stock feature; reads are isolated and a lasting notice says which storage is
  unreadable. Armoury, glamour dresser and armoire are recorded whatever the auto-save setting; a full dresser saves and reloads.
  Retainer crystals count as a lower bound. The 3 s Universalis pause no longer shows an error or wipes a price. History, craft book
  cache, sort preview errors, bank texts and documents fixed. 23 improvement proposals listed in docs/project/ITEMS-PROPOSALS.md.

## 0.328.0
- Étape e) de la file, la carte d'abord. Audit de la carte en lecture seule (4 agents, puis un vérificateur) : 50 remarques, 45
  confirmées, soit 28 défauts distincts. Corrections faites par 4 agents sur des fichiers séparés, puis relues (3 relecteurs et un
  vérificateur : 8 remarques confirmées, corrigées). Sauvegarde : checkpoints/backup-20261102-162017-avant-carte-0328.zip.
- Le plus important : avec « Remplacer la carte du jeu », la touche M ne montrait plus aucune carte quand la carte Tensho ne pouvait pas
  s'afficher (catalogue en échec ou en chargement, masquage en combat ou en mission). La carte du jeu reste maintenant à l'écran dans
  ces cas. « Ouvrir la carte du jeu » depuis la carte Tensho ramène aussi une carte du jeu cachée.
- Résistance aux mises à jour (R-14) :
  - lectures des marqueurs et des coûts de téléportation bornées ;
  - chaque lecture secondaire isolée : une panne ne coupe plus la position ni le drapeau ;
  - une lecture des icônes en échec est dite sur la carte, au lieu d'une carte vide ; une image introuvable dit « indisponible » au
    lieu de « chargement » à vie ; une position inconnue est dite ;
  - un coût de téléportation illisible est dit au lieu de « Lecture du coût… ».
- Carte :
  - zoom +/−/curseur autour du centre de la vue (la vue glissait) ;
  - un clic sur un repère au bord ne déclenche plus l'icône dessous ;
  - les repères au bord suivent le drapeau et les icônes masqués ;
  - le survol d'une zone suit les interrupteurs des zones ;
  - une erreur de lien ne suit plus la souris partout ; elle s'affiche 5 s au survol de la carte ;
  - ouvrir la carte sur une carte choisie l'affiche entière ;
  - bouton ↺ par icône ; « Rétablir les valeurs par défaut » remet aussi le mode du clic droit ;
  - priorité et menu propre grisés pour les couches Tensho, le drapeau et les coffres (sans effet) ;
  - infobulles des réglages grisés ; échec d'enregistrement des réglages de carte affiché ;
  - erreur de lecture du wiki des panoramas dite, avec un nouvel essai au plus chaque minute ;
  - vendeurs et étiquettes mis en cache (ils étaient refaits à chaque image) ; listes du clic droit aussi.
- Listes, favoris, alarmes :
  - clic droit d'un panorama dans la liste : l'alarme est proposée ; numéros des panoramas alignés sur le carnet du wiki ;
  - alarme de panorama : une météo inconnue est dite, sans promettre « créneau et météo » ;
  - favoris : nom en double, trop long ou liste pleine dits comme tels (plus « Échec de sauvegarde ») ;
  - page Cartes : le bouton pose le drapeau et ouvre la carte (Tensho si elle remplace celle du jeu, sinon celle du jeu) ; libellé
    corrigé.
- Textes : « Niveau à côté des points de récolte » (le nom n'était pas écrit) ; aide des infobulles de quête ; boutique des crédits de
  compagnie traduite ; ARCHITECTURE et DECISIONS mis à jour.
- Reporté à la fin : identifiant 60561 de l'icône du drapeau à prouver en jeu (ROADMAP). 28 propositions d'amélioration de la carte,
  rien d'implémenté : à choisir dans docs/project/MAP-PROPOSALS.md (recommandées : P1, P2, P3, puis P4, P6, P8).
- Tests : Map328Tests (4 cas). Verify.ps1 (agent) : 629/629, 0 avertissement, 0 erreur ; DLL 0.328.0.0. Non validé en jeu.
### English
- Queue step e, the map first: a read-only map audit (28 distinct defects), fixed and reviewed. With « Replace the game map », M no
  longer shows no map at all when the Tensho map cannot draw (catalogue failed or loading, hidden in combat or duties): the game map
  stays. Native map reads are bounded and isolated per part (R-14); unreadable icons, textures, position and teleport costs are said.
  Zoom buttons keep the view centre; edge markers no longer trigger the icon under them and follow hidden layers; area hover follows
  the area switches; map errors no longer follow the mouse; per-icon reset; vendor labels and right-click lists cached. Bookmark,
  vista alarm and map page fixes; texts corrected. 28 improvement proposals listed for the user in docs/project/MAP-PROPOSALS.md.

## 0.327.0
- Étape e) de la file, 1re version : réglages « sans question » de l'audit du point d), tous avec infobulle et ↺. Par défaut, rien ne
  change. Fiches préparées par 4 agents en lecture seule, puis appliquées (90 modifications) :
  - nombre de résultats de la recherche d'objets (30 par défaut, 10 à 100) ; l'avis « N résultats au plus » suit la valeur ;
  - ouvrir sur la dernière page utilisée (désactivé : Accueil, comme avant) ;
  - portée de la page Marché mémorisée (centre de données par défaut) ;
  - masquer le panneau de survol en mission (désactivé par défaut) ;
  - quatre parties du panneau de survol (collection, stock, usages, comment l'obtenir), toutes activées ;
  - trois interrupteurs des aides Triple Triad en jeu (plateau, défi d'un PNJ, éditeur de decks), activés ;
  - calcul automatique de l'aide du carnet d'artisanat (activé ; sinon bouton « Calculer ») ;
  - alarmes : pas de ligne de discussion en mission (désactivé par défaut, sous « Pas de son en mission »).
- Écarté : « prix de la boutique » au survol (cette partie n'existe pas dans le panneau).
- Trois textes français au tutoiement corrigés (decks Triple Triad).
- Reporté à la fin : « Monstres au survol » ne commande plus rien (code mort) ; recommandé : le retirer (ROADMAP).
- Tests : 5 nouvelles classes (17 cas). Verify.ps1 (agent) : 625/625, 0 avertissement, 0 erreur ; DLL 0.327.0.0. Non validé en jeu.
- Relecture multi-agents en lecture seule (3 relecteurs puis un vérificateur ; relancée après la limite de dépense) : 9 remarques,
  6 confirmées (4 défauts distincts), corrigées :
  - carnet : couper « calcul automatique » pendant un calcul affichait un bouton « Calculer » qui ne faisait rien ;
  - Marché : une portée « Région » mémorisée dont le nom n'est pas encore connu disait « Monde inconnu » ; elle dit maintenant
    pourquoi (table d'Universalis pas encore lue, ou Universalis coupé) ;
  - Triple Triad : l'aide du match ne promet plus le cadre vert quand « Conseil en match » est décoché ;
  - recherche d'objets : l'avis ne conseille plus d'augmenter un nombre déjà à 100.
  Écartées (3) : les calculs Triple Triad déjà lancés finissent seuls en quelques secondes, comme quand la fenêtre de défi se ferme.
### English
- Queue step e (1/…): eight settings with tooltip and reset, defaults unchanged: item search result count, open on the last page,
  remembered Market scope, hide the hover panel in duties, four hover panel parts, three Triple Triad in-game aid switches, crafting
  log auto compute, alarm chat line muted in duties. Three French texts fixed. A read-only multi-agent review confirmed 4 small defects,
  fixed: a dead Compute button, a saved Region scope with no reason shown, a Triple Triad help text and the item search notice.

## 0.326.0
- Revue multi-agents du point d) (0.321 à 0.325), en lecture seule : 4 relecteurs (ImGui et état de l'interface ; services,
  réseau et threads ; exactitude des textes ; tests et scripts), puis un vérificateur chargé de réfuter chaque remarque. Base : le
  diff exact depuis la sauvegarde prise avant 0.321. 35 remarques, 33 confirmées (4 en double), toutes corrigées ici.
  Sauvegarde avant les corrections : checkpoints/backup-20261102-135157-avant-revue-d.zip.
- Interrupteurs de sources :
  - Garland (sources d'objets), wiki (mode d'obtention) et Teamcraft : les demandes déjà en file ne partent plus une fois
    l'interrupteur coupé. Le wiki téléchargeait encore une page toutes les 3 s, Teamcraft postait les lieux restants.
  - Ces demandes sont oubliées, donc redemandées quand l'interrupteur est rallumé. Avant, Garland répétait le refus 5 fois
    (attente de 3 s chaque fois, file bloquée environ 15 s par objet), puis gardait l'erreur jusqu'à la fin de la session.
  - Duty Finder : le contenu refusé par l'interrupteur est redemandé une fois quand Garland est rallumé.
  - Revue de mode : « Actualiser » est grisé quand sa source est coupée (il ne faisait rien sans rien dire).
  - Accueil : une source coupée n'est plus listée comme « service en erreur ».
  - Marché : la portée « Région » grisée dit que Universalis est coupé (elle parlait d'une table illisible, réessayée chaque minute).
  - Paramètres › Interface : la raison du grisé de « Monstres au survol » est l'infobulle de la case elle-même (elle passait sur ↺).
- Corrections de 0.325 :
  - Duty Finder : le cache des groupes suit l'état des collections ; « non obtenus seulement » ne reste plus vide après le
    chargement des collections, ni périmé après une obtention ;
  - alarmes : « Tout supprimer » est armé pour son groupe dans sa section seulement (le même objet dans deux sections de métier
    armait les deux). Il est désarmé dès que le groupe n'est plus affiché ;
  - « Ajouter (chaque jour ET) » affiche le délai réellement donné à l'alarme (60 min au plus).
- Corrections de 0.324 :
  - copie des macros : chaque ↺ garde début ≤ fin (le bornage annulait le ↺ de la fin) ;
  - Banque : les titres des cadres utilisent aussi le nom traduit du rangement.
- Textes :
  - les textes qui promettaient des copies affichées pour toutes les sources sont corrigés. C'est vrai pour le marché, le butin, les
    monstres du wiki, le carnet d'exploration et la Revue de mode ; c'est faux pour les sources d'objets, le mode d'obtention,
    Teamcraft, LuminaSupplemental et GatherBuddy. Sont concernés l'infobulle Universalis (les prix au survol sont masqués),
    « Source désactivée », SECURITY.md et l'entrée 0.324 ;
  - menus en français : « Paramètres › … » (le menu ne s'appelle pas « Réglages »), dans les textes et les listes de tests en jeu ;
  - la ligne Réglage de la page Crédits ne parle plus de réglage partiel ;
  - CONTRIBUTING : ordre réel de la release, client Teamcraft borné à la lecture depuis 0.321 ;
  - commentaire de MapViewSettings remis au-dessus de sa classe.
- Release :
  - Verify.ps1 -Release ne s'arrête plus sur un tag existant. Release.ps1 décide : il s'arrête si la release est complète (release
    GitHub et main), sinon il reprend les étapes manquantes, y compris main non poussé (repo.json jamais publié) ;
  - à la reprise, Devloppement est de nouveau sélectionné avant d'être avancé ;
  - les sondes gh passent par Test-Native : sous Windows PowerShell 5.1, « release not found » arrêtait le script (essayé ici
    sous 5.1 et 7 sur une commande factice) ;
  - les nouveaux fichiers aux noms accentués sont listés sans guillemets, donc l'archive ou le gros fichier est bien refusé ;
  - vérifié par l'analyse de syntaxe sous PowerShell 5.1 et 7 seulement : l'agent ne lance ni -Commit ni -Release.
- Tests :
  - le test Teamcraft vérifie la méthode, le User-Agent et le refus pour taille après la réponse (avant, une erreur dans le
    gestionnaire était avalée par le service et le test passait) ;
  - le cliquet des infobulles voit aussi les curseurs verticaux et les champs multi-lignes ; il s'arrête à tout autre élément
    dessiné (un bouton suivant ne couvre plus le contrôle) ; il échoue s'il voit moins de 150 contrôles (201 aujourd'hui) ;
  - les filtres bin/obj portent sur le chemin relatif (un dépôt cloné sous un dossier « bin » vidait les scans) ;
  - nouvelles infobulles : choix « Tous » des collections et des quêtes, zoom de la carte ;
  - ReviewFixes326Tests : 4 cas, dont un test de comportement Teamcraft (le lieu en attente n'est pas posté, puis il est redemandé).
- Écartées par le vérificateur (2) : « relevés » dans l'infobulle « Tout remettre par défaut » (formulation antérieure, non
  contredite par le code) ; le nom d'un test des brouillons GitHub.
- Verify.ps1 (agent) : 608/608, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.326.0.0. Non validé en jeu.
### English
- Read-only multi-agent review of queue step d (0.321–0.325): 33 confirmed findings, all fixed. Turning a source off now also stops
  the requests already queued for Garland item sources, wiki item pages and Teamcraft, and forgets them so they are asked again
  when the switch is back on (Garland retried the refusal for about 15 s per item and then kept the error). The Duty Finder asks
  again after Garland comes back, and its 0.325 cache now follows the collections state. « Delete all » of an alarm group is armed
  per job section and dropped when not shown. Fashion Report « Refresh » follows its switch; Home no longer lists a turned-off
  source as failed. Texts that promised cached copies for every source were corrected. The French menu is named « Paramètres ».
  Release: Verify.ps1 -Release no longer stops on an existing tag; Release.ps1 resumes when the GitHub release or main is missing,
  and its gh probes no longer abort Windows PowerShell 5.1. Non-ASCII new file names are checked. The tests are stronger: the
  Teamcraft checks can now fail, the tooltip ratchet is wider and guarded, and bin/obj filters use relative paths.

## 0.325.0
- Point d) de la file, 5/5 : infobulles des filtres et des champs, confirmations, petits restes (revue 0.316). Fait sans
  l'utilisateur, choix provisoires notés pour la fin.
- Cause : sur les pages, environ 115 filtres, recherches, tris et champs de saisie n'avaient pas d'infobulle (relevé par un
  scan du code). Le test des clés existant ne voyait ni l'emplacement d'une infobulle ni les clés construites.
- Infobulles : 140 nouvelles clés en français et en anglais (dont les textes des confirmations), posées sur les pages suivantes :
  - Banque, Collections, Succès, Orchestrion, Butin des contenus, Quêtes et sources de quêtes ;
  - Récolte et optimiseur de PR ;
  - Objets, simulateur et recherche d'artisanat, projection, plan, séquences, chaîne, enveloppe des conditions ;
  - Marché et coût de préparation ;
  - Cartes, favoris, listes locales, menus Région/Zone/Étage, tailles des icônes ;
  - Rangement (aperçu, règles, modèles) ;
  - Triple Triad (carnet, PNJ, simulation) ;
  - Revue de mode, Équipement (matérias), Météo, Monstres, Outils, Macros, Itinéraires de sous-marin.
- Méthode des textes : rédigés par l'audit en lecture seule, puis revérifiés sur le code actuel par 4 agents (un groupe de
  fichiers chacun). Une trentaine de textes ont été corrigés quand le code disait autre chose. Exemples :
  - les catégories de la Banque sont celles de l'onglet affiché ;
  - « Non confirmés » ne veut pas dire la même chose pour les titres et succès que pour les autres collections ;
  - « Tout afficher » et « Tout masquer » des icônes de carte n'agissent que sur les icônes filtrées.
- Libellés corrigés :
  - carte : « Tout remettre à 100 % » devient un libellé exact, avec une infobulle par portée. En réalité, le groupe revient à
    50 % et quelques priorités à ±1, et les réglages propres d'une carte sont gardés ;
  - quêtes : le filtre dit « Non confirmées » (l'état d'une quête reste « Non confirmée ») ;
  - Revue de mode : l'aide de recherche ne dit plus « nom français », car la recherche suit la langue du client.
- Duty Finder (panneau du butin) :
  - la recherche accepte l'identifiant exact d'un objet, comme son texte d'aide l'annonçait (seul le nom était comparé) ;
  - ↻ et la liste d'origine du butin ont leur infobulle ;
  - les comptes et les groupes sont calculés une fois par changement de filtre, au lieu d'à chaque image.
- Suppressions :
  - « Tout supprimer » d'un groupe d'alarmes demande un second clic (« Confirmer : supprimer N alarme(s) », ou Annuler) ;
  - la suppression d'une alarme, d'un deck, d'un modèle de rangement ou d'une séquence d'artisanat dit, au survol, qu'elle est
    immédiate.
- Alarme manuelle : nom, heure, minute et bouton « Ajouter » ont leur infobulle.
- Commentaire ShowVendors (Configuration.cs) mis d'accord avec la valeur réelle (activé par défaut).
- Nouveau test « à cliquet » (TooltipRatchet325Tests.cs, liste tests/TenshoAssistant.Tests/tooltip-baseline.txt) :
  - tout nouveau contrôle sans infobulle fait échouer Verify ;
  - une ligne de la liste qui ne correspond plus à rien aussi, pour que la liste ne fasse que diminuer ;
  - restent 3 exceptions : la grille du Mini Cactpot et la fenêtre « Règles du score » (aide en tête) ;
  - deuxième cas : une infobulle ne doit jamais devenir le corps d'un « if » autour d'un contrôle (le défaut de 0.287).
- Reporté à l'étape e) : mise en cache du plan et du coût de préparation (mesurés sous 0,5 ms par image en 0.292,
  docs/evidence/0.292-draw-times.md), à mesurer en jeu avant de changer.
- Reporté à la fin : la confirmation en deux clics ne couvre que « Tout supprimer » d'un groupe d'alarmes ; les autres
  suppressions ont une infobulle (choix recommandé appliqué).
- Verify.ps1 (agent) : 604/604, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.325.0.0. Non validé en jeu.
### English
- Queue step d (5/5): about 115 filters, searches, sorts and input fields gain a tooltip (140 new keys, English and French). The
  texts were checked against the current code, and about thirty audit drafts were corrected. The map « Reset all to 100 % » label
  is now accurate, the quest filter reads « Unconfirmed », and the French Fashion Report search hint no longer says « French
  name ». The Duty Finder loot panel accepts an exact item ID, has tooltips, and no longer recomputes its groups every frame.
  « Delete all » of an alarm group asks for a second click; other immediate deletions say so on hover. A ratchet test fails on
  any new control without a tooltip, and on a baseline line that no longer matches (3 exemptions left). A second check rejects
  a Tip that becomes an if-body.

## 0.324.0
- Point d) de la file, 4/5 : les interrupteurs de sources coupent toute leur source (revue 0.316, points 3.0 à 3.3). Fait sans
  l'utilisateur, choix provisoires notés pour la fin.
- Cause : les interrupteurs de Réglages › Avancé ne coupaient qu'une partie de leur source, et la page Crédits le disait :
  - Garland Tools : seulement les sources d'objets ; le butin des contenus et des monstres l'interrogeait encore ;
  - le wiki : seulement le mode d'obtention des objets ; les monstres et le carnet d'exploration l'interrogeaient encore ;
  - Universalis : rien pour la page Marché ; seul existait « prix au survol ».
- Correction :
  - Garland Tools et le wiki : l'interrupteur arrête toutes leurs requêtes. Pour le butin des contenus et des monstres, les
    monstres du wiki et le carnet d'exploration, la copie gardée sur disque reste lue ; quand la source est coupée, elle est
    montrée quel que soit son âge (rien ne peut la rafraîchir). Les sources d'objets Garland et le mode d'obtention du wiki sont
    masqués tant que leur interrupteur est coupé (précision apportée en 0.326).
  - Universalis : nouvel interrupteur « Universalis (prix du marché) », activé par défaut. Il coupe la page Marché, les annonces,
    les prix au survol, le coût de préparation et la table des centres de données. « Prix au survol » passe en dessous, grisé quand
    Universalis est coupé.
  - « Monstres au survol » est grisé quand le wiki est coupé, et le survol ne le lit plus.
  - « Actualiser » est grisé quand sa source est coupée, avec une infobulle qui dit pourquoi. « Charger » reste actif, car il lit
    la copie en cache.
  - Une page qui tombe sur une source coupée affiche « Source désactivée » au lieu de « erreur » (Community/SourceDisabledException.cs,
    un seul message reconnaissable).
  - Le tableau des annonces se recharge une fois quand Universalis est réactivé.
  - Crédits : plus aucun réglage « partiel » ; Universalis est relié au nouvel interrupteur. Le manifeste et SECURITY.md sont mis
    à jour.
- Bouton ↺ (valeur par défaut) ajouté aux anciens réglages qui ne l'avaient pas :
  - ouverture de la fenêtre au chargement et à la connexion ;
  - survol (usages, monstres) et menu contextuel ;
  - butin du Duty Finder, panneau du personnage, capture automatique de la banque ;
  - durée de cache de chaque domaine ;
  - options des alarmes ;
  - copie des macros ;
  - options de la Revue de mode ;
  - zones de la carte, avec une infobulle sur la couleur.
- Tests : 2 nouvelles classes de cas (6 cas) :
  - aucune requête n'est envoyée quand Garland, Universalis ou le wiki sont coupés (services de test qui comptent les requêtes) ;
  - les crédits ne contiennent plus de réglage partiel ;
  - le branchement est vérifié dans Plugin.cs et dans le survol ;
  - les nouveaux boutons ↺ sont présents.
- Reporté à la fin : la décision R-13 sur les réglages partiels est résolue à titre provisoire. Les interrupteurs Garland et wiki
  couvrent toute leur source, et Universalis a le sien.
- Verify.ps1 (agent) : 602/602, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.324.0.0. Non validé en jeu.
### English
- Queue step d (4/5): each community source switch now stops every request of its source. Garland Tools also covers duty and
  monster loot. The wiki also covers monsters and the sightseeing log. A new « Universalis (market prices) » switch covers the
  Market page, listings, hover prices, preparation cost and the data centre list, with « prices on hover » nested under it.
  Copies kept on disk are still shown for duty and monster loot, wiki monsters and the sightseeing log (Garland item sources and
  wiki item acquisition are hidden while off). Forced refreshes are greyed with a tooltip, and pages say « source turned off » instead of
  « error ». The Credits page no longer lists any partial switch; the manifest and SECURITY.md are updated. Older settings gain
  their « ↺ » reset button: window opening, hover and menu options, Duty Finder, character panel, bank auto capture, cache
  durations, alarm options, macro copy, Fashion Report options and map areas.

## 0.323.0
- Point d) de la file, 3/5 : git, GitHub et outils pour les contributeurs. Fait sans l'utilisateur. Seuls des fichiers sont créés ou
  modifiés : rien n'est poussé, aucun réglage GitHub n'est changé, aucun workflow n'est activé.
- Release.ps1 (choix provisoire, à suivre de près à la première vraie release) :
  - Nouvel ordre : contrôles d'abord (fetch, Devloppement à jour, origin/main contenu dans Devloppement, gh connecté, paquet
    présent) ; puis les étapes locales (repo.json, fusion, tag) ; puis la publication (tag, release GitHub avec latest.zip, main
    qui porte repo.json en dernier, enfin Devloppement).
  - Avant, main et repo.json partaient avant la release : si gh échouait, les joueurs voyaient une version dont le téléchargement
    renvoyait 404, et une relance s'arrêtait tout de suite.
  - Reprise : si le tag existe sans release GitHub, seules les étapes manquantes sont faites.
  - Mode Commit : liste des nouveaux fichiers ajoutés, et arrêt (code 2) sur une archive, un binaire ou un fichier de plus de 5 Mo.
  - Vérifié par l'analyse de syntaxe seulement : l'agent ne lance pas -Commit ni -Release, même en -DryRun.
- Verify.ps1 :
  - nouvelle option -TestsOnly : tests et langues sans Dalamud, pour les contributeurs sans le jeu (596/596 en local) ;
  - l'empreinte inclut la version de Dalamud et du SDK, donc une mise à jour relance tous les contrôles ;
  - le rapport du validateur de langues est affiché quand il échoue (il était toujours jeté) ;
  - avertissement si l'entrée de tête du CHANGELOG ne correspond pas à <Version> ;
  - commentaire de -Release corrigé.
- Fichiers ajoutés :
  - .github : modèles d'issues en anglais (bug avec le rapport de diagnostic, demande de fonction, traduction, config sans issue
    vide, lien vers le signalement privé) et modèle de pull request (Verify, tests, CHANGELOG, réglages avec infobulle et ↺,
    vouvoiement, licences, essai en jeu, déclaration d'IA) ;
  - publish/github : brouillons inactifs avec leur README d'activation, à savoir CodeQL corrigé (branches main et Devloppement,
    build-mode none, l'actuel surveille une branche « Stable » absente de GitHub et échoue chaque semaine), compilation et tests
    sous Windows avec Dalamud téléchargé, et Dependabot pour les actions ;
  - tools/README.md : les 15 sondes en lecture seule et le script du butin des sous-marins ;
  - .editorconfig, aligné sur les fichiers actuels ;
  - .githooks/pre-push : lance Verify avant chaque push, désactivé tant que vous ne l'activez pas.
- .gitignore : .claude/settings.local.json, Thumbs.db, desktop.ini, __pycache__/. Sauvegarde zip mal placée
  (src/TenshoAssistant/checkpoints) déplacée dans checkpoints/.
- CONTRIBUTING et README : DALAMUD_HOME, -TestsOnly, outils pour contributeurs ; la mention « restored during the build », ambiguë,
  est corrigée.
- Tests : 2 nouveaux, dont un qui exige que toute classe de tests soit dans la liste (une classe oubliée ne tournait jamais).
- Reportés à la fin, recommandations notées :
  - activer les brouillons GitHub ;
  - protections de branches ;
  - passage des fins de ligne en eol=lf (renormalisation, à faire par vous après un commit propre) ;
  - code de conduite et CODEOWNERS (non créés).
- Verify.ps1 (agent) : 596/596, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.323.0.0 ; Verify.ps1 -TestsOnly aussi
  vert. Non validé en jeu.
### English
- Queue step d (3/5), files only (nothing pushed or enabled on GitHub): Release.ps1 now checks first, then publishes the tag and the
  GitHub release before main (which carries repo.json), and resumes a release whose tag exists without its GitHub release; commit mode
  lists new files and refuses archives, binaries and large files. Verify.ps1 gains -TestsOnly (no Dalamud needed), a stamp that
  includes Dalamud and SDK versions, and shows the language report on failure. Issue and PR templates, inactive CI drafts
  (publish/github), tools/README.md, .editorconfig, an optional pre-push hook, .gitignore entries; a test catches unregistered test
  classes.

## 0.322.0
- Point d) de la file, 2/5 (points ouverts de la revue 0.316, 2/2), fait sans l'utilisateur (« fais un max sans moi »), choix
  provisoires notés pour la fin.
- Noms de rangements traduits. Avant, « Sacs du personnage », « Cristaux », « Sacoche chocobo non importée », « Servants non
  importés », « Arsenal », « Coffre à apparence », « Armoire » et « Sacoche chocobo (supplémentaire) » étaient des littéraux français,
  affichés à tous les joueurs (Banque, survol, Équipement, panneau d'import, page Rangements). Maintenant :
  - chaque ligne porte un libellé (StorageLabel), et Inventory/StorageNames.cs en donne le nom traduit d'après le type ;
  - seul un servant garde son nom, qui vient du jeu ;
  - les anciennes banques qui gardent un nom français s'affichent traduites.
- Cristaux : classés d'après le nom anglais de l'objet (que le catalogue garde dans toutes les langues), le nom français en secours.
  Sur un client allemand ou japonais, aucun cristal n'était classé. Le classement est calculé une fois par objet (il l'était pour
  chaque case à chaque image). Non validé sur un client non français.
- Erreurs du catalogue d'actions d'artisanat : texte traduit (« ServiceErrors.Craft »), le détail seulement en mode développeur.
- Messages internes de MarketService et TriadSolveSession en anglais (journal et mode développeur).
- Nouveau test : chaque phrase des tables UiMessages et CraftMessages doit être émise par le code, et chaque clé doit exister en
  anglais et en français. Avant, une ponctuation changée dans un service faisait retomber l'affichage sur le message générique sans
  que rien ne le voie. Le passage de toutes les chaînes des services à des codes de message (environ 290) est reporté à l'étape g2.
- Texte au vouvoiement corrigé (« utilisez le bouton d'import »).
- Manifeste :
  - Description réécrite : fonctions actuelles, toutes les sources réseau, interrupteurs (dont les partiels), Discord facultatif,
    alarmes du jeu automatiques seulement avec leur réglage, « German and Japanese: menu titles only » ;
  - changelog de manifest.toml aligné ;
  - nouveau test : la description nomme chaque source réseau du catalogue des crédits.
- SECURITY.md : section « Supported versions » (seule la dernière release reçoit les correctifs, choix provisoire), portée datée sur
  la branche Devloppement, limite Teamcraft et étendue réelle des interrupteurs corrigées.
- Tests : 4 nouveaux.
- Verify.ps1 (agent) : 594/594, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.322.0.0. Non validé en jeu.
### English
- Queue step d (2/5): storage names are translated (they were French literals shown to everyone); crystals classified from the
  English item name on every client and once per item; crafting action errors translated; MarketService and TriadSolveSession internal
  messages in English; a test keeps the French-sentence → key tables in step with the sentences services emit; manifest description and
  D17 changelog rewritten (every network source, Discord, partial switches) with a test; SECURITY.md gains « Supported versions ».

## 0.321.0
- Point d) de la file (tour du code), 1/5. Sauvegarde : checkpoints/backup-20261102-121837-avant-point-d.zip.
- Audit multi-agents en lecture seule, sur quatre angles : réglages manquants, contrôles sans infobulle, points ouverts de la revue
  0.316, git, GitHub et outils pour contributeurs. Une synthèse contre-vérifiée a retenu 32 constats, rejeté 7 et découpé le travail
  en cinq versions :
  - 0.321 : enregistrement, réseau, Draw, réglages oubliés ;
  - 0.322 : textes figés en français, manifeste, SECURITY ;
  - 0.323 : git, GitHub et outils, en fichiers seulement ;
  - 0.324 : interrupteurs de sources respectés partout ;
  - 0.325 : infobulles des filtres et petits restes.
  Les choix de l'utilisateur sont reportés à la fin, avec un choix provisoire appliqué.
- Corrigé :
  - Case « Aide du carnet » (Réglages › Modules) : depuis la passe d'infobulles 0.287, son « if » ne gardait plus que l'infobulle.
    La configuration était donc réécrite sur le disque à chaque image tant que la page était ouverte, et l'infobulle ne s'affichait
    qu'au clic. Elle passe par la ligne de réglage commune (enregistrement au clic, ↺).
  - Un test parcourt le code : une infobulle ne doit jamais suivre directement le « if » d'un contrôle.
  - Limites de recherche d'artisanat (étapes, largeur, budget) et options d'export des macros (attente, écho) : enregistrées dans la
    configuration (elles revenaient aux valeurs d'origine à chaque chargement), avec ↺ et les mêmes bornes. Nouvelle ligne commune
    IntSetting : enregistrement en quittant le champ.
  - Universalis : après un échec, la table des centres de données n'est redemandée qu'une fois par minute (elle l'était à chaque
    image) ; le bouton « Région » grisé a une infobulle.
  - Teamcraft : la réponse est lue dans la limite de 1 Mio pendant la lecture, avec le User-Agent (elle était chargée entière puis
    mesurée). BoundedHttp accepte maintenant une requête POST.
  - Rappels : la lecture des minuteurs du jeu se fait une fois par seconde, et une fois toutes les dix secondes après un échec ; le
    journal n'est écrit qu'une fois par message (avant, il l'était à chaque image après un patch).
  - Cabine d'essayage : un échec est rattrapé, consigné une fois, puis ses boutons se grisent ; EndDisabled est dans un finally.
  - Revue de mode : nouvel interrupteur (Réglages › Avancé › « Revue de mode (FFXIVGachaSpreadsheet) »). Coupé, plus aucun
    téléchargement, et la page le dit ; une copie récente sur disque reste lue (choix provisoire). Le crédit est relié au réglage.
  - « Tout remettre par défaut » :
    - les groupes repliés de la navigation sont gardés ;
    - les anciennes valeurs reviennent si l'enregistrement échoue ;
    - les deux listes (gardé et remis à zéro) sont dans Configuration.cs, et un test exige que chaque propriété soit dans une
      seule, ce qui a aussitôt classé trois réglages oubliés ;
    - l'infobulle est complétée.
- Choix provisoires notés dans ROADMAP « Décisions reportées à la fin » ; liste de contrôle en jeu dans TESTING.md (« 0.321 »).
- Tests : 6 nouveaux.
- Verify.ps1 (agent) : 590/590, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.321.0.0. Non validé en jeu.
### English
- Queue step d (1/5), from a multi-agent audit (32 findings in five versions): the « Crafting log helper » checkbox no longer saves
  the configuration on every frame (a tooltip had slipped under its « if »; a test now catches that pattern); crafting search limits
  and macro export options are saved, with ↺; Universalis data-centre table retried at most once a minute; Teamcraft replies read
  within 1 MiB; reminders and Try On no longer fail or log on every frame; new Fashion Report source switch; « reset all » keeps folded
  navigation groups, rolls back on save failure, and a test assigns every configuration property to kept or reset.

## 0.320.0
- Demande (R-13) : « réserve une section complète dans le plugin pour les crédits de ce qu'on aurait utilisé dans tout le code ».
  Sauvegarde avant modification : checkpoints/backup-20261102-112058-avant-r13-credits.zip.
- Inventaire multi-agents en lecture seule : relevé du code (bibliothèques, données intégrées, hôtes réseau, règles reprises), relevé
  de la documentation (README, REFERENCES, DECISIONS, preuves), licences relues à leur source, puis fusion contre-vérifiée.
  72 entrées, preuve : docs/evidence/0.320-credits.json.
- Nouveau : page **Crédits** (Préférences › Crédits), visible par tout le monde.
  - Sept sections : le jeu, bibliothèques, données intégrées à Tensho, données lues en ligne, règles et formules reprises, plugins et
    sites lus pour comprendre, outils de fabrication.
  - Pour chaque entrée : nom, auteur, licence (propriétaire, aucune licence publiée ou inconnue quand c'est le cas), à quoi elle sert,
    ce qui est copié (données avec avis, règle reprise comme fait) ou seulement lu, état du réglage lié et ce qu'il coupe quand il est
    partiel, exemples de fichiers ; boutons « Copier le lien », « Copier la licence » et l'adresse du dépôt ; filtre.
  - Mention SQUARE ENIX (FINAL FANTASY XIV © SQUARE ENIX CO., LTD.), avec la politique d'usage des contenus du jeu en lien.
- Une seule liste dans le code (Credits/CreditCatalog.cs) ; trois tests la gardent alignée :
  - chaque donnée intégrée et chaque hôte réseau écrit dans le code a son crédit ;
  - fichiers et réglages cités existants, noms et auteurs identiques dans toutes les langues ;
  - lien de licence selon la licence, avis MIT complet pour chaque donnée MIT copiée ;
  - une référence n'est jamais marquée copiée.
- Avis de licence complétés (exigés par MIT pour une copie) : texte MIT complet de Character Panel Refined en tête de
  Character/StatFormulas.cs, et de FFXIVGachaSpreadsheet dans submarine-loot.json (champ « License », aussi écrit par
  tools/submarine-loot.py).
- README : les deux sections Crédits renvoient à la page, portent la mention SQUARE ENIX et sont corrigées (Mappy et SortaKinda
  AGPL-3.0-or-later, licence d'Argus non confirmée, règle de frozen_rabbit_tome reprise comme fait).
- Revue (relecteur, puis contre-vérificateur adverse) : 14 remarques, toutes confirmées et corrigées.
  - Réglages partiels annoncés comme complets : le comportement réseau n'a pas changé, la décision est reportée au point d.
  - Noms et auteurs en français dans l'interface anglaise.
  - Avis MIT manquants.
  - Jeu présenté « sans licence » au lieu de « propriétaire ».
  - Textes « jamais copié » contradictoires.
  - Pied de page vers une page du mode développeur.
  - Textes coupés dans les colonnes.
  - Recalcul de la liste à chaque image.
  - Licence d'Argus affirmée sans preuve.
  - Table du Mini Cactpot non mentionnée.
  - Liste de fichiers présentée comme complète.
  - README désaligné.
  - Lien du guide en français.
- Localisation : méthode Has (texte facultatif avec repli). Tests : 4 nouveaux.
- Verify.ps1 (agent) : 584/584, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.320.0.0. Non validé en jeu.
### English
- R-13: new Credits page (Preferences › Credits) listing everything Tensho uses (72 entries from a multi-agent inventory with each
  licence read at its source): the game (SQUARE ENIX notice), libraries, embedded data, online data (with the tied setting and what a
  partial one turns off), rules taken as facts, references read but never copied, and build tools; filter and copy-link buttons.
  One list in code (Credits/CreditCatalog.cs) kept in step with the code by tests (embedded data, network hosts, settings, MIT notices).
  Full MIT notices added for Character Panel Refined and FFXIVGachaSpreadsheet data; README credits aligned.

## 0.319.0
- Demande (R-12) : « prévois dans une todo list un hook Discord pour envoyer les alarmes avec message sur un serveur Discord ; voir
  plus si possible ou intéressant ». Sauvegarde avant modification : checkpoints/backup-20261102-104125-avant-r12-discord.zip.
- Nouveau : Réglages › Alarmes › **Discord**, désactivé par défaut.
  - Adresse du webhook saisie par le joueur, masquée. Seules les adresses de webhook des hôtes de Discord sont acceptées
    (discord.com, ptb, canary, discordapp.com ; HTTPS, port par défaut, ni requête ni fragment) ; une autre reste dans le champ avec un
    message, sans être enregistrée.
  - Quand une alarme Tensho sonne, son message part dans le salon : modèle réglable (repères [label], [minutes], [time], [character]),
    nom de l'expéditeur, catégories (récolte, pêche, rappels, compagnie libre, autres), nom du personnage seulement sur option, mention
    d'un rôle (identifiant ou mention collée). « allowed_mentions » ne laisse passer que ce rôle : @everyone ou @here dans un nom
    d'alarme ne notifie personne.
  - Page Alarmes : case « Discord » et message propre à chaque groupe ; une alarme qui rejoint un groupe en reprend les réglages.
  - Bouton « Envoyer un test », ligne d'état (envoyé, refusé avec le code, Discord indisponible, limite, réseau) et compteurs.
  - Les alarmes écrites dans la liste du jeu sonnent toujours dans le jeu, et partent aussi sur Discord.
- Envoi (Alarms/DiscordNotifier.cs) sur un worker, jamais sur le thread du jeu ni dans Draw :
  - file de 20 messages au plus et deux secondes entre deux messages ;
  - « 429 » : attente de Retry-After jusqu'à 60 s ; au-delà, le message est abandonné et la file attend la pause demandée ;
  - pas de nouvel essai après un délai dépassé ni une erreur 5xx (un POST pourrait être publié deux fois), un seul après une connexion
    qui n'a pas pu s'ouvrir ;
  - « Effacer » l'adresse, la changer, couper l'envoi ou « Tout remettre par défaut » abandonne les messages en attente et l'envoi en
    cours ;
  - User-Agent du plugin, sans donnée personnelle.
- Secret : l'adresse n'est jamais écrite dans les journaux, dans le rapport de diagnostic (seulement « webhook valid/invalid/empty »)
  ni dans l'état affiché. « Tout remettre par défaut » coupe l'envoi mais garde l'adresse, et l'infobulle le dit maintenant.
- Catégories : les alarmes de poisson de la prévision de pêche portent maintenant le métier 18 ; une alarme d'objet sans métier le
  prend dans le catalogue de récolte ; les rappels et retours de vaisseaux d'avant 0.319 sont reconnus une fois par leur groupe.
- Correctif au passage : changer un réglage d'alarme remettait le délai des commandes « /alarm » (Réglages › Avancé) à 500 ms.
- Revue multi-agents (justesse et fils, sécurité et règles Dalamud, interface et textes), puis contre-vérification adverse :
  28 remarques, 23 confirmées et corrigées, 5 réfutées.
  - Défauts corrigés : worker qui pouvait s'arrêter sur une erreur imprévue, messages encore envoyés après « Effacer », doublons
    possibles, alarmes de pêche rangées dans « Autres », message de groupe pris à moitié tapé, chiffres Unicode acceptés dans
    l'identifiant de rôle, textes inexacts.
  - Documents de publication mis à jour : SECURITY, PUBLISHING et le brouillon de PR D17 déclarent l'envoi Discord.
- Règles de l'utilisateur notées pendant ce point :
  - les tests en jeu de tout le plugin se feront à la fin, guidés (étape h) ;
  - les décisions sont gardées pour la fin, avec un choix provisoire noté (ROADMAP « Décisions reportées à la fin ») ;
  - R-13, page « Crédits » complète, vient juste après ;
  - R-14, résistance aux mises à jour de FF14 directement dans la logique du code : règle ajoutée à docs/ai/RULES.md pour tout
    nouveau code, passage complet sur l'existant vers la toute fin de la file (étape g2, avant les tests en jeu).
- Tests : 11 nouveaux (adresses, message, mentions, catégories, nouvelles tentatives, pause demandée, file, « Effacer », worker qui
  survit, identifiants de rôle, sources des anciennes alarmes) ; TESTING.md : liste de contrôle en jeu du webhook.
- Un test de la file comptait mal le message en cours d'envoi (instable) : corrigé, puis 8 passages de suite verts.
- Verify.ps1 (agent) : 580/580, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.319.0.0. Non validé en jeu.
### English
- R-12: optional Discord webhook for Tensho alarms (Settings › Alarms › Discord, off by default): Discord hosts only, masked address
  never logged or reported, message template, categories, per-group switch and message, optional character name and role mention
  (mentions otherwise disabled), test button and status. Posts leave on a worker (queue of 20, 2 s apart, Retry-After honoured, no
  retry that could double-post); clearing the address or turning it off drops queued posts. Also fixes the /alarm command delay being
  reset by other alarm settings. Multi-agent review: 23 confirmed findings fixed.

## 0.318.0
- Demande (file c) : « prévois aussi un plan pour ajouter à notre plugin ce qui manque par rapport aux plugins dispo », élargie à
  « pas que plugin de référence mais ceux du dépôt Dalamud (voir repo non officiel si ça respecte les règles Dalamud upload) », puis
  « ajoute une phase décision avant aligner avec roadmap pour que je choisisse ce qu'on rajoute ».
- Comparaison en lecture seule par 11 agents :
  - règles de publication relevées sur dalamud.dev et le dépôt D17 ;
  - dépôt officiel complet : 500 plugins, dont 177 dans un domaine d'aide non combat de Tensho ;
  - dépôts non officiels : 658 plugins, 76 gardés en tout ou en partie, jugés règle par règle ;
  - chaque fonction cherchée dans le code de Tensho, licence lue dans environ 250 dépôts ;
  - contre-vérification adverse : 20 affirmations de couverture corrigées, 5 noms de structures du jeu rectifiés par sonde,
    portée de licence d'Allagan Tools (AllaganLib sans licence), tri automatique classé en zone grise.
  Résultat : docs/project/GAPS.md (103 manques, plugins sources, donnée du jeu, licence, conformité, taille, valeur) et
  docs/evidence/0.318-plugin-gaps.json (données brutes).
- Phase de décision faite dans la conversation (la page de choix publiée n'a pas servi) : 101 manques retenus, I-14 et X-05 plus
  tard ; ordre domaine par domaine (Inventaire et marché → Collections → Monde → Récolte → Artisanat → Nouveaux domaines) ; choix
  précis tranchés (colonne « Choix » de GAPS.md) ; licence EUPL-1.2 acceptée comme compatible. Décision DEC-025 ; nouvelle étape g)
  de la file (ROADMAP, STATUS) ; BACKLOG, USER-REQUESTS, CONTRIBUTING et l'index des docs alignés.
- Autres demandes notées pendant ce point :
  - R-12, webhook Discord pour les alarmes, placé juste après c) (USER-REQUESTS, ROADMAP, STATUS) ;
  - amélioration du dépôt git et de GitHub, rattachée au point d) ;
  - règle de numérotation des versions (DEC-024) : X = grandes étapes (1.0.0 = première publication Dalamud), Y = nos versions,
    Z = versions de la communauté ; écrite dans CLAUDE.md et CONTRIBUTING.md.
- Aucun changement de code. Verify.ps1 (agent) : 570/570, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.318.0.0.
  Non validé en jeu.
### English
- Gap plan against the 17 reference plugins, the whole official Dalamud repository (500 plugins) and rule-compliant unofficial
  plugins (658 seen): docs/project/GAPS.md (103 gaps with sources, game data, licence, D17 compliance) plus raw evidence. The user
  chose 101 of them (DEC-025), done domain by domain as queue step g); EUPL-1.2 accepted as GPL-compatible. Also recorded: R-12
  Discord webhook for alarms (next), git/GitHub improvements in step d), and the version numbering rule (DEC-024).

## 0.317.0
- Demande : « D'abord les décisions » sur les points « À décider / à revoir » de la feuille de route (0.316), avant la suite de la
  file. Douze questions posées, réponses de l'utilisateur inscrites dans docs/project/DECISIONS.md (DEC-023) :
  - sous-marins : seuils gardés, 19 secteurs signalés « non confirmé par les relevés » ; prix du marché (Universalis) et paliers
    de surveillance / récupération (si la source le permet) dans la valeur des trajets ;
  - dirigeables : arbre de déblocage de LuminaSupplemental (GPL-3.0) embarqué avec avis de licence ; planificateur de trajets par
    distance et durée, butin « inconnu » (DEC-021 modifiée en conséquence) ;
  - matérias : taux de surmatérialisation lu dans les données du jeu ; R-11 étape 4 : barde de chocobo et escouade ; rappels de
    l'escouade et des Planeurs d'Ehcatl : sonde ; R-09 après la file ;
  - publication D17 à la version 1.0.0 (décision de l'auteur) ; cadres ULD avant D17 ; traductions allemande et japonaise gardées ;
  - textes français au vouvoiement.
- Feuille de route : section « À décider / à revoir » réduite aux points qui attendent un relevé en jeu ou une étape de la file,
  nouvelle section « Décidé le 2026-11-02 » ; nouvelle étape f) de la file (suite des décisions, ordre fixé). STATUS, R-11,
  PUBLISHING et USER-REQUESTS alignés ; les points de la revue 0.316 sont rattachés à l'étape d). `FilteredTable` retiré du plan
  (choix de l'agent : pas d'abstraction sans besoin répété).
- Vouvoiement : 14 textes français passés du « tu » au « vous » (Triple Triade, Titres et Succès, servants et sacoche, Quêtes,
  ALEA, butin des missions) ; nouveau test « French texts address the player with « vous » » qui refuse tu / ton / ta / tes / toi
  dans fr.json.
- Verify.ps1 (agent) : 570/570, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.317.0.0. Non validé en jeu.
### English
- Roadmap decisions recorded (DEC-023): flag 19 unconfirmed submarine breakpoint sectors; add market prices and survey tiers to
  route value; embed the GPL-3.0 airship unlock tree; airship route planner by distance and duration only; overmeld success rate
  from game data; chocobo barding and squadron gear; probe squadron / Ehcatl timers; seasonal events after the queue; Dalamud
  submission at 1.0.0; ULD frames before it; German and Japanese kept. French texts now consistently use « vous » (14 strings, new
  test).

## 0.316.0
- Demande : faire le tour des .md et des descriptions du code des outils pour les mettre à jour, et compléter la feuille de route
  avec les points indécis à revoir et les tests à faire. Sauvegarde avant modification :
  checkpoints/backup-20261102-045314-avant-revue-docs.zip.
- Travail fait en parallèle par huit agents, chacun sur ses propres fichiers, avec vérification de chaque affirmation dans le code.
  Un agent critique a ensuite contre-vérifié l'ensemble : il a corrigé 10 erreurs et confirmé que les fichiers .cs ne diffèrent
  que par des commentaires et que tous les liens existent.
  - README (deux langues), docs/README, PROJECT, CONTRIBUTING, SECURITY, Localization/README, PUBLISHING et brouillon de PR D17 :
    fonctions, commandes, options de Verify, crédits et licences à jour.
  - ARCHITECTURE : carte des modules (Compagnie libre, Équipement, Rappels, marché, modèles communs, essayage…), flux de données,
    workers, ressources embarquées, configuration et KeptOnReset.
  - REFERENCES et DECISIONS : sources étudiées depuis 0.235 (SubmarineTracker, FFXIVGachaSpreadsheet, Argus,
    HimbeertoniRaidTool, Glamourer, xivgear.app…) et décisions de la période.
  - TESTING : inventaire des tests automatiques (569) et liste de contrôle en jeu pour chaque version non validée.
  - ROADMAP, BACKLOG, USER-REQUESTS et GOAL : état des demandes R-01 à R-11, file d'attente, et deux nouvelles sections :
    **« À décider / à revoir »** (18 points) et **« Tests à faire en jeu »**.
  - Descriptions des pages dans le plugin (fr / en), corrigées d'après le code : Accueil, Revue de mode, « Tout remettre par
    défaut »… ; en-têtes de description en tête de 86 fichiers d'outils (commentaires seulement).
- Descriptions des outils dans le code, signalées par le critique :
  - annuaire « Tous les outils » : six entrées ajoutées (Équipement, Compagnie libre, Marchands PNJ, Météo, Alarmes, Rappels),
    chacune avec son bouton « Ouvrir » ;
  - tuiles de l'accueil : « Enchaînement de macros » et « Personnage », au lieu des anciens noms ;
  - fiche contributeur : fichiers source exacts par page (Monstres, Mage bleu, Équipement, Compagnie libre…), liste des dossiers
    complète, sources SubmarineTracker et FFXIVGachaSpreadsheet.
- STATUS : la dernière release est Stable-0.242.0 (tag git vérifié) ; GOAL R-11 ; liste « à valider en jeu » renvoyée à la
  feuille de route ; couverture de Character Panel et de Macro Chain mise à jour. CLAUDE.md : règle de sauvegarde zip avant toute
  grosse modification (demande de l'utilisateur).
- Verify.ps1 (agent) : 569/569, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.316.0.0. Non validé en jeu.
### English
- Documentation pass (8 agents + critic, every claim checked in code): README, CONTRIBUTING, SECURITY, ARCHITECTURE, REFERENCES,
  DECISIONS, TESTING (in-game checklist), ROADMAP (« to decide / review » and « in-game tests »), page descriptions (fr/en), file
  header comments. Tool directory gains Gear, Free company, NPC vendors, Weather, Alarms and Reminders; home tile names and
  contributor source map fixed; STATUS aligned (last release Stable-0.242.0).

## 0.315.0
- Retour en jeu : 0.314 (sources et possession) validée par l'utilisateur.
- Demande R-11 étape 3 : montages de sous-marins et de dirigeables « dans le même style » que la page Équipement. Recherche faite
  par plusieurs agents (données du jeu, SubmarineTracker, dirigeables, code Tensho), contre-vérifiée par un agent critique ;
  preuves : docs/evidence/0.315-vessel-builds.md.
  - Compagnie libre, nouvel onglet **Montages** :
    - on choisit un vaisseau lu dans l'atelier (son montage lu sert de départ) ou une saisie manuelle, et un rang ;
    - les statistiques sont à gauche : lues, prévues (pièces + bonus de rang), écart, coût / capacité, rang des pièces,
      réparation et nom court communautaire (« SSUC++ ») ;
    - les quatre pièces sont à droite, comme dans la page Équipement : icône, nom, rang, cadre doré si le plan diffère ; un clic
      liste les pièces de l'emplacement avec ce qu'elles changent ;
    - ⚠ si les statistiques lues ne valent pas la somme calculée (contrôle de la règle, non encore vérifiée en jeu) ;
    - un plan est gardé par vaisseau.
  - Sous-marins, section **Trajet** (bouton « Montage » d'un trajet dans Meilleurs trajets, ou « Trajet en cours ») :
    - distance contre autonomie, durée avec la vitesse prévue ;
    - paliers atteints d'exploration, de prélèvement et de faveur, d'après les seuils communautaires ; « inconnu » si un secteur
      manque, jamais 0 ;
    - usure estimée (nombre de voyages avant réparation) ;
    - **Meilleurs montages pour ce trajet** : les 10 000 combinaisons au plus sont essayées sur un worker, avec les paliers voulus,
      l'autonomie et la capacité ; les plus rapides d'abord, puis la réparation la moins chère ; bouton « Appliquer ».
  - Meilleurs trajets :
    - nouveau filtre « Secteurs débloqués seulement » (activé par défaut ; les secteurs sont lus dans l'atelier avec
      IsSubmarineExplorationUnlocked) ;
    - bouton « Montage prévu » pour reprendre le rang, la vitesse et l'autonomie du plan ;
    - bouton « Montage » sur chaque trajet.
  - Atelier : Tensho lit maintenant aussi les pièces des vaisseaux, les statistiques des dirigeables et les secteurs débloqués.
  - Dirigeables : statistiques, capacité et rang des pièces seulement. Aucune donnée de seuils ni d'arbre de déblocage n'existe
    sous une licence compatible.
- Correctifs trouvés pendant la recherche :
  - « Tout remettre par défaut » effaçait les vaisseaux lus, les plans d'équipement et les plans de matérias : ils sont gardés
    maintenant, comme les nouvelles données ;
  - les recherches de trajets et de montages sont annulées à la fermeture du plugin.
- Données : seuils de 143 secteurs copiés de SubmarineTracker (MIT, avis de licence dans le fichier, crédits README).
- Sauvegarde avant modification : checkpoints/backup-20261102-043419-pendant-0.315.zip (demande de l'utilisateur).
- Revue adversariale à plusieurs agents (4 angles, chaque point contre-vérifié) avant livraison : 14 points confirmés (9
  distincts), tous corrigés.
  - Le filtre des secteurs débloqués utilise maintenant la compagnie du sous-marin choisi, et non toutes les compagnies réunies ;
    une compagnie non lue = pas de filtre.
  - Le bouton « Montage » ouvre toujours un sous-marin : le vaisseau du planificateur, celui déjà choisi, ou le sous-marin manuel
    au rang du planificateur. Avant, il pouvait ouvrir un dirigeable, sans trajet affiché.
  - Une seule infobulle par ligne de trajet.
  - Le rang d'un plan suit le rang lu du vaisseau quand celui-ci monte.
  - Un trajet au-dessus du rang est signalé, et aucun montage n'est proposé pour lui.
  - Textes : « Recherche des montages… », aide du planificateur mise à jour, « d'Elena ».
  - Preuve corrigée : liste exacte des secteurs dont le seuil embarqué est sous les observations.
- Test ajouté : statistiques, capacité, rang des pièces, rangs, seuils de trajet et inconnus, paliers, usure, noms de montage,
  meilleurs montages, aller-retour du plan, fichier embarqué ; puis les correctifs de la revue (secteurs débloqués par
  compagnie, rang du plan, rang du trajet).
- Verify.ps1 (agent) : 569/569, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.315.0.0. Non validé en jeu.
### English
- Free company « Builds » tab (R-11 step 3): submarine and airship builds in the Gear page style (read / planned / difference stats,
  cost vs capacity, part rank, repair, community build name, part picker), a mismatch check of read vs computed stats; submarine
  route check (range, duration, breakpoint tiers from SubmarineTracker's MIT table, wear estimate) and best builds for the route.
  Route planner: unlocked-sectors filter (read in the workshop), « planned build » and « Build » buttons. Fix: « reset all » no
  longer erases saved vessels and gear / materia plans; searches are cancelled on unload.

## 0.314.0
- Retour en jeu : 0.310 à 0.313 (page Équipement, mirage, matérias, plafonds) validées par l'utilisateur.
- Demande R-11 étape 2 : sources et possession. Page Équipement :
  - ✓ / ✗ devant chaque pièce de la liste de choix et sur les pièces prévues : ✓ si elle est portée ou présente dans un rangement
    connu (sacs, armurerie, coffre à apparence, armoire, sacoche, servants : les mêmes comptes que l'infobulle des objets et la
    Banque). L'infobulle dit où, ou que certains rangements ne sont pas encore importés.
  - Nouveau filtre « Seulement ce que je possède » (infobulle, ↺, désactivé par défaut).
  - Nouvelle section « Pièces à obtenir (N) » sous l'équipement : les pièces du plan (mirage compris) que vous n'avez pas, chacune
    avec ses groupes de sources (marchands, artisanat, récolte, donjons, quêtes…, comme le menu Sources). Un clic ouvre la liste ;
    chaque ligne mène à l'outil concerné, avec un bouton Drapeau quand la source a une position.
- Verify.ps1 (agent) : 568/568, 0 avertissement, 0 erreur ; DLL 0.314.0.0. Non validé en jeu.
### English
- Gear page: owned / not owned marks (worn or in known storages, with where), an « Only what I own » filter, and a « Pieces to
  obtain » section listing the plan's missing pieces with their source groups (vendors, crafting, duties…, flags when placed).

## 0.313.0
- Demande : prévenir quand on essaie de poser une matéria qui dépasse le plafond. Dans l'éditeur de matérias :
  - les statistiques et les grades qui dépasseraient le plafond de la pièce sont écrits en orange, avec « dépasse le plafond :
    N perdus » ;
  - une matéria déjà posée qui dépasse affiche ⚠ et le nombre de points perdus ; l'infobulle donne le plafond et conseille un
    grade plus bas ou une autre statistique.
  - Le calcul suit l'ordre des emplacements, comme en jeu : les matérias des emplacements précédents sont comptées d'abord.
- Verify.ps1 (agent) : 568/568, 0 avertissement, 0 erreur ; DLL 0.313.0.0. Non validé en jeu.
### English
- Materia editor warns before going over a piece's cap: options that would waste points are shown in orange with the loss, and a
  melded materia over the cap shows ⚠ with the points lost.

## 0.312.0
- Demande : une fonction pour les matérias, sur l'équipement porté et sur l'équipement à l'essai. Page Équipement :
  - Sous chaque pièce, ses emplacements de matéria (pleins = matéria posée ; orange = surmatérialisation). L'infobulle liste les
    matérias et ce qui est perdu au plafond. Un clic ouvre l'éditeur : statistique et grade par emplacement (seules les
    statistiques du type de job sont proposées ; en surmatérialisation, les grades VI, VIII, X et XII ne le sont pas). L'éditeur
    montre le gain réel une fois le plafond appliqué et les points perdus. Boutons : « Reprendre les matérias portées »,
    « Tout enlever », « Revenir aux matérias de la pièce ».
  - Les statistiques prévues comptent les matérias plafonnées par pièce : la pièce portée garde les siennes, sauf si on en prévoit
    d'autres ; une pièce prévue n'en a pas, sauf celles qu'on lui pose.
  - Demande (pendant le travail) : « ne pas oublier les plafonds de chaque équipement pour chaque statistique ». Ils sont appliqués
    par pièce et par statistique dans les calculs, et maintenant affichés : l'infobulle des emplacements et l'éditeur montrent un
    tableau « statistique · valeur / plafond · place restante » (doré au plafond).
  - Le partage emporte les matérias (« emplacement=objet:matéria.matéria ») ; les anciens codes restent lisibles.
- Données du jeu : plafond = ItemLevel × pourcentage de l'emplacement (BaseParam) / 1000. Vérifié : il est égal à la plus haute
  statistique secondaire de la pièce (111 au NO 560, 374 au NO 730). Valeurs des matérias tirées de la feuille Materia.
  docs/evidence/0.312-materia.md.
- Test complété : plafonds, perte, surmatérialisation, valeurs des matérias, code de partage avec matérias.
- Verify.ps1 (agent) : 568/568, 0 avertissement, 0 erreur ; DLL 0.312.0.0. Non validé en jeu.
### English
- Gear page materia: sockets under every piece (worn or planned), an editor per socket (stat and grade, overmeld rules), gains
  capped per piece (ItemLevel × slot percentage, checked against real items), planned stats and shared builds include materia.

## 0.311.0
- Retour en jeu sur 0.310 (capture) : la page Équipement fonctionne.
- Demande : un filtre pour ajouter l'équipement des autres jobs à essayer, « on peut mirager tout le stuff maintenant (vérifie) ».
  - Vérifié : depuis le patch 7.4 (décembre 2025), toute armure ou tout accessoire peut servir de mirage, quels que soient le job
    et le niveau ; les armes et les outils restent liés au job (sources : PC Gamer, Game Informer, communiqué 7.4).
  - Nouveau filtre « Tous les jobs (mirage) » (infobulle, ↺, désactivé par défaut) : les listes montrent alors les armures et
    accessoires de tous les jobs et de tout niveau ; les armes restent celles du job.
  - Une pièce qu'on ne peut pas porter (autre job ou niveau plus élevé) est marquée « Mirage ». On peut l'essayer et la partager ;
    les statistiques gardent la pièce portée.
- Retour (capture) sur la disposition : statistiques à gauche, puis les deux colonnes d'équipement côte à côte. Les statistiques
  sans rapport avec le job sont masquées (plus de PR / PS sur un job de combat), et les valeurs dérivées sont alignées sur les
  mêmes colonnes que les statistiques.
- Correctif : « Essayer le plan » ajoutait les pièces de mirage à chaque image (accolades manquantes), corrigé avant livraison.
- Test complété : tous les jobs pour le mirage, armes toujours liées au job, pièces « mirage seulement ».
- Verify.ps1 (agent) : 568/568, 0 avertissement, 0 erreur ; DLL 0.311.0.0. Non validé en jeu.
### English
- Gear page: « All jobs (glamour) » filter — since patch 7.4 any armor or accessory can be a glamour whatever the job and level,
  weapons stay job-locked; such pieces are marked « Glamour » (try on, share; stats keep the worn piece). Stats now on the left,
  both gear columns side by side, only the job kind's stats, derived values in the same columns.

## 0.310.0
- Demande R-11 (nouvelle, plan : docs/goals/R-11.md) : planificateur d'équipement pour le personnage, dans le style de la
  fenêtre Personnage ; essayage de l'équipement, partout où l'apparence compte ; partage des builds. Étape 1 faite :
  - Page **Équipement** (catégorie Progression) : l'équipement porté à gauche et à droite, comme dans le jeu (pièce prévue
    encadrée en doré). Au milieu, les statistiques actuelles (lues dans le jeu), prévues (actuelles + plan − porté), l'écart, et
    pour les jobs de combat les valeurs dérivées. En tête, le niveau d'objet moyen porté et prévu.
  - Un clic sur un emplacement liste l'équipement du job proche de son niveau, du meilleur niveau d'objet au plus bas, avec ce que
    chaque pièce change, et une recherche. Filtres réglables (infobulle, ↺, aussi dans Paramètres) : niveaux en dessous (5), niveau
    exact hors paliers 50 / 60 / 70 / 80 / 90 / 100 (activé), niveau d'objet minimum, HQ supposé. Un plan est gardé par job.
  - **Essayer** : bouton « Essayer le plan », et « Essayer (cabine d'essayage) » dans le menu clic droit de tout équipement
    affiché par Tensho (Revue de mode, objets, butins…). Ces deux entrées ouvrent la cabine d'essayage du jeu.
  - **Partager** : copie la liste lisible du build et un code « TENSHO-GEAR1: » ; **Importer** lit un code dans le presse-papiers.
- Sources : données du jeu uniquement (Item, EquipSlotCategory, ClassJobCategory, BaseParam, Materia, équipement porté), voir
  docs/evidence/0.310-gear-sheets.md. Références lues : HimbeertoniRaidTool (MIT), Glamourer (Apache-2.0), xivgear.app (sans
  licence : lu seulement).
- Test ajouté : fenêtre de niveaux, choix par job et emplacement, bagues, HQ, matérias, statistiques prévues, niveau moyen, codes.
- Verify.ps1 (agent) : 568/568, 0 avertissement, 0 erreur ; DLL 0.310.0.0. Non validé en jeu.
### English
- New Gear page (R-11 step 1): character-window style gear planner — worn slots, real / planned / difference stats, candidates of
  the job near its level with filters (levels below, exact level outside caps, minimum item level, assumed HQ), one plan per job;
  « Try on » (the game's Try On window) from the plan and from every equipment menu in Tensho; build sharing by code.

## 0.309.0
- Demande : prévoir les meilleurs trajets de sous-marin et de dirigeable, « surtout le butin », en s'appuyant sur les plugins
  existants. Page Compagnie libre, nouvel onglet **Meilleurs trajets** :
  - on choisit un sous-marin lu dans l'atelier (rang, vitesse et autonomie lus dans le jeu, modifiables) ou une saisie manuelle,
    une zone et un tri : valeur par voyage, valeur par jour (voyages enchaînés) ou expérience par jour ;
  - Tensho essaie tous les ensembles de 1 à 5 secteurs que le rang et l'autonomie permettent, chacun dans l'ordre le plus court,
    sur un worker, et affiche les 25 meilleurs : trajet, distance / autonomie, durée, valeur, gils par jour, expérience ;
  - le survol d'un trajet montre le butin moyen d'un voyage, objet par objet, avec sa valeur.
- Sources : le butin n'est pas dans les données du jeu. Les moyennes viennent de FFXIVGachaSpreadsheet (MIT, relevés
  communautaires), embarquées dans la DLL (24 Ko, `tools/submarine-loot.py`). La valeur est le prix de revente au PNJ (données
  du jeu). Les formules de durée et de distance ont été comprises en lisant SubmarineTracker (MIT) ; le code est original.
  Crédits dans le README. Pas de relevé public pour les dirigeables : pas de planificateur pour eux.
- Contrôle sur les vraies feuilles : sur la Mer des noyades, les trajets OJ, ROJ et JORZ arrivent en tête, les trajets rentables
  connus de la communauté (docs/evidence/0.309-submarine-routes.md).
- Vaisseaux lus : on garde aussi les secteurs du trajet et les statistiques du sous-marin (surveillance, récupération, vitesse,
  autonomie, faveur).
- Test ajouté : distance, temps, ordre le plus court, autonomie, rang, valeur, lecture du fichier embarqué.
- Verify.ps1 (agent) : 567/567, 0 avertissement, 0 erreur ; DLL 0.309.0.0. Non validé en jeu.
### English
- Free company page, new « Best routes » tab: best submarine routes for a vessel's rank, speed and range, ranked by expected loot
  value (community averages from FFXIVGachaSpreadsheet, MIT, embedded; NPC sell prices from the game), value per day or
  experience; formulas understood from SubmarineTracker (MIT), original code. Checked: OJ / ROJ / JORZ come first on the first map.

## 0.308.0
- Retour (capture) : le niveau de zoom sous la colonne d'outils était presque invisible sur la carte (option « Menus et outils
  par-dessus la carte »). Il a maintenant un fond sombre arrondi, comme les noms de zone, et un texte clair.
- Verify.ps1 (agent) : 566/566, 0 avertissement, 0 erreur ; DLL 0.308.0.0. Non validé en jeu.
### English
- Map overlay: the zoom level gets a dark backdrop and light text, like the zone names.

## 0.307.0
- Demande R-07 (compagnie libre) : nouvelle page **Compagnie libre** (catégorie Monde). Dans l'atelier de la compagnie, Tensho lit
  toutes les 3 s les sous-marins et dirigeables (nom, rang, expérience vers le rang suivant, date d'enregistrement, retour, trajet
  des sous-marins, butin du dernier voyage) et les garde dans la configuration (le jeu ne les donne que dans l'atelier ; une
  lecture qui ne change rien n'est pas réenregistrée). La page affiche par type : rang + barre d'expérience, trajet (noms
  `SubmarineExploration`), retour (temps restant / « Rentré » / « Au port »), butin (clic, icônes d'objets interactives) et un
  bouton d'alarme au retour. La page Rappels gagne la ligne « Expéditions de la compagnie » (en mer / rentrés, prochain retour,
  alarme, bouton « Détails »).
- Preuve : docs/evidence/0.307-fc-voyages.md (structures et feuilles sondées). Test ajouté (état, expérience, fusion par maison,
  détection de changement, prochain retour).
- Verify.ps1 (agent) : 566/566, 0 avertissement, 0 erreur ; DLL 0.307.0.0. Non validé en jeu.
### English
- New Free company page (R-07): submarines and airships read in the company workshop and kept (rank, experience, route, return,
  last finds), with return alarms; a « Company voyages » line on the Reminders page.

## 0.306.0
- Retour : 0.305 validée en jeu par l'utilisateur, sauf les boutons et menus par-dessus la carte qui ne répondaient toujours pas.
  Cause : un bouton enfoncé devient « l'élément actif » d'ImGui ; le test « souris sur la fenêtre » qui affiche les outils ne le
  comptait pas, donc les outils et menus disparaissaient entre l'appui et le relâchement, et le clic (qui part au relâchement)
  n'arrivait jamais. Correction : le test accepte l'élément actif (AllowWhenBlockedByActiveItem), de même pour la détection de
  la souris sur les outils et sur les noms.
- Verify.ps1 (agent) : 565/565, 0 avertissement, 0 erreur ; DLL 0.306.0.0. Non validé en jeu.
### English
- Map overlay buttons and menus: the hover test ignored the active (pressed) item, so the overlays vanished between press and
  release; it now allows the active item.

## 0.305.0
- Retour (captures) sur « Menus et outils par-dessus la carte » :
  - **Aucun bouton ne répondait** : la carte était posée avant les outils et prenait le clic. Les outils et les noms sont
    maintenant posés avant la carte (ils prennent le clic en premier, tout en restant dessinés par-dessus) et la carte ignore la
    souris quand elle est sur eux.
  - Les noms (région, zone, étage) ne sont plus sous la colonne d'outils mais **à sa droite**, en haut ; la colonne commence en
    haut et ne dépend plus du nombre de lignes de noms.
  - Le **bandeau sombre suit le texte** : sombre sous les noms, puis dégradé vers la droite et vers le bas (plus toute la largeur).
  - **Nom de la zone toujours affiché** (nouveau réglage, Paramètres de la carte › Affichage, activé par défaut, infobulle et ↺) :
    quand la souris n'est pas sur la carte, le nom de la zone affichée (et la sous-zone) reste écrit en haut.
  - La **colonne d'outils s'adapte à la fenêtre** (avec ou sans l'option) : le curseur de zoom raccourcit, puis disparaît, puis les
    boutons passent sur plusieurs colonnes ; le **% de zoom** reste toujours visible (infobulle) et laisse la place aux
    coordonnées écrites en bas de la carte.
- Verify.ps1 (agent) : 565/565, 0 avertissement, 0 erreur ; DLL 0.305.0.0. Non validé en jeu.
### English
- Map overlay: buttons clickable again (overlays submitted before the map, which ignores the mouse over them); names to the right
  of the tool column; dark band sized to the text; new "zone name always shown" setting; the tool column fits the window
  (shorter slider, then none, then several columns) and always shows the zoom %.

## 0.304.0
- Retour (captures, carte du jeu en exemple) sur l'option « Menus et outils par-dessus la carte » : les menus du haut ne sont plus
  des listes déroulantes encadrées. Comme la carte du jeu, la région, la zone et l'étage s'affichent l'un sous l'autre, en texte
  doré détouré, avec une flèche ; un clic déroule leur liste **vers le bas** (« > nom », le choix actuel en doré, ailes de vol
  pour les zones) et un clic ailleurs la referme. Opacité : le bandeau sombre de 0.303 était dessiné sous la carte, donc
  invisible ; il est maintenant dans sa propre couche (sombre derrière les noms puis dégradé vers transparent), sans capter la
  souris : la carte se fait toujours glisser dessous.
- Verify.ps1 (agent) : tous les tests verts (0 échec), 0 avertissement, 0 erreur ; DLL 0.304.0.0. Non validé en jeu.
### English
- Map overlay header like the game map: region, zone and floor names stacked in outlined gold text, each unrolling its list
  downward; the dark gradient band (hidden under the map in 0.303) is now visible and does not block dragging.

## 0.303.0
- Demande (captures de la carte Tensho et de la carte du jeu) : option « Menus et outils par-dessus la carte » (Paramètres de la carte
  › Affichage, infobulle et ↺, désactivée par défaut). La carte occupe alors toute la fenêtre ; la colonne d'outils de gauche et les
  menus de région / zone du haut s'affichent par-dessus, seulement quand la souris est sur la carte (ou qu'un de ses menus est
  ouvert). Le haut est un bandeau en dégradé (sombre vers transparent) avec les noms en doré et la flèche des menus, comme la
  carte du jeu. La colonne d'outils ne couvre que la hauteur de ses boutons : le reste de la carte se fait glisser normalement.
- Verify.ps1 (agent) : 565/565, 0 avertissement, 0 erreur ; DLL 0.303.0.0. Non validé en jeu.
### English
- New map option: menus and tool column drawn over the map (gradient header like the game map), shown only while the mouse is
  over it, the map using the whole window.

## 0.302.0
- Demande (capture « 🕒 1 · 03:35 ») : la barre du serveur affiche maintenant le **temps restant** avant la prochaine alarme
  (« 2 h 15 », « 45 min », « < 1 min ») au lieu de son heure ; l'infobulle Tensho donne pour chaque alarme le temps restant, puis
  l'heure entre parenthèses.
- Verify.ps1 (agent) : 565/565, 0 avertissement, 0 erreur ; DLL 0.302.0.0. Non validé en jeu.
### English
- The server bar shows the time left before the next alarm instead of its clock time; the tooltip lists time left and clock time.

## 0.301.0
- Demande : l'infobulle de l'icône d'alarme de la barre du serveur doit s'afficher en dessous, et être réglable. L'infobulle du jeu
  passe sous les fenêtres des plugins (la carte Tensho la cachait). Tensho retrouve maintenant le texte de son entrée dans la barre
  (addon « _DTR ») et dessine **sa propre infobulle juste en dessous**, par-dessus toutes les fenêtres : les prochaines alarmes
  (heure et nom) et « Clic : page Alarmes ». Si l'entrée n'est pas retrouvée à l'écran, l'infobulle du jeu reste utilisée.
  Réglages (Paramètres › Avancé, sous l'icône d'alarme) : « Infobulle Tensho sous l'icône » et « Alarmes listées » (1 à 10, 5 par
  défaut), avec infobulles et ↺.
- Verify.ps1 (agent) : 565/565, 0 avertissement, 0 erreur ; DLL 0.301.0.0. Non validé en jeu (la recherche du texte dans la barre
  est à confirmer en jeu).
### English
- The server bar alarm icon gets Tensho's own tooltip drawn just below it (above plugin windows), listing the next alarms; it can
  be turned off and the number of alarms set (1–10).

## 0.300.0
- R-08 (Rappels), d'après le Calendrier du jeu (capture, `docs/evidence/0.300-calendar-resets.md`) : nouvelle page **Rappels** (MONDE)
  avec, pour chaque minuteur, l'état, la prochaine fois (compte à rebours et heure locale), une case « fait » qui se décoche seule à
  la remise à zéro suivante, et une cloche qui crée l'alarme à la prochaine heure (module Alarmes) :
  - missions de ravitaillement (chaque jour 20:00 UTC) ; commandes spéciales avec les livraisons restantes lues dans le jeu, et Revue
    de mode (mardi 08:00 UTC) ; quêtes quotidiennes des peuples alliés avec le nombre restant lu dans le jeu (15:00 UTC) ;
    autorisations de mandats avec leur nombre et leur prochaine heure lus dans le jeu ; cartes au trésor (le jeu ne donne pas
    l'heure : bouton « Carte ouverte », 18 h d'attente) ; tâches de servant (en cours, terminées, prochaine fin ; lues après avoir
    ouvert la liste des servants).
  - Calcul des remises testé (`Reminders/ResetSchedule.cs`) ; compteurs copiés depuis le jeu une fois par seconde
    (`Reminders/GameTimers.Dalamud.cs`).
  - Pas encore : escouade de Grande Compagnie, livraisons des Planeurs d'Ehcatl, expéditions d'aéronef / sous-marin (à relever).
- Demande : **icône d'alarme dans la barre d'informations du jeu** (à côté du nom du serveur) quand au moins une alarme Tensho est
  active : horloge du jeu, nombre d'alarmes et heure de la prochaine ; clic = page Alarmes ; réglage dans Paramètres › Avancé. L'heure
  est dans le texte de la barre, car l'infobulle du jeu passe sous les fenêtres des plugins (la carte Tensho la cachait).
- Verify.ps1 (agent) : 565/565, 0 avertissement, 0 erreur ; DLL 0.300.0.0. Non validé en jeu.
### English
- New Reminders page (game calendar timers: supply missions, custom deliveries, Fashion Report, allied society quests, leves,
  treasure maps, retainer ventures) with resets, game counters, « done » boxes and alarm buttons; alarm clock with count and next
  time in the game's server info bar.

## 0.299.0
- Retour en jeu (capture, clic droit sur un lieu de pêche de la carte) : on pouvait poser le drapeau mais pas le retirer. « Retirer le
  drapeau » est ajouté, quand un drapeau est posé, au menu propre de chaque icône de la carte et aux menus de quête, de PNJ et de
  lieu (le menu général de la carte l'avait déjà).
- Verify.ps1 (agent) : 564/564, 0 avertissement, 0 erreur ; DLL 0.299.0.0. Non validé en jeu.
### English
- « Remove the flag » is offered in every right-click menu that can place it (map icon menus, quest / NPC / place menus).

## 0.298.0
- Retour en jeu (captures, réglages des icônes de la carte) : chaque ligne avait deux cases à cocher semblables, dont la première
  (afficher l'icône) sans infobulle. La première a maintenant son infobulle ; la seconde (menu du clic droit) devient un petit
  bouton souris, doré quand l'icône a son propre menu, gris quand le clic droit passe au menu général, avec une infobulle qui dit
  l'état actif.
- « Mandats et autres événements » (repères au bord) : la séparation demandée attend une preuve. Le journal de Dalamud montre les
  types de marqueurs 0, 3 (quêtes), 4 et 6, mais aucun relevé ne dit encore lequel désigne un mandat ; à relever en jeu avec un
  mandat accepté (aucune valeur devinée).
- Verify.ps1 (agent) : 564/564, 0 avertissement, 0 erreur ; DLL 0.298.0.0. Non validé en jeu.
### English
- Map icon settings: the visibility box has a tooltip and the right-click setting is a gold/grey mouse button instead of a second
  checkbox.

## 0.297.0
- Point décidé avant la publication D17 (« flèches boussole »), adapté aux demandes de l'utilisateur : des icônes plutôt que des
  flèches, pour tous les types de cibles, chacun réglable. Quand une cible de la carte affichée est hors de la vue, **sa propre
  icône** reste sur le bord de la carte, du côté où elle se trouve, avec une petite pointe dorée ; survol = son nom ; clic = la
  carte se centre dessus (votre personnage : la carte le suit à nouveau).
  - Types (Paramètres de la carte › Joueur, un interrupteur chacun avec infobulle et ↺, sous « Repères au bord de la carte ») :
    votre personnage, drapeau, cible indiquée par le jeu (journal), quêtes actives (une icône par quête), mandats et autres
    événements, ALEA en cours, points de récolte à horaire ouverts maintenant (les points ordinaires seraient trop nombreux),
    membres du groupe (icône de leur classe).
- Verify.ps1 (agent) : 564/564, 0 avertissement, 0 erreur ; DLL 0.297.0.0. Non validé en jeu.
### English
- Off-view map targets (character, flag, journal target, active quests, leves and events, FATEs, open timed gathering points,
  party) keep their own icon on the map's edge, on their side; click centres the map; each kind can be switched off.

## 0.296.0
- Retour en jeu (capture, menu de l'inventaire › Échanges) : les échanges contre des sceaux des trois Grandes Compagnies
  s'affichaient. Les sources (menus de clic droit dans Tensho et dans l'inventaire du jeu, liens de sources) ne gardent plus que
  les sceaux de la Grande Compagnie du personnage (lue par Dalamud, `IPlayerState.GrandCompany`) : échanges, et vendeurs payés
  seulement en sceaux d'une autre compagnie. Compagnie inconnue ou aucune : tout reste affiché.
- Verify.ps1 (agent) : 564/564, 0 avertissement, 0 erreur ; DLL 0.296.0.0. Non validé en jeu.
### English
- Item sources only list the Grand Company seals of the character's own company (exchanges and seal-only vendors).

## 0.295.0
- Point décidé avant la publication D17 (BACKLOG, « Faire ») : **boutiques de compagnie libre**. Sonde (`docs/evidence/0.295-fcc-shops.md`)
  de la feuille `FccShop` : objet, prix en crédits de compagnie, rang de compagnie requis ; vendues par les officiers du logement,
  etc. Le catalogue des PNJ vendeurs les lit maintenant : ces objets apparaissent dans l'outil PNJ vendeurs, les sources des objets,
  le clic droit (PNJ vendeurs), la carte. Le prix s'affiche « 1 500 crédits de compagnie (rang 6 de la compagnie) ». Le cache du
  catalogue des vendeurs est relu une fois.
- Verify.ps1 (agent) : 564/564, 0 avertissement, 0 erreur ; DLL 0.295.0.0. Non validé en jeu.
### English
- Free company credit shops (FccShop) are read: their items show in NPC vendors and item sources with the price in company
  credits and the required company rank.

## 0.294.0
- R-06, étape 6 (style) : la police de titre du jeu (TrumpGothic, en plus petit) s'étend aux 78 en-têtes de section repliables de
  tous les modules (tous passent par le modèle `UiComponents.Header`), aux titres des catégories de la barre latérale et à
  « OBJET EN CONTEXTE ». Police normale tant que celle du jeu n'est pas prête.
- Verify.ps1 (agent) : 564/564, 0 avertissement, 0 erreur ; DLL 0.294.0.0. Non validé en jeu.
### English
- The game's title font now also styles every collapsing section header (shared Header template), the sidebar categories and the
  context item label.

## 0.293.0
- R-06, étape 6 (style, ressources du jeu d'abord) : les titres des pages s'affichent dans la police de titre du jeu
  (TrumpGothic, celle des titres de ses fenêtres), fournie par Dalamud depuis les fichiers du jeu : rien d'ajouté au paquet. Tant
  que la police n'est pas prête, la police normale est utilisée.
- Verify.ps1 (agent) : 564/564, 0 avertissement, 0 erreur ; DLL 0.293.0.0. Non validé en jeu.
### English
- Page titles use the game's own title font (TrumpGothic) through Dalamud's game font atlas.

## 0.292.0
- Demandes (captures, fiche objet « Minerai de cuivre ») :
  - **Craft lié** : « Produit par » et « Utilisé dans » sont deux onglets (avec leur nombre) au lieu de deux sections repliables ;
    un nouvel objet s'ouvre sur la direction qui a des recettes. L'en-tête « Craft lié » qui répétait le nom de l'onglet de la
    fiche est retiré.
  - **Recherche d'objets** : la liste des résultats se replie (« Résultats (n) »), pour laisser la place à la fiche.
- Diagnostics : les temps d'affichage gardent les 120 dernières images de chaque page et montrent la **médiane** (coût habituel) et
  le **maximum** (pics, comme le premier chargement des icônes). La moyenne glissante d'avant gardait longtemps le pic d'une page
  quittée juste après son ouverture (Récolte à 15 ms, Mini Cactpot à 6,5 ms alors que leurs calculs sont en cache ou en tâche de
  fond) ; à remesurer avec cette version.
- Verify.ps1 (agent) : 564/564, 0 avertissement, 0 erreur ; DLL 0.292.0.0. Non validé en jeu.
### English
- Item sheet › Craft: « Made by » and « Used in » are tabs; the item search result list folds; Diagnostics draw times show the
  median and maximum of the last 120 frames per page.

## 0.291.0
- R-06, étape 6, d'après les temps mesurés par l'utilisateur dans Diagnostics (Triple Triade 14 ms, Alarmes 9,4 ms) :
  - **Triple Triade › PNJ et cartes** : le classement des 142 PNJ, celui des lots de cartes et la liste des cartes manquantes étaient
    recalculés à chaque image ; ils ne le sont plus que quand le catalogue, les collections, la liste des PNJ battus ou un filtre
    changent. Les listes des PNJ et des cartes manquantes ne dessinent plus que les lignes visibles (clipper).
  - **Alarmes** (et la tuile de l'accueil) : la prochaine heure de chaque alarme (une recherche dans les périodes de météo pour les
    alarmes à météo) était calculée plusieurs fois par alarme et par image ; elle est gardée et recalculée au plus une fois par
    seconde, ou dès qu'elle est passée.
  - Revue de mode (5,7 ms) et Météo (3,6 ms) : leurs calculs principaux étaient déjà en cache ; à remesurer après cette version.
- Verify.ps1 (agent) : 564/564, 0 avertissement, 0 erreur ; DLL 0.291.0.0. Non validé en jeu.
### English
- Triple Triad NPCs and cards: rankings and missing-card list cached, long lists clipped; alarms: next times cached (at most one
  computation per second per alarm).

## 0.290.0
- R-06, étape 6 (légèreté) : les listes de collections (plusieurs milliers de lignes) et de quêtes étaient parcourues en entier,
  à chaque image, pour retrouver une seule ligne : pastilles ✓/✗, infobulles d'objet (hauts faits qui le donnent), noms de cartes
  de Triple Triade, rouleaux d'orchestrion, noms de quêtes des marqueurs de la carte, PNJ et cartes. Ces recherches passent
  maintenant par des index communs (`UI/Components/DataIndexes.cs`), refaits seulement quand la liste est relue. Les
  dictionnaires de cartes de Triple Triade reconstruits à chaque image (PNJ et cartes, onglet Triple Triad de la carte) sont aussi
  gardés en cache.
- Diagnostics (mode dev) : « Temps d'affichage des pages », moyenne glissante du temps de calcul de chaque page visitée, la plus
  lente en premier, pour repérer ce qui reste lourd.
- Verify.ps1 (agent) : 564/564, 0 avertissement, 0 erreur ; DLL 0.290.0.0. Non validé en jeu.
### English
- Draw-time look-ups of collection and quest rows use shared indexes rebuilt only when the lists are read again (no more full
  scans every frame); Diagnostics shows each page's average draw time.

## 0.289.0
- Demande (captures des réglages de la carte) : les valeurs choisies par l'utilisateur deviennent les valeurs par défaut (lues dans
  sa configuration, en lecture seule) : membres du groupe à 50 % ; éthérites (icônes 60453, 60430, 60959) en priorité +1 ; icônes
  60442 et 71021 (quête annexe) en priorité −1 ; suivi du zoom 15 % ; opacité du cône de vision 50 % ; « Masquer ceux déjà
  obtenus », PNJ vendeurs et points de récolte activés. « Réinitialiser » (icônes, toutes cartes) et « Tout remettre par défaut »
  reviennent à ces valeurs ; les infobulles disent les nouvelles valeurs par défaut. Les configurations déjà enregistrées ne
  changent pas.
- Demande : couleur du cône de vision (Paramètres de la carte › Joueur), avec infobulle et ↺ ; l'opacité reste son propre réglage ;
  par défaut l'or du thème, comme avant.
- Tests mis à jour pour les nouvelles valeurs par défaut (suivi du zoom 15 %, priorités par défaut après réinitialisation).
- Verify.ps1 (agent) : 564/564, 0 avertissement, 0 erreur ; DLL 0.289.0.0. Non validé en jeu.
### English
- The user's tuned map values become the defaults (party 50 %, aetheryte and side-quest icon priorities, zoom follow 15 %, view
  cone opacity 50 %, hide collected, vendors and gathering points shown); new view cone colour setting.

## 0.288.0
- Demande : réglages manquants de la carte. Paramètres de la carte › Joueur : « Taille de ma position » (50–300 %), « Longueur du
  cône de vision » (50–300 %) et « Opacité du cône de vision » (0–100 %, 18 % par défaut, valeur d'avant). Chacun a son infobulle et
  son bouton ↺ ; les réglages du cône sont grisés quand le cône est masqué.
- Verify.ps1 (agent) : 564/564, 0 avertissement, 0 erreur ; DLL 0.288.0.0. Non validé en jeu.
### English
- Map › Player: settings for the position marker size, the view cone length and the view cone opacity.

## 0.287.0
- R-06, étape 5 (réglages et infobulles) : relevé des contrôles sans infobulle. Une infobulle est ajoutée à chaque vrai réglage qui
  n'en avait pas (28) : langue, thème, chaque durée de cache, aide du carnet de synthèse, limites de la recherche de rotation,
  export et copie des macros (livre, premier / dernier emplacement, préfixe, écrasement, enchaînement /tenshonext), tailles et
  opacité de la carte, alarmes (activer, prévenance). Chaque texte dit ce que fait le réglage (et sa valeur par défaut quand elle
  est simple), repris des libellés existants.
- Contrôle automatique (test) : chaque infobulle de réglage citée dans le code (`Tip`, `BoolSetting`, réglages de la carte) doit
  exister en anglais et en français, sinon Verify échoue.
- Restent sans infobulle des filtres d'outils (tri, recherche…), hors réglages ; à revoir au cas par cas.
- Verify.ps1 (agent) : 564/564, 0 avertissement, 0 erreur ; DLL 0.287.0.0. Non validé en jeu.
### English
- Every setting now has a tooltip (28 added: language, theme, cache durations, craft search limits, macro export and copy,
  map sizes, alarms); a test fails Verify when a setting tooltip named in the code is missing in English or French.

## 0.286.0
- Retour en jeu (capture : sept quêtes « Non confirmée » restantes dans la 7e ère ombrale, épopée finie) : ce sont des options non
  prises. Sonde (`docs/evidence/0.286-quest-alternatives.md`) : `QuestLock` (les trois « En bonne compagnie » se bloquent entre
  elles) et `PreviousQuestJoin` = 2 (une seule quête précédente suffit : « Le danger des Sylphes », « Les tréfonds de Sastasha »,
  « Visite au Bannock »…).
  - `Quests/QuestAlternatives.cs` (testé) : une quête non terminée est non jouable si une quête terminée la bloque, si une autre
    option de son embranchement est terminée, ou par propagation (toutes ses suites non jouables, ou ses prérequis nécessaires non
    jouables). Couvre les sept quêtes de la capture : autres Grandes Compagnies et leurs suites (« Que la Sylve vous protège »,
    « Pour la prospérité d'Ul'dah »), l'autre « L'appel de la mer », les autres « Une visite s'impose » de sa cité. S'applique à
    tous les types de quêtes, pas seulement à l'épopée.
  - Ces quêtes rejoignent celles des autres cités : masquées, hors pourcentages ; la case devient « Afficher les quêtes non
    jouables (n) ».
  - Le cache des quêtes est relu une fois (nouvelle forme : quêtes bloquées).
- Verify.ps1 (agent) : 563/563, 0 avertissement, 0 erreur ; DLL 0.286.0.0. Non validé en jeu.
### English
- Quests closed by a choice (Grand Company locks, other options of an « any one of » branch, and what only leads to or follows
  them) are hidden and left out of percentages like the other starting cities' quests.

## 0.285.0
- Retour en jeu (capture, « Chroniques de la 7e ère ombrale (161/215 · 74 %) ») : l'épopée d'A Realm Reborn commence différemment
  dans chacune des trois cités de départ, et les quêtes des deux autres cités ne sont pas jouables par le personnage. Sonde
  (`docs/evidence/0.285-quest-start-cities.md`) : dans le genre 1, les « Une visite s'impose » de chaque cité (3 à Gridania,
  2 à Limsa Lominsa, 3 à Ul'dah) sont les racines ; 21 / 21 / 23 quêtes ne remontent qu'aux racines d'une cité, 141 forment le
  tronc commun.
  - Tensho calcule la cité de chaque quête par sa chaîne de quêtes précédentes (`Quests/QuestStartCities.cs`, testé) et la cité du
    personnage (celle dont une quête est confirmée terminée). Les quêtes des deux autres cités sont masquées des listes et ne
    comptent plus dans les pourcentages ; une case « Afficher les débuts d'épopée des autres cités (n) » les réaffiche.
  - Sans quête confirmée (cité inconnue), rien n'est masqué.
  - Le cache des quêtes est relu une fois (nouvelle forme : genre et lieu de chaque quête).
- Verify.ps1 (agent) : 562/562, 0 avertissement, 0 erreur ; DLL 0.285.0.0. Non validé en jeu.
### English
- ARR main scenario quests of the two other starting cities (found through the previous-quest chains) are hidden from the quest
  lists and left out of the completion percentages; a checkbox shows them again.

## 0.284.0
- Demande : le pourcentage de complétion des types et catégories de quêtes dans les menus déroulants. Chaque entrée de « Type de
  quête » et de « Catégorie » affiche « nom (terminées/total · %) », aussi dans le champ fermé. Calcul fait une fois par lecture
  de la liste des quêtes. « Terminées » = complétion confirmée par le jeu ; les quêtes non confirmées (dont les débuts d'épopée des
  autres cités de départ) comptent dans le total.
- Verify.ps1 (agent) : 561/561, 0 avertissement, 0 erreur ; DLL 0.284.0.0. Non validé en jeu.
### English
- Quest kind and category drop-downs show completion (done/total · %).

## 0.283.0
- Demande (capture, Suivi des quêtes) : une catégorie principale par sorte de quête, liée aux données du jeu, et la catégorie
  actuelle en sous-catégorie. Sonde (`docs/evidence/0.283-quest-journal-sections.md`) : `Quest.JournalGenre` →
  `JournalCategory` → `JournalSection`. Les sections du journal deviennent la catégorie principale « Type de quête » : Épopée (ARR
  à Endwalker), Épopée (Dawntrail), Chroniques d'une nouvelle ère (raids), Quêtes annexes, Quêtes des peuples alliés (deux
  sections), Quêtes de classe/job, Divers, Mandats, Missions, dans l'ordre du jeu.
  - Filtre « Type de quête », puis « Catégorie » qui ne propose que les sous-catégories (genres) du type choisi.
  - Infobulles et fiche de quête : « Type › Catégorie » (ex. « Chroniques d'une nouvelle ère › L'Arcadion »).
  - Le cache des quêtes est relu une fois (nouvelle forme).
- Verify.ps1 (agent) : 561/561, 0 avertissement, 0 erreur ; DLL 0.283.0.0. Non validé en jeu.
### English
- Quest tracker: main « Quest kind » filter from the game's journal sections (main scenario, side quests, raid chronicles,
  allied societies, class/job, other, leves, missions), then the genre as sub-category.

## 0.282.0
- Retour en jeu (capture, fiche « Branchette de restauration (2e phase) ») : l'onglet « Craft lié » était affiché alors que l'objet
  n'a aucune recette (« Produit par (0) », « Utilisé dans (0) »). La fiche ne montre plus que les onglets qui ont un contenu,
  décidés comme le menu du clic droit : Craft lié si l'objet se fabrique ou sert d'ingrédient, Usages s'il a un usage connu,
  Récolte s'il se récolte, Marché s'il se vend. Aperçu et Stocks restent toujours. Un catalogue pas encore chargé garde son
  onglet (inconnu n'est pas « aucun »). Calcul refait seulement quand l'objet ou un catalogue change.
- Nom harmonisé : l'outil « Matériaux des monstres » de la liste des outils s'appelle « Butin des monstres » comme partout.
- Verify.ps1 (agent) : 561/561, 0 avertissement, 0 erreur ; DLL 0.282.0.0. Non validé en jeu.
### English
- The item sheet only shows tabs with content (Craft, Usages, Gathering, Market decided like the right-click menu); the
  « Monster materials » tool is renamed « Monster loot » like everywhere else.

## 0.281.0
- Retour en jeu (capture, fiche « Branchette de restauration », onglet Marché) : « La consultation des prix a échoué » sans rien
  afficher. Corrections :
  - **repli sur le cache** : les annonces sont maintenant aussi gardées sur disque (`market-board`, durée « Prix ») ; si Universalis
    échoue, la dernière lecture connue est affichée avec sa date (« Lecture impossible : données en cache du … »). Le résumé des
    prix garde aussi son dernier prix connu à côté de l'erreur ;
  - **objets non vendables** : cet objet ne se vend pas au marché ; la fiche n'a plus d'onglet Marché pour ces objets et la page
    Marché le dit au lieu d'interroger Universalis ;
  - la pause de 3 s entre deux requêtes ne relance plus une tâche à chaque image : la même tâche attend, en montrant le cache.
- Demande : vérifier que les réglages de cache sont utilisés partout, et ajouter « jamais ». Revérification faite (détail dans
  `docs/evidence/0.246-cache-audit.md`, section 0.281) : toutes les sources suivent leur zone de Réglages › Caches ; seul le cache
  disque des annonces manquait (ajouté). Nouvelle durée **« Jamais (seulement sur demande) »** dans chaque zone : les données ne sont
  plus relues d'elles-mêmes, les boutons « Actualiser » restent disponibles.
- Verify.ps1 (agent) : 561/561, 0 avertissement, 0 erreur ; DLL 0.281.0.0. Non validé en jeu.
### English
- Market listings are cached on disk and the last known board (or price) is shown, dated, when Universalis fails; items that
  cannot be sold have no Market tab; new « Never (on request only) » cache duration for every cache area.

## 0.280.0
- R-06, étape 3 (suite) : « Objets & rangements » est séparé en deux pages, rangées sous OBJETS dans la barre latérale.
  - **Objets** : la recherche et la fiche complète de l'objet, sans autre onglet.
  - **Rangements** (nouvelle page) : onglets Banque (où sont vos objets : personnage, servants, sacoches, arsenal, coffre à
    apparence, armoire ; grilles par conteneur, onglets d'origine, filtres de catégorie et de qualité, recherche), Cristaux,
    Imports. Le lien « Banque d'objets » de l'accueil ouvre directement l'onglet Banque.
  - Les liens vers un objet (fiche, stocks, craft…) ouvrent toujours la fiche, sur le bon onglet.
- Verify.ps1 (agent) : 561/561, 0 avertissement, 0 erreur ; DLL 0.280.0.0. Non validé en jeu.
### English
- « Items & storage » is split: Items (search and item sheet) and a new Storage page (Bank grids with origin tabs and filters,
  Crystals, Imports).

## 0.279.0
- Demande : deux onglets dans le carnet des cartes de Triple Triade, « Cartes » (les 460 ordinaires) et « Cartes Ex. » (les 15 de
  la série Ex.), chacune avec sa propre numérotation ; recherche, filtres et tri s'appliquent dans l'onglet ouvert. « Voir dans
  Triple Triade » ouvre l'onglet de la série de la carte.
- Verify.ps1 (agent) : 561/561, 0 avertissement, 0 erreur ; DLL 0.279.0.0. Non validé en jeu.
### English
- Triple Triad notebook: two tabs, ordinary cards and Ex. cards; « Show in Triple Triad » opens the card's series tab.

## 0.278.0
- Retour en jeu (capture, carnet de Triple Triade) : le tri par numéro mélangeait « 1 · dodo » et « 1 · Guerrier de la Lumière ».
  Le jeu a deux séries numérotées à part : 460 cartes ordinaires et 15 cartes « Ex. ». Sonde des données du jeu
  (`docs/evidence/0.278-triad-ex-cards.md`) : `TripleTriadCardResident.UIPriority` ≠ 0 marque les cartes Ex.
  - Les cartes Ex. s'affichent « Ex. 1 · Guerrier de la Lumière » (carnet, fiche, simulateur, deck, PNJ et cartes, Collections) et
    se rangent après les 460 cartes ordinaires, comme dans le carnet du jeu ; la recherche accepte « Ex. 1 ».
  - Le cache des collections est relu une fois (nouvelle forme de carte).
- Verify.ps1 (agent) : 561/561, 0 avertissement, 0 erreur ; DLL 0.278.0.0. Non validé en jeu.
### English
- Triple Triad « Ex. » cards (UIPriority ≠ 0, probed from game data) print « Ex. n » and sort after the 460 ordinary cards.

## 0.277.0
- Demande (capture, carnet de Triple Triade) : valeurs des cartes dans la liste, tri par côté ou par tous les côtés, score de carte
  selon les règles.
  - Chaque carte affiche sous son nom : « H 9 · D 1 · B 7 · G 8   Σ 25   score 27 » (haut, droite, bas, gauche, A = 10).
  - « Trier par » : numéro, haut, droite, bas, gauche, total des côtés, côté le plus faible, score ; bouton croissant / décroissant.
    À égalité, l'ordre du carnet est gardé.
  - « Règles du score » : Inversion, Tueur d'as, Identique, Bonus / Malus. Le score est une **estimation Tensho** à la formule
    affichée en infobulle, pas une donnée du jeu : somme des 4 côtés (Inversion : 11 − valeur ; Tueur d'as : un côté qui vaut 1
    compte 6) + 2 × le côté le plus faible ; Identique +2 par paire de côtés égaux ; Bonus +4 / Malus −4 pour une carte typée.
    Plus et Ordre ne changent pas la valeur d'une carte seule.
  - Tri et score refaits seulement quand un filtre, le tri ou les règles changent (`MiniGames/TriadCardScore.cs`, testé).
- Verify.ps1 (agent) : 561/561, 0 avertissement, 0 erreur ; DLL 0.277.0.0. Non validé en jeu.
### English
- Triple Triad notebook: card values under each name, sort by any side, the sum, the weakest side or a rule-based score
  (ascending / descending), with score rules (Reverse, Fallen Ace, Same, Ascension / Descension) and the formula in the tooltip.

## 0.276.0
- Demande (capture, Triple Triade › PNJ et cartes) : l'onglet empilait trois sections repliables. Il a maintenant trois
  sous-onglets : « PNJ à affronter », « Lots de cartes à acheter en priorité », « Cartes manquantes ». Seul le sous-onglet ouvert
  est calculé, et chaque liste prend toute la hauteur disponible (elles étaient limitées à 280 et 320 pixels). Fait avec le
  modèle `DetailPage`.
- Verify.ps1 (agent) : 560/560, 0 avertissement, 0 erreur ; DLL 0.276.0.0. Non validé en jeu.
### English
- Triple Triad › NPCs and cards: the three stacked sections become sub-tabs (NPCs to face, packs to buy, missing cards), each
  list using the full height.

## 0.275.0
- Demande : la pastille ✓ / ✗ (obtenu ou non) doit être partout où l'objet est un objet de collection. Elle n'était posée à la main
  que dans trois listes (butin des contenus, tableau de pêche, recherche de contenus). Elle fait maintenant partie du modèle
  d'icône d'objet : toute icône d'objet de collection (monture, mascotte, carte de Triple Triade, rouleau d'orchestrion) l'affiche
  dans tous les modules (fiche objet, usages, sources, butins, revue de mode, quêtes, marchands, alarmes…). Les listes de
  Collections et le carnet de Triple Triade, dont les icônes ne sont pas des objets, l'affichent aussi (`DrawEntryMark`).
  Inconnu (collections pas encore lues) = pas de pastille, jamais une croix par défaut.
- Coût : la recherche de l'entrée de collection d'un objet parcourait toute la liste ; elle passe par un index refait seulement
  quand l'état des collections change.
- Verify.ps1 (agent) : 560/560, 0 avertissement, 0 erreur ; DLL 0.275.0.0. Non validé en jeu.
### English
- The obtained ✓ / not obtained ✗ badge is now part of the item icon template (every collectible item icon in every module) and
  is drawn on Collections and Triple Triad notebook entries; collection lookups are indexed.

## 0.274.0
- Retour en jeu (captures, rouleau d'orchestrion « Torn from the Heavens… ») : le clic droit sur la ligne de la collection ne
  montrait que les sources (« Contenus (1) ») et une note, alors que le clic droit sur l'objet montre aussi les outils (prix du
  marché, butin des contenus, stocks, fiche…). Une entrée de collection débloquée par un seul objet (rouleau, carte, objet de
  monture, de mascotte…) ouvre maintenant le même menu que l'objet. Les entrées débloquées par plusieurs objets gardent la liste
  commune de leurs sources.
- Verify.ps1 (agent) : 560/560, 0 avertissement, 0 erreur ; DLL 0.274.0.0. Non validé en jeu.
### English
- Right clicking a collection entry unlocked by a single item opens that item's full menu (tools and grouped sources), the
  same as right clicking the item.

## 0.273.0
- Demande : vérifier, partout, que chaque lien arrive sur le bon onglet. Revue de tous les liens vers les pages à onglets
  (Objets & rangements, Marché, Triple Triade, Collections, Paramètres, Banque) :
  - **Objets & rangements** : cinq liens ouvraient la page sans choisir d'onglet et pouvaient tomber sur Banque ou Imports
    (ouvrir l'objet d'une alarme, « Comment l'obtenir (fiche) », repli du lien de collection, monnaie d'un échange, objet
    d'origine d'une désynthèse…). Ils ouvrent maintenant l'onglet Recherche avec la fiche de l'objet sur « Aperçu »
    (`OpenItemSheet`). Les liens Stocks, Craft lié, Usages, Récolte ouvrent la fiche sur leur onglet (0.272).
  - **Triple Triade** : « Ouvrir l'outil Triple Triade » depuis un PNJ (carte, liste des PNJ) ouvre l'onglet « PNJ et cartes » ;
    le bouton « Deck » des réglages ouvre « Mon deck ». « Voir dans Triple Triade » ouvrait déjà le carnet (0.267).
  - **Marché** : « Prix du marché » depuis un objet ouvre l'onglet Résumé, plus le dernier onglet utilisé.
  - Collections, Paramètres et Banque : chaque lien choisissait déjà son onglet ; rien à changer.
- Verify.ps1 (agent) : 560/560, 0 avertissement, 0 erreur ; DLL 0.273.0.0. Non validé en jeu.
### English
- Every link into a tabbed page now opens the right tab: item links land on the item sheet (Overview or the asked tab), NPC
  « Triple Triad tool » opens NPCs and cards, the settings Deck button opens My deck, item « Market prices » opens Summary.

## 0.272.0
- R-06, étape 3 (fiches complètes), d'après l'exemple de l'utilisateur (Objets & rangements › Recherche, « Carte Bugaal Ja ») :
  sélectionner un objet affiche sa **fiche complète** sous la recherche, au lieu de « Passez à l'onglet Stocks ou Craft lié ».
  - En-tête : icône (infobulle et clic droit), pastille de collection, nom, catégorie, niveaux, stock total, dernier prix connu
    (cache de prix du monde) avec sa date, ID en mode dev.
  - Onglets (seul l'onglet ouvert est calculé) : Aperçu (comment l'obtenir, usages, collection, stock : le même contenu que
    l'infobulle), Stocks, Craft lié, Usages, Récolte, Marché (annonces et coût pour N unités).
  - Les onglets du haut Stocks, Craft lié, Usages et Récolte sont retirés : la fiche les contient. Les liens qui les ouvraient
    (menus, « Voir les quantités », autres outils) ouvrent maintenant la fiche sur l'onglet correspondant. Restent en haut :
    Recherche, Cristaux, Banque, Imports.
- Nouveau modèle `UI/Components/DetailPage.cs` (en-tête + onglets calculés seulement quand ils sont ouverts, onglet demandé
  sélectionné une fois), premier utilisateur : la fiche objet.
- Verify.ps1 (agent) : 560/560, 0 avertissement, 0 erreur ; DLL 0.272.0.0. Non validé en jeu.
### English
- Selecting an item shows its full sheet on the same page (header with stock and last price; Overview, Stocks, Craft, Usages,
  Gathering and Market tabs); the separate Stocks / Craft / Usages / Gathering tabs are folded into it and links open the
  matching sheet tab. New DetailPage template.

## 0.271.0
- Retour en jeu (captures, Marché › Annonces) :
  - les colonnes ne se triaient pas : les tableaux Annonces et Historique des ventes se trient maintenant en cliquant sur un titre
    de colonne (un second clic inverse l'ordre). Par défaut : prix unitaire croissant pour les annonces, date la plus récente
    d'abord pour les ventes. Le tri n'est refait que quand l'ordre ou les données changent ;
  - l'icône de gil était après « (2 annonce(s), 100 unité(s)) » : elle suit maintenant directement le montant.
- Verify.ps1 (agent) : 560/560, 0 avertissement, 0 erreur ; DLL 0.271.0.0. Non validé en jeu.
### English
- Market Listings and Sale history tables sort by any column (click the header, again to reverse); the gil icon now follows
  the « cost for N units » amount.

## 0.270.0
- R-06, étape 4 (marché enrichi, plan `docs/goals/R-06.md`) : la page Marché a trois onglets.
  - **Résumé** : l'ancien contenu (prix minimum et dernière vente NQ / HQ sur le monde).
  - **Annonces** : toutes les annonces Universalis (jusqu'à 100) du monde, du centre de données ou de la région au choix :
    prix unitaire, quantité, total taxe comprise, qualité, monde, vendeur, date de relevé. Filtres : NQ / HQ, quantité min / max,
    prix unitaire min / max, monde.
  - **Coût pour N unités** : les annonces les moins chères qui donnent au moins N unités, une annonce s'achetant en entière (règle
    du jeu), taxe comprise ; total, nombre d'annonces et d'unités, prix par unité ; les annonces choisies sont surlignées. Le
    calcul est exact (sac à dos 0/1), pas une approximation, et peut écarter le prix unitaire le plus bas quand le total est meilleur.
  - **Historique des ventes** : ventes récentes (jusqu'à 100) avec min, moyenne pondérée par la quantité, max, et ventes moyennes
    par jour données par Universalis ; mêmes filtres de qualité et de monde.
- Garde-fous réseau : lecture seulement depuis la page Marché, une requête à la fois, 3 s minimum entre deux requêtes, réponse
  bornée à 4 Mo, résultats gardés pendant la durée du cache « Prix » (64 entrées en mémoire). La région d'un centre de données est
  lue une fois par session (`/api/v2/data-centers`). Champs sondés : `docs/evidence/0.270-universalis-listings.md`.
- Tests : lecture des annonces et ventes (monde sans nom de monde, taxe), filtres, coût pour N unités (annonces entières, cas où
  le prix unitaire le plus bas perd, unités insuffisantes), statistiques pondérées. 560/560.
- Verify.ps1 (agent) : 560/560, 0 avertissement, 0 erreur ; DLL 0.270.0.0. Non validé en jeu.
### English
- Market page tabs: Summary, Listings (all Universalis listings for world / data center / region with quality, quantity, unit
  price and world filters, plus the exact cheapest set of whole listings for N units, tax included) and Sale history (min,
  weighted average, max, units per day). One request at a time, 3 s apart, bounded, cached for the price cache lifetime.

## 0.269.0
- Retour en jeu (capture, carnet de Triple Triade, carte Cloud) : le menu du clic droit montrait deux fois son contenu (outils et
  échanges en double). Cause : l'icône et le nom de la carte attachaient le même menu sous le même identifiant ; ImGui dessinait
  alors le menu ouvert une fois par appel. Chaque menu attaché (icônes d'objet, cartes de Triple Triade) reçoit maintenant un
  identifiant unique, stable d'une image à l'autre (`UniqueMenuId`). Corrige aussi le cas d'un même objet affiché deux fois dans
  une liste.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.269.0.0. Non validé en jeu.
### English
- Right-click menus attached to item icons and Triple Triad cards no longer show their content twice (each gets a unique,
  frame-stable popup id).

## 0.268.0
- Retour de l'utilisateur : dans le carnet des cartes de Triple Triade, pas de clic droit, et le nom d'une carte n'avait pas
  d'infobulle (seule l'icône en avait une). Le modèle d'infobulle de carte (`TriadCardTooltip`) porte maintenant aussi le menu
  de la carte (outils et sources de l'objet « carte »). Effet sur les 10 endroits qui affichent une carte : carnet, deck, decks
  enregistrés, simulateur, demande de deck, PNJ et cartes, lots, Collections. Dans le carnet, le nom de la carte a l'infobulle
  et le clic droit comme l'icône.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.268.0.0. Non validé en jeu.
### English
- Every Triple Triad card (icon or name, in all Triad views and Collections) now has the card item's right-click menu; the
  notebook card name shows the full tooltip.

## 0.267.0
- Retour de l'utilisateur : « Voir dans Triple Triade » ne faisait rien de visible (seulement l'objet en contexte). Cause : le
  lien sélectionnait la carte mais restait sur l'onglet ouvert de Triple Triade (souvent « PNJ et cartes »), et les filtres du
  carnet pouvaient masquer la carte, ce qui annulait la sélection. Maintenant : l'onglet « Carnet des cartes » s'ouvre, la
  recherche et les filtres (étoiles, type, obtenues) sont vidés, la carte est sélectionnée avec sa fiche et la liste défile
  jusqu'à elle. Même chemin depuis tous les menus (`ShowTriadCard`).
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.267.0.0. Non validé en jeu.
### English
- « Show in Triple Triad » now opens the card notebook tab with filters cleared, selects the card and scrolls to it (it only
  set the context item before).

## 0.266.0
- Remarque de l'utilisateur : une carte de Triple Triade n'a pas d'entrée dans Collections, sa « collection » est l'outil Triple
  Triade lui-même. Le lien menait déjà à Triple Triade, mais le libellé disait « Ouvrir la collection ». Il dit maintenant
  « Voir dans Triple Triade » pour une carte, partout : clic droit d'un objet dans Tensho, menu de l'inventaire du jeu, menu des
  lignes d'objet, icônes de l'objet en contexte.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.266.0.0. Non validé en jeu.
### English
- For a Triple Triad card, the « open collection » entry now reads « Show in Triple Triad » in every menu (the card's
  collection is the Triple Triad tool).

## 0.265.0
- Retour en jeu (capture, Triple Triade › PNJ et cartes) : un clic droit sur une carte de la colonne « Cartes qui me manquent »
  ouvrait le menu du PNJ de la ligne (lieu, drapeau), car les icônes de carte n'avaient pas de menu. Elles ouvrent maintenant
  le menu de la carte elle-même (objet « carte de Triple Triade » : outils et sources), aussi dans « Lots de cartes à acheter en
  priorité ». Le menu du PNJ reste sur le reste de la ligne.
- Menu de lieu : les coordonnées n'apparaissent plus en double dans l'en-tête (« Kugane · X 8,0 Y 13,6  (X 8,0  Y 13,6) »).
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.265.0.0. Non validé en jeu.
### English
- Triple Triad NPC list: right clicking a card icon opens the card's own menu (tools and sources) instead of the row NPC's
  menu; place menus no longer repeat the coordinates.

## 0.264.0
- R-06, étape 2 (plan `docs/goals/R-06.md`) : la barre latérale est rangée par catégories repliables (clic sur le titre ; l'état
  est gardé ; une catégorie repliée qui contient la page ouverte a son titre en doré) :
  - Accueil & outils ;
  - OBJETS : Objets & rangements, Tri de l'inventaire, Marché ;
  - PRODUCTION : Artisanat, Récolte, Enchaînement de macros ;
  - MONDE : Carte, Météo, Alarmes, PNJ vendeurs, Butin des contenus, Butin des monstres ;
  - PROGRESSION : Quêtes, Collections, Personnage ;
  - GOLD SAUCER : Revue de mode, Triple Triade, Mini Cactpot ;
  - PRÉFÉRENCES : Paramètres, Diagnostics (mode dev).
  Les pages « concentrateurs » de la carte et du Gold Saucer et la paire macros / tri deviennent des entrées directes ; leurs
  barres d'onglets en haut de page sont retirées. Artisanat, Récolte et Collections gardent leurs sous-onglets.
- Noms harmonisés (même nom dans la barre, le titre de la page et les menus) : « Combat » → « Personnage » (le combat est hors
  périmètre), « Carte / monde » → « Carte », « Collection » → « Collections », « Sources sur monstres » et « Matériaux des
  monstres » → « Butin des monstres », « Butins des contenus » → « Butin des contenus », « Séquences de macros » →
  « Enchaînement de macros ».
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.264.0.0. Non validé en jeu.
### English
- Sidebar grouped in foldable categories (Items, Production, World, Progression, Gold Saucer, Preferences); map and Gold Saucer
  hub pages become direct entries; names aligned between sidebar, page titles and menus (Combat → Character, Monster loot,
  Duty loot, Macro chain, Map, Collections).

## 0.263.0
- R-06, étape 1 (suite) : nouveau modèle `UI/Components/EntityMenus.cs`, un menu de clic droit par type de chose. Une quête, un
  PNJ ou un monstre propose les mêmes entrées partout où il est listé :
  - **quête** : ouvrir la quête ; début de la quête (si sa position est connue) : drapeau, voir sur la carte Tensho, météo de la
    zone. Branché sur la liste des quêtes, les quêtes liées (précédentes / suivantes) et l'onglet Quêtes du clic droit de la carte.
  - **PNJ** : ouvrir ses ventes (vendeur) ou l'outil Triple Triad (joueur de cartes), puis pour chaque position : drapeau, voir sur
    la carte Tensho, météo de la zone. Branché sur l'outil PNJ vendeurs, les onglets Vendeurs et Triple Triad de la carte, la liste
    des PNJ de Triple Triad.
  - **monstre** : voir son butin. Branché sur la liste des monstres d'un objet.
  - **lieu** : drapeau, carte Tensho, météo (prêt pour les listes de lieux).
  L'ancien menu « drapeau » de l'outil PNJ vendeurs est remplacé par le menu PNJ (même pose de drapeau, la carte Tensho se centre
  en plus dessus).
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.263.0.0. Non validé en jeu.
### English
- One shared right-click menu per kind of thing (quest, NPC, monster, place): open its tool, then flag, Tensho map and zone
  weather for each known position; used in the quest list and links, NPC vendors, map Vendors / Triple Triad / Quests tabs, Triple
  Triad NPCs and the monster list.

## 0.262.0
- R-06, étape 1 (suite) : les 26 réglages de la carte Tensho n'avaient aucune infobulle. Chacun en a une maintenant (ce qu'il fait
  et sa valeur par défaut, en français et en anglais), et les 22 cases ont le bouton ↺ quand elles diffèrent du défaut.
- Relecture des fichiers de suivi (demande utilisateur) : GOAL.md pointait encore sur l'ancien objectif Triple Triade ; STATUS
  omettait dans sa reprise les boutiques de compagnie libre et les flèches boussole de la carte (décidées avant D17), et sa liste
  « à valider » s'arrêtait à 0.233. Les trois sont corrigés.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.262.0.0. Non validé en jeu.
### English
- Every Tensho map setting now has a help tooltip (what it does, default value) and the « restore default » button; tracking
  files (GOAL, STATUS) brought up to date.

## 0.261.0
- R-06, étape 1 (suite) : bouton ↺ sur le choix du thème (Réglages › Interface), qui revient à « Suivre le jeu ».
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.261.0.0. Non validé en jeu.
### English
- Settings › Interface: the theme choice gets the « restore default » button (back to following the game).

## 0.260.0
- R-06, étape 1 (suite) : bouton ↺ (`DefaultButton`) sur le délai entre commandes du jeu (Réglages › Avancé), dernier curseur
  des réglages qui ne l'avait pas.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.260.0.0. Non validé en jeu.
### English
- Settings › Advanced: the game command delay slider gets the « restore default » button.

## 0.259.0
- R-06, étape 1 (suite) : modèle `DefaultButton` (bouton ↺ après n'importe quel réglage, affiché seulement si la valeur
  diffère du défaut). Appliqué aux curseurs de la carte : taille des noms de lieux, taille des icônes, suivi du zoom, opacité.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.259.0.0. Non validé en jeu.
### English
- Map sliders (place names, icon size, zoom follow, opacity) get the « restore default » button when changed.

## 0.258.0
- Précision de l'utilisateur : « remettre les réglages par défaut » vise tous les réglages. Réglages › Avancé › « Tout remettre par
  défaut », avec confirmation. Chaque réglage public de la configuration reprend la valeur d'une configuration neuve. Sont
  conservés : la langue, les alarmes, les préréglages d'artisanat, les decks de Triple Triad, les quêtes suivies, les favoris de
  carte, le plan de macros, les profils et modèles de tri, les relevés. La banque est dans un fichier à part et n'est pas touchée.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.258.0.0. Non validé en jeu.
### English
- Settings › Advanced › « Restore all defaults » (with confirmation): every setting returns to its default; language, alarms,
  presets, decks, followed quests, bookmarks, macro plan, sort profiles and observations are kept.

## 0.257.0
- Retour en jeu (capture, carte de Triple Triad) : le panneau Tensho ajouté sous l'infobulle de l'inventaire du jeu ne montrait que
  la collection, le stock et les usages. L'infobulle d'objet de Tensho montrait en plus « Comment l'obtenir » (PNJ, quêtes, hauts
  faits, contenus, monstres, ALEA, désynthèse, boutique, fabrication, récolte, marché). Les deux partagent maintenant le même
  contenu (`DrawItemInfoBody`). Le panneau du jeu garde ses prix et n'a pas d'en-tête, l'icône et le nom étant déjà dans
  l'infobulle du jeu.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.257.0.0. Non validé en jeu.
### English
- The Tensho panel under the game's inventory tooltip now shows the same content as Tensho's item tooltip (including « how to
  get it »), through one shared body.

## 0.256.0
- Demande : le menu Tensho du clic droit dans l'inventaire du jeu n'était pas harmonisé avec celui de Tensho (il n'avait que les
  outils). Il affiche maintenant, sous les outils, les mêmes sources regroupées par type (Hauts faits, Contenus, À partir d'autres
  objets, Échanges, PNJ vendeurs, Quêtes) en sous-menus natifs, avec Fabrication et Récolte en ligne directe et « ↳ Drapeau sur la
  carte » sous chaque PNJ placé. Les sources sont calculées une seule fois (`SourceLines`), puis affichées soit dans Tensho, soit dans
  le menu du jeu : les deux menus restent identiques. Calcul fait seulement à l'ouverture du sous-menu Tensho.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.256.0.0. Non validé en jeu.
### English
- The Tensho entry of the game's inventory right-click menu now lists, below the tools, the same grouped sources as Tensho's own
  menu (native sub-menus, map flag under placed NPCs); both menus share one source computation.

## 0.255.0
- R-06, étape 1 (suite) : les deux dernières listes d'objets sans l'infobulle commune. Récompenses de quête et Revue de mode :
  le nom de l'objet affiche l'infobulle complète, suivie de leurs informations propres (qualité et quantité ; détails de la
  revue de mode), et le clic droit ouvre les outils. Toutes les listes d'objets repérées dans `docs/goals/R-06.md` sont
  maintenant traitées.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.255.0.0. Non validé en jeu.
### English
- Quest rewards and Fashion Report item names now show the common item tooltip (followed by their own details) and the
  tools menu; every item list listed in the R-06 plan is done.

## 0.254.0
- Demande : les boutons d'alarme doivent tenir compte de la météo quand le poisson (ou une autre ressource) en a besoin.
  - Récolte › lignes de pêche : l'alarme reprend les conditions GatherBuddy complètes, plage ET plus météo et météo
    précédente. Sans plage horaire mais avec une météo, une alarme est posée à chaque période météo (00, 08, 16 ET). Le calcul
    est partagé avec « Comment le pêcher » (`GatherBuddyFishAlarmButton`), donc les deux boutons font la même alarme.
  - Les autres boutons de pêche en tenaient déjà compte : sources de pêche (carnet de pêche du jeu) et prévisions.
  - Mineur et botaniste : les données de récolte du jeu lues par Tensho (points de récolte et leurs horaires) ne portent que
    des plages horaires, pas de condition de météo. Leurs alarmes restent sur l'horaire.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.254.0.0. Non validé en jeu.
### English
- Fishing alarms from the gathering rows now carry GatherBuddy's weather and previous-weather conditions (shared with
  « How to fish it »); mining/botany alarms stay time-based (the game's gathering data has no weather condition).

## 0.253.0
- Retour en jeu (capture, Récolte › pêcheur) : le bouton « alarme » des lignes de pêche avait disparu. Cause : depuis 0.230,
  l'horaire affiché pour un poisson vient de GatherBuddy, mais le bouton lisait encore l'horaire des données du jeu, inconnu pour
  la pêche, et ne s'affichait donc pas. Le bouton suit maintenant l'horaire affiché. L'alarme porte sur la plage horaire ET ;
  la météo éventuelle n'est pas prise en compte par ce bouton (celui de « Comment le pêcher » la prend en compte).
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.253.0.0. Non validé en jeu.
### English
- Gathering › fishing rows: the alarm button is back; it now follows the GatherBuddy time window shown (it read the game's
  schedule, unknown for fish).

## 0.252.0
- R-06, étape 1 (suite) : `ItemCell` dans le butin des contenus et le tableau de pêche (avec la pastille de collection, nouvelle
  option du modèle). Le nom de l'objet du panneau de récolte en jeu a l'infobulle complète et le clic droit. Les appâts
  (pêche, GatherBuddy) n'ouvrent plus deux menus de clic droit superposés.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.252.0.0. Non validé en jeu.
### English
- ItemCell now used in duty loot and the fishing table (with the collection badge); gathering panel item names get the full
  tooltip and tools menu; bait icons no longer stack two right-click menus.

## 0.251.0
- Retour en jeu (capture) : la liste des sources du clic droit d'un objet était trop longue (toutes les sources à la suite,
  avec des lignes en double). Le haut du menu (outils) ne change pas. Les sources sont maintenant regroupées par type en
  sous-menus qui se déploient au survol : Hauts faits, Contenus, À partir d'autres objets (désynthèse, etc.), Échanges,
  PNJ vendeurs, Quêtes, avec leur nombre. Fabrication et Récolte restent une ligne chacune. Une ligne identique n'apparaît plus
  qu'une fois (ex. le même PNJ listé sous plusieurs identifiants : « 1000 × Crédit cosmique — Mesouaidonque » ×4).
  Même regroupement partout où ces sources sont listées (menu d'objet, panneau de survol, collections).
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.251.0.0. Non validé en jeu.
### English
- Item right-click menu: sources are grouped by kind in hover sub-menus (achievements, duties, from other items, exchanges,
  NPC vendors, quests) with counts; identical lines are shown once. The tools at the top are unchanged.

## 0.250.0
- Retour en jeu (capture) : dans la liste de la récolte, le nom d'un objet affichait une infobulle réduite au seul nom, et
  l'icône l'infobulle complète, soit deux infobulles différentes pour le même objet. Le nom donne maintenant l'infobulle
  complète :
  - liste des sources de récolte : passée sur `ItemCell` ;
  - résultats de recherche d'objets : infobulle complète et clic droit des outils ;
  - monnaie des marchands PNJ : l'infobulle « nom seul » est retirée ;
  - mode dev : l'ID de l'objet est ajouté à l'infobulle commune (il n'était que dans l'infobulle réduite).
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.250.0.0. Non validé en jeu.
### English
- Item names no longer show a name-only tooltip next to the full one on the icon (gathering sources, item search, vendor
  currencies); developer mode shows the item ID in the common tooltip.

## 0.249.0
- R-06, étape 1 (suite) :
  - `ItemCell` (icône + nom) remplace les paires écrites à la main dans le butin des monstres, le coût de préparation, les
    sources de pêche, l'observation de récolte, les usages d'objet (fabrication, achats, échanges), l'orchestrion et les
    hauts faits. Le nom de l'objet a maintenant lui aussi l'infobulle complète et le clic droit des outils. Un clic sur l'icône
    ou sur le nom fait de l'objet l'objet en contexte.
  - Nouveau modèle `UI/Components/SettingRow.cs` : un réglage a toujours son infobulle, et dès qu'il diffère de la valeur par
    défaut, un petit bouton ↺ la remet. La valeur par défaut est lue sur une configuration neuve, donc toujours exacte. Les
    12 réglages à case à cocher de Réglages et Avancé l'utilisent.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.249.0.0. Non validé en jeu.
### English
- ItemCell (icon + name with the full item tooltip and tools menu) now used in monster loot, preparation cost, fishing
  sources, gathering observation, item uses, orchestrion and achievements. New SettingRow template: every checkbox setting
  keeps its tooltip and gets a « restore default » button when changed.

## 0.248.0
- Demande : tout doit être dans la même DLL, y compris la localisation. Les langues étaient déjà intégrées à la DLL, mais
  une copie des fichiers JSON était aussi livrée dans le paquet (`Localization/Languages/*.json`). Elle ne l'est plus : le paquet
  contient seulement `TenshoAssistant.dll`, `TenshoAssistant.json` et `TenshoAssistant.deps.json`.
  - Verify contrôle désormais qu'aucun fichier de langue n'est livré à côté de la DLL.
  - Un dossier `Localization/Languages` posé à la main à côté de la DLL reste lu en surcharge, pour tester une traduction
    (`Localization/README.md`).
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.248.0.0. Non validé en jeu.
### English
- Languages now ship only inside TenshoAssistant.dll; the package no longer carries the JSON language files (Verify enforces it).
  A hand-made Localization/Languages folder beside the DLL still overrides them for translators.

## 0.247.0
- R-06, étape 1 (plan `docs/goals/R-06.md`) : premiers modèles communs dans `src/TenshoAssistant/UI/Components/ItemCell.cs`.
  - `DrawItemIcon` devient le modèle d'icône d'objet. Pour un vrai objet, le survol donne l'infobulle d'objet commune
    (provenance, stocks, usages, prix) et le clic droit ouvre les outils utiles et les sources (`ItemLinksMenu` /
    `RelevantRoutes`). Effet immédiat sur environ 45 listes de tous les modules (butin, pêche, récolte, artisanat, quêtes, revue de
    mode, marchands, alarmes, banque…). Les icônes qui ne sont pas des objets (contenus, actions, métiers, météo) restent de
    simples images.
  - `ItemCell` : icône + nom sélectionnable sur une ligne (clic = objet en contexte), prêt pour la suite de la migration.
  - Une seule infobulle d'objet par image : l'icône et le nom voisin ne l'affichent plus en double.
  - Retrait des infobulles réduites au seul nom (banque, cristaux), remplacées par l'infobulle complète.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.247.0.0. Non validé en jeu.
### English
- UI templates (R-06 step 1): every item icon now shows the full item tooltip on hover and the item tools menu on right
  click, across all modules; new ItemCell template (icon + selectable name); one item tooltip per frame.

## 0.246.0
- R-10 (audit des caches) : relevé de toutes les sources réseau et données du jeu, de leurs durées et cadences dans
  `docs/evidence/0.246-cache-audit.md`. Deux défauts corrigés :
  - LuminaSupplemental et GatherBuddy sont demandés à chaque image ; après un échec (hors ligne, GitHub indisponible), le
    téléchargement repartait aussitôt, en boucle. Désormais 5 min d'attente avant un nouvel essai.
  - Quand la copie en cache a expiré et que le réseau échoue, l'ancienne copie est utilisée au lieu de perdre la source.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.246.0.0. Non validé en jeu.
### English
- Cache audit (docs/evidence/0.246-cache-audit.md). LuminaSupplemental and GatherBuddy no longer retry every frame after a
  failed download (5 min wait), and fall back to their expired cached copy when the network fails.

## 0.245.0
- R-04 : un clic droit sur une icône de la carte Tensho ouvre le menu de cette icône : son nom, « Placer le drapeau », puis selon
  l'icône « Se téléporter ici » (éthérite), « Ouvrir cette carte » (lien de carte), les ressources du point de récolte (ouvre l'outil
  Récolte), « Retirer la cible indiquée par le jeu » (cible du journal), et « Menu général de la carte… ». Ailleurs, le clic droit
  ouvre toujours le menu général. Réglages de la carte › icônes : une case par icône (après la priorité) ; décochée, le clic droit
  sur cette icône passe au menu général (liste commune à toutes les cartes, `RightClickIgnore`).
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.245.0.0. Non validé en jeu.
### English
- Map: right clicking an icon opens that icon's own menu (flag, teleport, open linked map, gathering resources, remove journal
  target, general menu). A per-icon box in the icon settings sends the right click to the general menu instead.

## 0.244.0
- R-03 : clic droit sur la carte › nouvel onglet « Triple Triad » : les PNJ de Triple Triad de la carte affichée (catalogue
  TriadNpcCatalog déjà en cache), avec l'état battu [x] / non battu [ ] / inconnu [?], le nombre de cartes de récompense
  manquantes et les coordonnées. Clic = drapeau ; clic droit = outil Triple Triad ; survol = prix d'entrée, cartes de récompense, état.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.244.0.0. Non validé en jeu.
### English
- Map right-click menu: new « Triple Triad » tab listing the map's Triple Triad NPCs (beaten state, missing reward cards,
  coordinates; click = flag, right click = Triple Triad tool).

## 0.243.0
- R-01 (suite) : l'icône « Cible indiquée par le jeu (journal) » est maintenant liée à sa quête. Quand la quête n'est plus active
  (accomplie ou abandonnée), l'icône disparaît d'elle-même. Cause : la cible gardait seulement une position, sans la quête.
  La liste des quêtes n'étant relue que toutes les 5 min, une relecture est demandée au plus une fois par minute tant qu'une
  telle cible est affichée ; seule une liste lue après la pose de la cible (pas le cache disque) peut la retirer.
  Délai attendu : jusqu'à environ une minute après l'accomplissement.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.243.0.0. Non validé en jeu.
### English
- Map: the journal target icon is now tied to its quest and disappears on its own once the quest is no longer active
  (within about a minute).

## 0.242.0
- R-01, première partie : l'icône « Cible indiquée par le jeu (journal) » restait sur la carte sans moyen de la retirer. Clic droit sur la
  carte › « Retirer la cible indiquée par le jeu (journal) ». Le retrait automatique quand la quête est accomplie reste à faire (R-01).
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.242.0.0. Non validé en jeu.
### English
- Map: right click › « Remove the target shown by the game (journal) » removes the journal target icon that stayed on the map.

## 0.241.0
- R-02 (docs/project/USER-REQUESTS.md) : cartes de Triple Triad et rouleaux d'orchestrion sans ✓/✗ et absents de « Non obtenus seulement »
  dans le butin. Cause : un objet de collection non confirmé n'était tenu pour « non obtenu » (collection chargée) que pour les montures et
  mascottes. Correction : même règle pour les cartes et les rouleaux.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.241.0.0. Non validé en jeu.
### English
- Triple Triad cards and orchestrion rolls now get the obtained / not obtained mark and the « not obtained only » filter, like mounts.

## 0.240.0
- Retour : les tenues complètes ne sont pas prises en compte pour le coffre à apparence. Cause : une tenue est rangée comme un seul objet
  (la tenue) dans MirageManager.PrismBoxItemIds. Correction : pour chaque tenue rangée, ses pièces (feuille MirageStoreSetItem : arme,
  bouclier, tête, torse, mains, jambes, pieds, oreilles, cou, poignets, bague) sont aussi comptées comme possédées (stocks, Revue de mode).
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.240.0.0. Non validé en jeu.
### English
- Glamour dresser: the pieces of a stored outfit (MirageStoreSetItem) now count as owned, not only the outfit item.

## 0.239.0
- Retour (capture) : Banque toujours indisponible après 0.238. Fichier InventoryBank.json examiné (lecture seule) : l'entrée « Armoire »
  enregistrée par 0.236 porte des numéros de case jusqu'à 3343 (numéros de ligne Cabinet), refusés au chargement (borne 1000) ; 0.238
  évitait seulement d'en écrire de nouveaux. L'arsenal n'avait jamais été enregistré (capture bloquée tant que la Banque est en erreur).
  La sacoche supplémentaire n'est pas en cause (entrée présente, vide).
- Correction : au chargement, numéros de case acceptés jusqu'à 100 000 et 4 096 cases par conteneur ; le fichier actuel se relit sans perte.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.239.0.0. Non validé en jeu.
### English
- Fix: the Bank now loads armoire records saved by 0.236 (large slot numbers) instead of failing as a whole.

## 0.238.0
- Retour (capture) : Banque non fonctionnelle, « Les relevés de stocks ne sont pas disponibles », servants et sacoches disparus.
  Cause (régression 0.235–0.236) : le fichier de la Banque refusait plus de 7 conteneurs par rangement, or l'arsenal en a 12 ; et les
  cases de l'armoire portaient le numéro de ligne Cabinet, au-delà de la borne de 1000. Le chargement échouait en entier ; le fichier
  n'a pas été écrasé (protection « échec de chargement »), les relevés sont intacts. Correction : 16 conteneurs au plus, cases de
  l'armoire numérotées dans l'ordre.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.238.0.0. Non validé en jeu.
### English
- Fix: the Bank failed to load after the armoury chest was saved (12 containers over the old limit of 7; armoire slot numbers too large);
  saved records were kept intact.

## 0.237.0
- Retour (capture de la Banque) : l'onglet « Coffre à apparence » affichait « ordre visuel indisponible. Réimportez le rangement
  ouvert ». Cause : la vue par onglet range les objets selon la mise en page des sacs du jeu (InventoryDisplayLayout), qui n'existe pas
  pour l'arsenal ni pour les conteneurs virtuels du coffre et de l'armoire. Correction : pour ces rangements, les pièces dans l'ordre,
  35 par page (grille 5 × 7, même affichage et mêmes infobulles que les sacs). La lecture elle-même fonctionnait (relevé 10:46).
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.237.0.0. Non validé en jeu.
### English
- Bank: the armoury chest, glamour dresser and armoire tabs now show their pieces in pages of 35 instead of « visual order unavailable ».

## 0.236.0
- Décision de l'utilisateur : ne pas dépendre d'un autre plugin (Allagan Tools écarté). Coffre à apparence et armoire lus dans le jeu.
- Coffre à apparence : MirageManager.PrismBoxItemIds (HQ = +1 000 000, collectionnable = +500 000), connu après l'ouverture du coffre en
  jeu (PrismBoxLoaded). Armoire : chaque ligne de la feuille Cabinet (ligne → objet) testée par UIState.Cabinet.IsItemInCabinet, connue après
  l'ouverture de l'armoire (IsCabinetLoaded). Les deux sont enregistrés dans la Banque (conteneurs virtuels 25000 et 25001), affichés dans
  leurs sections, comptés dans les stocks et le « possédé » de la Revue de mode ; rappel « ouvrez une fois… » tant qu'ils ne sont pas lus.
  Arsenal, coffre et armoire relus toutes les 2 minutes (au lieu de 30 s) ; exclus de l'aperçu de tri.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.236.0.0. Non validé en jeu.
### English
- Glamour dresser and armoire read from the game (no other plugin needed), kept in the Bank and counted in stocks and the Fashion Report
  « owned » state; known once each has been opened in the game.

## 0.235.0
- Plan avant publication, point 2 (élargi : coffre à apparence, armoire, arsenal, reliés à la Banque). Première étape : l'arsenal.
- Arsenal lu comme un rangement (12 conteneurs InventoryType 3500, 3200–3203, 3205–3209, 3300, 3400 ; ids vérifiés par réflexion de
  FFXIVClientStructs), enregistré dans la Banque (« Arsenal », relu toutes les 30 s quand la capture automatique est active), affiché dans
  ses propres lignes et compté dans les stocks partout (« possédé » de la Revue de mode compris). Exclu de l'aperçu de tri.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.235.0.0. Non validé en jeu.
### English
- Armoury chest read as a storage, kept in the Bank and counted in stocks everywhere (Fashion Report « owned » included).

## 0.234.0
- Demande : que le commit et la release ajoutent les textes de toutes les versions pas encore commitées ou publiées.
- `Release.ps1` : le message de commit reprend la section anglaise de chaque version du CHANGELOG depuis la dernière version commitée
  (dernier commit dont le titre commence par un numéro), et les notes de la release GitHub et de repo.json celles de chaque version depuis
  la dernière release publiée (plus haut tag `Stable-x.y.z`, tags récupérés de GitHub), de la plus récente à la plus ancienne, chacune sous
  « ## x.y.z » ; une seule version : ses notes seules comme avant. `-DryRun` affiche le message et les notes. Testé (-DryRun) : le commit
  en attente couvre 0.224.0 à 0.233.0.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.234.0.0. Aucun changement du plugin.
### English
- Commit messages and release notes now gather the English notes of every version since the last commit or the last published release
  (newest first, one « ## x.y.z » section each); `-DryRun` prints them.

## 0.233.0
- Retour : le clic droit ne donne pas accès à nos modules sur les poissons ni sur les appâts.
- Clic droit sur un poisson ou un appât de l'aide à la pêche, et sur un appât de « Comment le pêcher » : menu des outils utiles pour cet
  objet. Le menu « liens » commun (butin des contenus, mission, Revue de mode, pêche) liste maintenant les mêmes outils que le menu de
  l'inventaire et l'objet en contexte (`RelevantRoutes` : usages, artisanat, récolte, vendeurs, marché, butin, monstres, quête,
  collection, stocks, fiche, outils), puis les sources de l'objet.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.233.0.0. Non validé en jeu.
### English
- Right click on a fish or a bait (fishing help, Gathering page) opens the menu of useful tools; the shared item links menu now lists the
  same tools as the inventory menu and the context item panel, then the item's sources.

## 0.232.0
- Retour (capture de l'aide à la pêche, temps de touche affichés) : plus compact sans perdre d'information, et adapté au texte.
- Tableau d'une ligne par poisson : poisson (stock, ✓/✗), route d'appâts en icônes, touche « ! / !! / !!! » en couleur avec le ferrage
  abrégé, temps de touche « 9–16 s (12) » (vert dans l'intervalle du chrono), et une colonne Conditions seulement si un poisson en a
  (créneau, météo, météo précédente, leurre, accrochage, proies). Les textes complets passent en infobulle ; la note des sources devient
  « Sources (i) » ; la fenêtre prend la largeur de son contenu.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.232.0.0. Non validé en jeu.
### English
- Fishing help as a compact table, one row per fish (fish, bait route, coloured bite and hookset, bite time, conditions only when any);
  full texts in tooltips; the window fits its content.

## 0.231.0
- Retour : il manque le temps de pêche des poissons. GatherBuddy ne publie pas de temps de touche (il les mesure sur le jeu du joueur) ;
  Teamcraft (MIT) les publie par son API publique (docs/evidence/0.231-teamcraft-bite-times.md).
- « Touche après 13–26 s (souvent 13 s) » par poisson dans l'aide à la pêche et dans « Comment le pêcher » (par lieu), d'après les relevés
  Teamcraft (secondes ayant au moins 3 relevés et 2 % du total ; `FishBiteTimeSummary` testé), une requête par lieu en cache.
  Chrono dans l'aide à la pêche (depuis le début de la pêche, bouton « Relancer » à chaque lancer) ; le temps de touche passe en vert
  quand le chrono est dans l'intervalle. Paramètres › Avancé : « Teamcraft (temps de touche) ».
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.231.0.0. Non validé en jeu.
### English
- Fishing bite times (« bite after 13–26 s, usually 13 s ») per fish and spot from Teamcraft's public reports, in the fishing help and the
  Gathering page; a stopwatch in the fishing help turns the bite time green while inside the range. New switch in Settings › Advanced.

## 0.230.0
- Retour : aide à la pêche 0.229 vue en jeu (touches et ferrages GatherBuddy affichés). Demande : revoir le module Récolte / pêche pour
  utiliser GatherBuddy en priorité, interface plus pratique, garder les liens avec les autres modules et ajouter ceux qui manquent.
- Page Récolte : en tête l'objet, l'heure ET et les icônes des autres modules utiles pour lui (mêmes que l'objet en contexte : usages,
  artisanat, vendeurs, marché, butin, monstres, quête, collection, stocks). Pour un poisson, section « Comment le pêcher · GatherBuddy »
  ouverte en premier : route d'appâts en icônes (clic = fiche de l'appât), touche et ferrage, leurre, accrochage, proies, créneau ET avec
  les prochaines ouvertures, météo et météo précédente, puis alarme (chaque jour ET avec la condition météo), météo de zone, carte et aide
  à la pêche. Tableau des lieux : créneau GatherBuddy au lieu de « Horaire ET inconnu » pour la pêche. Carnet du jeu, Garland (secours)
  et lecture du point de récolte passent en bas.
- Verify.ps1 (agent) : 556/556, 0 avertissement, 0 erreur ; DLL 0.230.0.0. Non validé en jeu.
### English
- Gathering page: the item's other modules first; for a fish, a « How to catch it · GatherBuddy » section first (bait route, bite and
  hookset, lure, snagging, predators, ET window with next openings, weathers, alarm, zone weather, map, fishing help); fishing rows show
  GatherBuddy's window; the game log, Garland and the live node reading move down.

## 0.229.0
- Retour (capture du panneau en pêche, « Pêche en cours » détecté) : appâts venus de Garland sans touche ni ferrage, lignes « Horaire ET
  inconnu / non couvert » partout. Cause : les fichiers anciens de GatherBuddy alignent leurs appels (« data.Apply     (4869, … ) ») et
  écrivent une mooch comme chaîne complète (« Mooch(data, 2585, 4869) ») ; le lecteur exigeait « Apply( » collé : tous ces poissons
  étaient ignorés. Correction : espaces acceptés avant la parenthèse, route complétée depuis une chaîne entière ou un dernier poisson.
  Vérifié sur les 36 vrais fichiers (lus le 2026-11-01) : 2 616 poissons lus (au lieu d'environ 1 180) ; test ajouté sur l'ancien format.
  Les lignes d'horaire de Garland ne s'affichent plus que pour un vrai créneau ET.
- Verify.ps1 (agent) : 556/556, 0 avertissement, 0 erreur ; DLL 0.229.0.0. Non validé en jeu.
### English
- Fishing help: GatherBuddy's older data files (aligned calls, whole mooch chains) are now read too — 2,616 fish instead of about
  1,180 — so ARR fish show their bite and hookset; Garland's « unknown window » lines are no longer shown.

## 0.228.0
- Demande : un bouton pour ouvrir l'aide à la pêche (doute sur la détection de la pêche).
- Ouverture manuelle : bouton « Ouvrir l'aide à la pêche » dans les pages Récolte, et commande `/tensho peche` (ou `fish`) ; la fenêtre
  reste ouverte quel que soit le métier, même sans pêcher, et se ferme par sa croix ou le même bouton. Elle indique « pêche en cours »
  ou « lieu le plus proche (pas en train de pêcher) », et « aucun lieu de pêche connu dans cette zone » le cas échéant. L'ouverture
  automatique au lancer (condition Dalamud « Fishing ») reste en place.
- Verify.ps1 (agent) : 556/556, 0 avertissement, 0 erreur ; DLL 0.228.0.0. Non validé en jeu.
### English
- Fishing help can be opened by hand: button on the Gathering pages and `/tensho fish`; it stays open on any job, even when not fishing,
  until closed. The automatic opening when casting stays.

## 0.227.0
- Demande : utiliser GatherBuddy (Apache-2.0) pour les appâts plutôt que Garland ; question « Lumina au lieu de Garland ? » : les appâts
  ne sont ni dans les données du jeu ni dans LuminaSupplemental (le jeu donne déjà lieux, poissons et positions).
- `Community/GatherBuddyFish.cs` : téléchargement unique des fichiers de données de pêche de GatherBuddy (liste via l'API GitHub, cache
  « gatherbuddy-fish ») et analyseur original des instructions « data.Apply(…) » : appât, poisson à réutiliser (route complète), touche
  (« ! », « !! », « !!! ») et ferrage, créneau ET, météo et météo précédente, leurre, proies à attraper d'abord, accrochage. Testé sur un
  extrait réel.
- Aide à la pêche : GatherBuddy en premier (route d'appâts, touche/ferrage, créneau, leurre, météos nommées par la feuille Weather, proies),
  Garland en secours. Paramètres › Avancé : interrupteur « GatherBuddy (pêche) ». Crédit dans README, références et pied du panneau.
- Verify.ps1 (agent) : 556/556, 0 avertissement, 0 erreur ; DLL 0.227.0.0. Non validé en jeu.
### English
- Fishing help now uses GatherBuddy's fish data (Ottermandias, Apache-2.0, downloaded at runtime and parsed as data): bait route with
  mooches, bite and hookset, ET window, weathers, lure, predators and snagging; Garland stays as a fallback. New switch in Settings ›
  Advanced.

## 0.226.0
- Demande : une aide à la pêche, avec les appâts une fois la pêche lancée (et une aide du style Distant Seas).
- Panneau « Aide à la pêche · <lieu> » pendant la pêche (pêcheur, condition « Fishing ») : lieu de pêche le plus proche du joueur dans
  sa zone (feuille FishingSpot, coordonnées prouvées par `PROBE_FISHSPOT`, docs/evidence/0.226-fishing-spot-coordinates.md), ses
  poissons (données du jeu) avec ✓/✗ de collection et stock, et pour chacun les appâts de ce lieu (appât → poisson à réutiliser), créneau
  ET et météo d'après Garland (fiche de l'objet, une requête à la fois, cache ; la fiche Garland est maintenant lue aussi pour la pêche).
  Fenêtre déplaçable ; réglage « Aide à la pêche en jeu » (Paramètres › Modules › Récolte, activé par défaut).
- Distant Seas (AGPL, lu seulement) concerne la pêche en mer (croisière) et s'appuie sur un tableur communautaire (Fisherman's Horizon) :
  non repris faute de source de données utilisable ; la pêche en mer n'est pas couverte par ce panneau.
- Verify.ps1 (agent) : 555/555, 0 avertissement, 0 erreur ; DLL 0.226.0.0. Non validé en jeu.
### English
- In-game fishing help: while fishing, a panel lists the fish of the nearest fishing spot (game data) with their bait routes for that spot
  (bait → mooch), ET window and weather (Garland community data). Setting « In-game fishing help » (on by default). Ocean fishing is not
  covered.

## 0.225.0
- Retour (capture de la fenêtre « Extraction de minerai ») : aucune aide en jeu à la récolte ; bien séparer chaque métier.
- Panneau « Aide à la récolte · Mineur / Botaniste » à droite de la fenêtre de récolte du jeu : en-tête du métier (icône de classe et
  couleur propres), niveau, PR et essais ; chaque objet du point avec réussite, récolte supplémentaire, Abondance estimée (0.224) et
  stock (clic = objet en contexte, infobulle complète) ; plan de PR conseillé pour l'objet choisi (objet en contexte, sinon le premier)
  avec les actions du métier, calculé comme l'outil PR et seulement quand les valeurs lues changent. Position lue avec le point (thread
  framework). Réglage « Aide à la récolte en jeu » (Paramètres › Modules › Récolte, activé par défaut) : active la lecture de la fenêtre.
- Verify.ps1 (agent) : 555/555, 0 avertissement, 0 erreur ; DLL 0.225.0.0. Non validé en jeu.
### English
- In-game gathering help: a panel beside the game's gathering window, headed by the job (miner or botanist, own icon and colour), listing
  each item of the node (success, gatherer's boon, estimated Bountiful bonus, stock) and the advised GP plan with that job's actions.
  Setting « In-game gathering help » (on by default).

## 0.224.0
- Plan avant publication, point 1 : bonus de Récolte abondante II (« Abondance ») dans l'outil PR de récolte.
- Recherche dans l'existant : GatheringOptimizer (MIT) le laisse manuel ; frozen_rabbit_tome (sans licence, règle relevée sans copie) :
  +3 si récolte ≥ ⌊base × 1,1⌋, +2 si ≥ ⌊base × 0,9⌋, sinon +1. Base prouvée par sonde `PROBE_GATHERLEVEL` : colonne Gathering de la
  feuille ItemLevel à la ligne GatheringItem.GatheringItemLevel (3420 pour un minerai de niveau 90 ; la ligne « niveau » donnait 600,
  fausse) — docs/evidence/0.224-bountiful-base.md.
- Outil PR : à côté du champ « bonus », « Estimé : +N » pour l'objet en contexte, avec la valeur de récolte du personnage en mineur ou
  botaniste ; infobulle : récolte, base, paliers +2/+3, ce qui manque pour le palier suivant, règle communautaire ; bouton « Utiliser »
  qui remplit le champ (toujours modifiable). `Gathering/BountifulEstimate.cs` testé.
- Verify.ps1 (agent) : 555/555, 0 avertissement, 0 erreur ; DLL 0.224.0.0. Non validé en jeu.
### English
- Gathering GP tool: Bountiful Yield/Harvest II bonus estimated (+1/+2/+3) from the character's gathering rating against the item's base
  value (game sheet ItemLevel, Gathering column, at the item's GatheringItemLevel row), using a community rule (+3 from 110 %, +2 from
  90 %); shown as an estimate with a « Use » button, the field stays editable.

## 0.223.0
- Retour (capture) : le cadre ne tenait pas à l'icône (il couvrait toute la ligne avec la zone de texte) ; il faut une petite fenêtre à côté
  de l'emplacement choisi plutôt qu'un panneau global sur le côté.
- Bouton et cadre limités au carré de l'icône (bout de la ligne côté mannequin : droite pour la colonne de gauche, gauche pour celle de
  droite). Le panneau latéral listant les 11 emplacements est remplacé par une petite fenêtre ouverte à côté de l'emplacement cliqué
  (à gauche de la fenêtre du jeu pour la colonne de gauche, à droite pour celle de droite, alignée sur la case) : pièces et teintures,
  croix pour fermer. La page Tensho garde la liste complète. Option renommée « Boutons sur la Revue de mode du jeu ».
- Retour : le cadre bleu (couleur d'accent du thème) était peu visible sur le parchemin : cadre rouge.
- `commit-release.bat` (fichier de l'utilisateur, revu) : lancement par PowerShell (cmd n'exécute pas un .ps1 directement), pwsh si présent
  sinon Windows PowerShell 5.1, dossier du dépôt quel que soit le lecteur, release seulement si le commit a réussi, mode « dry » et
  « commit », fenêtre gardée ouverte en double-clic, code de sortie transmis (Verify.ps1 renvoie celui de l'étape git) ; fins de ligne
  CRLF (`.gitattributes`). Testé : `commit-release.bat dry` (Verify 554/554, commandes de commit affichées, release arrêtée en code 2).
- Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ; DLL 0.223.0.0. VALIDATED_IN_GAME : « c'est ok » (capture : cadre rouge sur l'icône Pieds, petite fenêtre « Pieds · Bibliophilie » avec pièces et teintures, utilisateur, 2026-11-01).
### English
- Fashion Report: the button and highlight now cover only the slot icon; the side panel listing every slot is replaced by a small
  window opened beside the clicked slot (gear and dyes). The Tensho page keeps the full list. The highlight is now red.
- `commit-release.bat`: verify, commit and release in one double-click (`dry` and `commit` modes), stopping before the release when the
  commit step fails.

## 0.222.0
- Retour (capture) : même icône t-shirt sur chaque case au lieu d'une icône adaptée à l'emplacement ; cases de droite sans bouton.
- Bouton invisible posé sur toute la case de l'emplacement (l'icône du jeu reste visible : chaque case garde la sienne), cadre doré au
  survol et quand l'emplacement est ouvert ; bouton sur les 11 cases, aussi sans catégorie cette semaine (le panneau l'indique, et
  montre les teintures d'arme à pieds). Cause des cases de droite : elles étaient ignorées sans catégorie. Mode développeur : le journal
  donne aussi la position des 11 cases (si une case de droite manque encore, sa position n'est pas lue : envoyer cette ligne).
- Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ; DLL 0.222.0.0. Non validé en jeu.
### English
- Fashion Report: the Tensho button now covers each slot of the game window invisibly, so every slot keeps the game's own icon, with a
  golden frame when hovered or open; buttons on all 11 slots, including slots without a category this week (their dyes still show).
- Developer mode logs the 11 slot rectangles with the categories.

## 0.221.0
- Retour : « le git ne se fait pas en anglais ». Les messages de commit et les notes de release GitHub viennent désormais de la sous-section
  `### English` de l'entrée du CHANGELOG (le texte français au-dessus reste pour le suivi) ; `Release.ps1` s'arrête (en jaune) si elle
  manque ou est vide. Sections anglaises ajoutées aux entrées 0.219.0 et 0.220.0. Règle ajoutée à CLAUDE.md et CONTRIBUTING.md.
- Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ; DLL 0.221.0.0. Aucun changement du plugin. Testé : -Commit -DryRun (section anglaise trouvée).
### English
- Git commit messages and GitHub release notes now come from the `### English` section of each CHANGELOG entry; the release script
  stops if it is missing or empty.
- No plugin change.

## 0.220.0
- Demande : éviter qu'une version déjà envoyée soit commitée ou publiée deux fois.
- `Verify.ps1 -Release` : arrêt immédiat, avant compilation, si le tag `Stable-x.y.z` existe (ici ou sur GitHub), message clair (code 2).
- `Release.ps1` : `-Release` s'arrête aussi si rien n'est nouveau sur Devloppement depuis `origin/main` ; `-Commit` après la release
  d'une version refuse de commiter du code (monter `<Version>` et ajouter l'entrée du CHANGELOG), accepte la documentation seule
  (titre « x.y.z · docs ») ; titre « x.y.z · suite » pour un nouveau commit avant la release ; pas de push si GitHub est déjà à jour.
  Arrêts attendus en jaune, sans pile d'erreur. Testé (-DryRun, Windows PowerShell 5.1) : release 0.219.0 refusée avant compilation,
  commit de code sur 0.219.0 refusé.
- Retour (capture) : `-Release` s'arrêtait sur « git.exe : From https://github.com/… » : Windows PowerShell 5.1 change en erreur fatale
  toute ligne de git sur stderr quand elle est redirigée. `Invoke-Git` capture stderr en texte et ne juge que le code de sortie.
  Arrêt survenu avant toute action (rien fusionné, tagué ni publié). Arrêts « fichiers non commités » et « tag existant » en jaune.
- Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ; DLL 0.220.0.0. Non validé en jeu (aucun changement du plugin).
### English
- Prevents committing or releasing the same version twice.
- `Verify.ps1 -Release` stops before building when the `Stable-x.y.z` tag already exists (locally or on GitHub).
- `Release.ps1`: `-Release` also stops when `Devloppement` has nothing new since `origin/main`; after a release, `-Commit` refuses code
  changes under the released version (raise `<Version>` and the CHANGELOG entry) but accepts documentation only (« x.y.z · docs »);
  « x.y.z · suite » for further commits before a release; no push when GitHub is already up to date. Expected stops are shown in yellow.
- Fix: under Windows PowerShell 5.1, git messages on stderr (e.g. the « From https://… » line of a fetch) no longer abort the release.
- No plugin change.
## 0.219.0
- Demande : prévoir avec Verify.ps1 le commit et la release git ; vérifier l'existant et les .md ; puis reprendre la Revue de mode.
- Existant vérifié : `Stable` locale publiée vers `origin/main`, tags `Stable-0.217.0` et `Stable-0.218.0` publiés, branche distante
  `Devloppement` en retard (ancêtre de Stable), workflow CodeQL seul, GitHub CLI absent. Choix de l'utilisateur : commit + tag + push,
  travail sur `Devloppement` puis `main` (retour : la branche de release sur GitHub est `main`, `Stable` n'est qu'un nom local), release via GitHub CLI, dépôt Dalamud personnalisé (repo.json).
- `scripts/Release.ps1` (appelé seulement après un Verify vert) : `Verify.ps1 -Commit` (sur Devloppement : commit « x.y.z » avec l'entrée
  du CHANGELOG, push) ; `Verify.ps1 -Release` (repo.json, main local remis sur origin/main, fusion Devloppement → main, push main, tag `Stable-x.y.z`,
  `gh release create` avec latest.zip et les notes, mise à jour de Devloppement) ; `-DryRun` affiche les commandes. Garde-fous : branche,
  entrée du CHANGELOG, arbre propre, tag existant, gh absent. Verify affiche aussi « VERSION x.y.z (DLL …) » et ne relance pas les contrôles si la même version et les mêmes sources (hors .md, docs/, repo.json) sont déjà passées au vert (`artifacts/verify-stamp.json`, `-Force` pour relancer) ; recherche de gh dans son dossier d'installation ; compatible Windows PowerShell 5.1. Documentation : CONTRIBUTING
  « Versions and releases », README (URL du dépôt personnalisé), CLAUDE.md étape 5. Testé : syntaxe des deux scripts, garde-fou de branche
  (-DryRun). Lancé par l'utilisateur le 2026-11-01 : -Commit puis -Release réussis (release GitHub Stable-0.219.0 avec latest.zip, main, repo.json en ligne) ; un second -Release de la même version est refusé (tag existant).
- Revue de mode : un bouton Tensho (t-shirt) sur chaque ligne d'emplacement de la fenêtre du jeu (nœud 8 + n, position lue avec les
  catégories) ; clic = ouvre cet emplacement dans le panneau (pièces et teintures), nouveau clic = ferme. Petites fenêtres séparées pour
  laisser le reste de la fenêtre du jeu cliquable.
- Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ; DLL 0.219.0.0. Non validé en jeu.
### English
- Release tooling: `Verify.ps1 -Commit` (commit and push `Devloppement`) and `Verify.ps1 -Release` (merge into `main`, tag
  `Stable-x.y.z`, GitHub release with `latest.zip`, `repo.json` custom Dalamud repository); `-DryRun` prints the commands; checks are
  skipped when the same version and sources already passed (`-Force` runs them again).
- Fashion Report: a Tensho button on each slot row of the game window opens that slot (gear and dyes) in the Tensho panel.
## 0.218.0
- Retour (capture, clic droit sur la carte › Récolte) : le menu d'un point ouvrait toujours la première ressource. Le clic droit liste
  maintenant chaque ressource du point avec son icône (infobulle complète) ; celle choisie s'ouvre dans l'outil Récolte.
- Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ; DLL 0.218.0.0. Non validé en jeu.
## 0.217.0
- Retour (captures) : ne pas retirer les teintures des emplacements sans catégorie ; mettre les icônes (boutons) sur les cases d'équipement
  de la fenêtre du jeu. Fait : un emplacement teignable (arme à pieds) sans catégorie s'ouvre et montre ses teintures de la semaine.
- Non fait (quota 97 %) : boutons sur les icônes d'emplacement de la fenêtre « Revue de mode » du jeu.
- Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ; DLL 0.217.0.0. Non validé en jeu.
## 0.216.0
- Retour (captures) : interface mal agencée ; chaque emplacement doit être une ligne qui ouvre sa liste au clic, pas tout d'un coup.
- Revue de mode (page et panneau) : un seul emplacement ouvert à la fois (clic sur la ligne : ▶/▼, catégorie, possédés / signalés, ✓) ;
  la liste des pièces et des teintures s'ouvre dans un cadre défilant sous la ligne ; les emplacements sans catégorie cette semaine sont
  seulement listés ; le nombre de signalements est abrégé « ×28 » (texte complet en infobulle) pour ne plus couper les lignes.
- Non fait : boutons posés sur les cases de la fenêtre du jeu (autre option demandée) — à faire à la reprise si souhaité.
- Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ; DLL 0.216.0.0. Non validé en jeu.
## 0.215.0
- Demande : les teintures de la Revue de mode. FashionReport.json (FFXIVGachaSpreadsheet) contient « WeeklyDyes » : semaine (même
  numéro que FashionWeek.Number, 442–457 lus le 2026-11-01) → 6 emplacements (Weapon, Head, Body, Hands, Legs, Feet) → teinture
  (Count, Pct). Lu sans rejeter les données d'équipement si la section est mal formée (`FashionDataset.WeeklyDyes`, `DyesFor`).
- Sous chaque emplacement teignable (arme à pieds) : « Teintures signalées cette semaine » avec pastille de couleur et nom (feuille Stain),
  part en % et nombre de signalements, 10 au plus, triées par part.
- Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ; DLL 0.215.0.0. Non validé en jeu.
## 0.214.0
- Retour (capture) : l'infobulle d'une pièce non possédée était grisée aussi. Cause : l'opacité de la ligne restait active pendant le
  dessin de l'infobulle et du menu. Correction : opacité retirée juste après la ligne, avant l'infobulle et le clic droit.
- Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ; DLL 0.214.0.0. Non validé en jeu.
## 0.213.0
- Retour : 0.212 « c'est ok ». Demande : examiner Avant-Garde et refaire la page Gold Saucer › Revue de mode dans le même esprit.
- Avant-Garde (AGPL-3.0) lu sans copie (docs/project/REFERENCES.md, REF-FASHION-AVANTGARDE).
- Page Revue de mode : section « Cette semaine » en tête — thème de la semaine (feuille FashionCheckWeeklyTheme), prochaine revue, les
  11 emplacements avec la catégorie lue dans la fenêtre du jeu (`FashionCheckReader`, fenêtre « FashionCheck », AtkValue 2 + 11 × n),
  le nombre de pièces possédées / signalées, et par emplacement la liste des pièces : possédées en premier, non possédées grisées,
  tri par popularité (options enregistrées), infobulle complète et clic droit vers les autres outils. L'ancienne liste de toutes les
  catégories reste dessous. Données d'équipement téléchargées automatiquement à l'ouverture de la fenêtre du jeu.
- Panneau « Revue de mode · Tensho » à côté de la fenêtre du jeu quand elle est ouverte (désactivable). Mode développeur : journal des
  11 catégories lues (preuve à relever).
- Test ajouté (semaines, ligne de thème, ordre des emplacements). Verify.ps1 (agent) : 554/554, 0 avertissement, 0 erreur ;
  DLL 0.213.0.0. Non validé en jeu.
## 0.212.0
- Demande : organiser les icônes (Réglages › Carte › Icônes) par catégorie ; retour (capture) : léger décalage des lignes Tensho.
- Catégories repliables avec nombre d'icônes et boutons « Tout afficher / Tout masquer » : Couches Tensho, Quêtes, Objectifs et événements,
  ALEA, Récolte, Boutiques, Drapeau et cible, Lieux et services du jeu, Autres ; dans chacune, tri par priorité ; le filtre ouvre tout.
  Ligne d'icône extraite dans `DrawIconSizeRow`.
- Décalage : le symbole des couches Tensho était plus étroit que les images du jeu ; il est dessiné dans le même carré (hauteur de ligne).
- Verify.ps1 (agent) : 553/553, 0 avertissement, 0 erreur ; DLL 0.212.0.0. Non validé en jeu.
## 0.211.0
- Retour : « nos icônes c'est ok » (0.210) mais il manque des icônes : quêtes de l'épopée, bleues et autres, quêtes terminées.
- Réglages › Carte › Icônes : toutes les icônes de marqueurs de quête du jeu (EventIconType : MapIconAvailable / MapIconInvalid + 1..IconRange,
  sonde `PROBE_EVENTICONS`, docs/evidence/0.211-event-icons.md) quand les quêtes sont affichées, seulement celles dont l'image existe ;
  nom « Quête de l'épopée / annexe / de déblocage (bleue) / Quête (type n) · disponible|non disponible · variante n ». Même visibilité,
  taille et priorité que les autres icônes.
- Verify.ps1 (agent) : 553/553, 0 avertissement, 0 erreur ; DLL 0.211.0.0. Non validé en jeu.
## 0.210.0
- Retour : 0.209 « c'est ok » (usages). L'icône « Coffre au trésor » existe déjà dans le jeu : l'utiliser pour les coffres de la carte
  Tensho ; ajouter aux réglages les icônes créées par Tensho qui y manquent.
- Coffres proches : icône du jeu MapSymbol 63852 « Coffre au trésor » (docs/evidence/0.139.0-symbols-probe.log) au lieu du symbole
  BoxOpen (gardé seulement si la texture manque).
- Réglages › Carte › Icônes : nouvelles lignes pour les couches de Tensho — coffre au trésor, repères d'exploration, courants éthérés,
  points de récolte proches, membres du groupe — avec la même visibilité / taille / priorité (globale ou par carte) que les icônes du jeu
  (clés hors de la plage des icônes du jeu) ; le dessin de chaque couche suit ces réglages. Infobulle « couche ajoutée par Tensho ».
- Verify.ps1 (agent) : 553/553, 0 avertissement, 0 erreur ; DLL 0.210.0.0. Non validé en jeu.
## 0.209.0
- Retour : « usages ne prend pas en compte les outils adaptés pour trouver les références ». L'onglet Usages ajoute, avant les quêtes et
  lèves : « Monnaie » (boutiques du jeu qui acceptent l'objet comme paiement, par PNJ : lien outil Vendeurs + drapeau, objets achetables
  avec leur prix, clic = fiche ; index `NpcVendorCatalog.PaidWith`) ; « Ce qu'il donne » (LuminaSupplemental ItemSupplement lu dans
  l'autre sens : désynthèse, réduction, contenu, coffres, lots de cartes, Eureka, Bozja… avec ✓/✗ de collection) ; « Quêtes qui le
  demandent » (LuminaSupplemental QuestRequiredItem, noms de la feuille Quest, clic = outil Quêtes). Le menu de l'inventaire et le panneau
  d'objet en contexte proposent Usages aussi pour ces cas.
- Test ajouté (lectures LuminaSupplemental : ce qu'un objet donne, quêtes, PNJ des boutiques non reliées).
- Verify.ps1 (agent) : 553/553, 0 avertissement, 0 erreur ; DLL 0.209.0.0. Non validé en jeu.
## 0.208.0
- Demande : le menu du clic droit de l'inventaire doit suivre la logique de l'objet en contexte et n'afficher que les outils utiles.
- `RelevantRoutes(item)` (MainWindow.ItemContextMenu.cs) décide une seule fois, pour le menu « Tensho — objet » et le panneau « Objet en
  contexte » (mêmes outils, même ordre) : usages (lèves, approvisionnement, aquarium, ingrédient), artisanat (recette qui le produit),
  récolte, PNJ vendeurs (+ LuminaSupplemental), marché (objet vendable), butin des contenus, monstres, quête qui le donne, collection, puis
  quantités, fiche et outils. Un catalogue pas encore chargé garde l'outil (inconnu ≠ aucun). Ouverture commune `OpenRoute`.
- Verify.ps1 (agent) : 552/552, 0 avertissement, 0 erreur ; DLL 0.208.0.0. Non validé en jeu.
## 0.207.0
- Retours (captures) : infobulles qui dépassent l'écran ; outils manquants au clic droit dans l'inventaire ; icônes du carnet de craft qui se
  décalent ; butin : ✓/✗ sur les objets de collection et filtres par type ; panneau de mission affiché en mission aléatoire.
- Infobulles : les 80 infobulles des fenêtres passent par `WrappedTooltip` (retour à la ligne à ~32 caractères de police).
- Menu « Tensho — objet » de l'inventaire : + Prix du marché, Butin des contenus (premier contenu qui lâche l'objet, filtré dessus), Monstres
  qui le lâchent, Ouvrir la collection, Comment l'obtenir (fiche).
- Carnet de craft · métier : cause = l'icône dessinée devenait le dernier élément, le bouton suivant se plaçait après elle (+3 px à chaque
  fois) ; un élément invisible à la place du bouton rétablit l'alignement ; bouton « tous » à la même taille (38×38).
- Butin (outil Butin des contenus et panneau de mission) : pastille ✓ verte (obtenu) / ✗ rouge (non obtenu) sur l'icône des objets de
  collection ; filtre par type (montures, mascottes, cartes, rouleaux, équipement, autres) et « Non obtenus seulement ».
- Panneau de mission : plus affiché pour une mission aléatoire ni sans mission choisie.
- Verify.ps1 (agent) : 552/552, 0 avertissement, 0 erreur ; DLL 0.207.0.0. Non validé en jeu.
## 0.206.0
- Demandes : heure PC une seule fois (en-tête, à côté de la version) ; refaire l'accueil selon les outils actuels ; mode développeur avec un
  maximum d'aide pour la communauté open source ; réglages cohérents et onglet « Avancé » ; un maximum de descriptions en infobulles.
- Horloge : les outils (récolte, météo, alarmes) n'affichent plus que l'heure ET (avertissement en infobulle) ; l'en-tête montre l'heure PC
  et l'heure ET, chacune désactivable (Paramètres › Général).
- Accueil (`MainWindow.Home.cs`) : état des catalogues, travaux en cours, prochaine alarme (clic → module), erreurs ; une tuile par module
  (Artisanat, Récolte, Collections, Quêtes, Carte, Marché, Alarmes, Automatisation, Combat, Gold Saucer) avec une ligne vivante ; objet en
  contexte avec ses icônes d'outils ; annuaire des outils (désactivable).
- Paramètres › Avancé (nouveau, `MainWindow.Advanced.cs`) : mode développeur (désormais enregistré), identifiants du jeu, sources
  communautaires activables (LuminaSupplemental, Garland, wiki, prix Universalis — désactivées = aucune requête), délai entre commandes
  /alarm, suppression de toutes les alarmes (deux clics), copie du rapport de diagnostic (sans données de personnage) et de l'adresse du dépôt.
  Les prix au survol passent d'Interface à Avancé.
- Mode développeur › Diagnostics : section « Contribuer au projet » (dépôt, commande de test, licence, documents, fichiers de la page affichée,
  rôle de chaque dossier, sources de données actives, sondes et exemple de commande, traductions et clés).
- Infobulles sur les réglages (Général, Interface, Données, Alarmes, Avancé, observations, Débogage) et sur les outils de développement.
- Verify.ps1 (agent) : 552/552, 0 avertissement, 0 erreur ; DLL 0.206.0.0. Non validé en jeu.
## 0.205.0
- Retour : le jeu a une commande texte d'alarme (`/alerte "Alerte" et rp 2308 60 es00 silencieux`, `/alarm` en anglais). Sonde
  `PROBE_ALARMCMD` : TextCommand 229, « /alarm » dans toutes les langues (alias FR « /alerte »), sous-commandes anglaises acceptées partout
  (docs/evidence/0.205-alarm-command.md).
- Réglage « Créer aussi les alarmes dans le jeu (commande /alarm) » (Alarmes › Réglages) : les alarmes « chaque jour ET » créées sont
  ajoutées à la fenêtre « Alarmes » du jeu (`/alarm "nom" et rp HHmm rappel seNN [mute]`, nom ≤ 20 car. finissant par l'heure ET) ;
  sous-option pour inclure celles avec condition météo (le jeu ne vérifie pas la météo). Bouton « Envoyer au jeu » par groupe.
  Envoi par `UIModule.ProcessChatBoxEntry`, au clic seulement, une commande / 0,5 s, arrêt à 30 alarmes du jeu, commandes `/alarm`
  construites par Tensho uniquement. Créneau : « dans le jeu » (vu dans la liste du jeu) ou « envoyée, absente du jeu ». Le jeu sonne
  lui-même ces alarmes ; Tensho ne sonne plus pour elles. Suppression dans Tensho : rappel de la supprimer aussi dans le jeu (pas de
  suppression automatique : `DeleteAlarm(index)` non prouvé).
- Test ajouté (syntaxe, nom, garde de commande). Verify.ps1 (agent) : 552/552, 0 avertissement, 0 erreur ; DLL 0.205.0.0. Non validé en jeu.
## 0.204.0
- Retour : « ça ne sépare pas les alarmes par métier ». Le module Alarmes est maintenant découpé en sections repliables par métier
  (Mineur, Botaniste, Pêcheur, avec icône et nombre d'alarmes), puis « Autres alarmes (météo, repères, personnelles) » ; dans chaque
  section, les groupes par objet triés par prochaine sonnerie. Le métier n'est plus répété sur chaque groupe.
- Verify.ps1 (agent) : 551/551, 0 avertissement, 0 erreur ; DLL 0.204.0.0. Non validé en jeu.
## 0.203.0
- Demande : les alarmes doivent afficher le métier avec lequel l'objet est récolté. Chaque groupe d'alarmes d'objet affiche l'icône et le
  nom du métier (mineur, botaniste, pêcheur, noms de la feuille ClassJob) ; enregistré à la création (récolte : type du point ; pêche :
  pêcheur), sinon déduit des sources de récolte de l'objet pour les alarmes déjà créées (un seul métier possible, sinon rien).
- Preuve : docs/evidence/0.203-gathering-jobs.md (sonde `PROBE_GTYPE`). Test `JobOf` ajouté au cas des fenêtres d'alarmes.
- Verify.ps1 (agent) : 551/551, 0 avertissement, 0 erreur ; DLL 0.203.0.0. Non validé en jeu.
## 0.202.0
- Demande : alarmes dans un module à part, réglage pour utiliser les alarmes du jeu, affichage regroupé par objet avec icône et infobulle.
- Module « Alarmes (n) » dans la navigation (page dédiée) : réglages repliables, alarmes groupées par objet (icône + infobulle « Comment
  l'obtenir »), ou par météo (icône de la météo) / repère / nom ; par groupe : prochaine sonnerie, actives, rappel, tout supprimer, ouvrir
  l'objet ou la météo ; créneaux dépliables (activer, rappel, supprimer chacun). Groupes triés par prochaine sonnerie. La page Météo
  renvoie au module.
- Réglage « Créer aussi les alarmes dans le jeu » : visible mais désactivé, avec la raison (aucune fonction de création dans l'API ;
  champs natifs Timestamp / sauvegarde non prouvés). Bouton « Copier le relevé des alarmes du jeu » (index, horloge, heure, rappel, son,
  horodatage, drapeaux) pour prouver ces champs sur une vraie alarme avant d'écrire quoi que ce soit.
- Verify.ps1 (agent) : 551/551, 0 avertissement, 0 erreur ; DLL 0.202.0.0. Non validé en jeu.
## 0.201.0
- Demande : terminer la météo, un système d'alarmes relié aux alarmes du jeu, appliqué partout où la météo compte ; alarmes répétables
  chaque jour ET, délai du rappel au choix, tous les créneaux de l'objet sélectionné ; à jour dans la récolte et partout où il y a de la météo.
- Météo : choix de la zone sur la page (toutes les zones avec table météo), 12 périodes, recherche « Quand aura lieu une météo ? »
  (8 prochaines occurrences sur ~3 jours réels), cloche par période et « alarme à chaque <météo> » (périodes 00/08/16 ET avec condition).
- Alarmes Tensho (`Alarms/TenshoAlarm.cs`, testé) : une fois ou chaque jour ET (70 min réelles), rappel N min avant (par alarme, modifiable),
  conditions météo et météo précédente (prévision inconnue = pas de correspondance), 60 au plus, alarmes uniques passées retirées.
  `AlarmService` (thread framework, 1 s) : message dans le chat + son des alarmes du jeu (`AgentAlarm.PlayAlarmSoundEffect`), muet en mission
  au choix ; lit les alarmes du jeu (lecture) et ouvre leur fenêtre. Créer une alarme dans le jeu : aucune API (preuve :
  docs/evidence/0.201-agent-alarm.md).
- Cloche partout où l'heure ou la météo compte : récolte (tous les créneaux ET du point), pêche (tous les créneaux du poisson avec sa météo,
  ou 00/08/16 ET si seule la météo compte ; chaque créneau calculé), repères d'exploration (clic droit sur la carte : créneau ET + météo du
  wiki ; infobulle : prochaine fenêtre). Paramètres › Modules › Alarmes : rappel par défaut, son (+ écouter), chat, muet en mission.
- Verify.ps1 (agent) : 551/551 (3 nouveaux tests d'alarmes), 0 avertissement, 0 erreur ; DLL 0.201.0.0. Non validé en jeu.
## 0.200.0
- Retour (capture « Objet en contexte » : 4 icônes) : ajouter les icônes manquantes des outils appropriés. Toujours : artisanat, récolte,
  usages de l'objet (nouveau), marché, monstres. Selon l'objet, quand les données le relient : PNJ vendeurs (catalogue ou LuminaSupplemental
  ENpcShop), butin des contenus (premier contenu LuminaSupplemental ou Garland, ouvert filtré sur l'objet), quête (récompense), collection
  ou Triple Triad (carte sélectionnée). Les icônes passent à la ligne quand la colonne est étroite.
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.200.0.0. Non validé en jeu.
## 0.199.0
- Retour (captures adamankhélone, adenium insulaire) : dans le menu du clic droit, « Échange » renvoyait sur la monnaie (PGS) alors qu'un
  vendeur est connu ; sans vendeur, rien n'indiquait que le PNJ manque. Correction : la ligne d'échange ouvre le PNJ qui vend l'objet contre
  cette monnaie (outil Vendeurs, + drapeau), la monnaie devient un sous-lien ; sans PNJ connu : « PNJ inconnu ; ouvre la monnaie ».
- LuminaSupplemental (demande « oublie pas LuminaSupplemental ») : ENpcShop (boutiques que les feuilles du jeu ne relient à aucun PNJ) et
  ENpcPlace (position) — les ventes de ces boutiques donnent un lien PNJ avec drapeau. Nouvel index `NpcVendorCatalog.SalesOf`.
- Preuve : docs/evidence/0.199-adenium-shop.md (sonde `PROBE_SELLS` : boutique 1770734 sans PNJ dans le jeu, Garland et LuminaSupplemental).
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.199.0.0. Non validé en jeu.
## 0.198.0
- Retour : l'infobulle (« Comment l'obtenir ») n'est pas appliquée sur toutes les icônes de carte. Cause : seules la liste des cartes manquantes
  et les lots de cartes l'utilisaient ; ailleurs, simple nom ou rien. Correction : `TriadCardTooltip` (fiche de l'objet de la carte si elle
  en a un, sinon nom) sur toutes les icônes : collection Triple Triad, liste et détail du Triple Triad, deck en cours, decks enregistrés,
  deck généré, PNJ (cartes manquantes), lots de cartes.
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.198.0.0. Non validé en jeu.
## 0.197.0
- GOAL COLL-004 (suite) : dernière source LuminaSupplemental, `ItemSupplement.csv` (objet obtenu depuis un autre objet). « Comment l'obtenir »
  (infobulle) affiche, avant Garland : désynthèse, réduction éthérée, contenu d'un objet, jardinage, bâtisseurs d'Ishgard, lots de cartes,
  coffres, Palais des morts, Pilier des Cieux, Eureka Orthos/Anemos/Pagos/Pyros/Hydatos, Bozja, logogrammes, Traversée des pèlerins, Oizys,
  Auxesia ; 3 objets sources nommés + « (+n autres) ». Clic droit : jusqu'à 4 objets sources par type, qui ouvrent la fiche de l'objet source.
- Source : LuminaSupplemental (Critical-Impact, GPL-3.0), colonnes ItemId, SourceItemId, ItemSupplementSource ; types 1–19 de l'énumération
  `ItemSupplementSource`. Fichier en cache comme les autres (limite 4 Mo, fichier ≈ 2,7 Mo).
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.197.0.0. Non validé en jeu.
## 0.196.0
- Retour : le système de macros ne prend pas en charge Macro Chain.
- Option « Chaîner les pages » (Paramètres › Modules › Export, activée par défaut) : chaque page laisse une ligne et se termine par
  « /tenshonext » (marqueur de la Macro Chain de Tensho, `MacroDefinition.Marker`) ; export du simulateur et du carnet d'artisan.
- Après une copie dans les macros du jeu, les emplacements écrits deviennent le plan de la Macro Chain (outil Automatisation), et un bouton
  « Lancer la chaîne de macros » apparaît sous le bouton de copie : les pages s'exécutent l'une après l'autre.
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.196.0.0. VALIDATED_IN_GAME : « macro chain fonctionne » (utilisateur, 2026-10-31).
## 0.195.0
- Retour (capture du panneau du carnet de synthèse) : bouton et prévision coupés à droite. Dans le panneau étroit : libellé court « Copier dans
  les macros du jeu », plage (livre, premier–dernier) dans l'infobulle ; ligne de prévision qui revient à la ligne au lieu de déborder.
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.195.0.0. VALIDATED_IN_GAME (utilisateur, 2026-10-31).
## 0.194.0
- Retour (capture « Macros écrites dans les emplacements 50 à 51. ») : prédire les emplacements utilisés avant la copie, pas seulement le
  signaler après.
- Sous le bouton (export et carnet d'artisan) : « Utilisera les emplacements 50 à 51 (2 macro(s) : 50, 51). » ou « Pas assez d'emplacements
  libres… : rien ne sera écrit », calculé avec la même règle que la copie (`MacroSlotPlan`) sur l'état des 100 emplacements de chaque livre,
  copié une fois par seconde sur le thread framework (rien n'est lu pendant le dessin). Le message du chat reste la confirmation finale.
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.194.0.0. VALIDATED_IN_GAME (prévision affichée, capture utilisateur 2026-10-31).
## 0.193.0
- Demande : notification ou message dans le chat pour prévenir du statut de la copie des macros. Message « [Tensho] Macros écrites dans les
  emplacements a à b » dans le chat du jeu, ou en erreur (pas assez de place, plage invalide, échec), après chaque copie (export et carnet).
  Nouveau service injecté : `IChatGui`.
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.193.0.0. VALIDATED_IN_GAME : capture du message (utilisateur, 2026-10-31).
## 0.192.0
- Demande : même bouton dans le panneau lié au carnet d'artisan, à côté de « Copier bloc ». Bouton partagé (`DrawMacroToGame`) dans l'export
  de macros et dans le panneau du carnet ; même plage et mêmes règles (Paramètres › Modules).
- Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.192.0.0. Non validé en jeu.
## 0.191.0
- Priorité utilisateur : copier les macros de l'outil de génération de macros dans les macros du jeu, avec des réglages pour indiquer où ça
  commence et où est la limite, comme Craftimizer.
- Bouton « Copier dans les macros du jeu (individuelles 80–89) » dans l'export de macros : pages écrites sur le thread framework au clic, via
  FFXIVClientStructs (RaptureMacroModule.ReplaceMacroLines, nom, icône par défaut si aucune, SetSavePendingFlag, puis
  RaptureHotbarModule.ReloadMacroSlots) — même suite d'appels que Craftimizer, référence fonctionnelle sans copie de code.
- Sécurité (`MacroSlotPlan`, testé) : seulement entre le premier et le dernier emplacement réglés ; par défaut seulement dans les emplacements
  vides ou déjà nommés par Tensho (« Tensho 1/3 »…) ; toutes les pages ou aucune ; écrasement des autres macros seulement si l'option est cochée.
- Paramètres › Modules › Export : macros individuelles ou partagées, premier emplacement (0–99), dernier emplacement (limite), nom des macros,
  autorisation d'écraser. Liens du dépôt mis à jour vers Shigu330/Tensho-Assistant (manifeste, CLAUDE.md, publication D17).
- Test : 1 groupe `MacroSlotPlanTests`. Verify.ps1 (agent) : 548/548, 0 avertissement, 0 erreur ; DLL 0.191.0.0. VALIDATED_IN_GAME : « ça fonctionne » (utilisateur, 2026-10-31).
## 0.190.0
- Retour (capture Butin des monstres, Épaule de rroneek : wiki puis Garland, LuminaSupplemental absent) : section « Source principale ·
  LuminaSupplemental » en tête de l'outil, monstres qui lâchent l'objet (MobDrop), lieux toujours par le wiki et Garland en dessous.
- Verify.ps1 (agent) : 547/547, 0 avertissement, 0 erreur ; DLL 0.190.0.0. VALIDATED_IN_GAME : « 188-190 ok » (utilisateur, 2026-10-31). Arrêt quota (5 h à 95 %).
## 0.189.0
- Suite LuminaSupplemental (« continue dans la mesure du quota ») : « Comment l'obtenir » montre aussi, avant Garland et le wiki, les monstres
  qui lâchent l'objet (MobDrop, noms de la feuille BNpcName), les ALEA (FateItem, noms de la feuille Fate), les entreprises de servant
  (RetainerVentureItem) et la boutique en ligne avec son prix (StoreItem). Noms résolus à la construction des données, hors du dessin.
  Reste : ItemSupplement (désynthèse, réduction, coffres, lots de cartes, donjons profonds, Eureka, Bozja) et l'outil Butin des monstres.
- Verify.ps1 (agent) : 547/547, 0 avertissement, 0 erreur ; DLL 0.189.0.0. VALIDATED_IN_GAME : « 188-190 ok » (utilisateur, 2026-10-31).
## 0.188.0
- Demande : ajouter LuminaSupplemental et, si cette source offre plus que le wiki, Garland ou d'autres, l'utiliser en premier, pour les
  autres outils aussi.
- Source : LuminaSupplemental (Critical-Impact, GPL-3.0, CSV « Generated » sur GitHub, clés = identifiants du jeu) ; couverture relevée le
  2026-10-31 : 283 contenus avec coffres de boss, 1 373 coffres de zone avec taux, butin direct des boss. Téléchargée une fois au premier
  usage (4 Mo max par fichier), gardée dans le cache disque « lumina-supplemental » pour la durée « Sources ».
- Butin des contenus (outil et panneau de l'outil de mission) : coffres de boss, butin direct et coffres de zone de LuminaSupplemental en
  premier ; récompenses de Garland gardées ; coffres de Garland seulement pour les catégories absentes de LuminaSupplemental.
- Sources d'objets : « Butin de contenu : … (LuminaSupplemental) » dans « Comment l'obtenir » avant Garland et le wiki, et lien du clic droit
  vers ce contenu. Prochaine étape : monstres (MobDrop), ALEA (FateItem), entreprises (RetainerVentureItem), boutique en ligne (StoreItem),
  désynthèse/coffres/réduction (ItemSupplement).
- Test : 1 groupe `LuminaSupplementalTests` (premières lignes réelles, Sastasha). Verify.ps1 (agent) : 547/547, 0 avertissement, 0 erreur ;
  DLL 0.188.0.0. VALIDATED_IN_GAME : « 188-190 ok » (utilisateur, 2026-10-31).
## 0.187.0
- Retour : coffres de combat et coffres de zone semblent vides.
- Vérification : l'analyseur lit bien les coffres quand Garland les donne (test sur le document réel d'Alexander - Le Fardeau du Père sadique :
  19 objets dans le coffre du boss ; Sastasha : 3 boss et des coffres de zone). Mais pour les contenus récents (Ihuykatumu, Cénote de
  Skydeep, Everkeep…) Garland n'a ni coffre de boss ni coffre de zone, seulement les récompenses. Les feuilles du jeu ne contiennent pas ces
  listes (Treasure = modèles de coffre, InstanceContentRewardItem sans objets) : le contenu des coffres est décidé par le serveur.
- Correction : nombre d'objets dans chaque filtre du panneau, et message clair quand la source n'a aucun coffre pour ce contenu.
  Piste : Duty Loot Preview utilise LuminaSupplemental (Critical-Impact) — boss, coffres de boss, butin direct, coffres de zone par contenu.
- Test : document Garland réel (A4S). Verify.ps1 (agent) : 546/546, 0 avertissement, 0 erreur ; DLL 0.187.0.0. Non validé en jeu.
## 0.186.0
- Retour (capture, Alexander - Le Fardeau du Père sadique : contenu bien identifié depuis l'outil de mission) : interface mal découpée, aucun
  lien avec les autres outils.
- Panneau de l'outil de mission compact : en-tête sur deux lignes, date de récupération et bouton d'actualisation sur une ligne, texte de source
  et limites dans l'infobulle (i), filtre en liste déroulante à côté de la recherche, objets regroupés par origine (récompenses, chaque boss
  avec son nom, coffres) dans des sections repliables, « ✓ » quand la collection de l'objet est obtenue.
- Liens : survol d'un objet = infobulle complète (collection, stock, comment l'obtenir) ; clic droit = fiche de l'objet, prix du marché,
  recettes et usages, collection, puis ses sources (vendeur, succès, quête, contenu…). Même survol et même clic droit dans l'outil Butin des
  contenus.
- Verify.ps1 (agent) : 545/545, 0 avertissement, 0 erreur ; DLL 0.186.0.0. Non validé en jeu.
## 0.185.0
- Demande : butin des contenus lié à l'outil de mission du jeu — sélectionner un donjon, raid ou autre affiche une interface avec le butin
  (équipement, récompenses, monnaies), en prenant Duty Loot Preview comme référence et en l'améliorant.
- Lecture : `AgentContentsFinder.SelectedDuty` (FFXIVClientStructs : type Aucun/Mission aléatoire/Normal + identifiant) pendant que la fenêtre
  « ContentsFinder » est visible, 5 fois par seconde ; l'identifiant normal est pris comme ContentFinderCondition (catalogue des contenus) ;
  le panneau affiche le nom du contenu pour qu'un écart se voie ; en mode développeur, chaque sélection est notée dans dalamud.log (preuve).
- Panneau « Tensho — butin du contenu » à droite de l'outil de mission : même vue que l'outil Butin des contenus (récompenses, boss, coffres,
  recherche et filtres, données Garland Tools en cache), demande faite une fois par contenu sélectionné ; mission aléatoire signalée.
  Réglage dans Paramètres › Données (activé par défaut).
- Verify.ps1 (agent) : 545/545, 0 avertissement, 0 erreur ; DLL 0.185.0.0. VALIDATED_IN_GAME (sélection) : capture utilisateur 2026-10-31, contenu sélectionné correctement identifié.
## 0.184.0
- Retour (capture, Sage niv. 71) : confirme le niveau absent de la table ; Détermination et PM non calculés.
- Correction : modificateur principal pour les niveaux 1 à 100, repris comme données de Character Panel Refined (Kouzukii, licence MIT,
  LevelModifiers.cs, plugin de référence de la feuille de route) ; ses colonnes secondaire/diviseur concordent avec la feuille du jeu ParamGrow
  aux niveaux 50/70/80/90/100, et ses valeurs principales avec les 6 déjà connues. Niveau 71 : 296.
- Test : table niveaux 1 à 100. Verify.ps1 (agent) : 545/545, 0 avertissement, 0 erreur ; DLL 0.184.0.0. VALIDATED_IN_GAME : « c'est ok » (utilisateur, 2026-10-31, stats réelles 0.182–0.184).
## 0.183.0
- Retour : pas de PM pour un soigneur. Cause probable : niveau absent de la table communautaire du modificateur principal (connue seulement aux
  niveaux 50, 60, 70, 80, 90, 100 ; absent des données du jeu), donc régénération de PM et Détermination non calculées, sans message dans le
  panneau. Le panneau le signale maintenant. Table complète non récupérée (page AkhMorning sans tableau lisible) : pas de valeur inventée.
- Verify.ps1 (agent) : 545/545, 0 avertissement, 0 erreur ; DLL 0.183.0.0. Non validé en jeu.
## 0.182.0
- Demandes : clic droit « Contenu » → outil Butin des contenus ; Combat : stats réelles en %, selon le métier équipé, seulement les stats
  utiles, dans une fenêtre à côté des stats du jeu sans doublon, avec infobulles (formule et description) ; données du jeu d'abord.
- Clic droit (sources) : un contenu cité par Garland (identifiant InstanceContent) ouvre ce contenu dans Butin des contenus (prévu en 0.181,
  livré ici).
- Stats réelles (`Character/StatFormulas.cs`) : modificateurs de niveau secondaire et diviseur lus dans la feuille du jeu ParamGrow (niv. 100 :
  420 / 2780, [docs/evidence/0.182.0-paramgrow-probe.log](docs/evidence/0.182.0-paramgrow-probe.log)), catégorie/rôle du métier depuis
  ClassJob ; modificateur principal (440 au niv. 100) et formules depuis la communauté (Allagan Studies), absents des données du jeu.
  Combat : taux et dégâts critiques, coup direct, Détermination, temps de recharge global et bonus de vitesse (Vivacité ou Incantation selon
  la catégorie), Ténacité (tanks), régénération de PM (soigneurs), réduction des dégâts. Récolte/artisanat : leurs trois stats seulement.
- Panneau « Tensho — stats réelles » à droite de la fenêtre Personnage du jeu (seulement les valeurs réelles, pas les brutes déjà affichées),
  survol = description, formule et modificateurs ; réglage dans Paramètres › Données (activé par défaut).
- Test : 1 groupe `StatFormulaTests`. Verify.ps1 (agent) : 545/545, 0 avertissement, 0 erreur ; DLL 0.182.0.0. Non validé en jeu.
## 0.180.0
- Retour (captures) : les liens du clic droit marchent pour un PNJ vendeur (adamankhélone) ; Aeturna (succès connu par le wiki) et
  adenium insulaire (échange Garland contre 200 Certificats végétaux) n'avaient aucune source cliquable.
- Preuve ([docs/evidence/0.180.0-achievement-probe.log](docs/evidence/0.180.0-achievement-probe.log)) : le succès 3185 « Vintage Vogue »
  (« Vêtements vertueux ») existe mais sa récompense (Achievement.Item) vaut 0 : le jeu donne la monture autrement ; seul le nom anglais le relie.
- Correction : nom anglais de chaque succès copié avec le nom français ; une ligne wiki « Achievement » qui nomme un succès s'affiche en
  français (« Succès : Vêtements vertueux ») et devient un lien du clic droit ; l'échange Garland devient un lien vers la monnaie (ses propres
  sources). Copie des collections antérieure relue une fois (états des titres/succès gardés).
- Verify.ps1 (agent) : 544/544, 0 avertissement, 0 erreur ; DLL 0.180.0.0. Non validé en jeu.
## 0.179.0
- Retour (captures Aeturna) : l'infobulle affichait « Succès : achievement table header » et « achievement table row Vintage Vogue » ; le menu
  du clic droit disait « aucune source connue » alors que l'infobulle en montrait une.
- Correction : modèles wiki « achievement table header » (ignoré) et « achievement table row » (nom du succès) → « Succès : Vintage Vogue » ;
  cache « wiki-items » v5. Menu : « Aucune source cliquable dans les données du jeu » + rappel que les sources wiki/Garland sont dans
  l'infobulle, sans lien.
- Test : ligne de tableau de succès. Verify.ps1 (agent) : 544/544, 0 avertissement, 0 erreur ; DLL 0.179.0.0. Non validé en jeu.
## 0.178.0
- Demande : la collection n'est pas cliquable pour faire le lien avec le PNJ vendeur, le succès lié et le reste.
- Clic droit sur une monture, une mascotte, une carte Triple Triade ou un morceau d'orchestrion (Collections), et sur un objet dans les
  listes d'outils : menu de liens vers les sources de l'objet qui le débloque — succès (onglet Succès filtré), PNJ vendeur (outil PNJ
  vendeurs centré sur lui) et « drapeau sur la carte », quête (fiche de la quête), artisanat (recette), récolte. Indication « Clic droit :
  liens vers les sources » dans l'infobulle des collections. Mêmes données que l'infobulle « Comment l'obtenir ».
- Verify.ps1 (agent) : 544/544, 0 avertissement, 0 erreur ; DLL 0.178.0.0. Non validé en jeu.
## 0.177.0
- COLL-004 (sources inconnues, reprise) : capture utilisateur d'Alsvid — la source wiki (campagne Twitch) était déjà affichée, suivie de
  4 lignes parasites (« vendor list y », « contained in », « {{quest list||…}} », « deployed from Y »).
- Cause : ces modèles du wiki génèrent des listes à l'affichage ; leur texte source ne contient aucune acquisition.
- Correction : lignes de générateurs de listes ignorées (vendor/quest/item/recipe/drop… list, contained in, deployed from, fonctions {{#…}},
  PAGENAME) ; cache « wiki-items » passé en v4 (pages relues).
- Test : page brute d'Alsvid Whistle → seule la ligne de la campagne. Verify.ps1 (agent) : 544/544, 0 avertissement, 0 erreur ;
  DLL 0.177.0.0. Non validé en jeu.
## 0.176.0
- Retour (captures) : le second Échangeur d'Auxesia est Kaede « Fouilles archéologiques » — menu « Obtenir des batteries portatives » →
  « Échange de devises », Batterie portative (Auxesia) pour 200.
- Preuve ([docs/evidence/0.176.0-kaede-talk-probe.log](docs/evidence/0.176.0-kaede-talk-probe.log)) : Kaede n'a qu'un dialogue scripté
  (CustomTalk 721862 « CtsWksTreasure_00966 ») sans aucun argument ; la boutique 1771038 (sans nom) est la seule à vendre cet objet.
- Correction : lien observé en jeu ajouté (`NpcShopLinks.ObservedTalkShops` : 721862 → 1771038), règles de copie 11 (le cache des
  marchands est reconstruit). L'autre boutique de batteries (1771006) n'est pas reliée tant qu'elle n'est pas observée.
- Verify.ps1 (agent) : 544/544, 0 avertissement, 0 erreur ; DLL 0.176.0.0. VALIDATED_IN_GAME : « ok » (utilisateur, 2026-10-31).
## 0.175.0
- Retour (Auxesia) : toujours aucun marchand sur la carte malgré la 0.171.
- Preuve ([docs/evidence/0.175.0-auxesia-maps-probe.log](docs/evidence/0.175.0-auxesia-maps-probe.log)) : le catalogue des marchands (cache disque) contient bien Mesouaidonque et Godgyth sur la carte 1246 ; mais Auxesia a 22 cartes (c1w4/01–22) pour le même territoire 1319, qui changent avec la progression de l'exploration cosmique, toutes à la même échelle et au même décalage. Le joueur voit une autre variante que la carte par défaut.
- Correction : `MapCatalog.Equivalent` — une carte du même territoire, même échelle et même décalage montre le même terrain ; marchands et listes du clic droit (quêtes, exploration, courants…) des cartes équivalentes sont affichés.
- Test : 1 groupe (variantes d'Auxesia, sous-sol et autre zone exclus). Verify.ps1 (agent) : 544/544, 0 avertissement, 0 erreur ; DLL 0.175.0.0. VALIDATED_IN_GAME : capture utilisateur 2026-10-31 — Mesouaidonque sous l'Échangeur, Godgyth sous l'Échoppe (Auxesia). Un second Échangeur reste sans marchand (PNJ dont la boutique passe par un script, non relié).
## 0.174.0
- Preuve ([docs/evidence/0.174.0-triad-seldeck-capture.log](docs/evidence/0.174.0-triad-seldeck-capture.log), Kikimo, 17:41) : dans « Choix
  du jeu », les icônes 088NNN sont lues et les 3 rangées reconnues avec les bonnes cartes, mais « challenge kept: no » : entre la fermeture
  de la fenêtre de défi et l'ouverture du choix du deck, aucune des deux n'est affichée pendant quelques images, et le PNJ était oublié.
- Correction : le défi (PNJ, règles, decks) est gardé 30 s après la fermeture de sa fenêtre et rendu au choix du deck ; les % et le cadre du
  meilleur deck peuvent s'afficher.
- Verify.ps1 (agent) : 543/543, 0 avertissement, 0 erreur ; DLL 0.174.0.0. Non validé en jeu.
## 0.173.0
- Retour (capture « Choix du jeu ») : toujours pas de % dans cette fenêtre. dalamud.log ne contient aucune capture de `TripleTriadSelDeck`
  (mode développeur désactivé) : pas de preuve, donc pas de nouvelle supposition.
- Capture renforcée en mode développeur : toutes les textures visibles de la fenêtre (pas seulement les icônes de cartes), ses valeurs, et une
  ligne « Triad deck choice » : rangées trouvées, cartes de chaque rangée, PNJ du défi gardé ou non. Correction suivante sur preuve.
- Verify.ps1 (agent) : 543/543, 0 avertissement, 0 erreur ; DLL 0.173.0.0. Non validé en jeu.
## 0.172.0
- Retour (Kikimo, Chaos + Échange + Tueur d'as) : deck généré 30 % de victoires contre 35 % pour un deck existant.
- Causes : le générateur partait de zéro sans les decks existants, comparait les decks sur 60 matchs (trop bruité avec Chaos) et donnait
  son % final sur d'autres tirages que ceux des decks listés.
- Correction : les decks existants (jeu + Tensho) sont des points de départ et un plancher ; essais sur 150 matchs ; % final calculé sur les
  mêmes tirages que la liste ; le deck généré n'est jamais moins bon que le meilleur deck existant (sinon c'est lui qui est proposé). 8 s.
  Limite : la règle Échange (une carte échangée avant le match) n'est pas simulée.
- Test : plancher du deck existant (groupe du générateur). Verify.ps1 (agent) : 543/543, 0 avertissement, 0 erreur ; DLL 0.172.0.0.
  Non validé en jeu.
## 0.171.0
- Retour (capture, Auxesia, exploration cosmique) : la carte ne semble pas voir les PNJ ; l'infobulle « Échangeur » ne listait personne.
- Preuve ([docs/evidence/0.171.0-cosmic-vendors-probe.log](docs/evidence/0.171.0-cosmic-vendors-probe.log)) : Mesouaidonque (échange de
  crédits, 3 SpecialShop) est bien placé sur la carte 1246 (Level), à 2,3 px d'une Échoppe et 3,7 px de l'Échangeur ; Godgyth (GilShop) est
  dans la disposition de la zone. Le marchand était rattaché à l'icône la plus proche (Échoppe).
- Correction : un marchand rejoint d'abord l'icône de son type à portée (Échangeur s'il n'a que des échanges, Échoppe sinon), puis la plus
  proche ; même règle pour l'icône de l'onglet Vendeurs. La région « ??? » est le nom donné par le jeu lui-même (PlaceName 2405).
- Test : cas Auxesia ajouté (groupe MapVendorIcons). Verify.ps1 (agent) : 543/543, 0 avertissement, 0 erreur ; DLL 0.171.0.0. Non validé en jeu.
## 0.170.0
- Retour 0.169 (capture) : PNJ de Triple Triade « lieu inconnu » (Aurifort l'As de trèfle, Jonas le Valet de pique, Nell la Boit-sans-soif…).
- Cause : ces PNJ ne sont pas dans la feuille Level ; ils sont placés par les fichiers de disposition des zones. Le catalogue lit maintenant
  aussi planevent/planner (en-tête LGB vérifié, comme les marchands PNJ : docs/evidence/0.98.0-lgb-probe.log) pour les PNJ sans position,
  hors du thread framework, une fois au chargement.
- Verify.ps1 (agent) : 543/543, 0 avertissement, 0 erreur ; DLL 0.170.0.0. Non validé en jeu.
## 0.169.0
- Retour 0.168 : « ImGui assertion failed : inner_window == g.CurrentWindow » (imgui_tables.cpp:1219), pile dalamud.log 17:27:37 :
  `MainWindow.Draw` ligne 226 (fin de la table de mise en page).
- Cause : la zone défilante des cartes manquantes (onglet PNJ et cartes) n'était fermée (`EndChild`) que si elle était visible ; ImGui
  exige `EndChild` après chaque `BeginChild`. Même défaut latent dans la colonne d'outils de la carte Tensho. Les deux sont corrigés.
- Verify.ps1 (agent) : 543/543, 0 avertissement, 0 erreur ; DLL 0.169.0.0. Non validé en jeu.
## 0.168.0
- Demandes : trouver les PNJ qui manquent à affronter et les cartes manquantes avec où les trouver ; Triple Triade seulement dans Gold
  Saucer (pas de retour depuis Collections) ; quel lot de cartes acheter en priorité pour débloquer le plus de cartes.
- Preuves ([docs/evidence/0.168.0-triad-sources-probe.log](docs/evidence/0.168.0-triad-sources-probe.log)) : TripleTriadResident = mêmes ids que
  TripleTriad (146) ; objet carte = ItemAction 3357, Data[0] = n° de carte (« Carte dodo » → 1) ; source de carte : AcquisitionType 6 = PNJ
  (+ position), 8 = lot de cartes (objet). PNJ battus : `UIState.IsTripleTriadNpcBeaten` (FFXIVClientStructs) avec l'id TripleTriad, lu
  toutes les 5 s — à confirmer en jeu.
- Triple Triade › onglet « PNJ et cartes » : PNJ à affronter (non battus ou avec des cartes qui manquent ; lieu, coordonnées, clic =
  drapeau ; mise et cartes possibles au survol) ; lots de cartes classés par nombre de cartes manquantes (survol = où acheter) ; cartes
  manquantes avec leurs PNJ et leur lot, survol de l'icône = « Comment l'obtenir » de l'objet carte. Catalogue construit hors du thread
  framework depuis les feuilles.
- Collections : le bouton qui sautait vers Triple Triade est retiré ; Triple Triade reste dans Gold Saucer.
- Test : 1 groupe `TriadNpcCatalogTests`. Verify.ps1 (agent) : 543/543, 0 avertissement, 0 erreur ; DLL 0.168.0.0. Non validé en jeu.
## 0.167.0
- Demande : même principe que titres/succès pour les sacs — au lieu de l'interface d'import, remplir le cache à l'ouverture, signaler dans
  l'outil si une première ouverture est requise, pareil pour tout ce qui en a besoin, réglage dans Paramètres.
- Servants et sacoches de chocobo (normale et supplémentaire) : copiés automatiquement dans le cache à chaque ouverture dans le jeu, puis
  toutes les 30 s tant que la fenêtre reste ouverte (mêmes contrôles qu'avant : inventaire chargé, bon propriétaire, lecture complète, sinon
  l'ancien cache est gardé). Le panneau d'import à côté des fenêtres du jeu n'apparaît plus que si l'option est désactivée.
- Outil Objets › rangements : messages « jamais ouvert » — sacoche, et chaque servant par son nom (liste `RetainerManager` connue après
  l'ouverture de la sonnette), sinon « ouvre chaque servant une fois ».
- Paramètres › Données : « Enregistrer automatiquement les servants et sacoches à leur ouverture » (activé par défaut). Titres et succès
  (0.166) : relus automatiquement dès que le jeu a envoyé leur liste, réglage « Titres et succès » au même endroit.
- Test : 1 groupe (option par défaut, ancienne configuration, choix gardé). Verify.ps1 (agent) : 542/542, 0 avertissement, 0 erreur ;
  DLL 0.167.0.0. Non validé en jeu.
## 0.166.0
- Demandes : titres et succès sans cache ou pas assez longtemps, réglage à ajouter ; % du PNJ visibles mais pas dans « Choix du jeu ».
- Titres et succès : le jeu n'envoie ces listes qu'après ouverture de leur fenêtre, donc une relecture remettait les états en « inconnu ».
  `CompletionMemory` : obtenu reste obtenu ; « non obtenu » gardé tant que la dernière lecture complète a moins que le nouveau réglage
  Paramètres › Données › « Titres et succès (états conservés) » (7 jours par défaut) ; mémoire gardée dans le cache disque (dates de lecture
  complète), même quand la copie des collections est périmée.
- Choix du jeu : la fenêtre de défi reste ouverte dessous, et la 0.165 effaçait alors les rangées lues à chaque passage ; elles sont lues dans
  les deux cas. Icônes de carte petites (087NNN) acceptées en plus des grandes (088NNN). Capture de la fenêtre en mode développeur si besoin.
- Test : 1 groupe `CompletionMemoryTests`. Verify.ps1 (agent) : 541/541, 0 avertissement, 0 erreur ; DLL 0.166.0.0. Non validé en jeu.
## 0.165.0
- Demande : % de victoire dans la fenêtre « Choix du jeu », comme TriadBuddy.
- Fenêtre `TripleTriadSelDeck` (nom présent dans ffxiv_dx11.exe) : rangées de decks reconnues par les icônes de cartes (088NNN = carte NNN,
  preuve 0.163), associées aux decks enregistrés par leurs cartes ; « xx % victoire · yy % nul » écrit à droite de chaque rangée, meilleur
  deck encadré en vert. Le PNJ et les règles du défi sont gardés pendant le choix du deck. Lecture seule.
- Verify.ps1 (agent) : 540/540, 0 avertissement, 0 erreur ; DLL 0.165.0.0. Non validé en jeu.
## 0.164.0
- Demande : faire comme TriadBuddy (surbrillance des bonnes cartes dans la liste du jeu) et améliorer si possible.
- Preuve : capture de l'éditeur de decks ([docs/evidence/0.163.0-triad-editdeck-capture.log](docs/evidence/0.163.0-triad-editdeck-capture.log)) —
  chaque carte de la liste est un nœud image 40×40 de texture `ui/icon/088000/088NNN_hr1.tex` = carte NNN (icône 88000 + id).
- Les cartes du deck Tensho choisi (Triple Triade › Deck, par défaut le dernier enregistré) sont encadrées en vert dans la liste du jeu et le
  cadre suit le défilement ; le nom du deck en surbrillance s'affiche au-dessus de l'éditeur. Sans mode développeur. Lecture seule.
- Verify.ps1 (agent) : 540/540, 0 avertissement, 0 erreur ; DLL 0.164.0.0. Non validé en jeu.
## 0.163.0
- Retour 0.162 : l'aide-mémoire s'affiche bien à côté de l'éditeur de decks (nom `GSInfoEditDeck` confirmé en jeu, rapport utilisateur
  avec capture) ; demande : pas une interface mais une surbrillance des bonnes cartes dans la liste du jeu, comme TriadBuddy.
- Aide-mémoire retiré. Capture en mode développeur de l'éditeur de decks : nœuds image visibles (identifiant, rectangle écran, fichier
  d'icône) et valeurs de la fenêtre, une ligne « Triad edit deck capture » dans dalamud.log par changement. Sert à prouver quelle carte est
  dessinée où avant de l'encadrer.
- Verify.ps1 (agent) : 540/540, 0 avertissement, 0 erreur ; DLL 0.163.0.0. Non validé en jeu.
## 0.162.0
- Demande : après génération, voir les cartes du deck plus facilement que les autres (comme TriadBuddy), pouvoir supprimer le deck,
  les decks enregistrés dans une liste et nommés d'après le PNJ.
- Liste des decks Tensho (`Configuration.TripleTriadDecks` : nom du PNJ, identifiant du PNJ, 5 cartes ; un deck par PNJ, remplacé à la
  génération suivante). Triple Triade › Deck : chaque deck avec ses 5 icônes, « Afficher dans l'éditeur », « Supprimer ». Le panneau de la
  fenêtre de défi donne aussi le % de chaque deck Tensho.
- Aide-mémoire à droite de l'éditeur de decks du jeu (fenêtre `GSInfoEditDeck`, nom tiré du type FFXIVClientStructs `AddonGSInfoEditDeck`,
  à confirmer en jeu) : les 5 cartes du deck choisi en grand — icône, numéro de carte (N°), nom, rareté, chiffres — pour les retrouver
  dans la liste du jeu. Lecture seule ; rien n'est modifié dans le jeu.
- Test : 1 groupe (decks persistés avec le nom du PNJ, configuration ancienne vide). Verify.ps1 (agent) : 540/540, 0 avertissement,
  0 erreur ; DLL 0.162.0.0. Non validé en jeu.
## 0.161.0
- Retour utilisateur 0.160 : « test ok » (conseil en match) ; manquaient la génération de deck depuis la fenêtre de défi du PNJ et les %
  de victoire estimés par deck, comme TriadBuddy.
- Panneau « Tensho — Triple Triade » à droite de la fenêtre de défi `TripleTriadRequest` (observation Triple Triade active) : PNJ, règles,
  % de victoires et de nuls estimés pour chaque deck enregistré dans le jeu et le deck Tensho, bouton « Générer un deck pour ce PNJ »
  (cartes possédées), enregistrement comme deck Tensho. Rien n'est écrit dans le jeu : le joueur compose le deck dans l'éditeur du jeu.
- Données : règles du défi = valeurs 102–105 de la fenêtre (identifiants TripleTriadRule 10, 12, 4, 9 dans la capture du match Munglig =
  règles ensuite affichées en match) ; decks du joueur = `GoldSaucerModule.Decks` (FFXIVClientStructs : nom + 5 identifiants de carte),
  lus seulement si le module appartient au personnage ; deck du PNJ = feuille TripleTriad.
- Estimation (`TriadMatchSimulator`) : 400 matchs simulés par deck, main du PNJ tirée de son deck (fixes + variables au hasard), premier joueur
  au hasard, chaque camp joue le coup qui maximise l'écart après une réponse ; Chaos impose une carte au hasard, Ordre la suivante. Les
  règles Roulette (tirées au début du match) et Aléatoire sont signalées. Estimation : la logique des PNJ du jeu n'est pas connue.
- Génération : cartes possédées classées par leurs chiffres pour les règles (Inversion : petits chiffres), deck de départ valide (≤ 2 cartes
  4★ ou plus, ≤ 1 carte 5★), puis échanges d'une carte gardés s'ils améliorent le score simulé (mêmes tirages pour tous les decks), 6 s.
- Capture de match : seulement en mode développeur (elle coûtait une image de 66 ms au début du match, dalamud.log 14:47:36).
- Tests : 2 groupes `TriadMatchSimulatorTests` (reproductible, deck fort > faible, Inversion + Chaos, rareté, refus). Verify.ps1 (agent) :
  539/539, 0 avertissement, 0 erreur ; DLL 0.161.0.0. Non validé en jeu.
## 0.160.0
- TRIAD-003 étape 1 : conseil pendant le match, superposé à la fenêtre du jeu (comme TriadBuddy) : cadre vert sur la meilleure carte de la
  main et la meilleure case ; texte dans Triple Triade › Match observé. Lecture seule : le joueur pose la carte.
- Preuves (match contre Munglig, PNJ 1019323, 14:39, [docs/evidence/0.160.0-triad-match-capture.log](docs/evidence/0.160.0-triad-match-capture.log)) :
  `TurnState` 1 coup libre / 2 coup imposé / 0 attente (énumération FFXIVClientStructs, 2 observé à chacun des 4 coups du joueur en Chaos) ;
  valeurs 5–9 de la fenêtre = noms des règles du match (Inversion, Bonus, Identique, Chaos — la feuille du PNJ n'en donne que 2, d'où la
  lecture dans le match) ; valeur 25 = carte imposée (4/4 coups) ; PNJ = cible ; rectangles des 5 cartes de la main et des 9 cases.
- Correction : **NumSideR et NumSideL sont inversés dans FFXIVClientStructs** — les 5 cartes jouées par Munglig ne correspondent à son
  deck (TripleTriadCardResident) qu'en échangeant droite et gauche (Garuda U7 R6 D1 L7 lu U7 « R »7 D1 « L »6). L'observation corrige l'ordre ;
  l'observation 0.26–0.159 affichait droite et gauche inversées.
- Cartes cachées de l'adversaire : deck du PNJ (feuille TripleTriad liée par ENpcBase) — toutes les cartes fixes pas encore vues, puis
  chaque combinaison des variables pas vues, équiprobables ; chaque main est résolue par le solveur existant et les coups sont classés par
  écart moyen. Règles jouées par le solveur : Identique, Plus, Inversion, Tueur d'as, Bonus, Malus ; Chaos/Ordre via la carte imposée.
  Refus explicites (pas de conseil inventé) : carte illisible, deck inconnu, cartes adverses hors du deck.
- Tests : 3 groupes `TriadMatchAdvisorTests` sur la position réelle de 14:39:58 (cartes cachées = Cid et Sadu, révélées ensuite ; carte
  imposée respectée ; refus). Verify.ps1 (agent) : 537/537, 0 avertissement, 0 erreur ; DLL 0.160.0.0. VALIDATED_IN_GAME : « test ok » (utilisateur, 2026-10-31).
## 0.159.0
- Demande : « commence Triad » (TRIAD-003, finir l'outil comme TriadBuddy ; plan : [docs/goals/TRIAD-003-PLAN.md](docs/goals/TRIAD-003-PLAN.md)).
- Preuve hors jeu : sonde `PROBE_TRIAD` — 146 lignes TripleTriad, 144 PNJ liés par ENpcData, règles (Roulette, Toutes/Trois sur table,
  Identique, Mort subite, Plus, Aléatoire, Ordre, Chaos, Inversion, Tueur d'as, Bonus, Malus, Échange, Draft), cartes fixes et variables,
  récompenses, 46 PNJ aux règles régionales ([docs/evidence/0.159.0-triad-npc-probe.log](docs/evidence/0.159.0-triad-npc-probe.log)).
  Conséquence : les règles réellement appliquées (Roulette, régionales) ne se lisent que dans le match.
- Capture de match (avec l'observation Triple Triade active) : une ligne « Triad capture » dans dalamud.log à chaque changement de la fenêtre
  `TripleTriad` (état du tour, PNJ ciblé, valeurs de la fenêtre, faces et rectangles des cartes et des cases) et « Triad request capture »
  pour la fenêtre d'avant-match `TripleTriadRequest`. Chaînes lues bornées (240 octets, 60 caractères), 160 valeurs au plus. Sert à prouver
  règles, tour et positions avant de lier l'observation au solveur. Nouveau service injecté : `ITargetManager` (PNJ ciblé).
- Verify.ps1 (agent) : 534/534, 0 avertissement, 0 erreur ; DLL 0.159.0.0. Non validé en jeu.
## 0.158.0
- Demande : « régler d'abord le Mini Cactpot qui ne superpose aucune interface en jeu pour la solution, comme ezMiniCactpot »
  (COLL-004 sources inconnues mis en pause ; Triple Triade ensuite).
- Cause : la solution n'existait que dans la page Tensho, et n'était calculée que lorsque cette page était ouverte.
- Correction : superposition sur la fenêtre du jeu `LotteryDaily`, dessinée à chaque image tant que l'observation Mini Cactpot est active
  (Paramètres › Modules), même fenêtre Tensho fermée : cadre vert sur la ou les meilleures cases, puis sur la ou les meilleures lignes.
  Positions lues au thread framework : `GameBoard` (9 cases) et `LaneSelector` (8 boutons de ligne) de FFXIVClientStructs
  `AddonLotteryDaily` (réflexion sur l'assembly installée 7.57.0.9414) ; correspondance lignes du solveur → boutons du jeu
  (lignes, colonnes, diagonale ↘, diagonale ↙ → 5,6,7,1,2,3,0,4 ; sens des diagonales à confirmer en jeu). Lecture seule : le joueur clique.
  Libellé du réglage : « … et afficher la solution sur la fenêtre du jeu ».
- Verify.ps1 (agent) : 534/534 (+1 test : rectangles complets, copies, correspondance des lignes), 0 avertissement, 0 erreur ; DLL 0.158.0.0.
  Non validé en jeu.
## 0.157.0
- Demande (PUBLISH-018) : « fais-le et déclare créé par IA » pour l'icône, la déclaration d'IA, le nom et le choix du dépôt officiel ;
  puis « l'icône doit montrer une boîte à outils en rapport avec FF14 ».
- Icône 512×512 : boîte à outils rouge et or, cristal bleu (forme de cristal élémentaire, pas le logo du jeu), clé et marteau ; dessinée par
  l'agent avec System.Drawing ; `src/TenshoAssistant/images/icon.png`, copiée à côté de la DLL (icône des plugins de développement).
- Manifeste : nom affiché « Tensho Assistant » (au lieu de « Tensho--Assistant », aussi dans le titre de la fenêtre et l'aide de /tensho) ;
  description : déclaration « code et icône générés avec un agent IA, dirigés et testés en jeu par l'auteur » (la politique IA demande de
  déclarer les éléments graphiques générés dans la description). README : icône déclarée.
- Dépôt officiel choisi : dossier prêt `publish/d17/testing/live/TenshoAssistant/` (manifest.toml, images/icon.png) et brouillon de PR
  `publish/d17/PULL_REQUEST.md` avec déclaration IA niveau « Auto ». La politique exige que la déclaration et le texte de PR soient écrits par
  un humain : brouillon à relire et reformuler par l'auteur. Hash du commit à renseigner après push.
- Verify.ps1 (agent) : 533/533, 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.157.0.0. VALIDATED_IN_GAME (icône) : rapport utilisateur 2026-10-31, visible après relance du jeu (cache d'icônes de Dalamud par session).
## 0.156.0
- Demande (PUBLISH-018, choix utilisateur) : préparer une publication sur le dépôt officiel Dalamud. Règles relues le 2026-10-31
  (soumission, restrictions, politique IA, métadonnées, considérations techniques) ; liste de contrôle : [docs/project/PUBLISHING.md](docs/project/PUBLISHING.md).
- Carte Tensho : passe par le système de fenêtres Dalamud (`MapViewWindow`, `WindowSystem`) au lieu d'un `ImGui.Begin` direct, comme
  Dalamud le demande pour les fenêtres normales ; comportement inchangé (taille, agrandissement, opacité, verrouillage, masquage en
  combat/instance, Échap ne ferme pas la carte, fermeture liée à la carte du jeu).
- Manifeste : nom, accroche et description en anglais à jour (carte, collections, sources communautaires, aucune action sans clic),
  `RepoUrl`, mots-clés ; `InternalName`, `AssemblyVersion` et `DalamudApiLevel` retirés de la source (remplis par DalamudPackager).
  La version ne se change plus que dans le csproj ; `Verify.ps1` refuse ces champs dans la source et compare la version au csproj.
- Audit : aucune requête serveur sans clic, aucune automatisation, aucun identifiant d'autre joueur, aucune télémétrie, requêtes HTTPS
  minimales. Restent à l'auteur : icône (faite main de préférence), `manifest.toml` D17, déclaration IA écrite par lui, nom affiché.
- Verify.ps1 (agent) : 533/533, 0 avertissement (avertissements traités en erreurs), 0 erreur, langues et paquet verts ; DLL 0.156.0.0. Non validé en jeu.
## 0.155.0
- Demande (MAP-017) : « vérifier les icônes des commissionnaires comme Rowena, pour mettre leurs icônes au clic droit et sur la carte ».
- Cause : un marchand ne rejoignait l'icône du jeu que pour Échoppe, Échangeur et Marchand. Les commises de Rowena (objets de classe/job,
  Limsa, Ul'dah, Gridania, Ishgard, Kugane) ont l'icône du jeu 63919 « Commise aux objets de classe/job » à 2–5 pixels : la carte dessinait
  en plus une icône d'échangeur générique, et l'onglet Vendeurs du clic droit montrait cette icône générique.
- Preuve : sonde `PROBE_MARKERS` de `tools/ShopCurrencyProbe` ; recensement des marchands placés par la feuille Level : icône du jeu à moins
  de 8 pixels = Échangeur 103, Échoppe 99, Réparations 15, Officier du logement 6, Mage bleu, Guide, Commise, Commandes spéciales, Dresseur
  (1 chacun) ; 132 sans marqueur ([docs/evidence/0.155.0-vendor-markers-probe.log](docs/evidence/0.155.0-vendor-markers-probe.log)).
- Correction : règle pure `MapVendorIcons` (Maps/MapCatalog.cs) — icônes de boutique à 16 pixels comme avant, icônes de service ci-dessus à
  8 pixels et seulement sans nom de lieu. Le marchand s'ajoute à l'infobulle de l'icône du jeu ; l'onglet Vendeurs affiche la même icône.
- Verify.ps1 (agent) : 533/533 (+1 test), 0 avertissement, 0 erreur, langues et paquet verts ; DLL 0.155.0.0. VALIDATED_IN_GAME : rapport utilisateur 2026-10-31 « carte et rowena ok » (carte 0.151–0.155 comprise).
## 0.154.0
- Nettoyage (CLEANUP-017, demande utilisateur) : `checkpoints/` sorti de git (dossier local) ; 28 preuves déplacées dans `docs/evidence/` et références mises à jour ; ce CHANGELOG généré depuis les notes de vérification.
- Documentation rangée : `docs/ai/` (règles, état, objectif, prompt maître), `docs/project/` (périmètre, feuille de route, backlog, décisions, architecture, tests, références), `docs/goals/` (79 objectifs), index `docs/README.md` ; 78 liens relatifs corrigés.
- Nouveaux/réécrits : CLAUDE.md (entrée IA, procédure par version), CONTRIBUTING.md (build, règles, publication Dalamud D17, politique IA), README (structure, carte Tensho, sources communautaires, déclaration d'usage de l'IA), STATUS et GOAL.
- Paramètres : nouvel onglet « Carte » reprenant tous les réglages de la carte (même contenu que la roue dentée de la carte). Verify.ps1 (agent) : 532/532, 0 avertissement, 0 erreur. Non validé en jeu.
- Relecture de `docs/project/` contre le code (documents seuls, DLL inchangée) : ARCHITECTURE, TESTING, REFERENCES, ROADMAP et BACKLOG ont une partie « état au 0.154.0 » relue contre le code (domaines, carte, sources d'objets, cache disque, services externes, protocole de test, backlog ouvert) et leur historique est marqué comme archive ; décisions DEC-010 (dépôt et documents) et DEC-011 (ordre des sources, outil ALEA) ; hors périmètre précisé dans PROJECT.
## 0.153.0
- Retour utilisateur 0.152 : icônes réglées pour un zoom rapproché deviennent minuscules vers 40 % (captures Solution Neuf).
- Cause : taille strictement proportionnelle au zoom (32 px × zoom).
- Correction : réglage « Suivi du zoom » (MapViewSettings.ZoomFollow, 0–100 %, défaut 50 %) ; facteur = zoom^suivi (1 à 100 %) appliqué aux icônes, symboles et noms de lieux. 50 % : à 40 % de zoom la taille reste 63 %. 0 % = taille fixe comme le jeu.
- Test ajouté (défaut, fixe, borne, invalide). Verify.ps1 (agent) : 532/532, 0 avertissement, 0 erreur. Non validé en jeu.

## 0.152.0
- Choix utilisateur : « Suivre le zoom » (captures Solution Neuf 44 % / 121 %).
- Constat : les icônes du jeu suivaient déjà le zoom (32 px × zoom, plancher 4 px) ; les noms de lieux étaient bornés (0,75 à 2,6 × police) et la pastille de groupe ≥ 14 px, d'où des proportions fausses aux zooms extrêmes.
- Correction : noms de lieux = 2,4 × police × zoom × échelle des noms (plancher 6 px) ; pastille de groupe plancher 6 px. Flèche du joueur inchangée (taille fixe, comme le jeu).

## 0.151.0
- Reprise étape 2 (corrections carte/listes, captures 77–79).
- Marqueurs non rafraîchis : la carte du jeu était fermée (agent inactif → EventMarkers figés). Référence Mappy (AddonAreaMapController, lecture seule) : AreaMap gardée ouverte hors écran. Désormais : AreaMap hébergée hors écran (RootNode à −9000, chaque image) tant que la carte Tensho est ouverte ; fermer l'une ferme l'autre (AgentMap.Hide / fermeture Tensho) ; position restaurée au déchargement …
- Quête derrière un PNJ de changement de zone (Solution Neuf → Sentinelle Neuf) : GameMapRequest.Resolve préfère la carte demandée quand une cible du jeu existe (test ajouté).
- Zones par type : quêtes (marqueur type 3), mandats et autres évènements, ALEA, récolte — case et couleur RGBA (ColorEdit4) ; contour renforcé.

## 0.150.0
- Reprise étape 1 (priorité utilisateur) : traduire les lignes du wiki dans la langue du plugin ; capture Aerith miniature (ligne « clear »).
- Analyseur : modèles de mise en page (clear, -, clr, br) retirés ; ligne réduite à « clear » ignorée ; cache « wiki-items » v3 (pages relues).
- Affichage : WikiTerm traduit les titres et termes connus via la section « WikiTerms » des fichiers de langue (≈ 60 termes : Purchase → Achat, Online Store → Boutique en ligne, Achievement → Succès, PvP → JcJ, Seasonal Event → Évènement saisonnier…) et les deux formules générées (« Boutique en ligne (N USD) », « Récompense de série JcJ (rang N) ») ; texte libre laissé en anglais ; terme absent = …
- Test : {{clear}} non retenu. Verify.ps1 (agent) : 531/531, 0 avertissement, 0 erreur, langues et paquet verts. Non validé en jeu.

## 0.149.0
- Retour utilisateur (capture outil Récolte) : « Temps restant » sans dire avant quoi.
- Correction : « Temps avant la fermeture » pendant une plage ouverte, « Temps avant l'ouverture » sinon (LocalClock.CountdownClose/CountdownOpen, FR/EN).

## 0.148.0
- Retours utilisateur 0.147 (captures) : « achetable au marché » est un critère, pas une source ; lien externe brut (Alsvid) ; encore inconnus (Aeturna, armures de Livia/Nero, baleine lunaire, ballons de céruleum, bénou).
- Preuve : pages brutes du wiki — Bennu Horn et Lunar Whale Identification Key : infobox « online-store-id » + ligne sans puce {{onlinestore|24}} / {{onlinestore|42}} dans ==Acquisition==.
- Correction : lignes sans puce de la section lues ; modèles connus traduits (onlinestore → « Online Store (N USD) », seriesmalmstonesreward → « PvP series reward (rank N) ») ; liens externes réduits à leur texte ; infobox online-store-id → « Online Store » ; cache « wiki-items » passé en v2 (pages relues avec le nouvel analyseur). Marché affiché « Critère : achetable au marché » sans arrêter la r…

## 0.147.0
- Retour utilisateur 0.146 : tous les points OK (VALIDATED_IN_GAME : échanges, Garland en dernier recours, infobulle) ; il reste des inconnus (capture astéropoing).
- Preuve : astéropoing = item 52262 « Rocket Punch Identification Key » ; Garland sans source ; page brute du wiki FFXIV : section ==Acquisition== → ===PvP=== + {{seriesmalmstonesreward|12|25}}.
- Ajout : WikiItemAcquisitionParser (section Acquisition, sous-titres, puces nettoyées : liens, modèles lisibles « nom paramètres », 12 lignes) + WikiItemAcquisitionService (nom anglais du catalogue, une requête toutes les 3 s, HTTP borné, DataCache « wiki-items » pour la durée des sources). Ordre : données du plugin → Garland → wiki → « Aucune source connue (données du jeu, Garland Tools et wiki)…
- Test ajouté. Verify.ps1 (agent) : 531/531, 0 avertissement, 0 erreur. Non validé en jeu.

## 0.146.0
- Retours utilisateur 0.145 (captures Alsvid, buffle insulaire, adamankhélone, armure aérienne magitek) : sources encore inconnues ; ne pas utiliser Garland quand la source est déjà connue ; infobulle déformée (prix en colonne).
- Preuves : sonde (0.146.0-achievement-probe.log) — aucun succès ne récompense 41498 (Sifflet Alsvid), 40382 (Cor buffle insulaire) ni 39486 ; recherche boutiques — 40382 vendu par SpecialShop 1770601 (50 000 × 37549 Assignat insulaire azur) sans PNJ relié (île), 41498 dans aucune boutique ; Garland : 41498 rien, 40382 tradeShops.
- Correction : GarlandItemSourceList.Trades lus dans tradeShops (échanges de l'objet, même sans PNJ) → « Échange : 50 000 × Assignat insulaire azur » ; Garland consulté seulement si aucune source locale (vendeurs, quêtes, succès, fabrication, récolte, marché) ; ligne vendeur repliée puis prix sur sa propre ligne indentée ; message « Aucune source connue (données du jeu et Garland Tools) ».
- Test ajouté (échanges tradeShops). Verify.ps1 (agent) : 530/530, 0 avertissement, 0 erreur. Quota relevé utilisateur : 88 % sur 5 h (réinit. 12:10), 39 % semaine. Non validé en jeu.

## 0.145.0
- Retour utilisateur 0.144 : « Garland est chargé à chaque fois au lieu de faire un cache ».
- Constat (DataCache-v1 sur le PC, 0.145.0-cache-before.log) : dossier plein (≈256 Mo, limite) ; un fichier « quests » de 4,8 Mo par version du plugin (clé contenant la version : 0.120, 0.127, 0.141, 0.144…), deux formes du catalogue d'objets (18 Mo chacune). La rotation supprimait les fichiers les plus anciens : catalogues et sources Garland étaient effacés puis relus.
- Correction DataCache.Trim : un seul fichier par famille + portée (le plus récent ; lu dans l'en-tête), les « catalog-* » ne sont jamais supprimés par le nombre de fichiers, les autres entrées gardent jusqu'à 2048 fichiers, la limite d'octets retire d'abord les petites entrées les plus anciennes. Le nettoyage se fait à la prochaine écriture.
- Test ajouté (nouvelle version remplace l'ancienne, autre portée intacte). Verify.ps1 (agent) : 529/529, 0 avertissement, 0 erreur. Non validé en jeu.

## 0.144.0
- Retours utilisateur 0.143 (capture Alexander articulé) : les sources doivent suivre l'objet qui débloque (pas seulement la monture/mascotte) ; titres et succès terminés mal affichés.
- Preuve Garland : item/fr/3/16574 (Alexander articulé) → partials instance 30042 et 30046 « Alexander - L'Âme du Créateur (sadique) » ; 39486 (Cor Aeturna) sans source.
- Sources Garland : GarlandItemSourceParser (partials instance/mob/fate/quest/achievement/leve/npc, ids texte ou nombre, doublons retirés) + GarlandItemSourceService (à la demande, une requête à la fois, cache « sources »). Affichées dans l'infobulle des collections (par objet) et dans l'infobulle d'objet des outils, avec une section « Comment l'obtenir » (vendeurs, quêtes, succès, fabrication, ré…
- Titres/succès : CollectionService.TitleListLoaded/AchievementListLoaded publiés par le lecteur (thread framework) ; la page affiche en rouge « ouvre une fois la fenêtre Titres/Succès du jeu » tant que le jeu n'a pas envoyé la liste (sans elle, l'état reste inconnu). Cause exacte du retour « mal affichés » non prouvée : à confirmer avec ce message.

## 0.143.0
- Retour utilisateur 0.142 (captures Castor géant, Aeturna) : état « pas encore obtenue » OK (VALIDATED_IN_GAME) ; l'infobulle ne cite pas les récompenses de succès, quêtes…
- Vérification : Garland Tools n'a aucune source pour « Cor Aeturna » (item 39486 : pas de vendors/drops/instances).
- Correction : infobulle « comment l'obtenir » : succès dont la récompense est l'objet (Achievement.Item via l'observation des collections), quêtes (index des récompenses), fabrication, récolte (type, zone), vendeurs PNJ, marché ; si aucune de ces sources : « Aucune source connue dans les données lues (contenu, évènement, boutique en ligne… non couverts) ».
- Limite : butins de contenus/coffres, évènements et boutique en ligne ne sont pas couverts par les données lues.

## 0.142.0
- Retours utilisateur 0.141 (captures Castor purgateur, adamankhélone) : l'infobulle ne sait pas si l'objet est obtenu (« état inconnu (collections non lues) ») ; pas de clic droit objet → collection ; la collection n'a pas d'infobulle « comment l'obtenir ».
- État : montures et mascottes sont lues en direct (IUnlockState) à la connexion ; une fois les collections lues pour le personnage, « non débloqué » vaut « pas encore obtenue » (CollectionStateOf). Les autres catégories gardent la règle de preuve. Texte « collections non lues » remplacé par « état inconnu ».
- Lien : ItemCatalog.ForMount/ForCompanion ; clic droit sur un objet (fiche vendeur, objet choisi de l'outil PNJ) → « Voir dans les collections » (onglet forcé Montures/Mascottes/Emotes/Orchestrion, recherche sur le nom ; cartes → Triple Triade).
- Page Collections : infobulle « comment l'obtenir » pour montures, mascottes, emotes, cartes, rouleaux : état, lieu et description du jeu, objets qui l'enseignent, vendeurs PNJ avec prix, quêtes qui récompensent l'objet, recettes, marché ; « aucun objet du jeu ne l'enseigne » sinon.

## 0.141.0
- Retour utilisateur 0.140 : l'outil PNJ vendeurs n'affiche aucune infobulle d'objet et ne fait pas le lien avec les collections.
- Preuve (sonde, 0.141.0-itemaction-probe.log) : ItemAction.Action 1322 = monture (« Sifflet chocobo » → Mount 1 « chocobo destrier »), 853 = mascotte (« Bombino » → Companion 1 « bombino »).
- ItemDefinition.MountId/CompanionId (ItemUnlockCopy.Lumina) ; forme changée → cache des objets relu une fois depuis le jeu.
- DrawItemCollection : état « déjà obtenue / pas encore obtenue / état inconnu » pour monture, mascotte, carte Triple Triade, rouleau d'orchestrion, d'après l'observation des collections (aucun « non obtenu » sans preuve). Ajouté au survol des objets du jeu et à l'en-tête de l'outil PNJ vendeurs.

## 0.140.0
- Retours utilisateur 0.139 (capture Ose Wyd) : deux icônes pour la même chose (icône du jeu + icône Tensho) ; un PNJ d'échange prend l'icône du vendeur en gils ; pas de lien clic droit vendeur → outil PNJ vendeurs.
- Doublon : un vendeur à 16 pixels de texture au plus d'une icône du jeu Échoppe (60412), Échangeur (60935) ou Marchand (63851) n'a plus d'icône Tensho ; ses boutiques s'ajoutent à l'infobulle de l'icône du jeu (clic = drapeau sur le premier vendeur).
- Icône par type : vendeurs n'ayant que des échanges spéciaux → Échangeur (60935), sinon Échoppe (60412) ; les deux dans « Icônes de cette carte ».
- Lien : onglet Vendeurs du clic droit, clic droit sur un vendeur → « Ouvrir dans l'outil PNJ vendeurs » ; l'outil affiche une fiche vendeur (emplacements avec « Voir sur la carte », stock par boutique avec icône, nom et prix ; clic sur un objet → vue par objet ; bouton retour).

## 0.139.0
- Retour utilisateur 0.138 (capture Ys Gyuf) : infobulle correcte ; le vendeur doit utiliser l'icône du jeu et son infobulle.
- Preuve (sonde, 0.139.0-symbols-probe.log) : MapSymbol 60412 = « Échoppe » (63851 = « Marchand »).
- Correction : les vendeurs sont dessinés avec l'icône 60412 dans la liste commune des icônes du jeu (taille, masquage, priorité, liste au survol avec icône et titre doré, action « placer le drapeau » sur l'icône du dessus) ; l'ancien symbole sac est retiré ; l'icône apparaît dans les réglages « Icônes de cette carte » quand la couche est active.

## 0.138.0
- Retour utilisateur 0.137 (captures) : infobulle récolte OK ; vendeurs : nom de boutique japonais « アイテムの購入 » (Aenc Ose), vendeurs sans boutique affichée (castor géant, Ose Wyd), pas d'indication du nombre d'articles.
- Correction : chaque boutique affiche son nom, ou son type (Boutique (gils) / Grande Compagnie / Échange spécial) si le nom est vide ou un texte japonais non traduit hors langue japonaise ; nombre d'objets distincts en vente ; « N boutique(s) sans vente lue dans le catalogue » ; « Aucune boutique lue » si rien. 6 boutiques au plus puis « … et N autres ».

## 0.137.0
- Retour utilisateur 0.136 : nom du marqueur de mandat OK (VALIDATED_IN_GAME). Suite du plan MAP-016 : PNJ vendeurs.
- Couche « PNJ vendeurs » (désactivée par défaut, section À proximité) : vendeurs du catalogue NPC-VENDORS-014 en cache (positions Level et décors planevent/planner, coordonnées monde X/Z), sac doré ; infobulle nom + boutiques (6 au plus) ; clic = drapeau. Liste par carte mise en mémoire (changement de carte ou de catalogue seulement).
- Clic droit : onglet « Vendeurs » (nombre, nom, coordonnées, boutiques au survol, clic = drapeau).

## 0.136.0
- Retour utilisateur 0.135 : quêtes nommées OK (VALIDATED_IN_GAME : « Tapage nocturne », « Mais si on danse ? ») ; mandat 71041 toujours « Icône 71041 ».
- Preuve (/xllog, 0.136.0-marker-evidence.log) : StringLength = 0 pour tous les marqueurs ; BufUsed = octets + terminateur (71041 : 8 ; 71021 : 24 ; 63922 : 1 = vide), BufSize 32/64.
- Correction : longueur = BufUsed − 1 (comme Utf8String.ToString), bornée à 1–256 octets et ≤ BufSize ; copie exacte de cette longueur. Le mandat doit afficher le texte du jeu (7 octets).

## 0.135.0
- Retour utilisateur 0.134 (captures) : icônes 71041, 71151, 71021 sans nom (« Icône 71041 »…).
- Preuve (/xllog, 0.135.0-marker-evidence.log) : toutes les infobulles « unread » → la règle 0.133 « BufUsed = longueur + 1 » rejetait tout ; type 3 = quête avec DataId = ligne − 65536 (71021 : data 24 ; 71151 : data 141) ; 71041 : objective 393220 (= 6×65536 + 4, d'où la collision 0.133 avec la quête 65540).
- Correction : lecture bornée conservée (1–256 octets, dans le tampon, longueur propre) sans la règle BufUsed ; marqueur de type 3 nommé par la quête exacte (DataId + 65536) ; repli ObjectiveId limité aux quêtes en cours. Trace complétée avec longueur/used/size pour vérifier la lecture.

## 0.134.0
- Retour utilisateur 0.133 (capture Gridania) : noms des quêtes OK ; l'icône de mandat affiche le nom d'une quête (« Dites-le avec des gâteaux », quête 65540).
- Cause probable : quand l'infobulle du marqueur n'est pas lisible, le repli 0.133 cherchait une quête par les 16 bits bas de l'ObjectiveId ; 65540 & 0xFFFF = 4 peut coïncider avec l'identifiant d'un mandat.
- Correction : repli limité aux quêtes en cours (seules leurs étapes portent un ObjectiveId de quête) ; sinon pas de titre et la liste affiche le nom de symbole du jeu (MapSymbol). Trace Information « Map event marker icon … objective, type, data, tooltip read/unread » une fois par icône (64 au plus) pour établir en jeu ce que donne chaque type de marqueur.

## 0.133.0
- Retour utilisateur 0.132 : liste des icônes survolées OK (VALIDATED_IN_GAME) ; le jeu a planté une fois.
- Analyse (dalamud.crashhandler.log, 2026-10-31 08:19:55 ; extrait : 0.133.0-crash-excerpt.log) : pile du thread fautif = TenshoAssistant.Maps.MapLiveReader.ReadEvents → FFXIVClientStructs Utf8String.ToString → Encoding.GetString → Ascii.GetIndexOfFirstNonAsciiByte_Vector : lecture de l'infobulle d'un marqueur d'événement (MapMarkerData.TooltipString) pointant vers une mémoire libérée, sans borne …
- Correction : TooltipText lit seulement une chaîne cohérente (StringLength 1–256, BufUsed = longueur + 1, BufSize ≥ BufUsed et ≤ 4096, pointeur non nul) et copie exactement cette longueur ; sinon "". La carte retrouve alors le nom de la quête par ObjectiveId (EventLabel). Réf. Mappy (lecture seule) : lit StringPtr sans borne lui aussi.
- Limite : une lecture d'une mémoire libérée reste possible si l'en-tête paraît cohérent ; le risque est fortement réduit (lecture bornée à 256 octets), pas éliminé. À surveiller (plantage récurrent ?).

## 0.132.0
- Demande utilisateur : exploration en pause ; voir une liste des icônes de la zone survolée, comme Mappy.
- Référence lue (AGPL, lecture seule, rien copié) : ffxiv-tc-port/Mappy Classes/DrawHelpers.cs DrawTooltip — chaque marqueur survolé, ou dont le rayon contient le curseur, ajoute à la même infobulle une ligne icône 32 px + texte principal + texte secondaire.
- Correction : toutes les icônes du jeu sous le curseur (ou dont la zone le contient : quêtes, ALEA, points de récolte) sont listées dans une seule infobulle, la plus haute d'abord (12 au plus, « … et N autres ») : icône, titre, lignes secondaires ; points de récolte avec horaire et objets (icônes + noms) ; l'icône du dessus indique l'action du clic (téléporter, ouvrir la carte, drapeau). Le clic …

## 0.131.0
- Retour utilisateur 0.130 (capture Stèle du chant marin) : « c'est mieux mais il manque encore d'info » — météo demandée absente, pas d'indication d'emplacement.
- Source ajoutée : page brute du wiki FFXIV (consolegameswiki, Sightseeing_Log, action=raw, ~98 Ko) lue une fois (HTTP borné 25 s), gardée dans DataCache « wiki-vistas » pour la durée des sources. Couverture vérifiée sur la page téléchargée : 340 lignes lisibles (80 ARR avec météo et heure, puis 62/62/45/46/45) = 340 repères du jeu.
- Association : coordonnées à 0,3 près sur la carte du repère ; pour ARR, n° = index de ligne + 1 en plus (vérifié #001–#003) ; deux candidats ou aucun = pas d'association (rien n'est deviné).
- Infobulle : n° de carnet et extension, météo demandée traduite (WeatherCatalog.WeatherBySourceName) en vert si elle correspond à la météo actuelle, rouge sinon ; « où se placer » (commentaire du wiki, en anglais) ; source et date. Repères HW+ : « aucune condition de météo ».

## 0.130.0
- Retours utilisateur 0.129 : les points de récolte à horaire ne se distinguent pas ; exploration : rien ne se déclenche même à la bonne heure ; référence demandée : https://arr-exploration.fr/.
- Référence lue (lecture seule) : site React, données écrites à la main par repère (titre, description, time [8,12], position [9.5,7.8], weather1/weather2, emote) ; heure d'Éorzéa calculée × 1440/70 (identique à Tensho). Mêmes positions/heures/emotes que nos données du jeu ; la météo y est ajoutée à la main (absente des données du jeu). Aucune donnée copiée.
- Récolte : MapLocalGatheringPoint.Schedule/Timed/ActiveAt (fenêtres ET, y compris après minuit) ; pastille horloge verte (ouvert maintenant) ou orange (fermé) sur la carte ; infobulle « À horaire : … — ouvert/fermé maintenant ».
- Exploration : infobulle avec la météo actuelle de la zone (prévisions Tensho) et l'état de déblocage du carnet (PlayerState.SightseeingLogUnlockState : entrées 1–20 palier 1, 21–80 palier 2 ; au-delà, paliers affichés sans règle supposée). Si l'entrée n'est pas débloquée, le jeu ne la valide pas.

## 0.129.0
- Retour utilisateur 0.128 : « exploration toujours rien aux coordonnées de l'icône ».
- Vérification (sonde, 0.129.0-sightseeing-probe.log) : #001 Quais du barracuda (9,6 ; 7,8), #002 Astalicia (7,1 ; 15,1), #003 Stèle du chant marin (20,3 ; 19,1) — identiques au carnet publié (ffxiv.consolegameswiki.com, Sightseeing Log : 9,5/7,8 ; 7,1/15,1 ; 20,3/19,1). Les positions sont justes ; un repère d'exploration est invisible en jeu et ne se valide qu'avec heure, météo et emote.
- Ajout : MapLocalPosition.MinTime/MaxTime (hhmm Éorzéa, ex. 800–1159, 1800–459) et Extra (emote : Scruter, Prier…) copiés de Adventure ; infobulle : fenêtre horaire avec « (en ce moment) » selon l'heure d'Éorzéa, emote, et mention que la météo n'est pas dans les données. Forme changée → cache réécrit une fois.
- Test : fenêtres horaires (y compris après minuit). Verify.ps1 (agent) : 523/523, 0 avertissement, 0 erreur. Non validé en jeu.

## 0.128.0
- Retours utilisateur 0.127 : repères d'exploration mal placés ou état « découvert » ignoré (option Masquer cochée) ; courants éthérés : se cachent bien avec l'option (état lu correctement) ; demander les explorations et courants avec statut dans le clic droit ; liste des icônes incomplète.
- Repères : ClientStructs (PlayerState.cs, main) confirme IsAdventureComplete(index de ligne) et _completedAdventures = 340 bits. Lecture désormais par le tableau de bits brut (bit i = ligne i), la fonction native n'étant que comparée ; trace « Sightseeing: N discovered by the bit array, M by IsAdventureComplete, of 340 » dans /xllog à chaque changement. Coordonnées : Adventure → Level inchangé, à…
- Clic droit : onglets Exploration et Courants (état [x]/[ ]/[?], compteur obtenus/total, n° et coordonnées, image du repère au survol, clic = drapeau).
- Icônes : en mode « Toutes les cartes », tous les symboles de carte nommés du jeu (MapSymbol) + cible du journal ; filtre par nom.

## 0.127.0
- Retour utilisateur 0.126 : coins de pêche OK (VALIDATED_IN_GAME). Suite du plan MAP-016 : groupe, repères d'exploration, courants éthérés.
- Preuves (sonde lecture seule, 0.127.0-sightseeing-probe.log) : Adventure 340 lignes nommées (2162688…2163027), toutes avec Level + carte ; AetherCurrent 448 lignes, 151 liées à une quête, 152 courants de terrain portés par un EObj (Data) que place une Level avec carte (152/152).
- Index « catalog-map-positions » : MapLocalKind.Sightseeing (nom, image découverte) et AetherCurrent (terrain à la Level de l'EObj ; quête au donneur, Data = quête), listes par carte (OnMap). Forme changée → cache réécrit une fois.
- MapLiveReader : courants obtenus (IsAetherCurrentUnlocked) et repères découverts (IsAdventureComplete(ligne − première ligne), index À CONFIRMER en jeu) toutes les 2 s ; membres du groupe de la même zone (IPartyList) toutes les 0,5 s.

## 0.126.0
- Retour utilisateur 0.125 : points de botaniste et mineur OK (VALIDATED_IN_GAME) ; aucun point de pêche.
- Cause : seuls les points de minage/botanique avaient une position (ExportedGatheringPoint) ; les coins de pêche n'étaient pas dans l'index.
- Preuve (sonde lecture seule, 0.126.0-fishing-probe.log) : FishingSpot.X/Z sont des pixels de la texture de carte — spot 2 → 22,90/22,18 et spot 35 → 7,69/12,21, identiques aux coordonnées Garland Tools. Unité du rayon non prouvée (400, 3000) → zone non dessinée.
- Correction : MapLocalKind.FishingSpot dans l'index « catalog-map-positions » (monde = (pixel − 1024)/échelle − décalage) ; la forme change, le cache est réécrit une fois automatiquement. Les coins de pêche apparaissent sur la carte, dans l'onglet Récolte (Pêche) et avec l'infobulle des poissons ; clic = drapeau.

## 0.125.0
- Demande utilisateur : l'infobulle des points de récolte doit afficher les icônes et les noms des objets.
- Correction : DrawGatheringTooltip — icône du type, type · niveau (caché), lieu, puis une ligne par objet (icône du jeu + nom, 16 au plus), « Clic : placer le drapeau ». Utilisée sur la carte (icône du dessus) et dans l'onglet Récolte du clic droit.

## 0.124.0
- Retours utilisateur 0.123 : points de récolte sur la carte ne fonctionnent pas (le clic droit oui) ; la priorité ne change pas celle des quêtes.
- Récolte : la couche ne montrait que les points chargés par le jeu autour du joueur (ciblables seulement avec le bon métier). Désormais, avec « Points de récolte », la carte dessine tous les points connus de la carte (catalogue « catalog-gathering » + positions « catalog-map-positions », mis en mémoire par carte) : icône du type (GatheringType.IconMain), zone (rayon), niveau, survol (type, niveau…
- Priorité : toutes les icônes du jeu (fixes, récolte, ALEA, quêtes, cible du journal) dans une seule liste triée par priorité ; à priorité égale, ordre des couches du jeu. Seule l'icône du dessus réagit au survol/clic (lien de carte, téléportation, drapeau). Zones dessinées sous les icônes. Types de récolte ajoutés à la liste « Icônes de cette carte ».

## 0.123.0
- Retour utilisateur : quête depuis le journal → carte OK (VALIDATED_IN_GAME, zoom et icône). Outil ALEA jugé inutile (pas d'expérience, liste d'objets identique) : à retirer en gardant la partie carte.
- Retiré : page ALEA (Page.Fates, MainWindow.Fates.cs, lien de navigation), FateRewardService/FateRewardParser et leurs tests, route Garland « fate », entrée « Ouvrir dans l'outil ALEA » du clic droit, textes FateUi/Category.Fates.
- Conservé : couche ALEA en cours sur la carte (réglage « ALEA en cours »), onglet ALEA du clic droit (drapeau au clic), tailles/priorités d'icônes.

## 0.122.0
- Retour utilisateur 0.121 : le zoom sur la quête se fait bien (méthode TempMapMarkers validée en jeu) ; mais l'icône de la quête disparaît.
- Cause : la cible est un marqueur temporaire du jeu (pas un EventMarker) ; la carte du jeu étant fermée, il n'était dessiné nulle part.
- Correction : GameMapRequest.TargetIcon (MapMarkerBase.IconId) ; la carte Tensho garde la cible (carte, position, icône) jusqu'à la prochaine demande du jeu et la dessine (taille/masquage par icône respectés), survol « Cible indiquée par le jeu (journal) ».

## 0.121.0
- Retour utilisateur 0.120 : toujours pas de zoom sur la quête ; demande « regarde comment fait Mappy ».
- Référence lue (AGPL, lecture seule, rien copié) : fork à jour ffxiv-tc-port/Mappy, Controllers/IntegrationsController.cs — pour MapType.QuestLog, Mappy centre sur AgentMap.TempMapMarkers[0].MapMarker, dont X/Y valent la position monde × 16 (mêmes formules de centrage que pour une position monde).
- Correction : au moment où la carte du jeu apparaît (QuestLog ou GatheringLog), lecture de TempMapMarkerCount et TempMapMarkers[0].MapMarker.X/Y ÷ 16 → GameMapRequest.Target (monde X/Z) ; la carte Tensho se centre d'abord sur cette cible (zoom ≥ 100 %), puis les replis 0.120. Trace « Game map replaced: … target … » dans /xllog.
- Test : Target rend la demande pointée. Verify.ps1 (agent) : 524/524, 0 avertissement, 0 erreur. Non validé en jeu.

## 0.120.0
- Retour utilisateur 0.119 : quête du journal sur une autre zone = bonne carte ; mais sur la zone courante, le clic sur la quête (ou le bouton carte du journal) ne zoome pas sur la quête.
- Causes probables : demande du journal sans QuestId → traitée comme « M » (suivi du joueur) ; marqueur non trouvé par ObjectiveId → carte entière.
- Correction : GameMapRequest.QuestLog (MapType.QuestLog) rend la demande « pointée » ; recherche du marqueur par ObjectiveId puis par nom de quête dans l'infobulle du marqueur, réessayée 1,5 s ; repli donneur/éthérite/carte entière. Traces Information dans /xllog : « Game map replaced: … » et « Map focus found / not found … markers: objectiveId:icon:tooltip » pour prouver le lien en jeu.
- Test : GameMapRequest.Pointed. Verify.ps1 (agent) : 524/524, 0 avertissement, 0 erreur. Non validé en jeu.

## 0.119.0
- Reprise prévue : réglages globaux des icônes + surcharges par carte, priorité réglable.
- MapViewSettings : MapIconSizes (carte → icône → taille), IconPriorities (global) et MapIconPriorities (carte → icône → priorité −9..9). SizeOf/PriorityOf(icon, map) : surcharge de la carte, sinon global, sinon défaut. Reset(map) = revenir au global ; Reset(0) = remettre le global à zéro. Configurations 0.111+ conservées.
- Carte : icônes fixes, quêtes et ALEA dessinées par priorité croissante (la plus prioritaire au-dessus et reçoit le survol) ; tailles par carte appliquées (drapeau compris).
- Réglages « Icônes de cette carte » : choix Toutes les cartes / Cette carte, état (propres / suit le global), bouton Revenir aux réglages globaux, colonne priorité, lignes triées par priorité ; ALEA de la zone listées aussi.

## 0.118.0
- Git : .gitignore complété (IDE, TestResults, checkpoints/*.zip) ; 96 archives retirées du suivi (gardées sur disque). Commits faits par l'application de bureau. DEC-009.
- ACT/IINACT/Combat retirés du projet (docs : PROMPT-MAITRE §5, RULES, ROADMAP, BACKLOG, PROJECT, CLAUDE, BASE-AUDIT-002 ; aucun code concerné).
- Quête cliquée dans le journal du jeu : CurrentOpenMapInfo.QuestId/AetheryteId/FateId lus ; carte Tensho ouverte sur la carte demandée et centrée (zoom ≥ 100 %) sur le marqueur de la quête (MapMarkerData.ObjectiveId), sinon le donneur (index en cache), sinon l'éthérite donnée par le jeu, sinon carte entière. Plus de bascule « M » pour une demande pointée. Correspondance ObjectiveId ↔ QuestId non …
- Agrandir/Restaurer : l'ajustement attend 2 images (taille réelle de la fenêtre).

## 0.117.0
- Demande utilisateur : vue double au lieu des sous-onglets (capture Mer de Rubis, onglets tronqués).
- Correction : DrawSplitView — catégories (quêtes) / types (récolte) à gauche avec leur nombre, lignes à droite ; panneau élargi à 640 × 360 ; EndChild toujours appelé.

## 0.116.0
- Retour utilisateur 0.115 : la carte ne se centre pas sur le drapeau créé.
- Correction : PlaceFlagFromMapView coupe le suivi du joueur et demande le centrage sur le pixel du drapeau (MapProjection.PixelFromCoordinate), appliqué à l'image suivante quand la taille du canevas est connue. Zoom inchangé.

## 0.115.0
- Retour utilisateur 0.114 : placer un drapeau depuis la carte Tensho ferme la carte.
- Cause : le drapeau passe par OpenMapWithMapLink, qui ouvre la carte du jeu sur la carte du drapeau ; le remplaçant la ferme et demande la carte Tensho ; sur la zone courante, la demande vaut « M » (suivi du joueur) et bascule la fenêtre fermée.
- Correction : PlaceFlagFromMapView (menu « Placer le drapeau » et listes locales) ignore la prochaine demande du jeu pendant 2 s ; la carte Tensho reste ouverte sur la même carte. L'outil Carte/Monde garde l'ancien comportement (ouvre la carte Tensho sur le point).

## 0.114.0
- Demande utilisateur : dans le menu clic droit de la carte, listes locales en onglets — quêtes par catégorie, ALEA, zones de récolte, etc. — en respectant le cache.
- Preuve des liens (sonde en lecture seule, 0.114.0-local-probe.log) : Quest.IssuerLocation → Level avec carte pour 5 292/5 292 quêtes ; ExportedGatheringPoint (id = GatheringPointBase) a une position pour 1 077/1 426 bases ; Fate.Location ne pointe vers aucune Level avec carte (0/1 712) → pas de position fixe des ALEA, seules les ALEA en cours (IFateTable) sont utilisées.
- Cache : nouvel index MapLocalIndex (positions donneurs de quête + points de récolte), copié en lots de 2 ms sur le thread framework, indexé sur un worker, gardé dans DataCache famille « catalog-map-positions » (clé = versions des dépôts + schéma Lumina + règles de copie + forme, pas la version du plugin), comme CATALOG-CACHE-012. Quêtes (cache « quests » par personnage) et récolte (« catalog-gat…
- Menu clic droit : onglets Quêtes (sous-onglets par catégorie du journal, terminées masquées par défaut), ALEA (zone actuelle, niveau, progression, temps), Récolte (sous-onglets par type, objets au survol, pêche sans position), Éthérites (téléportation avec coût). Clic = drapeau du jeu à la position.

## 0.113.0
- Demande utilisateur : les icônes doivent suivre le zoom.
- Correction : bornes 10–64 px (et 8–96 px par icône, 12 px pour coffres/récoltables) retirées ; taille = 32 px × zoom × taille globale × taille de l'icône, plancher de 4 px seulement. Flèche du joueur inchangée (taille fixe).

## 0.112.0
- Retour utilisateur 0.111 : un clic sur une quête affiche le monde au lieu de la carte concernée.
- Cause probable (non prouvée en jeu) : à la première image où la carte du jeu apparaît, AgentMap.SelectedMapId vaut encore la carte du monde ; seul ce champ était lu.
- Correction : lecture aussi de AgentMap.CurrentOpenMapInfo (MapId, TerritoryId, Type) ; décision pure GameMapRequest.Resolve : sélection de zone > carte demandée > première carte du territoire demandé > sélection. Trace Debug « Game map replaced: type, map, territory, selected » dans /xllog pour diagnostic.
- Test ajouté : Game map request (monde sélectionné + zone demandée, territoire seul, M sur la zone courante, sélection de zone, rien d'autre).

## 0.111.0
- Demande utilisateur : vrai prix en gils dans la fenêtre de téléportation ; taille réglable ou masquage pour chaque icône.
- Coût : copié sur le thread framework depuis Telepo.UpdateAetheryteList() (TeleportInfo.AetheryteId → GilCost, entrées de logement exclues), relu à chaque clic sur une éthérite. Éthérite absente de la liste = « coût inconnu (non harmonisée ?) », jamais 0. Icône Gil du jeu après le montant.
- Icônes : MapViewSettings.IconSizes (id d'icône → taille, 0 = masquée, absente = 100 %), en plus de la taille globale. Section « Icônes de cette carte » : une ligne par icône présente (fixes, quêtes/événements, drapeau) avec case, aperçu, curseur 50–200 %, nom MapSymbol du jeu ; bouton « Tout remettre à 100 % ».
- Test ajouté : Per-icon sizes (défaut, masquage, bornes, JSON).

## 0.110.0
- Retour en jeu 0.109 : AltGr + M n'ouvre toujours rien. À la demande de l'utilisateur, la fonction est retirée : touche, réglage « AltGr + M ouvre quand même la carte du jeu », textes FR/EN, dépendance IKeyState.
- Conservé : bouton « Ouvrir la carte du jeu » (même mécanisme AgentMap.Show, non validé en jeu) et réglage « Remplacer la carte du jeu ».
- Barre de zoom réduite (16 × 80 px, centrée) ; pourcentage du zoom affiché sous le bouton −.
- Menus Région / Zone / Étage : largeur calculée sur l'entrée la plus longue + flèche (+ place de l'aile pour les zones).

## 0.109.0
- Retour en jeu 0.108 : M ouvre bien la carte Tensho (VALIDATED_IN_GAME) ; AltGr + M n'ouvre pas la carte officielle.
- Analyse : AltGr arrive au jeu comme Ctrl + Alt ; « Ctrl + Alt + M » n'est pas un raccourci du jeu, qui n'ouvre donc rien à garder.
- Correction : le plugin détecte AltGr + M (front montant) et ouvre lui-même la carte du jeu (AgentMap.Show) avec un passe-droit de 2 s pour ne pas la remplacer ; bouton « Ouvrir la carte du jeu » dans la colonne d'outils de la carte Tensho (même mécanisme).

## 0.108.0
- Retour en jeu 0.107 : M ouvrait toujours la carte officielle ; Maj + M est déjà utilisé dans le jeu.
- Correction : NativeMapReplacer surveille la fenêtre AreaMap à chaque tick framework (IGameGui) au lieu de l'écouteur IAddonLifecycle PostOpen. À son apparition, il la ferme (AtkUnitBase.Close(true)) et ouvre la carte Tensho sur AgentMap.SelectedMapId (ou la zone courante). Décision prise une fois par ouverture ; touche de maintien de la carte du jeu : AltGr (RMENU, ou Ctrl + Alt), option KeepGam…

## 0.107.0
- Demande : coffres en donjon, zones de mandats et de quêtes, récoltables avec info-bulles (désactivables, désactivés par défaut), téléportation sur éthérite, plus d'options par section, remplacer la carte du jeu.
- Sources vérifiées : Telepo.Teleport(aetheryteId, subIndex) ; ObjectKind.Treasure / GatheringPoint ; IAddonLifecycle PostOpen « AreaMap », AgentInterface.Hide, AgentMap.SelectedMapId ; MapMarker DataType 3 = éthérite (DataKey = Aetheryte), type 4 = éthérites secondaires (0.107.0-aetheryte-probe.log).
- Ajouts : coffres et points de récolte ciblables (table des objets, 0,5 s, 200 au plus ; niveau via GatheringPoint → GatheringPointBase) avec info-bulle et étiquette « nom · niv. » ; clic sur une éthérite → confirmation → Telepo.Teleport sur le thread framework ; remplacement de la carte du jeu (M ouvre ou ferme la carte Tensho sur la même carte ; Maj + M garde la carte du jeu ; désactivable) ; r…
- Zones de quêtes et de mandats : marqueurs d'objectifs du jeu (EventMarkers, cercle quand un rayon est donné), à confirmer en jeu pour les mandats.

## 0.106.0
- Retour en jeu 0.105 : « Réduire » donnait une fenêtre minuscule ; icônes de taille fixe ; pas d'icônes de quête ni de réglages façon Mappy.
- Corrections : taille et position de la fenêtre lues hors de la colonne d'outils (dans un enfant, GetWindowSize renvoyait la colonne) + taille minimale 320×280 ; icônes à 32 px × zoom × taille choisie (10 à 64 px) et noms de lieux qui suivent le zoom.
- Quêtes : AgentMap.EventMarkers (MapMarkerData : icône, position monde, rayon, carte, info-bulle, vérifiés par réflexion) copiés toutes les 0,5 s sur le thread framework (256 au plus) ; icône et zone de l'objectif sur la carte.
- Réglages (engrenage, enregistrés aussitôt, Configuration.MapView) : icônes du jeu, noms de lieux, quêtes, zones des objectifs, drapeau, ma position ; taille des icônes 50–200 % ; opacité 20–100 % ; verrouiller ; masquer en combat ; masquer en instance (ICondition InCombat / BoundByDuty).

## 0.105.0
- Retour en jeu 0.104 : menus Région/Zone et ailes OK, mais la liste des zones n'avait que 3 entrées sur 8 (donjons absents).
- Sonde 0.105.0-zones-probe.log (Ilsabard) : le menu du jeu = cartes de même PriorityCategoryUI que la carte de région (17), triées par PriorityUI, une entrée par priorité ; les étages (MapIndex) partagent la priorité. Le Clyteum (priorité 10) figure dans les données mais pas dans le menu de l'utilisateur : probablement masqué tant qu'il n'est pas découvert (non géré, affiché ici).
- Changements : MapDefinition.MenuCategory/MenuPriority ; MapMenus.Zones (testé sur les lignes de la sonde) ; menu Zone avec entrée « -- » (carte de région) ; menu Niveau pour les cartes à plusieurs étages.

## 0.104.0
- Demande (captures des menus de la carte du jeu) : menus déroulants Région / Zone, interface réorganisée, simple symbole de vol à côté du nom.
- Disposition : menus Région (ordre des cartes region/NN, entrée Monde) et Zone (zones liées par les marqueurs de la carte de région, dans leur ordre) en haut ; colonne d'outils à gauche (remonter, suivre, ajuster, agrandir/réduire, zoom + / curseur vertical / −, info-bulles) ; carte à droite. Aile FontAwesome après le nom de zone et dans le menu des zones : colorée si le vol est débloqué, grisée …

## 0.103.0
- Demande : retirer le drapeau depuis notre carte ; outils de la carte officielle (zoom, retour région, chemin complet, vol possible ou non, agrandir).
- Sondes (0.103.0-maps-probe.log, réflexion FFXIVClientStructs) : cartes de région « region/xx » et du monde « world/00 » sans territoire ; MapMarker DataType 1 → DataKey = carte cible ; AgentMap.FlagMarkerCount, FlagMapMarkers ; PlayerState.IsAetherCurrentUnlocked / IsAetherCurrentZoneComplete ; AetherCurrentCompFlgSet par territoire ; TerritoryType.Mount.
- Ajouts : chemin cliquable Monde › Région › Zone · sous-zone ; Remonter ; curseur de zoom 15–400 % ; Ajuster (carte entière centrée) ; Agrandir (90 % de l'écran) / Réduire ; clic sur une icône liée d'une carte de région ou du monde pour y aller ; « Retirer le drapeau » dans le clic droit (FlagMarkerCount remis à 0, écrit seulement sur le thread framework) ; état du vol de la zone affichée (vol po…
- À confirmer en jeu : effet de FlagMarkerCount = 0 sur la carte et la mini-carte du jeu ; zones sans AetherCurrentCompFlgSet (vol non géré par les courants).

## 0.102.0
- Retour en jeu 0.101 (captures Radz-at-Han) : image, icônes fixes et positions conformes à la carte du jeu, thème bleu appliqué ; noms de lieux absents, drapeau non affiché, bouton « − » rendu « = ».
- Corrections : marqueurs MapMarker sans icône dessinés comme noms de lieux (grand texte, contour) ; drapeau lu dans AgentMap (FlagMarkerCount, FlagMapMarkers[0] : carte, X/Z monde) sur le thread framework et dessiné (icône 60561 supposée, triangle rouge sinon) ; bouton de zoom arrière en ASCII.

## 0.101.0
- MAP-VIEW-016, tranche 1 de docs/goals/MAP-016-PLAN.md : fenêtre « Carte Tensho » ; image de carte du jeu (ui/map/…_m.tex via ITextureProvider), zoom molette autour du curseur, déplacement à la souris, coordonnées sous le curseur, icônes fixes MapMarker (plage Map.MapMarkerRange, info-bulle du sous-titre), flèche du joueur (position et orientation copiées sur le thread framework par MapLiveReader), sui…
- Projection testée (MapProjectionTests : chemins, cohérence avec la formule des PNJ, aller-retour, zoom). À confirmer en jeu : position des icônes MapMarker (X/Y supposés en pixels de texture), sens de la flèche (rotation 0 = +Z).

## 0.100.0
- Demande : le plugin doit prendre le thème d'interface choisi dans FF14 (avant le chantier carte).
- Ajouts : UI/ThemePalettes.cs (palettes Sombre, Clair, Classic FF, Bleu/Blanc/Vert translucides ; couleurs approximées, pas des ressources du jeu ; tests de contraste), UI/GameThemeReader.Dalamud.cs (IGameConfig SystemConfigOption.ColorThemeType au démarrage et à chaque SystemChanged), FfxivTheme lit la palette active, MainWindow.Accent/Success/Muted suivent le thème, réglage « Thème de l'interfa…
- Correspondance des valeurs du jeu : 0 Sombre, 1 Clair, 2 Classic FF, 3 Bleu translucide ; 4/5 supposés Blanc/Vert translucides, à confirmer en jeu (valeur affichée dans les réglages).

## 0.99.0
- Retour en jeu 0.98 : position de la Prêtresse à la robe noire OK ; consigneurs 1/3 ; Officier de gestion du Maelstrom (Jetée, « Échange de cristaux-insignes ») absent.
- Officier : PNJ 1043099, dialogue 721735 « RegSeaPvpCommendationCrystalShop », sans argument ; boutique 1770684 (42 objets, nom resté en japonais), seule payée en cristaux-insignes (40479), seul PNJ utilisant ce dialogue (0.99.0-crystal-probe.log). Ajout à ObservedTalkShops.
- Positions : les consigneurs sont dans planner.lgb (Limsa, Gridania) ou planevent.lgb (Ul'dah). planner.lgb est maintenant lu aussi. La sonde a montré que Lumina manque de mémoire sur certains fichiers de décor (fichier vide de 72 octets ; fichier de 312 octets avec LGP1 à l'octet 32) : seuls les fichiers ayant LGB1, la bonne taille déclarée et LGP1 à l'octet 12 sont analysés (NpcShopLinks.ValidL…
- CopyRules 10 : cache PNJ reconstruit une fois. Verify.ps1 (agent) : voir 0.99.0-verify.log. Sources : TenshoAssistant-0.99.0-source.zip. Non validé en jeu.

## 0.98.0
- Retour en jeu 0.97 : la Prêtresse à la robe noire apparaît (2 800 marques de loup, icône), mais « Position non renseignée », comme beaucoup de PNJ (Consigneur, Magasinier personnel…).
- Cause : seules les lignes Level (type 8) donnaient les positions ; la plupart des PNJ sont placés par les décors des zones. Sonde 0.98.0-lgb-probe.log : 1 134 fichiers planevent.lgb, 166 434 placements EventNPC ; Prêtresse (1018655) = territoire 250, carte 51 « Jetée de l'Antre des loups », X 5,0 Y 5,3.
- Ajout : pour les vendeurs sans position, lecture de planevent.lgb (un territoire par étape, 2 ms par tick), positions ajoutées sans doublon (même carte et même point) et 8 positions au plus. Fichier absent ou illisible = pas de position supplémentaire. CopyRules 9.
- Les PNJ convoqués (magasinier personnel, consigneur) n'ont pas de position fixe : ils resteront sans position.

## 0.97.0
- Retour en jeu 0.96 : menu clic droit Tensho dans les boutiques/échanges OK.
- Marques de loup : la Prêtresse à la robe noire (PNJ 1018655, Quai des Loups) a pour seule donnée le dialogue 721280 « RegSeaPvpGaro », dont le script compilé ouvre les échanges (menu en jeu : mêlée 1, mêlée 2, distance). Sonde 0.97.0-wolf-npc-probe.log : les seules boutiques à marques de loup (type 4) qu'aucune feuille ne référence sont 1769743 (Loup doré), 1770537 (avant-garde Makai) et 1769744…
- Ajout : NpcShopLinks.ObservedTalkShops (721280 → ces trois boutiques), table d'observations en jeu documentée, fusionnée aux liens des feuilles. CopyRules 8.

## 0.96.0
- Retour en jeu 0.95 : mémoquartz OK (validé par l'utilisateur).
- Marques de loup : la sonde (0.96.0-wolf-probe.log) montre la boutique 1769743 (Visage du Loup doré, 2 000 × t4:25, CostType 0) ; aucune feuille ne la relie à un PNJ (direct, TopicSelect, PreHandler, CustomTalk, CustomTalkNestHandlers, FateShop, InclusionShop) : le jeu l'ouvre par son code. Limite connue : PNJ non affiché. La monnaie t4:25 est traduite en Marque de loup (capture en jeu de l'échan…
- Menu clic droit : en plus de l'inventaire, les menus « par défaut » (boutiques, échanges, listes d'objets) reçoivent l'entrée Tensho pour l'objet survolé à l'ouverture (IGameGui.HoveredItem). Les menus visant un personnage (ContentId, objet ou nom ciblé) sont exclus. Mêmes gardes de session (propriétaire, révision, âge).
- CopyRules 7 : cache PNJ reconstruit une fois. Verify.ps1 (agent) : 509/509, 0 avertissement/erreur (0.96.0-verify.log). Sources : TenshoAssistant-0.96.0-source.zip. Non validé en jeu.

## 0.95.0
- Retour en jeu 0.94 : assignats orange OK (300/900, 18–19 PNJ, icônes) ; mémoquartz non résolus (Casque d'abatteur du roman royal, Zircon, 495 × code 2).
- Preuve : sonde 0.95.0-shop-currency-probe.log. SpecialShop CostType sépare les familles quel que soit UseCurrencyType : CostType 2 = mémoquartz (n° 1 boutiques poétiques/Gerolt/Forges, n° 2 boutiques du mémoquartz actuel, n° 3 mnémoniques), CostType 3 = assignats (n° 2/4 mauves artisan/récolteur, n° 6/7 orange). La boutique du casque est 1770911 « Mémoquartz allagois mathématiques (guerre - NvOb…
- Traduction : (CostType 2, n°) → 28/48/49 ; (CostType 3, n°) → 33913/33914/41784/41785 ; codes directs t16:25 / t4:27 / t4:29 → marques de loup, insignes alliés, MGP. t4:25 (aucune boutique nommée) reste non résolu.
- CopyRules 6 : cache PNJ reconstruit une fois. Verify.ps1 (agent) : voir 0.95.0-verify.log. Sources : TenshoAssistant-0.95.0-source.zip. Non validé en jeu.

## 0.94.0
- Monnaies codées des boutiques spéciales : table (type, n°) limitée aux cas prouvés par deux sources (lecture en jeu de l'utilisateur et familles de boutiques de la sonde, 0.94.0-shop-currency-probe.log) : t16 n° 2/4 = assignats mauves 33913/33914 ; n° 6/7 = assignats orange 41784/41785 ; n° 25 = marques de loup ; t2 n° 2/3 et t4 n° 3 = mémoquartz mathématique 48 / mnémonique 49 ; t4 n° 27 = insi…
- Les mémoquartz suivent le mémoquartz *actuel* : la table est à revoir à chaque patch.
- NpcVendorCache.CopyRules = 5 : le cache PNJ est reconstruit une fois.

## 0.93.0
- Assignats mauves : SpecialShop type 16 n° 2 = Assignat mauve d'artisan (33913), n° 4 = Assignat mauve de récolteur (33914), d'après l'observation en jeu de l'utilisateur (comptable des assignats de Radz-at-Han, icône violette, boutique « Assignats mauves… ») et la sonde 0.92. Les autres codes du type 16 (1, 6, 7, 25) restent non résolus.
- NpcVendorCache.CopyRules = 4 : le cache PNJ est reconstruit une fois.

## 0.92.0
- Demande : assignats d'artisanat et de récolte absents de l'outil PNJ vendeurs. Indice utilisateur : « Comptable des assignats », Radz-at-Han (11.5, 9.4), deux monnaies (artisanat et récolte).
- Preuve : `tools/ShopCurrencyProbe`, une sonde en lecture seule sur le sqpack installé (journal 0.92.0-shop-currency-probe.log).
- Chaîne réelle : PNJ → PreHandler (3539xxx) → InclusionShop (3801xxx, « Échange d'assignats ») → InclusionShopCategory → InclusionShopSeries → SpecialShop (1770xxx).
- Coût du Breuvage cordial supérieur : `UseCurrencyType` 16, `ItemCost` 4, `CostType` 3. Le même code est utilisé dans des boutiques de récolte (« Appât et autres ») et mixtes.

## 0.91.0
- Écart reconnu : de 0.76 à 0.90, seules des notes ont été écrites ; ni archive des sources ni journal n'ont été déposés dans checkpoints/ au fil de l'eau. Signalé par l'utilisateur, rattrapé le 2026-10-30 vers 20:50.
- Journaux Verify.ps1 conservés pour chaque version : 0.76.0-verify.log … 0.91.0-verify.log (exécutions de l'agent sur le PC). Tous verts : 477 → 509 tests, 0 avertissement/erreur, langues et paquet/SHA-256 passants.
- DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll (0.91.0.0).
- Validé en jeu (rapports/captures utilisateur) : tri natif et zones 0.77–0.80, cache des objets 0.81, survol 0.85–0.87 (accroche, stock, moins cher, monstres wiki, icône Gil), outil PNJ vendeurs pour gils, espoilles et sceaux (0.91). Non validés : 0.82 recettes, 0.83 récolte, 0.84 critères de tri, clic droit inventaire → PNJ, mise en page PNJ 0.90 ; assignats non pris en charge.

## 0.76.0
- Tranche : QUEST-ITEM-PROGRESS-007 (progression du même relevé jointe aux sources de quête d'un objet).
- Ensuite : csproj et manifeste portés à 0.76.0.0 pour identifier la livraison. Recompilation requise (.\scripts\Verify.ps1) avant l'essai en jeu.
- Code écrit depuis une session cloud sans SDK ; la seule preuve de build/tests est la sortie utilisateur.
- 18:54 : Verify.ps1 relancé par l'agent sur le PC (session desktop) : DLL bin\x64\Release 0.76.0.0, 477/477, 0 avertissement/erreur, langues et paquet verts.

## 0.75.0
- Demande : cache partagé, durées par domaine dans les réglages et actualisation explicite des outils.
- ConfigSerializationProbe : Newtonsoft None/Auto/Objects passants, durées de cache sauvegardées/restaurées. Journal : 0.75.0-config-json-probe.log.
- DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll.
- Cache nouveau non validé en jeu ; aucune validation différée de Macro Chain/projection revendiquée. Les outils restent PARTIAL et l'ancien objectif autonome reste en pause.

## 0.74.0

## 0.72.0

## 0.71.0

## 0.70.0

## 0.69.0

## 0.68.0
- Huit groupes index/lifecycle : variantes et IDs, couverture, copies, bornes/annulation, worker réel, réponses tardives, frontières de fin de vie et erreurs isolées.
- Probe de production : 3 496 Items/8 288 associations, tous ensembles et variantes comparés à la projection directe 5 373 quêtes ; Potion = 25 candidates. Quinze références/quatre quantités inconnues préservées, aucune progression hors personnage.
- Lien Usages → fiche de quête, cache de fiche corrigé ; aucun rendu/clic natif en jeu déclaré.
- DLL C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll, version 0.68.0.0.

## 0.67.0
- QuestRewardsProbe production : 5 373 quêtes, 9 313 lignes/variantes, identités Item/teinte résolues ; 4 quantités et 15 monnaies non résolues gardées explicitement.
- Cinq groupes supplémentaires : alternatives/slots/qualité, inconnus, monnaie par type, propriété/provenance/owner et bornes.
- Sept slots fixes et quatre slots de métier natifs couverts ; les anciennes constantes six/trois ne sont pas transplantées. Tous les modes présents traités sans choisir les conditions du personnage.
- GatheringProbe recompilé ; aucun rendu/choix/récompense en jeu observé.

## 0.66.0
- Cinq groupes de preuve d’acquisition, couverture stable, propriétaire, contradiction atomique et récompenses distinctes.
- CollectionProbe : 885 titres/530 variantes de genre ; 4 003 succès/83 catégories ; 192 récompenses Item et 891 références Title, identités/icônes résolues, jointures étrangères refusées. Orchestrion antérieur toujours passant.
- API et source primaire chargement/getters/signal Unlock inspectées ; aucun état réel de joueur ni callback en jeu déclaré validé.
- DLL canonique C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll, version 0.66.0.0.

## 0.65.0
- CollectionProbe natif : 886 morceaux, 12 catégories, 886 liens Item, tous ItemId ≠ OrchestrionId ; UI/catégories étrangères refusées et icônes présentes.
- Quatre groupes managés : identités/index multiples, copies, propriétaire/confirmation, erreurs de catégorie isolées.
- API IUnlockState et source primaire vérifiées ; aucune acquisition réelle ni rendu en jeu déclarés.
- Lien Objet/Usages ↔ fiche d’orchestrion ↔ stocks/contexte commun ; aucun réseau ou lecteur supplémentaire.

## 0.64.0
- Sept groupes supplémentaires, dont oracle indépendant d’environ 5 000 dates, frontières/minuit, météo précédente, inconnus/alternatives, bornes/copies/annulation.
- Probe réel 8753, fixture UTC 12:00 : critères météo + précédente seulement, plage 13:13:20–13:36:40, horaire absent explicitement.
- Probe réel 4915 : horaire seulement, quatre plages sur l’horizon de douze périodes ; critères météo absents explicitement.
- DLL canonique C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll, version 0.64.0.0.

## 0.63.0
- Quatre groupes pour alias exacts, portée/ambiguïtés, taux/métadonnées et copies.
- WeatherProbe : 214 lignes Weather, 43 noms anglais globalement ambigus ; correspondances par territoire et icônes vérifiées. Tables/prévisions antérieures passantes, couverture 90/100 toujours inconnue pour les tirages manquants.
- GatheringProbe réel 8753 : spot 107/territoire 137, Clear Skies → 1, Rain → 7, Showers → 8, noms/icônes français, trois appâts/intermédiaires natifs.
- La tentative globale a été refusée par preuve de doublons ; choix explicite de la portée du territoire, pas de premier résultat arbitraire.

## 0.62.0
- Cinq groupes : oracle 600 horaires, UTC/minuit, inconnus/vides, météo précédente et copies, erreurs isolées.
- Probes réels : 7685 horaire absent préservé ; 4915 Mahi-Mahi 10–18 ET, fixture UTC 00:29:10–00:52:30 à Unix ; 8753 Clear Skies précédé de Rain/Showers, trois appâts/intermédiaires natifs. Points natifs tous résolus.
- FashionProbe compilé sans avertissement/erreur ; lecteurs/services/transport communs conservés.
- DLL canonique C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll, version 0.62.0.0.

## 0.61.0
- Sept groupes de pêche communautaire dont cache/transport commun, annulation, inconnus, bornes et places typées.
- GatheringProbe --fish-baits=4869 : 19 variantes, 19 FishingSpot natifs, deux appâts/intermédiaires Item résolus ; requête réelle explicite et cache d’objet partagé.
- Source de génération et licence MIT consultées, commit 04cadd2e1e0de86c20aa9303faa082c7971f8d8b. Pas de code/base externe importé.
- Code météo 2 : divergence des sources, 491 fiches natives restent inconnues. Pas de disponibilité actuelle déduite.

## 0.60.0
- FishingNotebookProbe : 2 461 fiches/Item distincts et notes concordantes, 296 tomes, aucun lien Item/spot non résolu ; 491 codes météo non interprétés conservés inconnus. Notes absentes/étrangères rejetées.
- GatheringProbe : contrôles natifs antérieurs passants, 4 199 sources, pas d’option réseau.
- Source primaire GatherBuddy épinglée et métadonnées locales inspectées ; aucune base externe d’appâts importée.
- DLL : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll, version 0.60.0.0.

## 0.59.0
- ConfigSerializationProbe : Newtonsoft None/Auto/Objects, bibliothèque et profils imbriqués passants.
- Bibliothèque sans identifiants de personnage/servant ou observations ; géométrie strictement compatible, chargement en brouillon, pas de requête native implicite.
- DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll ; version 0.59.0.0.
- Seule autre copie sous bin : archive legacy-Release-20261027 0.5.22.0, laissée intacte ; aucun ancien bin/Release actif.

## 0.59.0

## 0.58.0
- ConfigSerializationProbe : Newtonsoft None/Auto/Objects avec filtres et masque combiné passants.
- FashionProbe natif sans réseau : 269 indices, 20 238 Wearable, 7 804 MainHand, 984 OffHand, 31 SoulCrystal, 51 vêtements bloquant Head ; OffHand négatifs exclus.
- ApiProbe : nouveaux champs EquipSlotCategory confirmés ; loader et probe utilisent le même helper de masque.
- Métadonnées absentes ou étrangères inconnues, ne satisfaisant ni Oui ni Non. Aucune requête marché liée aux filtres.

## 0.57.0
- Cinq groupes nouveaux, dont 20 scènes multi-scopes avec oracles de positions protégées, trous physiques et quantités.
- ConfigSerializationProbe : Newtonsoft None/Auto/Objects, zones protégées/inversées et v1 passants.
- Aucun nouveau lecteur ou changement du bridge d’écriture ; contrôle des positions par le modèle partagé.
- DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll.

## 0.56.0
- Huit groupes nouveaux ; 24 scènes multi-scopes vérifient piles/coordonnées/trous et quantité totale indépendante.
- ConfigSerializationProbe : Newtonsoft None/Auto/Objects, critères/zones imbriqués et configuration v1 passants.
- Catégories du jeu dans le catalogue Item partagé ; profils par personnage/rangement et critères multiples ; positions explicites conservées dans le plan natif.
- DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll.

## 0.55.0
- Six groupes nouveaux dont 144 scénarios couvrent sacoches/servant, géométrie, propriétaires, permutations, reçus directs, cache refusé, fraîcheur et lifecycle.
- Lecture manuelle sans import obligatoire ; application revalidée sur rangement visible et chargé. Banque persistante inchangée sans import explicite.
- DLL : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll.
- Capture/apparition des fenêtres/écriture/rendu/persistance natifs NOT VALIDATED_IN_GAME. Tests craft/Macro Chain différés.

## 0.54.0
- 48 scénarios de permutation couvrent remplissage, critères et sens ; mutations de quantité/qualité/position/ordre et lifecycle refusés.
- Source SortaKinda épinglée et licence lues ; SDK local inspecté avant écriture du bridge.
- DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll.
- Application explicite de l’ordre visuel des quatre sacs ; aucun InventoryItem physique modifié par ce code.

## 0.53.0
- ConfigSerializationProbe : Newtonsoft None/Auto/Objects passants, option menu conservée et ancien v1 désactivé.
- SDK local API 15 et sources primaires Dalamud inspectés ; aucun callback/rendu natif observé.
- DLL : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll.
- Menu Inventory optionnel, cinq routes, revalidation au Draw, aucune requête de prix.

## 0.52.0
- ConfigSerializationProbe : modes None/Auto/Objects, options indépendantes et anciennes configurations v1 passants.
- GatheringProbe : contrôles des fichiers natifs passants.
- DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll.
- Survol des usages et des prix mutualisé ; usages seuls sans réseau, scope et annulation des prix testés avec transport simulé.

## 0.51.0

## 0.50.0

## 0.50.0

## 0.49.0

## 0.49.0

## 0.48.0

## 0.47.0

## 0.46.0

## 0.45.0

## 0.44.0

## 0.43.0

## 0.42.0

## 0.41.0

## 0.40.0

## 0.39.0

## 0.38.0

## 0.37.0

## 0.35.0

## 0.34.0

## 0.33.0

## 0.32.0

## 0.31.1

## 0.31.0

## 0.30.1

## 0.30.0

## 0.29.0

## 0.28.0

## 0.27.0

## 0.26.0

## 0.25.0

## 0.24.0

## 0.23.0

## 0.22.0

## 0.21.0

## 0.20.0

## 0.17.1
