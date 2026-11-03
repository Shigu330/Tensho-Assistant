# Roadmap des 17 plugins

État au 0.446.0 (2026-11-03, relu contre le CHANGELOG, STATUS et le code) : les 17 lignes restent PARTIAL (aucune validation en jeu
complète). Files a) à g) faites ; restent g2) (R-14, en ultracode), h) (tests en jeu avec l'utilisateur) et les décisions
([G-PROPOSALS](G-PROPOSALS.md)). Dernière release publiée : Stable-0.451.0 (2026-11-03,
versions 0.431 à 0.451 ; plan de commit et de release : [STATUS](../ai/STATUS.md) « Git »). Détail par version : [CHANGELOG.md](../../CHANGELOG.md) ; validé en jeu / à valider : [STATUS](../ai/STATUS.md).

Source autoritaire : docs/ai/PROMPT-MAITRE.md, §4. Toutes les lignes sont conservées. State décrit le travail ; Coverage décrit la capacité réellement validée. TODO/READY/ACTIVE/BLOCKED/DONE ; NOT_STARTED/PARTIAL/FUNCTIONAL/REPLACEABLE. NEEDS_STUDY est une note d'incertitude, pas une suppression. Aucun pourcentage arbitraire.

## Demandes utilisateur R-01 à R-14 (état au 0.446)

Détail des demandes : [USER-REQUESTS](USER-REQUESTS.md). « Validé » = rapport ou capture de l'utilisateur ; sinon, à valider en jeu.

| N° | Sujet | Fait | Validé en jeu | Reste |
|---|---|---|---|---|
| R-01 | Carte : cible de quête du journal qui restait | 0.242 (retrait au clic droit), 0.243 (retrait quand la quête n'est plus active) | non | — |
| R-02 | Butin des contenus : cartes et rouleaux ✓/✗ et filtre « Non obtenus » | 0.241 | non | — |
| R-03 | Carte : onglet « Triple Triad » au clic droit (PNJ de la zone) | 0.244 | non | — |
| R-04 | Carte : clic droit sur une icône = son propre menu | 0.245 ; bouton souris des réglages d'icônes 0.298 | 0.297–0.299 (« Retirer le drapeau » dans ces menus, bouton souris) | — |
| R-05 | Ordre des fenêtres | clos sans code (2026-11-01, [preuve](../evidence/0.246-r05-window-order.md)) | — | — |
| R-06 | Refonte de l'interface ([plan](../goals/R-06.md)) | étapes 1 (0.247–0.263), 2 (0.264), 4 (0.270–0.271) ; 3 pour l'essentiel (fiche objet 0.272, Rangements 0.280) ; 5 en partie (↺ 0.249–0.262, « Tout remettre par défaut » 0.258, infobulles de tous les réglages 0.287, réglages de carte 0.288–0.289) ; 6 : légèreté (0.290–0.292) et police de titre du jeu (0.293–0.294) | 0.265–0.271, 0.276–0.279, 0.287–0.294 | réglages utiles manquants (étape 5, point d) ; cadres et textures du jeu (étape 6, avant D17) ; `FilteredTable` retiré du plan (DEC-023) |
| R-07 | Compagnie libre | page et expéditions 0.307, meilleurs trajets 0.309, montages 0.315 ; suite DEC-023 faite : secteurs signalés 0.334, prix du marché et paliers 0.335, dirigeables 0.336–0.337 ; historique 0.379 | non | à valider en jeu (étape h) |
| R-08 | Rappels | page 0.300, icône et infobulle dans la barre du serveur 0.300–0.302, expéditions 0.307, escouade 0.333 ; puis étape g (M-01 à M-05, N-01 à N-03) | non | Planeurs d'Ehcatl laissés (rien de lisible, 0.334) ; à valider en jeu |
| R-09 | Événements saisonniers | page Événements (0.342) : en cours, quêtes, boutiques, noms | 0.342 | à valider ; pas de dates à venir (aucune source fiable) |
| R-10 | Audit des caches | 0.246 ([audit](../evidence/0.246-cache-audit.md)), revérifié 0.281 (durée « Jamais ») | — | — |
| R-11 | Équipement ([plan](../goals/R-11.md)) | étape 1 (0.310), mirage (0.311), matérias et plafonds (0.312–0.313), sources et possession (0.314), montages de vaisseaux (0.315) | 0.310–0.314 | 0.315 et 0.340 à valider ; étape 4 : barde de chocobo FAITE 0.340, page Chocobo 0.341 (escouade retirée, défaut recommandé, décision de la fin) |
| R-12 | Webhook Discord pour les alarmes ([demande](USER-REQUESTS.md)) | 0.319 | non | à valider en jeu (étape h) ; utilisé ensuite par C-10, I-02, M-01 |
| R-13 | Page « Crédits » complète dans le plugin ([demande](USER-REQUESTS.md)) | 0.320 | non | à valider en jeu (étape h) |
| R-14 | Résistance aux mises à jour de FF14, dans la logique du code ([demande](USER-REQUESTS.md)) | règle pour le nouveau code (0.319) | — | passage complet vers la toute fin (g2) |

Pages ajoutées hors des 17 plugins (barre latérale, `Page` de `MainWindow.cs`) : Rangements (0.280), Météo et Alarmes, Rappels
(0.300), Compagnie libre (0.307 : onglets Vaisseaux, Meilleurs trajets, Montages), Équipement (0.310), PNJ vendeurs, Crédits (0.320),
Chocobo (0.341, la page Personnage est retirée), Événements (0.342), Mandats (0.363), Bestiaire (0.364), Pêche en mer (0.392).

## File d'attente (STATUS « Prochaine action », demandes du 2026-11-02)

Une à la fois, dans cet ordre :

1. **a)** Fait 0.315 (montages) ; retour en jeu repris à l'étape h).
2. **b)** Fait 0.316 : tour des `.md` et des descriptions des outils dans le code ; feuille de route complétée (ce document :
   points à décider, tests à faire). Décisions prises le 2026-11-02 (0.317, [DEC-023](DECISIONS.md)), voir « À décider / à revoir ».
3. **c)** Fait 0.318 : plan de ce qui manque à Tensho par rapport aux plugins existants ([GAPS](GAPS.md) : 17 références, dépôt
   officiel, dépôts non officiels conformes aux règles), puis choix de l'utilisateur ([DEC-025](DECISIONS.md)) : étape g) ci-dessous.
   Juste après c) : **R-12**, webhook Discord pour les alarmes ([USER-REQUESTS](USER-REQUESTS.md)).
