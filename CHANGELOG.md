# Journal des versions / Changelog

Résumé par version, en français puis `### English` (partie anglaise obligatoire depuis 0.219.0) ; la partie anglaise sert aussi de corps
aux commits et aux notes de release sur GitHub. Preuves citées : docs/evidence/.
Chaque version a été compilée et testée par `scripts/Verify.ps1` ; la validation en jeu est indiquée quand l'utilisateur l'a rapportée.
Ce fichier garde la dernière release et les versions qui suivent ; les plus anciennes sont dans [docs/changelog/](docs/changelog/)
(déplacées par `tools/changelog.py archive`, un fichier par centaine de versions).

## 0.1323.0
- Plan global, X-342 : Tickets des joueurs séparés du plan du projet : les formulaires GitHub (bug, idée, outil, traduction) ajoutent l'étiquette player-report et un titre préfixé [Bug], [Idea] ou [Translation] ; le formulaire « Signaler un problème » du jeu préfixe le titre de la même façon (une seule fois si le joueur l'a tapé) et l'aperçu montre ce titre ; le lien des problèmes ouverts du formulaire de bug filtre les signalements des joueurs ; README : liens « notre plan » (label:plan) et « signalements des joueurs » (label:player-report). L'envoi direct par Tensho reste à faire (X-340).
- Pour les joueurs : Les bugs et idées envoyés depuis le jeu portent un titre [Bug], [Idea] ou [Translation] et l'étiquette player-report, à part du plan du projet.
- Tests : PlayerReport1323Tests. Verify.ps1 (agent) : 2474/2474, 0 avertissement, 0 erreur ; DLL 0.1323.0.0. Non validé en jeu.
### English
- Player issues apart from the project's plan: the GitHub forms (bug, idea, tool, translation) add the player-report label and a title prefixed [Bug], [Idea] or [Translation]; the in-game « Report a problem » form prefixes the title the same way (once if the player typed it) and the preview shows that title; the bug form's open problems link filters player reports; README: « our plan » (label:plan) and « player reports » (label:player-report) links. Direct sending by Tensho is still to do (X-340).
- For players: Bug reports and ideas sent from the game now carry a [Bug], [Idea] or [Translation] title and the player-report label, so they stay apart from the project's plan.

## 0.1322.0
- Plan global, X-321 : Tri visuel des sacs (Objets › Organisation, bouton Tensho de l'inventaire, Banque › Personnage) : une zone qui couvre plusieurs sacs les remplit aussi dans l'ordre 1, 3, 2, 4 (X-321, signalement de l'utilisateur du 14/11, essai T-0.1172-1, captures de l'inventaire du jeu et de la banque de Tensho). Dans le jeu, le sac 1 est en haut à gauche, le 2 en haut à droite, le 3 en bas à gauche ; l'ordre voulu (X-187, 0.1172) remplit la colonne de gauche d'abord. 0.1172 ne l'appliquait qu'aux objets hors des zones ; or le profil de l'utilisateur a une seule zone sur les 140 cases (liste de catégories), remplie case après case, donc 1, 2, 3, 4. Les cases d'une zone suivent désormais le même ordre (FillOrder ; à l'envers pour « remplir depuis la fin ») ; une zone dans un seul sac, les servants et les sacoches ne changent pas. L'aperçu et « Appliquer » suivent ce placement. Test ZoneBagOrder1322 (3), de comportement sur 101 piles comme la capture (aperçu et plan appliqué).
- Pour les joueurs : Tri des sacs : une zone qui couvre plusieurs sacs les remplit maintenant dans l'ordre de lecture du jeu (colonne de gauche d'abord), comme le reste.
- Tests : ZoneBagOrder1322Tests. Verify.ps1 (agent) : 2472/2472, 0 avertissement, 0 erreur ; DLL 0.1322.0.0. Non validé en jeu.
### English
- Visual sort of the bags (Items › Organisation, Tensho's button on the inventory, Bank › Character): a zone over several bags also fills them in the order 1, 3, 2, 4 (X-321, user report of 11/14, test T-0.1172-1, captures of the game's inventory and Tensho's bank). In the game, bag 1 is top left, bag 2 top right, bag 3 bottom left; the order wanted (X-187, 0.1172) fills the left column first. 0.1172 applied it only to the stacks outside zones; the user's profile has one zone over the 140 slots (a category list), filled position after position, hence 1, 2, 3, 4. A zone's positions now follow the same order (FillOrder; reversed for « fill from the end »); a zone inside one bag, retainers and saddlebags do not change. The preview and « Apply » follow that placement. Test ZoneBagOrder1322 (3), behaviour on 101 stacks as in the capture (preview and applied plan).
- For players: Bag sorting: a zone that covers several bags now fills them in the game's reading order (left column first), like the rest.
