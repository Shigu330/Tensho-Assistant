# CLEANUP-017 — réorganisation de la documentation et du dépôt

Demande utilisateur 2026-10-31. Objectif : documentation cohérente avec le code actuel, dépôt allégé, suivi et reprise IA complets,
accueil des contributeurs open source, prise en compte des règles de publication Dalamud (D17).

STATE: CLOSE côté agent (2026-10-31), relecture utilisateur demandée.

DONE (0.154.0, 2026-10-31):
- `checkpoints/` sorti de git (dossier local) ; preuves citées déplacées dans `docs/evidence/` et références mises à jour ;
  [CHANGELOG.md](../../CHANGELOG.md) généré depuis les notes de vérification (124 versions).
- Docs rangées : `docs/ai/` (pilotage agent), `docs/project/` (feuille de route, architecture, tests, références, décisions), `docs/goals/` (79 objectifs).
- Entrées réécrites : CLAUDE.md, docs/ai/STATUS.md, docs/ai/GOAL.md, docs/README.md, README.md, CONTRIBUTING.md.
- Réglages : onglet Paramètres › Carte ; les autres réglages de `Configuration` sont déjà dans Paramètres (le reste est des données de travail).
- Relecture de `docs/project/` contre le code : partie « état au 0.154.0 » dans ARCHITECTURE, TESTING, REFERENCES (services externes),
  ROADMAP, BACKLOG (ouvert) ; historique marqué comme archive ; décisions DEC-010 et DEC-011 ; 0 lien relatif cassé.

Non fait, volontairement : réécriture du contenu historique (daté) ; réécriture de l'historique git (seulement sur demande explicite) ;
soumission D17 (décision de l'utilisateur).
