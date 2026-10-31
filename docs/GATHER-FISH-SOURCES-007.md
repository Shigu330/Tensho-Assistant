# GATHER-FISH-SOURCES-007 — Appâts par consultation explicite

0.61.0.0 — 2026-10-29 — IMPLEMENTED / TESTED ; NOT VALIDATED_IN_GAME.

Le cadre « Appâts renseignés · Garland Tools » des lieux de récolte permet une consultation manuelle pour le poisson sélectionné. Les appâts et poissons intermédiaires sont affichés avec les icônes/noms du catalogue local ; cliquer sur un objet résolu le place dans le contexte commun. Les alternatives et variantes de lieu restent séparées. Les chaînes gardent l’ordre fourni : appât initial, puis poissons intermédiaires de pêche au vif éventuels.

Le carnet natif 0.60 reste distinct. Les données communautaires ne confirment ni prise, ni déblocage, ni météo favorable ou disponibilité actuelle. Les heures, transitions météo, hameçons et autres conditions du document ne sont pas interprétés dans cette tranche. Liste de lieux absente, liste vide, appâts absents et routes listées vides ont des états différents ; une liste vide ne veut pas dire « aucun appât nécessaire ».

La consultation réutilise exactement le service et cache d’objet des sources sur monstres, dont le nom historique est MonsterLootService/MonsterItemDocument. La projection d’objet ajoute Fishing/FishingError ; aucun service ou cache parallèle, nouvelle requête de sélection ou nouveau lecteur natif. Une réponse identifiée peut servir les deux vues sans téléchargement supplémentaire. Cache de trente minutes, capacité seize objets, annulation/générations et transport GarlandClient borné/cadencé existants sont conservés. Seul l’ID public d’objet est envoyé ; la date affichée est FetchedAt, pas une date de mise à jour de la base.

FishingSourceParser projette uniquement fish.spots et baits : objets et IDs positifs uint, au plus 256 variantes, 64 routes par variante, huit étapes par route, 4 096 références au total. Les contrôles parent de taille JSON un Mio, profondeur 32 et identité Item restent applicables. Les chaînes sont copiées en listes readonly, sans tri ni déduplication de leurs étapes. Un node de pêche sous-marine n’est pas converti en FishingSpot. Une erreur dans le bloc optionnel de pêche conserve les références de monstres valides ; une mauvaise identité globale ou l’annulation rejette toujours la réponse.

GatheringCatalog ajoute un index typé FishingSpot, calculé avec le catalogue, pour résoudre les libellés de lieu en temps constant dans Draw. Un point MIN/BTN du même numéro n’est pas pris pour un lieu de pêche. Un lieu non relié localement reste explicitement non résolu ; aucune position n’est inventée. La sélection d’un appât non résolu dans Item est désactivée.

Preuves primaires : document public item/fr/3/4869.json de Garland Tools et source de génération ufx/GarlandTools commit 04cadd2e1e0de86c20aa9303faa082c7971f8d8b, Garland.Data/Modules/FishingSpots.cs. BuildBaitChains/GetBaitChains confirme l’ordre des étapes et les alternatives. LICENSE MIT vérifiée pour le code consulté ; aucune implémentation ou base complète copiée/redistribuée. Les références factuelles sont consultées par l’API déjà utilisée par le plugin.

L’étude du code météo 2 n’a pas établi sa sémantique native : GatherBuddy épinglé teste ==1, tandis que le générateur Garland consulté teste >0 ; la définition SaintCoinach nomme le champ sans en décoder les valeurs. Cette divergence est conservée. Les 491 fiches natives concernées restent inconnues ; le parseur communautaire n’utilise pas ce drapeau pour inventer une météo.

Verify.ps1 : 396/396 tests, Release sans avertissement/erreur, versions/package/SHA-256 canonique/exclusion hôte verts. Sept groupes nouveaux : ordre/variantes/répétitions, absents/vides, isolation d’erreur optionnelle et identité, bornes/annulation/copies, cache HTTP commun et sélection sans réseau, réponse tardive et index FishingSpot distinct des points miniers.

GatheringProbe --fish-baits=4869 exécuté : consultation réelle du Gobie de Merlthor, 19 variantes de source, 19 liens FishingSpot natifs résolus et deux IDs d’appât/intermédiaire distincts présents dans Item ; horodatage de consultation conservé. Les contrôles natifs antérieurs du probe passent aussi. Son routage de l’option de contenu, auparavant exclusif de --monster-loot, a été corrigé pour les trois options explicites ; premier essai pêche refusé avant requête, puis essai complet réussi. Le rendu et les interactions Dalamud ne sont pas observés.

Protocole ultérieur conservé : sélectionner le Gobie de Merlthor, consulter le cadre, comparer lieux/appâts au jeu et à la source, cliquer sur une icône pour changer de contexte puis revenir. Vérifier le cache partagé avec la vue monstres et un changement d’objet pendant une consultation. Essais projection craft/Macro Chain différés, non redemandés.

Suite : GATHER-FISH-WINDOWS-008-STUDY — schéma et unités des horaires/conditions communautaires, provenance et limites avant calcul ; code météo natif 2 demeure inconnu jusqu’à preuve. Les 17 domaines restent PARTIAL.
