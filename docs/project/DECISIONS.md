# Décisions durables

Index : 001 toolchain · 002 cycle de vie · 003 inconnu ≠ zéro (inventaire) · 004 lecture des sacs · 005 preuve de complétude ·
006 interface commune · 007 banque servants / chocobo · 008 vue Objets & rangements · 009 ACT hors périmètre · 010 git et
documentation · 011 ordre des sources · 012 plan avant D17 · 013 aucun autre plugin requis · 014 ordre des fenêtres (R-05) ·
015 caches et réseau · 016 une infobulle, un menu · 017 tout dans la DLL · 018 réglages et « Tout remettre par défaut » ·
019 inconnu ≠ aucun dans l'interface · 020 mirage depuis 7.4 · 021 données communautaires embarquées · 022 estimations signalées.

DEC-ID: DEC-001
Timestamp: 2026-10-27 Europe/Brussels
Problem: Choisir la toolchain réelle du nouveau plugin.
Evidence: Dalamud.NET.Sdk local 15.0.0 cible net10.0-windows/C#14 ; SDK 10.0.403 trouvé hors PATH ; compilation réussie contre Hooks/dev.
Options: Utiliser .NET 9 du PATH ; installer un SDK ; utiliser le SDK .NET 10 existant.
Decision: global.json épingle .NET 10.0.403 et Dalamud.NET.Sdk 15.0.0 ; Verify.ps1 accepte un chemin explicite.
Reason: Compatibilité locale démontrée, aucune installation nécessaire.
Consequences: Utiliser le script ou le bon exécutable ; aucun ancien code repris.
References: REF-LOCAL-001, REF-SAMPLE

DEC-ID: DEC-002
Timestamp: 2026-10-27 Europe/Brussels
Problem: Vérifier le lifecycle sans exécuter le jeu.
Evidence: Les callbacks et la commande sont les seules ressources du socle.
Options: Introduire un framework de modules ; gérer chaque unsubscribe isolément ; petite pile d'acquisitions.
Decision: RegistrationScope avec libération inverse, poursuite après erreur et tests exécutables sans NuGet de test.
Reason: Couvre les échecs partiels et les rechargements sans infrastructure métier prématurée.
Consequences: Le smoke test dans Dalamud reste manuel et obligatoire avant publication ; le test JSON porte sur les paramètres managés.
References: REF-LOCAL-001

DEC-ID: DEC-003
Timestamp: 2026-10-27 Europe/Brussels
Problem: Éviter de confondre absence dans un inventaire partiel et quantité zéro.
Evidence: REF-INVENTORY, REF-SNAPSHOT et huit tests du modèle managé CORE-001.
Options: Quantité entière seule ; quantité nullable seule ; borne observée et couverture explicite par conteneur.
Decision: Snapshot immuable avec portée propriétaire/conteneur ; requête sur portée explicite, résultat ExactQuantity nullable plus ObservedQuantity minimale et Coverage. Les propriétaires et les qualités restent distincts. InventoryService conserve le dernier lot seulement, publication atomique, sans fusion implicite.
Reason: Permet aux futurs calculs de besoins d'exprimer ce qui manque en connaissance autant qu'en quantité, sans annoncer de faux zéros.
Consequences: L'adaptateur doit prouver Complete, normaliser les identifiants du jeu et garantir l'ordre de publication. Clear invalide l'état. Aucune complétude globale ni propriété personnage déduite d'une collection vide.
References: REF-INVENTORY, REF-SNAPSHOT, REF-INV-MODEL

DEC-ID: DEC-004
Timestamp: 2026-10-27 Europe/Brussels
Problem: Lire les sacs sans surinterpréter la complétude ni conserver la mémoire native.
Evidence: REF-INV-API ; compilation et sept tests du coordinateur.
Options: Déduire Complete de la longueur du span ; employer de nouveaux offsets ; rester Partial/Unknown via le contrat public.
Decision: Quatre sacs seulement ; lecture sur Update confirmé framework, copie immédiate de primitives, propriétaire contrôlé avant/après. Span vide Unknown, non vide Partial. Invalidation atomique, collecte coalescée 250 ms, reprises Unknown/erreur cinq secondes, rejet des publications invalidées.
Reason: Le contrat installé permet l'observation mais ne prouve pas la complétude des conteneurs. Ce choix évite offsets et faux zéros tout en fournissant un diagnostic réel à valider.
Consequences: Les calculs futurs doivent respecter ExactQuantity nullable. L'adaptateur ne permet pas encore d'affirmer le total possédé ; validation live séparée RUNTIME-001.
References: REF-INV-API, REF-INV-MODEL

DEC-ID: DEC-005
Timestamp: 2026-10-27 Europe/Brussels
Problem: Le compteur concorde avec l'inventaire déclaré, mais affiche toujours « total inconnu » dans 0.1.0.
Evidence: Captures utilisateur Potion 4551 et sirop 12892 ; REF-INV-LOAD ; 25 tests passants après modification.
Options: Changer seulement le texte ; garder Partial systématique ; vérifier le chargement via les structures locales installées.
Decision: Étendre DEC-004 avec les membres typés InventoryManager/InventoryContainer. Complete exige un en-tête chargé, type et taille valides, buffer stable avant/après et tous les slots lus. Limiter l'affirmation de total aux quatre sacs ; réessayer les lectures Partial comme Unknown. Livrer 0.1.1 compilée avant de solliciter un test.
Reason: Donne un sens vérifiable à Complete et permet une quantité zéro exacte dans une portée entièrement lue, sans prétendre couvrir les autres rangements.
Consequences: Nouveau chemin natif à confirmer en jeu ; aucune validation live de 0.1.1 ne peut être déduite des captures de 0.1.0.
References: REF-INV-LOAD, TESTING.md section INV-002

