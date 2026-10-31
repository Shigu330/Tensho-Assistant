# GATHER-FISH-FORECAST-010 — Plages compatibles avec les critères renseignés

0.64.0.0 — 2026-10-29 — IMPLEMENTED / COMPILED / TESTED ; NOT VALIDATED_IN_GAME. Les 17 domaines restent PARTIAL.

Dans chaque variante de lieu communautaire résolue, un panneau repliable permet de calculer explicitement les plages compatibles avec les critères renseignés. Calcul local uniquement, sans requête réseau. L’horizon de l’interface couvre douze périodes météo, jusqu’à la fin de la douzième ; toutes les plages candidates de cet horizon sont affichées, sans pagination. Chaque résultat porte CalculatedAt et HorizonEndsAt en UTC. Données communautaires FetchedAt et version native restent dans leurs cadres précédents.

## Contrat d’interprétation

FishingConditionFields conserve séparément les critères effectivement renseignés : horaire, météo requise, météo précédente. Absents et listes vides ne sont jamais convertis en « pas de restriction » ; ils ne sont pas vérifiés et l’interface annonce un calcul partiel. Si aucun critère n’est exploitable ou si le parseur a signalé une erreur, aucune plage n’est déduite. Même lorsque les trois critères sont renseignés, intuition, prédateurs/prises préalables, accès, équipement, compétences, capture et météo forcée ne sont pas établis.

Chaque segment possède ReportedConditionsMatch : true pour les seuls critères renseignés compatibles, false lorsqu’au moins un critère est certainement non réuni, null lorsque l’intersection reste indéterminée. Un critère certainement faux domine un autre inconnu. Les listes météo représentent des alternatives : une alternative native certainement satisfaite suffit ; une alternative non résolue empêche de conclure faux lorsque les alternatives connues ne correspondent pas. Aucun libellé « pêchable maintenant » ni garantie de capture.

La correspondance météo exacte par territoire de 0.63 est réutilisée. La météo précédente vient de la période immédiatement antérieure de huit heures ET, y compris avant la période contenant CalculatedAt ; avant l’époque Unix elle reste inconnue. Tables absentes/incohérentes, météo individuelle/forcée non couverte, identité non résolue et tirages manquants d’une table partielle conservent les inconnus. La météo actuelle n’est pas utilisée à la place de la précédente.

## Calcul, bornes et partage

FishingForecast.Calculate est un calcul managé annulable, horizon 1–24 périodes de 1 400 secondes, sans service, cache, lecteur natif ou worker supplémentaire. Il réutilise WeatherCatalog.Forecast et ses identités, poids et couvertures. GatheringTime.Between étend l’horloge ET commune à un intervalle UTC borné à neuf jours ET : copie des fenêtres statiques, frontières rationnelles arrondies au premier tick représentable, début inclus/fin exclue, clipping et fusion des morceaux de minuit. L’horizon maximal du forecast couvre huit jours ET et reste dans cette borne. Les états d’horaire inconnus ne produisent aucune ouverture prouvée.

L’ensemble ordonné des frontières horaires/météo partitionne l’horizon. Les critères sont évalués au début inclus de chaque segment, puis les segments adjacents de même état sont fusionnés. Les listes de résultat sont readonly. Les dates avant Unix, horizons invalides et dépassements de DateTimeOffset sont refusés. L’annulation est contrôlée avant et pendant les boucles bornées.

MainWindow conserve seulement le dernier résultat explicite, lié par références au bloc de conditions et au catalogue météo, et par identités à l’objet/territoire. Un changement d’objet, actualisation du document, variante ou catalogue empêche de réutiliser un résultat étranger. Aucun recalcul massif par frame ; le travail ne part que sur le bouton. L’horizon écoulé ou un recul d’horloge est annoncé et demande une actualisation locale. Les segments false sont omis du résumé, sans en déduire une impossibilité de pêche ; les segments null sont affichés « Indéterminé ».

## Vérifications effectuées

Verify.ps1 final : 412/412 tests, zéro avertissement/erreur ; package, API/versions, SHA-256 DLL/manifeste du ZIP canonique et exclusion des assemblages hôtes verts. Journal : checkpoints/0.64.0-verify.log.

Sept nouveaux groupes : intersection des trois critères et frontières ; couverture partielle/absents/vides ; alias/périodes/tables individuelles inconnus et logique ternaire ; période précédente/époque Unix/fuseau ; intervalle ET rationnel/minuit/readonly ; oracle indépendant météo BigInteger et appartenance horaire sur douze horaires et environ cinq mille dates échantillonnées ; bornes/annulation/erreurs/copies. Les partitions sont vérifiées contiguës et maximales. Une erreur initiale de compilation des fixtures params/new() a été corrigée ; une assertion de frontière avait placé sa minute à l’intérieur d’une autre plage de minuit et a été isolée dans sa propre fixture, sans modifier le calcul pour satisfaire cette assertion erronée.

GatheringProbe réel 8753 : source communautaire actuelle du Vieil homme à la mer reliée à FishingSpot 107/territoire 137, Weather 1/7/8 et trois appâts/intermédiaires. Fixture UTC 2026-10-29 12:00, horizon 16:20 : un segment compatible 13:13:20–13:36:40 ; ReportedFields = Weather + PreviousWeather, horaire non renseigné donc calcul partiel. Ce résultat daté n’est pas une observation du ciel ou d’une prise.

GatheringProbe réel 4915 : Mahi-Mahi 10–18 ET, spot 47 ; même fixture/horizon, quatre plages horaires compatibles et ReportedFields = Time seulement. Exemples 12:09:10–12:32:30, 13:19:10–13:42:30, 14:29:10–14:52:30 UTC. Les deux critères météo restent non renseignés. Projections/cache de production, copies natives et contrôles précédents du probe passent.

## Limites et suite

Aucune validation en jeu ni intervention demandée ; essais projection craft/Macro Chain toujours différés. Les 491 codes météo natifs 2 restent inconnus, bonus natif d’Abondance également. Les calculs de pêche sont une base utile avec leurs limites explicites, pas une couverture complète de GatherBuddy.

Suite indépendante COLL-002-STUDY : enrichir les collections natives, notamment orchestrion, en vérifiant les identités/états de chargement/unlocks locaux et les liens d’objets avant intégration. Conserver les services/catalogues communs et les états inconnus.
