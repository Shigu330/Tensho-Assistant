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

## 0.1574.0
- Plan global, X-551 : Dédale bestial, panneau à côté du plateau du jeu : les faiblesses et les types de dégâts portent l'icône du jeu. Elle vient du texte même de la feuille XBMElement (« <icon(56)>Feu », « <icon(186)>Perforant » : une icône de la police du jeu avant le mot, comme dans la disposition du plateau et le bestiaire du jeu ; preuve docs/evidence/0.1574-beast-element-icons.md), lue par le chargeur (BeastCatalog.ElementIcons, IconOfMacro) et dessinée par le rendu de texte de Dalamud ; un élément sans icône, l'application de bureau ou un rendu en échec gardent le mot seul. Combat montré : un bloc par ennemi comme dans la fenêtre du jeu (icône et nom, « Faiblesse » avec son icône et son mot, étoiles, actions précédées de l'icône de leur type de dégâts), un trait entre les ennemis et autour du bloc ; les bêtes du combat et les lignes de l'équipe (icône, nom, rang, type de dégâts) portent l'icône de leur attaque. Point laissé ouvert : la comparaison à la fenêtre du jeu se fait en jeu.
- Pour les joueurs : dresseur : le panneau à côté du plateau du Dédale montre les icônes du jeu pour les faiblesses et les types de dégâts, un bloc par ennemi.
- Tests : BeastElementIcons1574Tests. Verify.ps1 (agent) : 3210/3210, 0 avertissement, 0 erreur ; DLL 0.1574.0.0. Non validé en jeu.
### English
- Crucible of the Unbroken, panel beside the game's board: weaknesses and damage types carry the game's icon. It comes from the XBMElement sheet's own text (« <icon(56)>Feu », « <icon(186)>Perforant »: an icon of the game's font before the word, as in the game's board layout and beast book; evidence docs/evidence/0.1574-beast-element-icons.md), read by the loader (BeastCatalog.ElementIcons, IconOfMacro) and drawn by Dalamud's text renderer; an element without an icon, the desktop application or a failed renderer keep the word alone. Fight shown: one block per enemy as in the game's window (icon and name, « Weakness » with its icon and word, stars, actions after the icon of their damage type), a line between enemies and around the block; the fight's beasts and the team's rows (icon, name, rank, damage type) carry the icon of their attack. Point left open: the comparison with the game's window is made in game.
- For players: Beastmaster: the panel beside the Crucible's board shows the game's own icons for weaknesses and damage types, one block per enemy.

## 0.1573.0
- Plan global, X-600 : Pages à onglets : un seul en-tête pour une page à sous-pages. Artisanat (Simulation d'artisanat, Séquences sauvegardées, Synthèse en cours, et « Retour à l'objet » au bout de la barre), Récolte (Récolte & pêche, Planification des PR, Collectionnables) et Collections (Collections, Magie bleue) montrent leurs sous-pages dans une vraie barre d'onglets, comme la page Rangements, sous un titre de famille et la description de la sous-page ouverte. L'en-tête est dessiné par un seul composant (DrawPageHeader) à partir de la liste des pages (champs Tab, About et TabAction de MainWindow.Pages.cs) ; la rangée de petits boutons écrite à la main (DrawCategoryNavigation, CategoryLink, UiComponents.CategoryItem) et les boutons de retour des sous-pages sont retirés. PageTabSync garde la barre et la page ensemble quand la page change d'ailleurs (barre de côté, lien, commande).
- Pour les joueurs : artisanat, Récolte et Collections montrent maintenant leurs sous-pages en onglets, comme la page Rangements.
- Tests : PageHeader1573Tests. Verify.ps1 (agent) : 3207/3207, 0 avertissement, 0 erreur ; DLL 0.1573.0.0. Non validé en jeu.
### English
- Pages with tabs: one header for a page with sub-pages. Crafting (Crafting simulation, Saved sequences, Current synthesis, and « Back to the item » at the end of the bar), Gathering (Gathering & fishing, GP planning, Collectables) and Collections (Collections, Blue magic) show their sub-pages in a real tab bar, as the Storage page does, under the family's title and the description of the open sub-page. The header is drawn by one component (DrawPageHeader) from the page list (fields Tab, About and TabAction of MainWindow.Pages.cs); the row of small buttons written by hand (DrawCategoryNavigation, CategoryLink, UiComponents.CategoryItem) and the sub-pages' back buttons are removed. PageTabSync keeps the bar and the page together when the page changes from elsewhere (side bar, link, command).
- For players: Crafting, Gathering and Collections now show their sub-pages as tabs, like the Storage page.

## 0.1572.0
- Plan global, X-581 : Jardin : une parcelle n'est plus dite « Mûr » quand le jeu dit le contraire. En ouvrant un emplacement, le jeu écrit son état dans le chat (« Le semis de … se porte bien. », « … n'a pas l'air en grande forme... », « … est arrivé à maturité ! », « … s'est fané... », feuille CmnDefHousingGardeningPlant_00151) ; Tensho le lit et le garde sur la parcelle du menu qui s'ouvre juste après. « Mûr » seulement quand le jeu le dit, ou quand la date de plantation plus la durée de la graine est passée sans que le jeu ait dit depuis que le semis poussait encore ; sinon « Inconnu », avec les mots du jeu et leur heure au survol. Cause du 18/11 : carottes dites mûres à 23 h 55 par la durée de 72 h des données de Lotlab, le jeu disait encore « se porte bien » à 0 h 13 (preuve docs/evidence/0.1572-garden-ripe.md, avec la sonde du stade de pousse : rien de lisible dans ClientStructs).
- Pour les joueurs : jardin : une parcelle n'est dite « Mûr » que si le jeu le dit ou si son heure prévue est passée ; quand le jeu dit que le semis pousse encore, elle montre « Inconnu » avec la raison.
- Tests : GardenRipe1572Tests. Verify.ps1 (agent) : 3203/3203, 0 avertissement, 0 erreur ; DLL 0.1572.0.0. Non validé en jeu.
### English
- Garden: a bed is no longer said « Ripe » when the game says otherwise. When a bed is opened, the game writes its state in the chat (« This crop is doing well. », « … has seen better days... », « … is ready to be harvested. », « … is beyond hope. », sheet CmnDefHousingGardeningPlant_00151); Tensho reads it and keeps it on the bed whose menu opens just after it. « Ripe » only when the game says so, or when the planting time plus the seed's growing time has passed and the game has not said since that the crop was still growing; else « Unknown », with the game's words and their time under the mouse. Cause of the report of 18/11: carrots said ripe at 23:55 by the 72 h of Lotlab's data, the game still said « doing well » at 00:13 (evidence docs/evidence/0.1572-garden-ripe.md, with the probe of the growth stage: nothing readable in ClientStructs).
- For players: Garden: a bed is only said « Ripe » when the game says so or its expected time has passed; when the game still says the crop is growing, the bed shows « Unknown » with why.

