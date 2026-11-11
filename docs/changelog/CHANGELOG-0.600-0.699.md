# Journal des versions / Changelog, 0.600 à 0.699

Archive de [CHANGELOG.md](../../CHANGELOG.md) : versions plus anciennes que la dernière release, déplacées par `tools/changelog.py archive`. Même format (français, puis `### English` depuis 0.219.0) ; liens relatifs à la racine réécrits depuis ce dossier.

## 0.699.0
- Plan global, W-09 : Demande de l'utilisateur : les panneaux à l'écran (atelier, préparation, liste de fabrication) ont une option « Seulement ce qui reste à récupérer, par provenance » : ce qui se fabrique est remplacé par ses ingrédients, jusqu'à ce qui se récolte, s'achète à un PNJ, tombe d'un monstre ou s'obtient autrement, avec une case par provenance. Un objet fabriqué qui s'achète ou se récolte reste tel quel. Les monstres viennent de LuminaSupplemental, donc de la source communautaire. COMPILED et TESTED, non validé en jeu.
- Tests : BaseMaterials699Tests. Verify.ps1 (agent) : 1244/1244, 0 avertissement, 0 erreur ; DLL 0.699.0.0. Non validé en jeu.
### English
- User request: the on-screen panels (workshop, preparation, crafting list) have an option showing only what is left to fetch, by origin: crafted items are replaced by their ingredients, down to what is gathered, bought from an NPC, dropped by a monster or found otherwise, with a box per origin. A crafted item that can be bought or gathered stays as it is. Monsters come from LuminaSupplemental, the community source. COMPILED and TESTED, not validated in game.

## 0.698.0
- Plan global, W-08 : Demande de l'utilisateur : clic droit sur un mandat : « Éthérite la plus proche du mandateur » ou « du mandat ». Le drapeau se place, la carte Tensho s'ouvre dessus et la question de téléportation habituelle s'affiche. Le lieu vient des feuilles du jeu (LevelLevemete, LevelStart). COMPILED et TESTED, non validé en jeu.
- Tests : ReminderTabs687Tests. Verify.ps1 (agent) : 1239/1239, 0 avertissement, 0 erreur ; DLL 0.698.0.0. Non validé en jeu.
### English
- User request: right click on a leve: nearest aetheryte to the client or to the leve. The flag is placed, the Tensho map opens on it and the usual teleport question shows. The place comes from the game's sheets (LevelLevemete, LevelStart). COMPILED and TESTED, not validated in game.

## 0.697.0
- Plan global, Monnaies : Demande de l'utilisateur : l'onglet « Que faire avec ? » a une case « Chercher les prix du marché », qui arrête aussi une recherche en cours, un filtre (Tout, Collection seulement, Collection manquante, Vendable au marché) et une recherche par nom. La collection couvre les montures et les mascottes. COMPILED et TESTED, non validé en jeu.
- Tests : CurrencyValue695Tests. Verify.ps1 (agent) : 1238/1238, 0 avertissement, 0 erreur ; DLL 0.697.0.0. Non validé en jeu.
### English
- User request: the What to do with it? tab has a Look up market prices box, which also stops a running search, a filter (Everything, Collection only, Collection still missing, Sold on the market) and a name search. Collection covers mounts and minions. COMPILED and TESTED, not validated in game.

## 0.696.0
- Plan global, W-07 : Demande de l'utilisateur : « Que puis-je fabriquer ? » compte maintenant les sous-recettes, du minerai à la dernière étape, avec un seul stock partagé. Une case permet de revenir aux ingrédients directs. Le nombre est un minimum : les restes d'une sous-fabrication ne sont pas réutilisés et une seule recette par objet est essayée. La profondeur et le nombre sont bornés. COMPILED et TESTED, non validé en jeu.
- Tests : CraftChained696Tests. Verify.ps1 (agent) : 1237/1237, 0 avertissement, 0 erreur ; DLL 0.696.0.0. Non validé en jeu.
### English
- User request: What can I craft? now counts sub-recipes, from the ore through every step, on one shared stock. A box goes back to direct ingredients. The count is a minimum: leftovers are not reused and one recipe per item is tried. Depth and count are bounded. COMPILED and TESTED, not validated in game.

## 0.695.0
- Plan global, Monnaies : Rapport et demande de l'utilisateur : « Que faire ? » ne montrait aucun prix du marché, car il fallait un second clic, 32 prix à la fois, et un échec ne s'affichait pas. Choisir une monnaie demande maintenant les prix, par lots de 32 jusqu'au bout, sous l'interrupteur Universalis ; un échec s'affiche avec Réessayer ; un objet sans offre l'indique. La page a son propre onglet, Rangements › Que faire avec ?, avec le choix de la monnaie. COMPILED et TESTED, non validé en jeu.
- Tests : CurrencyValue695Tests. Verify.ps1 (agent) : 1233/1233, 0 avertissement, 0 erreur ; DLL 0.695.0.0. Non validé en jeu.
### English
- User report and request: What to do with it? showed no market price: it took a second click, 32 prices at a time, and a failure did not show. Choosing a currency now asks for the prices, in batches of 32 to the end, under the Universalis switch; a failure shows with Try again; an item with no offer says so. The page has its own tab, Storage > What to do with it?, with the currency picker. COMPILED and TESTED, not validated in game.

## 0.694.0
- Plan global, W-05 : Demande de l'utilisateur : dans Compagnie libre › Atelier, « Suivre ce projet » et « Suivre toutes ses parties » ajoutent des projets à une liste de projets suivis. Leurs matériaux s'additionnent, sur la page et dans le panneau à l'écran. Les parties sont reliées par le prototype et l'ordre du jeu, relevés pour les sous-marins. COMPILED et TESTED, non validé en jeu.
- Tests : WorkshopSets694Tests. Verify.ps1 (agent) : 1231/1231, 0 avertissement, 0 erreur ; DLL 0.694.0.0. Non validé en jeu.
### English
- User request: in Free Company > Workshop, Follow this project and Follow all its parts add projects to a followed list. Their materials add up, on the page and in the on-screen panel. Parts are linked by the game's draft and order, read for submarines. COMPILED and TESTED, not validated in game.

## 0.693.0
- Plan global, W-05 : Demande de l'utilisateur : les panneaux de projet d'atelier, de préparation et de liste de fabrication restent affichés, en combat et derrière les fenêtres du jeu, tant qu'on ne les coupe pas. Deux cases, désactivées par défaut, permettent de les cacher en combat ou derrière les fenêtres. Restent la coupure en JcJ et en contenu classé, et le masquage de toute l'interface par le jeu. COMPILED et TESTED, non validé en jeu.
- Tests : ListPanels691Tests. Verify.ps1 (agent) : 1228/1228, 0 avertissement, 0 erreur ; DLL 0.693.0.0. Non validé en jeu.
### English
- User request: the workshop, preparation and crafting list panels stay up, in combat and behind game windows, until turned off. Two boxes, off by default, hide them in combat or behind game windows. The cut in PvP and ranked content, and the game hiding its whole interface, still apply. COMPILED and TESTED, not validated in game.

## 0.692.0
- Plan global, W-05 : Rapports de l'utilisateur : la case manquait dans Liste de fabrication, elle y est, avec son panneau à l'écran qui suit la liste choisie. Le panneau d'atelier ne s'affichait pas sur l'île : le journal, qui dit désormais ce qui écarte un panneau, a donné la cause : sa place par défaut chevauche les jauges de métier (JobHudRRP0, JobHudRRP1), comptées comme des fenêtres devant lui. Elles n'en sont plus, ni le tableau de gestion de l'île. COMPILED et TESTED, non validé en jeu.
- Tests : ListPanels691Tests. Verify.ps1 (agent) : 1228/1228, 0 avertissement, 0 erreur ; DLL 0.692.0.0. Non validé en jeu.
### English
- User reports: the box was missing from the Crafting list tab; it is there, with an on-screen panel following the chosen list. The workshop panel did not show on the island: the log, which now says what hides a panel, gave the cause: its default place overlaps the job gauges (JobHudRRP0, JobHudRRP1), counted as windows in front of it. They no longer count, nor does the island's management board. COMPILED and TESTED, not validated in game.

## 0.691.0
- Plan global, W-05 : Demande de l'utilisateur : deux petits panneaux à l'écran, juste la liste des matériaux avec le stock sur le requis. Compagnie libre › Atelier : « Afficher ce projet à l'écran », qui suit le projet choisi. Artisanat › Préparation : « Afficher cette préparation à l'écran », qui suit la recette et la quantité. Masqués en combat, retenus entre deux sessions, désactivés par défaut. COMPILED et TESTED, non validé en jeu.
- Tests : ListPanels691Tests. Verify.ps1 (agent) : 1227/1227, 0 avertissement, 0 erreur ; DLL 0.691.0.0. Non validé en jeu.
### English
- User request: two small on-screen panels, just the list of materials with stock over needed. Free Company > Workshop: show this project, following the chosen project. Crafting > Preparation: show this preparation, following recipe and target. Hidden in combat, kept between sessions, off by default. COMPILED and TESTED, not validated in game.

## 0.690.0
- Plan global, Dresseur : Rapport de l'utilisateur : la page Dresseur ne disait pas où capturer les bêtes. Nouvelle colonne Capture : la zone ou le contenu, lus dans les feuilles du jeu, dans la langue du jeu, et la recherche les trouve. L'ennemi à capturer et ses coordonnées ne sont pas dans ces colonnes. Sens des colonnes déduit, à comparer au bestiaire du jeu. Île : la comparaison de l'utilisateur confirme la popularité, la disponibilité et l'évolution sur 14 créations, et les cycles de repos. COMPILED et TESTED, non validé en jeu pour la capture.
- Tests : BeastPlaces688Tests. Verify.ps1 (agent) : 1225/1225, 0 avertissement, 0 erreur ; DLL 0.690.0.0. Non validé en jeu.
### English
- User report: the Beastmaster page did not say where to catch beasts. New Caught at column: the zone or duty, from the game's sheets, in its language, and search finds them. The enemy to catch and its coordinates are not in these columns. The columns' meaning is inferred, to compare with the game's bestiary. Island: the user's comparison confirms popularity, supply and demand shift on 14 crafts, and the rest cycles. COMPILED and TESTED, capture not validated in game.