4. **d)** Fait 0.321–0.326 : tour du code : réglages qui manquent, outils de développement pour la communauté open source, amélioration du dépôt git
   et de GitHub (rien n'est poussé ni réglé sur GitHub sans accord).
5. **e)** Fait 0.327–0.332 : améliorations générales : la carte d'abord, puis les autres outils ; cadres et textures du jeu (ULD) avant D17.
6. **f)** Fait 0.333–0.342 : suite des décisions du 2026-11-02 ([DEC-023](DECISIONS.md)), dans cet ordre : 1) seuils des 19 secteurs signalés ;
   2) valeur des trajets (prix du marché, puis paliers si la source le permet) ; 3) dirigeables (arbre de déblocage embarqué, puis
   planificateur distance et durée) ; 4) taux de surmatérialisation ; 5) R-11 étape 4 (barde de chocobo, puis escouade) ;
   6) rappels de l'escouade et des Planeurs d'Ehcatl (sonde) ; 7) R-09 événements saisonniers (sonde).
7. **Publication D17** ([PUBLISHING](PUBLISHING.md)) : à la version 1.0.0, moment fixé par l'auteur. Côté code : fait (0.156–0.157).
   Côté auteur : commit et push, hash du commit dans `manifest.toml`, texte de PR et déclaration d'IA réécrits par l'auteur.
   Avant D17 : cadres ULD (e) ; icônes de quête 71141 / 71151 seulement avec un relevé en jeu.
8. **g)** Fait 0.343–0.446 (sauf les points laissés dans [G-PROPOSALS](G-PROPOSALS.md) « Écarts laissés ») : ajouts retenus du plan c) ([GAPS](GAPS.md), 101 manques, choix précis dans sa colonne « Choix »), domaine par domaine
   dans l'ordre choisi par l'utilisateur ; dans un domaine, valeur haute et petite taille d'abord ; un manque = une version :
   1) Inventaire et marché : I-01, I-02, I-03, I-04, I-05, I-06, I-07, I-08, I-09, I-10, X-07, I-11, I-12, I-13, I-15 ;
   2) Collections : C-01, C-02, C-03, C-04, C-05, C-06, C-07, C-08, C-09, C-10, C-11, C-12, C-13, C-14, C-15, X-02, X-03, X-04 ;
   3) Monde : M-01, M-02, M-03, M-04, M-05, M-06, M-07, M-08, M-09, M-10, X-01, M-11, M-12, M-13, M-14, X-06, X-08, X-10 ;
   4) Récolte : R-01, R-02, R-03, R-04, R-05, R-06, R-07, R-08, R-09, R-10, R-11, R-12, R-13, R-14, R-15 ;
   5) Artisanat : A-01, X-11, A-02, A-03, A-04, A-05, A-06, A-07, A-08, A-09, A-10, A-11, A-12, X-12, A-13, A-14, A-15 ;
   6) Nouveaux domaines : N-01, N-02, N-03, N-04, N-05, N-06, N-07, N-08, N-09, N-10, N-11, N-12, N-13, N-14, N-15, X-09, X-13, X-14.
   Plus tard : I-14 (tri de l'arsenal et tri automatique, zone grise D17) et X-05 (bulles des PNJ dans le chat). Liens avec la
   file : C-06 = R-09 ; M-11 (escouade) rejoint les rappels d'escouade de f) ; C-04 (bardes) rejoint R-11 étape 4 ; C-10, I-02 et
   M-01 utilisent R-12 (Discord). Ordre de g) par rapport à D17 (1.0.0) : à fixer par l'auteur.
9. **g2)** Vers la toute fin, juste avant h) : **R-14**, résistance aux mises à jour de FF14, directement dans la logique du code
   ([USER-REQUESTS](USER-REQUESTS.md)) ; la règle s'applique déjà à tout nouveau code ([RULES](../ai/RULES.md)).
10. **h)** À la fin de la file (demande du 2026-11-02) : campagne de tests en jeu guidée de **tout** le plugin, pas à pas avec
   l'utilisateur ([TESTING](TESTING.md), section « Tests à faire en jeu » ci-dessous), puis les décisions reportées (section
   « À décider / à revoir »). Règle du 2026-11-02 : les décisions de l'utilisateur sont gardées pour la fin, sauf celles trop
   importantes pour attendre ; en attendant, l'agent prend le choix recommandé et le note ci-dessous.

Les compléments des 17 outils prévus « après publication » ([BACKLOG](BACKLOG.md)) sont repris dans g) : rotation adaptative (A-04),
tri de l'arsenal (I-14, plus tard) ; règles natives de Triple Triade : pas relevées comme manque par la comparaison, restent au BACKLOG.

## À décider / à revoir

Points encore ouverts : chacun attend un relevé en jeu ou se règle dans une étape de la file. Aucune valeur n'est devinée en attendant.

| Point | Source | Constat | Relevé ou étape attendus |
|---|---|---|---|
| Règle « coût ≤ capacité » sous le rang 50 | [0.315-vessel-builds](../evidence/0.315-vessel-builds.md), « Coût ≤ capacité » | Règle des outils communautaires. Cohérente avec 11 montages de dirigeables publiés ; non vérifiée en jeu sous le rang 50. | Relevé en jeu sur un vaisseau de rang inférieur à 50. |
| Type de marqueur des mandats | CHANGELOG 0.298 ; STATUS (validé 0.297–0.299) | Le journal montre les types 0, 3 (quêtes), 4 et 6 ; aucun relevé ne dit lequel désigne un mandat. | Relevé en jeu : mandat accepté, carte ouverte, mode développeur. |
| Icône du drapeau 60561 | `MainWindow.MapView.cs` (`FlagIcon`) ; CHANGELOG 0.102 (« supposée ») ; audit de la carte 0.328 | L'identifiant de l'icône du drapeau n'est pas prouvé dans docs/evidence (la pose du drapeau, elle, est validée en jeu). | Relevé en jeu : drapeau posé, `AgentMap.FlagMapMarkers[0].MapMarker.IconId` lu sur le framework (sonde), journal dans docs/evidence. |
| Icônes de quête 71141 / 71151 | [BACKLOG](BACKLOG.md) ; [0.211-event-icons](../evidence/0.211-event-icons.md) | Le sens des variantes n'est pas dans les données du jeu : les réglages affichent « variante n ». | Relevé en jeu avant D17, sinon garder « variante n ». |
| Bonus d'Abondance estimé (0.224) | [BACKLOG](BACKLOG.md), « Revue des points ouverts » ; [0.224-bountiful-base](../evidence/0.224-bountiful-base.md) | Règle communautaire non officielle, affichée « estimé » et modifiable. | À comparer en jeu avec le bonus réel. |
| Matérias : pièce sur plusieurs emplacements | [0.312-materia](../evidence/0.312-materia.md), « Non vérifié en jeu » | Une pièce qui couvre plusieurs emplacements prend le pourcentage du torse. | Relevé en jeu (plafond affiché contre le jeu). |
| Réglages utiles manquants | [R-06](../goals/R-06.md) étape 5 ; audit du point d) (2026-11-02) | FAIT 0.327 (relu le 2026-11-03) : nombre de résultats de recherche, portée du marché mémorisée, aides Triple Triad, calcul automatique du carnet, survol masqué en mission, silence en mission de la ligne de discussion, page d'ouverture, parties du panneau au survol. | Tests en jeu de l'étape h). |

