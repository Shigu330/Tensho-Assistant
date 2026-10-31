# Base multi-outils — 0.14.0 — 2026-10-27

NOTE HISTORIQUE : ce document décrit la tranche 0.14.0. Depuis 0.35.0, les 17 domaines ont une première capacité PARTIAL ; ROADMAP.md et STATUS.md font autorité. Les anciennes mentions NOT_STARTED et « sans horaires/prérequis/survol » ci-dessous restent le constat de cette ancienne tranche uniquement, pas l'état actuel.

Complément 0.14.0 : navigateur Récolte & pêche avec recherche de ressource, zone/lieu et icônes métier ; liste avec clipper et filtres mémorisés, sélection dans le contexte commun et liens Stocks/Recettes/Prix. Les accès contextuels et liens de craft ouvrent maintenant directement les outils disponibles. Quêtes : résultats/catégories/compteurs mémorisés. Lecteur du monde marché : garde de disposal, invalidation et reprise après erreur. Catalogue Recipe : vues publiques et ingrédients désormais protégés contre les mutations par cast. Vérification actualisée : 83/83 tests et package 0.14.0 verts.

Essai supplémentaire : dans Accueil & outils → GatherBuddy, chercher « bouleau » ou « marron », puis filtrer une zone et un métier. Cliquer un objet, vérifier le contexte et ses lieux, puis Stocks/Recettes/Prix. Aucun appel Universalis ne doit partir avant clic sur Consulter. Vérifier les listes sur fenêtre étroite et les liens depuis Craft lié.

Ce checkpoint remplace les états READY/TODO historiques des tranches ci-dessous. Les 17 capacités globales restent dans ROADMAP.md ; aucune n'est déclarée REPLACEABLE.

## Périmètres réellement implémentés

| Tranche | Capacité | Limites |
|---|---|---|
| GATHER-001A | Sources récolte/pêche, objets cachés, filtre métier, niveau et zone, carte | Sources statiques ; carte au centre de zone ; sans météo/horaires/harpon |
| SORT-001A | Aperçu par nom/catégorie/quantité/qualité, règle persistée | Piles et slots originaux préservés ; aucune application native |
| COLL-001A | Montures, mascottes, emotes ; recherche et acquisition confirmée | Un flag négatif reste non confirmé, pas absent |
| MARKET-001A | Universalis NQ/HQ par monde, consultation manuelle et cache daté | Sans survol ; aucune substitution silencieuse monde/DC |
| QUEST-001A | Catalogue, états actif/terminé/non confirmé, séquence native | Sans prérequis ALL/ANY ni pourcentage inventé |
| BLU-001A | Grimoire, numéro, rang, description, lieux disponibles et acquisition | Numéro affiché distinct de l'identifiant natif AozAction |
| CHAR-001A | Attributs natifs combat/artisanat/récolte | Sans formules dérivées ni pourcentages calculés |
| CACT-001A | Meilleure révélation puis meilleure ligne Mini Cactpot | Saisie manuelle de 1 à 4 chiffres ; aucune lecture/action sur le ticket |

Craftimizer et Item Use conservent les recettes/carnet/préparation existants. Avant-Garde, Duty Loot Preview, LazyGatherer, Macro Chain, Mappy, Monster Loot Hunter et TriadBuddy restent NOT_STARTED dans leur périmètre global.

## Validation exécutée

Verify.ps1 : Release 0.14.0.0, 83/83 tests réussis ; identité/version du package, ZIP et exclusion des assemblies hôte vérifiés. DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll. Le csproj impose cette sortie même en build direct. Ancien bin/Release archivé sous bin/legacy-Release-20261027, plus utilisé.

Nouveaux tests : jointures/déduplication/immutabilité des sources ; preview préservant piles/HQ/slots, refus des données partielles et persistance ; invalidation des collections/quêtes par propriétaire et déconnexion ; parsing marché NQ/HQ/monde/dates, cache manuel et annulation ; attributs copiés ; solveur Mini Cactpot (espérance de référence avec 1 dans un coin : 1677,7854166666664 PGS, ligne 1+2+3 à 10000 PGS, entrées invalides/annulation).

Lecture locale des fichiers du jeu par tools/GatheringProbe : 4 199 sources dédupliquées ; Sève de bouleau et Marron sombre reliés aux zones natives, Marron sombre marqué caché via GatheringItemPoint. 11 186 liens récolte, 2 420 liens pêche, 5 857 points. Feuilles présentes : 451 montures, 600 mascottes, 334 emotes, 5 533 quêtes. Ces nombres décrivent les données installées, pas les possessions du personnage ni une validation runtime.

