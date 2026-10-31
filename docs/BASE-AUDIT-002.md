# BASE-AUDIT-002 — Bilan de la base 0.59

2026-10-29. Audit ciblé de livraison et de périmètre ; aucune validation de jeu ajoutée.

- Liste autoritaire comparée à la matrice ROADMAP et au répertoire du code : 17 noms distincts présents, aucun manquant. Le répertoire a 18 entrées car la banque commune s’ajoute aux 17 références prioritaires. Aucun plugin prioritaire supprimé.
- Les 17 couvertures de la matrice restent PARTIAL. Cet audit ne prouve ni leur remplacement complet ni le fonctionnement runtime des nouveaux lecteurs.
- 61 fichiers Markdown hors bin/obj/checkpoints inspectés automatiquement pour leurs liens locaux, exemples de code exclus : 71 liens résolus, aucun manquant. Ce contrôle de liens n’est pas une relecture sémantique de tous les anciens documents historiques.
- DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll, version 0.59.0.0. Paquet latest.zip dans le sous-dossier TenshoAssistant : deps.json, DLL et manifeste. Une seule DLL présente, aucune dépendance hôte redistribuée.
- Verify.ps1 final exécuté : 385/385 tests, compilation sans avertissement/erreur, identité/API/version, entrées uniques et SHA-256 DLL/manifeste identiques aux fichiers canoniques. Pas de nouvelle répétition après les seules modifications documentaires.
- La copie 0.5.22 sous bin/legacy-Release-20261027 est une archive conservée, pas une sortie de build. L’ancien bin/Release est explicitement invalidé dans README. Aucun fichier utilisateur supprimé.
- Newtonsoft None/Auto/Objects avec profils et bibliothèque passants. FashionProbe exécuté sans réseau sur les fichiers du jeu : emplacements natifs positifs et exclusions de slots bloqués confirmés. Les probes antérieurs gardent leurs dates et limites propres.

La base SortaKinda possède maintenant critères multiples, profils par scope, zones protégées/filtrées, inversion, métadonnées d’équipement/marché/HQ, modèles réutilisables et application explicite après capture directe. Les tests managés n’exécutent pas le bridge mémoire ou l’interface du jeu. Rendu, capture réelle, persistance de l’ordre et autres cas avancés demeurent à confirmer. Aucun état FUNCTIONAL/REPLACEABLE revendiqué par cet audit.

Les validations utilisateur jusqu’à 0.5.27 restent les seules confirmations runtime consignées. Les essais de projection craft et Macro Chain sont différés à la demande de l’utilisateur : ne pas les redemander pour poursuivre un travail indépendant. Le bonus natif d’Abondance reste inconnu face aux sources divergentes ; ne pas fabriquer une formule.

Point de reprise : vérifier le quota réel et conserver 5 %. Ensuite reprendre une tranche indépendante de GatherBuddy/pêche : inspecter le catalogue et ses feuilles/sources avant d’ajouter des informations d’appâts ou de prérequis. Garder les identifiants Item/point/poisson distincts et toute donnée non prouvée inconnue. Les 17 outils restent intégralement au périmètre.
