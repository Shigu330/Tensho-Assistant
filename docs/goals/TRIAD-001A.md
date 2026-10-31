# TRIAD-001A — carnet et préparation de deck

État IMPLEMENTED / TESTED, version 0.21.0, 2026-10-28. Rendu et acquisitions natifs non validés en jeu. TriadBuddy reste PARTIAL : aucun solveur ni lecture du plateau dans cette tranche.

Le lecteur Collections existant copie les cartes depuis TripleTriadCard et TripleTriadCardResident, par lots de 32 avec budget souple de 2 ms au framework. IUnlockState.IsTripleTriadCardUnlocked confirme les acquisitions positives du personnage courant. Déconnexion, erreur ou changement de propriétaire invalident ces confirmations ; les valeurs des cartes restent consultables. Aucun pointeur ou ligne native différée.

Le carnet propose recherche par nom/numéro, rareté, type et acquisition, liste avec clipper, icônes propres aux cartes, description et valeurs en croix. La valeur 10 s'affiche A. RowId, numéro Order et identifiant d'objet restent distincts. Le catalogue Item partagé conserve la jointure typée AdditionalData → TripleTriadCard ; le bouton Stocks et outils place le véritable objet carte dans le contexte commun. Pas de recherche textuelle ou d'équivalence d'ID inventée.

Une préparation de deck de cinq cartes est enregistrable dans la configuration du plugin. Ajout/retrait explicites, doublons empêchés à l'ajout, validation de taille/rareté/identités et conservation des identifiants devenus indisponibles. La sauvegarde peut conserver une préparation incomplète ; aucun deck natif n'est modifié. En cas d'échec de sauvegarde, la configuration précédente est restaurée et le brouillon reste éditable. La composition valide ne prouve ni acquisition, ni compatibilité avec les règles particulières d'un match.

Source primaire CURRENT_VERIFIED le 2026-10-28 : [guide officiel Triple Triad](https://na.finalfantasyxiv.com/lodestone/playguide/contentsguide/goldsaucer/tripletriad/) : cinq cartes par deck, maximum une 5 étoiles et deux cartes de 4 étoiles ou plus au total. Cette limite est implémentée et testée. La page contient aussi des mentions historiques ; le nombre actuel de cartes provient exclusivement des fichiers locaux.

LOCAL_VERIFIED : ApiProbe sur Lumina/Dalamud actuels (méthode d'acquisition, noms des champs, feuilles associées). GatheringProbe utilise le modèle et le catalogue Item de production sur le sqpack installé : 475 cartes valides et leurs 475 icônes 88000 + RowId présentes ; 475 objets liés par AdditionalData. Dodo : RowId/Order 1, haut 4, droite 2, bas 3, gauche 4, objet 9772. Vérification explicite qu'au moins une carte possède RowId différent de Order. Aucune inversion des champs natifs pour imiter un ancien export.

COMPILED / TESTED : Verify.ps1 Release sans avertissement/erreur, 129/129 tests et package cohérent. Six groupes supplémentaires vérifient identités/orientation/A, limites de rareté, inconnus/doublons/taille, jointure Item, invalidation des acquisitions et configuration ancienne/rechargée.

Essai ultérieur : Accueil & outils → TriadBuddy → carnet. Vérifier recherche, filtres, icônes et valeurs ; préparer cinq cartes, enregistrer puis recharger le plugin ; vérifier le lien Stocks et outils. Tester déconnexion/reconnexion pour que les confirmations d'un personnage ne survivent pas sur un autre. Protocole non demandé à nouveau pendant le report du test Macro Chain.

Suite vers le solveur : moteur déterministe des coups normaux, captures sur les côtés opposés, score incluant la dixième carte non jouée, saisie manuelle et calcul annulable hors Draw ; règles non couvertes explicitement refusées avant toute suggestion. Les règles spéciales et la lecture du plateau suivent séparément.