## 0.689.0
- Plan global, Mandats : Rapport de l'utilisateur : l'infobulle d'un mandat ne donnait que son identifiant, en mode développeur. Elle donne maintenant le niveau, le type, le mandateur, la zone, les gils et les allocations, pour tout le monde ; l'identifiant reste en mode développeur. COMPILED et TESTED, non validé en jeu.
- Tests : ReminderTabs687Tests. Verify.ps1 (agent) : 1223/1223, 0 avertissement, 0 erreur ; DLL 0.689.0.0. Non validé en jeu.
### English
- User report: a leve's tooltip gave only its id, in developer mode. It now gives level, type, client, zone, gil and allowances, for everyone; the id stays in developer mode. COMPILED and TESTED, not validated in game.

## 0.688.0
- Version sans changement de code : une erreur de l'agent a publié le numéro alors que le correctif n'avait pas été appliqué, avec le texte de la 0.687. La colonne Capture du Dresseur est en 0.690.
- Tests : 1222/1222.

### English

No code change: an agent error published this number with the fix not applied and the text of 0.687. The Beastmaster column is in 0.690.

## 0.687.0
- Plan global, Rappels : Demande de l'utilisateur : la page Rappels passe en 5 onglets : Minuteurs, Mes tâches, Roulettes, Escadron, État du personnage. Rien n'est retiré. COMPILED et TESTED, non validé en jeu.
- Tests : ReminderTabs687Tests. Verify.ps1 (agent) : 1222/1222, 0 avertissement, 0 erreur ; DLL 0.687.0.0. Non validé en jeu.
### English
- User request: the Reminders page becomes 5 tabs: Timers, My tasks, Roulettes, Squadron, Character status. Nothing is removed. COMPILED and TESTED, not validated in game.

## 0.686.0
- Plan global, Félicité insulaire : Corrections, captures de l'utilisateur : Tensho lisait quatre fabriques alors qu'une seule est prête, et préparait une planification pour les quatre ; seules les fabriques construites comptent maintenant, d'après un octet observé sur un seul joueur. Les mots de la disponibilité et de l'évolution sont ceux du jeu (Introuvable, En surplus, Forte augmentation, Constante…), et « fabrique » remplace « atelier » sur la page. COMPILED et TESTED, non validé en jeu.
- Tests : IslandWorkshops686Tests. Verify.ps1 (agent) : 1221/1221, 0 avertissement, 0 erreur ; DLL 0.686.0.0. Non validé en jeu.
### English
- Fixes from user screenshots: Tensho read four workshops with only one ready, and planned for four; only built ones now count, from a byte observed on one player. Supply and demand shift use the game's French words, and fabrique replaces atelier on the page. COMPILED and TESTED, not validated in game.

## 0.685.0
- Plan global, W-04 : Animaux de la Félicité insulaire, à la demande de l'utilisateur. La page passe en deux onglets, Atelier et Pâturage. Pâturage : vos animaux lus dans le jeu (surnom, humeur, satiété, collecte, curation), et sur clic la liste du wiki : lieu avec drapeau, heure d'Éorzéa, météo, taille, capture, ressources. Le jeu ne donne ni lieux ni conditions : ces données sont lues sur le wiki à la demande, marquées comme communautaires, avec noms, objets et météos dans la langue du jeu. Rien n'est collecté ni nourri. COMPILED et TESTED, non validé en jeu.
- Tests : IslandAnimals685Tests. Verify.ps1 (agent) : 1219/1219, 0 avertissement, 0 erreur ; DLL 0.685.0.0. Non validé en jeu.
### English
- Island Sanctuary animals, at the user's request. The page becomes two tabs, Workshop and Pasture. Pasture: your animals read from the game, and on a click the wiki's list: place with a flag, Eorzea time, weather, size, capture, leavings. The game holds neither places nor conditions: those are read from the wiki on request, marked as community data, with names, items and weathers in the game's language. Nothing is collected or fed. COMPILED and TESTED, not validated in game.

## 0.684.0
- Plan global, Félicité insulaire, carte : Demandes de l'utilisateur : les ressources de la Félicité insulaire se règlent par type, avec les cinq types du jeu (plantes, bois, roches, terre, pêche), dans le menu Affichage seulement. Deux lieux du même type au même endroit ne font plus qu'une icône. Le « ! » sans texte prend le nom de ce qui se trouve là, un panneau de construction par exemple. Le classement des objets par type est déduit des noms : aucune feuille ne le donne. COMPILED et TESTED, non validé en jeu.
- Tests : Island679Tests. Verify.ps1 (agent) : 1213/1213, 0 avertissement, 0 erreur ; DLL 0.684.0.0. Non validé en jeu.
### English
- User requests: the Island Sanctuary's resources are set by kind, the game's five kinds, in the Display menu only. Two places of one kind on one spot make one icon. The marker with no text takes the name of what stands there, such as a construction board. Sorting items into kinds is inferred from their names: no sheet gives it. COMPILED and TESTED, not validated in game.

## 0.683.0
- Plan global, W-02 : La Grande Mascarade, sujet choisi par l'utilisateur. Nouvelle page Progression › La Grande Mascarade : les 32 niveaux, réussis ou non, avec leurs ennemis, le temps standard et le temps idéal, et un filtre « Non réussis seulement ». Aucun conseil de combat : les faiblesses de la feuille ne sont pas reprises. Lecture seule, sans requête au serveur. COMPILED et TESTED, non validé en jeu.
- Tests : MaskedCarnivale683Tests. Verify.ps1 (agent) : 1209/1209, 0 avertissement, 0 erreur ; DLL 0.683.0.0. Non validé en jeu.
### English
- Masked Carnivale, a topic chosen by the user. New page Progression > Masked Carnivale: the 32 stages, cleared or not, with their enemies, standard and ideal times, and an « only those left » filter. No fight advice: the sheet's weaknesses are left out. Read only, no server request. COMPILED and TESTED, not validated in game.

## 0.682.0
- Plan global, Félicité insulaire, carte : Correction, rapport de l'utilisateur : les ressources de la Félicité insulaire ne se désactivaient pas depuis le menu Affichage de la carte, et « Aucun » les laissait. La case y est ajoutée, Tout et Aucun les comptent, et dans les réglages elle passe de la section des quêtes à celle des récoltes. COMPILED et TESTED, non validé en jeu.
- Tests : Island679Tests. Verify.ps1 (agent) : 1203/1203, 0 avertissement, 0 erreur ; DLL 0.682.0.0. Non validé en jeu.
### English
- Fix from a user report: the Island Sanctuary's resources could not be turned off from the map's display menu, and None left them on. The box is added there, All and None count them, and in the settings it moves from the quest section to the gathering one. COMPILED and TESTED, not validated in game.

## 0.681.0
- Plan global, Félicité insulaire, carte : Demande de l'utilisateur : Rodolfermier (Champs), Béberger (Pâturage) et Chamoiselle (Guide) paraissent sur la carte Tensho. Marchandréa est reliée aux 8 boutiques payées en assignats insulaires azur et émeraude : ses articles entrent dans les sources d'objets. Ce lien est déduit, aucune feuille ne le donne. COMPILED et TESTED, non validé en jeu.
- Tests : Island679Tests. Verify.ps1 (agent) : 1202/1202, 0 avertissement, 0 erreur ; DLL 0.681.0.0. Non validé en jeu.
### English
- User request: Rodolfermier, Béberger and Chamoiselle appear on the Tensho map. Marchandréa is linked to the 8 shops priced in Seafarer's and Islander's Cowries, so their goods join the item sources. That link is inferred: no sheet gives it. COMPILED and TESTED, not validated in game.

## 0.680.0
- Plan global, Félicité insulaire, carte : Laracheteuse (Exportations) paraît sur la carte Tensho, à la position lue dans le jeu : aucun fichier du jeu ne la donne. La même lecture montre des repères sur deux versions de la carte à la fois (773 et 775), ce qui explique le « ! » manquant. La capture temporaire est retirée. COMPILED et TESTED, non validé en jeu.
- Tests : Island679Tests. Verify.ps1 (agent) : 1201/1201, 0 avertissement, 0 erreur ; DLL 0.680.0.0. Non validé en jeu.
### English
- Laracheteuse (Exportations) appears on the Tensho map, at the position read in the game: no game file gives it. The same reading shows markers on two versions of the map at once (773 and 775), which explains the missing marker. The temporary capture is removed. COMPILED and TESTED, not validated in game.

## 0.679.0
- Plan global, Félicité insulaire, carte : Corrections, rapports de l'utilisateur. Le marqueur « ! » de l'objectif de développement manquait : l'île a plusieurs versions de sa carte dans le jeu et Tensho ne gardait que les repères de la version affichée. La carte Tensho disparaissait dans tous les modes : MJIPadGuide, invisible, comptait comme une fenêtre devant elle. Marchandréa (Divers) et Laracheteuse (Exportations) sont ajoutées comme PNJ de service, sans vente inventée : Marchandréa est placée d'après la feuille Level ; Laracheteuse n'a de position dans aucun fichier du jeu, une capture en mode développeur relève sa place. COMPILED et TESTED, non validé en jeu.
- Tests : Island679Tests. Verify.ps1 (agent) : 1200/1200, 0 avertissement, 0 erreur ; DLL 0.679.0.0. Non validé en jeu.
### English
- Fixes from user reports. The development objective's marker was missing: the island has several versions of its map in the game and Tensho kept only the markers of the one shown. The Tensho map left in every mode: the invisible MJIPadGuide counted as a window in front of it. Marchandréa and Laracheteuse are added as service NPCs with no made-up sales: the first is placed from the Level sheet; the second is in no game file, and a developer-mode capture records its position. COMPILED and TESTED, not validated in game.

## 0.678.0
- Plan global, Félicité insulaire : Correction, rapport de l'utilisateur : le jeu dit « Félicité insulaire », pas « Île sanctuaire ». Le reste de la page a été comparé à la feuille Addon du jeu : « entrain » devient « enthousiasme », « agenda » devient « carnet de fabrication » ou « planification », « offre » devient « disponibilité », « rang de l'île » devient « rang de félicité ». Ces mots étaient des traductions de l'agent. Textes français seulement. COMPILED et TESTED, non validé en jeu.
- Tests : Languages. Verify.ps1 (agent) : 1195/1195, 0 avertissement, 0 erreur ; DLL 0.678.0.0. Non validé en jeu.
### English
- Fix from a user report: the French game says Félicité insulaire, not Île sanctuaire. The rest of the page was compared with the game's Addon sheet and its French words corrected: enthousiasme, carnet de fabrication, planification, disponibilité, rang de félicité. French texts only. COMPILED and TESTED, not validated in game.

