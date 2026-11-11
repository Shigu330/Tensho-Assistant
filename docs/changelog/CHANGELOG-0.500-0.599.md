# Journal des versions / Changelog, 0.500 à 0.599

Archive de [CHANGELOG.md](../../CHANGELOG.md) : versions plus anciennes que la dernière release, déplacées par `tools/changelog.py archive`. Même format (français, puis `### English` depuis 0.219.0) ; liens relatifs à la racine réécrits depuis ce dossier.

## 0.599.0
- Plan global, T10 : Accueil : bloc « Aujourd'hui » : rappels à faire, ce qui est terminé (servants, vaisseaux, escouade, jardin), prochain vaisseau, quêtes suivies, trois prochaines alarmes ; un clic ouvre la page. « Personnaliser » choisit les lignes et les tuiles affichées (par défaut : toutes). Rien n'est lu du jeu ici, et le bloc est refait une fois par seconde.
- Tests : Today. Verify.ps1 (agent) : 1094/1094, 0 avertissement, 0 erreur ; DLL 0.599.0.0. Non validé en jeu.
### English
- Home: « Today » block: reminders to do, what is finished (ventures, vessels, squadron, garden), next vessel, followed quests, next three alarms; a click opens the page. « Customize » picks the lines and tiles shown (all by default). Nothing is read from the game here, and the block is rebuilt once a second.

