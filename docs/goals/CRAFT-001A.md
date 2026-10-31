# GOAL sélectionné

ID: CRAFT-001A
PLUGIN: Craftimizer #5 — fondation recette commune
OBJECTIVE: Afficher les recettes produisant l'objet sélectionné, leurs ingrédients et quantités, en réutilisant les catalogues et l'inventaire communs.

WHY_NOW: ITEM-001-RUNTIME accepté ; catalogue français de 50 880 objets et sélection reliée aux totaux des sacs confirmés. Première chaîne utile vers l'artisanat sans construire un simulateur prématurément.

CURRENT_STATE: PARTIAL_RUNTIME_VALIDATED — catalogue recette managé ajouté en 0.5.0, Diagnostics runtime validés avec 13 892 recettes ; capture Craft lié validant la chaîne, le rendement, cinq ingrédients, stocks NQ/HQ et borne de 51 crafts. Les imports servant/chocobo sont maintenant validés dans une capture séparée ; les transferts ultérieurs restent soumis à réimport manuel.

REFERENCES: REF-CRAFT (historique), REF-ITEM-API, REF-LOCAL-001 ; inspection ciblée des membres locaux Recipe/RecipeLevelTable nécessaire.

FILES: Craft/RecipeCatalog.cs, Craft/RecipeCatalogLoader.Dalamud.cs, MainWindow.Items.cs, Plugin.cs et tests ciblés. API locale sondée par ApiProbe puis vérifiée par compilation.

IN_SCOPE: RecipeId distinct du résultat ItemId, rendement, métier/niveau de recette selon API vérifiée, ingrédients et quantités (y compris cristaux selon les données). Liens par IDs au catalogue Item. Possession des ingrédients via InventoryService seulement, avec Unknown hors portée. Afficher les variantes multiples d'un même résultat sans en supprimer.

OUT_OF_SCOPE: Simulation, solver, exécution de macro, craft automatique, hooks, recettes spéciales non vérifiées, prix/récolte et développement complet des autres plugins. Pas de faux zéro sur les cristaux si leur conteneur n'est pas observé.

VALIDATION: 57/57 tests et Verify.ps1 verts avant essai. Essai candidat : sélectionner le sirop de bouleau, ouvrir « Recettes qui produisent cet objet », puis comparer recette, rendement, métier/niveau et ingrédients au carnet du jeu. Aucun succès runtime supposé avant ce contrôle.

RECOMMENDED_MODEL: LIGHT
MODEL_REASON: Petite tranche de données, patterns de chargement déjà établis ; HEAVY seulement si jointures/API réellement difficiles. Pas de simulation dans ce GOAL.

QUOTA_CHECKPOINT: YES — relevé effectué avant l'implémentation ; aucun nouveau gros changement avant un prochain checkpoint.

DEFINITION_OF_DONE: Catalogue recettes réel relié aux objets, ingrédients/rendement affichés, variantes préservées, pas de duplication d'inventaire, build/tests verts, métadonnées et limites documentées. L'essai live doit confirmer au moins le sirop de bouleau.