## 0.677.0
- Plan global, Cartes : Demande de l'utilisateur : la page Cartes & repères était une longue suite de sections, elle passe en onglets. Cartes : recherche, liste des cartes, coordonnées. Repères : favoris, liens du chat, annuaire des lieux. Chasse : train de chasse et conditions des rangs S, ouverts d'office. Terrains en vente : PaissaDB. Le bouton d'ouverture de la carte Tensho reste au-dessus. Choisir un favori, utiliser un point de la carte ou le lieu d'un monstre ramène sur l'onglet Cartes, qui contient les coordonnées. COMPILED et TESTED, non validé en jeu.
- Tests : MapTabs677Tests. Verify.ps1 (agent) : 1195/1195, 0 avertissement, 0 erreur ; DLL 0.677.0.0. Non validé en jeu.
### English
- User request: the Maps page was one long run of sections and is now in tabs. Maps: search, map list, coordinates. Markers: bookmarks, chat links, place directory. Hunt: hunt train and S rank conditions, open by default. Plots for sale: PaissaDB. The button that opens the Tensho map stays above them. Choosing a bookmark, a point from the map or a monster's place brings the Maps tab forward, since it holds the coordinates. COMPILED and TESTED, not validated in game.

## 0.676.0
- Plan global, C22 : Correction, rapport de l'utilisateur : dans Compagnie libre › Atelier, la colonne « En stock » restait sur un tiret alors que l'infobulle de l'objet donnait le bon nombre. Cause : c'était la seule page à lire les stocks sans les rafraîchir, elle ne voyait donc que ce qu'une autre page avait déjà lu. Corrigé, et un test exige désormais le rafraîchissement de toute page qui lit les stocks. COMPILED et TESTED, non validé en jeu.
- Tests : WorkshopStock676Tests. Verify.ps1 (agent) : 1193/1193, 0 avertissement, 0 erreur ; DLL 0.676.0.0. Non validé en jeu.
### English
- Fix from a user report: in Free Company > Workshop, the In stock column stayed on a dash while the item's tooltip gave the right count. Cause: it was the only page reading the stocks without refreshing them. Fixed, and a test now requires the refresh on every page that reads stocks. COMPILED and TESTED, not validated in game.

## 0.675.0
- Plan global, W-01 : Commandes spéciales, sujet choisi par l'utilisateur. Nouvelle page Production › Commandes spéciales : pour chaque client, son lieu, son niveau, son rang, ses points et ses livraisons de la semaine, avec le reste de la semaine et le renouvellement. Les clients sans leur quête faite sont marqués non débloqués. Lecture seule, sans requête au serveur. Les objets demandés cette semaine ne sont pas affichés : le jeu ne les donne que dans la fenêtre du client. Le site EorzeaWorld interdit l'usage de ses données dans un plugin : rien n'en vient, c'est noté dans REFERENCES. COMPILED et TESTED, non validé en jeu.
- Tests : CustomDeliveries675Tests. Verify.ps1 (agent) : 1192/1192, 0 avertissement, 0 erreur ; DLL 0.675.0.0. Non validé en jeu.
### English
- Custom deliveries, a topic chosen by the user. New page Production > Custom deliveries: each client's place, level, rank, points and deliveries this week, with what is left this week and the renewal. Clients whose quest is not done are marked not unlocked. Read only, no server request. This week's requested items are not shown: the game gives them only in the client's window. EorzeaWorld forbids using its data in a plugin: nothing comes from it, and REFERENCES says so. COMPILED and TESTED, not validated in game.

## 0.674.0
- Plan global, N-10 : Île sanctuaire (N-10, entrain) : le total d'un agenda compte maintenant la montée de l'entrain, une création efficace à la fois, jusqu'au plafond lu d'après vos repères, et la page affiche son évolution sur le cycle. Les sources divergent sur le moment de la hausse (début ou fin de la création) : la fin est retenue, le total le plus bas. Le choix des créations ne change pas. Crédit ajouté : page Island Workshop du wiki, qui confirme aussi qu'une création efficace consomme ses matériaux une seule fois.
- Tests : Island groove. Verify.ps1 (agent) : 1186/1186, 0 avertissement, 0 erreur ; DLL 0.674.0.0. Non validé en jeu.
### English
- Island sanctuary (N-10, groove): an agenda's total now counts the groove's rise, one efficient craft at a time, up to the cap from your landmarks, and the page shows how it moves over the cycle. Sources differ on when it rises (start or end of the craft): the end is kept, the lower total. The choice of crafts does not change. Credit added: the wiki's Island Workshop page, which also confirms an efficient craft uses its materials once.

## 0.673.0
- Plan global, N-07 : Île sanctuaire (N-07, liste des matériaux) : chaque création affiche ses matériaux, et un agenda proposé donne le total de ses matériaux pour un cycle, à copier. D'après les données du jeu ; une création efficace compte une fois (un guide le dit, à comparer en jeu) ; les stocks de l'île ne sont pas lus. Plan : l'île de Lunule est notée pour plus tard, à la demande de l'utilisateur. Script de version : la ligne de reprise ne garde plus de restes après un numéro de version.
- Tests : Island materials. Verify.ps1 (agent) : 1182/1182, 0 avertissement, 0 erreur ; DLL 0.673.0.0. Non validé en jeu.
### English
- Island sanctuary (N-07, materials list): each craft shows its materials, and a proposed agenda gives its total materials for one cycle, to copy. From the game's data; an efficient craft counts once (a guide says so, to compare in game); the island's stocks are not read. Plan: Occult Crescent is noted for later, as the user asked. Version script: the resume line no longer leaves remains after a version number.

## 0.672.0
- Plan global, N-10 : Île sanctuaire (N-10), deuxième partie : « Proposer un agenda » calcule un cycle pour vos ateliers construits, atelier par atelier, en tenant compte de l'offre déjà utilisée, et se copie en texte. Vous le saisissez vous-même : Tensho ne programme rien. Un bon agenda plutôt que le meilleur : l'entrain ne monte pas pendant le calcul, et les matériaux ne sont pas vérifiés. Règle des créations efficaces réécrite d'après Workshop Optimizer Plugin (MIT). La prévision de la semaine reste à faire.
- Tests : Island agenda. Verify.ps1 (agent) : 1178/1178, 0 avertissement, 0 erreur ; DLL 0.672.0.0. Non validé en jeu.
### English
- Island sanctuary (N-10), second part: « Propose an agenda » computes one cycle for your built workshops, one workshop at a time, taking the supply already used into account, and copies as text. You enter it yourself: Tensho schedules nothing. A good agenda rather than the best: groove does not rise in the computation, and materials are not checked. Efficient-craft rule rewritten from Workshop Optimizer Plugin (MIT). The week's forecast remains to do.

## 0.671.0
- Plan global, N-10 : Île sanctuaire (N-10), première partie : nouvelle page Production › Île sanctuaire. Une fois l'agenda ouvert dans le jeu, un clic copie le rang, les ateliers, l'entrain, la popularité, l'offre et la demande, et la table donne une valeur estimée par création. Tensho ne demande rien au serveur et ne programme rien. Le rang d'atelier, l'arrondi et les deux lectures de l'entrain restent à vérifier en jeu ; la capture en écrit les valeurs.
- Tests : Island. Verify.ps1 (agent) : 1172/1172, 0 avertissement, 0 erreur ; DLL 0.671.0.0. Non validé en jeu.
### English
- Island sanctuary (N-10), first part: new page Production › Island sanctuary. Once the agenda is open in the game, a click copies the rank, workshops, groove, popularity, supply and demand, and the table gives an estimated value per craft. Tensho asks the server for nothing and schedules nothing. The workshop row, the rounding and the two groove readings remain to check in game; the capture writes their values.

## 0.670.0
- Plan global, N-12 : Chasse (N-12) : Cartes › « Conditions des rangs S » donne, pour les 50 marques, la condition d'apparition, la fenêtre et si la tentative est sûre ou aléatoire. Ces conditions ne sont pas dans les données du jeu : savoir communautaire croisé entre plusieurs sources (wiki, guide, S Rank Assistant en lecture seule), réécrit dans les mots de Tensho, crédité. Noms dans la langue du jeu ; les marques de votre zone en premier. Pas de compteurs.
- Tests : S rank conditions. Verify.ps1 (agent) : 1166/1166, 0 avertissement, 0 erreur ; DLL 0.670.0.0. Non validé en jeu.
### English
- Hunt (N-12): Maps › « S rank conditions » gives, for the 50 marks, the spawn condition, the window and whether an attempt is certain or random. These conditions are not in the game's data: community knowledge crossed between several sources (wiki, guide, S Rank Assistant read only), in Tensho's own words, credited. Names in the game's language; the marks of your zone first. No counters.

## 0.669.0
- Plan global, C12, I22 : Artisanat (C12) : dans le simulateur, un curseur par ingrédient pouvant être HQ calcule la qualité initiale selon le niveau d'objet (règle de Craftimizer, MIT), qui peut encore être tapée. À comparer à la qualité du carnet du jeu. Performance (I22) : l'historique du marché filtre et calcule ses statistiques une fois par changement, et le coût de préparation se recalcule au plus une fois par seconde, comme celui de la chaîne ; le gain n'est pas mesuré en jeu.
- Tests : Collectible. Verify.ps1 (agent) : 1163/1163, 0 avertissement, 0 erreur ; DLL 0.669.0.0. Non validé en jeu.
### English
- Crafting (C12): in the simulator, a slider per ingredient that can be HQ computes the starting quality by item level (rule from Craftimizer, MIT), which can still be typed. To compare with the game's crafting log. Performance (I22): the market history filters and computes its statistics once per change, and the preparation cost is rebuilt at most once a second, like the chain's; the gain is not measured in game.

## 0.668.0
- Plan global, C13 : Artisanat (C13) : le simulateur donne la chance HQ d'une qualité finale, d'après la table de Craftimizer (MIT), pour une recette qui peut être HQ. La table reste à comparer à la synthèse du jeu : la capture écrit les valeurs du jeu.
- Tests : Craft. Verify.ps1 (agent) : 1163/1163, 0 avertissement, 0 erreur ; DLL 0.668.0.0. Non validé en jeu.
### English
- Crafting (C13): the simulator gives the HQ chance of a final quality, from Craftimizer's table (MIT), for a recipe that can be HQ. The table remains to compare with the game's synthesis: the capture writes the game's values.

## 0.667.0
- Plan global, N-07 : Carte (N-07, partie carte) : sur l'île sanctuaire, les ressources à récolter s'affichent avec leur zone et leur nom, d'après les données du jeu, avec un réglage. Leur place reste à comparer sur l'île : l'unité des positions n'est pas prouvée. Artisanat (C6) : correction de la 0.666, les seuils de mission sont des pourcentages de la qualité maximale, relus dans les fichiers du jeu.
- Tests : Island resources. Verify.ps1 (agent) : 1163/1163, 0 avertissement, 0 erreur ; DLL 0.667.0.0. Non validé en jeu.
### English
- Map (N-07, the map part): on the Island Sanctuary, resources show with their area and name, from the game's data, with a setting. Their place remains to compare on the island: the unit of the positions is not proven. Crafting (C6): correction to 0.666, mission thresholds are percents of the maximum quality, read in the game's files.

