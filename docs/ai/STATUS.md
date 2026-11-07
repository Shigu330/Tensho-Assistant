# État courant (mis à jour 2026-11-04)

VERSION: 0.793.0.0 (dernière release : Stable-0.539.0, 2026-11-05) — Verify.ps1 (agent) 1366/1366, 0 avertissement, 0 erreur, langues et paquet verts. Détail par version : [CHANGELOG.md](../../CHANGELOG.md).
GOAL: finir le plan global, sur le dépôt personnalisé, sans soumission (DEC-039) ; voir [GOAL](GOAL.md) et [PUBLISHING](../project/PUBLISHING.md).
QUOTA: plus de réserve (utilisateur, 2026-11-05) : coder tant que possible, reprise tenue à jour ici. L'agent ne voit pas le compteur. Dernier relevé de l'utilisateur (2026-11-05) : 5 h 0 %, réinitialisation à 9:50 ; semaine 95 %, réinitialisation à 23:00 (une vingtaine de versions ≈ 1 % de la semaine).

Historique (rapports de validation un par un, anciennes reprises, couverture des 17 plugins au 2026-11-02) : [STATUS-ARCHIVE](STATUS-ARCHIVE.md).

## Où en est le projet
- Fait : files a) à g) de la [ROADMAP](../project/ROADMAP.md) (0.315–0.446), R-14 résistance aux mises à jour (0.452–0.454, 0.491–0.492, 0.507), solveur Raphael A-03 et A-04 (0.465, 0.509), modèles communautaires (0.476), auto-contrôle au rechargement (0.494–0.496), page dresseur (0.500), prise en main ONBOARD-001 (0.502–0.506), ventes des servants I-05 première version (0.508), cadre du jeu retiré et conseils en JcE seulement (0.510), panneaux libres et panneaux accrochés au jeu en fenêtres Dalamud (0.514, 0.516), aide à la récolte par nœud avec les effets du lieu (0.515).
- Abandonné : chat personnalisable ([DEC-030](../project/DECISIONS.md)).
- Mis de côté par l'utilisateur : dresseur, suite (sonde 0.501, rangs, panneau du Dédale ; [DEC-032](../project/DECISIONS.md)) ; I-14 et X-05.
- À faire avec une séance en jeu : I-15, suite de I-05, sondes de l'issue #23 ([G-PROPOSALS](../project/G-PROPOSALS.md) « Écarts laissés »).

## Validé en jeu
Sur rapports de l'utilisateur, détail dans [STATUS-ARCHIVE](STATUS-ARCHIVE.md) et les cases de [TESTING](../project/TESTING.md) :
- jusqu'à 0.223 : tout ce qui était à valider au 2026-11-01 ; 0.265–0.271, 0.276–0.279, 0.283–0.299 ; 0.303–0.306, 0.310–0.314 ;
- campagne guidée du 2026-11-04 : lots 1 à 9 (sur place, alarmes, ville, caserne, atelier, récolte et pêche, carte, Triple Triade, Mini Cactpot, revue de mode, collections, quêtes, contenus, compagnie). Lot 10 clos à la demande de l'utilisateur (« J'ai casiment tous essaie, les dernières erreurs on verra en jeu »). La chasse et le train de chasse (lot 7, points 12 à 16) restent à tester quand il en fera.
- 2026-11-07, sur rapport de l'utilisateur (« récolte ok pour ce que j'ai pu voir ») : aide à la récolte 0.728–0.734, ligne « Ensuite », cadre sur la barre, ordre des étapes ; les cas non vus (Sagesse du fermier après la première récolte, PR restants en fin de nœud) restent à revoir s'ils se présentent.
- 2026-11-07, sur capture de l'utilisateur (« c'est ok ») : ventes par jour et tri par marché de « Que puis-je fabriquer ? » (0.725), « ~X gils en 7 jours », toute la liste classée.
- 2026-11-07, sur captures de l'utilisateur : X-04 (objets des listes au point de récolte, cadre vert dans la fenêtre du jeu, 0.751) et X-05 (listes par zone de récolte, 0.752).
- 2026-11-07, sur captures de l'utilisateur : ventes des servants (I-05, 0.508) ; lien d'objet des alarmes, clic droit → menu Tensho (X-03, 0.774).
- Auto-contrôle, dernier passage relu dans dalamud.log : 27 ok, 4 vus avant, 0 non lu, 0 suspect, 0 échec.

