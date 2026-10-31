# Décisions durables

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
Consequences: Version 0.2.0 ; contrôle visuel requis. CRAFT-001A reste READY et son plan est conservé dans docs/CRAFT-001A.md.
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
