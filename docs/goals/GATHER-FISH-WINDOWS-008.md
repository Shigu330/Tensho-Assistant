# GATHER-FISH-WINDOWS-008 — Conditions par lieu et plages horaires

0.62.0.0 — 2026-10-29 — IMPLEMENTED / COMPILED / TESTED ; NOT VALIDATED_IN_GAME. GatherBuddy reste PARTIAL.

La consultation manuelle partagée 0.61 devient « Appâts & conditions ». Chaque variante de lieu possède un cadre repliable, fermé initialement : horaires, météos renseignées, lien vers les prévisions natives de zone, puis appâts et chaînes cliquables. Les dix-neuf lieux d’un poisson commun ne développent plus toutes leurs étapes simultanément. Aucun nouveau service, cache, lecteur natif, abonnement ou accès réseau automatique.

## Contrat et preuves

Sources primaires épinglées : ufx/GarlandTools commit 04cadd2e1e0de86c20aa9303faa082c7971f8d8b, FishingSpots.cs (production during/weather/transition et héritage), Garland.Web/bell/gt.bell.js (calculateWindow, iterateWeather), Supplemental/FFXIV Data - Fishing.tsv. La licence MIT du code consulté est vérifiée ; aucun code ni base externe importés. Le générateur fournit des heures entières ET ; le consommateur utilise début inclus, fin exclue, avec passage de minuit. transition filtre la météo de la période précédente de huit heures ET, séparément de weather. Les conditions peuvent être héritées d’un appât/prédateur par le producteur : elles ne prouvent pas que le personnage satisfait les prises préalables.

FishingConditionParser projette seulement les champs explicitement présents dans la variante courante. during doit être un objet avec start entier 0–23 et end entier 0–24 ; pas de conversion depuis HHMM, chaîne ou fraction. Les bornes identiques couvrent toute la journée suivant le prédicat circulaire du consommateur étudié ; 0–24 est aussi une journée complète. Champs manquants/null restent Unknown, sans réutiliser des horaires provenant d’un autre lieu, du carnet natif ou d’une ancienne source. Réutilisation de GatheringSchedule.FromEphemeral puis GatheringTime/DrawGatheringTime, donc même horloge rationnelle ET → UTC, frontières et fusion de minuit que les points MIN/BTN. Seul l’horaire est projeté, jamais la disponibilité du poisson.

weather et transition sont chacun une liste optionnelle readonly de noms de la source (au plus 32 noms de 1–64 caractères, sans contrôles). Alternatives, ordre et orthographe restent conservés. Absent, liste vide et liste renseignée sont distingués. Une liste vide ne confirme pas l’absence de restriction. Les noms anglais externes ne sont pas reliés par supposition à une météo native française ; aucun calcul d’intersection météo/horaire. « Prévisions de la zone » réutilise le territoire natif du FishingSpot résolu, sans en faire une preuve que la condition externe est satisfaite.

Une erreur de conditions conserve les chaînes d’appâts et les références de monstres valides ; les conditions de cette variante deviennent inconnues avec motif générique. Les limites globales 0.61 (taille, profondeur, variantes, routes, IDs, annulation et identité d’objet) sont inchangées. Les copies ne dépendent pas de la durée de vie du JsonDocument. L’assemblage du probe Fashion inclut maintenant GatheringSchedule puisque sa compilation des sources Community intègre cette projection.

## Vérifications réellement exécutées

Verify.ps1 final : 401/401 tests, zéro avertissement/erreur ; package, versions, SHA-256 du ZIP canonique et exclusion des assemblages hôtes verts. Cinq nouveaux groupes : oracle indépendant d’appartenance sur les 600 couples d’heures et 206 minutes/jour, frontières UTC et minuit, états absents/vides, copies et séparation météo courante/précédente, entrées invalides isolées des appâts/monstres. Deux erreurs initiales de compilation des fixtures (littéral interpolé, mot-clé C#14 field/annotations nullable) corrigées avant validation finale. FashionProbe compilé séparément sans avertissement/erreur.

GatheringProbe --fish-baits=7685 : Papillon d’or, lieu 36 et appât natifs résolus. Le document actuel ne renseigne aucun during, alors que le fichier Bell épinglé mentionne 09–14 : l’horaire courant reste inconnu, sans importer cette ancienne valeur.

GatheringProbe --fish-baits=4915 : Mahi-Mahi, lieu 47, horaire communautaire 10–18 ET et appât résolus. Fixture calculée à l’époque Unix : 00:29:10–00:52:30 UTC ; ce calcul n’est pas une observation actuelle de présence.

GatheringProbe --fish-baits=8753 : Vieil homme à la mer, lieu 107, trois références d’appâts/intermédiaires résolues ; météo courante Clear Skies, précédente Rain / Showers, horaire absent donc inconnu. Exemple API 7686 consulté : Clear Skies / Fair Skies et chaîne 2611 → 4874. Les contrôles natifs antérieurs du probe passent à chaque exécution ; FetchedAt reste date de consultation, pas de mise à jour de la base.

## Limites et suite

Le code météo natif 2 reste inconnu (491 fiches) ; l’étude communautaire ne le décode pas. Accès, intuition/prédateurs, équipement, compétences, événements et prise actuelle non observés. La météo demandée n’est pas encore reliée au catalogue natif ; aucune fenêtre combinée ni « pêchable maintenant ». Aucun essai en jeu demandé ; les essais craft/Macro Chain restent différés selon l’utilisateur.

Suite GATHER-FISH-WEATHER-009-STUDY : vérifier une correspondance explicite entre noms météo communautaires et IDs natifs multilingues, sans second cache ni ambiguïté ; seulement ensuite envisager l’intersection bornée des conditions et prévisions avec Unknown préservé.
