# SORT-NATIVE-002 — Appliquer l’ordre visuel personnel

Historique 0.54 : l’extension 0.55 aux rangements ouverts et à la capture directe est décrite dans [SORT-SCOPES-003](SORT-SCOPES-003.md). Les durées et limites ci-dessous décrivent la tranche personnelle initiale.

0.54.0.0 — 2026-10-29 — IMPLEMENTED / TESTED. VALIDATED_IN_GAME: YES — rapport utilisateur 2026-10-30 sur la DLL 0.76.0.0 (« tout est OK » pour le protocole SORT-RUNTIME-VALIDATION : application, refus, rangement ouvert, persistance) ; non observé directement par l’agent.

Dans Accueil & outils → SortaKinda → Organisation de l’inventaire, choisir Inventaire personnel, une règle et son sens. L’aperçu existant montre le résultat. « Enregistrer cette règle » sauvegarde uniquement la préférence. « Appliquer cet ordre aux quatre sacs » remet une demande unique au prochain framework.Update. Aucun tri automatique à la connexion, aux événements d’inventaire ou pendant un worker.

Périmètre : les quatre sacs personnels, 140 cases, 35 par sac. Nom, catégorie du jeu, quantité et qualité utilisent exactement les critères de l’aperçu partagé. Les piles identiques restent distinctes, NQ/HQ conservés ; les cases vides sont placées après les piles selon leurs coordonnées physiques. Les rangements importés demeurent en aperçu dans cette tranche. Le tri visuel n’est pas un transfert physique ni une fusion de piles.

InventoryVisualSortPlan construit une bijection complète des coordonnées physiques. L’aperçu doit contenir exactement les piles du snapshot complet, bon propriétaire, vrais slots et indices visuels distincts. Les requêtes et snapshots expirent après trois secondes ; date future rejetée. Avant écriture, le bridge recapture les quatre sacs : identités, qualités, quantités, positions physiques et indices visuels doivent tous correspondre. Une égalité des seuls totaux ne suffit pas.

Le bridge s’exécute exclusivement dans framework.Update. Il vérifie le CharacterContentId du module, InventoryType, ItemsPerPage=35, 140 entrées, vecteur présent/capacité bornée et SortFunctionIndex=-1. Les adresses d’entrées et coordonnées doivent être uniques et complètes ; les entrées occupées doivent correspondre à la capture. Avant la première écriture, il prépare toutes les copies, vérifie encore propriétaire, module, sorter, vecteur, compte et chaque entrée Page/Slot/Index/Flags.

L’écriture permute les valeurs des entrées ItemOrderModuleSorterItemEntry existantes, Index/Flags inclus, puis marque ItemOrderModule.HasChanges. Aucun InventoryItem physique, quantité ou fichier de banque n’est écrit. Aucun hook ou pointeur n’est conservé entre frames. Aucun appel SaveFile forcé : la persistance native ultérieure n’est pas revendiquée. Le lecteur commun d’inventaire rafraîchit déjà l’ordre chaque seconde.

InventoryVisualSortService ne conserve qu’une demande managée. Logout efface la demande ; changement de propriétaire/date la rejette ; une génération obsolète ne peut publier un résultat. Dispose désabonne framework/logout et ferme le service. L’application n’est pas présentée comme une transaction native garantie face à des écritures concurrentes externes ; aucun comportement runtime n’a été observé.

Référence primaire : [SortingController de SortaKinda](https://github.com/MidoriKami/SortaKinda/blob/95a1bf9161f4bc595c21094eeaae9fda556f4b4b/SortaKinda/Classes/SortingController.cs), avec InventoryTypeExtensions et ItemOrderModuleSorterExtensions au même commit. SwapInventorySlots permute les valeurs de deux entrées visuelles ; PerformSort marque HasChanges. Licence AGPL v3 ou ultérieure observée dans LICENSE. Faits techniques uniquement : implémentation originale, pas de worker natif parallèle ou interception du tri repris.

ApiProbe confirme localement ItemOrderModule, ItemOrderModuleSorter, ses trois types d’entrées et StdVector. Les accès du bridge compilent contre ces bindings installés ; aucun offset manuel. Un premier nom de type imbriqué supposé a été rejeté par le probe, remplacé par les types autonomes effectivement exposés avant implémentation.

Verify.ps1 : 355/355 tests, Release sans avertissement/erreur, package/versions/ZIP/exclusion hôte verts. Six groupes nouveaux : permutation complète et piles distinctes ; 48 scénarios (six remplissages × quatre critères × deux sens) avec oracle de coordonnées indépendant ; refus des mutations ; admission/aperçus incohérents ; remise/générations ; expiration/logout/Dispose. Le bridge d’écriture mémoire lui-même et son rendu ne sont pas exécutés par ces tests managés. Une variable de fixture incompatible avec le mot-clé C#14 field a été renommée ; suite complète repassée.

Protocole ultérieur conservé, non demandé maintenant : recharger la DLL canonique, ouvrir les sacs du jeu, comparer l’aperçu puis appliquer une règle, vérifier ordre, positions visuelles et piles/quantités ; changer un objet entre aperçu et application pour vérifier le refus ; confirmer persistance après reconnexion séparément. Les essais de projection craft et Macro Chain restent différés.

Suite : SORT-SCOPES-003, étendre la capture en direct et l’application aux sacoches/servant réellement ouverts, sans jamais écrire sur la seule base d’un cache importé. Les règles par zones de slots et la personnalisation complète restent au périmètre SortaKinda PARTIAL.