## 0.666.0
- Plan global, C6 : Artisanat (C6) : le simulateur, le carnet et la synthèse en direct acceptent les recettes à niveau ajusté et à qualité imposée, dont l'Exploration cosmique. La page dit ce que demande la recette : niveau 100, qualité requise et seuils de mission. Règles lues dans Craftimizer (MIT) et réécrites. Les seuils sont affichés mais pas encore visés par la recherche, et rien n'est comparé en jeu. Le catalogue de recettes se reconstruit une fois.
- Tests : Craft. Verify.ps1 (agent) : 1162/1162, 0 avertissement, 0 erreur ; DLL 0.666.0.0. Non validé en jeu.
### English
- Crafting (C6): the simulator, the log panel and live synthesis accept adjusted and fixed-quality recipes, Cosmic Exploration included. The page says what the recipe asks: level 100, required quality and mission thresholds. Rules read in Craftimizer (MIT) and rewritten. Thresholds are shown but not yet aimed at by the search, and nothing is compared in game. The recipe catalogue is rebuilt once.

## 0.665.0
- Plan global, P22 : Carte (P22) : les réglages d'icônes nomment les variantes de quête relevées : la variante 1 est une quête proposée, disponible ou non, et l'icône 71155 une étape en cours. Les autres gardent leur numéro, car rien ne dit leur sens. D'après les journaux et les données, à confirmer en jeu.
- Tests : Quest icons. Verify.ps1 (agent) : 1162/1162, 0 avertissement, 0 erreur ; DLL 0.665.0.0. Non validé en jeu.
### English
- Map (P22): the icon settings name the quest variants read: variant 1 is a quest on offer, available or not, and icon 71155 a step under way. The others keep their number, as nothing tells their meaning. From the logs and the data, to confirm in game.

## 0.664.0
- Plan global, C22 : Compagnie libre (C22) : un onglet Atelier liste les projets de l'atelier, avec leurs matériaux pièce par pièce et phase par phase, le total du projet et vos stocks relevés, et un lien vers chaque objet. Ce que l'atelier a déjà reçu n'est pas lu. La quantité requise est lue comme la quantité par livraison multipliée par le nombre de livraisons, d'après le nom des colonnes : à comparer à la fenêtre du jeu.
- Tests : Workshop projects. Verify.ps1 (agent) : 1161/1161, 0 avertissement, 0 erreur ; DLL 0.664.0.0. Non validé en jeu.
### English
- Free Company (C22): a Workshop tab lists the workshop's projects, with their materials part by part and phase by phase, the project's total and your stocks as read, and a link to each item. What the workshop has already received is not read. The need is read as the quantity per delivery times the number of deliveries, from the columns' names: to compare with the game's window.

## 0.663.0
- Plan global, C28 : Pêche (C28) : pour un poisson à proies, l'aide à la pêche et la page Récolte donnent la durée de l'intuition selon GatherBuddy ; l'aide affiche aussi le temps restant quand l'intuition est active. Le nombre est lu comme des secondes d'après son nom et ses valeurs : à confirmer sur une vraie intuition. Les données GatherBuddy sont téléchargées une fois de plus.
- Tests : GatherBuddy. Verify.ps1 (agent) : 1160/1160, 0 avertissement, 0 erreur ; DLL 0.663.0.0. Non validé en jeu.
### English
- Fishing (C28): for a fish with predators, the fishing help and the Gathering page give the intuition's length according to GatherBuddy; the help also shows the time left when intuition is on. The number is read as seconds from its name and values: to confirm on a real intuition. GatherBuddy data are downloaded once more.

## 0.662.0
- Plan global, 0.662 : Plantage du jeu corrigé : Tensho appelait une fonction du jeu (la liste de téléportation) pendant un changement de zone, sans joueur chargé. Ce risque existait depuis la 0.645, quand le survol des éthérites a demandé cette liste à chaque changement de carte. La fonction n'est plus appelée qu'une fois le joueur présent depuis 3 secondes. Les coûts au survol arrivent donc un instant après l'arrivée.
- Tests : Teleport list. Verify.ps1 (agent) : 1160/1160, 0 avertissement, 0 erreur ; DLL 0.662.0.0. Non validé en jeu.
### English
- Game crash fixed: Tensho called a game function (the teleport list) during a change of zone, with no player loaded. The risk existed since 0.645, when aetheryte hovering began asking for this list at each map change. The function is now called only once the player has been there for 3 seconds. Costs on hover therefore arrive a moment after arrival.

## 0.661.0
- Plan global, 0.661 : Carte : fermer la carte avec sa touche pendant une incantation la laisse fermée, et elle ne revient plus juste avant le chargement : le joueur doit rester libre 1,5 seconde avant qu'elle revienne. Très dézoomée, une zone ne garde que ses noms de liens, comme le jeu : les noms de lieux partent sous 60 %, valeur choisie à l'œil. Les cartes de région et du monde gardent les leurs.
- Tests : Map resume. Verify.ps1 (agent) : 1159/1159, 0 avertissement, 0 erreur ; DLL 0.661.0.0. Non validé en jeu.
### English
- Map: closing the map with its key during a cast leaves it closed, and it no longer comes back just before the loading screen: the player must stay free for 1.5 seconds first. Far out, a zone keeps only its link names, like the game: place names go below 60 %, a value chosen by eye. Region and world maps keep theirs.

## 0.660.0
- Plan global, 0.660 : Carte : les noms sont plus petits et plus proches de ceux du jeu : noms de lieux à 0,65 du réglage, noms à côté des icônes à 0,41, mesurés sur tes captures. Les noms à côté des icônes apparaissent dès 75 % de zoom au lieu de 100 %, valeur choisie à l'œil.
- Tests : Map labels. Verify.ps1 (agent) : 1159/1159, 0 avertissement, 0 erreur ; DLL 0.660.0.0. Non validé en jeu.
### English
- Map: names are smaller and closer to the game's: place names at 0.65 of the setting, names beside icons at 0.41, measured on the user's captures. Names beside icons appear from 75 % zoom instead of 100 %, a value chosen by eye.

## 0.659.0
- Plan global, 0.659 : Carte : une carte ouverte revient aussi à l'entrée dans un contenu en instance, une fois le joueur libre. Le JcJ et le contenu classé restent à part : la coupure compétitive annule l'attente.
- Tests : Map resume. Verify.ps1 (agent) : 1159/1159, 0 avertissement, 0 erreur ; DLL 0.659.0.0. Non validé en jeu.
### English
- Map: an open map also comes back on entering a duty, once the player is free. PvP and ranked content stay apart: the competitive cut cancels the wait.

## 0.658.0
- Plan global, 0.658 : Carte : après une téléportation, la carte se rouvre, y compris dans une autre zone. Elle ne revenait que dans la même zone, d'où le « pas tout le temps ». La fermeture pendant un sort ou un changement de zone compte aussi comme un événement. Elle ne revient pas si la nouvelle zone est un contenu en instance, ni après une fermeture par sa touche. Fermer la carte avec sa touche pendant un sort la fera revenir.
- Tests : Map resume. Verify.ps1 (agent) : 1159/1159, 0 avertissement, 0 erreur ; DLL 0.658.0.0. Non validé en jeu.
### English
- Map: after a teleport the map opens again, in another zone too. It only came back in the same zone, hence « not every time ». A close during a cast or a change of zone also counts as an event. It does not come back if the new zone is a duty, nor after being closed with its key. Closing the map with its key during a cast will bring it back.

## 0.657.0
- Plan global, 0.657 : Carte : depuis une autre zone, cliquer un mandat dans le journal affiche sa zone verte sur la carte Tensho. Le jeu la donne dans sa demande de carte, avec son rayon, et elle reste jusqu'à ce que le jeu ne liste plus le mandat. Récolte : l'alerte de PR ne sonne plus quand les PR remontent après un changement de zone, qui en retire (809 à 772 au relevé) ; elle sonne de nouveau dès que des PR sont dépensés. Les 15 secondes de la 0.656 sont retirées.
- Tests : Leve areas. Verify.ps1 (agent) : 1158/1158, 0 avertissement, 0 erreur ; DLL 0.657.0.0. Non validé en jeu.
### English
- Map: from another zone, clicking a leve in the journal shows its green area on the Tensho map. The game gives it in its map request, with its radius, and it stays until the game no longer lists the leve. Gathering: the GP alert no longer rings when GP climbs back after a change of zone, which takes some (809 to 772 in the reading); it rings again once GP is spent. The 15 seconds of 0.656 are gone.

## 0.656.0
- Plan global, 0.656 : Récolte : l'alerte de PR ne sonne plus après une téléportation quand les PR étaient déjà pleins. Elle garde ce qu'elle savait pendant le changement de zone et ne lit rien pendant 15 secondes après. Des PR réellement remontés pendant la téléportation la font encore sonner, une fois. La cause exacte est déduite du code : le journal écrit maintenant les PR autour d'un changement de zone pour le vérifier.
- Tests : GP alert. Verify.ps1 (agent) : 1157/1157, 0 avertissement, 0 erreur ; DLL 0.656.0.0. Non validé en jeu.
### English
- Gathering: the GP alert no longer rings after a teleport when GP was already full. It keeps what it knew across the change of zone and reads nothing for 15 seconds after it. GP that truly rose during the teleport still rings it, once. The exact cause is inferred from the code: the log now writes GP around a change of zone to check it.

## 0.655.0
- Plan global, 0.655 : Carte : le repère noir au bord, la cible du journal sur un mandat, part quand le jeu ne liste plus le mandat. Il reste sinon jusqu'à ce qu'on le retire à la main, comme avant. Le lien passe par le titre de la demande de carte du jeu, écrit maintenant au journal : qu'il soit bien le nom du mandat reste à voir en jeu.
- Tests : Leve target. Verify.ps1 (agent) : 1156/1156, 0 avertissement, 0 erreur ; DLL 0.655.0.0. Non validé en jeu.
### English
- Map: the dark edge badge, the journal's target on a leve, goes when the game no longer lists the leve. Otherwise it stays until removed by hand, as before. The link goes through the title of the game's map request, now written to the log: that it is the leve's name remains to be seen in game.

