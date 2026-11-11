# Journal des versions / Changelog, 0.700 à 0.799

Archive de [CHANGELOG.md](../../CHANGELOG.md) : versions plus anciennes que la dernière release, déplacées par `tools/changelog.py archive`. Même format (français, puis `### English` depuis 0.219.0) ; liens relatifs à la racine réécrits depuis ce dossier.

## 0.799.0
- Plan global, Verify : Demande (X-38), suite : l'icône du mage bleu au-dessus de la tête d'un monstre proche qui apprend un sort que vous n'avez pas, dans la vue du jeu, dessinée derrière les fenêtres de Tensho, les sorts au survol. Au choix aussi pour les monstres de vos listes (patte), du bestiaire (livre) et des ALÉA (étoile), désactivés au départ. Réglages dans la carte (« Icônes au-dessus des monstres proches » : raisons, taille, distance jusqu'à 150 yalms, masquer en combat) et sur la page Magie bleue. Les monstres sont relus tous les dixièmes de seconde pour suivre leurs déplacements. Non validé en jeu.
- Tests : MonsterIcons799Tests. Verify.ps1 (agent) : 1379/1379, 0 avertissement, 0 erreur ; DLL 0.799.0.0. Non validé en jeu.
### English
- Request (X-38), continued: the Blue Mage icon above the head of a nearby monster that teaches a spell you lack, in the game's view, drawn behind Tensho's windows, the spells on hover. Also, when chosen, for monsters of your lists (paw), hunting log (book) and FATEs (star), off at first. Settings on the map (« Icons above nearby monsters »: reasons, size, distance up to 150 yalms, hide in combat) and on the Blue magic page. Monsters are read every tenth of a second to follow their moves. Not validated in game.

## 0.798.0
- Plan global, Verify : Demande (X-38), première partie : les monstres proches qui apprennent une magie bleue que vous n'avez pas sont marqués d'un chapeau de magicien sur la carte, les sorts au survol. Le jeu ne relie aucun sort à un monstre : la source est le wiki, déjà lu sort par sort ; la page Magie bleue a maintenant un bouton qui lit les sorts manquants, une page toutes les trois secondes, gardées sur le disque, rien sans ce clic. Les noms anglais du wiki sont reliés aux monstres du jeu par leur nom. Réglages : « Monstres pour le mage bleu » (carte et page Magie bleue) et « Même hors mage bleu », désactivé au départ. L'icône au-dessus de la tête vient ensuite. Non validé en jeu.
- Tests : BlueNearby798Tests. Verify.ps1 (agent) : 1378/1378, 0 avertissement, 0 erreur ; DLL 0.798.0.0. Non validé en jeu.
### English
- Request (X-38), first part: nearby monsters that teach a blue magic spell you lack are marked with a wizard's hat on the map, the spells on hover. The game links no spell to a monster: the source is the wiki, already read spell by spell; the Blue magic page now has a button that reads the missing spells, one page every three seconds, kept on disk, nothing without that click. The wiki's English names are matched to the game's monsters by name. Settings: « Monsters for Blue Mage » (map and Blue magic page) and « Even when not Blue Mage », off at first. The icon above the head comes next. Not validated in game.

## 0.797.0
- Plan global, Verify : Demande (X-37) : les monstres proches qui font partie d'un ALÉA en cours sont marqués d'une étoile sur la carte, avec le nom de l'ALÉA au survol (réglage « Monstres des ALÉA » dans la carte) ; un monstre du même nom hors de l'ALÉA n'est pas marqué. Pour les mandats, le lien entre un monstre et le mandat n'est pas encore connu : en mode développeur, l'événement de chaque monstre proche est noté au journal, à relever pendant un mandat de combat. Non validé en jeu.
- Tests : FateMonsters797Tests. Verify.ps1 (agent) : 1376/1376, 0 avertissement, 0 erreur ; DLL 0.797.0.0. Non validé en jeu.
### English
- Request (X-37): nearby monsters that belong to a running FATE are marked with a star on the map, with the FATE's name on hover (map setting « Monsters of FATEs »); a monster of the same name outside the FATE is not marked. For leves, the link between a monster and the leve is not known yet: in developer mode, each nearby monster's event is written to the log, to read during a battle leve. Not validated in game.

## 0.796.0
- Plan global, Verify : Demande (X-36) : les monstres proches que le bestiaire demande encore. Sur la carte, un livre marque un monstre encore à vaincre au rang actuel d'un bestiaire (le seul que le jeu compte) ; au survol, l'entrée et les victoires (« Gladiateur 01, 1/2 »). Réglage « Monstres de votre bestiaire » dans la carte, et le même sur la page Bestiaire. Les monstres proches sont maintenant une seule couche : une icône par monstre, toutes ses raisons au survol (butin pour vos listes, bestiaire). Non validé en jeu.
- Tests : HuntNearby796Tests. Verify.ps1 (agent) : 1374/1374, 0 avertissement, 0 erreur ; DLL 0.796.0.0. Non validé en jeu.
### English
- Request (X-36): nearby monsters the hunting log still asks for. On the map, a book marks a monster still to defeat in a log's current rank (the only one the game counts); on hover, the entry and the kills (« Gladiator 01, 1/2 »). Setting « Monsters for your hunting log » on the map, and the same on the Hunting log page. Nearby monsters are now one layer: one icon per monster, every reason on hover (drops for your lists, hunting log). Not validated in game.

