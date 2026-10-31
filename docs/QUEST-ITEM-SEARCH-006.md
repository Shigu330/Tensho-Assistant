# QUEST-ITEM-SEARCH-006 — Recherche des sources de quête

2026-10-29, version 0.70.0. Extension de l’index commun 0.68 ; aucune lecture native ou requête réseau supplémentaire.

Dans Usages, ouvrir « Quêtes proposant cet objet ». Le filtre accepte plusieurs mots séparés par des espaces : chaque mot doit correspondre au nom, à la catégorie native ou à l’identifiant entier exact de la quête. Nom/catégorie ignorent la casse ; aucun comportement sans accents n’est promis. Effacer restaure toutes les sources indexées de cet objet. Le filtre est conservé quand on change d’objet, et un bouton permet de l’effacer.

La catégorie est visible dans sa colonne ; le tooltip donne nom, catégorie et ID. Le nombre filtré/total et le message vide distinguent absence de correspondance et absence de source dans cet index. Cliquer ouvre la fiche existante et conserve toutes les variantes. Aucun choix automatique, somme des variantes ni disponibilité ne sont déduits.

QuestRewardIndex.Search retourne des listes readonly sans modifier l’index. Les résultats UI ne sont recalculés que lorsque l’index, l’objet ou la requête changent. Le parcours reste limité aux candidats de cet objet ; le clipper existant est conservé. Le cache retient au plus le dernier index statique et une liste de résultats, sans progression ou pointeur natif.

Verify.ps1 exécuté : 435/435 tests, zéro avertissement/erreur, paquet/API/version/SHA-256 et exclusion hôte passants. Le nouveau cas couvre mots combinés entre champs, casse/espaces, ID entier, isolation entre objets, conservation des variantes, restauration et readonly. Les autres tests de lifecycle de l’index restent verts. Pas de validation de rendu en jeu.

DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll (0.70.0.0). Aucun nouvel essai en jeu demandé ; les tests craft/Macro Chain restent différés. Les 17 domaines restent PARTIAL. Vérifier le quota et conserver 5 % avant toute nouvelle tranche.