## 0.654.0
- Plan global, 0.654 : Carte : la zone d'un mandat reste affichée quand vous n'y êtes pas, y compris depuis une autre zone, une fois que le jeu l'a donnée. Tensho la garde, carte par carte, tant que le jeu liste le mandat comme accepté, et l'oublie dès qu'il ne l'est plus, par exemple s'il est abandonné. Une zone jamais vue depuis le dernier chargement ne peut pas s'afficher : rien n'est sauvegardé. La capture écrit aussi l'état des mandats acceptés.
- Tests : Leve areas. Verify.ps1 (agent) : 1155/1155, 0 avertissement, 0 erreur ; DLL 0.654.0.0. Non validé en jeu.
### English
- Map: a leve's area stays shown when you are not on it, even from another zone, once the game has given it. Tensho keeps it, map by map, as long as the game lists the leve as accepted, and forgets it as soon as it is not, for instance when abandoned. An area not seen since the last load cannot be shown: nothing is saved. The capture also writes the state of accepted leves.

## 0.653.0
- Plan global, 0.653 : Carte : cliquer un mandat lancé dans le journal ne retire plus sa zone. Pendant cette demande, le jeu vide toute sa liste de marqueurs, les 12 et pas seulement celui du mandat, puis la remplit de nouveau. Tensho garde ce qu'il a lu juste avant, tant que dure la demande et sur la même carte.
- Tests : Event markers. Verify.ps1 (agent) : 1154/1154, 0 avertissement, 0 erreur ; DLL 0.653.0.0. Non validé en jeu.
### English
- Map: clicking a started leve in the journal no longer removes its area. During this request the game empties its whole marker list, all 12 and not only the leve's, then fills it again. Tensho keeps what it read just before, while the request lasts and on the same map.

## 0.652.0
- Plan global, P17 : Carte (P17) : les mandats forment une catégorie à part. Leurs zones sont en vert comme dans le jeu, avec leur propre réglage et leur couleur, et leurs repères au bord ont leur interrupteur, séparés des autres événements. Un marqueur est reconnu par l'icône relevée en jeu ou par son nom, trouvé dans la feuille des mandats. Le journal écrit aussi le rayon et le texte de chaque type de marqueur. Seul un mandat de mineur a été relevé, et le vert est réglé à l'œil.
- Tests : Leve markers. Verify.ps1 (agent) : 1153/1153, 0 avertissement, 0 erreur ; DLL 0.652.0.0. Non validé en jeu.
### English
- Map (P17): leves are now a category of their own. Their areas are green as in the game, with their own setting and colour, and their edge markers have their own switch, apart from other events. A marker is recognised by the icon read in game or by its name, found in the Leve sheet. The log also writes the radius and text of each marker kind. Only a miner leve was read, and the green is matched by eye.

## 0.651.0
- Plan global, T27 : Rappels (T27) : une ligne « Bienfaits de compagnie » nomme ceux actifs sur votre personnage et, une fois l'onglet Bienfaits de la fenêtre de compagnie ouvert, le temps restant de chacun, avec l'heure de la première fin et une cloche. Le nom vient du statut, sans le rang (« Prix d'ami », pas « Prix d'ami II »). Les minuteurs sont associés aux bienfaits dans l'ordre, ce que le relevé n'a pas pu prouver. La capture de relevés ne suit plus les PR ni ces minuteurs.
- Tests : Company actions. Verify.ps1 (agent) : 1152/1152, 0 avertissement, 0 erreur ; DLL 0.651.0.0. Non validé en jeu.
### English
- Reminders (T27): a « Company actions » row names those active on your character and, once the Actions tab of the Free Company window was opened, the time left for each, with the time of the first end and a bell. The name comes from the status, without the rank. Timers are paired with actions in order, which the reading could not prove. The reading capture no longer follows GP or these timers.

## 0.650.0
- Plan global, C33 : Relevés en jeu (phase 5, C33 et T5) : en mode développeur seulement, une capture temporaire écrit dans le journal ce que les points suivants attendent : PR pendant une récolte, statuts d'un récolteur dont l'intuition, chance HQ à côté de la qualité, lignes de chat d'une prise, minuteurs des actions de compagnie, cristaux et gils des servants, journaux de voyage des dirigeables. Rien n'est changé dans le jeu, et chaque partie a une limite de lignes. Coupée en JcJ et en contenu classé comme le reste.
- Tests : Reading capture. Verify.ps1 (agent) : 1151/1151, 0 avertissement, 0 erreur ; DLL 0.650.0.0. Non validé en jeu.
### English
- In-game readings (phase 5, C33 and T5): in developer mode only, a temporary capture writes to the log what the next points need: GP while gathering, a gatherer's statuses including intuition, the HQ chance beside the quality, the chat lines of a catch, company action timers, retainers' crystals and gil, airship voyage logs. Nothing changes in the game, and each part has a line limit. Cut in PvP and ranked content like everything else.

## 0.649.0
- Plan global, C29 : Récolte (C29) : les poissons au harpon apparaissent dans la recherche de la page Récolte avec leur lieu, comme les autres ressources. Le catalogue est reconstruit une fois.
- Tests : Gathering sources. Verify.ps1 (agent) : 1150/1150, 0 avertissement, 0 erreur ; DLL 0.649.0.0. Non validé en jeu.
### English
- Gathering (C29): spearfishing fish appear in the Gathering page's search with their place, like other resources. The catalogue is rebuilt once.

## 0.648.0
- Plan global, T13 : Contenus et quêtes (T13) : la page Butin des contenus et le panneau de la Recherche de mission disent quelle quête débloque un contenu, avec son état et un lien ; la fiche d'une quête dit ce qu'elle débloque. Seuls environ 130 contenus sur plus de 700 nomment leur quête dans les données du jeu ; pour les autres, rien n'est affiché, et rien n'est deviné.
- Tests : Duty unlocks. Verify.ps1 (agent) : 1149/1149, 0 avertissement, 0 erreur ; DLL 0.648.0.0. Non validé en jeu.
### English
- Duties and quests (T13): the Duty loot page and the Duty Finder panel say which quest unlocks a duty, with its state and a link; a quest's sheet says what it unlocks. Only about 130 duties out of more than 700 name their quest in the game's data; for the others nothing is shown, and nothing is guessed.

## 0.647.0
- Plan global, C27 : Récolte (C27) : le survol d'un point de récolte sur la carte donne sa classe : à horaire, éphémère ou légendaire, déduite du type du point dans les données du jeu. Le tome requis était déjà indiqué depuis la 0.518. Les libellés sont ceux de Tensho, déduits des données.
- Tests : Gather point kinds. Verify.ps1 (agent) : 1148/1148, 0 avertissement, 0 erreur ; DLL 0.647.0.0. Non validé en jeu.
### English
- Gathering (C27): hovering a gathering point on the map gives its kind: timed, ephemeral or legendary, inferred from the point's type in the game's data. The required tome has been shown since 0.518. The labels are Tensho's, inferred from the data.

## 0.646.0
- Plan global, T21 : Équipement (T21) : une section liste tous les jobs dans l'ordre du jeu, avec leur niveau et une barre d'expérience, et ceux pas encore débloqués. Les niveaux suivent la lecture déjà faite pour les ensembles ; l'expérience du niveau vient de la feuille ParamGrow.
- Tests : Job progress. Verify.ps1 (agent) : 1147/1147, 0 avertissement, 0 erreur ; DLL 0.646.0.0. Non validé en jeu.
### English
- Gear (T21): a section lists all jobs in the game's order, with their level and an experience bar, and those not unlocked yet. Levels follow the read already made for gear sets; a level's experience comes from the ParamGrow sheet.

## 0.645.0
- Plan global, P7 : Carte (P7) : une éthérite absente de la liste de téléportation du jeu est grisée, et son survol dit qu'elle n'est pas harmonisée ; une éthérite harmonisée donne son coût au survol. La liste est relue quand la carte affichée ou votre carte change.
- Tests : Map labels. Verify.ps1 (agent) : 1146/1146, 0 avertissement, 0 erreur ; DLL 0.645.0.0. Non validé en jeu.
### English
- Map (P7): an aetheryte missing from the game's teleport list is greyed, and its tooltip says it is not attuned; an attuned aetheryte gives its cost on hover. The list is read again when the shown map or your map changes.

## 0.644.0
- Plan global, P6 : Carte (P6) : vos coordonnées restent affichées en bas à gauche, le curseur donne aussi sa distance en yalms par rapport à vous, et le repère de bord du drapeau montre dessous ses coordonnées et sa distance. Deux réglages, activés par défaut.
- Tests : Map. Verify.ps1 (agent) : 1145/1145, 0 avertissement, 0 erreur ; DLL 0.644.0.0. Non validé en jeu.
### English
- Map (P6): your coordinates stay at the bottom left, the cursor also gives its distance in yalms from you, and the flag's edge marker shows its coordinates and distance underneath. Two settings, on by default.