### Décisions reportées à la fin (choix provisoire de l'agent, à confirmer à l'étape h)

Lot du 2026-11-03 ([DEC-026](DECISIONS.md)) : « Monstres au survol » retiré (0.447) ; inventaire 250 ms : DEC-004 gardée, éditeur du tri
gardé à l'écran (0.447) ; fins de ligne : rien à normaliser ; GitHub : protections, Dependabot et compilation en CI activés (0.447) ;
mémoquartz : 80 % confirmé ; choix de g) (G-PROPOSALS) confirmés en bloc. Les autres lignes restent à confirmer pendant les tests en jeu.

| Point | Choix provisoire | Où le changer |
|---|---|---|
| R-12 Discord : valeurs par défaut | Envoi désactivé ; toutes les catégories cochées ; message « ⏰ [label] : dans [minutes] min ([time]) » ; nom « Tensho » ; nom du personnage non envoyé. | Réglages › Alarmes › Discord |
| R-12 Discord : débit | Deux secondes au moins entre deux messages, file de 20 messages (les plus anciens écartés au-delà), trois essais au plus sur « 429 ». | `Alarms/DiscordNotifier.cs` |
| Revue de mode : source coupée (0.321) | Plus aucun téléchargement ; une copie récente gardée sur disque reste affichée. | `Fashion/FashionDataService.cs` |
| « Tout remettre par défaut » (0.321) | Les groupes repliés de la navigation sont gardés (état de l'interface) ; les réglages de la carte, dont les tailles et priorités d'icônes par carte, sont remis à zéro. | `Configuration.cs` (ConfigurationReset) |
| Nouvel ordre de Release.ps1 (0.323) | Contrôles, tag et release GitHub avec le zip, puis main (repo.json) en dernier ; reprise si le tag existe sans release. Vérifié par analyse de syntaxe seulement : suivre de près la première release. | `scripts/Release.ps1` |
| GitHub Actions et Dependabot (0.323) | Brouillons prêts dans `publish/github/` (CodeQL corrigé, compilation et tests, Dependabot) ; recommandé : les activer (copie dans `.github/`). | `publish/github/README.md` |
| Protections du dépôt GitHub (0.323) | Recommandé : interdire push forcé et suppression sur main et Devloppement, protéger les tags Stable-*, protection des secrets ; ne pas exiger de PR (bloquerait -Commit et -Release). À faire par vous sur GitHub. | `publish/github/README.md` |
| Fins de ligne eol=lf (0.323) | Recommandé, mais à faire par vous juste après un commit propre (`git add --renormalize .`), car l'opération réécrit la copie de travail. | `.gitattributes` |
| Code de conduite, CODEOWNERS, hook pre-push (0.323) | Non créés (code de conduite, CODEOWNERS) ; hook prêt, inactif (`git config core.hooksPath .githooks` pour l'activer). | `.githooks/pre-push` |
| R-13 Crédits : réglages partiels | Résolu à titre provisoire en 0.324 (choix recommandé appliqué) : les interrupteurs Garland et wiki coupent toute leur source, Universalis a son propre interrupteur (activé par défaut) avec « prix au survol » en dessous ; les copies gardées restent affichées. À confirmer ou à changer à la fin. | `Configuration.cs`, services de `Plugin.cs` (0.324) |
| R-12 Discord : alarmes écrites dans la liste du jeu | Elles sonnent dans le jeu et partent quand même sur Discord. | `Alarms/AlarmService.Dalamud.cs` (Ring) |
| Test des infobulles (0.325) | Cliquet (liste d'exceptions qui ne fait que diminuer, 3 restantes) et expressions régulières, sans dépendance ; pas de tolérance zéro ni de Roslyn. | `tests/TenshoAssistant.Tests/TooltipRatchet325Tests.cs`, `tooltip-baseline.txt` |
| Étape g) : propositions et choix provisoires | Tenus au fil de g) dans [G-PROPOSALS](G-PROPOSALS.md) : choix provisoires appliqués (I-01, I-02, I-03…), idées repérées en codant, écarts laissés pour une séance ensemble (I-05, I-15). | [G-PROPOSALS](G-PROPOSALS.md) |
| Alertes de monnaies (I-02, 0.344) | Choix provisoires de l'agent : seuil 80 % du plafond pour toutes (mémoquartz compris ; Currency Watchdog prend 70 % pour eux) ; sans Grande Compagnie, pas d'alerte de sceaux ; une monnaie déjà au-dessus au chargement est listée sans message ; pas de rappel à la connexion ; monnaies suivies : sceaux de la compagnie actuelle, marques, cristaux-trophées, sceaux alliés, Centurio, insignes de chasse, gemmes bicolores, assignats d'Azurée, assignats mauves et orange, mémoquartz permanent, standard et limité. Cristaux, mandats et gils non suivis. À confirmer ou changer. | Inventory/Currencies.cs, MainWindow.Currencies.cs |
| Touche d'actualisation du prix survolé (I-01, 0.343) | Choix provisoire de l'agent : Ctrl+Alt pendant le survol (les touches F et les lettres sont prises par le jeu), réglable (désactivable). Garder Ctrl+Alt, ou une autre combinaison ? | MainWindow.MarketHover.cs, Items/ItemHoverReader.Dalamud.cs |
| Carte : quoi ajouter (étape e) | 28 propositions de l'audit du 2026-11-02, en partie faites depuis à l'étape g (ligne « État » de chaque proposition, relu le 2026-11-03) : zoom à l'ouverture gardé, taille des étiquettes, éthérite la plus proche avec son coût, liens de carte du chat, coordonnées et distances, favoris dessinés, recherche sur la carte, cadres du jeu (ULD)… Recommandé, par valeur : P1, P2, P3 puis P4 (M-03), P6, P8. P0 (repli sur la carte du jeu) est corrigé en 0.328. | [MAP-PROPOSALS](MAP-PROPOSALS.md) |
| Objets, banque, tri, marché : quoi ajouter (étape e) | 23 propositions de l'audit du 2026-11-02, en partie faites depuis à l'étape g (ligne « État » de chaque proposition, relu le 2026-11-03) : pièces portées comptées dans les stocks, servant renvoyé signalé, prix des marchands PNJ dans le coût de préparation, prix Universalis par lots, annuler le dernier tri… Recommandé, par valeur : I0, I1, I3, puis I4 et I14. Plusieurs avancent des manques déjà retenus pour g) (I-01, I-06, I-07, I-10, I-12, I-13, X-07). | [ITEMS-PROPOSALS](ITEMS-PROPOSALS.md) |
| Inventaire vidé 250 ms à chaque changement (audit 0.329, défaut 7) | Non corrigé : la correction contredit DEC-004 (« invalidation atomique, collecte coalescée 250 ms ») et ses tests. Effet : l'éditeur de tri et les stocks clignotent un court instant après chaque mouvement d'objet. Recommandé : garder DEC-004 et faire garder à l'éditeur de tri la dernière liste complète (jamais pour l'aperçu ni le tri). | `MainWindow.SortPreview.cs` ; DEC-004 |
| Artisanat et récolte : quoi ajouter (étape e) | 37 propositions de l'audit du 2026-11-02, en partie faites depuis à l'étape g (ligne « État » de chaque proposition, relu le 2026-11-03) : qualité visée dans le carnet, Manipulation dans la macro proposée, séquences essayées sur toutes les recettes, suivi de la séquence pendant la synthèse, points à horaire ouverts maintenant, « possible maintenant ? » pour la pêche… Recommandé : C0, C1, C5, C7, puis C8 et C10 ; C33 (une séance de relevés en jeu avant de coder) d'abord pour ce qui demande une sonde. | [CRAFT-GATHER-PROPOSALS](CRAFT-GATHER-PROPOSALS.md) |
| Alarmes, compagnie libre, quêtes, équipement, mini-jeux : quoi ajouter (étape e) | 30 propositions de l'audit du 2026-11-02, en partie faites depuis à l'étape g (ligne « État » de chaque proposition, relu le 2026-11-03) : alarmes en heure réelle, rappels par personnage, alarme de retour des vaisseaux d'office, colonnes Niveau et Zone des quêtes, drapeau sur la position d'un monstre, missions aléatoires du jour… Recommandé : T0, T1, T2, T3, T4 ; T5 (une séance de relevés en jeu) d'abord pour ce qui demande une sonde. | [TOOLS-PROPOSALS](TOOLS-PROPOSALS.md) |
| Cadre des fenêtres du jeu (0.332) | Réglage désactivé par défaut tant que l'aspect n'a pas été vu en jeu. Recommandé après essai : l'activer par défaut, puis l'étendre à la fenêtre de la carte et aux panneaux, et ajouter les séparateurs du jeu (Journal_Separator, WindowA_Line, déjà prouvés). | `UI/Components/GameFrame.cs` ; docs/evidence/0.332-uld-window-frame.md |
| Escouade dans R-11 étape 4 (sonde 0.332) | L'équipement d'un membre d'escouade n'est qu'une apparence (mirage, teintures), sans caractéristiques : rien à planifier. Recommandé : retirer l'escouade de R-11 étape 4 et garder la barde de chocobo. Les rappels mission et entraînement sont faits (0.333). Appliqué en 0.340 (barde faite, escouade non planifiée) ; à confirmer à la fin. | docs/evidence/0.332-squadron-probe.md |
| « Monstres au survol » sans effet (relevé en 0.327) | Rien n'est retiré en attendant votre choix. Le réglage ne commande plus rien : la fonction qui lisait les monstres du wiki au survol (`DrawHoveredSources`) n'est plus appelée depuis la refonte du panneau, qui montre déjà les monstres dans « Comment l'obtenir » (données du jeu, LuminaSupplemental, puis Garland et le wiki), désormais réglable par la case « Sources ». Recommandé : retirer la case et le code mort ; sinon la rebrancher (requêtes wiki au survol). | `MainWindow.MarketHover.cs` (DrawHoveredSources), `Monsters/MonsterHoverSources.cs` |
| Suppressions immédiates (0.325) | Confirmation en deux clics pour « Tout supprimer » d'un groupe d'alarmes seulement ; ailleurs (alarme, deck, modèle de rangement, séquence d'artisanat), une infobulle dit que la suppression est immédiate. | `MainWindow.Alarms.cs` (modèle à deux clics) |

### Décidé le 2026-11-02 ([DEC-023](DECISIONS.md))

| Point | Décision | Place dans la file |
|---|---|---|
| Table des seuils de butin des sous-marins | Garder la table SubmarineTracker ; sur les 19 secteurs où elle est sous toutes les observations, le palier porte la mention « non confirmé par les relevés ». | f) 1 |
| Valeur des trajets de sous-marin | Ajouter le prix du marché (Universalis) à côté du prix PNJ, et les paliers de surveillance / récupération si la source sépare les relevés par palier (sonde d'abord). | f) 2 |
| Arbre de déblocage des dirigeables | Données de LuminaSupplemental (GPL-3.0) embarquées dans la DLL, avec l'avis de licence dans le fichier. | f) 3 |
| Planificateur de trajets de dirigeable | Distance et durée seulement ; le butin est affiché « inconnu », jamais estimé. | f) 3 |
| Matérias : taux de surmatérialisation | Lire le taux de réussite dans les données du jeu (feuille à sonder) et l'afficher par emplacement avancé. | f) 4 |
| Page Personnage retirée (0.341, demande de l'utilisateur) | La page Équipement montre les mêmes valeurs réelles. Gardé par défaut : le panneau à côté de la fenêtre Personnage du jeu (réglage « ShowCharacterPanel », validé en jeu en 0.182–0.184), qu'Équipement n'a pas. DÉCIDÉ 2026-11-02 par l'utilisateur : le panneau est gardé. | MainWindow.Character.cs |
| Autres équipements (R-11 étape 4) | Barde de chocobo : FAITE 0.340 (docs/evidence/0.340-chocobo-barding.md). Escouade : apparence seulement, retirée (défaut recommandé appliqué). | f) 5 |
| Rappels : escouade, Planeurs d'Ehcatl | Sonder une heure lisible dans la mémoire du jeu (FFXIVClientStructs) ; sinon laisser de côté. | f) 6 |
| Événements saisonniers (R-09) | FAIT 0.342 : données du jeu seulement (événements en cours, quêtes, PNJ et boutiques des calques) ; noms par le PNJ guide (français) ou LuminaSupplemental ; pas de dates à venir. | f) 7 |
| Cadres et textures du jeu (ULD) | Avant D17. | e) |
| Publication D17 | À la version 1.0.0, moment fixé par l'auteur. | après la file |
| Traductions allemande et japonaise | Gardées dans le paquet (partielles, repli anglais). | — |
| Ton des textes français | Vouvoiement partout (fait 0.317, test automatique). | fait |
| Pêche en mer | Calendrier (R-01, onglet « Pêche en mer » de la page Récolte) et aide pendant la traversée (R-07, tout d'un coup) ; données du jeu (IKDRoute, IKDRouteTable, IKDSpot). | g) Récolte |
| Compléments des 17 outils | Repris dans le plan c) : rotation adaptative (A-04), tri de l'arsenal et automatique (I-14, plus tard). | g) |
| Licence EUPL-1.2 | Compatible avec la GPL v3 par sa clause (avis requis) : Tourist, Glamaholic, Quest Map, Better Party Finder (DEC-025). | — |
| Modèle `FilteredTable` | Retiré du plan (choix de l'agent, règle « pas d'abstraction prématurée ») : à créer au premier besoin réel répété. | — |

