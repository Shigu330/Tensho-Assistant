# USE-CONTEXT-004 — L’objet du jeu vers les outils

0.53.0.0 — 2026-10-29 — IMPLEMENTED / TESTED, NOT VALIDATED_IN_GAME.

Dans Réglages, « Ajouter Tensho au clic droit des objets d’inventaire » active un sous-menu optionnel, désactivé par défaut dans les configurations v1. Il propose Usages, Stocks, Craft lié, Sources de récolte et Tous les outils avec cet objet. Les options de survol restent indépendantes. Le menu ne consulte pas les prix et ne déclenche ni craft, ni récolte, ni déplacement d’objet.

Le clic réutilise SelectItem, son historique et les pages existantes, même avec la fenêtre principale fermée. Le répertoire complet permet ensuite d’accéder aux autres outils actuels ou futurs. Les stocks communs continuent à afficher NQ et HQ ensemble.

ItemContextMenuBridge lit TargetItem uniquement pendant OnMenuOpened. ItemContextMenuSession conserve identité normalisée, qualité, propriétaire, révision et date ; jamais args, target, adresse ou pointeur dans les closures. La normalisation est celle d’ItemHoverService : NQ/HQ/collectionnable distingués, objet inconnu/HQ impossible/ID >=2000000 rejetés. Les objets non vendables restent acceptés.

Chaque ouverture de menu invalide la précédente, même pour le même objet. Option, propriétaire, catalogue, révision et âge sont recontrôlés au clic puis à la remise unique au Draw suivant. Expiration après deux minutes, date future rejetée. Désactivation observée au Draw et Logout invalident le menu ; une reconnexion au même personnage ne le rétablit pas. Dispose désabonne menu/logout et rend la session terminale. Aucun lecteur natif continu ajouté.

Périmètre : cibles Inventory du SDK, y compris les rangements utilisant cette cible. Menus Default, liens de discussion et objets spéciaux non couverts. Fermer sans cliquer n’ouvre aucune page. Présence dans chaque rangement, callbacks et rendu natifs restent non observés en jeu.

Preuves : ApiProbe sur IContextMenu, MenuArgs, IMenuOpenedArgs, IMenuItemClickedArgs, MenuTargetInventory, MenuItem, GameInventoryItem et ContextMenuType installés. Source primaire Dalamud commit 1e514694497ae448fca8e8a46f7b63642ad1e5ad : copie du slot ciblé, callbacks, branche AddonContextSub sans nouvel OnMenuOpened. Voir REF-USE-CONTEXT-004. Faits d’API uniquement ; implémentation originale.

Verify.ps1 : 349/349 tests ; Release sans avertissement/erreur ; package/versions/ZIP/exclusion hôte verts. Six groupes nouveaux : routes/qualités/remise unique, remplacement, revalidation au dispatch, admission/limites, logout/reconnexion/Dispose, option v1 indépendante. ConfigSerializationProbe passe Newtonsoft None/Auto/Objects.

Protocole pour plus tard, non exécuté : recharger la DLL canonique, activer l’option, fermer Tensho puis ouvrir chaque entrée sur un objet connu. Comparer l’objet en contexte ; vérifier qu’un autre survol ne le remplace pas, qu’une fermeture sans choix ne navigue pas et que désactivation/déconnexion invalident le menu précédent. Les essais projection de synthèse et Macro Chain reportés ne sont pas redemandés.

Suite : SORT-NATIVE-002-STUDY, commandes de tri et SDK avant application explicite. Bonus GP inconnu conservé ; les 17 domaines restent PARTIAL.