## 0.643.0
- Plan global, phase 5 : Carte (capture de l'utilisateur) : sur la carte de région ou du monde, la cible de la quête ne se cliquait pas. Un clic descend maintenant d'une étape : du monde à la région, de la région à la carte de la cible.
- Tests : Map labels. Verify.ps1 (agent) : 1145/1145, 0 avertissement, 0 erreur ; DLL 0.643.0.0. Non validé en jeu.
### English
- Map (user capture): on the region or world map, the quest's target could not be clicked. A click now goes down one step: from the world to the region, from the region to the target's map.

## 0.642.0
- Plan global, phase 5 : Carte (captures de l'utilisateur, jeu à côté de Tensho) : la cible d'une quête reste en remontant, dans l'image de sa zone sur la carte de région et sur le lien de la région sur le monde, comme le jeu. Les noms de lieux en noir suivent maintenant la carte en vue éloignée, et ne sont plus trop grands. Différence gardée : au clic, Tensho ouvre directement la zone, là où le jeu passe par le monde.
- Tests : Map labels. Verify.ps1 (agent) : 1145/1145, 0 avertissement, 0 erreur ; DLL 0.642.0.0. Non validé en jeu.
### English
- Map (user captures, game beside Tensho): a quest's target stays when going up, inside its zone's picture on the region map and on the region's link on the world, like the game. Black place names now follow the map when zoomed out and are no longer too large. Difference kept: on a click, Tensho opens the zone directly, where the game goes through the world.

## 0.641.0
- Plan global, phase 5 : Carte (capture de l'utilisateur, jeu à côté de Tensho) : le menu des zones du jeu ajoute des cartes sans catégorie, vu pour la carte affichée et celle de la quête, comme les Locaux de la Société de l'Arcadion et le Ring des Griffes félines. Tensho les ajoute à la fin de sa liste. La règle exacte du jeu reste inconnue : sa liste défile et peut en contenir d'autres.
- Tests : Map. Verify.ps1 (agent) : 1144/1144, 0 avertissement, 0 erreur ; DLL 0.641.0.0. Non validé en jeu.
### English
- Map (user capture, game beside Tensho): the game's zone menu adds maps with no category, seen for the shown map and the quest's map, such as Locaux de la Société de l'Arcadion and Ring des Griffes félines. Tensho adds them at the end of its list. The game's exact rule remains unknown: its list scrolls and may hold others.

## 0.640.0
- Plan global, phase 5 : Carte (captures de l'utilisateur) : les Locaux de la Société de l'Arcadion et le Paddock ne menaient nulle part, car seuls deux types d'icônes étaient pris en compte, et une icône de quête par-dessus cachait aussi le lien. Toute icône nommée comme une carte l'ouvre maintenant, même sous une icône de quête. Les services n'ont pas de nom et ne changent pas.
- Tests : Map labels. Verify.ps1 (agent) : 1144/1144, 0 avertissement, 0 erreur ; DLL 0.640.0.0. Non validé en jeu.
### English
- Map (user captures): Locaux de la Société de l'Arcadion and Le Paddock led nowhere, because only two kinds of icons were covered, and a quest icon on top also hid the link. Any icon named like a map now opens it, even under a quest icon. Services have no name and do not change.

## 0.639.0
- Plan global, phase 5 : Carte (demande de l'utilisateur) : cliquer l'icône d'un donjon, ou celle d'une porte comme la Ville fantôme, ou leur nom, ouvre la carte nommée. Le jeu le fait pour la Ville fantôme ; pour les donjons, c'est un ajout de Tensho, demandé. Leur nom s'affiche donc comme celui d'un lien, aussi en vue éloignée.
- Tests : Map labels. Verify.ps1 (agent) : 1144/1144, 0 avertissement, 0 erreur ; DLL 0.639.0.0. Non validé en jeu.
### English
- Map (user request): clicking a dungeon's icon, or a door's like Phantom Village, or their name, opens the map it names. The game does it for Phantom Village; for dungeons it is a Tensho addition, as requested. Their names therefore show like a link's, also when zoomed out.

## 0.638.0
- Plan global, phase 5 : Carte (rapport de l'utilisateur, journal) : une quête de donjon ouvrait encore la carte du donjon, car une carte de ce territoire était trouvée avant le chemin donné par le jeu. Elle ouvre maintenant la carte de l'entrée, centrée sur le marqueur du jeu, comme les autres quêtes au-delà d'un changement de zone. À l'intérieur du donjon, sa propre carte.
- Tests : Map request. Verify.ps1 (agent) : 1143/1143, 0 avertissement, 0 erreur ; DLL 0.638.0.0. Non validé en jeu.
### English
- Map (user report, log): a dungeon quest still opened the dungeon's map, because a map of that territory was found before the way given by the game. It now opens the entrance's map, centred on the game's marker, like other quests beyond a zone change. Inside the dungeon, its own map.

## 0.637.0
- Plan global, phase 5 : Carte (demande de l'utilisateur) : depuis le journal de quêtes ou une quête active, la carte est ajustée à chaque fois et le zoom ne change plus. Elle se centre sur la quête, ou sur le milieu de la carte tant que rien n'est trouvé, au lieu de garder l'ancienne position puis d'afficher la carte entière. Les autres ouvertures, comme les liens du chat ou la recherche, gardent le réglage de zoom.
- Tests : Map request. Verify.ps1 (agent) : 1142/1142, 0 avertissement, 0 erreur ; DLL 0.637.0.0. Non validé en jeu.
### English
- Map (user request): from the quest journal or an active quest, the map is adjusted every time and the zoom no longer changes. It centres on the quest, or on the middle of the map while nothing is found, instead of keeping the old position and then showing the whole map. Other openings, such as chat links or search, keep the zoom setting.

## 0.636.0
- Plan global, phase 5 : Carte (rapport de l'utilisateur) : recliquer une quête faisait clignoter la carte du jeu, que Tensho rouvrait, et effaçait l'icône, car le jeu oublie alors son marqueur. La carte du jeu reste fermée et seule celle de Tensho reste ouverte sur la quête, avec son icône gardée. La touche de carte la ferme toujours.
- Tests : Map request. Verify.ps1 (agent) : 1141/1141, 0 avertissement, 0 erreur ; DLL 0.636.0.0. Non validé en jeu.
### English
- Map (user report): clicking a quest again made the game's map blink, as Tensho reopened it, and erased the icon, as the game then forgets its marker. The game's map stays closed and only the Tensho map stays open on the quest, with its icon kept. The map key still closes it.

## 0.635.0
- Plan global, phase 5 : Carte (rapport de l'utilisateur, journal) : recliquer une quête déjà affichée ramenait la carte sur le joueur, car le jeu ferme sa carte à ce clic et oublie la demande. La dernière quête demandée est gardée et la carte se rouvre dessus. Les essais 0.629 et 0.633, cochés trop vite d'après un malentendu, sont décochés.
- Tests : Map request. Verify.ps1 (agent) : 1141/1141, 0 avertissement, 0 erreur ; DLL 0.635.0.0. Non validé en jeu.
### English
- Map (user report, log): clicking a quest already shown brought the map back to the player, because the game closes its map on that click and forgets the request. The last quest asked is kept and the map opens again on it. Checks 0.629 and 0.633, ticked too early after a misunderstanding, are unticked.

## 0.634.0
- Plan global, phase 5 : Carte (demande de l'utilisateur) : une quête dans un donjon ouvre, depuis l'extérieur, la carte où se trouve son entrée, centrée sur le marqueur du donjon. Le marqueur vient des données du jeu : 102 donjons sur 122 en ont un, les autres gardent la carte demandée par le jeu. À l'intérieur du donjon, sa propre carte. Non vu : la demande du jeu pour une quête de donjon, le journal n'en contient aucune.
- Tests : Map request. Verify.ps1 (agent) : 1141/1141, 0 avertissement, 0 erreur ; DLL 0.634.0.0. Non validé en jeu.
### English
- Map (user request): a quest in a dungeon opens, from outside, the map holding its entrance, centred on the dungeon's marker. The marker comes from the game's data: 102 dungeons out of 122 have one, the others keep the map the game asks. Inside the dungeon, its own map. Not seen: the game's request for a dungeon quest, none is in the log.

## 0.633.0
- Plan global, phase 5 : Carte (rapport et captures de l'utilisateur, sonde du journal). Une quête au-delà d'un changement de zone ouvre la carte où se trouve ce passage, comme le montre le jeu : Tuliyollal pour la Ville fantôme, centrée sur son marqueur. Déjà dans la zone, la carte de la zone. Le clic sur une même quête suit maintenant l'état du bouton de la souris, car l'indicateur du jeu restait faux dans le journal. Sous « Monde », le menu ne défile plus. La sonde temporaire de la 0.630 est retirée.
- Tests : Map request. Verify.ps1 (agent) : 1140/1140, 0 avertissement, 0 erreur ; DLL 0.633.0.0. Non validé en jeu.
### English
- Map (user report and captures, log probe). A quest beyond a zone change opens the map holding that passage, as the game shows it: Tuliyollal for Phantom Village, centred on its marker. Already in the zone, the zone's map. A click on the same quest now follows the mouse button's state, because the game's flag stayed false in the log. Under « World », the menu no longer scrolls. The temporary probe of 0.630 is removed.

## 0.632.0
- Plan global, phase 5 : Carte (captures de l'utilisateur, jeu à côté de Tensho). Cartes de région : les zones sont à la bonne échelle (la 0.631 les dessinait trop grandes), dans un ton de parchemin, avec les cristaux de leurs zones, cliquables pour la téléportation. Carte du monde : celle du jeu avec Tural et non plus l'ancienne, et sous « Monde » le choix Hydaelyn / Premier reflet, comme les étages d'une zone, au lieu de « -- ». Le ton est réglé à l'œil : ImGui ne sait pas assombrir comme le jeu.
- Tests : Map labels. Verify.ps1 (agent) : 1139/1139, 0 avertissement, 0 erreur ; DLL 0.632.0.0. Non validé en jeu.
### English
- Map (user captures, game beside Tensho). Region maps: zones at the right scale (0.631 drew them too large), in a parchment tone, with the crystals of their zones, clickable to teleport. World map: the game's one with Tural instead of the old one, and under « World » the choice Hydaelyn / The First, like a zone's floors, instead of « -- ». The tone is set by eye: ImGui cannot darken like the game.

## 0.631.0
- Plan global, phase 5 : Carte (rapport et capture de l'utilisateur) : sur une carte de région, l'icône d'un lien est l'image de toute la zone. Tensho la réduisait à la taille d'une icône, d'où des taches blanches. Elle est maintenant dessinée à sa taille, sous les icônes, comme le jeu. Pas encore fait : les cristaux que le jeu ajoute sur ces cartes, et le clic sur toute la forme d'une zone.
- Tests : Map labels. Verify.ps1 (agent) : 1138/1138, 0 avertissement, 0 erreur ; DLL 0.631.0.0. Non validé en jeu.
### English
- Map (user report and capture): on a region map, a link's icon is a picture of the whole zone. Tensho shrank it to icon size, hence white blotches. It is now drawn at its own size, under the icons, like the game. Not done yet: the crystals the game adds on these maps, and clicking anywhere on a zone's shape.

## 0.630.0
- Plan global, phase 5 : Carte (rapport et captures de l'utilisateur) : pour une quête au-delà d'un PNJ de changement de zone, le jeu montre ce PNJ. Sonde temporaire, en lecture seule, dans le journal : à l'ouverture d'une quête, ce que le jeu garde pour la cible (marqueurs temporaires, d'événement et de quête), pour savoir où il range cet endroit. Rien ne change encore à l'écran.
- Tests : Map request. Verify.ps1 (agent) : 1137/1137, 0 avertissement, 0 erreur ; DLL 0.630.0.0. Non validé en jeu.
### English
- Map (user report and captures): for a quest beyond a zone-change NPC, the game shows that NPC. Temporary read-only probe in the log: when a quest opens, what the game keeps for the target (temporary, event and quest markers), to learn where it stores that place. Nothing changes on screen yet.

## 0.629.0
- Plan global, phase 5 : Carte (rapport de l'utilisateur, journal) : cliquer la quête déjà affichée fermait ou rouvrait la carte, sans aller sur la quête, car le jeu ferme alors sa carte et la 0.627 prenait la demande pour la touche de carte. Un clic gauche juste avant, lu dans les entrées du jeu, distingue les deux : la carte reste ouverte et revient sur la quête. La touche de carte l'ouvre toujours sur le joueur. Limite : un clic à la manette ne se voit pas ainsi.
- Tests : Map request. Verify.ps1 (agent) : 1137/1137, 0 avertissement, 0 erreur ; DLL 0.629.0.0. Non validé en jeu.
### English
- Map (user report, log): clicking the quest already shown closed or reopened the map without going to the quest, because the game then closes its map and 0.627 took the request for the map key. A left click just before, read from the game's inputs, tells them apart: the map stays open and returns to the quest. The map key still opens it on the player. Limit: a controller click is not seen this way.

## 0.628.0
- Plan global, phase 5 : Carte (rapport et captures de l'utilisateur, journal) : une quête d'une zone sans région, comme la Ville fantôme, fait demander au jeu la carte du monde avec les coordonnées de la zone ; Tensho les plaçait sur le monde, donc en pleine mer. La carte propre à la zone, celle que la feuille TerritoryType du jeu indique, s'ouvre maintenant avec l'icône dessus, et aucune cible n'est plus posée sur une carte du monde ou de région. Différence voulue : le jeu reste sur le monde, à côté de Yok Tural.
- Tests : Map request. Verify.ps1 (agent) : 1136/1136, 0 avertissement, 0 erreur ; DLL 0.628.0.0. Non validé en jeu.
### English
- Map (user report and captures, log): a quest in a zone without a region, such as Phantom Village, makes the game ask for the world map with the zone's coordinates; Tensho placed them on the world, so in the middle of the sea. The zone's own map, named by the game's TerritoryType sheet, now opens with the icon on it, and no target is placed on a world or region map any more. Deliberate difference: the game stays on the world map, beside Yok Tural.

## 0.627.0
- Plan global, phase 5 : Carte (rapport de l'utilisateur, journal) : après un clic sur une quête, chaque ouverture avec la touche de carte revenait sur cette quête, car le jeu garde sa dernière demande. Une demande identique à la précédente ouvre maintenant la carte sur le joueur. Limite : recliquer la même quête après avoir fermé la carte ne la recentre plus sur elle.
- Tests : Map request. Verify.ps1 (agent) : 1135/1135, 0 avertissement, 0 erreur ; DLL 0.627.0.0. Non validé en jeu.
### English
- Map (user report, log): after clicking a quest, each opening with the map key went back to that quest, because the game keeps its last request. A request identical to the previous one now opens the map on the player. Limit: clicking the same quest again after closing the map no longer centres on it.

## 0.626.0
- Plan global, phase 5 : Carte (rapport de l'utilisateur) : cliquer sur le nom d'un lien de carte, comme « Limsa Lominsa - L'Entrepont », ouvre cette carte, comme son icône ; le nom s'éclaircit au survol.
- Tests : Map labels. Verify.ps1 (agent) : 1134/1134, 0 avertissement, 0 erreur ; DLL 0.626.0.0. Non validé en jeu.
### English
- Map (user report): clicking the name of a map link, such as « Limsa Lominsa - L'Entrepont », opens that map, like its icon; the name lightens on hover.

## 0.625.0
- Plan global, phase 5 : Carte (rapport de l'utilisateur, journal) : parler à un PNJ fait fermer sa carte au jeu, et la carte Tensho se fermait pour de bon. Fermée ainsi, elle se rouvre une fois le joueur libre, dans la même zone. Fermée par la touche de carte, elle reste fermée.
- Tests : Map resume. Verify.ps1 (agent) : 1133/1133, 0 avertissement, 0 erreur ; DLL 0.625.0.0. Non validé en jeu.
### English
- Map (user report, log): talking to an NPC makes the game close its map, and the Tensho map closed for good. Closed that way, it opens again once the player is free, in the same zone. Closed with the map key, it stays closed.

## 0.624.0
- Plan global, phase 5 : Carte : le journal dit maintenant pourquoi elle s'efface ou revient (fenêtre du jeu ou panneau, lesquelles), et quand le jeu ferme sa propre carte, pour comprendre le rapport « la carte disparaît à l'ouverture d'un menu et ne revient pas ». Pas de correctif encore.
- Tests : Map enlarged. Verify.ps1 (agent) : 1130/1130, 0 avertissement, 0 erreur ; DLL 0.624.0.0. Non validé en jeu.
### English
- Map: the log now says why it steps aside or comes back (game window or panel, which ones), and when the game closes its own map, to understand the report « the map vanishes when a menu opens and never comes back ». No fix yet.

## 0.623.0
- Plan global, phase 5 : Carte (rapport de l'utilisateur, captures du jeu) : les noms à côté des icônes se chevauchaient. Comme sur la carte du jeu, de loin seuls les liens de carte gardent leur nom, les autres reviennent de près ; deux noms ne se superposent plus, et les couleurs suivent le jeu (blanc, liens en bleu-vert). Le zoom exact du jeu n'est pas connu : seuil à ajuster en jeu.
- Tests : Map labels. Verify.ps1 (agent) : 1130/1130, 0 avertissement, 0 erreur ; DLL 0.623.0.0. Non validé en jeu.
### English
- Map (user report, with captures of the game): the names beside icons overlapped. As on the game's map, zoomed out only map links keep their name, the others come back close up; two names no longer overlap, and colours follow the game (white, links in blue-green). The game's exact zoom is unknown: threshold to adjust in game.

## 0.622.0
- Plan global, phase 5 : Carte (demande de l'utilisateur) : le sous-lieu ne s'écrit que s'il existe, sans le « -- » de la carte du jeu.
- Tests : Map place. Verify.ps1 (agent) : 1129/1129, 0 avertissement, 0 erreur ; DLL 0.622.0.0. Non validé en jeu.
### English
- Map (user request): the sub-place is written only when there is one, without the game map's « -- ».

## 0.621.0
- Plan global, phase 5 : Carte (comparée au jeu par l'utilisateur) : sous la zone, Tensho écrit maintenant le lieu où se trouve le joueur, puis le sous-lieu ou « -- », comme la carte du jeu, seulement sur la carte où il est. Lu dans TerritoryInfo sur le thread framework, avec garde.
- Tests : Map place. Verify.ps1 (agent) : 1129/1129, 0 avertissement, 0 erreur ; DLL 0.621.0.0. Non validé en jeu.
### English
- Map (compared with the game by the user): under the zone, Tensho now writes where the player stands, then the sub-place or « -- », like the game's map, only on the player's map. Read from TerritoryInfo on the framework thread, guarded.

## 0.620.0
- Plan global, phase 5 : Carte (rapport de l'utilisateur) : le « 0 » restait dans le menu au-dessus de la carte ; il suit maintenant la même règle que l'autre menu. Plan : le JcJ, le classé, les timers et les instances particulières passent en dernier dans les essais en jeu.
- Tests : Map floors. Verify.ps1 (agent) : 1128/1128, 0 avertissement, 0 erreur ; DLL 0.620.0.0. Non validé en jeu.
### English
- Map (user report): the « 0 » remained in the menu above the map; it now follows the same rule as the other menu. Plan: PvP, ranked content, timers and specific instances come last in the in-game checks.

## 0.619.0
- Plan global, phase 5 : Carte (rapport de l'utilisateur) : le menu des étages affichait « 0 » pour une carte principale sans sous-zone, comme le Tillac de Limsa à côté de son aérodrome ; il affiche maintenant le nom de la zone.
- Tests : Map floors. Verify.ps1 (agent) : 1128/1128, 0 avertissement, 0 erreur ; DLL 0.619.0.0. Non validé en jeu.
### English
- Map (user report): the floor menu showed « 0 » for a main map without a subzone, such as Limsa's upper decks beside its Airship Landing; it now shows the zone's name.

## 0.618.0
- Plan global, phase 5 : Carte (précision de l'utilisateur) : agrandie, elle reste visible quelles que soient les fenêtres du jeu ; à sa taille normale, elle s'efface comme avant. Remplace la règle de la 0.617, retirée. Reconnue aussi à sa taille après un rechargement.
- Tests : Map enlarged. Verify.ps1 (agent) : 1127/1127, 0 avertissement, 0 erreur ; DLL 0.618.0.0. Non validé en jeu.
### English
- Map (user's precision): enlarged, it stays visible whatever game windows it touches; at its usual size it steps aside as before. Replaces the 0.617 rule, removed. Also recognised by its size after a reload.

## 0.617.0
- Plan global, phase 5 : Carte (rapport de l'utilisateur) : cliquer sur « Agrandir » la rendait invisible, car elle touchait alors une fenêtre du jeu. Une fenêtre déjà là au moment où la carte s'ouvre, s'agrandit, se déplace ou change de taille ne la cache plus ; seules celles qui s'ouvrent après la font s'effacer. En mode développeur, le journal dit lesquelles.
- Tests : Map aside. Verify.ps1 (agent) : 1128/1128, 0 avertissement, 0 erreur ; DLL 0.617.0.0. Non validé en jeu.
### English
- Map (user report): clicking « Enlarge » made it invisible, because it then touched a game window. A window already there when the map opens, enlarges, moves or is resized no longer hides it; only those opened afterwards make it step aside. In developer mode, the log says which ones.

## 0.616.0
- Plan global, #36 : Jardin : l'entretien et la plante arrachée sont reconnus dans le chat, messages captés en jeu (LogMessage 4017, 3450, 4025, 3452). Avec un seul emplacement noté à cet endroit, le journal se met à jour ; avec plusieurs, rien n'est deviné et une ligne de chat le dit. Reste la récolte, en attente du timer.
- Tests : Garden care. Verify.ps1 (agent) : 1125/1125, 0 avertissement, 0 erreur ; DLL 0.616.0.0. Non validé en jeu.
### English
- Garden: tending and uprooting are recognised in the chat, messages captured in game (LogMessage 4017, 3450, 4025, 3452). With one bed noted at that place the journal updates; with several nothing is guessed and a chat line says so. The harvest remains, waiting for the timer.

## 0.615.0
- Plan global, DEC-038 : Coupure compétitive : un oubli corrigé. Les deux lecteurs du chat (jardin, liens de carte) lisaient encore pendant la coupure ; ils sont maintenant arrêtés, et un test l'impose.
- Tests : Competitive (0.615). Verify.ps1 (agent) : 1121/1121, 0 avertissement, 0 erreur ; DLL 0.615.0.0. Non validé en jeu.
### English
- Competitive cut: a gap closed. The two chat listeners (garden, map links) still read during a cut; they now stop, enforced by a test.

## 0.614.0
- Plan global, DEC-038 : Triple Triade : dans le hall de la Battlehall (579), l'aide de match revient contre un PNJ identifié, comme ailleurs. Contre un joueur, aucun PNJ n'est identifié : pas d'aide. Les tournois (940, 941) restent sans aide.
- Tests : Triad advisor. Verify.ps1 (agent) : 1120/1120, 0 avertissement, 0 erreur ; DLL 0.614.0.0. Non validé en jeu.
### English
- Triple Triad: in the Battlehall's hall (579), match help is back against an identified NPC, as elsewhere. Against a player no NPC is identified: no help. Tournaments (940, 941) stay without help.

## 0.613.0
- Plan global, DEC-038 : Coupure compétitive, Battlehall : le journal montre que le hall public est lui aussi lié à un contenu (BoundByDuty vrai), donc seul IsInPvPInstance reste utilisé. Hall : plus de coupure, constaté. Vrai dans un match de l'arène : à confirmer.
- Tests : Competitive (0.612). Verify.ps1 (agent) : 1120/1120, 0 avertissement, 0 erreur ; DLL 0.613.0.0. Non validé en jeu.
### English
- Competitive cut, Battlehall: the log shows the public hall is bound to a content too (BoundByDuty true), so only IsInPvPInstance stays. Hall: no more cut, observed. True in an arena match: to confirm.

## 0.612.0
- Plan global, DEC-038 : Coupure compétitive : la Battlehall (territoire 579) est aussi un hall public avec des matchs JcE. Tensho n'y est plus coupé à l'arrivée ni pendant ces matchs, seulement dans l'instance de l'arène, d'après IsInPvPInstance ou BoundByDuty. Hypothèse à confirmer en jeu : le journal écrit les valeurs lues.
- Tests : Competitive (0.612). Verify.ps1 (agent) : 1120/1120, 0 avertissement, 0 erreur ; DLL 0.612.0.0. Non validé en jeu.
### English
- Competitive cut: the Battlehall (territory 579) is also a public hall with PvE matches. Tensho is no longer cut on arrival or during those, only in the arena's instance, from IsInPvPInstance or BoundByDuty. A hypothesis to confirm in game: the log writes the values read.

## 0.611.0
- Plan global, DEC-038 : Coupure compétitive : le journal dit le nombre de zones connues au chargement et chaque début ou fin de coupure, avec le territoire, pour vérifier les essais en jeu.
- Tests : Competitive. Verify.ps1 (agent) : 1119/1119, 0 avertissement, 0 erreur ; DLL 0.611.0.0. Non validé en jeu.
### English
- Competitive cut: the log says how many zones are known at load, and each start or end of a cut with its territory, to check the in-game tests.

## 0.610.0
- Plan global, DEC-038 : Tensho se coupe tout seul en JcJ et en contenu classé, sans réglage, et repart tout seul : JcJ, Battlehall et tournois de Triple Triade, Lord of Verminion, Mahjong, Dédale bestial, donjons profonds, quai de Wolves' Den. Rien n'est lu du jeu : tous les abonnements au framework passent par un gardien, qu'un test impose. Affichage, commandes et menu contextuel coupés, barre de serveur masquée, carte du jeu rendue, Discord vidé, une ligne de chat à l'entrée. Écarts : COMPETITIVE-OFF-PLAN.md.
- Tests : Competitive. Verify.ps1 (agent) : 1119/1119, 0 avertissement, 0 erreur ; DLL 0.610.0.0. Non validé en jeu.
### English
- Tensho stops on its own in PvP and ranked content, with no setting, and comes back on its own: PvP, Triple Triad Battlehall and tournaments, Lord of Verminion, Mahjong, Beastmaster labyrinth, deep dungeons, Wolves' Den pier. Nothing is read from the game: every framework subscription goes through a gate, enforced by a test. UI, commands and context menu off, server bar hidden, game map given back, Discord queue dropped, one chat line on entry. Gaps: COMPETITIVE-OFF-PLAN.md.

## 0.609.0
- Plan global, phase 4 : Fiche objet : « Sur vos autres personnages » donne, pour chacun, les quantités NQ et HQ et la date, d'après ce que la Banque a enregistré. Version compacte : pas de recherche globale, et un rangement jamais lu n'est pas compté.
- Tests : Other stocks. Verify.ps1 (agent) : 1115/1115, 0 avertissement, 0 erreur ; DLL 0.609.0.0. Non validé en jeu.
### English
- Item sheet: « On your other characters » gives each one's NQ and HQ quantities and date, from what the Bank saved. Compact version: no global search, and a storage never read is not counted.

## 0.608.0
- Plan global, I4 : Prix du coût de préparation et des listes : une seule requête Universalis pour toute la liste au lieu d'une par objet toutes les 3 secondes. Les objets sans réponse sont demandés seuls comme avant, et une panne du serveur arrête la liste. Preuve : docs/evidence/0.608-universalis-batch.md. La limite de 32 objets par liste reste en place.
- Tests : Market batch. Verify.ps1 (agent) : 1113/1113, 0 avertissement, 0 erreur ; DLL 0.608.0.0. Non validé en jeu.
### English
- Prices of the preparation cost and lists: a single Universalis request for the whole list instead of one per item every 3 seconds. Items without an answer are asked alone as before, and a server failure stops the list. Evidence: docs/evidence/0.608-universalis-batch.md. The limit of 32 items per list stays.

## 0.607.0
- Plan global, phase 4 : Objets : section « Objets annotés » : ce qui a une note ou une étiquette, filtré par étiquette (Important, À vendre) et par texte dans le nom ou la note ; un clic ouvre l'objet.
- Tests : Annotated. Verify.ps1 (agent) : 1110/1110, 0 avertissement, 0 erreur ; DLL 0.607.0.0. Non validé en jeu.
### English
- Items: « Annotated items » section: what has a note or a label, filtered by label (Important, To sell) and by text in the name or note; a click opens the item.

## 0.606.0
- Plan global, phase 4 : Monnaies : avec les alertes activées, Tensho prévient une fois quand les mémoquartz limités de la semaine atteignent leur plafond, dans le chat, par un son et sur Discord ; une case le retire.
- Tests : Weekly cap. Verify.ps1 (agent) : 1108/1108, 0 avertissement, 0 erreur ; DLL 0.606.0.0. Non validé en jeu.
### English
- Currencies: with alerts on, Tensho says once when the week's limited tomestones reach their limit, in chat, by sound and on Discord; a box removes it.

## 0.605.0
- Plan global, phase 4 : Banque : un rangement relevé d'office et inchangé n'est plus réécrit à chaque lecture (toutes les 2 minutes, ou 30 secondes) : le fichier n'est écrit que si son contenu change, ou au plus une fois par heure pour sa date. La date reste à jour en mémoire.
- Tests : Bank save. Verify.ps1 (agent) : 1106/1106, 0 avertissement, 0 erreur ; DLL 0.605.0.0. Non validé en jeu.
### English
- Bank: a storage read automatically and unchanged is no longer rewritten at each reading (every 2 minutes, or 30 seconds): the file is written only when its contents change, or at most once an hour for its date. The date stays current in memory.

## 0.604.0
- Plan global, C35 : Aide à la pêche : le joueur est copié sur le thread framework et l'affichage ne lit que cette copie ; les tables des postes de pêche et d'Abondance II sont construites hors de l'affichage à la première demande. Rien de visible, sinon moins d'à-coup à la première ouverture ; ces deux lignes peuvent manquer un court instant.
- Tests : Fishing help. Verify.ps1 (agent) : 1104/1104, 0 avertissement, 0 erreur ; DLL 0.604.0.0. Non validé en jeu.
### English
- Fishing help: the player is copied on the framework thread and the frame only reads that copy; the fishing spot and Bountiful II tables are built off the frame on first use. Nothing visible except less stutter on first opening; those two lines may be missing for a moment.

## 0.603.0
- Plan global, T23 : Compagnie libre : avec des personnages dans plusieurs compagnies, chaque atelier a son cadre et la date de son relevé. Oublier une compagnie existait déjà. Pas fait : le nom de la compagnie (pas enregistré, cadres numérotés) et la valeur du dernier butin.
- Tests : Workshops. Verify.ps1 (agent) : 1103/1103, 0 avertissement, 0 erreur ; DLL 0.603.0.0. Non validé en jeu.
### English
- Free company: with characters in several companies, each workshop has its own frame and reading date. Forgetting a company already existed. Not done: the company's name (not stored, frames are numbered) and the value of the last loot.

## 0.602.0
- Plan global, T28 : Collections : « Obtenus récemment » liste avec la date ce qui a été gagné depuis le relevé précédent (la première lecture sert de base ; ni les succès ni les types lus à part, coiffures et autres, ne sont suivis) ; le clic droit sur une emote ouvre ses sources.
- Tests : Recent. Verify.ps1 (agent) : 1102/1102, 0 avertissement, 0 erreur ; DLL 0.602.0.0. Non validé en jeu.
### English
- Collections: « Recently obtained » lists with the date what was gained since the previous reading (the first reading is the baseline; achievements and the separately read kinds, hairstyles and others, are not followed); right-clicking an emote opens its sources.

## 0.601.0
- Plan global, C36 : Si la lecture du carnet, de la synthèse ou de la récolte échoue, une courte ligne « indisponible » apparaît à côté de la fenêtre du jeu au lieu de rien, et Diagnostics nomme la lecture. La pêche n'est pas couverte. Aussi : la ligne de faits de C14 dans le panneau du carnet.
- Tests : Failed reading. Verify.ps1 (agent) : 1099/1099, 0 avertissement, 0 erreur ; DLL 0.601.0.0. Non validé en jeu.
### English
- If reading the crafting log, the synthesis or gathering fails, a short « unavailable » line appears beside the game's window instead of nothing, and Diagnostics names the reading. Fishing is not covered. Also: C14's facts line in the Crafting Log panel.

## 0.600.0
- Plan global, C14 : La recette dit, sur la fiche objet et dans le simulateur : HQ possible ou non, synthèse rapide et ses minimums, livre de maître, spécialiste, recette experte, objet et état requis, depuis la feuille Recipe du jeu. L'écart chiffré existait déjà dans le simulateur (A-12). Pas fait : savoir si le livre est lu, et l'affichage dans le panneau du carnet.
- Tests : Recipe facts. Verify.ps1 (agent) : 1097/1097, 0 avertissement, 0 erreur ; DLL 0.600.0.0. Non validé en jeu.
### English
- The recipe says, on the item sheet and in the simulator: HQ possible or not, quick synthesis and its minimums, master book, specialist, expert, required item and status, from the game's Recipe sheet. The numeric gap already existed in the simulator (A-12). Not done: whether the book is read, and the Crafting Log panel.
