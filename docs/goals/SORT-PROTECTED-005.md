# SORT-PROTECTED-005 — Cases protégées et sens de remplissage

0.57.0.0 — 2026-10-29 — IMPLEMENTED / TESTED ; NOT VALIDATED_IN_GAME.

Chaque zone peut maintenant « Protéger les cases actuelles ». Ses piles restent aux indices visuels observés, qualités et quantités intactes ; ses cases vides restent vides. Les zones protégées sont traitées avant toute zone filtrée, même si elles figurent plus bas dans la liste : un autre filtre ne peut pas récupérer leurs objets. L’option de protection désactive les filtres et le remplissage inversé de la zone sans effacer ses anciens filtres, utiles si la protection est retirée.

Pour une zone filtrée, « Remplir depuis la dernière case » place le premier objet trié dans sa dernière case, le suivant dans l’avant-dernière, etc. Le sens des critères de tri demeure indépendant. Les cases libres restantes de cette zone sont donc au début. Les autres objets continuent à occuper les cases non réservées, avec refus explicite si elles ne suffisent pas.

La protection exige les indices visuels complets et uniques du rangement, ses vrais slots physiques et le bon propriétaire. InventorySortScope.Extract fournit la validation partagée. Une observation ancienne sans indices, un doublon, une valeur hors capacité ou des conteneurs manquants refuse l’aperçu protégé ; aucun ordre compact n’est inventé. L’aperçu simple non protégé d’un ancien cache reste consultable, mais le cache ne peut toujours pas autoriser une application.

La protection concerne les positions visuelles des objets et l’état vide des autres cases. Aucune identité physique particulière n’est promise pour une case vide. InventoryVisualSortPlan affecte chaque trou à une case physique réellement vide, et conserve toutes les piles dans une permutation complète. Le bridge natif, ses contrôles de capture directe/âge/propriétaire/fenêtre et son écriture explicite restent inchangés.

Dans l’aperçu, une bordure verte et le tooltip signalent les cases protégées ; les zones filtrées gardent la bordure dorée. Noms, qualités et quantités restent dans le tooltip d’objet. Les deux réglages ajoutés à InventorySortZone sont facultatifs, false par défaut : les profils v1 déjà sauvegardés conservent le comportement filtré vers l’avant. Un profil combinant protection et inversion est refusé ; l’éditeur remet l’inversion à false quand la protection est cochée.

Verify.ps1 : 374/374 tests, Release sans avertissement/erreur, package/versions/ZIP/exclusion hôte verts. Cinq groupes nouveaux : priorité de protection et vrais indices physiques distincts, inversion avec trous initiaux, refus des indices absents/ambigus, compatibilité JSON et 20 scènes de protection/inversion dans les quatre scopes. Les oracles vérifient chaque cellule protégée initialement vide ou occupée, l’identité physique de chaque pile et la somme indépendante des quantités. ConfigSerializationProbe passe Newtonsoft None/Auto/Objects avec une zone inversée et une zone protégée.

Rendu, détection/capture réelle, écriture native et persistance de l’ordre non observés en jeu. Aucun nouveau test utilisateur requis pour continuer ; les essais projection craft/Macro Chain restent différés. Protocole futur : protéger une zone mixte de cases vides/occupées, filtrer la même catégorie ailleurs et vérifier que rien n’est prélevé dans la zone protégée ; inverser une autre zone et comparer les positions après application. Ne pas déclarer ce protocole exécuté.

Suite : SORT-FILTERS-006, filtres d’équipement et de marché à partir des métadonnées Item déjà partagées, avec inconnu distinct de false et sans nouvelle lecture native. Les 17 domaines restent PARTIAL.
