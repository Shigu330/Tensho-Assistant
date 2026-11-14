# Journal des versions / Changelog, 0.400 à 0.499

Archive de [CHANGELOG.md](../../CHANGELOG.md) : versions plus anciennes que la dernière release, déplacées par `tools/changelog.py archive`. Même format (français, puis `### English` depuis 0.219.0) ; liens relatifs à la racine réécrits depuis ce dossier.

## 0.499.0
- Demande de l'utilisateur : un clic sur un lien vers la carte doit la faire passer au-dessus de Tensho. Elle restait derrière, surtout quand elle était déjà ouverte. Elle passe maintenant devant, une fois par demande.
- Tests : SelfCheckTests complété. Verify.ps1 (agent) : 939/939, 0 avertissement, 0 erreur ; DLL 0.499.0.0. Non validé en jeu.
### English
- A link to the map now brings it above the Tensho window.

## 0.498.0
- Demande de l'utilisateur : quand Tensho est replié et qu'on lance quelque chose qui l'ouvre, il doit se rouvrir. Tout ce qui ouvre la fenêtre ou change sa page la déplie, pendant deux images ; elle se replie ensuite normalement.
- `tools/XbmProbe` ajouté : sonde en lecture seule des feuilles du dresseur (XBM), lues brutes, car les lignes typées de ce Lumina.Excel ne correspondent pas toutes au jeu.
- Tests : SelfCheckTests complété. Verify.ps1 (agent) : 938/938, 0 avertissement, 0 erreur ; DLL 0.498.0.0. Non validé en jeu.
### English
- The Tensho window unfolds when something asks for it while it is collapsed. New read-only probe tools/XbmProbe for the Beastmaster sheets.

## 0.497.0
- Rapport de l'utilisateur en jeu : « Comparer les prochains coups » ne vérifiait pas l'état pour prendre le bon coup. Relu dans le code : les chiffres utilisaient déjà l'état (Bon ×1,5 ou 1,75 selon l'outil, Excellent ×4, Mauvais ×0,5), mais rien ne l'affichait, et les coups permis par l'état restaient mêlés aux autres : une action comme Ficelles du métier, sans gain de qualité, finissait en bas.
- La fenêtre dit maintenant l'état du jeu, son effet et l'état suivant. Les coups possibles seulement sur Bon ou Excellent passent en tête, marqués « Seulement dans cet état ». Ce n'est pas un conseil de rotation.
- Non vérifié : je n'ai pas vu le cas signalé en jeu. Si le jeu refuse un de ces coups, il n'apparaît pas ; le mode développeur note à chaque état ce que le jeu répond pour eux.
- Tests : CraftProjectionTests complété. Verify.ps1 (agent) : 937/937, 0 avertissement, 0 erreur ; DLL 0.497.0.0. Non validé en jeu.
### English
- "Compare the next moves" now shows the state of the synthesis, what it does and what comes next. Actions only allowed in that state come first and are marked.

## 0.496.0
- Rapport de l'utilisateur en jeu : l'auto-contrôle n'enregistrait pas les OK. Pendant une synthèse, « synthesis » passait bien en OK et « crafting log » retombait en NOT_READ, car le jeu ferme le carnet : aucune passe ne pouvait tout montrer.
- Nouvel état OK_SEEN : pas lisible maintenant, mais bien lu plus tôt, avec l'heure. En mode développeur, les 5 lignes de fenêtres sont surveillées chaque seconde : un OK est noté dès que la fenêtre est ouverte, sans lancer de passe. Gardé dans SelfCheckSeen.json, y compris après un rechargement. Une réponse en direct passe toujours avant (FAILED et SUSPECT ne sont jamais masqués) et une autre version des données du jeu efface tout, pour qu'un ancien OK ne cache pas une panne après une mise à jour.
- Tests : SelfCheckTests complété. Verify.ps1 (agent) : 936/936, 0 avertissement, 0 erreur ; DLL 0.496.0.0. En jeu, le premier essai comptait à tort l'état Waiting comme OK : corrigé avant livraison (seul Observed compte), le fichier faux effacé. Le reste n'est pas validé en jeu.
### English
- Self-check: a new OK_SEEN state keeps what was read well earlier, with its time. In developer mode the game's windows are watched once a second and the result survives a reload; a new version of the game's data clears it.

## 0.495.0
- Rapport de l'utilisateur en jeu : carnet d'artisanat et carnet de récolte ouverts, l'auto-contrôle disait « craft window » et « gathering window : window closed ». La lecture était juste : ces lignes surveillent la synthèse en cours et un point de récolte ouvert, pas les carnets. Le libellé trompait.
- Lignes renommées d'après ce qu'elles surveillent : synthesis, gathering node, triad match, cactpot ticket, avec le motif (« no synthesis running »…). Nouvelle ligne « crafting log », lue par l'aide du carnet. Le carnet de récolte n'a pas de ligne : Tensho l'ouvre, mais n'y lit rien.
- Tests : SelfCheckTests complété. Verify.ps1 (agent) : 934/934, 0 avertissement, 0 erreur ; DLL 0.495.0.0. Passe automatique en jeu, carnet d'artisanat ouvert chez l'utilisateur : « crafting log OK, recipe 5623 », 26 OK, 4 NOT_READ, 0 SUSPECT, 0 FAILED. Le texte d'aide de la section passe à la ligne.
### English
- Self-check: the lines are named after what they watch (synthesis, gathering node, triad match, cactpot ticket), and a new line reads the crafting log.

## 0.494.0
- Demande de l'utilisateur : un auto-contrôle, pour que l'agent vérifie sans lui ce que le plugin lit. Une passe en lecture seule écrit dans le journal une ligne par domaine (catalogues, personnage, sacs, quêtes, collections, Triple Triade, carte, ensembles, carnets, mandats, monnaies, rappels, fenêtres du jeu), avec OK, NOT_READ, SUSPECT ou FAILED, et un résumé.
- Se lance seule en mode développeur, une fois par connexion et après chaque rechargement ; sinon `/tensho selftest` ou le bouton de Diagnostics. Une sonde par image. Aucune action de jeu, aucune requête réseau, des nombres seulement (ni nom ni identifiant, chemins retirés des erreurs).
- Limites : plausible ne veut pas dire juste ; ni l'affichage ni les clics ne sont couverts ; les fenêtres fermées restent en NOT_READ.
- Tests : SelfCheckTests (4 cas). Verify.ps1 (agent) : 934/934, 0 avertissement, 0 erreur ; DLL 0.494.0.0. Exécuté en jeu sans action de l'utilisateur : 25 OK, 4 NOT_READ (les fenêtres fermées), 0 SUSPECT, 0 FAILED, aucune erreur au journal (docs/evidence/0.494-selfcheck-first-pass.log). Triple Triade : 45 PNJ battus, aucun marqué « Quête requise », ce qui ne contredit pas la 0.493 sans la prouver. Le reste de la version n'est pas validé en jeu.
### English
- Self-check: a read-only pass writes to the log what each part of the plugin has read, as numbers, with OK, NOT_READ, SUSPECT or FAILED. It runs by itself in developer mode, or with /tensho selftest and a button in Diagnostics.

## 0.493.0
- Rapport de l'utilisateur : Triple Triade ne détectait pas si un PNJ était vraiment disponible. La liste ne regardait que « battu » et les cartes manquantes, alors que la feuille TripleTriad du jeu liste les quêtes demandées (108 PNJ sur 146).
- Page PNJ et cartes : colonne « Disponible » (Oui, Quête requise, ?), les quêtes au survol avec ✓, ces PNJ en dernier, case « Masquer les PNJ pas encore disponibles ». Cartes manquantes et clic droit de la carte : même mention. Si la quête qui ouvre le jeu n'est pas faite, la page le dit.
- Limites : seules les quêtes sont lues, le jeu ne donne aucune autre condition (horaires à 0 sur les 146 lignes). Quêtes non lues : « ? », jamais « Quête requise ». PreviousQuestJoin 2 = une des quêtes (trois quêtes de villes exclusives) ; 1 = toutes est déduit, à confirmer en jeu. Preuve : docs/evidence/0.493-triad-npc-requirements.md.
- Tests : TriadNpcCatalogTests complété. Verify.ps1 (agent) : 930/930, 0 avertissement, 0 erreur ; DLL 0.493.0.0. Non validé en jeu.
### English
- Triple Triad: the NPC list now shows whether an NPC is available, from the quests the game lists for it. Locked NPCs come last and can be hidden; the quests are in the tooltip. The map's Triple Triad tab says the same.

