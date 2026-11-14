# Journal des versions / Changelog, 0.200 à 0.299

Archive de [CHANGELOG.md](../../CHANGELOG.md) : versions plus anciennes que la dernière release, déplacées par `tools/changelog.py archive`. Même format (français, puis `### English` depuis 0.219.0) ; liens relatifs à la racine réécrits depuis ce dossier.

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
  `https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/evidence/0.246-cache-audit.md`, section 0.281) : toutes les sources suivent leur zone de Réglages › Caches ; seul le cache
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
  `https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/evidence/0.246-cache-audit.md`. Deux défauts corrigés :
  - LuminaSupplemental et GatherBuddy sont demandés à chaque image ; après un échec (hors ligne, GitHub indisponible), le
    téléchargement repartait aussitôt, en boucle. Désormais 5 min d'attente avant un nouvel essai.
  - Quand la copie en cache a expiré et que le réseau échoue, l'ancienne copie est utilisée au lieu de perdre la source.
- Verify.ps1 (agent) : 557/557, 0 avertissement, 0 erreur ; DLL 0.246.0.0. Non validé en jeu.
### English
- Cache audit (https://github.com/Shigu330/Tensho-Assistant/blob/024c426c1186a2e8f51e09087996a9c55208c28a/docs/evidence/0.246-cache-audit.md). LuminaSupplemental and GatherBuddy no longer retry every frame after a
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
  manque ou est vide. Sections anglaises ajoutées aux entrées 0.219.0 et 0.220.0. Règle ajoutée à consignes de l'agent et CONTRIBUTING.md.
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
  « Versions and releases », README (URL du dépôt personnalisé), consignes de l'agent étape 5. Testé : syntaxe des deux scripts, garde-fou de branche
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