## 0.795.0
- Plan global, Verify : Demande (X-34, X-35) : les monstres proches, en information seulement. Tensho lit les ennemis vivants que le jeu charge autour du personnage (nom, lieu, niveau ; 100 au plus ; coupé en JcJ). Premier usage : un monstre proche qui lâche un objet que vos listes de suivi demandent encore apparaît sur la carte avec une patte (son nom, son niveau et ce qu'il lâche au survol), et la ligne de l'objet dans la liste porte la même patte. Réglages : « Monstres de vos listes » dans la carte, « Monstres proches qui le lâchent » sous « Monstre » dans les options des listes. Données de butin : LuminaSupplemental. Non validé en jeu.
- Tests : NearbyMonsters795Tests. Verify.ps1 (agent) : 1372/1372, 0 avertissement, 0 erreur ; DLL 0.795.0.0. Non validé en jeu.
### English
- Request (X-34, X-35): nearby monsters, as information only. Tensho reads the living enemies the game streams around the character (name, place, level; 100 at most; cut in PvP). First use: a nearby monster that drops an item your tracking lists still need shows on the map with a paw (name, level and drops on hover), and the item's line in the list carries the same paw. Settings: « Monsters for your lists » on the map, « Nearby monsters that drop it » under « Monster » in the list options. Drop data: LuminaSupplemental. Not validated in game.

## 0.794.0
- Plan global, Verify : Demande (X-33) : « Que faire avec ? » tient compte des ventes par jour, comme « Que puis-je fabriquer ? ». Une vente isolée (Matière sombre grade I « vendue » 350 000) mettait l'objet en tête. Nouvelles colonnes « Ventes / jour » et « Gils en N jours » : ce qui se vend en ce nombre de jours au rythme du marché, sans dépasser ce que votre solde achète ; le classement suit ces gils, la vitesse inconnue en dernier. Le nombre de jours est celui de « Que puis-je fabriquer ? », réglable sur la page. Non validé en jeu.
- Tests : CurrencySales794Tests. Verify.ps1 (agent) : 1369/1369, 0 avertissement, 0 erreur ; DLL 0.794.0.0. Non validé en jeu.
### English
- Request (X-33): « What to do with? » counts the sales per day, like « What can I craft? ». One odd sale (Dark Matter grade I « sold » for 350,000) put the item first. New columns « Sold a day » and « Gil in N days »: what sells in that many days at the market's speed, no more than your balance buys; the ranking follows that gil, unknown speed last. The days are those of « What can I craft? », set on the page too. Not validated in game.

## 0.793.0
- Plan global, Verify : Demande (X-32) : plus besoin d'ouvrir et fermer la carte du jeu pour voir la zone d'un mandat. La capture de 0.792 (mandat 1670) montre que le jeu tient la zone dans sa propre liste dès l'acceptation, alors que la liste de la fenêtre carte, la seule que Tensho lisait, est vide ; une fois le mandat commencé, elle tient aussi ses points, un de moins à chaque récolte. Tensho lit maintenant cette liste (lecture seule, sans rien ouvrir ni appeler) et ajoute ce qui manque, sans doublon. Non validé en jeu.
- Tests : GameLeves793Tests. Verify.ps1 (agent) : 1366/1366, 0 avertissement, 0 erreur ; DLL 0.793.0.0. Non validé en jeu.
### English
- Request (X-32): no more opening and closing the game map to see a leve's area. The 0.792 capture (leve 1670) shows the game keeps the area in its own list from acceptance, while the map window's list, the only one Tensho read, is empty; once the leve is started it also holds its points, one fewer per gathering. Tensho now reads that list (read only, nothing opened or called) and adds what is missing, without doubles. Not validated in game.

## 0.792.0
- Plan global, Verify : Demande (X-32) : une solution au blocage qu'on contournait en ouvrant puis fermant la carte du jeu (zone d'un mandat absente tant que la carte du jeu n'avait pas été ouverte à portée). Piste, sans rien ouvrir ni appeler dans le jeu : le jeu garde aussi ses marqueurs de quêtes et de mandats dans sa propre liste (Map), à part de celle de la fenêtre carte que Tensho lit. En mode développeur, cette liste est notée au journal avec l'état de la carte du jeu, pour vérifier qu'elle contient les zones carte fermée. Aucun changement visible. Non validé en jeu.
- Tests : QuestMarkers792Tests. Verify.ps1 (agent) : 1364/1364, 0 avertissement, 0 erreur ; DLL 0.792.0.0. Non validé en jeu.
### English
- Request (X-32): a fix for what opening and closing the game map worked around (a leve's area missing until the game map had been opened in range). Lead, without opening or calling anything in the game: the game also keeps its quest and leve markers in its own list (Map), apart from the map window's list Tensho reads. In developer mode that list is written to the log with the game map's state, to check it holds the areas while the map is closed. No visible change. Not validated in game.

## 0.791.0
- Plan global, Verify : Lourdeur (X-01) : le journal du 2026-11-07 montre la carte à 110–160 ms plusieurs fois par heure (« map overlay layout »), pas seulement à l'ouverture. Cette mesure englobait tout le dessin de la carte : en mode développeur, elle est maintenant découpée (commandes, puis dans la carte : texture et noms, points de récolte, zones, icônes, signets et joueur, bords et info-bulles), pour trouver le morceau lent. Aucun changement visible. Non validé en jeu.
- Tests : MapMarks791Tests. Verify.ps1 (agent) : 1363/1363, 0 avertissement, 0 erreur ; DLL 0.791.0.0. Non validé en jeu.
### English
- Heaviness (X-01): the 2026-11-07 log shows the map at 110-160 ms several times an hour (« map overlay layout »), not only at opening. That measure covered the whole map drawing: in developer mode it is now split (controls, then inside the map: texture and names, gathering points, areas, icons, bookmarks and player, edges and tooltips), to find the slow part. No visible change. Not validated in game.

## 0.790.0
- Plan global, Verify : Demande (X-31) : dans « Fabrications restantes », chaque étape se déplie et se replie, jusqu'à l'étape finale, avec son compte (faits / total). Les lignes sont écrites comme les autres, « possédés / demandés » avec les mêmes couleurs ; le métier et le nombre de fabrications restantes passent au survol de l'icône du métier. Non validé en jeu.
- Tests : CraftStepsFold790Tests. Verify.ps1 (agent) : 1362/1362, 0 avertissement, 0 erreur ; DLL 0.790.0.0. Non validé en jeu.
### English
- Request (X-31): in « Crafts left », each step folds and unfolds, down to the final one, with its count (done / total). The lines are written like the others, « held / needed » in the same colours; the job and the crafts left move to the job icon's tooltip. Not validated in game.

## 0.789.0
- Plan global, Verify : Demande (X-30) : voir les étapes de fabrication qui restent pour les listes d'atelier et d'artisanat. Chaque liste de suivi a une section repliable « Fabrications restantes », par étape (l'étape 1 se fabrique d'abord, chaque objet après ce qui le compose) : icône du métier, objet, nombre de fabrications et quantité obtenue. Le stock est déduit, les objets récoltés ou achetés ne sont pas fabriqués, comme pour les matériaux. Non validé en jeu.
- Tests : CraftSteps789Tests. Verify.ps1 (agent) : 1361/1361, 0 avertissement, 0 erreur ; DLL 0.789.0.0. Non validé en jeu.
### English
- Request (X-30): see the crafting steps left for the workshop and crafting lists. Each tracking list has a folding « Crafts left » section, by step (step 1 is made first, each item after what it is made of): job icon, item, number of crafts and quantity made. The stock is taken off, gathered or bought items are not crafted, as for the materials. Not validated in game.

## 0.788.0
- Plan global, Verify : Signalement (X-29) : en Menuisier sur une recette de Métallurgie, le panneau du carnet ne semblait pas voir le mauvais métier. Il le voyait (le calcul utilise les attributs relevés du bon métier), mais la note était en couleur d'information et coupée. Elle dit maintenant « Mauvais métier : vous êtes Menuisier, cette recette est de Métallurgie », en couleur d'avertissement, sur plusieurs lignes, avec le bouton pour porter le métier de la recette. Non validé en jeu.
- Tests : WrongJob788Tests. Verify.ps1 (agent) : 1358/1358, 0 avertissement, 0 erreur ; DLL 0.788.0.0. Non validé en jeu.
### English
- Report (X-29): as Carpenter on a Blacksmith recipe, the crafting log panel did not seem to see the wrong job. It did (the calculation uses the right job's recorded attributes), but the note was in the information colour and cut off. It now says « Wrong job: you are Carpenter, this recipe is Blacksmith », in the warning colour, wrapped, with the button to wear the recipe's job. Not validated in game.

## 0.787.0
- Plan global, Verify : Demande (X-28) : l'atelier de compagnie tient compte de ce qui est déjà livré. La capture de la fenêtre de livraison (0.786, livraison de rivets en bronze) a donné sa disposition : phase en cours, lots livrés et requis de chaque objet. Tensho la lit chaque seconde quand elle est ouverte, la garde, retire les phases déjà validées et déduit les lots livrés de la phase en cours, dans le panneau et la page Atelier ; la page dit la phase et l'heure du relevé. Utilisé seulement si cela correspond au projet. Non validé en jeu.
- Tests : WorkshopDelivered787Tests. Verify.ps1 (agent) : 1357/1357, 0 avertissement, 0 erreur ; DLL 0.787.0.0. Non validé en jeu.
### English
- Request (X-28): the company workshop counts what it already received. The capture of the delivery window (0.786, bronze rivets delivered) gave its layout: phase in progress, sets delivered and required per item. Tensho reads it every second while it is open, keeps it, leaves out the phases already validated and takes the delivered sets off the phase in progress, in the panel and the Workshop page; the page gives the phase and when it was seen. Used only when it fits the project. Not validated in game.

## 0.786.0
- Plan global, Verify : Demande (X-28) : tenir compte de ce qui est déjà livré à l'atelier de compagnie. Le jeu ne le garde dans aucune structure connue de Tensho (l'agent de la liste des matériaux ne donne que les objets et le résultat) : seule la fenêtre de livraison le montre. Première étape : en mode développeur, cette fenêtre et la liste des matériaux sont notées au journal quand elles s'ouvrent ou changent, pour lire leur disposition avant de compter. Aucun changement visible. Non validé en jeu.
- Tests : WorkshopCapture786Tests. Verify.ps1 (agent) : 1353/1353, 0 avertissement, 0 erreur ; DLL 0.786.0.0. Non validé en jeu.
### English
- Request (X-28): count what the company workshop already received. The game keeps it in no structure Tensho knows (the material list's agent gives only the items and the result): only the delivery window shows it. First step: in developer mode, that window and the material list are written to the log when they open or change, to read their layout before counting. No visible change. Not validated in game.

## 0.785.0
- Plan global, Verify : Demande (X-27) : les listes de suivi peuvent n'afficher que les objets finis. Nouveau réglage « Objets finis seulement », éteint au départ, sous les options des listes : liste d'artisanat (ses objets), atelier de compagnie (les livraisons), préparation (l'objet préparé), sans leurs matériaux. L'aide de récolte suit toujours les matériaux. Non validé en jeu.
- Tests : FinishedOnly785Tests. Verify.ps1 (agent) : 1352/1352, 0 avertissement, 0 erreur ; DLL 0.785.0.0. Non validé en jeu.
### English
- Request (X-27): the tracking lists can show only the finished items. New setting « Finished items only », off at first, under the list options: crafting list (its items), company workshop (the deliveries), preparation (the prepared item), without their materials. The gathering help still follows the materials. Not validated in game.

## 0.784.0
- Plan global, Verify : Signalement (X-26) : « Comparer les prochains coups » ne projetait rien sur une recette en haute qualité impossible (Kit de réparation d'appareil) : le jeu y montre une barre de qualité sur 2500, Tensho garde 0 exprès, et la vérification refusait cet écart. Il est maintenant accepté pour une recette sans qualité à chercher ; les autres vérifications restent. Non validé en jeu.
- Tests : NoHqLive784Tests. Verify.ps1 (agent) : 1351/1351, 0 avertissement, 0 erreur ; DLL 0.784.0.0. Non validé en jeu.
### English
- Report (X-26): « Compare the next moves » projected nothing on a recipe where HQ is impossible (Device repair kit): the game shows a quality bar of 2,500 there, Tensho keeps 0 on purpose, and the check refused that gap. It is now accepted for a recipe with no quality to seek; the other checks stay. Not validated in game.

## 0.783.0
- Plan global, Verify : Suite de X-25, vérifiée en jeu sur capture : la recette qui faisait geler le jeu s'ouvre, Tensho propose sa séquence. Le message « Raphael a échoué » était trompeur, Raphael n'étant pas appelé exprès (recette en haute qualité impossible, sans qualité à chercher) : il dit maintenant « Raphael non utilisé », sans avertissement au journal. Non validé en jeu pour le message.
- Tests : RaphaelGuard782Tests. Verify.ps1 (agent) : 1350/1350, 0 avertissement, 0 erreur ; DLL 0.783.0.0. Non validé en jeu.
### English
- X-25, continued, checked in game on screenshot: the recipe that froze the game opens, Tensho offers its sequence. « Raphael failed » was misleading, Raphael being left out on purpose (HQ impossible, no quality to seek): it now says « Raphael not used », with no warning in the log. The message is not validated in game.

## 0.782.0
- Plan global, Verify : Gel du jeu (X-25), causé par Tensho : en ouvrant le carnet d'artisan (cuisinier, recette 5623), le solveur Raphael, code natif, a divisé par un gain de qualité de 0 ; une telle erreur arrête le jeu sans pouvoir être rattrapée. Tensho vérifie maintenant chaque valeur avant l'appel (gains nuls, durabilité ou progression nulles, valeurs trop grandes) et, dans ces cas, ne demande rien à Raphael ; sa propre recherche reste. Non validé en jeu.
- Tests : RaphaelGuard782Tests. Verify.ps1 (agent) : 1350/1350, 0 avertissement, 0 erreur ; DLL 0.782.0.0. Non validé en jeu.
### English
- Game freeze (X-25), caused by Tensho: opening the crafting log (culinarian, recipe 5623), the Raphael solver, native code, divided by a quality gain of 0; such an error stops the game and cannot be caught. Tensho now checks every value before the call (zero gains, zero durability or progress, values too large) and, in those cases, asks Raphael nothing; its own search remains. Not validated in game.

## 0.781.0
- Plan global, Verify : Lien d'alarme (X-03) : le clic droit faisait défiler le chat sans ouvrir de menu. En mode développeur, chaque menu contextuel ouvert est maintenant noté au journal (type, fenêtre, cible, objet survolé), pour voir ce que le jeu ouvre. Aucun changement visible. Non validé en jeu.
- Tests : MenuLog781Tests. Verify.ps1 (agent) : 1348/1348, 0 avertissement, 0 erreur ; DLL 0.781.0.0. Non validé en jeu.
### English
- Alarm link (X-03): the right click scrolled the chat and opened no menu. In developer mode, every context menu opened is now noted in the log (type, window, target, hovered item), to see what the game opens. No visible change. Not validated in game.

## 0.780.0
- Plan global, Verify : Signalement (X-03) : le clic droit ne faisait rien sur l'indication grise « (clic droit : Tensho) », qui ne faisait pas partie du lien. L'indication est maintenant dans le lien d'objet : un clic droit sur toute la ligne ouvre le menu de l'objet. Non validé en jeu.
- Tests : AlarmLinkShape771Tests. Verify.ps1 (agent) : 1347/1347, 0 avertissement, 0 erreur ; DLL 0.780.0.0. Non validé en jeu.
### English
- Report (X-03): a right click did nothing on the grey « (right click: Tensho) » hint, which was not part of the link. The hint is now inside the item link: a right click anywhere on the line opens the item's menu. Not validated in game.

## 0.779.0
- Plan global, Verify : Signalement (X-24) : après un chargement du plugin, les peaux et la corne passaient en « Autres », puis en « Monstres » plus tard. Cause : la source des monstres (LuminaSupplemental) ne se chargeait que quand une autre page la demandait. Les listes de suivi la demandent maintenant elles-mêmes ; elle se relit de sa copie sur disque, sans réseau tant que la copie existe. Non validé en jeu.
- Tests : ListZones752Tests. Verify.ps1 (agent) : 1347/1347, 0 avertissement, 0 erreur ; DLL 0.779.0.0. Non validé en jeu.
### English
- Report (X-24): after a plugin reload, hides and the horn sat in « Others », then moved to « Monsters » later. Cause: the monster source (LuminaSupplemental) loaded only when another page asked for it. The tracking lists now ask for it themselves; it is read from its copy on disk, with no network while the copy exists. Not validated in game.

## 0.778.0
- Plan global, Verify : Demande (X-23) : une nouvelle alarme n'est plus envoyée sur Discord par défaut ; une alarme qui rejoint un groupe prend le réglage du groupe ; vos alarmes déjà enregistrées gardent le leur. Non validé en jeu.
- Tests : DiscordOff778Tests. Verify.ps1 (agent) : 1347/1347, 0 avertissement, 0 erreur ; DLL 0.778.0.0. Non validé en jeu.
### English
- Request (X-23): a new alarm is no longer sent to Discord by default; one joining a group takes the group's setting; your saved alarms keep theirs. Not validated in game.

## 0.777.0
- Plan global, Verify : Signalement (X-22) : certaines coordonnées du wiki (Butin des monstres, Magie bleue) n'avaient pas le clic droit vers l'éthérite la plus proche. Cause : le nom anglais du lieu n'était traduit qu'en un seul numéro, alors que le jeu en a parfois plusieurs (« Mor Dhona » : 26, 67, 509) ; si le premier n'avait pas de carte, le menu disparaissait. Tous les numéros sont essayés ; un lieu toujours introuvable est noté au journal en mode développeur. Non validé en jeu.
- Tests : WikiPlaces777Tests. Verify.ps1 (agent) : 1346/1346, 0 avertissement, 0 erreur ; DLL 0.777.0.0. Non validé en jeu.
### English
- Report (X-22): some wiki coordinates (Monster loot, Blue magic) had no right click to the nearest aetheryte. Cause: the place's English name was turned into one place row only, while the game sometimes has several (« Mor Dhona »: 26, 67, 509); if the first had no map, the menu was gone. Every row is tried; a place still not found is noted in the log in developer mode. Not validated in game.

## 0.776.0
- Plan global, Verify : Validé en jeu sur capture de l'utilisateur : l'alarme porte le lien d'objet du jeu, et le clic droit dessus ouvre le menu « Tensho — objet » complet (X-03). Ventes des servants (I-05) notées validées. Aucun changement du plugin.
- Tests : GilTeleport775Tests. Verify.ps1 (agent) : 1345/1345, 0 avertissement, 0 erreur ; DLL 0.776.0.0. Non validé en jeu.
### English
- Validated in game on the user's screenshot: the ring carries the game's item link, and a right click on it opens the full « Tensho — item » menu (X-03). Retainer sales (I-05) recorded as validated. No change to the plugin.

## 0.775.0
- Plan global, Verify : Journal des gils (X-18) : le relevé montrait des téléportations rangées en « Autre », le jeu prélevant le prix à l'arrivée, après la fermeture de la fenêtre Téléportation. Une dépense qui suit un écran de chargement compte maintenant comme téléportation, jamais un gain. Les anciennes lignes restent comme elles étaient. Non validé en jeu.
- Tests : GilTeleport775Tests. Verify.ps1 (agent) : 1345/1345, 0 avertissement, 0 erreur ; DLL 0.775.0.0. Non validé en jeu.
### English
- Gil journal (X-18): the log showed teleports filed as « Other », the game taking the price on arrival, after the Teleport window closed. A spending right after a loading screen now counts as a teleport, never a gain. Older lines stay as they were. Not validated in game.

## 0.774.0
- Plan global, Verify : Lien d'alarme (X-03) : la console de Dalamud en mode détaillé montre que le lien « Ouvrir dans Tensho » était bien enregistré, mais que le jeu ne le transmettait jamais à Dalamud ; il ouvrait une bulle d'objet. L'alarme porte maintenant le vrai lien d'objet du jeu, suivi de « (clic droit : Tensho) » : le survol montre l'objet, le clic droit ouvre son menu, avec l'entrée Tensho. Non validé en jeu.
- Tests : AlarmLinkShape771Tests. Verify.ps1 (agent) : 1344/1344, 0 avertissement, 0 erreur ; DLL 0.774.0.0. Non validé en jeu.
### English
- Alarm link (X-03): Dalamud's console in verbose mode shows that the « Open in Tensho » link was registered, but the game never handed it to Dalamud; it opened an item tooltip. The ring now carries the game's own item link, followed by « (right click: Tensho) »: hovering shows the item, a right click opens its menu, with Tensho's entry. Not validated in game.

## 0.773.0
- Plan global, Verify : Lien d'alarme (X-03), toujours aucun clic après 0.771 : en mode développeur, chaque alarme écrit au journal l'identité du lien imprimé et si Dalamud l'a bien enregistré, pour savoir si le défaut est dans l'enregistrement ou dans le clic. Aucun changement visible. Non validé en jeu.
- Tests : AlarmLinkShape771Tests. Verify.ps1 (agent) : 1344/1344, 0 avertissement, 0 erreur ; DLL 0.773.0.0. Non validé en jeu.
### English
- Alarm link (X-03), still no click after 0.771: in developer mode, each ring writes to the log the printed link's identity and whether Dalamud has it registered, to tell whether the fault is in the registration or in the click. No visible change. Not validated in game.

## 0.772.0
- Plan global, Verify : Signalement (X-21) : « Ajouter les alarmes » proposait 19 alarmes alors qu'un seul objet en avait besoin. Cause : il comptait aussi les objets qu'on trouve sans horaire (cristaux et éclats, présents aussi sur des points ordinaires). Correction : seuls les objets qui manquent encore et ne poussent qu'à des points à horaire, comme le groupe « Points à horaire ». Non validé en jeu.
- Tests : ListAlarms755Tests. Verify.ps1 (agent) : 1344/1344, 0 avertissement, 0 erreur ; DLL 0.772.0.0. Non validé en jeu.
### English
- Report (X-21): « Add the alarms » offered 19 alarms when one item needed them. Cause: it also counted items found without hours (crystals and shards, also at ordinary points). Fix: only items still missing that grow nowhere but at timed points, as the « Timed points » group. Not validated in game.

## 0.771.0
- Plan global, Verify : Signalement (X-03) : « Ouvrir dans Tensho » ne marche toujours pas, même avec Tensho seul ; aucun clic n'arrive. Le code de couleur était placé à l'intérieur du lien, entre son début et son texte ; il est maintenant autour, comme pour les liens de Dalamud. Le journal note toujours chaque clic. Non validé en jeu.
- Tests : AlarmLinkShape771Tests. Verify.ps1 (agent) : 1344/1344, 0 avertissement, 0 erreur ; DLL 0.771.0.0. Non validé en jeu.
### English
- Report (X-03): « Open in Tensho » still does not work, even with Tensho alone; no click arrives. The colour code sat inside the link, between its start and its text; it now surrounds it, as Dalamud's own links do. The log still notes each click. Not validated in game.

## 0.770.0
- Plan global, Verify : Signalement (X-20) : un objet déposé chez un servant, dans les sacoches ou au coffre de compagnie n'apparaissait qu'en rouvrant. Le rangement ouvert est maintenant relu toutes les 2 secondes au lieu de 30 ; le fichier n'est écrit que si la copie change. Non validé en jeu.
- Tests : OpenStorage770Tests. Verify.ps1 (agent) : 1343/1343, 0 avertissement, 0 erreur ; DLL 0.770.0.0. Non validé en jeu.
### English
- Report (X-20): an item put in a retainer, the saddlebag or the company chest showed only after reopening it. The open storage is now read again every 2 seconds instead of 30; the file is written only when the copy changes. Not validated in game.

## 0.769.0
- Plan global, Verify : Signalement (X-19) : un dépôt chez un servant comptait comme une dépense du personnage et un gain des servants, et le total des servants ne suivait qu'en rouvrant. Corrections : les échanges avec les servants et le coffre de compagnie sont des transferts, affichés à part et hors du net ; une hausse des servants au-delà de vos dépôts compte comme leurs ventes au marché ; le servant ouvert est lu en direct. Les anciennes lignes « Servants » deviennent « Échanges avec les servants ». Non validé en jeu.
- Tests : GilTransfer769Tests. Verify.ps1 (agent) : 1342/1342, 0 avertissement, 0 erreur ; DLL 0.769.0.0. Non validé en jeu.
### English
- Report (X-19): a deposit with a retainer counted as the character's spending and the retainers' gain, and the retainers' total followed only after reopening. Fixes: exchanges with retainers and the company chest are transfers, shown apart and out of the net; a rise of the retainers beyond your deposits counts as their market sales; the open retainer is read live. Older « Retainers » lines become « Exchanges with retainers ». Not validated in game.

## 0.768.0
- Plan global, Verify : Demande (X-18) : le journal des gils montre l'icône des gils du jeu, une icône par catégorie, les gains en vert et les dépenses en rouge. Pour réduire « Autre » sans deviner : en mode développeur, chaque mouvement « Autre » écrit au journal les fenêtres du jeu ouvertes dans les 15 secondes d'avant. Non validé en jeu.
- Tests : GilJournal768Tests. Verify.ps1 (agent) : 1340/1340, 0 avertissement, 0 erreur ; DLL 0.768.0.0. Non validé en jeu.
### English
- Request (X-18): the gil journal shows the game's gil icon, an icon per category, gains in green and spendings in red. To shrink « Other » without guessing: in developer mode, each « Other » change writes to the log the game windows open in the 15 seconds before. Not validated in game.

## 0.767.0
- Plan global, Verify : Relevé en jeu (I10), confirmé par l'utilisateur : en enregistrant un servant, Tensho garde aussi ses gils et ses cristaux. Ses cristaux comptent dans les stocks, comptés exactement et non plus comme un minimum ; la page Personnages ajoute « (+N chez les servants) ». Un servant enregistré avant reste valable. Vente des servants (I-05) validée en jeu sur capture. Non validé en jeu pour I10.
- Tests : RetainerExtras767Tests. Verify.ps1 (agent) : 1339/1339, 0 avertissement, 0 erreur ; DLL 0.767.0.0. Non validé en jeu.
### English
- In-game reading (I10), confirmed by the user: when a retainer is recorded, Tensho also keeps its gil and crystals. Its crystals count in the stocks, exactly rather than as a lower bound; the Characters page adds « (+N with retainers) ». A retainer recorded before stays valid. Retainer sales (I-05) validated in game on screenshot. I10 not validated in game.

## 0.766.0
- Plan global, Verify : Carte, parties non découvertes (P11) : première sonde des données du jeu. Trouvés : la fonction qui dit si une région est découverte, et l'atlas des régions de chaque carte. Manque : où chaque morceau se place sur la carte ; rien n'est deviné, P11 attend. Nouvelle sonde tools/MapMaskProbe. Aucun changement du plugin.
- Tests : FewerClicks763Tests. Verify.ps1 (agent) : 1337/1337, 0 avertissement, 0 erreur ; DLL 0.766.0.0. Non validé en jeu.
### English
- Map, undiscovered parts (P11): first probe of the game's data. Found: the function that says whether a region is discovered, and the region atlas of each map. Missing: where each piece goes on the map; nothing is guessed, P11 waits. New probe tools/MapMaskProbe. No change to the plugin.

## 0.765.0
- Plan global, Verify : Suite de X-17 : un panneau de suivi réduit depuis le chargement du plugin, jamais dessiné, comptait encore pour rien ; sa source est maintenant connue dès qu'il est actif. Non validé en jeu.
- Tests : TrackedNeed751Tests. Verify.ps1 (agent) : 1337/1337, 0 avertissement, 0 erreur ; DLL 0.765.0.0. Non validé en jeu.
### English
- X-17, continued: a tracking panel folded since the plugin loaded, never drawn, still counted for nothing; its source is now known as soon as it is on. Not validated in game.

## 0.764.0
- Plan global, Verify : Signalements (X-17) : panneau de suivi réduit, l'aide perdait « Vos listes en demandent encore » et le cadre ; et les cadres passaient au-dessus des autres fenêtres. Corrections : les lignes d'un panneau réduit sont recalculées (un panneau fermé ne compte pas) ; les cadres de la fenêtre de récolte et des barres sont dessinés sous les fenêtres de Tensho, au-dessus du jeu. Non validé en jeu.
- Tests : TrackedNeed751Tests. Verify.ps1 (agent) : 1337/1337, 0 avertissement, 0 erreur ; DLL 0.764.0.0. Non validé en jeu.
### English
- Reports (X-17): with the tracking panel folded, the help lost « Your lists still need » and its frame; and the frames went over other windows. Fixes: the lines of a folded panel are worked out (a closed panel does not count); the frames on the gathering window and the hotbars are drawn under Tensho's windows, over the game. Not validated in game.

## 0.763.0
- Plan global, Verify : Demande (X-16) : quand deux plans de PR donnent le même rendement, l'aide choisit celui qui demande le moins de clics (une action qui dure toute la récolte plutôt qu'une par récolte), puis le moins cher. Un meilleur rendement reste prioritaire. Non validé en jeu.
- Tests : FewerClicks763Tests. Verify.ps1 (agent) : 1337/1337, 0 avertissement, 0 erreur ; DLL 0.763.0.0. Non validé en jeu.
### English
- Request (X-16): when two GP plans give the same yield, the help picks the one with the fewest clicks (an action lasting the whole node rather than one per gather), then the cheapest. A better yield still comes first. Not validated in game.

## 0.762.0
- Plan global, Verify : Demande (X-15) : le menu du clic droit d'un objet récolté propose « Y aller », comme la page Récolte : drapeau, carte de Tensho et téléportation vers l'éthérite la plus proche (question, sauf réglage). Le point choisi est un point ouvert d'abord, puis dans votre zone, puis la téléportation la moins chère. Non validé en jeu.
- Tests : GoThereMenu762Tests. Verify.ps1 (agent) : 1335/1335, 0 avertissement, 0 erreur ; DLL 0.762.0.0. Non validé en jeu.
### English
- Request (X-15): the right-click menu of a gathered item offers « Go there », as the Gathering page does: flag, Tensho map and teleport to the nearest aetheryte (a question, unless set otherwise). The point chosen is an open one first, then one in your zone, then the cheapest teleport. Not validated in game.

## 0.761.0
- Plan global, Verify : Demande (X-14) : dans le groupe « Points à horaire » des listes de suivi, chaque objet dit « ouvert · encore h:mm:ss » ou « fermé · ouvre dans h:mm:ss », d'après le plus proche de ses points. Non validé en jeu.
- Tests : ZoneOrder757Tests. Verify.ps1 (agent) : 1334/1334, 0 avertissement, 0 erreur ; DLL 0.761.0.0. Non validé en jeu.
### English
- Request (X-14): in the « Timed points » group of the tracking lists, each item says « open · h:mm:ss left » or « closed · opens in h:mm:ss », from the nearest of its points. Not validated in game.

## 0.760.0
- Plan global, Verify : Lourdeur (X-01), régression de 0.757–0.758 : le journal montre le panneau des matériaux de l'atelier bloquant le jeu de 1,7 à 7,6 s, surtout du nettoyage mémoire. Le regroupement par zone et les prix de téléportation étaient recalculés à chaque image, en parcourant toutes les sources de récolte pour chaque zone. Ils sont maintenant calculés une fois par seconde, ou au changement de zone, avec des index préparés une fois. Non validé en jeu.
- Tests : ZoneOrder757Tests. Verify.ps1 (agent) : 1334/1334, 0 avertissement, 0 erreur ; DLL 0.760.0.0. Non validé en jeu.
### English
- Heaviness (X-01), regression of 0.757–0.758: the log shows the workshop materials panel holding the game for 1.7 to 7.6 s, mostly memory collection. The zone grouping and teleport prices were worked out at every frame, scanning every gathering source for each zone. They are now worked out once a second, or when the zone changes, with indexes built once. Not validated in game.

## 0.759.0
- Plan global, Verify : Signalement (X-03) : « Ouvrir dans Tensho » sur une alarme n'ouvrait rien. Le journal de 0.743 montre qu'aucun clic n'arrivait. Cause, dans le code de Dalamud : un lien sans gestionnaire ne fait rien, et chaque rechargement du plugin effaçait ses gestionnaires ; les messages d'alarme déjà affichés ne menaient donc plus nulle part. Correction : au chargement, les liens de toutes vos alarmes sont réenregistrés. Non validé en jeu.
- Tests : AlarmLinks759Tests. Verify.ps1 (agent) : 1334/1334, 0 avertissement, 0 erreur ; DLL 0.759.0.0. Non validé en jeu.
### English
- Report (X-03): « Open in Tensho » on an alarm opened nothing. The 0.743 log shows that no click arrived. Cause, in Dalamud's code: a link with no handler does nothing, and each reload of the plugin removed its handlers; rings already printed led nowhere. Fix: at load, the links of all your alarms are registered again. Not validated in game.

## 0.758.0
- Plan global, Verify : Demande (X-13) : dans les listes de suivi, les objets qui ne poussent qu'à des points à horaire forment leur propre groupe, « Points à horaire », en tête et repliable ; leur zone s'affiche au survol. Les autres restent par zone. Non validé en jeu.
- Tests : ZoneOrder757Tests. Verify.ps1 (agent) : 1333/1333, 0 avertissement, 0 erreur ; DLL 0.758.0.0. Non validé en jeu.
### English
- Request (X-13): in the tracking lists, items that grow only at timed points form their own group, « Timed points », first and folding; their zone shows on hover. The others stay by zone. Not validated in game.

## 0.757.0
- Plan global, Verify : Demandes (X-11, X-12) : dans les listes de suivi, chaque zone de récolte se replie ; la zone où vous êtes vient d'abord (« vous y êtes »), puis les autres de la téléportation la moins chère à la plus chère, avec son prix lu dans le jeu depuis votre position ; un prix inconnu passe après. Non validé en jeu.
- Tests : ZoneOrder757Tests. Verify.ps1 (agent) : 1333/1333, 0 avertissement, 0 erreur ; DLL 0.757.0.0. Non validé en jeu.
### English
- Requests (X-11, X-12): in the tracking lists, each gathering zone folds; the zone you are in comes first (« you are here »), then the others from the cheapest teleport to the dearest, with its price read in the game from where you are; an unknown price goes after. Not validated in game.

## 0.756.0
- Plan global, Verify : Demande (X-10) : à l'ouverture d'un point de récolte, l'aide se place sur l'objet dont vos listes de suivi ont le plus besoin ; un clic en choisit toujours un autre. X-09 (alarmes des listes) validé en jeu sur votre retour. Non validé en jeu pour X-10.
- Tests : TrackedFirst756Tests. Verify.ps1 (agent) : 1331/1331, 0 avertissement, 0 erreur ; DLL 0.756.0.0. Non validé en jeu.
### English
- Request (X-10): when a node opens, the gathering help puts the item your tracking lists need most in context; a click still picks another. X-09 (list alarms) validated in game on the user's report. X-10 not validated in game.

## 0.755.0
- Plan global, Verify : Demande (X-09) : en tête des listes de suivi, « Ajouter les alarmes (N) » crée une alarme pour chaque ouverture des points à horaire de ce dont la liste a encore besoin ; une alarme déjà présente (même nom, même heure) n'est pas doublée. Non validé en jeu.
- Tests : ListAlarms755Tests. Verify.ps1 (agent) : 1330/1330, 0 avertissement, 0 erreur ; DLL 0.755.0.0. Non validé en jeu.
### English
- Request (X-09): at the top of the tracking lists, « Add the alarms (N) » creates an alarm for each opening of the timed points of what the list still needs; an alarm already there (same name, same time) is not doubled. Not validated in game.

## 0.754.0
- Plan global, Verify : Signalement (X-08) : l'infobulle d'un point à horaire sur la carte disait « fermé maintenant » sans compte à rebours. Elle dit maintenant dans combien de temps il ouvre, ou ferme s'il est ouvert, comme la page Récolte. Non validé en jeu.
- Tests : TimedCountdown754Tests. Verify.ps1 (agent) : 1329/1329, 0 avertissement, 0 erreur ; DLL 0.754.0.0. Non validé en jeu.
### English
- Report (X-08): the map tooltip of a timed point said « closed now » with no countdown. It now says how long until it opens, or closes when open, as the Gathering page does. Not validated in game.

## 0.753.0
- Plan global, Verify : Demande (X-06) : quand « Récolter automatiquement l'objet sélectionné » est coché dans la fenêtre du jeu, le plan de PR ne garde que ce qui s'utilise avant la première récolte : les effets durables, une action « pour une récolte » une fois au plus, aucun essai rendu ; l'aide le dit. Non validé en jeu.
- Tests : AutoGather753Tests. Verify.ps1 (agent) : 1328/1328, 0 avertissement, 0 erreur ; DLL 0.753.0.0. Non validé en jeu.
### English
- Request (X-06): when « Gather the selected item automatically » is ticked in the game's window, the GP plan keeps only what is used before the first gather: lasting effects, an action for one gather once at most, no attempt given back; the help says so. Not validated in game.

## 0.752.0
- Plan global, Verify : Demande (X-05) : dans les listes de suivi, les objets à récolter sont rassemblés par zone, celle qui en a le plus d'abord ; un objet présent dans plusieurs zones va avec celle qui en regroupe le plus, pour moins de trajets. Réglable sous « Récolte ». Non validé en jeu.
- Tests : ListZones752Tests. Verify.ps1 (agent) : 1326/1326, 0 avertissement, 0 erreur ; DLL 0.752.0.0. Non validé en jeu.
### English
- Request (X-05): in the tracking lists, items to gather are grouped by zone, the zone holding the most first; an item found in several zones goes with the one that holds most, for fewer trips. Optional, under « Gathering ». Not validated in game.

## 0.751.0
- Plan global, Verify : Demande (X-04) : au point de récolte, l'aide dit « Vos listes en demandent encore N » pour chaque objet dont la liste d'artisanat, l'atelier ou la préparation affichés ont besoin, et un cadre vert entoure sa ligne dans la fenêtre de récolte du jeu (réglable). Dessin seulement. Non validé en jeu.
- Tests : TrackedNeed751Tests. Verify.ps1 (agent) : 1324/1324, 0 avertissement, 0 erreur ; DLL 0.751.0.0. Non validé en jeu.
### English
- Request (X-04): at a node, the help says « Your lists still need N » for each item the crafting list, the workshop or the preparation shown need, and a green frame surrounds its row in the game's gathering window (optional). Drawn only. Not validated in game.

## 0.750.0
- Plan global, Verify : Cache (X-02) : durées par défaut plus longues, appliquées une fois aux réglages existants s'ils avaient gardé les anciennes : quêtes 5 min → 1 h, collections 24 h → 7 jours, sources communautaires 7 jours → seulement si absentes, calculs d'artisanat 24 h → 7 jours. Prix et Revue de mode inchangés. « Jamais » devient « Seulement si absent, ou sur Actualiser ». Un choix fait par vous reste. Non validé en jeu.
- Tests : CacheDefaults750Tests. Verify.ps1 (agent) : 1323/1323, 0 avertissement, 0 erreur ; DLL 0.750.0.0. Non validé en jeu.
### English
- Cache (X-02): longer default durations, applied once to existing settings that kept the old ones: quests 5 min → 1 h, collections 24 h → 7 days, community sources 7 days → only when missing, crafting calculations 24 h → 7 days. Prices and the Fashion Report unchanged. « Never » becomes « Only when missing, or on Refresh ». A choice you made stays. Not validated in game.

## 0.749.0
- Plan global, Verify : Lourdeur (X-01) : en mode développeur, le relevé des images lentes détaille la carte (menus, outils, carte elle-même), qui coûtait 195 ms à l'ouverture. Aucun changement visible. Non validé en jeu.
- Tests : SlowFrame745Tests. Verify.ps1 (agent) : 1321/1321, 0 avertissement, 0 erreur ; DLL 0.749.0.0. Non validé en jeu.
### English
- Heaviness (X-01): in developer mode, the slow-frame log details the map (menus, tools, the map itself), which cost 195 ms when it opened. No visible change. Not validated in game.

## 0.748.0
- Plan global, Verify : Lourdeur (X-01) : en mode développeur, le passage des pages repassait à chaque rechargement et donnait des images de 110 à 160 ms ; il passe au plus une fois toutes les deux heures, et sa part apparaît dans le relevé des images lentes. Inscrit au plan : X-08, compte à rebours d'un point à horaire fermé. Non validé en jeu.
- Tests : PageSweep724Tests. Verify.ps1 (agent) : 1321/1321, 0 avertissement, 0 erreur ; DLL 0.748.0.0. Non validé en jeu.
### English
- Heaviness (X-01): in developer mode, the page sweep ran at each reload and gave frames of 110 to 160 ms; it now runs at most once in two hours, and its share shows in the slow-frame log. Added to the plan: X-08, countdown of a closed timed point. Not validated in game.

## 0.747.0
- Plan global, Verify : Lourdeur (X-01) : le plan de PR d'un point de récolte et son recalcul de fin de nœud se font hors de l'image ; le journal avait mesuré 97 ms sur l'image où le point s'ouvre. « Calcul du plan… » s'affiche le temps du calcul. Le relevé des images lentes mesure aussi la fenêtre principale et les autres fenêtres. Non validé en jeu.
- Tests : SlowFrame745Tests. Verify.ps1 (agent) : 1320/1320, 0 avertissement, 0 erreur ; DLL 0.747.0.0. Non validé en jeu.
### English
- Heaviness (X-01): a node's GP plan and its end-of-node replan are worked out off the frame; the log had measured 97 ms on the frame the node opened. « Working out the plan… » shows meanwhile. The slow-frame log also measures the main window and the other windows. Not validated in game.

## 0.746.0
- Plan global, Verify : Plan : les demandes faites en jouant le 2026-11-07 sont inscrites dans le plan global (X-01 à X-07), avec leur état, pour n'en oublier aucune. Aucun changement du plugin.
- Tests : SlowFrame745Tests. Verify.ps1 (agent) : 1320/1320, 0 avertissement, 0 erreur ; DLL 0.746.0.0. Non validé en jeu.
### English
- Plan: the requests made while playing on 2026-11-07 are written in the global plan (X-01 to X-07), with their state, so that none is forgotten. No change to the plugin.

## 0.745.0
- Plan global, Verify : Signalement : Tensho paraît lourd. Le journal montre des images de 120 à 230 ms toutes les quelques minutes, et une de 2,6 s, sans dire quelle partie. En mode développeur, chaque image de plus de 100 ms est maintenant écrite au journal avec ses parties les plus coûteuses (panneau, page) et les pauses du moteur .NET. Aucun changement visible. Non validé en jeu.
- Tests : SlowFrame745Tests. Verify.ps1 (agent) : 1320/1320, 0 avertissement, 0 erreur ; DLL 0.745.0.0. Non validé en jeu.
### English
- Report: Tensho feels heavy. The log shows frames of 120 to 230 ms every few minutes, and one of 2.6 s, without saying which part. In developer mode, every frame above 100 ms is now written to the log with its costliest parts (panel, page) and the .NET runtime's pauses. No visible change. Not validated in game.

## 0.744.0
- Plan global, Verify : Clignotement de la liste de suivi, cause trouvée par le relevé de 0.743 : la lecture affichée des sacs était bonne, mais le calcul des stocks des listes lisait encore la lecture stricte, vide pendant 250 ms. La correction de 0.741 était tombée dans une autre fonction qui commençait par la même ligne. Corrigé, et le test vérifie cette fonction précise. Non validé en jeu.
- Tests : ListBlink741Tests. Verify.ps1 (agent) : 1318/1318, 0 avertissement, 0 erreur ; DLL 0.744.0.0. Non validé en jeu.
### English
- Tracking list blink, cause found by the 0.743 logging: the shown bag reading was fine, but the stock counts of the lists still read the strict one, empty for 250 ms. The 0.741 fix had landed on another function beginning with the same line. Fixed, and the test checks that very function. Not validated in game.

## 0.743.0
- Plan global, Verify : Le clignotement revient en 0.742 : la cause visée n'était pas la bonne. En mode développeur, le journal écrit maintenant ce que chaque rangement donnait au moment où un stock tombe à 0. Il note aussi les clics sur « Ouvrir dans Tensho » d'une alarme, signalé comme n'ouvrant rien. Aucun changement visible. Non validé en jeu.
- Tests : ListBlink740Tests. Verify.ps1 (agent) : 1318/1318, 0 avertissement, 0 erreur ; DLL 0.743.0.0. Non validé en jeu.
### English
- The blink is back in 0.742: the cause aimed at was not the right one. In developer mode, the log now writes what each storage said when a stock falls to 0. It also logs clicks on an alarm's « Open in Tensho », reported as opening nothing. No visible change. Not validated in game.

## 0.742.0
- Plan global, Verify : Suite du clignotement : il revenait en 0.741. Le journal montre qu'une lecture faite pendant le changement trouvait les sacs illisibles et était affichée. Correction : à l'écran, un sac illisible garde sa dernière lecture complète du même personnage ; le tri voit toujours ce qui a été lu. Non validé en jeu.
- Tests : ListBlink742Tests. Verify.ps1 (agent) : 1318/1318, 0 avertissement, 0 erreur ; DLL 0.742.0.0. Non validé en jeu.
### English
- Blink, continued: it came back in 0.741. The log shows that a reading taken during the change found the bags unreadable and was shown. Fix: on screen, an unreadable bag keeps its last complete reading of the same character; the sort still sees what was read. Not validated in game.

## 0.741.0
- Plan global, Verify : Signalement : la liste de suivi clignotait pendant la récolte. Cause, relevée dans le journal : à chaque objet récolté, la lecture des sacs était effacée jusqu'à la suivante (jusqu'à 250 ms), et tous les stocks tombaient à 0. Correction : les stocks affichés gardent la dernière lecture du même personnage ; le tri des sacs, qui agit, attend toujours une lecture sûre. Non validé en jeu.
- Tests : ListBlink741Tests. Verify.ps1 (agent) : 1316/1316, 0 avertissement, 0 erreur ; DLL 0.741.0.0. Non validé en jeu.
### English
- Report: the tracking list blinked while gathering. Cause, found in the log: at each item gathered, the bag reading was cleared until the next one (up to 250 ms), and every stock fell to 0. Fix: stock counts on screen keep the last reading of the same character; the bag sort, which acts, still waits for a sure reading. Not validated in game.

## 0.740.0
- Plan global, Verify : Signalement : la liste de suivi clignote entre deux valeurs pendant la récolte. Cause non trouvée en lisant le code : en mode développeur, le journal note maintenant la ligne qui revient à sa valeur d'avant, et si c'est le stock ou le besoin. Aucun changement visible. Non validé en jeu.
- Tests : ListBlink740Tests. Verify.ps1 (agent) : 1315/1315, 0 avertissement, 0 erreur ; DLL 0.740.0.0. Non validé en jeu.
### English
- Report: the tracking list blinks between two values while gathering. Cause not found by reading the code: in developer mode, the log now notes the line that goes back to its previous value, and whether the stock or the need moved. No visible change. Not validated in game.

## 0.739.0
- Plan global, Verify : Demande : « Que puis-je fabriquer ? » montre une ligne par objet, avec une icône par métier qui peut le fabriquer ; le nom du métier, son niveau et les fabrications en infobulle, un clic simule cette recette. Les ventes d'un objet ne sont plus comptées deux fois. Non validé en jeu.
- Tests : CraftLines739Tests. Verify.ps1 (agent) : 1314/1314, 0 avertissement, 0 erreur ; DLL 0.739.0.0. Non validé en jeu.
### English
- Request: « What can I craft? » shows one line per item, with an icon per job that can make it; the job's name, level and crafts on hover, a click simulates that recipe. An item's sales are no longer counted twice. Not validated in game.

## 0.738.0
- Plan global, Verify : Décision DEC-040 : le passage des pages reste automatique, en mode développeur seulement, avec les requêtes que les pages font à leur ouverture (choix de l'utilisateur). Aucun changement du plugin.
- Tests : PageSweep724Tests. Verify.ps1 (agent) : 1312/1312, 0 avertissement, 0 erreur ; DLL 0.738.0.0. Non validé en jeu.
### English
- Decision DEC-040: the page sweep stays automatic, in developer mode only, with the requests pages make when opened (the user's choice). No change to the plugin.

## 0.737.0
- Plan global, Verify : Signalement : le menu du clic droit sur l'icône d'un matériau de projet se fermait parfois tout de suite. Cause : son identifiant comptait toutes les icônes de l'image, dans toutes les fenêtres ; une icône de plus ou de moins ailleurs le changeait et ImGui fermait le menu. Correction : il ne compte plus que le même objet au même endroit. Vaut pour toutes les icônes d'objets. Non validé en jeu.
- Tests : ItemMenu737Tests. Verify.ps1 (agent) : 1312/1312, 0 avertissement, 0 erreur ; DLL 0.737.0.0. Non validé en jeu.
### English
- Report: the right-click menu on a project material's icon sometimes closed at once. Cause: its id counted every icon of the frame, in every window; one icon more or less elsewhere changed it and ImGui closed the menu. Fix: it counts only the same item at the same place. Applies to every item icon. Not validated in game.

## 0.736.0
- Plan global, Verify : Journal : l'entrée de 0.735 reprenait le texte de 0.734, la publication ayant gardé l'ancienne note ; elle est réécrite. Le script de l'agent efface maintenant la note après chaque publication. Aucun changement du plugin.
- Tests : RecipeStock735Tests. Verify.ps1 (agent) : 1311/1311, 0 avertissement, 0 erreur ; DLL 0.736.0.0. Non validé en jeu.
### English
- Changelog: the entry of 0.735 repeated the text of 0.734, as the release kept the previous note; it is rewritten. The agent's script now deletes the note after each release. No change to the plugin.

## 0.735.0
- Plan global, Verify : Signalement : les cristaux marqués « partiel » dans les ingrédients d'une recette, alors que vous en aviez assez. Cause : les cristaux des servants ne sont jamais lus, donc le total est un minimum. Correction : un minimum qui couvre déjà le besoin dit « OK, au moins autant » ; sinon la ligne dit que des rangements ne sont pas lus. (Entrée corrigée en 0.736 : la publication avait repris le texte de 0.734.)
- Tests : RecipeStock735Tests. Verify.ps1 (agent) : 1311/1311, 0 avertissement, 0 erreur ; DLL 0.735.0.0. Non validé en jeu.
### English
- Report: crystals marked « partial » among a recipe's ingredients although there were enough. Cause: retainers' crystals are never read, so the total is a lower bound. Fix: a lower bound that already covers the need says « OK, at least this much »; otherwise the line says some storage is not read. Not validated in game. (Entry corrected in 0.736: the release had copied the text of 0.734.)

## 0.734.0
- Plan global, Verify : Signalement : « Cette action n'est pas sur vos barres » pour Récolte abondante II, pourtant présente. Cause : la case garde Récolte abondante, que le jeu fait évoluer en II à l'affichage. Correction : la case est comparée à l'action qu'elle affiche. Non validé en jeu.
- Tests : GatherNext728Tests. Verify.ps1 (agent) : 1310/1310, 0 avertissement, 0 erreur ; DLL 0.734.0.0. Non validé en jeu.
### English
- Report: « This action is not on your hotbars » for Bountiful Harvest II, which was there. Cause: the slot keeps Bountiful Harvest, which the game shows as II. Fix: the slot is matched by the action it shows. Not validated in game.

## 0.733.0
- Plan global, Verify : Signalements : aucun cadre sur les barres, et « Sagesse du fermier » demandée à 4/4 essais. Causes : la demande de repérage était effacée avant d'être lue, le panneau étant dessiné après ; cette action rend un essai et le jeu la refuse tant qu'aucun n'est dépensé. Corrections : le panneau demande lui-même le repérage ; l'action vient après la première récolte. Non validé en jeu.
- Tests : GatherNext728Tests. Verify.ps1 (agent) : 1310/1310, 0 avertissement, 0 erreur ; DLL 0.733.0.0. Non validé en jeu.
### English
- Reports: no frame on the hotbars, and Ageless Words asked at 4/4 attempts. Causes: the request to find the slot was cleared before it was read, as the panel is drawn later; that action gives back an attempt and the game refuses it while none is spent. Fixes: the panel asks itself; the action comes after the first gather. Not validated in game.

## 0.732.0
- Plan global, Verify : Page Récolte : la liste « En ce moment » se calcule hors de l'image ; le passage des pages y avait mesuré une image de 41 ms à chaque minute d'Eorzéa. Non validé en jeu.
- Tests : PageSweep724Tests. Verify.ps1 (agent) : 1310/1310, 0 avertissement, 0 erreur ; DLL 0.732.0.0. Non validé en jeu.
### English
- Gathering page: the « Now and soon » list is worked out off the frame; the page sweep had measured a 41 ms frame there every Eorzea minute. Not validated in game.

## 0.731.0
- Plan global, Verify : Signalement : à la fin du plan il restait 53 PR et 90 % de supplément, sans proposer Sols fertiles I. Cause : chaque récolte rend 6 PR (relevé : +6 après une, +30 après cinq), que le plan d'ouverture ne compte pas. Correction : les étapes en tiennent compte, et une fois le plan fini, le calcul est refait avec les PR restants et les taux du moment. Non validé en jeu.
- Tests : GatherNext728Tests. Verify.ps1 (agent) : 1310/1310, 0 avertissement, 0 erreur ; DLL 0.731.0.0. Non validé en jeu.
### English
- Report: at the end of the plan 53 GP and 90 % boon were left, and Pioneer's Gift I was not offered. Cause: each gather gives back 6 GP (+6 after one, +30 after five), which the plan made at the opening does not count. Fix: the steps count it, and once the plan is done, it is worked out again with the GP left and the current rates. Not validated in game.

## 0.730.0
- Plan global, Verify : Commandes spéciales : onglet Récompenses, par client : points, gils et assignats à chaque rang et palier, bonus de la semaine, objets liés à chaque rang. Codes des monnaies reliés aux assignats, montants comparés au wiki. Non validé en jeu.
- Tests : DeliveryRewards730Tests. Verify.ps1 (agent) : 1309/1309, 0 avertissement, 0 erreur ; DLL 0.730.0.0. Non validé en jeu.
### English
- Custom deliveries: Rewards tab, per client: points, gil and scrips at each rank and tier, weekly bonus, objects attached to each rank. Currency codes tied to the scrips, amounts compared with the wiki. Not validated in game.

## 0.729.0
- Plan global, Verify : Aide à la récolte : une ligne sépare la prochaine action du plan, à la demande de l'utilisateur. Non validé en jeu.
- Tests : GatherNext728Tests. Verify.ps1 (agent) : 1306/1306, 0 avertissement, 0 erreur ; DLL 0.729.0.0. Non validé en jeu.
### English
- Gathering help: a line now separates the next action from the plan, as the user asked. Not validated in game.

## 0.728.0
- Plan global, Verify : Aide à la récolte : la ligne « Ensuite » donne la prochaine action du plan ou dit de récolter, d'après les PR dépensés et les essais restants ; le plan montre ce qui est fait. Un cadre repère l'action sur vos barres (réglable, barres croisées non prises en charge). Tensho n'exécute rien, aucun hook. Non validé en jeu.
- Tests : GatherNext728Tests. Verify.ps1 (agent) : 1306/1306, 0 avertissement, 0 erreur ; DLL 0.728.0.0. Non validé en jeu.
### English
- Gathering help: the « Next » line gives the plan's next action or says to gather, from the GP spent and the attempts left; the plan shows what is done. A frame marks the action on your hotbars (optional, cross hotbars not covered). Tensho uses nothing, no hook. Not validated in game.

## 0.727.0
- Plan global, Verify : Signalement : « Suivre toutes ses parties » sur la coque Bronco ajoutait aussi une base de tours et un mur de maisonnette. Cause : tout ce qui n'a pas de prototype et l'ordre 1 formait un même groupe. Correction : la catégorie compte aussi. Les aéronefs de type II, oubliés, ont maintenant leurs quatre parties. Les deux projets déjà ajoutés restent à retirer avec leur croix. Non validé en jeu.
- Tests : WorkshopSets694Tests. Verify.ps1 (agent) : 1301/1301, 0 avertissement, 0 erreur ; DLL 0.727.0.0. Non validé en jeu.
### English
- Report: « Follow all its parts » on the Bronco hull also added a tower base and a housing wall. Cause: everything with no prototype and order 1 formed one group. Fix: the category counts too. The second airship class, left out, now gets its four parts. The two projects already added stay until removed with their cross. Not validated in game.

## 0.726.0
- Plan global, Verify : Correction de 0.724 : hors de l'écran, ImGui sautait le contenu des sous-fenêtres, donc le résultat « 49 pages sans erreur » prouvait peu. Le passage dessine maintenant dans une fenêtre masquée, à l'écran : le contenu s'exécute, rien n'est affiché, et il s'arrête si elle n'est pas masquée.
- Tests : PageSweep724Tests. Verify.ps1 (agent) : 1300/1300, 0 avertissement, 0 erreur ; DLL 0.726.0.0. Non validé en jeu.
### English
- Fix of 0.724: outside the screen, ImGui skipped the content of child windows, so « 49 pages with no error » proved little. The sweep now draws in a hidden window on the screen: the content runs, nothing shows, and it stops if the window is not hidden.

## 0.725.0
- Plan global, Verify : Signalement : « ventes par jour » restait à « ? ». Cause : la réponse agrégée d'Universalis n'en donne pas sans vente récente. Correction : la vitesse sur sept jours vient de la réponse de l'objet. Demande : le tri par marché de « Que puis-je fabriquer ? » ne compte plus que ce qui peut se vendre dans les jours choisis (7 par défaut, réglable) ; une vieille vente HQ isolée ne fixe plus le prix quand seul le NQ se vend. Prouvé contre les réponses réelles d'Universalis ; non validé en jeu.
- Tests : SalesSpeed725Tests. Verify.ps1 (agent) : 1300/1300, 0 avertissement, 0 erreur ; DLL 0.725.0.0. Non validé en jeu.
### English
- Report: « sales a day » stayed at « ? ». Cause: Universalis's aggregated answer leaves it out without a recent sale. Fix: the seven-day speed comes from the item's own answer. Request: the market sort of « What can I craft? » now counts only what can sell in the chosen days (7 by default, adjustable); one old HQ sale no longer sets the price when only NQ sells. Checked against real Universalis answers; not validated in game.

## 0.724.0
- Plan global, Verify : Auto-contrôle : en mode développeur, après l'auto-contrôle, chaque page est dessinée hors de l'écran, sans clic possible, et le journal donne son coût et ses erreurs. Il attend pendant les combats, contenus, synthèses, récoltes, pêche et cinématiques. La page Marché reste à part : elle interroge Universalis.
- Tests : PageSweep724Tests. Verify.ps1 (agent) : 1295/1295, 0 avertissement, 0 erreur ; DLL 0.724.0.0. Non validé en jeu.
### English
- Self-check: in developer mode, after the self-check, every page is drawn off-screen with no input possible, and the log gives its cost and any error. It waits during combat, duties, crafting, gathering, fishing and cutscenes. The market page is left out: it asks Universalis.

## 0.723.0
- Plan global, Verify : Preuves corrigées d'après la première lecture dans le jeu : 50 rapports du front et non 55, 1 123 ALEA nommés, 47 et 41 lieux sur les fronts de Bozja. Aucun changement de code du plugin. COMPILED et TESTED ; lectures de 0.722 exécutées dans le jeu, affichage non validé.
- Tests : FieldContent717Tests. Verify.ps1 (agent) : 1292/1292, 0 avertissement, 0 erreur ; DLL 0.723.0.0. Non validé en jeu.
### English
- Evidence corrected from the first read inside the game: 50 field records rather than 55, 1,123 named FATEs, 47 and 41 places on the Bozjan fronts. No change to the plugin's code. COMPILED and TESTED; the reads of 0.722 ran in the game, display not validated.

## 0.722.0
- Plan global, Verify : Auto-contrôle : les pages codées sans le jeu sont maintenant lues dans le jeu au rechargement (mode développeur ou sur demande) et leurs comptes vont au journal.
- Tests : FieldContent717Tests. Verify.ps1 (agent) : 1292/1292, 0 avertissement, 0 erreur ; DLL 0.722.0.0. Non validé en jeu.
### English
- Self-check: pages coded without the game are now read in the game at reload (developer mode or on request), with their counts in the log.

## 0.721.0
- Plan global, W-14b : Trouvé en faisant Eureka et Bozja : les fichiers de zone du jeu donnent le lieu de chaque ALEA. Nouvelle page Monde › ALEA : 47 zones ouvertes et 1 127 ALEA avec niveau et position, recherche par nom dans toutes les zones, clic pour la carte, clic droit pour le drapeau et l'éthérite. Le livre du zodiaque a un bouton « Où ? » sur ses ALEA. COMPILED et TESTED, non validé en jeu.
- Tests : FieldContent717Tests. Verify.ps1 (agent) : 1291/1291, 0 avertissement, 0 erreur ; DLL 0.721.0.0. Non validé en jeu.
### English
- Found while doing Eureka and Bozja: the game's zone files give each FATE's place. New page World > FATEs: 47 open zones and 1,127 FATEs with level and position, search by name in every zone, click for the map, right click for the flag and aetheryte. The Zodiac book has a Where? button on its FATEs. COMPILED and TESTED, not validated in game.

## 0.720.0
- Plan global, W-13 : Fin des reliques suivantes, sujet choisi par l'utilisateur. La page s'appelle maintenant Reliques ; Eurêka, Résistance et fantômes ont deux onglets, Armes et Armures : pour le groupe de jobs choisi, chaque étape avec ses pièces au survol et le nombre en stock. COMPILED et TESTED, non validé en jeu.
- Tests : RelicWeapons708Tests. Verify.ps1 (agent) : 1290/1290, 0 avertissement, 0 erreur ; DLL 0.720.0.0. Non validé en jeu.
### English
- End of the later relics, a topic chosen by the user. The page is now called Relics; Eureka, Resistance and Phantom have two tabs, Weapons and Armour: for the chosen job group, each stage with its pieces on hover and how many are in stock. COMPILED and TESTED, not validated in game.

## 0.719.0
- Plan global, W-16 : Lexique, sujet choisi par l'utilisateur. Nouvelle page Objets › Lexique : chercher un mot du jeu en français ou en anglais, sans tenir compte des accents, dans seize types de feuilles (objets, actions, lieux, monstres, PNJ, quêtes, interface…), résultats par pages, clic pour copier. Chargement et recherche hors de l'image. COMPILED et TESTED, non validé en jeu.
- Tests : Lexicon719Tests. Verify.ps1 (agent) : 1288/1288, 0 avertissement, 0 erreur ; DLL 0.719.0.0. Non validé en jeu.
### English
- Lexicon, a topic chosen by the user. New page Objects > Lexicon: search a word of the game in French or English, ignoring accents, across sixteen kinds of sheets (items, actions, places, monsters, NPCs, quests, interface…), results in pages, click to copy. Loaded and searched off the frame. COMPILED and TESTED, not validated in game.

## 0.718.0
- Plan global, W-15 : Donjons profonds, sujet choisi par l'utilisateur. Nouvelle page Progression › Donjons profonds, un onglet par donjon : étages avec niveau, état et quête de déblocage ; poteries, pierres et objets particuliers avec le texte du jeu, le mot « sous-sol », « étage » ou « zone » rempli selon le donjon ; coffres d'après LuminaSupplemental, sous son réglage. Pas de conseil de combat, et Tensho reste en pause à l'intérieur. Corrigé aussi : deux noms de la liste des sources suivent maintenant le jeu (Eurêka Orthos, Sanctuaire des pèlerins). COMPILED et TESTED, non validé en jeu.
- Tests : DeepDungeons718Tests. Verify.ps1 (agent) : 1285/1285, 0 avertissement, 0 erreur ; DLL 0.718.0.0. Non validé en jeu.
### English
- Deep dungeons, a topic chosen by the user. New page Progression > Deep dungeons, a tab per dungeon: floors with level, state and unlock quest; pomanders, stones and special items with the game's text, its floor word filled in per dungeon; coffers from LuminaSupplemental, under its switch. No fight advice, and Tensho still pauses inside. Also fixed: two source names now follow the game. COMPILED and TESTED, not validated in game.

## 0.717.0
- Plan global, W-14 : Eureka et Bozja, sujet choisi par l'utilisateur. Nouvelle page Progression › Eureka et Bozja : par zone, les ALEA, escarmouches et affrontements cruciaux avec leur niveau et leur position, lues dans les fichiers de zone du jeu ; clic pour la carte, clic droit pour le drapeau et l'éthérite quand il y en a une ; rapports du front bozjien et actions oubliées avec les textes du jeu. Corrigé aussi : l'étape « Augmented » des armes de la Résistance prenait des armes fabriquées (Augmented Crystarium). Ce que le personnage possède n'est pas lu. COMPILED et TESTED, non validé en jeu.
- Tests : FieldContent717Tests. Verify.ps1 (agent) : 1281/1281, 0 avertissement, 0 erreur ; DLL 0.717.0.0. Non validé en jeu.
### English
- Eureka and Bozja, a topic chosen by the user. New page Progression > Eureka and Bozja: for each zone, FATEs, skirmishes and critical engagements with level and position, read from the game's zone layouts; click for the map, right click for the flag and the aetheryte where there is one; field records and lost actions with the game's texts. Also fixed: the Resistance weapons' Augmented stage took crafted weapons (Augmented Crystarium). What the character holds is not read. COMPILED and TESTED, not validated in game.

## 0.716.0
- Plan global, W-13 : Reliques suivantes, sujet choisi par l'utilisateur. La page Armes reliques a maintenant un onglet par lignée : Zodiaque, Anima, Eurêka, Résistance, Manderville, Fantômes, avec les étapes par arme et ce que vos stocks détiennent. Les objets viennent des feuilles du jeu ; seuls les noms d'étapes et leurs niveaux ont été relevés sur le wiki, puis comptés dans le jeu. Les armures ne sont pas encore listées. COMPILED et TESTED, non validé en jeu.
- Tests : RelicWeapons708Tests. Verify.ps1 (agent) : 1278/1278, 0 avertissement, 0 erreur ; DLL 0.716.0.0. Non validé en jeu.
### English
- Later relics, a topic chosen by the user. The Relic weapons page now has a tab per line: Zodiac, Anima, Eureka, Resistance, Manderville, Phantom, with stages per weapon and what your stocks hold. Items come from the game's sheets; only stage names and levels were read on the wiki, then counted in the game. Armour is not listed yet. COMPILED and TESTED, not validated in game.

## 0.715.0
- Plan global, W-12 : Livraisons spéciales, sujet choisi par l'utilisateur. Nouvelle page Progression › Livraisons spéciales : les quêtes de livraison des artisans et récolteurs (Cristarium, académie de Sharlayan, bazar aux Mille Perles), faites ou à faire, avec niveau, lieu et clic droit vers le départ et l'éthérite. COMPILED et TESTED, non validé en jeu.
- Tests : SpecialDeliveries715Tests. Verify.ps1 (agent) : 1276/1276, 0 avertissement, 0 erreur ; DLL 0.715.0.0. Non validé en jeu.
### English
- Special deliveries, a topic chosen by the user. New page Progression > Special deliveries: the delivery quest lines of crafters and gatherers (Crystarium, Studium, Wachumeqimeqi), done or left, with level, place and a right click to the start and the aetheryte. COMPILED and TESTED, not validated in game.

## 0.714.0
- Plan global, W-11 : Grandes compagnies, sujet choisi par l'utilisateur. Nouvelle page Progression › Grandes compagnies : grade et sceaux dans chacune, prochaine promotion avec sa quête, et un onglet de ce que les sceaux achètent, avec filtre par grade et recherche. Les missions du jour ne sont pas lues : le jeu ne les donne que dans sa fenêtre. COMPILED et TESTED, non validé en jeu.
- Tests : GrandCompanies714Tests. Verify.ps1 (agent) : 1275/1275, 0 avertissement, 0 erreur ; DLL 0.714.0.0. Non validé en jeu.
### English
- Grand Companies, a topic chosen by the user. New page Progression > Grand Companies: grade and seals in each, next promotion with its quest, and a tab of what seals buy, with a grade filter and search. Today's supply missions are not read: the game gives them only in its window. COMPILED and TESTED, not validated in game.

## 0.713.0
- Plan global, W-10 : Peuples alliés, sujet choisi par l'utilisateur. Nouvelle page Progression › Peuples alliés : pour chaque peuple, son extension, son lieu (clic droit vers l'éthérite), votre rang, votre réputation et la monnaie détenue, avec les quêtes restantes du jour. Lecture seule, sans requête au serveur. Le sujet rejoint le plan avec six autres (W-11 à W-16). COMPILED et TESTED, non validé en jeu.
- Tests : AlliedSocieties713Tests. Verify.ps1 (agent) : 1271/1271, 0 avertissement, 0 erreur ; DLL 0.713.0.0. Non validé en jeu.
### English
- Allied societies, a topic chosen by the user. New page Progression > Allied societies: each society's expansion, place (right click to the aetheryte), your rank, reputation and currency held, with today's quests left. Read only, no server request. It joins the plan with six others (W-11 to W-16). COMPILED and TESTED, not validated in game.

## 0.712.0
- Plan global, W-06 : Rapport de l'utilisateur : le tri par assignats n'affichait aucun assignat. Les récompenses se chargent à la première demande, et la liste n'était pas recalculée à leur arrivée. Elle l'est maintenant, et la page dit quand elles se chargent. COMPILED et TESTED, non validé en jeu.
- Tests : MarketPace710Tests. Verify.ps1 (agent) : 1268/1268, 0 avertissement, 0 erreur ; DLL 0.712.0.0. Non validé en jeu.
### English
- User report: the scrip sort showed no scrips. The rewards load on first asking and the list was not worked out again when they arrived. It now is, and the page says when they are loading. COMPILED and TESTED, not validated in game.

## 0.711.0
- Plan global, Marché : Rapport de l'utilisateur : la rentabilité comptait le prix demandé, or un objet affiché à 2 000 000 de gils ne se vend pas forcément. « Que puis-je fabriquer ? » et « Que faire avec ? » utilisent maintenant ce qui s'est réellement vendu sur votre monde : le plus bas entre la dernière vente et la vente moyenne, avec le nombre de ventes par jour. Sans vente connue, l'objet passe en fin de liste. COMPILED et TESTED, non validé en jeu.
- Tests : MarketPace710Tests. Verify.ps1 (agent) : 1267/1267, 0 avertissement, 0 erreur ; DLL 0.711.0.0. Non validé en jeu.
### English
- User report: profitability counted the asking price, and an item listed at 2,000,000 gil does not always sell. What can I craft? and What to do with it? now use what really sold on your world: the lower of the last sale and the average sale, with sales per day. With no known sale, the item goes last. COMPILED and TESTED, not validated in game.

## 0.710.0
- Plan global, Marché : Demande de l'utilisateur : les grosses demandes de prix vont plus vite sans gêner Universalis. Sur « Que faire avec ? » et « Que puis-je fabriquer ? » : une requête à la fois, 100 objets, une par seconde, au lieu de requêtes objet par objet toutes les 3 secondes. Un refus du site (429, 503) arrête la liste et impose au moins 30 secondes de pause. La mémoire garde maintenant 4096 prix, car 128 faisait perdre le début d'une longue liste. COMPILED et TESTED, non validé en jeu.
- Tests : MarketPace710Tests. Verify.ps1 (agent) : 1265/1265, 0 avertissement, 0 erreur ; DLL 0.710.0.0. Non validé en jeu.
### English
- User request: large price requests go faster without troubling Universalis. On What to do with it? and What can I craft?: one request at a time, 100 items, one a second, instead of single requests every 3 seconds. A refusal (429, 503) stops the list and holds at least 30 seconds. The memory now keeps 4096 prices: 128 lost the start of a long list. COMPILED and TESTED, not validated in game.

## 0.709.0
- Plan global, W-06 : Demande de l'utilisateur : « Que puis-je fabriquer ? » s'affiche par pages de 25, avec une recherche par nom dans toute la liste. En tri marché, les prix viennent page par page et chaque page est classée seule ; « Charger tous les prix » les demande tous, et toute la liste est alors classée. COMPILED et TESTED, non validé en jeu.
- Tests : CraftChained696Tests. Verify.ps1 (agent) : 1261/1261, 0 avertissement, 0 erreur ; DLL 0.709.0.0. Non validé en jeu.
### English
- User request: What can I craft? shows pages of 25, with a name search across the whole list. On the market sort, prices come page by page and each page is ranked on its own; Load all prices asks for them all, and the whole list is then ranked. COMPILED and TESTED, not validated in game.

## 0.708.0
- Plan global, W-03 : Armes reliques, première partie. Nouvelle page Progression › Armes reliques : les armes du zodiaque par étape, ce que vos stocks en détiennent, et le livre en cours avec ses monstres, donjons, aléas et mandats. Lecture seule, sans requête au serveur. Les extensions suivantes ne sont pas encore couvertes. COMPILED et TESTED, non validé en jeu.
- Tests : RelicWeapons708Tests. Verify.ps1 (agent) : 1259/1259, 0 avertissement, 0 erreur ; DLL 0.708.0.0. Non validé en jeu.
### English
- Relic weapons, first part. New page Progression > Relic weapons: Zodiac weapons by stage, what your stocks hold, and the book in progress with its monsters, dungeons, FATEs and leves. Read only, no server request. Later expansions are not covered yet. COMPILED and TESTED, not validated in game.

## 0.707.0
- Plan global, W-06 : Demande de l'utilisateur : « Que puis-je fabriquer ? » se trie par niveau, gils au marché, vente à un PNJ ou assignats, avec la valeur sur chaque ligne. Marché : prix minimum de votre monde (Universalis), HQ si l'objet peut l'être, demandés au choix de ce tri, avec une case pour arrêter. Assignats : palier haut, chaque sorte à part. Les matériaux ne sont pas déduits, ils sont déjà en stock. COMPILED et TESTED, non validé en jeu.
- Tests : CraftChained696Tests. Verify.ps1 (agent) : 1255/1255, 0 avertissement, 0 erreur ; DLL 0.707.0.0. Non validé en jeu.
### English
- User request: What can I craft? sorts by level, market gil, NPC sale or scrips, with the value on each line. Market: your world's lowest price (Universalis), HQ when the item can be, asked when this sort is chosen, with a box to stop. Scrips: high tier, each kind apart. Materials are not deducted, as they are already in stock. COMPILED and TESTED, not validated in game.

## 0.706.0
- Plan global, Éthérites : Rapport de l'utilisateur : la Félicité insulaire n'a pas de téléportation. L'entrée « Éthérite la plus proche » disparaît sur cette carte et sur toute carte sans éthérite, dans les menus et sur la carte Tensho. COMPILED et TESTED, non validé en jeu.
- Tests : PlaceMenus704Tests. Verify.ps1 (agent) : 1254/1254, 0 avertissement, 0 erreur ; DLL 0.706.0.0. Non validé en jeu.
### English
- User report: the Island Sanctuary has no teleport. The nearest aetheryte entry goes away on that map and on any map with no aetheryte, in menus and on the Tensho map. COMPILED and TESTED, not validated in game.

## 0.705.0
- Plan global, Infobulles : Demande de l'utilisateur : les vendeurs PNJ des infobulles ont leurs réglages, sous Comment l'obtenir : les afficher ou non, exclure les prérequis non remplis, ne garder que certaines monnaies. Par défaut, tout reste affiché comme avant. La fiche de l'objet et l'outil des vendeurs ne changent pas. COMPILED et TESTED, non validé en jeu.
- Tests : PlaceMenus704Tests. Verify.ps1 (agent) : 1253/1253, 0 avertissement, 0 erreur ; DLL 0.705.0.0. Non validé en jeu.
### English
- User request: the NPC vendors of tooltips have their settings, under How to get it: show them or not, leave out unmet requirements, keep only some currencies. By default everything shows as before. The item sheet and vendor tool do not change. COMPILED and TESTED, not validated in game.

## 0.704.0
- Plan global, Éthérites : Demande de l'utilisateur : partout où des coordonnées s'affichent, le clic droit propose l'éthérite la plus proche, avec drapeau et question de téléportation habituelle. Ajouté aux lieux de monstres du wiki et de Garland, sources de magie bleue, liens du chat, train de chasse, animaux de l'île, favoris, listes de la carte Tensho, recherche sur les cartes, page Cartes, fiches de vendeurs, sources d'objets, suivi, carnet d'exploration et récolte. Un test exige ce menu sur toute page qui place un drapeau. COMPILED et TESTED, non validé en jeu.
- Tests : PlaceMenus704Tests. Verify.ps1 (agent) : 1252/1252, 0 avertissement, 0 erreur ; DLL 0.704.0.0. Non validé en jeu.
### English
- User request: wherever coordinates show, a right click offers the nearest aetheryte, with the flag and the usual teleport question. Added to wiki and Garland monster places, blue magic sources, chat links, hunt train, island animals, bookmarks, the Tensho map's lists, map search, the Maps page, vendor sheets, item sources, tracker, sightseeing log and gathering. A test requires this menu on any page placing a flag. COMPILED and TESTED, not validated in game.

## 0.703.0
- Plan global, W-09 : Demande de l'utilisateur : option « Exclure les PNJ aux prérequis non remplis » dans les panneaux : un article dont la quête n'est pas faite ne compte plus. Un prérequis non lu compte comme rempli : les hauts faits ne sont retenus que s'ils sont confirmés, et le rang de compagnie n'est pas vérifié. Désactivée par défaut. COMPILED et TESTED, non validé en jeu.
- Tests : BaseMaterials699Tests. Verify.ps1 (agent) : 1249/1249, 0 avertissement, 0 erreur ; DLL 0.703.0.0. Non validé en jeu.
### English
- User request: Leave out NPCs with unmet requirements, in the panels: an article whose quest is not done no longer counts. A requirement not read counts as met: achievements only when confirmed, and company rank is not checked. Off by default. COMPILED and TESTED, not validated in game.

## 0.702.0
- Plan global, W-09 : Demande de l'utilisateur : la liste des monnaies ne garde que celles des PNJ qui vendent vraiment des matériaux, c'est-à-dire un ingrédient d'une recette ou une fourniture d'un projet d'atelier. Une monnaie déjà cochée reste listée pour pouvoir être décochée. COMPILED et TESTED, non validé en jeu.
- Tests : BaseMaterials699Tests. Verify.ps1 (agent) : 1248/1248, 0 avertissement, 0 erreur ; DLL 0.702.0.0. Non validé en jeu.
### English
- User request: the currency list keeps only those of NPCs really selling materials: a recipe ingredient or a workshop project supply. A currency already ticked stays listed so it can be unticked. COMPILED and TESTED, not validated in game.

## 0.701.0
- Plan global, W-09 : Rapport et demande de l'utilisateur : la liste des monnaies montrait des numéros. #0 est une monnaie que le jeu code et que rien ne résout, #8223 une ligne sans nom dans la feuille du jeu : ils sortent de la liste et leurs offres ne comptent pas. #4294905804 est le code interne de Tensho pour les crédits de compagnie, maintenant nommés. La liste part tout décochée, avec une recherche par nom et « Tout décocher » : un PNJ ne compte que si toutes les monnaies qu'il demande sont cochées. COMPILED et TESTED, non validé en jeu.
- Tests : BaseMaterials699Tests. Verify.ps1 (agent) : 1247/1247, 0 avertissement, 0 erreur ; DLL 0.701.0.0. Non validé en jeu.
### English
- User report and request: the currency list showed numbers. #0 is a currency the game encodes and nothing resolves, #8223 a row with no name in the game's sheet: they leave the list and their offers do not count. #4294905804 is Tensho's own code for company credits, now named. The list starts all unticked, with a name search and Untick all: an NPC counts only if every currency it asks for is ticked. COMPILED and TESTED, not validated in game.

## 0.700.0
- Plan global, W-09 : Demandes de l'utilisateur : dans les panneaux, chaque provenance se replie, avec le nombre d'objets complets, et les cristaux ont leur propre catégorie. La case Vendeurs PNJ coupe les PNJ du calcul : ce qu'ils vendent descend à ses ingrédients ou passe sous une autre provenance. Une liste exclut des monnaies : un PNJ qui ne prend que celles-là ne compte plus. COMPILED et TESTED, non validé en jeu.
- Tests : BaseMaterials699Tests. Verify.ps1 (agent) : 1246/1246, 0 avertissement, 0 erreur ; DLL 0.700.0.0. Non validé en jeu.
### English
- User requests: in the panels, each origin folds, with the count of complete items, and crystals have their own category. The NPC vendors box removes vendors from the calculation: what they sell goes down to its ingredients or under another origin. A list leaves currencies out: an NPC taking only those no longer counts. COMPILED and TESTED, not validated in game.