## 0.598.0
- Plan global, T24 : Discord : chaque catégorie peut écrire dans son propre salon (une adresse de webhook par catégorie, vide : l'adresse principale). Adresses masquées, gardées lors d'une remise à zéro, jamais dans le journal ni les rapports.
- Tests : Discord channels. Verify.ps1 (agent) : 1091/1091, 0 avertissement, 0 erreur ; DLL 0.598.0.0. Non validé en jeu.
### English
- Discord: each category can post to its own channel (one webhook address per category, empty: the main address). Addresses masked, kept by a reset, never in the log or reports.

## 0.597.0
- Plan global, P10 : Section Carte du plugin : un champ cherche sur toutes les cartes un lieu, une éthérite, un vendeur, un PNJ de Triple Triad, un donneur de quête ou une ressource ; un clic ouvre la carte Tensho centrée sur le résultat, sans poser de drapeau. Liste préparée hors de l'affichage.
- Tests : Map search. Verify.ps1 (agent) : 1087/1087, 0 avertissement, 0 erreur ; DLL 0.597.0.0. Non validé en jeu.
### English
- Plugin's Maps section: a field searches every map for a place, an aetheryte, a vendor, a Triple Triad NPC, a quest giver or a resource; a click opens the Tensho map centred on the result, without a flag. The list is prepared off the frame.

## 0.596.0
- Plan global, P12 : Carte : les noms que le jeu écrit à côté de certaines icônes (éthérites, lieux importants) sont écrits sur la carte Tensho, du même côté, à partir du même zoom que les noms de lieux ; case « Noms des icônes » pour les retirer. Le côté vient de MapMarker.SubtextOrientation, mesuré sur les données du jeu (preuve : docs/evidence/0.596-map-label-orientation.md), à confirmer en jeu.
- Tests : Map labels. Verify.ps1 (agent) : 1083/1083, 0 avertissement, 0 erreur ; DLL 0.596.0.0. Non validé en jeu.
### English
- Map: the names the game writes beside some icons (aetherytes, important places) are written on Tensho's map, on the same side, from the same zoom as place names; an « Icon names » box removes them. The side comes from MapMarker.SubtextOrientation, measured on the game's data (evidence: docs/evidence/0.596-map-label-orientation.md), to confirm in game.

## 0.595.0
- Plan global, I21 : Règles de tri : une zone peut ne prendre que l'équipement d'un job, ou les pièces de vos sets d'équipement du jeu (oui ou non). Tant que les sets ne sont pas lus, le tri réel attend et le dit ; rien ne bouge sur ce qui n'est pas lu.
- Tests : Sort gear. Verify.ps1 (agent) : 1080/1080, 0 avertissement, 0 erreur ; DLL 0.595.0.0. Non validé en jeu.
### English
- Sorting rules: a zone can take only the gear of a job, or the pieces of your game gear sets (yes or no). While the sets are unread, a real sort waits and says so; nothing moves on what is unread.

## 0.594.0
- Plan global, I11 : Banque : case « Afficher en liste » : un objet par ligne avec NQ, HQ, total et ses rangements, à la place des grilles, triable par nom, catégorie ou quantité en cliquant sur la colonne ; l'onglet et les filtres restent appliqués.
- Tests : Bank list. Verify.ps1 (agent) : 1077/1077, 0 avertissement, 0 erreur ; DLL 0.594.0.0. Non validé en jeu.
### English
- Bank: « Show as a list » box: one item per line with NQ, HQ, total and its storages, instead of the grids, sorted by name, category or quantity by clicking the column; the tab and filters still apply.

## 0.593.0
- Plan global, T25 : Page Équipement : plusieurs plans nommés par job. Liste des plans, nouveau plan (copie de l'actuel), renommer, supprimer ; en prendre un garde l'actuel tel quel, et le code de partage donne le plan affiché.
- Tests : Gear plans. Verify.ps1 (agent) : 1074/1074, 0 avertissement, 0 erreur ; DLL 0.593.0.0. Non validé en jeu.
### English
- Gear page: several named plans per job. A list of plans, new plan (a copy of the current one), rename, delete; taking one keeps the current one as it is, and the share code gives the plan shown.

## 0.592.0
- Plan global, C21 : Nouvel onglet « Que puis-je fabriquer ? » dans Artisanat : sur un clic, les recettes que les sacs et la Banque enregistrée peuvent fabriquer maintenant, avec le nombre de fabrications, filtrées par métier et niveau. Ingrédients directs seulement ; un stock jamais vu n'est pas compté comme zéro.
- Tests : From stock. Verify.ps1 (agent) : 1069/1069, 0 avertissement, 0 erreur ; DLL 0.592.0.0. Non validé en jeu.
### English
- New « What can I craft? » tab in Crafting: on a click, the recipes the bags and saved Bank can make now, with the number of crafts, filtered by job and level. Direct ingredients only; a stock never seen is not taken as zero.

## 0.591.0
- Plan global, C18 : Dans la chaîne de fabrication, une recette rencontrée plusieurs fois n'a plus qu'une ligne avec son total, chaque ligne venant après les recettes de ses ingrédients ; chaque ligne a ses boutons Carnet et Simuler et une case « fait », gardée jusqu'au prochain calcul.
- Tests : Chain. Verify.ps1 (agent) : 1064/1064, 0 avertissement, 0 erreur ; DLL 0.591.0.0. Non validé en jeu.
### English
- In the crafting chain, a recipe met several times is now one row with its total, each row after the recipes of its ingredients; each row has Log and Simulate buttons and a done box, kept until the next calculation.

## 0.590.0
- Plan global, C16 : La page des séquences enregistrées se cherche par nom, objet ou métier et se trie ; une séquence se renomme, se duplique et se copie en texte. L'import d'une macro collée existait déjà dans le simulateur (0.425) et relit ce texte.
- Tests : Sequences. Verify.ps1 (agent) : 1060/1060, 0 avertissement, 0 erreur ; DLL 0.590.0.0. Non validé en jeu.
### English
- The saved sequences' page can be searched by name, item or job and sorted; a sequence can be renamed, duplicated and copied as text. Importing a pasted macro already existed in the simulator (0.425) and reads that text back.

## 0.589.0
- Plan global, C5 : Pendant une synthèse, la fenêtre à côté de la synthèse et la page Synthèse en cours suivent la séquence choisie (simulateur, séquence enregistrée pour la recette, suggestion du panneau du carnet) : action suivante, chiffres du plan contre ceux du jeu, avertissement quand ils se séparent, avec les états autres que Normal déjà joués. Réglage pour le retirer. Rien n'est joué.
- Tests : Follow. Verify.ps1 (agent) : 1056/1056, 0 avertissement, 0 erreur ; DLL 0.589.0.0. Non validé en jeu.
### English
- During a synthesis, the window beside it and the live craft page follow the chosen sequence (simulator, saved for the recipe, Crafting Log panel's suggestion): next action, the plan's numbers against the game's, a warning when they part, with the non-Normal states already played. A setting turns it off. Nothing is played.

## 0.588.0
- Plan global, C7 : page Récolte : une section liste les points à horaire ouverts maintenant, puis ceux qui ouvrent dans l'heure, avec le temps restant, la zone, le niveau et une cloche. Recalculée une fois par minute d'Éorzéa. Les poissons ne sont pas inclus : leur météo n'y est pas vérifiée. Les filtres de métier et de niveau restent à faire.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1049/1049, 0 avertissement, 0 erreur ; DLL 0.588.0.0. Non validé en jeu.
### English
- The gathering page lists the timed points open now, then those opening within the hour, with a countdown and a bell.

## 0.587.0
- Plan global, Carte : demande de l'utilisateur : la roue dentée de la carte ouvre l'onglet Carte des Paramètres, au lieu d'une fenêtre surgissante devenue trop longue. Ce sont les mêmes réglages, et la liste des icônes reste celle de la carte affichée.
- Tests : MapZoom542Tests complété. Verify.ps1 (agent) : 1048/1048, 0 avertissement, 0 erreur ; DLL 0.587.0.0. Non validé en jeu.
### English
- The map's cog now opens the Map tab of Settings instead of a long popup.

## 0.586.0
- Plan global, Carte : rapport de l'utilisateur en jeu : pas d'aile sur les vieilles zones. Tensho lisait le lien depuis l'ensemble de courants, qui ne nomme que Mor Dhona, alors que toutes ces zones partagent l'ensemble 19 (docs/evidence/0.586-flight-sets.md). Il lit maintenant la colonne de chaque zone.
- Tests : MapZoom542Tests complété. Verify.ps1 (agent) : 1047/1047, 0 avertissement, 0 erreur ; DLL 0.586.0.0. Non validé en jeu.
### English
- The map's wing now shows on the old zones too (La Noscea, Thanalan, the Shroud, Coerthas): they share one aether current set, which only named Mor Dhona.

## 0.585.0
- Plan global, Équipement : rapport de l'utilisateur en jeu : la liste « Plan pour » montrait deux jobs nommés 44 et 45 : ce sont des lignes vides de la feuille ClassJob, qui pointent sur le compteur du pugiliste. L'index des niveaux ignore les lignes sans nom, pour tous ses usages (docs/evidence/0.585-classjob-empty-rows.md). Le texte passe avant la liste, et le job porté prend sa majuscule.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1046/1046, 0 avertissement, 0 erreur ; DLL 0.585.0.0. Non validé en jeu.
### English
- The gear page's job list no longer shows two empty rows of the game's job sheet. Its label now comes before the list.

## 0.584.0
- Plan global, Rappels : demande de l'utilisateur : le réglage Discord d'un rappel n'existe plus qu'à un endroit. Sur la page Alarmes, un groupe issu de Rappels dit « Discord : réglé dans Rappels » au lieu de sa case, et seule la case de la ligne dans Rappels décide. Les autres alarmes gardent la leur, et les alarmes de rappel plus anciennes sont reliées à leur ligne par leur nom.
- Tests : DiscordWebhookTests complété. Verify.ps1 (agent) : 1045/1045, 0 avertissement, 0 erreur ; DLL 0.584.0.0. Non validé en jeu.
### English
- A reminder's Discord switch now exists in one place: the Alarms page says it is set in Reminders, and only that row's box decides. Other alarms keep their own.

## 0.583.0
- Plan global, T12 : fiche d'une quête : ce qui reste à faire avant elle, dans l'ordre, et la prochaine à prendre. Parcours borné sans récursion, qui résiste à un cycle et à une très longue chaîne. Une condition illisible est dite.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1045/1045, 0 avertissement, 0 erreur ; DLL 0.583.0.0. Non validé en jeu.
### English
- A quest's sheet lists what is left to do before it, in order, and the next one to take.

## 0.582.0
- Plan global, T11 : page Quêtes : filtres « Disponibles maintenant » (non commencée, prérequis connus faits) et « Cette zone ». Un niveau ou une étape que Tensho ne sait pas lire n'est pas vérifié, et l'infobulle le dit. Le donneur sur la carte reste sur la fiche de la quête.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1044/1044, 0 avertissement, 0 erreur ; DLL 0.582.0.0. Non validé en jeu.
### English
- The quest page has two more filters: available now and this zone. What Tensho cannot read is not checked, and the tooltip says so.

## 0.581.0
- Plan global, T15 : Revue de mode : une case par pièce compose la tenue de la semaine, gardée jusqu'à la remise. La section dit quelles pièces sont possédées, avec le rangement au survol, et « Essayer la tenue » les met dans la cabine d'essayage, sur clic. Les sources des pièces manquantes restent dans la fiche d'objet.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1043/1043, 0 avertissement, 0 erreur ; DLL 0.581.0.0. Non validé en jeu.
### English
- Fashion Report: a box per piece builds the week's outfit. It says which pieces are owned and where, and a button tries them all on.

## 0.580.0
- Plan global, T14 : page Équipement : un menu « Plan pour » prépare le plan d'un autre job sans le porter : pièces, matérias, essayage. Ses attributs réels ne sont pas affichés, car ils ne se lisent qu'en le portant, et aucune pièce portée ne lui est prêtée. La somme des pièces n'est pas encore affichée pour ce job.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1042/1042, 0 avertissement, 0 erreur ; DLL 0.580.0.0. Non validé en jeu.
### English
- The gear page can plan another job than the one worn: pieces, materia and try on. Its real attributes are not shown, as they are only read while wearing it.

## 0.579.0
- Plan global, Équipement : demande de l'utilisateur : la case « Tous les jobs (mirage) » est aussi directement sur la page Équipement. Elle existait derrière le bouton de filtres et dans les Paramètres ; c'est le même réglage, sans doublon.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1041/1041, 0 avertissement, 0 erreur ; DLL 0.579.0.0. Non validé en jeu.
### English
- The All jobs (glamour) box is now on the gear page itself. It is the same setting as in the filters.

## 0.578.0
- Plan global, Rappels : demande de l'utilisateur : dans Rappels, une case Discord par ligne décide si les alarmes de ce rappel partent sur Discord, y compris les alarmes automatiques (servants, escouade, vaisseaux). Par défaut, tout reste coché, et la catégorie des réglages garde son effet.
- Tests : DiscordWebhookTests complété. Verify.ps1 (agent) : 1040/1040, 0 avertissement, 0 erreur ; DLL 0.578.0.0. Non validé en jeu.
### English
- The Reminders page has a Discord box per row: it decides whether that reminder's alarms go to Discord, automatic ones included.

## 0.577.0
- Plan global, T0 : alarmes en heure réelle : une fois, chaque jour, chaque semaine à partir d'une date, ou à chaque remise à zéro du jour ou de la semaine, sans date. Les alarmes existantes ne changent pas.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1040/1040, 0 avertissement, 0 erreur ; DLL 0.577.0.0. Non validé en jeu.
### English
- Alarms in real time can now repeat every day, every week, or at each daily or weekly reset.

## 0.576.0
- Plan global, I1 : page Rangements : un servant qui n'est plus à la sonnette est signalé, jamais effacé tout seul, et « Effacer ce relevé » (deux clics) retire un relevé de vos stocks.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1039/1039, 0 avertissement, 0 erreur ; DLL 0.576.0.0. Non validé en jeu.
### English
- The storage page flags a retainer no longer at the bell, and a read can be erased with two clicks.

## 0.575.0
- Plan global, I3 : le coût de préparation prend le prix en gils d'un PNJ quand il est plus bas que le marché, aussi pour un objet que le marché ne vend pas ; la ligne le marque. Réglage activé par défaut, à part si du HQ est demandé. La ligne du Résumé de la page Marché reste à faire.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1038/1038, 0 avertissement, 0 erreur ; DLL 0.575.0.0. Non validé en jeu.
### English
- The preparation's cost now takes an NPC's gil price when lower, also for items the market does not sell. The line is marked.

## 0.574.0
- Plan global, I5 : le coût de préparation peut se calculer au moins cher du centre de données, et chaque ligne nomme alors le monde où acheter. Aucune nouvelle requête. Réglage désactivé par défaut.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1037/1037, 0 avertissement, 0 erreur ; DLL 0.574.0.0. Non validé en jeu.
### English
- The preparation's cost can use the cheapest offer of the data center, naming the world to buy on.

## 0.573.0
- Plan global, C17 : macros : son de la ligne de fin réglable de 1 à 16, ou aucun, et ligne de fin sur la dernière page seulement. Par défaut, rien ne change. L'attente séparée existait déjà (0.419). L'icône des macros reste à sonder en jeu.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1036/1036, 0 avertissement, 0 erreur ; DLL 0.573.0.0. Non validé en jeu.
### English
- Macro export: the end line's sound can be set from 1 to 16 or none, and the line can stay on the last page only.

## 0.572.0
- Plan global, C15 : le simulateur a sa propre recherche de recette, et le résultat se recalcule seul après chaque changement, une fois par changement (réglage activé par défaut). Le glisser-déposer des actions reste à faire.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1035/1035, 0 avertissement, 0 erreur ; DLL 0.572.0.0. Non validé en jeu.
### English
- The simulator has its own recipe search, and it runs again by itself once after each change.

## 0.571.0
- Plan global, T29 : la page Mini Cactpot montre, repliée, la table des gains selon la somme de la ligne : celle que le conseil utilise déjà. Elle reste à comparer au ticket du jeu en phase 6.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1034/1034, 0 avertissement, 0 erreur ; DLL 0.571.0.0. Non validé en jeu.
### English
- The Mini Cactpot page shows, folded, the gains by line sum that the advice uses.

## 0.570.0
- Plan global, T16 : la ligne des tâches de servants se déplie : par servant, sa tâche finie ou son heure de fin avec une cloche, et une alarme pour le retour du dernier. L'alarme automatique de 0.371 reste.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1033/1033, 0 avertissement, 0 erreur ; DLL 0.570.0.0. Non validé en jeu.
### English
- The retainer tasks line expands: each retainer with its end time and a bell, and one alarm for the last back.

## 0.569.0
- Plan global, P15 : après une mise à jour du jeu, la carte dit ce qui ne se lit plus (vol, marqueurs de quête, ALEA, groupe…) au lieu de sembler vide, et Diagnostics aussi. Une partie qui se relit quitte la liste.
- Tests : MapZoom542Tests complété. Verify.ps1 (agent) : 1032/1032, 0 avertissement, 0 erreur ; DLL 0.569.0.0. Non validé en jeu.
### English
- The map and Diagnostics now say which parts no longer read, perhaps after a game update. A part that reads again leaves the list.

## 0.568.0
- Plan global, C9 : dans l'aide à la pêche, chaque poisson dit si son créneau, sa météo et sa météo d'avant sont bons maintenant (oui, non, ?), d'après GatherBuddy. Météo calculée une fois par période. C0 était déjà présent : rien à coder.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1031/1031, 0 avertissement, 0 erreur ; DLL 0.568.0.0. Non validé en jeu.
### English
- The fishing help shows, for each fish, whether its time window, weather and previous weather hold now.

## 0.567.0
- Plan global, I18 : page Marché : un bouton copie le prix le plus bas moins 1, sans vos servants, avec la date du relevé dans l'infobulle. Il reprend le calcul existant de la sous-cotation. Copié seulement : rien n'est mis en vente.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1030/1030, 0 avertissement, 0 erreur ; DLL 0.567.0.0. Non validé en jeu.
### English
- The market page can copy the lowest offer minus 1, without your retainers. It is only copied.

## 0.566.0
- Plan global, I15 : un modèle de tri se copie en texte et se colle dans une autre bibliothèque. Le texte collé est vérifié et reçoit son propre identifiant ; il ne contient ni personnage ni objet possédé.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1029/1029, 0 avertissement, 0 erreur ; DLL 0.566.0.0. Non validé en jeu.
### English
- Sort templates can be copied as text and pasted elsewhere. Pasted text is checked first.

## 0.565.0
- Plan global, I20 : l'aperçu du tri encadre en or les cases qui changeraient et en donne le nombre. Une pile dont la place n'a pas été lue ne compte pas.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1028/1028, 0 avertissement, 0 erreur ; DLL 0.565.0.0. Non validé en jeu.
### English
- The sort preview frames in gold the slots that would change and counts them.

## 0.564.0
- Plan global, T18 : nouvelles balises des messages Discord : [discordtime] (l'heure dans le fuseau de chaque lecteur), [relative], [et], [zone] et [item]. [time] ne change pas. Un nom inconnu ne laisse rien.
- Tests : DiscordWebhookTests complété. Verify.ps1 (agent) : 1027/1027, 0 avertissement, 0 erreur ; DLL 0.564.0.0. Non validé en jeu.
### English
- New Discord message markers: [discordtime] and [relative] read in each reader's time zone, [et], [zone] and [item].

## 0.563.0
- Plan global, C31 : page Météo : un tableau de vos zones, 12 au plus, sur les 6 prochaines périodes. Calculé une fois par période. Un clic sur une zone l'affiche.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1027/1027, 0 avertissement, 0 erreur ; DLL 0.563.0.0. Non validé en jeu.
### English
- The weather page has a table of your zones, up to 12, over the next 6 periods.

## 0.562.0
- Plan global, I19 : réglage, désactivé par défaut : le nom anglais d'un objet sous le sien, dans sa fiche et son infobulle, quand ils diffèrent.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1026/1026, 0 avertissement, 0 erreur ; DLL 0.562.0.0. Non validé en jeu.
### English
- New setting, off by default: an item's English name under its own, in its sheet and tooltip.

## 0.561.0
- Plan global, I8 : le menu d'un objet copie son nom, affiche un lien dans votre propre chat (message local, rien n'est envoyé au serveur) et ouvre sa page sur Universalis, Garland Tools ou Teamcraft, sur clic. Avec I9. Le menu du jeu n'a pas encore ces entrées.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1025/1025, 0 avertissement, 0 erreur ; DLL 0.561.0.0. Non validé en jeu.
### English
- An item's menu now copies its name, prints a link in your own chat and opens its page on Universalis, Garland Tools or Teamcraft.

## 0.560.0
- Plan global, C24 : dans les aides à la récolte et à la pêche, un objet utile à la dernière chaîne d'artisanat est marqué « Encore 14 pour… », et le nombre baisse à mesure de la récolte.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1024/1024, 0 avertissement, 0 erreur ; DLL 0.560.0.0. Non validé en jeu.
### English
- The gathering and fishing helps mark an item the last crafting chain still needs, and the number falls as you get it.

## 0.559.0
- Plan global, C19 : au survol d'un matériau, dans la préparation et la chaîne : le détail par rangement (sacs, servant, sacoche), avec la date des relevés enregistrés. Rien n'est recalculé.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1023/1023, 0 avertissement, 0 erreur ; DLL 0.559.0.0. Non validé en jeu.
### English
- Hovering a material in the preparation and the chain lists which storage holds it, with the date of saved reads.

## 0.558.0
- Plan global, T17 : quand le jeu indique 0 quête de tribu ou 0 commande spéciale restante, la case de Rappels se coche seule jusqu'à la remise, aussi pour le compteur de la barre du serveur. Un compteur inconnu ne coche rien. Réglage activé par défaut.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1022/1022, 0 avertissement, 0 erreur ; DLL 0.558.0.0. Non validé en jeu.
### English
- A reminder ticks itself when the game reads 0 tribal quests or 0 special deliveries left. An unknown counter ticks nothing.

## 0.557.0
- Plan global, T19 : sur une alarme : « Modifier » (nom et, pour une alarme unique, date) et « Dans 5 min », qui ajoute la même alarme une fois de plus. Une alarme automatique ne se modifie pas, car elle suit son échéance. Pour une copie dans le jeu, un message dit de la corriger à la main.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1021/1021, 0 avertissement, 0 erreur ; DLL 0.557.0.0. Non validé en jeu.
### English
- Alarms can be edited (name and, for a one-off, date) and put off with a button that adds the same alarm a few minutes later.

## 0.556.0
- Plan global, T20 : réglage, désactivé par défaut : une alarme sonne avec son avance, puis une seconde fois à l'heure exacte, sans doubler Discord. Une occurrence écartée pour sa météo ne sonne pas. Réglage général : pas d'option par alarme.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1020/1020, 0 avertissement, 0 erreur ; DLL 0.556.0.0. Non validé en jeu.
### English
- New alarm setting, off by default: an alarm rings its lead ahead, then again at the exact time. Discord only gets the first.

## 0.555.0
- Plan global, I16 : commandes « /tensho objet », « prix », « recolte » et « recette » (aussi item, price, gather, recipe), avec C32 : elles ouvrent le premier objet trouvé dans Tensho, comme wiki et lodestone.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1019/1019, 0 avertissement, 0 erreur ; DLL 0.555.0.0. Non validé en jeu.
### English
- New commands: /tensho item, price, gather and recipe, in French and English, open the first matching item in Tensho.

## 0.554.0
- Plan global, C3 : le panneau du carnet calcule avec les derniers attributs relevés du métier de la recette, marqués « relevés le… », au lieu de dire « Équipez le métier ». Le simulateur propose les mêmes valeurs. Écrit seulement quand ça change. Le bouton « Simuler avec ces attributs » de la page Équipement reste à faire.
- Tests : CraftThresholds553Tests complété. Verify.ps1 (agent) : 1018/1018, 0 avertissement, 0 erreur ; DLL 0.554.0.0. Non validé en jeu.
### English
- The crafting log panel and the simulator can use a job's last read attributes when that job is not worn, always marked with the date.

## 0.553.0
- Plan global, C4 : sous une simulation terminée, l'Habileté, le Contrôle et les PS minimum pour que la séquence finisse à la même qualité, avec la marge. Calcul hors du dessin, annulé si le résultat change, avec le moteur de simulation.
- Tests : CraftThresholds553Tests. Verify.ps1 (agent) : 1017/1017, 0 avertissement, 0 erreur ; DLL 0.553.0.0. Non validé en jeu.
### English
- Under a finished simulation, the lowest Craftsmanship, Control and CP the sequence needs at that quality, with the margin.

## 0.552.0
- Plan global, C2 : le panneau du carnet essaie aussi les séquences enregistrées pour d'autres recettes, et montre la meilleure qui termine, avec la recette d'origine. Réglage activé par défaut. La partie « Essayer mes séquences » du simulateur reste à faire.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1016/1016, 0 avertissement, 0 erreur ; DLL 0.552.0.0. Non validé en jeu.
### English
- The crafting log panel also tries sequences saved for other recipes and says which recipe the best one was saved for.

## 0.551.0
- Plan global, T4 : le bouton carte d'un monstre pose le drapeau à la position de sa fiche et ouvre la carte Tensho dessus, au lieu du centre. L'aide dit que la position vient de Garland Tools et qu'une apparition n'y est pas prouvée. Sans position, rien ne change.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1015/1015, 0 avertissement, 0 erreur ; DLL 0.551.0.0. Non validé en jeu.
### English
- A monster's map button now places the flag at its known position (community data from Garland Tools) and opens the Tensho map there.

## 0.550.0
- Plan global, T3 : la liste des quêtes montre le niveau et la zone, se trie sur chaque colonne et se limite à une plage de niveaux, gardée entre les sessions. Une quête de niveau inconnu n'est pas prise pour le niveau 0.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1014/1014, 0 avertissement, 0 erreur ; DLL 0.550.0.0. Non validé en jeu.
### English
- The quest list shows level and zone, sorts on every column and can be limited to a level range.

## 0.549.0
- Plan global, I0 : l'équipement porté compte comme possédé, sur une ligne « Équipement porté » : la Revue de mode ne dit plus « non possédé » pour une pièce portée, et le stock au survol et la fiche objet l'incluent. Le tri et la grille de la Banque restent sur les sacs.
- Tests : ItemHoldingsTests complété. Verify.ps1 (agent) : 1013/1013, 0 avertissement, 0 erreur ; DLL 0.549.0.0. Non validé en jeu.
### English
- Worn gear now counts as owned, on its own line, for example in the Fashion Report and in stocks. Sorting and the Bank's grid are unchanged.

## 0.548.0
- Plan global, P9 : une ALEA à bonus d'expérience le dit dans son libellé, sur la carte et dans la liste du clic droit (`IFate.HasBonus`). À la demande de l'utilisateur, la zone garde sa couleur rose : pas de couleur selon le temps restant.
- Tests : MapZoom542Tests complété. Verify.ps1 (agent) : 1012/1012, 0 avertissement, 0 erreur ; DLL 0.548.0.0. Non validé en jeu.
### English
- A FATE with an EXP bonus says so in its label, on the map and in the right-click list. Its area keeps its colour.

## 0.547.0
- Plan global, P20 : bouton « Couches » dans la colonne d'outils : les couches de la carte en une petite liste, avec « Tout afficher » et « Tout masquer ». Ce sont les mêmes réglages que dans la roue dentée, sans doublon.
- Tests : MapZoom542Tests complété. Verify.ps1 (agent) : 1011/1011, 0 avertissement, 0 erreur ; DLL 0.547.0.0. Non validé en jeu.
### English
- A Layers button in the map's tool column lists the layers, with show all and hide all. They are the same settings as in the cog.

## 0.546.0
- Plan global, P16 : réglage, désactivé par défaut (demande de l'utilisateur) : un clic gauche bref sur un endroit vide d'une carte de zone pose le drapeau. Un glisser déplace toujours la carte, et un clic sur une icône garde son effet.
- Tests : MapZoom542Tests complété. Verify.ps1 (agent) : 1010/1010, 0 avertissement, 0 erreur ; DLL 0.546.0.0. Non validé en jeu.
### English
- New setting, off by default: a short left click on an empty spot of a zone map places the flag.

## 0.545.0
- Plan global, P5 : la molette ne zoomait que sur le fond de carte. Elle marche maintenant sur toute la fenêtre de la carte (colonne d'outils, curseur, noms posés dessus), autour du centre de la vue. Réglages : pas de la molette et sens inversé. Par défaut, le pas reste 15 %.
- Tests : MapZoom542Tests complété. Verify.ps1 (agent) : 1009/1009, 0 avertissement, 0 erreur ; DLL 0.545.0.0. Non validé en jeu.
### English
- The mouse wheel now zooms over the whole map window. Its step and direction are settings.

## 0.544.0
- Plan global, P3 : depuis un point quelconque ou le drapeau, le clic droit de la carte propose l'éthérite la plus proche avec son coût. Déjà présent : le calcul et la question de 0.373, repris sans doublon. Téléportation seulement après confirmation.
- Tests : MapZoom542Tests complété. Verify.ps1 (agent) : 1008/1008, 0 avertissement, 0 erreur ; DLL 0.544.0.0. Non validé en jeu.
### English
- The map's right-click menus now offer the nearest aetheryte of that map from any point or the flag, with its cost, after confirmation.

## 0.543.0
- Plan global, P2 : les noms de lieux suivaient le zoom comme les icônes et se chevauchaient une fois dézoomés ; les étiquettes de récolte avaient une taille et un seuil figés. Réglages : suivi du zoom des noms, zoom minimal des noms, seuil et taille des étiquettes de récolte. Par défaut, rien ne change.
- Tests : MapZoom542Tests complété. Verify.ps1 (agent) : 1007/1007, 0 avertissement, 0 erreur ; DLL 0.543.0.0. Non validé en jeu.
### English
- Map place names can follow the zoom apart from the icons and hide under a chosen zoom. Gathering labels have their own size and threshold.

## 0.542.0
- Tri des propositions, lot 1, P1 (demande de l'utilisateur) : la carte démarrait toujours à 50 %, et le zoom sur une quête ou un lien était figé à 100 % au moins. Trois réglages : zoom à l'ouverture (dernier zoom utilisé, gardé après une relance ; zoom fixe en suivant le personnage ; carte entière), valeur du zoom fixe, zoom sur une quête ou un lien. Par défaut, le dernier zoom, à 50 % la première fois. Il est enregistré après 2 secondes sans changement, jamais à chaque image. Une carte choisie s'ouvre toujours en entier.
- Tests : MapZoom542Tests. Verify.ps1 (agent) : 1006/1006, 0 avertissement, 0 erreur ; DLL 0.542.0.0. Non validé en jeu.
### English
- The map's zoom at opening is now a setting: last zoom used (kept across restarts), fixed zoom following the character, or the whole map. The zoom used on a quest or link is a setting too.

## 0.541.0
- Plan global, phase 1 (issue #37) : le bloc d'objets de « Mes outils » ne montrait pas les prix. Nouvelle colonne « Prix à l'unité » (le plus bas de NQ et HQ dans le cache partagé), coût de ce qui manque, et bouton « Demander les prix », 32 objets au plus par clic. Un prix ou un stock inconnu est compté comme inconnu (« au moins »), jamais comme gratuit. Rien n'est demandé sans le clic.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1005/1005, 0 avertissement, 0 erreur ; DLL 0.541.0.0. Non validé en jeu.
### English
- The item block of My Tools now shows prices: price each, cost of what is missing, and a button that asks Universalis for them.

## 0.540.0
- Plan global, phase 1 (issue #36) : les bleuets manquaient au journal du jardin : la ligne 108 de `GardeningSeed` n'est pas dans les données de Lotlab. « Graines de centaurée » (51189, récolte 51190) est ajoutée. Identifiants tirés des données du jeu ; 24 h sans flétrissement, déduits des 26 autres fleurs en pot, modifiables dans le journal (docs/evidence/0.540-cornflower-seed.md). Une plantation de cette graine lue dans le chat est donc reconnue.
- Tests : GardenSeeds449Tests complété. Verify.ps1 (agent) : 1004/1004, 0 avertissement, 0 erreur ; DLL 0.540.0.0. Non validé en jeu.
### English
- The garden journal now knows cornflower seeds, missing from the embedded data (game row 108). Their 24 hours are inferred from the 26 other pot flowers.

## 0.539.0
- Rapport de l'utilisateur en jeu : en déplaçant une fenêtre du jeu, le panneau de Tensho lié à elle suivait avec du retard, car sa position venait des lecteurs, rafraîchis 5 fois par seconde (1 fois pour le personnage). Un nouveau lecteur `UI/GameWindowFollower.Dalamud.cs` relit à chaque tick la seule position des fenêtres dont un panneau est affiché ; rien n'est lu quand aucun panneau ne le demande. Panneaux concernés : récolte, objets de collection, pêche au harpon, personnage, mission, carnet d'artisanat, Fashion Report, Triple Triade, import de banque.
- Rapport de l'utilisateur en jeu : nos panneaux liés ne faisaient pas disparaître la carte (réglage « S'effacer devant les fenêtres du jeu »), alors que leur fenêtre le fait. Ils comptent maintenant, ainsi que le panneau de survol ; les panneaux placés librement (pêche, voyage en mer) ne comptent pas. Précision de l'utilisateur : la fenêtre principale de Tensho ne doit pas faire disparaître la carte ; c'est le cas, et un test le garde.
- Demande de l'utilisateur : un délai avant le retour de la carte : elle part aussitôt et revient 1 seconde après que plus rien n'est devant, pour ne pas clignoter d'une infobulle à l'autre. Les autres panneaux et les barres ne changent pas.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1004/1004, 0 avertissement, 0 erreur ; DLL 0.539.0.0. Validé en jeu (rapport de l'utilisateur, 2026-11-05, « tous es ok ») : les panneaux suivent, la carte s'efface devant eux et revient après le délai.
### English
- Attached panels now follow their game window every tick instead of lagging behind it when it moves.
- The map also steps aside for attached panels, and comes back one second after nothing is in front of it.

## 0.538.0
- Rapport de l'utilisateur en jeu (capture) : l'infobulle d'un objet du jeu passait sous la carte, car 0.537 ignorait les infobulles et les menus. Ils comptent maintenant comme les fenêtres. Le panneau de survol de Tensho, lui, reste affiché avec l'infobulle. 0.537 validée sur les fenêtres (« fenêtre ok »).
- Tests : Interfaces535Tests adapté. Verify.ps1 (agent) : 1003/1003, 0 avertissement, 0 erreur ; DLL 0.538.0.0. Non validé en jeu.
### English
- Game tooltips and context menus now count too: an interface steps aside when one touches it.

## 0.537.0
- Demande de l'utilisateur en jeu, avec réglages : les interfaces de Tensho s'effacent quand une fenêtre du jeu passe devant, et reviennent ensuite ([relevé](../../docs/evidence/0.537-game-windows.md)). Le jeu ne peut pas dessiner par-dessus : c'est l'interface entière qui se cache.
- Panneaux accrochés à une fenêtre du jeu : seulement ce qui passe devant leur fenêtre (activé par défaut). Barres et panneaux libres : ce qui les touche (activé par défaut). Carte : ce qui la touche (Paramètres de la carte, désactivé par défaut, car elle part en entier). Le bouton Trier suit la même règle, sans réglage.
- Tests : Interfaces535Tests, avec les cas des captures. Verify.ps1 (agent) : 1003/1003, 0 avertissement, 0 erreur ; DLL 0.537.0.0. Non validé en jeu.
### English
- Tensho's panels, bars and, on a setting, the map step aside when a game window comes in front, and come back after. Three settings; the map's is off by default.

## 0.536.0
- Rapport de l'utilisateur en jeu (deux captures) : Trier faisait l'inverse : il restait sur le Bestiaire passé devant, et partait quand l'inventaire était devant. Dans une couche du jeu, la liste va de l'avant vers l'arrière ; le test était à l'envers.
- Tests : Interfaces535Tests complété. Verify.ps1 (agent) : 1003/1003, 0 avertissement, 0 erreur ; DLL 0.536.0.0. Non validé en jeu.
### English
- The sort button now leaves when a game window is in front of the inventory: the order test was the wrong way round.

## 0.535.0
- Demandes de l'utilisateur en jeu.
- Équilibré grisé ne se choisit plus. Un Équilibré enregistré s'affiche comme Sûr sur ce nœud, puisque c'est la même chose, et revient sur le nœud suivant où il a son plan.
- Carte : réglage « Sans barre de titre », désactivé par défaut. Elle se déplace par sa colonne d'outils et se ferme avec la touche de la carte.
- Interfaces : tout panneau qui a une barre de titre peut se réduire et se fermer. Fermé, il reste fermé jusqu'à ce que la fenêtre du jeu parte. Le panneau de banque gagne une barre de titre. Restent sans : barres d'usure et de PR, survol d'objet, bouton Trier, qui dépendent de leur réglage ou de leur fenêtre.
- Trier : le bouton disparaît quand une fenêtre du jeu passe devant, d'après l'ordre d'affichage du jeu ; les éléments du HUD et les grilles de l'inventaire ne comptent pas. Non vérifié en jeu : le nom de la fenêtre des filtres de tri ; le journal de débogage dit lesquelles comptent.
- Tests : Interfaces535Tests. Verify.ps1 (agent) : 1003/1003, 0 avertissement, 0 erreur ; DLL 0.535.0.0. Non validé en jeu.
### English
- Balanced cannot be chosen while greyed. The map can drop its title bar (setting). Panels with a title bar can be collapsed and closed, staying closed until their game window goes away. The sort button hides when a game window covers it.

## 0.534.0
- Choix de l'utilisateur, sur « sûr et équilibré exactement pareil » : l'icône d'Équilibré se grise quand elle donne la même chose que Sûr sur ce nœud, et son infobulle le dit. Elle reste cliquable. Calculé sur 40 situations : 18 n'ont qu'un résultat possible, 11 en ont deux, 11 en ont trois ; le risque y rapporte souvent 1 à 3 assignats en moyenne pour un plancher bien plus bas.
- Les crans partent des plans que le nœud offre vraiment. Sûr prend le plancher le plus haut et, à plancher égal, la meilleure moyenne. Équilibré ne descend que si la moyenne gagne au moins la moitié de ce que le plancher perd : par exemple 20/38,3 devient 14/43,0, mais 20/43,9 reste devant 14/45,2.
- Tests : Gathering478Tests complété. Verify.ps1 (agent) : 1000/1000, 0 avertissement, 0 erreur ; DLL 0.534.0.0. Non validé en jeu.
### English
- The Balanced icon is greyed when it gives the same as Safe on this node, and its tooltip says so. Safe takes the highest floor with the best average; Balanced lowers the floor only when the average gains at least half of what the floor loses.

## 0.533.0
- Demande de l'utilisateur (« il faut aussi la liste des skills utilisés pour chaque cap, il donne que pour Sûr ») : chaque cran montre maintenant ses coups, sans coup de chance, avec les PR qu'ils dépensent : par exemple, sur le nœud des captures à 625 PR, cran « Tentez votre chance » : « Sélection méthodique ×3 → Sagesse du fermier → Sélection méthodique → Sagesse du fermier → Sélection méthodique → Récolte. 600 PR sur 625 ». La suite vient du calcul du cran choisi, recalculée à chaque tour. Elle remplace les lignes propres à Sûr.
- Tests : Gathering478Tests complété (coups, PR et tentatives sur chaque cran). Verify.ps1 (agent) : 1000/1000, 0 avertissement, 0 erreur ; DLL 0.533.0.0. Non validé en jeu.
### English
- Every risk step now lists its moves and the GP they spend, on the line where no roll brings anything extra.

## 0.532.0
- Rapport de l'utilisateur en jeu (trois captures, « texte répétitif sur les mêmes pages ») : le même chiffre revenait jusqu'à quatre fois. Chaque ligne dit maintenant ce que les autres ne disent pas : la référence du palier haut et « Sur Sûr » n'apparaissent que si leur chiffre diffère de la phrase du cran ; le nombre d'assignats par récolte n'est plus répété. Ce qui vaut sur tous les nœuds (tentative gratuite, Œil et Toucher) passe dans l'infobulle « Comment est-ce calculé ? ».
- Les captures montraient aussi Équilibré sous Sûr (40,2 contre 43,9, même plancher de 20). Tous les crans gardent maintenant leur plancher avec la meilleure moyenne, pour tous les PR essayés.
- Tests : Gathering478Tests complété. Verify.ps1 (agent) : 1000/1000, 0 avertissement, 0 erreur ; DLL 0.532.0.0. Non validé en jeu.
### English
- The Collectable panel no longer repeats the same figure: each line says what the others do not, and what holds on every node moved to the tooltip. A step never earns less than a safer one for the same floor.

## 0.531.0
- Rapport de l'utilisateur en jeu (trois captures, « desc ne colle pas au changement ») : la description restait générale alors que les chiffres changeaient. Elle dit maintenant ce que le cran donne sur ce nœud : par exemple « Garde au moins 40 assignats, et vise 70,2 en moyenne ». Elle remplace la ligne « Avec ces coups ».
- Les captures montraient aussi un défaut : Sûr donnait 65,9 en moyenne et Équilibré 70,2, avec le même plancher de 40. Sûr ne regardait que le plancher. À plancher égal, il prend maintenant la meilleure moyenne.
- Tests : Gathering478Tests complété. Verify.ps1 (agent) : 1000/1000, 0 avertissement, 0 erreur ; DLL 0.531.0.0. Non validé en jeu.
### English
- The risk step's description now gives this node's figures. Safe now takes the better average between moves with the same floor.

## 0.530.0
- Demande de l'utilisateur (capture : « Tentez votre » coupé) : les crans de risque deviennent trois icônes, le nom dans l'infobulle : une main qui tient un objet (Sûr), une balance (Équilibré, ex-Incertain), un dé (Tentez votre chance). Le cran choisi est coloré.
- Tests : Gathering478Tests adapté. Verify.ps1 (agent) : 1000/1000, 0 avertissement, 0 erreur ; DLL 0.530.0.0. Non validé en jeu.
### English
- The collectable risk steps are now three icons with their name in the tooltip: a hand holding an item (Safe), a scale (Balanced), a die (Try your luck).

## 0.529.0
- Demande de l'utilisateur (« suive des caps plutôt que % », avec une description) : le risque du plan des collectionnables n'est plus un pourcentage, mais trois crans décrits : Sûr (0), Incertain (50), Tentez votre chance (100). Une valeur enregistrée va au cran le plus proche. Même choix dans les paramètres.
- Un seul plan à l'écran, celui du cran choisi. Les deux plans se mêlaient : le coup conseillé suivait la moyenne, les lignes « Avant elles » et « Ce plan dépense » le plan sans chance. Ces lignes ne s'affichent plus que sur « Sûr » ; ailleurs, une ligne dit ce que « Sûr » garantirait.
- Tests : Gathering478Tests complété. Verify.ps1 (agent) : 1000/1000, 0 avertissement, 0 erreur ; DLL 0.529.0.0. Non validé en jeu.
### English
- The collectable plan's risk is now three described steps: Safe, Uncertain, Try your luck. Only the chosen step's plan is shown, instead of two plans mixed together.

## 0.528.0
- Capture de l'utilisateur : la prochaine action du panneau Collectionnable était coupée à sa largeur (« … sur la prochaine actio »), comme la ligne des seuils. Elles passent à la ligne.
- Calcul relu sur la capture, 727 PR sans Inspection active : Inspection d'abord, 65,1 assignats en moyenne, comme prévu ([calcul](../../docs/evidence/0.527-priming-touch.md)). La consigne d'essai de Toucher était impraticable avec 823 PR au plus : corrigée.
- Tests : GpCrystals519Tests complété. Verify.ps1 (agent) : 999/999, 0 avertissement, 0 erreur ; DLL 0.528.0.0. Non validé en jeu.
### English
- The Collectable panel's next action and thresholds now wrap at its width instead of being cut.

## 0.527.0
- Rapport de l'utilisateur en jeu (captures) : le plan des collectionnables ne proposait ni Œil du collectionneur ni Toucher d'amorçage. Toucher n'existait pas dans le calcul : il y entre, à ×2 sur Sélection méthodique, confirmé par la capture (24 → 48 %). Œil y était déjà (40 → 70 %, confirmé aussi).
- Les deux ne sont proposés que s'ils rapportent plus que les mêmes PR ailleurs, et le panneau le dit. Sur la capture, à 623 PR avec Inspection active, ce n'est pas le cas : deux tentatives rendues valent plus. À 423 ou 723 PR, Toucher est conseillé ([calcul](../../docs/evidence/0.527-priming-touch.md)).
- Réglage ajouté : Paramètres › Modules › Récolte, Toucher d'amorçage dans le plan.
- Tests : Gathering478Tests complété, dont le nœud de la capture. Verify.ps1 (agent) : 998/998, 0 avertissement, 0 erreur ; DLL 0.527.0.0. Non validé en jeu.
### English
- Priming Touch now counts in the collectable plan (doubles the chance of a free Meticulous, confirmed in game). It and Collector's Focus are advised only when they earn more than the same GP elsewhere, and the panel says so.

## 0.526.0
- Demande de l'utilisateur : Collections ne couvrait pas les légendes locales. Nouvel onglet avec les 36 tomes de mineur, botaniste et pêcheur, lu ou non, et comment obtenir chacun ([preuve](../../docs/evidence/0.526-folklore-tomes.md)).
- La carte (0.518) ne voyait pas le douzième tome : FFXIVClientStructs déclare 11 bits, le tome d'Alexandrie est le bit 11. La lecture passe par les octets.
- Tests : Folklore526Tests. Verify.ps1 (agent) : 997/997, 0 avertissement, 0 erreur ; DLL 0.526.0.0. Non validé en jeu.
### English
- Collections has a new Folklore tab: the 36 tomes of miner, botanist and fisher, read or not, with how to obtain each. The map now also sees the twelfth tome of each job.

## 0.525.0
- Rapport de l'utilisateur en jeu (« les saccades pour les déplacements des membres de mon groupe sur la carte ») : le groupe n'était lu que deux fois par seconde, et un membre hors de vue ne reçoit sa position du jeu que de temps en temps. Lu dix fois par seconde, position exacte quand le membre est en vue, et le repère glisse entre deux positions (`MapPartySmoother`). Un grand saut, comme une téléportation, reste direct.
- Rapport de l'utilisateur en jeu (« le jeu lag au chargement du plugin ») : mesuré ([preuve](../../docs/evidence/0.525-load-cost.md)). La coupure de 1,2 à 1,9 s est une collecte complète de .NET, qui arrête tout le jeu, sur 2 rechargements sur 3 relevés ; le plugin ne la demande pas. Démarrage du jeu pas encore mesuré : une ligne « Load » du journal le dira. Corrigé : les données de vaisseaux se construisent hors du thread du framework (85 à 175 ms de moins).
- Reste à chaque chargement : première image 200 à 400 ms, journal des gils 80 ms. Rapport de l'utilisateur ensuite : désactiver puis réactiver le plugin ne donne aucun lag, et Ctrl+Alt marche (0.524 validée en jeu).
- Tests : PartyAndLoad525Tests. Verify.ps1 (agent) : 995/995, 0 avertissement, 0 erreur ; DLL 0.525.0.0. Non validé en jeu.
### English
- Party members now glide on the map: read ten times a second, exact position when in view, smoothed between positions, a teleport stays a jump.
- Load cost measured: the long pause is a full .NET collection, not requested by the plugin. Voyage data is now built off the framework thread, and a log line reports the cost of each load.

## 0.524.0
- Rapport de l'utilisateur en jeu (« ctrl alt fonctionne pas pour actualiser les prix ») ; 0.523 validée (« c'est ok »). Cause : l'appui ne dure qu'un tic, et la borne de 5 s entre deux demandes se consommait à chaque tic, demande ou pas : ils ne tombaient presque jamais ensemble, depuis 0.343.
- L'appui attend maintenant la borne, et seule une vraie demande la consomme. La pause de 3 s du marché ne le perd plus non plus : il attend le créneau suivant. Changer d'objet oublie l'appui. L'heure « Récupéré à » montre les secondes, sinon une actualisation dans la minute ne se voyait pas.
- Tests : HoverPrices343Tests adapté. Verify.ps1 (agent) : 992/992, 0 avertissement, 0 erreur ; DLL 0.524.0.0. Non validé en jeu.
### English
- Ctrl+Alt now refreshes the hovered price: the key press lasted one tick while the 5 s bound was used up on every tick, so they almost never met. The fetched time now shows seconds.

## 0.523.0
- Rapport de l'utilisateur en jeu (capture, Noix de coco et Rondin de palmier collectionnables) : l'aide à la récolte ne donnait pas les assignats pour choisir le meilleur. Chaque collectionnable du nœud montre ses assignats par palier, et celui qui paie le plus au palier haut est marqué.
- Le marquage ne compare que des assignats du même type (colonne Currency de la feuille) ; sinon le panneau le dit. Le nom du type reste inconnu, comme relevé en 0.409 (`docs/evidence/0.409-collectables.md`).
- Tests : GpCrystals519Tests complété. Verify.ps1 (agent) : 991/991, 0 avertissement, 0 erreur ; DLL 0.523.0.0. Non validé en jeu.
### English
- The gathering help now shows each collectable's scrips per tier and marks the one paying the most at the high tier, between scrips of the same kind only.

## 0.522.0
- Rapport de l'utilisateur en jeu (capture) : Bénédiction de la nature restait proposée pendant son temps de recharge, 180 s d'après la feuille du jeu ([preuve](../../docs/evidence/0.519-crystal-actions.md)). Elle n'est plus conseillée tant qu'elle recharge, ses PR restent pour Bénédiction des Douze, et une ligne dit dans combien de secondes elle revient. Temps lu dans le jeu ; s'il ne peut pas être lu, rien ne change.
- Parmi les actions à PR de récolte, c'est la seule à avoir un temps de recharge : le plan des autres objets n'est pas touché.
- Tests : GpCrystals519Tests complété. Verify.ps1 (agent) : 990/990, 0 avertissement, 0 erreur ; DLL 0.522.0.0. Non validé en jeu.
### English
- Nature's Bounty is no longer advised during its 180 s recast: its GP stays for The Twelve's Bounty, and a line says when it is back.

## 0.521.0
- Rapport de l'utilisateur en jeu : Bénédiction des Douze change aussi la quantité sur les cristaux, comme Bénédiction de la nature, alors que le texte du jeu ne parle que des éclats. Le panneau propose les deux sur les éclats, les cristaux et les agrégats, selon le niveau et les PR.
- Tests : GpCrystals519Tests adapté. Verify.ps1 (agent) : 989/989, 0 avertissement, 0 erreur ; DLL 0.521.0.0. Non validé en jeu.
### English
- The Twelve's Bounty and Nature's Bounty are now both proposed on shards, crystals and clusters, as reported in game.

## 0.520.0
- Rapport de l'utilisateur en jeu (capture de Sagesse du fermier, « pas pris en compte ») : le plan des collectionnables comptait la tentative rendue, mais pas son effet additionnel, 50 % de chances d'une tentative gratuite (niveau 90 et plus). Sur la capture : 136 en moyenne, soit 4 × 34.
- La moyenne le compte maintenant (153 sur ce cas), jamais le pire cas (136) ; une ligne du panneau le dit. Si Étalage des connaissances est déjà prêt (statut 2765), la tentative gratuite compte pour de bon et passe avant la dernière récolte, qui fermerait le nœud.
- Lecture de l'utilisateur à confirmer : c'est bien l'effet à 50 % qui manquait, la tentative elle-même figurait déjà au plan.
- Tests : Gathering478Tests complété, dont le cas de la capture. Verify.ps1 (agent) : 989/989, 0 avertissement, 0 erreur ; DLL 0.520.0.0. Non validé en jeu.
### English
- The collectable plan now counts Solid Reason's and Ageless Words' 50 % chance of a free attempt in its average, never in its worst case. A free attempt already ready counts for sure and comes before the last gather.

## 0.519.0
- Rapport de l'utilisateur en jeu (captures) : sur un Cristal de vent, le plan proposait Récolte royale II et Extraction abondante ×3, « ≈ 15 objets », alors que le jeu en donne 8 à 30 par récolte et que les actions propres aux cristaux n'étaient pas proposées ([preuve](../../docs/evidence/0.519-crystal-actions.md)).
- Éclats, cristaux et agrégats (catégorie d'objet du jeu) : le panneau propose Bénédiction de la nature, et Bénédiction des Douze pour les éclats seulement comme le dit son texte, selon le niveau et les PR. Aucune quantité n'est calculée : le jeu ne donne pas de chiffre.
- Tests : GpCrystals519Tests. Verify.ps1 (agent) : 988/988, 0 avertissement, 0 erreur ; DLL 0.519.0.0. Non validé en jeu.
### English
- Shards, crystals and clusters now get their own actions in the gathering help (Nature's Bounty, and The Twelve's Bounty on shards), with no computed amount: the game gives no figure, and the old estimate was far from the real yields.

## 0.518.0
- Rapport de l'utilisateur en jeu (capture) : carte, point d'Ostrakon Dékaokto « ouvert maintenant » alors qu'il n'est pas là. Cause : la ligne ne parlait que des heures ; ce point légendaire demande un tome ([preuve](../../docs/evidence/0.518-gather-tomes.md)).
- Infobulle et liste de la carte : « ouvert, mais pas encore pour vous » et le nom du tome quand il n'est pas lu ; l'horloge du point passe en gris, et il sort des repères au bord de la carte. Si le niveau du métier est sous celui du point, le panneau l'écrit.
- Les tomes lus et les niveaux sont copiés toutes les dix secondes sur le thread du framework ; inconnu ne dit rien.
- Tests : GatherPointAccess518Tests. Verify.ps1 (agent) : 986/986, 0 avertissement, 0 erreur ; DLL 0.518.0.0. Non validé en jeu.
### English
- Map: a legendary gathering point was shown « open now » although the character has not read its tome. The tooltip now says so and names the tome, its clock turns grey and it leaves the edge markers. It also says when the job's level is under the point's.

## 0.517.0
- Rapport de l'utilisateur en jeu (capture, « plan toujours pas fonctionnel ») : 809 PR, 54 %, aucune action proposée. Vraie cause, que 0.515 ne corrigeait pas : le filtre des actions non apprises (0.488) demandait à Dalamud si l'action était débloquée, et Dalamud répond non quand elle n'a pas de lien de déblocage, soit 22 actions sur 24 ([preuve](../../docs/evidence/0.517-gp-action-unlocks.md)). Elles comptent maintenant comme acquises au niveau.
- Même défaut pour les traits de regain de PR sans quête : ils comptent au niveau du métier.
- Sans Extraction abondante II (quête), le plan prend la première, qui existe toujours.
- Cristaux : le « ? » restait ; le jeu donne un tiret sans le signe %, il se lit maintenant « 0 % ».
- Tests : GatherNodeEffects515Tests complété, dont le cas de la capture. Verify.ps1 (agent) : 984/984, 0 avertissement, 0 erreur ; DLL 0.517.0.0. Non validé en jeu.
### English
- The GP plan proposed nothing since 0.488: Dalamud answers « not unlocked » for an action without an unlock link, and 22 of the 24 gathering actions have none. They now count as learned at their level, and GP regeneration traits without a quest likewise.
- Without Bountiful Yield II the plan uses the first one. A crystal's dash now reads as no extra chance.

## 0.516.0
- Décision de l'utilisateur (issue #38, règle Dalamud : « si ça ressemble à une fenêtre, utiliser l'API de fenêtres ») : les panneaux accrochés à une fenêtre du jeu passent aussi par le système de fenêtres de Dalamud : import de banque, Personnage, collectionnable, carnet d'artisanat, projection, outil de mission, Revue de mode, pêche, récolte, bouton de tri, survol d'objet, pêche en mer, ventes des servants, harpon, demande de Triple Triade.
- Comment : un hôte commun (`AttachedPanels`). Chaque panneau décide encore à chaque image s'il s'affiche, puis demande sa fenêtre ; leur contenu ne change pas, mêmes identifiants. Dans `Plugin.Draw`, les panneaux passent maintenant avant `windows.Draw()`. Échap ne les ferme pas, ils ne s'épinglent pas, et une erreur ne coupe que le panneau concerné. Seule exception : les zones de clic invisibles de la Revue de mode, qui ne ressemblent pas à une fenêtre.
- Tests : AttachedPanels516Tests (aucun `ImGui.Begin` hors de ces zones). Verify.ps1 (agent) : 982/982, 0 avertissement, 0 erreur ; DLL 0.516.0.0. Non validé en jeu. Sauvegarde : `checkpoints/backup-20261104-224219-before-panel-windows.zip`.
### English
- The panels attached to a game window now use the Dalamud window system too, through one shared host. Their contents and ids are unchanged; Escape does not close them, and a panel that throws turns off alone. Only exception: the invisible click zones of the Fashion Report.

## 0.515.0
- Rapport de l'utilisateur en jeu (capture, 5 tentatives, effet du lieu « PR ≧ 770 → tentatives +1 ») : l'aide à la récolte ne proposait rien avec 809 PR. Cause : le plan sur la liste de fabrication gardait les PR pour un « nœud à bonus majeur », et le premier nœud vu comptait toujours comme normal.
- Décision de l'utilisateur (DEC-036) : plus de plan sur une série de nœuds. Retirés : répartition des PR, « garder les PR pour les nœuds à bonus », secondes entre deux nœuds (`GpRoutes`, `GatherNodeNorms`, 2 réglages). Le panneau montre le meilleur plan du nœud en cours. La ligne de la liste de fabrication reste, à la demande de l'utilisateur : ce qu'il manque et le nombre de nœuds comme celui-ci, à titre d'information.
- Tous les effets du lieu lus dans les feuilles du jeu, dans ses mots ([preuve](../../docs/evidence/0.515-node-effects.md)) : actif, le panneau le dit (tentatives : « déjà compté dans le plan, 5 au lieu de 4 ») ; inactif, il dit ce qu'il manque et, pour les tentatives, la quantité, la chance supplémentaire et le taux de récolte, le rendement avec lui ; une condition illisible par Tensho est affichée sans état. La condition du jeu porte sur les PR maximum, donc dépenser ne la perd pas (texte anglais du jeu, « Max GP » ; le français dit seulement « PR »).
- Cristaux : le jeu écrit « -% » pour la chance supplémentaire ; c'est aucune, plus « ? », et leur plan se calcule.
- Tests : GatherNodeEffects515Tests, tests de route retirés. Verify.ps1 (agent) : 979/979, 0 avertissement, 0 erreur ; DLL 0.515.0.0. Non validé en jeu.
### English
- The gathering help no longer plans GP over a series of nodes: it shows the best plan for the node in front of you. This fixes a plan that spent nothing with 809 GP on a node with an extra attempt. The crafting list line stays, as information.
- All node effects are read from the game's sheets, in the game's words: an active one is named, an inactive one says what is missing and, when it can be estimated, what it would give. A crystal's « -% » extra chance now reads as none.

## 0.514.0
- Demande de l'utilisateur (issue #38, règle Dalamud : « si ça ressemble à une fenêtre, utiliser l'API de fenêtres ») : fenêtre de suivi, tâches à l'écran, barre d'usure et barre de PR passent par le système de fenêtres de Dalamud (`PanelWindow`). Mêmes identifiants, donc positions gardées ; mêmes réglages. Échap ne les ferme pas (cela couperait le réglage). Une erreur ne coupe que le panneau concerné, comme avant, et le temps de dessin reste dans Diagnostics.
- Vu en passant : pour le guide et la carte, `Plugin.Draw` recopie l'état dans `IsOpen` avant que `OnClose` puisse s'exécuter, ce qui peut annuler la croix de fermeture. L'état est maintenant écrit dès `PostDraw`, dans la même image. Déduit de la lecture du code et de la mémoire du fonctionnement de Dalamud, non observé en jeu.
- Reste de l'issue #38 : relire les panneaux accrochés à une fenêtre du jeu.
- Tests : PanelWindows514Tests. Verify.ps1 (agent) : 976/976, 0 avertissement, 0 erreur ; DLL 0.514.0.0. Non validé en jeu.
### English
- The tracker window, the on-screen tasks, the gear wear bar and the GP tick bar now use the Dalamud window system. Ids and settings are unchanged, so saved positions stay. Escape does not close them, and a panel that throws still turns off alone.
- The guide and the map write their close button back in the same frame, so the next draw cannot undo it.

## 0.513.0
- Question de l'utilisateur : « pourquoi toucher aux alarmes ? ». Erreur de l'agent en 0.512 : les documents disaient que les alarmes du jeu partaient sans clic. Relu dans le code : les commandes `/alarm` partent toujours d'un clic (ajouter une alarme avec le réglage actif, ou « Envoyer ») et les alarmes automatiques n'en créent jamais. Aucun changement de code ; manifeste, PUBLISHING, SECURITY, CONTRIBUTING et brouillon de PR corrigés.
- Tests : aucun changement de logique. Verify.ps1 (agent) : 972/972, 0 avertissement, 0 erreur ; DLL 0.513.0.0. Non validé en jeu.
### English
- Documents corrected: game alarms are only ever sent from a click (adding an alarm with the setting on, or Send); automatic alarms never create one. No code change.

## 0.512.0
- Demande de l'utilisateur : remettre tous les .md et GitHub à l'étape actuelle, et relever ce qui manque pour la 1.0.0.
- Compilation chez Dalamud : leur conteneur utilise le SDK 10.0.101 sous Linux, réseau coupé ; `global.json` exigeait 10.0.403, Tensho n'y aurait pas compilé. Il accepte maintenant 10.0.100 et plus, `Verify.ps1` prend le SDK 10.x local le plus récent, et un job CI compile avec leur image et leur commande ([relevé](../../docs/evidence/0.512-d17-build.md), DEC-034).
- Manifeste : Description réécrite (lignes coupées à la main, fonctions et réseau à jour, les deux réglages qui agissent sans clic sont nommés). `manifest.toml` : changelog à jour. Brouillon de PR relu contre le code.
- Documents : STATUS raccourci, son historique déplacé dans `docs/ai/STATUS-ARCHIVE.md` ; GOAL, PUBLISHING (liste de ce qui reste, en ordre), ROADMAP, BACKLOG, TESTING, SECURITY, CONTRIBUTING, README (crédits : Raphael.Net, jardinage), CLAUDE.md, RULES, DECISIONS remis à jour.
- Tests : aucun changement de logique. Verify.ps1 (agent) : 972/972, 0 avertissement, 0 erreur ; DLL 0.512.0.0. Non validé en jeu.
### English
- Build: the Dalamud repository compiles with SDK 10.0.101 on Linux, and `global.json` demanded 10.0.403, so the plugin could not have built there. It now accepts 10.0.100 and newer, and a new CI job builds with their image and command.
- The manifest description is rewritten (wrapped by hand, up to date, naming the two settings that act without a click). All documents and GitHub issues now match the current stage; what remains for 1.0.0 is listed in `docs/project/PUBLISHING.md`.

## 0.511.0
- Relecture du code de l'après-midi, sans essai en jeu. Deux défauts corrigés : la page Aide restait dans l'ancienne langue après un changement de langue ; la liste des fenêtres sur l'accueil parcourait les collections à chaque image, elle ne le fait plus que toutes les deux secondes.
- Tests : GuideFlowTests complété. Verify.ps1 (agent) : 972/972, 0 avertissement, 0 erreur ; DLL 0.511.0.0. Non validé en jeu.
### English
- The Help page follows a language change. The Home checklist no longer scans the collections on every frame.

## 0.510.0
- Décisions de l'utilisateur (DEC-033). Cadre des fenêtres du jeu retiré, code, réglage et textes (« rendu horrible »).
- Conseils en JcE seulement : les conseils de match et les estimations de défi de Triple Triade ne valent plus que contre un PNJ, jamais au Battlehall, en tournoi ni contre un joueur. Le panneau le dit. Preuve : docs/evidence/0.510-triad-competitive.md.
- Tests : TriadMatchAdvisorTests complété, GameFrame332Tests retiré. Verify.ps1 (agent) : 971/971, 0 avertissement, 0 erreur ; DLL 0.510.0.0. Non validé en jeu.
### English
- The game window frame option is removed. Triple Triad advice and odds now only apply against an NPC, never in the Battlehall, in a tournament or against a player.

## 0.509.0
- A-04 (DEC-028, issue #28), et suite de la remarque de l'utilisateur sur le bon coup selon l'état. « Comparer les prochains coups » conseille un coup, calculé d'après la synthèse telle qu'elle est : chaque coup permis par le jeu est joué sous l'état en cours, puis la suite est cherchée en condition normale. Affichage seulement, rien n'est joué. Hors du thread du jeu, annulé dès que l'état change, avec un réglage. Les actions interdites et les coups à chance ne sont pas proposés.
- Limites : seul ce coup tient compte de l'état, le combo en cours est pris comme absent, et c'est une recherche limitée, pas une preuve. Le coût réel en jeu n'a pas été mesuré.
- Tests : CraftProjectionTests complété ; CraftSolver découpé pour partir de n'importe quel état, ses tests existants passent. Verify.ps1 (agent) : 971/971, 0 avertissement, 0 erreur ; DLL 0.509.0.0. Non validé en jeu.
### English
- During a synthesis, the comparison now suggests a next move, worked out from the current state with the plugin's own search. Display only; it plays nothing. It has a setting and respects forbidden actions.

## 0.508.0
- I-05 (issue #26), première version : quand la liste de ventes d'un servant est ouverte, un panneau affiche vos objets et vos prix, tels que le jeu les montre. « Comparer aux autres vendeurs » demande à Universalis, sur clic seulement, les offres de votre monde d'origine : autre vendeur le plus bas, état de chaque ligne (sous-coté, même prix, le plus bas, aucun autre vendeur, inconnu). Un bouton copie le prix conseillé, rien d'autre n'est écrit.
- Lecture seule : aucun prix n'est modifié, rien n'est déplacé. Vos propres servants ne comptent pas. Absence de données ou liste coupée : « inconnu », jamais « le plus bas ».
- Écarts avec la conception, après sa relecture : prix lus dans la liste affichée et non dans le tableau partagé, requête sur clic seulement (décision laissée à l'utilisateur, G-PROPOSALS), service de requête à part. Preuve : docs/evidence/0.508-retainer-sales.md.
- Tests : RetainerSalesTests (5 cas). Verify.ps1 (agent) : 969/969, 0 avertissement, 0 erreur ; DLL 0.508.0.0. **Non validé en jeu : la fenêtre n'a jamais été ouverte avec ce code.** Le journal note ce qu'il lit, pour le vérifier au premier essai.
### English
- Retainer sales, first version: beside a retainer's sale list, your items and prices as the game shows them. On a click, it compares them with other sellers through Universalis and can copy a price. It changes nothing in the game. Not validated in game yet.

## 0.507.0
- R-14 : fin de la relecture des constats de l'audit, un par un contre le code. Beaucoup étaient déjà corrigés par 0.452 à 0.454 (alarme, escouade, rappels, mandats, carte, macros, monnaies). Restaient, et sont corrigés :
  - Une feuille facultative ne coûte plus que sa valeur : statistiques sans ParamGrow, rangements sans World, équipement sans taux de surmatérialisation ni bardes, survol sans la pile, une garde par feuille sur la carte (les marques de chasse n'y passaient plus en silence).
  - Un lien cassé saute sa ligne, pas tout le catalogue : recettes, contenus, vendeurs.
  - Une fonction perdue est dite ou refusée, une ancienne lecture ne reste pas affichée comme actuelle : demande de progression d'un succès, ouverture du carnet, état des actions, recettes fabriquées, mandats, carnet de chasse (rien avant le chargement du personnage), armoire. Une téléportation ratée ne vide plus la carte. Cartes de région : tout ou rien.
  - Le cache suit aussi la version de Dalamud, et une capture de fenêtre n'est plus coupée par un saut de ligne.
- Laissés en l'état, jugés sans effet réel : #35 (lecture de la pile, validée en jeu par l'utilisateur, le constat ne se vérifie pas), #5, #27, #29, #31, #44, #47, #64, #65, #66, #67, #70 (journal seulement, ou panneau déjà isolé par 0.452). Liste dans l'issue #25.
- Tests : Resilience507Tests (4 cas). Verify.ps1 (agent) : 964/964, 0 avertissement, 0 erreur ; DLL 0.507.0.0. Non validé en jeu : ces cas ne surviennent qu'après une mise à jour qui casse une feuille ou une fonction ; l'auto-contrôle couvrira ce jour-là.
### English
- R-14: the audit's findings read one by one. An optional sheet now costs only its own value, a dangling link skips its row rather than the whole catalog, a lost function is refused or logged, and old readings are no longer shown as current.

## 0.506.0
- ONBOARD-001, lots 7 et 8. Vidéos et guides de la communauté : `community-help/index.json`, vide pour commencer, proposé en pull request et vérifié par un test. Le « ? » et la page Aide le lisent sur clic. Un lien part vers le navigateur après confirmation de son adresse, seulement vers YouTube ou ce dépôt (G6). Une liste cassée est refusée en entier. Visible après la prochaine release, puisqu'elle est lue sur `main`.
- Contributeurs : `scripts/New-Tool.ps1` crée les textes d'aide d'un nouvel outil ; CONTRIBUTING explique l'aide, les vidéos et les guides ; `community-help/README.md` donne les règles.
- Tests : HelpTests complété. Verify.ps1 (agent) : 960/960, 0 avertissement, 0 erreur ; DLL 0.506.0.0. Non validé en jeu. `New-Tool.ps1` lancé pour de vrai sur une copie du dépôt : les textes d'aide sont créés et les tests passent (961/961).
### English
- Community videos and guides: a reviewed list in community-help/, read on a click from the ? and the Help page; a link opens in the browser after showing its address, only to YouTube or this repository. New-Tool.ps1 now creates a new tool's help texts, and CONTRIBUTING explains help and guides.

## 0.505.0
- ONBOARD-001, lots 4 à 6. Un guide des premiers pas : bienvenue et langue, sources en ligne avec leur interrupteur (défauts inchangés, G3), quelques réglages, intérêts (G4), fenêtres du jeu à ouvrir une fois, visite des pages, fin. On le quitte à chaque étape, il reprend là où il en était, depuis Aide. Nouvelle installation : il s'ouvre une fois à la première ouverture de Tensho. Joueur existant : une proposition unique sur l'accueil (G2).
- Fenêtres à ouvrir : Tensho coche d'après ce que le jeu a réellement donné, et n'ouvre rien à la place du joueur. Ce qui reste figure sur l'accueil.
- Masqué en combat et en mission. « Tout remettre par défaut » ne relance pas le guide.
- Tests : GuideFlowTests (6 cas). Verify.ps1 (agent) : 958/958, 0 avertissement, 0 erreur ; DLL 0.505.0.0. Non validé en jeu, et la vraie première installation n'a pas pu être essayée.
### English
- First-steps guide: welcome and language, online sources, a few settings, interests, game windows to open once (ticked from what the game really gave), a look around. It can be left at any step and resumed from Help. It opens once on a new install, and is only offered once to anyone else.

## 0.504.0
- ONBOARD-001, lot 3 (G5). Aide › Signaler un problème : bug, idée ou traduction. Le rapport de diagnostic et l'auto-contrôle sont visibles et modifiables. « Ouvrir le formulaire GitHub » ouvre le navigateur sur le bon formulaire, rempli. Le plugin n'envoie rien et ne garde aucun jeton. Trop long : le texte passe dans le presse-papiers. « Déjà signalé ? » cherche dans le navigateur. Dossiers et adresses Discord sont retirés. Un problème de sécurité renvoie vers le signalement privé.
- Tests : HelpTests complété. Verify.ps1 (agent) : 952/952, 0 avertissement, 0 erreur ; DLL 0.504.0.0. Non validé en jeu : le préremplissage vient de la documentation de GitHub, aucun formulaire réel n'a été ouvert.
### English
- Report a problem from the game: a form that opens the project's GitHub form filled in, in the browser. The plugin sends nothing and holds no token. The report is visible and editable first.

## 0.503.0
- ONBOARD-001, lot 2. Première visite d'une page : un bandeau de deux lignes, une seule fois, avec « Compris » et « En savoir plus » ; réglage dans Paramètres › Général (G8). Le « ? » dit aussi où en est la page sur ce personnage (tout lu, en attente, inhabituel, échec), d'après l'auto-contrôle.
- Tests : HelpTests complété. Verify.ps1 (agent) : 950/950, 0 avertissement, 0 erreur ; DLL 0.503.0.0. Non validé en jeu.
### English
- First-visit help on each page, once, with a setting. The "?" also says what the page has read on this character, from the self-check.

## 0.502.0
- Dresseur mis en attente sur décision de l'utilisateur. Suite de la feuille de route : prise en main (ONBOARD-001), lot 1.
- Aide à chaque page : un « ? » en haut de la fenêtre explique la page ouverte (à quoi elle sert, comment commencer, que faire si elle reste vide). Nouvelle page Préférences › Aide : recherche, questions fréquentes, toutes les pages, rapport de bug. Un test refuse une page sans ses textes, en anglais et en français.
- Limite : les textes viennent du code et des tests ; ils n'ont pas tous été rejoués en jeu.
- Tests : HelpTests (3 cas). Verify.ps1 (agent) : 948/948, 0 avertissement, 0 erreur ; DLL 0.502.0.0. Non validé en jeu.
### English
- Help on every page: a "?" explains the open page, and a new Help page searches them all, with frequent questions and the bug report. A test refuses a page without its help texts.

## 0.501.0
- Décision de l'utilisateur (DEC-032) : pour le dresseur, un panneau d'informations pour le Dédale, en JcE seulement, rien en JcJ. Pas encore codé : il lui faut le rang des bêtes et la faiblesse des ennemis.
- Sonde en mode développeur : quand une fenêtre du dresseur est ouverte (fiche du bestiaire, formation de l'équipe, fiche d'un ennemi, plateau), ses textes et ses valeurs vont au journal, à chaque changement, en quantité limitée. Lecture seule. Les fenêtres de classement et de résultat sont exclues : elles nomment d'autres joueurs ou le personnage.
- Tests : BeastCatalogTests complété. Verify.ps1 (agent) : 945/945, 0 avertissement, 0 erreur ; DLL 0.501.0.0. Non validé en jeu.
### English
- Read-only developer probe for the Beastmaster windows, to find the rank of each beast and enemy weakness. Ranking and result windows are left out. Team help will be information only, for this PvE content.

## 0.500.0
- Demande de l'utilisateur : le dresseur et ses bêtes manquaient. Nouvelle page Progression › Dresseur, en lecture seule. Le conseil d'équipe viendra ensuite : ce qui rend une bête meilleure contre un ennemi n'est pas établi.
- Bêtes : les 50, obtenues ou non (lu dans le jeu), classe, attaque automatique, textes d'Assaut et de Salve, cinq attributs au rang choisi, tri par colonne, recherche, classe. Plateaux : niveau, taille d'équipe, rang de synchronisation, ennemis et actions, gardien à part, bonus avec leurs points.
- Les feuilles sont lues brutes, car les lignes typées de ce Lumina.Excel ne correspondent pas au jeu ; une autre disposition des colonnes donne une erreur affichée, jamais de fausses valeurs. Colonnes confirmées sur les captures de l'utilisateur (docs/evidence/0.500-xbm-columns.md).
- Limites : la faiblesse des ennemis, les formes et les cibles des actions sont lues comme telles, non confirmées, et la page le dit. Le rang de chaque bête et sa taille ne sont pas lus.
- Tests : BeastCatalogTests (5 cas). Verify.ps1 (agent) : 944/944, 0 avertissement, 0 erreur ; DLL 0.500.0.0. Passe automatique en jeu : 50 bêtes, 35 obtenues, comme les 35/50 de la capture du bestiaire ; 5 plateaux, 122 ennemis, aucune erreur au journal. Affichage de la page non validé en jeu.
### English
- New read-only Beastmaster page: the 50 beasts with class, attacks and attributes per rank, and the boards of the labyrinth with enemies and score bonuses. Team advice comes later. Enemy weakness is read as such and not yet confirmed.