## Tests à faire en jeu

Protocole et liste de contrôle : [TESTING.md](TESTING.md). État de référence : [STATUS](../ai/STATUS.md) (« À valider en jeu »).
Le relevé ci-dessous liste les versions sans rapport de validation au 2026-11-02 (relu contre STATUS et le CHANGELOG).

- **0.224–0.228, 0.230, 0.232–0.233** : récolte et pêche (bonus d'Abondance estimé, panneaux d'aide, données GatherBuddy d'abord,
  ouverture manuelle, page Récolte, tableau compact, clic droit sur poissons et appâts). 0.229 et 0.231 sont validées.
- **0.235–0.240** : arsenal, coffre à apparence, armoire et tenues complètes dans la Banque et dans « possédé ».
- **0.241–0.246** : R-02, R-01, R-03, R-04, R-10.
- **0.247–0.264** : R-06 étapes 1 et 2 (icônes et menus communs, ↺, « Tout remettre par défaut », barre latérale par catégories),
  langues dans la DLL seule (0.248), alarmes de pêche avec météo (0.253–0.254).
- **0.272–0.275** : fiche objet, liens vers le bon onglet, menu d'une entrée de collection, ✓/✗ sur toutes les icônes de collection.
- **0.280–0.282** : page Rangements, repli du marché sur le cache, durée « Jamais », onglets de fiche selon leur contenu.
- **0.300–0.302** : Rappels, icône d'alarme dans la barre du serveur (retrouver son texte dans la barre est à confirmer), temps
  restant.
