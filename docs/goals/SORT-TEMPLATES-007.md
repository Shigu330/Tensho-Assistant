# SORT-TEMPLATES-007 — Bibliothèque de règles réutilisables

0.59.0.0 — 2026-10-29 — IMPLEMENTED / TESTED ; NOT VALIDATED_IN_GAME.

Dans Organisation de l’inventaire → Modèles réutilisables, donner un nom au brouillon courant et créer un modèle. La bibliothèque est commune aux personnages, limitée à 64 modèles aux noms uniques sans distinction de casse. Un modèle contient un GUID de bibliothèque, sa version, son nom, sa géométrie et les critères/zones. Il ne contient ni CharacterId, ni RetainerId, ni snapshot ou quantité observée.

Charger un modèle reconstruit seulement le brouillon du personnage et du rangement choisis. L’enregistrement de ce profil et l’application native restent deux commandes séparées. Les sacoches normale et supplémentaire peuvent partager un modèle 2 × 35 ; les sacs personnels 4 × 35 et servants 7 × 25 utilisent des géométries distinctes. Aucun ajustement de plage ou rognage silencieux n’est effectué.

Une mise à jour garde le GUID et la géométrie du modèle. Pour une autre géométrie, créer un autre modèle. La suppression enlève seulement l’entrée choisie de la bibliothèque ; les profils déjà chargés/sauvegardés et les autres modèles restent indépendants. Le panneau désactive chargement/mise à jour incompatibles. Toutes les mutations du fichier de configuration ont un rollback de référence si la sauvegarde échoue.

InventorySortTemplates réutilise la validation de contenu d’InventorySortRules, sans fabriquer un propriétaire de substitution : bornes, chevauchements, critères et filtres invalides sont refusés. Versions différentes de 1, géométries inconnues et noms invalides sont rejetés. Les listes de critères/zones sont copiées. La configuration v1 ajoute une bibliothèque initialement vide ; aucun modèle n’est créé automatiquement dans une ancienne configuration.

Le chargement ne demande aucune capture, ne crée pas de reçu de lecture directe, ne remplace pas un propriétaire natif et ne lance aucun tri. Le brouillon passe par le même aperçu positionné et la même permutation complète. Les limites de capture directe, d’âge, de propriétaire et de fenêtre restent en vigueur.

Verify.ps1 : 385/385 tests, Release sans avertissement/erreur, versions/package/exclusion hôte verts. Six groupes nouveaux couvrent absence d’identité d’observation et copie des listes, géométrie/rebinding, invalides, mises à jour/noms/capacité/suppression, compatibilité v1 et chaîne modèle → brouillon → aperçu → permutation d’une autre sacoche sans autorisation native implicite. Un premier test comparait à tort l’égalité de références de listes copiées ; il vérifie maintenant les IDs et le contenu préservés. Suite complète repassée. ConfigSerializationProbe passe Newtonsoft None/Auto/Objects avec la bibliothèque et tous les réglages imbriqués.

L’audit de livraison renforce Verify.ps1 : entrées DLL/manifeste uniques dans latest.zip, SHA-256 identique aux fichiers canoniques bin/x64/Release. Contrôle exécuté avec succès. La seule autre DLL trouvée sous bin est l’archive explicitement nommée legacy-Release-20261027 en version 0.5.22 ; ce n’est pas une sortie active et elle n’est pas supprimée. Le fichier courant et le ZIP portent 0.59.0.0.

Rendu, application native et persistance d’ordre en jeu non observés. Les 17 domaines restent PARTIAL ; cette tranche fournit une bibliothèque de règles, sans prétendre remplacer entièrement SortaKinda. Essais projection craft et Macro Chain reportés, non redemandés. Protocole ultérieur conservé : créer un modèle, le charger sur un autre personnage/rangement de même géométrie, inspecter les zones puis enregistrer le profil ; vérifier le refus d’une autre géométrie et l’absence de tri au chargement.

Suite : BASE-AUDIT-002 — audit ciblé du périmètre des 17 outils, chemins/documents et bilan des limites, puis reprendre les autres domaines en respectant la réserve de quota. Pas de nouvelle grosse tranche avant vérification du compteur.
