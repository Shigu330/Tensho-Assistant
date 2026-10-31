# UI-SETTINGS-001 — Réglages regroupés

2026-10-29, livraison 0.71.0. Demande : ajouter tous les réglages dans la section Réglages.

Réglages devient le point commun des préférences, avec rubriques Fenêtre, Objets & marché, Artisanat, Récolte & pêche, Mini-jeux, Macros et Profils & données enregistrées.

- Fenêtre : ouverture au chargement et à la connexion, indépendantes et sauvegardées immédiatement.
- Objets & marché : prix au survol, usages au survol et menu contextuel, avec leur sauvegarde et rollback existants. L’erreur de sauvegarde s’affiche après tous ces contrôles pour être visible au même rendu.
- Artisanat : observation de synthèse ; limites de recherche (1–100 étapes, 1–512 états, 1–500 000 transitions) ; attente supplémentaire d’export (0–3 secondes) et message sonore de fin de bloc.
- Récolte & pêche : observation du point MIN/BTN courant.
- Mini-jeux : observation du ticket Mini Cactpot et observation des faces Triple Triade.
- Macros : observation passive des transitions natives.
- Profils & données : accès aux éditeurs des profils/modèles de tri, scénarios craft enregistrés, favoris des cartes, deck Triple Triade, quêtes suivies et séquence Macro Chain.

Les cinq activations d’observation sont retirées des pages des outils/diagnostics et remplacées par des raccourcis « Réglages de l’outil ». Les anciens éditeurs des limites de recherche et options d’export sont remplacés par ce lien et un résumé des valeurs actuelles. Les paramètres d’un scénario (attributs simulés, quantité/cible, effets de récolte, règles d’un plateau) et les données persistantes restent dans leurs outils : ce sont les entrées du travail courant, distinctes des préférences de fonctionnement.

Comportement conservé : observations désactivées au rechargement, activables explicitement pour la session, aucune exécution automatique. Le changement du mode Mini Cactpot efface le ticket, retire son owner et annule la résolution précédente. Modifier les limites invalide la recherche courante via la validation existante ; modifier l’export retire le texte mis en cache. Les paramètres de recherche/export restent des réglages de session comme auparavant ; les préférences communes restent sauvegardées. Aucun champ ou format de configuration ajouté.

Vérification : scripts/Verify.ps1 final, 435/435 tests, zéro avertissement/erreur. Identité/API/version/paquet, SHA-256 DLL/manifeste et exclusion des assemblies hôte passants. Contrôle des sites d’activation et des paramètres déplacés dans les sources. Pas de nouveau test miroir d’un déplacement UI ; les tests existants des services/cycles de vie restent passants. Aucun rendu en jeu observé, aucun nouvel essai craft/Macro Chain demandé.

DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll (0.71.0.0). Source immuable : checkpoints/TenshoAssistant-0.71.0-source.zip ; journal checkpoints/0.71.0-verify.log. Les 17 domaines restent PARTIAL. La demande ponctuelle est terminée ; le développement autonome antérieur reste en pause à la réserve de quota demandée.