## 0.492.0
- R-14, 5 constats relus contre le code et confirmés. Après une mise à jour du jeu qui casserait une feuille, la page le dit au lieu de mentir :
  - Recettes, contenus, vendeurs : une copie vide est une erreur publiée, plus un catalogue vide qui se lit « aucune recette » ou « personne ne le vend » (les caches disque refusaient déjà ce cas).
  - Équipement et barde de chocobo : un catalogue impossible à construire le dit, plus « Chargement » pendant toute la session.
  - Ensembles du jeu : une lecture ratée le dit et retire les anciens ensembles, qui restaient affichés comme actuels.
- Tests : TriadMatchAdvisorTests complété. Verify.ps1 (agent) : 929/929, 0 avertissement, 0 erreur ; DLL 0.492.0.0. Non validé en jeu (ces cas ne se produisent qu'après une mise à jour qui casse une feuille).
### English
- R-14: empty recipe, duty and vendor copies are reported as failures instead of empty catalogs. The gear pages and gear sets say when their data could not be read instead of loading forever or showing old sets.

## 0.491.0
- Campagne de tests : close à la demande de l'utilisateur (« les dernières erreurs on verra en jeu ») ; les cases non cochées de TESTING restent ouvertes. Suite de la feuille de route : g2.
- R-14, les 4 constats « valeur peut-être fausse » restés sans vérification, relus contre le code et confirmés. Ce qui n'est pas lu reste inconnu au lieu de passer pour un zéro :
  - Triple Triade, match : une règle illisible (valeur absente, autre type, nom hors de la feuille) ne disparaît plus en silence ; pas de conseil, et le panneau le dit. Coup masqué sans sa carte imposée : pas de conseil non plus. La feuille des règles a sa propre garde : illisible, elle ne coupe plus toute l'observation.
  - Triple Triade, défi : les identifiants viennent de la feuille, plus d'une borne écrite en dur à 15. Règles illisibles : pas d'estimation au lieu de « Règles : aucune ». La preuve citée pour les valeurs 102–105 ne les montre pas (lignes de capture coupées à la valeur 8) : le code le dit, et le journal donne maintenant ces valeurs.
  - Carte : feuille non lue, ou plus de panoramas que le tableau de bits du jeu : [?] au lieu de « non découvert ». Une garde par feuille.
  - Coffre à apparence : depuis la 7.4 une tenue peut être rangée incomplète. Seules les pièces que le jeu dit présentes comptent (MirageManager.IsSetSlotUnlocked). Aucune présente ou fonction indisponible : toutes comptent, comme avant, et le journal le note. Le sens de cette fonction n'est pas encore confirmé en jeu.
- Tests : TriadMatchAdvisorTests complété. Verify.ps1 (agent) : 928/928, 0 avertissement, 0 erreur ; DLL 0.491.0.0. Non validé en jeu.
### English
- R-14: what cannot be read stays unknown. Triple Triad rules not read give no advice and no odds instead of a no-rules answer; rule ids come from the sheet. The map shows unknown instead of "not discovered" when its sheets are unread. Outfits stored incomplete since 7.4 count only their stored pieces (not yet confirmed in game).

## 0.490.0
- Demande de l'utilisateur, lot 7 : les favoris ne se retiraient que depuis la page Cartes. Le clic droit de la carte a un onglet Favoris, en premier : ceux de cette carte, puis les autres, avec « Retirer ». Un clic pose le drapeau, ou ouvre l'autre carte.
- Tests : Gathering478Tests complété. Verify.ps1 (agent) : 926/926, 0 avertissement, 0 erreur ; DLL 0.490.0.0. Non validé en jeu.
### English
- The Tensho map's right-click menu has a Favorites tab, first, to go to a favorite or remove it.

## 0.489.0
- Rapport de l'utilisateur en jeu : la carte n'apparaissait pas dans les temps d'affichage. Elle a sa propre fenêtre, comptée ni comme page ni comme panneau. Diagnostics a maintenant une ligne « Map », en mode développeur.
- Tests : Gathering478Tests complété. Verify.ps1 (agent) : 925/925, 0 avertissement, 0 erreur ; DLL 0.489.0.0. Non validé en jeu.
### English
- Diagnostics now lists the draw time of the Tensho map.

## 0.488.0
- Rapports de l'utilisateur en jeu.
- Panneau Collectionnable qui clignote : le journal montre la réponse du jeu pour Raison solide qui change en quelques secondes (statut 579, puis 0, puis 579, avec le verrou de chaque action). Le plan en dépendait et se recalculait, et le panneau raccourcissait pendant ce temps. La réponse du jeu ne change plus le plan, elle dit seulement d'attendre, et le dernier plan reste affiché pendant le calcul. La détection de l'arrêt du tic n'était pas en cause.
- Raison solide n'est plus conseillée qu'à la dernière tentative, avant qu'elle ferme le nœud.
- Temps avant le plein faux : la mesure gardait la plus petite hausse jamais vue (2 PR). Une hausse ne compte plus que si elle arrive un tic après la précédente, et la dernière mesure remplace l'ancienne. Les mesures sur un nœud sont retirées.
- Demande de l'utilisateur : tenir compte des PR débloqués. Les traits Enhanced GP Regeneration ajoutent +1, +2 puis +3, métier par métier ([preuve](../../docs/evidence/0.488-gp-regen-traits.md)). La mesure donne la base, et le taux de chaque métier est cette base plus ses traits débloqués, lus dans le jeu. L'ancienne valeur mesurée est abandonnée.
- Demande de l'utilisateur : les aides suivent aussi ce qui est débloqué. Le plan de PR, la page PR et le plan de collection retirent les actions que le métier n'a pas apprises, lues dans le jeu (un niveau ne suffit pas, certaines viennent d'une quête), et le plan de collection celles que la fenêtre ne propose pas.
- Tests : Gathering478Tests complété. Verify.ps1 (agent) : 925/925, 0 avertissement, 0 erreur ; DLL 0.488.0.0. Non validé en jeu.
### English
- The collectable panel no longer blinks, Solid Reason and Ageless Words are advised at the last attempt only, and the time until full GP uses a measure of real ticks.

## 0.487.0
- Rapport de l'utilisateur en jeu : les temps des panneaux apparaissaient puis disparaissaient. Un panneau fermé enregistrait des temps nuls, qui chassaient ses vrais temps en 2 secondes. Seules les images réellement dessinées comptent maintenant, et un panneau garde ses derniers temps une fois fermé.
- Tests : Gathering478Tests complété. Verify.ps1 (agent) : 924/924, 0 avertissement, 0 erreur ; DLL 0.487.0.0. Non validé en jeu.
### English
- Diagnostics: a panel beside the game's windows keeps its last draw times once closed.

## 0.486.0
- Demande de l'utilisateur : barre de tic avec une petite marque qui défile et le temps avant le plein. La barre se remplit vers le maximum de PR, la marque défile en 3 s à chaque tic, le texte donne le temps restant d'après les PR par tic mesurés. Tant qu'ils ne sont pas connus, le texte reste « tic dans… ». Une mesure faussée par une action ou une potion peut fausser ce temps.
- Rapport de l'utilisateur : les temps d'affichage de Diagnostics ne comptaient que les pages. Les panneaux à côté des fenêtres du jeu ont maintenant leur section, en mode développeur.
- Tests : Gathering478Tests complété. Verify.ps1 (agent) : 924/924, 0 avertissement, 0 erreur ; DLL 0.486.0.0. Non validé en jeu.
### English
- The GP tick bar fills towards full GP, a small highlight runs along it at each tick, and its text gives the time until full.

## 0.485.0
- Demande de l'utilisateur : le texte de la barre de tic était coupé. Il affiche maintenant « PR 227/809 · pause ».
- Verify.ps1 (agent) : 922/922, 0 avertissement, 0 erreur ; DLL 0.485.0.0. Non validé en jeu.
### English
- The GP tick bar now just says « paused » when GP stops rising; the longer text was cut.

## 0.484.0
- Demande de l'utilisateur : prendre en compte le risque, avec une tolérance réglable dans le panneau d'aide. Curseur « Risque accepté » dans le panneau Collectionnable, et dans Paramètres › Modules › Récolte, 50 % par défaut. Chaque tirage compte pour un mélange de sa moyenne et de son pire résultat : à 0 %, le pire décide ; à 100 %, la moyenne seule. Le panneau annonce la moyenne et le résultat si tous les tirages tournent mal.
- Tests : Gathering478Tests complété. Verify.ps1 (agent) : 922/922, 0 avertissement, 0 erreur ; DLL 0.484.0.0. Non validé en jeu.
- Le calcul prenait 0,8 s dans le pire cas mesuré : il se fait maintenant hors de l'affichage.
- Rapports de l'utilisateur en jeu. Barre de tic : elle continuait de tourner alors que les PR ne remontaient plus, par exemple à l'ouverture d'une fenêtre de récolte. Au-delà de 4,5 s sans hausse, elle le dit, et repart à la hausse suivante. Les PR par tic se mesurent maintenant là, puisqu'ils ne remontent pas sur un nœud. Carte Tensho : au rechargement, la carte du jeu restée ouverte apparaissait avant que le catalogue soit prêt, et Tensho la gardait pour de bon. La décision attend maintenant jusqu'à 15 s. Une carte ouverte sans la carte du jeu (remplacement désactivé) ne revient pas seule.
### English
- The GP tick bar now says when GP has stopped rising, as at a gathering window. After a plugin reload, the Tensho map comes back by itself when the game map was hosted.
- The collectable plan takes risk into account: a « Risk accepted » slider in the panel sets how much the worst outcome of each roll counts against its average, and the panel tells both.

## 0.483.0
- Rapport de l'utilisateur en jeu : « Raison solide pas dispo au moment où il la demande ». La règle exacte n'est pas connue. Sous le maximum de tentatives, Tensho demande au jeu si l'action est utilisable (`ActionManager.GetActionStatus`, lecture seule) ; si le jeu refuse, elle n'est plus conseillée ni comptée. À tentatives pleines, le jeu ne peut pas encore répondre : le plan peut encore la compter pour plus tard, ce qui peut rester optimiste. Le journal note la réponse en mode développeur.
- Tests : Gathering478Tests complété. Verify.ps1 (agent) : 919/919, 0 avertissement, 0 erreur ; DLL 0.483.0.0. Non validé en jeu.
### English
- The collectable plan asks the game whether Solid Reason or Ageless Words can be used now, and stops counting on it when the game refuses.

## 0.482.0
- Rapport de l'utilisateur en jeu : « ne conseille pas l'action Inspection ». L'ordre des actions ne change pas la moyenne, et l'égalité donnait « pas de PR maintenant » : Inspection restait pour plus tard. Une action bonus qui rapporte et peut passer d'abord est maintenant le coup conseillé, Inspection puis Œil du collectionneur.
- Le gain affiché était celui de la case cochée du jeu (+352) au lieu du gain réel (+177) : corrigé. Le panneau indique aussi les Inspections du plan et leurs PR. Rapport « le texte fait 5 actions » pour 4 tentatives : la cinquième vient de Raison solide, et le panneau le dit maintenant (de 4 à 5 tentatives). Le plan limitait déjà ses actions aux PR disponibles ; le panneau montre maintenant ce qu'il dépense et ce qu'il reste, et mesure les PR regagnés. Les PR regagnés pendant la passe ne sont pas encore comptés dans le plan.
- Tests : Gathering478Tests complété. Verify.ps1 (agent) : 918/918, 0 avertissement, 0 erreur ; DLL 0.482.0.0. Non validé en jeu.
### English
- The collectable plan now advises Scrutiny, then Collector's Focus, first when they pay, instead of leaving them for later, and shows the real gain rather than the one of a ticked box.

## 0.481.0
- Rapport de l'utilisateur en jeu : « ne prend toujours pas en compte action inspection et œil ». Erreur de la 0.480 : les cases étaient lues à l'envers. Le journal montre que 54 est la case « Avec Inspection rigoureuse » (+175) et 56 la case « Avec Intuition » (+88) ([preuve](../../docs/evidence/0.479-collectable-window-values.md)). La valeur fausse apprise en 0.480 est abandonnée : il faut recocher les cases une fois.
- Inspection rigoureuse et Œil du collectionneur entrent dans le calcul, ainsi qu'Intuition, au pourcentage affiché par le jeu (valeur 58). Ce qui est annoncé comme certain ne compte pas sur Intuition. Réglage pour ne pas utiliser Œil. Le panneau est raccourci : la méthode passe dans une bulle.
- Contrôle sur une passe réelle de l'utilisateur (809 PR, 4 tentatives, 1000 atteint, 20 assignats, alors que la 0.480 prédisait 14) : le plan retrouve ce résultat, gardé comme test. Avec Raison solide, il annonce 21 assignats certains (3 récoltes au palier moyen) : cela suppose que le jeu accepte l'action comme le dit sa description, à vérifier.
- Tests : Gathering478Tests complété. Verify.ps1 (agent) : 918/918, 0 avertissement, 0 erreur ; DLL 0.481.0.0. Non validé en jeu.
### English
- Fixes the estimate boxes, read the wrong way round in 0.480. Scrutiny, Collector's Focus and Intuition now count in the collectable plan's average; what is announced as certain never relies on Intuition.

## 0.480.0
- **Plantage du jeu, causé par une version de travail de cette 0.480, jamais publiée en commit** : le journal de Windows donne `0xc00000fd` (dépassement de pile) à 10:14:09 dans `ffxiv_dx11.exe`. Le calcul du plan pouvait s'appeler sans fin avec une Sélection méthodique gratuite à la valeur maximale. Corrigé : le calcul s'arrête au seuil haut et au maximum, le nombre d'états est borné, un test couvre ces cas, et le calcul ne se refait que si quelque chose change, plus à chaque image.
- Demandes de l'utilisateur : « assignats max et récolte max comme référence », « le plus rentable en assignats sur un seul nœud ». Le panneau donne la référence (palier haut, assignats par récolte, au mieux sans chance sur ce nœud, ou ce qui manque). Les PR vont où ils rapportent le plus, Inspection rigoureuse ou tentative regagnée par Raison solide ou Sagesse du fermier (sa description dit qu'elle rend 1 d'intégrité sur un collectionnable, [preuve](../../docs/evidence/0.410-gp-actions.md)). Réglage pour ne pas regagner de tentatives. L'Eurêka n'est pas comptée.
- Rapport de l'utilisateur en jeu : « actions et effets bonus pas pris en compte ». Correction d'une erreur de lecture de la 0.479 : la valeur 54 n'est pas une Intuition active, c'est la case d'estimation « Avec Intuition » du jeu ([preuve](../../docs/evidence/0.479-collectable-window-values.md)). Une case cochée est retirée du plan, car son bonus peut ne pas arriver.
- Inspection rigoureuse entre dans le plan : son montant est lu quand sa case seule est cochée ou quand elle est active, son coût vient de la feuille du jeu, et les PR ne sont dépensés que si cela rapporte plus. Réglage pour ne pas l'utiliser. Œil du collectionneur est proposé en option quand une Intuition atteindrait le palier suivant, mais le plan n'y compte jamais. Toucher d'amorçage n'est pas compté.
- Rapport de l'utilisateur : « même à 8 % il tente le skill plutôt qu'instinctive ». La règle qui préférait Sélection méthodique dès qu'elle suffisait est retirée. Chaque coup est comparé sur ce qu'il rapporte en moyenne sur les tentatives restantes, avec le pourcentage affiché par le jeu et l'intervalle de Sélection instinctive, supposé égal partout (le jeu ne donne pas plus). Le panneau donne cette moyenne et ce qui est certain sans chance.
- Tests : Gathering478Tests complété. Verify.ps1 (agent) : 917/917, 0 avertissement, 0 erreur ; DLL 0.480.0.0. Non validé en jeu.
### English
- The collectable plan now counts Scrutiny once its bonus is read from the game's window, spends GP only if that earns more, and proposes Collector's Focus as an option. A ticked estimate box in the game is no longer taken as a certain bonus.

## 0.479.0
- Demandes de l'utilisateur, lot 6.
- Plan de collection : le relevé en jeu de la fenêtre Collectionnable a donné l'objet, la collectabilité en cours et le gain de chaque action à ce tour ([preuve](../../docs/evidence/0.479-collectable-window-values.md)). Le panneau montre la collectabilité, le palier, la prochaine action et le plan. Chaque découpage des tentatives est comparé sur ce qu'il rapporte. Le plan est refait à chaque tour, rien n'est exécuté. Inspection rigoureuse, Œil du collectionneur et Toucher d'amorçage ne sont pas comptés. Lecture fondée sur un seul relevé : si une valeur manque ou change de type, pas de plan. Le panneau distingue un seuil qu'un bon tirage peut encore atteindre et un seuil devenu impossible (rapport en jeu : 0 avec une seule tentative).
- Plan de PR sur la liste : un nœud normal récolte simplement et ne dépense que les PR qui seraient perdus au maximum ; un nœud à bonus majeur (plus de tentatives, quantité plus grande, chance supplémentaire nettement plus forte que le plus simple vu pour cet objet) dépense tout. Réglage pour revenir au partage égal. Le premier nœud vu compte comme normal. Le plan utilise maintenant la quantité affichée par le jeu au lieu de 1.
- Tests : Gathering478Tests complété. Verify.ps1 (agent) : 914/914, 0 avertissement, 0 erreur ; DLL 0.479.0.0. Non validé en jeu.
### English
- The collectable panel now shows the current collectability, its tier, the next action and a plan, from the values the game's window gives each turn.
- Over a crafting list, normal nodes keep their GP and a node with a major bonus spends it; the previous even sharing stays as a setting.

## 0.478.0
- Retours de l'utilisateur, lot 6.
- Panneau Collectionnable : « Aucun seuil » venait du nom coupé par la fenêtre du jeu (« Minerai de durium noble c... »), alors que les seuils existent (400 / 700 / 1000, [preuve](../../docs/evidence/0.478-collectable-truncated-name.md)). Un nom coupé est cherché par son début s'il désigne un seul objet, et le nom complet est affiché. Le texte dit qu'il n'y a rien à faire. La collectabilité en cours n'a pas de champ nommé dans le jeu : en mode développeur, les valeurs de la fenêtre vont au journal pour la trouver.
- Aide à la récolte : le nom, puis les taux dessous, et les textes vont à la ligne au lieu d'être coupés. Sur un collectionnable, un message remplace « Plan non calculé » : ses PR se dépensent dans la fenêtre Collectionnable.
- Collectionnables : onglet à part dans Récolte, tableau trié sur chaque colonne dans les deux sens.
- Plan de PR : prend la liste de fabrication choisie. Besoin en matières brutes (recettes suivies), moins le stock, puis nombre de nœuds et PR répartis, avec les PR regagnés entre deux nœuds. Hypothèses dites : chaque nœud comme celui-ci, moyenne, intermédiaires déjà faits non déduits. Les PR par tic sont mesurés en jeu, jamais supposés, ou réglés à la main. Actions interdites par réglage, pour le mineur et le botaniste, dans le panneau et la page PR.
- Tests : Gathering478Tests (6). Verify.ps1 (agent) : 911/911, 0 avertissement, 0 erreur ; DLL 0.478.0.0. Non validé en jeu.
### English
- Collectable panel: thresholds are found when the game's window cuts a long name, and the text says nothing is needed from the player. The gathering panel no longer cuts its lines.
- Collectables have their own sortable tab under Gathering.
- The GP plan can follow the selected crafting list: raw materials still missing, nodes needed, GP shared across them with regeneration; GP actions can be forbidden as for macros.

## 0.477.0
- Rapport de l'utilisateur en jeu : « aucun moyen de repasser au thème original ». Le retour n'existait que dans Réglages › Interface, à un autre endroit que le bouton qui change le thème. Le thème d'avant est maintenant retenu, et un bouton « Revenir au thème d'origine » reste en haut de « Mes outils » tant que le thème d'un outil est en place. Pour un thème appliqué en 0.476, rien n'avait été retenu : le bouton propose alors le réglage par défaut, suivre le jeu. Un choix dans les réglages oublie le thème retenu.
- Tests : MyToolsTests complété. Verify.ps1 (agent) : 905/905, 0 avertissement, 0 erreur ; DLL 0.477.0.0. Non validé en jeu. Reste de « Mes outils » validé par l'utilisateur en jeu (rapport « reste ok »), galerie exceptée puisqu'elle attend une release.
### English
- After using a tool's theme, a button at the top of « My tools » brings the original theme back.

## 0.476.0
- TEMPLATES-001 phase B7 (T8) : un outil peut proposer un thème, seul réglage de la liste blanche. L'aperçu le dit et le thème ne change que sur le clic « Utiliser le thème… ». Tout autre réglage dans un fichier le fait refuser. Le webhook Discord ne peut donc pas être touché par un outil. La partie sans code des modèles est terminée.
- Tests : MyToolsTests complété. Verify.ps1 (agent) : 905/905, 0 avertissement, 0 erreur ; DLL 0.476.0.0. Non validé en jeu.
### English
- A tool in « My tools » may propose a theme, the only setting allowed. It changes only on the player's click, and any other setting in a file refuses it.

## 0.475.0
- TEMPLATES-001 phases B5 et B6 : galerie d'outils communautaires. Dossier `community-templates/` (T4), licence CC0 (T5), deux outils d'exemple, `scripts/Update-ToolGallery.ps1` pour l'index, tests qui appliquent à chaque fichier les règles du plugin, relecture humaine par pull request (T7). Dans le plugin, la liste et un outil se lisent seulement sur clic (T6), avec taille bornée et empreinte vérifiée, puis passent par le même aperçu qu'un outil collé. Crédit et description du plugin mis à jour.
- La galerie lit la branche `main` : elle restera vide tant qu'une release n'a pas publié ce dossier.
- Tests : MyToolsGalleryTests (5). Verify.ps1 (agent) : 904/904, 0 avertissement, 0 erreur ; DLL 0.475.0.0. Non validé en jeu.
### English
- « My tools » gains a gallery of community tools, reviewed then published in `community-templates/` under CC0. The plugin reads it only on a click, bounded and checked against a fingerprint, and shows each tool in full before it is installed.

## 0.474.0
- TEMPLATES-001 phase B3 : éditeur de « Mes outils » dans le jeu, sans écrire de JSON. Les sept blocs, l'ordre, la licence libre CC0 à cocher, le texte écrit dans la langue du plugin avec les autres langues gardées. Enregistrer passe par le même format strict qu'un fichier partagé. Un objet s'ajoute depuis l'objet sélectionné dans Tensho, jamais par un identifiant tapé.
- Tests : MyToolsTests complété. Verify.ps1 (agent) : 899/899, 0 avertissement, 0 erreur ; DLL 0.474.0.0. Non validé en jeu.
### English
- « My tools » gains an editor in the game: compose a tool block by block, without writing any file. Saving goes through the same strict format as a shared file.

## 0.473.0
- Demande de l'utilisateur, TEMPLATES-001 phases B0, B1, B2 et B4 ([DEC-029](../../docs/project/DECISIONS.md)) : page « Mes outils », sous l'accueil. Un outil est un fichier de données : titre, note, cases à remise à zéro, compteur, minuteur, objets (voulu, possédé, manque), lien. Il ne contient aucun code et n'envoie rien au jeu.
- Format strict : champ ou bloc inconnu, valeur hors bornes ou lien non https refuse le fichier avec la raison. Textes nettoyés. Une ligne partagée compressée est lue avec une taille bornée. Tout est montré avant l'installation, et un lien s'ouvre seulement après une confirmation qui montre l'adresse.
- Progression gardée dans `MyTools.json`, jamais écrasé s'il est illisible. Le partage ne contient ni cases ni compteurs. L'import par fichier n'est pas fait : coller le JSON suffit.
- Tests : MyToolsTests (8). Verify.ps1 (agent) : 898/898, 0 avertissement, 0 erreur ; DLL 0.473.0.0. Non validé en jeu.
### English
- New « My tools » page: pages made of data (headings, notes, checklists with resets, counters, timers, item lists, links), shared as one line of text. A strict format refuses anything unknown; tools hold no code and send nothing to the game.

## 0.472.0
- Demande de l'utilisateur : le chat personnalisable est retiré ([DEC-030](../../docs/project/DECISIONS.md)) : page Monde › Chat, fenêtre, réglages, textes, tests et plan. Il utilisera Chat 2 au besoin. Les fonctions qui lisent le chat pour autre chose restent (liens de carte, détection du jardinage). Les réglages déjà enregistrés par la 0.471 sont ignorés au chargement : essai fait avec le Newtonsoft.Json de Dalamud sur un fichier contenant ces types disparus.
- Tests : un test vérifie le retrait. Verify.ps1 (agent) : 890/890, 0 avertissement, 0 erreur ; DLL 0.472.0.0. Non validé en jeu.
### English
- The custom chat added in 0.471 is removed at the user's request: page, window, settings, texts and plan. Features that read chat lines for other purposes stay.

## 0.471.0
- Demande de l'utilisateur, CHAT-001 phases 0 et 1 ([DEC-029](../../docs/project/DECISIONS.md)) : nouvelle page Monde › Chat et une fenêtre de chat en lecture seule, désactivée par défaut, à côté de celle du jeu. Onglets par canal, couleurs, heure, canal, taille du texte, opacité, compteur de non-lus, jusqu'à 10 000 lignes, seules les lignes visibles sont dessinées. Elle lit ce que le jeu affiche, après les autres plugins, n'envoie rien et ne garde rien sur disque. Chat 2 actif : averti, jamais bloqué. La sonde (mode développeur) donne des compteurs, sans texte.
- La page a été créée avec `scripts/New-Tool.ps1`.
- Tests : ChatTests (6). Verify.ps1 (agent) : 895/895, 0 avertissement, 0 erreur ; DLL 0.471.0.0. Non validé en jeu.
### English
- New Chat page and a read-only chat window, off by default, beside the game's: tabs by channel, colours, time, text size, opacity, unread counts. It sends nothing and keeps nothing on disk.

## 0.470.0
- Décisions de l'utilisateur sur le chat et les modèles communautaires : [DEC-029](../../docs/project/DECISIONS.md). Partie code des modèles (TEMPLATES-001).
- Toutes les pages sont dans une seule liste, `MainWindow.Pages.cs` : la navigation, le dessin et la source montrée aux contributeurs la lisent. Trois listes séparées disparaissent, sans changement voulu à l'écran.
- `scripts/New-Tool.ps1` crée le squelette d'un outil (page, partie pure, test, textes, enregistrement). Essai réel sur un outil temporaire : compilé et tests passés, puis retiré. Guide « Adding a tool » dans CONTRIBUTING.md, formulaire « Tool proposal » et règles ajoutées au modèle de pull request.
- Tests : Pages470Tests (4). Verify.ps1 (agent) : 889/889, 0 avertissement, 0 erreur ; DLL 0.470.0.0. Non validé en jeu.
### English
- All pages now come from one list, `MainWindow.Pages.cs`, with no intended change on screen.
- `scripts/New-Tool.ps1` creates the skeleton of a new tool; CONTRIBUTING.md gains "Adding a tool", and GitHub a « Tool proposal » form.

## 0.469.0
- Demande de l'utilisateur (« j'ai pourtant ouvert calendrier et mission de ravitaillement ») : « demande du jour non observée » était un texte fixe, rien ne lisait cette fenêtre. Nouveau : `GcSupplyReader` lit la fenêtre du jeu ContentsInfoDetail (nom présent dans l'exécutable du jeu) une fois par seconde tant qu'elle est affichée, compare ses textes aux noms exacts des objets de livraison du catalogue (GCSupplyDuty), et garde le résultat par personnage jusqu'à la remise de 20:00 UTC. La mise en page n'a pas pu être sondée hors du jeu : une ligne `Grand Company supply window` du journal la documente. Les bulles disent demandée, absente de ce qui a été ouvert, ou comment ouvrir la fenêtre.
- Demande de l'utilisateur : dans le carnet, Manipulation est une case et l'icône du skill, sans texte. Repas et remède ont l'icône et la bulle de l'objet (aussi dans le simulateur) ; seuls ceux qui ont un bonus d'artisanat comptent, un autre repas se lit « aucun ».
- Demande de l'utilisateur (« restyliser la fenêtre liée au carnet ») : panneau en sections (recette, votre état, cible, calcul, macro), chiffres en tableau, barre de qualité, section de séquence enregistrée masquée s'il n'y en a pas, explications dans la bulle de « (?) ». Le texte « Manipulation exclue : déblocage non observé » datait d'avant la détection : retiré.
- Tests : GcSupply469Tests (6). Verify.ps1 (agent) : 885/885, 0 avertissement, 0 erreur ; DLL 0.469.0.0. Non validé en jeu.
- Plans ajoutés, sans code : chat personnalisable (CHAT-001 (document retiré depuis)) et modèles communautaires (TEMPLATES-001, notes du mainteneur hors dépôt), avec les décisions à prendre.
### English
- Plans added, without code: customisable chat and community templates, with the decisions to take.
- Today's Grand Company request is now read from the game's supply missions window; item tooltips say requested, not among what was opened, or how to open it.
- The crafting log panel is laid out in sections, with a table of figures, a quality bar and explanations in tooltips.
- In the crafting log panel, Manipulation is a box with the skill's icon, and food and medicine show the item's icon and tooltip, only when they give a crafting bonus.

## 0.468.0
- Demandes de l'utilisateur (campagne en jeu), à propos du panneau du carnet et de Raphael.
- « Raphael ne met pas en cache » : le disque montre que le carnet enregistrait bien ses résultats Raphael (fichiers du 2026-11-04, `UsedRaphael`). Le manque venait d'ailleurs : la recherche du simulateur n'a jamais eu de cache, et la clé du carnet contient la version de la DLL, les options de macro et la liste des séquences enregistrées, donc chaque version recalculait. Correction : `RaphaelSequenceCache` garde les séquences de Raphael sur disque (durée des calculs d'artisanat) selon ce dont Raphael dépend seulement (recette, attributs, qualité de départ, cible, actions interdites), pour le carnet et le simulateur. Une séquence gardée est rejouée dans le simulateur avant usage. « Recalculer » relance vraiment Raphael.
- Qualité cible dans le panneau, gardée par recette (`CraftLogTargets`), avec « Maximum » et les paliers des objets à collectionner. La qualité ne s'applique qu'à la sortie du champ.
- Le panneau montre repas, remède et spécialiste lus dans le jeu, et une case Manipulation avec ce qui est détecté. La case vaut jusqu'au rechargement. Spécialiste n'était jamais transmis au calcul du carnet : il l'est. Constat à vérifier en jeu : tous les calculs du 2026-11-04 ont Manipulation à non au niveau 100, d'où une ligne `Manipulation for job` dans le journal.
- « Enregistrer comme meilleure séquence » sous la proposition. Si une séquence enregistrée termine la synthèse et atteint la cible, rien n'est calculé (« Recalculer » cherche quand même).
- Fenêtre « Comparer les prochains coups » : réglage aussi dans Réglages › Modules › Artisanat, libellé au nom de la fenêtre, et croix qui décoche le réglage.
- Tests : Craft468Tests (7). Verify.ps1 (agent) : 879/879, 0 avertissement, 0 erreur ; DLL 0.468.0.0. Non validé en jeu.
### English
- Raphael's sequences are now kept on disk for both the crafting log and the simulator's search, independently of the plugin version and macro options, and replayed in the simulator before use.
- The crafting log panel gains a quality target by recipe, the food, medicine, specialist and Manipulation state, and a button to save the suggestion as the best sequence; a saved sequence reaching the target skips the search.
- The « Compare next actions » window has a setting under Crafting and a close button that turns it off.

## 0.467.0
- Demande de l'utilisateur : le panneau du carnet d'artisanat suit le solveur choisi. Raphael a atteint 12 000/12 000 en 19 actions (rapport en jeu) là où le carnet, avec le solveur Tensho, proposait 7 308/12 000.
  Une ligne « Solveur : Raphael » l'indique. Si Raphael échoue, la recherche Tensho répond, c'est dit dans le panneau et écrit dans le journal, et ce résultat n'est pas gardé en cache. Avec le calcul automatique,
  Raphael calcule à chaque recette choisie (le calcul précédent est annulé) ; le cache du carnet distingue les deux solveurs.
- Tests : Raphael465Tests complété. Verify.ps1 (agent) : 872/872, 0 avertissement, 0 erreur ; DLL 0.467.0.0. Recherche Raphael validée en jeu par l'utilisateur ; carnet non validé en jeu.
### English
- The crafting log panel now follows the chosen solver: with Raphael selected it computes with Raphael, says so, and falls back on Tensho's search if Raphael fails.

## 0.466.0
- Raphael en jeu (rapport de l'utilisateur, 01:29) : la recherche échouait, car Raphael ne connaît pas le niveau de la recette et proposait Main preste (Trained Eye) en première action alors qu'il faut dix niveaux de plus que la recette. Le
  simulateur a refusé la séquence, comme prévu. Cette action n'est plus proposée à Raphael dans ce cas, et Heart and Soul non plus (il ne sert qu'à des actions hors de sa liste). Message d'erreur interne corrigé (il
  nommait Quick Innovation au lieu de Trained Eye).
- Tests : Raphael465Tests complété. Verify.ps1 (agent) : 871/871, 0 avertissement, 0 erreur ; DLL 0.466.0.0. Raphael a bien calculé en jeu ; séquence jouée non validée.
### English
- Raphael: it does not know the recipe's level and used Trained Eye too low, which the simulator refused: Trained Eye is now left out of its actions unless the job is ten levels above the recipe.

## 0.465.0
- A-03 (DEC-028, demande de l'utilisateur) : solveur Raphael en option. Réglage « Solveur : Tensho / Raphael » dans Artisanat › Recherche, et « Utiliser le solveur Raphael » dans Paramètres › Modules ›
  Artisanat ; Tensho reste celui par défaut. Le calcul tourne hors du thread du jeu, annulable, un seul à la fois, avec tous les processeurs sauf deux. Il utilise les mêmes règles que la recherche Tensho :
  niveau, Manipulation, actions interdites, rien au hasard, et les actions de spécialiste seulement pour un spécialiste. Sa séquence est rejouée dans le simulateur de Tensho et refusée si elle ne tient pas.
- Le paquet contient maintenant Raphael.Net.dll et raphael_bindings.dll (win-x64), contrôlés par Verify ; crédits ajoutés.
- Mesure hors du jeu : 1,5 s et 108 Mo, jusqu'à 8,3 s et 642 Mo sur une recette difficile d'essai (docs/evidence/0.451-raphael-study.md). Le carnet d'artisanat garde le solveur Tensho.
- A-04 (suggestions depuis l'état en cours) n'est pas fait.
- Tests : Raphael465Tests (3 cas). Verify.ps1 (agent) : 871/871, 0 avertissement, 0 erreur ; DLL 0.465.0.0. Non validé en jeu ; Raphael n'a tourné qu'hors du jeu.
### English
- A-03: optional Raphael solver (Raphael.Net 5.0.0): a solver choice in the crafting search and a setting; Tensho stays the default. Runs off the game's thread, cancellable; its sequence is replayed in Tensho's simulator and refused if it does not hold. The package now ships its native library.

## 0.464.0
- Retours du lot 5 (2026-11-04) ; le reste du lot est validé par l'utilisateur :
  - Simulateur : « Conserver la séquence » vient maintenant après « Ajouter une action ».
  - Détection au lieu des cases à remplir : Manipulation (action apprise), spécialiste (PlayerState.IsMeisterFlag), repas et remède actifs (effets Bien nourri et Médicamenté du
    personnage, dont le paramètre désigne la ligne ItemFood, plus 10000 si HQ). Une ligne « Détecté » l'affiche, une fois par recette, et les cases restent modifiables. Métier porté : les attributs sont copiés
    d'office et comptent déjà le repas et le remède, qui ne s'ajoutent donc pas deux fois. Métier non porté : ils sont choisis dans les listes.
  - Couleurs : niveau, Habileté et Contrôle du simulateur sont en vert ou rouge selon la recette. Le niveau de la ligne de préparation vient aussi du métier porté : il restait gris tant que la lecture des
    ensembles d'équipement n'avait pas répondu (cause probable, non prouvée).
  - Artisanat : nouvel onglet « Liste de fabrication » : ingrédients en tableau (icône, requis, en stock, manquant en vert ou rouge), infobulle complète de l'objet au survol.
  - Synthèse en direct : la projection s'affiche à côté de la fenêtre de synthèse du jeu. Réglage « Projection à côté de la synthèse » (activé), qui suffit à activer la lecture de la synthèse. Rien n'est exécuté.
- Sonde N-06 enregistrée (docs/evidence/0.463-squadron-probe.log) : les totaux sont bien quatre membres plus le bonus d'entraînement, et l'utilisateur les rapporte conformes au jeu.
- Tests : Craft464Tests (4 cas) ; un test déjà écrit adapté. Verify.ps1 (agent) : 868/868, 0 avertissement, 0 erreur ; DLL 0.464.0.0. Non validé en jeu.
### English
- Simulator: « Save the sequence » after « Add an action »; Manipulation, specialist, active food and medicine are detected; level and attributes in green or red against the recipe.
- Crafting: a « Crafting list » tab with an ingredient table, icons and the full item tooltip. The next-action projection now shows beside the game's Synthesis window, with its own setting.

## 0.463.0
- Coffre de compagnie (retour du lot 3) : la lecture passait depuis 0.462 (cases à quantité 0 confirmées comme cause, la lecture a réussi à 00:54), mais la page Banque refusait de l'afficher : « ordre
  visuel indisponible », car le jeu ne garde pas d'ordre d'affichage pour ce coffre. Il s'affiche comme l'arsenal : onglets puis cases, 35 objets par cadre, gils et cristaux compris.
  L'organisation par onglet du jeu (50 cases) n'est pas reproduite.
- Tests : Storage463Tests (1 cas). Verify.ps1 (agent) : 864/864, 0 avertissement, 0 erreur ; DLL 0.463.0.0. Non validé en jeu.
### English
- Company chest: shown in the Bank in slot order, like the armoury chest (the game keeps no display order for it, which refused the whole tab).

## 0.462.0
- Retours du lot 3 (2026-11-04) ; le reste du lot est validé, sauf le point 12 reporté par l'utilisateur :
  - Comparaison sous l'infobulle : les statistiques passaient côte à côte sur une ligne et se coupaient lettre par lettre (« Vitalité » à la verticale). Le nom de la pièce, puis le
    niveau d'objet, puis une grille de statistiques deux par deux, comme l'infobulle du jeu.
  - « Tous les outils avec cet objet » retiré des menus : devenu inutile.
  - Coffre de compagnie vide : la lecture échouait d'un bloc (« Inventory slot inconsistent with requested bag », dalamud.log à 00:49:49). Cause supposée : des cases
    qui nomment un objet avec une quantité de 0, comme les cristaux du coffre. Une telle case compte maintenant comme vide. Si l'erreur revient, le journal donne le coffre, la
    case, l'objet et la quantité. Non vérifié en jeu.
- Tests : Lot3Fixes462Tests (3 cas). Verify.ps1 (agent) : 863/863, 0 avertissement, 0 erreur ; DLL 0.462.0.0. Non validé en jeu.
### English
- Gear comparison under the tooltip: stats in a two-by-two grid like the game's tooltip, instead of wrapping letter by letter. « All tools for this item » removed from the menus.
- Company chest: a slot naming an item with quantity 0 counts as empty, which failed the whole reading (suspected cause, not checked in game).

## 0.461.0
- Demandes de l'utilisateur après l'essai au potager (détection validée : Shirogane 2-17, potager 1, emplacement 6, bonne graine) :
  - Page Jardinage : chaque potager s'affiche en grille de 3 × 3, sans centre, comme le plan de plantation, avec le numéro, la graine (couleur selon l'état), les heures et
    les boutons de chaque emplacement ; un emplacement libre dit « vide ». Les pots et les lieux tapés à la main restent en liste. La disposition suit le wiki (emplacement 1
    en haut à gauche, puis sens horaire) : à comparer en jeu.
  - Bouton « Replanter » retiré : une nouvelle plantation est détectée et remplace l'ancienne.
- Tests : GardenGrid461Tests (2 cas) ; un test de Garden448 retiré avec le bouton. Verify.ps1 (agent) : 860/860, 0 avertissement, 0 erreur ; DLL 0.461.0.0. Non validé en jeu.
### English
- Garden page: each patch is drawn as a 3 × 3 grid (seed, state, times, buttons per bed), like the planting plan; pots and typed places stay a list. The « Replant » button
  is gone: a new planting is detected and replaces the old one.

## 0.460.0
- Demande de l'utilisateur pendant la campagne de tests : détecter le jardinage. Tensho lit les lignes du chat du jeu ; quand le jeu annonce une plantation (potager avec son
  numéro et son emplacement, ou pot), elle s'ajoute au journal avec :
  - la graine, lue dans la fenêtre de plantation (AgentHousingPlant, lecture seule) ;
  - l'heure et les durées de cette graine ;
  - le lieu (zone, quartier et terrain quand le jeu les donne).
  Une nouvelle plantation au même emplacement remplace l'ancienne. Une ligne dans le chat confirme. Si la graine n'a pas pu être lue, ou n'est pas dans la liste (bleuets),
  rien n'est deviné : le chat demande de l'ajouter à la main.
- Le texte de la page Jardinage et le réglage « Noter les plantations depuis le chat » (activé) expliquent que cela vient du chat. Les lignes viennent de la feuille LogMessage,
  dans la langue du jeu ; une ligne envoyée par un joueur ne compte jamais. Rien n'est envoyé ni fait dans le jeu.
- Limites : deux pots ne se distinguent pas, chaque plantation en pot ajoute une ligne. L'entretien et la récolte ne sont pas encore détectés (messages à identifier).
  Pour les trouver, dans une maison ou sur un terrain, les lignes système (sans expéditeur) vont pour l'instant dans le journal de Dalamud, en local (« Garden chat capture »).
- Croisements : le texte dit maintenant que les fleurs en pot, dont celles de Dawntrail, ne se croisent pas (preuve : docs/evidence/0.459-garden-detection.md).
- Tests : GardenChat460Tests (3 cas). Verify.ps1 (agent) : 858/858, 0 avertissement, 0 erreur ; DLL 0.460.0.0. Non validé en jeu.
### English
- Garden: plantings are detected from the game's chat lines and added to the journal with the seed read in the planting window, its time and hours. Explained on the page, with
  a setting (on). Nothing is sent or done in the game. Tending and harvesting are not detected yet. Crossbreeding text: flowerpot flowers do not crossbreed.

## 0.459.0
- Demandes de l'utilisateur pendant la campagne de tests :
  - Barre du serveur : une troisième icône pour les rappels terminés (tâches de servant, vaisseaux revenus, escouade, parcelles mûres), visible seulement s'il y
    en a, avec son réglage dans Avancé. L'icône des rappels ne compte plus que ce qui reste à faire (rappels non cochés, tâches à l'écran). Les cases « Barre » de la
    page Rappels valent pour les deux.
  - Jardinage : nouvelle page dédiée dans la catégorie Monde, avec deux sections, le journal et les croisements. Il sort de la page Rappels ; ses rappels dans le chat
    et ses parcelles enregistrées ne changent pas.
- Tests : 2 cas ajoutés à Todo458Tests ; 3 tests déjà écrits adaptés. Verify.ps1 (agent) : 855/855, 0 avertissement, 0 erreur ; DLL 0.459.0.0. Non validé en jeu.
### English
- Server bar: a third icon for finished reminders, shown only when there are some; the reminders icon now counts only what is left to do.
- Garden: its own page (World category) with the journal and the crossbreeds, out of Reminders.

## 0.458.0
- Demande de l'utilisateur pendant la campagne de tests : l'icône des rappels montre maintenant ce qui reste à faire, plus seulement ce qui est terminé.
  Son nombre et son infobulle (« Reste à faire ») comptent : tâches de servant finies, vaisseaux revenus, mission et entraînement d'escouade finis,
  parcelles mûres, rappels à case « Fait » non cochés depuis leur remise à zéro, et vos tâches affichées à l'écran.
- Page Rappels : nouvelle colonne « Barre », une case par rappel, cochée par défaut, pour choisir ce que l'icône montre. Les tâches personnelles gardent
  leur case « à l'écran » ; les parcelles mûres n'ont pas de case dans ce tableau (elles sont toujours comptées).
- Tests : Todo458Tests (2 cas). Verify.ps1 (agent) : 853/853, 0 avertissement, 0 erreur ; DLL 0.458.0.0. Non validé en jeu.
### English
- The Reminders icon now shows what is left to do (finished timers, reminders not ticked since their reset, your on-screen tasks), and the Reminders page
  has a « Bar » box per row to choose what it shows.

## 0.457.0
- Demande de l'utilisateur pendant la campagne de tests : les compétences ont leur icône dans les deux listes qui n'en avaient pas : « Actions
  interdites » (Réglages › Artisanat) et les actions de l'enregistreur (icônes dans l'ordre, numéro et nom au survol). Icônes lues dans les
  données du jeu, comme dans le simulateur. Dans « Actions interdites », l'icône est celle du Charpentier, comme le nom : certaines compétences ont
  une icône par classe.
