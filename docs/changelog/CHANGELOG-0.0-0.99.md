# Journal des versions / Changelog, 0.0 à 0.99

Archive de [CHANGELOG.md](../../CHANGELOG.md) : versions plus anciennes que la dernière release, déplacées par `tools/changelog.py archive`. Même format (français, puis `### English` depuis 0.219.0) ; liens relatifs à la racine réécrits depuis ce dossier.

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
