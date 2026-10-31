# CLAUDE.md — Contexte agent (Claude, session cloud reliée au PC)

Ajouté le 2026-10-30. Complète RULES.md sans le remplacer ; en cas de conflit :
PROMPT-MAITRE > RULES.md > ce fichier.

## Ordre de lecture à chaque reprise
1. RULES.md → STATUS.md → GOAL.md (obligatoire, court).
2. Ensuite seulement le doc du GOAL (docs/<ID>.md) et le code ciblé.
3. ROADMAP / ARCHITECTURE / REFERENCES / TESTING : recherche ciblée (grep par ID,
   plugin ou symbole), jamais relus en entier sans raison.

## État de référence au 2026-10-30
- Livraison canonique : Release 0.75.0.0 — Verify.ps1 471/471 (dernier passage
  connu, refonte UI), zéro avertissement/erreur, paquet/SHA-256/langues verts.
- Refonte UI + localisation (EN/FR 1763 clés ; DE/JA partiels avec fallback EN) :
  CLOSE, validation en jeu rapportée par l'utilisateur.
- CACHE-001 : IMPLEMENTED / TESTED, validation en jeu du cache encore à faire.
- QUEST-ITEM-PROGRESS-007 : CLOSE (Verify.ps1 477/477, DLL 0.76.0.0, validation en
  jeu rapportée par l'utilisateur, 2026-10-30).
- Session desktop sur le PC (2026-10-30) : PowerShell disponible, Verify.ps1 exécutable
  par l'agent ; la contrainte « sans shell » ci-dessous ne vaut que pour la session cloud.
- Autres pistes du BACKLOG : SORT-NATIVE-002-STUDY. Les 17 plugins restent PARTIAL.
- Blocage connu non résolu : bonus natif d'Abondance (ne pas inventer de formule).

## Contraintes propres à cet environnement d'agent
- L'agent tourne dans un conteneur Linux cloud ; le dépôt est sur le PC Windows
  de l'utilisateur (C:\Plugins-ff14), accessible par copie (stage/commit).
- Sans shell sur le PC, l'agent NE PEUT PAS compiler (SDK .NET 10.0.403,
  Dalamud.NET.Sdk 15.0.0, Hooks/dev Windows) ni lancer scripts/Verify.ps1.
  → Tout changement de code livré dans ce mode est NON COMPILÉ / NON TESTÉ
  tant que l'utilisateur n'a pas exécuté `.\scripts\Verify.ps1` et transmis
  la sortie. Ne jamais écrire COMPILED/TESTED sans ce retour.
- Avant toute demande d'essai en jeu : build + tests exécutés et version de DLL
  effectivement produite indiquée (consigne utilisateur, RULES.md).
- Écriture des fichiers : modifier seulement ce qui change (pas de réécriture
  complète des gros MD), vérifier que la copie locale n'a pas changé avant de
  renvoyer un fichier (mtime).

## Consigne de compte rendu (utilisateur, 2026-10-30)
- À CHAQUE étape, donner : l'estimation du % restant du projet global (signalée
  comme estimation subjective, non mesurée) et la commande exacte de test à lancer
  quand un test est requis (par défaut `.\scripts\Verify.ps1` depuis C:\Plugins-ff14).

## Quota
- L'agent n'a pas accès au compteur : ne jamais estimer le quota restant.
- Réserves utilisateur les plus récentes (2026-10-30 ~20:55) : arrêt à 1 % restant
  sur 5 h OU 1 % restant sur la semaine (la réserve de 5 % sur 5 h est abandonnée). Aux jalons,
  demander à l'utilisateur de relever le compteur.

## Rappels de méthode
- Un seul GOAL actif, petit, vérifiable ; KEEP → EXTEND → REFACTOR → REPLACE → NEW.
- Inconnu ≠ zéro ; absence de preuve ≠ preuve d'absence ; portée et date explicites.
- Natif lu sur le thread framework puis copié ; rien de lourd dans Draw.
- Rapport de fin d'étape au format §66 du prompt maître, concis.
- Point de contrôle à CHAQUE version livrée (convention historique, rappel utilisateur
  2026-10-30) : dans checkpoints/, déposer X.Y.Z-verify.log (sortie Verify.ps1),
  X.Y.Z-verification.md et TenshoAssistant-X.Y.Z-source.zip (dépôt sans bin/obj ni
  anciennes archives) avant de demander un essai en jeu. Écart 0.76→0.90 : voir
  checkpoints/0.91.0-verification.md.
- Combat/ACT/IINACT : retiré du projet (décision utilisateur 2026-10-31), ne rien faire dessus.
