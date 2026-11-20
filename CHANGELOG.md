# Journal des versions / Changelog

Résumé par version, en français puis `### English` (partie anglaise obligatoire depuis 0.219.0) ; la partie anglaise sert aussi de corps
aux commits et aux notes de release sur GitHub. Preuves citées : docs/evidence/.
Chaque version a été compilée et testée par `scripts/Verify.ps1` ; la validation en jeu est indiquée quand l'utilisateur l'a rapportée.
Ce fichier garde la dernière release et les versions qui suivent ; les plus anciennes sont dans [docs/changelog/](docs/changelog/)
(déplacées par `tools/changelog.py archive`, un fichier par centaine de versions).

Summary per version, in French then `### English` (English part required since 0.219.0); the English part is also the body of the
commits and of the release notes on GitHub. Evidence cited: docs/evidence/.
Each version was built and tested by `scripts/Verify.ps1`; in-game validation is stated when the user reported it.
This file keeps the last release and the versions after it; older ones are in [docs/changelog/](docs/changelog/)
(moved by `tools/changelog.py archive`, one file per hundred versions).

## 0.1569.0
- Plan global, X-625 : Outil des découvertes partagées (tools/discoveries-merge.py) : sa sortie passe en UTF-8 avant tout affichage ; lancé avec `python -I` seul sous Windows (console cp1252), `sync --dry-run` s'arrêtait sur la flèche « → » du rapport des faits à revérifier.
- Tests : MergeUtf81569Tests. Verify.ps1 (agent) : 3194/3194, 0 avertissement, 0 erreur ; DLL 0.1569.0.0. Rien à essayer en jeu.
### English
- Shared discoveries tool (tools/discoveries-merge.py): its output is turned to UTF-8 before anything is printed; run with plain `python -I` on Windows (cp1252 console), `sync --dry-run` stopped on the « → » arrow of the recheck report.

