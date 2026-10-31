# SORT-RULES-004 — Profils, critères et zones réservées

0.56.0.0 — 2026-10-29 — IMPLEMENTED / TESTED ; NOT VALIDATED_IN_GAME.

Organisation de l’inventaire propose un profil par personnage et rangement, jusqu’à quatre critères prioritaires (nom, catégorie native, quantité, qualité) et 32 zones réservées. Une zone indique une première case, un nombre de cases, une catégorie du jeu facultative, NQ/HQ et une recherche nom/identifiant. Les filtres se combinent avec ET. Les catégories et leurs noms viennent du catalogue Item commun, indexés une fois par identifiant natif ; aucune liste de catégories inventée.

Les cases sont numérotées à partir de 1, de gauche à droite puis dans le sac suivant. Les zones sont sans chevauchement et peuvent traverser une frontière de sac du même rangement. Leurs bordures apparaissent dans l’aperçu et leur nom dans le tooltip ; les icônes conservent nom, qualité et quantité. Les libellés des cadres indiquent leur plage de cases. Le rendu n’est pas validé en jeu.

Le moteur trie d’abord toutes les piles selon les critères. Les zones choisissent ensuite, dans leur ordre de priorité, les piles correspondant à leurs filtres. Chacune garde ses cases restantes vides ; les piles excédentaires ou non retenues remplissent les cases non réservées. Si ces cases ne suffisent pas, l’aperçu et l’application sont refusés avec un motif ; aucune pile n’est omise et aucune règle silencieusement ignorée. Un chevauchement, une limite hors rangement ou un critère dupliqué sont aussi refusés.

InventorySortResult peut désormais porter les positions complètes, cases vides comprises. InventoryVisualSortPlan vérifie que chaque pile y figure exactement une fois, avec origine/qualité/quantité intactes, puis affecte une vraie case physique vide à chaque trou. La permutation couvre toujours toute la table native. Les contrôles de capture directe, propriétaire, fraîcheur et fenêtre de SORT-SCOPES-003 restent en place ; les caches ne peuvent autoriser l’application.

La configuration v1 ajoute InventorySortProfiles sans migration destructive. Un profil distingue CharacterId, type de rangement et RetainerId éventuel ; limite 128 profils. Sauvegarder remplace seulement le profil sélectionné et copie ses listes imbriquées. En cas d’échec de SavePluginConfig, les références et anciennes préférences reviennent à leur valeur précédente. Les anciennes configurations conservent leur critère simple comme valeur par défaut sans zones. Un profil invalide/dupliqué donne un motif et un bouton de réinitialisation du brouillon.

« Enregistrer pour ce personnage et ce rangement » sauvegarde seulement le profil. « Appliquer » utilise explicitement l’aperçu du brouillon ; changer de rangement recharge son profil enregistré et abandonne le brouillon non sauvegardé, comme indiqué dans l’interface. Aucun tri automatique à l’enregistrement ni à la connexion. Aucun nouveau lecteur natif, réseau ou service d’inventaire parallèle.

Verify.ps1 : 369/369 tests, Release sans avertissement/erreur, package/versions/ZIP/exclusion hôte verts. Huit groupes nouveaux couvrent combinaisons de filtres et trous, priorité/débordement, critères multiples, chevauchement/bornes/capacité insuffisante, positions forgées, sauvegarde par scope/v1, copie des listes et catégories natives. Un groupe exerce 24 scènes de remplissage dans les quatre scopes avec oracle de coordonnées/quantités, y compris refus lorsque les zones rendent le placement impossible. ConfigSerializationProbe passe Newtonsoft None/Auto/Objects avec profil imbriqué, deux critères et zone filtrée ; les huit groupes et le probe utilisent le code de production.

Le premier build des tests a révélé une ambiguïté de new() avec un argument params et une annotation nullable de fixture ; corrigées avant l’exécution complète. Le bridge mémoire, l’affichage et la persistance de l’ordre dans le jeu restent non observés. Aucune promotion REPLACEABLE. Les essais projection craft et Macro Chain différés ne sont pas redemandés.

Protocole ultérieur conservé : créer une zone pour une catégorie présente, vérifier les bordures/cases vides, appliquer et comparer chaque pile au jeu ; tester deux critères et un chevauchement refusé ; recharger pour vérifier le profil. Les contrôles de fermeture de rangement et de changement de servant de SORT-SCOPES-003 restent à exécuter séparément.

Suite : SORT-PROTECTED-005, zones conservant leurs cases actuelles et remplissage inversé, via le même aperçu positionné et la permutation partagée. Les 17 domaines restent PARTIAL.
