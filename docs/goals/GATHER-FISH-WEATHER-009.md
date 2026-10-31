# GATHER-FISH-WEATHER-009 — Météos natives des conditions de pêche

0.63.0.0 — 2026-10-29 — IMPLEMENTED / COMPILED / TESTED ; NOT VALIDATED_IN_GAME. Les 17 domaines restent PARTIAL.

Le cadre communautaire de pêche affiche désormais les noms météo dans la langue du jeu avec leurs icônes natives de 24 pixels. Le nom d’origine et l’ID Weather sont conservés dans le tooltip. Météo requise et météo de la période précédente restent séparées ; les alternatives restent distinctes. Une correspondance non prouvée affiche le nom de source avec « correspondance locale inconnue », sans icône attribuée arbitrairement.

## Preuve de correspondance

La source Garland épinglée, Weather.cs et FishingSpots.cs au commit 04cadd2e1e0de86c20aa9303faa082c7971f8d8b, utilise les noms Weather anglais comme clés des conditions. Le service commun WeatherCatalog a donc reçu un EnglishName optionnel, copié de la même RowId dans la feuille Weather anglaise. Ce champ ne remplace pas le nom local ni l’ID. Le loader existant lit toutes les définitions par lots de 32, budget souple 2 ms, puis les territoires/taux dans sa phase précédente. Aucun second lecteur/service/cache ; aucune lecture native dans Draw. Les erreurs d’accès anglais sont isolées et journalisées une seule fois : les prévisions natives restent possibles, les alias non copiés inconnus.

Une recherche globale a été refusée par le probe : Clear Skies existe notamment aux IDs 1, 91 et 209. Le catalogue copie 214 lignes Weather non nulles ; 43 libellés anglais sont globalement ambigus. Il ne faut pas choisir le premier ou fusionner ces identités.

WeatherBySourceName(name, territory) utilise un index immuable construit à la publication. La correspondance exige un nom anglais exactement identique et un ID présent dans les taux strictement positifs du territoire. Répétitions d’un même ID coalescées ; deux IDs du même nom dans la même zone restent inconnus. Les poids zéro n’ajoutent aucun candidat. Table absente/incohérente ou référence/métadonnée anglaise manquante parmi les poids positifs : unicité non prouvée, aucun alias pour cette portée. Une table partielle de total inférieur à 100 permet seulement de connaître une identité, sans couvrir les tirages manquants. Aucun fallback au nom local, traduction inventée, comparaison approximative ou dictionnaire externe manuel.

WeatherCopy.Lumina est la projection originale commune au loader et aux probes. Une ligne anglaise absente ou d’un autre RowId ne fournit aucun EnglishName. Les records/chaînes sont copiés ; aucun RowRef/feuille ne traverse le traitement différé. WeatherCatalog possède ses listes et les index ; Draw fait seulement une recherche par nom/territoire, sans balayage des feuilles ou création d’index.

## Validation réelle

Verify.ps1 : 405/405 tests, zéro avertissement/erreur ; package, API/versions, SHA-256 du ZIP contre la DLL/manifeste canoniques et exclusion des assemblages hôtes verts. Quatre nouveaux groupes : noms exacts et absence de fallback, ambiguïtés par territoire, taux zéro/invalides et couverture des métadonnées, propriété des copies. Les prévisions antérieures et la distinction des tirages manquants restent testées.

WeatherProbe exécuté sur les fichiers installés : 214 lignes, 43 libellés globalement ambigus ; lignes anglaises absentes/étrangères rejetées. Territoire 129 : Clear Skies → Weather 1 Dégagé / icône 60201, Fair Skies → 2 Clair / 60202. Territoire 137 : Rain → 7 Pluie / 60207, Showers → 8 Pluie torrentielle / 60208. Les icônes sont présentes. 1 290 territoires, 581 tables avec taux, 24 zones individuelles, territoire 979 à total 90 reste partiel. Fixture de prévision et absence de remplacement passent. Le premier essai de correspondance globale avait échoué explicitement ; son hypothèse a été remplacée par la portée de territoire avant livraison.

GatheringProbe --fish-baits=8753 exécuté avec transport/cache de production : Vieil homme à la mer, FishingSpot 107 → territoire 137. Clear Skies, Rain et Showers se relient aux trois identités natives ci-dessus ; trois appâts/intermédiaires résolus. Projection Weather commune, FetchedAt conservé et contrôles natifs antérieurs passants. Pas d’observation du rendu ni du ciel dans le jeu.

## Limites et suite

La correspondance d’identité ne confirme pas que la météo courante satisfait la pêche. Aucune intersection horaire/météo calculée dans cette tranche. Prédateurs/intuition, accès, équipement, compétences, prises et météo forcée ne sont pas observés. Les 491 codes météo natifs 2 restent inconnus. Essais craft/Macro Chain différés, non redemandés.

Suite : GATHER-FISH-FORECAST-010, croisement borné des seules conditions explicitement connues, avec météo précédente et horaires ET, couverture et inconnus conservés. Une plage compatible avec les conditions renseignées ne doit jamais devenir une garantie de capture.
