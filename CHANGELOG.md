# Journal des versions / Changelog

Résumé par version, en français puis `### English` (partie anglaise obligatoire depuis 0.219.0) ; la partie anglaise sert aussi de corps
aux commits et aux notes de release sur GitHub. Preuves citées : docs/evidence/.
Chaque version a été compilée et testée par `scripts/Verify.ps1` ; la validation en jeu est indiquée quand l'utilisateur l'a rapportée.
Ce fichier garde la dernière release et les versions qui suivent ; les plus anciennes sont dans [docs/changelog/](docs/changelog/)
(déplacées par `tools/changelog.py archive`, un fichier par centaine de versions).

Summary per version, in French then `### English` (English part required since 0.219.0); the English part is also the body of the
commits and of the release notes on GitHub. Evidence cited: docs/evidence/.
Each version was built and tested by `scripts/Verify.ps1`; in-game validation is stated when the user reported it.
This file keeps the last release and the versions after it; older ones are in [docs/changelog/](docs/changelog/)
(moved by `tools/changelog.py archive`, one file per hundred versions).

## 0.1542.0
- Plan global, X-573 : X-573 M3 (relecture 0.1539) : la phrase « Les alarmes sonnent quand même depuis l'application (… le son de Windows et une notification Windows ; par défaut seulement quand le jeu n'est pas la fenêtre au premier plan) » de la FAQ et du README n'était reliée qu'au commentaire X-163 de l'application de bureau. FaqAlarms1542Tests la relie aux valeurs par défaut d'AlarmSettings (son, notification, seulement quand le jeu est derrière), aux trois réglages de la page Alarmes (activés sauf si on les coupe, libellés en anglais et en français) et au code qui les applique (silence quand le jeu est devant, son et notification selon leur option) : un défaut qui change casse un test au lieu de laisser le texte faux. M4 fait hors version (retour.py étape 9 et stats.py ci() couverts par leurs selftests). X-573 fini.
- Tests : FaqAlarms1542Tests. Verify.ps1 (agent) : 3084/3084, 0 avertissement, 0 erreur ; DLL 0.1542.0.0. Rien à essayer en jeu.
### English
- X-573 M3 (review 0.1539): the sentence « Alarms still ring from the app (… Windows' sound and a Windows notification; by default only while the game is not the window in front) » of the FAQ and the README was tied only to the desktop app's X-163 comment. FaqAlarms1542Tests ties it to AlarmSettings' defaults (sound, notification, only while the game is behind), to the three settings of the Alarms page (on unless turned off, labelled in English and French) and to the code that applies them (silent while the game is in front, sound and notification by their option): a changed default breaks a test instead of leaving the text wrong. M4 done outside a version (retour.py step 9 and stats.py ci() covered by their selftests). X-573 finished.

## 0.1541.0
- Plan global, X-571 : X-571 : le test « Fair lock 1309 » des deux magasins de banque sur un fichier échouait parfois sous charge (BankLockBusyException après son attente de 2 s). Cause mesurée par une sonde (retirée) : le verrou équitable se prend en 0 à 2 ms ; les à-coups viennent du disque, dans SQLite (une validation jusqu'à 772 ms, une fermeture avec point de contrôle du journal jusqu'à 1,7 s, une sauvegarde entière jusqu'à 4,3 s, avec ou sans charge, avec 10 lignes comme avec des centaines). Une attente de 2 s est plus courte qu'une seule de ces sauvegardes du magasin qui tient le verrou. Pas de défaut sur les données : le vrai magasin attend 10 s et un refus n'écrit rien et le dit. Le test attend maintenant 10 s (la valeur du magasin) et le magasin gourmand sauvegarde jusqu'à l'arrêt, au-delà de cette attente : un verrou injuste échoue toujours. Durée de chaque sauvegarde dans le message d'échec.
- Tests : FairLock1309Tests. Verify.ps1 (agent) : 3082/3082, 0 avertissement, 0 erreur ; DLL 0.1541.0.0. Rien à essayer en jeu.
### English
- X-571: the « Fair lock 1309 » test of two bank stores on one file sometimes failed under load (BankLockBusyException after its 2 s wait). Cause measured by a probe (removed): the fair lock is taken in 0 to 2 ms; the stalls come from the disk, inside SQLite (one commit up to 772 ms, one close with the journal's checkpoint up to 1.7 s, one whole save up to 4.3 s, with or without load, with 10 rows as with hundreds). A 2 s wait is shorter than one such save of the store holding the lock. No data defect: the real store waits 10 s and a refusal writes nothing and says so. The test now waits 10 s (the store's value) and the busy store saves until stopped, past that wait: an unfair lock still fails. Each save's duration in the failure message.

## 0.1540.0
- Plan global, X-573 : X-573 M2 (relecture 0.1539) : quand toutes les méthodes du préchauffage sont précoces (ou qu'il n'y en a aucune), la fin de la phase précoce était mesurée (EarlyReadyAfter) mais EarlyDone ne passait à vrai que dans le finally, après la ligne du journal qui la dit prête : les deux sont posés ensemble. Constat corrigé en partie : EarlyDone ne restait pas faux pour toujours (le finally le posait), seulement jusqu'à la fin du rappel. Test de comportement WarmupEarly1540Tests (toutes précoces, aucune, une partie : état lu au moment de la ligne du journal). M1 de X-573 et X-572 faits hors version (fichiers de l'agent : verrou de la file, rejeu au plus une fois).
- Tests : WarmupEarly1540Tests. Verify.ps1 (agent) : 3082/3082, 0 avertissement, 0 erreur ; DLL 0.1540.0.0. Rien à essayer en jeu.
### English
- X-573 M2 (review 0.1539): when every warm-up method is early (or there is none), the early phase's end was measured (EarlyReadyAfter) but EarlyDone only became true in the finally, after the log line that says it ready: both are set together. Finding partly corrected: EarlyDone did not stay false for ever (the finally set it), only until the callback ended. Behaviour test WarmupEarly1540Tests (all early, none, part: state read when the log line is written). X-573 M1 and X-572 done outside a version (agent files: queue lock, replay at most once).

## 0.1539.0
- X-570 (audit du 17/11, partie A, points 6 et 9), partie suivie : Verify.ps1 ne cherche plus le SDK .NET dans l'ancien dossier C:\ia-code (le plus récent ~/.codex/dotnet-10.*, sinon dotnet) ; TESTING le dit. tools/client-snapshot.py prend le SDK par la même règle (plus de version écrite en dur, TENSHO_DOTNET accepté). .github/codeql/codeql-config.yml retiré : aucun workflow ne le lisait. Test ToolPaths1539Tests.
- Tests : ToolPaths1539Tests. Verify.ps1 (agent) : 3079/3079, 0 avertissement, 0 erreur ; DLL 0.1539.0.0. Rien à essayer en jeu.
### English
- X-570 (audit of 17/11, part A, items 6 and 9), tracked part: Verify.ps1 no longer looks for the .NET SDK in the old C:\ia-code folder (the newest ~/.codex/dotnet-10.*, else dotnet); TESTING says so. tools/client-snapshot.py picks the SDK by the same rule (no hard-coded version, TENSHO_DOTNET accepted). .github/codeql/codeql-config.yml removed: no workflow read it. Test ToolPaths1539Tests.
