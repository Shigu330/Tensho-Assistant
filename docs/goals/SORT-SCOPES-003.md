# SORT-SCOPES-003 — Tri des rangements ouverts

0.55.0.0 — 2026-10-29 — IMPLEMENTED / TESTED. VALIDATED_IN_GAME: YES — rapport utilisateur 2026-10-30, DLL 0.76.0.0 (voir SORT-NATIVE-002).

L’outil Organisation de l’inventaire accepte maintenant les deux sacoches et le servant actuellement ouvert. Le sélecteur réunit les imports existants, les rangements détectés et la dernière capture directe ; importer d’abord n’est pas nécessaire. Choisir le rangement, cliquer « Lire le rangement ouvert pour l’aperçu », comparer la règle, puis « Appliquer cet ordre au rangement ouvert ». La sauvegarde de règle reste séparée de l’application.

Cette lecture directe n’écrit pas dans la banque persistante. Un cache importé reste consultable mais ne peut autoriser le tri, même avec une date récente ou une chaîne Source identique. InventoryVisualSortService exige la référence exacte du snapshot publié par la capture directe du même scope. Actualiser la banque après un tri reste une action d’import manuelle distincte.

InventorySortScope distingue le personnage connecté du propriétaire natif des piles. Personnel : 4 × 35 cases, IDs 0–3. Sacoche : 2 × 35, IDs 4000–4001. Supplémentaire : 2 × 35, IDs 4100–4101. Servant : 7 × 25, IDs 10000–10006 et propriétaire RetainerId. Le personnage connecté reste le propriétaire de la demande, pas le RetainerId. Aucun mélange de géométrie, de sacoches ou de personnages.

BankInventoryBridge fournit RequireOpenForSort et CaptureOpenForSort en réutilisant sa détection visible et ses contrôles de chargement. Ils s’exécutent seulement sur framework.Update. Pour le servant, fenêtre InventoryRetainer/InventoryRetainerLarge, RetainerManager prêt, identité active/LastSelected concordante et stabilité de 500 ms sont exigés. Pour les sacoches, une fenêtre InventoryBuddy/InventoryBuddy2 visible et les conteneurs chargés du groupe exact sont requis ; le code ne prétend pas reconnaître l’onglet visuel actif. Identité et chargement sont revérifiés après copie et avant écriture.

La capture directe d’un rangement peut être examinée pendant deux minutes. La demande d’application expire en trois secondes et exige toujours une nouvelle capture native de moins de trois secondes. Les piles, qualités, quantités, positions physiques et indices visuels doivent être identiques à l’aperçu. Les sacs personnels conservent leur observation commune et leur validité de trois secondes. Le changement de propriétaire, la fermeture, un tri du jeu en cours ou une table invalide refusent l’application.

Le bridge sélectionne InventorySorter, SaddleBagSorter, PremiumSaddleBagSorter ou GetActiveRetainerSorter ; ce dernier exige ActiveRetainerId égal au propriétaire natif attendu. Les contrôles de module/vecteur/entrées complets de SORT-NATIVE-002 s’appliquent à la capacité du scope. Le tri permute uniquement les entrées visuelles avec leurs Index/Flags et marque HasChanges. Aucun transfert, fusion ou écriture de piles physiques. Aucun pointeur ne traverse une frame, aucun tri automatique, aucun SaveFile forcé.

Les captures et applications partagent une file à génération unique. Une ancienne capture ne peut remplacer la nouvelle ; une réponse après Logout/Dispose est rejetée. L’aperçu suit le nombre réel de conteneurs et de cases ; ses dimensions incluent les espacements, et la case occupée réserve toute la surface de son icône. Le rendu natif n’a pas été observé.

Preuves locales : géométrie déjà utilisée par BankStorageScope/InventoryNativeReader ; bindings ItemOrderModule, GetActiveRetainerSorter, ActiveRetainerId et entrées inspectés dans la tranche 0.54 et compilés ici. Source SortaKinda épinglée dans REF-SORT-NATIVE-002 pour la permutation visuelle et HasChanges ; extension originale aux scopes, pas de code de référence importé.

Verify.ps1 : 361/361 tests, Release sans avertissement/erreur, package/versions/ZIP/exclusion hôte verts. Six groupes nouveaux incluent 144 scénarios (trois rangements × six remplissages × quatre critères × deux sens) avec oracle indépendant de coordonnées. Tests de propriétaire/géométrie, cache cloné sans reçu direct, inspection puis revalidation fraîche, changements de scope, logout, expiration, capture incomplète et Dispose. Le bridge mémoire et la détection de fenêtres ne sont pas exécutés par les tests managés ; pas de validation native ou de persistance revendiquée.

Protocole futur conservé : recharger la DLL canonique, ouvrir un rangement, le lire dans SortaKinda, comparer et appliquer une règle ; vérifier chaque pile/qualité/quantité dans le jeu. Fermer/changer le servant entre lecture et application doit refuser. Confirmer que le cache de banque ne change qu’après un import explicite. Les essais projection craft et Macro Chain reportés ne sont pas redemandés.

Suite : SORT-RULES-004 — règles de zones de cases et filtres à partir des catégories natives, aperçu puis application explicite par le même moteur. Les 17 domaines restent PARTIAL.