- **0.307–0.309** : Compagnie libre (moment où le jeu remplit les données de l'atelier), meilleurs trajets (durée réelle d'un voyage
  contre le calcul ; Base + Bonus lus), fond du niveau de zoom sur la carte (0.308).
- **0.315** : onglet Montages (sur un vaisseau lu dans l'atelier, aucun ⚠ ne doit apparaître ; sinon la règle « Base = somme des
  pièces, Bonus = bonus du rang » est fausse), règle de capacité sous le rang 50, lecture des secteurs débloqués, bouton
  « Montage » des trajets.
- **0.316–0.318** : annuaire « Tous les outils » (six entrées ajoutées), tuiles de l'accueil, textes français au « vous ».
- **0.319** : webhook Discord (liste détaillée dans [TESTING](TESTING.md), « Webhook Discord »).
- **0.320** : page Crédits (liste détaillée dans [TESTING](TESTING.md), « Page Crédits »).
- **0.321** : réglages d'artisanat gardés, interrupteur de la Revue de mode, « Tout remettre par défaut » (liste dans [TESTING](TESTING.md), « 0.321 »).

Relevés demandés (données inconnues, voir « À décider ») : type de marqueur d'un mandat accepté ; variantes d'icônes de quête
71141 / 71151. Points sans relevé dédié dans des versions validées globalement (0.310–0.314) : ordre des emplacements de
l'équipement porté et rendu de la cabine d'essayage ouverte par Tensho ([0.310-gear-sheets](../evidence/0.310-gear-sheets.md)).

## Fondations

> Notes et tableau de fondation conservés comme historique (2026-10-27 → 2026-10-28) ; leurs compteurs de tests sont ceux de leur date.

GATHER-001A 0.6.0 : consultation des sources récolte/pêche implémentée, objets cachés inclus ; jointures vérifiées sur les fichiers locaux du jeu (4 199 sources), 66 tests/package verts. Rendu en jeu à confirmer. SORT-001A et les tranches 0.8–0.13 sont maintenant intégrées, voir le checkpoint actuel.

État réconcilié au 2026-10-27 : CRAFT-001A, CRAFT-BOOK et CRAFT-PREP clos dans leurs périmètres, préparation 0.5.27 acceptée par retour utilisateur. GATHER-001A est terminé dans son périmètre statique ; BASE-TOOLS-001 consolide les tranches suivantes. Les notices anciennes ci-dessous sont historiques.

CRAFT-PREP 0.5.27 : préparation de fabrication implémentée (quantité cible, rendement, besoins, écarts et copie). 63 tests et package validés ; nouveau panneau à vérifier en jeu. Carnet CRAFT-BOOK confirmé OK par capture utilisateur v0.5.26. Craftimizer conserve une couverture partielle : simulation et macros à développer.

CRAFT-BOOK 0.5.26 : carnet Utilisé dans implémenté avec filtres métier natifs, recherche, liste et fiche sélectionnée. Build/61 tests/package validés ; rendu en jeu à confirmer. Banque filtrée validée par retour utilisateur et capture du Sac 3 sous catégorie Appât de pêche.

Banque UI 0.5.25 : implémentation terminée (tri ItemOrderModule, cadres par sac, Global et origines, catégories natives, qualité/recherche, masquage des sacs vides sous filtre, sélection en contexte et persistance des positions). Compilation et 61/61 tests réussis ; validation visuelle finale encore requise. Voir TESTING.md.

