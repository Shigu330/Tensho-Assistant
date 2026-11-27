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

## 0.1921.0
- Plan global, X-899 : Tests du verrou des écouteurs du runtime (relecture 0.1919, M2) : le cliquet de EventListenerGate1916Tests voit aussi un écouteur qui hérite d'EventListener par une classe de base à soi (« class X : MaBase »), dans toutes les sources lues (plugin, application de bureau, tests), pas seulement dans le code compilé dans les tests : la première base de chaque classe est lue, et une classe dont la base est un écouteur en est un aussi, de proche en proche d'un fichier à l'autre. Cas nouveau sur exemples (héritage indirect sur deux niveaux, à travers trois fichiers).
- Tests : EventListenerGate1916Tests. Verify.ps1 (agent) : 4254/4254, 0 avertissement, 0 erreur ; DLL 0.1921.0.0. Rien à essayer en jeu.
### English
- Tests of the runtime listeners' gate (review of 0.1919, M2): the ratchet of EventListenerGate1916Tests also sees a listener deriving from EventListener through a base class of its own (« class X : MyBase »), in every source read (plugin, desktop app, tests), not only in the code compiled into the tests: each class's first base is read, and a class whose base is a listener is one too, step by step across files. New case on samples (indirect inheritance over two steps, across three files).

## 0.1920.0
- Plan global, X-898 : Tests du verrou des écouteurs du runtime (relecture 0.1919, M1) : le cas des 6 fils de RuntimeGate1919Tests vérifie d'abord que les deux chargements ont le même verrou, et échoue tout de suite sinon, avant de créer un écouteur ; un verrou par chargement revenu ne fige plus tout le processus de tests (le rapport sort).
- Tests : RuntimeGate1919Tests. Verify.ps1 (agent) : 4253/4253, 0 avertissement, 0 erreur ; DLL 0.1920.0.0. Rien à essayer en jeu.
### English
- Tests of the runtime listeners' gate (review of 0.1919, M1): the 6 threads' case of RuntimeGate1919Tests first checks that both loads hold the same gate and fails at once otherwise, before any listener is made; a gate per load coming back no longer freezes the whole test process (the report comes out).

## 0.1919.0
- Plan global, X-897 : Mode développeur et outil de tests (relecture 0.1916, M2, déjà là avant le bloc) : le verrou des écouteurs des événements du runtime (X-891) était un champ statique de l'assembly du plugin, donc un par chargement (chaque rechargement de Dalamud ouvre un nouveau contexte de chargement) : un écouteur de l'ancien chargement fermé en retard et un écouteur créé par le nouveau prenaient deux verrous différents, et l'interblocage de X-891 restait possible à un rechargement, figeant le jeu. Le verrou est désormais un seul objet pour tout le processus, rangé dans les données du processus (AppDomain.SetData), partagé par chaque chargement du plugin et par l'application de bureau ; un simple object, qui ne retient aucun ancien contexte de chargement. L'écouteur des compilations du chat, nommé 2 s après la première image, a maintenant un propriétaire (la fenêtre) : sa fermeture le libère tout de suite au déchargement et le travail différé ne fait alors plus rien. Tests de comportement RuntimeGate1919Tests : un second chargement du même code dans son propre contexte reçoit le même verrou (échoue sur l'ancien code) ; des écouteurs des deux chargements créés et fermés par 6 fils à la fois finissent tous (sur l'ancien code, le processus de tests s'est figé, 0 % de processeur, arrêté à la main) ; la libération unique sous concurrence.
- Tests : RuntimeGate1919Tests. Verify.ps1 (agent) : 4253/4253, 0 avertissement, 0 erreur ; DLL 0.1919.0.0. Rien à essayer en jeu.
### English
- Developer mode and test tool (review of 0.1916, M2, already there before the block): the gate of the runtime's event listeners (X-891) was a static field of the plugin's assembly, so one per load (each Dalamud reload opens a new load context): a listener of the old load disposed late and one made by the new load took two different gates, and X-891's deadlock stayed possible at a reload, freezing the game. The gate is now one object for the whole process, kept in the process's data (AppDomain.SetData), shared by every load of the plugin and by the desktop app; a plain object, which keeps no old load context alive. The chat's compilation listener, named 2 s after the first frame, now has an owner (the window): its Dispose lets it go at once at an unload, and the later work then does nothing. Behaviour tests RuntimeGate1919Tests: a second load of the same code in its own context gets the same gate (fails on the old code); listeners of both loads made and disposed by 6 threads at once all end (on the old code, the test process froze, 0 % processor, stopped by hand); the single release under a race.