- Tests : Icons457Tests (1 cas). Verify.ps1 (agent) : 851/851, 0 avertissement, 0 erreur ; DLL 0.457.0.0. Non validé en jeu.
### English
- Crafting actions now show their icons in « Forbidden actions » and in the recorder's list (number and name on hover).

## 0.456.0
- Demande de l'utilisateur pendant la campagne de tests : un plan pour obtenir le plus possible d'une graine. Dans Croisements, mode « Ce qui donne cette
  graine » :
  - les paires qui peuvent donner le moins d'autres graines passent en tête, avec « seulement cette graine » ou le nombre d'autres graines (leurs noms au
    survol) ;
  - un clic sur une paire montre le plan du potager de luxe : 8 parcelles en cercle, numérotées dans l'ordre de plantation, les deux graines en alternance
    (4 de chacune), la plus lente d'abord (durées Lotlab) ;
  - le texte dit que la parcelle 1 ne donne pas de croisement, et donne la terre (Thanalan Topsoil Grade 3 environ 90 %, Grade 2 environ 50 %), au nom du jeu.
- Les règles viennent du wiki Console Games, reformulées et créditées, non vérifiées par Tensho (docs/evidence/0.456-garden-grid.md). La disposition exacte
  des parcelles est à comparer en jeu. Les probabilités de chaque résultat ne sont pas connues : Tensho ne les affiche pas.