| ID | Plugin | Description | Dependencies | State | Tests | References |
|---|---|---|---|---|---|---|
| CORE-000 | Socle transversal | Solution, plugin, manifest, configuration, fenêtre, commande, lifecycle | SDK local | DONE | 5 tests managés et paquet ; runtime non validé | REF-LOCAL-001, REF-DALAMUD, REF-SAMPLE |
| CORE-001 | Fondation SortaKinda/Craftimizer/GatherBuddy | Snapshot inventaire managé avec portée et couverture | CORE-000 | DONE | 8 tests inventaire ; 13/13 avec socle, build et paquet verts | REF-DALAMUD, REF-INVENTORY, REF-SNAPSHOT, REF-INV-MODEL |
| INV-001 | Fondation commune | Lecture des quatre sacs via Dalamud et invalidation ; diagnostic | CORE-001 | DONE | 7 tests coordinateur ; 20/20 avec fondations, build/paquet verts ; runtime à valider | REF-DALAMUD, REF-INV-API |
| INV-002 | Fondation commune | Vérification de chargement natif et total exact des quatre sacs si lecture complète | INV-001 | DONE | 25/25 tests, build/paquet 0.1.1 ; quatre sacs complets visibles et total confirmé correct en jeu | REF-INV-LOAD ; preuve dans TESTING.md |
| UI-001 | Socle transversal | Options indépendantes d'ouverture au chargement et à la connexion | CORE-000 ; IClientState.Login | DONE | Build 0.1.2, 26/26 tests ; confirmation utilisateur du comportement | TESTING.md |
| UI-002 | Fondation inventaire | Affichage simultané NQ, HQ et total pour l'objet recherché | INV-002 | DONE | Build 0.1.3 et 26/26 tests ; rendu simultané live à confirmer | TESTING.md |
| UI-003 | Interface commune | Accueil, navigation, objets/inventaire, répertoire des 17 outils, réglages et diagnostics | ITEM-001 ; UI-001 ; UI-002 | DONE | Build/paquet 0.2.0, 31/31 tests ; répertoire exact des 17 plugins contrôlé ; visuel à vérifier | TESTING.md ; ARCHITECTURE.md |
| UI-004 | Interface commune | Grille d'icônes natives et fiches détaillées ouvertes uniquement au clic | UI-003 | DONE | Build/paquet 0.2.1, 31/31 tests ; 17 références conservées ; interaction visuelle à confirmer | REF-UI-ICONS ; TESTING.md |
| BANK-001 | Inventaire commun | Banque persistante servant/chocobo par imports manuels attachés aux menus et intégrée aux recherches | INV-002 ; ITEM-001 | DONE | Build/paquet 0.3.0 et 43/43 tests ; imports natifs et attache visuelle à valider | REF-BANK-API ; TESTING.md |
| BANK-001-RUNTIME | Inventaire commun | Vérifier boutons attachés, imports, recherche et persistance servant/chocobo | BANK-001 | TODO | Capture 0.3.0 : cache chocobo et cumul visibles ; servant/attache/persistance encore à confirmer | TESTING.md |
| UI-005 | Interface inventaire commune | Icônes d'objets du jeu et vue unique groupée Personnage/Chocobo/Servants | BANK-001 ; ITEM-001 | DONE | Build/paquet 0.4.0, 55/55 tests ; nouveau rendu à vérifier | REF-ITEM-TEXTURES ; TESTING.md |
| UI-006 | Interface commune | Corriger dimensions et centrage des icônes d'outils après capture | UI-004 | DONE | Build/paquet 0.4.1, 55/55 tests ; correction acceptée par l'utilisateur | TESTING.md |
| UI-005-RUNTIME | Interface inventaire commune | Vérifier les icônes, filtres et regroupements des stocks en jeu | UI-005 | ACTIVE | Essai ciblé sur cordial après rechargement de la DLL compilée | TESTING.md |
| UI-003-RUNTIME | Interface commune | Vérifier navigation, taille et conservation de sélection entre pages | UI-003 | ACTIVE | Protocole visuel ciblé après build dans TESTING.md | TESTING.md |
| ITEM-001 | Fondation commune | Index Item partagé et recherche bornée par nom/ID | CORE-000 | DONE | Build/paquet 0.1.4, 31/31 tests ; catalogue réel de 50 880 objets français et sélection validés en jeu | REF-ITEM-API |
| ITEM-001-RUNTIME | Fondation commune | Recherche par nom et sélection reliée au total des sacs | ITEM-001 | DONE | Capture : recherche sirop, sélection 12892, 0 NQ/156 HQ/total 156 ; retour utilisateur OK | TESTING.md |
| CRAFT-001A | Craftimizer #5 — première tranche | Catalogue recettes et ingrédients relié au catalogue Item commun | ITEM-001 | DONE | Build/paquet 0.5.0, 57/57 tests ; rendu et recette réelle à vérifier | REF-CRAFT ; API locale vérifiée |
| CRAFT-001A-RUNTIME | Craftimizer #5 | Vérifier une recette et ses ingrédients en jeu | CRAFT-001A | DONE | Captures du carnet 0.5.26 et confirmation préparation 0.5.27 | TESTING.md |
| GATHER-001A | GatherBuddy #8 | Sources statiques récolte/pêche et objets cachés | ITEM-001 ; CRAFT-PREP | DONE | Jointures locales vérifiées ; tests verts, runtime à confirmer | docs/goals/BASE-TOOLS-001.md |
| USE-001 | Item Use | Usages d'un objet reliés aux recettes connues | ITEM-001 ; CRAFT-001A | PARTIAL | Section Utilisé dans et ouverture directe de Craft lié ; couverture recettes uniquement | TESTING.md |
| RUNTIME-001 | Fondation commune | Smoke test réel du socle et de l'acquisition des sacs | INV-001 ; session Dalamud | DONE | Protocole utilisateur accepté, quantités et options confirmées ; cas avancés non revendiqués dans TESTING.md | REF-INV-API ; preuves dans TESTING.md |

## Matrice principale

État relu au 0.315 (2026-11-02). « Validé » = rapport de l'utilisateur (voir STATUS) ; les autres versions citées sont à valider en jeu.

