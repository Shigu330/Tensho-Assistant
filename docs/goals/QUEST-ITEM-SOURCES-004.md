# QUEST-ITEM-SOURCES-004 — D’un objet aux quêtes qui le proposent

0.68.0.0 — 2026-10-29 — IMPLEMENTED / COMPILED / TESTED ; NOT VALIDATED_IN_GAME. Les 17 domaines restent PARTIAL.

Usages de l’objet ajoute « Quêtes proposant cet objet ». La liste affiche icône/nom de quête et nombre de variantes, avec aperçu des conditions/quantités au survol. Cliquer ouvre la fiche existante et ses récompenses complètes. La section fonctionne indépendamment du chargement des mandats/GC/aquarium. Liste avec clipper, sans nouvelle lecture native ou requête réseau. Le résultat signifie candidat du catalogue, pas quête disponible ni obtention garantie.

## Index partagé et couverture

QuestRewardIndex est construit uniquement depuis les snapshots managés 0.67. Pour chaque Item/quête, toutes les lignes de récompense sont conservées en ordre, sans sommer, prendre un maximum, choisir genre/métier/rang ou fusionner HQ/NQ. Une quête apparaît une fois par Item ; sa liste de variantes reste entière. Les IDs complets de quête sont conservés, pas tronqués en ushort. Nom/catégorie/icône sont statiques ; aucun état de progression n’est copié dans le résultat.

Un dictionnaire figé permet la recherche par Item en temps constant. Listes de candidats et variantes readonly, versions natives distinctes conservées. Couverture explicite : nombre total de quêtes nommées, quêtes à récompenses couvertes, références non résolues, quantités inconnues et sources sans version. Snapshot absent ou RewardsError : quête non couverte, pas récompenses considérées vides. Une recherche sans résultat ne prouve jamais l’absence générale de source ; les quinze monnaies inconnues restent hors des associations Item avec cette limite visible.

Le calcul est annulable, sans assemblage natif : maximum 50 000 quêtes et 200 000 lignes examinées, IDs nuls/doublons de quête refusés sans publication d’un index partiel. Annulation contrôlée dans les boucles et autour de la phase finale de gel/tri. Ces bornes dépassent largement les données installées tout en limitant le travail et les allocations.

## Lifecycle et publication

QuestService est étendu, pas doublé : observations copiées et publiées immédiatement, index construit par un worker managé depuis cette copie readonly. QuestState distingue RewardIndexLoading/RewardIndexError de Loading/Error des observations natives. L’index ne remplace ni date, ni owner, ni séquence/progression. Une erreur de construction, y compris une annulation inattendue du constructeur, conserve les observations et termine correctement l’état de chargement de l’index.

Chaque construction possède sa génération, son snapshot attendu et son CancellationTokenSource. Publication seulement si service actif, génération/snapshot toujours courants et jeton non annulé. Nouvelle publication, invalidation, erreur globale, Clear et Dispose annulent le travail précédent ; résultat tardif ignoré même si le producteur ne respecte pas l’annulation. Les ressources de jeton sont libérées dans finally ; le handle du travail courant est retiré aux frontières d’invalidation. Dispose est idempotent et rend le service inerte. QuestReader.Dispose appelle cette fin de vie après désabonnement du callback framework. Aucun pointeur/RowRef ni getter natif ne traverse le worker.

Le rendu ne construit pas l’index. Cache des métadonnées/liens de fiche corrigé pour suivre Entries plutôt que seulement le QuestState : la publication de l’index ne le reconstruit pas, et l’ouverture d’une fiche avant la vue catalogue ne relance plus ce travail à chaque frame. Les liens de prérequis existants et le suivi persistant restent utilisés.

## Vérification réelle

Verify.ps1 final : 434/434 tests, zéro avertissement/erreur ; package/API/versions/SHA-256 canonique/exclusion hôte verts. Journal checkpoints/0.68.0-verify.log. Huit groupes nouveaux : variantes/IDs complets, couverture manquante/erreur, copies/readonly, bornes/annulation, worker réel et copie des producteurs, résultat tardif nouveau/même personnage, frontières invalidation/clear/fail/dispose, erreurs/annulations isolées. Les producteurs tardifs des tests ignorent volontairement l’annulation : le contrôle de génération/snapshot empêche quand même leur publication.

QuestRewardsProbe utilise le worker/service de production et compare l’index inverse aux 5 373 projections natives : 3 496 Items, 8 288 associations quête/Item, chaque ensemble de quêtes et chaque variante identiques à la projection directe. Potion 4551 : 25 quêtes candidates. Couverture 5 373/5 373, quinze références et quatre quantités inconnues inchangées, une version de données. Publication hors personnage reste sans date d’observation/progression confirmée. GatheringProbe recompilé avec les modèles partagés ; projection native antérieure toujours passante.

Aucun callback de quête, clic UI ou réception de récompense validé en jeu. Essais projection craft/Macro Chain toujours différés, non redemandés ; aucune intervention nécessaire pour la suite.

Suite QUEST-CURRENCIES-005-STUDY : étudier les quinze monnaies sans Item direct et les clés GC natives. Préserver quantité/condition, ne pas choisir une compagnie du personnage ou un ID de monnaie par convention non prouvée.