- Tests : GardenPlan456Tests (3 cas). Verify.ps1 (agent) : 850/850, 0 avertissement, 0 erreur ; DLL 0.456.0.0. Non validé en jeu.
### English
- Crossbreeding: to get as many of a seed as possible, the pairs that can give the fewest other seeds come first, and a click shows a planting plan for
  the Deluxe Garden Patch (8 beds, numbered order, seeds alternating, soil). Rules from the Console Games Wiki, credited, not checked by Tensho.

## 0.455.0
- Campagne de tests en jeu, lot 1 (retours de l'utilisateur, 2026-11-04) :
  - Journal de l'or : « +0 / =1 276 » venait du signe − (U+2212), que la police du jeu affiche « = ». Les totaux et les dernières variations
    sont maintenant des tableaux (zéro affiché « — »), et ce signe est remplacé par « - » dans tous les textes.
  - Escouade : l'alarme n'existait que par le bouton de la ligne, visible seulement pendant une mission. Les alarmes automatiques couvrent
    maintenant la fin de la mission et de l'entraînement (comme les tâches de servant et les vaisseaux).
  - Barre du serveur : une entrée séparée pour les rappels, avec sa propre icône et le nombre de rappels terminés (tâches de servant, vaisseaux
    revenus, mission et entraînement d'escouade, parcelles mûres) ; survol = la liste ; clic = page Rappels ; réglage dans Avancé (activé). L'icône
    des alarmes garde le nombre d'alarmes actives (choix de l'utilisateur).
- Validé en jeu par l'utilisateur (lot 1) : ouverture de la fenêtre, liste des graines, parcelle de test et ses boutons, croisements, message hors
  caserne, lien partake.gg, réglage retiré, journal de l'or sans fausse entrée. Cases cochées dans TESTING.md.
- Tests : Campaign455Tests (4 cas). Verify.ps1 (agent) : 847/847, 0 avertissement, 0 erreur ; DLL 0.455.0.0. Non validé en jeu.
### English
- In-game campaign, batch 1. Gil journal: tables instead of lines; the U+2212 minus sign, which the game font shows as « = », is replaced
  everywhere. Automatic alarms now cover the squadron's mission and training. The server bar has a separate reminders entry with its own icon and
  the number of finished reminders; the alarm icon keeps the number of active alarms.

## 0.454.0
- R-14, troisième lot : les derniers constats confirmés par l'audit.
  - Titre par classe : la liste est commune à tous vos personnages ; seul un titre que ce personnage possède est envoyé au jeu.
  - Collections : une feuille liée qui ne se charge plus ne laisse de côté qu'une carte ou un lieu, plus toutes les catégories.
  - Objets de collection et carnets : un échec affiche « n'a pas pu être lu » au lieu de « Lecture… » sans fin. Un sac pas encore chargé
    ne publie plus une liste incomplète. Le carnet de récolte illisible est signalé au lieu d'afficher 0/0.
  - Noms des pages communautaires : une feuille perdue est mémorisée (elle était relancée à chaque image du survol) et chaque table est
    construite seule.
  - Fonctions vérifiées avant l'appel : tri du servant, déplacement du nœud de la carte (la carte du jeu reste, au lieu d'un remplacement à
    moitié fait toutes les 5 s).
  - Carte au trésor : une fonction perdue est écrite une fois dans le journal, au lieu d'un « pas de carte » silencieux.
  - Triple Triade : aucune carte ou aucun PNJ reconnu fait échouer le catalogue, au lieu d'une liste vide.
  - Fenêtre de récolte : le nœud de texte est lu par un simple transtypage (une fonction du jeu de moins à risque).
  - Message d'erreur en français dans le code remplacé par l'anglais (règle des langues).
