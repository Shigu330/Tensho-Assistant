# COLL-003 — Titres, succès et preuve d’acquisition

0.66.0.0 — 2026-10-29 — IMPLEMENTED / COMPILED / TESTED ; NOT VALIDATED_IN_GAME. Les 17 domaines restent PARTIAL.

Collections ajoute Titres et Succès au lecteur partagé. Titres : formes masculine/féminine conservées, position avant/après le nom, recherche dans les deux formes et liens vers tous les succès indiquant ce titre en récompense. L’icône couronne est un symbole d’interface, pas une icône native de titre inventée. Succès : icônes natives, catégories/genres et ordres du jeu, description/points statiques, récompense Item vers stocks/contexte et récompense Title vers sa fiche. Aucune déduction de réclamation de récompense depuis la complétion du succès ; aucun titre équipé, récompense récupérée ou requête serveur forcée.

## Acquisition à trois états

CollectionEntry conserve ConfirmedUnlocked et ajoute NegativeConfirmed, tous deux faux pour Unknown. Acquisition expose Acquired, NotAcquired ou Unknown. Un ancien constructeur booléen false garde donc son sens inconnu. Une observation positive est valide même si la liste complète n’est pas reçue ; une observation négative n’est confirmée que si les indicateurs dédiés sont vrais avant/après le getter et si la couverture reste stable jusqu’au contrôle final de la catégorie. Une perte de couverture efface tous ses négatifs mais conserve ses positifs. Une contradiction positive/négative est rejetée atomiquement avant publication.

Publish sans propriétaire, Invalidate, logout et erreurs globales effacent les deux types d’acquisition ; les métadonnées statiques restent indépendantes. Le propriétaire est revérifié après les contrôles natifs finaux. Les catégories nouvelles isolent leurs erreurs, sans supprimer les autres collections. UI : filtres Tous, positifs, inconnus et non acquis/non terminés confirmés ; compteurs séparés, sans pourcentage ni progression numérique inventés. Les autres collections restent en preuve positive uniquement.

LOCAL_VERIFIED : IUnlockState.IsTitleListLoaded, IsAchievementListLoaded, IsTitleUnlocked, IsAchievementComplete et UnlockDelegate(RowRef). Source primaire Dalamud 1e514694497ae448fca8e8a46f7b63642ad1e5ad relue : TitleList.DataReceived et Achievement.IsLoaded pour la couverture ; getters individuels acceptent aussi des événements positifs sans liste complète. Les signatures de conversion native Title ushort et Achievement int sont couvertes par les bornes des identités vérifiées dans le probe.

Le callback existant observe aussi les changements de chargement entre relevés. Une transition invalide les états précédents et relance la lecture. Si la liste se termine de charger pendant le relevé, un nouveau cycle coalescé reprend une couverture cohérente. Quand elle n’est pas chargée, la page indique d’ouvrir Titres/Succès dans le jeu puis d’actualiser, sans déclencher cette action elle-même.

Le signal IUnlockState.Unlock est abonné une seule fois, filtré sur les huit types de collections couverts. Il ne lit que RowType et demande un refresh via le compteur interlocked existant : aucun RowRef/pointeur retenu ou travail natif dans le callback. Un signal arrivé pendant le scan empêche de publier ce lot avant reprise. L’attachement framework est protégé par rollback ; Dispose retire les deux abonnements. Les comportements de callback/chargement réels restent à vérifier en jeu.

## Modèles et jointures

CompletionCopy.Lumina partage les projections originales entre lecteur et CollectionProbe. Title garde RowId, deux noms, IsPrefix et Order ; aucune sélection de genre du personnage par supposition. Achievement garde identité, description, icon, points, ordres, catégorie/genre strictement joints et références de récompenses Item/Title. Catégorie/genre étrangers ne fournissent aucun libellé. Les conditions d’affichage caché sont conservées comme référence, sans prétendre les évaluer. Ligne zéro ou sans nom exclue ; aucune identité de récompense n’est substituée à celle du succès.

Le lecteur commun conserve 32 lignes/budget souple 2 ms par update framework. La phase titres/succès est dans une partial dédiée, pas un second lecteur. Les erreurs d’une phase gardent les autres phases ; la fin d’une phase défaillante ne décale pas les indices des suivantes. Les modèles sont managés, textes et IDs copiés, aucune feuille dans Draw. UI avec clipper et vues/catégories/fiches liées mises en cache par snapshot/identité ; navigation réutilisant le contexte Item et les stocks.

## Validation exécutée

Verify.ps1 final : 421/421 tests, zéro avertissement/erreur ; package/API/versions/SHA-256 canonique/exclusion des assemblages hôtes verts. Journal checkpoints/0.66.0-verify.log. Cinq groupes nouveaux : table de vérité positive/chargement avant/après ; couverture finale stable ; propriétaire et métadonnées conservées ; contradiction refusée sans remplacer le snapshot ; récompense de succès distincte d’acquisition du titre/objet. Les scénarios précédents orchestrion/collections restent passants.

CollectionProbe sur les fichiers installés : 885 titres nommés, 530 formes de genre différentes ; 4 003 succès nommés, 83 catégories, 192 références de récompense Item et 891 références Title. Toutes ces références et icônes se résolvent ; plusieurs succès peuvent partager un titre, aucun lien écrasé. Catégorie/genre étrangers rejetés. Les projections restent utilisables après disposal GameData. Les 886 morceaux d’orchestrion/12 catégories/886 objets sont toujours vérifiés. Aucune acquisition native de personnage lue par le probe.

Rendu, événements Unlock, transitions de chargement et états réels en jeu non observés. Essais craft/Macro Chain différés, non redemandés. Aucun blocage global ni intervention demandée.

Suite indépendante QUEST-REWARDS-003-STUDY : vérifier les récompenses natives de quête, objets fixes/choix et liens de contexte, sans inventer progression, disponibilité ou récompense effectivement obtenue.