## 0.1568.0
- Plan global, X-610 : Fiche d'objet, partie « Comment l'obtenir » : les secteurs d'exploration en aéronef et en sous-marin où l'objet peut être trouvé sont listés (noms des secteurs et des mers tirés des feuilles du jeu ; butin : LuminaSupplemental pour les aéronefs, relevés de FFXIVGachaSpreadsheet pour les sous-marins, déjà embarqués, source citée sous les lignes). Ces explorations comptent comme une source connue : « Aucune source connue » ne s'affiche plus pour ces objets (Fibre alchimique, 12926 : points 10 et 11 en aéronef et des secteurs de sous-marins).
- Pour les joueurs : fiche d'objet : « Comment l'obtenir » liste maintenant les secteurs d'aéronefs et de sous-marins où trouver l'objet.
- Tests : VoyageItemSources1568Tests. Verify.ps1 (agent) : 3192/3192, 0 avertissement, 0 erreur ; DLL 0.1568.0.0. Non validé en jeu.
### English
- Item sheet, « How to get it » part: the airship and submarine exploration sectors where the item may be found are listed (sector and sea names from the game's sheets; loot: LuminaSupplemental for airships, FFXIVGachaSpreadsheet records for submarines, already embedded, source cited under the lines). These voyages count as a known source: « No known source » no longer shows for such items (Alchemic Fibre, 12926: airship points 10 and 11 and submarine sectors).
- For players: Item sheet: « How to get it » now lists the airship and submarine sectors where an item can be found.

## 0.1567.0
- Plan global, X-599 : Fiche d'objet, onglet des recettes, ligne « Outils liés » : le bouton « Recettes, simulation et macros d'artisanat » ouvre la simulation d'artisanat sur la recette de l'objet choisi ; avant, il rouvrait l'onglet des recettes où l'on était déjà, sans effet visible. Pour un objet sans recette, il n'est pas montré. Les autres boutons de la ligne ne changent pas.
- Pour les joueurs : fiche d'objet : le bouton de simulation d'artisanat de l'onglet des recettes ouvre maintenant la simulation sur la recette de l'objet.
- Tests : RecipeTools1567Tests. Verify.ps1 (agent) : 3188/3188, 0 avertissement, 0 erreur ; DLL 0.1567.0.0. Non validé en jeu.
### English
- Item sheet, recipes tab, « Related tools » line: the « Recipes, simulation and craft macros » button opens the craft simulation on the chosen item's recipe; before, it opened again the recipes tab already shown, with no visible effect. For an item without a recipe it is not shown. The other buttons of the line are unchanged.
- For players: Item sheet: the craft simulation button of the recipes tab now opens the simulation on the item's recipe.

## 0.1566.0
- Plan global, X-508 : Réglages du chat : « Seulement avec » et « Cacher avec » (de chaque onglet), « Joueurs cachés », « Joueurs mis en avant » et « Mots d'alerte » s'affichent en lignes, une par joueur ou par mot, chacune avec un bouton pour la retirer, et une case « Ajouter… » (Entrée ou bouton ; plusieurs d'un coup avec des virgules ; un doublon n'est pas ajouté). Toujours enregistrées avec des virgules : les valeurs déjà enregistrées sont reprises ligne par ligne. Fenêtre de chat : le clic droit sur un message propose « Cacher <joueur> » et « Mettre <joueur> en avant » (ou « Ne plus mettre … en avant »). Les noms des filtres ne sont plus coupés depuis 0.1528 (X-560).
- Pour les joueurs : réglages du chat : joueurs cachés, mis en avant et mots sont maintenant des listes, une ligne chacun, et un clic droit sur un message cache ou met en avant son auteur.
- Tests : ChatLists1566Tests. Verify.ps1 (agent) : 3186/3186, 0 avertissement, 0 erreur ; DLL 0.1566.0.0. Non validé en jeu.
### English
- Chat settings: « Only with » and « Hide with » (of each tab), « Hidden players », « Highlighted players » and « Alert words » are shown as lines, one per player or word, each with a button to remove it, and an « Add… » box (Enter or the button; several at once with commas; a double is not added). Still saved with commas: the values saved before come back line by line. Chat window: a right click on a message offers « Hide <player> » and « Put <player> forward » (or « No longer put … forward »). Filter names are no longer cut since 0.1528 (X-560).
- For players: Chat settings: hidden players, highlighted players and words are now lists, one line each, and a right click on a message hides or highlights its sender.

## 0.1565.0
- Plan global, X-560, X-565 : Réglages du chat, onglets : en fenêtre étroite, chaque contrôle de la ligne d'un onglet (messages non lus, couleur du nom, retirer) passe à la ligne quand la largeur restante ne le tient pas, au lieu de sortir de la colonne et d'être coupé. Les grilles de cases du panneau (X-560, faites en 0.1528) ont maintenant leur test : colonnes selon la largeur, numéro de chaque linkshell gardé dans son nom.
- Pour les joueurs : réglages du chat : la ligne de chaque onglet ne sort plus d'une fenêtre étroite.
- Tests : ChatSettingsFit1565Tests. Verify.ps1 (agent) : 3181/3181, 0 avertissement, 0 erreur ; DLL 0.1565.0.0. Non validé en jeu.
### English
- Chat settings, tabs: in a narrow window, each control of a tab's line (unread messages, name colour, remove) goes to the next line when the width left does not hold it, instead of going out of the column and being cut. The panel's grids of boxes (X-560, done in 0.1528) now have their test: columns from the width, each linkshell's number kept in its name.
- For players: Chat settings: the line of each tab no longer goes out of a narrow window.

## 0.1564.0
- Plan global, X-624 : Relecture de 0.1562, constats mineurs : M2, deux clients du jeu sur un même profil : le second, qui trouve les lignes mises de côté déjà réglées par le premier, reçoit quand même dans sa mémoire la copie relue sur le gist (sans réécrire la base) quand le premier l'a gardée ; avant, son envoi suivant remplaçait le gist sans ce fait ; M3, une copie relue pendant l'écriture d'une sauvegarde qui échoue ensuite n'est plus ajoutée une deuxième fois par la sauvegarde suivante (la base d'avant reprend la copie que le fichier a reçue) ; M1 fait en 0.1563 ; M4 (lecture de gist.githubusercontent.com sous le consentement « Connecter GitHub ») laissé à la décision D-190.
- Pour les joueurs : découvertes : avec deux clients du jeu sur un même PC, vos découvertes mises de côté ne disparaissent plus de votre copie GitHub, ni ne sont comptées deux fois après une sauvegarde échouée.
- Tests : AsideToken1563Tests. Verify.ps1 (agent) : 3177/3177, 0 avertissement, 0 erreur ; DLL 0.1564.0.0. Rien à essayer en jeu.
### English
- Review of 0.1562, minor findings: M2, two game clients on one profile: the second, which finds the rows set aside already settled by the first, still gets in its memory the copy read back from the gist (the database not written again) when the first kept it; before, its next send replaced the gist without that fact; M3, a copy read back while a save is being written, that save then failing, is no longer added a second time by the next save (the base of before takes the copy the file received); M1 done in 0.1563; M4 (reading gist.githubusercontent.com under the « Connect GitHub » consent) left to decision D-190.
- For players: Discoveries: with two game clients on one PC, your discoveries kept aside are no longer lost from your GitHub copy, nor counted twice after a failed save.
