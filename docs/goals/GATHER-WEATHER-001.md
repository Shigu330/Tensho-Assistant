# GATHER-WEATHER-001 — prévisions météo partagées

Version 0.42.0.0, 2026-10-28. IMPLEMENTED / COMPILED / TESTED ; VALIDATED_IN_GAME : non. Mappy et GatherBuddy restent PARTIAL.

## Accès et usage

Dans Mappy, sélectionner une carte puis **Prévisions météo de la zone**. Dans les sources de récolte/pêche d'un objet, **Météo de zone** ouvre la même page à partir du TerritoryId natif, même sans carte ouvrable. L'objet en contexte n'est pas remplacé.

La page présente six périodes avec icône/nom de météo, dates de début/fin UTC et plage horaire ET. La période courante est calculée, pas observée. Une période dure huit heures éorzéennes, soit 1 400 secondes réelles (23 min 20 s). L'heure utilisée est celle de l'ordinateur en UTC ; une horloge décalée décalerait la prévision. Les passages de jour ET et UTC sont conservés.

Le tableau se recalcule au changement de période, de zone ou de catalogue ; aucune requête réseau. Un panneau minimisable présente les taux natifs et leur couverture. Les conditions de météo propres aux poissons/points, météo précédente exigée, accès et événements ne sont pas déduits. Cette fonction renseigne la zone, sans certifier qu'une ressource apparaît.

## Données et modèle

TerritoryType.WeatherRate → WeatherRate.Weather/Rate → Weather.Name/Icon ; clé de territoire distincte de MapId. TerritoryType.IndividualWeather non nul signale une météo individuelle non prise en charge par la prévision standard. Les météos réellement forcées par contenu/événement ne sont pas observées, même lorsque la table standard existe.

Les taux sont des poids ordonnés et non déjà cumulés. Les cases de poids zéro sont ignorées ; les intervalles sont évalués avec une borne supérieure stricte. Une somme inférieure à 100 conserve une zone de tirages inconnus ; aucun dernier résultat de remplacement. Somme supérieure à 100, poids invalide, table absente, météo non résolue ou zone individuelle : inconnu avec motif. Les autres zones restent utilisables.

Calcul temporel : une heure ET = 175 secondes, jour ET = 4 200 secondes, période météo = 1 400 secondes. Depuis les secondes Unix non négatives, utiliser l'heure de fin de la plage de huit heures modulo 24 avec le jour ET, puis le mélange binaire 32 bits et modulo 100 établi dans la référence. Les débordements non signés sont intentionnels et testés. Dates antérieures à l'époque Unix, horizons hors 1–24 périodes et dépassements de DateTimeOffset sont refusés.

WeatherCatalog est immuable, versionné et partagé entre cartes/récolte. Le loader copie au maximum 32 territoires par update framework avec budget souple 2 ms ; les définitions météo sont dédupliquées. Publication atomique en fin de lecture ; échec/disposal efface le service et désabonne. Draw ne lit aucune feuille native et ne fait aucun calcul massif.

## Preuves

CURRENT_VERIFIED le 2026-10-28 : [GatherBuddy WeatherManager.Computation](https://github.com/FFXIV-CombatReborn/GatherBuddyReborn/blob/a204f2f45cb98302dbe938921255c3a6d617c775/GatherBuddy/Weather/WeatherManager.Computation.cs) pour périodes, tirage et sélection ; CumulativeWeatherRates pour les poids. Référence Apache-2.0, faits étudiés, implémentation originale. La référence signale notamment Empyrée 979 à total 90 et emploie un remplacement par dernière météo ; Tensho conserve ce manque comme inconnu, conformément aux règles du projet.

LOCAL_VERIFIED : ApiProbe sur WeatherRate/Weather/TerritoryType puis tools/WeatherProbe sur les fichiers installés. 1 290 territoires, 72 définitions météo, 581 tables avec taux, 24 zones à météo individuelle, une seule table non totalisée à 100 : territoire 979, WeatherRate 142, total 90. Toutes les icônes référencées présentes. Tirage non couvert d'Empyrée vérifié inconnu sans remplacement.

Fixture temporelle fixe et non observation actuelle : territoire 129, 2026-10-28 12:00:00 UTC → Couvert ; période 11:56:40–12:20:00 UTC. Il s'agit d'un calcul sur données natives, pas d'une capture du ciel dans le jeu.

COMPILED / TESTED : Verify.ps1 Release 0.42.0.0, 275/275 tests, package/versions/ZIP/assemblies hôtes verts ; zéro avertissement/erreur. Cinq groupes nouveaux : trois tirages calculés à la main et oracle indépendant BigInteger sur 500 dates, frontières/UTC/ET, bornes cumulées et masse manquante, tables absentes/individuelles/invalides, copies/immutabilité/version/erreurs. WeatherProbe compilé et exécuté sans avertissement après correction d'une annotation nullable.

## Essai en jeu différé

DLL `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`, version 0.42.0.0. Choisir une zone ordinaire de Mappy, ouvrir ses prévisions et comparer le nom/icône/heure ET avec le jeu, puis vérifier le passage de période. Depuis une ressource, ouvrir Météo de zone et confirmer le même territoire et le maintien de l'objet en contexte. Vérifier le motif inconnu pour une zone individuelle et la couverture 90/100 d'Empyrée. Aucun de ces essais n'est déclaré réalisé ; aucune validation de prérequis météo de poisson ou de point.

Suite indépendante : horloge ET et prochaines fenêtres horaires UTC des points, réutilisant les horaires natifs déjà copiés. Les conseils natifs de craft et autres extensions des 17 domaines restent au périmètre ; Macro Chain différé par l'utilisateur.