DEC-ID: DEC-006
Timestamp: 2026-10-27 Europe/Brussels
Problem: La fenêtre de diagnostic doit préparer une interface commune aux différents outils, à la demande utilisateur.
Evidence: Fonctions disponibles validées jusqu'à 0.1.4 ; les 17 modules métier restent à construire ; interdiction des onglets métier vides dans le prompt maître.
Options: Dix-sept onglets placeholders ; navigation par espaces existants et répertoire unifié du périmètre futur.
Decision: Cinq pages réelles avec sélection d'objet partagée. Les sept domaines futurs sont décrits dans un seul répertoire filtrable des 17 références, toutes marquées à venir. Réglages et provenance technique isolés dans leurs pages dédiées. Conserver le style Dalamud de l'utilisateur.
Reason: Prépare l'arrivée des outils sans fausse fonctionnalité, dupliquer les services ou mélanger les diagnostics avec l'usage courant.
Consequences: Version 0.2.0 ; contrôle visuel requis. CRAFT-001A reste READY et son plan est conservé dans docs/goals/CRAFT-001A.md.
References: ROADMAP.md, TESTING.md UI-003, ARCHITECTURE.md

DEC-ID: DEC-007
Timestamp: 2026-10-27 Europe/Brussels
Problem: Inclure servants et chocobo dans les recherches quand leurs inventaires sont fermés, avec actualisation volontaire depuis leurs menus.
Evidence: Demande explicite utilisateur ; REF-BANK-API ; banque managée et persistance testées ; API de lecture des sacs déjà validée et factorisée.
Options: Scans/rafraîchissements permanents ; import manuel de snapshots complets et datés ; déduire zéro des inventaires non chargés.
Decision: Banque commune par personnage et rangement, import manuel uniquement depuis un menu visible vérifié. Copie native au framework, persistance sérialisée hors framework, remplacement par clé et conservation du cache antérieur sur erreur. Panneaux ImGui compagnons ancrés, sans injection de nœud natif. Totaux cache et live explicitement distingués.
Reason: Préserve la portée, évite le double comptage des réimports et rend les recherches possibles hors contexte servant/chocobo sans prétendre posséder une vue live des rangements fermés.
Consequences: L'utilisateur doit réimporter après transfert. Les sept pages servant sont couvertes, pas équipement/cristaux/marché. Extension chocobo indépendante et conditionnelle. Noms d'addons/attache et identité au changement de servant nécessitent un essai live après compilation.
References: REF-BANK-API, TESTING.md BANK-001, ARCHITECTURE.md Banque

DEC-ID: DEC-008
Timestamp: 2026-10-27 Europe/Brussels
Problem: L'interface sépare trop les sacs et la banque, et la recherche n'affiche pas les icônes réelles des objets.
Evidence: Demande et capture utilisateur 0.3.0 : 182 NQ en sacs et 26 HQ en cache chocobo, affichés dans deux sections longues.
Options: Conserver deux pages avec doublons ; vue commune et projection groupée ; simuler un total possédé actuel à partir des caches.
Decision: Une destination Objets & rangements avec icônes du jeu, filtres et rubriques Personnage/Chocobo/Servants. Projection pure testée réutilisant snapshots et banque. Les caches restent datés et le cumul est seulement recensé. Le filtre Personnage complet est le seul total exact au présent.
Reason: Réunit les emplacements autour de l'objet tout en conservant la fraîcheur et la portée explicites.
Consequences: Pas de migration de banque ni nouvelle lecture native ; images empruntées au provider de l'hôte. Ancienne page Banque fusionnée, gestion des imports repliable ; nouveau contrôle visuel requis en 0.4.0.
References: REF-ITEM-TEXTURES, TESTING.md UI-005, ARCHITECTURE.md vue unifiée