## À valider en jeu
Liste complète : [TESTING](../project/TESTING.md), 284 cases ouvertes, aucune déclarée validée sans rapport ; elles se règlent quand l'utilisateur les signale en jouant. À essayer d'abord :
- 0.520 Sagesse du fermier dans le plan des collectionnables ; 0.519 éclats, cristaux et agrégats ; 0.518 carte : point à tome non lu ; 0.517 plan de PR ;
- 0.516 tous les panneaux accrochés au jeu (fenêtres Dalamud) ;
- 0.515 aide à la récolte : plan du nœud, ligne « Effet du lieu », cristaux ;
- 0.514 les 4 panneaux en fenêtres Dalamud, et la croix du guide et de la carte ;
- 0.508 ventes des servants, 0.509 coup conseillé pendant la synthèse ;
- 0.502–0.506 aide, guide, signaler un problème ; 0.511 aide qui suit la langue ;
- 0.497 fenêtre de comparaison, 0.499 carte au-dessus de Tensho, 0.500 page dresseur, 0.510 conseil de Triple Triade coupé en tournoi.
Dépôt personnel : installation et mise à jour fonctionnent (rapport de l'utilisateur, 2026-11-05, 0.539.0). Possible depuis cette release : galerie de Mes outils (0.476) et liste de vidéos (0.506), qui lisent la branche `main`.

## Prochaine action
0) [Plan global](../project/GLOBAL-PLAN.md) : phases 1 à 4 sans jouer, puis le jeu en dernier, un point à la fois ; la 1.0.0 après tout le plan (DEC-037). Feu vert donné le 2026-11-05 : enchaîner les phases 1 à 4 sans demander. Phase 1 finie. Reprise : X-33 : « Que faire avec ? » et les ventes par jour ; X-01 ; X-03. Pas de soumission au dépôt officiel (DEC-039).
1) Décisions de l'auteur sur les points Dalamud ([PUBLISHING](../project/PUBLISHING.md) « Ce qui reste », points 4 à 7 ; titre selon le job décidé, DEC-035), avec son accord à chaque règle.
2) Fait le 2026-11-05 : release Stable-0.539.0, sur demande de l'utilisateur. La prochaine : une séance de jeu sur la dernière version d'abord (DEC-033).
3) Vérifier en jeu les fenêtres converties (issue #38).
4) Passage en 1.0.0 après tout le plan global, au moment choisi par l'auteur. Pas de soumission au dépôt officiel (DEC-039) ; règles Dalamud gardées.
Après chaque rechargement en mode développeur, l'agent lit les lignes « Selftest » de dalamud.log.

## Git
Gestion par l'agent, accordée par l'utilisateur le 2026-11-03 (règle de [CLAUDE.md](../../CLAUDE.md)) :
- Commit : à la fin de chaque version (Verify vert, CHANGELOG, STATUS), `.\scripts\Verify.ps1 -Commit -DryRun`, puis `-Commit`, avec `$env:TENSHO_COMMIT_TRAILER` = la ligne `Co-Authored-By` de l'agent. Corps du message : notes anglaises du CHANGELOG. Détail : CONTRIBUTING « Versions and releases ».
- Release : jamais d'office ; sur demande de l'utilisateur : `-Release -DryRun`, puis `-Release` (repo.json, fusion dans main, tag `Stable-x.y.z`, release GitHub avec `latest.zip`, main poussée en dernier). `main` a plus de 64 commits de retard sur `Devloppement`.
- Jamais : push forcé, réécriture d'historique, `--no-verify`, fusion sur main hors de Release.ps1, réglages GitHub sans accord. Un arrêt (code 2) ou une erreur : rien n'est relancé à l'aveugle, l'agent rapporte le message.
- Avant chaque commit : aucun secret (adresse Discord, jeton) dans les fichiers suivis ; `checkpoints/`, `bin/`, `obj/` restent ignorés.
- GitHub, tenu par l'agent, en anglais : une issue est commentée quand une version la fait avancer, et fermée seulement sur preuve (Verify vert pour le code, rapport de l'utilisateur pour le jeu). Milestones : 2 modules, 3 travail de fond, « 1.0.0 — Dalamud publication ». Actions : `Build and test` (Windows, plus un job qui compile comme le dépôt Dalamud depuis 0.512), CodeQL, Dependabot. main et Devloppement protégées contre le push forcé et la suppression, tags `Stable-*` protégés (0.447).
- État relevé à chaque version : `gh issue list`, `gh run list`.

## Blocages connus
- Bonus d'Abondance : règle communautaire (frozen_rabbit_tome) appliquée comme estimation citée (0.224) ; à comparer en jeu.
- Plantage 08:19 (2026-10-31) causé par la lecture d'une infobulle de marqueur : lecture bornée depuis 0.133 ; surveiller ([extrait](../evidence/0.133.0-crash-excerpt.log)).
- Compilation Dalamud : le job CI garde le réseau, leur compilation non ; seule une vraie PR prouve le reste.