- Fin de cette passe R-14 : les 23 constats confirmés et les 12 classés « plantage » sont traités. Pour cinq lectures de feuille depuis les
  panneaux, seule l'isolation de 0.452 s'applique : le panneau se coupe, sans valeur « inconnue » propre. Restent 44 constats jamais vérifiés
  (quota épuisé pendant l'audit), aucun classé plantage : 21 mineurs, 19 dégradations silencieuses, 4 valeurs peut-être fausses (règles de
  Triple Triade ×2, tenues partielles de la cabine, cases collectées de la carte). Liste locale : checkpoints/r14-audit-2026-11-03.json.
- Tests : Resilience454Tests (5 cas) ; 2 tests déjà écrits adaptés au nouveau code. Verify.ps1 (agent) : 843/843, 0 avertissement, 0 erreur ;
  DLL 0.454.0.0. Non validé en jeu.
### English
- R-14, third batch: the last confirmed findings. A title is only sent if this character owns it; a lost linked sheet leaves one card or location
  instead of every collection; collectables and notebooks say « unavailable » instead of « reading » for ever; community names remember a lost
  sheet; retainer sorter and map node move are checked before use; the treasure map failure is logged once; an unrecognised Triple Triad
  catalogue fails instead of showing an empty list.

## 0.453.0
- R-14, deuxième lot de l'audit : une valeur inconnue reste inconnue, jamais zéro, « aucun » ou « aucune catégorie ». Chaque constat a été
  relu dans le code.
  - Journal de l'or : l'or était lu avant le chargement du conteneur de monnaies. Ce 0 servait de point de départ à la connexion, puis
    créait une fausse entrée « + tout l'or ». Il est lu après ce chargement, et chaque fonction est vérifiée.
  - Prix de sous-cotation : tant que vos servants ne sont pas lus (ou si leur lecture échoue), aucun prix n'est copié, au lieu d'annoncer
    « vos servants écartés ».
  - Revue de mode : une valeur de case absente de la fenêtre n'est plus lue comme « aucune catégorie cette semaine » ; erreur écrite une fois.
    Une valeur présente mais non textuelle reste lue comme vide, faute de preuve en jeu.
  - Escouade : un échec de lecture dit maintenant « n'a pas pu être lue » au lieu d'envoyer le joueur à la caserne.
  - Liste de téléportation illisible : affichée en échec au lieu de « aucune destination ».
  - Mandats, collections supplémentaires, chocobo : lus seulement une fois le personnage chargé (le jeu répond « non » avant).
  - Rappels : aucun compteur avant le chargement du personnage (l'écran titre montrait des zéros et le rôle « Tank »), et chaque compteur
    a sa propre garde (une signature perdue faisait tout disparaître).
  - Monnaies : CurrencyManager inconnu donne un solde inconnu, au lieu de compter dans les sacs.
  - Macros : la lecture des emplacements est gardée, un module de barres absent n'empêche plus l'écriture, et les pages déjà écrites sont
    sauvegardées même si une suivante échoue.
- Tests : Resilience453Tests (9 cas) ; 3 tests déjà écrits adaptés au nouveau code. Verify.ps1 (agent) : 838/838, 0 avertissement, 0 erreur ;
  DLL 0.453.0.0. Non validé en jeu.
### English
- R-14, second batch: an unknown value stays unknown. Gil is read once the currency container is loaded (no more fake « + all your gil »
  entry at login); no undercut price while your retainers are unknown; a missing Fashion Report value is no longer « no category »; a failed
  squadron or teleport read is said as such; unlock readers wait for the loaded character; Reminders show no zeros before login and each
  counter is guarded alone; an unknown currency manager gives an unknown balance; the macro writer saves pages already written.

## 0.452.0
- R-14 et A-14 (demande de l'utilisateur, sans ultracode), à partir de l'audit gardé, chaque constat relu dans le code. Premier lot : ce qui
  pouvait planter le jeu ou faire tomber toute l'interface.
- Interface : Dalamud enveloppe tout le dessin de Tensho dans un seul catch, donc un panneau qui échouait (feuille perdue après une mise à
  jour) remplaçait toutes les fenêtres par sa fenêtre d'erreur. Les 26 panneaux et observations de Plugin.Draw échouent maintenant seuls :
  erreur écrite une fois, panneau coupé jusqu'au rechargement.
- Chargement : trois feuilles lues au démarrage (ClassJob, ContentRoulette, ContentsNote) faisaient refuser tout le plugin si l'une ne se
  chargeait plus ; seule leur fonction se coupe.
- Hooks (A-14), revue finie : ils restent, avec ces corrections.
  - Enregistreur : le retour `bool` était lu sur 4 octets au lieu d'un, donc une action refusée pouvait passer pour acceptée, et ce résultat
    repartait au jeu ; il utilise le délégué de FFXIVClientStructs.
  - Enregistreur : une action mise en file était notée deux fois ; elle l'est une fois, quand elle part.
  - Chaîne de macros : plus aucune exception ne peut remonter dans le jeu, la fonction du jeu s'exécute toujours, et une macro refusée par
    /macrolock n'arrête plus la chaîne.
  - Les deux hooks utilisent OriginalDisposeSafe.
- Bogues réels trouvés par l'audit :
  - Alarmes : Dalamud refuse un second gestionnaire de lien pour le même objet, donc dès la deuxième sonnerie d'un objet, la ligne de chat, le
    son et Discord étaient perdus. Le lien est gardé par objet, et un échec du chat n'empêche plus le son ni Discord.
  - Effets de craft (Veneration, Innovation, Great Strides, Waste Not I et II, Manipulation, Final Appraisal) : ils ont une ligne Action par
    classe, mais toutes les classes recevaient celle du Charpentier. L'enregistreur les perdait sur les 7 autres classes. Preuve :
    docs/evidence/0.452-craft-buff-rows.md (vérifié sur XIVAPI v2).
  - Synthèse en direct : le contrôle « tous les effets lus » ne pouvait presque jamais réussir (l'énumérateur saute les cases vides), donc la
    projection en direct ne tournait pas. Elle va maintenant tourner, ce qui n'a jamais été essayé en jeu.
- Lectures de la mémoire du jeu : liste des scènes de musique bornée ; parcours des nœuds de Triple Triade gardés comme ceux de la récolte ;
  texture lue seulement quand son type le permet ; Try On seulement avec un personnage chargé et une fonction trouvée.
- Tests : Resilience452Tests (6 cas) ; 6 tests déjà écrits adaptés au nouveau code. Verify.ps1 (agent) : 829/829, 0 avertissement, 0 erreur ;
  DLL 0.452.0.0. Non validé en jeu.
### English
- R-14 / A-14, first batch from the saved audit (crash class). Each overlay now fails alone instead of replacing every Tensho window with
  Dalamud's error window; sheets read at load no longer refuse the whole plugin.
- Hook review finished: both hooks stay. The recorder reads the game's 1-byte bool correctly and notes a queued action once; the macro chain
  hook can no longer throw into the game and ignores macros refused by /macrolock.
- Fixed: from an item's second ring, an alarm lost its chat line, sound and Discord post. Crafting buffs use each class's own action row (the
  recorder dropped them on 7 classes). The live craft projection could almost never run: it will now, untested in game.
- Native lists bounded (music scenes, Triple Triad node walks); Try On needs a loaded character.

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
- Git confié à l'agent (accord du 2026-11-03) : consignes de l'agent, RULES et STATUS « Git » ; commit et push par `Verify.ps1 -Commit`, release
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
- Pour la suite : règle écrite dans notes de l'agent, et test Languages406Tests qui refuse tout texte français écrit dans le code (hors
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