| # | ID | Plugin | Auteur(s) | Description | Dependencies | State | Coverage | Tests | References | Groupe |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | FASHION-001 | Avant-Garde | NeNeppie | Équipements valides associés aux indices Fashion Report | Indices natifs ; Item/emplacements ; observations datées | ACTIVE | PARTIAL | Répertoire manuel, pièces signalées, filtres/stock/contexte et provenance testés ; boutons sur les cases de la fenêtre du jeu et teintures de la semaine validés en jeu (0.213–0.223) ; « possédé » avec arsenal, coffre à apparence et armoire lus dans le jeu (0.235–0.240, à valider) ; « Essayer » (cabine d'essayage) au clic droit de tout équipement (0.310, validé). Score par pièce non fait : aucune donnée (décision 2026-11-01) | docs/goals/FASHION-001.md ; docs/goals/R-11.md | C |
| 2 | BLU-001 | Blue Mage Helper | Infi, Sl0nderman, harbingerftw | Grimoire Mage bleu : sorts, acquisition et progression | ITEM-001 ; collections ; sources | ACTIVE | PARTIAL | Grimoire natif, numéros distincts des IDs, acquisitions confirmées ; build/tests verts, runtime à confirmer | docs/goals/BASE-TOOLS-001.md | C |
| 3 | CHAR-001 | Character Panel Refined | Kouzukii | Statistiques utiles du personnage | Lecture des stats locale ; formules vérifiées | ACTIVE | PARTIAL | Attributs natifs copiés et invalidés ; valeurs réelles de combat (`Character/StatFormulas.cs` : ParamGrow du jeu, formules communautaires, modificateur principal repris comme données de Character Panel Refined, MIT) dans un panneau à côté de la fenêtre Personnage du jeu, validées en jeu (0.182–0.184) ; reprises pour le plan dans la page Équipement (R-11, 0.310, validée) | docs/goals/BASE-TOOLS-001.md ; docs/evidence/0.182.0-paramgrow-probe.log ; docs/goals/R-11.md | D |
| 4 | COLL-001 | Collections | Seventh | Découverte, progression et interactions avec les collections | ITEM-001 ; unlocks ; couverture runtime | ACTIVE | PARTIAL | Montures/mascottes/emotes/cartes/orchestrion/titres/succès ; catégories/fiches, récompenses et contexte, acquisition à trois états pour listes dédiées ; état obtenu/non obtenu dans l'infobulle des objets, clic droit vers la collection et « Comment l'obtenir » (marchands, échanges, quêtes, succès, artisanat, récolte, puis Garland Tools et wiki) validés en jeu (0.146). Pastille ✓/✗ sur toute icône d'objet de collection (0.275) et cartes/rouleaux dans « Non obtenus seulement » (0.241), à valider ; cartes Ex. de Triple Triade (0.278, validé). Autres types et sources inconnues restantes à compléter | docs/goals/COLL-002.md ; docs/goals/COLL-003.md | B |
| 5 | CRAFT-001 | Craftimizer | Asriel | Simulation de craft, macros assistées et suggestions en cours de craft | ITEM-001 ; Recipe/RecipeLevelTable ; inventaire ; simulateur | ACTIVE | PARTIAL | Recettes/préparation validées ; simulation/export/recherche/séquences, projection ordinaire et aide macro près du carnet natif (sélection, attributs, comparaison et copies) compilées/testées ; collectionnables ordinaires/HQ initial et priorité aux attentes intégrés ; copie des macros dans les macros du jeu, prévision des emplacements et enchaînement validés en jeu (0.191–0.196) ; optimum global, rotation adaptative et recettes spéciales à compléter (après publication) | docs/goals/CRAFT-CONDITIONS-004.md ; docs/goals/CRAFT-PRESETS-001.md ; docs/goals/CRAFT-LOG-001.md ; docs/goals/CRAFT-QUALITY-TIME-002.md ; docs/goals/CRAFT-MACRO-001.md | A |
| 6 | DUTY-001 | Duty Loot Preview | grittyfrog | Aperçu du butin des contenus | ITEM-001 ; catalogue natif ; Garland Tools ; LuminaSupplemental | ACTIVE | PARTIAL | Catalogue et consultation manuelle, cache/annulation, provenance et contexte testés ; panneau à côté de l'outil de mission du jeu (0.185–0.186, sélection validée), butin des contenus récents par LuminaSupplemental (0.188–0.190, validé), butin ✓/✗ et mission aléatoire (0.207–0.208, validés) ; cartes et rouleaux ✓/✗ (R-02, 0.241, à valider) ; données partielles | docs/goals/DUTY-001.md | C |
| 7 | CACT-001 | ezMiniCactpot | daemitus, Minmoose, MidoriKami | Calcul du meilleur choix au Mini Cactpot | Solveur managé ; observation LotteryDaily | ACTIVE | PARTIAL | Solveur manuel exact ; mode observé opt-in et recalcul par ticket, worker propriétaire/génération ; solution superposée à la fenêtre du jeu `LotteryDaily` (0.158), validée en jeu (rapport 2026-11-01) | docs/goals/CACT-LIVE-002.md ; docs/goals/CACTPOT-019.md | D |
| 8 | GATHER-001 | GatherBuddy | Ottermandias | Récolte et pêche | Item ; Gathering ; cartes ; météo ; GatherBuddy (Apache-2.0) ; Teamcraft (MIT) | ACTIVE | PARTIAL | Sources/cartes, observation MIN/BTN opt-in, carnet et appâts/conditions explicites ; météo localisée et calcul borné des critères renseignés. Panneaux d'aide à la récolte et à la pêche en jeu (0.225–0.233), données de pêche de GatherBuddy d'abord puis Garland (0.227), anciens fichiers GatherBuddy (0.229, validé), temps de touche Teamcraft et chrono (0.231, validé), alarmes de pêche avec météo (0.253–0.254) ; le reste à valider. Pêche en mer non couverte (pas de source utilisable) ; intuition/prérequis à compléter | docs/goals/GATHER-NATIVE-004.md ; docs/goals/GATHER-FISH-FORECAST-010.md ; docs/evidence/0.226-fishing-spot-coordinates.md ; docs/evidence/0.231-teamcraft-bite-times.md | A |
| 9 | USE-001 | Item Use | PunishedPineapple | Usages courants d’un objet | Item/recettes ; catalogues natifs ; contexte commun | ACTIVE | PARTIAL | Onglet, survol opt-in et menu Inventory (0.207, validé) ; recettes/mandats/GC/aquarium/orchestrion et sources de quêtes ; onglet Usages de la fiche objet (0.272) ; menu Tensho de l'inventaire du jeu et panneau sous son infobulle alignés sur Tensho (0.256–0.257, à valider) ; autres usages/disponibilité/liens à compléter | docs/goals/USE-002.md ; docs/goals/USE-HOVER-003.md ; docs/goals/USE-CONTEXT-004.md | C |
| 10 | GP-001 | LazyGatherer | Hiroa | Optimisation des PA/GP de récolte | Actions natives ; statistiques communes ; modèle/observation partagés | ACTIVE | PARTIAL | Optimiseur manuel avec familles déjà payées et bonus limité ; observation/copier ressources (testés à l'époque) ; bonus de Récolte abondante II prérempli par une règle communautaire citée, affiché « estimé » et modifiable (0.224, à valider) ; décodeur natif/régénération/procs/collectionnables et runtime à compléter | docs/goals/GP-ACTIVE-005.md ; docs/goals/GATHER-NATIVE-004.md ; docs/evidence/0.224-bountiful-base.md | D |
| 11 | MACRO-001 | Macro Chain | Caraxi | Exécution de macros successives | Macro lifecycle ; marqueur final ; hook d'intervention externe | ACTIVE | PARTIAL | Plan persistant, protocole de marqueur, arrêt/identité et tests managés ; plan rempli après copie des macros dans le jeu et enchaînement validés en jeu (0.196, « macro chain fonctionne ») | docs/goals/MACRO-001A.md ; REF-MACRO-BRIDGE-20261028 | D |
| 12 | MAP-001 | Mappy | MidoriKami, harbingerftw | Carte en jeu polyvalente | Territory/Map ; coordonnées ; points partagés | ACTIVE | PARTIAL | Carte Tensho remplaçant la carte du jeu (M, quête du journal, téléporteurs) : icônes du jeu à taille/priorité par carte, quêtes et mandats, ALEA, groupe, récolte/pêche à horaires, courants, exploration (météo du wiki), marchands, drapeau, téléportation au coût réel, listes par onglet. Validé en jeu : 0.100–0.156 (dont carte hôte hors écran, zones colorées, zoom, commises de Rowena), variantes de carte (0.171–0.175), icônes de quêtes (0.211), réglages et cône de vision (0.287–0.289), repères au bord de la carte et « Retirer le drapeau » (0.297–0.299), menus et outils par-dessus la carte (0.303–0.306). À valider : R-01 (0.242–0.243), R-03 (0.244), R-04 (0.245), fond du zoom (0.308). Inconnus : type de marqueur des mandats, sens des variantes d'icônes de quête | docs/goals/MAP-016-PLAN.md ; docs/goals/MAP-017.md ; docs/goals/MAP-002.md ; docs/goals/GATHER-WEATHER-001.md ; docs/evidence/0.211-event-icons.md | C |
| 13 | LOOT-001 | Monster Loot Hunter | Sylf | Sources de matériaux sur les monstres | ITEM-001 ; Garland partagé ; LuminaSupplemental ; cartes par PlaceName | ACTIVE | PARTIAL | Matériau/monstre/zone, fiches, cache/cadence/annulation et contexte testés ; LuminaSupplemental (MobDrop) en tête (0.190, validé), puis wiki FFXIV et Garland pour les lieux ; données partielles | docs/goals/LOOT-001.md | B |
| 14 | MARKET-001 | Price Insight | Kouzukii | Prix du marché au survol d'un objet | ITEM-001 ; service marché ; cache horodaté | ACTIVE | PARTIAL | Consultation manuelle et survol opt-in NQ/HQ/monde, accroché à l'infobulle, stock et moins cher du centre de données validés en jeu (0.85–0.86) ; page Marché enrichie (R-06 étape 4 : annonces Universalis, filtres, coût pour N unités, historique, tri des colonnes) validée en jeu (0.270–0.271) ; repli sur le cache disque si Universalis échoue (0.281, à valider) | docs/goals/BASE-TOOLS-001.md ; docs/goals/R-06.md ; docs/evidence/0.270-universalis-listings.md | B |
| 15 | QUEST-001 | QuestTracker | isaiahcat, keifufu | Progression et complétion des quêtes | Catalogue Quest ; prérequis ; snapshots | ACTIVE | PARTIAL | Fiches/liens/niveaux/zones/carte, récompenses natives conditionnelles et index inverse Item → quête hors rendu ; progression observée jointe aux sources d'un objet (0.76, validé) ; types de quêtes des sections du journal, pourcentages, cités de départ et options non prises (0.283–0.286, validés). Monnaies/rangs non prouvés ; disponibilité à compléter | docs/goals/QUEST-REWARDS-003.md ; docs/goals/QUEST-ITEM-SOURCES-004.md ; docs/goals/QUEST-ITEM-PROGRESS-007.md ; docs/evidence/0.283-quest-journal-sections.md ; docs/evidence/0.285-quest-start-cities.md ; docs/evidence/0.286-quest-alternatives.md | B |
| 16 | SORT-001 | SortaKinda | MidoriKami | Personnalisation de l'inventaire, règles, aperçu et application | CORE-001 ; INV-001 ; ITEM-001 ; profils persistants | ACTIVE | PARTIAL | Profils et modèles réutilisables, critères multiples, zones protégées/filtrées, remplissage dans les deux sens et application visuelle après capture directe ; tri natif validé en jeu (2026-10-30). Arsenal lu comme rangement (0.235, à valider) mais pas trié : SORT-ARMOURY-009 et SORT-AUTO-010 après publication | docs/goals/SORT-SCOPES-003.md ; docs/goals/SORT-RULES-004.md ; docs/goals/SORT-TEMPLATES-007.md ; docs/goals/SORT-GAP-008-STUDY.md | A |
| 17 | TRIAD-001 | TriadBuddy | MgAl2O4 | Solveur Triple Triade | Cartes ; règles ; solveur ; état du plateau | ACTIVE | PARTIAL | Carnet/deck/solveur manuel testés ; TRIAD-003 terminé : conseil superposé en match (0.160), % par deck et génération de deck à la fenêtre de défi (0.161), decks Tensho (0.162) validés en jeu ; carnet (menus, valeurs, tri, score estimé, cartes Ex.) validé (0.265–0.269, 0.276–0.279). Règles natives non couvertes : après publication | docs/goals/TRIAD-002.md ; docs/goals/TRIAD-003-PLAN.md ; docs/evidence/0.278-triad-ex-cards.md | C |

Les IDs métier désignent la capacité globale à découper en petits GOAL au moment de l'activation. CRAFT-001 commence par le catalogue, puis simulation, macros et suggestions ; GATHER-001 par les lieux, puis horaires/météo/pêche ; SORT-001 par l'aperçu avant toute application native. Les autres capacités suivent le même principe, sans déclarer le plugin couvert dès la première tranche.

## Domaine complémentaire

Combat/ACT/IINACT : retiré du projet (décision utilisateur 2026-10-31).