DEC-ID: DEC-009
Timestamp: 2026-10-31 Europe/Brussels
Problem: ACT/IINACT et un domaine Combat figuraient comme étude complémentaire ; dépôt git ajouté avec les archives des points de contrôle suivies.
Evidence: Demande utilisateur 2026-10-31 : « retire tout ce qui touche à ACT, on va pas le faire » ; « j'ai ajouté un repo git, pense à faire le nécessaire pour gitignore ».
Options: Garder ACT en étude facultative ; le retirer du périmètre. Garder les zips dans git ; les ignorer.
Decision: ACT/IINACT et Combat retirés du projet (docs mises à jour, aucun code concerné). Archives checkpoints/*.zip ignorées par git (gardées sur disque) ; logs et notes de vérification restent suivis.
Reason: Décision de périmètre de l'utilisateur ; l'historique git conserve déjà chaque version du code.
Consequences: Les zips restent créés à chaque version mais hors dépôt. Les commits sont faits automatiquement par l'application de bureau, pas par l'agent. Les zips déjà présents dans l'historique local ne sont pas réécrits.
References: .gitignore, docs/ai/PROMPT-MAITRE.md §5, CLAUDE.md

DEC-ID: DEC-010
Timestamp: 2026-10-31 Europe/Brussels
Problem: Les sauvegardes (zips de points de contrôle) et la documentation à plat devenaient lourdes et peu lisibles une fois le dépôt sur GitHub ; il fallait aussi accueillir des contributeurs.
Evidence: Demande utilisateur 2026-10-31 : « les backup ne sont plus utile si on utilise github », « réorganiser les md … suivi complet et reprise IA complète », « prendre en compte dev par la communauté open source », règles D17.
Options: Garder checkpoints/ suivi ; l'ignorer entièrement. Documents à la racine ; rangement par public.
Decision: `checkpoints/` entièrement ignoré par git (local), plus de zip par version ; preuves citées par le code dans `docs/evidence/` ; historique par version dans `CHANGELOG.md` ; documents rangés en `docs/ai/`, `docs/project/`, `docs/goals/` avec la carte `docs/README.md` ; `CONTRIBUTING.md` (règles, licences, publication Dalamud et politique IA). Remplace la conséquence « zips créés à chaque version » de DEC-009.
Reason: L'historique git conserve le code de chaque version ; une entrée par public (joueur, contributeur, agent) rend la reprise plus sûre.
Consequences: L'utilisateur fait les commits (GitHub Desktop). Les zips déjà présents dans l'historique git n'en sont retirés que sur demande explicite (réécriture d'historique). Les gros documents de `docs/project/` gardent leur partie historique, marquée comme archive.
References: .gitignore, CHANGELOG.md, docs/README.md, CONTRIBUTING.md, CLAUDE.md

DEC-ID: DEC-011
Timestamp: 2026-10-31 Europe/Brussels
Problem: Ordre et nature des sources affichées pour « Comment l'obtenir » ; place de l'outil ALEA.
Evidence: Retours utilisateur 0.123–0.146 : « achetable au marché est un critère pas une source », « ne pas utiliser Garland si on connaît déjà », « retire l'outil ALEA en gardant la partie faite par la carte ».
Options: Tout interroger en parallèle ; données du jeu puis repli communautaire.
Decision: Données du jeu d'abord (marchands, échanges, quêtes, succès, artisanat, récolte) ; Garland Tools seulement si aucune source n'est connue ; puis section « Acquisition » du wiki, traduite dans la langue du plugin. Le marché est un critère, pas une source. L'outil ALEA est retiré (0.123) ; les ALEA restent sur la carte.
Reason: Moins de requêtes externes, données du jeu prioritaires et vérifiables.
Consequences: Certaines sources restent « inconnues » ; ne pas les déduire.
References: docs/project/ARCHITECTURE.md (Sources d'objets), CHANGELOG.md 0.123–0.150

DEC-ID: DEC-012
Timestamp: 2026-11-01 Europe/Brussels (avant 0.224)
Problem: Fixer ce qui reste à faire avant la publication D17 et ce qui ne sera pas fait.
Evidence: Revue des points ouverts avec l'utilisateur (docs/project/BACKLOG.md, « Revue des points ouverts (2026-11-01) ») ; sources vérifiées par point (GatheringOptimizer MIT, frozen_rabbit_tome sans licence, feuille FccShop, sous-commandes /alarm).
Options: Tout traiter avant publication ; publier en l'état ; trier point par point.
Decision: Faire : bonus d'Abondance estimé (0.224), coffre à apparence et armoire dans « possédé » (0.235–0.240), boutiques de compagnie libre (0.295), icônes de quête 71141 / 71151 seulement avec un relevé en jeu. Ne pas faire : score de la Revue de mode (aucune donnée, serait inventé), suppression d'une alarme du jeu depuis Tensho (DeleteAlarm non prouvé, risque d'effacer la mauvaise), sources inconnues restantes (inconnu documenté). Abandonné : injection de texte dans l'infobulle native du jeu.
Reason: Ne livrer que ce qui est prouvé par les données du jeu ou une source citée ; éviter les actions à risque sur l'état du jeu.
Consequences: Les « Ne pas faire » ne sont pas rouverts sans nouvelle source ou nouvelle demande. Les compléments des 17 outils viennent après la publication, au cas par cas.
References: docs/project/BACKLOG.md, docs/ai/STATUS.md (« PLAN AVANT PUBLICATION »), CHANGELOG.md 0.224, 0.235–0.240, 0.295

DEC-ID: DEC-013
Timestamp: 2026-11-01 Europe/Brussels (0.236)
Problem: Compter l'arsenal, le coffre à apparence et l'armoire dans les stocks et le « possédé » de la Revue de mode.
Evidence: Décision de l'utilisateur notée en CHANGELOG 0.236 (« ne pas dépendre d'un autre plugin », Allagan Tools écarté) ; API vérifiées par réflexion de FFXIVClientStructs (InventoryType de l'arsenal, MirageManager.PrismBoxItemIds, UIState.Cabinet).
Options: S'appuyer sur Allagan Tools ; lire le jeu directement.
Decision: Lecture directe du jeu sur le thread framework, copie dans la Banque (arsenal en 12 conteneurs ; coffre et armoire en conteneurs virtuels 25000 et 25001), relue toutes les 2 min au plus ; coffre et armoire « inconnus » tant qu'ils n'ont pas été ouverts en jeu.
Reason: Aucune dépendance à l'installation d'un autre plugin ; même modèle que les autres rangements (DEC-007).
Consequences: Rappel « ouvrez une fois… » tant que le coffre ou l'armoire ne sont pas lus ; une tenue rangée compte par ses pièces (feuille MirageStoreSetItem, 0.240) ; limites du fichier de la Banque relevées en 0.238–0.239. 0.234–0.240 à valider en jeu.
References: CHANGELOG.md 0.235–0.240, src/TenshoAssistant/Inventory/BankInventoryBridge.Dalamud.cs, docs/project/REFERENCES.md REF-BANK-STORAGES

DEC-ID: DEC-014
Timestamp: 2026-11-01 Europe/Brussels (0.246)
Problem: Les fenêtres de Tensho passent au-dessus des fenêtres du jeu (demande R-05).
Evidence: docs/evidence/0.246-r05-window-order.md : Dalamud dessine toutes les fenêtres ImGui des plugins après l'interface du jeu, sans API pour les intercaler ; le menu de barre de titre de Dalamud (épingler, clic traversant, opacité) existe déjà.
Options: A. retrait automatique (fenêtre Tensho presque transparente quand une fenêtre du jeu la chevauche) ; B. raccourci pour masquer / réafficher ; C. rien de plus.
Decision: C, décision de l'utilisateur ; R-05 clos sans code.
Reason: Un vrai ordre d'affichage est impossible ; l'outil de Dalamud suffit.
Consequences: Quand une infobulle du jeu est cachée par une fenêtre Tensho, Tensho dessine la sienne (barre d'informations du serveur, 0.300–0.301).
References: docs/project/USER-REQUESTS.md (R-05), CHANGELOG.md 0.301

DEC-ID: DEC-015
Timestamp: 2026-11-01 → 2026-11-02 Europe/Brussels (0.246, 0.270, 0.281)
Problem: Après un échec réseau, LuminaSupplemental et GatherBuddy relançaient le téléchargement à chaque image ; une copie expirée était perdue hors ligne ; les annonces du marché n'avaient pas de cache disque.
Evidence: docs/evidence/0.246-cache-audit.md (relevé dans le code le 2026-11-01, mise à jour 0.281) ; docs/evidence/0.270-universalis-listings.md.
Options: Corriger au cas par cas ; règle commune à toutes les sources.
Decision: 5 min d'attente après un échec ; la copie expirée sert quand le réseau échoue ; chaque source lit la durée de sa zone de cache (Paramètres › Données), avec « Jamais (seulement sur demande) » ; réponses bornées en taille et en durée. Universalis : annonces et historique lus depuis la page Marché seulement, une requête à la fois, 3 s d'écart, 4 Mo au plus, dernière lecture affichée et datée en cas d'échec, aucune requête pour un objet non vendable.
Reason: Ne jamais solliciter une source en boucle ; garder une donnée datée plutôt que rien.
Consequences: Toute nouvelle source réseau suit la même règle (BoundedHttp ou bornes équivalentes, cache disque daté, zone de cache). La réponse de Teamcraft n'est contrôlée qu'après la lecture (Community/FishBiteTimes.cs), à revoir.
References: src/TenshoAssistant/Community/BoundedHttp.cs, src/TenshoAssistant/Caching/DataCache.cs, src/TenshoAssistant/Market/MarketBoardService.cs, CHANGELOG.md 0.246, 0.270, 0.281

DEC-ID: DEC-016
Timestamp: 2026-11-01 → 2026-11-02 Europe/Brussels (0.247–0.269, 0.315)
Problem: Infobulles réduites et complètes empilées pour un même objet, menus de clic droit en double ou différents selon l'endroit.
Evidence: Retours en jeu (captures) 0.250, 0.251, 0.252, 0.269 ; plan docs/goals/R-06.md étape 1.
Options: Corriger liste par liste ; modèles communs.
Decision: Modèles communs (src/TenshoAssistant/UI/Components/ItemCell.cs, EntityMenus.cs, DetailPage.cs, SettingRow.cs). Toute icône d'objet donne l'infobulle complète et le menu des outils et des sources ; une seule infobulle d'objet par image (ItemCell.cs) et une seule infobulle par ligne de trajet (0.315) ; chaque menu attaché a un identifiant unique et stable (UniqueMenuId, 0.269) ; un menu par type de chose (quête, PNJ, monstre, lieu) ; même contenu pour l'infobulle Tensho et le panneau sous l'infobulle de l'inventaire du jeu (DrawItemInfoBody, 0.257), mêmes sources pour les deux menus de clic droit (SourceLines, 0.256).
Reason: Même comportement partout, moins de code dupliqué, moins de travail par image.
Consequences: Une nouvelle liste réutilise ces modèles ; pas d'infobulle « nom seul » ni de second menu superposé. Pas d'injection dans l'infobulle native (DEC-012).
References: CHANGELOG.md 0.247–0.269, 0.315 ; docs/goals/R-06.md

DEC-ID: DEC-017
Timestamp: 2026-11-01/02 Europe/Brussels (0.248 ; date exacte non relevée)
Problem: Le paquet livrait une copie des fichiers de langue à côté de la DLL.
Evidence: Demande de l'utilisateur : « tout doit être dans la même DLL » (CHANGELOG 0.248).
Options: Garder les fichiers à côté ; tout intégrer à la DLL.
Decision: Langues et données intégrées à la DLL (EmbeddedResource) ; le paquet contient seulement TenshoAssistant.dll, TenshoAssistant.json et TenshoAssistant.deps.json ; Verify.ps1 refuse un fichier de langue livré. Un dossier Localization/Languages posé à la main à côté de la DLL reste lu en surcharge, pour tester une traduction.
Reason: Installation et mise à jour en un seul fichier.
Consequences: Les données communautaires embarquées (0.309, 0.315) suivent la même règle (src/TenshoAssistant/TenshoAssistant.csproj).
References: scripts/Verify.ps1 (contrôle 0.248), Localization/README.md, CHANGELOG.md 0.248

DEC-ID: DEC-018
Timestamp: 2026-11-01 → 2026-11-02 Europe/Brussels (0.249, 0.258, 0.287, 0.289, 0.315)
Problem: Des réglages sans aide ni retour au défaut ; « remettre les réglages par défaut » doit viser tous les réglages sans perdre les données de l'utilisateur.
Evidence: Précision de l'utilisateur (0.258) ; défaut trouvé pendant 0.315 : « Tout remettre par défaut » effaçait les vaisseaux lus, les plans d'équipement et les plans de matérias.
Options: Réinitialiser toute la configuration ; réinitialiser les seuls réglages, données enregistrées gardées.
Decision: Chaque réglage a son infobulle (anglais et français ; un test fait échouer Verify si elle manque) et un bouton ↺ quand il diffère du défaut, lu sur une configuration neuve. « Tout remettre par défaut » (avec confirmation) remet les propriétés listées dans ConfigurationReset.Reset (Configuration.cs, depuis 0.321) ; ConfigurationReset.Kept garde version, langue, alarmes, rappels faits, carte au trésor, plan de macros, favoris de carte, préréglages d'artisanat, decks de Triple Triad, quêtes suivies, relevés, profils et modèles de tri ; depuis 0.315 aussi vaisseaux lus, plans d'équipement, matérias, secteurs débloqués, plans de montages ; depuis 0.321 l'état de l'interface (groupes de navigation repliés) ; un test exige que chaque propriété publique soit dans exactement une des deux listes. La banque (fichier à part) n'est pas touchée. Valeurs par défaut de la carte reprises des réglages de l'utilisateur (0.289) ; configurations déjà enregistrées inchangées.
Reason: Un retour au défaut ne doit jamais détruire ce que l'utilisateur a lu ou préparé.
Consequences: Toute nouvelle propriété de Configuration va dans ConfigurationReset.Kept (données) ou ConfigurationReset.Reset (réglages), dans Configuration.cs ; depuis 0.321 un test exige que chaque propriété publique soit dans exactement une des deux listes.
References: src/TenshoAssistant/UI/Components/SettingRow.cs, tests/TenshoAssistant.Tests/Program.cs (infobulles des réglages), CHANGELOG.md 0.249, 0.258, 0.287, 0.289, 0.315

DEC-ID: DEC-019
Timestamp: 2026-11-02 Europe/Brussels (0.275–0.315)
Problem: Plusieurs vues transformaient une donnée non lue en « aucun », « non obtenu » ou 0.
Evidence: Règle durable (docs/ai/RULES.md, DEC-003) ; cas relevés en 0.275, 0.282, 0.285, 0.296, 0.298, 0.315 ; SubmarineTracker renvoie 0 pour un secteur sans seuil (docs/evidence/0.315-vessel-builds.md).
Options: Valeur par défaut (0, croix, onglet masqué) ; état « inconnu » explicite.
Decision: Inconnu reste inconnu dans l'interface : pas de pastille ✓/✗ tant que les collections ne sont pas lues (0.275) ; un catalogue pas encore chargé garde son onglet (0.282) ; sans quête confirmée, aucune quête d'une autre cité n'est masquée (0.285) ; Grande Compagnie inconnue ou aucune : tous les sceaux restent affichés (0.296) ; type de marqueur des mandats non deviné, à relever en jeu (0.298) ; seuils d'un trajet « inconnus » si un secteur manque, jamais 0, nom de montage « ? » si une pièce manque, compagnie non lue = pas de filtre des secteurs débloqués (0.315).
Reason: Une valeur devinée est pire qu'un manque signalé.
Consequences: Les tests des modèles purs couvrent ces cas (VesselBuilds, QuestStartCities…) ; tout nouvel écran distingue lu, absent et inconnu.
References: src/TenshoAssistant/FreeCompany/VesselBuilds.cs (RouteBreakpoint), src/TenshoAssistant/MainWindow.SourceLinks.cs, CHANGELOG.md 0.275–0.315

DEC-ID: DEC-020
Timestamp: 2026-11-02 Europe/Brussels (0.311)
Problem: L'utilisateur demande d'essayer l'équipement des autres jobs : « on peut mirager tout le stuff maintenant (vérifie) ».
Evidence: Patch 7.4 (décembre 2025) : toute armure ou tout accessoire peut servir de mirage, quels que soient le job et le niveau ; armes et outils restent liés au job (presse : PC Gamer, Game Informer ; communiqué du patch, cités en CHANGELOG 0.311). Règle non sondée dans les données du jeu.
Options: Garder les listes du job ; ouvrir toutes les pièces sans distinction ; ouvrir armures et accessoires seulement, marqués.
Decision: Filtre « Tous les jobs (mirage) », désactivé par défaut : armures et accessoires de tous les jobs et niveaux ; les armes restent celles du job ; une pièce que le personnage ne peut pas porter est marquée « Mirage », essayable et partageable, et les statistiques gardent la pièce portée.
Reason: Suivre la règle du jeu actuelle sans fausser les statistiques prévues.
Consequences: Si la règle du jeu change, mettre à jour GearCatalog (AllJobs) ; page validée en jeu par l'utilisateur (rapport 2026-11-02, 0.310–0.314).
References: src/TenshoAssistant/Gear/GearCatalog.cs, src/TenshoAssistant/Configuration.cs, docs/goals/R-11.md étape 1b

DEC-ID: DEC-021
Timestamp: 2026-11-02 Europe/Brussels (0.309, 0.315)
Problem: Le butin et les seuils des sous-marins ne sont pas dans les données du jeu (décidés côté serveur).
Evidence: docs/evidence/0.309-submarine-routes.md, docs/evidence/0.315-vessel-builds.md ; licences MIT lues (SubmarineTracker, FFXIVGachaSpreadsheet, (c) 2023 Infi).
Options: Télécharger à l'exécution ; embarquer une copie datée ; ne pas proposer la fonction.
Decision: Embarquer seulement une donnée sous licence compatible avec la GPL v3 (MIT à ce jour), avec dans le fichier la source, le commit ou l'empreinte SHA-256, la licence et le copyright (texte MIT complet dans submarine-breakpoints.csv ; ligne « Source » dans submarine-loot.json), crédit dans le README ; valeurs gardées telles quelles ; transformation par un script versionné (tools/submarine-loot.py). Une donnée sans licence (xivgear.app, seuils de dirigeables d'Argus, frozen_rabbit_tome) est lue seulement, jamais embarquée.
Reason: Seule façon d'offrir ces outils sans inventer ; licence et crédit respectés.
Consequences: Pas de butin de dirigeable (aucune donnée sous licence compatible) ; planificateur par distance et durée et arbre de déblocage de LuminaSupplemental (GPL-3.0, embarqué avec avis) décidés le 2026-11-02 (DEC-023) ; mettre à jour = nouveau téléchargement et nouvelle empreinte. Les seuils viennent à l'origine d'une feuille sans licence (Elena / saerielle), reprise par SubmarineTracker sous MIT.
References: README.md (Credits), src/TenshoAssistant/FreeCompany/Data/, docs/project/REFERENCES.md (REF-FC-SUBMARINETRACKER, REF-FC-GACHASPREADSHEET)

DEC-ID: DEC-022
Timestamp: 2026-11-01 → 2026-11-02 Europe/Brussels (0.224, 0.277, 0.315)
Problem: Plusieurs calculs reposent sur des règles absentes des données du jeu (communautaires ou propres à Tensho).
Evidence: docs/evidence/0.224-bountiful-base.md, docs/evidence/0.315-vessel-builds.md ; CHANGELOG 0.277 (score de carte).
Options: Les présenter comme des données ; les signaler comme estimations, avec leur source.
Decision: Libellé « communautaire » ou « estimé » dans l'interface et les infobulles, source citée, valeur modifiable quand elle sert d'entrée : seuils de secteurs (« seuils communautaires »), usure (« estimation communautaire »), règle « coût ≤ capacité » (« outils communautaires »), bonus d'Abondance (« estimé », modifiable), score de carte de Triple Triade (« estimation Tensho », formule en infobulle). Les statistiques de vaisseau lues sont comparées au calcul : un écart est signalé (⚠), pas corrigé en silence.
Reason: L'utilisateur doit savoir ce qui vient du jeu et ce qui est une estimation.
Consequences: Une règle communautaire non vérifiée en jeu le reste dans l'interface jusqu'à un relevé ; voir docs/project/REFERENCES.md « Règles communautaires reprises comme faits ».
References: src/TenshoAssistant/Localization/Languages/fr.json (VesselBuilds.Sources, VesselBuilds.SurveillanceTip, VesselBuilds.Mismatch, TriadSort.ScoreHelp), src/TenshoAssistant/FreeCompany/VesselBuilds.cs

DEC-ID: DEC-023
Timestamp: 2026-11-02 Europe/Brussels (0.317)
Problem: Points « À décider / à revoir » de la feuille de route (0.316), tranchés par l'utilisateur.
Evidence: docs/project/ROADMAP.md (« Décidé le 2026-11-02 ») ; docs/evidence/0.315-vessel-builds.md ; docs/evidence/0.309-submarine-routes.md ; docs/evidence/0.312-materia.md ; fr.json au 0.316 (44 « vous », 14 textes au « tu »).
Options: Celles listées dans ROADMAP au 0.316, une question par point.
Decision: (1) Seuils des sous-marins : garder la table SubmarineTracker et signaler les 19 secteurs où elle est sous toutes les observations (« non confirmé par les relevés »). (2) Arbre de déblocage des dirigeables : données de LuminaSupplemental (GPL-3.0) embarquées dans la DLL avec l'avis de licence. (3) Dirigeables : planificateur de trajets par distance et durée, butin affiché « inconnu ». (4) Valeur des trajets de sous-marin : prix du marché (Universalis) en plus du prix PNJ, et paliers de surveillance / récupération si la source sépare les relevés par palier (sonde d'abord). (5) R-11 étape 4 : barde de chocobo et escouade (sonde d'abord pour l'escouade). (6) Rappels de l'escouade et des Planeurs d'Ehcatl : sonder une heure lisible dans la mémoire du jeu, sinon laisser de côté. (7) R-09 : après la file c) → f), données du jeu sondées d'abord. (8) Matérias : taux de réussite de la surmatérialisation lu dans les données du jeu (feuille à sonder). (9) Publication D17 : à la version 1.0.0, moment fixé par l'auteur. (10) Traductions allemande et japonaise : gardées dans le paquet, repli anglais. (11) Cadres et textures du jeu (ULD) : avant D17. (12) Textes français : vouvoiement.
Reason: Réponses de l'utilisateur du 2026-11-02 (« D'abord les décisions »).
Consequences: DEC-021 change sur deux points : l'arbre de déblocage GPL-3.0 est embarqué (même licence que Tensho, avis dans le fichier) et un planificateur de dirigeable sans butin est proposé. Les points restants de la feuille de route attendent un relevé en jeu ou une étape de la file. Le modèle `FilteredTable` est retiré du plan (choix de l'agent, règle « pas d'abstraction prématurée » ; à créer au premier besoin réel répété). Le vouvoiement est gardé par un test automatique.
References: docs/project/ROADMAP.md, docs/ai/STATUS.md, docs/goals/R-11.md, docs/project/PUBLISHING.md, tests/TenshoAssistant.Tests/Program.cs (« French texts address the player with « vous » »)

DEC-ID: DEC-024
Timestamp: 2026-11-02 Europe/Brussels (0.318)
Problem: Règle de numérotation des versions du plugin (`<Version>` du csproj, tags `Stable-x.y.z`).
Evidence: Message de l'utilisateur du 2026-11-02 : « 0.. = numéro de version des grosses étapes du genre pour le premier release Dalamud ; .0. = les versions que je fais (nous) ; ..0 = version de la communauté ».
Options: Numérotation libre ; SemVer classique (majeur, mineur, correctif) ; règle de l'utilisateur.
Decision: X.y.z = grandes étapes, sur décision de l'utilisateur seulement (1.0.0 = première publication sur le dépôt officiel Dalamud, DEC-023) ; x.Y.0 = versions de l'utilisateur et de l'agent (le troisième nombre revient à 0) ; x.y.Z = versions de la communauté (contributions extérieures, numéro fixé par le mainteneur à la fusion, une PR peut le proposer).
Reason: Décision de l'utilisateur ; la numérotation actuelle (0.317.0) la suit déjà.
Consequences: L'agent monte seulement le deuxième nombre ; jamais le premier sans demande ; le troisième est réservé aux contributions. Documenté dans CLAUDE.md (« À chaque version livrée ») et CONTRIBUTING.md (« Versions and releases »).
References: CLAUDE.md, CONTRIBUTING.md, src/TenshoAssistant/TenshoAssistant.csproj

DEC-ID: DEC-025
Timestamp: 2026-11-02 Europe/Brussels (0.318)
Problem: Quels manques du plan c) ajouter à Tensho, dans quel ordre, et avec quels choix précis.
Evidence: docs/project/GAPS.md (103 manques : 17 plugins de référence, dépôt officiel Dalamud de 500 plugins, dépôts non officiels de 658 plugins jugés règle par règle) ; docs/evidence/0.318-plugin-gaps.json (comparaison fonction par fonction, licences lues, contre-vérification).
Options: Pour chaque manque : ajouter, plus tard ou non ; ordre par valeur ou par domaine ; choix précis maintenant ou à chaque ajout.
Decision: 101 manques retenus sur 103 ; plus tard : I-14 (tri de l'arsenal et tri automatique) et X-05 (bulles des PNJ dans le chat). Ordre : domaine par domaine, Inventaire et marché → Collections → Monde → Récolte → Artisanat → Nouveaux domaines ; dans un domaine, valeur haute et petite taille d'abord. Choix précis tranchés maintenant (colonne « Choix » de GAPS.md), notamment : solveur d'artisanat optimal proposé en deux versions au choix par un réglage (C# original dans la DLL, Raphael natif) ; si Macro Chain est actif, Tensho s'appuie sur lui ; Universalis seulement au survol ou à la demande pour les prix et les ventes des servants ; un fichier par personnage, jamais d'Account ID ; alarmes d'échéance créées d'office (alarmes Tensho, jamais /alarm d'office) ; pêche en mer dans la page Récolte, aide complète d'un coup ; rotations de collectionnables propres à Tensho selon les seuils de remise ; événements saisonniers par les données du jeu seulement ; Faux Hollows : données du jeu d'abord, puis licence compatible, sinon relevés ; PaissaDB accepté en surveillance régulière (réglage), lecture seule. Licence EUPL-1.2 : compatible avec la GPL v3 (avis requis).
Reason: Choix de l'utilisateur, lot par lot dans la conversation (2026-11-02).
Consequences: Étape g) de la file (docs/project/ROADMAP.md, docs/ai/STATUS.md), après R-12, d), e) et f) ; chaque manque est une version vérifiable à part. Toute fonction qui agit d'office reste désactivable et respecte les règles Dalamud relevées dans GAPS.md (pas de requête serveur sans action du joueur, données lues seulement après ouverture par le joueur). Le solveur Raphael ajoute un fichier natif hors de la DLL : exception voulue par l'utilisateur, en option.
References: docs/project/GAPS.md, docs/project/ROADMAP.md (étape g), docs/evidence/0.318-plugin-gaps.json, CONTRIBUTING.md (licences)

DEC-ID: DEC-026
Timestamp: 2026-11-03 Europe/Brussels (0.447)
Problem: Décisions gardées pour la fin (G-PROPOSALS, ROADMAP « Décisions reportées à la fin ») : lesquelles trancher maintenant, et comment.
Evidence: Réponses de l'utilisateur du 2026-11-03, en trois lots, dans la conversation.
Options: Pour chaque point : le choix recommandé par l'agent, ou l'alternative notée.
Decision: (1) Les choix provisoires de G-PROPOSALS sont confirmés en bloc (leurs alternatives restent des idées). (2) X-04 : un simple lien vers partake.gg (page Événements), Tensho n'en lit rien. (3) X-13 : l'annonce du conducteur du train de chasse peut être envoyée au chat (/sh, /y, /p, /fc, /ls1, /cwl1) sur clic, en plus de la copie (alternative choisie par l'utilisateur, contre le défaut recommandé). (4) Le réglage « Monstres au survol », sans effet depuis la refonte du panneau, est retiré avec son code. (5) Release Stable de 0.431 à 0.448 après ce lot. (6) Fins de ligne : normalisation faite (rien à changer : le dépôt est déjà en LF). (7) GitHub : branches main et Devloppement protégées (pas de push forcé ni de suppression), tags Stable-* protégés, Dependabot et la compilation avec tests en CI activés. (8) Inventaire (défaut 7 de l'audit 0.329) : DEC-004 gardée, l'éditeur de règles du tri reste affiché pendant la relecture. (9) Mémoquartz : seuil de 80 % comme les autres monnaies. (10) Raphael (A-03, A-04) : étudié en séance (licence, build Plogon, taille) avant tout code. (11) N-06 et N-14 : chercher une source communautaire, licence vérifiée et soumise avant de coder. (12) N-08 : journal de jardinage avec des durées tapées par le joueur.
Reason: Choix de l'utilisateur (2026-11-03).
Consequences: 0.447 (points 2, 3, 4, 8), 0.448 (point 12) ; GitHub et CI hors du plugin ; issue #24 mise à jour ; G-PROPOSALS et ROADMAP marqués « DÉCIDÉ 2026-11-03 ». L'envoi au chat (point 3) est la seule fonction de Tensho qui écrit dans un canal visible des autres joueurs : clic seulement, canal parmi une liste fixe, texte nettoyé, 5 s d'écart, vérifié de nouveau avant l'appel au jeu.
References: docs/project/G-PROPOSALS.md, docs/project/ROADMAP.md, tests/TenshoAssistant.Tests/Decisions447Tests.cs, GitHub issue #24

DEC-ID: DEC-027
Timestamp: 2026-11-03 Europe/Brussels (0.449)
Problem: Sources de N-06 (escouade) et N-14 (croisements), DEC-026 point 11 ; recherche : docs/evidence/0.448-n06-n14-sources.md.
Options: N-06 depuis le jeu seul avec sonde, ou attendre les tests en jeu ; N-14 avec les paires MIT de nick75g, ou pas de N-14 ; N-08 durées pré-remplies (Lotlab GPL-3.0) ou tapées seulement.
Decision: (1) N-06 : codé depuis les feuilles du jeu (GcArmyExpedition, GcArmyMemberGrow, GcArmyTraining) et FFXIVClientStructs, avec une sonde en jeu ; le pourcentage reste « non validé en jeu » jusqu'à la sonde. (2) N-14 : paires MIT de nick75g/FFXIV-Crossbreed-Helper, reliées aux objets par les noms anglais de la feuille Item et affichées dans la langue du jeu (« corrélation vers le fr », choix de l'utilisateur) ; noms non trouvés écartés et comptés. (3) N-08 : durées de pousse et d'entretien pré-remplies depuis Lotlab/FFXIV-Gardening-Tracker (GPL-3.0), toujours modifiables.
Reason: Choix de l'utilisateur (2026-11-03) ; la règle de réussite de l'escouade est dans les feuilles du jeu ; seules ces deux sources ont une licence compatible.
Consequences: 0.449 (point 3), 0.450 (point 2), 0.451 (point 1) ; données embarquées avec leur avis de licence, crédits ajoutés ; aucune requête réseau.
References: docs/evidence/0.448-n06-n14-sources.md, docs/project/G-PROPOSALS.md

DEC-ID: DEC-028
Timestamp: 2026-11-03 Europe/Brussels (séance ultracode, après Stable-0.451.0)
Problem: A-03 / A-04 : dépendance native Raphael (licence, build hors ligne de Plogon, taille), et solveur des suggestions pendant la synthèse.
Evidence: docs/evidence/0.451-raphael-study.md (Raphael.Net 5.0.0 MIT, raphael-rs Apache-2.0, Craftimizer publié sur D17 avec la même version figée par packages.lock.json, raphael_bindings.dll win-x64 566 784 octets).
Options: Raphael en option ou solveur C# seul ; A-04 avec le solveur Tensho (exact, depuis l'état observé) ou Raphael relancé (approximatif).
Decision: (1) A-03 : Raphael.Net 5.0.0 ajouté en option, réglage « Solveur : Tensho / Raphael », Tensho par défaut ; chaque macro Raphael rejouée dans CraftSimulation avant d'être proposée ; calcul hors du thread du jeu, annulable, un seul à la fois ; avis MIT et Apache-2.0, crédits. (2) A-04 : suggestions pendant la synthèse avec le solveur C# de Tensho, depuis l'état observé (condition et effets compris), affichage seulement.
Reason: Choix de l'utilisateur (2026-11-03, séance ultracode).
Consequences: Ordre de la séance : R-14 et A-14 d'abord, puis I-05, I-15, N-10, A-03 / A-04 ; package vérifié (raphael_bindings.dll dans latest.zip).
References: docs/evidence/0.451-raphael-study.md, docs/project/G-PROPOSALS.md, GitHub issue #28
