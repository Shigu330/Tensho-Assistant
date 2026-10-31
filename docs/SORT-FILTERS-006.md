# SORT-FILTERS-006 — Types d’équipement et propriétés d’objet

0.58.0.0 — 2026-10-29 — IMPLEMENTED / TESTED ; NOT VALIDATED_IN_GAME.

Les zones filtrées proposent désormais « Type vendable au marché », « Type existant en HQ » et un emplacement d’équipement. Les propriétés ont trois choix : sans filtre, Oui et Non. Elles se combinent avec la catégorie native, la qualité réelle NQ/HQ et la recherche. Les zones protégées continuent à ignorer leurs filtres inactifs.

InventorySortMetadata copie les informations du catalogue Item commun une seule fois par ID pour tous les critères et zones d’un aperçu. Une ligne absente ou d’identité différente donne Marketable/CanBeHq/Equipment inconnus. Ni Oui ni Non ne les acceptent. Les anciens callbacks ne fournissant que nom/catégorie restent compatibles et conservent les autres propriétés inconnues. Les objets non retenus restent dans les cases non réservées ; aucune pile n’est perdue.

« Type vendable » reprend le classement statique déjà utilisé par le catalogue : non IsUntradable et ItemSearchCategory non nul. Il ne prouve pas la possibilité de vendre cette pile précise, ses flags individuels ou la présence d’une annonce. « Type existant en HQ » reprend CanBeHq et reste indépendant de la qualité de la pile. Aucune requête Universalis ou autre réseau n’est déclenchée.

Le masque partagé EquipmentSlots est étendu aux valeurs MainHand, OffHand et SoulCrystal ; les neuf emplacements Wearable existants conservent leurs valeurs. Les données proviennent d’EquipSlotCategory et seuls les champs strictement positifs attribuent un emplacement ; les valeurs négatives réservent/bloquent le slot. Les filtres proposent un emplacement, tous les emplacements pris en charge, main principale ou secondaire, ou vêtements/accessoires. Un masque multiple correspond à l’un des emplacements, pas à leur intersection.

Ces métadonnées décrivent le type d’emplacement, pas les niveaux/métiers/prérequis permettant au personnage courant de porter l’objet. L’ancien emplacement Waist n’est pas ajouté aux choix pris en charge. La Revue de mode garde ses filtres Wearable ; son texte n’affiche pas une liste vide pour une arme ou pierre d’âme. Aucun second catalogue ou lecteur natif n’est créé.

Le profil v1 ajoute trois filtres facultatifs, initialement sans restriction. Les valeurs de filtre inconnues et bits d’emplacement non pris en charge sont refusés. L’éditeur respecte toujours l’enregistrement séparé de l’application ; les garde-fous de capture directe et de permutation complète restent inchangés.

Preuves : ApiProbe confirme les champs sbyte MainHand/OffHand/SoulCrystal dans l’EquipSlotCategory installé. FashionProbe exécuté sur les fichiers locaux, sans --fetch : 269 indices, 20 238 vêtements/accessoires, 7 804 types de main principale, 984 de main secondaire, 31 pierres d’âme et 51 vêtements bloquant la tête plutôt que l’équipant. Il vérifie aussi que les OffHand négatifs ne deviennent pas des emplacements positifs. Le probe et le loader utilisent EquipmentSlotMap commun.

Verify.ps1 : 379/379 tests, Release sans avertissement/erreur, package/versions/ZIP/exclusion hôte verts. Cinq groupes nouveaux couvrent filtres combinés, inconnus distincts des négatifs, masques natifs/union, identité des métadonnées et cache par ID, compatibilité JSON/invalides/protection. Une fixture Contains a nécessité un littéral uint explicite avant compilation ; corrigée, puis suite complète passée. ConfigSerializationProbe passe Newtonsoft None/Auto/Objects avec les nouveaux filtres et un masque combiné.

Capture/rendu/application native et persistance de l’ordre restent non observés en jeu. Aucun test projection craft ou Macro Chain redemandé. Protocole ultérieur : comparer une zone main principale/secondaire avec le catalogue du jeu, essayer Oui/Non marché et potentiel HQ, vérifier que ces informations ne sont pas présentées comme une disponibilité de vente ou d’équipement du personnage.

Suite : SORT-TEMPLATES-007 — modèles réutilisables de règles indépendants du personnage, bornés à une géométrie compatible et chargeables en brouillon sans application native implicite. Les 17 domaines restent PARTIAL.