RUNTIME : les retours utilisateur acceptent la base jusqu'à 0.5.27. Aucun rendu ni callback natif des nouveaux outils 0.6–0.14 n'est déclaré validé en jeu.

## Protocole en jeu après chargement de cette DLL

1. Vérifier la version 0.14.0 dans Diagnostics ; ouvrir chaque outil depuis sa fiche Accueil & outils.
2. Récolte : chercher Sève de bouleau puis Marron sombre ; contrôler métier/zone/caché. Le bouton Carte doit ouvrir la zone, sans prétendre pointer le nœud exact.
3. Aperçu de tri : choisir un inventaire complet, changer les quatre critères, sauvegarder puis recharger. L'ordre réel du jeu doit rester intact.
4. Collections/grimoire/quêtes : chercher une entrée connue, comparer à l'interface du jeu, changer de personnage puis déconnecter. Aucune ancienne confirmation ne doit survivre ; une acquisition non prouvée reste non confirmée.
5. Marché : choisir un objet et demander le prix ; contrôler monde, NQ/HQ et dates. Une donnée absente doit afficher Non renseigné. Changer de monde/personnage pendant une requête ne doit pas publier l'ancien résultat.
6. Personnage : comparer les attributs au panneau du jeu après changement de métier/équipement ; déconnecter et vérifier l'invalidation.
7. Mini Cactpot : saisir 1 dans un coin, calculer, éditer pendant le calcul, puis saisir 1/2/3 sur la première ligne et 4 ailleurs. Vérifier recommandation/espérance, erreurs sur doublons et fermeture/rechargement pendant un calcul.
8. Recharger/décharger le plugin et consulter les logs : pas de callback, requête ou worker publié après Dispose.

## Architecture et propriété

Lectures natives exclusivement sur callbacks framework, par lots bornés. Les services publient des observations managées immuables. Gathering construit l'index sur worker annulable après copie. Collections/quêtes invalident les confirmations par personnage ; les grands catalogues utilisent un clipper UI. Les attributs sont copiés une fois par seconde.

MarketService possède HttpClient (timeout 12 s, réponse bornée), cache cinq minutes/128 entrées, minimum trois secondes entre appels réseau, une requête active, annulation et génération. Seuls les IDs publics monde/objet sont transmis sur demande. Les dates de réception, upload et transaction restent distinctes.

Le solveur est une implémentation originale par récursion mémoïsée et combinaisons équiprobables ; pas de code externe copié. MainWindow possède son worker et sa génération ; édition/effacement annulent et invalident, Dispose annule le travail. L'aperçu de tri est mémorisé par snapshot/règle/catalogue.

## Références vérifiées

- REF-GATHER-LOCAL-20261027, LOCAL_VERIFIED : feuilles et jointures du jeu installé, compilation et probe local ; voir REFERENCES.md.
- REF-MARKET-20261027, CURRENT_VERIFIED : https://docs.universalis.app/ ; endpoint agrégé /api/v2/aggregated/{world}/{item}, vérifié par requête publique monde 71/objet 12892. L'absence de minimum monde n'autorise pas à substituer le DC. API utilisée, aucun code copié.
- REF-NATIVE-TOOLS-20261027, LOCAL_VERIFIED : assemblies et documentation locales Dalamud API 15, Lumina, FFXIVClientStructs ; IUnlockState, QuestManager.IsQuestAccepted/GetQuestSequence, IPlayerState.GetAttribute et AozAction/AozActionTransient. Compatibilité confirmée par build, observation réelle à effectuer.
- REF-CACT-20261027, CURRENT_VERIFIED : https://github.com/MidoriKami/EzMiniCactpot commit 63173afb3eff69aa64a3edc613a377c4053d26fa, PerfectCactpot.cs ; barème et valeur mathématique de référence seulement. Licence non établie (LICENSE absent), aucune implémentation réutilisée.

## Suite

BASE-TOOLS-001 est terminé côté intégration et vérification managée ; runtime toujours à effectuer. Prochain objectif borné : [CRAFT-CHAIN-001](CRAFT-CHAIN-001.md), préparation des sous-recettes. Les 83 tests incluent l'oracle Mini Cactpot indépendant sur 16 tickets et 120 complétions chacun.

Consolider l'intégration et les liens entre outils avant une nouvelle grande capacité. Extensions restantes : disponibilité récolte, actions collections, prix au survol, prérequis quêtes, formules de stats, adaptation native du ticket, simulation craft et autres outils de la matrice. Dernier quota observé : 61 % sur 5 h et 5 % hebdomadaires restants ; instruction utilisateur de continuer en sauvegardant chaque tranche.
