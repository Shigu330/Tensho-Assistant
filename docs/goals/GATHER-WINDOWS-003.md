# GATHER-WINDOWS-003 — horloge ET et prochaines plages UTC

Version 0.43.0.0, 2026-10-28. IMPLEMENTED / COMPILED / TESTED ; VALIDATED_IN_GAME : non. GatherBuddy reste PARTIAL.

Le navigateur Récolte & pêche et la fiche des lieux d'un objet affichent l'heure ET calculée depuis l'horloge UTC de l'ordinateur. Dans chaque ligne à horaires limités, afficher la plage horaire en cours avec sa fin UTC, ou le prochain début UTC. Le survol présente trois plages complètes de début/fin. La date est conservée lorsque la prochaine plage passe au jour réel suivant.

Les horaires natifs normalisés par GatheringSchedule restent la source. Une plage active signifie seulement « dans l'horaire statique » : le point n'est pas détecté et météo, accès, déblocages et prérequis ne sont pas vérifiés. Les horaires inconnus, invalides, sans fenêtre renseignée et sans restriction restent distincts ; aucune ouverture n'est inventée pour les trois premiers et aucune présence n'est promise pour le dernier. La pêche/points non couverts par les données horaires restent inconnus.

GatheringTime calcule en ticks à partir du rapport exact 35/12 secondes réelles par minute ET : heure ET 175 secondes, jour 4 200 secondes. La conversion prend le premier tick représentable après une borne rationnelle, évitant la dérive d'un arrondi flottant. Début inclus, fin exclue. Générer les occurrences journalières puis fusionner les intervalles contigus ; les morceaux de part et d'autre de minuit restent une seule fenêtre, avec un début éventuellement au jour ET précédent.

Résultat immuable avec Kind, heure ET, CalculatedAt UTC et jusqu'à douze plages ; l'interface en demande trois. Horizon borné, dates avant Unix ou trop proches de la limite DateTimeOffset refusées. Aucun réseau/worker/abonnement supplémentaire. Cache de présentation par seconde et clé d'horaire, invalidé à la fin exacte d'une plage ; le test d'appartenance utilise l'heure actuelle pour éviter une seconde de retard à l'ouverture.

Preuves réutilisées : encodages/jointures natifs dans [GATHER-002](GATHER-002.md), conversion ET dans [GATHER-WEATHER-001](GATHER-WEATHER-001.md). Aucune nouvelle source d'horaire externe ou approximation de présence ajoutée.

COMPILED / TESTED : Verify.ps1 Release 0.43.0.0, 280/280 tests et package/versions/ZIP/assemblies hôtes verts, zéro avertissement/erreur. Cinq nouveaux groupes : horloge rationnelle/frontières, début/fin/récurrence, fusion de minuit, états non limités/inconnus et oracle indépendant d'appartenance sur 2 880 minutes (deux jours ET), plus immutabilité.

LOCAL_VERIFIED : GatheringProbe sur les fichiers installés. Point 30999 : horaire natif 09:00–12:00 ET converti depuis l'époque Unix en +1 575 à +2 100 secondes UTC. Probe compilé/exécuté sans avertissement ; aucune requête réseau déclenchée par cette option sqpack seule. La fixture n'est pas une capture de présence d'un point dans le jeu.

Essai différé : DLL `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`, version 0.43.0.0. Ouvrir un objet à horaires limités, comparer l'heure ET à celle du jeu, survoler les trois plages UTC et vérifier un changement de plage. Vérifier aussi une fenêtre passant minuit et une source à horaire inconnu. Aucune validation en jeu déclarée ; Macro Chain reste reporté selon l'utilisateur.

Suite : conserver et recharger les séquences d'artisanat construites manuellement, sans activation native. Les conseils de craft et les prérequis météo restent au périmètre des 17 domaines, encore PARTIAL.
