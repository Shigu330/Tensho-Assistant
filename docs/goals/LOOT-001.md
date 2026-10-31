# LOOT-001 — Matériaux des monstres, 0.33.0

Première tranche utile intégrée : consultation manuelle des associations d'un matériau avec des monstres, fiches et lieux renseignés, carte de zone et autres objets du monstre. Réutilise le catalogue Item, le contexte commun, les cartes et Duty Loot. Aucun des 17 plugins n'est déclaré remplacé.

## Parcours

Accueil & outils → Monster Loot Hunter → Ouvrir les sources sur monstres. Rechercher un matériau dans le groupe Choisir un matériau, ou venir d'un ingrédient/objet sélectionné grâce au bouton contextuel Monster Loot Hunter. Consulter les monstres associés charge un seul document d'objet ; sélectionner un monstre reste local. Consulter la fiche du monstre charge ensuite son document, après la cadence commune.

Les noms, niveaux et zones de la liste sont ceux des références françaises de la source. Une référence sans détails reste visible par son grand ID. La fiche affiche les niveaux, la zone et les coordonnées indicatives lorsqu'ils sont renseignés. Les objets de sa liste secondaire ont les icônes du jeu et peuvent être sélectionnés dans le contexte commun. Le bouton vers Duty Loot, si une instance est référencée et reconnue, ne lance aucune consultation automatique.

La carte de zone est résolue par PlaceName et non par territoire. Si plusieurs cartes correspondent, il faut choisir ; aucune n'est choisie par simple collision d'ID. Le navigateur s'ouvre centré à 20,5/20,5, sans placer automatiquement le point indicatif. La source peut agréger des positions anciennes ; cela ne prouve pas un spawn exact ou actuel. Les identités non résolues restent conservées.

## Modèles et couverture

`MonsterDocuments.ParseItem` valide l'ID Item, lit uniquement `item.drops` comme liens de monstres, puis enrichit ces liens par les partials de type mob dont l'ID externe et `obj.i` concordent. Les partials non référencés ne deviennent pas des sources. Les doublons de lien sont coalescés, les détails manquants restent inconnus et des partials contradictoires sont rejetés.

Les IDs monstres restent des UInt64 opaques en production : ni troncature UInt32 ni conversion en double. Leurs voisins au-delà de 2^53 sont testés séparément. La décomposition BNpcBase × 10^10 + BNpcName documentée par le fournisseur sert seulement à la vérification locale du probe, pas à inventer un lien natif dans Draw.

La fiche valide à nouveau son ID, copie niveaux/lieu/instance et coordonnées finies bornées, et conserve les IDs Item de ses drops. Une absence de liste est distincte d'une liste vide. Si le document du monstre ne reprend pas le matériau annoncé par le document de l'objet, une divergence est affichée. Aucun taux, quantité, exhaustivité ou accès au lieu n'est déduit. La récupération et la mise à jour de la source restent deux notions distinctes ; seule la récupération est connue et datée UTC.

Les noms du matériau et les icônes des objets viennent du catalogue français du jeu ; noms/niveaux du monstre viennent du document communautaire français et leur provenance reste explicitement indiquée. Aucun rapprochement par simple nom traduit n'est nécessaire.

## Service et ownership

`GarlandClient` est maintenant le transport commun à Duty Loot et Monster Loot. Il possède le HttpClient par défaut et sa durée de vie ; les services du plugin l'empruntent, puis sont disposés avant lui. Routes numériques fixes, cadence de début minimale 3 s pour les deux domaines, délai 12 s, corps limité à 1 Mio déclaré et effectivement lu par flux, décodage UTF-8 strict. Seuls des IDs publics sont envoyés ; pas de personnage, inventaire ou historique. Aucun appel par Draw, sélection ou survol : seules les commandes de consultation/actualisation lancent le réseau.

`MonsterLootService` conserve au plus 16 documents d'objet et 32 documents de monstre, réutilisables 30 min après récupération. Après expiration, la consultation reste explicite. Chaque requête a son CTS, son contexte et sa génération ; changement de matériau, changement de monstre, annulation et Dispose rejettent les réponses tardives et leur mise en cache. La publication d'une nouvelle liste retire une fiche devenue étrangère aux associations actuelles. Un échec conserve éventuellement l'ancien document daté avec une erreur explicite.