## 0.1571.0
- Plan global, X-626 : Listes du chat (relecture de 0.1568, M1 à M3) : le texte en cours dans la case « Ajouter… » est gardé par liste et par onglet (il passait d'un onglet à l'autre) ; après Entrée, le curseur reste dans la case pour la ligne suivante ; « Ne plus mettre X en avant » retire aussi une ligne enregistrée avec le monde (« Nom@Monde ») ; avec « Respecter la casse », les mots d'un onglet s'ajoutent avec leurs majuscules (« Foo » à côté de « foo »).
- Pour les joueurs : listes du chat : ce qu'on tape dans une case « Ajouter… » reste dans son onglet, Entrée garde le curseur dans la case, « Ne plus mettre en avant » marche pour un nom écrit avec son monde, et les mots gardent leurs majuscules avec « Respecter la casse ».
- Tests : ChatLists1571Tests. Verify.ps1 (agent) : 3197/3197, 0 avertissement, 0 erreur ; DLL 0.1571.0.0. Non validé en jeu.
### English
- Chat lists (review of 0.1568, M1 to M3): the text being typed in the « Add » box is kept by list and by tab (it moved from one tab to another); after Enter, the cursor stays in the box for the next line; « No longer put X forward » also removes a line saved with the world (« Name@World »); with « Match case », a tab's words are added with their capitals (« Foo » beside « foo »).
- For players: Chat lists: what you type in an « Add » box stays in that tab, Enter keeps the cursor in the box, « No longer put forward » works for names saved with their world, and words keep their capitals when « Match case » is on.

## 0.1570.0
- Plan global, X-627 : Test de la sortie UTF-8 de l'outil des découvertes partagées (MergeUtf81569Tests) : le second cas lançait l'aide de l'outil, sans « → », avec PYTHONIOENCODING ignoré sous `python -I` ; il ne pouvait pas échouer sur le code d'avant la correction. Il lance maintenant l'outil derrière une sortie cp1252 sur un rapport hors ligne qui contient « → », et vérifie que la même exécution sur une copie de l'outil sans la correction s'arrête sur une UnicodeEncodeError.
- Tests : MergeUtf81569Tests. Verify.ps1 (agent) : 3194/3194, 0 avertissement, 0 erreur ; DLL 0.1570.0.0. Rien à essayer en jeu.
### English
- Test of the shared discoveries tool's UTF-8 output (MergeUtf81569Tests): the second case ran the tool's help, which holds no « → », with PYTHONIOENCODING ignored under `python -I`; it could not fail on the code from before the fix. It now runs the tool behind a cp1252 output on an offline report that holds « → », and checks that the same run on a copy of the tool without the fix stops on a UnicodeEncodeError.

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
