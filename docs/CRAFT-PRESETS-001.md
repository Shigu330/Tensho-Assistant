# CRAFT-PRESETS-001 — séquences d'artisanat conservées

Version 0.44.0.0, 2026-10-28. IMPLEMENTED / COMPILED / TESTED ; VALIDATED_IN_GAME : non. Craftimizer reste PARTIAL.

Depuis une recette dans le simulateur, ouvrir **Conserver la séquence**, saisir un nom puis **Sauvegarder une nouvelle séquence**. Le répertoire **Séquences sauvegardées** est accessible depuis le simulateur et la fiche Craftimizer. Il présente résultat/icône, nom, RecipeId, nombre d'actions et date UTC, avec chargement ou retrait explicites.

La configuration v1 existante reçoit une liste de séquences. Chaque entrée possède un Guid, un nom, les IDs recette/résultat/métier, les actions copiées et SavedAt UTC. Les attributs, ressources observées, résultat de simulation et textes de macros ne sont pas enregistrés. Charger remplace seulement les actions dans le simulateur, conserve ou réinitialise les champs de scénario selon le changement de recette existant, efface résultat/export et annule la recherche. Il faut vérifier les attributs et recalculer ; aucune validation d'une ancienne séquence n'est présumée après changement de données du jeu.

Résolution par IDs exacts et métier, jamais par nom ni première recette du résultat. Recette absente, action inconnue ou entrée malformée : chargement indisponible, entrée conservée et retrait explicite possible. Aucune suppression automatique lors d'un changement de version. Les valeurs d'énumération des actions existantes doivent rester stables ; les nouvelles familles s'ajoutent à la fin.

Limites : 100 séquences, nom 1–80 caractères sans contrôles, 1–100 actions reconnues. Un même nom pour la même recette est refusé ; pas d'écrasement silencieux. Le modèle copie les actions et les modifications produisent de nouvelles collections. Une erreur de SavePluginConfig restaure la liste précédente en mémoire, pour ajout comme pour retrait. L'indicateur « sauvegardée » cesse de correspondre dès que les actions changent. Les données invalides déjà chargées ne sont pas effacées pour permettre un ajout valide.

Aucun abonnement/worker/réseau/commande native supplémentaire. Le navigateur borne la présentation à 1 000 lignes en cas de configuration manuellement hors limites, sans la nettoyer automatiquement. Les principes de persistance/rollback du projet sont réutilisés ; pas de nouvelle référence externe nécessaire.

COMPILED / TESTED : Verify.ps1 Release 0.44.0.0, 283/283 tests, package/versions/ZIP/assemblies hôtes verts, zéro avertissement/erreur. Trois nouveaux groupes : copie immuable et roundtrip JSON managé, résolution de variantes/bornes/actions inconnues/doublons/retrait pur, rollback d'ajout et retrait avec conservation d'entrées non résolues. Configuration.Version reste 1. Le roundtrip du test utilise System.Text.Json ; l'enregistrement/rechargement réel de la configuration dans Dalamud reste à observer.

Essai différé : DLL `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`, version 0.44.0.0. Créer une séquence courte, la nommer/sauvegarder, modifier les actions puis la recharger : l'ancienne liste doit revenir avec résultat effacé. Recharger le plugin pour vérifier sa persistance native, contrôler la bonne variante de recette et tester le retrait. Aucun lancement de macro ou craft n'est demandé automatiquement. Macro Chain reste reporté selon l'utilisateur.

Audit complémentaire : ConfigSerializationProbe passe avec Newtonsoft.Json 13.0.0.0 installée auprès de Dalamud, pour une fixture locale en modes None/Auto/Objects. IDs, Guid, actions et date UTC conservés. Il s'agit d'un roundtrip de bibliothèque isolé, pas d'un rechargement en jeu. Voir checkpoints/0.44.0-audit.md ; DLL et ZIP également comparés par SHA256.