Les tableaux sont managés, copiés et immuables. Profondeur JSON 32, collections limitées à 4 096 références/partials ; les listes UI utilisent des clippers. `MapCatalog` ajoute un index PlaceName multicarte, sans modifier les identités Map/Territory et sans déduire une carte unique lorsque le lieu est ambigu. Les ressources réseau sont enregistrées pour nettoyage avant la lecture de configuration, y compris si son chargement échoue.

## Sources primaires examinées le 2026-10-28

- [Garland Tools, génération des monstres](https://github.com/ufx/GarlandTools/blob/04cadd2e1e0de86c20aa9303faa082c7971f8d8b/Garland.Data/Modules/Mobs.cs) : identité composée, zoneid PlaceName, noms localisés, niveaux et coordonnées issues de sources supplémentaires.
- [Garland Tools, vue des monstres](https://github.com/ufx/GarlandTools/blob/04cadd2e1e0de86c20aa9303faa082c7971f8d8b/Garland.Web/db/js/gt.mob.js) : document mob version 2, zone/instance distinctes, positions indicatives et références d'objets.
- [Document français Peau de bouquetin](https://www.garlandtools.org/db/doc/item/fr/3/5296.json) et [Bouquetin Myotragus](https://www.garlandtools.org/db/doc/mob/fr/2/1930000000273.json) : structures item.drops/partials et mob observées.
- [MonsterLootHunter, client Garland](https://github.com/danielbrenom/MonsterLootHunter/blob/1e603afc0c60d12c401eb6b7b360623dee766ebc/MonsterLootHunter/Clients/GarlandClient.cs) : dépôt/auteur et route d'objet identifiés, licence MIT signalée par GitHub. Ce client de référence résout un nom d'objet pour d'autres sources ; notre consultation directe est une implémentation originale, sans copie de code/paquet tiers.

## Vérifications exécutées

COMPILED / TESTED : Verify.ps1, Release 0.33.0.0, 206/206 tests, zéro avertissement/erreur ; manifeste, versions, ZIP et exclusion des assemblages hôte valides. Nouveaux cas : grandes identités et précision, liens explicites/partials étrangers, inconnus, limites/malformations/annulation/copies, PlaceName ambigu ou collision numérique, cadence commune avec Duty Loot, caches/expiration/éviction, associations retirées, réponses tardives, erreurs conservées, propriété partagée et UTF-8 invalide. Les tests Duty Loot passent après extraction du transport commun.

LOCAL_VERIFIED / CURRENT_VERIFIED : GatheringProbe exécuté avec `--monster-loot=5296`, qui effectue les deux consultations explicites avec la cadence commune. Peau de bouquetin 5296 → 7 références. Première fiche 12810000000879, Longues-cornes Âgé : BNpcBase 1281, BNpcName 879, nom français concordant avec le sqpack installé, PlaceName 44 résolu à une carte native ; 3 objets et lien inverse vers 5296 présent. Les autres références ne sont pas toutes certifiées par cette vérification d'un échantillon.

VALIDATED_IN_GAME : non. Le succès du probe ne démontre pas le rendu UI ou les clics en jeu. Macro Chain reste reporté par l'utilisateur et aucun nouvel essai n'est requis pour poursuivre le projet.

## Essai en jeu conservé pour plus tard

DLL `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`, version 0.33.0.0. Ouvrir l'outil, choisir Peau de bouquetin puis consulter. Sélectionner un monstre, attendre la cadence puis consulter sa fiche. Vérifier noms/icônes et carte de zone, sélectionner un autre objet depuis ses drops et vérifier le contexte. Changer de matériau pendant une consultation, annuler une autre, fermer/recharger le plugin : aucune publication étrangère ne doit remplacer le contexte. Vérifier le retour à la ligne des outils liés sur une fenêtre étroite. Les échecs réseau doivent rester des erreurs et non de nouvelles listes vides.
