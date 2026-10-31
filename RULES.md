# Règles durables

- Projet neuf Tensho--Assistant. Les 17 plugins du prompt maître et de ROADMAP.md déterminent le périmètre.
- Un seul GOAL actif, petit et vérifiable. Pas de modules vides ni d'abstractions prématurées.
- Source de vérité commune pour chaque domaine partagé. KEEP → EXTEND → REFACTOR → REPLACE → NEW.
- Prompt maître > environnement local > source actuelle > références historiques > inférence. Aucune décision héritée d'Éorzéa ne fait autorité.
- Références : HISTORICAL_VERIFIED / CURRENT_VERIFIED / LOCAL_VERIFIED / STALE / UNKNOWN avec date et preuve. Baseline : 2026-10-27.
- Runtime : valeur, état, source, portée, couverture, date d'observation si pertinent. Inconnu et incomplet ne signifient pas zéro ou absent.
- Lire l'état natif sur le thread approprié, copier en données managées avant worker. Pas de pointeur/span natif différé ni de travail massif dans Draw.
- Chaque abonnement/commande/worker possède un propriétaire et un nettoyage. Préférer invalidation, cache, batching et annulation.
- Build/tests ciblés ; distinguer COMPILED, TESTED et VALIDATED_IN_GAME. Ne déclarer que les vérifications exécutées.
- Avant de demander un nouvel essai en jeu après modification du code, compiler et exécuter les tests pertinents ; fournir la version de la DLL effectivement produite (consigne utilisateur).
- Lire d'abord ce fichier, STATUS.md et GOAL.md ; recherches ciblées. Ne pas réétudier les preuves actuelles sans motif.
- LIGHT pour le travail courant ; HEAVY pour difficulté démontrée ; VERY_HEAVY après échecs raisonnables. Sauvegarder avant escalade/downgrade.
- HEAVY/VERY_HEAVY : checkpoint après étape significative, avant nouveau gros travail. Écrire le point de reprise, demander vérification du quota et arrêter. Ne jamais inventer un quota. Arrêter après préparation d'un downgrade LIGHT.
- Après trois corrections infructueuses du même problème, réévaluer les preuves avant de modifier à nouveau.
- Consigne utilisateur du 2026-10-29 : continuer de manière autonome en conservant une réserve de 5 % sur la fenêtre de quota limitante ; vérifier le compteur réel aux jalons. Ce seuil remplace la consommation jusqu'à épuisement.
